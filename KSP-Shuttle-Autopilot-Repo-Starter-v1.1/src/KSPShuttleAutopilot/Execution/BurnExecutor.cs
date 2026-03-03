using System;

namespace KSPShuttleAutopilot.Execution
{
    /// <summary>
    /// Time-based burn executor with SAS maneuver hold (when available) and throttle control.
    /// Governed by System Requirements (Burn Execution) and Algorithm Specification (Burn Execution Algorithm).
    /// </summary>
    public sealed class BurnExecutor
    {
        private readonly Autopilot.VesselContext _vessel;
        private readonly Persistence.SettingsStore _settings;
        private readonly Persistence.PlanStore _plans;

        public BurnExecutor(Autopilot.VesselContext vessel, Persistence.SettingsStore settings, Persistence.PlanStore plans)
        {
            _vessel = vessel;
            _settings = settings;
            _plans = plans;
        }

        public void Arm(PlanningModels.DeorbitPlan plan)
        {
            throw new NotImplementedException("BurnExecutor.Arm not implemented yet.");
        }

        public void Disarm()
        {
        }

        public void OnUpdate()
        {
        }
    }
}
