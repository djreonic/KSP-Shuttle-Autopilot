using System;
using UnityEngine;

namespace KSPShuttleAutopilot.Execution
{
    /// <summary>
    /// Maneuver node creation and management (PatchedConicSolver).
    /// Governed by System Requirements §3 (Burn Execution - node creation) and ICD §1 (External Interfaces).
    /// Phase 1: Create/update retrograde maneuver nodes with tolerance-based deduplication.
    /// </summary>
    public sealed class ManeuverNodeService
    {
        private readonly Autopilot.VesselContext _vessel;
        private readonly Persistence.SettingsStore _settings;
        private readonly Persistence.PlanStore _plans;

        // In-memory marker for last created node to avoid duplicates
        private ManeuverNode _lastCreatedNode;
        private double _lastCreatedNodeUT;
        private double _lastCreatedNodeDeltaV;

        // Tolerance parameters for node matching
        private const double BurnUTTolerance = 1.0; // seconds
        private const double DeltaVTolerance = 0.5; // m/s

        public ManeuverNodeService(Autopilot.VesselContext vessel, Persistence.SettingsStore settings, Persistence.PlanStore plans)
        {
            _vessel = vessel;
            _settings = settings;
            _plans = plans;
            _lastCreatedNode = null;
            _lastCreatedNodeUT = -1.0;
            _lastCreatedNodeDeltaV = -1.0;
        }

