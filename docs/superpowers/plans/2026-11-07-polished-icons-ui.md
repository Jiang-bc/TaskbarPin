# TaskbarPin icon and native UI implementation plan

> **For agentic workers:** Execute inline; the user has authorized autonomous implementation and a final local commit.

**Goal:** Ship a consistent multi-size icon and a compact, legible, theme-aware WinForms menu with a reproducible Framework csc build.

**Architecture:** Keep the existing single-file application. Share vector geometry between runtime badges and an offline icon generator; use cached palettes and a small ToolStrip renderer. Do not change taskbar behavior or add a dashboard.

**Tech Stack:** C# 5, .NET Framework 4.x, System.Drawing, Windows Forms, PowerShell.

**Spec:** The task handoff in this session: real ICO assets, native menu spacing/status/icons, light/dark/high-contrast support, clear pin states, reliable resource disposal, screenshots, local branch/commit, outputs/TaskbarPin.exe.

## Global constraints

- Use the installed Windows Framework csc; no SDK or package installation.
- Preserve the existing reflection tests and their criteria.
- Tests must restore taskbar auto-hide state; never modify global theme to test rendering.
- Keep direct compilation of src/TaskbarPin.cs supported.
- Commit only locally on feat/polished-icons-ui; do not push or create a PR.

## Review focus

- Light taskbar and dark application theme may differ; resolve their palettes separately.
- High contrast must use system colors and preserve orientation/state cues.
- Shell/tray icon replacement must not leak native or managed icon resources.
- Menu reopen/theme refresh and repeated disposal must not retain images/fonts/events.
- Paths with spaces and an arbitrary working directory must build the same assets.

### Task 1: Vector artwork and build

Files: src/TaskbarPin.cs, tools/IconAssetBuilder.cs, assets/TaskbarPin.ico, assets/TaskbarPin.png, build.ps1, README.md, tests/UiTests.cs.

- [ ] Add and run checks for embedded icon resources and ICO frames (16, 20, 24, 32, 40, 48, 64, 128, 256); observe missing-resource failure on baseline.
- [ ] Implement shared scale-aware badge/pin geometry and deterministic ICO generation using PNG frames.
- [ ] Compile through build.ps1 with /win32icon, using Framework64 csc with Framework fallback.
- [ ] Verify direct single-source csc compilation and reproducible generated asset hashes.

### Task 2: Menu, themes, and lifetime

Files: src/TaskbarPin.cs, tests/UiTests.cs.

- [ ] Add checks for persistent state text, action icons, checked startup indication, theme contrast, and resource lifetime; observe failures on baseline.
- [ ] Add a compact menu status row, consistent item height/gutters and pin/reset/start/exit glyphs.
- [ ] Resolve application/taskbar theme independently; use system rendering/colors in high contrast and refresh through Windows theme/settings messages and menu opening.
- [ ] Own/dispose menu images, fonts, tray wrappers/native handles, and extracted application icon; keep disposal idempotent.
- [ ] Run original regression tests and new UI/lifetime checks.

### Task 3: Visual verification and delivery

Files: tests/UiTests.cs, README.md, outputs outside repository.

- [ ] Render real ToolStrip menus and badges in light/dark/high-contrast palettes without changing Windows settings; inspect screenshots and correct clipping/spacing if necessary.
- [ ] Run real desktop smoke check, preserve/restore auto-hide, and close only test-owned windows/processes.
- [ ] Review final diff, run final build/tests, copy EXE to the session outputs directory and record its hash.
- [ ] Commit final changes locally with requested attribution, report branch/commit/tests and platform limitations.

## Execution notes

- The user explicitly requested autonomous execution; design and routine implementation choices are made inline.
- Baseline: clean main branch; no repository-specific CLAUDE.md or AGENTS.md found.
