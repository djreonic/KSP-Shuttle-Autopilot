using System;

namespace KSPShuttleAutopilot.Persistence
{
    /// <summary>
    /// ConfigNode persistence for settings (stored under PluginData).
    /// </summary>
    public sealed class SettingsStore
    {
        public SettingsModels.DeorbitSettings Deorbit { get; private set; } = new SettingsModels.DeorbitSettings();
        public SettingsModels.ExecutionSettings Execution { get; private set; } = new SettingsModels.ExecutionSettings();
        public SettingsModels.UiSettings UI { get; private set; } = new SettingsModels.UiSettings();

        public void Load() => throw new NotImplementedException("SettingsStore.Load not implemented yet.");
        public void Save() => throw new NotImplementedException("SettingsStore.Save not implemented yet.");
    }
}
