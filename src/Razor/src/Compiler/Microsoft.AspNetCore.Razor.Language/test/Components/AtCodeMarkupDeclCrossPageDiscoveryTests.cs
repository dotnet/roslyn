// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.AspNetCore.Razor.Test.Common;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Emit;
using Xunit;

namespace Microsoft.AspNetCore.Razor.Language.IntegrationTests;

// When a component's decl document is null (or missing the parameter), other pages
// can't see the component's parameters via cross-page tag-helper discovery. The
// source generator uses GetDeclCSharpDocument() to feed pre-compilation source
// output (see RazorSourceGenerator.cs:114-135); a null decl means consuming pages
// fail to compile any reference to the component, because the consumer's tag-
// helper resolution pass walks the in-progress compilation and never sees a type
// for the producer component.
//
// These tests simulate the build's two-phase order: the producer's decl half is
// generated with NO tag helpers visible (BuildProducerDeclReference ->
// GenerateDeclWithoutDiscovery, an empty reference list), because the decl is the
// INPUT to cross-page discovery and so cannot itself depend on discovery having
// run. The impl half is generated in the full (post-discovery) pass. Both halves
// are compiled to an assembly the consumer's project engine sees as a metadata
// reference -- exactly how the source generator's RegisterPreCompilationSourceOutput
// emits the decl text into the compilation tag-helper discovery walks. Generating
// the decl through the full pipeline instead would resolve components against the
// current compilation and mask whether the decl is genuinely discovery-independent.
public class AtCodeMarkupDeclCrossPageDiscoveryTests : RazorIntegrationTestBase
{
    internal override RazorFileKind? FileKind => RazorFileKind.Component;

    /// <summary>
    /// Compile the producer through the FULL razor pipeline, emit its decl half
    /// as a standalone assembly, and return the assembly as a metadata
    /// reference. This is the test-side equivalent of what the source generator
    /// does for cross-page tag-helper discovery: the consumer's project engine
    /// must be created with the producer's decl-assembly in its reference list
    /// for the resolver to see the producer's type.
    /// </summary>
    private MetadataReference? BuildProducerDeclReference(string producerSource, string producerComponentName)
    {
        // Generate the decl half the way the real build does: in a declaration pass
        // with NO tag helpers visible, so it cannot resolve any component reference --
        // cross-page discovery hasn't run yet, and the decl is its INPUT. Generating
        // the decl through the full pipeline (CompileToCSharp) instead would resolve
        // components against the current compilation, masking whether the decl is
        // genuinely discovery-independent.
        var declCode = GenerateDeclWithoutDiscovery(producerSource, producerComponentName);

        if (declCode is null)
        {
            // Mirrors the SG's filter: producers with a null decl contribute
            // nothing to the pre-compilation compilation that tag-helper
            // discovery walks.
            return null;
        }

        // The impl half is generated in the full (post-discovery) pass, with tag
        // helpers resolved -- matching the real build's second phase.
        var implCode = CompileToCSharp(
            cshtmlRelativePath: $"/{producerComponentName}.cshtml",
            cshtmlContent: producerSource).Code;

        // We compile DECL + IMPL together as a metadata-only assembly. This
        // matches how the SG eventually feeds both halves into the user's
        // compilation: decl provides the API surface visible to cross-page
        // tag-helper discovery, impl provides the runtime bodies (including
        // any extended-partial-method implementations the decl declares).
        // Decl alone might not compile if the synth uses extended partial
        // methods (C# 9+) -- those require an implementation, which the impl
        // half provides.
        var declTree = CSharpSyntaxTree.ParseText(
            declCode,
            CSharpParseOptions,
            path: $"/{producerComponentName}.cshtml.g.decl.cs");
        var implTree = CSharpSyntaxTree.ParseText(
            implCode,
            CSharpParseOptions,
            path: $"/{producerComponentName}.cshtml.g.cs");

        var declCompilation = CSharpCompilation.Create(
            $"{producerComponentName}_decl",
            new[] { declTree, implTree },
            ReferenceUtil.AspNetLatestAll,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        using var peStream = new MemoryStream();
        var emitResult = declCompilation.Emit(peStream, options: new EmitOptions(metadataOnly: true));
        if (!emitResult.Success)
        {
            // Producer's decl text didn't compile cleanly on its own. Treat as
            // a missing decl for the consumer's reference list -- same outcome
            // as a null decl in the SG's filter.
            // Debug: surface diagnostics so failing tests can see WHY emit failed.
            var diags = string.Join("\n", emitResult.Diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error).Select(d => d.ToString()));
            if (!string.IsNullOrEmpty(diags))
            {
                throw new InvalidOperationException($"Producer decl+impl failed to emit:\n{diags}\n---DECL:\n{declCode}\n---IMPL:\n{implCode}");
            }
            return null;
        }
        return MetadataReference.CreateFromImage(peStream.ToArray());
    }

    /// <summary>
    /// Generate the producer's decl half with NO tag helpers visible -- the project
    /// engine is built with an empty reference list, so tag-helper discovery finds
    /// nothing and component references in any markup the decl emits cannot resolve.
    /// This reproduces the build's declaration pass, which runs before cross-page
    /// discovery. The decl half is what discovery consumes, so it must be valid
    /// without discovery having run.
    /// </summary>
    private string? GenerateDeclWithoutDiscovery(string producerSource, string producerComponentName)
    {
        var projectEngine = CreateProjectEngine(
            Configuration,
            Array.Empty<MetadataReference>(),
            supportLocalizedComponentNames: false,
            csharpParseOptions: null);
        var projectItem = CreateProjectItem(
            $"/{producerComponentName}.cshtml",
            producerSource,
            FileKind,
            cssScope: null);
        var codeDocument = projectEngine.Process(projectItem);
        return codeDocument.GetDeclCSharpDocument()?.Text.ToString();
    }

