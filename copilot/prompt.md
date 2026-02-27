# Test Porting Loop Prompt

You are porting .NET Framework-only tests to run on .NET Core in the Roslyn repository.

## Setup — do this EVERY iteration

1. **Re-read instructions**: Read `copilot/port.md` for the latest porting instructions
2. **Read the list**: Read `copilot/list.md` to find the next unchecked (`- [ ]`) class
3. If all classes are checked, stop and report completion

## For each class — do this ONE class per iteration

### Step 1: Analyze

Read the test file. For each method with a conditional attribute (`ConditionalFact`/`ConditionalTheory` with `DesktopOnly`, `WindowsOnly`, `WindowsDesktopOnly`, `DesktopClrOnly`, or `ClrOnly`):
- Identify the skip reason
- Determine if the restriction can be removed based on the porting strategies in `port.md`

### Step 2: Port

For each method that CAN be ported:
- Change `[ConditionalFact(typeof(XxxOnly))]` → `[Fact]`
- Change `[ConditionalTheory(typeof(XxxOnly))]` → `[Theory]`
- If the test used `DebugInformationFormat.Pdb`, consider changing to `DebugInformationFormat.PortablePdb`
- Make any other minimal changes needed for the test to work on .NET Core

For each method that CANNOT be ported:
- Leave the conditional attribute as-is
- Note why in the actions log

### Step 3: Build

```bash
dotnet build src/Compilers/CSharp/Test/Emit2/Microsoft.CodeAnalysis.CSharp.Emit2.UnitTests.csproj -f net9.0 --nologo -v minimal
```

If the build fails, fix the issue and rebuild.

### Step 4: Test

Run only the modified tests on .NET Core:

```bash
dotnet test src/Compilers/CSharp/Test/Emit2/Microsoft.CodeAnalysis.CSharp.Emit2.UnitTests.csproj -f net9.0 --filter "FullyQualifiedName~ClassName" --nologo -v minimal
```

Replace `ClassName` with the actual class name (e.g., `CheckSumTest`, `PDBTests`).

If tests fail:
- Analyze the failure
- Fix if possible with minimal changes
- If the test genuinely needs desktop, revert the change and note it

### Step 5: Log actions

Append to `copilot/actions.md`:
```
### ClassName (filename)
- MethodName: [ported/skipped] — reason
```

### Step 6: Update list

In `copilot/list.md`, change `- [ ]` to `- [x]` for the completed class.

### Step 7: Format

Run formatting on any modified .cs files:
```bash
dotnet format whitespace --folder . --include path/to/modified/file.cs
```

### Step 8: Commit

```bash
git add -A
git commit -m "Port ClassName tests to .NET Core

Remove unnecessary .NET Framework restrictions from test methods
in ClassName so they run on net9.0.

Co-authored-by: Copilot <223556219+Copilot@users.noreply.github.com>"
```

## Then loop back to Setup for the next class
