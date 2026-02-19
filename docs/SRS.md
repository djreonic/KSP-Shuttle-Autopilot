# Software Requirements Specification (SRS)

## Version 1.2 – DEL Alpha

### Functional Requirements

FR-001 Disclaimer Acknowledgement  
Autopilot shall not compute or execute guidance until disclaimer is acknowledged.

FR-002 Scale Detection  
System shall support Auto, Manual, and Preset scale modes.

FR-003 Deorbit Solver  
Shall compute OMS burn targeting runway intercept.

FR-004 Entry Guidance  
Shall perform bank-modulated energy control.

FR-005 TAEM / HAC  
Shall compute intercept geometry.

FR-006 Autoland  
Shall perform flare and rollout management.

### Non-Functional Requirements

- Deterministic control decisions
- No runaway oscillations
- Manual disengage always available
- No hard mod dependencies