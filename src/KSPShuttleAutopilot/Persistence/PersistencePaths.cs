using System;
using System.IO;
using UnityEngine;

namespace KSPShuttleAutopilot.Persistence
{
    /// <summary>
    /// Centralized path management for plugin persistence files.
    /// Target directory: GameData/KSPShuttleAutopilot/PluginData/
    /// </summary>
    internal static class PersistencePaths
    {
        private const string PluginDataRelPath = "GameData/KSPShuttleAutopilot/PluginData";

        /// <summary>Returns the full path to the PluginData directory.</summary>
        public static string GetPluginDataPath()
        {
            return Path.Combine(KSPUtil.ApplicationRootPath, PluginDataRelPath);
        }

        /// <summary>Creates the PluginData directory if it does not already exist.</summary>
        public static void EnsurePluginDataDirectory()
        {
            try
            {
                string dir = GetPluginDataPath();
                if (!Directory.Exists(dir))
                    Directory.CreateDirectory(dir);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[KSPShuttleAutopilot] Failed to create PluginData directory: {ex}");
            }
        }

        /// <summary>Full path to the settings configuration file.</summary>
        public static string SettingsPath => Path.Combine(GetPluginDataPath(), "settings.cfg");

        /// <summary>Full path to the last plan configuration file.</summary>
        public static string PlanPath => Path.Combine(GetPluginDataPath(), "last_plan.cfg");
    }
}
