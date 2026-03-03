using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

namespace KSPShuttleAutopilot.UI
{
    /// <summary>
    /// IMGUI window with 4-tab interface: Deorbit, Execution, Settings, Status.
    /// Flight-scene safe: never throws from OnGUI.
    /// </summary>
    public sealed class MainWindow
    {
        private readonly Planning.DeorbitPlanner _planner;
        private readonly Execution.ManeuverNodeService _nodeService;
        private readonly Execution.BurnExecutor _burnExecutor;
        private readonly Persistence.SettingsStore _settings;
        private readonly Persistence.PlanStore _plans;

        // ── Window state ────────────────────────────────────────────────────────
        private Rect _windowRect = new Rect(100f, 100f, 420f, 480f);
        private int _selectedTab = 0;
        private readonly string[] _tabNames = { "Deorbit", "Execution", "Settings", "Status" };
        private const int WindowId = 0x4B535041; // "KSPA"

        // ── Deorbit tab inputs ──────────────────────────────────────────────────
        private string _runwayId     = "KSC";
        private string _runwayLatStr = "-0.04619";
        private string _runwayLonStr = "-74.73673";

        // ── Settings tab fields ─────────────────────────────────────────────────
        private string _lookAheadOrbitsStr;
        private string _samplesPerOrbitStr;
        private string _targetPeriapsisStr;
        private string _maxDeltaVStr;
        private string _leadTimeStr;

        // ── Execution tab state ─────────────────────────────────────────────────
        private bool _isArmed = false;

        // ── Status tab ──────────────────────────────────────────────────────────
        private readonly Queue<string> _statusLog = new Queue<string>();
        private const int MaxLogLines = 20;
        private Vector2 _logScrollPos = Vector2.zero;

        public MainWindow(
            Planning.DeorbitPlanner planner,
            Execution.ManeuverNodeService nodeService,
            Execution.BurnExecutor burnExecutor,
            Persistence.SettingsStore settings,
            Persistence.PlanStore plans)
        {
            _planner      = planner;
            _nodeService  = nodeService;
            _burnExecutor = burnExecutor;
            _settings     = settings;
            _plans        = plans;
        }

        public void Initialize()
        {
            SyncSettingsToFields();
        }

        public void OnUpdate()
        {
        }

        /// <summary>
        /// Renders the IMGUI window. Must be called from MonoBehaviour.OnGUI().
        /// Never throws; all errors are caught and written to the status log.
        /// </summary>
        public void OnGUI()
        {
            try
            {
                if (!_settings.UI.ShowWindow)
                    return;

                _windowRect = GUILayout.Window(WindowId, _windowRect, DrawWindow, "KSP Shuttle Autopilot");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[KSPShuttleAutopilot] MainWindow.OnGUI failed: {ex}");
            }
        }

        public void Dispose()
        {
        }

        // ── Window callback ─────────────────────────────────────────────────────

        private void DrawWindow(int id)
        {
            try
            {
                // Close (X) button in top-right corner
                GUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("X", GUILayout.Width(22)))
                    _settings.UI.ShowWindow = false;
                GUILayout.EndHorizontal();

                // Tab bar
                _selectedTab = GUILayout.Toolbar(_selectedTab, _tabNames);

                // Tab contents
                switch (_selectedTab)
                {
                    case 0: DrawDeorbitTab();    break;
                    case 1: DrawExecutionTab();  break;
                    case 2: DrawSettingsTab();   break;
                    case 3: DrawStatusTab();     break;
                }

                GUI.DragWindow();
            }
            catch (Exception ex)
            {
                Log($"Window draw error: {ex.Message}");
            }
        }

        // ── Tab 1: Deorbit ──────────────────────────────────────────────────────