    /// <summary>
    /// The main (NeedsHelper) path defers a surface RenderFragment property's markup
    /// body to the impl half, leaving only a stub in decl. So a component referenced in
    /// that markup never appears in the decl -- the decl can't mis-lower it, which is
    /// exactly why the decl is safe to generate before discovery. Asserted against the
    /// pre-discovery decl (no tag helpers visible).
    /// </summary>
    [Fact]
    public void PreDiscoveryDecl_NeedsHelperMarkup_DefersComponentToImpl()
    {
        var declCode = GenerateDeclWithoutDiscovery(@"
@using Microsoft.AspNetCore.Components;

@code {
    [Parameter] public RenderFragment Body => @<ChildThing>hi</ChildThing>;
}", "Producer");

        Assert.NotNull(declCode);
        // The surface property is present (discoverable) and delegates to a synth stub.
        Assert.Contains("RenderFragment Body", declCode);
        Assert.Contains("__razor_synth", declCode);
        // The component markup is NOT in the decl -- it lives in the impl synth method,
        // so the pre-discovery decl never tries to resolve ChildThing.
        Assert.DoesNotContain("ChildThing", declCode);
    }

    /// <summary>
    /// A set/init accessor body with markup routes through the helper synth (a
    /// `partial void Synth(value)` method), so the markup body moves to the impl half
    /// just like the getter case -- only a delegating setter stays in decl. A component
    /// in that markup therefore never appears in the pre-discovery decl, so it can't be
    /// mis-lowered before discovery. (Before the setter synth existed these shapes fell
    /// back to DeclOnly and left the component unresolved in decl.)
    /// </summary>
    [Fact]
    public void PreDiscoveryDecl_SetterMarkup_DefersComponentToImpl()
    {
        var declCode = GenerateDeclWithoutDiscovery(@"
@using Microsoft.AspNetCore.Components;

@code {
    private RenderFragment _f;
    [Parameter] public RenderFragment Body { get => _f; set => _f = (@<ChildThing>hi</ChildThing>); }
}", "Producer");

        Assert.NotNull(declCode);
        // The property is present (discoverable) with a delegating setter.
        Assert.Contains("RenderFragment Body", declCode);
        Assert.Contains("__razor_synth", declCode);
        // The component markup is NOT in the decl -- it moved to the impl synth method,
        // so the pre-discovery decl never tries to resolve ChildThing.
        Assert.DoesNotContain("ChildThing", declCode);
    }

    /// <summary>
    /// A property carrying markup in BOTH accessors mints one synth per markup-bearing
    /// accessor -- a getter synth (<c>partial RF Synth()</c>) and a setter synth
    /// (<c>partial void Synth(value)</c>) -- so every markup body moves to impl and the
    /// decl keeps only the delegating property. Neither component in those markups
    /// appears in the pre-discovery decl, so neither can be mis-lowered before discovery.
    /// </summary>
    [Fact]
    public void PreDiscoveryDecl_MultiAccessorMarkup_DefersBothToImpl()
    {
        var declCode = GenerateDeclWithoutDiscovery(@"
@using Microsoft.AspNetCore.Components;

@code {
    private RenderFragment _f;
    [Parameter] public RenderFragment Body
    {
        get => @<ChildGet>hi</ChildGet>;
        set => _f = (@<ChildSet>bye</ChildSet>);
    }
}", "Producer");

        Assert.NotNull(declCode);
        // The property is present (discoverable) with both accessors delegating to synths.
        Assert.Contains("RenderFragment Body", declCode);
        // Two distinct synths: the getter body synth and the setter body synth.
        Assert.Contains("__razor_synth_BodyBody", declCode);
        Assert.Contains("__razor_synth_BodySet", declCode);
        // Neither component markup is in the decl -- both moved to impl synth methods, so
        // the pre-discovery decl never tries to resolve either ChildGet or ChildSet.
        Assert.DoesNotContain("ChildGet", declCode);
        Assert.DoesNotContain("ChildSet", declCode);
    }

    /// <summary>
    /// A string literal containing <c>//</c> earlier in a markup-bearing expression must
    /// not be mistaken for a line comment when the splitter decides the markup's syntactic
    /// slot. If it were, the shim would mis-parse and the whole component would fall back to
    /// un-split DeclOnly -- leaving the component markup in the pre-discovery decl. This
    /// asserts the split still applies: the property delegates to a synth and the component
    /// is deferred to impl.
    /// </summary>
    [Fact]
    public void PreDiscoveryDecl_SlashesInStringBeforeMarkup_StillSplits()
    {
        var declCode = GenerateDeclWithoutDiscovery(@"
@using Microsoft.AspNetCore.Components;

@code {
    private string Tag => null;
    [Parameter] public RenderFragment Body => Tag == ""a//b"" ? @<ChildThing>hi</ChildThing> : @<span>x</span>;
}", "Producer");

        Assert.NotNull(declCode);
        Assert.Contains("RenderFragment Body", declCode);
        // The split applied: the body delegates to a synth (it was not left verbatim in decl).
        Assert.Contains("__razor_synth", declCode);
        // The component markup moved to impl, so the pre-discovery decl never resolves it
        // and the `//` in the string never disabled the split.
        Assert.DoesNotContain("ChildThing", declCode);
    }

    /// <summary>
    /// C# 14 <c>field</c> keyword: a property with markup in BOTH a bodied accessor (that
    /// does not itself read <c>field</c>) AND a markup initializer. Each markup site gets
    /// its own synth and both bodies move to impl; the decl keeps a delegating property with
    /// no leaked placeholder, and the two halves compile cleanly.
    /// </summary>
    [Fact]
    public void FieldKeyword_MarkupInAccessorAndInitializer_DefersBothToImpl()
    {
        var producerSource = @"
@using Microsoft.AspNetCore.Components;

@code {
    [Parameter] public RenderFragment Foo { get => @<ChildA>g</ChildA>; set => field = value; } = @<ChildB>i</ChildB>;
}";
        var producerResult = CompileToCSharp(
            cshtmlRelativePath: "/ChildComp.cshtml",
            cshtmlContent: producerSource);

        var decl = producerResult.DeclCode ?? "";
        // No raw markup placeholder leaks into decl, and the property delegates to synths.
        Assert.DoesNotContain("__razor_markup_", decl);
        Assert.Contains("__razor_synth", decl);
        // Both markup bodies land in impl.
        Assert.Contains("ChildA", producerResult.Code);
        Assert.Contains("ChildB", producerResult.Code);
        // The decl + impl halves compile together with no errors.
        CompileToAssembly(producerResult, diagnostics =>
            Assert.DoesNotContain(diagnostics, d => d.Severity == DiagnosticSeverity.Error));
    }

    private CSharpCompilation BuildBaseCompilationWithProducerRef(string producerSource, string producerComponentName)
    {
        var producerRef = BuildProducerDeclReference(producerSource, producerComponentName);
        if (producerRef is null)
        {
            return DefaultBaseCompilation;
        }
        return DefaultBaseCompilation.AddReferences(producerRef);
    }

    /// <summary>
    /// Producer with a markup-bearing auto-property + initializer (the shape that
    /// the splitter can't extract via the helper-delegation synth because C#
    /// forbids calling instance methods from field initializers). Consumer
    /// references the producer by name. If the producer's decl is null, the
    /// consumer's tag-helper resolver doesn't find a type for &lt;ChildComp&gt;
    /// and falls back to treating it as plain HTML -- the consumer's compiled
    /// output emits OpenElement("ChildComp") instead of
    /// OpenComponent&lt;ChildComp&gt;. This test asserts the latter, which fails
    /// when the producer's decl is missing.
    /// </summary>
    [Fact]
    public void Consumer_CanReference_ProducerWithMarkupAutoPropertyInitializer()
    {
        var producerSource = @"
@using Microsoft.AspNetCore.Components;

@code {
    [Parameter] public RenderFragment ChildContent { get; set; } = @<p>default</p>;
}";
        var baseCompilation = BuildBaseCompilationWithProducerRef(producerSource, "ChildComp");

        var consumerResult = CompileToCSharp(
            cshtmlContent: @"<ChildComp />",
            baseCompilation: baseCompilation);

        var producerResult = CompileToCSharp(
            cshtmlRelativePath: "/ChildComp.cshtml",
            cshtmlContent: producerSource);
        Assert.True(consumerResult.Code.Contains("OpenComponent"),
            $"OpenComponent missing. ProducerDecl:\n{producerResult.DeclCode}\n---ProducerImpl:\n{producerResult.Code}\n---Consumer:\n{consumerResult.Code}");
        Assert.DoesNotContain("OpenElement(0, \"ChildComp\")", consumerResult.Code);
    }

    [Fact]
    public void Consumer_CanReference_ProducerWithTypedRenderFragmentAutoPropertyInitializer()
    {
        var producerSource = @"
@using Microsoft.AspNetCore.Components;

@code {
    [Parameter] public RenderFragment<string> ChildContent { get; set; } = (context) => @<p>@context</p>;
}";
        var baseCompilation = BuildBaseCompilationWithProducerRef(producerSource, "ChildComp");

        var consumerResult = CompileToCSharp(
            cshtmlContent: @"<ChildComp />",
            baseCompilation: baseCompilation);

        Assert.Contains("OpenComponent", consumerResult.Code);
        Assert.DoesNotContain("OpenElement(0, \"ChildComp\")", consumerResult.Code);
    }

    /// <summary>
    /// Sanity check: a producer with NO markup body (just an auto-property)
    /// always emits a non-null decl. Used as a baseline -- if THIS test fails,
    /// the test harness itself is broken, not the production codegen.
    /// </summary>
    [Fact]
    public void Consumer_CanReference_ProducerWithSimpleAutoProperty_SanityCheck()
    {
        var producerSource = @"
@using Microsoft.AspNetCore.Components;

@code {
    [Parameter] public string Title { get; set; } = """";
}";
        var baseCompilation = BuildBaseCompilationWithProducerRef(producerSource, "ChildComp");

        var consumerResult = CompileToCSharp(
            cshtmlContent: @"<ChildComp />",
            baseCompilation: baseCompilation);

        Assert.Contains("OpenComponent", consumerResult.Code);
    }

    /// <summary>
    /// Adversarial: producer's @code contains a preprocessor directive (#if DEBUG
    /// ... #endif). Asserts both that consumer discovery works AND that the
    /// producer's ChildContent emits the &lt;p&gt; wrapping correctly (the markup
    /// body in decl must produce an OpenElement("p") call or AddMarkupContent
    /// literal, not be dropped entirely).
    /// </summary>
    [Fact]
    public void Consumer_CanReference_ProducerWithPreprocessorDirectiveInCode()
    {
        var producerSource = @"
@using Microsoft.AspNetCore.Components;

@code {
#if DEBUG
    private int _debugCounter;
#endif
    [Parameter] public RenderFragment ChildContent { get; set; } = @<p>default</p>;
}";
        var baseCompilation = BuildBaseCompilationWithProducerRef(producerSource, "ChildComp");

        var consumerResult = CompileToCSharp(
            cshtmlContent: @"<ChildComp />",
            baseCompilation: baseCompilation);

        Assert.Contains("OpenComponent", consumerResult.Code);
        Assert.DoesNotContain("OpenElement(0, \"ChildComp\")", consumerResult.Code);

        // Verify markup body survives: Razor emits the `<p>` wrapping either as
        // element-tree calls (`OpenElement(N, "p")`) or as a single
        // `AddMarkupContent(N, "<p>...")` literal when the markup is pure HTML.
        // The regression we're guarding against is the UnresolvedElement-drop
        // bug, where the wrapping `<p>` disappears entirely.
        var producerResult = CompileToCSharp(
            cshtmlRelativePath: "/ChildComp.cshtml",
            cshtmlContent: producerSource);
        var combined = (producerResult.DeclCode ?? "") + "\n" + producerResult.Code;
        Assert.True(
            combined.Contains("OpenElement") || combined.Contains("<p>default</p>"),
            $"Markup body lost from producer. Decl: {producerResult.DeclCode}\n---\nImpl: {producerResult.Code}");
    }

    /// <summary>
    /// Adversarial: producer's parameter type is a `using` alias for
    /// RenderFragment. The splitter's <c>TryGetRenderFragmentShape</c> resolves
    /// <c>@using ALIAS = TYPE</c> directives via the document's
    /// <c>UsingDirectiveIntermediateNode</c> list so aliased forms route through
    /// the helper-synth too. Asserts consumer discovery AND that the markup body
    /// survives in the emitted output.
    /// </summary>
    [Fact]
    public void Consumer_CanReference_ProducerWithAliasedRenderFragment()
    {
        var producerSource = @"
@using Microsoft.AspNetCore.Components;
@using RF = Microsoft.AspNetCore.Components.RenderFragment;

@code {
    [Parameter] public RF ChildContent { get; set; } = @<p>default</p>;
}";
        var baseCompilation = BuildBaseCompilationWithProducerRef(producerSource, "ChildComp");

        var consumerResult = CompileToCSharp(
            cshtmlContent: @"<ChildComp />",
            baseCompilation: baseCompilation);

        Assert.Contains("OpenComponent", consumerResult.Code);
        Assert.DoesNotContain("OpenElement(0, \"ChildComp\")", consumerResult.Code);

        var producerResult = CompileToCSharp(
            cshtmlRelativePath: "/ChildComp.cshtml",
            cshtmlContent: producerSource);
        var combined = (producerResult.DeclCode ?? "") + "\n" + producerResult.Code;
        // The markup body must survive in the merged output. Razor may render it
        // either as element-tree calls (`OpenElement(N, "p")`...) or, when the
        // markup is pure HTML with no expressions/tag-helpers, as a single
        // `AddMarkupContent(N, "<p>...</p>")` literal -- both are runtime-correct.
        // The regression we're guarding against is the UnresolvedElement-drop
        // bug, where the wrapping `<p>` disappears entirely.
        Assert.True(
            combined.Contains("OpenElement") || combined.Contains("<p>default</p>"),
            $"Markup body lost from producer. Decl: {producerResult.DeclCode}\n---\nImpl: {producerResult.Code}");
    }

    // ---------------------------------------------------------------------------------
    // Second-pass adversarial cases discovered during the systematic audit.
    // ---------------------------------------------------------------------------------

    /// <summary>
    /// Adversarial: property with an accessor-level expression body containing
    /// markup. The property itself has neither an <c>ExpressionBody</c> (no
    /// <c>=&gt;</c> at the property level) nor an <c>Initializer</c>, so the
    /// existing <c>TryBuildHelperSynth</c> filter returns false. ClassifyOne
    /// then routes the property as DeclOnly, which triggers the
    /// UnresolvedElement-drop bug.
    /// </summary>
    [Fact]
    public void Consumer_CanReference_ProducerWithAccessorExpressionBodyMarkup()
    {
        var producerSource = @"
@using Microsoft.AspNetCore.Components;

@code {
    [Parameter] public RenderFragment ChildContent { get => @<p>default</p>; }
}";
        var baseCompilation = BuildBaseCompilationWithProducerRef(producerSource, "ChildComp");

        var consumerResult = CompileToCSharp(
            cshtmlContent: @"<ChildComp />",
            baseCompilation: baseCompilation);

        Assert.Contains("OpenComponent", consumerResult.Code);

        var producerResult = CompileToCSharp(
            cshtmlRelativePath: "/ChildComp.cshtml",
            cshtmlContent: producerSource);
        var combined = (producerResult.DeclCode ?? "") + "\n" + producerResult.Code;
        Assert.True(
            combined.Contains("OpenElement") || combined.Contains("<p>default</p>"),
            $"Markup body lost from producer. Decl: {producerResult.DeclCode}\n---\nImpl: {producerResult.Code}");
    }

    /// <summary>
    /// Adversarial: statement-bodied getter returning markup
    /// (<c>{ get { return @&lt;p/&gt;; } }</c>). Same root as the accessor-
    /// expression-body case but with a block body instead of an expression body;
    /// requires the synth to extract the markup from the return statement's
    /// expression.
    /// </summary>
    [Fact]
    public void Consumer_CanReference_ProducerWithStatementBodiedGetterMarkup()
    {
        var producerSource = @"
@using Microsoft.AspNetCore.Components;

@code {
    [Parameter] public RenderFragment ChildContent
    {
        get { return @<p>default</p>; }
    }
}";
        var baseCompilation = BuildBaseCompilationWithProducerRef(producerSource, "ChildComp");

        var consumerResult = CompileToCSharp(
            cshtmlContent: @"<ChildComp />",
            baseCompilation: baseCompilation);

        var producerResult = CompileToCSharp(
            cshtmlRelativePath: "/ChildComp.cshtml",
            cshtmlContent: producerSource);
        var combined = (producerResult.DeclCode ?? "") + "\n" + producerResult.Code;
        Assert.True(consumerResult.Code.Contains("OpenComponent"),
            $"OpenComponent missing from consumer.\nDecl: {producerResult.DeclCode}\n---\nImpl: {producerResult.Code}\n---\nConsumer: {consumerResult.Code}");
        Assert.True(
            combined.Contains("OpenElement") || combined.Contains("<p>default</p>"),
            $"Markup body lost. Decl: {producerResult.DeclCode}\n---\nImpl: {producerResult.Code}");
    }

    /// <summary>
    /// Adversarial: typed accessor expression body
    /// (<c>RenderFragment&lt;string&gt; { get =&gt; s =&gt; @&lt;p&gt;@s&lt;/p&gt;; }</c>).
    /// Verifies the accessor-body extraction works for typed RF too.
    /// </summary>
    [Fact]
    public void Consumer_CanReference_ProducerWithTypedAccessorExpressionBodyMarkup()
    {
        var producerSource = @"
@using Microsoft.AspNetCore.Components;

@code {
    [Parameter] public RenderFragment<string> ChildContent
    {
        get => s => @<p>@s</p>;
    }
}";
        var baseCompilation = BuildBaseCompilationWithProducerRef(producerSource, "ChildComp");

        var consumerResult = CompileToCSharp(
            cshtmlContent: @"<ChildComp />",
            baseCompilation: baseCompilation);

        var producerResult = CompileToCSharp(
            cshtmlRelativePath: "/ChildComp.cshtml",
            cshtmlContent: producerSource);
        var combined = (producerResult.DeclCode ?? "") + "\n" + producerResult.Code;
        Assert.True(consumerResult.Code.Contains("OpenComponent"),
            $"OpenComponent missing. Decl: {producerResult.DeclCode}\n---\nImpl: {producerResult.Code}\n---\nConsumer: {consumerResult.Code}");
        Assert.True(
            combined.Contains("OpenElement") || combined.Contains("<p>"),
            $"Markup body lost. Decl: {producerResult.DeclCode}\n---\nImpl: {producerResult.Code}");
    }

    /// <summary>
    /// Adversarial: <c>Func&lt;T, RenderFragment&gt;</c> typed property -- semantically
    /// equivalent to <c>RenderFragment&lt;T&gt;</c> but the splitter's textual
    /// shape detection only recognizes the literal <c>RenderFragment</c> head.
    /// </summary>
    [Fact]
    public void Consumer_CanReference_ProducerWithFuncReturningRenderFragment()
    {
        var producerSource = @"
@using System;
@using Microsoft.AspNetCore.Components;

@code {
    [Parameter] public Func<string, RenderFragment> ChildContent => s => @<p>@s</p>;
}";
        var baseCompilation = BuildBaseCompilationWithProducerRef(producerSource, "ChildComp");

        var consumerResult = CompileToCSharp(
            cshtmlContent: @"<ChildComp />",
            baseCompilation: baseCompilation);

        var producerResult = CompileToCSharp(
            cshtmlRelativePath: "/ChildComp.cshtml",
            cshtmlContent: producerSource);
        var combined = (producerResult.DeclCode ?? "") + "\n" + producerResult.Code;
        Assert.True(consumerResult.Code.Contains("OpenComponent"),
            $"OpenComponent missing. Decl: {producerResult.DeclCode}\n---\nImpl: {producerResult.Code}\n---\nConsumer: {consumerResult.Code}");
        Assert.True(
            combined.Contains("OpenElement") || combined.Contains("<p>"),
            $"Markup body lost. Decl: {producerResult.DeclCode}\n---\nImpl: {producerResult.Code}");
    }

    /// <summary>
    /// Adversarial: parenthesized markup as the property's expression body
    /// (<c>=&gt; (@&lt;p/&gt;)</c>). Preprocessor's <c>IsExpressionPosition</c>
    /// only treats <c>=&gt;</c>/<c>=</c> as expression positions; markup
    /// preceded by <c>(</c> emits a statement-shaped placeholder that breaks
    /// the parse.
    /// </summary>
    [Fact]
    public void Consumer_CanReference_ProducerWithParenthesizedMarkupBody()
    {
        var producerSource = @"
@using Microsoft.AspNetCore.Components;

@code {
    [Parameter] public RenderFragment ChildContent => (@<p>default</p>);
}";
        var baseCompilation = BuildBaseCompilationWithProducerRef(producerSource, "ChildComp");

        var consumerResult = CompileToCSharp(
            cshtmlContent: @"<ChildComp />",
            baseCompilation: baseCompilation);

        var producerResult = CompileToCSharp(
            cshtmlRelativePath: "/ChildComp.cshtml",
            cshtmlContent: producerSource);
        var combined = (producerResult.DeclCode ?? "") + "\n" + producerResult.Code;
        Assert.True(consumerResult.Code.Contains("OpenComponent"),
            $"OpenComponent missing. Decl: {producerResult.DeclCode}\n---\nImpl: {producerResult.Code}\n---\nConsumer: {consumerResult.Code}");
        Assert.True(
            combined.Contains("OpenElement") || combined.Contains("<p>default</p>"),
            $"Markup body lost. Decl: {producerResult.DeclCode}\n---\nImpl: {producerResult.Code}");
    }

    /// <summary>
    /// Adversarial: conditional template
    /// (<c>=&gt; cond ? @&lt;p/&gt; : @&lt;span/&gt;</c>).
    /// </summary>
    [Fact]
    public void Consumer_CanReference_ProducerWithConditionalTemplate()
    {
        var producerSource = @"
@using Microsoft.AspNetCore.Components;

@code {
    private bool UseP => true;
    [Parameter] public RenderFragment ChildContent => UseP ? @<p>default</p> : @<span>alt</span>;
}";
        var baseCompilation = BuildBaseCompilationWithProducerRef(producerSource, "ChildComp");

        var consumerResult = CompileToCSharp(
            cshtmlContent: @"<ChildComp />",
            baseCompilation: baseCompilation);

        var producerResult = CompileToCSharp(
            cshtmlRelativePath: "/ChildComp.cshtml",
            cshtmlContent: producerSource);
        var combined = (producerResult.DeclCode ?? "") + "\n" + producerResult.Code;
        Assert.True(consumerResult.Code.Contains("OpenComponent"),
            $"OpenComponent missing. Decl: {producerResult.DeclCode}\n---\nImpl: {producerResult.Code}\n---\nConsumer: {consumerResult.Code}");
        Assert.True(
            combined.Contains("OpenElement") || combined.Contains("<p>default</p>"),
            $"Markup body lost. Decl: {producerResult.DeclCode}\n---\nImpl: {producerResult.Code}");
    }

    /// <summary>
    /// Null-coalescing template without parens (<c>=&gt; Override ?? @&lt;p/&gt;</c>).
    /// Razor lowers the markup to a bare lambda, and <c>expr ?? lambda</c> doesn't satisfy
    /// the null-coalescing grammar (the lambda arrow binds against the wrong operand). This
    /// is invalid C# regardless of the split; the user works around it by parenthesising the markup
    /// (<c>=&gt; Override ?? (@&lt;p/&gt;)</c>, covered by
    /// <see cref="Consumer_CanReference_ProducerWithParenthesizedMarkupBody"/>).
    /// </summary>
    [Fact]
    public void InvalidCode_NullCoalesceUnparenthesizedMarkup()
    {
        var producerSource = @"
@using Microsoft.AspNetCore.Components;

@code {
    private RenderFragment Override => null;
    [Parameter] public RenderFragment ChildContent => Override ?? @<p>default</p>;
}";
        var producerResult = CompileToCSharp(
            cshtmlRelativePath: "/ChildComp.cshtml",
            cshtmlContent: producerSource);
        CompileToAssembly(producerResult, diagnostics =>
            Assert.Contains(diagnostics, d => d.Severity == DiagnosticSeverity.Error));
    }

    /// <summary>
    /// Adversarial: spaced generic type text (<c>RenderFragment &lt; string &gt;</c>).
    /// The shape parser slices before the first <c>&lt;</c> without trimming the
    /// head; whitespace there breaks the literal match.
    /// </summary>
    [Fact]
    public void Consumer_CanReference_ProducerWithSpacedGenericRenderFragment()
    {
        var producerSource = @"
@using Microsoft.AspNetCore.Components;

@code {
    [Parameter] public RenderFragment < string > ChildContent => s => @<p>@s</p>;
}";
        var baseCompilation = BuildBaseCompilationWithProducerRef(producerSource, "ChildComp");

        var consumerResult = CompileToCSharp(
            cshtmlContent: @"<ChildComp />",
            baseCompilation: baseCompilation);

        var producerResult = CompileToCSharp(
            cshtmlRelativePath: "/ChildComp.cshtml",
            cshtmlContent: producerSource);
        var combined = (producerResult.DeclCode ?? "") + "\n" + producerResult.Code;
        Assert.True(consumerResult.Code.Contains("OpenComponent"),
            $"OpenComponent missing. Decl: {producerResult.DeclCode}\n---\nImpl: {producerResult.Code}\n---\nConsumer: {consumerResult.Code}");
        Assert.True(
            combined.Contains("OpenElement") || combined.Contains("<p>"),
            $"Markup body lost. Decl: {producerResult.DeclCode}\n---\nImpl: {producerResult.Code}");
    }

    // -- Round 3 adversarial cases from rubber-duck attack on the new fixes ----------

    /// <summary>
    /// Adversarial: the string literal <c>"case"</c> appears in a ternary's
    /// condition. <c>IsLabelContext</c> strips string and char literals before
    /// scanning for <c>case</c>/<c>default</c> keywords so a substring inside a
    /// string doesn't misclassify the ternary <c>:</c> as a switch-label.
    /// </summary>
    [Fact]
    public void Consumer_CanReference_ProducerWithCaseStringInTernary()
    {
        var producerSource = @"
@using Microsoft.AspNetCore.Components;

@code {
    private string Label = ""case"";
    [Parameter] public RenderFragment ChildContent => Label == ""case"" ? @<p>yes</p> : @<span>no</span>;
}";
        var baseCompilation = BuildBaseCompilationWithProducerRef(producerSource, "ChildComp");

        var consumerResult = CompileToCSharp(
            cshtmlContent: @"<ChildComp />",
            baseCompilation: baseCompilation);

        var producerResult = CompileToCSharp(
            cshtmlRelativePath: "/ChildComp.cshtml",
            cshtmlContent: producerSource);
        var combined = (producerResult.DeclCode ?? "") + "\n" + producerResult.Code;
        Assert.True(consumerResult.Code.Contains("OpenComponent"),
            $"OpenComponent missing. Decl: {producerResult.DeclCode}\n---\nImpl: {producerResult.Code}\n---\nConsumer: {consumerResult.Code}");
        Assert.True(
            combined.Contains("OpenElement") || combined.Contains("<p>yes</p>"),
            $"Markup body lost. Decl: {producerResult.DeclCode}\n---\nImpl: {producerResult.Code}");
    }

    /// <summary>
    /// Adversarial: comment between <c>return</c> and markup. The
    /// expression-position scan walks back past whitespace only, sees <c>/</c>
    /// (the end of the comment), and misclassifies the position as statement-
    /// shaped.
    /// </summary>
    [Fact]
    public void Consumer_CanReference_ProducerWithCommentBeforeReturnMarkup()
    {
        var producerSource = @"
@using Microsoft.AspNetCore.Components;

@code {
    [Parameter] public RenderFragment ChildContent
    {
        get { return /* render fallback */ @<p>default</p>; }
    }
}";
        var baseCompilation = BuildBaseCompilationWithProducerRef(producerSource, "ChildComp");

        var consumerResult = CompileToCSharp(
            cshtmlContent: @"<ChildComp />",
            baseCompilation: baseCompilation);

        var producerResult = CompileToCSharp(
            cshtmlRelativePath: "/ChildComp.cshtml",
            cshtmlContent: producerSource);
        var combined = (producerResult.DeclCode ?? "") + "\n" + producerResult.Code;
        Assert.True(consumerResult.Code.Contains("OpenComponent"),
            $"OpenComponent missing. Decl: {producerResult.DeclCode}\n---\nImpl: {producerResult.Code}\n---\nConsumer: {consumerResult.Code}");
        Assert.True(
            combined.Contains("OpenElement") || combined.Contains("<p>default</p>"),
            $"Markup body lost. Decl: {producerResult.DeclCode}\n---\nImpl: {producerResult.Code}");
    }

    /// <summary>
    /// Adversarial: <c>Func&lt;T, RF&gt;</c> where RF is an alias for
    /// RenderFragment. The Func-recognition path checks the second arg
    /// literally; it must also consult the alias map.
    /// </summary>
    [Fact]
    public void Consumer_CanReference_ProducerWithFuncReturningAliasedRenderFragment()
    {
        var producerSource = @"
@using System;
@using Microsoft.AspNetCore.Components;
@using RF = Microsoft.AspNetCore.Components.RenderFragment;

@code {
    [Parameter] public Func<string, RF> ChildContent => s => @<p>@s</p>;
}";
        var baseCompilation = BuildBaseCompilationWithProducerRef(producerSource, "ChildComp");

        var consumerResult = CompileToCSharp(
            cshtmlContent: @"<ChildComp />",
            baseCompilation: baseCompilation);

        var producerResult = CompileToCSharp(
            cshtmlRelativePath: "/ChildComp.cshtml",
            cshtmlContent: producerSource);
        var combined = (producerResult.DeclCode ?? "") + "\n" + producerResult.Code;
        Assert.True(consumerResult.Code.Contains("OpenComponent"),
            $"OpenComponent missing. Decl: {producerResult.DeclCode}\n---\nImpl: {producerResult.Code}\n---\nConsumer: {consumerResult.Code}");
        Assert.True(
            combined.Contains("OpenElement") || combined.Contains("<p>"),
            $"Markup body lost. Decl: {producerResult.DeclCode}\n---\nImpl: {producerResult.Code}");
    }

    /// <summary>
    /// Adversarial: tuple type arg in <c>Func&lt;(T1,T2), RenderFragment&gt;</c>.
    /// The Func arg splitter only tracks angle bracket depth, so the comma
    /// inside the tuple is mis-split.
    /// </summary>
    [Fact]
    public void Consumer_CanReference_ProducerWithFuncTupleArgRenderFragment()
    {
        var producerSource = @"
@using System;
@using Microsoft.AspNetCore.Components;

@code {
    [Parameter] public Func<(int Id, string Name), RenderFragment> ChildContent
        => row => @<p>@row.Id - @row.Name</p>;
}";
        var baseCompilation = BuildBaseCompilationWithProducerRef(producerSource, "ChildComp");

        var consumerResult = CompileToCSharp(
            cshtmlContent: @"<ChildComp />",
            baseCompilation: baseCompilation);

        var producerResult = CompileToCSharp(
            cshtmlRelativePath: "/ChildComp.cshtml",
            cshtmlContent: producerSource);
        var combined = (producerResult.DeclCode ?? "") + "\n" + producerResult.Code;
        Assert.True(consumerResult.Code.Contains("OpenComponent"),
            $"OpenComponent missing. Decl: {producerResult.DeclCode}\n---\nImpl: {producerResult.Code}\n---\nConsumer: {consumerResult.Code}");
        Assert.True(
            combined.Contains("OpenElement") || combined.Contains("<p>"),
            $"Markup body lost. Decl: {producerResult.DeclCode}\n---\nImpl: {producerResult.Code}");
    }

    /// <summary>
    /// Adversarial: comment between lambda <c>=&gt;</c> and markup. Same
    /// look-back fragility as the return-comment case but for lambda body.
    /// </summary>
    [Fact]
    public void Consumer_CanReference_ProducerWithCommentBeforeLambdaMarkup()
    {
        var producerSource = @"
@using System;
@using Microsoft.AspNetCore.Components;

@code {
    [Parameter] public Func<int, RenderFragment> ChildContent
        => item => /* comment */ @<p>@item</p>;
}";
        var baseCompilation = BuildBaseCompilationWithProducerRef(producerSource, "ChildComp");

        var consumerResult = CompileToCSharp(
            cshtmlContent: @"<ChildComp />",
            baseCompilation: baseCompilation);

        var producerResult = CompileToCSharp(
            cshtmlRelativePath: "/ChildComp.cshtml",
            cshtmlContent: producerSource);
        var combined = (producerResult.DeclCode ?? "") + "\n" + producerResult.Code;
        Assert.True(consumerResult.Code.Contains("OpenComponent"),
            $"OpenComponent missing. Decl: {producerResult.DeclCode}\n---\nImpl: {producerResult.Code}\n---\nConsumer: {consumerResult.Code}");
        Assert.True(
            combined.Contains("OpenElement") || combined.Contains("<p>"),
            $"Markup body lost. Decl: {producerResult.DeclCode}\n---\nImpl: {producerResult.Code}");
    }

    /// <summary>
    /// Adversarial: typed RenderFragment via alias (`@using RFS = RenderFragment&lt;string&gt;;`).
    /// Verifies that alias resolution preserves the generic type argument so
    /// the synth picks the correct curried shape.
    /// </summary>
    [Fact]
    public void Consumer_CanReference_ProducerWithAliasedTypedRenderFragment()
    {
        var producerSource = @"
@using Microsoft.AspNetCore.Components;
@using RFS = Microsoft.AspNetCore.Components.RenderFragment<string>;

@code {
    [Parameter] public RFS ChildContent { get; set; } = (context) => @<p>@context</p>;
}";
        var baseCompilation = BuildBaseCompilationWithProducerRef(producerSource, "ChildComp");

        var consumerResult = CompileToCSharp(
            cshtmlContent: @"<ChildComp />",
            baseCompilation: baseCompilation);

        Assert.Contains("OpenComponent", consumerResult.Code);

        var producerResult = CompileToCSharp(
            cshtmlRelativePath: "/ChildComp.cshtml",
            cshtmlContent: producerSource);
        var combined = (producerResult.DeclCode ?? "") + "\n" + producerResult.Code;
        Assert.True(
            combined.Contains("OpenElement") || combined.Contains("<p>"),
            $"Markup body lost from producer. Decl: {producerResult.DeclCode}\n---\nImpl: {producerResult.Code}");
    }

    // ---------------------------------------------------------------------------------
    // Init-only auto-property with a markup initializer: a supported shape (not a gap).
    // ---------------------------------------------------------------------------------

    /// <summary>
    /// An <c>init</c>-only auto-property with a markup initializer
    /// (<c>{ get; init; } = @&lt;p/&gt;;</c>). The initializer is a property-level body
    /// site, so it moves to a static-partial synth while the accessor list -- including
    /// the <c>init</c> modifier -- is emitted verbatim. The two halves compile cleanly.
    /// </summary>
    [Fact]
    public void InitOnlyAutoPropMarkupInitializer_PreservesInitAndCompiles()
    {
        var producerSource = @"
@using Microsoft.AspNetCore.Components;

@code {
    [Parameter] public RenderFragment ChildContent { get; init; } = @<p>default</p>;
}";
        var producerResult = CompileToCSharp(
            cshtmlRelativePath: "/ChildComp.cshtml",
            cshtmlContent: producerSource);

        // The init modifier survives: the accessor list is emitted verbatim.
        Assert.Contains("init;", producerResult.DeclCode ?? "");

        // The decl + impl halves compile together with no errors -- a supported shape.
        CompileToAssembly(producerResult, diagnostics =>
            Assert.DoesNotContain(diagnostics, d => d.Severity == DiagnosticSeverity.Error));
    }

    // ---------------------------------------------------------------------------------
    // Invalid-input probes. These inputs don't compile with or without the split, so the
    // splitter declines to transform them -- it emits the member as the user wrote it and
    // lets the C# compiler report the same diagnostic it would for the unsplit document.
    // Each test asserts that exact error, proving the split neither masks nor worsens it.
    // ---------------------------------------------------------------------------------

    /// <summary>
    /// Explicit interface implementation of a RenderFragment member
    /// (<c>RenderFragment IFoo.Bar =&gt; @&lt;p/&gt;;</c>). A .razor component can't add
    /// <c>: IFoo</c> to its generated class, so the explicit member has no interface to
    /// implement -- a CS0540 error independent of the split.
    /// </summary>
    [Fact]
    public void InvalidCode_ExplicitInterfaceMarkupProperty()
    {
        var producerSource = @"
@using Microsoft.AspNetCore.Components;

@code {
    public interface IFoo
    {
        RenderFragment Bar { get; }
    }

    RenderFragment IFoo.Bar => @<p>default</p>;
}";
        var producerResult = CompileToCSharp(
            cshtmlRelativePath: "/ChildComp.cshtml",
            cshtmlContent: producerSource);
        CompileToAssembly(producerResult, diagnostics =>
            Assert.Contains(diagnostics, d => d.Severity == DiagnosticSeverity.Error && d.Id == "CS0540"));
    }

    /// <summary>
    /// Property with both a markup <c>get</c> and a plain (non-markup) <c>set</c>.
    /// <see cref="ClassBodySplitter"/> collects every markup-bearing accessor, so the
    /// getter routes through a synth while the plain setter stays verbatim in the decl.
    /// The getter markup therefore moves to impl, leaving a discoverable delegating
    /// property in the pre-discovery decl.
    /// </summary>
    [Fact]
    public void Consumer_CanReference_ProducerWithMixedGetMarkupPlainSet()
    {
        var producerSource = @"
@using Microsoft.AspNetCore.Components;

@code {
    private RenderFragment _foo;
    [Parameter] public RenderFragment Foo
    {
        get => @<p>default</p>;
        set => _foo = value;
    }
}";
        var baseCompilation = BuildBaseCompilationWithProducerRef(producerSource, "ChildComp");

        var consumerResult = CompileToCSharp(
            cshtmlContent: @"<ChildComp />",
            baseCompilation: baseCompilation);

        var producerResult = CompileToCSharp(
            cshtmlRelativePath: "/ChildComp.cshtml",
            cshtmlContent: producerSource);
        var combined = (producerResult.DeclCode ?? "") + "\n" + producerResult.Code;
        Assert.True(consumerResult.Code.Contains("OpenComponent"),
            $"OpenComponent missing. Decl: {producerResult.DeclCode}\n---\nImpl: {producerResult.Code}\n---\nConsumer: {consumerResult.Code}");
        Assert.True(combined.Contains("OpenElement") || combined.Contains("<p>default</p>"),
            $"Markup lost. Decl: {producerResult.DeclCode}\n---\nImpl: {producerResult.Code}");
    }

    /// <summary>
    /// Multi-statement getter where the last statement is the markup return. The
    /// statement-body synth wraps the <em>entire</em> accessor block (preserving the
    /// preceding <c>var x</c> local), so the markup moves to impl and a discoverable
    /// delegating getter stays in decl.
    /// </summary>
    [Fact]
    public void Consumer_CanReference_ProducerWithMultiStatementGetterMarkup()
    {
        var producerSource = @"
@using Microsoft.AspNetCore.Components;

@code {
    [Parameter] public RenderFragment Foo
    {
        get
        {
            var x = ""hello"";
            return @<p>@x</p>;
        }
    }
}";
        var baseCompilation = BuildBaseCompilationWithProducerRef(producerSource, "ChildComp");

        var consumerResult = CompileToCSharp(
            cshtmlContent: @"<ChildComp />",
            baseCompilation: baseCompilation);

        var producerResult = CompileToCSharp(
            cshtmlRelativePath: "/ChildComp.cshtml",
            cshtmlContent: producerSource);
        var combined = (producerResult.DeclCode ?? "") + "\n" + producerResult.Code;
        Assert.True(consumerResult.Code.Contains("OpenComponent"),
            $"OpenComponent missing. Decl: {producerResult.DeclCode}\n---\nImpl: {producerResult.Code}\n---\nConsumer: {consumerResult.Code}");
        Assert.True(combined.Contains("OpenElement") || combined.Contains("<p>"),
            $"Markup lost. Decl: {producerResult.DeclCode}\n---\nImpl: {producerResult.Code}");
    }

    /// <summary>
    /// <c>[Parameter]</c> on a field with a markup initializer. <c>[Parameter]</c>
    /// targets properties, so this is a CS0592 error regardless of the split -- the field
    /// never compiles regardless of how its markup is handled.
    /// </summary>
    [Fact]
    public void InvalidCode_ParameterFieldMarkupInitializer()
    {
        var producerSource = @"
@using Microsoft.AspNetCore.Components;

@code {
    [Parameter] public RenderFragment Foo = @<p>default</p>;
}";
        var producerResult = CompileToCSharp(
            cshtmlRelativePath: "/ChildComp.cshtml",
            cshtmlContent: producerSource);
        CompileToAssembly(producerResult, diagnostics =>
            Assert.Contains(diagnostics, d => d.Severity == DiagnosticSeverity.Error && d.Id == "CS0592"));
    }

    /// <summary>
    /// A user-named label before markup in a hand-written render method
    /// (<c>retry: &lt;p/&gt;</c>). Markup in statement position after a non-<c>case</c>
    /// label doesn't lower correctly and fails with CS1525 regardless of the split -- a pre-existing
    /// Razor limitation, not a split artifact.
    /// </summary>
    [Fact]
    public void InvalidCode_LabelledStatementMarkup()
    {
        var producerSource = @"
@using Microsoft.AspNetCore.Components;
@using Microsoft.AspNetCore.Components.Rendering;

@code {
    void Render(RenderTreeBuilder __builder)
    {
        retry:
        <p>retrying</p>
    }
}";
        var producerResult = CompileToCSharp(
            cshtmlRelativePath: "/ChildComp.cshtml",
            cshtmlContent: producerSource);
        CompileToAssembly(producerResult, diagnostics =>
            Assert.Contains(diagnostics, d => d.Severity == DiagnosticSeverity.Error && d.Id == "CS1525"));
    }

    // ---------------------------------------------------------------------------------
    // Round 4 (post-holistic-refactor) adversarial cases from the second rubber-duck
    // attack on the in-place transform design. These probe shapes we haven't yet
    // exercised: setter-body markup, multi-variable fields, multi-arm switch
    // expressions, collection expressions, multiple @code blocks, alias chains,
    // and nested lambda body sites.
    // ---------------------------------------------------------------------------------

    /// <summary>
    /// Setter accessor body contains markup (rare but legal -- the setter uses
    /// the markup as a fallback expression assigned to a backing field).
    /// </summary>
    /// <remarks>
    /// A set/init accessor body is void and reads the incoming `value`, so it can't use
    /// the getter-shaped synth (`partial RF Synth()`). It uses a `partial void
    /// Synth(value)` synth instead: the decl keeps the property with a delegating setter
    /// (`set => Synth(value);`) -- discoverable, no markup -- and the impl emits the synth
    /// method wrapping the markup body. So the markup lands in impl with resolved tag
    /// helpers, and the decl is discovery-independent.
    ///
    /// The markup is parenthesized (`(@&lt;p/&gt;)`). The bare null-coalesced form
    /// (`value ?? @&lt;p/&gt;`) lowers to invalid C# regardless of the split -- see
    /// <see cref="InvalidCode_NullCoalesceUnparenthesizedMarkup"/> -- so
    /// the parens are the documented user workaround, not a split artifact.
    /// </remarks>
    [Fact]
    public void Consumer_CanReference_ProducerWithSetterBodyMarkup()
    {
        var producerSource = @"
@using Microsoft.AspNetCore.Components;

@code {
    private RenderFragment _foo;
    [Parameter] public RenderFragment ChildContent
    {
        get => _foo;
        set => _foo = (@<p>fallback</p>);
    }
}";
        var baseCompilation = BuildBaseCompilationWithProducerRef(producerSource, "ChildComp");

        var consumerResult = CompileToCSharp(
            cshtmlContent: @"<ChildComp />",
            baseCompilation: baseCompilation);

        var producerResult = CompileToCSharp(
            cshtmlRelativePath: "/ChildComp.cshtml",
            cshtmlContent: producerSource);
        var combined = (producerResult.DeclCode ?? "") + "\n" + producerResult.Code;
        Assert.True(consumerResult.Code.Contains("OpenComponent"),
            $"OpenComponent missing. Decl: {producerResult.DeclCode}\n---\nImpl: {producerResult.Code}\n---\nConsumer: {consumerResult.Code}");
        Assert.True(
            combined.Contains("OpenElement") || combined.Contains("fallback"),
            $"Markup body lost. Decl: {producerResult.DeclCode}\n---\nImpl: {producerResult.Code}");
    }

    /// <summary>
    /// Init accessor body contains markup -- the init-only counterpart of the
    /// setter case. Like a setter, an init accessor is void and references the
    /// `value` parameter, so it uses the `partial void Synth(value)` synth.
    /// </summary>
    /// <remarks>
    /// The decl keeps the property with a delegating init accessor; the markup body
    /// moves to the impl synth method. Markup is parenthesized for the same reason as
    /// the setter case (`?? @&lt;p/&gt;` is invalid regardless of the split).
    /// </remarks>
    [Fact]
    public void Consumer_CanReference_ProducerWithInitBodyMarkup()
    {
        var producerSource = @"
@using Microsoft.AspNetCore.Components;

@code {
    private RenderFragment _foo;
    [Parameter] public RenderFragment ChildContent
    {
        get => _foo;
        init => _foo = (@<p>fallback</p>);
    }
}";
        var baseCompilation = BuildBaseCompilationWithProducerRef(producerSource, "ChildComp");

        var consumerResult = CompileToCSharp(
            cshtmlContent: @"<ChildComp />",
            baseCompilation: baseCompilation);

        var producerResult = CompileToCSharp(
            cshtmlRelativePath: "/ChildComp.cshtml",
            cshtmlContent: producerSource);
        var combined = (producerResult.DeclCode ?? "") + "\n" + producerResult.Code;
        Assert.True(consumerResult.Code.Contains("OpenComponent"),
            $"OpenComponent missing. Decl: {producerResult.DeclCode}\n---\nImpl: {producerResult.Code}\n---\nConsumer: {consumerResult.Code}");
        Assert.True(
            combined.Contains("OpenElement") || combined.Contains("fallback"),
            $"Markup body lost. Decl: {producerResult.DeclCode}\n---\nImpl: {producerResult.Code}");
    }

    /// <summary>
    /// Property with markup in BOTH accessors -- a markup getter and a markup setter on
    /// the same property. The splitter mints a synth per accessor (getter synth +
    /// <c>partial void Synth(value)</c> setter synth), moving both markup bodies to impl
    /// while leaving a discoverable delegating property in decl. The consumer resolves
    /// the producer as a component, and both markup fragments survive in impl.
    /// </summary>
    [Fact]
    public void Consumer_CanReference_ProducerWithMultiAccessorMarkup()
    {
        var producerSource = @"
@using Microsoft.AspNetCore.Components;

@code {
    private RenderFragment _foo;
    [Parameter] public RenderFragment ChildContent
    {
        get => @<p>getter</p>;
        set => _foo = (@<div>setter</div>);
    }
}";
        var baseCompilation = BuildBaseCompilationWithProducerRef(producerSource, "ChildComp");

        var consumerResult = CompileToCSharp(
            cshtmlContent: @"<ChildComp />",
            baseCompilation: baseCompilation);

        var producerResult = CompileToCSharp(
            cshtmlRelativePath: "/ChildComp.cshtml",
            cshtmlContent: producerSource);
        var combined = (producerResult.DeclCode ?? "") + "\n" + producerResult.Code;
        Assert.True(consumerResult.Code.Contains("OpenComponent"),
            $"OpenComponent missing. Decl: {producerResult.DeclCode}\n---\nImpl: {producerResult.Code}\n---\nConsumer: {consumerResult.Code}");
        // Both accessor markups survive (in impl).
        Assert.True(producerResult.Code.Contains("getter"),
            $"Getter markup lost. Impl: {producerResult.Code}");
        Assert.True(producerResult.Code.Contains("setter"),
            $"Setter markup lost. Impl: {producerResult.Code}");
    }

    /// <summary>
    /// Multi-variable field declaration with a markup initializer on each variable
    /// (<c>[Parameter] RenderFragment A = @&lt;p/&gt;, B = @&lt;span/&gt;;</c>).
    /// <c>[Parameter]</c> targets properties, so each variable is a CS0592 error regardless
    /// of the split -- the field never compiles, so the splitter leaves it untransformed.
    /// </summary>
    [Fact]
    public void InvalidCode_MultiVariableParameterField()
    {
        var producerSource = @"
@using Microsoft.AspNetCore.Components;

@code {
    [Parameter] public RenderFragment A = @<p>aa</p>, B = @<span>bb</span>;
}";
        var producerResult = CompileToCSharp(
            cshtmlRelativePath: "/ChildComp.cshtml",
            cshtmlContent: producerSource);
        CompileToAssembly(producerResult, diagnostics =>
            Assert.Contains(diagnostics, d => d.Severity == DiagnosticSeverity.Error && d.Id == "CS0592"));
    }

    /// <summary>
    /// Switch expression with multiple markup-bearing arms. The property body
    /// contains TWO markup expressions; our design assumes ONE body site per
    /// property. The whole property expression body must be moved to impl as a
    /// single unit, not per-arm.
    /// </summary>
    [Fact]
    public void Consumer_CanReference_ProducerWithSwitchExpressionMarkupArms()
    {
        var producerSource = @"
@using Microsoft.AspNetCore.Components;

@code {
    private int Kind = 1;
    [Parameter] public RenderFragment ChildContent => Kind switch
    {
        0 => @<p>zero</p>,
        _ => @<span>other</span>
    };
}";
        var baseCompilation = BuildBaseCompilationWithProducerRef(producerSource, "ChildComp");

        var consumerResult = CompileToCSharp(
            cshtmlContent: @"<ChildComp />",
            baseCompilation: baseCompilation);

        var producerResult = CompileToCSharp(
            cshtmlRelativePath: "/ChildComp.cshtml",
            cshtmlContent: producerSource);
        var combined = (producerResult.DeclCode ?? "") + "\n" + producerResult.Code;
        Assert.True(consumerResult.Code.Contains("OpenComponent"),
            $"OpenComponent missing. Decl: {producerResult.DeclCode}\n---\nImpl: {producerResult.Code}\n---\nConsumer: {consumerResult.Code}");
        Assert.True(
            combined.Contains("OpenElement") || (combined.Contains("zero") && combined.Contains("other")),
            $"Markup body lost. Decl: {producerResult.DeclCode}\n---\nImpl: {producerResult.Code}");
    }

    /// <summary>
    /// Global-qualified alias (<c>@using RF = global::Microsoft.AspNetCore.Components.RenderFragment</c>).
    /// The alias map stores the resolved type verbatim including the <c>global::</c>
    /// prefix; the shape recognizer must accept the prefixed form.
    /// </summary>
    [Fact]
    public void Consumer_CanReference_ProducerWithGlobalQualifiedAlias()
    {
        var producerSource = @"
@using RF = global::Microsoft.AspNetCore.Components.RenderFragment;

@code {
    [Microsoft.AspNetCore.Components.Parameter] public RF ChildContent => @<p>default</p>;
}";
        var baseCompilation = BuildBaseCompilationWithProducerRef(producerSource, "ChildComp");

        var consumerResult = CompileToCSharp(
            cshtmlContent: @"<ChildComp />",
            baseCompilation: baseCompilation);

        var producerResult = CompileToCSharp(
            cshtmlRelativePath: "/ChildComp.cshtml",
            cshtmlContent: producerSource);
        var combined = (producerResult.DeclCode ?? "") + "\n" + producerResult.Code;
        Assert.True(consumerResult.Code.Contains("OpenComponent"),
            $"OpenComponent missing. Decl: {producerResult.DeclCode}\n---\nImpl: {producerResult.Code}\n---\nConsumer: {consumerResult.Code}");
        Assert.True(
            combined.Contains("OpenElement") || combined.Contains("<p>default</p>"),
            $"Markup body lost. Decl: {producerResult.DeclCode}\n---\nImpl: {producerResult.Code}");
    }

    /// <summary>
    /// Multiple <c>@code</c> blocks in the same component. The splitter aggregates
    /// across blocks; synth method-name generation must be deterministic and
    /// non-colliding.
    /// </summary>
    [Fact]
    public void Consumer_CanReference_ProducerWithMultipleCodeBlocks()
    {
        var producerSource = @"
@using Microsoft.AspNetCore.Components;

@code {
    [Parameter] public RenderFragment Header => @<h1>head</h1>;
}

<p>middle</p>

@code {
    [Parameter] public RenderFragment Footer => @<footer>foot</footer>;
}";
        var baseCompilation = BuildBaseCompilationWithProducerRef(producerSource, "ChildComp");

        var consumerResult = CompileToCSharp(
            cshtmlContent: @"<ChildComp />",
            baseCompilation: baseCompilation);

        var producerResult = CompileToCSharp(
            cshtmlRelativePath: "/ChildComp.cshtml",
            cshtmlContent: producerSource);
        var combined = (producerResult.DeclCode ?? "") + "\n" + producerResult.Code;
        Assert.True(consumerResult.Code.Contains("OpenComponent"),
            $"OpenComponent missing. Decl: {producerResult.DeclCode}\n---\nImpl: {producerResult.Code}\n---\nConsumer: {consumerResult.Code}");
        Assert.True(
            (combined.Contains("OpenElement") && combined.Contains("h1")) || combined.Contains("head"),
            $"Markup body lost. Decl: {producerResult.DeclCode}\n---\nImpl: {producerResult.Code}");
    }

    /// <summary>
    /// User-defined member name that exactly matches the FIRST synth name the
    /// splitter would pick. <c>PickFreshName</c> must pick a suffix so the
    /// generated partial method doesn't collide with the user's method.
    /// </summary>
    [Fact]
    public void Consumer_CanReference_ProducerWithUserDefinedSynthName()
    {
        var producerSource = @"
@using Microsoft.AspNetCore.Components;
@using Microsoft.AspNetCore.Components.Rendering;

@code {
    private void __razor_synth_ChildContentBody(RenderTreeBuilder b) { }
    [Parameter] public RenderFragment ChildContent => @<p>default</p>;
}";
        var baseCompilation = BuildBaseCompilationWithProducerRef(producerSource, "ChildComp");

        var consumerResult = CompileToCSharp(
            cshtmlContent: @"<ChildComp />",
            baseCompilation: baseCompilation);

        var producerResult = CompileToCSharp(
            cshtmlRelativePath: "/ChildComp.cshtml",
            cshtmlContent: producerSource);
        var combined = (producerResult.DeclCode ?? "") + "\n" + producerResult.Code;
        Assert.True(consumerResult.Code.Contains("OpenComponent"),
            $"OpenComponent missing. Decl: {producerResult.DeclCode}\n---\nImpl: {producerResult.Code}\n---\nConsumer: {consumerResult.Code}");
        Assert.True(
            combined.Contains("OpenElement") || combined.Contains("<p>default</p>"),
            $"Markup body lost. Decl: {producerResult.DeclCode}\n---\nImpl: {producerResult.Code}");
    }
}
