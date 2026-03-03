namespace KSPShuttleAutopilot.Persistence
{
    /// <summary>
    /// Stores last computed plan (Phase 1 requirement: reloadable).
    /// </summary>
    public sealed class PlanStore
    {
        public PlanningModels.DeorbitPlan LastPlan { get; private set; }

        public void SetLastPlan(PlanningModels.DeorbitPlan plan) => LastPlan = plan;

        public void Load()
        {
        }

        public void Save()
        {
        }
    }
}
