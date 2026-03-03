# Documentation Index (v1.1)

This index is optimized for **GitHub Claude** and mobile workflows.

## Canonical Baseline

Start every work session with:

> Use Master Reference Index v1.1 as baseline. We are implementing [ModuleName], Phase [1 or 2], governed by System Requirements §X and Algorithm Specification §Y.

## Documents

- **Master Reference Index** (authority + module map + assembly refs)
- **Project Scope** (phase boundaries + non-goals)
- **System Requirements** (what must be true)
- **Architecture Design Document** (module boundaries + data flow)
- **Interface Control Document (ICD)** (KSP API touchpoints + contracts)
- **Detailed Algorithm Specification** (how Phase 1 planner/executor behave)

## Module → “What to cite” cheat sheet

### AutopilotController / VesselContext
- Architecture: Core Modules + Data Flow
- Master Reference Index: Module map and cross-chat protocol

### DeorbitPlanner (Phase 1)
- System Requirements: **Deorbit Planner Requirements**
- Algorithm Spec: **Deorbit Planning Algorithm**

### ManeuverNodeService (Phase 1)
- System Requirements: **Burn Execution Requirements** (node creation + integration)
- ICD: **PatchedConicSolver** interface expectations

### BurnExecutor (Phase 1)
- System Requirements: **Burn Execution Requirements**
- Algorithm Spec: **Burn Execution Algorithm**

### Persistence (SettingsStore / PlanStore)
- System Requirements: **Persistence Requirements**
- ICD: **ConfigNode API** usage

### UI (MainWindow)
- System Requirements: **User Interface Requirements**
- ICD: **User Interface Contract**

## Recommended working sequence (Phase 1)

1. Implement **SettingsStore** and **PlanStore** (ConfigNode read/write).
2. Implement **DeorbitPlanner** core loop (search → solve → propagate → miss distance).
3. Implement **ManeuverNodeService** (create/update node from DeorbitPlan).
4. Implement **BurnExecutor** (arm → orient → throttle → terminate).
5. Wire minimal UI controls (plan → create node → arm/execute) and status output.
