# SimpleOps GSX Ramp

Compiled C# ramp companion for Microsoft Flight Simulator 2020/2024 with GSX integration.

It polls your local telemetry API at `http://127.0.0.1:4789/telemetry`, checks `onGround`, and only becomes active when the aircraft is on the ground. It then listens for voice phrases, opens GSX with the configured GSX hotkey, reads the live GSX menu file, and picks the matching menu entry automatically.

## What it does

- Polls your local telemetry bridge
- Arms only when telemetry says `onGround = true`
- Recognizes:
  - `connect ground power`
  - `connect ground power unit`
  - `prepare for pushback`
  - `push south west`
  - `push back south west`
  - `push south`
  - `push west`
- Sends the matching request into GSX with the GSX hotkey plus live menu selection
- Skips unsafe actions when the phrase is blocked, weak, unknown, ambiguous, or the GSX menu is ambiguous

## Project

The source is in a proper `.NET Framework 4.8` project:

- [SimpleOps.GsxRamp.csproj](C:\Users\Alex\SimpleOPS\SimpleOps.GsxRamp.csproj)

The built executables are:

- [SimpleOps.GsxRamp.exe](C:\Users\Alex\SimpleOPS\SimpleOps.GsxRamp.exe)
- [SimpleOps.GsxRamp.exe](C:\Users\Alex\SimpleOPS\bin\Release\SimpleOps.GsxRamp.exe)

## Run

Launch the executable directly:

```cmd
C:\Users\Alex\SimpleOPS\SimpleOps.GsxRamp.exe
```

## Helpful flags

```cmd
C:\Users\Alex\SimpleOPS\SimpleOps.GsxRamp.exe --no-speech --run-duration-seconds 5
```

```cmd
C:\Users\Alex\SimpleOPS\SimpleOps.GsxRamp.exe --dry-run --no-speech --test-phrase "push south west"
```

Run the built-in parser and safety harness without sending live GSX keypresses:

```cmd
C:\Users\Alex\SimpleOPS\SimpleOps.GsxRamp.exe --run-parser-tests
```

## Notes

- GSX must already be installed and running.
- The FSDreamTeam install root is read from `HKCU\Software\Fsdreamteam\root`.
- The pushback submenu selection is text-based, so if GSX uses different wording on your setup, update the pattern list in [RampController.cs](C:\Users\Alex\SimpleOPS\src\RampController.cs).
- Parser tests and dry-run mode do not send real GSX keypresses.
