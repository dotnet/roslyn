# Dummy ref-assembly pairs

This folder contains small, hand-authored before/after pairs used to validate the ref-assembly analyzer on known changes.

## Structure

Each test case lives in its own subfolder, for example:

- `rename-public-class\`
- `add-local-function\`

Inside each test-case folder:

- `*.before.cs` is the source for the "before" assembly
- `*.after.cs` is the source for the "after" assembly
- `*.before.dll` is the produced reference assembly for the before source
- `*.after.dll` is the produced reference assembly for the after source

## Source format

The source files are file-based C# apps rather than project-based builds. They use modern C# file directives such as:

- `#:sdk Microsoft.NET.Sdk`
- `#:property TargetFramework=net8.0`
- `#:property ProduceReferenceAssembly=true`
- `#:property AssemblyName=...`

The `AssemblyName` property should be the same in the before and after source so the pair differs only in the intended change.

## Producing the ref assemblies

From a test-case folder, build each source file directly with `dotnet build`:

```powershell
dotnet build .\rename-public-class.before.cs -nologo -v:minimal
dotnet build .\rename-public-class.after.cs -nologo -v:minimal
```

The file-based build writes intermediate outputs under a temporary `dotnet\runfile\...` folder. The reference assembly is taken from:

- `obj\debug\ref\<AssemblyName>.dll`

and then copied next to the source as:

- `*.before.dll`
- `*.after.dll`

## Running the analyzer

Run the analyzer on this folder:

```powershell
Set-Location Q:\repos\roslyn2\src\Tools\RefAssembliesAnalysis
dotnet run .\Program.cs -- ".\dummy-assemblies"
```

The analyzer writes its report under:

- `Q:\repos\roslyn2\src\Tools\RefAssembliesAnalysis\dummy-assemblies\output\`

That output contains:

- `pair-results.json`
- `summary.json`
- `summary.txt`

## Current test cases

### `rename-public-class`

Validates a simple public API rename:

- before: `public class BeforeTypeRename { }`
- after: `public class AfterTypeRename { }`

The expected analyzer result is a `valid-pair`, with the rename showing up under the `other-public` bucket.

### `add-local-function`

Validates adding a local function to an existing public method body:

- before: `public int M() => 1;`
- after: `public int M()` with a local function `static int Local() => 1;`

In this simple case, the expected analyzer result is `same-mvid`, which confirms that adding a local function inside a method body does not by itself change the produced reference assembly.

### `add-local-function-with-closure`

Validates adding a local function that captures a parameter from the enclosing public method:

- before: `public int M(int value) => value + 1;`
- after: `public int M(int value)` with a local function `int Local() => value + 1;`

This case is intended to show whether introducing a closure-backed local function changes the produced ref assembly.
The expected analyzer result is a `valid-pair` classified under `display-class`, since the captured local state requires a synthesized closure type.

### `private-type-public-property`

Validates that a member with `public` metadata inside a private type is still treated as a non-public change:

- before: `private class Hidden { public int Value { get; } = 1; }`
- after: `private class Hidden { public int Count { get; } = 1; }`

The expected analyzer result is a `valid-pair`. The property should stay out of the `other-public` bucket because its containing type is private, and it should be classified under `user-authored-other`.

### `add-private-method`

Validates adding a user-authored private method to an otherwise unchanged public type:

- before: `public int M() => 1;`
- after: adds `private void PrivateMethod()`

The expected analyzer result is a `valid-pair` classified under `user-authored-other`.

### `add-internal-method-ivt`

Validates adding an `internal` member in an assembly that explicitly grants friend access:

- before: `public int M() => 1;`
- after: adds `internal void InternalHelper()`

The expected analyzer result is a `valid-pair` classified under `user-authored-ivt`.

### `struct-with-private-field`

Validates adding a private field to a public struct:

- before: `public struct S { public int Value; }`
- after: adds `private int _cachedHash;`

The expected analyzer result is a `valid-pair` classified under `user-authored-other`.