        /// <summary>
        /// Create or update a retrograde maneuver node at plan.BurnUT with magnitude plan.DeltaV_mps.
        /// Uses in-memory tracking + tolerance-based matching to avoid duplicates.
        /// Validates inputs and fails gracefully without exceptions.
        /// </summary>
        public void CreateOrUpdateNode(PlanningModels.DeorbitPlan plan)
        {
            // === STEP 1: INPUT VALIDATION ===
            if (!ValidateInputs(plan))
            {
                return; // Error already logged
            }

            Vessel activeVessel = _vessel.Vessel;
            Orbit activeOrbit = _vessel.Orbit;

            try
            {
                // Ensure solver is ready
                FlightPlannerGUI.Instance.UpdateFlightPlan();
            }
            catch (Exception ex)
            {
                Debug.LogError($"[ManeuverNodeService] Failed to ensure solver ready: {ex.Message}");
                return;
            }

            // === STEP 2: CHECK FOR EXISTING NODE (in-memory or by tolerance match) ===
            ManeuverNode existingNode = FindExistingNode(activeVessel, plan);

            if (existingNode != null)
            {
                // Update existing node
                UpdateExistingNode(existingNode, activeOrbit, plan);
            }
            else
            {
                // Create new node
                CreateNewNode(activeVessel, activeOrbit, plan);
            }

            // === STEP 3: REFRESH SOLVER AND RENDERING ===
            try
            {
                if (FlightPlannerGUI.Instance != null && FlightPlannerGUI.Instance.patchedConicSolver != null)
                {
                    FlightPlannerGUI.Instance.patchedConicSolver.UpdateFlightPlan();
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[ManeuverNodeService] Failed to update flight plan solver: {ex.Message}");
            }
        }

        // ===== HELPER METHODS =====

        /// <summary>
        /// Validate all inputs: vessel, solver, orbit, plan, ΔV.
        /// Returns false and logs single error if validation fails.
        /// </summary>
        private bool ValidateInputs(PlanningModels.DeorbitPlan plan)
        {
            if (plan == null)
            {
                Debug.LogError("[ManeuverNodeService] Plan is null.");
                return false;
            }

            if (_vessel.Vessel == null)
            {
                Debug.LogError("[ManeuverNodeService] No active vessel.");
                return false;
            }

            if (_vessel.Orbit == null)
            {
                Debug.LogError("[ManeuverNodeService] Vessel has no orbit.");
                return false;
            }

            if (FlightPlannerGUI.Instance == null)
            {
                Debug.LogError("[ManeuverNodeService] FlightPlannerGUI not available.");
                return false;
            }

            if (plan.DeltaV_mps <= 0)
            {
                Debug.LogError($"[ManeuverNodeService] Plan ΔV {plan.DeltaV_mps} is not positive.");
                return false;
            }

            return true;
        }

        /// <summary>
        /// Find an existing node matching the plan:
        /// 1. Check in-memory _lastCreatedNode if within tolerance
        /// 2. Search all nodes on active vessel within tolerance
        /// Returns null if no match found.
        /// </summary>
        private ManeuverNode FindExistingNode(Vessel vessel, PlanningModels.DeorbitPlan plan)
        {
            // Check in-memory marker first
            if (_lastCreatedNode != null && 
                Math.Abs(_lastCreatedNode.UT - plan.BurnUT) < BurnUTTolerance &&
                Math.Abs(_lastCreatedNodeDeltaV - plan.DeltaV_mps) < DeltaVTolerance)
            {
                return _lastCreatedNode;
            }

            // Search all nodes on vessel within tolerance
            try
            {
                foreach (ManeuverNode node in vessel.patchedConicSolver.maneuverNodes)
                {
                    double nodeDeltaVMag = node.DeltaV.magnitude;
                    if (Math.Abs(node.UT - plan.BurnUT) < BurnUTTolerance &&
                        Math.Abs(nodeDeltaVMag - plan.DeltaV_mps) < DeltaVTolerance)
                    {
                        return node;
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[ManeuverNodeService] Error searching for existing nodes: {ex.Message}");
            }

            return null;
        }

        /// <summary>
        /// Create a new retrograde maneuver node at plan.BurnUT with magnitude plan.DeltaV_mps.
        /// Computes the retrograde burn vector in the correct coordinate frame.
        /// </summary>
        private void CreateNewNode(Vessel vessel, Orbit orbit, PlanningModels.DeorbitPlan plan)
        {
            try
            {
                // Get orbit state at burn UT
                Vector3d burnPosition = orbit.getRelativePositionAtUT(plan.BurnUT);
                Vector3d burnVelocity = orbit.getOrbitalVelocityAtUT(plan.BurnUT);

                // Compute retrograde direction (opposite of velocity)
                Vector3d retrogradeDirection = -burnVelocity.normalized;

                // Compute burn vector (retrograde impulse)
                Vector3d burnVector = retrogradeDirection * plan.DeltaV_mps;

                // Create the maneuver node
                ManeuverNode newNode = vessel.patchedConicSolver.AddManeuverNode(plan.BurnUT);

                if (newNode == null)
                {
                    Debug.LogError("[ManeuverNodeService] Failed to create maneuver node.");
                    return;
                }

                // Set the burn vector (in KSP's reference frame convention)
                newNode.DeltaV = burnVector;

                // Update solver to propagate the new node
                vessel.patchedConicSolver.UpdateFlightPlan();

                // Store in-memory marker
                _lastCreatedNode = newNode;
                _lastCreatedNodeUT = plan.BurnUT;
                _lastCreatedNodeDeltaV = plan.DeltaV_mps;

                Debug.Log($"[ManeuverNodeService] Created retrograde node at UT={{plan.BurnUT}}, ΔV={{plan.DeltaV_mps}} m/s");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[ManeuverNodeService] Failed to create maneuver node: {ex.Message}");
            }
        }

        /// <summary>
        /// Update an existing maneuver node to match the new plan.
        /// Updates UT and burn vector.
        /// </summary>
        private void UpdateExistingNode(ManeuverNode node, Orbit orbit, PlanningModels.DeorbitPlan plan)
        {
            try
            {
                // Update burn UT if necessary
                if (Math.Abs(node.UT - plan.BurnUT) > BurnUTTolerance * 0.1)
                {
                    node.UT = plan.BurnUT;
                }

                // Recompute burn vector at current (or updated) UT
                Vector3d burnPosition = orbit.getRelativePositionAtUT(node.UT);
                Vector3d burnVelocity = orbit.getOrbitalVelocityAtUT(node.UT);
                Vector3d retrogradeDirection = -burnVelocity.normalized;
                Vector3d burnVector = retrogradeDirection * plan.DeltaV_mps;

                // Update the burn vector
                node.DeltaV = burnVector;

                // Update solver
                if (_vessel.Vessel != null && _vessel.Vessel.patchedConicSolver != null)
                {
                    _vessel.Vessel.patchedConicSolver.UpdateFlightPlan();
                }

                // Update in-memory marker
                _lastCreatedNode = node;
                _lastCreatedNodeUT = plan.BurnUT;
                _lastCreatedNodeDeltaV = plan.DeltaV_mps;

                Debug.Log($"[ManeuverNodeService] Updated retrograde node at UT={{plan.BurnUT}}, ΔV={{plan.DeltaV_mps}} m/s");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[ManeuverNodeService] Failed to update maneuver node: {ex.Message}");
            }
        }
    }
}