// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using System.Runtime.CompilerServices;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.AspNetCore.Razor.Language.IntegrationTests;

// Language features not covered by tests:
// - Params-collections: requires collection-builder plumbing that is not specific to Razor-generated source.
// - Partial properties: requires a multi-part partial type/member setup that is not a natural Razor-authored surface.
// - Collection expression better conversion from expression: semantic conversion refinement rather than a distinct Razor syntax surface.
public sealed class CSharp13LanguageFeaturesIntegrationTest_Legacy : IntegrationTestBase
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

    public CSharp13LanguageFeaturesIntegrationTest_Legacy()
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
    [WorkItem("https://github.com/dotnet/csharplang/blob/main/proposals/csharp-13.0/esc-escape-sequence.md")]
    public void EscapeCharacter()
    {
        var generated = CompileToCSharp("""
            @inherits global::LegacyTemplateBase
            
            @{
                var value = "\e[31m";
                _ = value.Length;
            }

            @("\e[31m")
            """,
            path: DefaultLegacyFileName);

        AssertDocumentNodeMatchesBaseline(generated.CodeDocument.GetRequiredDocumentNode());
        AssertCSharpDocumentMatchesBaseline(generated.CodeDocument.GetRequiredCSharpDocument());
        AssertCSharpDiagnosticsMatchBaseline(generated.CodeDocument);
        CompileToAssembly(generated);
    }

    [Fact]
    [WorkItem("https://github.com/dotnet/csharplang/blob/main/proposals/csharp-13.0/method-group-natural-type-improvements.md")]
    public void MethodGroupNaturalTypeImprovements()
    {
        var generated = CompileToCSharp("""
            @inherits global::LegacyTemplateBase
            
            @{
                int Increment(int value) => value + 1;
                var increment = Increment;
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
    [WorkItem("https://github.com/dotnet/csharplang/issues/7104")]
    public void LockObject()
    {
        AddCSharpSyntaxTree("""
            namespace System.Threading
            {
                public sealed class Lock
                {
                    public Scope EnterScope()
                        => new();
            
                    public readonly ref struct Scope
                    {
                        public void Dispose()
                        {
                        }
                    }
                }
            }
            """);

        var generated = CompileToCSharp("""
            @inherits global::LegacyTemplateBase
            
            @{
                var gate = new System.Threading.Lock();
                lock (gate)
                {
                    _ = gate;
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
    [WorkItem("https://github.com/dotnet/roslyn/pull/70649")]
    public void ImplicitIndexerAccessInObjectInitializers()
    {
        AddCSharpSyntaxTree("""
            public class IndexerBuffer
            {
                private readonly int[] _values = new int[4];
            
                public int this[int index]
                {
                    get => _values[index];
                    set => _values[index] = value;
                }
            }
            """);

        var generated = CompileToCSharp("""
            @inherits global::LegacyTemplateBase
            
            @{
                var buffer = new IndexerBuffer { [0] = 1, [1] = 2 };
                _ = buffer[0] + buffer[1];
            }
            """,
            path: DefaultLegacyFileName);

        AssertDocumentNodeMatchesBaseline(generated.CodeDocument.GetRequiredDocumentNode());
        AssertCSharpDocumentMatchesBaseline(generated.CodeDocument.GetRequiredCSharpDocument());
        AssertCSharpDiagnosticsMatchBaseline(generated.CodeDocument);
        CompileToAssembly(generated);
    }

    [Fact]
    [WorkItem("https://github.com/dotnet/csharplang/blob/main/proposals/csharp-13.0/ref-unsafe-in-iterators-async.md")]
    public void RefUnsafeInIteratorsAsync()
    {
        var generated = CompileToCSharp("""
            @inherits global::LegacyTemplateBase
            
            @functions {
                public static async System.Threading.Tasks.Task<int> ReadAsync(int[] values)
                {
                    ref var first = ref values[0];
                    var copy = first;
                    await System.Threading.Tasks.Task.Yield();
                    return copy;
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
    [WorkItem("https://github.com/dotnet/csharplang/blob/main/proposals/csharp-13.0/ref-struct-interfaces.md")]
    public void RefStructInterfacesAndAllowsRefStructConstraint()
    {
        var generated = CompileToCSharp("""
            @inherits global::LegacyTemplateBase
            
            @functions {
                public interface IValue<TSelf>
                    where TSelf : IValue<TSelf>, allows ref struct
                {
                }
            }
            """,
            path: DefaultLegacyFileName);

        AssertDocumentNodeMatchesBaseline(generated.CodeDocument.GetRequiredDocumentNode());
        AssertCSharpDocumentMatchesBaseline(generated.CodeDocument.GetRequiredCSharpDocument());
        AssertCSharpDiagnosticsMatchBaseline(generated.CodeDocument);
    }

    [Fact]
    [WorkItem("https://github.com/dotnet/csharplang/issues/7706")]
    public void OverloadResolutionPriority()
    {
        AddCSharpSyntaxTree("""
            namespace System.Runtime.CompilerServices
            {
                [System.AttributeUsage(System.AttributeTargets.All, Inherited = false)]
                public sealed class OverloadResolutionPriorityAttribute : System.Attribute
                {
                    public OverloadResolutionPriorityAttribute(int priority)
                    {
                    }
                }
            }
            
            public static class OverloadPriorityHelpers
            {
                [System.Runtime.CompilerServices.OverloadResolutionPriority(1)]
                public static string Pick(int value)
                    => "int";
            
                public static string Pick(object value)
                    => "object";
            }
            """);

        var generated = CompileToCSharp("""
            @inherits global::LegacyTemplateBase
            
            @{
                _ = OverloadPriorityHelpers.Pick(1);
            }
            """,
            path: DefaultLegacyFileName);

        AssertDocumentNodeMatchesBaseline(generated.CodeDocument.GetRequiredDocumentNode());
        AssertCSharpDocumentMatchesBaseline(generated.CodeDocument.GetRequiredCSharpDocument());
        AssertCSharpDiagnosticsMatchBaseline(generated.CodeDocument);
        CompileToAssembly(generated);
    }

}
