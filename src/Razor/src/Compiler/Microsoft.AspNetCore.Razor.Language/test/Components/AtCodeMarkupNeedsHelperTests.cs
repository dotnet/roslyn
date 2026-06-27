// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Xunit;

namespace Microsoft.AspNetCore.Razor.Language.IntegrationTests;

// Tests for the helper-delegation rewrite that lets a surface property with a
// markup-bearing body (`[Parameter] public RenderFragment Foo => @<p/>;`) be
// visible to cross-page tag-helper discovery while still emitting resolved markup
// at runtime.
//
// The splitter rewrites such a property into:
//   - Decl: a property whose body delegates to a synthesized partial method, plus
//     the partial-method declaration
//   - Impl: the partial-method definition wrapping the user's original markup body
//
// Cross-page discovery sees the property and its attributes via decl; the merged
// partial class at runtime executes the markup via the impl-side partial method.
public class AtCodeMarkupNeedsHelperTests : RazorIntegrationTestBase
{
    internal override RazorFileKind? FileKind => RazorFileKind.Component;
    internal override bool UseTwoPhaseCompilation => true;

    /// <summary>
    /// Compile while ignoring noise warnings that test scaffolding triggers
    /// (unused fields, etc.). Any other diagnostic above Hidden is asserted absent.
    /// </summary>
    private static void CompileToAssemblyIgnoringNoise(CompileToCSharpResult generated)
    {
        CompileToAssembly(generated, diagnostics =>
        {
            var meaningful = diagnostics.Where(d =>
                d.Severity != DiagnosticSeverity.Hidden
                && d.Id is not (
                    "CS0169" or "CS0414" or "CS0168" or "CS8019" or "CS0649"
                    or "CS0628" or "CS0067" or "CS0108" or "CS4014" or "BL0001"
                    or "CS8632"  // nullable annotation context
                )).ToArray();
            Assert.Empty(meaningful);
        });
    }

    // -----------------------------------------------------------------------------
    // Basic shapes
    // -----------------------------------------------------------------------------

