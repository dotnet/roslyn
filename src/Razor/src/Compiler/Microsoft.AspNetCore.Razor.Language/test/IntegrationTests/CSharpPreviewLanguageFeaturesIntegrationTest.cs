// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.AspNetCore.Razor.Language.IntegrationTests;

// Language features not covered by tests:
// - Unsafe evolution: broad ongoing unsafe-surface work without a single stable Razor-focused scenario here.
// - ExtendedLayoutAttribute: metadata/runtime interop feature rather than Razor-authored source.
// - Runtime Async: runtime/library work rather than a distinct Razor-authored source feature.

public sealed class CSharpPreviewLanguageFeaturesIntegrationTest()
    : RazorBaselineIntegrationTestBase(layer: TestProject.Layer.Compiler)
{
    internal override RazorFileKind? FileKind => RazorFileKind.Component;

    internal override bool UseTwoPhaseCompilation => true;

    internal override string DefaultFileName => "TestComponent.razor";

    protected override string GetDirectoryPath(string testName)
        => $"TestFiles/IntegrationTests/{GetType().Name}/{testName}";

    [Fact]
    [WorkItem("https://github.com/dotnet/razor/issues/13188")]
    public void StringLiteralAttributeOnUnionParameter()
    {
        AdditionalSyntaxTrees.Add(Parse("""
            #nullable enable

            namespace System.Runtime.CompilerServices
            {
                public interface IUnion
                {
                    object? Value { get; }
                }

                public class UnionAttribute : System.Attribute
                {
                }
            }
            """));

        AdditionalSyntaxTrees.Add(Parse("""
            using Microsoft.AspNetCore.Components;

            namespace Test
            {
                public union SlotContent(string, MarkupString, RenderFragment);

                public class Slot : ComponentBase
                {
                    [Parameter]
                    public SlotContent Content { get; set; }
                }
            }
            """));

        var generated = CompileToCSharp("""
            @{
                var content = new Test.SlotContent(new MarkupString("<strong>hello</strong>"));
            }

            <Slot Content="hello" />
            <Slot Content="@content" />
            """);

        AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
        AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
        CompileToAssembly(generated);
    }

    [Fact]
    [WorkItem("https://github.com/dotnet/csharplang/blob/main/proposals/collection-expression-arguments.md")]
    public void CollectionExpressionArguments()
    {
        var generated = CompileToCSharp("""
            @{
                System.Collections.Generic.List<string> values = [with(capacity: 32), "a", "b", "c"];
                _ = values.Count;
            }

            <p>@(CountValues([with(capacity: 32), "d", "e"]))</p>
            <p>@CountValues([with(capacity: 32), "f", "g"])</p>

            @code {
                private static int CountValues(System.Collections.Generic.List<string> values)
                    => values.Count;
            }
            """);

        AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
        AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
        CompileToAssembly(generated);
    }

}
