using ChatApp.Interfaces;
using ChatApp.Models;

namespace ChatApp.Services
{

    public class ChatAssignmentService : IChatAssignmentService
    {
        private readonly ITeamService _teamService;
        private readonly ISessionQueueService _sessionQueueService;

        public ChatAssignmentService(ITeamService teamService, ISessionQueueService sessionQueueService)
        {
            _teamService = teamService;
            _sessionQueueService = sessionQueueService;
        }
        public async Task<bool> AssignChatToAgentAsync(int chatId)
        {
            var queuedChats = await _sessionQueueService.GetQueuedChatsAsync();
            var chat = queuedChats.FirstOrDefault(c => c.Id == chatId);

            if (chat == null || chat.Status != ChatStatus.Queued || !chat.IsActive)
                return false;

            _teamService.UpdateAgentShiftStatus();

            var activeTeams = _teamService.GetActiveTeams();
            var availableAgents = GetAvailableAgentsRoundRobin(activeTeams);

            if (!availableAgents.Any())
            {
                var totalCapacity = GetTotalCapacity(activeTeams);
                var queueStatus = await _sessionQueueService.GetQueueStatusAsync();
                var queueLength = queueStatus.TotalQueuedChats;
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

                var assignmentResult = await _sessionQueueService.AssignChatToAgentAsync(chatId, assignedAgent.Id);

                if (assignmentResult)
                {
                    assignedAgent.CurrentChatCount++;
                    assignedAgent.AssignedChatIds.Add(chat.Id);

                    if (assignedAgent.CurrentChatCount >= assignedAgent.MaxConcurrentChats)
                    {
                        assignedAgent.Status = AgentWorkStatus.Busy;
                    }

                    return true;
                }
            }

            return false;
        }
        public async Task<bool> CompleteChatAsync(int chatId)
        {
            var allChats = await _sessionQueueService.GetAllChatsAsync();
            var chat = allChats.FirstOrDefault(c => c.Id == chatId && c.Status == ChatStatus.InProgress);

            if (chat == null)
                return false;

            var completionResult = await _sessionQueueService.CompleteChatAsync(chatId);

            if (completionResult)
            {
                if (chat.AssignedAgent != null)
                {
                    chat.AssignedAgent.CurrentChatCount--;
                    chat.AssignedAgent.AssignedChatIds.Remove(chat.Id);

                    if (chat.AssignedAgent.Status == AgentWorkStatus.Busy &&
                        chat.AssignedAgent.Status != AgentWorkStatus.ShiftEnding)
                    {
                        chat.AssignedAgent.Status = AgentWorkStatus.Available;
                    }

                    if (chat.AssignedAgent.Status == AgentWorkStatus.ShiftEnding &&
                        chat.AssignedAgent.CurrentChatCount == 0)
                    {
                        chat.AssignedAgent.Status = AgentWorkStatus.Offline;
                    }
                }

                await ProcessQueueAsync();
                return true;
            }

            return false;
        }
        public async Task<ChatQueueStatus> GetQueueStatusAsync()
        {
            return await _sessionQueueService.GetQueueStatusAsync();
        }
        public async Task<List<ChatSession>> GetQueuedChatsAsync()
        {
            return await _sessionQueueService.GetQueuedChatsAsync();
        }
        public async Task<List<ChatSession>> GetAgentChatsAsync(int agentId)
        {
            var allChats = await _sessionQueueService.GetAllChatsAsync();
            return allChats.Where(c => c.AssignedAgentId == agentId && c.Status == ChatStatus.InProgress)
                          .ToList();
        }
        public async Task ProcessQueueAsync()
        {
            var queuedChats = await _sessionQueueService.GetQueuedChatsAsync();

            var activeQueuedChats = queuedChats
                .Where(c => c.IsActive && c.Status == ChatStatus.Queued)
                .OrderBy(c => c.CreatedAt)
                .ToList();

            foreach (var chat in activeQueuedChats)
            {
                await AssignChatToAgentAsync(chat.Id);
            }
        }

        public async Task<ChatAcceptanceResult> CanAcceptNewChatAsync()
        {
            _teamService.UpdateAgentShiftStatus();

            var activeTeams = _teamService.GetActiveTeams();
            var totalCapacity = GetTotalCapacity(activeTeams);

            var queueStatus = await _sessionQueueService.GetQueueStatusAsync();
            var currentQueueLength = queueStatus.TotalQueuedChats;
            var maxQueueLength = (int)(totalCapacity * 1.5);
            var isOfficeHours = _teamService.IsOfficeHours();

            if (currentQueueLength < maxQueueLength)
            {
                return new ChatAcceptanceResult
                {
                    CanAccept = true,
                    Reason = "Queue has available capacity",
                    CurrentQueueLength = currentQueueLength,
                    MaxQueueLength = maxQueueLength
                };
            }

            if (isOfficeHours)
            {
                var overflowTeam = _teamService.GetOverflowTeam();
                var isOverflowActive = overflowTeam.Agents.Any(a => a.Status != AgentWorkStatus.Offline);

                if (!isOverflowActive)
                {
                    ActivateOverflowTeam();
                    isOverflowActive = overflowTeam.Agents.Any(a => a.Status != AgentWorkStatus.Offline);
                }

                if (isOverflowActive)
                {
                    var overflowCapacity = overflowTeam.TotalCapacity;
                    var totalCapacityWithOverflow = totalCapacity + overflowCapacity;
                    var maxQueueWithOverflow = (int)(totalCapacityWithOverflow * 1.5);

                    if (currentQueueLength < maxQueueWithOverflow)
                    {
                        return new ChatAcceptanceResult
                        {
                            CanAccept = true,
                            Reason = "Overflow team activated - queue has capacity",
                            CurrentQueueLength = currentQueueLength,
                            MaxQueueLength = maxQueueWithOverflow,
                            IsOverflowActive = true
                        };
                    }
                    else
                    {
                        return new ChatAcceptanceResult
                        {
                            CanAccept = false,
                            Reason = "Both main queue and overflow queue are at maximum capacity",
                            CurrentQueueLength = currentQueueLength,
                            MaxQueueLength = maxQueueWithOverflow,
                            IsOverflowActive = true
                        };
                    }
                }
                else
                {
                    return new ChatAcceptanceResult
                    {
                        CanAccept = false,
                        Reason = "Main queue is full and overflow team could not be activated during office hours",
                        CurrentQueueLength = currentQueueLength,
                        MaxQueueLength = maxQueueLength,
                        IsOverflowActive = false
                    };
                }
            }
            else
            {
                return new ChatAcceptanceResult
                {
                    CanAccept = false,
                    Reason = "Main queue is full and overflow team is not available outside office hours",
                    CurrentQueueLength = currentQueueLength,
                    MaxQueueLength = maxQueueLength,
                    IsOverflowActive = false
                };
            }
        }

        private List<Agent> GetAvailableAgentsRoundRobin(IEnumerable<Team> teams)
        {
            var allAvailableAgents = new List<Agent>();

            foreach (var team in teams)
            {
                allAvailableAgents.AddRange(team.AvailableAgents);
            }

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
                if (agent.Status == AgentWorkStatus.Offline)
                {
                    agent.Status = AgentWorkStatus.Available;
                    agent.ShiftStartTime = DateTime.Now;
                    agent.ShiftEndTime = DateTime.Now.AddHours(8);
                }
            }
        }
    }
}
