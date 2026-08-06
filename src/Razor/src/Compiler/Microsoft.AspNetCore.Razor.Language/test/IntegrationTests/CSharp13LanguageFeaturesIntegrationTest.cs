// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.CodeAnalysis;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.AspNetCore.Razor.Language.IntegrationTests;

// Language features not covered by tests:
// - Params-collections: requires collection-builder plumbing that is not specific to Razor-generated source.
// - Partial properties: requires a multi-part partial type/member setup that is not a natural Razor-authored surface.
// - Collection expression better conversion from expression: semantic conversion refinement rather than a distinct Razor syntax surface.

public sealed class CSharp13LanguageFeaturesIntegrationTest()
    : RazorBaselineIntegrationTestBase(layer: TestProject.Layer.Compiler)
{
    internal override RazorFileKind? FileKind => RazorFileKind.Component;

    internal override bool UseTwoPhaseCompilation => true;

    internal override string DefaultFileName => "TestComponent.razor";

    protected override string GetDirectoryPath(string testName)
        => $"TestFiles/IntegrationTests/{GetType().Name}/{testName}";

    [Fact]
    [WorkItem("https://github.com/dotnet/csharplang/blob/main/proposals/csharp-13.0/esc-escape-sequence.md")]
    public void EscapeCharacter()
    {
        var generated = CompileToCSharp("""
            @{
                var value = "\e[31m";
                _ = value.Length;
            }

            @("\e[31m")
            """);

        AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
        AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
        CompileToAssembly(generated);
    }

    [Fact]
    [WorkItem("https://github.com/dotnet/csharplang/blob/main/proposals/csharp-13.0/method-group-natural-type-improvements.md")]
    public void MethodGroupNaturalTypeImprovements()
    {
        var generated = CompileToCSharp("""
            @{
                int Increment(int value) => value + 1;
                var increment = Increment;
                _ = increment(1);
            }
            """);

        AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
        AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
        CompileToAssembly(generated);
    }

    [Fact]
    [WorkItem("https://github.com/dotnet/csharplang/issues/7104")]
    public void LockObject()
    {
        AdditionalSyntaxTrees.Add(Parse("""
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
            """));

        var generated = CompileToCSharp("""
            @{
                var gate = new System.Threading.Lock();
                lock (gate)
                {
                    _ = gate;
                }
            }
            """);

        AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
        AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
        CompileToAssembly(generated);
    }

    [Fact]
    [WorkItem("https://github.com/dotnet/roslyn/pull/70649")]
    public void ImplicitIndexerAccessInObjectInitializers()
    {
        AdditionalSyntaxTrees.Add(Parse("""
            public class IndexerBuffer
            {
                private readonly int[] _values = new int[4];
            
                public int this[int index]
                {
                    get => _values[index];
                    set => _values[index] = value;
                }
            }
            """));

        var generated = CompileToCSharp("""
            @{
                var buffer = new IndexerBuffer { [0] = 1, [1] = 2 };
                _ = buffer[0] + buffer[1];
            }
            """);

        AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
        AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
        CompileToAssembly(generated);
    }

    [Fact]
    [WorkItem("https://github.com/dotnet/csharplang/blob/main/proposals/csharp-13.0/ref-unsafe-in-iterators-async.md")]
    public void RefUnsafeInIteratorsAsync()
    {
        var generated = CompileToCSharp("""
            @code {
                public static async System.Threading.Tasks.Task<int> ReadAsync(int[] values)
                {
                    ref var first = ref values[0];
                    var copy = first;
                    await System.Threading.Tasks.Task.Yield();
                    return copy;
                }
            }
            """);

        AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
        AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
        CompileToAssembly(generated);
    }

    [Fact]
    [WorkItem("https://github.com/dotnet/csharplang/blob/main/proposals/csharp-13.0/ref-struct-interfaces.md")]
    public void RefStructInterfacesAndAllowsRefStructConstraint()
    {
        var generated = CompileToCSharp("""
            @code {
                public interface IValue<TSelf>
                    where TSelf : IValue<TSelf>, allows ref struct
                {
                }
            }
            """,
            Diagnostic(ErrorCode.ERR_RuntimeDoesNotSupportByRefLikeGenerics, "ref struct").WithLocation(3, 45));

        AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
        AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
        CompileToAssembly(generated,
            Diagnostic(ErrorCode.ERR_RuntimeDoesNotSupportByRefLikeGenerics, "ref struct").WithLocation(3, 45));
    }

    [Fact]
    [WorkItem("https://github.com/dotnet/csharplang/issues/7706")]
    public void OverloadResolutionPriority()
    {
        AdditionalSyntaxTrees.Add(Parse("""
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
            """));

        var generated = CompileToCSharp("""
            @{
                _ = OverloadPriorityHelpers.Pick(1);
            }
            """);

        AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
        AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
        CompileToAssembly(generated);
    }

}
