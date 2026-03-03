# KSP Shuttle Autopilot Documentation

## Version 1.1 Baseline

This directory contains the authoritative documentation set for the KSP
Shuttle Autopilot project.

### Governing Documents

-   Master Reference Index
-   Project Scope
-   System Requirements
-   Architecture Design Document
-   Interface Control Document
-   Detailed Algorithm Specification

### Claude Usage Recommendation

When working with GitHub Claude:

Use the following initialization statement:

"Use Master Reference Index v1.1 as baseline. We are implementing
\[ModuleName\], Phase \[1 or 2\], governed by System Requirements §X and
Algorithm Specification §Y."

PDF versions are included for improved parsing and readability within
AI-assisted development tools.
---

## Repo Starter Scaffold (added)

This repo includes a minimal, buildable *starter* layout aligned to the **Architecture Design Document** and **Master Reference Index v1.1**.

### Directory Layout

- `src/` — C# plugin source (Phase 1 baseline modules scaffolded)
- `GameData/` — install-ready folder structure (place built DLL into `Plugins/`)
- `.github/` — issue templates and PR template
- `docs/` — authoritative documentation set (DOCX + PDF)

### Mobile + GitHub Claude Workflow

1. Open the relevant PDF in `docs/v1.1/` when referencing requirements.
2. Start each Claude session with:

> Use Master Reference Index v1.1 as baseline. We are implementing [ModuleName], Phase [1 or 2], governed by System Requirements §X and Algorithm Specification §Y.

3. Keep PRs/commits scoped to a single module whenever practical.

### Attribution

Repo owner / handle: **DJ Reonic**
