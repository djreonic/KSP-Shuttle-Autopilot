// DeorbitPlanner.cs

using System;
using System.Collections.Generic;
using System.Linq;

namespace KSPShuttleAutopilot.Planning
{
    public class DeorbitPlanner
    {
        /// <summary>
        /// Compute a deorbit plan for the specified runway target.
        /// Phase 1: returns a placeholder plan; full algorithm is deferred to Phase 2.
        /// </summary>
        public PlanningModels.DeorbitPlan ComputePlan(PlanningModels.RunwayRecord runway)
        {
            if (runway == null)
                throw new ArgumentNullException(nameof(runway));

            // Phase 1 stub: return a plan seeded with the runway coordinates.
            return new PlanningModels.DeorbitPlan
            {
                RunwayId               = runway.Id ?? string.Empty,
                BurnUT                 = 0.0,
                DeltaV_mps             = 0.0,
                PeriapsisTarget_m      = 0.0,
                PredictedImpactLatDeg  = runway.LatitudeDeg,
                PredictedImpactLonDeg  = runway.LongitudeDeg,
                MissDistance_m         = 0.0,
            };
        }

        // Phase 1 deorbit planning algorithm

        public void PlanDeorbit(double currentAltitude, double desiredAltitude, double deltaV)
        {
            ValidateInputs(currentAltitude, desiredAltitude, deltaV);
            var searchWindow = GenerateSearchWindow(currentAltitude, desiredAltitude);
            double optimalDeltaV = SolveBisectionDeltaV(searchWindow, deltaV);
            var missDistance = CalculateMissDistance(optimalDeltaV);
            IntegratePersistence(missDistance);
        }

        private void ValidateInputs(double currentAltitude, double desiredAltitude, double deltaV)
        {
            if (currentAltitude < desiredAltitude)
            {
                throw new ArgumentException("Current altitude must be greater than desired altitude.");
            }
            if (deltaV <= 0)
            {
                throw new ArgumentException("Delta-V must be positive.");
            }
        }

        private List<double> GenerateSearchWindow(double currentAltitude, double desiredAltitude)
        {
            // Generate a search window for the bisection method
            return Enumerable.Range((int)desiredAltitude, (int)(currentAltitude - desiredAltitude)).Select(i => (double)i).ToList();
        }

        private double SolveBisectionDeltaV(List<double> searchWindow, double targetDeltaV)
        {
            // Implement bisection method to find optimal Delta-V
            double lowerBound = searchWindow.First();
            double upperBound = searchWindow.Last();
            double optimalDeltaV = 0;

            while (upperBound - lowerBound > 0.01) // Precision of 0.01
            {
                double mid = (lowerBound + upperBound) / 2;
                if (CalculateDeltaV(mid) < targetDeltaV)
                {
                    lowerBound = mid;
                }
                else
                {
                    upperBound = mid;
                }
            }
            optimalDeltaV = (lowerBound + upperBound) / 2;
            return optimalDeltaV;
        }

        private double CalculateDeltaV(double altitude)
        {
            // Simulate the Delta-V calculation based on altitude
            return Math.Sqrt(2 * 9.81 * altitude); // Simplified gravitational formula
        }

        private double CalculateMissDistance(double deltaV)
        {
            // Great-circle miss distance calculation
            double R = 6371000; // Earth radius in meters
            return (deltaV * deltaV) / (2 * 9.81); // Simplified calculation
        }

        private void IntegratePersistence(double missDistance)
        {
            // Integrate with persistence layer for saving data
            Console.WriteLine("Miss Distance: " + missDistance + " meters");
            // Save missDistance to a database or file here
        }
    }
}