        private void DrawDeorbitTab()
        {
            GUILayout.Label("Runway Target");

            GUILayout.BeginHorizontal();
            GUILayout.Label("ID:",        GUILayout.Width(80));
            _runwayId     = GUILayout.TextField(_runwayId,     GUILayout.Width(150));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("Latitude:",  GUILayout.Width(80));
            _runwayLatStr = GUILayout.TextField(_runwayLatStr, GUILayout.Width(150));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("Longitude:", GUILayout.Width(80));
            _runwayLonStr = GUILayout.TextField(_runwayLonStr, GUILayout.Width(150));
            GUILayout.EndHorizontal();

            if (GUILayout.Button("Compute Plan"))
            {
                try
                {
                    double lat, lon;
                    if (!double.TryParse(_runwayLatStr, NumberStyles.Float, CultureInfo.InvariantCulture, out lat))
                    {
                        Log("Error: invalid latitude value");
                        return;
                    }
                    if (!double.TryParse(_runwayLonStr, NumberStyles.Float, CultureInfo.InvariantCulture, out lon))
                    {
                        Log("Error: invalid longitude value");
                        return;
                    }

                    var runway = new PlanningModels.RunwayRecord
                    {
                        Id           = _runwayId,
                        LatitudeDeg  = lat,
                        LongitudeDeg = lon,
                    };

                    var plan = _planner.ComputePlan(runway);
                    _plans.SetLastPlan(plan);
                    Log($"Plan computed: BurnUT={plan.BurnUT:F1} s  ΔV={plan.DeltaV_mps:F1} m/s");
                }
                catch (Exception ex)
                {
                    Log($"Error computing plan: {ex.Message}");
                }
            }

            var lastPlan = _plans.LastPlan;
            if (lastPlan != null)
            {
                GUILayout.Space(6);
                GUILayout.Label("Last Plan:");
                GUILayout.Label($"  Burn UT:       {lastPlan.BurnUT:F1} s");
                GUILayout.Label($"  ΔV:            {lastPlan.DeltaV_mps:F1} m/s");
                GUILayout.Label($"  Miss Distance: {lastPlan.MissDistance_m:F0} m");
                GUILayout.Label($"  Impact Lat:    {lastPlan.PredictedImpactLatDeg:F4}°");
                GUILayout.Label($"  Impact Lon:    {lastPlan.PredictedImpactLonDeg:F4}°");
            }
        }

        // ── Tab 2: Execution ────────────────────────────────────────────────────

        private void DrawExecutionTab()
        {
            if (GUILayout.Button("Create/Update Node"))
            {
                try
                {
                    var plan = _plans.LastPlan;
                    if (plan == null)
                    {
                        Log("Error: no plan available – compute a plan first");
                    }
                    else
                    {
                        _nodeService.CreateOrUpdateNode(plan);
                        Log("Maneuver node created/updated");
                    }
                }
                catch (Exception ex)
                {
                    Log($"Error creating node: {ex.Message}");
                }
            }

            GUILayout.Space(4);

            GUILayout.BeginHorizontal();

            if (GUILayout.Button("Arm"))
            {
                try
                {
                    var plan = _plans.LastPlan;
                    if (plan == null)
                    {
                        Log("Error: no plan available – compute a plan first");
                    }
                    else
                    {
                        _burnExecutor.Arm(plan);
                        _isArmed = true;
                        Log("Burn executor armed");
                    }
                }
                catch (Exception ex)
                {
                    Log($"Error arming: {ex.Message}");
                }
            }

            if (GUILayout.Button("Disarm"))
            {
                try
                {
                    _burnExecutor.Disarm();
                    _isArmed = false;
                    Log("Burn executor disarmed");
                }
                catch (Exception ex)
                {
                    Log($"Error disarming: {ex.Message}");
                }
            }

            GUILayout.EndHorizontal();

            GUILayout.Space(4);
            GUILayout.Label(_isArmed ? "Status: ARMED" : "Status: DISARMED");
        }

        // ── Tab 3: Settings ─────────────────────────────────────────────────────

