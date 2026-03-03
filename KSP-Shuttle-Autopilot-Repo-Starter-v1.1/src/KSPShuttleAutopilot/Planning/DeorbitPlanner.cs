using System;

namespace KSPShuttleAutopilot.Planning
{
    /// <summary>
    /// Phase 1: Burn search, ΔV solving, vacuum impact prediction, miss distance evaluation.
    /// Governed by System Requirements (Deorbit Planner) and Algorithm Specification (Deorbit Planning Algorithm).
    /// </summary>
    public sealed class DeorbitPlanner
    {
        private readonly Autopilot.VesselContext _vessel;
        private readonly Persistence.SettingsStore _settings;
        private readonly Persistence.PlanStore _plans;

        public DeorbitPlanner(Autopilot.VesselContext vessel, Persistence.SettingsStore settings, Persistence.PlanStore plans)
        {
            _vessel = vessel;
            _settings = settings;
            _plans = plans;
        }

        public PlanningModels.DeorbitPlan ComputePlan(PlanningModels.RunwayRecord runway)
        {
            throw new NotImplementedException("DeorbitPlanner.ComputePlan not implemented yet.");
        }
    }
}
