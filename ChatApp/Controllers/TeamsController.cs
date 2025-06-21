using Microsoft.AspNetCore.Mvc;
using ChatApp.Models;
using ChatApp.Services;

namespace ChatApp.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TeamsController : ControllerBase
    {
        private readonly ITeamService _teamService;
        private readonly ILogger<TeamsController> _logger;

        public TeamsController(ITeamService teamService, ILogger<TeamsController> logger)
        {
            _teamService = teamService;
            _logger = logger;
        }

        /// <summary>
        /// Get all teams and their agents
        /// </summary>
        [HttpGet]
        public ActionResult<List<Team>> GetAllTeams()
        {
            try
            {
                var teams = _teamService.GetAllTeams();
                return Ok(teams);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting all teams");
                return StatusCode(500, "Internal server error");
            }
        }

        /// <summary>
        /// Get active teams (currently working)
        /// </summary>
        [HttpGet("active")]
        public ActionResult<List<Team>> GetActiveTeams()
        {
            try
            {
                var activeTeams = _teamService.GetActiveTeams();
                return Ok(activeTeams);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting active teams");
                return StatusCode(500, "Internal server error");
            }
        }

        /// <summary>
        /// Get specific team by ID
        /// </summary>
        [HttpGet("{teamId}")]
        public ActionResult<Team> GetTeam(string teamId)
        {
            try
            {
                var team = _teamService.GetTeam(teamId);
                if (team == null)
                {
                    return NotFound(new { message = $"Team {teamId} not found" });
                }
                return Ok(team);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting team {TeamId}", teamId);
                return StatusCode(500, "Internal server error");
            }
        }

        /// <summary>
        /// Get overflow team status
        /// </summary>
        [HttpGet("overflow")]
        public ActionResult<Team> GetOverflowTeam()
        {
            try
            {
                var overflowTeam = _teamService.GetOverflowTeam();
                return Ok(overflowTeam);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting overflow team");
                return StatusCode(500, "Internal server error");
            }
        }

        /// <summary>
        /// Update agent shift statuses
        /// </summary>
        [HttpPost("update-shifts")]
        public ActionResult UpdateShifts()
        {
            try
            {
                _teamService.UpdateAgentShiftStatus();
                return Ok(new { message = "Agent shift statuses updated successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating agent shifts");
                return StatusCode(500, "Internal server error");
            }
        }

        /// <summary>
        /// Check if it's currently office hours
        /// </summary>
        [HttpGet("office-hours")]
        public ActionResult<bool> IsOfficeHours()
        {
            try
            {
                var isOfficeHours = _teamService.IsOfficeHours();
                return Ok(new { isOfficeHours, currentTime = DateTime.Now });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking office hours");
                return StatusCode(500, "Internal server error");
            }
        }

        /// <summary>
        /// Reinitialize all teams (for testing/demo purposes)
        /// </summary>
        [HttpPost("reinitialize")]
        public ActionResult ReinitializeTeams()
        {
            try
            {
                _teamService.InitializeTeams();
                return Ok(new { message = "Teams reinitialized successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error reinitializing teams");
                return StatusCode(500, "Internal server error");
            }
        }
    }
}
