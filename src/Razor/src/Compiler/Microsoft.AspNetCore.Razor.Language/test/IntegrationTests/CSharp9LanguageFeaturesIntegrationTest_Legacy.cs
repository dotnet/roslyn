// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using System.Runtime.CompilerServices;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.AspNetCore.Razor.Language.IntegrationTests;

// Language features not covered by tests:
// - Top-level statements: compilation-unit / executable-program feature that Razor does not author directly.

public sealed class CSharp9LanguageFeaturesIntegrationTest_Legacy : IntegrationTestBase
{
    private const string DefaultLegacyFileName = "TestView.cshtml";

    private const string LegacyTemplateBaseSource =
        """
        public abstract class LegacyTemplateBase
        {
            public virtual System.Threading.Tasks.Task ExecuteAsync()
                => System.Threading.Tasks.Task.CompletedTask;

            protected void WriteLiteral(string value)
            {
            }

            protected void Write(object value)
            {
            }
        }
        """;

    public CSharp9LanguageFeaturesIntegrationTest_Legacy()
        : base(layer: TestProject.Layer.Compiler)
    {
        AddCSharpSyntaxTree(LegacyTemplateBaseSource, filePath: "LegacyTemplateBase.cs");
    }

    public override string GetTestFileName([CallerMemberName] string? testName = null)
    {
        var fileName = $"TestFiles/IntegrationTests/{GetType().Name}/{testName}";
        var directory = Path.GetDirectoryName(fileName);
        if (directory is not null)
        {
            Directory.CreateDirectory(Path.Combine(TestProjectRoot, directory));
        }

        return fileName;
    }

    [Fact]
    [WorkItem("https://github.com/dotnet/csharplang/blob/main/proposals/csharp-9.0/target-typed-new.md")]
    public void TargetTypedNew()
    {
        var generated = CompileToCSharp("""
            @inherits global::LegacyTemplateBase
            
            @{
                System.Collections.Generic.List<int> values = new();
                _ = values.Count;
            }
            """,
            path: DefaultLegacyFileName);

        AssertDocumentNodeMatchesBaseline(generated.CodeDocument.GetRequiredDocumentNode());
        AssertCSharpDocumentMatchesBaseline(generated.CodeDocument.GetRequiredCSharpDocument());
        AssertCSharpDiagnosticsMatchBaseline(generated.CodeDocument);
        CompileToAssembly(generated);
    }

    [Fact]
    [WorkItem("https://github.com/dotnet/csharplang/issues/1738")]
    public void SkipLocalsInit()
    {
        BaseCompilation = BaseCompilation.WithOptions(BaseCompilation.Options.WithAllowUnsafe(true));

        var generated = CompileToCSharp("""
            @inherits global::LegacyTemplateBase
            
            @functions {
                [System.Runtime.CompilerServices.SkipLocalsInit]
                public static int GetValue()
                {
                    var value = 1;
                    return value;
                }
            }
            """,
            path: DefaultLegacyFileName);

        AssertDocumentNodeMatchesBaseline(generated.CodeDocument.GetRequiredDocumentNode());
        AssertCSharpDocumentMatchesBaseline(generated.CodeDocument.GetRequiredCSharpDocument());
        AssertCSharpDiagnosticsMatchBaseline(generated.CodeDocument);
        CompileToAssembly(generated);
    }

    [Fact]
    [WorkItem("https://github.com/dotnet/csharplang/blob/main/proposals/csharp-9.0/lambda-discard-parameters.md")]
    public void LambdaDiscardParameters()
    {
        var generated = CompileToCSharp("""
            @inherits global::LegacyTemplateBase
            
            @{
                System.Func<int, int, int> func = (_, _) => 42;
                _ = func(1, 2);
            }
            """,
            path: DefaultLegacyFileName);

        AssertDocumentNodeMatchesBaseline(generated.CodeDocument.GetRequiredDocumentNode());
        AssertCSharpDocumentMatchesBaseline(generated.CodeDocument.GetRequiredCSharpDocument());
        AssertCSharpDiagnosticsMatchBaseline(generated.CodeDocument);
        CompileToAssembly(generated);
    }

    [Fact]
    [WorkItem("https://github.com/dotnet/csharplang/blob/main/proposals/csharp-9.0/native-integers.md")]
    public void NativeInts()
    {
        var generated = CompileToCSharp("""
            @inherits global::LegacyTemplateBase
            
            @{
                nint value = 1;
                value += 2;
                _ = value;
            }
            """,
            path: DefaultLegacyFileName);

        AssertDocumentNodeMatchesBaseline(generated.CodeDocument.GetRequiredDocumentNode());
        AssertCSharpDocumentMatchesBaseline(generated.CodeDocument.GetRequiredCSharpDocument());
        AssertCSharpDiagnosticsMatchBaseline(generated.CodeDocument);
        CompileToAssembly(generated);
    }

