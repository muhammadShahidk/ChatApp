using ChatApp.Models;

namespace ChatApp.Services
{
    public interface IChatAssignmentService
    {
        Task<ChatSession> CreateChatSessionAsync(string customerId, string customerName);
        Task<bool> AssignChatToAgentAsync(int chatId);
        Task<bool> CompleteChatAsync(int chatId);
        Task<ChatQueueStatus> GetQueueStatusAsync();
        Task<List<ChatSession>> GetQueuedChatsAsync();
        Task<List<ChatSession>> GetAgentChatsAsync(int agentId);
        Task ProcessQueueAsync();
    }

    public class ChatAssignmentService : IChatAssignmentService
    {
        private readonly ITeamService _teamService;
        private readonly List<ChatSession> _chatSessions;
        private readonly Queue<ChatSession> _chatQueue;
        private int _nextChatId = 1;

        public ChatAssignmentService(ITeamService teamService)
        {
            _teamService = teamService;
            _chatSessions = new List<ChatSession>();
            _chatQueue = new Queue<ChatSession>();
        }

        public async Task<ChatSession> CreateChatSessionAsync(string customerId, string customerName)
        {
            var chatSession = new ChatSession
            {
                Id = _nextChatId++,
                CustomerId = customerId,
                CustomerName = customerName,
                Status = ChatStatus.Queued,
                CreatedAt = DateTime.Now
            };

            _chatSessions.Add(chatSession);
            _chatQueue.Enqueue(chatSession);

            // Try to assign immediately
            await AssignChatToAgentAsync(chatSession.Id);

            return chatSession;
        }

        public async Task<bool> AssignChatToAgentAsync(int chatId)
        {
            var chat = _chatSessions.FirstOrDefault(c => c.Id == chatId);
            if (chat == null || chat.Status != ChatStatus.Queued)
                return false;

            // Update agent shift statuses first
            _teamService.UpdateAgentShiftStatus();

            // Get active teams and their available agents
            var activeTeams = _teamService.GetActiveTeams();
            var availableAgents = GetAvailableAgentsRoundRobin(activeTeams);

            if (!availableAgents.Any())
            {
                // Check if we need to activate overflow team
                var totalCapacity = GetTotalCapacity(activeTeams);
                var queueLength = _chatQueue.Count;
                var maxQueueLength = (int)(totalCapacity * 1.5);

                if (queueLength >= maxQueueLength && _teamService.IsOfficeHours())
                {
                    ActivateOverflowTeam();
                    availableAgents = GetAvailableAgentsRoundRobin(activeTeams.Concat(new[] { _teamService.GetOverflowTeam() }));
                }
            }

            if (availableAgents.Any())
            {
                var assignedAgent = availableAgents.First();
                
                // Assign chat to agent
                chat.AssignedAgentId = assignedAgent.Id;
                chat.AssignedAgent = assignedAgent;
                chat.Status = ChatStatus.InProgress;
                chat.AssignedAt = DateTime.Now;
                chat.TeamId = assignedAgent.TeamId;

                // Update agent's chat count and assigned chats
                assignedAgent.CurrentChatCount++;
                assignedAgent.AssignedChatIds.Add(chat.Id);                if (assignedAgent.CurrentChatCount >= assignedAgent.MaxConcurrentChats)
                {
                    assignedAgent.Status = AgentWorkStatus.Busy;
                }

                // Remove from queue
                var queueItems = _chatQueue.ToList();
                _chatQueue.Clear();
                foreach (var item in queueItems.Where(i => i.Id != chatId))
                {
                    _chatQueue.Enqueue(item);
                }

                return true;
            }

            return false;
        }

