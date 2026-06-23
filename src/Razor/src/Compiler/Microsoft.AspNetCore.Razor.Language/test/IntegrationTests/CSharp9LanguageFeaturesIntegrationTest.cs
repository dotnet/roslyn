// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.AspNetCore.Razor.Language.IntegrationTests;

// Language features not covered by tests:
// - Top-level statements: compilation-unit / executable-program feature that Razor does not author directly.

public sealed class CSharp9LanguageFeaturesIntegrationTest()
    : RazorBaselineIntegrationTestBase(layer: TestProject.Layer.Compiler)
{
    internal override RazorFileKind? FileKind => RazorFileKind.Component;

    internal override bool UseTwoPhaseCompilation => true;

    internal override string DefaultFileName => "TestComponent.razor";

    protected override string GetDirectoryPath(string testName)
        => $"TestFiles/IntegrationTests/{GetType().Name}/{testName}";

    [Fact]
    [WorkItem("https://github.com/dotnet/csharplang/blob/main/proposals/csharp-9.0/target-typed-new.md")]
    public void TargetTypedNew()
    {
        var generated = CompileToCSharp("""
            @{
                System.Collections.Generic.List<int> values = new();
                _ = values.Count;
            }
            """);

        AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
        AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
        CompileToAssembly(generated);
    }

    [Fact]
    [WorkItem("https://github.com/dotnet/csharplang/issues/1738")]
    public void SkipLocalsInit()
    {
        var baseCompilation = BaseCompilation;
        baseCompilation = baseCompilation.WithOptions(baseCompilation.Options.WithAllowUnsafe(true));

        var generated = CompileToCSharp("""
            @code {
                [System.Runtime.CompilerServices.SkipLocalsInit]
                public static int GetValue()
                {
                    var value = 1;
                    return value;
                }
            }
            """,
            baseCompilation: baseCompilation);

        AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
        AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
        CompileToAssembly(generated);
    }

    [Fact]
    [WorkItem("https://github.com/dotnet/csharplang/blob/main/proposals/csharp-9.0/lambda-discard-parameters.md")]
    public void LambdaDiscardParameters()
    {
        var generated = CompileToCSharp("""
            @{
                System.Func<int, int, int> func = (_, _) => 42;
                _ = func(1, 2);
            }
            """);

        AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
        AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
        CompileToAssembly(generated);
    }

    [Fact]
    [WorkItem("https://github.com/dotnet/csharplang/blob/main/proposals/csharp-9.0/native-integers.md")]
    public void NativeInts()
    {
        var generated = CompileToCSharp("""
            @{
                nint value = 1;
                value += 2;
                _ = value;
            }
            """);

        AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
        AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
        CompileToAssembly(generated);
    }

    [Fact]
    [WorkItem("https://github.com/dotnet/csharplang/blob/main/proposals/csharp-9.0/local-function-attributes.md")]
    public void AttributesOnLocalFunctions()
    {
        var generated = CompileToCSharp("""
            @{
                [System.Diagnostics.DebuggerStepThrough]
                void Local()
                {
                }
                
                Local();
            }
            """);

        AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
        AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
        CompileToAssembly(generated);
    }

    [Fact]
    [WorkItem("https://github.com/dotnet/csharplang/issues/191")]
    public void FunctionPointers()
    {
        var baseCompilation = BaseCompilation;
        baseCompilation = baseCompilation.WithOptions(baseCompilation.Options.WithAllowUnsafe(true));

        var generated = CompileToCSharp("""
            @code {
                public static unsafe delegate* managed<int, int> Pointer => &Double;
                
                private static int Double(int value)
                    => value * 2;
            }
            """,
            baseCompilation: baseCompilation);

        AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
        AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
        CompileToAssembly(generated);
    }

    [Fact]
    [WorkItem("https://github.com/dotnet/csharplang/issues/2850")]
    public void PatternMatchingImprovements()
    {
        var generated = CompileToCSharp("""
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
            """);

        AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
        AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
        CompileToAssembly(generated);
    }

    [Fact]
    [WorkItem("https://github.com/dotnet/csharplang/issues/275")]
    public void StaticLambdas()
    {
        var generated = CompileToCSharp("""
            @{
                System.Func<int, int> increment = static value => value + 1;
                _ = increment(1);
            }
            """);

        AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
        AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
        CompileToAssembly(generated);
    }

    [Fact]
    [WorkItem("https://github.com/dotnet/csharplang/blob/main/proposals/csharp-9.0/records.md")]
    public void Records()
    {
        var generated = CompileToCSharp("""
            @code {
                public record Person(string Name);
            }
            """);

        AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
        AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
        CompileToAssembly(generated);
    }

    [Fact]
    [WorkItem("https://github.com/dotnet/csharplang/blob/main/proposals/csharp-9.0/target-typed-conditional-expression.md")]
    public void TargetTypedConditional()
    {
        var generated = CompileToCSharp("""
            @{
                var useArgumentException = true;
                System.Exception value = useArgumentException ? new System.ArgumentException() : new System.InvalidOperationException();
                _ = value.Message;
            }
            """);

        AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
        AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
        CompileToAssembly(generated);
    }

    [Fact]
    [WorkItem("https://github.com/dotnet/csharplang/blob/main/proposals/csharp-9.0/covariant-returns.md")]
    public void CovariantReturns()
    {
        var generated = CompileToCSharp("""
            @code {
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
            """);

        AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
        AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
        CompileToAssembly(generated);
    }

    [Fact]
    [WorkItem("https://github.com/dotnet/csharplang/issues/3194")]
    public void ExtensionGetEnumerator()
    {
        AdditionalSyntaxTrees.Add(Parse("""
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
            """));

        var generated = CompileToCSharp("""
            @{
                foreach (var value in new Numbers())
                {
                    _ = value;
                }
            }
            """);

        AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
        AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
        CompileToAssembly(generated);
    }

    [Fact]
    [WorkItem("https://github.com/dotnet/csharplang/blob/main/proposals/csharp-9.0/module-initializers.md")]
    public void ModuleInitializers()
    {
        var generated = CompileToCSharp("""
            @code {
                [System.Runtime.CompilerServices.ModuleInitializer]
                public static void Initialize()
                {
                }
            }
            """);

        AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
        AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
        CompileToAssembly(generated);
    }

    [Fact]
    [WorkItem("https://github.com/dotnet/csharplang/blob/main/proposals/csharp-9.0/extending-partial-methods.md")]
    public void ExtendingPartial()
    {
        var generated = CompileToCSharp("""
            @code {
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
            """);

        AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
        AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
        CompileToAssembly(generated);
    }

}

