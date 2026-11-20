// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace Microsoft.AspNetCore.Razor.Language.IntegrationTests;

// The decl/impl markup split is opt-in (RazorCodeGenerationOptions.EnableMarkupSplit) because only a
// host that emits both halves -- the Razor source generator -- can consume it. A host that reads just
// the implementation document, such as the SDK's classic (non-source-generator) compilation, must keep
// getting the whole component as a single file. These tests pin that default-off behavior.
public class MarkupSplitterDisabledComponentTest : RazorIntegrationTestBase
{
    internal override RazorFileKind? FileKind => RazorFileKind.Component;

    internal override bool EnableMarkupSplit => false;

    [Fact]
    public void SplittableComponent_ProducesSingleDocumentWithFullClassBody()
    {
        // This component splits cleanly when the split is enabled: the markup-free member goes to the
        // decl half and the markup-bearing method lifts to the impl half. With the split off there must
        // be no decl half at all, and the single document must carry both members.
        var generated = CompileToCSharp("""
            @code {
                [Microsoft.AspNetCore.Components.Parameter] public int Count { get; set; }
                private Microsoft.AspNetCore.Components.RenderFragment Make() => @<p>Hi</p>;
            }
            """);

        Assert.Null(generated.DeclCode);
        Assert.Null(generated.CodeDocument.GetDeclCSharpDocument());

        Assert.Contains("Count", generated.Code);
        Assert.Contains("Make", generated.Code);

        CompileToAssembly(generated);
    }

    [Fact]
    public void MarkupFreeComponent_ProducesSingleDocument()
    {
        // A component with no class-body markup still splits when the split is enabled (whole body is
        // the decl). With the split off it stays a single document.
        var generated = CompileToCSharp("""
            <p>@Count</p>

            @code {
                [Microsoft.AspNetCore.Components.Parameter] public int Count { get; set; }
            }
            """);

        Assert.Null(generated.DeclCode);
        Assert.Contains("Count", generated.Code);

        CompileToAssembly(generated);
    }
}
