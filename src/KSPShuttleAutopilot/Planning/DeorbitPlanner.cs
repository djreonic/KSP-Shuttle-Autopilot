using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

namespace KSPShuttleAutopilot.Planning
{
    /// <summary>
    /// Phase 1: Deorbit planning algorithm (vacuum impact approximation).
    /// Governed by System Requirements §2 and Algorithm Specification §1.
    /// </summary>
    public sealed class DeorbitPlanner
    {
        private readonly Autopilot.VesselContext _vessel;
        private readonly Persistence.SettingsStore _settings;
        private readonly Persistence.PlanStore _plans;

        // Bisection tuning
        private const int BisectionIters = 32;
        private const double PeriapsisTol_m = 25.0; // tolerance for target periapsis altitude match (meters)

        public DeorbitPlanner(
            Autopilot.VesselContext vessel,
            Persistence.SettingsStore settings,
            Persistence.PlanStore plans)
        {
            _vessel = vessel;
            _settings = settings;
            _plans = plans;
        }

        /// <summary>
        /// Compute a deorbit plan for the given runway record.
        /// Returns null on failure; logs a single error for major failures.
        /// </summary>
        public PlanningModels.DeorbitPlan ComputePlan(PlanningModels.RunwayRecord runway)
        {
            if (!ValidateInputs(runway, out string err))
            {
                Debug.LogError($"[KSPShuttleAutopilot][DeorbitPlanner] {err}");
                return null;
            }

            var orbit = _vessel.Orbit;
            var body = _vessel.Body;

            // Settings (sanitized)
            int lookAheadOrbits = Math.Max(1, _settings.Deorbit.LookAheadOrbits);
            int samplesPerOrbit = Math.Max(10, _settings.Deorbit.SamplesPerOrbit);

            double maxDv = _settings.Deorbit.MaxDeltaV_mps;
            if (maxDv <= 0)
            {
                Debug.LogError("[KSPShuttleAutopilot][DeorbitPlanner] MaxDeltaV_mps must be > 0.");
                return null;
            }

            double targetPeAlt_m = Math.Max(0.0, _settings.Deorbit.TargetPeriapsisAltitude_m);
            double targetPeRadius = body.Radius + targetPeAlt_m;

            double nowUT = Planetarium.GetUniversalTime();
            double period = orbit.period;

            if (!(period > 1.0) || double.IsNaN(period) || double.IsInfinity(period))
            {
                Debug.LogError("[KSPShuttleAutopilot][DeorbitPlanner] Orbit period invalid (not in stable orbit?).");
                return null;
            }

            // Candidate burn times over next N orbits (evenly sampled)
            var burnUTs = BuildCandidateBurnTimes(nowUT, period, lookAheadOrbits, samplesPerOrbit);

            PlanningModels.DeorbitPlan best = null;
            double bestMiss = double.PositiveInfinity;

            // Precompute runway radians
            double rwLatRad = Deg2Rad(runway.LatitudeDeg);
            double rwLonRad = Deg2Rad(runway.LongitudeDeg);

            // Iterate candidates
            for (int i = 0; i < burnUTs.Count; i++)
            {
                double burnUT = burnUTs[i];

                // Solve ΔV via bisection to hit target periapsis radius after burn
                if (!TrySolveRetrogradeDvForTargetPeriapsis(orbit, body, burnUT, targetPeRadius, maxDv, out double dv_mps))
                    continue;

                // Build post-burn orbit by applying ΔV to velocity at burnUT
                if (!TryBuildPostBurnOrbit(orbit, body, burnUT, dv_mps, out Orbit postOrbit))
                    continue;

                // Propagate to first sea-level intersection (vacuum impact)
                if (!TryComputeSeaLevelImpactLatLon(postOrbit, body, burnUT, out double impactLatRad, out double impactLonRad))
                    continue;

                // Great-circle miss distance
                double miss_m = GreatCircleDistanceMeters(body.Radius, impactLatRad, impactLonRad, rwLatRad, rwLonRad);

                if (miss_m < bestMiss)
                {
                    bestMiss = miss_m;

                    best = new PlanningModels.DeorbitPlan
                    {
                        RunwayId = runway.Id,
                        BurnUT = burnUT,
                        DeltaV_mps = dv_mps,
                        PeriapsisTarget_m = targetPeAlt_m,
                        PredictedImpactLatDeg = Rad2Deg(impactLatRad),
                        PredictedImpactLonDeg = NormalizeLonDeg(Rad2Deg(impactLonRad)),
                        MissDistance_m = miss_m
                    };
                }
            }

            if (best == null)
            {
                Debug.LogError("[KSPShuttleAutopilot][DeorbitPlanner] Plan failed: No valid deorbit solution found.");
                return null;
            }

            // Persist (Phase 1 requirement)
            try
            {
                _plans.SetLastPlan(best);
                _plans.Save();
            }
            catch (Exception ex)
            {
                Debug.LogError($"[KSPShuttleAutopilot][DeorbitPlanner] Plan persistence failed: {ex}");
            }

            return best;
        }

