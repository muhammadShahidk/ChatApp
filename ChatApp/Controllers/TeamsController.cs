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

        public TeamsController(ITeamService teamService)
        {
            _teamService = teamService;
        }

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
                return StatusCode(500, "Internal server error");
            }
        }

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
                return StatusCode(500, "Internal server error");
            }
        }

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
                return StatusCode(500, "Internal server error");
            }
        }

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
                return StatusCode(500, "Internal server error");
            }
        }

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
                return StatusCode(500, "Internal server error");
            }
        }

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
                return StatusCode(500, "Internal server error");
            }
        }

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
                return StatusCode(500, "Internal server error");
            }
        }
    }
}
