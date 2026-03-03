# Contributing

## Ground Rules (Project Authority)

This project is governed by the documentation set in `docs/v1.1/`.

**Baseline authority:** `KSP-Shuttle-Autopilot-Master-Reference-Index-v1.1`

All work must preserve:
- Module boundaries defined by the **Architecture Design Document**
- Interfaces and contracts defined by the **ICD**
- Behavioral requirements defined by **System Requirements** and **Algorithm Specification**

## Work Protocol

### 1) Declare scope up front (required)

Every issue/PR must state:

- **Module:** (e.g., DeorbitPlanner, BurnExecutor, ManeuverNodeService, UI, Persistence)
- **Phase:** 1 or 2
- **Governing references:** System Requirements §X, Algorithm Specification §Y (and/or ICD sections)

Suggested header:

> Use Master Reference Index v1.1 as baseline. We are implementing [ModuleName], Phase [1 or 2], governed by System Requirements §X and Algorithm Specification §Y.

### 2) Keep changes modular

Avoid cross-cutting changes unless the Architecture document explicitly requires it.

### 3) Testing expectations (Phase 1)

At minimum, document how you validated:
- Orbit state used
- Target runway used
- Planner result (burn UT, ΔV, miss distance)
- Execution behavior (node created, throttle applied, burn terminated correctly)

### 4) Style

- Prefer small, composable classes
- No hard dependency on third-party mods (optional integrations must be guarded)
- ConfigNode persistence under `PluginData/` only