        private void DrawSettingsTab()
        {
            GUILayout.Label("Deorbit Settings");
            _lookAheadOrbitsStr = LabeledTextField("Look Ahead Orbits:",    _lookAheadOrbitsStr);
            _samplesPerOrbitStr = LabeledTextField("Samples/Orbit:",        _samplesPerOrbitStr);
            _targetPeriapsisStr = LabeledTextField("Target Periapsis (m):", _targetPeriapsisStr);
            _maxDeltaVStr       = LabeledTextField("Max ΔV (m/s):",         _maxDeltaVStr);

            GUILayout.Space(4);
            GUILayout.Label("Execution Settings");
            _leadTimeStr = LabeledTextField("Lead Time (s):", _leadTimeStr);

            GUILayout.BeginHorizontal();
            GUILayout.Label("Auto Create Node:", GUILayout.Width(140));
            _settings.Execution.AutoCreateNode = GUILayout.Toggle(_settings.Execution.AutoCreateNode, "");
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("Auto Execute:", GUILayout.Width(140));
            _settings.Execution.AutoExecute = GUILayout.Toggle(_settings.Execution.AutoExecute, "");
            GUILayout.EndHorizontal();

            GUILayout.Space(4);
            GUILayout.Label("UI Settings");

            GUILayout.BeginHorizontal();
            GUILayout.Label("Show Window:", GUILayout.Width(140));
            _settings.UI.ShowWindow = GUILayout.Toggle(_settings.UI.ShowWindow, "");
            GUILayout.EndHorizontal();

            GUILayout.Space(6);
            GUILayout.BeginHorizontal();

            if (GUILayout.Button("Save Settings"))
            {
                try
                {
                    ApplySettingsFields();
                    _settings.Save();
                    Log("Settings saved");
                }
                catch (Exception ex)
                {
                    Log($"Error saving settings: {ex.Message}");
                }
            }

            if (GUILayout.Button("Reload Settings"))
            {
                try
                {
                    _settings.Load();
                    SyncSettingsToFields();
                    Log("Settings reloaded");
                }
                catch (Exception ex)
                {
                    Log($"Error reloading settings: {ex.Message}");
                }
            }

            GUILayout.EndHorizontal();
        }

        // ── Tab 4: Status ───────────────────────────────────────────────────────

        private void DrawStatusTab()
        {
            GUILayout.Label("Status Log");

            _logScrollPos = GUILayout.BeginScrollView(_logScrollPos, GUILayout.Height(320));
            foreach (string line in _statusLog)
                GUILayout.Label(line);
            GUILayout.EndScrollView();

            if (GUILayout.Button("Clear Log"))
                _statusLog.Clear();
        }

        // ── Helpers ─────────────────────────────────────────────────────────────

        private string LabeledTextField(string label, string value)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label(label, GUILayout.Width(160));
            string result = GUILayout.TextField(value ?? string.Empty, GUILayout.Width(110));
            GUILayout.EndHorizontal();
            return result;
        }

        private void Log(string message)
        {
            double ut = 0.0;
            try { ut = Planetarium.GetUniversalTime(); } catch { /* outside flight scene */ }

            _statusLog.Enqueue($"[{ut:F0}] {message}");
            while (_statusLog.Count > MaxLogLines)
                _statusLog.Dequeue();
        }

        private void SyncSettingsToFields()
        {
            _lookAheadOrbitsStr = _settings.Deorbit.LookAheadOrbits.ToString(CultureInfo.InvariantCulture);
            _samplesPerOrbitStr = _settings.Deorbit.SamplesPerOrbit.ToString(CultureInfo.InvariantCulture);
            _targetPeriapsisStr = _settings.Deorbit.TargetPeriapsisAltitude_m.ToString(CultureInfo.InvariantCulture);
            _maxDeltaVStr       = _settings.Deorbit.MaxDeltaV_mps.ToString(CultureInfo.InvariantCulture);
            _leadTimeStr        = _settings.Execution.LeadTime_s.ToString(CultureInfo.InvariantCulture);
        }

        private void ApplySettingsFields()
        {
            int    intValue;
            double doubleValue;

            if (int.TryParse(_lookAheadOrbitsStr, out intValue))
                _settings.Deorbit.LookAheadOrbits = intValue;
            if (int.TryParse(_samplesPerOrbitStr, out intValue))
                _settings.Deorbit.SamplesPerOrbit = intValue;
            if (double.TryParse(_targetPeriapsisStr, NumberStyles.Float, CultureInfo.InvariantCulture, out doubleValue))
                _settings.Deorbit.TargetPeriapsisAltitude_m = doubleValue;
            if (double.TryParse(_maxDeltaVStr, NumberStyles.Float, CultureInfo.InvariantCulture, out doubleValue))
                _settings.Deorbit.MaxDeltaV_mps = doubleValue;
            if (double.TryParse(_leadTimeStr, NumberStyles.Float, CultureInfo.InvariantCulture, out doubleValue))
                _settings.Execution.LeadTime_s = doubleValue;
        }
    }
}
