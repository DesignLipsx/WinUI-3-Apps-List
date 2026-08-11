---
name: winui-packaging
description: "MSIX packaging, code signing, and distribution for WinUI 3 apps — build for release, certificate generation (winapp cert generate), certificate trust, code signing (winapp sign), self-contained deployment, CI/CD with GitHub Actions, and Microsoft Store submission. Use when preparing for release, creating MSIX installers, managing certificates, setting up CI/CD packaging, or publishing to the Microsoft Store."
---

### Quick Reference

| Task | Command |
|------|---------|
| Build for release | `.\BuildAndRun.ps1 /p:Configuration=Release` |
| Package + sign | `winapp package <dir> --cert devcert.pfx` |
| Generate + sign + package | `winapp package <dir> --generate-cert --install-cert` |
| Generate dev certificate | `winapp cert generate` |
| Trust certificate (admin) | `winapp cert install ./devcert.pfx` |
| Sign existing file | `winapp sign ./app.msix ./devcert.pfx` |
| Self-contained deployment | `winapp package <dir> --cert devcert.pfx --self-contained` |

### End-to-End Workflow

#### Step 1: Build for Release
Use the BuildAndRun.ps1 script from the `winui-dev-workflow` skill to build your app in Release configuration without launching it:

```powershell
.\BuildAndRun.ps1 /p:Configuration=Release -SkipRun
```

#### Step 2: Generate Certificate (one-time)
```powershell
winapp cert generate --manifest .
```
Creates `devcert.pfx` (default password: `password`). The `--manifest` flag auto-matches the `Publisher` field in `Package.appxmanifest`.

#### Step 3: Trust Certificate (one-time, requires admin)
```powershell
winapp cert install ./devcert.pfx
```
Adds cert to machine Trusted Root store. Persists across reboots.

#### Step 4: Package and Sign
```powershell
winapp package <build-output-dir> --cert ./devcert.pfx
```
This locates `appxmanifest.xml`, stages the layout, generates `resources.pri`, creates `.msix`, and signs it.

#### Step 5: Install or Distribute
```powershell
# Local install
Add-AppxPackage ./MyApp.msix

# Or double-click the .msix file
```

### Key Rules

- **Publisher must match** between certificate and manifest `Identity.Publisher` — use `winapp cert generate --manifest` to auto-match
- **Prefer `winapp package --cert`** over separate `winapp sign` — one step instead of two
- **`cert install` requires admin** — run terminal as Administrator
- **Default PFX password** is `password` — override with `--password`
- **`--timestamp`** is critical for production — without it, signatures expire with the cert:
  ```powershell
  winapp package <dir> --cert prod.pfx --timestamp http://timestamp.digicert.com
  ```
- **`--self-contained`** bundles Windows App SDK runtime — larger but no runtime dependency

### CI/CD with GitHub Actions

```yaml
name: Build and Package
on: [push]
jobs:
  build:
    runs-on: windows-latest
    steps:
      - uses: actions/checkout@v4
      - uses: microsoft/setup-WinAppCli@v0.1

      - name: Build
        run: dotnet build -c Release -p:Platform=x64

      - name: Package
        run: |
          winapp cert generate --if-exists skip --quiet
          winapp package ./bin/x64/Release/ --cert ./devcert.pfx --quiet

      - name: Upload artifact
        uses: actions/upload-artifact@v4
        with:
          name: msix-package
          path: "*.msix"
```

**CI/CD tips:**
- Use `--quiet` for clean output
- Use `--if-exists skip` with `cert generate` to avoid failures on re-runs
- Store production PFX as a repository secret

### Store Submission

1. **Partner Center account** — register at [partner.microsoft.com](https://partner.microsoft.com)
2. **Age ratings** — complete the questionnaire in Partner Center
3. **Screenshots** — capture at 1366x768 minimum resolution
4. **Privacy policy** — required for apps that access internet or user data
5. **Submit:** upload the signed `.msix` / `.msixbundle` produced by `winapp package` via [Microsoft Partner Center](https://partner.microsoft.com/dashboard) — Apps and games → your app → Packages. Microsoft Store submission is browser-based; there is no first-party CLI submit command yet.

### Troubleshooting

| Error | Solution |
|-------|----------|
| "Publisher mismatch" | Run `winapp cert generate --manifest` to re-generate |
| "Certificate not trusted" | Run `winapp cert install ./devcert.pfx` as admin |
| "Access denied" | `cert install` needs admin elevation |
| "Certificate file already exists" | Use `--if-exists overwrite` or `--if-exists skip` |
| "appxmanifest.xml not found" | Run `winapp init` or pass `--manifest <path>` |
| "Package installation failed" | Trust cert first; remove stale: `Get-AppxPackage <name> \| Remove-AppxPackage` |
| Signature invalid after time | Re-sign with `--timestamp` |

### References

| File | Read when... |
|------|-------------|
| `references/sourcegen-patterns.md` | Setting up AOT/trimming, JSON source generators, NativeAOT readiness, CsWin32 |
