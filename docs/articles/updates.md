# Updates

## What happens
1. **Check**: about 10 seconds after launch, then every 12 hours, `UpdateChecker` calls
   `GET https://api.github.com/repos/Stevehunn/soundswap/releases/latest` (drafts and pre-releases are never returned).
2. If the release tag (`v1.3.0`) is newer than the running version, the header shows **New version available: v1.3.0**.
3. **Click** → `UpdateDownloader` downloads the installer asset to `%TEMP%\SoundSwap\update` with a progress percentage.
4. **Verify** → size and, when GitHub provides it, the SHA-256 digest of the asset. A mismatch deletes the file and the button offers to retry.
5. The button becomes **Install version 1.3.0**. Click → the installer starts and SoundSwap exits so files can be replaced;
   the installer offers to relaunch SoundSwap when it finishes.

## Which asset is downloaded
The first `.exe` whose name contains `setup` (for example `SoundSwap-Setup-1.3.0.exe`), otherwise the first `.msi`.
The portable zip is ignored. If a release has no installer the button opens the release page.

## Safety
- Only `https` URLs on `github.com`, `*.github.com` and `*.githubusercontent.com` are contacted or opened (also after redirects).
- File name is reduced to its last segment; only `.exe` / `.msi` are accepted.
- Downloads go to a `.part` file and are renamed only after verification.
- Installers are not code-signed yet (SmartScreen may warn).

## Privacy and control
The only network request SoundSwap makes is this check, so GitHub sees the user's IP address like for any web request.
No identifier or telemetry is sent. Untick **Check for updates automatically** to disable it; **Check for updates** in the footer checks on demand.

## Forks
Pass another `UpdateOptions(owner, repository)` to `UpdateService` to follow your own repository.
