// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Xunit;

namespace Microsoft.AspNetCore.Razor.Language.IntegrationTests;

// Adversarial / weird-shape tests for the @code-block per-member splitter. Each test
// here exercises a syntactic shape the splitter has to handle correctly to avoid
// regressing some valid (if unusual) Razor pattern. Inspired by the spike-phase
// fuzz suite that catalogued 148 cases across 12 categories; this file ports the
// most interesting ones into actual Razor source so they exercise the full pipeline
// (parser -> IR -> splitter -> decl/impl phases -> Roslyn compilation).
//
// Convention: every test asserts CompileToAssembly succeeds with no errors. C#
// warnings about unused fields/methods (CS0169 / CS0414 / CS0168 / CS8019) are
// expected -- our tests declare members purely to exercise the splitter's routing
// rather than to use them at runtime -- so the test helper suppresses them.
public class AtCodeWeirdShapesAdversarialTests : RazorIntegrationTestBase
{
    internal override RazorFileKind? FileKind => RazorFileKind.Component;
    internal override bool UseTwoPhaseCompilation => true;

    /// <summary>
    /// Compile the result while ignoring "field/local never used" warnings, which our
    /// test members trigger as a side-effect of declaring structure without using it.
    /// Any other diagnostic above Hidden severity is asserted absent.
    /// </summary>
    private static void CompileToAssemblyIgnoringUnusedWarnings(CompileToCSharpResult generated)
    {
        CompileToAssembly(generated, diagnostics =>
        {
            var meaningful = diagnostics.Where(d =>
                d.Severity != DiagnosticSeverity.Hidden
                && d.Id is not (
                    "CS0169"   // The field is never used
                    or "CS0414"   // The field is assigned but its value is never used
                    or "CS0168"   // The variable is declared but never used
                    or "CS8019"   // Unnecessary using directive
                    or "CS0649"   // Field is never assigned to
                    or "CS0628"   // New protected member declared in sealed class
                    or "CS0067"   // The event is never used
                    or "CS0108"   // Hides inherited member; new keyword
                    or "CS4014"   // Call is not awaited (async helper invoked without await)
                )).ToArray();
            Assert.Empty(meaningful);
        });
    }

    // -----------------------------------------------------------------------------
    // Category A: modifier weirdness on user @code members
    // -----------------------------------------------------------------------------

