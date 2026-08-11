---
name: winui-setup
description: "Install and verify the prerequisites the win-dev-skills WinUI 3 toolchain depends on — .NET SDK 10, the WinApp CLI, the WinUI 3 .NET templates, and Developer Mode. Use when setting up a new machine, after a Windows reset, or when another winui skill reports a missing prerequisite (e.g., `winapp` not found, `dotnet` not found, the WinUI 3 templates are missing, or Developer Mode is off)."
disable-model-invocation: true
---

### Purpose

Install and verify the prerequisites every other `winui-*` skill assumes are already present on the machine.

This skill is **idempotent** — every step checks first, skips if already satisfied, prints `[OK] already installed` and moves on. Re-running on a fully set-up machine is a fast no-op.

### Steps

The first thing to do is **batch all detection up front** — run every check in parallel/together so you can show the user the full picture before installing anything. Then install only what's missing.

#### Detect everything

Run all of these together; collect the results:

```powershell
# .NET SDK — accept any installed SDK >= 8.0
$dotnetSdks = (& dotnet --list-sdks 2>$null) -replace ' \[.*$',''
$dotnetOk   = $dotnetSdks | ForEach-Object { [version]($_ -split '-')[0] } |
              Where-Object { $_.Major -ge 8 } | Select-Object -First 1

# WinApp CLI — needs to be present AND >= 0.3
$winappVersion = $null
$winappOk      = $false
$winappCmd     = Get-Command winapp -ErrorAction SilentlyContinue
if ($winappCmd) {
    $raw = (& winapp --version 2>$null) -as [string]
    if ($raw) {
        $base = ($raw -split '-')[0]   # strip "-prerelease.N" if present
        try {
            $winappVersion = [version]$base
            $winappOk      = $winappVersion -ge [version]'0.3'
        } catch {}
    }
}

# WinUI 3 templates
$templatesOk = [bool](dotnet new list winui 2>$null | Select-String 'winui-mvvm' -Quiet)

# Developer Mode
$devModeOk = ((Get-ItemProperty `
  -Path 'HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\AppModelUnlock' `
  -Name AllowDevelopmentWithoutDevLicense -ErrorAction SilentlyContinue
).AllowDevelopmentWithoutDevLicense) -eq 1
```

Print a one-shot status table so the user sees what you're about to do:

```
.NET SDK ≥ 8           ✅ found 10.0.100   (or ❌ missing — will install Microsoft.DotNet.SDK.10)
WinApp CLI             ⚠ found 0.3.1 — will upgrade to latest
                       (or ❌ missing/too old — will install Microsoft.WinAppCLI)
WinUI 3 templates      ✅ found — will reinstall to make sure they're at latest
Developer Mode         ❌ disabled — needs admin to enable
```

> **Always upgrade WinApp CLI and the WinUI templates** even when they're already present — they ship breaking changes between releases and the rest of the `winui-*` skills assume latest. The minimum bar is "WinApp CLI ≥ 0.3 and templates installed at all"; the goal is "both at latest".

#### Install what's missing

Skip anything already-OK from detection. The remaining steps:

##### .NET SDK (only if no SDK ≥ 8.0 was found)

```powershell
winget install --id Microsoft.DotNet.SDK.10 --exact --silent --accept-package-agreements --accept-source-agreements
```

`.NET 8.0` is the floor. If the user already has 8.0, 9.0, or 10.0 installed (any patch), the requirement is met — do not install another SDK side-by-side.

##### WinApp CLI — install if missing/old, then always upgrade

If `$winappOk` is false (missing or `< 0.3`), install it. Then **always** run `winget upgrade` regardless, so even already-present installs get bumped to latest:

```powershell
# Install only if missing or too old
winget install --id Microsoft.WinAppCLI --exact --silent --accept-package-agreements --accept-source-agreements

# Always — upgrade to latest (no-op if already at latest)
winget upgrade --id Microsoft.WinAppCLI --exact --silent --accept-package-agreements --accept-source-agreements
```

##### Refresh `$env:Path`

If you installed the .NET SDK or anything else via winget in this session, **refresh PATH** so subsequent steps can find the new tools. Without this, `dotnet new install` will fail with "command not found" even though the SDK is on disk:

```powershell
$env:Path = [Environment]::GetEnvironmentVariable('Path','Machine') + ';' + [Environment]::GetEnvironmentVariable('Path','User')
```

##### WinUI 3 .NET templates — always reinstall to get latest

Run this every time, whether or not `$templatesOk` was true. `dotnet new install` against an already-installed template package upgrades it in place to the latest version:

```powershell
dotnet new install Microsoft.WindowsAppSDK.WinUI.CSharp.Templates
```

##### Developer Mode (ask the user first!)

Developer Mode is the DWORD `AllowDevelopmentWithoutDevLicense` under `HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\AppModelUnlock`. Setting it requires admin, which means a UAC prompt will pop up. **Do not just trigger UAC out of nowhere — ask the user first** so they're not surprised by the elevation prompt. Use language like:

> Developer Mode is currently disabled. Enabling it requires a one-time admin elevation (a UAC prompt will appear). Would you like me to enable it now? (yes / no / I'll do it later)

Only if the user agrees, re-elevate **only this step** via UAC:

```powershell
Start-Process powershell -Verb RunAs -ArgumentList @(
  '-NoProfile','-Command',
  "New-Item -Path 'HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\AppModelUnlock' -Force | Out-Null; " +
  "Set-ItemProperty -Path 'HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\AppModelUnlock' " +
  "-Name AllowDevelopmentWithoutDevLicense -Type DWord -Value 1"
) -Wait
```

If the user declines (either says no, or accepts but then dismisses the UAC prompt), do not abort the whole skill. Print the literal command above so they can run it later from an elevated PowerShell, and continue to the summary.

### Final summary — always print this

After everything, print a single-table summary so the user knows exactly what changed:

```
==== winui-setup summary ====
.NET SDK ≥ 8               ⏭ already present (9.0.313)
WinApp CLI                 ✅ upgraded to 0.4.0  (or ✅ installed, ⏭ already at latest, ❌ failed)
WinUI 3 templates          ✅ updated to latest
Developer Mode             ✅ enabled  (or ⏭ skipped — user declined, or ❌ failed: <reason>)

You're ready. Try:
  copilot --agent winui:winui-dev -p "build me a WinUI 3 markdown editor"
```

### Things to NOT do

- ❌ **Do not install Visual Studio.** It's optional and multi-GB. If the user wants the full Visual Studio + WinUI workload (recommended for the XAML-diagnostic workaround that `winui-dev-workflow` calls out), tell them at the end of the summary they can install it themselves with:
  ```powershell
  winget install Microsoft.VisualStudio.Community --override "--add Microsoft.VisualStudio.Workload.Universal"
  ```
- ❌ **Do not install GitHub Copilot CLI.** If this skill is running, it's already installed.
- ❌ **Do not elevate the entire session** — only step 5 needs admin. Elevating earlier steps would install winget packages into the admin user's profile instead of the user's, which is wrong.
- ❌ **Do not skip the PATH refresh** — agents that skip it install the SDK and then immediately fail on `dotnet new install`.
- ❌ **Do not trigger UAC for Developer Mode without asking the user first** — the prompt is jarring if it pops up unannounced. Always confirm before elevating.
- ❌ **Do not silently retry on failure.** If a `winget install` fails (no network, package source down, permissions), record the error in the summary table and move on. Let the user see what failed.
- ❌ **Do not install .NET 10 if the machine already has any .NET SDK ≥ 8.0** — the floor is 8.0, and adding another SDK side-by-side wastes disk space.
