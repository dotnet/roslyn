// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace Microsoft.AspNetCore.Razor.Language.IntegrationTests;

// Regression tests for user @code helper methods whose body contains markup
// (e.g. `void Render(RTB) { <p>...</p> }`). Each test covers a Razor pattern that
// depends on the helper executing correctly at render time: typed component
// attributes, child content, @bind, implicit generic components, event handlers,
// fully-qualified component names, C# expression attributes, nested tags, and body
// expressions that capture a method-local or inner-scope variable.
//
// We assert via CompileToAssembly only -- that's the strongest pragmatic check
// that the emitted decl + impl actually combine into a valid component. We
// deliberately do NOT snapshot baselines, so the source files behave identically
// regardless of how decl/impl emission is structured. NOT inheriting from
// ComponentCodeGenerationTestBase: that class's [Fact] methods get re-run by xUnit
// against the subclass (its baselines target the parent's TestFiles/ directory).
public class AtCodeMarkupRegressionTests : RazorIntegrationTestBase
{
    internal override RazorFileKind? FileKind => RazorFileKind.Component;
    internal override bool UseTwoPhaseCompilation => true;
    [Fact]
    public void Risk_TypedAttributeOnComponent()
    {
        AdditionalSyntaxTrees.Add(Parse(@"
using Microsoft.AspNetCore.Components;

namespace Test
{
    public class MyComponent : ComponentBase
    {
        [Parameter] public int Count { get; set; }
    }
}
"));

        var generated = CompileToCSharp(@"
@using Microsoft.AspNetCore.Components.Rendering;

@{ RenderChildComponent(__builder); }

@code {
    void RenderChildComponent(RenderTreeBuilder __builder)
    {
        <MyComponent Count=""5"" />
    }
}");

        CompileToAssembly(generated);
    }

    [Fact]
    public void Risk_ChildContentOnComponent()
    {
        AdditionalSyntaxTrees.Add(Parse(@"
using Microsoft.AspNetCore.Components;

namespace Test
{
    public class MyComponent : ComponentBase
    {
        [Parameter] public RenderFragment ChildContent { get; set; }
    }
}
"));

        var generated = CompileToCSharp(@"
@using Microsoft.AspNetCore.Components.Rendering;

@{ RenderChildComponent(__builder); }

@code {
    void RenderChildComponent(RenderTreeBuilder __builder)
    {
        <MyComponent><span>inner</span></MyComponent>
    }
}");

        CompileToAssembly(generated);
    }

    [Fact]
    public void Risk_BindOnComponent()
    {
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

@{ RenderChildComponent(__builder); }

@code {
    int x = 5;
    void RenderChildComponent(RenderTreeBuilder __builder)
    {
        <MyComponent @bind-Value=""x"" />
    }
}");

        CompileToAssembly(generated);
    }

    [Fact]
    public void Risk_ImplicitGenericComponent()
    {
        AdditionalSyntaxTrees.Add(Parse(@"
using System.Collections.Generic;
using Microsoft.AspNetCore.Components;

namespace Test
{
    public class MyGeneric<T> : ComponentBase
    {
        [Parameter] public List<T> Items { get; set; }
    }
}
"));

        var generated = CompileToCSharp(@"
@using System.Collections.Generic
@using Microsoft.AspNetCore.Components.Rendering;

@{ RenderChildComponent(__builder); }

@code {
    List<int> list = new() { 1, 2, 3 };
    void RenderChildComponent(RenderTreeBuilder __builder)
    {
        <MyGeneric Items=""@list"" />
    }
}");

        CompileToAssembly(generated);
    }

    [Fact]
    public void Risk_EventHandlerOnComponent()
    {
        AdditionalSyntaxTrees.Add(Parse(@"
using System;
using Microsoft.AspNetCore.Components;

namespace Test
{
    public class MyComponent : ComponentBase
    {
        [Parameter] public EventCallback OnClick { get; set; }
    }
}
"));

        var generated = CompileToCSharp(@"
@using Microsoft.AspNetCore.Components.Rendering;

@{ RenderChildComponent(__builder); }

@code {
    void Handler() { }
    void RenderChildComponent(RenderTreeBuilder __builder)
    {
        <MyComponent OnClick=""Handler"" />
    }
}");

        CompileToAssembly(generated);
    }

    [Fact]
    public void Risk_FullyQualifiedComponentName()
    {
        AdditionalSyntaxTrees.Add(Parse(@"
using Microsoft.AspNetCore.Components;

namespace MyNs
{
    public class MyComponent : ComponentBase { }
}
"));

        var generated = CompileToCSharp(@"
@using Microsoft.AspNetCore.Components.Rendering;

@{ RenderChildComponent(__builder); }

@code {
    void RenderChildComponent(RenderTreeBuilder __builder)
    {
        <MyNs.MyComponent />
    }
}");

        CompileToAssembly(generated);
    }

    [Fact]
    public void Risk_CSharpExpressionAttribute()
    {
        AdditionalSyntaxTrees.Add(Parse(@"
using Microsoft.AspNetCore.Components;

namespace Test
{
    public class MyComponent : ComponentBase
    {
        [Parameter] public string Caption { get; set; }
    }
}
"));

        var generated = CompileToCSharp(@"
@using Microsoft.AspNetCore.Components.Rendering;

@{ RenderChildComponent(__builder); }

@code {
    string text = ""hi"";
    void RenderChildComponent(RenderTreeBuilder __builder)
    {
        <MyComponent Caption=""@text"" />
    }
}");

        CompileToAssembly(generated);
    }

    [Fact]
    public void Risk_NestedTagsInCodeBlock()
    {
        AdditionalSyntaxTrees.Add(Parse(@"
using Microsoft.AspNetCore.Components;

namespace Test
{
    public class MyComponent : ComponentBase
    {
        [Parameter] public RenderFragment ChildContent { get; set; }
    }
}
"));

        var generated = CompileToCSharp(@"
@using Microsoft.AspNetCore.Components.Rendering;

@{ RenderChildComponent(__builder); }

@code {
    void RenderChildComponent(RenderTreeBuilder __builder)
    {
        <MyComponent>
            <div>
                <MyComponent />
            </div>
        </MyComponent>
    }
}");

        CompileToAssembly(generated);
    }

    [Fact]
    public void Risk_BodyExpressionReferencesLocal()
    {
        // <p>@output</p> -- body C# expression references a local declared in the
        // enclosing method scope. The deferred-helper path captures `output` as a
        // typed generic parameter so the helper method sees it as a real value.
        var generated = CompileToCSharp(@"
@using Microsoft.AspNetCore.Components.Rendering;

@{ RenderChildComponent(__builder); }

@code {
    void RenderChildComponent(RenderTreeBuilder __builder)
    {
        var output = ""hello"";
        <p>@output</p>
    }
}");

        CompileToAssembly(generated);
    }

    [Fact]
    public void Risk_BodyExpressionInInnerScope_DoesNotRegress()
    {
        // <li>@i</li> inside an @for loop -- `i` is in an inner C# scope. The test
        // asserts CompileToAssembly succeeds (so users get a working build) for the
        // for-loop-with-inner-markup pattern.
        var generated = CompileToCSharp(@"
@using Microsoft.AspNetCore.Components.Rendering;

@{ MyMethod(__builder); }

@code {
    void MyMethod(RenderTreeBuilder __builder)
    {
        <ul>
            @for (var i = 0; i < 3; i++)
            {
                <li>@i</li>
            }
        </ul>
    }
}");

        CompileToAssembly(generated);
    }
}
