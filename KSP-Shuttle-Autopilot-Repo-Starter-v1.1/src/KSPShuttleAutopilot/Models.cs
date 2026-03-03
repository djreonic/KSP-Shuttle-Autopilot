namespace KSPShuttleAutopilot.PlanningModels
{
    public sealed class RunwayRecord
    {
        public string Id;
        public string DisplayName;

        public double LatitudeDeg;
        public double LongitudeDeg;

        public double HeadingDeg; // runway heading (true)
        public double ElevationM;
    }

    public sealed class DeorbitPlan
    {
        public string RunwayId;

        public double BurnUT;
        public double DeltaV_mps;

        public double PeriapsisTarget_m;

        public double PredictedImpactLatDeg;
        public double PredictedImpactLonDeg;

        public double MissDistance_m;
    }
}

namespace KSPShuttleAutopilot.SettingsModels
{
    public sealed class DeorbitSettings
    {
        public int LookAheadOrbits = 3;
        public int SamplesPerOrbit = 180;

        public double TargetPeriapsisAltitude_m = 30000; // placeholder
        public double MaxDeltaV_mps = 350; // placeholder
    }

    public sealed class ExecutionSettings
    {
        public double LeadTime_s = 30.0;
        public bool AutoCreateNode = true;
        public bool AutoExecute = false;
    }

    public sealed class UiSettings
    {
        public bool ShowWindow = true;
    }
}