    [Fact]
    [WorkItem("https://github.com/dotnet/csharplang/blob/main/proposals/csharp-9.0/local-function-attributes.md")]
    public void AttributesOnLocalFunctions()
    {
        var generated = CompileToCSharp("""
            @inherits global::LegacyTemplateBase
            
            @{
                [System.Diagnostics.DebuggerStepThrough]
                void Local()
                {
                }
                
                Local();
            }
            """,
            path: DefaultLegacyFileName);

        AssertDocumentNodeMatchesBaseline(generated.CodeDocument.GetRequiredDocumentNode());
        AssertCSharpDocumentMatchesBaseline(generated.CodeDocument.GetRequiredCSharpDocument());
        AssertCSharpDiagnosticsMatchBaseline(generated.CodeDocument);
        CompileToAssembly(generated);
    }

    [Fact]
    [WorkItem("https://github.com/dotnet/csharplang/issues/191")]
    public void FunctionPointers()
    {
        BaseCompilation = BaseCompilation.WithOptions(BaseCompilation.Options.WithAllowUnsafe(true));

        var generated = CompileToCSharp("""
            @inherits global::LegacyTemplateBase
            
            @functions {
                public static unsafe delegate* managed<int, int> Pointer => &Double;
                
                private static int Double(int value)
                    => value * 2;
            }
            """,
            path: DefaultLegacyFileName);

        AssertDocumentNodeMatchesBaseline(generated.CodeDocument.GetRequiredDocumentNode());
        AssertCSharpDocumentMatchesBaseline(generated.CodeDocument.GetRequiredCSharpDocument());
        AssertCSharpDiagnosticsMatchBaseline(generated.CodeDocument);
        CompileToAssembly(generated);
    }

    [Fact]
    [WorkItem("https://github.com/dotnet/csharplang/issues/2850")]
    public void PatternMatchingImprovements()
    {
        var generated = CompileToCSharp("""
            @inherits global::LegacyTemplateBase
            
            @{
                var value = 5;
                _ = value is > 0 and < 10;
                _ = value is < 0 or > 10;
            }

            @if (value is > 0 and < 10)
            {
                <p>Value is between 0 and 10.</p>
            }

            @if (value is < 0 or > 10)
            {
                <p>Value is outside the range of 0 to 10.</p>
            }
            """,
            path: DefaultLegacyFileName);

        AssertDocumentNodeMatchesBaseline(generated.CodeDocument.GetRequiredDocumentNode());
        AssertCSharpDocumentMatchesBaseline(generated.CodeDocument.GetRequiredCSharpDocument());
        AssertCSharpDiagnosticsMatchBaseline(generated.CodeDocument);
        CompileToAssembly(generated);
    }

    [Fact]
    [WorkItem("https://github.com/dotnet/csharplang/issues/275")]
    public void StaticLambdas()
    {
        var generated = CompileToCSharp("""
            @inherits global::LegacyTemplateBase
            
            @{
                System.Func<int, int> increment = static value => value + 1;
                _ = increment(1);
            }
            """,
            path: DefaultLegacyFileName);

        AssertDocumentNodeMatchesBaseline(generated.CodeDocument.GetRequiredDocumentNode());
        AssertCSharpDocumentMatchesBaseline(generated.CodeDocument.GetRequiredCSharpDocument());
        AssertCSharpDiagnosticsMatchBaseline(generated.CodeDocument);
        CompileToAssembly(generated);
    }

    [Fact]
    [WorkItem("https://github.com/dotnet/csharplang/blob/main/proposals/csharp-9.0/records.md")]
    public void Records()
    {
        var generated = CompileToCSharp("""
            @inherits global::LegacyTemplateBase
            
            @functions {
                public record Person(string Name);
            }
            """,
            path: DefaultLegacyFileName);

        AssertDocumentNodeMatchesBaseline(generated.CodeDocument.GetRequiredDocumentNode());
        AssertCSharpDocumentMatchesBaseline(generated.CodeDocument.GetRequiredCSharpDocument());
        AssertCSharpDiagnosticsMatchBaseline(generated.CodeDocument);
        CompileToAssembly(generated);
    }

