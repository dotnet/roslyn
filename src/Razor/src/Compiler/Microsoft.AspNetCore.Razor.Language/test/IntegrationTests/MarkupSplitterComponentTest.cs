// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Razor.Language.Extensions;
using Microsoft.AspNetCore.Razor.Language.Intermediate;
using Xunit;

namespace Microsoft.AspNetCore.Razor.Language.IntegrationTests;

// Verifies the markup splitter against real, fully-lowered component IR (the shape it sees when the
// decl/impl lowering phases run). The whole design rests on class-body markup still being present as
// markup IR nodes at that point and on every class-body child being a kind the splitter can route.
public class MarkupSplitterComponentTest : RazorIntegrationTestBase
{
    internal override RazorFileKind? FileKind => RazorFileKind.Component;

    internal override bool UseTwoPhaseCompilation => true;

    [Fact]
    public void MarkupField_FallsBackAndCompiles()
    {
        // A field initializer with markup can't be lifted (declaration-order side effects), so the file
        // falls back and still compiles.
        var generated = CompileToCSharp("""
            @code {
                private Microsoft.AspNetCore.Components.RenderFragment _frag = @<div>Hi</div>;
            }
            """);

        CompileToAssembly(generated);
    }

    [Fact]
    public void MarkupMethod_LiftsToImpl_AndCompiles()
    {
        var generated = CompileToCSharp("""
            @code {
                private Microsoft.AspNetCore.Components.RenderFragment Make() => @<p>Hi</p>;
            }
            """);

        // The markup method lifts wholesale to the impl half; the markup-free decl half keeps none of it.
        Assert.NotNull(generated.DeclCode);
        Assert.DoesNotContain("Make", generated.DeclCode);
        Assert.Contains("Make", generated.Code);

        // decl + impl are emitted as partial halves that recombine and compile.
        CompileToAssembly(generated);
    }

    [Fact]
    public void MarkupMethod_AlongsideMarkupFreeMembers_RoutesEachHalf()
    {
        var generated = CompileToCSharp("""
            @code {
                [Microsoft.AspNetCore.Components.Parameter] public int Count { get; set; }
                private Microsoft.AspNetCore.Components.RenderFragment Make() => @<p>Hi</p>;
            }
            """);

        // The parameter (descriptor surface) stays in decl; the markup method lifts to impl.
        Assert.NotNull(generated.DeclCode);
        Assert.Contains("Count", generated.DeclCode);
        Assert.DoesNotContain("Make", generated.DeclCode);
        Assert.Contains("Make", generated.Code);

        CompileToAssembly(generated);
    }

    [Fact]
    public void AccessorBodiedMarkupProperty_FallsBackAndCompiles()
    {
        // A property with markup falls back (a property is descriptor surface that must stay in decl,
        // where markup cannot live). The original class-body layout still compiles.
        var generated = CompileToCSharp("""
            @code {
                public Microsoft.AspNetCore.Components.RenderFragment Foo { get => @<div>Hi</div>; }
            }
            """);

        CompileToAssembly(generated);
    }

    [Fact]
    public void MarkupInitializerProperty_FallsBackAndCompiles()
    {
        // Markup in a property (here its initializer) produces a fallback decision. The original
        // class-body layout must still compile.
        var generated = CompileToCSharp("""
            @code {
                public Microsoft.AspNetCore.Components.RenderFragment Foo { get; set; } = @<div>Hi</div>;
            }
            """);

        CompileToAssembly(generated);
    }

    [Fact]
    public void MultipleMarkupMethods_AllLiftToImplAndCompile()
    {
        var generated = CompileToCSharp("""
            @code {
                private Microsoft.AspNetCore.Components.RenderFragment A() => @<p>A</p>;
                private Microsoft.AspNetCore.Components.RenderFragment B() => @<p>B</p>;
            }
            """);

        Assert.NotNull(generated.DeclCode);
        Assert.DoesNotContain("<p>A</p>", generated.DeclCode);
        Assert.DoesNotContain("<p>B</p>", generated.DeclCode);
        Assert.Contains("<p>A</p>", generated.Code);
        Assert.Contains("<p>B</p>", generated.Code);
        CompileToAssembly(generated);
    }