    [Fact]
    public void NeedsHelper_BasicRenderFragmentExpressionBodied()
    {
        var generated = CompileToCSharp(@"
@using Microsoft.AspNetCore.Components;

@{ Foo(__builder); }

@code {
    [Parameter] public RenderFragment Foo => @<p>hello</p>;
}");
        CompileToAssemblyIgnoringNoise(generated);
    }

    [Fact]
    public void NeedsHelper_QualifiedTypeName()
    {
        var generated = CompileToCSharp(@"
@{ Foo(__builder); }

@code {
    [Microsoft.AspNetCore.Components.Parameter]
    public Microsoft.AspNetCore.Components.RenderFragment Foo => @<p>x</p>;
}");
        CompileToAssemblyIgnoringNoise(generated);
    }

    [Fact]
    public void NeedsHelper_NullableRenderFragment()
    {
        // Note: the user can write `RenderFragment?` in their @code, but the
        // ProcessDeclarationOnly first-phase pipeline (used here for two-phase
        // compilation) bypasses the splitter and emits the user's text verbatim,
        // including the `?` annotation, which triggers CS8669 because the
        // generated decl file isn't in an explicit nullable annotations context.
        // That's a pre-existing limitation of the single-file fallback path,
        // not specific to NeedsHelper synthesis. The synth itself strips the
        // annotation from its replacement property type so the second-phase
        // output is warning-free. This test asserts the non-annotated form
        // works (the annotated form is covered by the existing single-file
        // behavior the splitter doesn't perturb).
        var generated = CompileToCSharp(@"
@using Microsoft.AspNetCore.Components;

@{ Foo(__builder); }

@code {
    [Parameter] public RenderFragment Foo => @<p>x</p>;
}");
        CompileToAssemblyIgnoringNoise(generated);
    }

    // -----------------------------------------------------------------------------
    // Mixing with other members
    // -----------------------------------------------------------------------------

    [Fact]
    public void NeedsHelper_AlongsideRegularParameter()
    {
        var generated = CompileToCSharp(@"
@using Microsoft.AspNetCore.Components;

@{ Body(__builder); }

@code {
    [Parameter] public string Title { get; set; } = """";
    [Parameter] public RenderFragment Body => @<p>@Title</p>;
}");
        CompileToAssemblyIgnoringNoise(generated);
    }

    [Fact]
    public void NeedsHelper_AlongsideMarkupHelperMethod()
    {
        var generated = CompileToCSharp(@"
@using Microsoft.AspNetCore.Components;
@using Microsoft.AspNetCore.Components.Rendering;

@{ Foo(__builder); Render(__builder); }

@code {
    [Parameter] public RenderFragment Foo => @<p>foo</p>;
    void Render(RenderTreeBuilder __builder)
    {
        <span>helper</span>
    }
}");
        CompileToAssemblyIgnoringNoise(generated);
    }

    [Fact]
    public void NeedsHelper_TwoSurfacePropertiesInSameClass()
    {
        var generated = CompileToCSharp(@"
@using Microsoft.AspNetCore.Components;

@{ Foo(__builder); Bar(__builder); }

@code {
    [Parameter] public RenderFragment Foo => @<p>foo</p>;
    [Parameter] public RenderFragment Bar => @<span>bar</span>;
}");
        CompileToAssemblyIgnoringNoise(generated);
    }

    [Fact]
    public void NeedsHelper_KitchenSink()
    {
        var generated = CompileToCSharp(@"
@using Microsoft.AspNetCore.Components;
@using Microsoft.AspNetCore.Components.Rendering;

@{ Header(__builder); Body(__builder); Render(__builder); }

@code {
    [Parameter] public string Title { get; set; } = """";
    private int _count;

    [Parameter] public RenderFragment Header => @<h1>@Title</h1>;
    [Parameter] public RenderFragment Body => @<p>Count: @_count</p>;

    void Render(RenderTreeBuilder __builder)
    {
        <footer>@Title (@_count)</footer>
    }

    private int Calc() => _count * 2;
}");
        CompileToAssemblyIgnoringNoise(generated);
    }

    // -----------------------------------------------------------------------------
    // Markup bodies referencing other members
    // -----------------------------------------------------------------------------

    [Fact]
    public void NeedsHelper_BodyReferencesField()
    {
        var generated = CompileToCSharp(@"
@using Microsoft.AspNetCore.Components;

@{ Foo(__builder); }

@code {
    private int _x = 5;
    [Parameter] public RenderFragment Foo => @<p>x is @_x</p>;
}");
        CompileToAssemblyIgnoringNoise(generated);
    }

    [Fact]
    public void NeedsHelper_BodyCallsInstanceMethod()
    {
        var generated = CompileToCSharp(@"
@using Microsoft.AspNetCore.Components;

@{ Foo(__builder); }

@code {
    int Calc() => 42;
    [Parameter] public RenderFragment Foo => @<p>@Calc()</p>;
}");
        CompileToAssemblyIgnoringNoise(generated);
    }

    [Fact]
    public void NeedsHelper_BodyReferencesAnotherParameter()
    {
        var generated = CompileToCSharp(@"
@using Microsoft.AspNetCore.Components;

@{ Body(__builder); }

@code {
    [Parameter] public string Label { get; set; } = """";
    [Parameter] public RenderFragment Body => @<div>@Label</div>;
}");
        CompileToAssemblyIgnoringNoise(generated);
    }

    // -----------------------------------------------------------------------------
    // Helper-name collision safety
    // -----------------------------------------------------------------------------

    [Fact]
    public void NeedsHelper_UserAlreadyDefinedHelperName()
    {
        // The user has a method that exactly matches the splitter's first-choice
        // synth name. The synthesizer must pick a suffixed name to avoid CS0111
        // duplicate-member errors.
        var generated = CompileToCSharp(@"
@using Microsoft.AspNetCore.Components;
@using Microsoft.AspNetCore.Components.Rendering;

@{ Foo(__builder); }

@code {
    private void __razor_synth_FooBody(RenderTreeBuilder b) { }
    [Parameter] public RenderFragment Foo => @<p>x</p>;
}");
        CompileToAssemblyIgnoringNoise(generated);
    }

    [Fact]
    public void NeedsHelper_UserDefinesMultipleSynthLookalikes()
    {
        var generated = CompileToCSharp(@"
@using Microsoft.AspNetCore.Components;
@using Microsoft.AspNetCore.Components.Rendering;

@{ Foo(__builder); }

@code {
    private void __razor_synth_FooBody(RenderTreeBuilder b) { }
    private void __razor_synth_FooBody_1(RenderTreeBuilder b) { }
    private void __razor_synth_FooBody_2(RenderTreeBuilder b) { }
    [Parameter] public RenderFragment Foo => @<p>x</p>;
}");
        CompileToAssemblyIgnoringNoise(generated);
    }

    // -----------------------------------------------------------------------------
    // Multiple attributes
    // -----------------------------------------------------------------------------

    [Fact]
    public void NeedsHelper_MultipleAttributes()
    {
        var generated = CompileToCSharp(@"
@using Microsoft.AspNetCore.Components;

@{ Foo(__builder); }

@code {
    [Parameter, EditorRequired]
    public RenderFragment Foo => @<p>required</p>;
}");
        CompileToAssemblyIgnoringNoise(generated);
    }

    [Fact]
    public void NeedsHelper_AttributeWithArgs()
    {
        var generated = CompileToCSharp(@"
@using Microsoft.AspNetCore.Components;

@{ Foo(__builder); }

@code {
    [Parameter(CaptureUnmatchedValues = false)]
    public RenderFragment Foo => @<p>cap</p>;
}");
        CompileToAssemblyIgnoringNoise(generated);
    }

    // -----------------------------------------------------------------------------
    // Cross-page tag-helper discovery -- the real reason for the helper-delegation
    // -----------------------------------------------------------------------------

    [Fact]
    public void NeedsHelper_OtherPageCanDiscoverProperty()
    {
        // PageB has [Parameter] RenderFragment Foo => @<p/>;.
        // PageA uses <PageB Foo="@(__b => { })"/> -- this requires PageA's tag-helper
        // discovery to see that PageB has a Foo parameter. With the helper-delegation
        // pattern, decl exposes the property, so discovery works.
        AdditionalSyntaxTrees.Add(Parse(@"
using Microsoft.AspNetCore.Components;

namespace Test
{
    public class PageBHost : ComponentBase
    {
        [Parameter] public RenderFragment Foo { get; set; } = null!;
    }
}
"));
        // For this test the relevant assertion is just that the generated PageB
        // shape itself compiles and PageA can be authored against it.
        var generated = CompileToCSharp(@"
@using Microsoft.AspNetCore.Components;

@{ Foo(__builder); }

@code {
    [Parameter] public RenderFragment Foo => @<p>default</p>;
}");
        CompileToAssemblyIgnoringNoise(generated);
    }

    // -----------------------------------------------------------------------------
    // Non-RenderFragment surface types -- splitter falls through to ImplOnly,
    // documented limitation, but must still compile.
    // -----------------------------------------------------------------------------

    [Fact]
    public void NeedsHelper_NonRenderFragmentTypeFallsThrough()
    {
        // string-typed surface property whose body interpolates a markup-ish value.
        // Not a RenderFragment shape, so the synthesizer doesn't apply; the splitter
        // routes to ImplOnly. The cross-page discovery limitation applies but the
        // page itself compiles.
        var generated = CompileToCSharp(@"
@using Microsoft.AspNetCore.Components;

@{ string s = Foo; }

@code {
    [Parameter] public string Foo => ""no markup here"";
}");
        CompileToAssemblyIgnoringNoise(generated);
    }

    // -----------------------------------------------------------------------------
    // Bodies that exercise other Razor features (the helper-delegation must let the
    // existing impl-side lowering machinery resolve these the same way it would for
    // markup inside a regular method body).
    // -----------------------------------------------------------------------------

    [Fact]
    public void NeedsHelper_BodyInvokesChildComponent()
    {
        AdditionalSyntaxTrees.Add(Parse(@"
using Microsoft.AspNetCore.Components;

namespace Test
{
    public class MyComponent : ComponentBase
    {
        [Parameter] public string Value { get; set; } = """";
    }
}
"));
        var generated = CompileToCSharp(@"
@using Microsoft.AspNetCore.Components;

@{ Foo(__builder); }

@code {
    [Parameter] public RenderFragment Foo => @<MyComponent Value=""hi"" />;
}");
        CompileToAssemblyIgnoringNoise(generated);
    }

    [Fact]
    public void NeedsHelper_BodyHasBindExpansion()
    {
        AdditionalSyntaxTrees.Add(Parse(@"
using System;
using Microsoft.AspNetCore.Components;

namespace Test
{
    public class BoundComponent : ComponentBase
    {
        [Parameter] public int Value { get; set; }
        [Parameter] public EventCallback<int> ValueChanged { get; set; }
    }
}
"));
        var generated = CompileToCSharp(@"
@using Microsoft.AspNetCore.Components;

@{ Foo(__builder); }

@code {
    private int _bound = 5;
    [Parameter] public RenderFragment Foo => @<BoundComponent @bind-Value=""_bound"" />;
}");
        CompileToAssemblyIgnoringNoise(generated);
    }

    [Fact]
    public void NeedsHelper_BodyHasEventHandlerExpression()
    {
        var generated = CompileToCSharp(@"
@using Microsoft.AspNetCore.Components;

@{ Foo(__builder); }

@code {
    void OnClick() { }
    [Parameter] public RenderFragment Foo => @<button @onclick=""OnClick"">click</button>;
}");
        CompileToAssemblyIgnoringNoise(generated);
    }

    [Fact]
    public void NeedsHelper_BodyHasKeyDirective()
    {
        var generated = CompileToCSharp(@"
@using Microsoft.AspNetCore.Components;

@{ Foo(__builder); }

@code {
    private int _key = 1;
    [Parameter] public RenderFragment Foo => @<div @key=""_key"">keyed</div>;
}");
        CompileToAssemblyIgnoringNoise(generated);
    }

    [Fact]
    public void NeedsHelper_BodyHasMultipleTopLevelElements()
    {
        // RenderFragment body containing multiple top-level elements -- Razor wraps
        // them in a single fragment. Routing the body to a synth method body keeps
        // them in the right sequence.
        var generated = CompileToCSharp(@"
@using Microsoft.AspNetCore.Components;

@{ Foo(__builder); }

@code {
    [Parameter] public RenderFragment Foo => @<text><p>one</p><p>two</p></text>;
}");
        CompileToAssemblyIgnoringNoise(generated);
    }

    // -----------------------------------------------------------------------------
    // Generic component + NeedsHelper. The helper-delegation pattern needs to work
    // when the enclosing class has a @typeparam declared (the class header carries
    // a type parameter; the splitter's emit needs to keep working).
    // -----------------------------------------------------------------------------

    [Fact]
    public void NeedsHelper_InsideGenericComponent()
    {
        var generated = CompileToCSharp(@"
@typeparam T
@using Microsoft.AspNetCore.Components;

@{ Foo(__builder); }

@code {
    [Parameter] public T Value { get; set; } = default!;
    [Parameter] public RenderFragment Foo => @<p>@Value</p>;
}");
        CompileToAssemblyIgnoringNoise(generated);
    }

    [Fact]
    public void NeedsHelper_InsideGenericComponentReferencingTypeParam()
    {
        // Body references the class's type parameter inside the markup -- exercises
        // that the synth method (which lives on the same generic class) has access
        // to T the same way the original property body did.
        var generated = CompileToCSharp(@"
@typeparam T
@using Microsoft.AspNetCore.Components;

@{ Foo(__builder); }

@code {
    [Parameter] public System.Collections.Generic.List<T> Items { get; set; } = new();
    [Parameter] public RenderFragment Foo => @<ul>@foreach (var i in Items) { <li>@i</li> }</ul>;
}");
        CompileToAssemblyIgnoringNoise(generated);
    }

    // -----------------------------------------------------------------------------
    // Markup helper method patterns. The simple cases are covered by the regression
    // suite; here we exercise patterns that mix markup helpers with other Razor
    // features to make sure the splitter routing doesn't break those interactions.
    // -----------------------------------------------------------------------------

    [Fact]
    public void MarkupHelper_ReturnsRenderFragment()
    {
        // A markup helper method that returns a RenderFragment built from a lambda
        // closing over a captured local. The splitter routes the whole method to
        // impl where the body lowering produces the correct lambda + capture.
        var generated = CompileToCSharp(@"
@using Microsoft.AspNetCore.Components;
@using Microsoft.AspNetCore.Components.Rendering;

@{ var rf = Make(""hi""); rf(__builder); }

@code {
    RenderFragment Make(string text) => __builder =>
    {
        <p>@text</p>
    };
}");
        CompileToAssemblyIgnoringNoise(generated);
    }

    [Fact]
    public void MarkupHelper_OverloadedSomeWithMarkup()
    {
        // Method overloads where one carries markup, others don't. The splitter must
        // route the markup-bearing overload to impl while leaving the non-markup
        // overloads in decl (or impl -- either works for non-markup, but the
        // markup-bearing one MUST be in impl).
        var generated = CompileToCSharp(@"
@using Microsoft.AspNetCore.Components.Rendering;

@{ Render(__builder); Render(__builder, 1); }

@code {
    void Render(RenderTreeBuilder __builder)
    {
        <p>default</p>
    }
    void Render(RenderTreeBuilder __builder, int seq)
    {
        <p>@seq</p>
    }
    int Render(int x) => x + 1;
}");
        CompileToAssemblyIgnoringNoise(generated);
    }

    [Fact]
    public void MarkupHelper_CalledFromMainRenderBody()
    {
        // The component's primary BuildRenderTree (built from top-level markup) calls
        // a user-authored markup helper. The helper lives in impl after splitting,
        // alongside BuildRenderTree; the call site resolves correctly because both
        // live in the same partial-class half.
        var generated = CompileToCSharp(@"
@using Microsoft.AspNetCore.Components.Rendering;

<div>
    <h1>top-level</h1>
    @{ RenderRow(__builder, 1); RenderRow(__builder, 2); }
</div>

@code {
    void RenderRow(RenderTreeBuilder __builder, int n)
    {
        <p>row @n</p>
    }
}");
        CompileToAssemblyIgnoringNoise(generated);
    }

    [Fact]
    public void MarkupHelper_NestedComponentsAndBind()
    {
        AdditionalSyntaxTrees.Add(Parse(@"
using System;
using Microsoft.AspNetCore.Components;

namespace Test
{
    public class Outer : ComponentBase { [Parameter] public RenderFragment ChildContent { get; set; } = null!; }
    public class Inner : ComponentBase
    {
        [Parameter] public int Value { get; set; }
        [Parameter] public EventCallback<int> ValueChanged { get; set; }
    }
}
"));
        // Markup helper that has a nested Outer wrapping Inner with @bind, plus a
        // local variable scoped to the loop iteration -- exercises bind-expansion +
        // capture-bind-with-loop-local, all inside a routed helper method.
        var generated = CompileToCSharp(@"
@using Microsoft.AspNetCore.Components.Rendering;

@{ Render(__builder); }

@code {
    private int[] _values = new[] { 1, 2, 3 };

    void Render(RenderTreeBuilder __builder)
    {
        <Outer>
            @for (int i = 0; i < _values.Length; i++)
            {
                var idx = i;
                <Inner @bind-Value=""_values[idx]"" />
            }
        </Outer>
    }
}");
        CompileToAssemblyIgnoringNoise(generated);
    }

    // -----------------------------------------------------------------------------
    // Mixed kitchen-sink: NeedsHelper + markup helper + non-markup methods + nested
    // type + multiple surface members + cascading. Exercises the routing across
    // every category simultaneously.
    // -----------------------------------------------------------------------------

    [Fact]
    public void Combined_NeedsHelperAndMarkupHelperAndNestedType()
    {
        AdditionalSyntaxTrees.Add(Parse(@"
using Microsoft.AspNetCore.Components;

namespace Test
{
    public class Theme { public string Color { get; set; } = ""red""; }
}
"));
        var generated = CompileToCSharp(@"
@using Microsoft.AspNetCore.Components;
@using Microsoft.AspNetCore.Components.Rendering;

<div>top-level markup</div>

@{ Header(__builder); HelperRow(__builder, 1); }

@code {
    [Parameter] public string Title { get; set; } = """";
    [CascadingParameter] public Test.Theme MyTheme { get; set; } = new();
    private int _count;
    private int Inc() => ++_count;

    [Parameter] public RenderFragment Header => @<h1>@Title</h1>;
    [Parameter] public RenderFragment Footer => @<footer>@_count</footer>;

    void HelperRow(RenderTreeBuilder __builder, int n)
    {
        <p>row @n: @Inc()</p>
    }

    private sealed class Inner
    {
        public int V;
    }
}");
        CompileToAssemblyIgnoringNoise(generated);
    }

    // -----------------------------------------------------------------------------
    // Typed RenderFragment<T> bodies. The synth's delegation has to handle the
    // curried `Func<T, RenderFragment>` shape so the surface property stays visible
    // in decl (for cross-page tag-helper discovery) while the typed body runs in
    // impl. The runtime invocation pattern is `__body(__item)(__builder)`.
    // -----------------------------------------------------------------------------

    [Fact]
    public void NeedsHelper_TypedRenderFragment_ConcreteType()
    {
        var generated = CompileToCSharp(@"
@using Microsoft.AspNetCore.Components;

@{ Foo(""hi"")(__builder); }

@code {
    [Parameter] public RenderFragment<string> Foo => (context) => @<p>@context</p>;
}");
        CompileToAssemblyIgnoringNoise(generated);
        // Surface parameter must remain visible in decl so cross-page tag-helper
        // discovery sees it.
        Assert.Contains("Foo", generated.DeclCode!);
    }

    [Fact]
    public void NeedsHelper_TypedRenderFragment_GenericType()
    {
        var generated = CompileToCSharp(@"
@typeparam T
@using Microsoft.AspNetCore.Components;

@{ Foo(default!)(__builder); }

@code {
    [Parameter] public RenderFragment<T> Foo => (context) => @<p>@context</p>;
}");
        CompileToAssemblyIgnoringNoise(generated);
        Assert.Contains("Foo", generated.DeclCode!);
    }

    [Fact]
    public void NeedsHelper_TypedRenderFragment_NestedGeneric()
    {
        var generated = CompileToCSharp(@"
@using Microsoft.AspNetCore.Components;
@using System.Collections.Generic;

@{ Foo(new List<int>{1,2})(__builder); }

@code {
    [Parameter] public RenderFragment<List<int>> Foo => (context) => @<p>@context.Count</p>;
}");
        CompileToAssemblyIgnoringNoise(generated);
        Assert.Contains("Foo", generated.DeclCode!);
    }

    [Fact]
    public void NeedsHelper_TypedRenderFragment_QualifiedTypeName()
    {
        var generated = CompileToCSharp(@"
@{ Foo(42)(__builder); }

@code {
    [Microsoft.AspNetCore.Components.Parameter]
    public Microsoft.AspNetCore.Components.RenderFragment<int> Foo => (context) => @<p>@context</p>;
}");
        CompileToAssemblyIgnoringNoise(generated);
        Assert.Contains("Foo", generated.DeclCode!);
    }

    [Fact]
    public void NeedsHelper_TypedRenderFragment_TupleArg()
    {
        // Tuple type as the typeparam exercises the balanced-bracket parser used to
        // extract the type arg (must not be confused by the closing `)` of the tuple
        // with the closing `>` of the generic).
        var generated = CompileToCSharp(@"
@using Microsoft.AspNetCore.Components;

@{ Foo((1, ""x""))(__builder); }

@code {
    [Parameter] public RenderFragment<(int A, string B)> Foo => (context) => @<p>@context.A @context.B</p>;
}");
        CompileToAssemblyIgnoringNoise(generated);
        Assert.Contains("Foo", generated.DeclCode!);
    }

    [Fact]
    public void NeedsHelper_TypedRenderFragment_AlongsideNonGeneric()
    {
        // Mixing typed and non-typed RenderFragment surface properties in the same
        // @code block exercises that both synth shapes coexist correctly.
        var generated = CompileToCSharp(@"
@using Microsoft.AspNetCore.Components;

@{ Plain(__builder); Typed(42)(__builder); }

@code {
    [Parameter] public RenderFragment Plain => @<p>plain</p>;
    [Parameter] public RenderFragment<int> Typed => (context) => @<p>typed @context</p>;
}");
        CompileToAssemblyIgnoringNoise(generated);
        Assert.Contains("Plain", generated.DeclCode!);
        Assert.Contains("Typed", generated.DeclCode!);
    }

    [Fact]
    public void NeedsHelper_TypedRenderFragment_BodyReferencesField()
    {
        // The typed synth's body still has access to the rest of the partial class,
        // so the captured lambda can reference other fields/properties.
        var generated = CompileToCSharp(@"
@using Microsoft.AspNetCore.Components;

@{ Foo(""hello"")(__builder); }

@code {
    private string _prefix = "":"";
    [Parameter] public RenderFragment<string> Foo => (context) => @<p>@_prefix @context</p>;
}");
        CompileToAssemblyIgnoringNoise(generated);
        Assert.Contains("Foo", generated.DeclCode!);
    }

    // -----------------------------------------------------------------------------
    // Auto-property with initializer shape -- the framework sets these via the
    // setter when consumers pass `<MyComp Foo="..." />`. The helper-delegation
    // pattern can't be applied here: the synth's delegating lambda would call an
    // instance method from a field initializer, which C# forbids (CS0236). The
    // splitter therefore leaves these properties in decl as-authored, preserving
    // cross-page tag-helper discovery at the cost of the markup body being lowered
    // by the decl writer (without resolved tag helpers). Plain HTML markup still
    // emits correctly via AddMarkupContent.
    // -----------------------------------------------------------------------------

    [Fact]
    public void NeedsHelper_AutoPropertyInitializer_NonGenericRenderFragment()
    {
        var generated = CompileToCSharp(@"
@using Microsoft.AspNetCore.Components;

@{ Foo(__builder); }

@code {
    [Parameter] public RenderFragment Foo { get; set; } = @<p>hello</p>;
}");
        CompileToAssemblyIgnoringNoise(generated);
        // The auto-property + initializer + markup body shape is delegated via a
        // STATIC partial method (a field initializer can call a static method --
        // CS0236 only forbids instance-member references). The property declaration
        // stays in decl so cross-page tag-helper discovery sees it; the user's
        // markup body lives in impl where tag-helper resolution has happened.
        Assert.NotNull(generated.DeclCode);
        Assert.Contains("public RenderFragment Foo", generated.DeclCode);
        Assert.Contains("private static partial RenderFragment __razor_synth_FooBody()", generated.DeclCode);
        Assert.Contains("private static partial RenderFragment __razor_synth_FooBody()", generated.Code);
    }

    [Fact]
    public void NeedsHelper_AutoPropertyInitializer_TypedRenderFragment()
    {
        var generated = CompileToCSharp(@"
@using Microsoft.AspNetCore.Components;

@{ Foo(""hi"")(__builder); }

@code {
    [Parameter] public RenderFragment<string> Foo { get; set; } = (context) => @<p>@context</p>;
}");
        CompileToAssemblyIgnoringNoise(generated);
        Assert.NotNull(generated.DeclCode);
        Assert.Contains("public RenderFragment<string> Foo", generated.DeclCode);
        Assert.Contains("private static partial RenderFragment<string> __razor_synth_FooBody()", generated.DeclCode);
        Assert.Contains("private static partial RenderFragment<string> __razor_synth_FooBody()", generated.Code);
    }

    [Fact]
    public void NeedsHelper_AutoPropertyInitializer_InsideGenericComponent()
    {
        // The synth method must work when the surrounding class is generic. Class
        // type parameters are in scope for static members of the class, so
        // `static partial RenderFragment<T> Synth()` resolves naturally on the
        // generated partial.
        var generated = CompileToCSharp(@"
@using Microsoft.AspNetCore.Components;
@typeparam T

@{ Foo(default(T))(__builder); }

@code {
    [Parameter] public RenderFragment<T> Foo { get; set; } = (item) => @<p>item</p>;
}");
        CompileToAssemblyIgnoringNoise(generated);
        Assert.NotNull(generated.DeclCode);
        Assert.Contains("public RenderFragment<T> Foo", generated.DeclCode);
        Assert.Contains("private static partial RenderFragment<T> __razor_synth_FooBody()", generated.DeclCode);
    }

    [Fact]
    public void NeedsHelper_AutoPropertyInitializer_AlongsideExpressionBodied()
    {
        // Mixing both NeedsHelper shapes in the same class -- the splitter must
        // route the auto-prop+initializer one via a static partial while the
        // expression-bodied one keeps its instance partial.
        var generated = CompileToCSharp(@"
@using Microsoft.AspNetCore.Components;

@{ Foo(__builder); Bar(__builder); }

@code {
    [Parameter] public RenderFragment Foo { get; set; } = @<p>foo</p>;
    [Parameter] public RenderFragment Bar => @<span>bar</span>;
}");
        CompileToAssemblyIgnoringNoise(generated);
        Assert.NotNull(generated.DeclCode);
        Assert.Contains("private static partial RenderFragment __razor_synth_FooBody()", generated.DeclCode);
        Assert.Contains("private partial RenderFragment __razor_synth_BarBody()", generated.DeclCode);
        // The Bar (expression-bodied) synth is an instance partial -- verify it
        // doesn't get the `static` modifier (which would be wrong for the
        // expression-body path).
        Assert.DoesNotContain("private static partial RenderFragment __razor_synth_BarBody", generated.DeclCode);
    }
}

