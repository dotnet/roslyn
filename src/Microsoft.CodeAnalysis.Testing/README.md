# Microsoft.CodeAnalysis.Testing

## Installation

Separate packages are provided for each language and test framework.

### Azure Packages feed for prerelease packages

To reference prerelease packages, add a **NuGet.Config** file to your solution directory containing the following:

```xml
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <packageSources>
    <add key="dotnet-tools" value="https://pkgs.dev.azure.com/dnceng/public/_packaging/dotnet-tools/nuget/v3/index.json" />
  </packageSources>
</configuration>
```

### Packages

* C#
  * Microsoft.CodeAnalysis.CSharp.Analyzer.Testing
  * Microsoft.CodeAnalysis.CSharp.CodeFix.Testing
  * Microsoft.CodeAnalysis.CSharp.CodeRefactoring.Testing
  * Microsoft.CodeAnalysis.CSharp.SourceGenerators.Testing
* Visual Basic
  * Microsoft.CodeAnalysis.VisualBasic.Analyzer.Testing
  * Microsoft.CodeAnalysis.VisualBasic.CodeFix.Testing
  * Microsoft.CodeAnalysis.VisualBasic.CodeRefactoring.Testing
  * Microsoft.CodeAnalysis.VisualBasic.SourceGenerators.Testing

### Obsolete Packages