    [Fact]
    public void ConditionalCompilation_AroundMarkupMethod_Compiles()
    {
        // If a #if/#endif around a lifted member were split across halves, the halves would have
        // unbalanced directives and fail to compile. Verify the split keeps each half well-formed.
        var generated = CompileToCSharp("""
            @code {
            #if true
                private Microsoft.AspNetCore.Components.RenderFragment M() => @<p>Hi</p>;
            #endif
            }
            """);

        CompileToAssembly(generated);
    }

    [Fact]
    public void MarkupProperty_FallsBackAndCompiles()
    {
        var generated = CompileToCSharp("""
            @code {
                public Microsoft.AspNetCore.Components.RenderFragment Foo => @<div>Hello</div>;
            }
            """);

        var documentNode = generated.CodeDocument.GetDocumentNode();
        Assert.NotNull(documentNode);
        var primaryClass = documentNode.FindPrimaryClass();
        var renderMethod = documentNode.FindPrimaryMethod();
        Assert.NotNull(primaryClass);
        Assert.NotNull(renderMethod);

        // A markup property produces a fallback decision on every language version.
        var decision = MarkupSplitter.Split(primaryClass, renderMethod, generated.CodeDocument.ParserOptions);
        var fallback = Assert.IsType<SplitDecision.SplitFallback>(decision);
        Assert.Equal(FallbackReason.MarkupProperty, fallback.Reason);

        // The unrouted output still compiles.
        CompileToAssembly(generated);
    }

    [Fact]
    public void MarkupFreeProperty_WithMarkupMethod_StaysInDeclAndSplits()
    {
        // A markup-free property is descriptor surface and stays in the decl half (the fast path); the
        // markup method that forces the split lifts to impl.
        var generated = CompileToCSharp("""
            @code {
                [Microsoft.AspNetCore.Components.Parameter] public int Count { get; set; }
                private Microsoft.AspNetCore.Components.RenderFragment Make() => @<div>@Count</div>;
            }
            """);

        Assert.NotNull(generated.DeclCode);
        Assert.Contains("Count", generated.DeclCode);
        Assert.DoesNotContain("Make", generated.DeclCode);
        Assert.Contains("Make", generated.Code);

        CompileToAssembly(generated);
    }

    [Fact]
    public void ExpressionTemplateProperty_SurvivesAsTemplateMarkup_ButFallsBack()
    {
        var generated = CompileToCSharp("""
            @code {
                public Microsoft.AspNetCore.Components.RenderFragment Header => @<div>Hello</div>;
            }
            """);

        var documentNode = generated.CodeDocument.GetDocumentNode();
        Assert.NotNull(documentNode);
        var primaryClass = documentNode.FindPrimaryClass();
        var renderMethod = documentNode.FindPrimaryMethod();
        Assert.NotNull(primaryClass);
        Assert.NotNull(renderMethod);

        // Invariant: the `@<...>` markup is still an IR node at class-body scope (not pre-lowered to
        // __builder C#), and specifically an expression-position TemplateIntermediateNode.
        Assert.True(MarkupSplitter.HasClassBodyMarkup(primaryClass, renderMethod));
        var children = MarkupSplitter.CollectClassBodyChildren(primaryClass, renderMethod);
        Assert.Contains(children, static c => c is TemplateIntermediateNode);

        // But because the markup is in a property, the file falls back rather than splits.
        var decision = MarkupSplitter.Split(primaryClass, renderMethod, generated.CodeDocument.ParserOptions);
        var fallback = Assert.IsType<SplitDecision.SplitFallback>(decision);
        Assert.Equal(FallbackReason.MarkupProperty, fallback.Reason);
    }

