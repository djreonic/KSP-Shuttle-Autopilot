namespace KSPShuttleAutopilot.UI
{
    /// <summary>
    /// Minimal MechJeb-style IMGUI window scaffold (Phase 1).
    /// </summary>
    public sealed class MainWindow
    {
        private readonly Planning.DeorbitPlanner _planner;
        private readonly Execution.ManeuverNodeService _nodeService;
        private readonly Execution.BurnExecutor _burnExecutor;
        private readonly Persistence.SettingsStore _settings;
        private readonly Persistence.PlanStore _plans;

        public MainWindow(
            Planning.DeorbitPlanner planner,
            Execution.ManeuverNodeService nodeService,
            Execution.BurnExecutor burnExecutor,
            Persistence.SettingsStore settings,
            Persistence.PlanStore plans)
        {
            _planner = planner;
            _nodeService = nodeService;
            _burnExecutor = burnExecutor;
            _settings = settings;
            _plans = plans;
        }

        public void Initialize()
        {
        }

        public void OnUpdate()
        {
        }

        public void Dispose()
        {
        }
    }
}
