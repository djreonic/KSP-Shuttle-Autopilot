# Source Layout Guide (for GitHub Claude)

This file maps **Phase 1 requirements** to **exact files** in `src/`.

## Entry Point

- `src/KSPShuttleAutopilot/ShuttleAutopilotAddon.cs`
  - KSP Flight-scene addon bootstrap (loads controller)

## Core Orchestration

- `src/KSPShuttleAutopilot/Autopilot/AutopilotController.cs`
  - Owns module instances, update loop sequencing
- `src/KSPShuttleAutopilot/Autopilot/VesselContext.cs`
  - Safe access to `FlightGlobals.ActiveVessel`, `Orbit`, `CelestialBody`

## Phase 1 Planner

- `src/KSPShuttleAutopilot/Planning/DeorbitPlanner.cs`
  - Implement: search window, ΔV solve (bisection), post-burn orbit, vacuum sea-level impact,
    great-circle miss distance, optimal selection
  - Output: `PlanningModels.DeorbitPlan`

## Phase 1 Execution

- `src/KSPShuttleAutopilot/Execution/ManeuverNodeService.cs`
  - Implement: create/update maneuver node at `DeorbitPlan.BurnUT` with retrograde ΔV
  - Touchpoints: `PatchedConicSolver` / node list (see ICD)

- `src/KSPShuttleAutopilot/Execution/BurnExecutor.cs`
  - Implement: arm/disarm, lead time, SAS hold (if available), throttle scheduling,
    time-based termination, user override

## Persistence (Phase 1 required)

- `src/KSPShuttleAutopilot/Persistence/SettingsStore.cs`
  - ConfigNode read/write for settings under `GameData/KSPShuttleAutopilot/PluginData/`

- `src/KSPShuttleAutopilot/Persistence/PlanStore.cs`
  - Store and reload last computed plan (`DeorbitPlan`) under `PluginData/`

## Models

- `src/KSPShuttleAutopilot/Models.cs`
  - Data contracts (runway records, plans, settings classes)
  - If you expand schemas, keep them backward-compatible (PlanStore reload)

## UI (Phase 1 minimal)

- `src/KSPShuttleAutopilot/UI/MainWindow.cs`
  - Implement: IMGUI window + tabs (Planner / Execution / Settings / Status)
  - Must expose: plan compute, node create, arm/execute, save settings, status output

## GameData Install Target

- `GameData/KSPShuttleAutopilot/Plugins/`
  - Place the built DLL here for in-game testing.
- `GameData/KSPShuttleAutopilot/PluginData/`
  - Settings + plan persistence here only.

## Claude instructions

When asking Claude to implement a feature:
1) paste the **module declaration** string from `docs/v1.1/INDEX.md`
2) point Claude to the **exact file** above
3) require explicit references to doc sections (System Requirements / Algorithm Spec / ICD)
