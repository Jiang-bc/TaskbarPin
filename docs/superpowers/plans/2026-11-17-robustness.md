# TaskbarPin Robustness Implementation Plan

> **For agentic workers:** Use executing-plans to implement this plan inline, with test-driven-development and one independent review at the end.

**Goal:** Fix the five reviewed lifecycle, interaction, geometry and rendering defects.

**Architecture:** Retain the existing form under an independent ApplicationContext. Share pure taskbar geometry between positioning, dragging and collapse. Commit rendering state only on native success.

**Tech Stack:** WinForms, .NET Framework 4.x, C# 5, Win32; no dependencies.

**Spec:** `docs/superpowers/specs/2026-11-17-robustness-design.md`

## Global Constraints

- Keep the portable, single-source application and existing command-line interface.
- Work in the current checkout; do not create branches or commits without a user request.
- Preserve the user's running installation and avoid restarting Explorer.
- Tests may temporarily change auto-hide only with restoration in finally.

## Review Focus

- A missing taskbar must not recreate a detached topmost widget or end the message loop.
- An intentional exit must not trigger recovery or leave tray/timer resources alive.
- A user dragging while collapse is pending must not remain captured or click through forever.
- Negative monitor coordinates and very small taskbars must produce valid, contained geometry.
- A failed blit must retry even when the desired coordinates have not changed.

### Task 1: Lifecycle and input

**Files:** `src/TaskbarPin.cs`, `tests/RobustnessTests.cs`.

- [x] Add failing regressions for independent context lifetime, native handle destruction/recreation, restored mouse input after collapse, and capture loss without a toggle.
- [x] Run against the current executable and confirm the named failures.
- [x] Add `PinApplicationContext`, reset handle-bound state on destruction, restrict transparency to `_waitingForCollapse`, and end canceled drags.
- [x] Run the lifecycle/input regressions and existing UI suite.

### Task 2: Geometry and rendering

**Files:** `src/TaskbarPin.cs`, `tests/RobustnessTests.cs`.

- [x] Add literal geometry fixtures for bottom/top/left/right, negative origins, collapsed rectangles, cursor exit points, small bars, and extreme offsets.
- [x] Add a native drawing failure test that verifies unchanged committed coordinates and a subsequent retry.
- [x] Confirm failures, then add `TaskbarGeometry` with placement, visibility and collapse methods used by production.
- [x] Update dragging to the geometry's long axis; retain the OffsetX configuration key.
- [x] Check parenting and coordinate conversion, commit draw coordinates only on success, and preserve a dirty flag until successful recovery.
- [x] Run all robustness tests and both existing executable test suites.

### Task 3: Documentation and review

**Files:** `README.md`, `REVIEW.md`.

- [x] Document recovery, restored input, supported geometry and the regression commands.
- [x] Run build, all three suites and git diff checks; record exact results.
- [x] Request an independent review of the uncommitted patch and fix significant findings with regressions.
