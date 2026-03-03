using UnityEngine;

namespace KSPShuttleAutopilot.Autopilot
{
    /// <summary>
    /// Orchestrates all autopilot subsystems. Created and owned by <see cref="ShuttleAutopilotAddon"/>.
    /// </summary>
    public sealed class AutopilotController
    {
        private readonly VesselContext _vessel;
        private readonly Persistence.SettingsStore _settings;
        private readonly Persistence.PlanStore _plans;
        private readonly Planning.DeorbitPlanner _planner;
        private readonly Execution.ManeuverNodeService _nodeService;
        private readonly Execution.BurnExecutor _burnExecutor;
        private readonly UI.MainWindow _ui;

        public AutopilotController()
        {
            _vessel    = new VesselContext();
            _settings  = new Persistence.SettingsStore();
            _plans     = new Persistence.PlanStore();
            _planner   = new Planning.DeorbitPlanner();
            _nodeService = new Execution.ManeuverNodeService(_vessel, _settings, _plans);
            _burnExecutor = new Execution.BurnExecutor(_vessel, _settings, _plans);
            _ui = new UI.MainWindow(_planner, _nodeService, _burnExecutor, _settings, _plans);
        }

        public void Initialize()
        {
            try
            {
                _settings.Load();
                _plans.Load();
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[KSPShuttleAutopilot] Initialize failed: {ex}");
            }

            _ui.Initialize();
        }

        public void OnUpdate()
        {
            _vessel.Refresh();
            _burnExecutor.OnUpdate();
            _ui.OnUpdate();
        }

        public void OnGUI()
        {
            _ui.OnGUI();
        }

        public void Dispose()
        {
            try
            {
                _settings.Save();
                _plans.Save();
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[KSPShuttleAutopilot] Dispose failed: {ex}");
            }

            _ui.Dispose();
        }
    }
}