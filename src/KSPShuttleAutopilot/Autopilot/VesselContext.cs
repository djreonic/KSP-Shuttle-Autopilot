namespace KSPShuttleAutopilot.Autopilot
{
    /// <summary>
    /// Safe wrapper around FlightGlobals.ActiveVessel and orbit state.
    /// </summary>
    public sealed class VesselContext
    {
        public Vessel Vessel { get; private set; }
        public Orbit Orbit => Vessel != null ? Vessel.orbit : null;
        public CelestialBody Body => Vessel != null ? Vessel.mainBody : null;

        public void Refresh() => Vessel = FlightGlobals.ActiveVessel;
    }
}
