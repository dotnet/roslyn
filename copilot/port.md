
## Goal

Remove unnecessary .NET Framework / Windows restrictions from test methods so they run on .NET Core (net9.0) and non-Windows platforms.

## Condition Types

These test attributes are used to restrict tests to .NET Framework + Windows:

| Attribute | Meaning |
|-----------|---------|
| `[ConditionalFact(typeof(DesktopOnly))]` | Skips on .NET Core |
| `[ConditionalTheory(typeof(DesktopOnly))]` | Skips on .NET Core (parameterized) |
| `[ConditionalFact(typeof(WindowsOnly))]` | Skips on non-Windows |
| `[ConditionalTheory(typeof(WindowsOnly))]` | Skips on non-Windows (parameterized) |
| `[ConditionalFact(typeof(WindowsDesktopOnly))]` | Skips on non-Windows AND non-Desktop |
| `[ConditionalTheory(typeof(WindowsDesktopOnly))]` | Skips on non-Windows AND non-Desktop (parameterized) |

The name of the type passed into these attributes describes the behavior of the condition. For example, `DesktopOnly` means the test will only run on .NET Framework, and `WindowsOnly` means the test will only run on Windows.

## Porting Strategies

### NativePdbRequiresDesktop

Many tests are marked `WindowsOnly` with reason `NativePdbRequiresDesktop`. These tests use native PDB writing which only works on Windows with the native SymWriter COM component.

**How to port:** Check if the test actually *requires* native PDB format. If the test:
- Uses `EmitOptions` with `DebugInformationFormat.Pdb` → Try changing to `DebugInformationFormat.PortablePdb`
- Explicitly tests native PDB writer behavior (SymWriter errors, WinMdExp data) → **Cannot port, leave as-is**
- Tests general PDB content (sequence points, scopes, etc.) → Likely portable-PDB compatible

### NoPiaNeedsDesktop

Tests marked with `NoPiaNeedsDesktop` use COM interop / NoPIA embedding features.

**How to port:** These typically **cannot be ported**. NoPIA is a desktop-only feature. Mark these as "skipped - genuine restriction" in the actions log.

### Simple Desktop restriction with no specific reason

Tests with just `ConditionalFact(typeof(DesktopOnly))` or `ConditionalFact(typeof(WindowsOnly))` without a specific skip reason may have been conservatively restricted.

**How to port:** 
1. Read the test carefully
2. Check if it uses any desktop-specific APIs (AppDomain, COM, Registry, etc.)
3. If not, simply change the attribute to `[Fact]` or `[Theory]`
4. If it uses Windows paths, check if those are test-input strings vs actual filesystem access

### TestExecutionNeedsDesktopTypes

Tests that depend on types only available in .NET Framework.

**How to port:** Usually **cannot be ported**. Check what specific types are needed.

### Hardcoded Windows paths

Tests that use hardcoded Windows paths (e.g., `C:\temp\file.txt`) may fail on non-Windows platforms.

**How to port:** 
1. Refactor the base path to a local in the method 
2. Have the local change from a windows to a unix path based on `ExecutionConditionUtil.IsWindows` or similar
2. Update the paths in the test to use the local variable with `Path.Combine` instead of hardcoded separators

## Testing

After modifying tests, run them on .NET Core:

```bash
dotnet test src/Compilers/CSharp/Test/Emit2/Microsoft.CodeAnalysis.CSharp.Emit2.UnitTests.csproj -f net9.0 --filter "FullyQualifiedName~ClassName.MethodName" --nologo -v minimal
```

## Rules

1. **Never remove a restriction that is genuinely needed** — if a test truly requires native PDB or NoPIA, leave it
2. **Minimal changes only** — change the attribute, fix the test if needed, nothing more
3. **One class at a time** — port all methods in a class, test, commit
4. **If unsure, leave it** — mark as skipped with a note explaining why
5. **Re-read this file each iteration** — instructions may be updated between runs