---
name: winui-ui-testing
description: "Automated UI testing for WinUI 3 apps — generate a batch test script, run all tests in one pass, read results. Covers element assertions, interactions, value checking (TextBox, ComboBox, ToggleSwitch), file pickers, flyouts, dialogs, persistence, and accessibility audits."
---

### Approach

The goal of this skill is to validate UI and app functionality automatically, without manual interaction, by exercising the app's UI elements, verifying their state, and asserting that the app behaves as expected under test conditions.

There are two main approaches:
1. Interactive exploration — manually run the app, use `winapp ui <command>` to explore the UI tree, find AutomationIds, verify element properties, and test functionality interactively. This is useful for discovery, but slow and expensive if repeated for every test iteration.
2. Scripted batch testing — generate a `ui-tests.ps1` script that exercises all UI elements and asserts expected behavior in one pass. This allows you to run the tests automatically, capture results, and iterate quickly without manually interacting with the app each time.

Unless the user asked for interactive exploration, or you are unfamiliar with the code/app or need to explore the UI tree to discover AutomationIds for hidden or dynamically generated elements (flyouts, dialogs, lazy-loaded content), **prefer scripted batch testing** — it is faster, repeatable, and produces a record of pass/fail results that can be reviewed and acted on.

### `winapp ui` Verbs

`status`, `inspect`, `search`, `get-property`, `get-value`, `screenshot`, `invoke`, `click`, `set-value`, `focus`, `scroll`, `scroll-into-view`, `wait-for`, `list-windows`, `get-focused`. Run `winapp ui --cli-schema` for the complete command structure as JSON, or `winapp ui <verb> --help` for any single verb.

### Step 1: Use the Running App

If the app is already running, use its PID. **Do NOT relaunch** — use the PID already captured from the build step. If the app is not running, build and launch it using the guidance in the winui-dev-workflow skill.

### Step 2: Write the Test Script

**If you wrote the code:** Skip inspect — you already know all the AutomationIds and control structure from the XAML and code-behind. Write tests directly from that knowledge. Inspect misses popups, flyouts, dialogs, and lazy-loaded content anyway.

**If you're verifying code you didn't write:** Run inspect first to discover the UI:
```powershell
winapp ui inspect -a <PID> --interactive
```
Then read the XAML files to find AutomationIds that aren't currently visible (flyout items, dialog buttons, secondary pages).

Create a `ui-tests.ps1` file that tests all the app's requirements in one pass:

```powershell
# ui-tests.ps1
param([Parameter(Mandatory)][int]$AppPid)
# NOTE: Do NOT name the parameter $Pid — it's read-only in PowerShell

$ErrorActionPreference = 'Continue'
$pass = 0; $fail = 0; $results = @()

# Get main window HWND (avoids PopupHost interference with JSON parsing)
$windows = winapp ui list-windows -a $AppPid --json 2>$null | ConvertFrom-Json
$hwnd = ($windows | Where-Object { $_.title -ne "PopupHost" } | Select-Object -First 1).hwnd

function Test-UI {
    param([string]$Name, [scriptblock]$Script)
    # IMPORTANT: Inside $Script, use 'throw' to signal failure — NOT 'exit 1'
    # (exit terminates the entire script, not just the test)
    try {
        $output = & $Script 2>&1
        if ($LASTEXITCODE -eq 0) {
            $script:pass++; $script:results += @{ name = $Name; status = "PASS" }
        } else {
            $script:fail++; $script:results += @{ name = $Name; status = "FAIL"; detail = "$output" }
        }
    } catch {
        $script:fail++; $script:results += @{ name = $Name; status = "FAIL"; detail = "$_" }
    }
}

# ─── Element Existence ───
Test-UI "NavHome exists" { winapp ui wait-for "NavHome" -a $AppPid -t 3000 }
Test-UI "NavSettings exists" { winapp ui wait-for "NavSettings" -a $AppPid -t 3000 }

# ─── Navigation ───
Test-UI "Navigate to Settings" { winapp ui invoke "NavSettings" -a $AppPid }
Test-UI "Settings page loaded" { winapp ui wait-for "TxtUserName" -a $AppPid -t 3000 }

# ─── Interactions ───
Test-UI "Set username" { winapp ui set-value "TxtUserName" "TestUser" -a $AppPid }
Test-UI "Click Save" { winapp ui invoke "BtnSave" -a $AppPid }  # commits the TextBox binding
Test-UI "Username value set" {
    winapp ui wait-for "TxtUserName" -a $AppPid --value "TestUser" -t 2000
}

# ─── Value assertions for different control types ───
Test-UI "Theme is System default" {
    winapp ui wait-for "CmbTheme" -a $AppPid --value "System default" -t 2000
}
Test-UI "Logging is off" {
    winapp ui wait-for "TglLogging" -a $AppPid --value "Off" -t 2000
}

# ─── Accessibility Audit ───
# Only audit controls in the app's main window (exclude OS picker/popup controls)
$allElements = (winapp ui inspect -a $AppPid --interactive --json 2>$null | ConvertFrom-Json).elements
$appElements = @($allElements | Where-Object {
    $_.type -match 'Button|TextBox|ComboBox|CheckBox|ToggleSwitch|TabItem|Edit' -and
    $_.name -notmatch 'Minimize|Maximize|Close|System' -and          # window chrome
    $_.className -notmatch 'PickerHost|#32770|CabinetWClass'         # OS dialogs
})
$missingId = @($appElements | Where-Object { -not $_.automationId })
if ($missingId.Count -eq 0) {
    $pass++; $results += @{ name = "All app controls have AutomationId"; status = "PASS" }
} else {
    $fail++
    $names = ($missingId | ForEach-Object { "$($_.type) '$($_.name)'" }) -join ", "
    $results += @{ name = "AutomationId coverage"; status = "FAIL"; detail = "Missing: $names" }
}

# ─── State Screenshots (capture each meaningful state for visual review) ───
New-Item -ItemType Directory -Force -Path "screenshots" | Out-Null
winapp ui screenshot -a $AppPid -o "screenshots/01-initial.png" 2>$null
# ...take more screenshots after key interactions above (mode switches, dialogs opened, etc.)

# ─── Final Screenshot ───
winapp ui screenshot -a $AppPid -o "test-screenshot.png" 2>$null

# ─── Results ───
Write-Host "`nPassed: $pass | Failed: $fail"
$results | Where-Object { $_.status -eq "FAIL" } | ForEach-Object {
    Write-Host "  FAIL: $($_.name) — $($_.detail)" -ForegroundColor Red
}
$results | ConvertTo-Json | Out-File "test-results.json"
if ($fail -gt 0) { exit 1 } else { exit 0 }
```

### What to Test

Write tests for **every requirement** from the user's prompt:

| Requirement type | Test approach |
|---|---|
| "Has a button that does X" | `search` to verify exists, `invoke` to click, `wait-for --value` to check result |
| "Text field shows value" | `wait-for "TxtName" --value "expected"` — works for TextBox, TextBlock, labels |
| "Status bar contains text" | `wait-for "StatusBar" --value "words" --contains` — substring match for dynamic content |
| "Dropdown is set to X" | `wait-for "CmbTheme" --value "Dark"` — reads the selected item automatically |
| "Toggle is on/off" | `wait-for "TglFeature" --value "On"` — reads the toggle state |
| "Navigation between pages" | `invoke` nav item, `wait-for` a page-specific element to appear |
| "Open file dialog" | `invoke` trigger, `list-windows` to find picker HWND, interact with `-w` |
| "Save file dialog" | Same as open — find picker with `list-windows`, `set-value` filename, `invoke` Save |
| "Right-click context menu" | `click --right` on element, `invoke` the flyout MenuItem |
| "Confirmation dialog" | `invoke` trigger, `search` for dialog buttons, `invoke` Primary/Secondary/Close |
| "Data persists" | Set values, `invoke` a button (to commit bindings), verify data file on disk (`Get-Content` + `ConvertFrom-Json`) |
| "All controls accessible" | `inspect --interactive --json` + check all have AutomationId |

### Step 3: Run and Read Results

```powershell
.\ui-tests.ps1 -AppPid <PID>
```

Read `test-results.json` for structured pass/fail. Only fix code if tests fail.

### Step 3.5: Look at the Screenshots

UIA assertions don't see clipping, overlap, wrong theming, or controls bleeding past their container — UIA returns `PASS` while the app is visually broken. **Capture screenshots with `winapp ui screenshot` and view each PNG.**

Capture the initial state and any state after a major interaction (the State Screenshots block in the script template above handles this).

**Visual checklist — fail the run if any item is `no`:**
- [ ] No unintended scrollbars
- [ ] No text ending in `…` that shouldn't be
- [ ] Hero elements fully visible (not sliced)
- [ ] Right-edge controls fully visible
- [ ] No overlapping rows
- [ ] Content uses the available width — no asymmetric dead zones (e.g. content pinned to one edge leaving empty space on the other)
- [ ] Spacing intentional — not cramped, not unintentionally vast
- [ ] Theming matches the user's ask (Light/Dark/HighContrast if relevant)
- [ ] Focus/hover/error states render if tested

If the checklist fails, it's a bug — fix before declaring done. Window too small → grow per `winui-design` Step 4.

### Step 4: Fix and Rerun (if the user asked for it)

If tests fail:
1. Read the failure details from `test-results.json`
2. Batch-fix all issues in one pass
3. Rebuild with `.\BuildAndRun.ps1` (blocking mode — shows crash info if the fix broke something)
4. Rerun `.\ui-tests.ps1 -AppPid <PID>` (parse PID from the `launched (PID: XXXXX)` output)

**Maximum 2 fix-and-rerun cycles.** If the same tests keep failing after 2 cycles, report them as known issues and move on — do not keep iterating.

### Assertion Reference

Use `wait-for --value` as the primary assertion — it uses a smart fallback chain that reads the right value for any control type:

| Control type | `--value` reads from | Example |
|---|---|---|
| TextBlock / Label | Name property | `wait-for "LblTitle" --value "Home"` |
| TextBox / NumberBox | ValuePattern | `wait-for "TxtName" --value "John"` |
| RichEditBox | TextPattern | `wait-for "Editor" --value "Hello"` |
| ComboBox | Selected item (SelectionPattern) | `wait-for "CmbTheme" --value "Dark"` |
| ToggleSwitch | Toggle state (On/Off) | `wait-for "TglDark" --value "On"` |
| CheckBox | Toggle state (On/Off) | `wait-for "ChkAgree" --value "On"` |

**Full assertion commands:**

| Assertion | Command |
|---|---|
| Element exists | `winapp ui wait-for "Id" -a PID -t 3000` |
| Element has exact value | `winapp ui wait-for "Id" -a PID --value "expected" -t 3000` |
| Value contains text | `winapp ui wait-for "Id" -a PID --value "words" --contains -t 3000` |
| Element gone | `winapp ui wait-for "Id" -a PID --gone -t 3000` |
| Specific property | `winapp ui wait-for "Id" -a PID -p IsEnabled --value "True" -t 3000` |
| Button clickable | `winapp ui invoke "Id" -a PID` (exit code 0) |
| Set then verify | `winapp ui set-value "Id" "text" -a PID` then `wait-for --value` |
| Screenshot | `winapp ui screenshot -a PID -o path.png` |
| Dialog appeared | `winapp ui list-windows -a PID --json` (check window count) |
| Right-click menu | `winapp ui click "Id" -a PID --right` then `wait-for` menu item |
| Read raw property | `winapp ui get-property "Id" -a PID -p IsEnabled --json` |
| Read current value (no wait) | `(winapp ui get-value "Id" -a PID --json \| ConvertFrom-Json).text` — always pass `--json` when capturing into a variable (plain stdout can include advisory text like "Auto-selected HWND … from N windows"); otherwise prefer `wait-for --value` |
| Scroll item into view | `winapp ui scroll-into-view "Id" -a PID` — call before `wait-for` on virtualized ListView/repeater items below the fold |
| Set keyboard focus | `winapp ui focus "Id" -a PID` — cleaner than clicking another control to trigger a TextBox `LostFocus` commit |

### Testing File Pickers

File/folder pickers (FileOpenPicker, FileSavePicker, FolderPicker) run in a separate `PickerHost` process but are fully interactable. The picker appears as an owned dialog window.

```powershell
# 1. Trigger the picker
winapp ui invoke "BtnOpenFile" -a $AppPid

