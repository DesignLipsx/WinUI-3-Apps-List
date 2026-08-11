# Source Generator Patterns — Detailed Reference

Detailed code patterns for AOT compilation and source generators. See [SKILL.md](../SKILL.md) for rules summary.

---

## Trimming Configuration

```xml
<PropertyGroup>
  <PublishTrimmed>true</PublishTrimmed>
  <TrimMode>full</TrimMode>
  <SuppressTrimAnalysisWarnings>false</SuppressTrimAnalysisWarnings>
</PropertyGroup>
```

For reflection-heavy code, annotate to preserve members:

```csharp
public void LoadService([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] Type serviceType)
    => Activator.CreateInstance(serviceType);
```

---

## Trim Compatibility Testing

Keep warnings enabled during development:

```xml
<SuppressTrimAnalysisWarnings>false</SuppressTrimAnalysisWarnings>
<TrimmerSingleWarn>false</TrimmerSingleWarn>
```

Patterns to eliminate:

- `Type.GetType("MyNamespace.MyClass")` — use `typeof(T)` or `[DynamicDependency]`
- `Activator.CreateInstance(someType)` without `[DynamicallyAccessedMembers]`
- `Assembly.LoadFrom()` — use compile-time references
- Unattributed reflection: `typeof(T).GetProperties()`

---

## JSON Source Generator Setup

```csharp
[JsonSerializable(typeof(UserProfile))]
[JsonSerializable(typeof(List<UserProfile>))]
internal partial class AppJsonContext : JsonSerializerContext { }

// No reflection at runtime
var json = JsonSerializer.Serialize(profile, AppJsonContext.Default.UserProfile);
var obj = JsonSerializer.Deserialize(json, AppJsonContext.Default.UserProfile);
```

---

## Regex Source Generator

```csharp
public partial class InputValidator
{
    [GeneratedRegex(@"^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$")]
    private static partial Regex EmailRegex();
    public bool IsValidEmail(string input) => EmailRegex().IsMatch(input);
}
```

---

## XAML Compilation (x:Bind)

```xml
<!-- ✅ AOT-safe: compile-time generated -->
<TextBlock Text="{x:Bind ViewModel.Title, Mode=OneWay}" />

<!-- ❌ Reflection-based, not trim-safe -->
<TextBlock Text="{Binding Path=Title}" />
```

Set `x:DataType` on pages: `<Page x:DataType="viewmodels:MainViewModel">`

---

## CsWin32 Setup

Generates P/Invoke wrappers at compile time. Add `Microsoft.Windows.CsWin32` NuGet package. List needed APIs in `NativeMethods.txt`:

```
GetDpiForWindow
SetWindowPos
ShowWindow
```

---

## CommunityToolkit.Mvvm Source Generators

Use partial properties (CommunityToolkit.Mvvm 8.4+). The legacy field form emits **MVVMTK0045** in WinRT/WinUI projects.

```csharp
public partial class SettingsViewModel : ObservableObject
{
    [ObservableProperty] public partial string UserName { get; set; }
    [RelayCommand] private async Task SaveAsync() => await _service.SaveAsync(UserName);
}
```

---

## Single-File Publishing Configuration

```xml
<PropertyGroup>
  <SelfContained>true</SelfContained>
  <PublishSingleFile>true</PublishSingleFile>
  <EnableCompressionInSingleFile>true</EnableCompressionInSingleFile>
</PropertyGroup>
```

Primarily for unpackaged apps. Combine with `<PublishTrimmed>true</PublishTrimmed>`.