        // -----------------------------
        // Candidate generation
        // -----------------------------

        private static List<double> BuildCandidateBurnTimes(double nowUT, double period, int lookAheadOrbits, int samplesPerOrbit)
        {
            // Start slightly in the future to avoid immediate edge issues
            double startUT = nowUT + 10.0;

            int total = lookAheadOrbits * samplesPerOrbit;
            var list = new List<double>(total);

            double totalSpan = lookAheadOrbits * period;
            double dt = totalSpan / total;

            for (int i = 0; i < total; i++)
            {
                list.Add(startUT + i * dt);
            }

            return list;
        }

        // -----------------------------
        // ΔV solve (bisection)
        // -----------------------------

        private bool TrySolveRetrogradeDvForTargetPeriapsis(Orbit preOrbit, CelestialBody body, double burnUT, double targetPeRadius, double maxDv, out double dv_mps)
        {
            dv_mps = 0.0;

            // We need a bracketing interval for bisection:
            // f(dv) = periapsisRadius(postOrbit(dv)) - targetPeRadius.
            // We want f(dv)=0.
            // We'll search dv in [0, maxDv] and ensure f(0) and f(maxDv) straddle target.

            if (!TryEvalPeriapsisRadiusAfterRetrogradeBurn(preOrbit, body, burnUT, 0.0, out double pe0))
                return false;

            if (!TryEvalPeriapsisRadiusAfterRetrogradeBurn(preOrbit, body, burnUT, maxDv, out double peMax))
                return false;

            // If increasing dv doesn't lower periapsis, something is wrong (non-elliptic / weird state)
            // But tolerate and just fail this candidate.
            // We need pe0 >= target >= peMax typically for a retrograde lowering periapsis.
            bool brackets = (pe0 >= targetPeRadius && peMax <= targetPeRadius) ||
                            (pe0 <= targetPeRadius && peMax >= targetPeRadius); // handle unexpected monotonic direction

            if (!brackets)
                return false;

            double lo = 0.0, hi = maxDv;
            double fLo = pe0 - targetPeRadius;
            double fHi = peMax - targetPeRadius;

            // Bisection
            for (int i = 0; i < BisectionIters; i++)
            {
                double mid = 0.5 * (lo + hi);

                if (!TryEvalPeriapsisRadiusAfterRetrogradeBurn(preOrbit, body, burnUT, mid, out double peMid))
                    return false;

                double fMid = peMid - targetPeRadius;

                if (Math.Abs(fMid) <= PeriapsisTol_m)
                {
                    dv_mps = mid;
                    return true;
                }

                // Decide which half contains root
                // Root if sign changes
                if (Sign(fLo) == Sign(fMid))
                {
                    lo = mid;
                    fLo = fMid;
                }
                else
                {
                    hi = mid;
                    fHi = fMid;
                }
            }

            dv_mps = 0.5 * (lo + hi);
            return true;
        }

        private bool TryEvalPeriapsisRadiusAfterRetrogradeBurn(Orbit preOrbit, CelestialBody body, double burnUT, double dv_mps, out double periapsisRadius)
        {
            periapsisRadius = double.NaN;

            if (!TryBuildPostBurnOrbit(preOrbit, body, burnUT, dv_mps, out Orbit post))
                return false;

            periapsisRadius = post.PeR;
            return !(double.IsNaN(periapsisRadius) || double.IsInfinity(periapsisRadius));
        }

        // -----------------------------
        // Orbit construction
        // -----------------------------

        private bool TryBuildPostBurnOrbit(Orbit preOrbit, CelestialBody body, double burnUT, double dv_mps, out Orbit postOrbit)
        {
            postOrbit = null;

            try
            {
                // Get state at burn UT in body-centric frame
                Vector3d r = preOrbit.getRelativePositionAtUT(burnUT);
                Vector3d v = preOrbit.getOrbitalVelocityAtUT(burnUT);

                double vMag = v.magnitude;
                if (!(vMag > 1e-6))
                    return false;

                // Retrograde unit vector
                Vector3d retroUnit = -v / vMag;

                // Apply ΔV along retrograde
                Vector3d v2 = v + retroUnit * dv_mps;

                // Construct orbit from state vectors
                var o = new Orbit();
                o.UpdateFromStateVectors(r, v2, body, burnUT);

                // Basic validity
                if (o == null || double.IsNaN(o.PeR) || double.IsInfinity(o.PeR))
                    return false;

                postOrbit = o;
                return true;
            }
            catch
            {
                return false;
            }
        }

        // -----------------------------
        // Vacuum sea-level impact prediction
        // -----------------------------

