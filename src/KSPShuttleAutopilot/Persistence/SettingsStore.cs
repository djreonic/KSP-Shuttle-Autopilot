using System;
using System.Globalization;
using System.IO;
using UnityEngine;

namespace KSPShuttleAutopilot.Persistence
{
    /// <summary>
    /// ConfigNode persistence for settings (stored under PluginData).
    /// Schema: SETTINGS { DEORBIT { ... } EXECUTION { ... } UI { ... } }
    /// </summary>
    public sealed class SettingsStore
    {
        public SettingsModels.DeorbitSettings Deorbit { get; private set; } = new SettingsModels.DeorbitSettings();
        public SettingsModels.ExecutionSettings Execution { get; private set; } = new SettingsModels.ExecutionSettings();
        public SettingsModels.UiSettings UI { get; private set; } = new SettingsModels.UiSettings();

        /// <summary>
        /// Reads settings from settings.cfg. Applies defaults for any missing nodes or keys.
        /// Safe to call even if the file does not exist.
        /// </summary>
        public void Load()
        {
            try
            {
                string path = PersistencePaths.SettingsPath;
                if (!File.Exists(path))
                    return;

                ConfigNode root = ConfigNode.Load(path);
                if (root == null)
                    return;

                ConfigNode settings = root.GetNode("SETTINGS");
                if (settings == null)
                    return;

                ConfigNode deorbit = settings.GetNode("DEORBIT");
                if (deorbit != null)
                    ReadDeorbit(deorbit);

                ConfigNode execution = settings.GetNode("EXECUTION");
                if (execution != null)
                    ReadExecution(execution);

                ConfigNode ui = settings.GetNode("UI");
                if (ui != null)
                    ReadUi(ui);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[KSPShuttleAutopilot] SettingsStore.Load failed: {ex}");
            }
        }

        /// <summary>
        /// Serializes all settings to settings.cfg in ConfigNode format.
        /// Creates the PluginData directory if missing.
        /// </summary>
        public void Save()
        {
            try
            {
                PersistencePaths.EnsurePluginDataDirectory();

                ConfigNode root = new ConfigNode();
                ConfigNode settings = root.AddNode("SETTINGS");

                ConfigNode deorbit = settings.AddNode("DEORBIT");
                deorbit.AddValue("lookAheadOrbits", Deorbit.LookAheadOrbits.ToString(CultureInfo.InvariantCulture));
                deorbit.AddValue("samplesPerOrbit", Deorbit.SamplesPerOrbit.ToString(CultureInfo.InvariantCulture));
                deorbit.AddValue("targetPeriapsisAltitude", Deorbit.TargetPeriapsisAltitude_m.ToString(CultureInfo.InvariantCulture));
                deorbit.AddValue("maxDeltaV", Deorbit.MaxDeltaV_mps.ToString(CultureInfo.InvariantCulture));

                ConfigNode execution = settings.AddNode("EXECUTION");
                execution.AddValue("leadTime", Execution.LeadTime_s.ToString(CultureInfo.InvariantCulture));
                execution.AddValue("autoCreateNode", Execution.AutoCreateNode.ToString(CultureInfo.InvariantCulture));
                execution.AddValue("autoExecute", Execution.AutoExecute.ToString(CultureInfo.InvariantCulture));

                ConfigNode ui = settings.AddNode("UI");
                ui.AddValue("showWindow", UI.ShowWindow.ToString(CultureInfo.InvariantCulture));

                root.Save(PersistencePaths.SettingsPath);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[KSPShuttleAutopilot] SettingsStore.Save failed: {ex}");
            }
        }

        private void ReadDeorbit(ConfigNode node)
        {
            string v;
            if ((v = node.GetValue("lookAheadOrbits")) != null && int.TryParse(v, out int lookAhead))
                Deorbit.LookAheadOrbits = lookAhead;
            if ((v = node.GetValue("samplesPerOrbit")) != null && int.TryParse(v, out int samples))
                Deorbit.SamplesPerOrbit = samples;
            if ((v = node.GetValue("targetPeriapsisAltitude")) != null && double.TryParse(v, NumberStyles.Float, CultureInfo.InvariantCulture, out double periapsis))
                Deorbit.TargetPeriapsisAltitude_m = periapsis;
            if ((v = node.GetValue("maxDeltaV")) != null && double.TryParse(v, NumberStyles.Float, CultureInfo.InvariantCulture, out double maxDv))
                Deorbit.MaxDeltaV_mps = maxDv;
        }

        private void ReadExecution(ConfigNode node)
        {
            string v;
            if ((v = node.GetValue("leadTime")) != null && double.TryParse(v, NumberStyles.Float, CultureInfo.InvariantCulture, out double lead))
                Execution.LeadTime_s = lead;
            if ((v = node.GetValue("autoCreateNode")) != null && bool.TryParse(v, out bool autoCreate))
                Execution.AutoCreateNode = autoCreate;
            if ((v = node.GetValue("autoExecute")) != null && bool.TryParse(v, out bool autoExec))
                Execution.AutoExecute = autoExec;
        }

        private void ReadUi(ConfigNode node)
        {
            string v;
            if ((v = node.GetValue("showWindow")) != null && bool.TryParse(v, out bool show))
                UI.ShowWindow = show;
        }
    }
}
