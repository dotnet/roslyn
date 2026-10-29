// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using System.Runtime.CompilerServices;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.AspNetCore.Razor.Language.IntegrationTests;

// Language features not covered by tests:
// - Interceptors (experimental feature): requires dedicated feature switches and source-location plumbing outside a plain Razor document.

public sealed class CSharp12LanguageFeaturesIntegrationTest_Legacy : IntegrationTestBase
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

    public CSharp12LanguageFeaturesIntegrationTest_Legacy()
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
    [WorkItem("https://github.com/dotnet/csharplang/blob/main/proposals/csharp-12.0/ref-readonly-parameters.md")]
    public void RefReadonlyParameters()
    {
        var generated = CompileToCSharp("""
            @inherits global::LegacyTemplateBase
            
            @functions {
                public static int Sum(ref readonly int value)
                    => value;
            }
            """,
            path: DefaultLegacyFileName);

        AssertDocumentNodeMatchesBaseline(generated.CodeDocument.GetRequiredDocumentNode());
        AssertCSharpDocumentMatchesBaseline(generated.CodeDocument.GetRequiredImplCSharpDocument());
        AssertCSharpDiagnosticsMatchBaseline(generated.CodeDocument);
        CompileToAssembly(generated);
    }

    [Fact]
    [WorkItem("https://github.com/dotnet/csharplang/blob/main/proposals/csharp-12.0/collection-expressions.md")]
    public void CollectionExpressions()
    {
        var generated = CompileToCSharp("""
            @inherits global::LegacyTemplateBase
            
            @{
                int[] values = [1, 2, 3];
                _ = values.Length;
            }

            @((int[])[1, 2, 3])
            """,
            path: DefaultLegacyFileName);

        AssertDocumentNodeMatchesBaseline(generated.CodeDocument.GetRequiredDocumentNode());
        AssertCSharpDocumentMatchesBaseline(generated.CodeDocument.GetRequiredImplCSharpDocument());
        AssertCSharpDiagnosticsMatchBaseline(generated.CodeDocument);
        CompileToAssembly(generated);
    }

    [Fact]
    [WorkItem("https://github.com/dotnet/csharplang/blob/main/proposals/csharp-12.0/inline-arrays.md")]
    public void InlineArrays()
    {
        var generated = CompileToCSharp("""
            @inherits global::LegacyTemplateBase
            
            @functions {
                [System.Runtime.CompilerServices.InlineArray(4)]
                public struct Buffer
                {
                    private int _element0;
                }
            }
            """,
            path: DefaultLegacyFileName);

        AssertDocumentNodeMatchesBaseline(generated.CodeDocument.GetRequiredDocumentNode());
        AssertCSharpDocumentMatchesBaseline(generated.CodeDocument.GetRequiredImplCSharpDocument());
        AssertCSharpDiagnosticsMatchBaseline(generated.CodeDocument);
        CompileToAssembly(generated);
    }

    [Fact]
    [WorkItem("https://github.com/dotnet/csharplang/issues/4037")]
    public void NameofAccessingInstanceMembers()
    {
        var generated = CompileToCSharp("""
            @inherits global::LegacyTemplateBase
            
            @functions {
                public string Capture()
                    => nameof(ToString);
            }
            """,
            path: DefaultLegacyFileName);

        AssertDocumentNodeMatchesBaseline(generated.CodeDocument.GetRequiredDocumentNode());
        AssertCSharpDocumentMatchesBaseline(generated.CodeDocument.GetRequiredImplCSharpDocument());
        AssertCSharpDiagnosticsMatchBaseline(generated.CodeDocument);
        CompileToAssembly(generated);
    }

    [Fact]
    [WorkItem("https://github.com/dotnet/csharplang/issues/4284")]
    public void UsingAliasesForAnyType()
    {
        var generated = CompileToCSharp("""
            @inherits global::LegacyTemplateBase
            
            @using NumberPair = (int Left, int Right)
            
            @{
                NumberPair pair = (1, 2);
                _ = pair.Left + pair.Right;
            }
            """,
            path: DefaultLegacyFileName);

        AssertDocumentNodeMatchesBaseline(generated.CodeDocument.GetRequiredDocumentNode());
        AssertCSharpDocumentMatchesBaseline(generated.CodeDocument.GetRequiredImplCSharpDocument());
        AssertCSharpDiagnosticsMatchBaseline(generated.CodeDocument);
        CompileToAssembly(generated);
    }

    [Fact]
    [WorkItem("https://github.com/dotnet/csharplang/blob/main/proposals/csharp-12.0/primary-constructors.md")]
    public void PrimaryConstructors()
    {
        var generated = CompileToCSharp("""
            @inherits global::LegacyTemplateBase
            
            @functions {
                public class Person(string name)
                {
                    public string Name => name;
                }
            }
            """,
            path: DefaultLegacyFileName);

        AssertDocumentNodeMatchesBaseline(generated.CodeDocument.GetRequiredDocumentNode());
        AssertCSharpDocumentMatchesBaseline(generated.CodeDocument.GetRequiredImplCSharpDocument());
        AssertCSharpDiagnosticsMatchBaseline(generated.CodeDocument);
        CompileToAssembly(generated);
    }

    [Fact]
    [WorkItem("https://github.com/dotnet/csharplang/blob/main/proposals/csharp-12.0/lambda-method-group-defaults.md")]
    public void LambdaOptionalParameters()
    {
        var generated = CompileToCSharp("""
            @inherits global::LegacyTemplateBase
            
            @{
                var formatter = (int value = 1) => value + 1;
                _ = formatter();
            }
            """,
            path: DefaultLegacyFileName);

        AssertDocumentNodeMatchesBaseline(generated.CodeDocument.GetRequiredDocumentNode());
        AssertCSharpDocumentMatchesBaseline(generated.CodeDocument.GetRequiredImplCSharpDocument());
        AssertCSharpDiagnosticsMatchBaseline(generated.CodeDocument);
        CompileToAssembly(generated);
    }

    [Fact]
    [WorkItem("https://github.com/dotnet/csharplang/blob/main/proposals/csharp-12.0/experimental-attribute.md")]
    public void ExperimentalAttribute()
    {
        var generated = CompileToCSharp("""
            @inherits global::LegacyTemplateBase
            
            @functions {
                [System.Diagnostics.CodeAnalysis.Experimental("RAZOR0001")]
                public class ExperimentalType
                {
                }
            }
            """,
            path: DefaultLegacyFileName);

        AssertDocumentNodeMatchesBaseline(generated.CodeDocument.GetRequiredDocumentNode());
        AssertCSharpDocumentMatchesBaseline(generated.CodeDocument.GetRequiredImplCSharpDocument());
        AssertCSharpDiagnosticsMatchBaseline(generated.CodeDocument);
        CompileToAssembly(generated);
    }

}