    [Fact]
    public void Weird_StaticMarkupHelper()
    {
        var generated = CompileToCSharp(@"
@using Microsoft.AspNetCore.Components.Rendering;

@{ Render(__builder, 5); }

@code {
    static void Render(RenderTreeBuilder __builder, int value)
    {
        <i>@value</i>
    }
}");
        CompileToAssemblyIgnoringUnusedWarnings(generated);
    }

    [Fact]
    public void Weird_VirtualMarkupHelper()
    {
        var generated = CompileToCSharp(@"
@using Microsoft.AspNetCore.Components.Rendering;

@{ Render(__builder); }

@code {
    protected virtual void Render(RenderTreeBuilder __builder)
    {
        <p>virtual</p>
    }
}");
        CompileToAssemblyIgnoringUnusedWarnings(generated);
    }

    [Fact]
    public void Weird_OverrideOnInitialized()
    {
        // A markup-FREE override -- shouldn't move from decl to impl. Verifies the
        // splitter doesn't lift members it has no reason to lift.
        var generated = CompileToCSharp(@"
@code {
    protected override void OnInitialized() { base.OnInitialized(); }
}");
        CompileToAssemblyIgnoringUnusedWarnings(generated);
    }

    [Fact]
    public void Weird_AsyncMarkupHelper()
    {
        var generated = CompileToCSharp(@"
@using System.Threading.Tasks;
@using Microsoft.AspNetCore.Components.Rendering;

@{ RenderAsync(__builder); }

@code {
    private async Task RenderAsync(RenderTreeBuilder __builder)
    {
        await Task.Yield();
        <p>after await</p>
    }
}");
        CompileToAssemblyIgnoringUnusedWarnings(generated);
    }

    [Fact]
    public void Weird_GenericMethodWithMarkup()
    {
        var generated = CompileToCSharp(@"
@using Microsoft.AspNetCore.Components.Rendering;

@{ Show(__builder, ""hello""); }

@code {
    void Show<T>(RenderTreeBuilder __builder, T value) where T : notnull
    {
        <span>@value</span>
    }
}");
        CompileToAssemblyIgnoringUnusedWarnings(generated);
    }

    [Fact]
    public void Weird_PartialMethodFromUser()
    {
        // The user themselves writes a partial method declaration. The splitter should
        // treat it as ImplOnly (no markup) and let the C# compiler decide whether the
        // partial pair is well-formed.
        var generated = CompileToCSharp(@"
@code {
    partial void OnHooked();
    partial void OnHooked() { }
}");
        CompileToAssemblyIgnoringUnusedWarnings(generated);
    }

    // -----------------------------------------------------------------------------
    // Category B: attribute weirdness
    // -----------------------------------------------------------------------------

    [Fact]
    public void Weird_MultipleSurfaceAttributes()
    {
        var generated = CompileToCSharp(@"
@code {
    [Parameter, EditorRequired] public string Name { get; set; } = """";
}");
        CompileToAssemblyIgnoringUnusedWarnings(generated);
    }

    [Fact]
    public void Weird_FullyQualifiedParameterAttribute()
    {
        var generated = CompileToCSharp(@"
@code {
    [Microsoft.AspNetCore.Components.Parameter] public int X { get; set; }
}");
        CompileToAssemblyIgnoringUnusedWarnings(generated);
    }

    [Fact]
    public void Weird_CascadingParameter()
    {
        AdditionalSyntaxTrees.Add(Parse(@"
namespace Test
{
    public class Theme { public string Color { get; set; } = ""red""; }
}
"));
        var generated = CompileToCSharp(@"
@code {
    [CascadingParameter] public Test.Theme MyTheme { get; set; } = null!;
}");
        CompileToAssemblyIgnoringUnusedWarnings(generated);
    }

    [Fact]
    public void Weird_AttributeWithFieldTarget()
    {
        var generated = CompileToCSharp(@"
@code {
    [Parameter] [field: System.Obsolete(""old"")] public int X { get; set; }
}");
        CompileToAssemblyIgnoringUnusedWarnings(generated);
    }

    // -----------------------------------------------------------------------------
    // Category C: constructors, operators, indexers, finalizers
    // -----------------------------------------------------------------------------

    [Fact]
    public void Weird_UserDefinedConstructor()
    {
        var generated = CompileToCSharp(@"
@code {
    public TestComponent() { }
    private int _x;
}");
        CompileToAssemblyIgnoringUnusedWarnings(generated);
    }

    [Fact]
    public void Weird_StaticConstructor()
    {
        var generated = CompileToCSharp(@"
@code {
    static TestComponent() { }
}");
        CompileToAssemblyIgnoringUnusedWarnings(generated);
    }

    [Fact]
    public void Weird_IndexerInAtCode()
    {
        var generated = CompileToCSharp(@"
@code {
    public int this[int i] => i * 2;
}");
        CompileToAssemblyIgnoringUnusedWarnings(generated);
    }

    [Fact]
    public void Weird_OperatorOverloadInAtCode()
    {
        var generated = CompileToCSharp(@"
@code {
    public static int operator +(TestComponent a, int b) => b;
}");
        CompileToAssemblyIgnoringUnusedWarnings(generated);
    }

    [Fact]
    public void Weird_FinalizerInAtCode()
    {
        var generated = CompileToCSharp(@"
@code {
    ~TestComponent() { }
}");
        CompileToAssemblyIgnoringUnusedWarnings(generated);
    }

    // -----------------------------------------------------------------------------
    // Category D: markup in nested statement positions
    // -----------------------------------------------------------------------------

    [Fact]
    public void Weird_MarkupInIfElse()
    {
        var generated = CompileToCSharp(@"
@using Microsoft.AspNetCore.Components.Rendering;

@{ Render(__builder, true); }

@code {
    void Render(RenderTreeBuilder __builder, bool flag)
    {
        if (flag)
        {
            <p>yes</p>
        }
        else
        {
            <p>no</p>
        }
    }
}");
        CompileToAssemblyIgnoringUnusedWarnings(generated);
    }

    [Fact]
    public void Weird_MarkupInSwitchCase()
    {
        var generated = CompileToCSharp(@"
@using Microsoft.AspNetCore.Components.Rendering;

@{ Render(__builder, 1); }

@code {
    void Render(RenderTreeBuilder __builder, int x)
    {
        switch (x)
        {
            case 1:
                <p>one</p>
                break;
            case 2:
                <p>two</p>
                break;
            default:
                <p>other</p>
                break;
        }
    }
}");
        CompileToAssemblyIgnoringUnusedWarnings(generated);
    }

    [Fact]
    public void Weird_MarkupInTryCatchFinally()
    {
        var generated = CompileToCSharp(@"
@using Microsoft.AspNetCore.Components.Rendering;

@{ Render(__builder); }

@code {
    void Render(RenderTreeBuilder __builder)
    {
        try
        {
            <p>try</p>
        }
        catch
        {
            <p>catch</p>
        }
        finally
        {
            <p>finally</p>
        }
    }
}");
        CompileToAssemblyIgnoringUnusedWarnings(generated);
    }

    [Fact]
    public void Weird_MarkupInUsingBlock()
    {
        var generated = CompileToCSharp(@"
@using System;
@using Microsoft.AspNetCore.Components.Rendering;

@{ Render(__builder); }

@code {
    void Render(RenderTreeBuilder __builder)
    {
        using (var _ = (IDisposable)null!)
        {
            <p>scoped</p>
        }
    }
}");
        CompileToAssemblyIgnoringUnusedWarnings(generated);
    }

    [Fact]
    public void Weird_MarkupInLocalFunction()
    {
        // Markup inside a local function inside a method. The splitter sees the OUTER
        // method as containing markup (the placeholder is inside the local function but
        // textually inside the outer method).
        var generated = CompileToCSharp(@"
@using Microsoft.AspNetCore.Components.Rendering;

@{ Render(__builder); }

@code {
    void Render(RenderTreeBuilder __builder)
    {
        void Inner()
        {
            <p>local function</p>
        }
        Inner();
    }
}");
        CompileToAssemblyIgnoringUnusedWarnings(generated);
    }

    [Fact]
    public void Weird_MarkupInDeeplyNestedForLoops()
    {
        var generated = CompileToCSharp(@"
@using Microsoft.AspNetCore.Components.Rendering;

@{ Render(__builder); }

@code {
    void Render(RenderTreeBuilder __builder)
    {
        for (int i = 0; i < 2; i++)
        {
            for (int j = 0; j < 2; j++)
            {
                for (int k = 0; k < 2; k++)
                {
                    <span>@i @j @k</span>
                }
            }
        }
    }
}");
        CompileToAssemblyIgnoringUnusedWarnings(generated);
    }

    [Fact]
    public void Weird_MultipleMarkupChunksInSameMethod()
    {
        var generated = CompileToCSharp(@"
@using Microsoft.AspNetCore.Components.Rendering;

@{ Render(__builder); }

@code {
    void Render(RenderTreeBuilder __builder)
    {
        <h1>first</h1>
        int x = 1;
        <h2>second</h2>
        if (x > 0)
        {
            <h3>third</h3>
        }
    }
}");
        CompileToAssemblyIgnoringUnusedWarnings(generated);
    }

    [Fact]
    public void Weird_MarkupOnlyBody()
    {
        var generated = CompileToCSharp(@"
@using Microsoft.AspNetCore.Components.Rendering;

@{ Render(__builder); }

@code {
    void Render(RenderTreeBuilder __builder)
    {
        <p>solo</p>
    }
}");
        CompileToAssemblyIgnoringUnusedWarnings(generated);
    }

    // -----------------------------------------------------------------------------
    // Category E: generics / ref / out / in / params
    // -----------------------------------------------------------------------------

    [Fact]
    public void Weird_RefAndOutParameters()
    {
        var generated = CompileToCSharp(@"
@code {
    void Inc(ref int x) => x++;
    void Make(out int x) => x = 5;
}");
        CompileToAssemblyIgnoringUnusedWarnings(generated);
    }

    [Fact]
    public void Weird_ParamsArray()
    {
        var generated = CompileToCSharp(@"
@code {
    int Total(params int[] xs) { int s = 0; foreach (var x in xs) s += x; return s; }
}");
        CompileToAssemblyIgnoringUnusedWarnings(generated);
    }

    [Fact]
    public void Weird_TupleReturn()
    {
        var generated = CompileToCSharp(@"
@code {
    (int X, int Y) GetCoords() => (1, 2);
}");
        CompileToAssemblyIgnoringUnusedWarnings(generated);
    }

    // -----------------------------------------------------------------------------
    // Category F: identifier weirdness
    // -----------------------------------------------------------------------------

    [Fact]
    public void Weird_VerbatimIdentifiers()
    {
        var generated = CompileToCSharp(@"
@code {
    private string @class = ""btn"";
    private int @new = 1;
}");
        CompileToAssemblyIgnoringUnusedWarnings(generated);
    }

    [Fact]
    public void Weird_MethodOverloads()
    {
        var generated = CompileToCSharp(@"
@code {
    void Do() { }
    void Do(int x) { }
    void Do(string s) { }
}");
        CompileToAssemblyIgnoringUnusedWarnings(generated);
    }

    [Fact]
    public void Weird_NameofReference()
    {
        var generated = CompileToCSharp(@"
@code {
    private string SelfName = nameof(TestComponent);
}");
        CompileToAssemblyIgnoringUnusedWarnings(generated);
    }

    [Fact]
    public void Weird_UserDefinesPlaceholderLookalike()
    {
        // The user happens to define a method whose name resembles the splitter's
        // synthesized placeholder identifier. With a real markup region also present,
        // the splitter's classifier sees both. They should not collide.
        var generated = CompileToCSharp(@"
@using Microsoft.AspNetCore.Components.Rendering;

@{ Render(__builder); _ = MyHelper(); }

@code {
    private int MyHelper() => 42;
    private int __razor_markup_999__() => 1;
    void Render(RenderTreeBuilder __builder)
    {
        <p>x</p>
    }
}");
        CompileToAssemblyIgnoringUnusedWarnings(generated);
    }

    // -----------------------------------------------------------------------------
    // Category G: preprocessor directives
    // -----------------------------------------------------------------------------

    [Fact]
    public void Weird_PragmaWarningDisable()
    {
        var generated = CompileToCSharp(@"
@code {
#pragma warning disable CS0414
    private int _unused = 0;
#pragma warning restore CS0414
}");
        CompileToAssemblyIgnoringUnusedWarnings(generated);
    }

    [Fact]
    public void Weird_RegionEndregion()
    {
        var generated = CompileToCSharp(@"
@code {
#region Members
    private int _x;
    private int _y;
#endregion
}");
        CompileToAssemblyIgnoringUnusedWarnings(generated);
    }

    [Fact]
    public void Weird_NullableDirective()
    {
        var generated = CompileToCSharp(@"
@code {
#nullable enable
    private string Name = """";
#nullable restore
}");
        CompileToAssemblyIgnoringUnusedWarnings(generated);
    }

    // -----------------------------------------------------------------------------
    // Category H: nested types
    // -----------------------------------------------------------------------------

    [Fact]
    public void Weird_NestedClass()
    {
        var generated = CompileToCSharp(@"
@code {
    private sealed class Inner
    {
        public int Value { get; set; }
    }
}");
        CompileToAssemblyIgnoringUnusedWarnings(generated);
    }

    [Fact]
    public void Weird_NestedStruct()
    {
        var generated = CompileToCSharp(@"
@code {
    private struct Pair { public int A; public int B; }
}");
        CompileToAssemblyIgnoringUnusedWarnings(generated);
    }

    [Fact]
    public void Weird_NestedEnum()
    {
        var generated = CompileToCSharp(@"
@code {
    private enum Color { Red, Green, Blue }
}");
        CompileToAssemblyIgnoringUnusedWarnings(generated);
    }

    [Fact]
    public void Weird_NestedRecord()
    {
        var generated = CompileToCSharp(@"
@code {
    private sealed record Point(int X, int Y);
}");
        CompileToAssemblyIgnoringUnusedWarnings(generated);
    }

    // -----------------------------------------------------------------------------
    // Category I: async, yield, unsafe
    // -----------------------------------------------------------------------------

    [Fact]
    public void Weird_Iterator()
    {
        var generated = CompileToCSharp(@"
@using System.Collections.Generic;

@code {
    private IEnumerable<int> Range() { for (int i = 0; i < 3; i++) yield return i; }
}");
        CompileToAssemblyIgnoringUnusedWarnings(generated);
    }

    [Fact]
    public void Weird_AsyncIterator()
    {
        var generated = CompileToCSharp(@"
@using System.Collections.Generic;
@using System.Threading.Tasks;

@code {
    private async IAsyncEnumerable<int> ARange()
    {
        await Task.Yield();
        yield return 1;
    }
}");
        CompileToAssemblyIgnoringUnusedWarnings(generated);
    }

    // -----------------------------------------------------------------------------
    // Category J: markup-like text in strings/comments
    // -----------------------------------------------------------------------------

    [Fact]
    public void Weird_HtmlLookalikeInString()
    {
        // A string literal that contains HTML-looking text and a real markup region.
        // The splitter must not confuse the string for markup, and must not corrupt
        // the user's string when substituting placeholders.
        var generated = CompileToCSharp(@"
@using Microsoft.AspNetCore.Components.Rendering;

@{ Render(__builder); }

@code {
    private string TemplateText = ""<p>not really markup</p>"";
    void Render(RenderTreeBuilder __builder)
    {
        <span>real markup here</span>
    }
}");
        CompileToAssemblyIgnoringUnusedWarnings(generated);
    }

    [Fact]
    public void Weird_StringWithBraces()
    {
        // String literals with unmatched braces. Roslyn handles correctly even though
        // a naive lexer might trip up.
        var generated = CompileToCSharp(@"
@code {
    private string _s1 = ""a { b } c { d } e"";
    private string _s2 = ""}}}"";
    private string _s3 = ""{{{ "";
}");
        CompileToAssemblyIgnoringUnusedWarnings(generated);
    }

    [Fact]
    public void Weird_InterpolatedStringWithEscapedBraces()
    {
        var generated = CompileToCSharp(@"
@code {
    private int _n = 42;
    private string _s = $""verbatim {{ and {1 + 2} and {{n}}"";
}");
        CompileToAssemblyIgnoringUnusedWarnings(generated);
    }

    [Fact]
    public void Weird_PlaceholderLookalikeInComment()
    {
        // The user's comment mentions a string that resembles the splitter's
        // generated placeholder identifier. Combined with a real markup region in a
        // sibling method, this exercises the case where naive substring substitution
        // would corrupt the comment text.
        var generated = CompileToCSharp(@"
@using Microsoft.AspNetCore.Components.Rendering;

@{ Render(__builder); }

@code {
    // __razor_markup_0__() looks like a placeholder but it's just a comment
    private int _x = 0;
    void Render(RenderTreeBuilder __builder)
    {
        <p>real markup</p>
    }
}");
        CompileToAssemblyIgnoringUnusedWarnings(generated);
    }

    // -----------------------------------------------------------------------------
    // Category K: mixed shapes (params + markup helpers + everything)
    // -----------------------------------------------------------------------------

    [Fact]
    public void Weird_KitchenSink()
    {
        AdditionalSyntaxTrees.Add(Parse(@"
namespace Test
{
    public interface ILogger { void Log(string s); }
    public class NullLogger : ILogger { public void Log(string s) { } }
}
"));

        var generated = CompileToCSharp(@"
@using System;
@using System.Collections.Generic;
@using Microsoft.AspNetCore.Components.Rendering;

@{ RenderHeader(__builder); RenderFooter(__builder); }

@code {
    [Parameter] public string Title { get; set; } = ""default"";
    [Parameter] public int Count { get; set; }
    [CascadingParameter] public Test.ILogger Logger { get; set; } = null!;
    [Inject] public IServiceProvider Services { get; set; } = null!;

    private int _hits;
    private readonly List<int> _items = new() { 1, 2, 3 };
    private int Calc() => _hits * 2;

    void RenderHeader(RenderTreeBuilder __builder)
    {
        <h1>@Title (@Count)</h1>
    }
    void RenderFooter(RenderTreeBuilder __builder)
    {
        <footer>hits: @_hits, calc: @Calc()</footer>
    }

    private sealed class Helper { public int V; }
    private enum Mode { On, Off }
}");
        CompileToAssemblyIgnoringUnusedWarnings(generated);
    }

    [Fact]
    public void Weird_OnlyDirectivesNoCodeContent()
    {
        // A component with only directives (no @code at all) -- verifies the splitter
        // doesn't get confused by a class body containing nothing user-authored.
        var generated = CompileToCSharp(@"
@inject System.IServiceProvider Services

<p>just markup</p>
");
        CompileToAssemblyIgnoringUnusedWarnings(generated);
    }

    [Fact]
    public void Weird_InjectPlusMarkupBearingCode()
    {
        // An @inject directive emits a class-body property but isn't user @code text, so
        // the splitter excludes it from routing. When the same component also has
        // markup-bearing @code that drives a split, the injected property must still be
        // emitted -- it belongs in decl, alongside every other non-markup member. A
        // regression here drops the property from both partial halves and the component
        // fails to compile.
        //
        // DescribeService() is a plain (non-markup) method: it routes DeclOnly and stays
        // in decl. Because it references the injected Services, the injected property MUST
        // be in decl too -- otherwise the decl half wouldn't compile. This is the concrete
        // reason the @inject property belongs in decl, not impl.
        var generated = CompileToCSharp(@"
@inject System.IServiceProvider Services

@code {
    [Parameter]
    public RenderFragment Template { get; set; } = @<p>markup in initializer</p>;

    public string DescribeService() => Services.GetType().Name;
}

<button onclick=""@(() => Services.GetType())"">Use the injected service</button>
");
        // The injected property lives in the decl half (the API-surface document) and
        // not in impl -- it must appear in exactly one half of the partial class. Line
        // pragmas interleave the type and member names, so assert on the @inject marker
        // attribute and the service type rather than a contiguous declaration.
        var decl = generated.CodeDocument.GetDeclCSharpDocument();
        Assert.NotNull(decl);
        var declText = decl.Text.ToString();
        Assert.Contains("InjectAttribute", declText);
        Assert.Contains("IServiceProvider", declText);
        // The DeclOnly method that consumes the injected service also lives in decl.
        Assert.Contains("DescribeService", declText);

        var impl = generated.CodeDocument.GetImplCSharpDocument();
        Assert.NotNull(impl);
        Assert.DoesNotContain("InjectAttribute", impl.Text.ToString());

        CompileToAssemblyIgnoringUnusedWarnings(generated);
    }

    [Fact]
    public void Weird_EmptyAtCode()
    {
        var generated = CompileToCSharp(@"
@code {
}

<p>markup outside @@code</p>
");
        CompileToAssemblyIgnoringUnusedWarnings(generated);
    }

    [Fact]
    public void Weird_AtCodeWithOnlyWhitespace()
    {
        var generated = CompileToCSharp(@"
@code {



}

<p>markup outside</p>
");
        CompileToAssemblyIgnoringUnusedWarnings(generated);
    }

    [Fact]
    public void Weird_MultipleAtCodeBlocks()
    {
        var generated = CompileToCSharp(@"
@code {
    [Parameter] public string A { get; set; } = """";
}

<p>between</p>

@code {
    [Parameter] public string B { get; set; } = """";
    void Helper(Microsoft.AspNetCore.Components.Rendering.RenderTreeBuilder __builder)
    {
        <span>@B</span>
    }
}");
        CompileToAssemblyIgnoringUnusedWarnings(generated);
    }

    [Fact]
    public void Weird_PageDirectiveAndMarkupInCode()
    {
        var generated = CompileToCSharp(@"
@page ""/test""

@{ Helper(__builder); }

@code {
    void Helper(Microsoft.AspNetCore.Components.Rendering.RenderTreeBuilder __builder)
    {
        <p>page+helper</p>
    }
}");
        CompileToAssemblyIgnoringUnusedWarnings(generated);
    }

    [Fact]
    public void Weird_InheritsDirectiveAndMarkupInCode()
    {
        AdditionalSyntaxTrees.Add(Parse(@"
using Microsoft.AspNetCore.Components;

namespace Test
{
    public class CustomBase : ComponentBase { }
}
"));
        var generated = CompileToCSharp(@"
@inherits Test.CustomBase

@{ Helper(__builder); }

@code {
    void Helper(Microsoft.AspNetCore.Components.Rendering.RenderTreeBuilder __builder)
    {
        <p>inherits+helper</p>
    }
}");
        CompileToAssemblyIgnoringUnusedWarnings(generated);
    }

    [Fact]
    public void Weird_AttributeDirective()
    {
        var generated = CompileToCSharp(@"
@attribute [System.Obsolete(""old"")]

@{ Helper(__builder); }

@code {
    void Helper(Microsoft.AspNetCore.Components.Rendering.RenderTreeBuilder __builder)
    {
        <p>attr+helper</p>
    }
}");
        CompileToAssemblyIgnoringUnusedWarnings(generated);
    }

    [Fact]
    public void Weird_AtBindEventAndMarkupHelper()
    {
        // @bind expansion generates a CSharpExpression lambda. Combined with a
        // markup-helper user method, both routings need to coexist.
        AdditionalSyntaxTrees.Add(Parse(@"
using System;
using Microsoft.AspNetCore.Components;

namespace Test
{
    public class MyComponent : ComponentBase
    {
        [Parameter] public int Value { get; set; }
        [Parameter] public EventCallback<int> ValueChanged { get; set; }
    }
}
"));
        var generated = CompileToCSharp(@"
@using Microsoft.AspNetCore.Components.Rendering;

@{ Render(__builder); }

@code {
    private int _value = 5;
    void Render(RenderTreeBuilder __builder)
    {
        <MyComponent @bind-Value=""_value"" />
    }
}");
        CompileToAssemblyIgnoringUnusedWarnings(generated);
    }

    [Fact]
    public void Weird_AtPageWithBindAndHelperAndParameter()
    {
        // The combination test: @page + @bind + helper method + parameter property.
        AdditionalSyntaxTrees.Add(Parse(@"
using System;
using Microsoft.AspNetCore.Components;

namespace Test
{
    public class MyComponent : ComponentBase
    {
        [Parameter] public int Value { get; set; }
        [Parameter] public EventCallback<int> ValueChanged { get; set; }
    }
}
"));
        var generated = CompileToCSharp(@"
@page ""/everything""
@using Microsoft.AspNetCore.Components.Rendering;

@{ Render(__builder); }

@code {
    [Parameter] public string Caption { get; set; } = ""Hello"";
    private int _selected = 1;

    void Render(RenderTreeBuilder __builder)
    {
        <div>@Caption</div>
        <MyComponent @bind-Value=""_selected"" />
    }
}");
        CompileToAssemblyIgnoringUnusedWarnings(generated);
    }

    // -----------------------------------------------------------------------------
    // Category L: extra-adversarial whiteboxed against the splitter's substitution
    // -----------------------------------------------------------------------------

    [Fact]
    public void Weird_UserLocalInsideSameMethodMatchingPlaceholderPattern()
    {
        // The splitter substitutes markup chunks with `__razor_markup_N__()` placeholders.
        // Here the user has a local variable name that pattern-matches the substitution
        // shape AND there's a real markup chunk in the same method. The substitution
        // must not collide with the user's local.
        var generated = CompileToCSharp(@"
@using Microsoft.AspNetCore.Components.Rendering;

@{ Render(__builder); }

@code {
    void Render(RenderTreeBuilder __builder)
    {
        int __razor_markup_999__ = 42;
        <p>before local: @__razor_markup_999__</p>
        var _ = __razor_markup_999__;
    }
}");
        CompileToAssemblyIgnoringUnusedWarnings(generated);
    }

    [Fact]
    public void Weird_HtmlEntityAndAmpInMarkup()
    {
        // HTML entities and bare ampersands in markup body.
        var generated = CompileToCSharp(@"
@using Microsoft.AspNetCore.Components.Rendering;

@{ Render(__builder); }

@code {
    void Render(RenderTreeBuilder __builder)
    {
        <p>&amp; &lt; &gt; &quot;</p>
        <span title=""hello"">A &amp; B</span>
    }
}");
        CompileToAssemblyIgnoringUnusedWarnings(generated);
    }

    [Fact]
    public void Weird_RawStringLiteralAtCode()
    {
        // C# 11 raw string literals inside @code. Using a triple-quoted raw string
        // with embedded triple-quote sequences would need careful escaping in our outer
        // @" verbatim string, so we use a simpler single-line raw literal here.
        var generated = CompileToCSharp("\n@using Microsoft.AspNetCore.Components.Rendering;\n\n@{ Render(__builder); }\n\n@code {\n    private string _raw = \"\"\"hello raw\"\"\";\n    void Render(RenderTreeBuilder __builder)\n    {\n        <p>@_raw</p>\n    }\n}\n");
        CompileToAssemblyIgnoringUnusedWarnings(generated);
    }

    [Fact]
    public void Weird_DeeplyNestedConditionals()
    {
        var generated = CompileToCSharp(@"
@using Microsoft.AspNetCore.Components.Rendering;

@{ Render(__builder, 5); }

@code {
    void Render(RenderTreeBuilder __builder, int x)
    {
        if (x > 0)
        {
            if (x > 5)
            {
                if (x > 10)
                {
                    <p>big</p>
                }
                else
                {
                    <p>medium</p>
                }
            }
            else
            {
                <p>small positive</p>
            }
        }
        else if (x == 0)
        {
            <p>zero</p>
        }
        else
        {
            <p>negative</p>
        }
    }
}");
        CompileToAssemblyIgnoringUnusedWarnings(generated);
    }

    [Fact]
    public void Weird_TupleDeconstruction()
    {
        var generated = CompileToCSharp(@"
@using Microsoft.AspNetCore.Components.Rendering;

@{ Render(__builder); }

@code {
    void Render(RenderTreeBuilder __builder)
    {
        var (a, b) = (1, 2);
        <p>@a @b</p>
    }
}");
        CompileToAssemblyIgnoringUnusedWarnings(generated);
    }

    [Fact]
    public void Weird_PatternMatchingSwitchInMethod()
    {
        var generated = CompileToCSharp(@"
@using Microsoft.AspNetCore.Components.Rendering;

@{ Render(__builder, ""hello""); }

@code {
    void Render(RenderTreeBuilder __builder, object o)
    {
        switch (o)
        {
            case string s when s.Length > 3:
                <p>long string: @s</p>
                break;
            case int i:
                <p>int: @i</p>
                break;
            case null:
                <p>null</p>
                break;
            default:
                <p>other</p>
                break;
        }
    }
}");
        CompileToAssemblyIgnoringUnusedWarnings(generated);
    }

    [Fact]
    public void Weird_ConditionalAttributeMethod()
    {
        // [Conditional("DEBUG")] -- C# call-site elimination attribute.
        var generated = CompileToCSharp(@"
@code {
    [System.Diagnostics.Conditional(""DEBUG"")]
    private void DebugLog(string s) { }
}");
        CompileToAssemblyIgnoringUnusedWarnings(generated);
    }

    [Fact]
    public void Weird_EqualsAndGetHashCodeOverrides()
    {
        // Use `object` rather than `object?` -- the latter requires #nullable enable
        // in the user @code block, which is independent of the splitter's behavior.
        var generated = CompileToCSharp(@"
@code {
    public override bool Equals(object other) => true;
    public override int GetHashCode() => 42;
    public override string ToString() => ""x"";
}");
        CompileToAssemblyIgnoringUnusedWarnings(generated);
    }

    [Fact]
    public void Weird_LocalFunctionWithStaticModifier()
    {
        // Static local function -- can't capture enclosing locals. Mixed with markup.
        var generated = CompileToCSharp(@"
@using Microsoft.AspNetCore.Components.Rendering;

@{ Render(__builder); }

@code {
    void Render(RenderTreeBuilder __builder)
    {
        static int Add(int a, int b) => a + b;
        int s = Add(1, 2);
        <p>sum: @s</p>
    }
}");
        CompileToAssemblyIgnoringUnusedWarnings(generated);
    }

    [Fact]
    public void Weird_OutDiscardInMethod()
    {
        var generated = CompileToCSharp(@"
@using Microsoft.AspNetCore.Components.Rendering;

@{ Render(__builder); }

@code {
    bool TryParse(string s, out int v) { v = 0; return int.TryParse(s, out v); }
    void Render(RenderTreeBuilder __builder)
    {
        if (TryParse(""123"", out _))
        {
            <p>parsed</p>
        }
    }
}");
        CompileToAssemblyIgnoringUnusedWarnings(generated);
    }

    [Fact]
    public void Weird_LambdaThatReturnsRenderFragment()
    {
        // A field whose value is a RenderFragment lambda assigned inside the user @code.
        // The lambda body itself contains markup transitions.
        AdditionalSyntaxTrees.Add(Parse(@"
namespace Test
{
    public delegate void __PlaceholderForRenderFragment(Microsoft.AspNetCore.Components.Rendering.RenderTreeBuilder b);
}
"));
        var generated = CompileToCSharp(@"
@using Microsoft.AspNetCore.Components;
@using Microsoft.AspNetCore.Components.Rendering;

@{ var rf = MakeFragment(); rf(__builder); }

@code {
    RenderFragment MakeFragment() => (RenderTreeBuilder __builder) =>
    {
        __builder.OpenElement(0, ""p"");
        __builder.AddContent(1, ""hello from lambda"");
        __builder.CloseElement();
    };
}");
        CompileToAssemblyIgnoringUnusedWarnings(generated);
    }

    [Fact]
    public void Weird_GenericClassWithMarkupHelper()
    {
        // A nested generic class inside @code containing a markup helper. Markup inside
        // a nested type's method body is rare but valid.
        var generated = CompileToCSharp(@"
@using Microsoft.AspNetCore.Components.Rendering;

@{ Render(__builder); }

@code {
    void Render(RenderTreeBuilder __builder)
    {
        Inner<int>.WriteValue(__builder, 42);
    }

    private static class Inner<T>
    {
        public static void WriteValue(RenderTreeBuilder __builder, T value)
        {
            __builder.AddContent(0, value);
        }
    }
}");
        CompileToAssemblyIgnoringUnusedWarnings(generated);
    }

    [Fact]
    public void Weird_VeryLongMethodSignature()
    {
        // A method signature with many parameters split across many lines. The splitter
        // shouldn't get confused by long whitespace runs in the user source.
        var generated = CompileToCSharp(@"
@using Microsoft.AspNetCore.Components.Rendering;

@code {
    private void LongSig(
        int p1,
        int p2,
        int p3,
        int p4,
        int p5,
        int p6,
        int p7,
        int p8,
        int p9,
        int p10,
        RenderTreeBuilder __builder)
    {
        <p>@(p1 + p2 + p3 + p4 + p5)</p>
    }
}");
        CompileToAssemblyIgnoringUnusedWarnings(generated);
    }

    [Fact]
    public void Weird_AsyncTaskOfTReturnType()
    {
        var generated = CompileToCSharp(@"
@using System.Threading.Tasks;

@code {
    private async Task<int> ComputeAsync() { await Task.Yield(); return 42; }
    private async ValueTask<string> NamingAsync() { await Task.Yield(); return ""x""; }
}");
        CompileToAssemblyIgnoringUnusedWarnings(generated);
    }

    [Fact]
    public void Weird_ExpressionBodiedAccessors()
    {
        // Property with expression-bodied get and set accessors. Splitter shouldn't
        // route this away from decl (no markup, surface property).
        var generated = CompileToCSharp(@"
@code {
    private int _backing;
    [Parameter] public int X { get => _backing; set => _backing = value; }
}");
        CompileToAssemblyIgnoringUnusedWarnings(generated);
    }

    [Fact]
    public void Weird_AwaitInsideUsingBlock()
    {
        var generated = CompileToCSharp(@"
@using System;
@using System.Threading.Tasks;
@using Microsoft.AspNetCore.Components.Rendering;

@{ _ = RenderAsync(__builder); }

@code {
    private async Task RenderAsync(RenderTreeBuilder __builder)
    {
        await using var d = new MyDisposable();
        await Task.Yield();
        <p>after async using</p>
    }

    private sealed class MyDisposable : IAsyncDisposable
    {
        public ValueTask DisposeAsync() => default;
    }
}");
        CompileToAssemblyIgnoringUnusedWarnings(generated);
    }

    [Fact]
    public void Weird_GotoAndLabelInMethod()
    {
        // Razor's parser doesn't accept markup tokens immediately after a label
        // declaration (it treats `<` as expression-term). Test the splitter on a method
        // that uses goto/label control flow WITHOUT markup in the post-label position;
        // a markup chunk in a separate branch verifies the splitter still routes the
        // method to impl.
        var generated = CompileToCSharp(@"
@using Microsoft.AspNetCore.Components.Rendering;

@{ Render(__builder, true); }

@code {
    void Render(RenderTreeBuilder __builder, bool b)
    {
        if (b)
        {
            <p>before goto</p>
            goto end;
        }
        end:
        return;
    }
}");
        CompileToAssemblyIgnoringUnusedWarnings(generated);
    }

    [Fact]
    public void Weird_ExplicitInterfaceImplementation()
    {
        AdditionalSyntaxTrees.Add(Parse(@"
namespace Test
{
    public interface IPrintable { string GetPrintable(); }
}
"));
        var generated = CompileToCSharp(@"
@implements Test.IPrintable

@code {
    string Test.IPrintable.GetPrintable() => ""hello"";
}");
        CompileToAssemblyIgnoringUnusedWarnings(generated);
    }

    [Fact]
    public void Weird_ParseErrorInAtCode()
    {
        // Component with syntax error in @code block. The splitter should handle this
        // gracefully by routing all chunks to DeclOnly (preserving the verbatim text
        // so the user gets the real C# error from the compiler).
        var generated = CompileToCSharp(@"
@code {
    void Method(
    // Missing closing paren and brace -- parse error
}

<p>markup outside</p>
");
        
        // The parse error shouldn't prevent the document from compiling; it should
        // emit the @code verbatim and let the C# compiler report the errors.
        Assert.NotNull(generated.Code);
        // We expect C# compilation errors from the malformed method
        CompileToAssembly(generated, diagnostics =>
        {
            var errors = diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error).ToList();
            Assert.True(errors.Count > 0, "Expected at least one error from malformed @code");
        });
    }
}