        public async Task<bool> CompleteChatAsync(int chatId)
        {
            var chat = _chatSessions.FirstOrDefault(c => c.Id == chatId);
            if (chat == null || chat.Status != ChatStatus.InProgress)
                return false;

            // Update chat status
            chat.Status = ChatStatus.Completed;
            chat.CompletedAt = DateTime.Now;

            // Update agent availability
            if (chat.AssignedAgent != null)
            {
                chat.AssignedAgent.CurrentChatCount--;
                chat.AssignedAgent.AssignedChatIds.Remove(chat.Id);                // If agent was busy and now has capacity, make them available (unless shift is ending)
                if (chat.AssignedAgent.Status == AgentWorkStatus.Busy && 
                    chat.AssignedAgent.Status != AgentWorkStatus.ShiftEnding)
                {
                    chat.AssignedAgent.Status = AgentWorkStatus.Available;
                }

                // If agent's shift ended and they have no more chats, make them offline
                if (chat.AssignedAgent.Status == AgentWorkStatus.ShiftEnding && 
                    chat.AssignedAgent.CurrentChatCount == 0)
                {
                    chat.AssignedAgent.Status = AgentWorkStatus.Offline;
                }
            }

            // Try to assign queued chats
            await ProcessQueueAsync();

            return true;
        }

        public async Task<ChatQueueStatus> GetQueueStatusAsync()
        {
            _teamService.UpdateAgentShiftStatus();
            
            var activeTeams = _teamService.GetActiveTeams();
            var overflowTeam = _teamService.GetOverflowTeam();
            var isOverflowActive = overflowTeam.Agents.Any(a => a.Status != Models.AgentStatus.Offline);

            var allTeams = activeTeams.ToList();
            if (isOverflowActive)
                allTeams.Add(overflowTeam);

            var totalCapacity = GetTotalCapacity(allTeams);

            return new ChatQueueStatus
            {
                TotalQueuedChats = _chatQueue.Count,
                TotalCapacity = totalCapacity,
                MaxQueueLength = (int)(totalCapacity * 1.5),
                IsOverflowActive = isOverflowActive,
                TeamStatuses = allTeams.Select(MapTeamToStatus).ToList(),
                LastUpdated = DateTime.Now
            };
        }

        public async Task<List<ChatSession>> GetQueuedChatsAsync()
        {
            return _chatQueue.ToList();
        }

        public async Task<List<ChatSession>> GetAgentChatsAsync(int agentId)
        {
            return _chatSessions
                .Where(c => c.AssignedAgentId == agentId && c.Status == ChatStatus.InProgress)
                .ToList();
        }

        public async Task ProcessQueueAsync()
        {
            var queuedChats = _chatQueue.ToList();
            foreach (var chat in queuedChats)
            {
                await AssignChatToAgentAsync(chat.Id);
            }
        }

        private List<Agent> GetAvailableAgentsRoundRobin(IEnumerable<Team> teams)
        {
            var allAvailableAgents = new List<Agent>();

            foreach (var team in teams)
            {
                allAvailableAgents.AddRange(team.AvailableAgents);
            }

            // Sort by seniority (junior first) then by current chat count for fair distribution
            return allAvailableAgents
                .OrderBy(a => a.Seniority)
                .ThenBy(a => a.CurrentChatCount)
                .ToList();
        }

        private int GetTotalCapacity(IEnumerable<Team> teams)
        {
            return teams.Sum(t => t.TotalCapacity);
        }

        private void ActivateOverflowTeam()
        {
            var overflowTeam = _teamService.GetOverflowTeam();
            foreach (var agent in overflowTeam.Agents)
            {
                if (agent.Status == Models.AgentStatus.Offline)
                {
                    agent.Status = Models.AgentStatus.Available;
                    agent.ShiftStartTime = DateTime.Now;
                    agent.ShiftEndTime = DateTime.Now.AddHours(8); // 8-hour shift
                }
            }
        }

        private TeamStatus MapTeamToStatus(Team team)
        {
            return new TeamStatus
            {
                TeamId = team.Id,
                TeamName = team.Name,
                AvailableAgents = team.Agents.Count(a => a.Status == Models.AgentStatus.Available),
                TotalAgents = team.Agents.Count,
                TeamCapacity = team.TotalCapacity,
                ActiveChats = team.Agents.Sum(a => a.CurrentChatCount),
                IsActive = team.Shift == null || team.Agents.Any(a => a.Status != Models.AgentStatus.Offline),
                AgentStatuses = team.Agents.Select(a => new Models.AgentStatus
                {
                    AgentId = a.Id,
                    AgentName = a.Name,
                    Seniority = a.Seniority,
                    Status = a.Status,
                    CurrentChats = a.CurrentChatCount,
                    MaxChats = a.MaxConcurrentChats,
                    ShiftEndTime = a.ShiftEndTime
                }).ToList()
            };
        }
    }
}
