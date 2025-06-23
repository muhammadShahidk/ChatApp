using ChatApp.Models;

namespace ChatApp.Services
{
    public interface ITeamService
    {
        List<Team> GetAllTeams();
        Team? GetTeam(string teamId);
        List<Team> GetActiveTeams();
        Team GetOverflowTeam();
        void InitializeTeams();
        void UpdateAgentShiftStatus();
        bool IsOfficeHours();
    }

    public class TeamService : ITeamService
    {
        private readonly List<Team> _teams;
        private readonly Team _overflowTeam;

        public TeamService()
        {
            _teams = new List<Team>();
            _overflowTeam = CreateOverflowTeam();
            InitializeTeams();
        }

        public void InitializeTeams()
        {
            _teams.Clear();
            
            var teamA = new Team
            {
                Id = "TEAM_A",
                Name = "Team A",
                Shift = null,
                Agents = new List<Agent>
                {
                    new Agent
                    {
                        Id = 1,
                        Name = "TeamLead A1",
                        Seniority = Seniority.TeamLead,
                        Status = Models.AgentWorkStatus.Available,
                        TeamId = "TEAM_A",
                        CurrentShift = GetCurrentShift(),
                        ShiftStartTime = GetShiftStartTime(),
                        ShiftEndTime = GetShiftEndTime()
                    },
                    new Agent
                    {
                        Id = 2,
                        Name = "MidLevel A1",
                        Seniority = Seniority.MidLevel,
                        Status = Models.AgentWorkStatus.Available,
                        TeamId = "TEAM_A",
                        CurrentShift = GetCurrentShift(),
                        ShiftStartTime = GetShiftStartTime(),
                        ShiftEndTime = GetShiftEndTime()
                    },
                    new Agent
                    {
                        Id = 3,
                        Name = "MidLevel A2",
                        Seniority = Seniority.MidLevel,
                        Status = Models.AgentWorkStatus.Available,
                        TeamId = "TEAM_A",
                        CurrentShift = GetCurrentShift(),
                        ShiftStartTime = GetShiftStartTime(),
                        ShiftEndTime = GetShiftEndTime()
                    },
                    new Agent
                    {
                        Id = 4,
                        Name = "Junior A1",
                        Seniority = Seniority.Junior,
                        Status = Models.AgentWorkStatus.Available,
                        TeamId = "TEAM_A",
                        CurrentShift = GetCurrentShift(),
                        ShiftStartTime = GetShiftStartTime(),
                        ShiftEndTime = GetShiftEndTime()
                    }
                }
            };

            var teamB = new Team
            {
                Id = "TEAM_B",
                Name = "Team B",
                Shift = null,
                Agents = new List<Agent>
                {
                    new Agent
                    {
                        Id = 5,
                        Name = "Senior B1",
                        Seniority = Seniority.Senior,
                        Status = Models.AgentWorkStatus.Available,
                        TeamId = "TEAM_B",
                        CurrentShift = GetCurrentShift(),
                        ShiftStartTime = GetShiftStartTime(),
                        ShiftEndTime = GetShiftEndTime()
                    },
                    new Agent
                    {
                        Id = 6,
                        Name = "MidLevel B1",
                        Seniority = Seniority.MidLevel,
                        Status = Models.AgentWorkStatus.Available,
                        TeamId = "TEAM_B",
                        CurrentShift = GetCurrentShift(),
                        ShiftStartTime = GetShiftStartTime(),
                        ShiftEndTime = GetShiftEndTime()
                    },
                    new Agent
                    {
                        Id = 7,
                        Name = "Junior B1",
                        Seniority = Seniority.Junior,
                        Status = Models.AgentWorkStatus.Available,
                        TeamId = "TEAM_B",
                        CurrentShift = GetCurrentShift(),
                        ShiftStartTime = GetShiftStartTime(),
                        ShiftEndTime = GetShiftEndTime()
                    },
                    new Agent
                    {
                        Id = 8,
                        Name = "Junior B2",
                        Seniority = Seniority.Junior,
                        Status = Models.AgentWorkStatus.Available,
                        TeamId = "TEAM_B",
                        CurrentShift = GetCurrentShift(),
                        ShiftStartTime = GetShiftStartTime(),
                        ShiftEndTime = GetShiftEndTime()
                    }
                }
            };

            var teamC = new Team
            {
                Id = "TEAM_C",
                Name = "Team C (Night Shift)",
                Shift = Shift.Night,
                Agents = new List<Agent>
                {
                    new Agent
                    {
                        Id = 9,
                        Name = "MidLevel C1",
                        Seniority = Seniority.MidLevel,
                        Status = Models.AgentWorkStatus.Available,
                        TeamId = "TEAM_C",
                        CurrentShift = Shift.Night,
                        ShiftStartTime = GetShiftStartTime(Shift.Night),
                        ShiftEndTime = GetShiftEndTime(Shift.Night)
                    },
                    new Agent
                    {
                        Id = 10,
                        Name = "MidLevel C2",
                        Seniority = Seniority.MidLevel,
                        Status = Models.AgentWorkStatus.Available,
                        TeamId = "TEAM_C",
                        CurrentShift = Shift.Night,
                        ShiftStartTime = GetShiftStartTime(Shift.Night),
                        ShiftEndTime = GetShiftEndTime(Shift.Night)
                    }
                }
            };

            _teams.Add(teamA);
            _teams.Add(teamB);
            _teams.Add(teamC);

            UpdateAgentShiftStatus();
        }

