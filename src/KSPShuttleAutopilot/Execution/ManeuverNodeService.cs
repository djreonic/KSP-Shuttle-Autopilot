using System;

namespace KSPShuttleAutopilot.Execution
{
    /// <summary>
    /// Maneuver node creation and management (PatchedConicSolver).
    /// </summary>
    public sealed class ManeuverNodeService
    {
        private readonly Autopilot.VesselContext _vessel;
        private readonly Persistence.SettingsStore _settings;
        private readonly Persistence.PlanStore _plans;

        public ManeuverNodeService(Autopilot.VesselContext vessel, Persistence.SettingsStore settings, Persistence.PlanStore plans)
        {
            _vessel = vessel;
            _settings = settings;
            _plans = plans;
        }

        public void CreateOrUpdateNode(PlanningModels.DeorbitPlan plan)
        {
            throw new NotImplementedException("ManeuverNodeService.CreateOrUpdateNode not implemented yet.");
        }
    }
}
