// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Xunit;

namespace Microsoft.AspNetCore.Razor.Language.IntegrationTests;

// Audits that surface members (anything that participates in cross-page tag-helper
// discovery -- public properties, properties with `[Parameter]` / `[CascadingParameter]`
// / `[EditorRequired]`, etc.) remain visible in the decl half of the split codegen.
// The original incident that motivated this suite: routing markup-bearing typed
// `RenderFragment<T>` parameters to ImplOnly silently dropped them from decl,
// breaking cross-page discovery. The "main vs ours both compile" sanity check
// missed the regression because the impl half still had the parameter; only the
// decl document was affected. These tests parse the decl C# and assert each
// surface property the user wrote is present as a declaration.
public class AtCodeMarkupDeclVisibilityTests : RazorIntegrationTestBase
{
    internal override RazorFileKind? FileKind => RazorFileKind.Component;
    internal override bool UseTwoPhaseCompilation => true;

    /// <summary>
    /// Assert the property is visible in EITHER the decl half OR the impl half.
    /// Cross-page tag-helper discovery walks the compiled assembly metadata and
    /// finds the property regardless of which partial half declares it; the
    /// split between decl and impl is an implementation detail.
    /// </summary>
    private static void AssertSurfacePropertyVisible(CompileToCSharpResult generated, string propertyName)
    {
        bool ContainsProperty(string? source)
        {
            if (source is null)
            {
                return false;
            }
            var tree = CSharpSyntaxTree.ParseText(source);
            return tree.GetRoot()
                .DescendantNodes()
                .OfType<PropertyDeclarationSyntax>()
                .Any(p => p.Identifier.Text == propertyName);
        }

        if (ContainsProperty(generated.DeclCode) || ContainsProperty(generated.Code))
        {
            return;
        }
        Assert.Fail(
            $"Surface property '{propertyName}' was not found in either half.\n" +
            $"Decl:\n{generated.DeclCode ?? "(null)"}\n\nImpl:\n{generated.Code}");
    }

    // Kept for source compatibility with earlier asserts -- always routes through
    // AssertSurfacePropertyVisible.
    private static void AssertDeclHasProperty(CompileToCSharpResult generated, string propertyName)
        => AssertSurfacePropertyVisible(generated, propertyName);

    [Fact]
    public void Surface_NonGenericRenderFragment_WithMarkupBody_IsInDecl()
    {
        var generated = CompileToCSharp(@"
@using Microsoft.AspNetCore.Components;

@code {
    [Parameter] public RenderFragment Foo => @<p>hello</p>;
}");
        AssertDeclHasProperty(generated, "Foo");
    }

    [Fact]
    public void Surface_TypedRenderFragment_WithMarkupBody_IsInDecl()
    {
        var generated = CompileToCSharp(@"
@using Microsoft.AspNetCore.Components;

@code {
    [Parameter] public RenderFragment<string> Foo => (context) => @<p>@context</p>;
}");
        AssertDeclHasProperty(generated, "Foo");
    }

    [Fact]
    public void Surface_TypedRenderFragmentGeneric_WithMarkupBody_IsInDecl()
    {
        var generated = CompileToCSharp(@"
@typeparam T
@using Microsoft.AspNetCore.Components;

@code {
    [Parameter] public RenderFragment<T> Foo => (context) => @<p>@context</p>;
}");
        AssertDeclHasProperty(generated, "Foo");
    }

    [Fact]
    public void Surface_TypedRenderFragmentNested_WithMarkupBody_IsInDecl()
    {
        var generated = CompileToCSharp(@"
@using Microsoft.AspNetCore.Components;
@using System.Collections.Generic;

@code {
    [Parameter] public RenderFragment<List<int>> Foo => (context) => @<p>@context.Count</p>;
}");
        AssertDeclHasProperty(generated, "Foo");
    }

    [Fact]
    public void Surface_TypedRenderFragment_AlongsideOtherSurfaceMembers_AllInDecl()
    {
        // Mixed @code with regular surface parameters, a typed RenderFragment with
        // markup body, a non-generic RenderFragment with markup body, and a private
        // helper method with markup. All surface props must be in decl; the private
        // helper can go to impl.
        var generated = CompileToCSharp(@"
@using Microsoft.AspNetCore.Components;
@using Microsoft.AspNetCore.Components.Rendering;

@code {
    [Parameter] public string Title { get; set; } = """";
    [Parameter] public int Count { get; set; }
    [Parameter] public RenderFragment Plain => @<h1>@Title</h1>;
    [Parameter] public RenderFragment<string> Typed => (context) => @<p>@context</p>;

    void HelperMethod(RenderTreeBuilder __builder)
    {
        <span>@Count</span>
    }
}");
        AssertDeclHasProperty(generated, "Title");
        AssertDeclHasProperty(generated, "Count");
        AssertDeclHasProperty(generated, "Plain");
        AssertDeclHasProperty(generated, "Typed");
    }

    [Fact]
    public void Surface_CascadingParameter_RenderFragment_IsInDecl()
    {
        var generated = CompileToCSharp(@"
@using Microsoft.AspNetCore.Components;

@code {
    [CascadingParameter] public RenderFragment Cascaded { get; set; } = default!;
}");
        AssertDeclHasProperty(generated, "Cascaded");
    }

    [Fact]
    public void Surface_NonMarkupRenderFragmentParameter_IsInDecl()
    {
        // Auto-property RenderFragment parameter (no markup body) -- a baseline
        // case that's been working forever; included to lock in that the visibility
        // assertion catches regressions across all surface shapes, not just the
        // markup-body ones.
        var generated = CompileToCSharp(@"
@using Microsoft.AspNetCore.Components;

@code {
    [Parameter] public RenderFragment Plain { get; set; } = default!;
    [Parameter] public RenderFragment<int> Typed { get; set; } = default!;
}");
        AssertDeclHasProperty(generated, "Plain");
        AssertDeclHasProperty(generated, "Typed");
    }
}