        private Team CreateOverflowTeam()
        {
            var overflowAgents = new List<Agent>();
            for (int i = 1; i <= 6; i++)
            {
                overflowAgents.Add(new Agent
                {
                    Id = 100 + i,
                    Name = $"Overflow Agent {i}",
                    Seniority = Seniority.Junior,
                    Status = Models.AgentWorkStatus.Offline,
                    TeamId = "OVERFLOW",
                    IsOverflowTeam = true,
                    CurrentShift = GetCurrentShift(),
                    ShiftStartTime = GetShiftStartTime(),
                    ShiftEndTime = GetShiftEndTime()
                });
            }

            return new Team
            {
                Id = "OVERFLOW",
                Name = "Overflow Team",
                Shift = null,
                IsOverflowTeam = true,
                Agents = overflowAgents
            };
        }

        public List<Team> GetAllTeams()
        {
            var allTeams = new List<Team>(_teams);
            allTeams.Add(_overflowTeam);
            return allTeams;
        }

        public Team? GetTeam(string teamId)
        {
            return teamId == "OVERFLOW" ? _overflowTeam : _teams.FirstOrDefault(t => t.Id == teamId);
        }

        public List<Team> GetActiveTeams()
        {
            var activeTeams = new List<Team>();
            var currentTime = DateTime.Now;

            foreach (var team in _teams)
            {
                if (team.Shift == null)
                {
                    activeTeams.Add(team);
                }
                else if (IsTeamShiftActive(team.Shift.Value, currentTime))
                {
                    activeTeams.Add(team);
                }
            }

            return activeTeams;
        }

        public Team GetOverflowTeam()
        {
            return _overflowTeam;
        }

        public void UpdateAgentShiftStatus()
        {
            var currentTime = DateTime.Now;

            foreach (var team in _teams)
            {
                foreach (var agent in team.Agents)
                {
                    if (currentTime >= agent.ShiftEndTime.AddMinutes(-30))
                    {
                        if (agent.Status == Models.AgentWorkStatus.Available)
                        {
                            agent.Status = Models.AgentWorkStatus.ShiftEnding;
                        }
                    }
                    else if (currentTime >= agent.ShiftEndTime)
                    {
                        if (agent.CurrentChatCount == 0)
                        {
                            agent.Status = Models.AgentWorkStatus.Offline;
                        }
                    }
                   
                }
            }
        }

        public bool IsOfficeHours()
        {
            var currentTime = DateTime.Now;
            var hour = currentTime.Hour;
            //return hour >= 8 && hour < 18;

            return currentTime.DayOfWeek >= DayOfWeek.Monday &&
                   currentTime.DayOfWeek <= DayOfWeek.Friday &&
                   hour >= 8 && hour < 18;
        }

        private Shift GetCurrentShift()
        {
            var hour = DateTime.Now.Hour;
            return hour switch
            {
                >= 8 and < 16 => Shift.Morning,
                >= 16 and < 24 => Shift.Evening,
                >= 0 and < 8 => Shift.Night,
                _ => Shift.Morning
            };
        }

        private DateTime GetShiftStartTime(Shift? shift = null)
        {
            var currentShift = shift ?? GetCurrentShift();
            var today = DateTime.Today;
            
            return currentShift switch
            {
                Shift.Morning => today.AddHours(8),
                Shift.Evening => today.AddHours(16),
                Shift.Night => today.AddHours(0),
                _ => today.AddHours(8)
            };
        }

        private DateTime GetShiftEndTime(Shift? shift = null)
        {
            var currentShift = shift ?? GetCurrentShift();
            var today = DateTime.Today;
            
            return currentShift switch
            {
                Shift.Morning => today.AddHours(16),
                Shift.Evening => today.AddDays(1).AddHours(0),
                Shift.Night => today.AddHours(8),
                _ => today.AddHours(16)
            };
        }

        private bool IsTeamShiftActive(Shift shift, DateTime currentTime)
        {
            var hour = currentTime.Hour;
            return shift switch
            {
                Shift.Morning => hour >= 8 && hour < 16,
                Shift.Evening => hour >= 16 && hour < 24,
                Shift.Night => hour >= 0 && hour < 8,
                _ => true
            };
        }
    }
}