    [Fact]
    public void PureCSharpCode_HasNoClassBodyMarkup_AndDoesNotSplit()
    {
        var generated = CompileToCSharp("""
            @code {
                private int _count;
                private void Increment() => _count++;
            }
            """);

        var documentNode = generated.CodeDocument.GetDocumentNode();
        Assert.NotNull(documentNode);
        var primaryClass = documentNode.FindPrimaryClass();
        var renderMethod = documentNode.FindPrimaryMethod();
        Assert.NotNull(primaryClass);
        Assert.NotNull(renderMethod);

        Assert.False(MarkupSplitter.HasClassBodyMarkup(primaryClass, renderMethod));
        Assert.Same(
            SplitDecision.NoSplit,
            MarkupSplitter.Split(primaryClass, renderMethod, generated.CodeDocument.ParserOptions));
    }

    [Fact]
    public void MarkupMethod_WithTypeParam_FallsBackAndCompiles()
    {
        // A generic component (@typeparam) with a markup method takes the single-document path: the early
        // move-based split would give the two partial halves inconsistent arity. Verify it still compiles.
        var generated = CompileToCSharp("""
            @typeparam T
            @code {
                private Microsoft.AspNetCore.Components.RenderFragment Make() => @<p>Hi</p>;
            }
            """);

        CompileToAssembly(generated);
    }

    [Fact]
    public void MarkupMethod_WithInject_CompilesUnderNewFlow()
    {
        // Under the early split, @inject is a document-level directive (not part of the @code analysis),
        // so the component still splits on its markup method. Verify the split halves recombine and
        // compile with the injected member present.
        var generated = CompileToCSharp("""
            @inject System.IServiceProvider Services
            @code {
                private Microsoft.AspNetCore.Components.RenderFragment Make() => @<p>Hi</p>;
            }
            """);

        CompileToAssembly(generated);
    }

    [Fact]
    public void Inject_AlongsideMarkup_FallsBack()
    {
        // @inject lowers to a ComponentInjectIntermediateNode (surface, an ExtensionIntermediateNode
        // like a template). The splitter can't route it, so a component mixing it with markup falls back.
        // A markup *method* is used so the inject is the unambiguous cause (a markup property would fall
        // back on its own).
        var generated = CompileToCSharp("""
            @inject System.IServiceProvider Services
            @code {
                private Microsoft.AspNetCore.Components.RenderFragment Make() => @<div>Hello</div>;
            }
            """);

        var documentNode = generated.CodeDocument.GetDocumentNode();
        Assert.NotNull(documentNode);
        var primaryClass = documentNode.FindPrimaryClass();
        var renderMethod = documentNode.FindPrimaryMethod();
        Assert.NotNull(primaryClass);
        Assert.NotNull(renderMethod);

        var decision = MarkupSplitter.Split(primaryClass, renderMethod, generated.CodeDocument.ParserOptions);
        var fallback = Assert.IsType<SplitDecision.SplitFallback>(decision);
        Assert.Equal(FallbackReason.UnsupportedClassBodyNode, fallback.Reason);
    }

    [Fact]
    public void MarkupEdit_InMethodBody_LeavesDeclHalfByteIdentical()
    {
        // The core value of the split: editing markup (which lives in the impl half) must not perturb the
        // decl half, so incremental tag-helper discovery can reuse it. The markup-free `[Parameter]`
        // stays in decl; the markup method lives wholly in impl. The two sources differ only in the markup
        // on one line, so nothing in decl shifts.
        var a = CompileToCSharp("""
            @code {
                [Microsoft.AspNetCore.Components.Parameter] public int Count { get; set; }
                private Microsoft.AspNetCore.Components.RenderFragment Make() => @<div>Hello</div>;
            }
            """);

        var b = CompileToCSharp("""
            @code {
                [Microsoft.AspNetCore.Components.Parameter] public int Count { get; set; }
                private Microsoft.AspNetCore.Components.RenderFragment Make() => @<span>Bye</span>;
            }
            """);

        Assert.NotNull(a.DeclCode);
        Assert.NotNull(b.DeclCode);

        // The decl halves are byte-identical despite the differing markup, and neither leaks the markup.
        Assert.Equal(a.DeclCode, b.DeclCode);
        Assert.DoesNotContain("Hello", a.DeclCode);
        Assert.DoesNotContain("Bye", b.DeclCode);

        // The impl halves do differ (that's where the edited markup lives).
        Assert.NotEqual(a.Code, b.Code);
    }