    [Fact]
    [WorkItem("https://github.com/dotnet/csharplang/blob/main/proposals/csharp-9.0/target-typed-conditional-expression.md")]
    public void TargetTypedConditional()
    {
        var generated = CompileToCSharp("""
            @inherits global::LegacyTemplateBase
            
            @{
                var useArgumentException = true;
                System.Exception value = useArgumentException ? new System.ArgumentException() : new System.InvalidOperationException();
                _ = value.Message;
            }
            """,
            path: DefaultLegacyFileName);

        AssertDocumentNodeMatchesBaseline(generated.CodeDocument.GetRequiredDocumentNode());
        AssertCSharpDocumentMatchesBaseline(generated.CodeDocument.GetRequiredCSharpDocument());
        AssertCSharpDiagnosticsMatchBaseline(generated.CodeDocument);
        CompileToAssembly(generated);
    }

    [Fact]
    [WorkItem("https://github.com/dotnet/csharplang/blob/main/proposals/csharp-9.0/covariant-returns.md")]
    public void CovariantReturns()
    {
        var generated = CompileToCSharp("""
            @inherits global::LegacyTemplateBase
            
            @functions {
                public abstract class Base
                {
                    public abstract Base Clone();
                }
                
                public class Derived : Base
                {
                    public override Derived Clone()
                        => this;
                }
            }
            """,
            path: DefaultLegacyFileName);

        AssertDocumentNodeMatchesBaseline(generated.CodeDocument.GetRequiredDocumentNode());
        AssertCSharpDocumentMatchesBaseline(generated.CodeDocument.GetRequiredCSharpDocument());
        AssertCSharpDiagnosticsMatchBaseline(generated.CodeDocument);
        CompileToAssembly(generated);
    }

    [Fact]
    [WorkItem("https://github.com/dotnet/csharplang/issues/3194")]
    public void ExtensionGetEnumerator()
    {
        AddCSharpSyntaxTree("""
            public sealed class Numbers
            {
            }
            
            public struct NumberEnumerator
            {
                private int _index;
            
                public int Current => _index;
            
                public bool MoveNext()
                {
                    if (_index < 1)
                    {
                        _index++;
                        return true;
                    }
            
                    return false;
                }
            }
            
            public static class NumberExtensions
            {
                public static NumberEnumerator GetEnumerator(this Numbers numbers)
                    => new();
            }
            """);

        var generated = CompileToCSharp("""
            @inherits global::LegacyTemplateBase
            
            @{
                foreach (var value in new Numbers())
                {
                    _ = value;
                }
            }
            """,
            path: DefaultLegacyFileName);

        AssertDocumentNodeMatchesBaseline(generated.CodeDocument.GetRequiredDocumentNode());
        AssertCSharpDocumentMatchesBaseline(generated.CodeDocument.GetRequiredCSharpDocument());
        AssertCSharpDiagnosticsMatchBaseline(generated.CodeDocument);
        CompileToAssembly(generated);
    }

    [Fact]
    [WorkItem("https://github.com/dotnet/csharplang/blob/main/proposals/csharp-9.0/module-initializers.md")]
    public void ModuleInitializers()
    {
        var generated = CompileToCSharp("""
            @inherits global::LegacyTemplateBase
            
            @functions {
                [System.Runtime.CompilerServices.ModuleInitializer]
                public static void Initialize()
                {
                }
            }
            """,
            path: DefaultLegacyFileName);

        AssertDocumentNodeMatchesBaseline(generated.CodeDocument.GetRequiredDocumentNode());
        AssertCSharpDocumentMatchesBaseline(generated.CodeDocument.GetRequiredCSharpDocument());
        AssertCSharpDiagnosticsMatchBaseline(generated.CodeDocument);
        CompileToAssembly(generated);
    }

    [Fact]
    [WorkItem("https://github.com/dotnet/csharplang/blob/main/proposals/csharp-9.0/extending-partial-methods.md")]
    public void ExtendingPartial()
    {
        var generated = CompileToCSharp("""
            @inherits global::LegacyTemplateBase
            
            @functions {
                public partial class Greeter
                {
                    public partial string GetValue();
                }
                
                public partial class Greeter
                {
                    public partial string GetValue()
                        => "Razor";
                }
            }
            """,
            path: DefaultLegacyFileName);

        AssertDocumentNodeMatchesBaseline(generated.CodeDocument.GetRequiredDocumentNode());
        AssertCSharpDocumentMatchesBaseline(generated.CodeDocument.GetRequiredCSharpDocument());
        AssertCSharpDiagnosticsMatchBaseline(generated.CodeDocument);
        CompileToAssembly(generated);
    }

}

