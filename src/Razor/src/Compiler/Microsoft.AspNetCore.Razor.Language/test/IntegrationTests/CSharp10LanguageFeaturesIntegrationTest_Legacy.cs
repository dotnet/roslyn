// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using System.Runtime.CompilerServices;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.AspNetCore.Razor.Language.IntegrationTests;

// Language features not covered by tests:
// - Improved Definite Assignment: flow-analysis improvement without a distinct Razor syntax surface.
// - Source Generator V2 APIs: Roslyn API surface, not Razor-authored source.
// - Async method builder override: requires substantial task-like / builder plumbing that is not a Razor-specific surface.
// - Enhanced `#line` directive: Razor already owns emitted line directives; this sweep focuses on end-user Razor-authored source.
// - Interpolated string improvements: mostly handler plumbing / overload-shape interaction rather than a Razor-specific surface.

public sealed class CSharp10LanguageFeaturesIntegrationTest_Legacy : IntegrationTestBase
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

    public CSharp10LanguageFeaturesIntegrationTest_Legacy()
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
    [WorkItem("https://github.com/dotnet/csharplang/blob/main/proposals/csharp-10.0/record-structs.md")]
    public void RecordStructs()
    {
        var generated = CompileToCSharp("""
            @inherits global::LegacyTemplateBase
            
            @functions {
                public readonly record struct Point(int X, int Y);
            }
            """,
            path: DefaultLegacyFileName);

        AssertDocumentNodeMatchesBaseline(generated.CodeDocument.GetRequiredDocumentNode());
        AssertCSharpDocumentMatchesBaseline(generated.CodeDocument.GetRequiredCSharpDocument());
        AssertCSharpDiagnosticsMatchBaseline(generated.CodeDocument);
        CompileToAssembly(generated);
    }

    [Fact]
    [WorkItem("https://github.com/dotnet/csharplang/blob/main/proposals/csharp-10.0/constant_interpolated_strings.md")]
    public void ConstantInterpolatedStrings()
    {
        var generated = CompileToCSharp("""
            @inherits global::LegacyTemplateBase
            
            @functions {
                private const string Name = "Razor";
                private const string Value = $"{Name} Tests";
            }

            @($"{Name} Tests")
            """,
            path: DefaultLegacyFileName);

        AssertDocumentNodeMatchesBaseline(generated.CodeDocument.GetRequiredDocumentNode());
        AssertCSharpDocumentMatchesBaseline(generated.CodeDocument.GetRequiredCSharpDocument());
        AssertCSharpDiagnosticsMatchBaseline(generated.CodeDocument);
        CompileToAssembly(generated);
    }

    [Fact]
    [WorkItem("https://github.com/dotnet/csharplang/blob/main/proposals/csharp-10.0/extended-property-patterns.md")]
    public void ExtendedPropertyPatterns()
    {
        var generated = CompileToCSharp("""
            @inherits global::LegacyTemplateBase
            
            @{
                var value = new { A = new { B = 1 } };
                _ = value is { A.B: 1 };
            }

            @if (value is { A.B: 1 })
            {
                <p>Matched</p>
            }
            """,
            path: DefaultLegacyFileName);

        AssertDocumentNodeMatchesBaseline(generated.CodeDocument.GetRequiredDocumentNode());
        AssertCSharpDocumentMatchesBaseline(generated.CodeDocument.GetRequiredCSharpDocument());
        AssertCSharpDiagnosticsMatchBaseline(generated.CodeDocument);
        CompileToAssembly(generated);
    }

    [Fact]
    [WorkItem("https://github.com/dotnet/csharplang/issues/4174")]
    public void SealedRecordToString()
    {
        var generated = CompileToCSharp("""
            @inherits global::LegacyTemplateBase
            
            @functions {
                public sealed record Person(string Name);
                
                public static string Format()
                    => new Person("Razor").ToString();
            }
            """,
            path: DefaultLegacyFileName);

        AssertDocumentNodeMatchesBaseline(generated.CodeDocument.GetRequiredDocumentNode());
        AssertCSharpDocumentMatchesBaseline(generated.CodeDocument.GetRequiredCSharpDocument());
        AssertCSharpDiagnosticsMatchBaseline(generated.CodeDocument);
        CompileToAssembly(generated);
    }

    [Fact]
    [WorkItem("https://github.com/dotnet/csharplang/issues/125")]
    public void MixDeclarationsAndVariablesInDeconstruction()
    {
        var generated = CompileToCSharp("""
            @inherits global::LegacyTemplateBase
            
            @{
                var tuple = (1, 2);
                int second = 0;
                (int first, second) = tuple;
                _ = first + second;
            }
            """,
            path: DefaultLegacyFileName);

        AssertDocumentNodeMatchesBaseline(generated.CodeDocument.GetRequiredDocumentNode());
        AssertCSharpDocumentMatchesBaseline(generated.CodeDocument.GetRequiredCSharpDocument());
        AssertCSharpDiagnosticsMatchBaseline(generated.CodeDocument);
        CompileToAssembly(generated);
    }

    [Fact]
    [WorkItem("https://github.com/dotnet/csharplang/blob/main/proposals/csharp-10.0/lambda-improvements.md")]
    public void LambdaImprovements()
    {
        var generated = CompileToCSharp("""
            @inherits global::LegacyTemplateBase
            
            @{
                var increment = int (int value) => value + 1;
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
    [WorkItem("https://github.com/dotnet/csharplang/blob/main/proposals/csharp-11.0/static-abstracts-in-interfaces.md")]
    public void StaticAbstractMembersInInterfaces()
    {
        var generated = CompileToCSharp("""
            @inherits global::LegacyTemplateBase
            
            @functions {
                public interface IValue<TSelf>
                    where TSelf : IValue<TSelf>
                {
                    static abstract TSelf Zero { get; }
                }
                
                public readonly struct Value : IValue<Value>
                {
                    public static Value Zero => new();
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
    [WorkItem("https://github.com/dotnet/csharplang/blob/main/proposals/csharp-10.0/file-scoped-namespaces.md")]
    public void FileScopedNamespace()
    {
        AddCSharpSyntaxTree("""
            namespace Helpers;
            
            public static class Utility
            {
                public static string GetValue()
                    => "Razor";
            }
            """);

        var generated = CompileToCSharp("""
            @inherits global::LegacyTemplateBase
            
            @{
                _ = Helpers.Utility.GetValue();
            }
            """,
            path: DefaultLegacyFileName);

        AssertDocumentNodeMatchesBaseline(generated.CodeDocument.GetRequiredDocumentNode());
        AssertCSharpDocumentMatchesBaseline(generated.CodeDocument.GetRequiredCSharpDocument());
        AssertCSharpDiagnosticsMatchBaseline(generated.CodeDocument);
        CompileToAssembly(generated);
    }

    [Fact]
    [WorkItem("https://github.com/dotnet/csharplang/blob/main/proposals/csharp-10.0/GlobalUsingDirective.md")]
    public void GlobalUsingDirective()
    {
        AddCSharpSyntaxTree("""
            global using Helpers.GlobalUsing;
            
            namespace Helpers.GlobalUsing
            {
                public static class Utility
                {
                    public static string GetValue()
                        => "Razor";
                }
            }
            """);

        var generated = CompileToCSharp("""
            @inherits global::LegacyTemplateBase
            
            <p>@Utility.GetValue()</p>
            """,
            path: DefaultLegacyFileName);
        AssertDocumentNodeMatchesBaseline(generated.CodeDocument.GetRequiredDocumentNode());
        AssertCSharpDocumentMatchesBaseline(generated.CodeDocument.GetRequiredCSharpDocument());
        AssertCSharpDiagnosticsMatchBaseline(generated.CodeDocument);
        CompileToAssemblyUsingAppSyntaxTrees(generated);
    }

    [Fact]
    [WorkItem("https://github.com/dotnet/csharplang/blob/main/proposals/csharp-10.0/parameterless-struct-constructors.md")]
    public void ParameterlessStructConstructors()
    {
        var generated = CompileToCSharp("""
            @inherits global::LegacyTemplateBase
            
            @functions {
                public struct Counter
                {
                    public int Value { get; }
                
                    public Counter()
                    {
                        Value = 1;
                    }
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
    [WorkItem("https://github.com/dotnet/csharplang/blob/main/proposals/csharp-10.0/caller-argument-expression.md")]
    public void CallerExpressionAttribute()
    {
        NullableEnable = true;
        BaseCompilation = BaseCompilation.WithOptions(BaseCompilation.Options.WithNullableContextOptions(NullableContextOptions.Enable));

        var generated = CompileToCSharp("""
            @inherits global::LegacyTemplateBase
            
            @functions {
                public static string Capture(
                    bool condition,
                    [System.Runtime.CompilerServices.CallerArgumentExpression(nameof(condition))] string? expression = null)
                    => expression ?? string.Empty;
            }

            <div>@Capture(1 + 1 == 2)</div>
            """,
            path: DefaultLegacyFileName);

        AssertDocumentNodeMatchesBaseline(generated.CodeDocument.GetRequiredDocumentNode());
        AssertCSharpDocumentMatchesBaseline(generated.CodeDocument.GetRequiredCSharpDocument());
        AssertCSharpDiagnosticsMatchBaseline(generated.CodeDocument);
        CompileToAssembly(generated);
    }

    private void CompileToAssemblyUsingAppSyntaxTrees(CompiledCSharpCode code)
    {
        var generatedCode = code.CodeDocument.GetRequiredCSharpDocument();
        var generatedSyntaxTree = (CSharpSyntaxTree)CSharpSyntaxTree.ParseText(
            generatedCode.Text,
            CSharpParseOptions,
            path: code.CodeDocument.Source.FilePath ?? string.Empty);

        BaseCompilation
            .AddSyntaxTrees(CSharpSyntaxTrees)
            .AddSyntaxTrees(generatedSyntaxTree)
            .EmitToImageReference();
    }

}