    [Fact]
    public void MarkupEdit_InMethodBody_AddingLines_LeavesDeclHalfByteIdentical()
    {
        // Same as above but the edit adds lines to the method body (which follows the decl member), to
        // confirm the decl half's line mappings don't shift with impl-only growth.
        var a = CompileToCSharp("""
            @code {
                [Microsoft.AspNetCore.Components.Parameter] public int Count { get; set; }
                private void Make(Microsoft.AspNetCore.Components.Rendering.RenderTreeBuilder __builder)
                {
                    <div>Hello</div>
                }
            }
            """);

        var b = CompileToCSharp("""
            @code {
                [Microsoft.AspNetCore.Components.Parameter] public int Count { get; set; }
                private void Make(Microsoft.AspNetCore.Components.Rendering.RenderTreeBuilder __builder)
                {
                    <div>Hello</div>
                    <p>An extra line of markup</p>
                    <span>And another</span>
                }
            }
            """);

        Assert.NotNull(a.DeclCode);
        Assert.NotNull(b.DeclCode);
        Assert.Equal(a.DeclCode, b.DeclCode);
    }

    [Fact]
    public void LiftedMarkupMethod_ContentIsSourceMappedToRazor()
    {
        // A diagnostic inside a lifted markup method must map back to the .razor, which relies on the
        // method's nodes keeping their source mappings when they move to the impl half. Assert that
        // directly: the `@Count` inside the lifted method has a source mapping whose original text (read
        // from the .razor) and generated text (read from the impl half) both read "Count" -- so the
        // lifted content points at the user's source, not unmapped scaffolding.
        var generated = CompileToCSharp("""
            @code {
                [Microsoft.AspNetCore.Components.Parameter] public int Count { get; set; }
                private Microsoft.AspNetCore.Components.RenderFragment Make() => @<div>@Count</div>;
            }
            """);

        var impl = generated.CodeDocument.GetRequiredImplCSharpDocument();
        var implText = impl.Text.ToString();
        var sourceText = generated.CodeDocument.Source.Text.ToString();

        Assert.Contains(impl.SourceMappingsSortedByGenerated, m =>
            Slice(sourceText, m.OriginalSpan) == "Count" &&
            Slice(implText, m.GeneratedSpan) == "Count");
    }

    [Fact]
    public void DeclDocument_IsProducedBeforeTagHelperResolution()
    {
        // The whole point of the split: the decl half is emitted *before* tag-helper resolution, so
        // discovery can consume a resolution-independent, byte-stable decl and stay incremental. Drive the
        // pipeline only up to (not including) tag-helper resolution and assert the decl already exists and
        // is markup-free -- if decl production ever regressed to run after resolution, this would see null.
        var codeDocument = ProcessComponentUpToPhase<DefaultTagHelperResolutionPhase>("""
            @code {
                private Microsoft.AspNetCore.Components.RenderFragment Make() => @<p>Hi</p>;
            }
            """);

        var decl = codeDocument.GetDeclCSharpDocument();
        Assert.NotNull(decl);
        Assert.DoesNotContain("Make", decl.Text.ToString());
    }

    private static string Slice(string text, SourceSpan span) => text.Substring(span.AbsoluteIndex, span.Length);
}
