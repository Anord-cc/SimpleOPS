Copyright (c) 2026 Alex Nord. All rights reserved.
SPDX-FileCopyrightText: 2026 Alex Nord
SPDX-License-Identifier: LicenseRef-AlexNord-Proprietary-SourceAvailable
See LICENSE.md for terms. No copying, modification, distribution, commercial use, or AI/ML training except by written permission.

# SimpleOps

Standalone `.NET Framework 4.8` Windows desktop app for Microsoft Flight Simulator 2020/2024 with GSX Remote Control integration.

It polls `http://127.0.0.1:4789/telemetry`, keeps the `onGround` safety gate intact, listens for ramp phrases, and drives GSX through SimConnect Remote Control mode plus live GSX menu matching.

## Highlights

- Real desktop UI with live status, logs, parser diagnostics, and in-app settings
- Persisted settings in `%AppData%\SimpleOps\settings.json`
  - If `%AppData%` is unavailable, the app falls back to a local `appdata` folder beside the executable
- OpenAI ramp voice playback with the API key stored in Windows Credential Manager
- Selectable microphone and speaker devices
- Radio-style output controls
  - volume
  - left / right / both channel
  - pan
- Built-in deterministic phrase coverage plus user-editable aliases in `%AppData%\SimpleOps\phrases.json`
- GSX safety checks for unknown, weak, blocked, missing-menu, and ambiguous-menu cases

## Main files

- [SimpleOps.exe](C:\Users\Alex\SimpleOPS\SimpleOps.exe)
- [SimpleOps.csproj](C:\Users\Alex\SimpleOPS\SimpleOps.csproj)
- [RampControlForm.cs](C:\Users\Alex\SimpleOPS\src\RampControlForm.cs)
- [RampController.cs](C:\Users\Alex\SimpleOPS\src\RampController.cs)
- [OpenAiVoiceOutputService.cs](C:\Users\Alex\SimpleOPS\src\OpenAiVoiceOutputService.cs)
- [LocalSpeechInputService.cs](C:\Users\Alex\SimpleOPS\src\LocalSpeechInputService.cs)

## Run

Launch the desktop app directly:

```cmd
C:\Users\Alex\SimpleOPS\SimpleOps.exe
```

## Helpful flags

Dry-run without live GSX actions:

```cmd
C:\Users\Alex\SimpleOPS\SimpleOps.exe --dry-run
```

Dry-run one phrase:

```cmd
C:\Users\Alex\SimpleOPS\SimpleOps.exe --dry-run --no-speech --no-voice --test-phrase "request catering"
```

Run the built-in parser and safety harness:

```cmd
C:\Users\Alex\SimpleOPS\SimpleOps.exe --run-parser-tests
```

## Notes

- GSX must already be installed and running.
- The app keeps the `onGround == true` gate exactly intact before GSX actions are allowed.
- OpenAI is used for ramp voice output only in this version. Speech recognition remains local.
- The project now uses repo-local SimConnect and NAudio dependencies so the GitHub desktop build can run off-machine.
