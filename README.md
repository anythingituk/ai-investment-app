# AI Investment App

Private AI-assisted investment research app for Windows.

This repository currently contains early product direction, tray icon assets, and a static dashboard mockup for a local-first desktop application. The intended product is a private system tray app that opens a localhost dashboard, runs market scans and research jobs in the background, and presents suggestions for human review.

The app is intended to support research and monitoring only. It should not place trades automatically.

## Current Repository State

This repo now has an initial Windows tray app shell plus the earlier design assets.

Included today:

- A .NET 8 WinForms tray app in [`src/AlphaTray.App/`](/mnt/c/dev/ai-investment-app/src/AlphaTray.App)
- Windows tray icon variants in [`assets/`](/mnt/c/dev/ai-investment-app/assets)
- A static dashboard mockup in [`mockups/1/`](/mnt/c/dev/ai-investment-app/mockups/1)
- Product and architecture guidance in [`AGENTS.md`](/mnt/c/dev/ai-investment-app/AGENTS.md)

Not included yet:

- Market data integrations
- AI provider integrations
- Packaging, installer, or update workflow

## Product Direction

The planned application is a private Windows investment research tool with these core characteristics:

- Runs in the Windows system tray
- Opens a local dashboard at `127.0.0.1`
- Screens UK and US markets, with an initial focus on lower-priced shares
- Uses rules plus AI-assisted analysis to shortlist opportunities
- Tracks watchlists, research items, alerts, and API budget usage
- Surfaces long and short ideas for human review
- Keeps the human as the final decision-maker

The current mockup uses the product name `AlphaTray` and shows the intended dashboard shape:

- Market and saved-filter controls
- AI status and budget indicators
- AI-ranked share candidates
- Human approval suggestions
- Sector and index snapshot
- AI activity and sync status

## Preview The Mockup

Open the static HTML file directly in a browser:

- [`mockups/1/dashboard_mockup.html`](/mnt/c/dev/ai-investment-app/mockups/1/dashboard_mockup.html)

Supporting notes for that mockup are in:

- [`mockups/1/README.md`](/mnt/c/dev/ai-investment-app/mockups/1/README.md)

## Asset Structure

The icon set under [`assets/`](/mnt/c/dev/ai-investment-app/assets) includes multiple visual states for tray and app usage:

- `default_purple`
- `paused_grey`
- `syncing_active_green`
- `researching_orange`
- `researching_red`

Each variant includes multiple PNG sizes plus an `.ico` file.

## Intended MVP

The current planning direction in [`AGENTS.md`](/mnt/c/dev/ai-investment-app/AGENTS.md) points toward:

- Windows-first private installable app
- Localhost dashboard bound to `127.0.0.1`
- Local SQLite storage first
- Background jobs for scanning, research, monitoring, and sync
- GitHub-release-based update checks
- Later support for cross-device sync

Suggested tray menu for the MVP:

- `Open Dashboard`
- `Run Scan Now`
- `Pause AI`
- `Resume AI`
- `Settings`
- `Exit`

## Run The Tray App

Build and run the first Windows tray slice:

```powershell
dotnet build AlphaTray.sln
dotnet run --project src\AlphaTray.App
```

The app binds its local dashboard to `127.0.0.1`, starting at:

```text
http://127.0.0.1:48720
```

If that port is already occupied, it tries the next local ports. The tray menu includes `Open Dashboard`, `Run Scan Now`, `Pause AI`, `Resume AI`, `Settings`, and `Exit`.

Current implementation status:

- System tray process with state-specific icons.
- Embedded ASP.NET Core dashboard server bound to `127.0.0.1`.
- Dashboard and settings pages with pause/resume and manual scan controls.
- SQLite settings database at `%LOCALAPPDATA%\AlphaTray\alphatray.db`.
- Persisted pause/resume state.

## Build The Windows Installer

Build the self-contained Windows setup executable:

```powershell
powershell -ExecutionPolicy Bypass -File scripts\build-installer.ps1 -Version 0.1.0
```

The setup file is written to:

```text
dist\installer\AlphaTray-Setup-0.1.0.exe
dist\installer\setup.exe
```

The installer is per-user, installs under `%LOCALAPPDATA%\Programs\AlphaTray`, and includes a setup checkbox named `Start AlphaTray when Windows starts`. When selected, it writes an `HKCU` startup entry and removes that entry on uninstall.

The versioned filename is useful for release history. The stable `setup.exe` filename is useful as the predictable GitHub Release asset.

## Publish A GitHub Release

Install and authenticate GitHub CLI first:

```powershell
gh auth login
```

Then build the installer and publish both setup assets to GitHub Releases:

```powershell
powershell -ExecutionPolicy Bypass -File scripts\publish-github-release.ps1 -Version 0.1.0
```

This creates or updates tag `v0.1.0` in `anythingituk/ai-investment-app` and uploads:

- `AlphaTray-Setup-0.1.0.exe`
- `setup.exe`

## Notes

- This repository is for a private personal-use research application.
- The project should avoid exposing the dashboard to the public internet by default.
- Automated trading is explicitly out of scope for the intended MVP.
