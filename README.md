# KSP Shuttle Autopilot

STS-specific Deorbit, Entry, and Landing Autopilot for Kerbal Space Program (KSP 1.12.x)

---

## Project Status

**Version 1.0 Alpha — Deorbit / Entry / Landing (DEL)**

This release focuses exclusively on:

- Deorbit planning
- Lifting reentry guidance
- TAEM / HAC geometry
- Final approach & autoland
- System scale support (1×–10×)
- FAR primary backend
- Stock aero support (tuned at end of Alpha)

No ascent, abort, rendezvous, or realism integrations are included in this phase.

---

## Important Disclaimer

This autopilot is intended for STS-style Shuttle stacks (e.g., SOCK).

It is not a general-purpose autopilot and will likely not function correctly with non-Shuttle craft configurations.

The autopilot will not operate until the in-game disclaimer is acknowledged.

---

## Design Philosophy

- Correctness over cosmetics
- Deterministic guidance logic
- Strict SDLC governance
- No scope creep during Alpha
- Not a replacement for MechJeb

---

## Development Roadmap

### Version 1.0
DEL autopilot only (Alpha → Beta → Stable)

### Version 2.0
Ascent autopilot + abort modes + propellant dump

### Version 3.0
Orbital ops + rendezvous + rescue ascent

### Version 4.0
Optional realism integrations (RealFuels, failures, DRE, etc.)

---

## Compatibility

- KSP 1.12.x
- FAR (primary development backend)
- Stock aerodynamics (supported)

Not intended for RSS/RO.

---

## Repository Structure