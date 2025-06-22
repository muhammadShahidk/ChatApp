using ChatApp.Models;

namespace ChatApp.Services
{
    public interface IChatAssignmentEngine
    {
        Task<bool> AssignNextChatAsync();
        Task ProcessActiveSessionsAsync();
        Agent? FindNextAvailableAgent();
        bool AssignChatToAgent(ChatSession chat, Agent agent);
    }

    //public class ChatAssignmentEngine : IChatAssignmentEngine
    //{
    //    private readonly ITeamService _teamService;
    //    private readonly ISessionMonitorService _sessionMonitorService;
    //    private readonly ILogger<ChatAssignmentEngine> _logger;

    //    public ChatAssignmentEngine(
    //        ITeamService teamService, 
    //        ISessionMonitorService sessionMonitorService,
    //        ILogger<ChatAssignmentEngine> logger)
    //    {
    //        _teamService = teamService;
    //        _sessionMonitorService = sessionMonitorService;
    //        _logger = logger;
    //    }

    //    /// <summary>
    //    /// Main assignment logic: Pick active sessions and assign to agents
    //    /// Implements: "Once a chat request enters the queue a service will pick and assign the chat to the next available agent"
    //    /// </summary>
    //    public async Task ProcessActiveSessionsAsync()
    //    {
    //        // Get only active sessions from the session monitor
    //        var activeSessions = _sessionMonitorService.GetActiveSessions()
    //            .Where(s => s.Status == ChatStatus.Queued)
    //            .OrderBy(s => s.CreatedAt) // FIFO order
    //            .ToList();

    //        _logger.LogDebug("Processing {Count} active sessions for assignment", activeSessions.Count);

    //        foreach (var session in activeSessions)
    //        {
    //            var assigned = await AssignChatToAvailableAgent(session);
    //            if (assigned)
    //            {
    //                _logger.LogInformation("Session {ChatId} assigned to agent", session.Id);
    //            }
    //            else
    //            {
    //                _logger.LogDebug("No available agent for session {ChatId}", session.Id);
    //                break; // No point checking more if no agents available
    //            }
    //        }
    //    }

    //    public async Task<bool> AssignNextChatAsync()
    //    {
    //        var activeSessions = _sessionMonitorService.GetActiveSessions()
    //            .Where(s => s.Status == ChatStatus.Queued)
    //            .OrderBy(s => s.CreatedAt)
    //            .FirstOrDefault();

    //        if (activeSessions != null)
    //        {
    //            return await AssignChatToAvailableAgent(activeSessions);
    //        }

    //        return false;
    //    }

    //    /// <summary>
    //    /// Find next available agent using round-robin assignment
    //    /// Implements: "Chats are assigned in a round robin fashion, preferring to assign the junior first"
    //    /// </summary>
    //    public Agent? FindNextAvailableAgent()
    //    {
    //        _teamService.UpdateAgentShiftStatus();
            
    //        var activeTeams = _teamService.GetActiveTeams();
    //        var allAvailableAgents = new List<Agent>();

    //        // Collect all available agents from active teams
    //        foreach (var team in activeTeams)
    //        {
    //            allAvailableAgents.AddRange(team.AvailableAgents);
    //        }

    //        // Check if overflow team should be included
    //        if (allAvailableAgents.Count == 0 && _teamService.IsOfficeHours())
    //        {
    //            var overflowTeam = _teamService.GetOverflowTeam();
    //            // Activate overflow if not already active
    //            if (!overflowTeam.Agents.Any(a => a.Status != AgentWorkStatus.Offline))
    //            {
    //                ActivateOverflowTeam(overflowTeam);
    //            }
    //            allAvailableAgents.AddRange(overflowTeam.AvailableAgents);
    //        }

    //        // Round-robin: Junior first, then by current chat count
    //        return allAvailableAgents
    //            .OrderBy(a => a.Seniority) // Junior = 0, Mid = 1, Senior = 2, TeamLead = 3
    //            .ThenBy(a => a.CurrentChatCount)
    //            .ThenBy(a => a.Id) // Tie breaker for consistent ordering
    //            .FirstOrDefault();
    //    }

    //    /// <summary>
    //    /// Assign chat to specific agent and add to agent's queue
    //    /// </summary>
    //    public bool AssignChatToAgent(ChatSession chat, Agent agent)
    //    {
    //        if (!agent.CanTakeNewChat)
    //        {
    //            return false;
    //        }

    //        // Assign chat to agent
    //        chat.AssignedAgentId = agent.Id;
    //        chat.AssignedAgent = agent;
    //        chat.Status = ChatStatus.InProgress;
    //        chat.AssignedAt = DateTime.Now;
    //        chat.TeamId = agent.TeamId;

    //        // Add to agent's queue
    //        agent.AgentQueue.Enqueue(chat);
    //        agent.CurrentChatCount++;
    //        agent.AssignedChatIds.Add(chat.Id);

    //        // Update agent status if at capacity
    //        if (agent.CurrentChatCount >= agent.MaxConcurrentChats)
    //        {
    //            agent.Status = AgentWorkStatus.Busy;
    //        }

    //        _logger.LogInformation("Chat {ChatId} assigned to agent {AgentId} ({AgentName})", 
    //            chat.Id, agent.Id, agent.Name);

    //        return true;
    //    }

    //    private async Task<bool> AssignChatToAvailableAgent(ChatSession session)
    //    {
    //        var agent = FindNextAvailableAgent();
    //        if (agent != null)
    //        {
    //            return AssignChatToAgent(session, agent);
    //        }
    //        return false;
    //    }

    //    private void ActivateOverflowTeam(Team overflowTeam)
    //    {
    //        foreach (var agent in overflowTeam.Agents)
    //        {
    //            if (agent.Status == AgentWorkStatus.Offline)
    //            {
    //                agent.Status = AgentWorkStatus.Available;
    //                agent.ShiftStartTime = DateTime.Now;
    //                agent.ShiftEndTime = DateTime.Now.AddHours(8);
    //            }
    //        }
    //        _logger.LogInformation("Overflow team activated with {Count} agents", overflowTeam.Agents.Count);
    //    }
    //}
}
