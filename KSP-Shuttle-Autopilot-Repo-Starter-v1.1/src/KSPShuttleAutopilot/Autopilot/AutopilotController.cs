namespace KSPShuttleAutopilot.Autopilot
{
    /// <summary>
    /// Central state orchestration and module coordination.
    /// Governed by Architecture Design Document and Master Reference Index v1.1.
    /// </summary>
    public sealed class AutopilotController
    {
        private VesselContext _vessel;

        private Planning.DeorbitPlanner _planner;
        private Execution.ManeuverNodeService _nodeService;
        private Execution.BurnExecutor _burnExecutor;

        private Persistence.SettingsStore _settings;
        private Persistence.PlanStore _plans;

        private UI.MainWindow _ui;

        public void Initialize()
        {
            _vessel = new VesselContext();

            _settings = new Persistence.SettingsStore();
            _plans = new Persistence.PlanStore();

            _planner = new Planning.DeorbitPlanner(_vessel, _settings, _plans);
            _nodeService = new Execution.ManeuverNodeService(_vessel, _settings, _plans);
            _burnExecutor = new Execution.BurnExecutor(_vessel, _settings, _plans);

            _ui = new UI.MainWindow(_planner, _nodeService, _burnExecutor, _settings, _plans);
            _ui.Initialize();
        }

        public void OnUpdate()
        {
            _vessel.Refresh();

            _ui?.OnUpdate();
            _burnExecutor?.OnUpdate();
        }

        public void Dispose()
        {
            _ui?.Dispose();
            _ui = null;
        }
    }
}
