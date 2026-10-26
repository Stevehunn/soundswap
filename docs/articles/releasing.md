# Releasing

1. Update `CHANGELOG.md`.
2. Make sure `main` is green (`build` workflow).
3. Tag and push:
   ```bash
   git tag v1.3.0
   git push origin v1.3.0
   ```
4. The `release` workflow runs the tests, builds the self-contained ReadyToRun app, the installer
   (`SoundSwap-Setup-1.3.0.exe`) and the portable zip, and attaches them to a new GitHub release with generated notes.
5. Running copies of SoundSwap will show the update button within 12 hours.

## Rules the updater relies on
- Tag format `vMAJOR.MINOR.PATCH`; the assembly version is set from it (`-p:Version=`).
- The installer asset name contains `setup` and ends with `.exe`.
- Do not publish a release as *draft* or *pre-release* if users should receive it.

## Local build
```powershell
.\scripts\publish.ps1 -Version 1.3.0 -Installer -Portable   # needs Inno Setup 6
```

## Code signing (recommended next step)
Unsigned installers trigger SmartScreen warnings. Free code signing for open source is available through
[SignPath Foundation](https://signpath.org/). Once signed, no change is needed in the updater.