# 2. Find the picker window (it's a dialog owned by the app window)
Start-Sleep 1
$allWindows = winapp ui list-windows -a $AppPid --json 2>$null | ConvertFrom-Json
$picker = $allWindows | Where-Object { $_.title -match "Open|Save" }
$pickerHwnd = $picker.hwnd

# 3. Interact with the picker using -w <HWND>
#    Type a filename:
winapp ui set-value "FileNameControlHost" "test.txt" -w $pickerHwnd
#    Click Open/Save:
winapp ui invoke "Open" -w $pickerHwnd     # or "Save", "Cancel"
#    Or cancel:
winapp ui invoke "Cancel" -w $pickerHwnd

# 4. Verify the app processed the file
winapp ui wait-for "StatusBar" -a $AppPid -p Name --value "opened" -t 3000
```

**Tip:** Use `winapp ui inspect -w <pickerHwnd> --interactive` to discover the picker's controls — they include the folder tree, file list, filename textbox, and Open/Cancel buttons.

### Testing Context Menus and Flyouts

MenuFlyouts and ContextFlyouts are fully testable. They appear in the UI automation tree when open.

```powershell
# 1. Right-click to open a ContextFlyout
winapp ui click "LstItems" -a $AppPid --right
Start-Sleep 0.5

# 2. The flyout MenuItems appear in the tree immediately
#    Find them with inspect or search:
winapp ui inspect -a $AppPid --interactive   # shows MnuCopy, MnuDelete, etc.