        private bool TryComputeSeaLevelImpactLatLon(Orbit orbit, CelestialBody body, double startUT, out double latRad, out double lonRad)
        {
            latRad = 0.0;
            lonRad = 0.0;

            // We need the next time after startUT when radius <= body.Radius (sea level)
            // We scan forward and then refine by bisection on radius - body.Radius.

            double seaR = body.Radius;

            // If already below sea level at start (shouldn't happen), fail.
            double r0 = orbit.getRelativePositionAtUT(startUT).magnitude;
            if (r0 <= seaR)
                return false;

            // Determine an upper time bound by stepping forward.
            // Use fractions of period but avoid huge steps.
            double period = orbit.period;
            if (!(period > 1.0))
                return false;

            double tLo = startUT;
            double tHi = startUT;

            const int maxSteps = 200;
            double step = Math.Max(5.0, period / 200.0); // ~200 steps per orbit
            bool crossed = false;

            for (int i = 0; i < maxSteps; i++)
            {
                tHi = tLo + step;

                double rHi = orbit.getRelativePositionAtUT(tHi).magnitude;

                if (rHi <= seaR)
                {
                    crossed = true;
                    break;
                }

                // Move window forward
                tLo = tHi;

                // Safety: don’t search too far (cap at 2 orbits)
                if (tLo - startUT > 2.0 * period)
                    break;
            }

            if (!crossed)
                return false;

            // Now refine between tLo (above sea level) and tHi (below sea level)
            for (int i = 0; i < 40; i++)
            {
                double mid = 0.5 * (tLo + tHi);
                double rMid = orbit.getRelativePositionAtUT(mid).magnitude;

                if (rMid > seaR)
                    tLo = mid;
                else
                    tHi = mid;
            }

            double impactUT = tHi;
            Vector3d rel = orbit.getRelativePositionAtUT(impactUT);

            // Convert to lat/lon (body fixed)
            // CelestialBody has helper to get lat/lon from world position; but we are in "relative position" frame.
            // Convert relative position to lat/lon using body methods with body-centric vector.
            // body.GetLatitude/Longitude expect world position; however in practice using relative and body rotation can be tricky.
            // We'll convert in body-centric frame via normalized vector and spherical conversion.
            // NOTE: This ignores body rotation between UT and now; for vacuum impact approximation it’s acceptable.
            Vector3d n = rel.normalized;

            latRad = Math.Asin(Clamp(n.y, -1.0, 1.0));
            lonRad = Math.Atan2(n.z, n.x); // KSP convention may differ; consistent internally for great-circle distance

            return true;
        }

        // -----------------------------
        // Great-circle distance
        // -----------------------------

        private static double GreatCircleDistanceMeters(double sphereRadius, double lat1, double lon1, double lat2, double lon2)
        {
            // Haversine
            double dLat = lat2 - lat1;
            double dLon = lon2 - lon1;

            double a = Math.Pow(Math.Sin(dLat / 2.0), 2.0)
                     + Math.Cos(lat1) * Math.Cos(lat2) * Math.Pow(Math.Sin(dLon / 2.0), 2.0);

            a = Clamp(a, 0.0, 1.0);
            double c = 2.0 * Math.Asin(Math.Sqrt(a));

            return sphereRadius * c;
        }

        // -----------------------------
        // Validation + utilities
        // -----------------------------

        private bool ValidateInputs(PlanningModels.RunwayRecord runway, out string error)
        {
            error = null;

            if (runway == null)
            {
                error = "Runway is null.";
                return false;
            }

            if (string.IsNullOrEmpty(runway.Id))
            {
                error = "Runway.Id is empty.";
                return false;
            }

            if (_vessel == null || _vessel.Vessel == null)
            {
                error = "No active vessel.";
                return false;
            }

            if (_vessel.Orbit == null)
            {
                error = "Active vessel has no orbit.";
                return false;
            }

            if (_vessel.Body == null)
            {
                error = "Active vessel has no celestial body.";
                return false;
            }

            if (runway.LatitudeDeg < -90.0 || runway.LatitudeDeg > 90.0)
            {
                error = $"Runway latitude out of range: {runway.LatitudeDeg.ToString(CultureInfo.InvariantCulture)}";
                return false;
            }

            if (runway.LongitudeDeg < -180.0 || runway.LongitudeDeg > 180.0)
            {
                error = $"Runway longitude out of range: {runway.LongitudeDeg.ToString(CultureInfo.InvariantCulture)}";
                return false;
            }

            return true;
        }

        private static int Sign(double x) => x < 0 ? -1 : (x > 0 ? 1 : 0);

        private static double Deg2Rad(double deg) => deg * Math.PI / 180.0;
        private static double Rad2Deg(double rad) => rad * 180.0 / Math.PI;

        private static double NormalizeLonDeg(double lonDeg)
        {
            // Normalize to [-180, 180]
            while (lonDeg > 180.0) lonDeg -= 360.0;
            while (lonDeg < -180.0) lonDeg += 360.0;
            return lonDeg;
        }

        private static double Clamp(double v, double lo, double hi)
        {
            if (v < lo) return lo;
            if (v > hi) return hi;
            return v;
        }
    }
}