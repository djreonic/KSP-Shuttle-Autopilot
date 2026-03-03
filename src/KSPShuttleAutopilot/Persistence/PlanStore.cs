using System;
using System.Globalization;
using System.IO;
using UnityEngine;

namespace KSPShuttleAutopilot.Persistence
{
    /// <summary>
    /// Stores last computed deorbit plan (Phase 1 requirement: reloadable).
    /// Schema: LASTPLAN { runwayId burnUT deltaV periapsisTarget predictedImpactLat predictedImpactLon missDistance }
    /// </summary>
    public sealed class PlanStore
    {
        public PlanningModels.DeorbitPlan LastPlan { get; private set; }

        public void SetLastPlan(PlanningModels.DeorbitPlan plan) => LastPlan = plan;

        /// <summary>
        /// Deserializes the last plan from last_plan.cfg if present.
        /// Silently returns if the file does not exist.
        /// </summary>
        public void Load()
        {
            try
            {
                string path = PersistencePaths.PlanPath;
                if (!File.Exists(path))
                    return;

                ConfigNode root = ConfigNode.Load(path);
                if (root == null)
                    return;

                ConfigNode node = root.GetNode("LASTPLAN");
                if (node == null)
                    return;

                var plan = new PlanningModels.DeorbitPlan();
                string v;

                plan.RunwayId = node.GetValue("runwayId") ?? string.Empty;

                if ((v = node.GetValue("burnUT")) != null && double.TryParse(v, NumberStyles.Float, CultureInfo.InvariantCulture, out double burnUT))
                    plan.BurnUT = burnUT;
                if ((v = node.GetValue("deltaV")) != null && double.TryParse(v, NumberStyles.Float, CultureInfo.InvariantCulture, out double dv))
                    plan.DeltaV_mps = dv;
                if ((v = node.GetValue("periapsisTarget")) != null && double.TryParse(v, NumberStyles.Float, CultureInfo.InvariantCulture, out double peri))
                    plan.PeriapsisTarget_m = peri;
                if ((v = node.GetValue("predictedImpactLat")) != null && double.TryParse(v, NumberStyles.Float, CultureInfo.InvariantCulture, out double lat))
                    plan.PredictedImpactLatDeg = lat;
                if ((v = node.GetValue("predictedImpactLon")) != null && double.TryParse(v, NumberStyles.Float, CultureInfo.InvariantCulture, out double lon))
                    plan.PredictedImpactLonDeg = lon;
                if ((v = node.GetValue("missDistance")) != null && double.TryParse(v, NumberStyles.Float, CultureInfo.InvariantCulture, out double miss))
                    plan.MissDistance_m = miss;

                LastPlan = plan;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[KSPShuttleAutopilot] PlanStore.Load failed: {ex}");
            }
        }

        /// <summary>
        /// Serializes the current plan (if set) to last_plan.cfg.
        /// Creates the PluginData directory if missing.
        /// </summary>
        public void Save()
        {
            try
            {
                if (LastPlan == null)
                    return;

                PersistencePaths.EnsurePluginDataDirectory();

                ConfigNode root = new ConfigNode();
                ConfigNode node = root.AddNode("LASTPLAN");
                node.AddValue("runwayId", LastPlan.RunwayId ?? string.Empty);
                node.AddValue("burnUT", LastPlan.BurnUT.ToString(CultureInfo.InvariantCulture));
                node.AddValue("deltaV", LastPlan.DeltaV_mps.ToString(CultureInfo.InvariantCulture));
                node.AddValue("periapsisTarget", LastPlan.PeriapsisTarget_m.ToString(CultureInfo.InvariantCulture));
                node.AddValue("predictedImpactLat", LastPlan.PredictedImpactLatDeg.ToString(CultureInfo.InvariantCulture));
                node.AddValue("predictedImpactLon", LastPlan.PredictedImpactLonDeg.ToString(CultureInfo.InvariantCulture));
                node.AddValue("missDistance", LastPlan.MissDistance_m.ToString(CultureInfo.InvariantCulture));

                root.Save(PersistencePaths.PlanPath);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[KSPShuttleAutopilot] PlanStore.Save failed: {ex}");
            }
        }
    }
}
