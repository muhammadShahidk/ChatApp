using ChatApp.Interfaces;
using ChatApp.Models;

namespace ChatApp.Services
{
    public class SessionQueueService : ISessionQueueService
    {
        private readonly ITeamService _teamService;
        private readonly ISessionMonitorService _sessionMonitorService;
        private readonly List<ChatSession> _chatSessions;
        private readonly Queue<ChatSession> _sessionsQueue;
        private int _nextChatId = 1;

        public SessionQueueService(ITeamService teamService, ISessionMonitorService sessionMonitorService)
        {
            _teamService = teamService;
            _sessionMonitorService = sessionMonitorService;
            _chatSessions = new List<ChatSession>();
            _sessionsQueue = new Queue<ChatSession>();
        }
        public async Task<ChatSession> CreateChatSessionAsync(string customerId, string customerName)
        {
            // First, check if we can accept new chats based on queue capacity rules
            var canAcceptChat = await CanAcceptNewChatAsync();
            if (!canAcceptChat.CanAccept)
            {
                throw new InvalidOperationException($"Chat refused: {canAcceptChat.Reason}");
            }
            var chatSession = new ChatSession
            {
                Id = _nextChatId++,
                CustomerId = customerId,
                CustomerName = customerName,
                Status = ChatStatus.Queued,
                CreatedAt = DateTime.Now
            };

            _chatSessions.Add(chatSession);
            _sessionsQueue.Enqueue(chatSession);

            // Register the session for monitoring
            await _sessionMonitorService.RegisterSessionAsync(chatSession);

            // Try to assign immediately
            //await AssignChatToAgentAsync(chatSession.Id);

            return chatSession;
        }




        public async Task<ChatQueueStatus> GetQueueStatusAsync()
        {
            _teamService.UpdateAgentShiftStatus();

            var activeTeams = _teamService.GetActiveTeams();
            var overflowTeam = _teamService.GetOverflowTeam();
            var isOverflowActive = overflowTeam.Agents.Any(a => a.Status != AgentWorkStatus.Offline);

            var allTeams = activeTeams.ToList();
            if (isOverflowActive)
                allTeams.Add(overflowTeam);

            var totalCapacity = GetTotalCapacity(allTeams); // need to ceck the capicity

            return new ChatQueueStatus
            {
                TotalQueuedChats = _sessionsQueue.Count,
                TotalCapacity = totalCapacity,
                MaxQueueLength = (int)(totalCapacity * 1.5),
                IsOverflowActive = isOverflowActive,
                TeamStatuses = allTeams.Select(MapTeamToStatus).ToList(),
                LastUpdated = DateTime.Now
            };
        }
        public async Task<List<ChatSession>> GetQueuedChatsAsync()
        {
            // Only return active sessions in the queue
            //return _sessionsQueue.Where(s => s.IsActive && s.Status == ChatStatus.Queued).ToList();
            return _sessionsQueue.Where(s => s.Status ==  ChatStatus.Queued).ToList();
        }

        public async Task<List<ChatSession>> GetAllChatsAsync()
        {
            return await Task.FromResult(_chatSessions.ToList());
        }

        public async Task<bool> AssignChatToAgentAsync(int chatId, int agentId)
        {
            var chat = _chatSessions.FirstOrDefault(c => c.Id == chatId);
            if (chat == null || chat.Status != ChatStatus.Queued || !chat.IsActive)
                return false;

            // Find the agent from team service
            var allTeams = _teamService.GetActiveTeams().Concat(new[] { _teamService.GetOverflowTeam() });
            var agent = allTeams.SelectMany(t => t.Agents).FirstOrDefault(a => a.Id == agentId);
            
            if (agent == null)
                return false;

            // Assign chat to agent
            chat.AssignedAgentId = agentId;
            chat.AssignedAgent = agent;
            chat.Status = ChatStatus.InProgress;
            chat.AssignedAt = DateTime.Now;
            chat.TeamId = agent.TeamId;

            // Remove from queue
            var queueItems = _sessionsQueue.ToList();
            _sessionsQueue.Clear();
            foreach (var item in queueItems.Where(i => i.Id != chatId))
            {
                _sessionsQueue.Enqueue(item);
            }

            return await Task.FromResult(true);
        }

        public async Task<bool> CompleteChatAsync(int chatId)
        {
            var chat = _chatSessions.FirstOrDefault(c => c.Id == chatId);
            if (chat == null || chat.Status != ChatStatus.InProgress)
                return false;

            // Update chat status
            chat.Status = ChatStatus.Completed;
            chat.CompletedAt = DateTime.Now;

            // Unregister from monitoring service
            await _sessionMonitorService.UnregisterSessionAsync(chatId);

            return await Task.FromResult(true);
        }





        /// <summary>
        /// Determines if a new chat can be accepted based on queue capacity rules
        /// Implementation of: "Once the session queue is full, unless it's during office hours and an overflow is available. The chat is refused."
        /// </summary>
        public async Task<ChatAcceptanceResult> CanAcceptNewChatAsync()
        {
            _teamService.UpdateAgentShiftStatus();

            var activeTeams = _teamService.GetActiveTeams();
            var totalCapacity = GetTotalCapacity(activeTeams);
            var currentQueueLength = _sessionsQueue.Count;
            var maxQueueLength = (int)(totalCapacity * 1.5); //remove 1.5 multiplier
            //var maxQueueLength =GetMaxCapacity(activeTeams); //remove 1.5 multiplier
            var isOfficeHours = _teamService.IsOfficeHours();

            // Rule 1: If queue is not full, accept the chat
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

            // Rule 2: Queue is full - check if overflow is available during office hours
            if (isOfficeHours)
            {
                var overflowTeam = _teamService.GetOverflowTeam();
                var isOverflowActive = overflowTeam.Agents.Any(a => a.Status != AgentWorkStatus.Offline);

                if (!isOverflowActive)
                {
                    // Overflow team not active, try to activate it
                    ActivateOverflowTeam();
                    isOverflowActive = overflowTeam.Agents.Any(a => a.Status != AgentWorkStatus.Offline);
                }

                if (isOverflowActive)
                {
                    // Calculate overflow capacity
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
                        // Rule 4: "Same rules applies for overflow; once full, the chat is refused."
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
                // Rule 5: Not office hours and queue is full - refuse chat
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


        private int GetTotalCapacity(IEnumerable<Team> teams)
        {
            return teams.Sum(t => t.TotalCapacity);
        }
        private int GetMaxCapacity(IEnumerable<Team> teams)
        {
            return teams.Sum(t => t.MaxQueueLength);
        }


        private void ActivateOverflowTeam()  //kyia new kik off team ki shift time whan sy start ho gi jahan sy unhon ny join kyia 
        {
            var overflowTeam = _teamService.GetOverflowTeam();
            foreach (var agent in overflowTeam.Agents)
            {
                if (agent.Status == AgentWorkStatus.Offline)
                {
                    agent.Status = AgentWorkStatus.Available;
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
                AvailableAgents = team.Agents.Count(a => a.Status == AgentWorkStatus.Available),
                TotalAgents = team.Agents.Count,
                TeamCapacity = team.TotalCapacity,
                ActiveChats = team.Agents.Sum(a => a.CurrentChatCount),
                IsActive = team.Shift == null || team.Agents.Any(a => a.Status != AgentWorkStatus.Offline),
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