# 3. Click a flyout item
winapp ui invoke "MnuCopy" -a $AppPid

# 4. Verify the action
winapp ui wait-for "StatusText" -a $AppPid -p Name --value "Copied" -t 2000
```

**For MenuBar flyouts** (File, Edit, View menus):
```powershell
# Click the menu header to open
winapp ui invoke "FileMenu" -a $AppPid
Start-Sleep 0.5
# Click the sub-item
winapp ui invoke "MenuSaveAs" -a $AppPid
```

### Testing ContentDialogs

ContentDialogs are in-app controls (same window) — they appear directly in the UI tree when shown.

```powershell
# 1. Trigger the dialog
winapp ui invoke "BtnDelete" -a $AppPid
Start-Sleep 0.5

# 2. The dialog buttons appear in the tree
#    For a standard confirmation dialog:
winapp ui search "Primary" -a $AppPid --json   # finds the primary button
winapp ui invoke "Primary" -a $AppPid           # click "Yes"/"Delete"/"Save"
#    Or:
winapp ui invoke "Secondary" -a $AppPid         # click "No"/"Don't Save"
winapp ui invoke "Close" -a $AppPid             # click "Cancel"

# 3. Wait for dialog to dismiss
winapp ui wait-for "Primary" -a $AppPid --gone -t 3000
```

**Tip:** ContentDialog buttons often don't have custom AutomationIds — use `inspect` to find the actual selector (slug or text match).

### Key Gotchas

- **`set-value` does NOT commit default TextBox bindings** — WinUI 3 `x:Bind TwoWay` on TextBox.Text updates the ViewModel on `LostFocus` by default. UIA `set-value` changes the text but doesn't trigger focus events. **Fix:** apps should use `UpdateSourceTrigger=PropertyChanged` on TextBox bindings (see design skill). If the app doesn't, `invoke` a button or `click` another element after `set-value` to trigger `LostFocus`.
- **Verify persistence via the data file, not UI relaunch** — killing and relaunching a packaged app from a test script is fragile (MSIX registration timing, PID issues). Instead, check the data file on disk: `Get-Content $dataFile | ConvertFrom-Json` and verify expected values.
- **Use `$AppPid` not `$Pid`** — `$Pid` is a read-only automatic variable in PowerShell
- **Use `--value` without `-p`** — it auto-detects the right UIA pattern (TextPattern → ValuePattern → TogglePattern → SelectionPattern → Name). Only use `-p PropertyName --value` when you need a specific property like `IsEnabled`
- **File pickers need `-w <HWND>`** — they run in a separate PickerHost process, so `-a PID` won't find them. Use `list-windows` to discover the picker HWND first
- **Flyouts need a short `Start-Sleep`** after triggering — the menu items appear in the tree asynchronously