This collection of packages previously included a number of packages for testing with specific test frameworks, such as
MSTest, NUnit, and xUnit. These packages have been marked obsolete (see [issue #1175](https://github.com/dotnet/roslyn-sdk/issues/1175)),
and users are encouraged to migrate to the generic test packages. The migration process shown here is specific to MSTest;
other frameworks can substitute `NUnit` or `XUnit` as appropriate.

* Remove the **.MSTest** suffix from all referenced packages. For example:

    Microsoft.CodeAnalysis.CSharp.Analyzer.Testing.MSTest &rarr; Microsoft.CodeAnalysis.CSharp.Analyzer.Testing

* Remove the **.MSTest** suffix from namespaces. For example:

    ```patch
    -using Microsoft.CodeAnalysis.CSharp.Testing.MSTest;
    +using Microsoft.CodeAnalysis.CSharp.Testing;
    ```

* Use `CSharpAnalyzerVerifier<TAnalyzer, DefaultVerifier>` instead of `AnalyzerVerifier<TAnalyzer>`. A similar change
  should be applied for each of the other types impacted by this change (add the language prefix, and add the
  `DefaultVerifier` generic argument).

## Verifier overview

Testing analyzers and code fixes starts with the selection of a *verifier* helper type. A default analyzer and code fix
verifier types is defined for each language:

* `CSharpAnalyzerVerifier<TAnalyzer, TVerifier>` / `VisualBasicAnalyzerVerifier<TAnalyzer, TVerifier>`
* `CSharpCodeFixVerifier<TAnalyzer, TCodeFix, TVerifier>` / `VisualBasicCodeFixVerifier<TAnalyzer, TCodeFix, TVerifier>`

The verifier types provide limited functionality intended to serve the majority of analyzer and code fix tests:

1. Creating a `DiagnosticResult` representing a diagnostic reported by an analyzer
2. Executing a "basic use case" analyzer test
3. Executing a "basic use case" code fix test

Each of the verifier helper types is supported by a *test* helper type, which provides the primary implementation of
each test scenario:

* `CSharpAnalyzerTest<TAnalyzer, TVerifier>` / `VisualBasicAnalyzerTest<TAnalyzer, TVerifier>`
* `CSharpCodeFixTest<TAnalyzer, TCodeFix, TVerifier>` / `VisualBasicCodeFixTest<TAnalyzer, TCodeFix, TVerifier>`

### Imports

This document is written on the assumption that users will alias a verifier or code fix type to the name `Verify` within
the context of a test class.

```csharp
using Verify = Microsoft.CodeAnalysis.CSharp.Testing.CSharpAnalyzerVerifier<
    SomeAnalyzerType,
    Microsoft.CodeAnalysis.Testing.DefaultVerifier>;
```

Users writing tests involving compiler errors may also want to import the static members of the `DiagnosticResult` type,
which provides the helper method `CompilerError` and `CompilerWarning`.

```csharp
using static Microsoft.CodeAnalysis.Testing.DiagnosticResult;
```

## Best practices

### Use code fix testing when possible

`VerifyCodeFixAsync` automatically performs several tests related to code fix scenarios:

1. Verifies that the analyzer reports an expected set of diagnostics
2. Verifies that the code fix result does not contain fixable diagnostics
3. Verifies that the code fix applied to one diagnostic at a time produces the expected output
4. Verifies that Fix All operations, when supported by the code fix provider, produce the expected output

With this library, separation of analyzer and code fix tests increases complexity and code duplication, and tends to
decrease the overall confidence in the test suite. For analyzers that provide a code fix, it is preferable to only test
with the code fix verifier for test cases where diagnostics are reported in the input source. For test cases where no
diagnostic is reported in the input source, `VerifyAnalyzerAsync` remains appropriate.

## Examples

### Basic use cases

Basic use cases are supported by static helper methods in the verifier helper types. These helpers set the following
properties:

* `TestCode`: the single input source file
* `ExpectedDiagnostics`: the diagnostics expected to appear in the input source file
* `FixedCode`: (code fix tests only) the single output source file produced by applying a code fix to the input
* Other properties are set to their default values

#### Analyzer with no diagnostics

```csharp
string testCode = @"original source code";
await Verify.VerifyAnalyzerAsync(testCode);
```

#### Analyzer without a code fix

```csharp
string testCode = @"original source code";
var expected = Verify.Diagnostic().WithLocation(1, 7);
await Verify.VerifyAnalyzerAsync(testCode, expected);
```

#### Analyzer with a code fix

```csharp
string testCode = @"original source code";
string fixedCode = @"fixed source code";
var expected = Verify.Diagnostic().WithLocation(1, 7);
await Verify.VerifyCodeFixAsync(testCode, expected, fixedCode);
```

For cases where the code fix does not always offer a fix for a diagnostic, the `fixedCode` may be set to the `testCode`
to verify that no fix is offered for the scenario:

```csharp
string testCode = @"original source code";
var expected = Verify.Diagnostic().WithLocation(1, 7);

// Verifies that the expected diagnostics are reported, but no code fix is offered
await Verify.VerifyCodeFixAsync(testCode, expected, testCode);
```

#### Compiler errors

```csharp
string testCode = @"void Method() { return }";
var expected = DiagnosticResult.CompilerError("CS0246").WithLocation(1, 23).WithMessage("; expected");
await Verify.VerifyAnalyzerAsync(testCode, expected);
```

### Advanced use cases

Advanced use cases involve the instantiation of a test helper, setting the appropriate properties, and finally calling
`RunAsync` to run the test. These steps can be combined using the object initializer syntax:

```csharp
await new CSharpAnalyzerTest<SomeAnalyzerType, DefaultVerifier>
{
    // Configure test by setting property values here...
}.RunAsync();
```

#### Additional files

```csharp
await new CSharpAnalyzerTest<SomeAnalyzerType, DefaultVerifier>
{
    TestState =
    {
        Sources = { @"original source code" },
        ExpectedDiagnostics = { Verify.Diagnostic().WithLocation(1, 7) },
        AdditionalFiles =
        {
            ("File1.ext", "content1"),
            ("File2.ext", "content2"),
        },
    },
}.RunAsync();
```

#### Overriding the default verifier behavior

💡 If the same set of steps needs to be performed for each test in a test suite, consider creating a customized set of
verifier and test types. For example, [Microsoft/vs-threading](https://github.com/Microsoft/vs-threading) uses specific
Additional Files and metadata references in most of its tests, so it uses custom verifier and test types to allow the
use of "basic use cases" for test scenarios that would otherwise be considered advanced.

To create a custom verifier, add your test setup or configuration to the corresponding `Test` class provided by the
[analyzer template](../VisualStudio.Roslyn.SDK/Roslyn.SDK/ProjectTemplates/CSharp/Diagnostic/Test/Verifiers). See
https://learn.microsoft.com/en-us/dotnet/csharp/roslyn-sdk/tutorials/how-to-write-csharp-analyzer-code-fix for
instructions on using the analyzer template.
