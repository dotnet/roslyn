// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable

using System.Globalization;
using System.Linq;
using Microsoft.AspNetCore.Razor.Language.Components;
using Microsoft.AspNetCore.Razor.Test.Common;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.AspNetCore.Razor.Language.IntegrationTests;

public class ComponentCodeGenerationTestBase(bool designTime = false)
    : RazorBaselineIntegrationTestBase(layer: TestProject.Layer.Compiler)
{
    private RazorConfiguration _configuration;

    internal override RazorFileKind? FileKind => RazorFileKind.Component;

    internal override bool UseTwoPhaseCompilation => true;

    internal override RazorConfiguration Configuration => _configuration ?? base.Configuration;

    internal string ComponentName = "TestComponent";

    internal override string DefaultFileName => ComponentName + ".cshtml";

    internal override bool DesignTime => designTime;

    protected override string GetDirectoryPath(string testName)
    {
        var directory = DesignTime ? "ComponentDesignTimeCodeGenerationTest" : "ComponentRuntimeCodeGenerationTest";
        return $"TestFiles/IntegrationTests/{directory}/{testName}";
    }

    #region Basics

    [IntegrationTestFact]
    public void SingleLineControlFlowStatements_InCodeDirective()
    {
        // Arrange

        // Act
        var generated = CompileToCSharp(@"
@using Microsoft.AspNetCore.Components.Rendering;

@code {
    void RenderChildComponent(RenderTreeBuilder __builder)
    {
        var output = string.Empty;
        if (__builder == null) output = ""Builder is null!"";
        else output = ""Builder is not null!"";
        <p>Output: @output</p>
    }
}");

        // Assert
        AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
        AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
        CompileToAssembly(generated);
    }

    [IntegrationTestFact]
    public void SingleLineControlFlowStatements_InCodeBlock()
    {
        // Arrange

        // Act
        var generated = CompileToCSharp(@"
@using Microsoft.AspNetCore.Components.RenderTree;

@{
    var output = string.Empty;
    if (__builder == null) output = ""Builder is null!"";
    else output = ""Builder is not null!"";
    <p>Output: @output</p>
}");

        // Assert
        AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
        AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
        CompileToAssembly(generated);
    }
    [IntegrationTestFact]
    public void ChildComponent_InFunctionsDirective()
    {
        // Arrange
        AdditionalSyntaxTrees.Add(Parse(@"
using Microsoft.AspNetCore.Components;

namespace Test
{
    public class MyComponent : ComponentBase
    {
    }
}
"));

        // Act
        var generated = CompileToCSharp(@"
@using Microsoft.AspNetCore.Components.Rendering;

@{ RenderChildComponent(__builder); }

@code {
    void RenderChildComponent(RenderTreeBuilder __builder)
    {
        <MyComponent />
    }
}");

        // Assert
        AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
        AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
        CompileToAssembly(generated);
    }

    [IntegrationTestFact]
    public void ChildComponent_InLocalFunction()
    {
        // Arrange
        AdditionalSyntaxTrees.Add(Parse(@"
using Microsoft.AspNetCore.Components;

namespace Test
{
    public class MyComponent : ComponentBase
    {
    }
}
"));

        // Act
        var generated = CompileToCSharp(@"
@using Microsoft.AspNetCore.Components.RenderTree;
@{
    void RenderChildComponent()
    {
        <MyComponent />
    }
}

@{ RenderChildComponent(); }
");

        // Assert
        AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
        AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
        CompileToAssembly(generated);
    }

    [IntegrationTestFact]
    public void ChildComponent_Simple()
    {
        // Arrange
        AdditionalSyntaxTrees.Add(Parse(@"
using Microsoft.AspNetCore.Components;

namespace Test
{
    public class MyComponent : ComponentBase
    {
    }
}
"));

        // Act
        var generated = CompileToCSharp(@"
<MyComponent />");

        // Assert
        AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
        AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
        CompileToAssembly(generated);
    }

    [IntegrationTestFact]
    public void ChildComponent_WithParameters()
    {
        // Arrange
        AdditionalSyntaxTrees.Add(Parse(@"
using Microsoft.AspNetCore.Components;

namespace Test
{
    public class SomeType
    {
    }

    public class MyComponent : ComponentBase
    {
        [Parameter] public int IntProperty { get; set; }
        [Parameter] public bool BoolProperty { get; set; }
        [Parameter] public string StringProperty { get; set; }
        [Parameter] public SomeType ObjectProperty { get; set; }
    }
}
"));

        // Act
        var generated = CompileToCSharp(@"
<MyComponent
    IntProperty=""123""
    BoolProperty=""true""
    StringProperty=""My string""
    ObjectProperty=""new SomeType()""/>");

        // Assert
        AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
        AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
        CompileToAssembly(generated);
    }

    [IntegrationTestFact]
    public void ComponentWithDecimalParameter()
    {
        // Arrange

        // Act
        var generated = CompileToCSharp(@"
<strong>@TestDecimal</strong>

<TestComponent TestDecimal=""4"" />

@code {
    [Parameter]
    public decimal TestDecimal { get; set; }
}");

        // Assert
        AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
        AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
        CompileToAssembly(generated);
    }

    [IntegrationTestFact]
    public void ComponentWithBooleanParameter()
    {
        // Arrange

        // Act
        var generated = CompileToCSharp(@"
<strong>@TestBool</strong>

<TestComponent TestBool=""true"" />

@code {
    [Parameter]
    public bool TestBool { get; set; }
}");

        // Assert
        AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
        AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
        CompileToAssembly(generated);
    }

    [IntegrationTestFact]
    public void ComponentWithBooleanParameter_Minimized()
    {
        // Arrange

        // Act
        var generated = CompileToCSharp(@"
<strong>@TestBool</strong>

<TestComponent TestBool />

@code {
    [Parameter]
    public bool TestBool { get; set; }
}");

        // Assert
        AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
        AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
        CompileToAssembly(generated);
    }

    [IntegrationTestFact]
    public void ComponentWithDynamicParameter()
    {
        // Arrange

        // Act
        var generated = CompileToCSharp(@"
<strong>@TestDynamic</strong>

<TestComponent TestDynamic=""4"" />

@code {
    [Parameter]
    public dynamic TestDynamic { get; set; }
}");

        // Assert
        AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
        AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
        CompileToAssembly(generated);
    }

    [IntegrationTestFact]
    public void ComponentWithTypeParameters()
    {
        // Arrange

        // Act
        var generated = CompileToCSharp(@"
@using Microsoft.AspNetCore.Components;
@typeparam TItem1
@typeparam TItem2

<h1>Item1</h1>
@foreach (var item2 in Items2)
{
    <p>
    @ChildContent(item2);
    </p>
}
@code {
    [Parameter] public TItem1 Item1 { get; set; }
    [Parameter] public List<TItem2> Items2 { get; set; }
    [Parameter] public RenderFragment<TItem2> ChildContent { get; set; }
}");

        // Assert
        AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
        AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
        CompileToAssembly(generated);
    }

    [IntegrationTestFact, WorkItem("https://github.com/dotnet/razor/issues/8711")]
    public void ComponentWithTypeParameters_Interconnected()
    {
        // Arrange
        AdditionalSyntaxTrees.Add(Parse("""
            public class C<T> { }
            public class D<T1, T2> where T1 : C<T2> { }
            """));

        // Act
        var generated = CompileToCSharp("""
            @typeparam T1 where T1 : C<T2>
            @typeparam T2 where T2 : D<T1, T2>
            """);

        // Assert
        AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
        AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
        CompileToAssembly(generated);
    }

    [IntegrationTestFact]
    public void Component_WithEscapedParameterName()
    {
        // Arrange
        AdditionalSyntaxTrees.Add(Parse("""
            using Microsoft.AspNetCore.Components;

            namespace Test
            {
                public class MyComponent : ComponentBase
                {
                    [Parameter]
                    public int @class { get; set; }
                    [Parameter]
                    public int Prop2 { get; set; }
                }
            }
            """));

        // Act
        var generated = CompileToCSharp("""
            <MyComponent class="1" Prop2="2">
            </MyComponent>
            """);

        // Assert
        AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
        AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
        CompileToAssembly(generated);
    }

    [IntegrationTestFact]
    public void Component_WithWriteOnlyParameter()
    {
        // Arrange
        AdditionalSyntaxTrees.Add(Parse("""
            using Microsoft.AspNetCore.Components;
            namespace Test
            {
                public class MyComponent : ComponentBase
                {
                    [Parameter]
                    public int Prop { set { _ = value; } }
                }
            }
            """));

        // Act
        var generated = CompileToCSharp("""
            <MyComponent Prop="1">
            </MyComponent>
            """);

        // Assert
        AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
        AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
        CompileToAssembly(generated);
    }

    [IntegrationTestFact]
    public void Component_WithInitOnlyParameter()
    {
        // Arrange
        AdditionalSyntaxTrees.Add(Parse("""
            namespace System.Runtime.CompilerServices;
            internal static class IsExternalInit
            {
            }
            """));
        AdditionalSyntaxTrees.Add(Parse("""
            using Microsoft.AspNetCore.Components;
            namespace Test
            {
                public class MyComponent : ComponentBase
                {
                    [Parameter]
                    public int Prop { get; init; }
                }
            }
            """));

        // Act
        var generated = CompileToCSharp("""
            <MyComponent Prop="1">
            </MyComponent>
            """);

        // Assert
        AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
        AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
        CompileToAssembly(generated);
    }

    [IntegrationTestFact]
    public void ComponentWithTypeParameters_WithSemicolon()
    {
        // Arrange

        // Act
        var generated = CompileToCSharp(@"
@using Microsoft.AspNetCore.Components;
@typeparam TItem1;
@typeparam TItem2;

<h1>Item1</h1>
@foreach (var item2 in Items2)
{
    <p>
    @ChildContent(item2);
    </p>
}
@code {
    [Parameter] public TItem1 Item1 { get; set; }
    [Parameter] public List<TItem2> Items2 { get; set; }
    [Parameter] public RenderFragment<TItem2> ChildContent { get; set; }
}");

        // Assert
        AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
        AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
        CompileToAssembly(generated);
    }

    [IntegrationTestFact]
    public void ComponentWithTypeParameterArray()
    {
        // Arrange
        var classes = @"
public class Tag
{
    public string description { get; set; }
}
";

        AdditionalSyntaxTrees.Add(Parse(classes));

        // Act
        var generated = CompileToCSharp(@"
@using Microsoft.AspNetCore.Components;
@typeparam TItem

<h1>Item</h1>

<p>@ChildContent(Items1)</p>

@foreach (var item in Items2)
{
    <p>@ChildContent(item)</p>
}

<p>@ChildContent(Items3())</p>

@code {
    [Parameter] public TItem[] Items1 { get; set; }
    [Parameter] public List<TItem[]> Items2 { get; set; }
    [Parameter] public Func<TItem[]> Items3 { get; set; }
    [Parameter] public RenderFragment<TItem[]> ChildContent { get; set; }
}");

        // Assert
        AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
        AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
        CompileToAssembly(generated);

        AdditionalSyntaxTrees.Add(Parse(generated.CodeDocument.GetRequiredCSharpDocument().Text));
        var useGenerated = CompileToCSharp("UseTestComponent.cshtml", cshtmlContent: @"
@using Test
<TestComponent Items1=items1 Items2=items2 Items3=items3>
    <p>@context[0].description</p>
</TestComponent>

@code {
    static Tag tag = new Tag() { description = ""A description.""};
    Tag[] items1 = new [] { tag };
    List<Tag[]> items2 = new List<Tag[]>() { new [] { tag } };
    Tag[] items3() => new [] { tag };
}");
        AssertDocumentNodeMatchesBaseline(useGenerated.CodeDocument);
        AssertCSharpDocumentMatchesBaseline(useGenerated.CodeDocument);
        CompileToAssembly(useGenerated);
    }

    [IntegrationTestFact]
    public void ComponentWithTupleParameter()
    {
        // Arrange

        // Act
        var generated = CompileToCSharp(@"
@code {
    [Parameter] public (int Horizontal, int Vertical) Gutter { get; set; }
}

<TestComponent Gutter=""(32, 16)"">
</TestComponent>
");

        // Assert
        AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
        AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
        CompileToAssembly(generated);
    }

    [IntegrationTestFact]
    public void ComponentWithTypeParameterValueTuple()
    {
        // Arrange
        var classes = @"
public class Tag
{
    public string description { get; set; }
}
";

        AdditionalSyntaxTrees.Add(Parse(classes));

        // Act
        var generated = CompileToCSharp(@"
@using Microsoft.AspNetCore.Components;
@typeparam TItem1
@typeparam TItem2

<h1>Item</h1>

<p>@ChildContent(Item1)</p>

@foreach (var item in Items2)
{
    <p>@ChildContent(item)</p>
}

@code {
    [Parameter] public (TItem1, TItem2) Item1 { get; set; }
    [Parameter] public List<(TItem1, TItem2)> Items2 { get; set; }
    [Parameter] public RenderFragment<(TItem1, TItem2)> ChildContent { get; set; }
}");

        // Assert
        AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
        AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
        CompileToAssembly(generated);

        AdditionalSyntaxTrees.Add(Parse(generated.CodeDocument.GetRequiredCSharpDocument().Text));
        var useGenerated = CompileToCSharp("UseTestComponent.cshtml", cshtmlContent: @"
@using Test
<TestComponent Item1=item1 Items2=items2>
    <p>@context</p>
</TestComponent>

@code {
    (string, int) item1 = (""A string"", 42);
    static (string, int) item2 = (""Another string"", 42);
    List<(string, int)> items2 = new List<(string, int)>() { item2 };
}");
        AssertDocumentNodeMatchesBaseline(useGenerated.CodeDocument);
        AssertCSharpDocumentMatchesBaseline(useGenerated.CodeDocument);
        CompileToAssembly(useGenerated);
    }

    [IntegrationTestFact]
    public void ComponentWithTypeParameterValueTupleGloballyQualifiedTypes()
    {
        // Arrange
        var classes = @"
namespace N;

public class MyClass
{
    public int MyClassId { get; set; }
}

public struct MyStruct
{
    public int MyStructId { get; set; }
}
";

        AdditionalSyntaxTrees.Add(Parse(classes));

        // Act
        var generated = CompileToCSharp(@"
@using N
@typeparam TParam

@code {
    [Parameter]
    public TParam InferParam { get; set; }

    [Parameter]
    public RenderFragment<(MyClass I1, MyStruct I2, TParam P)> Template { get; set; }
}

<TestComponent InferParam=""1"">
    <Template>
        @context.I1.MyClassId - @context.I2.MyStructId
    </Template>
</TestComponent>");

        // Assert
        AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
        AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
        CompileToAssembly(generated);
    }

    [IntegrationTestFact, WorkItem("https://github.com/dotnet/razor/issues/7628")]
    public void ComponentWithTypeParameterValueTuple_ExplicitGenericArguments()
    {
        // Act
        var generated = CompileToCSharp("""
            @typeparam TDomain where TDomain : struct
            @typeparam TValue where TValue : struct

            <TestComponent Data="null" TDomain="decimal" TValue="decimal" />

            @code {
                [Parameter]
                public List<(TDomain Domain, TValue Value)> Data { get; set; }
            }
            """);

        // Assert
        AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
        AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
        CompileToAssembly(generated);
    }

    [IntegrationTestFact]
    public void ComponentWithConstrainedTypeParameters()
    {
        // Arrange
        var classes = @"
public class Image
{
    public string url { get; set; }
    public int id { get; set; }

    public Image()
    {
        url = ""https://example.com/default.png"";
        id = 1;
    }
}

public interface ITag
{
    string description { get; set; }
}

public class Tag : ITag
{
    public string description { get; set; }
}
";

        AdditionalSyntaxTrees.Add(Parse(classes));

        // Act
        var generated = CompileToCSharp(@"
@using Microsoft.AspNetCore.Components;
@typeparam TItem1 where TItem1 : Image
@typeparam TItem2 where TItem2 : ITag
@typeparam TItem3 where TItem3 : Image, new()

<h1>Item1</h1>
@foreach (var item2 in Items2)
{
    <p>
    @ChildContent(item2);
    </p>
}

<p>Item3</p>

@code {
    [Parameter] public TItem1 Item1 { get; set; }
    [Parameter] public List<TItem2> Items2 { get; set; }
    [Parameter] public TItem3 Item3 { get; set; }
    [Parameter] public RenderFragment<TItem2> ChildContent { get; set; }
}");

        // Assert
        AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
        AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
        CompileToAssembly(generated);

        AdditionalSyntaxTrees.Add(Parse(generated.CodeDocument.GetRequiredCSharpDocument().Text));
        var useGenerated = CompileToCSharp("UseTestComponent.cshtml", cshtmlContent: @"
@using Test
<TestComponent Item1=@item1 Items2=@items Item3=@item1>
    <p>@context</p>
</TestComponent>

@code {
    Image item1 = new Image() { id = 1, url=""https://example.com""};
    static Tag tag1 = new Tag() { description = ""A description.""};
    static Tag tag2 = new Tag() { description = ""Another description.""};
    List<Tag> items = new List<Tag>() { tag1, tag2 };
}");
        AssertDocumentNodeMatchesBaseline(useGenerated.CodeDocument);
        AssertCSharpDocumentMatchesBaseline(useGenerated.CodeDocument);
        CompileToAssembly(useGenerated);
    }

    [IntegrationTestFact]
    public void ComponentWithConstrainedTypeParameters_WithSemicolon()
    {
        // Arrange
        var classes = @"
public class Image
{
    public string url { get; set; }
    public int id { get; set; }

    public Image()
    {
        url = ""https://example.com/default.png"";
        id = 1;
    }
}

public interface ITag
{
    string description { get; set; }
}

public class Tag : ITag
{
    public string description { get; set; }
}
";

        AdditionalSyntaxTrees.Add(Parse(classes));

        // Act
        var generated = CompileToCSharp(@"
@using Microsoft.AspNetCore.Components;
@typeparam TItem1 where TItem1 : Image;
@typeparam TItem2 where TItem2 : ITag;
@typeparam TItem3 where TItem3 : Image, new();

<h1>Item1</h1>
@foreach (var item2 in Items2)
{
    <p>
    @ChildContent(item2);
    </p>
}

<p>Item3</p>

@code {
    [Parameter] public TItem1 Item1 { get; set; }
    [Parameter] public List<TItem2> Items2 { get; set; }
    [Parameter] public TItem3 Item3 { get; set; }
    [Parameter] public RenderFragment<TItem2> ChildContent { get; set; }
}");

        // Assert
        AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
        AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
        CompileToAssembly(generated);

        AdditionalSyntaxTrees.Add(Parse(generated.CodeDocument.GetRequiredCSharpDocument().Text));
        var useGenerated = CompileToCSharp("UseTestComponent.cshtml", cshtmlContent: @"
@using Test
<TestComponent Item1=@item1 Items2=@items Item3=@item1>
    <p>@context</p>
</TestComponent>

@code {
    Image item1 = new Image() { id = 1, url=""https://example.com""};
    static Tag tag1 = new Tag() { description = ""A description.""};
    static Tag tag2 = new Tag() { description = ""Another description.""};
    List<Tag> items = new List<Tag>() { tag1, tag2 };
}");
        AssertDocumentNodeMatchesBaseline(useGenerated.CodeDocument);
        AssertCSharpDocumentMatchesBaseline(useGenerated.CodeDocument);
        CompileToAssembly(useGenerated);
    }

    [IntegrationTestFact]
    public void ChildComponent_WithExplicitStringParameter()
    {
        // Arrange
        AdditionalSyntaxTrees.Add(Parse(@"
using Microsoft.AspNetCore.Components;

namespace Test
{
    public class MyComponent : ComponentBase
    {
        [Parameter]
        public string StringProperty { get; set; }
    }
}
"));

        // Act
        var generated = CompileToCSharp(@"
<MyComponent StringProperty=""@(42.ToString())"" />");

        // Assert
        AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
        AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
        CompileToAssembly(generated);
    }

    [IntegrationTestFact]
    public void ChildComponent_WithNonPropertyAttributes()
    {
        // Arrange
        AdditionalSyntaxTrees.Add(Parse(@"
using Microsoft.AspNetCore.Components;

namespace Test
{
    public class MyComponent : ComponentBase
    {
    }
}
"));

        // Act
        var generated = CompileToCSharp(@"
<MyComponent some-attribute=""foo"" another-attribute=""@(43.ToString())""/>");

        // Assert
        AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
        AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
        CompileToAssembly(generated);
    }

    [IntegrationTestFact]
    public void ComponentParameter_TypeMismatch_ReportsDiagnostic()
    {
        // Arrange
        AdditionalSyntaxTrees.Add(Parse(@"
using Microsoft.AspNetCore.Components;

namespace Test
{
    public class CoolnessMeter : ComponentBase
    {
        [Parameter] public int Coolness { get; set; }
    }
}
"));

        // Act
        var generated = CompileToCSharp(@"
<CoolnessMeter Coolness=""@(""very-cool"")"" />
");

        // Assert
        AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
        AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);

        CompileToAssembly(generated,
            // x:\dir\subdir\Test\TestComponent.cshtml(1,28): error CS1503: Argument 1: cannot convert from 'string' to 'int'
            //                            "very-cool"
            Diagnostic(ErrorCode.ERR_BadArgType, @"""very-cool""").WithArguments("1", "string", "int").WithLocation(1, 28));
    }

    [IntegrationTestFact]
    public void DataDashAttribute_ImplicitExpression()
    {
        // Arrange

        // Act
        var generated = CompileToCSharp(@"
@{
  var myValue = ""Expression value"";
}
<elem data-abc=""Literal value"" data-def=""@myValue"" />");

        // Assert
        AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
        AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
        CompileToAssembly(generated);
    }

    [IntegrationTestFact]
    public void DataDashAttribute_ExplicitExpression()
    {
        // Arrange

        // Act
        var generated = CompileToCSharp(@"
@{
  var myValue = ""Expression value"";
}
<elem data-abc=""Literal value"" data-def=""@(myValue)"" />");

        // Assert
        AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
        AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
        CompileToAssembly(generated);
    }

    [IntegrationTestFact]
    public void MarkupComment_IsNotIncluded()
    {
        // Arrange

        // Act
        var generated = CompileToCSharp(@"
@{
  var myValue = ""Expression value"";
}
<div>@myValue <!-- @myValue --> </div>");

        // Assert
        AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
        AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
        CompileToAssembly(generated);
    }

    [IntegrationTestFact]
    public void OmitsMinimizedAttributeValueParameter()
    {
        // Act
        var generated = CompileToCSharp(@"
<elem normal-attr=""@(""val"")"" minimized-attr empty-string-atttr=""""></elem>");

        // Assert
        AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
        AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
        CompileToAssembly(generated);
    }

    [IntegrationTestFact]
    public void IncludesMinimizedAttributeValueParameterBeforeLanguageVersion5()
    {
        // Arrange
        _configuration = base.Configuration with { LanguageVersion = RazorLanguageVersion.Version_3_0 };

        // Act
        var generated = CompileToCSharp(@"
<elem normal-attr=""@(""val"")"" minimized-attr empty-string-atttr=""""></elem>");

        // Assert
        AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
        AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
        CompileToAssembly(generated);
    }

    [IntegrationTestFact]
    public void Component_WithFullyQualifiedTagNames()
    {
        // Arrange
        AdditionalSyntaxTrees.Add(Parse(@"
using Microsoft.AspNetCore.Components;

namespace Test
{
    public class MyComponent : ComponentBase
    {
    }
}

namespace Test2
{
    public class MyComponent2 : ComponentBase
    {
    }
}
"));

        // Act
        var generated = CompileToCSharp(@"
<MyComponent />
<Test.MyComponent />
<Test2.MyComponent2 />");

        // Assert
        AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
        AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
        CompileToAssembly(generated);
    }

    [IntegrationTestFact]
    public void Component_WithNullableActionParameter()
    {
        AdditionalSyntaxTrees.Add(Parse(@"
using System;
using Microsoft.AspNetCore.Components;
namespace Test
{
    public class ComponentWithNullableAction : ComponentBase
    {
        [Parameter] public Action NullableAction { get; set; }
    }
}
"));
        var generated = CompileToCSharp(@"
<ComponentWithNullableAction NullableAction=""@NullableAction"" />
@code {
	[Parameter]
	public Action NullableAction { get; set; }
}
");
        AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
        AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
        CompileToAssembly(generated);
    }

    [IntegrationTestFact]
    public void Component_WithNullableRenderFragmentParameter()
    {
        AdditionalSyntaxTrees.Add(Parse(@"
using System;
using Microsoft.AspNetCore.Components;
namespace Test
{
    public class ComponentWithNullableRenderFragment : ComponentBase
    {
        [Parameter] public RenderFragment Header { get; set; }
    }
}
"));
        var generated = CompileToCSharp(@"
<ComponentWithNullableRenderFragment Header=""@Header"" />
@code {
	[Parameter] public RenderFragment Header { get; set; }
}
");
        AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
        AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
        CompileToAssembly(generated);
    }

    [IntegrationTestFact]
    public void Component_WithEditorRequiredParameter_NoValueSpecified()
    {
        AdditionalSyntaxTrees.Add(Parse(@"
using System;
using Microsoft.AspNetCore.Components;
namespace Test
{
    public class ComponentWithEditorRequiredParameters : ComponentBase
    {
        [Parameter]
        [EditorRequired]
        public string Property1 { get; set; }
    }
}
"));
        var generated = CompileToCSharp(@"
<ComponentWithEditorRequiredParameters />
");
        AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
        AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
        CompileToAssembly(generated);

        var diagnostics = Assert.Single(generated.RazorDiagnostics);
        Assert.Equal(RazorDiagnosticSeverity.Warning, diagnostics.Severity);
        Assert.Equal("RZ2012", diagnostics.Id);
    }

    [IntegrationTestFact]
    public void Component_WithEditorRequiredParameter_ValueSpecified()
    {
        AdditionalSyntaxTrees.Add(Parse(@"
using System;
using Microsoft.AspNetCore.Components;
namespace Test
{
    public class ComponentWithEditorRequiredParameters : ComponentBase
    {
        [Parameter]
        [EditorRequired]
        public string Property1 { get; set; }
    }
}
"));
        var generated = CompileToCSharp(@"
<ComponentWithEditorRequiredParameters Property1=""Some Value"" />
");
        AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
        AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
        CompileToAssembly(generated);

        Assert.Empty(generated.RazorDiagnostics);
    }

    [IntegrationTestFact, WorkItem("https://github.com/dotnet/razor/issues/10553")]
    public void Component_WithEditorRequiredParameter_ValueSpecified_EventCallbackRequired()
    {
        AdditionalSyntaxTrees.Add(Parse("""
            using System;
            using Microsoft.AspNetCore.Components;

            namespace Test;

            public class ComponentWithEditorRequiredParameters : ComponentBase
            {
                [Parameter, EditorRequired]
                public bool Property1 { get; set; }

                [Parameter, EditorRequired]
                public EventCallback<bool> Property1Changed { get; set; }
            }
            """));

        var generated = CompileToCSharp("""
            <ComponentWithEditorRequiredParameters Property1="false" />
            """);

        CompileToAssembly(generated);
        generated.RazorDiagnostics.Verify(
            // x:\dir\subdir\Test\TestComponent.cshtml(1,1): warning RZ2012: Component 'ComponentWithEditorRequiredParameters' expects a value for the parameter 'Property1Changed', but a value may not have been provided.
            Diagnostic("RZ2012").WithLocation(1, 1));
    }

    [IntegrationTestFact, WorkItem("https://github.com/dotnet/razor/issues/10553")]
    public void Component_WithEditorRequiredParameter_ValueSpecified_ExpressionRequired()
    {
        AdditionalSyntaxTrees.Add(Parse("""
            using System;
            using System.Linq.Expressions;
            using Microsoft.AspNetCore.Components;

            namespace Test;

            public class ComponentWithEditorRequiredParameters : ComponentBase
            {
                [Parameter, EditorRequired]
                public bool Property1 { get; set; }

                [Parameter, EditorRequired]
                public EventCallback<bool> Property1Changed { get; set; }

                [Parameter, EditorRequired]
                public Expression<Func<bool>> Property1Expression { get; set; }
            }
            """));

        var generated = CompileToCSharp("""
            <ComponentWithEditorRequiredParameters Property1="false" />
            """);

        CompileToAssembly(generated);
        generated.RazorDiagnostics.Verify(
            // x:\dir\subdir\Test\TestComponent.cshtml(1,1): warning RZ2012: Component 'ComponentWithEditorRequiredParameters' expects a value for the parameter 'Property1Changed', but a value may not have been provided.
            Diagnostic("RZ2012").WithLocation(1, 1),
            // x:\dir\subdir\Test\TestComponent.cshtml(1,1): warning RZ2012: Component 'ComponentWithEditorRequiredParameters' expects a value for the parameter 'Property1Expression', but a value may not have been provided.
            Diagnostic("RZ2012").WithLocation(1, 1));
    }

    [IntegrationTestFact]
    public void Component_WithEditorRequiredParameter_ValueSpecified_DifferentCasing()
    {
        AdditionalSyntaxTrees.Add(Parse("""
            using Microsoft.AspNetCore.Components;
            namespace Test;
            public class ComponentWithEditorRequiredParameters : ComponentBase
            {
                [Parameter, EditorRequired] public string Property1 { get; set; }
            }
            """));
        var generated = CompileToCSharp("""
            <ComponentWithEditorRequiredParameters property1="Some Value" />
            """);
        generated.RazorDiagnostics.Verify(
            // x:\dir\subdir\Test\TestComponent.cshtml(1,1): warning RZ2012: Component 'ComponentWithEditorRequiredParameters' expects a value for the parameter 'Property1', but a value may not have been provided.
            Diagnostic("RZ2012").WithLocation(1, 1));
    }

    [IntegrationTestFact]
    public void Component_WithEditorRequiredParameter_ValuesSpecifiedUsingSplatting()
    {
        AdditionalSyntaxTrees.Add(Parse(@"
using System;
using Microsoft.AspNetCore.Components;
namespace Test
{
    public class ComponentWithEditorRequiredParameters : ComponentBase
    {
        [Parameter]
        [EditorRequired]
        public string Property1 { get; set; }
    }
}
"));
        var generated = CompileToCSharp(@"
<ComponentWithEditorRequiredParameters @attributes=""@(new Dictionary<string, object>())"" />
");
        AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
        AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
        CompileToAssembly(generated);

        Assert.Empty(generated.RazorDiagnostics);
    }

    [IntegrationTestFact, WorkItem("https://github.com/dotnet/razor/issues/7395")]
    public void Component_WithEditorRequiredParameter_ValueSpecifiedUsingBind()
    {
        AdditionalSyntaxTrees.Add(Parse("""
            using System;
            using Microsoft.AspNetCore.Components;

            namespace Test;

            public class ComponentWithEditorRequiredParameters : ComponentBase
            {
                [Parameter]
                [EditorRequired]
                public string Property1 { get; set; }
            }
            """));

        var generated = CompileToCSharp("""
            <ComponentWithEditorRequiredParameters @bind-Property1="myField" />

            @code {
                private string myField = "Some Value";
            }
            """);

        AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
        AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
        CompileToAssembly(generated);
        Assert.Empty(generated.RazorDiagnostics);
    }

    [IntegrationTestFact, WorkItem("https://github.com/dotnet/razor/issues/10553")]
    public void Component_WithEditorRequiredParameter_ValueSpecifiedUsingBind_EventCallbackRequired()
    {
        AdditionalSyntaxTrees.Add(Parse("""
            using System;
            using Microsoft.AspNetCore.Components;

            namespace Test;

            public class ComponentWithEditorRequiredParameters : ComponentBase
            {
                [Parameter, EditorRequired]
                public string Property1 { get; set; }

                [Parameter, EditorRequired]
                public EventCallback<string> Property1Changed { get; set; }
            }
            """));

        var generated = CompileToCSharp("""
            <ComponentWithEditorRequiredParameters @bind-Property1="myField" />

            @code {
                private string myField = "Some Value";
            }
            """);

        CompileToAssembly(generated);
        generated.RazorDiagnostics.Verify();
    }

    [IntegrationTestFact, WorkItem("https://github.com/dotnet/razor/issues/10553")]
    public void Component_WithEditorRequiredParameter_ValueSpecifiedUsingBind_ExpressionRequired()
    {
        AdditionalSyntaxTrees.Add(Parse("""
            using System;
            using System.Linq.Expressions;
            using Microsoft.AspNetCore.Components;

            namespace Test;

            public class ComponentWithEditorRequiredParameters : ComponentBase
            {
                [Parameter, EditorRequired]
                public string Property1 { get; set; }

                [Parameter, EditorRequired]
                public EventCallback<string> Property1Changed { get; set; }

                [Parameter, EditorRequired]
                public Expression<Func<string>> Property1Expression { get; set; }
            }
            """));

        var generated = CompileToCSharp("""
            <ComponentWithEditorRequiredParameters @bind-Property1="myField" />

            @code {
                private string myField = "Some Value";
            }
            """);

        CompileToAssembly(generated);
        generated.RazorDiagnostics.Verify();
    }

    [IntegrationTestFact]
    public void Component_WithEditorRequiredParameter_ValueSpecifiedUsingBind_DifferentCasing()
    {
        AdditionalSyntaxTrees.Add(Parse("""
            using Microsoft.AspNetCore.Components;
            namespace Test;
            public class ComponentWithEditorRequiredParameters : ComponentBase
            {
                [Parameter, EditorRequired] public string Property1 { get; set; }
            }
            """));
        var generated = CompileToCSharp("""
            <ComponentWithEditorRequiredParameters @bind-property1="Some Value" />
            """);
        generated.RazorDiagnostics.Verify(
            // x:\dir\subdir\Test\TestComponent.cshtml(1,1): warning RZ2012: Component 'ComponentWithEditorRequiredParameters' expects a value for the parameter 'Property1', but a value may not have been provided.
            Diagnostic("RZ2012").WithLocation(1, 1));
    }

    [IntegrationTestFact, WorkItem("https://github.com/dotnet/razor/issues/10553")]
    public void Component_WithEditorRequiredParameter_ValueSpecifiedUsingBindGetSet()
    {
        AdditionalSyntaxTrees.Add(Parse("""
            using System;
            using Microsoft.AspNetCore.Components;

            namespace Test;

            public class ComponentWithEditorRequiredParameters : ComponentBase
            {
                [Parameter]
                [EditorRequired]
                public string Property1 { get; set; }
            }
            """));

        var generated = CompileToCSharp("""
            <ComponentWithEditorRequiredParameters @bind-Property1:get="myField" @bind-Property1:set="OnFieldChanged" />

            @code {
                private string myField = "Some Value";
                private void OnFieldChanged(string value) { }
            }
            """);

        CompileToAssembly(generated);
        Assert.Empty(generated.RazorDiagnostics);
    }

    [IntegrationTestFact, WorkItem("https://github.com/dotnet/razor/issues/10553")]
    public void Component_WithEditorRequiredParameter_ValueSpecifiedUsingBindGetSet_EventCallbackRequired()
    {
        AdditionalSyntaxTrees.Add(Parse("""
            using System;
            using Microsoft.AspNetCore.Components;

            namespace Test;

            public class ComponentWithEditorRequiredParameters : ComponentBase
            {
                [Parameter, EditorRequired]
                public string Property1 { get; set; }

                [Parameter, EditorRequired]
                public EventCallback<string> Property1Changed { get; set; }
            }
            """));

        var generated = CompileToCSharp("""
            <ComponentWithEditorRequiredParameters @bind-Property1:get="myField" @bind-Property1:set="OnFieldChanged" />

            @code {
                private string myField = "Some Value";
                private void OnFieldChanged(string value) { }
            }
            """);

        CompileToAssembly(generated);
        generated.RazorDiagnostics.Verify();
    }

    [IntegrationTestFact, WorkItem("https://github.com/dotnet/razor/issues/10553")]
    public void Component_WithEditorRequiredParameter_ValueSpecifiedUsingBindGetSet_ExpressionRequired()
    {
        AdditionalSyntaxTrees.Add(Parse("""
            using System;
            using System.Linq.Expressions;
            using Microsoft.AspNetCore.Components;

            namespace Test;

            public class ComponentWithEditorRequiredParameters : ComponentBase
            {
                [Parameter, EditorRequired]
                public string Property1 { get; set; }

                [Parameter, EditorRequired]
                public EventCallback<string> Property1Changed { get; set; }

                [Parameter, EditorRequired]
                public Expression<Func<string>> Property1Expression { get; set; }
            }
            """));

        var generated = CompileToCSharp("""
            <ComponentWithEditorRequiredParameters @bind-Property1:get="myField" @bind-Property1:set="OnFieldChanged" />

            @code {
                private string myField = "Some Value";
                private void OnFieldChanged(string value) { }
            }
            """);

        CompileToAssembly(generated);
        generated.RazorDiagnostics.Verify();
    }

    [IntegrationTestFact, WorkItem("https://github.com/dotnet/razor/issues/10553")]
    public void Component_WithEditorRequiredParameter_ValueSpecifiedUsingBindGet()
    {
        AdditionalSyntaxTrees.Add(Parse("""
            using System;
            using Microsoft.AspNetCore.Components;

            namespace Test;

            public class ComponentWithEditorRequiredParameters : ComponentBase
            {
                [Parameter]
                [EditorRequired]
                public string Property1 { get; set; }
            }
            """));

        var generated = CompileToCSharp("""
            <ComponentWithEditorRequiredParameters @bind-Property1:get="myField" />

            @code {
                private string myField = "Some Value";
            }
            """);

        CompileToAssembly(generated);
        Assert.Empty(generated.RazorDiagnostics);
    }

    [IntegrationTestFact]
    public void Component_WithEditorRequiredParameter_ValueSpecifiedUsingBindGet_DifferentCasing()
    {
        AdditionalSyntaxTrees.Add(Parse("""
            using Microsoft.AspNetCore.Components;
            namespace Test;
            public class ComponentWithEditorRequiredParameters : ComponentBase
            {
                [Parameter, EditorRequired] public string Property1 { get; set; }
            }
            """));
        var generated = CompileToCSharp("""
            <ComponentWithEditorRequiredParameters @bind-property1:get="myField" />
            @code {
                private string myField = "Some Value";
            }
            """);
        generated.RazorDiagnostics.Verify(
            // x:\dir\subdir\Test\TestComponent.cshtml(1,1): warning RZ2012: Component 'ComponentWithEditorRequiredParameters' expects a value for the parameter 'Property1', but a value may not have been provided.
            Diagnostic("RZ2012").WithLocation(1, 1));
    }

    [IntegrationTestFact, WorkItem("https://github.com/dotnet/razor/issues/10553")]
    public void Component_WithEditorRequiredParameter_ValueSpecifiedUsingBindSet()
    {
        AdditionalSyntaxTrees.Add(Parse("""
            using System;
            using Microsoft.AspNetCore.Components;

            namespace Test;

            public class ComponentWithEditorRequiredParameters : ComponentBase
            {
                [Parameter]
                [EditorRequired]
                public string Property1 { get; set; }
            }
            """));

        var generated = CompileToCSharp("""
            <ComponentWithEditorRequiredParameters @bind-Property1:set="OnFieldChanged" />

            @code {
                private void OnFieldChanged(string value) { }
            }
            """);

        var compiled = CompileToAssembly(generated);
        generated.RazorDiagnostics.Verify(
            // x:\dir\subdir\Test\TestComponent.cshtml(1,61): error RZ10016: Attribute 'bind-Property1:set' was used but no attribute 'bind-Property1:get' was found.
            Diagnostic("RZ10016").WithLocation(1, 61));
    }

    [IntegrationTestFact]
    public void Component_WithEditorRequiredChildContent_NoValueSpecified()
    {
        AdditionalSyntaxTrees.Add(Parse(@"
using System;
using Microsoft.AspNetCore.Components;
namespace Test
{
    public class ComponentWithEditorRequiredChildContent : ComponentBase
    {
        [Parameter]
        [EditorRequired]
        public RenderFragment ChildContent { get; set; }
    }
}
"));
        var generated = CompileToCSharp(@"
<ComponentWithEditorRequiredChildContent />
");
        AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
        AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
        CompileToAssembly(generated);

        var diagnostics = Assert.Single(generated.RazorDiagnostics);
        Assert.Equal(RazorDiagnosticSeverity.Warning, diagnostics.Severity);
        Assert.Equal("RZ2012", diagnostics.Id);
    }

    [IntegrationTestFact]
    public void Component_WithEditorRequiredChildContent_ValueSpecified_WithoutName()
    {
        AdditionalSyntaxTrees.Add(Parse(@"
using System;
using Microsoft.AspNetCore.Components;
namespace Test
{
    public class ComponentWithEditorRequiredChildContent : ComponentBase
    {
        [Parameter]
        [EditorRequired]
        public RenderFragment ChildContent { get; set; }
    }
}
"));
        var generated = CompileToCSharp(@"
<ComponentWithEditorRequiredChildContent>
    <h1>Hello World</h1>
</ComponentWithEditorRequiredChildContent>

");
        AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
        AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
        CompileToAssembly(generated);

        Assert.Empty(generated.RazorDiagnostics);
    }

    [IntegrationTestFact]
    public void Component_WithEditorRequiredChildContent_ValueSpecifiedAsText_WithoutName()
    {
        AdditionalSyntaxTrees.Add(Parse(@"
using System;
using Microsoft.AspNetCore.Components;
namespace Test
{
    public class ComponentWithEditorRequiredChildContent : ComponentBase
    {
        [Parameter]
        [EditorRequired]
        public RenderFragment ChildContent { get; set; }
    }
}
"));
        var generated = CompileToCSharp(@"
<ComponentWithEditorRequiredChildContent>This is some text</ComponentWithEditorRequiredChildContent>

");
        AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
        AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
        CompileToAssembly(generated);

        Assert.Empty(generated.RazorDiagnostics);
    }

    [IntegrationTestFact]
    public void Component_WithEditorRequiredChildContent_ValueSpecified()
    {
        AdditionalSyntaxTrees.Add(Parse(@"
using System;
using Microsoft.AspNetCore.Components;
namespace Test
{
    public class ComponentWithEditorRequiredChildContent : ComponentBase
    {
        [Parameter]
        [EditorRequired]
        public RenderFragment ChildContent { get; set; }
    }
}
"));
        var generated = CompileToCSharp(@"
<ComponentWithEditorRequiredChildContent>
    <ChildContent>
        <h1>Hello World</h1>
    </ChildContent>
</ComponentWithEditorRequiredChildContent>

");
        AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
        AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
        CompileToAssembly(generated);

        Assert.Empty(generated.RazorDiagnostics);
    }

    [IntegrationTestFact]
    public void Component_WithEditorRequiredNamedChildContent_NoValueSpecified()
    {
        AdditionalSyntaxTrees.Add(Parse(@"
using System;
using Microsoft.AspNetCore.Components;
namespace Test
{
    public class ComponentWithEditorRequiredChildContent : ComponentBase
    {
        [Parameter]
        [EditorRequired]
        public RenderFragment Found { get; set; }

        [Parameter]
        public RenderFragment NotFound { get; set; }
    }
}
"));
        var generated = CompileToCSharp(@"
<ComponentWithEditorRequiredChildContent>
</ComponentWithEditorRequiredChildContent>

");
        AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
        AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
        CompileToAssembly(generated);

        var diagnostics = Assert.Single(generated.RazorDiagnostics);
        Assert.Equal(RazorDiagnosticSeverity.Warning, diagnostics.Severity);
        Assert.Equal("RZ2012", diagnostics.Id);
    }

    [IntegrationTestFact]
    public void Component_WithEditorRequiredNamedChildContent_ValueSpecified()
    {
        AdditionalSyntaxTrees.Add(Parse(@"
using System;
using Microsoft.AspNetCore.Components;
namespace Test
{
    public class ComponentWithEditorRequiredChildContent : ComponentBase
    {
        [Parameter]
        [EditorRequired]
        public RenderFragment Found { get; set; }

        [Parameter]
        public RenderFragment NotFound { get; set; }
    }
}
"));
        var generated = CompileToCSharp(@"
<ComponentWithEditorRequiredChildContent>
    <Found><h1>Here's Johnny!</h1></Found>
</ComponentWithEditorRequiredChildContent>

");
        AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
        AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
        CompileToAssembly(generated);

        Assert.Empty(generated.RazorDiagnostics);
    }

    [IntegrationTestFact, WorkItem("https://github.com/dotnet/aspnetcore/issues/18042")]
    public void AddAttribute_ImplicitStringConversion_TypeInference()
    {
        _configuration = base.Configuration with { LanguageVersion = RazorLanguageVersion.Version_7_0 };

        AdditionalSyntaxTrees.Add(Parse("""
            using Microsoft.AspNetCore.Components;

            namespace Test;

            public class MyClass<T>
            {
                public static implicit operator string(MyClass<T> c) => throw null!;
            }

            public class MyComponent<T> : ComponentBase
            {
                [Parameter]
                public MyClass<T> MyParameter { get; set; } = null!;

                [Parameter]
                public bool BoolParameter { get; set; }

                [Parameter]
                public string StringParameter { get; set; } = null!;

                [Parameter]
                public System.Delegate DelegateParameter { get; set; } = null!;

                [Parameter]
                public object ObjectParameter { get; set; } = null!;
            }
            """));

        var generated = CompileToCSharp("""
            <MyComponent MyParameter="c"
                BoolParameter="true"
                StringParameter="str"
                DelegateParameter="() => { }"
                ObjectParameter="c" />

            @code {
                private readonly MyClass<string> c = new();
            }
            """);

        AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
        AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
        CompileToAssembly(generated);
    }

    [IntegrationTestFact, WorkItem("https://github.com/dotnet/aspnetcore/issues/18042")]
    public void AddAttribute_ImplicitStringConversion_Bind()
    {
        _configuration = base.Configuration with { LanguageVersion = RazorLanguageVersion.Version_7_0 };

        AdditionalSyntaxTrees.Add(Parse("""
            using Microsoft.AspNetCore.Components;

            namespace Test;

            public class MyClass<T>
            {
                public static implicit operator string(MyClass<T> c) => throw null!;
            }

            public class MyComponent<T> : ComponentBase
            {
                [Parameter]
                public MyClass<T> MyParameter { get; set; }

                [Parameter]
                public EventCallback<MyClass<T>> MyParameterChanged { get; set; }

                [Parameter]
                public bool BoolParameter { get; set; }

                [Parameter]
                public string StringParameter { get; set; } = null!;

                [Parameter]
                public System.Delegate DelegateParameter { get; set; } = null!;

                [Parameter]
                public object ObjectParameter { get; set; } = null!;
            }
            """));

        var generated = CompileToCSharp("""
            <MyComponent @bind-MyParameter="c"
                BoolParameter="true"
                StringParameter="str"
                DelegateParameter="() => { }"
                ObjectParameter="c" />

            @code {
                private MyClass<string> c = new();
            }
            """);

        AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
        AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
        CompileToAssembly(generated);
    }

    [IntegrationTestFact, WorkItem("https://github.com/dotnet/aspnetcore/issues/18042")]
    public void AddAttribute_ImplicitStringConversion_CustomEvent()
    {
        _configuration = base.Configuration with { LanguageVersion = RazorLanguageVersion.Version_7_0 };

        AdditionalSyntaxTrees.Add(Parse("""
            using Microsoft.AspNetCore.Components;

            namespace Test;

            public class MyClass<T>
            {
                public static implicit operator string(MyClass<T> c) => throw null!;
            }

            public class MyComponent<T> : ComponentBase
            {
                [Parameter]
                public MyClass<T> MyParameter { get; set; }

                [Parameter]
                public EventCallback MyEvent { get; set; }

                [Parameter]
                public bool BoolParameter { get; set; }

                [Parameter]
                public string StringParameter { get; set; } = null!;

                [Parameter]
                public System.Delegate DelegateParameter { get; set; } = null!;

                [Parameter]
                public object ObjectParameter { get; set; } = null!;
            }
            """));

        var generated = CompileToCSharp("""
            <MyComponent MyParameter="c"
                MyEvent="() => { }"
                BoolParameter="true"
                StringParameter="str"
                DelegateParameter="() => { }"
                ObjectParameter="c" />

            @code {
                private MyClass<string> c = new();
            }
            """);

        AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
        AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
        CompileToAssembly(generated);
    }

    [IntegrationTestFact, WorkItem("https://github.com/dotnet/aspnetcore/issues/18042")]
    public void AddAttribute_ImplicitStringConversion_BindUnknown()
    {
        _configuration = base.Configuration with { LanguageVersion = RazorLanguageVersion.Version_7_0 };

        AdditionalSyntaxTrees.Add(Parse("""
            using Microsoft.AspNetCore.Components;

            namespace Test;

            public class MyClass
            {
                public static implicit operator string(MyClass c) => throw null!;
            }

            public class MyComponent : ComponentBase
            {
            }
            """));

        var generated = CompileToCSharp("""
            <MyComponent @bind-Value="c" />

            @code {
                private MyClass c = new();
            }
            """);

        AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
        AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
        CompileToAssembly(generated);
    }

    [IntegrationTestFact, WorkItem("https://github.com/dotnet/aspnetcore/issues/18042")]
    public void AddAttribute_ImplicitStringConversion_BindUnknown_Assignment()
    {
        _configuration = base.Configuration with { LanguageVersion = RazorLanguageVersion.Version_7_0 };

        AdditionalSyntaxTrees.Add(Parse("""
            using Microsoft.AspNetCore.Components;

            namespace Test;

            public class MyClass
            {
                public static implicit operator string(MyClass c) => throw null!;
            }

            public class MyComponent : ComponentBase
            {
            }
            """));

        var generated = CompileToCSharp("""
            <MyComponent @bind-Value="c1 = c2" />

            @code {
                private MyClass c1 = new();
                private MyClass c2 = new();
            }
            """);

        AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
        AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
        CompileToAssembly(generated);
    }

    [IntegrationTestFact, WorkItem("https://github.com/dotnet/aspnetcore/issues/18042")]
    public void AddAttribute_ImplicitBooleanConversion()
    {
        _configuration = base.Configuration with { LanguageVersion = RazorLanguageVersion.Version_7_0 };

        AdditionalSyntaxTrees.Add(Parse("""
            using Microsoft.AspNetCore.Components;

            namespace Test;

            public class MyClass<T>
            {
                public static implicit operator bool(MyClass<T> c) => throw null!;
            }

            public class MyComponent<T> : ComponentBase
            {
                [Parameter]
                public MyClass<T> MyParameter { get; set; }

                [Parameter]
                public bool BoolParameter { get; set; }
            }
            """));

        var generated = CompileToCSharp("""
            <MyComponent MyParameter="c" BoolParameter="c" />

            @code {
                private MyClass<string> c = new();
            }
            """);

        AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
        AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
        CompileToAssembly(generated);
    }

    [IntegrationTestFact, WorkItem("https://github.com/dotnet/aspnetcore/issues/48778")]
    public void ImplicitStringConversion_ParameterCasing_AddAttribute()
    {
        _configuration = base.Configuration with { LanguageVersion = RazorLanguageVersion.Version_7_0 };

        AdditionalSyntaxTrees.Add(Parse("""
            using Microsoft.AspNetCore.Components;

            namespace Test;

            public class MyClass
            {
                public static implicit operator string(MyClass c) => "";
            }

            public class MyComponent : ComponentBase
            {
                [Parameter] public string Placeholder { get; set; } = "";
            }
            """));

        var generated = CompileToCSharp("""
            <MyComponent PlaceHolder="@(new MyClass())" />
            """);

        AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
        AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
        CompileToAssembly(generated);
    }

    [IntegrationTestFact, WorkItem("https://github.com/dotnet/aspnetcore/issues/48778")]
    public void ImplicitStringConversion_ParameterCasing_AddComponentParameter()
    {
        AdditionalSyntaxTrees.Add(Parse("""
            using Microsoft.AspNetCore.Components;

            namespace Test;

            public class MyClass
            {
                public static implicit operator string(MyClass c) => "";
            }

            public class MyComponent : ComponentBase
            {
                [Parameter] public string Placeholder { get; set; } = "";
            }
            """));

        var generated = CompileToCSharp("""
            <MyComponent PlaceHolder="@(new MyClass())" />
            """);

        AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
        AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
        CompileToAssembly(generated);
    }

    [IntegrationTestFact, WorkItem("https://github.com/dotnet/aspnetcore/issues/48778")]
    public void ImplicitStringConversion_ParameterCasing_Multiple()
    {
        AdditionalSyntaxTrees.Add(Parse("""
            using Microsoft.AspNetCore.Components;

            namespace Test;

            public class MyClass
            {
                public static implicit operator string(MyClass c) => "";
            }

            public class MyComponent : ComponentBase
            {
                [Parameter] public string Placeholder { get; set; } = "";
                [Parameter] public string PlaceHolder { get; set; } = "";
            }
            """));

        var generated = CompileToCSharp("""
            <MyComponent PlaceHolder="@(new MyClass())" />
            """);

        AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
        AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
        CompileToAssembly(generated);
    }

    [IntegrationTestFact, WorkItem("https://github.com/dotnet/aspnetcore/issues/48778")]
    public void ImplicitStringConversion_ParameterCasing_Bind()
    {
        AdditionalSyntaxTrees.Add(Parse("""
            using Microsoft.AspNetCore.Components;

            namespace Test;

            public class MyComponent : ComponentBase
            {
                [Parameter] public string Placeholder { get; set; } = "";
                [Parameter] public EventCallback<string> PlaceholderChanged { get; set; }
            }
            """));

        var generated = CompileToCSharp("""
            <MyComponent @bind-PlaceHolder="s" />

            @code {
                private string s = "abc";
            }
            """);

        AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
        AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
        CompileToAssembly(generated);
    }

    [IntegrationTestFact, WorkItem("https://github.com/dotnet/aspnetcore/issues/48778")]
    public void ImplicitStringConversion_ParameterCasing_Bind_02()
    {
        AdditionalSyntaxTrees.Add(Parse("""
            using Microsoft.AspNetCore.Components;

            namespace Test;

            public class MyComponent : ComponentBase
            {
                [Parameter] public string Placeholder { get; set; } = "";
                [Parameter] public EventCallback<string> PlaceholderChanged { get; set; }
            }
            """));

        var generated = CompileToCSharp("""
            <MyComponent @Bind-Placeholder="@s" />

            @code {
                private string s = "abc";
            }
            """);

        AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
        AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
        CompileToAssembly(generated);
    }

    [IntegrationTestFact, WorkItem("https://github.com/dotnet/aspnetcore/issues/48778")]
    public void ImplicitStringConversion_ParameterCasing_Bind_03()
    {
        AdditionalSyntaxTrees.Add(Parse("""
            using Microsoft.AspNetCore.Components;

            namespace Test;

            public class MyComponent : ComponentBase
            {
                [Parameter] public string Placeholder { get; set; } = "";
                [Parameter] public EventCallback<string> PlaceholderChanged { get; set; }
            }
            """));

        var generated = CompileToCSharp("""
            <MyComponent @bind-Placeholder:Get="s" @bind-Placeholder:set="Changed" />

            @code {
                private string s = "abc";
                private void Changed(string s) { }
            }
            """);

        AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
        AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
        CompileToAssembly(generated);
    }

    [IntegrationTestFact, WorkItem("https://dev.azure.com/devdiv/DevDiv/_workitems/edit/1869483")]
    public void AddComponentParameter()
    {
        var generated = CompileToCSharp("""
            @typeparam T

            <TestComponent Param="42" />

            @code {
                [Parameter]
                public T Param { get; set; }
            }
            """);

        CompileToAssembly(generated);

        if (DesignTime)
        {
            // In design-time, AddComponentParameter shouldn't be used.
            Assert.Contains("AddAttribute", generated.Code);
            Assert.DoesNotContain("AddComponentParameter", generated.Code);
        }
        else
        {
            Assert.DoesNotContain("AddAttribute", generated.Code);
            Assert.Contains("AddComponentParameter", generated.Code);
        }
    }

    [IntegrationTestFact]
    public void AddComponentParameter_GlobalNamespace()
    {
        // Arrange
        AdditionalSyntaxTrees.Add(Parse(@"
using System;
using Microsoft.AspNetCore.Components;

public class MyComponent : ComponentBase
{
    [Parameter]
    public string Value { get; set; }
}"));

        // Act
        var generated = CompileToCSharp(@"<MyComponent Value=""Hello"" />");

        // Assert
        AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
        AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
        CompileToAssembly(generated);
    }

    [IntegrationTestFact]
    public void AddComponentParameter_WithNameof()
    {
        // Arrange
        AdditionalSyntaxTrees.Add(Parse(@"
using System;
using Microsoft.AspNetCore.Components;

namespace Test
{
    public class MyComponent : ComponentBase
    {
        [Parameter]
        public string Value { get; set; }
    }
}"));

        // Act
        var generated = CompileToCSharp(@"
<MyComponent Value=""Hello"" />
@code {
    public string nameof(string s) => string.Empty;
}");

        // Assert
        AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
        AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
        CompileToAssembly(generated, designTime ? [] : [
                    // (21,55): error CS0120: An object reference is required for the non-static field, method, or property 'MyComponent.Value'
                    //             __builder.AddComponentParameter(1, nameof(global::Test.MyComponent.
                    Diagnostic(ErrorCode.ERR_ObjectRequired, "global::Test.MyComponent.\r\n#nullable restore\r\n#line (1,14)-(1,19) \"x:\\dir\\subdir\\Test\\TestComponent.cshtml\"\r\nValue").WithArguments("Test.MyComponent.Value").WithLocation(21, 55)
            ]);
    }

    [IntegrationTestFact]
    public void AddComponentParameter_EscapedComponentName()
    {
        // Arrange
        AdditionalSyntaxTrees.Add(Parse(@"
using System;
using Microsoft.AspNetCore.Components;

namespace Test
{
    public class @int : ComponentBase
    {
        [Parameter]
        public string Value { get; set; }
    }
}"));

        // Act
        var generated = CompileToCSharp(@"<int Value=""Hello"" />");

        // Assert
        AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
        AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
        CompileToAssembly(generated);
    }

    [IntegrationTestFact]
    public void AddComponentParameter_DynamicComponentName()
    {
        // Arrange
        AdditionalSyntaxTrees.Add(Parse(@"
using System;
using Microsoft.AspNetCore.Components;

namespace Test
{
    public class dynamic : ComponentBase
    {
        [Parameter]
        public string Value { get; set; }
    }
}"));

        // Act
        var generated = CompileToCSharp(@"<dynamic Value=""Hello"" />");

        // Assert
        AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
        AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
        CompileToAssembly(generated);
    }

    [IntegrationTestFact, WorkItem("https://github.com/dotnet/razor/issues/10965")]
    public void InvalidCode_EmptyTransition()
    {
        // Act
        var generated = CompileToCSharp("""
        <TestComponent Value="Hello" />

        @

        @code {
            [Parameter] public int Param { get; set; }
        }
        """);

        // Assert
        AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
        AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
        CompileToAssembly(generated, DesignTime ? [
            // x:\dir\subdir\Test\TestComponent.cshtml(3,7): error CS1525: Invalid expression term ';'
            // __o = ;
            Diagnostic(ErrorCode.ERR_InvalidExprTerm, ";").WithArguments(";").WithLocation(3, 7)
            ] : [
            // x:\dir\subdir\Test\TestComponent.cshtml(3,2): error CS1525: Invalid expression term ')'
            // __builder.AddContent(3, 
            Diagnostic(ErrorCode.ERR_InvalidExprTerm, "").WithArguments(")").WithLocation(3, 2)
            ]);
    }

    [IntegrationTestFact]
    public void ExplicitExpression_HtmlOnly()
    {
        // Act
        var generated = CompileToCSharp("""
            @{
                <p></p>
            }
            """);

        // Assert
        AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
        AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
        CompileToAssembly(generated);
    }

    [IntegrationTestFact]
    public void ExplicitExpression_Whitespace()
    {
        // Act
        var generated = CompileToCSharp("""
            @{
            }
            """);

        // Assert
        AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
        AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
        CompileToAssembly(generated);
    }

    [IntegrationTestFact, WorkItem("https://github.com/dotnet/razor/issues/11551")]
    public void LayoutDirective()
    {
        // Act
        var generated = CompileToCSharp("""
            @layout System.Object
            """);

        // Assert
        AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
        AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
        CompileToAssembly(generated);
    }

    [IntegrationTestFact]
    public void Component_AddContent_Multiline()
    {
        // Act
        var generated = CompileToCSharp(""""
            @(@"This
            is
            a
            multiline
            string")
            """");

        // Assert
        AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
        AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
        var result = CompileToAssembly(generated);
        AssertSequencePointsMatchBaseline(result, generated.CodeDocument);
    }

    #endregion

    #region Bind

    [IntegrationTestFact]
    public void BindToComponent_SpecifiesValue_WithMatchingProperties()
    {
        // Arrange
        AdditionalSyntaxTrees.Add(Parse(@"
using System;
using Microsoft.AspNetCore.Components;

namespace Test
{
    public class MyComponent : ComponentBase
    {
        [Parameter]
        public int Value { get; set; }

        [Parameter]
        public Action<int> ValueChanged { get; set; }
    }
}"));

        // Act
        var generated = CompileToCSharp(@"
<MyComponent @bind-Value=""ParentValue"" />
@code {
    public int ParentValue { get; set; } = 42;
}");

        // Assert
        AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
        AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
        CompileToAssembly(generated);
    }

    [IntegrationTestFact]
    public void BindToComponent_SpecifiesValue_WithMatchingProperties_WithNameof()
    {
        // Arrange
        AdditionalSyntaxTrees.Add(Parse(@"
using System;
using Microsoft.AspNetCore.Components;

namespace Test
{
    public class MyComponent : ComponentBase
    {
        [Parameter]
        public int Value { get; set; }

        [Parameter]
        public Action<int> ValueChanged { get; set; }
    }
}"));

        // Act
        var generated = CompileToCSharp(@"
<MyComponent @bind-Value=""ParentValue"" />
@code {
    public int ParentValue { get; set; } = 42;

    public string nameof(string s) => string.Empty;
}");

        // Assert
        AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
        AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
        CompileToAssembly(generated, designTime ? [] : [
                // (21,55): error CS0120: An object reference is required for the non-static field, method, or property 'MyComponent.Value'
                //             __builder.AddComponentParameter(1, nameof(global::Test.MyComponent.
                Diagnostic(ErrorCode.ERR_ObjectRequired, "global::Test.MyComponent.\r\n#nullable restore\r\n#line (1,20)-(1,25) \"x:\\dir\\subdir\\Test\\TestComponent.cshtml\"\r\nValue").WithArguments("Test.MyComponent.Value").WithLocation(21, 55),
                // (38,55): error CS0120: An object reference is required for the non-static field, method, or property 'MyComponent.ValueChanged'
                //             __builder.AddComponentParameter(2, nameof(global::Test.MyComponent.ValueChanged), (global::System.Action<System.Int32>)(__value => ParentValue = __value));
                Diagnostic(ErrorCode.ERR_ObjectRequired, "global::Test.MyComponent.ValueChanged").WithArguments("Test.MyComponent.ValueChanged").WithLocation(38, 55)
            ]);
    }

    [IntegrationTestFact]
    public void BindToComponent_SpecifiesValue_WithMatchingProperties_EscapedComponentName()
    {
        // Arrange
        AdditionalSyntaxTrees.Add(Parse(@"
using System;
using Microsoft.AspNetCore.Components;

namespace Test
{
    public class @int : ComponentBase
    {
        [Parameter]
        public int Value { get; set; }

        [Parameter]
        public Action<int> ValueChanged { get; set; }
    }
}"));

        // Act
        var generated = CompileToCSharp(@"
<int @bind-Value=""ParentValue"" />
@code {
    public int ParentValue { get; set; } = 42;
}");

        // Assert
        AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
        AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
        CompileToAssembly(generated);
    }

    [IntegrationTestFact]
    public void BindToComponent_SpecifiesValue_WithMatchingProperties_DynamicComponentName()
    {
        // Arrange
        AdditionalSyntaxTrees.Add(Parse(@"
using System;
using Microsoft.AspNetCore.Components;

namespace Test
{
    public class dynamic : ComponentBase
    {
        [Parameter]
        public int Value { get; set; }

        [Parameter]
        public Action<int> ValueChanged { get; set; }
    }
}"));

        // Act
        var generated = CompileToCSharp(@"
<dynamic @bind-Value=""ParentValue"" />
@code {
    public int ParentValue { get; set; } = 42;
}");

        // Assert
        AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
        AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
        CompileToAssembly(generated);
    }

    [IntegrationTestFact]
    public void BindToComponent_WithStringAttribute_DoesNotUseStringSyntax()
    {
        // Arrange
        AdditionalSyntaxTrees.Add(Parse(@"
using System;
using Microsoft.AspNetCore.Components;

namespace Test
{
    public class InputText : ComponentBase
    {
        [Parameter]
        public string Value { get; set; }

        [Parameter]
        public Action<string> ValueChanged { get; set; }
    }
}"));

        AdditionalSyntaxTrees.Add(Parse(@"
using System;

namespace Test
{
    public class Person
    {
        public string Name { get; set; }
    }
}
"));

        // Act
        var generated = CompileToCSharp(@"
<InputText @bind-Value=""person.Name"" />

@functions
{
    Person person = new Person();
}");

        // Assert
        AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
        AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
        CompileToAssembly(generated);
    }

    [IntegrationTestFact]
    public void BindToComponent_TypeChecked_WithMatchingProperties()
    {
        // Arrange
        AdditionalSyntaxTrees.Add(Parse(@"
using System;
using Microsoft.AspNetCore.Components;

namespace Test
{
    public class MyComponent : ComponentBase
    {
        [Parameter]
        public int Value { get; set; }

        [Parameter]
        public Action<int> ValueChanged { get; set; }
    }
}"));

        // Act
        var generated = CompileToCSharp(@"
<MyComponent @bind-Value=""ParentValue"" />
@code {
    public string ParentValue { get; set; } = ""42"";
}");

        // Assert
        AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
        AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);

        CompileToAssembly(generated, DesignTime
            ? [// x:\dir\subdir\Test\TestComponent.cshtml(1,27): error CS1503: Argument 1: cannot convert from 'string' to 'int'
               // ParentValue
               Diagnostic(ErrorCode.ERR_BadArgType, "ParentValue").WithArguments("1", "string", "int").WithLocation(1, 27),
               // (37,38): error CS0029: Cannot implicitly convert type 'int' to 'string'
               //             __builder.AddComponentParameter(2, "ValueChanged", (global::System.Action<System.Int32>)(__value => ParentValue = __value));
               Diagnostic(ErrorCode.ERR_NoImplicitConv, "__value").WithArguments("int", "string").WithLocation(37, 38)]
            : [// x:\dir\subdir\Test\TestComponent.cshtml(1,27): error CS1503: Argument 1: cannot convert from 'string' to 'int'
               // ParentValue
               Diagnostic(ErrorCode.ERR_BadArgType, "ParentValue").WithArguments("1", "string", "int").WithLocation(1, 27),
               // (38,166): error CS0029: Cannot implicitly convert type 'int' to 'string'
               //             __builder.AddComponentParameter(2, nameof(global::Test.MyComponent.ValueChanged), (global::System.Action<global::System.Int32>)(__value => ParentValue = __value));
               Diagnostic(ErrorCode.ERR_NoImplicitConv, "__value").WithArguments("int", "string").WithLocation(38, 166)]);
    }

    [IntegrationTestFact]
    public void BindToComponent_EventCallback_SpecifiesValue_WithMatchingProperties()
    {
        // Arrange
        AdditionalSyntaxTrees.Add(Parse(@"
using System;
using Microsoft.AspNetCore.Components;

namespace Test
{
    public class MyComponent : ComponentBase
    {
        [Parameter]
        public int Value { get; set; }

        [Parameter]
        public EventCallback<int> ValueChanged { get; set; }
    }
}"));

        // Act
        var generated = CompileToCSharp(@"
<MyComponent @bind-Value=""ParentValue"" />
@code {
    public int ParentValue { get; set; } = 42;
}");

        // Assert
        AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
        AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
        CompileToAssembly(generated);
    }

    [IntegrationTestFact]
    public void BindToComponent_EventCallback_TypeChecked_WithMatchingProperties()
    {
        // Arrange
        AdditionalSyntaxTrees.Add(Parse(@"
using System;
using Microsoft.AspNetCore.Components;

namespace Test
{
    public class MyComponent : ComponentBase
    {
        [Parameter]
        public int Value { get; set; }

        [Parameter]
        public EventCallback<int> ValueChanged { get; set; }
    }
}"));

        // Act
        var generated = CompileToCSharp(@"
<MyComponent @bind-Value=""ParentValue"" />
@code {
    public string ParentValue { get; set; } = ""42"";
}");

        // Assert
        AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
        AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);

        CompileToAssembly(generated, DesignTime
            ? [// x:\dir\subdir\Test\TestComponent.cshtml(1,27): error CS1503: Argument 1: cannot convert from 'string' to 'int'
               //                           ParentValue
               Diagnostic(ErrorCode.ERR_BadArgType, "ParentValue").WithArguments("1", "string", "int").WithLocation(1, 27),
               // (37,13): error CS1503: Argument 2: cannot convert from 'Microsoft.AspNetCore.Components.EventCallback<string>' to 'Microsoft.AspNetCore.Components.EventCallback'
               //             global::Microsoft.AspNetCore.Components.CompilerServices.RuntimeHelpers.CreateInferredEventCallback(this, __value => ParentValue = __value, ParentValue)));
               Diagnostic(ErrorCode.ERR_BadArgType, "global::Microsoft.AspNetCore.Components.CompilerServices.RuntimeHelpers.CreateInferredEventCallback(this, __value => ParentValue = __value, ParentValue)").WithArguments("2", "Microsoft.AspNetCore.Components.EventCallback<string>", "Microsoft.AspNetCore.Components.EventCallback").WithLocation(37, 13)]
            : [// x:\dir\subdir\Test\TestComponent.cshtml(1,27): error CS1503: Argument 1: cannot convert from 'string' to 'int'
               //                           ParentValue
               Diagnostic(ErrorCode.ERR_BadArgType, "ParentValue").WithArguments("1", "string", "int").WithLocation(1, 27),
               // (38,351): error CS1503: Argument 2: cannot convert from 'Microsoft.AspNetCore.Components.EventCallback<string>' to 'Microsoft.AspNetCore.Components.EventCallback'
               //             global::Microsoft.AspNetCore.Components.CompilerServices.RuntimeHelpers.CreateInferredEventCallback(this, __value => ParentValue = __value, ParentValue)));
               Diagnostic(ErrorCode.ERR_BadArgType, "global::Microsoft.AspNetCore.Components.CompilerServices.RuntimeHelpers.CreateInferredEventCallback(this, __value => ParentValue = __value, ParentValue)").WithArguments("2", "Microsoft.AspNetCore.Components.EventCallback<string>", "Microsoft.AspNetCore.Components.EventCallback").WithLocation(38, 351)
            ]
        );
    }

    [IntegrationTestFact]
    public void BindToComponent_SpecifiesValue_WithoutMatchingProperties()
    {
        // Arrange
        AdditionalSyntaxTrees.Add(Parse(@"
using System;
using Microsoft.AspNetCore.Components;

namespace Test
{
    public class MyComponent : ComponentBase
    {
    }
}"));

        // Act
        var generated = CompileToCSharp(@"
<MyComponent @bind-Value=""ParentValue"" />
@code {
    public int ParentValue { get; set; } = 42;
}");

        // Assert
        AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
        AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
        CompileToAssembly(generated);
    }

    [IntegrationTestFact]
    public void BindToComponent_SpecifiesValueAndChangeEvent_WithMatchingProperties()
    {
        // Arrange
        AdditionalSyntaxTrees.Add(Parse(@"
using System;
using Microsoft.AspNetCore.Components;

namespace Test
{
    public class MyComponent : ComponentBase
    {
        [Parameter]
        public int Value { get; set; }

        [Parameter]
        public Action<int> OnChanged { get; set; }
    }
}"));
        // Act
        var generated = CompileToCSharp(@"
<MyComponent @bind-Value=""ParentValue"" @bind-Value:event=""OnChanged"" />
@code {
    public int ParentValue { get; set; } = 42;
}");

        // Assert
        AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
        AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
        CompileToAssembly(generated);
    }

    [IntegrationTestFact]
    public void BindToComponent_SpecifiesValueAndChangeEvent_WithoutMatchingProperties()
    {
        // Arrange
        AdditionalSyntaxTrees.Add(Parse(@"
using System;
using Microsoft.AspNetCore.Components;

namespace Test
{
    public class MyComponent : ComponentBase
    {
    }
}"));

        var generated = CompileToCSharp(@"
<MyComponent @bind-Value=""ParentValue"" @bind-Value:event=""OnChanged"" />
@code {
    public int ParentValue { get; set; } = 42;
}");

        // Assert
        AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
        AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
        CompileToAssembly(generated);
    }

    [IntegrationTestFact]
    public void BindToComponent_SpecifiesValueAndExpression()
    {
        // Arrange
        AdditionalSyntaxTrees.Add(Parse(@"
using System;
using System.Linq.Expressions;
using Microsoft.AspNetCore.Components;

namespace Test
{
    public class MyComponent : ComponentBase
    {
        [Parameter]
        public int Value { get; set; }

        [Parameter]
        public Action<int> ValueChanged { get; set; }

        [Parameter]
        public Expression<Func<int>> ValueExpression { get; set; }
    }
}"));

        // Act
        var generated = CompileToCSharp(@"
<MyComponent @bind-Value=""ParentValue"" />
@code {
    public int ParentValue { get; set; } = 42;
}");

        // Assert
        AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
        AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
        CompileToAssembly(generated);
    }

    [IntegrationTestFact]
    public void BindToComponent_EventCallback_SpecifiesValueAndExpression()
    {
        // Arrange
        AdditionalSyntaxTrees.Add(Parse(@"
using System;
using System.Linq.Expressions;
using Microsoft.AspNetCore.Components;

namespace Test
{
    public class MyComponent : ComponentBase
    {
        [Parameter]
        public int Value { get; set; }

        [Parameter]
        public EventCallback<int> ValueChanged { get; set; }

        [Parameter]
        public Expression<Func<int>> ValueExpression { get; set; }
    }
}"));

        // Act
        var generated = CompileToCSharp(@"
<MyComponent @bind-Value=""ParentValue"" />
@code {
    public int ParentValue { get; set; } = 42;
}");

        // Assert
        AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
        AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
        CompileToAssembly(generated);
    }

    [IntegrationTestFact]
    public void BindToComponent_SpecifiesValueAndExpression_TypeChecked()
    {
        // Arrange
        AdditionalSyntaxTrees.Add(Parse(@"
using System;
using System.Linq.Expressions;
using Microsoft.AspNetCore.Components;

namespace Test
{
    public class MyComponent : ComponentBase
    {
        [Parameter]
        public int Value { get; set; }

        [Parameter]
        public Action<int> ValueChanged { get; set; }

        [Parameter]
        public Expression<Func<string>> ValueExpression { get; set; }
    }
}"));

        // Act
        var generated = CompileToCSharp(@"
<MyComponent @bind-Value=""ParentValue"" />
@code {
    public int ParentValue { get; set; } = 42;
}");


        CompileToAssembly(generated, DesignTime
            ? [// (38,195): error CS0029: Cannot implicitly convert type 'int' to 'string'
               //             __o = global::Microsoft.AspNetCore.Components.CompilerServices.RuntimeHelpers.TypeCheck<global::System.Linq.Expressions.Expression<global::System.Func<global::System.String>>>(() => ParentValue);
               Diagnostic(ErrorCode.ERR_NoImplicitConv, "ParentValue").WithArguments("int", "string").WithLocation(38, 195),
               // (38,195): error CS1662: Cannot convert lambda expression to intended delegate type because some of the return types in the block are not implicitly convertible to the delegate return type
               //             __o = global::Microsoft.AspNetCore.Components.CompilerServices.RuntimeHelpers.TypeCheck<global::System.Linq.Expressions.Expression<global::System.Func<global::System.String>>>(() => ParentValue);
               Diagnostic(ErrorCode.ERR_CantConvAnonMethReturns, "ParentValue").WithArguments("lambda expression").WithLocation(38, 195)]
            : [// (39,274): error CS0029: Cannot implicitly convert type 'int' to 'string'
               //             __builder.AddComponentParameter(3, nameof(global::Test.MyComponent.ValueExpression), global::Microsoft.AspNetCore.Components.CompilerServices.RuntimeHelpers.TypeCheck<global::System.Linq.Expressions.Expression<global::System.Func<global::System.String>>>(() => ParentValue));
               Diagnostic(ErrorCode.ERR_NoImplicitConv, "ParentValue").WithArguments("int", "string").WithLocation(39, 274),
               // (39,274): error CS1662: Cannot convert lambda expression to intended delegate type because some of the return types in the block are not implicitly convertible to the delegate return type
               //             __builder.AddComponentParameter(3, nameof(global::Test.MyComponent.ValueExpression), global::Microsoft.AspNetCore.Components.CompilerServices.RuntimeHelpers.TypeCheck<global::System.Linq.Expressions.Expression<global::System.Func<global::System.String>>>(() => ParentValue));
               Diagnostic(ErrorCode.ERR_CantConvAnonMethReturns, "ParentValue").WithArguments("lambda expression").WithLocation(39, 274)
            ]);
    }

    [IntegrationTestFact]
    public void BindToComponent_SpecifiesValueAndExpression_Generic()
    {
        // Arrange
        AdditionalSyntaxTrees.Add(Parse(@"
using System;
using System.Linq.Expressions;
using Microsoft.AspNetCore.Components;

namespace Test
{
    public class MyComponent<T> : ComponentBase
    {
        [Parameter]
        public T SomeParam { get; set; }

        [Parameter]
        public Action<T> SomeParamChanged { get; set; }

        [Parameter]
        public Expression<Func<T>> SomeParamExpression { get; set; }
    }
}"));

        // Act
        var generated = CompileToCSharp(@"
<MyComponent @bind-SomeParam=""ParentValue"" />
@code {
    public DateTime ParentValue { get; set; } = DateTime.Now;
}");

        // Assert
        AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
        AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
        CompileToAssembly(generated);
    }

    [IntegrationTestFact]
    public void BindToComponent_EventCallback_SpecifiesValueAndExpression_Generic()
    {
        // Arrange
        AdditionalSyntaxTrees.Add(Parse(@"
using System;
using System.Linq.Expressions;
using Microsoft.AspNetCore.Components;

namespace Test
{
    public class MyComponent<T> : ComponentBase
    {
        [Parameter]
        public T SomeParam { get; set; }

        [Parameter]
        public EventCallback<T> SomeParamChanged { get; set; }

        [Parameter]
        public Expression<Func<T>> SomeParamExpression { get; set; }
    }
}"));

        // Act
        var generated = CompileToCSharp(@"
<MyComponent @bind-SomeParam=""ParentValue"" />
@code {
    public DateTime ParentValue { get; set; } = DateTime.Now;
}");

        // Assert
        AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
        AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
        CompileToAssembly(generated);
    }

    [IntegrationTestFact]
    public void BindToComponent_EventCallback_SpecifiesValueAndExpression_NestedGeneric()
    {
        // Arrange
        AdditionalSyntaxTrees.Add(Parse(@"
using System;
using System.Linq.Expressions;
using System.Collections.Generic;
using Microsoft.AspNetCore.Components;

namespace Test
{
    public class MyComponent<T> : ComponentBase
    {
        [Parameter]
        public IEnumerable<T> SomeParam { get; set; }

        [Parameter]
        public EventCallback<IEnumerable<T>> SomeParamChanged { get; set; }

        [Parameter]
        public Expression<Func<IEnumerable<T>>> SomeParamExpression { get; set; }
    }
}"));

        // Act
        var generated = CompileToCSharp(@"
<MyComponent @bind-SomeParam=""ParentValue"" />
@code {
    public IEnumerable<DateTime> ParentValue { get; set; } = new [] { DateTime.Now };
}");

        // Assert
        AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
        AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
        CompileToAssembly(generated);
    }

    [IntegrationTestFact, WorkItem("https://github.com/dotnet/razor/issues/10609")]
    public void BindToComponent_SpecifiesValue_WithMatchingProperties_GlobalNamespaceComponent()
    {
        // Arrange
        AdditionalSyntaxTrees.Add(Parse(@"
using System;
using Microsoft.AspNetCore.Components;

public class MyComponent : ComponentBase
{
    [Parameter]
    public int Value { get; set; }

    [Parameter]
    public Action<int> ValueChanged { get; set; }
}"));

        // Act
        var generated = CompileToCSharp(@"
<MyComponent @bind-Value=""ParentValue"" />
@code {
    public int ParentValue { get; set; } = 42;
}");

        // Assert
        AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
        AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
        CompileToAssembly(generated);
    }

    [IntegrationTestFact]
    public void BindToElement_WritesAttributes()
    {
        // Arrange
        AdditionalSyntaxTrees.Add(Parse(@"
using System;
using Microsoft.AspNetCore.Components;

namespace Test
{
    [BindElement(""div"", null, ""myvalue"", ""myevent"")]
    public static class BindAttributes
    {
    }
}"));

        // Act
        var generated = CompileToCSharp(@"
<div @bind=""@ParentValue"" />
@code {
    public string ParentValue { get; set; } = ""hi"";
}");

        // Assert
        AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
        AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
        CompileToAssembly(generated);
    }

    [IntegrationTestFact]
    public void BindToElement_WithoutCloseTag()
    {
        // Arrange
        AdditionalSyntaxTrees.Add(Parse(@"
using System;
using Microsoft.AspNetCore.Components;

namespace Test
{
    [BindElement(""div"", null, ""myvalue"", ""myevent"")]
    public static class BindAttributes
    {
    }
}"));

        // Act
        var generated = CompileToCSharp(@"
<div>
  <input @bind=""@ParentValue"">
</div>
@code {
    public string ParentValue { get; set; } = ""hi"";
}");

        // Assert
        AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
        AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
        CompileToAssembly(generated);
    }

    [IntegrationTestFact]
    public void BindToElement_WithBindAfterAndSuffix()
    {
        // Arrange
        AdditionalSyntaxTrees.Add(Parse(@"
using System;
using Microsoft.AspNetCore.Components;

namespace Test
{
    [BindElement(""div"", ""myvalue"", ""myvalue"", ""myevent"")]
    public static class BindAttributes
    {
    }
}"));

        // Act
        var generated = CompileToCSharp(@"
<div @bind-myvalue=""@ParentValue"" @bind-myvalue:after=""DoSomething"">
</div>
@code {
    public string ParentValue { get; set; } = ""hi"";

    Task DoSomething()
    {
        return Task.CompletedTask;
    }
}");

        // Assert
        AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
        AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
        CompileToAssembly(generated);
    }

    [IntegrationTestFact]
    public void BindToElement_WithGetSetAndSuffix()
    {
        // Arrange
        AdditionalSyntaxTrees.Add(Parse(@"
using System;
using Microsoft.AspNetCore.Components;

namespace Test
{
    [BindElement(""div"", ""myvalue"", ""myvalue"", ""myevent"")]
    public static class BindAttributes
    {
    }
}"));

        // Act
        var generated = CompileToCSharp(@"
<div @bind-myvalue:get=""@ParentValue"" @bind-myvalue:set=""ValueChanged"">
</div>
@code {
    public string ParentValue { get; set; } = ""hi"";

    Task ValueChanged(string value)
    {
        return Task.CompletedTask;
    }
}");

        // Assert
        AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
        AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
        CompileToAssembly(generated);
    }

    [IntegrationTestFact]
    public void BindToComponent_WithGetSet_TaskReturningDelegate()
    {
        // Arrange
        AdditionalSyntaxTrees.Add(Parse(@"
using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;

namespace Test
{
    public class MyComponent : ComponentBase
    {
        [Parameter]
        public int Value { get; set; }

        [Parameter]
        public Func<int, Task> ValueChanged { get; set; }
    }
}"));

        // Act
        var generated = CompileToCSharp(@"
<MyComponent @bind-Value:get=""ParentValue"" @bind-Value:set=""UpdateValue"" />
@code {
    public int ParentValue { get; set; } = 42;

    public Task UpdateValue(int value) { ParentValue = value; return Task.CompletedTask; }
}");

        // Assert
        AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
        AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
        CompileToAssembly(generated);
    }

    [IntegrationTestFact]
    public void BindToComponent_WithGetSet_TaskReturningLambda()
    {
        // Arrange
        AdditionalSyntaxTrees.Add(Parse(@"
using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;

namespace Test
{
    public class MyComponent : ComponentBase
    {
        [Parameter]
        public int Value { get; set; }

        [Parameter]
        public Func<int, Task> ValueChanged { get; set; }
    }
}"));

        // Act
        var generated = CompileToCSharp(@"
<MyComponent @bind-Value:get=""ParentValue"" @bind-Value:set=""value => { ParentValue = value; return Task.CompletedTask; }"" />
@code {
    public int ParentValue { get; set; } = 42;
}");

        // Assert
        AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
        AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
        CompileToAssembly(generated);
    }

    [IntegrationTestFact]
    public void BindToComponent_WithGetSet_Action()
    {
        // Arrange
        AdditionalSyntaxTrees.Add(Parse(@"
using System;
using Microsoft.AspNetCore.Components;

namespace Test
{
    public class MyComponent : ComponentBase
    {
        [Parameter]
        public int Value { get; set; }

        [Parameter]
        public Action<int> ValueChanged { get; set; }
    }
}"));

        // Act
        var generated = CompileToCSharp(@"
<MyComponent @bind-Value:get=""ParentValue"" @bind-Value:set=""UpdateValue"" />
@code {
    public int ParentValue { get; set; } = 42;

    public void UpdateValue(int value) => ParentValue = value;
}");

        // Assert
        AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
        AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
        CompileToAssembly(generated);
    }

    [IntegrationTestFact]
    public void BindToComponent_WithGetSet_ActionLambda()
    {
        // Arrange
        AdditionalSyntaxTrees.Add(Parse(@"
using System;
using Microsoft.AspNetCore.Components;

namespace Test
{
    public class MyComponent : ComponentBase
    {
        [Parameter]
        public int Value { get; set; }

        [Parameter]
        public Action<int> ValueChanged { get; set; }
    }
}"));

        // Act
        var generated = CompileToCSharp(@"
<MyComponent @bind-Value:get=""ParentValue"" @bind-Value:set=""value => ParentValue = value"" />
@code {
    public int ParentValue { get; set; } = 42;
}");

        // Assert
        AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
        AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
        CompileToAssembly(generated);
    }

    [IntegrationTestFact]
    public void BindToComponent_WithGetSet_EventCallback()
    {
        // Arrange
        AdditionalSyntaxTrees.Add(Parse(@"
using System;
using Microsoft.AspNetCore.Components;
namespace Test
{
    public class MyComponent : ComponentBase
    {
        [Parameter]
        public int Value { get; set; }
        [Parameter]
        public EventCallback<int> ValueChanged { get; set; }
    }
}"));

        // Act
        var generated = CompileToCSharp(@"
<MyComponent @bind-Value:get=""ParentValue"" @bind-Value:set=""UpdateValue"" />
@code {
    public int ParentValue { get; set; } = 42;
    public EventCallback<int> UpdateValue { get; set; }
}");

        // Assert
        AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
        AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
        CompileToAssembly(generated);
    }

    [IntegrationTestFact]
    public void BindToGenericComponent_InferredType_WithGetSet_EventCallback()
    {
        // Arrange
        AdditionalSyntaxTrees.Add(Parse(@"
using System;
using Microsoft.AspNetCore.Components;
namespace Test
{
    public class MyComponent<TValue> : ComponentBase
    {
        [Parameter]
        public TValue Value { get; set; }
        [Parameter]
        public EventCallback<TValue> ValueChanged { get; set; }
    }
    public class CustomValue
    {
    }
}"));

        // Act
        var generated = CompileToCSharp(@"
<MyComponent @bind-Value:get=""ParentValue"" @bind-Value:set=""UpdateValue"" />
@code {
    public CustomValue ParentValue { get; set; } = new CustomValue();
    public EventCallback<CustomValue> UpdateValue { get; set; }
}");

        // Assert
        AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
        AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
        CompileToAssembly(generated);
    }

    [IntegrationTestFact]
    public void BindToGenericComponent_ExplicitType_WithGetSet_EventCallback()
    {
        // Arrange
        AdditionalSyntaxTrees.Add(Parse(@"
using System;
using Microsoft.AspNetCore.Components;
namespace Test
{
    public class MyComponent<TValue> : ComponentBase
    {
        [Parameter]
        public TValue Value { get; set; }
        [Parameter]
        public EventCallback<TValue> ValueChanged { get; set; }
    }
    public class CustomValue
    {
    }
}"));

        // Act
        var generated = CompileToCSharp(@"
<MyComponent TValue=""CustomValue"" @bind-Value:get=""ParentValue"" @bind-Value:set=""UpdateValue"" />
@code {
    public CustomValue ParentValue { get; set; } = new CustomValue();
    public EventCallback<CustomValue> UpdateValue { get; set; }
}");

        // Assert
        AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
        AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
        CompileToAssembly(generated);
    }

    [IntegrationTestFact]
    public void GenericComponentBindToGenericComponent_InferredType_WithGetSet_EventCallback()
    {
        // Arrange
        AdditionalSyntaxTrees.Add(Parse(@"
using System;
using Microsoft.AspNetCore.Components;
namespace Test
{
    public class MyComponent<TValue> : ComponentBase
    {
        [Parameter]
        public TValue Value { get; set; }
        [Parameter]
        public EventCallback<TValue> ValueChanged { get; set; }
    }
}"));

        // Act
        var generated = CompileToCSharp(@"
@typeparam TParam
<MyComponent @bind-Value:get=""ParentValue"" @bind-Value:set=""UpdateValue"" />
@code {
    public TParam ParentValue { get; set; } = default;
    public EventCallback<TParam> UpdateValue { get; set; }
}");

        // Assert
        AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
        AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
        CompileToAssembly(generated);
    }

    [IntegrationTestFact]
    public void GenericBindToGenericComponent_ExplicitType_WithGetSet_EventCallback()
    {
        // Arrange
        AdditionalSyntaxTrees.Add(Parse(@"
using System;
using Microsoft.AspNetCore.Components;
namespace Test
{
    public class MyComponent<TValue> : ComponentBase
    {
        [Parameter]
        public TValue Value { get; set; }
        [Parameter]
        public EventCallback<TValue> ValueChanged { get; set; }
    }
}"));

        // Act
        var generated = CompileToCSharp(@"
@typeparam TParam
<MyComponent TValue=""TParam"" @bind-Value:get=""ParentValue"" @bind-Value:set=""UpdateValue"" />
@code {
    public TParam ParentValue { get; set; } = default;
    public EventCallback<TParam> UpdateValue { get; set; }
}");

        // Assert
        AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
        AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
        CompileToAssembly(generated);
    }

    [IntegrationTestFact]
    public void BindToGenericComponent_InferredType_WithGetSet_Action()
    {
        // Arrange
        AdditionalSyntaxTrees.Add(Parse(@"
using System;
using Microsoft.AspNetCore.Components;

namespace Test
{
    public class MyComponent<TValue> : ComponentBase
    {
        [Parameter]
        public TValue Value { get; set; }

        [Parameter]
        public EventCallback<TValue> ValueChanged { get; set; }
    }

    public class CustomValue
    {
    }
}"));

        // Act
        var generated = CompileToCSharp(@"
<MyComponent @bind-Value:get=""ParentValue"" @bind-Value:set=""UpdateValue"" />
@code {
    public CustomValue ParentValue { get; set; } = new CustomValue();

    public void UpdateValue(CustomValue value) => ParentValue = value;
}");

        // Assert
        AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
        AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
        CompileToAssembly(generated);
    }

    [IntegrationTestFact]
    public void BindToGenericComponent_InferredType_WithGetSet_Function()
    {
        // Arrange
        AdditionalSyntaxTrees.Add(Parse(@"
using System;
using Microsoft.AspNetCore.Components;

namespace Test
{
    public class MyComponent<TValue> : ComponentBase
    {
        [Parameter]
        public TValue Value { get; set; }

        [Parameter]
        public EventCallback<TValue> ValueChanged { get; set; }
    }

    public class CustomValue
    {
    }
}"));

        // Act
        var generated = CompileToCSharp(@"
<MyComponent @bind-Value:get=""ParentValue"" @bind-Value:set=""UpdateValue"" />
@code {
    public CustomValue ParentValue { get; set; } = new CustomValue();

    public Task UpdateValue(CustomValue value) { ParentValue = value; return Task.CompletedTask; }
}");

        // Assert
        AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
        AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
        CompileToAssembly(generated);
    }


    [IntegrationTestFact]
    public void BindToGenericComponent_ExplicitType_WithGetSet_Action()
    {
        // Arrange
        AdditionalSyntaxTrees.Add(Parse(@"
using System;
using Microsoft.AspNetCore.Components;

namespace Test
{
    public class MyComponent<TValue> : ComponentBase
    {
        [Parameter]
        public TValue Value { get; set; }

        [Parameter]
        public EventCallback<TValue> ValueChanged { get; set; }
    }

    public class CustomValue
    {
    }
}"));

        // Act
        var generated = CompileToCSharp(@"
<MyComponent TValue=""CustomValue"" @bind-Value:get=""ParentValue"" @bind-Value:set=""UpdateValue"" />
@code {
    public CustomValue ParentValue { get; set; } = new CustomValue();

    public void UpdateValue(CustomValue value) => ParentValue = value;
}");

        // Assert
        AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
        AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
        CompileToAssembly(generated);
    }

    [IntegrationTestFact]
    public void BindToGenericComponent_ExplicitType_WithGetSet_Function()
    {
        // Arrange
        AdditionalSyntaxTrees.Add(Parse(@"
using System;
using Microsoft.AspNetCore.Components;

namespace Test
{
    public class MyComponent<TValue> : ComponentBase
    {
        [Parameter]
        public TValue Value { get; set; }

        [Parameter]
        public EventCallback<TValue> ValueChanged { get; set; }
    }

    public class CustomValue
    {
    }
}"));

        // Act
        var generated = CompileToCSharp(@"
<MyComponent TValue=""CustomValue"" @bind-Value:get=""ParentValue"" @bind-Value:set=""UpdateValue"" />
@code {
    public CustomValue ParentValue { get; set; } = new CustomValue();

        public Task UpdateValue(CustomValue value) { ParentValue = value; return Task.CompletedTask; }
}");

        // Assert
        AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
        AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
        CompileToAssembly(generated);
    }

    [IntegrationTestFact]
    public void GenericComponentBindToGenericComponent_InferredType_WithGetSet_Action()
    {
        // Arrange
        AdditionalSyntaxTrees.Add(Parse(@"
using System;
using Microsoft.AspNetCore.Components;

namespace Test
{
    public class MyComponent<TValue> : ComponentBase
    {
        [Parameter]
        public TValue Value { get; set; }

        [Parameter]
        public EventCallback<TValue> ValueChanged { get; set; }
    }
}"));

        // Act
        var generated = CompileToCSharp(@"
@typeparam TParam
<MyComponent @bind-Value:get=""ParentValue"" @bind-Value:set=""UpdateValue"" />
@code {
    public TParam ParentValue { get; set; } = default;

    public void UpdateValue(TParam value) { ParentValue = value; }
}");

        // Assert
        AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
        AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
        CompileToAssembly(generated);
    }

    [IntegrationTestFact]
    public void GenericComponentBindToGenericComponent_InferredType_WithGetSet_Function()
    {
        // Arrange
        AdditionalSyntaxTrees.Add(Parse(@"
using System;
using Microsoft.AspNetCore.Components;

namespace Test
{
    public class MyComponent<TValue> : ComponentBase
    {
        [Parameter]
        public TValue Value { get; set; }

        [Parameter]
        public EventCallback<TValue> ValueChanged { get; set; }
    }
}"));

        // Act
        var generated = CompileToCSharp(@"
@typeparam TParam
<MyComponent @bind-Value:get=""ParentValue"" @bind-Value:set=""UpdateValue"" />
@code {
    public TParam ParentValue { get; set; } = default;

    public Task UpdateValue(TParam value) { ParentValue = value; return Task.CompletedTask; }
}");

        // Assert
        AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
        AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
        CompileToAssembly(generated);
    }

    [IntegrationTestFact]
    public void GenericBindToGenericComponent_ExplicitType_WithGetSet_Action()
    {
        // Arrange
        AdditionalSyntaxTrees.Add(Parse(@"
using System;
using Microsoft.AspNetCore.Components;

namespace Test
{
    public class MyComponent<TValue> : ComponentBase
    {
        [Parameter]
        public TValue Value { get; set; }

        [Parameter]
        public EventCallback<TValue> ValueChanged { get; set; }
    }
}"));

        // Act
        var generated = CompileToCSharp(@"
@typeparam TParam
<MyComponent TValue=""TParam"" @bind-Value:get=""ParentValue"" @bind-Value:set=""UpdateValue"" />
@code {
    public TParam ParentValue { get; set; } = default;

    public void UpdateValue(TParam value) { ParentValue = value; }
}");

        // Assert
        AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
        AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
        CompileToAssembly(generated);
    }

    [IntegrationTestFact]
    public void GenericBindToGenericComponent_ExplicitType_WithGetSet_Function()
    {
        // Arrange
        AdditionalSyntaxTrees.Add(Parse(@"
using System;
using Microsoft.AspNetCore.Components;

namespace Test
{
    public class MyComponent<TValue> : ComponentBase
    {
        [Parameter]
        public TValue Value { get; set; }

        [Parameter]
        public EventCallback<TValue> ValueChanged { get; set; }
    }
}"));

        // Act
        var generated = CompileToCSharp(@"
@typeparam TParam
<MyComponent TValue=""TParam"" @bind-Value:get=""ParentValue"" @bind-Value:set=""UpdateValue"" />
@code {
    public TParam ParentValue { get; set; } = default;

    public Task UpdateValue(TParam value) { ParentValue = value; return Task.CompletedTask; }
}");

        // Assert
        AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
        AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
        CompileToAssembly(generated);
    }

    [IntegrationTestFact]
    public void BindToComponent_WithGetSet_EventCallback_ReceivesAction()
    {
        // Arrange
        AdditionalSyntaxTrees.Add(Parse(@"
using System;
using Microsoft.AspNetCore.Components;

namespace Test
{
    public class MyComponent : ComponentBase
    {
        [Parameter]
        public int Value { get; set; }

        [Parameter]
        public EventCallback<int> ValueChanged { get; set; }
    }
}"));

        // Act
        var generated = CompileToCSharp(@"
<MyComponent @bind-Value:get=""ParentValue"" @bind-Value:set=""value => ParentValue = value"" />
@code {
    public int ParentValue { get; set; } = 42;
}");

        // Assert
        AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
        AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
        CompileToAssembly(generated);
    }

    [IntegrationTestFact]
    public void BindToComponent_WithGetSet_EventCallback_ReceivesFunction()
    {
        // Arrange
        AdditionalSyntaxTrees.Add(Parse(@"
using System;
using Microsoft.AspNetCore.Components;

namespace Test
{
    public class MyComponent : ComponentBase
    {
        [Parameter]
        public int Value { get; set; }

        [Parameter]
        public EventCallback<int> ValueChanged { get; set; }
    }
}"));

        // Act
        var generated = CompileToCSharp(@"
<MyComponent @bind-Value:get=""ParentValue"" @bind-Value:set=""UpdateValue"" />
@code {
    public int ParentValue { get; set; } = 42;

    public Task UpdateValue(int value) { ParentValue = value; return Task.CompletedTask; }
}");

        // Assert
        AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
        AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
        CompileToAssembly(generated);
    }

    [IntegrationTestFact]
    public void BindToComponent_WithAfter_EventCallback()
    {
        // Arrange
        AdditionalSyntaxTrees.Add(Parse(@"
using System;
using Microsoft.AspNetCore.Components;
namespace Test
{
    public class MyComponent : ComponentBase
    {
        [Parameter]
        public int Value { get; set; }
        [Parameter]
        public EventCallback<int> ValueChanged { get; set; }
    }
}"));

        // Act
        var generated = CompileToCSharp(@"
<MyComponent @bind-Value:get=""ParentValue"" @bind-Value:after=""UpdateValue"" />
@code {
    public int ParentValue { get; set; } = 42;
    public EventCallback UpdateValue { get; set; }
}");

        // Assert
        AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
        AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
        CompileToAssembly(generated,
            // x:\dir\subdir\Test\TestComponent.cshtml(1,63): error CS1503: Argument 1: cannot convert from 'Microsoft.AspNetCore.Components.EventCallback' to 'System.Action'
            //                                                               UpdateValue
            Diagnostic(ErrorCode.ERR_BadArgType, "UpdateValue").WithArguments("1", "Microsoft.AspNetCore.Components.EventCallback", "System.Action").WithLocation(1, 63));
    }

    [IntegrationTestFact]
    public void BindToComponent_WithAfter_TaskReturningDelegate()
    {
        // Arrange
        AdditionalSyntaxTrees.Add(Parse(@"
using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;

namespace Test
{
    public class MyComponent : ComponentBase
    {
        [Parameter]
        public int Value { get; set; }

        [Parameter]
        public Func<int, Task> ValueChanged { get; set; }
    }
}"));

        // Act
        var generated = CompileToCSharp(@"
<MyComponent @bind-Value:get=""ParentValue"" @bind-Value:after=""Update"" />
@code {
    public int ParentValue { get; set; } = 42;

    public Task Update() => Task.CompletedTask;
}");

        // Assert
        AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
        AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
        CompileToAssembly(generated);
    }

    [IntegrationTestFact]
    public void BindToComponent_WithAfter_TaskReturningLambda()
    {
        // Arrange
        AdditionalSyntaxTrees.Add(Parse(@"
using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;

namespace Test
{
    public class MyComponent : ComponentBase
    {
        [Parameter]
        public int Value { get; set; }

        [Parameter]
        public Func<int, Task> ValueChanged { get; set; }
    }
}"));

        // Act
        var generated = CompileToCSharp(@"
<MyComponent @bind-Value:get=""ParentValue"" @bind-Value:after=""() => { return Task.CompletedTask; }"" />
@code {
    public int ParentValue { get; set; } = 42;
}");

        // Assert
        AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
        AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
        CompileToAssembly(generated);
    }

    [IntegrationTestFact]
    public void BindToComponent_WithAfter_Action()
    {
        // Arrange
        AdditionalSyntaxTrees.Add(Parse(@"
using System;
using Microsoft.AspNetCore.Components;

namespace Test
{
    public class MyComponent : ComponentBase
    {
        [Parameter]
        public int Value { get; set; }

        [Parameter]
        public Action<int> ValueChanged { get; set; }
    }
}"));

        // Act
        var generated = CompileToCSharp(@"
<MyComponent @bind-Value:get=""ParentValue"" @bind-Value:after=""Update"" />
@code {
    public int ParentValue { get; set; } = 42;

    public void Update() { }
}");

        // Assert
        AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
        AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
        CompileToAssembly(generated);
    }

    [IntegrationTestFact]
    public void BindToGenericComponent_InferredType_WithAfter_Action()
    {
        // Arrange
        AdditionalSyntaxTrees.Add(Parse(@"
using System;
using Microsoft.AspNetCore.Components;

namespace Test
{
    public class MyComponent<TValue> : ComponentBase
    {
        [Parameter]
        public TValue Value { get; set; }

        [Parameter]
        public Action<TValue> ValueChanged { get; set; }
    }
}"));

        // Act
        var generated = CompileToCSharp(@"
<MyComponent @bind-Value:get=""ParentValue"" @bind-Value:after=""Update"" />
@code {
    public int ParentValue { get; set; } = 42;

    public void Update() { }
}");

        // Assert
        AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
        AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
        CompileToAssembly(generated);
    }

    [IntegrationTestFact]
    public void BindToGenericComponent_ExplicitType_WithAfter_Action()
    {
        // Arrange
        AdditionalSyntaxTrees.Add(Parse(@"
using System;
using Microsoft.AspNetCore.Components;

namespace Test
{
    public class MyComponent<TValue> : ComponentBase
    {
        [Parameter]
        public TValue Value { get; set; }

        [Parameter]
        public Action<TValue> ValueChanged { get; set; }
    }
}"));

        // Act
        var generated = CompileToCSharp(@"
<MyComponent TValue=""int"" @bind-Value:get=""ParentValue"" @bind-Value:after=""Update"" />
@code {
    public int ParentValue { get; set; } = 42;

    public void Update() { }
}");

        // Assert
        AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
        AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
        CompileToAssembly(generated);
    }

    [IntegrationTestFact]
    public void GenericComponentBindToGenericComponent_InferredType_WithAfter_Action()
    {
        // Arrange
        AdditionalSyntaxTrees.Add(Parse(@"
using System;
using Microsoft.AspNetCore.Components;

namespace Test
{
    public class MyComponent<TValue> : ComponentBase
    {
        [Parameter]
        public TValue Value { get; set; }

        [Parameter]
        public Action<TValue> ValueChanged { get; set; }
    }
}"));

        // Act
        var generated = CompileToCSharp(@"
@typeparam TParam
<MyComponent @bind-Value:get=""ParentValue"" @bind-Value:after=""Update"" />
@code {
    public TParam ParentValue { get; set; }

    public void Update() { }
}");

        // Assert
        AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
        AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
        CompileToAssembly(generated);
    }

    [IntegrationTestFact]
    public void GenericComponentBindToGenericComponent_ExplicitType_WithAfter_Action()
    {
        // Arrange
        AdditionalSyntaxTrees.Add(Parse(@"
using System;
using Microsoft.AspNetCore.Components;

namespace Test
{
    public class MyComponent<TValue> : ComponentBase
    {
        [Parameter]
        public TValue Value { get; set; }

        [Parameter]
        public Action<TValue> ValueChanged { get; set; }
    }
}"));

        // Act
        var generated = CompileToCSharp(@"
@typeparam TParam
<MyComponent TValue=""TParam"" @bind-Value:get=""ParentValue"" @bind-Value:after=""Update"" />
@code {
    public TParam ParentValue { get; set; }

    public void Update() { }
}");

        // Assert
        AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
        AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
        CompileToAssembly(generated);
    }

    [IntegrationTestFact]
    public void BindToComponent_WithAfter_ActionLambda()
    {
        // Arrange
        AdditionalSyntaxTrees.Add(Parse(@"
using System;
using Microsoft.AspNetCore.Components;

namespace Test
{
    public class MyComponent : ComponentBase
    {
        [Parameter]
        public int Value { get; set; }

        [Parameter]
        public Action<int> ValueChanged { get; set; }
    }
}"));

        // Act
        var generated = CompileToCSharp(@"
<MyComponent @bind-Value:get=""ParentValue"" @bind-Value:after=""() => { }"" />
@code {
    public int ParentValue { get; set; } = 42;
}");

        // Assert
        AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
        AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
        CompileToAssembly(generated);
    }

    [IntegrationTestFact]
    public void BindToComponent_WithAfter_EventCallback_ReceivesAction()
    {
        // Arrange
        AdditionalSyntaxTrees.Add(Parse(@"
using System;
using Microsoft.AspNetCore.Components;

namespace Test
{
    public class MyComponent : ComponentBase
    {
        [Parameter]
        public int Value { get; set; }

        [Parameter]
        public EventCallback<int> ValueChanged { get; set; }
    }
}"));

        // Act
        var generated = CompileToCSharp(@"
<MyComponent @bind-Value:get=""ParentValue"" @bind-Value:after=""() => { }"" />
@code {
    public int ParentValue { get; set; } = 42;
}");

        // Assert
        AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
        AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
        CompileToAssembly(generated);
    }

    [IntegrationTestFact]
    public void BindToComponent_WithAfter_EventCallback_ReceivesFunction()
    {
        // Arrange
        AdditionalSyntaxTrees.Add(Parse(@"
using System;
using Microsoft.AspNetCore.Components;

namespace Test
{
    public class MyComponent : ComponentBase
    {
        [Parameter]
        public int Value { get; set; }

        [Parameter]
        public EventCallback<int> ValueChanged { get; set; }
    }
}"));

        // Act
        var generated = CompileToCSharp(@"
<MyComponent @bind-Value:get=""ParentValue"" @bind-Value:after=""UpdateValue"" />
@code {
    public int ParentValue { get; set; } = 42;

    public Task UpdateValue() => Task.CompletedTask;
}");

        // Assert
        AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
        AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
        CompileToAssembly(generated);
    }

    [IntegrationTestFact]
    public void BindToComponent_WithAfter_AsyncLambdaProducesError()
    {
        // Arrange
        AdditionalSyntaxTrees.Add(Parse(@"
using System;
using Microsoft.AspNetCore.Components;

namespace Test
{
    public class MyComponent : ComponentBase
    {
        [Parameter]
        public int Value { get; set; }

        [Parameter]
        public Action<int> ValueChanged { get; set; }
    }
}"));

        // Act
        var generated = CompileToCSharp(@"
<MyComponent @bind-Value:get=""ParentValue"" @bind-Value:set=""(value => { ParentValue = value; return Task.CompletedTask; })"" />
@code {
    public int ParentValue { get; set; } = 42;
}");

        // Assert
        AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
        AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
        CompileToAssembly(generated,
            // x:\dir\subdir\Test\TestComponent.cshtml(1,94): error CS8030: Anonymous function converted to a void returning delegate cannot return a value
            // (value => { ParentValue = value; return Task.CompletedTask; })
            Diagnostic(ErrorCode.ERR_RetNoObjectRequiredLambda, "return").WithLocation(1, 94));
    }

    [IntegrationTestFact]
    public void BindToElement_WithStringAttribute_WritesAttributes()
    {
        // Arrange
        AdditionalSyntaxTrees.Add(Parse(@"
using System;
using Microsoft.AspNetCore.Components;

namespace Test
{
    [BindElement(""div"", ""value"", ""myvalue"", ""myevent"")]
    public static class BindAttributes
    {
    }
}"));
        // Act
        var generated = CompileToCSharp(@"
<div @bind-value=""ParentValue"" />
@code {
    public string ParentValue { get; set; } = ""hi"";
}");

        // Assert
        AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
        AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
        CompileToAssembly(generated);
    }

    [IntegrationTestFact]
    public void BindToElementWithSuffix_WritesAttributes()
    {
        // Arrange
        AdditionalSyntaxTrees.Add(Parse(@"
using System;
using Microsoft.AspNetCore.Components;

namespace Test
{
    [BindElement(""div"", ""value"", ""myvalue"", ""myevent"")]
    public static class BindAttributes
    {
    }
}"));
        // Act
        var generated = CompileToCSharp(@"
<div @bind-value=""@ParentValue"" />
@code {
    public string ParentValue { get; set; } = ""hi"";
}");

        // Assert
        AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
        AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
        CompileToAssembly(generated);
    }

    [IntegrationTestFact]
    public void BindToElementWithSuffix_OverridesEvent()
    {
        // Arrange
        AdditionalSyntaxTrees.Add(Parse(@"
using System;
using Microsoft.AspNetCore.Components;

namespace Test
{
    [BindElement(""div"", ""value"", ""myvalue"", ""myevent"")]
    public static class BindAttributes
    {
    }
}"));
        // Act
        var generated = CompileToCSharp(@"
<div @bind-value=""@ParentValue"" @bind-value:event=""anotherevent"" />
@code {
    public string ParentValue { get; set; } = ""hi"";
}");

        // Assert
        AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
        AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
        CompileToAssembly(generated);
    }

    [IntegrationTestFact]
    public void BindToElement_WithEventAsExpression()
    {
        // Arrange
        AdditionalSyntaxTrees.Add(Parse(@"
using System;
using Microsoft.AspNetCore.Components;

namespace Test
{
    [BindElement(""div"", ""value"", ""myvalue"", ""myevent"")]
    public static class BindAttributes
    {
    }
}"));
        // Act
        var generated = CompileToCSharp(@"
@{ var x = ""anotherevent""; }
<div @bind-value=""@ParentValue"" @bind-value:event=""@x"" />
@code {
    public string ParentValue { get; set; } = ""hi"";
}");

        // Assert
        AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
        AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
        CompileToAssembly(generated);
    }

    [IntegrationTestFact]
    public void BindToElement_WithEventAsExplicitExpression()
    {
        // Arrange
        AdditionalSyntaxTrees.Add(Parse(@"
using System;
using Microsoft.AspNetCore.Components;

namespace Test
{
    [BindElement(""div"", ""value"", ""myvalue"", ""myevent"")]
    public static class BindAttributes
    {
    }
}"));
        // Act
        var generated = CompileToCSharp(@"
@{ var x = ""anotherevent""; }
<div @bind-value=""@ParentValue"" @bind-value:event=""@(x.ToString())"" />
@code {
    public string ParentValue { get; set; } = ""hi"";
}");

        // Assert
        AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
        AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
        CompileToAssembly(generated);
    }

    [IntegrationTestFact]
    public void BuiltIn_BindToInputWithoutType_WritesAttributes()
    {
        // Arrange

        // Act
        var generated = CompileToCSharp(@"
<input @bind=""@ParentValue"" />
@code {
    public int ParentValue { get; set; } = 42;
}");

        // Assert
        AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
        AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
        CompileToAssembly(generated);
    }

    [IntegrationTestFact]
    public void BuiltIn_BindToInputWithoutType_IsCaseSensitive()
    {
        // Arrange

        // Act
        var generated = CompileToCSharp(@"
<input @BIND=""@ParentValue"" />
@code {
    public int ParentValue { get; set; } = 42;
}");

        // Assert
        AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
        AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
        CompileToAssembly(generated);
    }

    [IntegrationTestFact]
    public void BuiltIn_BindToInputText_WithFormat_WritesAttributes()
    {
        // Arrange

        // Act
        var generated = CompileToCSharp(@"
<input type=""text"" @bind=""@CurrentDate"" @bind:format=""MM/dd/yyyy""/>
@code {
    public DateTime CurrentDate { get; set; } = new DateTime(2018, 1, 1);
}");

        // Assert
        AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
        AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
        CompileToAssembly(generated);
    }

    [IntegrationTestFact]
    public void BuiltIn_BindToInputText_WithFormatFromProperty_WritesAttributes()
    {
        // Arrange

        // Act
        var generated = CompileToCSharp(@"
<input type=""text"" @bind=""@CurrentDate"" @bind:format=""@Format""/>
@code {
    public DateTime CurrentDate { get; set; } = new DateTime(2018, 1, 1);

    public string Format { get; set; } = ""MM/dd/yyyy"";
}");

        // Assert
        AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
        AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
        CompileToAssembly(generated);
    }

    [IntegrationTestFact]
    public void BuiltIn_BindToInputText_WritesAttributes()
    {
        // Arrange

        // Act
        var generated = CompileToCSharp(@"
<input type=""text"" @bind=""@ParentValue"" />
@code {
    public int ParentValue { get; set; } = 42;
}");

        // Assert
        AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
        AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
        CompileToAssembly(generated);
    }

    [IntegrationTestFact]
    public void BuiltIn_BindToInputCheckbox_WritesAttributes()
    {
        // Arrange

        // Act
        var generated = CompileToCSharp(@"
<input type=""checkbox"" @bind=""@Enabled"" />
@code {
    public bool Enabled { get; set; }
}");

        // Assert
        AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
        AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
        CompileToAssembly(generated);
    }

    [IntegrationTestFact]
    public void BindToElementFallback_WritesAttributes()
    {
        // Arrange

        // Act
        var generated = CompileToCSharp(@"
<input type=""text"" @bind-value=""@ParentValue"" @bind-value:event=""onchange"" />
@code {
    public int ParentValue { get; set; } = 42;
}");

        // Assert
        AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
        AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
        CompileToAssembly(generated);
    }

    [IntegrationTestFact]
    public void BindToElementFallback_WithFormat_WritesAttributes()
    {
        // Arrange

        // Act
        var generated = CompileToCSharp(@"
<input type=""text"" @bind-value=""@CurrentDate"" @bind-value:event=""onchange"" @bind-value:format=""MM/dd"" />
@code {
    public DateTime CurrentDate { get; set; } = new DateTime(2018, 1, 1);
}");

        // Assert
        AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
        AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
        CompileToAssembly(generated);
    }

    [IntegrationTestFact]
    public void BindToElementFallback_WithCulture()
    {
        // Arrange

        // Act
        var generated = CompileToCSharp(@"
@using System.Globalization
<div @bind-value=""@ParentValue"" @bind-value:event=""onchange"" @bind-value:culture=""CultureInfo.InvariantCulture"" />
@code {
    public string ParentValue { get; set; } = ""hi"";
}");

        // Assert
        AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
        AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
        CompileToAssembly(generated);
    }

    [IntegrationTestFact]
    public void BindToElementWithCulture()
    {
        // Arrange
        AdditionalSyntaxTrees.Add(Parse(@"
using System;
using Microsoft.AspNetCore.Components;

namespace Test
{
    [BindElement(""div"", ""value"", ""myvalue"", ""myevent"")]
    public static class BindAttributes
    {
    }
}"));
        // Act
        var generated = CompileToCSharp(@"
@using System.Globalization
<div @bind-value=""@ParentValue"" @bind-value:event=""anotherevent"" @bind-value:culture=""CultureInfo.InvariantCulture"" />
@code {
    public string ParentValue { get; set; } = ""hi"";
}");

        // Assert
        AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
        AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
        CompileToAssembly(generated);
    }

    [IntegrationTestFact]
    public void BindToInputElementWithDefaultCulture()
    {
        // Arrange
        AdditionalSyntaxTrees.Add(Parse(@"
using System;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

namespace Test
{
    [BindInputElement(""custom"", null, ""value"", ""onchange"", isInvariantCulture: true, format: null)]
    public static class BindAttributes
    {
    }
}"));
        // Act
        var generated = CompileToCSharp(@"
@using System.Globalization
<input type=""custom"" @bind-value=""@ParentValue"" @bind-value:event=""anotherevent"" />
@code {
    public int ParentValue { get; set; }
}");

        // Assert
        AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
        AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
        CompileToAssembly(generated);
    }

    [IntegrationTestFact]
    public void BindToInputElementWithDefaultCulture_Override()
    {
        // Arrange
        AdditionalSyntaxTrees.Add(Parse(@"
using System;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

namespace Test
{
    [BindInputElement(""custom"", null, ""value"", ""onchange"", isInvariantCulture: true, format: null)]
    public static class BindAttributes
    {
    }
}"));
        // Act
        var generated = CompileToCSharp(@"
@using System.Globalization
<input type=""custom"" @bind-value=""@ParentValue"" @bind-value:event=""anotherevent"" @bind-value:culture=""CultureInfo.CurrentCulture"" />
@code {
    public int ParentValue { get; set; }
}");

        // Assert
        AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
        AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
        CompileToAssembly(generated);
    }

    [IntegrationTestFact]
    public void BindToElement_MixingBindAndParamBindSet()
    {
        // Arrange
        AdditionalSyntaxTrees.Add(Parse(@"
using System;
using Microsoft.AspNetCore.Components;

namespace Test
{
    [BindElement(""div"", ""value"", ""myvalue"", ""myevent"")]
    public static class BindAttributes
    {
    }
}"));
        // Act
        var generated = CompileToCSharp(@"
<div @bind-value=""@ParentValue"" @bind-value:set=""UpdateValue"" />
@code {
    public string ParentValue { get; set; } = ""hi"";

    public void UpdateValue(string value) => ParentValue = value;
}");

        // Assert
        Assert.Collection(generated.RazorDiagnostics,
            diagnostic => Assert.Equal("RZ10015", diagnostic.Id));

        AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
        AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
    }

    [IntegrationTestFact]
    public void BindToElement_MixingBindWithoutSuffixAndParamBindSetWithSuffix()
    {
        // Arrange
        AdditionalSyntaxTrees.Add(Parse(@"
using System;
using Microsoft.AspNetCore.Components;

namespace Test
{
    [BindElement(""div"", null, ""myvalue"", ""myevent"")]
    public static class BindAttributes
    {
    }
}"));
        // Act
        var generated = CompileToCSharp(@"
<div @bind=""@ParentValue"" @bind-value:set=""UpdateValue"" />
@code {
    public string ParentValue { get; set; } = ""hi"";

    public void UpdateValue(string value) => ParentValue = value;
}");

        // Assert
        Assert.Collection(generated.RazorDiagnostics,
            diagnostic => Assert.Equal("RZ10016", diagnostic.Id));

        AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
        AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
    }

    [IntegrationTestFact]
    public void BindToElement_MixingBindValueWithGetSet()
    {
        // Arrange
        AdditionalSyntaxTrees.Add(Parse(@"
using System;
using Microsoft.AspNetCore.Components;

namespace Test
{
    [BindElement(""div"", null, ""myvalue"", ""myevent"")]
    public static class BindAttributes
    {
    }
}"));
        // Act
        var generated = CompileToCSharp(@"
<div @bind=""@ParentValue"" @bind:get=""@ParentValue"" @bind:set=""UpdateValue"" />
@code {
    public string ParentValue { get; set; } = ""hi"";

    public void UpdateValue(string value) => ParentValue = value;
}");

        // Assert
        Assert.Collection(generated.RazorDiagnostics,
            diagnostic => Assert.Equal("RZ10018", diagnostic.Id),
            diagnostic => Assert.Equal("RZ10015", diagnostic.Id));

        AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
        AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
    }

    [IntegrationTestFact]
    public void BindToComponent_WithGetSet_ProducesErrorOnOlderLanguageVersions()
    {
        _configuration = new(
            RazorLanguageVersion.Version_6_0,
            "unnamed",
            Extensions: [],
            UseConsolidatedMvcViews: false);

        // Arrange
        AdditionalSyntaxTrees.Add(Parse(@"
using System;
using Microsoft.AspNetCore.Components;

namespace Test
{
    public class MyComponent : ComponentBase
    {
        [Parameter]
        public int Value { get; set; }

        [Parameter]
        public Action<int> ValueChanged { get; set; }
    }
}"));

        // Act
        var generated = CompileToCSharp(@"
<MyComponent @bind-Value:get=""ParentValue"" @bind-Value:set=""UpdateValue"" />
@code {
    public int ParentValue { get; set; } = 42;

    public void UpdateValue(int value) => ParentValue = value;
}");

        // Assert
        Assert.Collection(generated.RazorDiagnostics,
            diagnostic => Assert.Equal("RZ10020", diagnostic.Id));

        AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
        AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
    }

    [IntegrationTestFact]
    public void BindToElement_MixingSetWithAfter()
    {
        // Arrange
        AdditionalSyntaxTrees.Add(Parse(@"
using System;
using Microsoft.AspNetCore.Components;

namespace Test
{
    [BindElement(""div"", null, ""myvalue"", ""myevent"")]
    public static class BindAttributes
    {
    }
}"));
        // Act
        var generated = CompileToCSharp(@"
<div @bind:get=""@ParentValue"" @bind:set=""UpdateValue"" @bind:after=""AfterUpdate"" />
@code {
    public string ParentValue { get; set; } = ""hi"";

    public void UpdateValue(string value) => ParentValue = value;
    public void AfterUpdate() { }
}");

        // Assert
        Assert.Collection(generated.RazorDiagnostics,
            diagnostic => Assert.Equal("RZ10019", diagnostic.Id));

        AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
        AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
    }

    [IntegrationTestFact]
    public void BindToElement_MissingBindGet()
    {
        // Arrange
        AdditionalSyntaxTrees.Add(Parse(@"
using System;
using Microsoft.AspNetCore.Components;

namespace Test
{
    [BindElement(""div"", ""value"", ""myvalue"", ""myevent"")]
    public static class BindAttributes
    {
    }
}"));
        // Act
        var generated = CompileToCSharp(@"
<div @bind-value:set=""UpdateValue"" />
@code {
    public string ParentValue { get; set; } = ""hi"";

    public void UpdateValue(string value) => ParentValue = value;
}");

        // Assert
        Assert.Collection(generated.RazorDiagnostics,
            diagnostic => Assert.Equal("RZ10016", diagnostic.Id));
    }

    [IntegrationTestFact]
    public void BindToElement_MissingBindSet()
    {
        // Arrange
        AdditionalSyntaxTrees.Add(Parse(@"
using System;
using Microsoft.AspNetCore.Components;

namespace Test
{
    [BindElement(""div"", ""value"", ""myvalue"", ""myevent"")]
    public static class BindAttributes
    {
    }
}"));
        // Act
        var generated = CompileToCSharp(@"
<div @bind-value:get=""ParentValue"" />
@code {
    public string ParentValue { get; set; } = ""hi"";

    public void UpdateValue(string value) => ParentValue = value;
}");

        // Assert
        Assert.Collection(generated.RazorDiagnostics,
            diagnostic => Assert.Equal("RZ10017", diagnostic.Id));
    }

    [IntegrationTestFact]
    public void BuiltIn_BindToInputText_CanOverrideEvent()
    {
        // Arrange

        // Act
        var generated = CompileToCSharp(@"
@using Microsoft.AspNetCore.Components.Web
<input @bind=""@CurrentDate"" @bind:event=""oninput"" @bind:format=""MM/dd"" />
@code {
    public DateTime CurrentDate { get; set; } = new DateTime(2018, 1, 1);
}");

        // Assert
        AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
        AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
        CompileToAssembly(generated);
    }

    [IntegrationTestFact]
    public void BuiltIn_BindToInputWithSuffix()
    {
        // Arrange

        // Act
        var generated = CompileToCSharp(@"
@using Microsoft.AspNetCore.Components.Web
<input @bind-value=""@CurrentDate"" @bind-value:format=""MM/dd"" />
@code {
    public DateTime CurrentDate { get; set; } = new DateTime(2018, 1, 1);
}");

        // Assert
        AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
        AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
        CompileToAssembly(generated);
    }

    [IntegrationTestFact]
    public void BuiltIn_BindToInputWithSuffix_CanOverrideEvent()
    {
        // Arrange

        // Act
        var generated = CompileToCSharp(@"
<input @bind-value=""@CurrentDate"" @bind-value:event=""oninput"" @bind-value:format=""MM/dd"" />
@code {
    public DateTime CurrentDate { get; set; } = new DateTime(2018, 1, 1);
}");

        // Assert
        AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
        AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
        CompileToAssembly(generated);
    }

    [IntegrationTestFact]
    public void BuiltIn_BindToInputWithDefaultFormat()
    {
        // Arrange
        AdditionalSyntaxTrees.Add(Parse(@"
using System;
using Microsoft.AspNetCore.Components;

namespace Test
{
    [BindInputElement(""custom"", null, ""value"", ""onchange"", isInvariantCulture: false, format: ""MM/dd"")]
    public static class BindAttributes
    {
    }
}"));

        // Act
        var generated = CompileToCSharp(@"
<input type=""custom"" @bind=""@CurrentDate"" />
@code {
    public DateTime CurrentDate { get; set; } = new DateTime(2018, 1, 1);
}");

        // Assert
        AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
        AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
        CompileToAssembly(generated);
    }

    [IntegrationTestFact]
    public void BuiltIn_BindToInputWithDefaultFormat_Override()
    {
        // Arrange
        AdditionalSyntaxTrees.Add(Parse(@"
using System;
using Microsoft.AspNetCore.Components;

namespace Test
{
    [BindInputElement(""custom"", null, ""value"", ""onchange"", isInvariantCulture: false, format: ""MM/dd"")]
    public static class BindAttributes
    {
    }
}"));

        // Act
        var generated = CompileToCSharp(@"
<input type=""custom"" @bind=""@CurrentDate"" @bind:format=""MM/dd/yyyy""/>
@code {
    public DateTime CurrentDate { get; set; } = new DateTime(2018, 1, 1);
}");

        // Assert
        AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
        AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
        CompileToAssembly(generated);
    }

    [IntegrationTestFact]
    public void BuiltIn_BindToInputWithDefaultCultureAndDefaultFormat_Override()
    {
        // Arrange
        AdditionalSyntaxTrees.Add(Parse(@"
using System;
using Microsoft.AspNetCore.Components;

namespace Test
{
    [BindInputElement(""custom"", null, ""value"", ""onchange"", isInvariantCulture: true, format: ""MM/dd"")]
    public static class BindAttributes
    {
    }
}"));

        // Act
        var generated = CompileToCSharp(@"
<input type=""custom"" @bind=""@CurrentDate"" @bind:format=""MM/dd/yyyy""/>
@code {
    public DateTime CurrentDate { get; set; } = new DateTime(2018, 1, 1);
}");

        // Assert
        AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
        AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
        CompileToAssembly(generated);
    }

    #endregion

    #region Child Content

    [IntegrationTestFact]
    public void ChildComponent_WithChildContent()
    {
        // Arrange
        AdditionalSyntaxTrees.Add(Parse(@"
using Microsoft.AspNetCore.Components;

namespace Test
{
    public class MyComponent : ComponentBase
    {
        [Parameter]
        public string MyAttr { get; set; }

        [Parameter]
        public RenderFragment ChildContent { get; set; }
    }
}
"));

        // Act
        var generated = CompileToCSharp(@"
<MyComponent MyAttr=""abc"">Some text<some-child a='1'>Nested text</some-child></MyComponent>");

        // Assert
        AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
        AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
        var result = CompileToAssembly(generated);
        AssertSequencePointsMatchBaseline(result, generated.CodeDocument);
    }

    [IntegrationTestFact]
    public void ChildComponent_WithGenericChildContent()
    {
        // Arrange
        AdditionalSyntaxTrees.Add(Parse(@"
using Microsoft.AspNetCore.Components;

namespace Test
{
    public class MyComponent : ComponentBase
    {
        [Parameter]
        public string MyAttr { get; set; }

        [Parameter]
        public RenderFragment<string> ChildContent { get; set; }
    }
}
"));

        // Act
        var generated = CompileToCSharp(@"
<MyComponent MyAttr=""abc"">Some text<some-child a='1'>@context.ToLowerInvariant()</some-child></MyComponent>");

        // Assert
        AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
        AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
        var result = CompileToAssembly(generated);
        AssertSequencePointsMatchBaseline(result, generated.CodeDocument);
    }

    [IntegrationTestFact]
    public void ChildComponent_WithGenericChildContent_SetsParameterName()
    {
        // Arrange
        AdditionalSyntaxTrees.Add(Parse(@"
using Microsoft.AspNetCore.Components;

namespace Test
{
    public class MyComponent : ComponentBase
    {
        [Parameter]
        public string MyAttr { get; set; }

        [Parameter]
        public RenderFragment<string> ChildContent { get; set; }
    }
}
"));

        // Act
        var generated = CompileToCSharp(@"
<MyComponent MyAttr=""abc"">
  <ChildContent Context=""item"">
    Some text<some-child a='1'>@item.ToLowerInvariant()</some-child>
  </ChildContent>
</MyComponent>");

        // Assert
        AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
        AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
        CompileToAssembly(generated);
    }

    [IntegrationTestFact]
    public void ChildComponent_WithGenericChildContent_SetsParameterNameOnComponent()
    {
        // Arrange
        AdditionalSyntaxTrees.Add(Parse(@"
using Microsoft.AspNetCore.Components;

namespace Test
{
    public class MyComponent : ComponentBase
    {
        [Parameter]
        public string MyAttr { get; set; }

        [Parameter]
        public RenderFragment<string> ChildContent { get; set; }
    }
}
"));

        // Act
        var generated = CompileToCSharp(@"
<MyComponent MyAttr=""abc"" Context=""item"">
  <ChildContent>
    Some text<some-child a='1'>@item.ToLowerInvariant()</some-child>
  </ChildContent>
</MyComponent>");

        // Assert
        AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
        AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
        CompileToAssembly(generated);
    }

    [IntegrationTestFact]
    public void ChildComponent_WithElementOnlyChildContent()
    {
        // Arrange
        AdditionalSyntaxTrees.Add(Parse(@"
using Microsoft.AspNetCore.Components;

namespace Test
{
    public class MyComponent : ComponentBase
    {
        [Parameter]
        public RenderFragment ChildContent { get; set; }
    }
}
"));

        // Act
        var generated = CompileToCSharp(@"
<MyComponent><child>hello</child></MyComponent>");

        // Assert
        AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
        AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
        CompileToAssembly(generated);
    }

    [IntegrationTestFact]
    public void ChildComponent_WithExplicitChildContent()
    {
        // Arrange
        AdditionalSyntaxTrees.Add(Parse(@"
using Microsoft.AspNetCore.Components;

namespace Test
{
    public class MyComponent : ComponentBase
    {
        [Parameter]
        public RenderFragment ChildContent { get; set; }
    }
}
"));

        // Act
        var generated = CompileToCSharp(@"
<MyComponent><ChildContent>hello</ChildContent></MyComponent>");

        // Assert
        AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
        AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
        CompileToAssembly(generated);
    }

    [IntegrationTestFact]
    public void ChildComponent_WithExplicitGenericChildContent()
    {
        // Arrange
        AdditionalSyntaxTrees.Add(Parse(@"
using Microsoft.AspNetCore.Components;

namespace Test
{
    public class MyComponent : ComponentBase
    {
        [Parameter]
        public RenderFragment<string> ChildContent { get; set; }
    }
}
"));

        // Act
        var generated = CompileToCSharp(@"
<MyComponent><ChildContent>@context</ChildContent></MyComponent>");

        // Assert
        AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
        AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
        CompileToAssembly(generated);
    }

    [IntegrationTestFact]
    public void MultipleExplictChildContent()
    {
        // Arrange
        AdditionalSyntaxTrees.Add(Parse(@"
using Microsoft.AspNetCore.Components;

namespace Test
{
    public class MyComponent : ComponentBase
    {
        [Parameter]
        public RenderFragment Header { get; set; }

        [Parameter]
        public RenderFragment Footer { get; set; }
    }
}
"));

        // Act
        var generated = CompileToCSharp(@"
<MyComponent>
    <Header>Hi!</Header>
    <Footer>@(""bye!"")</Footer>
</MyComponent>");

        // Assert
        AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
        AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
        CompileToAssembly(generated);
    }

    [IntegrationTestFact]
    public void BodyAndAttributeChildContent()
    {
        // Arrange
        AdditionalSyntaxTrees.Add(Parse(@"
using Microsoft.AspNetCore.Components;

namespace Test
{
    public class MyComponent : ComponentBase
    {
        [Parameter]
        public RenderFragment<string> Header { get; set; }

        [Parameter]
        public RenderFragment ChildContent { get; set; }

        [Parameter]
        public RenderFragment Footer { get; set; }
    }
}
"));

        // Act
        var generated = CompileToCSharp(@"
@{ RenderFragment<string> header = (context) => @<div>@context.ToLowerInvariant()</div>; }
<MyComponent Header=@header>
    Some Content
</MyComponent>");

        // Assert
        AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
        AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
        var result = CompileToAssembly(generated);
        AssertSequencePointsMatchBaseline(result, generated.CodeDocument);
    }

    [IntegrationTestFact]
    public void BodyAndExplicitChildContent()
    {
        // Arrange
        AdditionalSyntaxTrees.Add(Parse(@"
using Microsoft.AspNetCore.Components;

namespace Test
{
    public class MyComponent : ComponentBase
    {
        [Parameter]
        public RenderFragment<string> Header { get; set; }

        [Parameter]
        public RenderFragment ChildContent { get; set; }

        [Parameter]
        public RenderFragment Footer { get; set; }
    }
}
"));

        // Act
        var generated = CompileToCSharp(@"
@{ RenderFragment<string> header = (context) => @<div>@context.ToLowerInvariant()</div>; }
<MyComponent Header=@header>
  <ChildContent>Some Content</ChildContent>
  <Footer>Bye!</Footer>
</MyComponent>");

        // Assert
        AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
        AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
        var result = CompileToAssembly(generated);
        AssertSequencePointsMatchBaseline(result, generated.CodeDocument);
    }

    [IntegrationTestFact]
    public void MultipleChildContentMatchingComponentName()
    {
        // Arrange
        AdditionalSyntaxTrees.Add(Parse(@"
using Microsoft.AspNetCore.Components;

namespace Test
{
    public class MyComponent : ComponentBase
    {
        [Parameter]
        public RenderFragment Header { get; set; }

        [Parameter]
        public RenderFragment Footer { get; set; }
    }

    public class Header : ComponentBase
    {
    }
}
"));

        // Act
        var generated = CompileToCSharp(@"
<MyComponent>
  <Header>Hi!</Header>
  <Footer>Bye!</Footer>
</MyComponent>
<Header>Hello!</Header>");

        // Assert
        AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
        AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
        CompileToAssembly(generated);
    }

    [IntegrationTestFact, WorkItem("https://github.com/dotnet/razor/issues/8460")]
    public void VoidTagName()
    {
        // Arrange
        AdditionalSyntaxTrees.Add(Parse("""
            using Microsoft.AspNetCore.Components;

            namespace Test;

            public class Col : ComponentBase
            {
                [Parameter]
                public RenderFragment ChildContent { get; set; }
            }
            """));

        // Act
        var generated = CompileToCSharp("""
            @using Microsoft.AspNetCore.Components

            <Col>in markup</Col>
            @{
                <Col>in code block</Col>
                RenderFragment template = @<Col>in template</Col>;
            }
            """);

        // Assert
        AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
        AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
        CompileToAssembly(generated, verifyDiagnostics: static diagnostics =>
        {
            // Malformed C# is generated due to everything after the <Col> tag being considered C#.
            Assert.Contains(diagnostics, static d => d.Severity == DiagnosticSeverity.Error);
        });
    }

    [IntegrationTestFact, WorkItem("https://github.com/dotnet/razor/issues/8460")]
    public void VoidTagName_FullyQualified()
    {
        // Arrange
        AdditionalSyntaxTrees.Add(Parse("""
            using Microsoft.AspNetCore.Components;

            namespace Test;

            public class Col : ComponentBase
            {
                [Parameter]
                public RenderFragment ChildContent { get; set; }
            }
            """));

        // Act
        var generated = CompileToCSharp("""
            @using Microsoft.AspNetCore.Components

            <Test.Col>in markup</Test.Col>
            @{
                <Test.Col>in code block</Test.Col>
                RenderFragment template = @<Test.Col>in template</Test.Col>;
            }
            """);

        // Assert
        AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
        AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
        CompileToAssembly(generated);
    }

    [IntegrationTestFact, WorkItem("https://github.com/dotnet/razor/issues/8460")]
    public void VoidTagName_SelfClosing()
    {
        // Arrange
        AdditionalSyntaxTrees.Add(Parse("""
            using Microsoft.AspNetCore.Components;

            namespace Test;

            public class Col : ComponentBase
            {
                [Parameter]
                public RenderFragment ChildContent { get; set; }
            }
            """));

        // Act
        var generated = CompileToCSharp("""
            @using Microsoft.AspNetCore.Components

            <Col />
            @{
                <Col />
                RenderFragment template = @<Col />;
            }
            """);

        // Assert
        AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
        AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
        CompileToAssembly(generated);
    }

    [IntegrationTestFact, WorkItem("https://github.com/dotnet/razor/issues/8460")]
    public void VoidTagName_NoMatchingComponent()
    {
        // Act
        var generated = CompileToCSharp("""
            @using Microsoft.AspNetCore.Components

            <Col>in markup</Col>
            @{
                <Col>in code block</Col>
                RenderFragment template = @<Col>in template</Col>;
            }
            """);

        // Assert
        AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
        AssertCSharpDocumentMatchesBaseline(generated.CodeDocument, verifyLinePragmas: !DesignTime);
        CompileToAssembly(generated, verifyDiagnostics: static diagnostics =>
        {
            // Malformed C# is generated due to everything after the <Col> tag being considered C#.
            Assert.Contains(diagnostics, static d => d.Severity == DiagnosticSeverity.Error);
        });
    }

    #endregion

    #region Directives

    [IntegrationTestFact]
    public void ChildComponent_WithPageDirective()
    {
        // Arrange
        AdditionalSyntaxTrees.Add(Parse(@"
using Microsoft.AspNetCore.Components;

namespace Test
{
    public class MyComponent : ComponentBase
    {
    }
}
"));

        // Act
        var generated = CompileToCSharp(@"
@page ""/MyPage""
@page ""/AnotherRoute/{id}""
<MyComponent />");

        // Assert
        AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
        AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
        CompileToAssembly(generated);
    }

    [IntegrationTestFact]
    public void Component_WithUsingDirectives()
    {
        // Arrange
        AdditionalSyntaxTrees.Add(Parse(@"
using Microsoft.AspNetCore.Components;

namespace Test
{
    public class MyComponent : ComponentBase
    {
    }
}

namespace Test2
{
    public class MyComponent2 : ComponentBase
    {
    }
}
"));

        // Act
        var generated = CompileToCSharp(@"
@page ""/MyPage""
@page ""/AnotherRoute/{id}""
@using Test2
<MyComponent />
<MyComponent2 />");

        // Assert
        AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
        AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
        CompileToAssembly(generated);
    }

    [IntegrationTestFact]
    public void Component_WithUsingDirectives_AmbiguousImport()
    {
        // Arrange
        AdditionalSyntaxTrees.Add(Parse(@"
using Microsoft.AspNetCore.Components;

namespace Test
{
    public class MyComponent : ComponentBase
    {
    }
}

namespace Test2
{
    public class SomeComponent : ComponentBase
    {
    }
}

namespace Test3
{
    public class SomeComponent : ComponentBase
    {
    }
}
"));

        // Act
        var generated = CompileToCSharp(@"
@using Test2
@using Test3
<MyComponent />
<SomeComponent />");

        // Assert
        AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
        AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
        var result = CompileToAssembly(generated);

        if (DesignTime)
        {
            Assert.Collection(generated.RazorDiagnostics, d =>
            {
                Assert.Equal("RZ9985", d.Id);
                Assert.Equal(RazorDiagnosticSeverity.Error, d.Severity);
                Assert.Equal("Multiple components use the tag 'SomeComponent'. Components: Test2.SomeComponent, Test3.SomeComponent", d.GetMessage(CultureInfo.InvariantCulture));
            });
        }
    }

    [IntegrationTestFact]
    public void Component_IgnoresStaticAndAliasUsings()
    {
        // Arrange
        AdditionalSyntaxTrees.Add(Parse(@"
using Microsoft.AspNetCore.Components;

namespace Test
{
    public class MyComponent : ComponentBase
    {
    }
}

namespace Test2
{
    public class SomeComponent : ComponentBase
    {
    }
}

namespace Test3
{
    public class SomeComponent : ComponentBase
    {
    }
}
"));

        // Act
        var generated = CompileToCSharp(@"
@using static Test2.SomeComponent
@using Foo = Test3
<MyComponent />
<SomeComponent /> <!-- Not a component -->");

        // Assert
        AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
        AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
        CompileToAssembly(generated);
    }

    [IntegrationTestFact]
    public void Component_WithMultipleUsingDirectives()
    {
        var generated = CompileToCSharp(@"
@using System.IO ;@using Microsoft.AspNetCore.Components
; @using System.Reflection;");

        // Assert
        AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
        AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
        CompileToAssembly(generated);
    }

    [IntegrationTestFact]
    public void ChildContent_FromAnotherNamespace()
    {
        // Arrange
        AdditionalSyntaxTrees.Add(Parse(@"
using System;
using Microsoft.AspNetCore.Components;

namespace Test
{
    public class HeaderComponent : ComponentBase
    {
        [Parameter]
        public RenderFragment Header { get; set; }
    }
}

namespace AnotherTest
{
    public class FooterComponent : ComponentBase
    {
        [Parameter]
        public RenderFragment<DateTime> Footer { get; set; }
    }
}
"));

        // Act
        var generated = CompileToCSharp(@"
@using AnotherTest

<HeaderComponent>
    <Header>Hi!</Header>
</HeaderComponent>
<FooterComponent>
    <Footer>@context</Footer>
</FooterComponent>
<Test.HeaderComponent>
    <Header>Hi!</Header>
</Test.HeaderComponent>
<AnotherTest.FooterComponent>
    <Footer>@context</Footer>
</AnotherTest.FooterComponent>
");

        // Assert
        AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
        AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
        CompileToAssembly(generated);
    }

    [IntegrationTestFact]
    public void Component_WithNamespaceDirective()
    {
        // Arrange
        AdditionalSyntaxTrees.Add(Parse(@"
using System;
using Microsoft.AspNetCore.Components;

namespace Test
{
    public class HeaderComponent : ComponentBase
    {
        [Parameter]
        public string Header { get; set; }
    }
}

namespace AnotherTest
{
    public class FooterComponent : ComponentBase
    {
        [Parameter]
        public string Footer { get; set; }
    }
}
"));

        // Act
        var generated = CompileToCSharp(@"
@using Test
@namespace AnotherTest

<HeaderComponent Header='head'>
</HeaderComponent>
<FooterComponent Footer='feet'>
</FooterComponent>
");

        // Assert
        AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
        AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
        CompileToAssembly(generated);
    }

    [IntegrationTestFact]
    public void Component_WithNamespaceDirective_WithWhitespace()
    {
        var generated = CompileToCSharp(@"
@namespace              My.Custom.Namespace
");

        // Assert
        AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
        AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
        CompileToAssembly(generated);
    }

    [IntegrationTestFact]
    public void Component_WithPreserveWhitespaceDirective_True()
    {
        // Arrange / Act
        var generated = CompileToCSharp(@"
@preservewhitespace true

<ul>
    @foreach (var item in Enumerable.Range(1, 100))
    {
        <li>
            @item
        </li>
    }
</ul>

");

        // Assert
        AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
        AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
        CompileToAssembly(generated);
    }

    [IntegrationTestFact]
    public void Component_WithPreserveWhitespaceDirective_False()
    {
        // Arrange / Act
        var generated = CompileToCSharp(@"
@preservewhitespace false

<ul>
    @foreach (var item in Enumerable.Range(1, 100))
    {
        <li>
            @item
        </li>
    }
</ul>

");

        // Assert
        AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
        AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
        CompileToAssembly(generated);
    }

    [IntegrationTestFact]
    public void Component_WithPreserveWhitespaceDirective_Invalid()
    {
        // Arrange / Act
        var generated = CompileToCSharp(@"
@preservewhitespace someVariable
@code {
    bool someVariable = false;
}
");

        // Assert
        Assert.Collection(generated.RazorDiagnostics, d => { Assert.Equal("RZ1038", d.Id); });
    }

    [IntegrationTestFact, WorkItem("https://github.com/dotnet/razor/issues/10963")]
    public void InheritsDirective()
    {
        // Arrange
        AdditionalSyntaxTrees.Add(Parse("""
            namespace Test;

            public class BaseComponent : Microsoft.AspNetCore.Components.ComponentBase
            {
            }
            """));

        // Act
        var generated = CompileToCSharp("""
            @inherits BaseComponent
            """);

        // Assert
        AssertDocumentNodeMatchesBaseline(generated.CodeDocument);

        AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
        CompileToAssembly(generated);
    }

    [IntegrationTestFact, WorkItem("https://github.com/dotnet/razor/issues/7169")]
    public void InheritsDirective_NullableReferenceType()
    {
        // Arrange
        AdditionalSyntaxTrees.Add(Parse("""
            namespace Test;

            public class BaseComponent<T> : Microsoft.AspNetCore.Components.ComponentBase
            {
                protected T _field = default!;
            }
            """));

        // Act
        var generated = CompileToCSharp("""
            @inherits BaseComponent<string?>

            <h1>My component</h1>
            @(_field.ToString())
            """,
            nullableEnable: true);

        // Assert
        AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
        AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
        CompileToAssembly(generated, DesignTime
            ? // x:\dir\subdir\Test\TestComponent.cshtml(4,7): warning CS8602: Dereference of a possibly null reference.
              // __o = _field.ToString();
              Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "_field").WithLocation(4, 7)
            : // x:\dir\subdir\Test\TestComponent.cshtml(4,7): warning CS8602: Dereference of a possibly null reference.
              // __o = _field.ToString();
              Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "_field").WithLocation(4, 3));
    }

    [IntegrationTestFact, WorkItem("https://github.com/dotnet/razor/issues/7169")]
    public void InheritsDirective_NullableReferenceType_NullableDisabled()
    {
        // Arrange
        AdditionalSyntaxTrees.Add(Parse("""
            namespace Test;

            public class BaseComponent<T> : Microsoft.AspNetCore.Components.ComponentBase
            {
                protected T _field;
            }
            """));

        // Act
        var generated = CompileToCSharp("""
            @inherits BaseComponent<string?>

            <h1>My component</h1>
            @(_field.ToString())
            """,
            nullableEnable: false,
            expectedCSharpDiagnostics:
                // (1,31): warning CS8669: The annotation for nullable reference types should only be used in code within a '#nullable' annotations context. Auto-generated code requires an explicit '#nullable' directive in source.
                //     public partial class TestComponent : BaseComponent<string?>
                Diagnostic(ErrorCode.WRN_MissingNonNullTypesContextForAnnotationInGeneratedCode, "?").WithLocation(1, 31));

        // Assert
        Assert.Empty(generated.RazorDiagnostics);
        AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
        AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
        CompileToAssembly(generated, DesignTime
            ? [
                // x:\dir\subdir\Test\TestComponent.cshtml(1,21): warning CS8669: The annotation for nullable reference types should only be used in code within a '#nullable' annotations context. Auto-generated code requires an explicit '#nullable' directive in source.
                // BaseComponent<string?> __typeHelper = default!;
                Diagnostic(ErrorCode.WRN_MissingNonNullTypesContextForAnnotationInGeneratedCode, "?").WithLocation(1, 21),
                // (14,62): warning CS8669: The annotation for nullable reference types should only be used in code within a '#nullable' annotations context. Auto-generated code requires an explicit '#nullable' directive in source.
                //     public partial class TestComponent : BaseComponent<string?>
                Diagnostic(ErrorCode.WRN_MissingNonNullTypesContextForAnnotationInGeneratedCode, "?").WithLocation(14, 62)
            ]
            : [
                // (1,31): warning CS8669: The annotation for nullable reference types should only be used in code within a '#nullable' annotations context. Auto-generated code requires an explicit '#nullable' directive in source.
                //     public partial class TestComponent : BaseComponent<string?>
                Diagnostic(ErrorCode.WRN_MissingNonNullTypesContextForAnnotationInGeneratedCode, "?").WithLocation(1, 31)
            ]);
    }

    [IntegrationTestFact, WorkItem("https://github.com/dotnet/razor/issues/10863")]
    public void PageDirective_NoForwardSlash()
    {
        // Act
        var generated = CompileToCSharp("""
            @page "MyPage"

            """);

        // Assert
        AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
        AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
        CompileToAssembly(generated);
    }

    [IntegrationTestFact, WorkItem("https://github.com/dotnet/razor/issues/10863")]
    public void PageDirective_NoForwardSlash_WithComment()
    {
        // Act
        var generated = CompileToCSharp("""
            @page /* comment */ "MyPage"

            """);

        // Assert
        AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
        AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
        CompileToAssembly(generated);
    }

    [IntegrationTestFact, WorkItem("https://github.com/dotnet/razor/issues/10863")]
    public void PageDirective_MissingRoute()
    {
        // Act
        var generated = CompileToCSharp("""
            @page

            """);

        // Assert
        AssertDocumentNodeMatchesBaseline(generated.CodeDocument);

        // Design time writer doesn't correctly emit pragmas for missing tokens, so don't validate them in design time
        AssertCSharpDocumentMatchesBaseline(generated.CodeDocument, verifyLinePragmas: !DesignTime);
        CompileToAssembly(generated);
    }

    [IntegrationTestFact, WorkItem("https://github.com/dotnet/razor/issues/10863")]
    public void PageDirective_MissingRoute_WithComment()
    {
        // Act
        var generated = CompileToCSharp("""
            @page /* comment */

            """);

        // Assert
        AssertDocumentNodeMatchesBaseline(generated.CodeDocument);

        // Design time writer doesn't correctly emit pragmas for missing tokens, so don't validate them in design time
        AssertCSharpDocumentMatchesBaseline(generated.CodeDocument, verifyLinePragmas: !DesignTime);
        CompileToAssembly(generated);
    }

    [IntegrationTestFact, WorkItem("https://github.com/dotnet/razor/issues/10863")]
    public void UsingDirective()
    {
        // Act
        var generated = CompileToCSharp("""
            @using System.Collections

            """);

        // Assert
        AssertDocumentNodeMatchesBaseline(generated.CodeDocument);

        AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
        CompileToAssembly(generated);
    }

    #endregion

    #region EventCallback

    [IntegrationTestFact]
    public void EventCallback_CanPassEventCallback_Explicitly()
    {
        // Arrange
        AdditionalSyntaxTrees.Add(Parse(@"
using System;
using Microsoft.AspNetCore.Components;

namespace Test
{
    public class MyComponent : ComponentBase
    {
        [Parameter]
        public EventCallback OnClick { get; set; }
    }
}
"));

        // Act
        var generated = CompileToCSharp(@"
<MyComponent OnClick=""@(EventCallback.Factory.Create(this, Increment))""/>

@code {
    private int counter;
    private void Increment() {
        counter++;
    }
}");

        // Assert
        AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
        AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
        CompileToAssembly(generated);
    }

    [IntegrationTestFact]
    public void EventCallbackOfT_GenericComponent_ExplicitType()
    {
        // Arrange
        AdditionalSyntaxTrees.Add(Parse(@"
using System;
using Microsoft.AspNetCore.Components;

namespace Test
{
    public class MyComponent<T> : ComponentBase
    {
        [Parameter]
        public EventCallback<T> OnClick { get; set; }
    }

    public class MyType
    {
    }
}
"));

        // Act
        var generated = CompileToCSharp(@"
<MyComponent T=""MyType"" OnClick=""@((MyType arg) => counter++)""/>

@code {
    private int counter;
}");

        // Assert
        AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
        AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
        CompileToAssembly(generated);
    }

    [IntegrationTestFact]
    public void EventCallbackOfT_GenericComponent_ExplicitType_MethodGroup()
    {
        // Arrange
        AdditionalSyntaxTrees.Add(Parse(@"
using System;
using Microsoft.AspNetCore.Components;

namespace Test
{
    public class MyComponent<T> : ComponentBase
    {
        [Parameter]
        public EventCallback<T> OnClick { get; set; }
    }

    public class MyType
    {
    }
}
"));

        // Act
        var generated = CompileToCSharp(@"
<MyComponent T=""MyType"" OnClick=""Increment""/>

@code {
    private int counter;

    public void Increment(MyType type) => counter++;
}");

        // Assert
        AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
        AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
        CompileToAssembly(generated);
    }

    [IntegrationTestFact]
    public void EventCallback_CanPassEventCallbackOfT_Explicitly()
    {
        // Arrange
        AdditionalSyntaxTrees.Add(Parse(@"
using System;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

namespace Test
{
    public class MyComponent : ComponentBase
    {
        [Parameter]
        public EventCallback<MouseEventArgs> OnClick { get; set; }
    }
}
"));

        // Act
        var generated = CompileToCSharp(@"
@using Microsoft.AspNetCore.Components.Web
<MyComponent OnClick=""@(EventCallback.Factory.Create<MouseEventArgs>(this, Increment))""/>

@code {
    private int counter;
    private void Increment() {
        counter++;
    }
}");

        // Assert
        AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
        AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
        CompileToAssembly(generated);
    }

    [IntegrationTestFact]
    public void EventCallback_CanPassEventCallback_Implicitly_Action()
    {
        // Arrange
        AdditionalSyntaxTrees.Add(Parse(@"
using System;
using Microsoft.AspNetCore.Components;

namespace Test
{
    public class MyComponent : ComponentBase
    {
        [Parameter]
        public EventCallback OnClick { get; set; }
    }
}
"));

        // Act
        var generated = CompileToCSharp(@"
<MyComponent OnClick=""@Increment""/>

@code {
    private int counter;
    private void Increment() {
        counter++;
    }
}");

        // Assert
        AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
        AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
        CompileToAssembly(generated);
    }

    [IntegrationTestFact]
    public void EventCallback_CanPassEventCallback_Implicitly_ActionOfObject()
    {
        // Arrange
        AdditionalSyntaxTrees.Add(Parse(@"
using System;
using Microsoft.AspNetCore.Components;

namespace Test
{
    public class MyComponent : ComponentBase
    {
        [Parameter]
        public EventCallback OnClick { get; set; }
    }
}
"));

        // Act
        var generated = CompileToCSharp(@"
<MyComponent OnClick=""@Increment""/>

@code {
    private int counter;
    private void Increment(object e) {
        counter++;
    }
}");

        // Assert
        AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
        AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
        CompileToAssembly(generated);
    }

    [IntegrationTestFact]
    public void EventCallback_CanPassEventCallback_Implicitly_FuncOfTask()
    {
        // Arrange
        AdditionalSyntaxTrees.Add(Parse(@"
using System;
using Microsoft.AspNetCore.Components;

namespace Test
{
    public class MyComponent : ComponentBase
    {
        [Parameter]
        public EventCallback OnClick { get; set; }
    }
}
"));

        // Act
        var generated = CompileToCSharp(@"
<MyComponent OnClick=""@Increment""/>

@code {
    private int counter;
    private Task Increment() {
        counter++;
        return Task.CompletedTask;
    }
}");

        // Assert
        AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
        AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
        CompileToAssembly(generated);
    }

    [IntegrationTestFact]
    public void EventCallback_CanPassEventCallback_Implicitly_FuncOfobjectTask()
    {
        // Arrange
        AdditionalSyntaxTrees.Add(Parse(@"
using System;
using Microsoft.AspNetCore.Components;

namespace Test
{
    public class MyComponent : ComponentBase
    {
        [Parameter]
        public EventCallback OnClick { get; set; }
    }
}
"));

        // Act
        var generated = CompileToCSharp(@"
<MyComponent OnClick=""@Increment""/>

@code {
    private int counter;
    private Task Increment(object e) {
        counter++;
        return Task.CompletedTask;
    }
}");

        // Assert
        AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
        AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
        CompileToAssembly(generated);
    }

    [IntegrationTestFact]
    public void EventCallback_CanPassEventCallbackOfT_Implicitly_Action()
    {
        // Arrange
        AdditionalSyntaxTrees.Add(Parse(@"
using System;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

namespace Test
{
    public class MyComponent : ComponentBase
    {
        [Parameter]
        public EventCallback<MouseEventArgs> OnClick { get; set; }
    }
}
"));

        // Act
        var generated = CompileToCSharp(@"
<MyComponent OnClick=""@Increment""/>

@code {
    private int counter;
    private void Increment() {
        counter++;
    }
}");

        // Assert
        AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
        AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
        CompileToAssembly(generated);
    }

    [IntegrationTestFact]
    public void EventCallback_CanPassEventCallbackOfT_Implicitly_ActionOfT()
    {
        // Arrange
        AdditionalSyntaxTrees.Add(Parse(@"
using System;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

namespace Test
{
    public class MyComponent : ComponentBase
    {
        [Parameter]
        public EventCallback<MouseEventArgs> OnClick { get; set; }
    }
}
"));

        // Act
        var generated = CompileToCSharp(@"
@using Microsoft.AspNetCore.Components.Web
<MyComponent OnClick=""@Increment""/>

@code {
    private int counter;
    private void Increment(MouseEventArgs e) {
        counter++;
    }
}");

        // Assert
        AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
        AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
        CompileToAssembly(generated);
    }

    [IntegrationTestFact]
    public void EventCallback_CanPassEventCallbackOfT_Implicitly_FuncOfTask()
    {
        // Arrange
        AdditionalSyntaxTrees.Add(Parse(@"
using System;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

namespace Test
{
    public class MyComponent : ComponentBase
    {
        [Parameter]
        public EventCallback<MouseEventArgs> OnClick { get; set; }
    }
}
"));

        // Act
        var generated = CompileToCSharp(@"
<MyComponent OnClick=""@Increment""/>

@code {
    private int counter;
    private Task Increment() {
        counter++;
        return Task.CompletedTask;
    }
}");

        // Assert
        AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
        AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
        CompileToAssembly(generated);
    }

    [IntegrationTestFact]
    public void EventCallback_CanPassEventCallbackOfT_Implicitly_FuncOfTTask()
    {
        // Arrange
        AdditionalSyntaxTrees.Add(Parse(@"
using System;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

namespace Test
{
    public class MyComponent : ComponentBase
    {
        [Parameter]
        public EventCallback<MouseEventArgs> OnClick { get; set; }
    }
}
"));

        // Act
        var generated = CompileToCSharp(@"
@using Microsoft.AspNetCore.Components.Web
<MyComponent OnClick=""@Increment""/>

@code {
    private int counter;
    private Task Increment(MouseEventArgs e) {
        counter++;
        return Task.CompletedTask;
    }
}");

        // Assert
        AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
        AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
        CompileToAssembly(generated);
    }

    [IntegrationTestFact]
    public void EventCallback_CanPassEventCallbackOfT_Implicitly_TypeMismatch()
    {
        // Arrange
        AdditionalSyntaxTrees.Add(Parse(@"
using System;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

namespace Test
{
    public class MyComponent : ComponentBase
    {
        [Parameter]
        public EventCallback<MouseEventArgs> OnClick { get; set; }
    }
}
"));

        // Act
        var generated = CompileToCSharp(@"
@using Microsoft.AspNetCore.Components.Web
<MyComponent OnClick=""@Increment""/>

@code {
    private int counter;
    private void Increment(ChangeEventArgs e) {
        counter++;
    }
}");

        // Assert
        AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
        AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);

        CompileToAssembly(generated,
            // x:\dir\subdir\Test\TestComponent.cshtml(2,24): error CS1503: Argument 2: cannot convert from 'method group' to 'Microsoft.AspNetCore.Components.EventCallback'
            //                        Increment
            Diagnostic(ErrorCode.ERR_BadArgType, "Increment").WithArguments("2", "method group", "Microsoft.AspNetCore.Components.EventCallback").WithLocation(2, 24));
    }

    [IntegrationTestFact]
    public void EventCallbackOfT_GenericComponent_MissingTypeParameterBinding_01()
    {
        // Arrange
        AdditionalSyntaxTrees.Add(Parse(@"
using System;
using Microsoft.AspNetCore.Components;

namespace Test
{
    public class MyComponent<T, T2> : ComponentBase
    {
        [Parameter]
        public EventCallback<T> OnClick { get; set; }
    }

    public class MyType
    {
    }
}
"));

        // Act
        var generated = CompileToCSharp(@"
<MyComponent OnClick=""@((MyType arg) => counter++)""/>

@code {
    private int counter;
}");

        // Assert
        AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
        AssertCSharpDocumentMatchesBaseline(generated.CodeDocument, verifyLinePragmas: DesignTime);
        CompileToAssembly(generated,
            // x:\dir\subdir\Test\TestComponent.cshtml(4,17): warning CS0169: The field 'TestComponent.counter' is never used
            //     private int counter;
            Diagnostic(ErrorCode.WRN_UnreferencedField, "counter").WithArguments("Test.TestComponent.counter").WithLocation(4, 17));
    }

    [IntegrationTestFact]
    public void EventCallbackOfT_GenericComponent_MissingTypeParameterBinding_02()
    {
        // Arrange
        AdditionalSyntaxTrees.Add(Parse(@"
using System;
using Microsoft.AspNetCore.Components;

namespace Test
{
    public class MyComponent<T, T2> : ComponentBase
    {
        [Parameter]
        public EventCallback<T> OnClick1 { get; set; }

        [Parameter]
        public EventCallback<T2> OnClick2 { get; set; }
    }

    public class MyType
    {
    }
}
"));

        // Act
        var generated = CompileToCSharp(@"
<MyComponent OnClick=""@((MyType arg) => counter++)""/>

@code {
    private int counter;
}");

        // Assert
        AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
        AssertCSharpDocumentMatchesBaseline(generated.CodeDocument, verifyLinePragmas: DesignTime);
        CompileToAssembly(generated,
            // x:\dir\subdir\Test\TestComponent.cshtml(4,17): warning CS0169: The field 'TestComponent.counter' is never used
            //     private int counter;
            Diagnostic(ErrorCode.WRN_UnreferencedField, "counter").WithArguments("Test.TestComponent.counter").WithLocation(4, 17));
    }

    [IntegrationTestFact, WorkItem("https://github.com/dotnet/aspnetcore/issues/48526")]
    public void EventCallbackOfT_Array()
    {
        // Arrange
        AdditionalSyntaxTrees.Add(Parse("""
            using Microsoft.AspNetCore.Components.Forms;

            namespace Test;

            public class MyComponent<TItem> : InputBase<TItem[]>
            {
                protected override bool TryParseValueFromString(string value, out TItem[] result, out string validationErrorMessage) => throw null;
            }
            """));

        // Act
        var generated = CompileToCSharp("""
            <MyComponent @bind-Value="Selected" />

            @code {
                string[] Selected { get; set; } = Array.Empty<string>();
            }
            """);

        // Assert
        AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
        AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
        CompileToAssembly(generated);
    }

    #endregion

    #region Event Handlers

    [IntegrationTestFact]
    public void Component_WithImplicitLambdaEventHandler()
    {
        // Arrange
        AdditionalSyntaxTrees.Add(Parse(@"
using System;
using Microsoft.AspNetCore.Components;

namespace Test
{
    public class MyComponent : ComponentBase
    {
    }
}
"));

        // Act
        var generated = CompileToCSharp(@"
<MyComponent @onclick=""() => Increment()""/>

@code {
    private int counter;
    private void Increment() {
        counter++;
    }
}");

        // Assert
        AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
        AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
        CompileToAssembly(generated);
    }

    [IntegrationTestFact]
    public void ChildComponent_WithLambdaEventHandler()
    {
        // Arrange
        AdditionalSyntaxTrees.Add(Parse(@"
using System;
using Microsoft.AspNetCore.Components;

namespace Test
{
    public class MyComponent : ComponentBase
    {
        [Parameter]
        public Action<EventArgs> OnClick { get; set; }
    }
}
"));

        // Act
        var generated = CompileToCSharp(@"
<MyComponent OnClick=""@(e => { Increment(); })""/>

@code {
    private int counter;
    private void Increment() {
        counter++;
    }
}");

        // Assert
        AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
        AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
        CompileToAssembly(generated);
    }

    // Regression test for #954 - we need to allow arbitrary event handler
    // attributes with weak typing.
    [IntegrationTestFact]
    public void ChildComponent_WithWeaklyTypeEventHandler()
    {
        // Arrange
        AdditionalSyntaxTrees.Add(Parse(@"
using System;
using Microsoft.AspNetCore.Components;

namespace Test
{
    public class DynamicElement : ComponentBase
    {
    }
}
"));

        // Act
        var generated = CompileToCSharp(@"
@using Microsoft.AspNetCore.Components.Web
<DynamicElement @onclick=""OnClick"" />

@code {
    private Action<MouseEventArgs> OnClick { get; set; }
}");

        // Assert
        AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
        AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
        CompileToAssembly(generated);
    }

    [IntegrationTestFact]
    public void ChildComponent_WithExplicitEventHandler()
    {
        // Arrange
        AdditionalSyntaxTrees.Add(Parse(@"
using System;
using Microsoft.AspNetCore.Components;

namespace Test
{
    public class MyComponent : ComponentBase
    {
        [Parameter]
        public Action<EventArgs> OnClick { get; set; }
    }
}
"));

        // Act
        var generated = CompileToCSharp(@"
<MyComponent OnClick=""@Increment""/>

@code {
    private int counter;
    private void Increment(EventArgs e) {
        counter++;
    }
}");

        // Assert
        AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
        AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
        CompileToAssembly(generated);
    }

    [IntegrationTestFact]
    public void EventHandler_OnElement_WithString()
    {
        // Arrange

        // Act
        var generated = CompileToCSharp(@"
@using Microsoft.AspNetCore.Components.Web
<input onclick=""foo"" />");

        // Assert
        AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
        AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
        CompileToAssembly(generated);
    }

    [IntegrationTestFact]
    public void EventHandler_OnElement_WithNoArgsLambdaDelegate()
    {
        // Arrange

        // Act
        var generated = CompileToCSharp(@"
@using Microsoft.AspNetCore.Components.Web
<input @onclick=""() => { }"" />");

        // Assert
        AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
        AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
        CompileToAssembly(generated);
    }

    [IntegrationTestFact]
    public void EventHandler_OnElement_WithEventArgsLambdaDelegate()
    {
        // Arrange

        // Act
        var generated = CompileToCSharp(@"
@using Microsoft.AspNetCore.Components.Web
<input @onclick=""x => { }"" />");

        // Assert
        AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
        AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
        CompileToAssembly(generated);
    }

    [IntegrationTestFact]
    public void EventHandler_OnElement_WithNoArgMethodGroup()
    {
        // Arrange

        // Act
        var generated = CompileToCSharp(@"
@using Microsoft.AspNetCore.Components.Web
<input @onclick=""OnClick"" />
@code {
    void OnClick() {
    }
}");

        // Assert
        AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
        AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
        CompileToAssembly(generated);
    }

    [IntegrationTestFact]
    public void EventHandler_OnElement_WithoutCloseTag()
    {
        // Arrange

        // Act
        var generated = CompileToCSharp(@"
@using Microsoft.AspNetCore.Components.Web
<div>
  <input @onclick=""OnClick"">
</div>
@code {
    void OnClick() {
    }
}");

        // Assert
        AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
        AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
        CompileToAssembly(generated);
    }

    [IntegrationTestFact]
    public void EventHandler_OnElement_WithEventArgsMethodGroup()
    {
        // Arrange

        // Act
        var generated = CompileToCSharp(@"
@using Microsoft.AspNetCore.Components.Web
<input @onclick=""OnClick"" />
@code {
    void OnClick(MouseEventArgs e) {
    }
}");

        // Assert
        AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
        AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
        CompileToAssembly(generated);
    }

    [IntegrationTestFact]
    public void EventHandler_OnElement_ArbitraryEventName_WithEventArgsMethodGroup()
    {
        // Arrange

        // Act
        var generated = CompileToCSharp(@"
@using Microsoft.AspNetCore.Components.Web
<input @onclick=""OnClick"" />
@code {
    void OnClick(EventArgs e) {
    }
}");

        // Assert
        AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
        AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
        CompileToAssembly(generated);
    }

    [IntegrationTestFact]
    public void AsyncEventHandler_OnElement_Action_MethodGroup()
    {
        // Arrange

        // Act
        var generated = CompileToCSharp(@"
@using System.Threading.Tasks
@using Microsoft.AspNetCore.Components.Web
<input @onclick=""OnClick"" />
@code {
    Task OnClick()
    {
        return Task.CompletedTask;
    }
}");

        // Assert
        AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
        AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
        CompileToAssembly(generated);
    }

    [IntegrationTestFact]
    public void AsyncEventHandler_OnElement_ActionEventArgs_MethodGroup()
    {
        // Arrange

        // Act
        var generated = CompileToCSharp(@"
@using System.Threading.Tasks
@using Microsoft.AspNetCore.Components.Web
<input @onclick=""OnClick"" />
@code {
    Task OnClick(MouseEventArgs e)
    {
        return Task.CompletedTask;
    }
}");

        // Assert
        AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
        AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
        CompileToAssembly(generated);
    }

    [IntegrationTestFact]
    public void AsyncEventHandler_OnElement_Action_Lambda()
    {
        // Arrange

        // Act
        var generated = CompileToCSharp(@"
@using System.Threading.Tasks
@using Microsoft.AspNetCore.Components.Web
<input @onclick=""@(async () => await Task.Delay(10))"" />
");

        // Assert
        AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
        AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
        CompileToAssembly(generated);
    }

    [IntegrationTestFact]
    public void AsyncEventHandler_OnElement_ActionEventArgs_Lambda()
    {
        // Arrange

        // Act
        var generated = CompileToCSharp(@"
@using System.Threading.Tasks
@using Microsoft.AspNetCore.Components.Web
<input @onclick=""@(async (e) => await Task.Delay(10))"" />
");

        // Assert
        AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
        AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
        CompileToAssembly(generated);
    }

    [IntegrationTestFact]
    public void EventHandler_OnElement_WithLambdaDelegate()
    {
        // Arrange

        // Act
        var generated = CompileToCSharp(@"
@using Microsoft.AspNetCore.Components.Web
<input @onclick=""x => { }"" />");

        // Assert
        AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
        AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
        CompileToAssembly(generated);
    }

    [IntegrationTestFact]
    public void EventHandler_OnElement_WithDelegate()
    {
        // Arrange

        // Act
        var generated = CompileToCSharp(@"
@using Microsoft.AspNetCore.Components.Web
<input @onclick=""OnClick"" />
@code {
    void OnClick(MouseEventArgs e) {
    }
}");

        // Assert
        AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
        AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
        CompileToAssembly(generated);
    }

    [IntegrationTestFact]
    public void EventHandler_AttributeNameIsCaseSensitive()
    {
        // Arrange

        // Act
        var generated = CompileToCSharp(@"
@using Microsoft.AspNetCore.Components.Web
<input @onCLICK=""OnClick"" />
@code {
    void OnClick(MouseEventArgs e) {
    }
}");

        // Assert
        AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
        AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
        CompileToAssembly(generated);
    }

    [IntegrationTestFact]
    public void EventHandler_PreventDefault_StopPropagation_Minimized()
    {
        // Arrange

        // Act
        var generated = CompileToCSharp(@"
@using Microsoft.AspNetCore.Components.Web
<button @onclick:preventDefault @onclick:stopPropagation>Click Me</button>");

        // Assert
        AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
        AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
        CompileToAssembly(generated);
    }

    [IntegrationTestFact]
    public void EventHandler_PreventDefault_StopPropagation()
    {
        // Arrange

        // Act
        var generated = CompileToCSharp(@"
@using Microsoft.AspNetCore.Components.Web
<button @onclick=""() => Foo = false"" @onfocus:preventDefault=""true"" @onclick:stopPropagation=""Foo"" @onfocus:stopPropagation=""false"">Click Me</button>
@code {
    bool Foo { get; set; }
}");

        // Assert
        AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
        AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
        CompileToAssembly(generated);
    }

    [IntegrationTestFact]
    public void EventHandler_WithDelegate_PreventDefault()
    {
        // Arrange

        // Act
        var generated = CompileToCSharp(@"
@using Microsoft.AspNetCore.Components.Web
<input @onfocus=""OnFocus"" @onfocus:preventDefault=""ShouldPreventDefault()"" />
@code {
    void OnFocus(FocusEventArgs e) { }

    bool ShouldPreventDefault() { return false; }
}");

        // Assert
        AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
        AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
        CompileToAssembly(generated);
    }

    [IntegrationTestFact]
    public void EventHandler_PreventDefault_Duplicates()
    {
        // Arrange

        // Act
        var generated = CompileToCSharp(@"
@using Microsoft.AspNetCore.Components.Web
<input @onclick:preventDefault=""true"" @onclick:preventDefault />");

        // Assert
        AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
        AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
        CompileToAssembly(generated);
    }

    #endregion

    #region Generics

    [IntegrationTestFact]
    public void ChildComponent_Generic()
    {
        // Arrange
        AdditionalSyntaxTrees.Add(Parse(@"
using Microsoft.AspNetCore.Components;

namespace Test
{
    public class MyComponent<TItem> : ComponentBase
    {
        [Parameter] public TItem Item { get; set; }
    }
}
"));

        // Act
        var generated = CompileToCSharp(@"
<MyComponent TItem=string Item=""@(""hi"")""/>");

        // Assert
        AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
        AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
        CompileToAssembly(generated);
    }

    [IntegrationTestFact, WorkItem("https://github.com/dotnet/razor/issues/8467")]
    public void ChildComponent_AtSpecifiedInRazorFileForTypeParameter()
    {
        AdditionalSyntaxTrees.Add(Parse("""
            using Microsoft.AspNetCore.Components;
            namespace Test
            {
                public class C<T> : ComponentBase
                {
                    [Parameter] public int Item { get; set; }
                }
            }
            """));

        var generated = CompileToCSharp("""<C T="@string" Item="1" />""");

        AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
        AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
        CompileToAssembly(generated);
    }

    [IntegrationTestFact]
    public void GenericComponent_NonPrimitiveType()
    {
        // Arrange
        AdditionalSyntaxTrees.Add(Parse(@"
using Microsoft.AspNetCore.Components;

namespace Test
{
    public class MyComponent<TItem> : ComponentBase
    {
        [Parameter] public TItem Item { get; set; }
    }

    public class CustomType
    {
    }
}
"));

        // Act
        var generated = CompileToCSharp(@"
<MyComponent TItem=""CustomType"" Item=""new CustomType()""/>");

        // Assert
        AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
        AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
        CompileToAssembly(generated);
    }

    [IntegrationTestFact]
    public void GenericComponent_NonPrimitiveTypeRenderFragment()
    {
        // Arrange
        AdditionalSyntaxTrees.Add(Parse(@"
using Microsoft.AspNetCore.Components;

namespace Test
{
    public class MyComponent<TItem> : ComponentBase
    {
        [Parameter] public TItem Item { get; set; }

        [Parameter] public RenderFragment<CustomType> ChildContent { get; set; }
    }

    public class CustomType
    {
    }
}
"));

        // Act
        var generated = CompileToCSharp(@"
<MyComponent Item=""new CustomType()"">@context.ToString()</MyComponent>");

        // Assert
        AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
        AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
        CompileToAssembly(generated);
    }

    [IntegrationTestFact]
    public void ChildComponent_Generic_TypeInference()
    {
        // Arrange
        AdditionalSyntaxTrees.Add(Parse(@"
using Microsoft.AspNetCore.Components;

namespace Test
{
    public class MyComponent<TItem> : ComponentBase
    {
        [Parameter] public TItem Item { get; set; }
    }
}
"));

        // Act
        var generated = CompileToCSharp(@"
<MyComponent Item=""@(""hi"")""/>");

        // Assert
        AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
        AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
        CompileToAssembly(generated);
    }

    [IntegrationTestFact]
    public void ChildComponent_Generic_TypeInference_Multiple()
    {
        // Arrange
        AdditionalSyntaxTrees.Add(Parse(@"
using Microsoft.AspNetCore.Components;

namespace Test
{
    public class MyComponent<TItem> : ComponentBase
    {
        [Parameter] public TItem Item { get; set; }
    }
}
"));

        // Act
        var generated = CompileToCSharp(@"
<MyComponent Item=""@(""hi"")""/>
<MyComponent Item=""@(""how are you?"")""/>
<MyComponent Item=""@(""bye!"")""/>");

        // Assert
        AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
        AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
        CompileToAssembly(generated);
    }

    [IntegrationTestFact]
    public void CascadingGenericInference_Explicit()
    {
        // Arrange
        AdditionalSyntaxTrees.Add(Parse(@"
using Microsoft.AspNetCore.Components;

namespace Test
{
    [CascadingTypeParameter(nameof(TItem))]
    public class Grid<TItem> : ComponentBase
    {
        [Parameter] public System.Collections.Generic.IEnumerable<TItem> Items { get; set; }
        [Parameter] public RenderFragment ChildContent { get; set; }
    }

    public class Column<TItem> : ComponentBase
    {
    }
}
"));

        // Act
        var generated = CompileToCSharp(@"
<Grid TItem=""DateTime"" Items=""@(Array.Empty<DateTime>())""><Column /><Column /></Grid>");

        // Assert
        AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
        AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
        CompileToAssembly(generated);
    }

    [IntegrationTestFact]
    public void CascadingGenericInference_ExplicitOverride()
    {
        // Arrange
        AdditionalSyntaxTrees.Add(Parse(@"
using Microsoft.AspNetCore.Components;

namespace Test
{
    [CascadingTypeParameter(nameof(TItem))]
    public class Grid<TItem> : ComponentBase
    {
        [Parameter] public System.Collections.Generic.IEnumerable<TItem> Items { get; set; }
        [Parameter] public RenderFragment ChildContent { get; set; }
    }

    public class Column<TItem> : ComponentBase
    {
    }
}
"));

        // Act
        var generated = CompileToCSharp(@"
<Grid TItem=""DateTime"" Items=""@(Array.Empty<DateTime>())""><Column TItem=""System.TimeZoneInfo"" /></Grid>");

        // Assert
        AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
        AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
        CompileToAssembly(generated);
    }

    [IntegrationTestFact]
    public void CascadingGenericInference_NotCascaded_Explicit()
    {
        // The point of this test is to show that, without [CascadingTypeParameter], we don't cascade

        // Arrange
        AdditionalSyntaxTrees.Add(Parse(@"
using Microsoft.AspNetCore.Components;

namespace Test
{
    public class Grid<TItem> : ComponentBase
    {
        [Parameter] public System.Collections.Generic.IEnumerable<TItem> Items { get; set; }
        [Parameter] public RenderFragment ChildContent { get; set; }
    }

    public class Column<TItem> : ComponentBase
    {
    }
}
"));

        // Act
        var generated = CompileToCSharp(@"
<Grid TItem=""DateTime"" Items=""@(Array.Empty<DateTime>())""><Column /></Grid>");

        // Assert
        AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
        AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);

        var diagnostic = Assert.Single(generated.RazorDiagnostics);
        Assert.Same(ComponentDiagnosticFactory.GenericComponentTypeInferenceUnderspecified.Id, diagnostic.Id);
    }

    [IntegrationTestFact]
    public void CascadingGenericInference_NotCascaded_Inferred()
    {
        // Arrange
        AdditionalSyntaxTrees.Add(Parse(@"
using Microsoft.AspNetCore.Components;

namespace Test
{
    [CascadingTypeParameter(nameof(TItem))]
    public class Grid<TItem> : ComponentBase
    {
        [Parameter] public System.Collections.Generic.IEnumerable<TItem> Items { get; set; }
        [Parameter] public RenderFragment ChildContent { get; set; }
    }

    public class Column<TItem> : ComponentBase
    {
    }
}
"));

        // Act
        var generated = CompileToCSharp(@"
<Grid Items=""@(Array.Empty<DateTime>())""><Column /><Column /></Grid>");

        // Assert
        AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
        AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
        CompileToAssembly(generated);
    }

    [IntegrationTestFact]
    public void CascadingGenericInference_Inferred_WithConstraints()
    {
        // Arrange
        AdditionalSyntaxTrees.Add(Parse(@"
using Microsoft.AspNetCore.Components;

namespace Test
{
    [CascadingTypeParameter(nameof(TItem))]
    public class Grid<TItem> : ComponentBase
    {
        [Parameter] public RenderFragment ColumnsTemplate { get; set; }
    }

    public abstract partial class BaseColumn<TItem> : ComponentBase where TItem : class
    {
        [CascadingParameter]
        internal Grid<TItem> Grid { get; set; }
    }

    public class Column<TItem> : BaseColumn<TItem>, IGridFieldColumn<TItem> where TItem : class
    {
        [Parameter]
        public string FieldName { get; set; }
    }

    internal interface IGridFieldColumn<TItem> where TItem : class
    {
    }

    public class WeatherForecast { }
}
"));

        // Act
        var generated = CompileToCSharp(@"
<Grid TItem=""WeatherForecast"" Items=""@(Array.Empty<WeatherForecast>())"">
    <ColumnsTemplate>
        <Column Title=""Date"" FieldName=""Date"" Format=""d"" Width=""10rem"" />
    </ColumnsTemplate>
</Grid>
");

        // Assert
        AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
        AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
        CompileToAssembly(generated);
    }

    [IntegrationTestFact]
    public void CascadingGenericInference_Inferred_MultipleConstraints()
    {
        // Arrange
        AdditionalSyntaxTrees.Add(Parse(@"
using Microsoft.AspNetCore.Components;

namespace Test
{
    [CascadingTypeParameter(nameof(TItem))]
    public class Grid<TItem> : ComponentBase
    {
        [Parameter] public RenderFragment ColumnsTemplate { get; set; }
    }

    public abstract partial class BaseColumn<TItem> : ComponentBase where TItem : class, new()
    {
        [CascadingParameter]
        internal Grid<TItem> Grid { get; set; }
    }

    public class Column<TItem> : BaseColumn<TItem>, IGridFieldColumn<TItem> where TItem : class, new()
    {
        [Parameter]
        public string FieldName { get; set; }
    }

    internal interface IGridFieldColumn<TItem> where TItem : class
    {
    }

    public class WeatherForecast { }
}
"));

        // Act
        var generated = CompileToCSharp(@"
<Grid TItem=""WeatherForecast"" Items=""@(Array.Empty<WeatherForecast>())"">
    <ColumnsTemplate>
        <Column Title=""Date"" FieldName=""Date"" Format=""d"" Width=""10rem"" />
    </ColumnsTemplate>
</Grid>
");

        // Assert
        AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
        AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
        CompileToAssembly(generated);
    }

    [IntegrationTestFact]
    public void CascadingGenericInference_Inferred_MultipleConstraints_ClassesAndInterfaces()
    {
        // Arrange
        AdditionalSyntaxTrees.Add(Parse(@"
using Microsoft.AspNetCore.Components;
using Models;

namespace Test
{
    [CascadingTypeParameter(nameof(TItem))]
    public class Grid<TItem> : ComponentBase
    {
        [Parameter] public RenderFragment ColumnsTemplate { get; set; }
    }

    public abstract partial class BaseColumn<TItem> : ComponentBase where TItem : WeatherForecast, new()
    {
        [CascadingParameter]
        internal Grid<TItem> Grid { get; set; }
    }

    public class Column<TItem> : BaseColumn<TItem>, IGridFieldColumn<TItem> where TItem : WeatherForecast, new()
    {
        [Parameter]
        public string FieldName { get; set; }
    }

    internal interface IGridFieldColumn<TItem> where TItem : class
    {
    }
}
namespace Models {
    public class WeatherForecast { }
}"));

        // Act
        var generated = CompileToCSharp(@"
@using Models;

<Grid TItem=""WeatherForecast"" Items=""@(Array.Empty<WeatherForecast>())"">
    <ColumnsTemplate>
        <Column Title=""Date"" FieldName=""Date"" Format=""d"" Width=""10rem"" />
    </ColumnsTemplate>
</Grid>
");

        // Assert
        AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
        AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
        CompileToAssembly(generated);
    }

    [IntegrationTestFact]
    public void CascadingGenericInference_Inferred_MultipleConstraints_GenericClassConstraints()
    {
        // Arrange
        AdditionalSyntaxTrees.Add(Parse(@"
using Microsoft.AspNetCore.Components;

namespace Test
{
    [CascadingTypeParameter(nameof(TItem))]
    public class Grid<TItem> : ComponentBase
    {
        [Parameter] public RenderFragment ColumnsTemplate { get; set; }
    }

    public abstract partial class BaseColumn<TItem> : ComponentBase where TItem : System.Collections.Generic.IEnumerable<TItem>
    {
        [CascadingParameter]
        internal Grid<TItem> Grid { get; set; }
    }

    public class Column<TItem> : BaseColumn<TItem> where TItem : System.Collections.Generic.IEnumerable<TItem>
    {
        [Parameter]
        public string FieldName { get; set; }
    }
}

namespace Models {
    using System;
    using System.Collections;
    using System.Collections.Generic;
    public class WeatherForecast : IEnumerable<WeatherForecast> {
        public IEnumerator<WeatherForecast> GetEnumerator()
        {
            throw new NotImplementedException();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            throw new NotImplementedException();
        }
    }
}"));

        // Act
        var generated = CompileToCSharp(@"
@using Models;
<Grid TItem=""WeatherForecast"" Items=""@(Array.Empty<WeatherForecast>())"">
    <ColumnsTemplate>
        <Column Title=""Date"" FieldName=""Date"" Format=""d"" Width=""10rem"" />
    </ColumnsTemplate>
</Grid>
");

        // Assert
        AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
        AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
        CompileToAssembly(generated);
    }

    [IntegrationTestFact]
    public void CascadingGenericInference_Partial_CreatesError()
    {
        // Arrange
        AdditionalSyntaxTrees.Add(Parse(@"
using Microsoft.AspNetCore.Components;

namespace Test
{
    [CascadingTypeParameter(nameof(TItem))]
    public class Grid<TItem> : ComponentBase
    {
        [Parameter] public System.Collections.Generic.IEnumerable<TItem> Items { get; set; }
        [Parameter] public RenderFragment ChildContent { get; set; }
    }

    public class Column<TItem, TChildOther> : ComponentBase
    {
    }
}
"));

        // Act
        var generated = CompileToCSharp(@"
<Grid Items=""@(Array.Empty<DateTime>())""><Column TChildOther=""long"" /></Grid>");

        // Assert
        AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
        AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);

        var diagnostic = Assert.Single(generated.RazorDiagnostics);
        Assert.Same(ComponentDiagnosticFactory.GenericComponentMissingTypeArgument.Id, diagnostic.Id);
    }

    [IntegrationTestFact]
    public void CascadingGenericInference_WithSplatAndKey()
    {
        // This is an integration test to show that our type inference code doesn't
        // have bad interactions with some of the other more complicated transformations

        // Arrange
        AdditionalSyntaxTrees.Add(Parse(@"
using System.Collections.Generic;
using Microsoft.AspNetCore.Components;

namespace Test
{
    [CascadingTypeParameter(nameof(TItem))]
    public class Grid<TItem> : ComponentBase
    {
        [Parameter] public System.Collections.Generic.IEnumerable<TItem> Items { get; set; }
        [Parameter] public RenderFragment ChildContent { get; set; }
    }

    public class Column<TItem> : ComponentBase
    {
        [Parameter(CaptureUnmatchedValues = true)] public IDictionary<string, object> OtherAttributes { get; set; }
    }
}
"));

        // Act
        var generated = CompileToCSharp(@"
@{ var parentKey = new object(); var childKey = new object(); }
<Grid @key=""@parentKey"" Items=""@(Array.Empty<DateTime>())"">
    <Column @key=""@childKey"" Title=""Hello"" Another=""@DateTime.MinValue"" />
</Grid>");

        // Assert
        AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
        AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
        CompileToAssembly(generated);
    }

    [IntegrationTestFact]
    public void CascadingGenericInference_Multilayer()
    {
        // Arrange
        AdditionalSyntaxTrees.Add(Parse(@"
using Microsoft.AspNetCore.Components;

namespace Test
{
    [CascadingTypeParameter(nameof(TItem))]
    public class Ancestor<TItem> : ComponentBase
    {
        [Parameter] public System.Collections.Generic.IEnumerable<TItem> Items { get; set; }
        [Parameter] public RenderFragment ChildContent { get; set; }
    }

    public class Passthrough : ComponentBase
    {
        [Parameter] public RenderFragment ChildContent { get; set; }
    }

    public class Child<TItem> : ComponentBase
    {
    }
}
"));

        // Act
        var generated = CompileToCSharp(@"
<Ancestor Items=""@(Array.Empty<DateTime>())""><Passthrough><Child /></Passthrough></Ancestor>");

        // Assert
        AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
        AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
        CompileToAssembly(generated);
    }

    [IntegrationTestFact]
    public void CascadingGenericInference_Override_Multilayer()
    {
        // Arrange
        AdditionalSyntaxTrees.Add(Parse(@"
using Microsoft.AspNetCore.Components;

namespace Test
{
    [CascadingTypeParameter(nameof(TItem))]
    public class TreeNode<TItem> : ComponentBase
    {
        [Parameter] public RenderFragment ChildContent { get; set; }
        [Parameter] public TItem Item { get; set; }
    }
}
"));

        // Act
        var generated = CompileToCSharp(@"
<TreeNode Item=""@DateTime.Now"">
    <TreeNode Item=""@System.Threading.Thread.CurrentThread"">
        <TreeNode>
            <TreeNode />
        </TreeNode>
    </TreeNode>
    <TreeNode />
</TreeNode>");

        // Assert
        AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
        AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
        CompileToAssembly(generated);
    }

    [IntegrationTestFact]
    public void CascadingGenericInference_Override()
    {
        // This test is to show that, even if an ancestor is trying to cascade its generic types,
        // a descendant can still override that through inference

        // Arrange
        AdditionalSyntaxTrees.Add(Parse(@"
using Microsoft.AspNetCore.Components;

namespace Test
{
    [CascadingTypeParameter(nameof(TItem))]
    public class Grid<TItem> : ComponentBase
    {
        [Parameter] public System.Collections.Generic.IEnumerable<TItem> Items { get; set; }
        [Parameter] public RenderFragment ChildContent { get; set; }
    }

    public class Column<TItem> : ComponentBase
    {
        [Parameter] public TItem OverrideParam { get; set; }
    }
}
"));

        // Act
        var generated = CompileToCSharp(@"
<Grid Items=""@(Array.Empty<DateTime>())""><Column OverrideParam=""@(""Some string"")"" /></Grid>");

        // Assert
        AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
        AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
        CompileToAssembly(generated);
    }

    [IntegrationTestFact]
    public void CascadingGenericInference_NotCascaded_CreatesError()
    {
        // Arrange
        AdditionalSyntaxTrees.Add(Parse(@"
using Microsoft.AspNetCore.Components;

namespace Test
{
    public class Grid<TItem> : ComponentBase
    {
        [Parameter] public System.Collections.Generic.IEnumerable<TItem> Items { get; set; }
        [Parameter] public RenderFragment ChildContent { get; set; }
    }

    public class Column<TItem> : ComponentBase
    {
    }
}
"));

        // Act
        var generated = CompileToCSharp(@"
<Grid Items=""@(Array.Empty<DateTime>())""><Column /></Grid>");

        // Assert
        AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
        AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);

        var diagnostic = Assert.Single(generated.RazorDiagnostics);
        Assert.Same(ComponentDiagnosticFactory.GenericComponentTypeInferenceUnderspecified.Id, diagnostic.Id);
    }

    [IntegrationTestFact]
    public void CascadingGenericInference_GenericChildContent()
    {
        // Arrange
        AdditionalSyntaxTrees.Add(Parse(@"
using Microsoft.AspNetCore.Components;

namespace Test
{
    [CascadingTypeParameter(nameof(TItem))]
    public class Grid<TItem> : ComponentBase
    {
        [Parameter] public System.Collections.Generic.IEnumerable<TItem> Items { get; set; }
        [Parameter] public RenderFragment ChildContent { get; set; }
    }

    public class Column<TItem> : ComponentBase
    {
        [Parameter] public RenderFragment<TItem> ChildContent { get; set; }
    }
}
"));

        // Act
        var generated = CompileToCSharp(@"
<Grid Items=""@(Array.Empty<DateTime>())""><Column>@context.Year</Column></Grid>");

        // Assert
        AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
        AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
        CompileToAssembly(generated);
    }

    [IntegrationTestFact]
    public void CascadingGenericInference_GenericLambda()
    {
        // Arrange
        AdditionalSyntaxTrees.Add(Parse(@"
using Microsoft.AspNetCore.Components;

namespace Test
{
    [CascadingTypeParameter(nameof(TItem))]
    public class Grid<TItem> : ComponentBase
    {
        [Parameter] public System.Collections.Generic.IEnumerable<TItem> Items { get; set; }
        [Parameter] public RenderFragment ChildContent { get; set; }
    }

    public class Column<TItem, TOutput> : ComponentBase
    {
        [Parameter] public System.Func<TItem, TOutput> SomeLambda { get; set; }
    }
}
"));

        // Act
        var generated = CompileToCSharp(@"
<Grid Items=""@(Array.Empty<DateTime>())""><Column SomeLambda=""@(x => x.Year)"" /></Grid>");

        // Assert
        AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
        AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
        CompileToAssembly(generated);
    }

    [IntegrationTestFact]
    public void CascadingGenericInference_MultipleTypes()
    {

        // Arrange
        AdditionalSyntaxTrees.Add(Parse(@"
using System.Collections.Generic;
using Microsoft.AspNetCore.Components;

namespace Test
{
    [CascadingTypeParameter(nameof(TKey))]
    [CascadingTypeParameter(nameof(TValue))]
    [CascadingTypeParameter(nameof(TOther))]
    public class Parent<TKey, TValue, TOther> : ComponentBase
    {
        [Parameter] public Dictionary<TKey, TValue> Data { get; set; }
        [Parameter] public TOther Other { get; set; }
        [Parameter] public RenderFragment ChildContent { get; set; }
    }

    public class Child<TOther, TValue, TKey, TChildOnly> : ComponentBase
    {
        [Parameter] public ICollection<TChildOnly> ChildOnlyItems { get; set; }
    }
}
"));

        // Act
        var generated = CompileToCSharp(@"
<Parent Data=""@(new System.Collections.Generic.Dictionary<int, string>())"" Other=""@DateTime.MinValue"">
    <Child ChildOnlyItems=""@(new[] { 'a', 'b', 'c' })"" />
</Parent>");

        // Assert
        AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
        AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
        CompileToAssembly(generated);
    }

    [IntegrationTestFact]
    public void CascadingGenericInference_WithUnrelatedType_CreatesError()
    {
        // It would succeed if you changed this to Column<TItem, TUnrelated>, or if the Grid took a parameter
        // whose type included TItem and not TUnrelated. It just doesn't work if the only inference parameters
        // also include unrelated generic types, because the inference methods we generate don't know what
        // to do with extra type parameters. It would be nice just to ignore them, but at the very least we
        // have to rewrite their names to avoid clashes and figure out whether multiple unrelated generic
        // types with the same name should be rewritten to the same name or unique names.

        // Arrange
        AdditionalSyntaxTrees.Add(Parse(@"
using Microsoft.AspNetCore.Components;

namespace Test
{
    [CascadingTypeParameter(nameof(TItem))]
    [CascadingTypeParameter(nameof(TUnrelated))]
    public class Grid<TItem, TUnrelated> : ComponentBase
    {
        [Parameter] public System.Collections.Generic.Dictionary<TItem, TUnrelated> Items { get; set; }
        [Parameter] public RenderFragment ChildContent { get; set; }
    }

    public class Column<TItem> : ComponentBase
    {
    }
}
"));

        // Act
        var generated = CompileToCSharp(@"
<Grid Items=""@(new Dictionary<int, string>())""><Column /></Grid>");

        // Assert
        AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
        AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);

        var diagnostic = Assert.Single(generated.RazorDiagnostics);
        Assert.Same(ComponentDiagnosticFactory.GenericComponentTypeInferenceUnderspecified.Id, diagnostic.Id);
    }

    [IntegrationTestFact, WorkItem("https://github.com/dotnet/razor/issues/9631")]
    public void CascadingGenericInference_GenericArgumentNested()
    {
        // Arrange
        AdditionalSyntaxTrees.Add(Parse("""
            using Microsoft.AspNetCore.Components;
            using System;
            using System.Collections.Generic;

            namespace Test;

            [CascadingTypeParameter(nameof(T))]
            public class Grid<T> : ComponentBase
            {
                [Parameter] public Func<List<T>>? Data { get; set; }
            }

            public partial class GridColumn<T> : ComponentBase
            {
            }
            """));

        // Act
        var generated = CompileToCSharp("""
            <Grid Data="@(() => new List<string>())">
                <GridColumn />
            </Grid>
            """, nullableEnable: true);

        // Assert
        AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
        AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
        CompileToAssembly(generated);
    }

    [IntegrationTestFact, WorkItem("https://github.com/dotnet/razor/issues/9631")]
    public void CascadingGenericInference_GenericArgumentNested_Dictionary()
    {
        // Arrange
        AdditionalSyntaxTrees.Add(Parse("""
            using Microsoft.AspNetCore.Components;
            using System;
            using System.Collections.Generic;

            namespace Test
            {
                [CascadingTypeParameter(nameof(T))]
                public class Grid<T> : ComponentBase
                {
                    [Parameter] public Func<Dictionary<X, T>>? Data { get; set; }
                }

                public partial class GridColumn<T> : ComponentBase
                {
                }
            }

            public class X { }
            """));

        // Act
        var generated = CompileToCSharp("""
            <Grid Data="@(() => new Dictionary<X, string>())">
                <GridColumn />
            </Grid>
            """, nullableEnable: true);

        // Assert
        AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
        AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
        CompileToAssembly(generated);
    }

    [IntegrationTestFact, WorkItem("https://github.com/dotnet/razor/issues/9631")]
    public void CascadingGenericInference_GenericArgumentNested_Dictionary_02()
    {
        // Arrange
        AdditionalSyntaxTrees.Add(Parse("""
            using Microsoft.AspNetCore.Components;
            using System;
            using System.Collections.Generic;

            namespace Test;

            [CascadingTypeParameter(nameof(T))]
            public class Grid<T> : ComponentBase
            {
                [Parameter] public Func<Dictionary<X, T>>? Data { get; set; }
            }

            public partial class GridColumn<T> : ComponentBase
            {
            }

            public class X { }
            """));

        // Act
        var generated = CompileToCSharp("""
            <Grid Data="@(() => new Dictionary<X, string>())">
                <GridColumn />
            </Grid>
            """, nullableEnable: true);

        // Assert
        AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
        AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
        CompileToAssembly(generated);
    }

    [IntegrationTestFact, WorkItem("https://github.com/dotnet/razor/issues/9631")]
    public void CascadingGenericInference_GenericArgumentNested_Dictionary_03()
    {
        // Arrange
        AdditionalSyntaxTrees.Add(Parse("""
            using Microsoft.AspNetCore.Components;
            using System;
            using System.Collections.Generic;

            namespace Test
            {
                [CascadingTypeParameter(nameof(T))]
                public class Grid<T> : ComponentBase
                {
                    [Parameter] public Dictionary<X, T>? Data { get; set; }
                }

                public partial class GridColumn<T> : ComponentBase
                {
                }
            }

            public class X { }
            """));

        // Act
        var generated = CompileToCSharp("""
            <Grid Data="@(new Dictionary<X, string>())">
                <GridColumn />
            </Grid>
            """, nullableEnable: true);

        // Assert
        AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
        AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
        CompileToAssembly(generated);
    }

    [IntegrationTestFact, WorkItem("https://github.com/dotnet/razor/issues/9631")]
    public void CascadingGenericInference_GenericArgumentNested_Dictionary_Dynamic()
    {
        // Arrange
        AdditionalSyntaxTrees.Add(Parse("""
            using Microsoft.AspNetCore.Components;
            using System;
            using System.Collections.Generic;

            namespace Test
            {
                [CascadingTypeParameter(nameof(T))]
                public class Grid<T> : ComponentBase
                {
                    [Parameter] public Dictionary<dynamic, T>? Data { get; set; }
                }

                public partial class GridColumn<T> : ComponentBase
                {
                }
            }
            """));

        // Act
        var generated = CompileToCSharp("""
            <Grid Data="@(new Dictionary<dynamic, string>())">
                <GridColumn />
            </Grid>
            """, nullableEnable: true);

        // Assert
        AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
        AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
        CompileToAssembly(generated);
    }

    [IntegrationTestFact]
    public void CascadingGenericInference_CombiningMultipleAncestors()
    {

        // Arrange
        AdditionalSyntaxTrees.Add(Parse(@"
using Microsoft.AspNetCore.Components;

namespace Test
{
    [CascadingTypeParameter(nameof(TOne))]
    public class ParentOne<TOne> : ComponentBase
    {
        [Parameter] public TOne Value { get; set; }
        [Parameter] public RenderFragment ChildContent { get; set; }
    }

    [CascadingTypeParameter(nameof(TTwo))]
    public class ParentTwo<TTwo> : ComponentBase
    {
        [Parameter] public TTwo Value { get; set; }
        [Parameter] public RenderFragment ChildContent { get; set; }
    }

    public class Child<TOne, TTwo> : ComponentBase
    {
    }
}
"));

        // Act
        var generated = CompileToCSharp(@"
<ParentOne Value=""@int.MaxValue"">
    <ParentTwo Value=""@(""Hello"")"">
        <Child />
    </ParentTwo>
</ParentOne>");

        // Assert
        AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
        AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
        CompileToAssembly(generated);
    }

    [IntegrationTestFact, WorkItem("https://github.com/dotnet/razor/issues/7103")]
    public void CascadingGenericInference_ParameterInNamespace()
    {
        // Arrange
        AdditionalSyntaxTrees.Add(Parse("""
            using Microsoft.AspNetCore.Components;

            namespace MyApp
            {
                public class MyClass<T> { }
            }

            namespace MyApp.Components
            {
                [CascadingTypeParameter(nameof(T))]
                public class ParentComponent<T> : ComponentBase
                {
                    [Parameter] public MyApp.MyClass<T> Parameter { get; set; } = null!;
                }

                public class ChildComponent<T> : ComponentBase { }
            }
            """));

        // Act
        var generated = CompileToCSharp("""
            @namespace MyApp.Components

            <ParentComponent Parameter="new MyClass<string>()">
                <ChildComponent />
            </ParentComponent>
            """);

        // Assert
        AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
        AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
        CompileToAssembly(generated);
    }

    [IntegrationTestFact]
    public void CascadingGenericInference_Tuple()
    {
        // Arrange
        AdditionalSyntaxTrees.Add(Parse("""
            using Microsoft.AspNetCore.Components;

            namespace Test
            {
                [CascadingTypeParameter(nameof(T))]
                public class ParentComponent<T> : ComponentBase
                {
                    [Parameter] public (T, T) Parameter { get; set; }
                }

                public class ChildComponent<T> : ComponentBase { }
            }
            """));

        // Act
        var generated = CompileToCSharp("""
            <ParentComponent Parameter="(1, 2)">
                <ChildComponent />
            </ParentComponent>
            """);

        // Assert
        AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
        AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
        CompileToAssembly(generated);
    }

    [IntegrationTestFact, WorkItem("https://github.com/dotnet/razor/issues/7428")]
    public void CascadingGenericInference_NullableEnabled()
    {
        // Arrange
        AdditionalSyntaxTrees.Add(Parse("""
            using Microsoft.AspNetCore.Components;

            namespace Test;

            [CascadingTypeParameter(nameof(TRow))]
            public class Parent<TRow>: ComponentBase
            {
                [Parameter]
                public RenderFragment<TRow>? ChildContent { get; set; }
            }

            public class Child<TRow> : ComponentBase
            {
                [Parameter]
                public RenderFragment<TRow>? ChildContent { get; set; }
            }
            """));

        // Act
        var generated = CompileToCSharp("""
            <Parent TRow="string">
                <Child Context="childContext">@childContext.Length</Child>
            </Parent>
            """, nullableEnable: true);

        // Assert
        CompileToAssembly(generated);
    }

    [IntegrationTestFact]
    public void ChildComponent_GenericWeaklyTypedAttribute()
    {
        // Arrange
        AdditionalSyntaxTrees.Add(Parse(@"
using Microsoft.AspNetCore.Components;

namespace Test
{
    public class MyComponent<TItem> : ComponentBase
    {
        [Parameter] public TItem Item { get; set; }
    }
}
"));

        // Act
        var generated = CompileToCSharp(@"
<MyComponent TItem=string Item=""@(""hi"")"" Other=""@(17)""/>");

        // Assert
        AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
        AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
        CompileToAssembly(generated);
    }

    [IntegrationTestFact]
    public void ChildComponent_GenericWeaklyTypedAttribute_TypeInference()
    {
        // Arrange
        AdditionalSyntaxTrees.Add(Parse(@"
using Microsoft.AspNetCore.Components;

namespace Test
{
    public class MyComponent<TItem> : ComponentBase
    {
        [Parameter] public TItem Item { get; set; }
    }
}
"));

        // Act
        var generated = CompileToCSharp(@"
<MyComponent Item=""@(""hi"")"" Other=""@(17)""/>");

        // Assert
        AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
        AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
        CompileToAssembly(generated);
    }

    [IntegrationTestFact]
    public void ChildComponent_GenericBind()
    {
        // Arrange
        AdditionalSyntaxTrees.Add(Parse(@"
using System;
using Microsoft.AspNetCore.Components;

namespace Test
{
    public class MyComponent<TItem> : ComponentBase
    {
        [Parameter]
        public TItem Item { get; set; }

        [Parameter]
        public Action<TItem> ItemChanged { get; set; }
    }
}
"));

        // Act
        var generated = CompileToCSharp(@"
<MyComponent TItem=string @bind-Item=Value/>
@code {
    string Value;
}");

        // Assert
        AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
        AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
        CompileToAssembly(generated);
    }

    [IntegrationTestFact]
    public void ChildComponent_GenericBind_TypeInference()
    {
        // Arrange
        AdditionalSyntaxTrees.Add(Parse(@"
using System;
using Microsoft.AspNetCore.Components;

namespace Test
{
    public class MyComponent<TItem> : ComponentBase
    {
        [Parameter]
        public TItem Item { get; set; }

        [Parameter]
        public Action<TItem> ItemChanged { get; set; }
    }
}
"));

        // Act
        var generated = CompileToCSharp(@"
<MyComponent @bind-Item=Value/>
<MyComponent @bind-Item=Value/>
@code {
    string Value;
}");

        // Assert
        AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
        AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
        CompileToAssembly(generated);
    }

    [IntegrationTestFact]
    public void ChildComponent_GenericBindWeaklyTyped()
    {
        // Arrange
        AdditionalSyntaxTrees.Add(Parse(@"
using System;
using Microsoft.AspNetCore.Components;

namespace Test
{
    public class MyComponent<TItem> : ComponentBase
    {
    }
}
"));

        // Act
        var generated = CompileToCSharp(@"
<MyComponent TItem=string @bind-Item=Value/>
@code {
    string Value;
}");

        // Assert
        AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
        AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
        CompileToAssembly(generated);
    }

    [IntegrationTestFact]
    public void ChildComponent_GenericBindWeaklyTyped_TypeInference()
    {
        // Arrange
        AdditionalSyntaxTrees.Add(Parse(@"
using System;
using Microsoft.AspNetCore.Components;

namespace Test
{
    public class MyComponent<TItem> : ComponentBase
    {
        [Parameter] public TItem Value { get; set; }
    }
}
"));

        // Act
        var generated = CompileToCSharp(@"
<MyComponent @bind-Item=Value Value=@(18)/>
@code {
    string Value;
}");

        // Assert
        AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
        AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
        CompileToAssembly(generated);
    }

    [IntegrationTestFact]
    public void ChildComponent_GenericChildContent()
    {
        // Arrange
        AdditionalSyntaxTrees.Add(Parse(@"
using Microsoft.AspNetCore.Components;

namespace Test
{
    public class MyComponent<TItem> : ComponentBase
    {
        [Parameter] public TItem Item { get; set; }

        [Parameter] public RenderFragment<TItem> ChildContent { get; set; }
    }
}
"));

        // Act
        var generated = CompileToCSharp(@"
<MyComponent TItem=string Item=""@(""hi"")"">
  <div>@context.ToLower()</div>
</MyComponent>");

        // Assert
        AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
        AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
        CompileToAssembly(generated);
    }

    [IntegrationTestFact]
    public void ChildComponent_GenericChildContent_TypeInference()
    {
        // Arrange
        AdditionalSyntaxTrees.Add(Parse(@"
using Microsoft.AspNetCore.Components;

namespace Test
{
    public class MyComponent<TItem> : ComponentBase
    {
        [Parameter] public TItem Item { get; set; }

        [Parameter] public RenderFragment<TItem> ChildContent { get; set; }
    }
}
"));

        // Act
        var generated = CompileToCSharp(@"
<MyComponent Item=""@(""hi"")"">
  <div>@context.ToLower()</div>
</MyComponent>");

        // Assert
        AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
        AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
        CompileToAssembly(generated);
    }

    [IntegrationTestFact]
    public void ChildComponent_NonGenericParameterizedChildContent_TypeInference()
    {
        // Arrange
        AdditionalSyntaxTrees.Add(Parse(@"
using Microsoft.AspNetCore.Components;

namespace Test
{
    public class MyComponent<TItem> : ComponentBase
    {
        [Parameter] public TItem Item { get; set; }

        [Parameter] public RenderFragment<TItem> GenericFragment { get; set; }

        [Parameter] public RenderFragment<int> IntFragment { get; set; }
    }
}
"));

        // Act
        var generated = CompileToCSharp(@"
<MyComponent Item=""@(""hi"")"">
  <GenericFragment>@context.ToLower()</GenericFragment>
  <IntFragment>@context</IntFragment>
</MyComponent>");

        // Assert
        AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
        AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
        CompileToAssembly(generated);
    }

    [IntegrationTestFact]
    public void GenericComponent_WithFullyQualifiedTagName()
    {
        // Arrange
        AdditionalSyntaxTrees.Add(Parse(@"
using Microsoft.AspNetCore.Components;

namespace Test
{
    public class MyComponent<TItem> : ComponentBase
    {
        [Parameter] public TItem Item { get; set; }

        [Parameter] public RenderFragment<TItem> ChildContent { get; set; }
    }
}
"));

        // Act
        var generated = CompileToCSharp(@"
<Test.MyComponent Item=""@(""hi"")"">
  <div>@context.ToLower()</div>
</Test.MyComponent>");

        // Assert
        AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
        AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
        CompileToAssembly(generated);
    }

    [IntegrationTestFact]
    public void ChildComponent_MultipleGenerics()
    {
        // Arrange
        AdditionalSyntaxTrees.Add(Parse(@"
using Microsoft.AspNetCore.Components;

namespace Test
{
    public class MyComponent<TItem1, TItem2> : ComponentBase
    {
        [Parameter] public TItem1 Item { get; set; }

        [Parameter] public RenderFragment<TItem1> ChildContent { get; set; }

        [Parameter] public RenderFragment<Context> AnotherChildContent { get; set; }

        public class Context
        {
            public TItem2 Item { get; set; }
        }
    }
}
"));

        // Act
        var generated = CompileToCSharp(@"
<MyComponent TItem1=string TItem2=int Item=""@(""hi"")"">
  <ChildContent><div>@context.ToLower()</div></ChildContent>
<AnotherChildContent Context=""item"">
  @System.Math.Max(0, item.Item);
</AnotherChildContent>
</MyComponent>");

        // Assert
        AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
        AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
        CompileToAssembly(generated);
    }

    [IntegrationTestFact]
    public void ChildComponent_MultipleGenerics_TypeInference()
    {
        // Arrange
        AdditionalSyntaxTrees.Add(Parse(@"
using System.Collections.Generic;
using Microsoft.AspNetCore.Components;

namespace Test
{
    public class MyComponent<TItem1, TItem2> : ComponentBase
    {
        [Parameter] public TItem1 Item { get; set; }

        [Parameter] public List<TItem2> Items { get; set; }

        [Parameter] public RenderFragment<TItem1> ChildContent { get; set; }

        [Parameter] public RenderFragment<Context> AnotherChildContent { get; set; }

        public class Context
        {
            public TItem2 Item { get; set; }
        }
    }
}
"));

        // Act
        var generated = CompileToCSharp(@"
<MyComponent Item=""@(""hi"")"" Items=@(new List<long>())>
  <ChildContent><div>@context.ToLower()</div></ChildContent>
<AnotherChildContent Context=""item"">
  @System.Math.Max(0, item.Item);
</AnotherChildContent>
</MyComponent>");

        // Assert
        AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
        AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
        CompileToAssembly(generated);
    }

    [IntegrationTestFact]
    public void NonGenericComponent_WithGenericEventHandler()
    {
        // Arrange
        AdditionalSyntaxTrees.Add(Parse(@"
using Microsoft.AspNetCore.Components;

namespace Test
{
    public class MyEventArgs { }

    public class MyComponent : ComponentBase
    {
        [Parameter] public string Item { get; set; }
        [Parameter] public EventCallback<MyEventArgs> Event { get; set; }
    }
}
"));

        // Act
        var generated = CompileToCSharp(@"
<MyComponent Item=""Hello"" MyEvent=""MyEventHandler"" />

@code {
    public void MyEventHandler() {}
}
");

        // Assert
        AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
        AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
        CompileToAssembly(generated);
    }

    [IntegrationTestFact]
    public void GenericComponent_WithKey()
    {
        // Arrange
        AdditionalSyntaxTrees.Add(Parse(@"
using Microsoft.AspNetCore.Components;

namespace Test
{
    public class MyComponent<TItem> : ComponentBase
    {
        [Parameter] public TItem Item { get; set; }
    }
}
"));

        // Act
        var generated = CompileToCSharp(@"
<MyComponent TItem=int Item=""3"" @key=""_someKey"" />

@code {
    private object _someKey = new object();
}
");

        // Assert
        AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
        AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
        CompileToAssembly(generated);
    }

    [IntegrationTestFact]
    public void GenericComponent_WithKey_TypeInference()
    {
        // Arrange
        AdditionalSyntaxTrees.Add(Parse(@"
using Microsoft.AspNetCore.Components;

namespace Test
{
    public class MyComponent<TItem> : ComponentBase
    {
        [Parameter] public TItem Item { get; set; }
    }
}
"));

        // Act
        var generated = CompileToCSharp(@"
<MyComponent Item=""3"" @key=""_someKey"" />

@code {
    private object _someKey = new object();
}
");

        // Assert
        AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
        AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
        CompileToAssembly(generated);
    }

    [IntegrationTestFact]
    public void GenericComponent_WithComponentRef_CreatesDiagnostic()
    {
        // Arrange
        AdditionalSyntaxTrees.Add(Parse(@"
using Microsoft.AspNetCore.Components;

namespace Test
{
    public class MyComponent<TItem> : ComponentBase
    {
        [Parameter] public TItem Item { get; set; }
    }
}
"));

        // Act
        var generated = CompileToCSharp(@"
<MyComponent TItem=int Item=""3"" @ref=""_my"" />

@code {
    private MyComponent<int> _my;
    public void Foo() { System.GC.KeepAlive(_my); }
}
");

        // Assert
        AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
        AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
        CompileToAssembly(generated);
    }

    [IntegrationTestFact]
    public void GenericComponent_WithComponentRef_TypeInference_CreatesDiagnostic()
    {
        // Arrange
        AdditionalSyntaxTrees.Add(Parse(@"
using Microsoft.AspNetCore.Components;

namespace Test
{
    public class MyComponent<TItem> : ComponentBase
    {
        [Parameter] public TItem Item { get; set; }
    }
}
"));

        // Act
        var generated = CompileToCSharp(@"
<MyComponent Item=""3"" @ref=""_my"" />

@code {
    private MyComponent<int> _my;
    public void Foo() { System.GC.KeepAlive(_my); }
}
");

        // Assert
        AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
        AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
        CompileToAssembly(generated);
    }

    [IntegrationTestFact]
    public void GenericComponent_NonGenericParameter_TypeInference()
    {
        // Arrange
        AdditionalSyntaxTrees.Add(Parse(@"
using Microsoft.AspNetCore.Components;
using Test.Shared;

namespace Test
{
    public class MyComponent<TItem> : ComponentBase
    {
        [Parameter] public TItem Item { get; set; }
        [Parameter] public MyClass Foo { get; set; }
    }
}

namespace Test.Shared
{
    public class MyClass
    {
    }
}
"));

        // Act
        var generated = CompileToCSharp(@"
@using Test.Shared
<MyComponent Item=""3"" Foo=""@Hello"" />

@code {
    MyClass Hello = new MyClass();
}
");

        // Assert
        AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
        AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
        CompileToAssembly(generated);
    }

    [IntegrationTestFact]
    public void GenericComponent_NonGenericEventCallback_TypeInference()
    {
        // Arrange
        AdditionalSyntaxTrees.Add(Parse(@"
using Microsoft.AspNetCore.Components;

namespace Test
{
    public class MyEventArgs { }

    public class MyComponent<TItem> : ComponentBase
    {
        [Parameter] public TItem Item { get; set; }
        [Parameter] public EventCallback MyEvent { get; set; }
    }
}
"));
        // Act
        var generated = CompileToCSharp(@"
@using Test
<MyComponent Item=""3"" MyEvent=""x => {}"" />
");

        // Assert
        AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
        AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
        CompileToAssembly(generated);
    }

    [IntegrationTestFact]
    public void GenericComponent_GenericEventCallback_TypeInference()
    {
        // Arrange
        AdditionalSyntaxTrees.Add(Parse(@"
using Microsoft.AspNetCore.Components;

namespace Test
{
    public class MyEventArgs { }

    public class MyComponent<TItem> : ComponentBase
    {
        [Parameter] public TItem Item { get; set; }
        [Parameter] public EventCallback<MyEventArgs> MyEvent { get; set; }
    }
}
"));

        // Act
        var generated = CompileToCSharp(@"
@using Test
<MyComponent Item=""3"" MyEvent=""x => {}"" />
");

        // Assert
        AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
        AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
        CompileToAssembly(generated);
    }

    [IntegrationTestFact]
    public void GenericComponent_NestedGenericEventCallback_TypeInference()
    {
        // Arrange
        AdditionalSyntaxTrees.Add(Parse(@"
using System.Collections.Generic;
using Microsoft.AspNetCore.Components;

namespace Test
{
    public class MyEventArgs { }

    public class MyComponent<TItem> : ComponentBase
    {
        [Parameter] public TItem Item { get; set; }
        [Parameter] public EventCallback<List<Dictionary<string, MyEventArgs[]>>> MyEvent { get; set; }
    }
}
"));

        // Act
        var generated = CompileToCSharp(@"
@using Test
<MyComponent Item=""3"" MyEvent=""x => {}"" />
");

        // Assert
        AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
        AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
        CompileToAssembly(generated);
    }

    [IntegrationTestFact]
    public void GenericComponent_GenericEventCallbackWithGenericTypeParameter_TypeInference()
    {
        // Arrange
        AdditionalSyntaxTrees.Add(Parse(@"
using Microsoft.AspNetCore.Components;

namespace Test
{
    public class MyEventArgs { }

    public class MyComponent<TItem> : ComponentBase
    {
        [Parameter] public TItem Item { get; set; }
        [Parameter] public EventCallback<TItem> MyEvent { get; set; }
    }
}
"));

        // Act
        var generated = CompileToCSharp(@"
@using Test
<MyComponent Item=""3"" MyEvent=""(int x) => {}"" />
");

        // Assert
        AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
        AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
        CompileToAssembly(generated);
    }

    [IntegrationTestFact]
    public void GenericComponent_GenericEventCallbackWithGenericTypeParameter_NestedTypeExplicit()
    {
        // Arrange
        AdditionalSyntaxTrees.Add(Parse(@"
using Microsoft.AspNetCore.Components;

namespace Test
{
    public class MyEventArgs { }

    public class MyComponent<TItem> : ComponentBase
    {
        [Parameter] public TItem Item { get; set; }
        [Parameter] public EventCallback<TItem> MyEvent { get; set; }
    }
}
"));

        // Act
        var generated = CompileToCSharp(@"
@typeparam TChild
@using Test
<MyComponent TItem=""TChild"" MyEvent=""(TChild x) => {}"" />
");

        // Assert
        AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
        AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
        CompileToAssembly(generated);
    }

    [IntegrationTestFact]
    public void GenericComponent_GenericEventCallbackWithGenericTypeParameter_NestedTypeInference()
    {
        // Arrange
        AdditionalSyntaxTrees.Add(Parse(@"
using Microsoft.AspNetCore.Components;

namespace Test
{
    public class MyEventArgs { }

    public class MyComponent<TItem> : ComponentBase
    {
        [Parameter] public TItem Item { get; set; }
        [Parameter] public EventCallback<TItem> MyEvent { get; set; }
    }
}
"));

        // Act
        var generated = CompileToCSharp(@"
@typeparam TChild
@using Test
<MyComponent Item=""ChildItem"" MyEvent=""MyChildEvent"" />
@code
{
        [Parameter] public TChild ChildItem { get; set; }
        [Parameter] public EventCallback<TChild> MyChildEvent { get; set; }
}
");

        // Assert
        AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
        AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
        CompileToAssembly(generated);
    }

    [IntegrationTestFact]
    public void GenericComponent_GenericEventCallbackWithNestedGenericTypeParameter_TypeInference()
    {
        // Arrange
        AdditionalSyntaxTrees.Add(Parse(@"
using Microsoft.AspNetCore.Components;
using System.Collections.Generic;

namespace Test
{
    public class MyEventArgs { }

    public class MyComponent<TItem> : ComponentBase
    {
        [Parameter] public TItem Item { get; set; }
        [Parameter] public EventCallback<IEnumerable<TItem>> MyEvent { get; set; }
    }
}
"));

        // Act
        var generated = CompileToCSharp(@"
@using Test
@using System.Collections.Generic
<MyComponent Item=""3"" MyEvent=""(IEnumerable<int> x) => {}"" />
");

        // Assert
        AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
        AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
        CompileToAssembly(generated);
    }

    [IntegrationTestTheory, WorkItem("https://github.com/dotnet/razor/issues/7074")]
    [InlineData("struct", null, "1")]
    [InlineData("class", null, "string.Empty")]
    [InlineData("notnull", null, "1")]
    [InlineData("unmanaged", null, "1")]
    [InlineData(null, "new()", "1")]
    public void GenericComponent_ConstraintOrdering(string first, string last, string arg)
    {
        // Arrange
        if (first != null)
        {
            first = $"{first}, ";
        }
        if (last != null)
        {
            last = $", {last}";
        }
        AdditionalSyntaxTrees.Add(Parse($$"""
            using Microsoft.AspNetCore.Components;
            using System;
            namespace Test;
            public class MyComponent<T> : ComponentBase where T : {{first}}IComparable{{last}}
            {
                [Parameter] public T Parameter { get; set; }
            }
            """));

        // Act
        var generated = CompileToCSharp($"""
            @using Test
            <MyComponent Parameter="{arg}" />
            """);

        // Assert
        CompileToAssembly(generated);
    }

    [IntegrationTestFact]
    public void GenericComponent_UnmanagedConstraint()
    {
        // Arrange
        AdditionalSyntaxTrees.Add(Parse("""
            using Microsoft.AspNetCore.Components;
            namespace Test;
            public class MyComponent<T> : ComponentBase where T : unmanaged
            {
                [Parameter] public T Parameter { get; set; }
            }
            """));

        // Act
        var generated = CompileToCSharp("""
            @using Test
            <MyComponent Parameter="1" />
            """);

        // Assert
        AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
        AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
        CompileToAssembly(generated);
    }

    [IntegrationTestFact, WorkItem("https://github.com/dotnet/razor/issues/9592")]
    public void GenericComponent_TypeParameterOrdering()
    {
        // Arrange
        AdditionalSyntaxTrees.Add(Parse("""
            using Microsoft.AspNetCore.Components;

            namespace Test;

            public interface IInterfaceConstraint<T> { }

            public interface IComposedInterface : IInterfaceConstraint<string> { }

            public class MyComponent<TService, TKey> : ComponentBase
                where TService : IInterfaceConstraint<TKey>
            {
                [Parameter] public TKey? Value { get; set; }
                [Parameter] public EventCallback<TKey> ValueChanged { get; set; }
            }
            """));

        // Act
        var generated = CompileToCSharp("""
            <MyComponent TKey="string" TService="IComposedInterface" @bind-Value="_componentValue" />
            <MyComponent TService="IComposedInterface" TKey="string" @bind-Value="_componentValue" />

            @code {
                string _componentValue = string.Empty;
            }
            """,
            nullableEnable: true);

        // Assert
        AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
        AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
        CompileToAssembly(generated);
    }

    [IntegrationTestFact]
    public void GenericComponent_MissingTypeParameter_SystemInNamespace()
    {
        // Arrange
        AdditionalSyntaxTrees.Add(Parse("""
            using Microsoft.AspNetCore.Components;
            namespace Test;
            public class MyComponent<TItem> : ComponentBase;
            """));

        // Act
        var generated = CompileToCSharp("""
            @namespace Test.System
            <MyComponent />
            """);

        // Assert
        CompileToAssembly(generated);
        generated.RazorDiagnostics.Verify(
            // x:\dir\subdir\Test\TestComponent.cshtml(2,1): error RZ10001: The type of component 'MyComponent' cannot be inferred based on the values provided. Consider specifying the type arguments directly using the following attributes: 'TItem'.
            Diagnostic("RZ10001").WithLocation(2, 1));
    }

    [IntegrationTestFact, WorkItem("https://github.com/dotnet/razor/issues/10827")]
    public void GenericTypeCheck()
    {
        var generated = CompileToCSharp("""
            <TestComponent Data="null" />

            @code {
                private class System
                {
                    private class String
                    {
                    }
                }

                [Parameter]
                public List<global::System.String> Data { get; set; }
            }
            """);

        // Assert
        AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
        AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
        CompileToAssembly(generated);
    }

    [IntegrationTestFact, WorkItem("https://github.com/dotnet/razor/issues/11505")]
    public void CaptureParametersConstraint()
    {
        AdditionalSyntaxTrees.Add(Parse("""
            using Microsoft.AspNetCore.Components;

            namespace Test;

            public interface IMyInterface;

            public class MyClass<T> where T : IMyInterface;

            [CascadingTypeParameter(nameof(T))]
            public class MyComponent<T> : ComponentBase where T : IMyInterface
            {
                [Parameter] public MyClass<T> Param { get; set; }
            }
            """));
        var generated = CompileToCSharp("""
            <MyComponent Param="new MyClass<IMyInterface>()" />
            """);
        AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
        CompileToAssembly(generated);
    }

    [IntegrationTestFact, WorkItem("https://github.com/dotnet/razor/issues/11552")]
    public void GenericComponentTypeUsage()
    {
        // Act
        var generated = CompileToCSharp("""
            @typeparam TItem
            @code {
                [Parameter]
                public TItem MyItem { get; set; }
            }

            <TestComponent TItem="string" />
            """);

        // Assert
        AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
        AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
        CompileToAssembly(generated);
    }

    [IntegrationTestFact, WorkItem("https://github.com/dotnet/razor/issues/11552")]
    public void GenericComponentTypeUsageWithInference()
    {
        // Act
        var generated = CompileToCSharp("""
            @typeparam TItem
            @code {
                [Parameter]
                public TItem MyItem { get; set; }
            }

            <TestComponent MyItem="1" />
            """);

        // Assert
        AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
        AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
        CompileToAssembly(generated);
    }

    [IntegrationTestFact, WorkItem("https://github.com/dotnet/razor/issues/11552")]
    public void GenericComponentMultipleTypeParamUsage()
    {
        // Act
        var generated = CompileToCSharp("""
            @typeparam TItem
            @typeparam TItem2
            @code {
                [Parameter]
                public TItem MyItem { get; set; }

                [Parameter]
                public TItem2 MyItem2 { get; set; }
            }

            <TestComponent TItem2="int" TItem="string" />
            """);

        // Assert
        AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
        AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
        CompileToAssembly(generated);
    }

    [IntegrationTestFact, WorkItem("https://github.com/dotnet/razor/issues/11552")]
    public void GenericComponentTypeParamUsageWithImplicitExpression()
    {
        // Act
        var generated = CompileToCSharp("""
            @typeparam TItem
            @code {
                [Parameter]
                public TItem MyItem { get; set; }
            }

            <TestComponent TItem="@string" />
            """);

        // Assert
        AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
        AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
        CompileToAssembly(generated);
    }

    [IntegrationTestFact, WorkItem("https://github.com/dotnet/razor/issues/11552")]
    public void GenericComponentTypeParamUsageWithImplicitExpression2()
    {
        // Act
        var generated = CompileToCSharp("""
            @typeparam TItem
            @code {
                [Parameter]
                public TItem MyItem { get; set; }
            }

            <TestComponent TItem="@(string)" />
            """);

        // Assert
        AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
        AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
        CompileToAssembly(generated);
    }

    [IntegrationTestFact, WorkItem("https://github.com/dotnet/razor/issues/11552")]
    public void GenericComponentTypeUsageWhitespace()
    {
        // Act
        var generated = CompileToCSharp("""
            @typeparam TItem
            @code {
                [Parameter]
                public TItem MyItem { get; set; }
            }

            <TestComponent TItem="  string  " />
            """);

        // Assert
        AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
        AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
        CompileToAssembly(generated);
    }

    [IntegrationTestFact, WorkItem("https://github.com/dotnet/razor/issues/11552")]
    public void GenericComponentTypeUsageWithGenericType()
    {
        // Act
        var generated = CompileToCSharp("""
            @typeparam TItem
            @code {
                [Parameter]
                public TItem MyItem { get; set; }
            }

            <TestComponent TItem="TestComponent<string>" />
            """);

        // Assert
        AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
        AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
        CompileToAssembly(generated);
    }

    [IntegrationTestFact, WorkItem("https://github.com/dotnet/razor/issues/11718")]
    public void GenericInference_DynamicallyAccessedMembers_01()
    {
        var generated = CompileToCSharp("""
            @using Microsoft.AspNetCore.Components.Forms

            <InputRadioGroup @bind-Value="value1">
                <InputRadio Value="@("false")" />
                <InputRadio Value="@("true")" />
            </InputRadioGroup>

            @code {
                private string value1 = "true";
            }
            """);

        AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
        CompileToAssembly(generated);

        Assert.Contains("DynamicallyAccessedMembers", generated.Code);
    }

    [IntegrationTestFact, WorkItem("https://github.com/dotnet/razor/issues/11718")]
    public void GenericInference_DynamicallyAccessedMembers_02()
    {
        AdditionalSyntaxTrees.Add(Parse("""
            using Microsoft.AspNetCore.Components;
            using System;
            using System.Diagnostics.CodeAnalysis;
            using DAM = System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembersAttribute;
            using DAMT = System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes;
            namespace Test;
            public class MyComponent<T1,
                [Attr, DAM(DAMT.PublicMethods | DAMT.PublicFields)] T2,
                [DAM(DAMT.None)] [x: DAM(DAMT.All)] T3>
                : ComponentBase
            {
                [Parameter] public required T1 P1 { get; set; }
                [Parameter] public required T2 P2 { get; set; }
                [Parameter] public required T3 P3 { get; set; }
            }
            class Attr : Attribute;
            """));

        var expectedDiagnostics = new[]
        {
            // (9,23): warning CS0658: 'x' is not a recognized attribute location. Valid attribute locations for this declaration are 'typevar'. All attributes in this block will be ignored.
            //     [DAM(DAMT.None)] [x: DAM(DAMT.All)] T3>
            Diagnostic(ErrorCode.WRN_InvalidAttributeLocation, "x").WithArguments("x", "typevar").WithLocation(9, 23)
        };

        var generated = CompileToCSharp("""
            <MyComponent P1="s" P2="2" P3="s" />
            @code {
                private string s = "x";
            }
            """, expectedDiagnostics);

        AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
        CompileToAssembly(generated, expectedDiagnostics);

        Assert.Contains("DynamicallyAccessedMembers", generated.Code);
    }

    #endregion

    #region Key

    [IntegrationTestFact]
    public void Element_WithKey()
    {
        // Arrange/Act
        var generated = CompileToCSharp(@"
<elem attributebefore=""before"" @key=""someObject"" attributeafter=""after"">Hello</elem>

@code {
    private object someObject = new object();
}
");

        // Assert
        AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
        AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
        CompileToAssembly(generated);
    }

    [IntegrationTestFact]
    public void Element_WithKey_AndOtherAttributes()
    {
        // Arrange/Act
        var generated = CompileToCSharp(@"
<input type=""text"" data-slider-min=""@Min"" @key=""@someObject"" />

@code {
        private object someObject = new object();

        [Parameter] public int Min { get; set; }
    }
");

        // Assert
        AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
        AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
        CompileToAssembly(generated);
    }

    [IntegrationTestFact]
    public void Component_WithKey()
    {
        // Arrange
        AdditionalSyntaxTrees.Add(Parse(@"
using Microsoft.AspNetCore.Components;

namespace Test
{
    public class MyComponent : ComponentBase
    {
    }
}
"));

        // Arrange/Act
        var generated = CompileToCSharp(@"
<MyComponent ParamBefore=""before"" @key=""someDate.Day"" ParamAfter=""after"" />

@code {
    private DateTime someDate = DateTime.Now;
}
");

        // Assert
        AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
        AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
        CompileToAssembly(generated);
    }

    [IntegrationTestFact]
    public void Component_WithKey_WithChildContent()
    {
        // Arrange
        AdditionalSyntaxTrees.Add(Parse(@"
using Microsoft.AspNetCore.Components;

namespace Test
{
    public class MyComponent : ComponentBase
    {
    }
}
"));

        // Arrange/Act
        var generated = CompileToCSharp(@"
<MyComponent @key=""123 + 456"" SomeProp=""val"">
    Some <el>further</el> content
</MyComponent>
");

        // Assert
        AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
        AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
        CompileToAssembly(generated);
    }

    [IntegrationTestFact]
    public void Element_WithKey_AttributeNameIsCaseSensitive()
    {
        // Arrange/Act
        var generated = CompileToCSharp(@"
<elem attributebefore=""before"" @KEY=""someObject"" attributeafter=""after"">Hello</elem>

@code {
    private object someObject = new object();
}
");

        // Assert
        AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
        AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
        CompileToAssembly(generated);
    }

    #endregion

    #region Splat

    [IntegrationTestFact]
    public void Element_WithSplat()
    {
        // Arrange/Act
        var generated = CompileToCSharp(@"
<elem attributebefore=""before"" @attributes=""someAttributes"" attributeafter=""after"">Hello</elem>

@code {
    private Dictionary<string, object> someAttributes = new Dictionary<string, object>();
}
");

        // Assert
        AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
        AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
        CompileToAssembly(generated);
    }

    [IntegrationTestFact]
    public void Element_WithSplat_ImplicitExpression()
    {
        // Arrange/Act
        var generated = CompileToCSharp(@"
<elem attributebefore=""before"" @attributes=""@someAttributes"" attributeafter=""after"">Hello</elem>

@code {
    private Dictionary<string, object> someAttributes = new Dictionary<string, object>();
}
");

        // Assert
        AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
        AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
        CompileToAssembly(generated);
    }

    [IntegrationTestFact]
    public void Element_WithSplat_ExplicitExpression()
    {
        // Arrange/Act
        var generated = CompileToCSharp(@"
<elem attributebefore=""before"" @attributes=""@(someAttributes)"" attributeafter=""after"">Hello</elem>

@code {
    private Dictionary<string, object> someAttributes = new Dictionary<string, object>();
}
");

        // Assert
        AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
        AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
        CompileToAssembly(generated);
    }

    [IntegrationTestFact]
    public void Component_WithSplat()
    {
        // Arrange
        AdditionalSyntaxTrees.Add(Parse(@"
using Microsoft.AspNetCore.Components;

namespace Test
{
    public class MyComponent : ComponentBase
    {
    }
}
"));

        // Arrange/Act
        var generated = CompileToCSharp(@"
<MyComponent AttributeBefore=""before"" @attributes=""someAttributes"" AttributeAfter=""after"" />

@code {
    private Dictionary<string, object> someAttributes = new Dictionary<string, object>();
}
");

        // Assert
        AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
        AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
        CompileToAssembly(generated);
    }

    [IntegrationTestFact]
    public void Component_WithSplat_ImplicitExpression()
    {
        // Arrange
        AdditionalSyntaxTrees.Add(Parse(@"
using Microsoft.AspNetCore.Components;

namespace Test
{
    public class MyComponent : ComponentBase
    {
    }
}
"));

        // Arrange/Act
        var generated = CompileToCSharp(@"
<MyComponent AttributeBefore=""before"" @attributes=""@someAttributes"" AttributeAfter=""after"" />

@code {
    private Dictionary<string, object> someAttributes = new Dictionary<string, object>();
}
");

        // Assert
        AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
        AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
        CompileToAssembly(generated);
    }

    [IntegrationTestFact]
    public void Component_WithSplat_ExplicitExpression()
    {
        // Arrange
        AdditionalSyntaxTrees.Add(Parse(@"
using Microsoft.AspNetCore.Components;

namespace Test
{
    public class MyComponent : ComponentBase
    {
    }
}
"));

        // Arrange/Act
        var generated = CompileToCSharp(@"
<MyComponent AttributeBefore=""before"" @attributes=""@(someAttributes)"" AttributeAfter=""after"" />

@code {
    private Dictionary<string, object> someAttributes = new Dictionary<string, object>();
}
");

        // Assert
        AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
        AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
        CompileToAssembly(generated);
    }

    [IntegrationTestFact]
    public void Component_WithSplat_GenericTypeInference()
    {
        // Arrange
        AdditionalSyntaxTrees.Add(Parse(@"
using Microsoft.AspNetCore.Components;

namespace Test
{
    public class MyComponent<T> : ComponentBase
    {
        [Parameter] public T Value { get; set;}
    }
}
"));

        // Arrange/Act
        var generated = CompileToCSharp(@"
<MyComponent Value=""18"" @attributes=""@(someAttributes)"" />

@code {
    private Dictionary<string, object> someAttributes = new Dictionary<string, object>();
}
");

        // Assert
        AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
        AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
        CompileToAssembly(generated);
    }

    [IntegrationTestFact]
    public void Element_WithSplat_AttributeNameIsCaseSensitive()
    {
        // Arrange/Act
        var generated = CompileToCSharp(@"
<elem attributebefore=""before"" @ATTributes=""someAttributes"" attributeafter=""after"">Hello</elem>

@code {
    private Dictionary<string, object> someAttributes = new Dictionary<string, object>();
}
");

        // Assert
        AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
        AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
        CompileToAssembly(generated);
    }

    #endregion

    #region Ref

    [IntegrationTestFact]
    public void Element_WithRef()
    {
        // Arrange/Act
        var generated = CompileToCSharp(@"
<elem attributebefore=""before"" @ref=""myElem"" attributeafter=""after"">Hello</elem>

@code {
    private Microsoft.AspNetCore.Components.ElementReference myElem;
    public void Foo() { System.GC.KeepAlive(myElem); }
}
");

        // Assert
        AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
        AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
        CompileToAssembly(generated);
    }

    [IntegrationTestFact]
    public void Element_WithRef_AndOtherAttributes()
    {
        // Arrange/Act
        var generated = CompileToCSharp(@"
<input type=""text"" data-slider-min=""@Min"" @ref=""@_element"" />

@code {
        private ElementReference _element;

        [Parameter] public int Min { get; set; }
        public void Foo() { System.GC.KeepAlive(_element); }
    }
");

        // Assert
        AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
        AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
        CompileToAssembly(generated);
    }

    [IntegrationTestFact]
    public void Component_WithRef()
    {
        // Arrange
        AdditionalSyntaxTrees.Add(Parse(@"
using Microsoft.AspNetCore.Components;

namespace Test
{
    public class MyComponent : ComponentBase
    {
    }
}
"));

        // Arrange/Act
        var generated = CompileToCSharp(@"
<MyComponent ParamBefore=""before"" @ref=""myInstance"" ParamAfter=""after"" />

@code {
    private Test.MyComponent myInstance;
    public void Foo() { System.GC.KeepAlive(myInstance); }
}
");

        // Assert
        AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
        AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
        CompileToAssembly(generated);
    }

    [IntegrationTestFact, WorkItem("https://github.com/dotnet/razor/issues/8170")]
    public void Component_WithRef_Nullable()
    {
        // Act
        var generated = CompileToCSharp("""
            <TestComponent @ref="myComponent" />

            @code {
                private TestComponent myComponent = null!;
                public void Use() { System.GC.KeepAlive(myComponent); }
            }
            """,
            nullableEnable: true);

        // Assert
        AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
        AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
        CompileToAssembly(generated);
    }

    [IntegrationTestFact, WorkItem("https://github.com/dotnet/razor/issues/8170")]
    public void Component_WithRef_Nullable_Generic()
    {
        // Arrange
        AdditionalSyntaxTrees.Add(Parse("""
            using Microsoft.AspNetCore.Components;

            namespace Test;

            public class MyComponent<T> : ComponentBase
            {
                [Parameter] public T MyParameter { get; set; } = default!;
            }
            """));

        // Act
        var generated = CompileToCSharp("""
            <MyComponent @ref="myComponent" MyParameter="1" />

            @code {
                private MyComponent<int> myComponent = null!;
                public void Use() { System.GC.KeepAlive(myComponent); }
            }
            """,
            nullableEnable: true);

        // Assert
        AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
        AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
        CompileToAssembly(generated);
    }

    [IntegrationTestFact]
    public void Component_WithRef_WithChildContent()
    {
        // Arrange
        AdditionalSyntaxTrees.Add(Parse(@"
using Microsoft.AspNetCore.Components;

namespace Test
{
    public class MyComponent : ComponentBase
    {
    }
}
"));

        // Arrange/Act
        var generated = CompileToCSharp(@"
<MyComponent @ref=""myInstance"" SomeProp=""val"">
    Some <el>further</el> content
</MyComponent>

@code {
    private Test.MyComponent myInstance;
    public void Foo() { System.GC.KeepAlive(myInstance); }
}
");

        // Assert
        AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
        AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
        CompileToAssembly(generated);
    }

    [IntegrationTestFact]
    public void Element_WithRef_AttributeNameIsCaseSensitive()
    {
        // Arrange/Act
        var generated = CompileToCSharp(@"
<elem attributebefore=""before"" @rEF=""myElem"" attributeafter=""after"">Hello</elem>");

        // Assert
        AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
        AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
        CompileToAssembly(generated);
    }

    [IntegrationTestFact, WorkItem("https://github.com/dotnet/razor/issues/9625")]
    public void Component_WithRef_Generic_SystemInNamespace()
    {
        var generated = CompileToCSharp("""
            @namespace X.Y.System.Z
            @typeparam T

            <TestComponent Param="Param" @ref="comp" />

            @code {
                private TestComponent<T?>? comp;
                [Parameter] public T? Param { get; set; }
            }
            """, nullableEnable: true);
        CompileToAssembly(generated);
    }

    [IntegrationTestFact, WorkItem("https://github.com/dotnet/razor/issues/12043")]
    public void Component_WithRef_NamespaceConflict()
    {
        AdditionalSyntaxTrees.Add(Parse("""
            using Microsoft.AspNetCore.Components;

            namespace Y
            {
                public class MyComponent : ComponentBase;
            }

            namespace X.Y
            {
                public static class E
                {
                    public static int M() => 123;
                }
            }
            """));

        var generated = CompileToCSharp("""
            @using global::Y
            @using global::X.Y
            @namespace X

            @E.M()
            <MyComponent @ref="comp" />

            @code {
                private MyComponent comp;
            }
            """);

        var expectedDiagnostics = DesignTime
            ? new[]
            {
                // x:\dir\subdir\Test\TestComponent.cshtml(9,25): warning CS0414: The field 'TestComponent.comp' is assigned but its value is never used
                //     private MyComponent comp;
                Diagnostic(ErrorCode.WRN_UnreferencedFieldAssg, "comp").WithArguments("X.TestComponent.comp"),
            }
            : [];

        CompileToAssembly(generated, expectedDiagnostics);
    }

    #endregion

    #region Templates

    [IntegrationTestFact]
    public void RazorTemplate_InCodeBlock()
    {
        // Arrange

        // Act
        var generated = CompileToCSharp(@"
@{
    RenderFragment<Person> p = (person) => @<div>@person.Name</div>;
}
@code {
    class Person
    {
        public string Name { get; set; }
    }
}");

        // Assert
        AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
        AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
        CompileToAssembly(generated);
    }

    [IntegrationTestFact]
    public void RazorTemplate_InExplicitExpression()
    {
        // Arrange

        // Act
        var generated = CompileToCSharp(@"
@(RenderPerson((person) => @<div>@person.Name</div>))
@code {
    class Person
    {
        public string Name { get; set; }
    }

    object RenderPerson(RenderFragment<Person> p) => null;
}");

        // Assert
        AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
        AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
        CompileToAssembly(generated);
    }

    [IntegrationTestFact]
    public void RazorTemplate_NonGeneric_InImplicitExpression()
    {
        // Arrange

        // Act
        var generated = CompileToCSharp(@"
@RenderPerson(@<div>HI</div>)
@code {
    object RenderPerson(RenderFragment p) => null;
}");

        // Assert
        AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
        AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
        CompileToAssembly(generated);
    }

    [IntegrationTestFact]
    public void RazorTemplate_Generic_InImplicitExpression()
    {
        // Arrange

        // Act
        var generated = CompileToCSharp(@"
@RenderPerson((person) => @<div>@person.Name</div>)
@code {
    class Person
    {
        public string Name { get; set; }
    }

    object RenderPerson(RenderFragment<Person> p) => null;
}");

        // Assert
        AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
        AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
        CompileToAssembly(generated);
    }

    [IntegrationTestFact]
    public void RazorTemplate_ContainsComponent()
    {
        // Arrange
        AdditionalSyntaxTrees.Add(Parse(@"
using Microsoft.AspNetCore.Components;

namespace Test
{
    public class MyComponent : ComponentBase
    {
        [Parameter] public string Name { get; set; }
    }
}
"));

        // Act
        var generated = CompileToCSharp(@"
@{
    RenderFragment<Person> p = (person) => @<div><MyComponent Name=""@person.Name""/></div>;
}
@code {
    class Person
    {
        public string Name { get; set; }
    }
}");

        // Assert
        AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
        AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
        CompileToAssembly(generated);
    }

    // Targeted at the logic that assigns 'builder' names
    [IntegrationTestFact]
    public void RazorTemplate_FollowedByComponent()
    {
        // Arrange
        AdditionalSyntaxTrees.Add(Parse(@"
using Microsoft.AspNetCore.Components;

namespace Test
{
    public class MyComponent : ComponentBase
    {
        [Parameter] public string Name { get; set; }
    }
}
"));

        // Act
        var generated = CompileToCSharp(@"
@{
    RenderFragment<Person> p = (person) => @<div><MyComponent Name=""@person.Name""/></div>;
}
<MyComponent>
@(""hello, world!"")
</MyComponent>

@code {
    class Person
    {
        public string Name { get; set; }
    }
}");

        // Assert
        AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
        AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
        CompileToAssembly(generated);
    }

    [IntegrationTestFact]
    public void RazorTemplate_NonGeneric_AsComponentParameter()
    {
        // Arrange
        AdditionalSyntaxTrees.Add(Parse(@"
using Microsoft.AspNetCore.Components;

namespace Test
{
    public class MyComponent : ComponentBase
    {
        [Parameter] public RenderFragment Template { get; set; }
    }
}
"));

        // Act
        var generated = CompileToCSharp(@"
@{ RenderFragment template = @<div>Joey</div>; }
<MyComponent Person=""@template""/>
");

        // Assert
        AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
        AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
        CompileToAssembly(generated);
    }

    [IntegrationTestFact]
    public void RazorTemplate_Generic_AsComponentParameter()
    {
        // Arrange
        AdditionalSyntaxTrees.Add(Parse(@"
using Microsoft.AspNetCore.Components;

namespace Test
{
    public class MyComponent : ComponentBase
    {
        [Parameter] public RenderFragment<Person> PersonTemplate { get; set; }
    }

    public class Person
    {
        public string Name { get; set; }
    }
}
"));

        // Act
        var generated = CompileToCSharp(@"
@{ RenderFragment<Person> template = (person) => @<div>@person.Name</div>; }
<MyComponent PersonTemplate=""@template""/>
");

        // Assert
        AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
        AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
        CompileToAssembly(generated);
    }

    [IntegrationTestFact]
    public void RazorTemplate_AsComponentParameter_MixedContent()
    {
        // Arrange
        AdditionalSyntaxTrees.Add(Parse(@"
using Microsoft.AspNetCore.Components;

namespace Test
{
    public class MyComponent : ComponentBase
    {
        [Parameter] public RenderFragment<Context> Template { get; set; }
    }

    public class Context
    {
        public int Index { get; set; }
        public string Item { get; set; }
    }
}
"));

        // Act
        var generated = CompileToCSharp(@"
@{ RenderFragment<Test.Context> template = (context) => @<li>#@context.Index - @context.Item.ToLower()</li>; }
<MyComponent Template=""@template""/>
");

        // Assert
        AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
        AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
        CompileToAssembly(generated);
    }

    #endregion

    #region Whitespace

    [IntegrationTestFact]
    public void LeadingWhiteSpace_WithDirective()
    {
        // Arrange/Act
        var generated = CompileToCSharp(@"

@using System

<h1>Hello</h1>");

        // Assert
        AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
        AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
        CompileToAssembly(generated);
    }

    [IntegrationTestFact]
    public void LeadingWhiteSpace_WithCSharpExpression()
    {
        // Arrange/Act
        var generated = CompileToCSharp(@"

@(""My value"")

<h1>Hello</h1>");

        // Assert
        AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
        AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
        CompileToAssembly(generated);
    }

    [IntegrationTestFact]
    public void LeadingWhiteSpace_WithComponent()
    {
        // Arrange
        AdditionalSyntaxTrees.Add(Parse(@"
using Microsoft.AspNetCore.Components;

namespace Test
{
    public class SomeOtherComponent : ComponentBase
    {
        [Parameter] public RenderFragment ChildContent { get; set; }
    }
}
"));

        // Act
        var generated = CompileToCSharp(@"
<SomeOtherComponent>
    <h1>Child content at @DateTime.Now</h1>
    <p>Very @(""good"")</p>
</SomeOtherComponent>

<h1>Hello</h1>");

        // Assert
        AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
        AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
        CompileToAssembly(generated);
    }

    [IntegrationTestFact]
    public void TrailingWhiteSpace_WithDirective()
    {
        // Arrange/Act
        var generated = CompileToCSharp(@"
<h1>Hello</h1>

@page ""/my/url""

");

        // Assert
        AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
        AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
        CompileToAssembly(generated);
    }

    [IntegrationTestFact]
    public void TrailingWhiteSpace_WithCSharpExpression()
    {
        // Arrange/Act
        var generated = CompileToCSharp(@"
<h1>Hello</h1>

@(""My value"")

");

        // Assert
        AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
        AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
        CompileToAssembly(generated);
    }

    [IntegrationTestFact]
    public void TrailingWhiteSpace_WithComponent()
    {
        // Arrange
        AdditionalSyntaxTrees.Add(Parse(@"
using Microsoft.AspNetCore.Components;

namespace Test
{
    public class SomeOtherComponent : ComponentBase
    {
    }
}
"));

        // Act
        var generated = CompileToCSharp(@"
<h1>Hello</h1>

<SomeOtherComponent />

");

        // Assert
        AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
        AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
        CompileToAssembly(generated);
    }

    [IntegrationTestFact]
    public void Whitespace_BetweenElementAndFunctions()
    {
        // Arrange

        // Act
        var generated = CompileToCSharp(@"
    <elem attr=@Foo />
    @code {
        int Foo = 18;
    }
");

        // Assert
        AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
        AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
        CompileToAssembly(generated);
    }

    [IntegrationTestFact]
    public void WhiteSpace_InsideAttribute_InMarkupBlock()
    {
        // Arrange

        // Act
        var generated = CompileToCSharp(@"<div class=""first second"">Hello</div>");

        // Assert
        AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
        AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
        CompileToAssembly(generated);
    }

    [IntegrationTestFact]
    public void WhiteSpace_InMarkupInFunctionsBlock()
    {
        // Arrange

        // Act
        var generated = CompileToCSharp(@"
@using Microsoft.AspNetCore.Components.Rendering
@code {
    void MyMethod(RenderTreeBuilder __builder)
    {
        <ul>
            @for (var i = 0; i < 100; i++)
            {
                <li>
                    @i
                </li>
            }
        </ul>
    }
}
");

        // Assert
        AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
        AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
        CompileToAssembly(generated);
    }

    [IntegrationTestFact]
    public void WhiteSpace_WithPreserveWhitespace()
    {
        // Arrange

        // Act
        var generated = CompileToCSharp(@"

@preservewhitespace true

    <elem attr=@Foo>
        <child />
    </elem>

    @code {
        int Foo = 18;
    }

");

        // Assert
        AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
        AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
        CompileToAssembly(generated);
    }

    #endregion

    #region Legacy 3.1 Whitespace

    [IntegrationTestFact]
    public void Legacy_3_1_LeadingWhiteSpace_WithDirective()
    {
        // Arrange/Act
        _configuration = base.Configuration with { LanguageVersion = RazorLanguageVersion.Version_3_0 };

        var generated = CompileToCSharp(@"

@using System

<h1>Hello</h1>");

        // Assert
        AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
        AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
        CompileToAssembly(generated);
    }

    [IntegrationTestFact]
    public void Legacy_3_1_LeadingWhiteSpace_WithCSharpExpression()
    {
        // Arrange/Act
        _configuration = base.Configuration with { LanguageVersion = RazorLanguageVersion.Version_3_0 };

        var generated = CompileToCSharp(@"

@(""My value"")

<h1>Hello</h1>");

        // Assert
        AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
        AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
        CompileToAssembly(generated);
    }

    [IntegrationTestFact]
    public void Legacy_3_1_LeadingWhiteSpace_WithComponent()
    {
        // Arrange
        _configuration = base.Configuration with { LanguageVersion = RazorLanguageVersion.Version_3_0 };

        AdditionalSyntaxTrees.Add(Parse(@"
using Microsoft.AspNetCore.Components;

namespace Test
{
    public class SomeOtherComponent : ComponentBase
    {
        [Parameter] public RenderFragment ChildContent { get; set; }
    }
}
"));

        // Act
        var generated = CompileToCSharp(@"
<SomeOtherComponent>
    <h1>Child content at @DateTime.Now</h1>
    <p>Very @(""good"")</p>
</SomeOtherComponent>

<h1>Hello</h1>");

        // Assert
        AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
        AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
        CompileToAssembly(generated);
    }

    [IntegrationTestFact]
    public void Legacy_3_1_TrailingWhiteSpace_WithDirective()
    {
        // Arrange/Act
        _configuration = base.Configuration with { LanguageVersion = RazorLanguageVersion.Version_3_0 };

        var generated = CompileToCSharp(@"
<h1>Hello</h1>

@page ""/my/url""

");

        // Assert
        AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
        AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
        CompileToAssembly(generated);
    }

    [IntegrationTestFact]
    public void Legacy_3_1_TrailingWhiteSpace_WithCSharpExpression()
    {
        // Arrange/Act
        _configuration = base.Configuration with { LanguageVersion = RazorLanguageVersion.Version_3_0 };

        var generated = CompileToCSharp(@"
<h1>Hello</h1>

@(""My value"")

");

        // Assert
        AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
        AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
        CompileToAssembly(generated);
    }

    [IntegrationTestFact]
    public void Legacy_3_1_TrailingWhiteSpace_WithComponent()
    {
        // Arrange
        _configuration = base.Configuration with { LanguageVersion = RazorLanguageVersion.Version_3_0 };

        AdditionalSyntaxTrees.Add(Parse(@"
using Microsoft.AspNetCore.Components;

namespace Test
{
    public class SomeOtherComponent : ComponentBase
    {
    }
}
"));

        // Act
        var generated = CompileToCSharp(@"
<h1>Hello</h1>

<SomeOtherComponent />

");

        // Assert
        AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
        AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
        CompileToAssembly(generated);
    }

    [IntegrationTestFact]
    public void Legacy_3_1_Whitespace_BetweenElementAndFunctions()
    {
        // Arrange
        _configuration = base.Configuration with { LanguageVersion = RazorLanguageVersion.Version_3_0 };

        // Act
        var generated = CompileToCSharp(@"
    <elem attr=@Foo />
    @code {
        int Foo = 18;
    }
");

        // Assert
        AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
        AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
        CompileToAssembly(generated);
    }

    [IntegrationTestFact]
    public void Legacy_3_1_WhiteSpace_InsideAttribute_InMarkupBlock()
    {
        // Arrange
        _configuration = base.Configuration with { LanguageVersion = RazorLanguageVersion.Version_3_0 };

        // Act
        var generated = CompileToCSharp(@"<div class=""first second"">Hello</div>");

        // Assert
        AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
        AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
        CompileToAssembly(generated);
    }

    [IntegrationTestFact]
    public void Legacy_3_1_WhiteSpace_InMarkupInFunctionsBlock()
    {
        // Arrange
        _configuration = base.Configuration with { LanguageVersion = RazorLanguageVersion.Version_3_0 };

        // Act
        var generated = CompileToCSharp(@"
@using Microsoft.AspNetCore.Components.Rendering
@code {
    void MyMethod(RenderTreeBuilder __builder)
    {
        <ul>
            @for (var i = 0; i < 100; i++)
            {
                <li>
                    @i
                </li>
            }
        </ul>
    }
}
");

        // Assert
        AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
        AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
        CompileToAssembly(generated);
    }

    #endregion

    #region Imports
    [IntegrationTestFact]
    public void Component_WithImportsFile()
    {
        // Arrange
        var importContent = @"
@using System.Text
@using System.Reflection
@attribute [Serializable]
";
        var importItem = CreateProjectItem("_Imports.razor", importContent, RazorFileKind.ComponentImport);
        ImportItems.Add(importItem);
        AdditionalSyntaxTrees.Add(Parse(@"
using Microsoft.AspNetCore.Components;

namespace Test
{
    public class Counter : ComponentBase
    {
        public int Count { get; set; }
    }
}
"));

        // Act
        var generated = CompileToCSharp(@"
<Counter />
");

        // Assert
        AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
        AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
        CompileToAssembly(generated);
    }

    [IntegrationTestFact]
    public void ComponentImports()
    {
        // Arrange
        AdditionalSyntaxTrees.Add(Parse(@"
namespace Test
{
    public class MainLayout : ComponentBase, ILayoutComponent
    {
        public RenderFragment Body { get; set; }
    }
}
"));

        // Act
        var generated = CompileToCSharp("_Imports.razor", @"
@using System.Text
@using System.Reflection

@layout MainLayout
@Foo
<div>Hello</div>
", fileKind: RazorFileKind.ComponentImport, expectedCSharpDiagnostics: [
            // (4,31): error CS0246: The type or namespace name 'ComponentBase' could not be found (are you missing a using directive or an assembly reference?)
            //     public class MainLayout : ComponentBase, ILayoutComponent
            Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "ComponentBase").WithArguments("ComponentBase").WithLocation(4, 31),
            // (4,46): error CS0246: The type or namespace name 'ILayoutComponent' could not be found (are you missing a using directive or an assembly reference?)
            //     public class MainLayout : ComponentBase, ILayoutComponent
            Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "ILayoutComponent").WithArguments("ILayoutComponent").WithLocation(4, 46),
            // (6,16): error CS0246: The type or namespace name 'RenderFragment' could not be found (are you missing a using directive or an assembly reference?)
            //         public RenderFragment Body { get; set; }
            Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "RenderFragment").WithArguments("RenderFragment").WithLocation(6, 16)]);

        // Assert
        AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
        AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
        CompileToAssembly(generated, DesignTime
            ? [// (4,31): error CS0246: The type or namespace name 'ComponentBase' could not be found (are you missing a using directive or an assembly reference?)
               //     public class MainLayout : ComponentBase, ILayoutComponent
               Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "ComponentBase").WithArguments("ComponentBase").WithLocation(4, 31),
               // (4,46): error CS0246: The type or namespace name 'ILayoutComponent' could not be found (are you missing a using directive or an assembly reference?)
               //     public class MainLayout : ComponentBase, ILayoutComponent
               Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "ILayoutComponent").WithArguments("ILayoutComponent").WithLocation(4, 46),
               // (6,16): error CS0246: The type or namespace name 'RenderFragment' could not be found (are you missing a using directive or an assembly reference?)
               //         public RenderFragment Body { get; set; }
               Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "RenderFragment").WithArguments("RenderFragment").WithLocation(6, 16),
               // x:\dir\subdir\Test\_Imports.razor(5,2): error CS0103: The name 'Foo' does not exist in the current context
               // Foo
               Diagnostic(ErrorCode.ERR_NameNotInContext, "Foo").WithArguments("Foo").WithLocation(5, 7)]
            : [// (4,31): error CS0246: The type or namespace name 'ComponentBase' could not be found (are you missing a using directive or an assembly reference?)
               //     public class MainLayout : ComponentBase, ILayoutComponent
               Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "ComponentBase").WithArguments("ComponentBase").WithLocation(4, 31),
               // (4,46): error CS0246: The type or namespace name 'ILayoutComponent' could not be found (are you missing a using directive or an assembly reference?)
               //     public class MainLayout : ComponentBase, ILayoutComponent
               Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "ILayoutComponent").WithArguments("ILayoutComponent").WithLocation(4, 46),
               // (6,16): error CS0246: The type or namespace name 'RenderFragment' could not be found (are you missing a using directive or an assembly reference?)
               //         public RenderFragment Body { get; set; }
               Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "RenderFragment").WithArguments("RenderFragment").WithLocation(6, 16),
               // x:\dir\subdir\Test\_Imports.razor(5,2): error CS0103: The name 'Foo' does not exist in the current context
               // Foo
               Diagnostic(ErrorCode.ERR_NameNotInContext, "Foo").WithArguments("Foo").WithLocation(5, 2),
               // x:\dir\subdir\Test\_Imports.razor(5,2): error CS0103: The name '__builder' does not exist in the current context
               // __builder.AddContent(0, Foo
               Diagnostic(ErrorCode.ERR_NameNotInContext, "__builder").WithArguments("__builder").WithLocation(5, 2)]);
    }

    [IntegrationTestFact]
    public void ComponentImports_GlobalPrefix()
    {
        // Arrange
        AdditionalSyntaxTrees.Add(Parse("""
            using Microsoft.AspNetCore.Components;

            namespace MyComponents
            {
                public class Counter : ComponentBase
                {
                }
            }
            """));

        // Act
        var generated = CompileToCSharp("Index.razor", cshtmlContent: """
            @using global::MyComponents

            <Counter />
            """);

        // Assert
        CompileToAssembly(generated);
        Assert.DoesNotContain("<Counter", generated.Code);
        Assert.Contains("global::MyComponents.Counter", generated.Code);
    }

    [IntegrationTestFact]
    public void Component_NamespaceDirective_InImports()
    {
        // Arrange
        var importContent = @"
@using System.Text
@using System.Reflection
@namespace New.Test
";
        var importItem = CreateProjectItem("_Imports.razor", importContent, RazorFileKind.ComponentImport);
        ImportItems.Add(importItem);
        AdditionalSyntaxTrees.Add(Parse(@"
using Microsoft.AspNetCore.Components;

namespace New.Test
{
    public class Counter : ComponentBase
    {
        public int Count { get; set; }
    }
}
"));

        // Act
        var generated = CompileToCSharp(@"
<Counter />
");

        // Assert
        AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
        AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
        CompileToAssembly(generated);
    }

    [IntegrationTestFact]
    public void Component_NamespaceDirective_OverrideImports()
    {
        // Arrange
        var importContent = @"
@using System.Text
@using System.Reflection
@namespace Import.Test
";
        var importItem = CreateProjectItem("_Imports.razor", importContent, RazorFileKind.ComponentImport);
        ImportItems.Add(importItem);
        AdditionalSyntaxTrees.Add(Parse(@"
using Microsoft.AspNetCore.Components;

namespace New.Test
{
    public class Counter2 : ComponentBase
    {
        public int Count { get; set; }
    }
}
"));

        // Act
        var generated = CompileToCSharp("Pages/Counter.razor", cshtmlContent: @"
@namespace New.Test
<Counter2 />
");

        // Assert
        AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
        AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
        CompileToAssembly(generated);
    }

    [IntegrationTestFact, WorkItem("https://github.com/dotnet/razor/issues/7091")]
    public void Component_NamespaceDirective_ContainsSystem()
    {
        // Act
        var generated = CompileToCSharp("""
            @namespace X.System.Y
            """);

        // Assert
        CompileToAssembly(generated);
    }

    [IntegrationTestFact]
    public void Component_PreserveWhitespaceDirective_InImports()
    {
        // Arrange
        var importContent = @"
@preservewhitespace true
";
        var importItem = CreateProjectItem("_Imports.razor", importContent, RazorFileKind.ComponentImport);
        ImportItems.Add(importItem);

        // Act
        var generated = CompileToCSharp(@"

<parent>
    <child> @DateTime.Now </child>
</parent>

");

        // Assert
        AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
        AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
        CompileToAssembly(generated);
    }

    [IntegrationTestFact]
    public void Component_PreserveWhitespaceDirective_OverrideImports()
    {
        // Arrange
        var importContent = @"
@preservewhitespace true
";
        var importItem = CreateProjectItem("_Imports.razor", importContent, RazorFileKind.ComponentImport);
        ImportItems.Add(importItem);

        // Act
        var generated = CompileToCSharp(@"
@preservewhitespace false

<parent>
    <child> @DateTime.Now </child>
</parent>

");

        // Assert
        AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
        AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
        CompileToAssembly(generated);
    }

    #endregion

    #region Namespace

    [IntegrationTestFact]
    public void EmptyRootNamespace()
    {
        DefaultRootNamespace = string.Empty;

        AdditionalSyntaxTrees.Add(Parse("""
            using Microsoft.AspNetCore.Components;
            public class Component1 : ComponentBase { }
            namespace Shared
            {
                public class Component2 : ComponentBase { }
            }
            class C
            {
                void M1(TestComponent t) { }
                void M2(global::TestComponent t) { }
            }
            """));
        var generated = CompileToCSharp("""
            <h1>Generated</h1>
            <Component1 />
            <Shared.Component2 />
            """);

        AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
        AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
        CompileToAssembly(generated);
    }

    [IntegrationTestFact]
    public void NamespaceWithSurrogatePair()
    {
        DefaultRootNamespace = "test𝔸namespace";

        AdditionalSyntaxTrees.Add(Parse("""
            
            using Microsoft.AspNetCore.Components;
            namespace test_namespace
            {
                public class Component1 : ComponentBase { }
                namespace Shared
                {
                    public class Component2 : ComponentBase { }
                }
            }
            """));

        var generated = CompileToCSharp("""
            <h1>Generated</h1>
            <Component1 />
            <Shared.Component2 />
            """);

        AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
        AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
        CompileToAssembly(generated);
    }

    #endregion

    #region "CSS scoping"
    [IntegrationTestFact]
    public void Component_WithCssScope()
    {
        // Arrange
        AdditionalSyntaxTrees.Add(Parse(@"
using Microsoft.AspNetCore.Components;

namespace Test
{
    public class TemplatedComponent : ComponentBase
    {
        [Parameter]
        public RenderFragment ChildContent { get; set; }
    }
}
"));

        // Act
        // This test case attempts to use all syntaxes that might interact with auto-generated attributes
        var generated = CompileToCSharp(@"
@using Microsoft.AspNetCore.Components.Web
@using Microsoft.AspNetCore.Components.Rendering
<h1>Element with no attributes</h1>
<parent with-attributes=""yes"" with-csharp-attribute-value=""@(123)"">
    <child />
    <child has multiple attributes=""some with values"">With text</child>
    <TemplatedComponent @ref=""myComponentReference"">
        <span id=""hello"">This is in child content</span>
    </TemplatedComponent>
</parent>
@if (DateTime.Now.Year > 1950)
{
    <with-ref-capture some-attr @ref=""myElementReference"">Content</with-ref-capture>
    <input id=""myElem"" @bind=""myVariable"" another-attr=""Another attr value"" />
}

@code {
    ElementReference myElementReference;
    TemplatedComponent myComponentReference;
    string myVariable;

    void MethodRenderingMarkup(RenderTreeBuilder __builder)
    {
        for (var i = 0; i < 10; i++)
        {
            <li data-index=@i>Something @i</li>
        }

        System.GC.KeepAlive(myElementReference);
        System.GC.KeepAlive(myComponentReference);
        System.GC.KeepAlive(myVariable);
    }
}
", cssScope: "TestCssScope");

        // Assert
        AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
        AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
        CompileToAssembly(generated);
    }
    #endregion

    #region Misc

    [IntegrationTestFact] // We don't process <!DOCTYPE ...> - we just skip them
    public void Component_WithDocType()
    {
        // Arrange

        // Act
        var generated = CompileToCSharp(@"
<!DOCTYPE html>
<div>
</div>");

        // Assert
        AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
        AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
        CompileToAssembly(generated);
    }

    [IntegrationTestFact]
    public void DuplicateMarkupAttributes_IsAnError()
    {
        // Arrange

        // Act
        var generated = CompileToCSharp(@"
<div>
  <a href=""/cool-url"" style="""" disabled href=""/even-cooler-url"">Learn the ten cool tricks your compiler author will hate!</a>
</div>");

        // Assert
        AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
        AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);

        var diagnostic = Assert.Single(generated.RazorDiagnostics);
        Assert.Same(ComponentDiagnosticFactory.DuplicateMarkupAttribute.Id, diagnostic.Id);
    }

    [IntegrationTestFact]
    public void DuplicateMarkupAttributes_IsAnError_EventHandler()
    {
        // Arrange

        // Act
        var generated = CompileToCSharp(@"
@using Microsoft.AspNetCore.Components.Web
<div>
  <a onclick=""test()"" @onclick=""() => {}"">Learn the ten cool tricks your compiler author will hate!</a>
</div>");

        // Assert
        AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
        AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);

        var diagnostic = Assert.Single(generated.RazorDiagnostics);
        Assert.Same(ComponentDiagnosticFactory.DuplicateMarkupAttributeDirective.Id, diagnostic.Id);
    }

    [IntegrationTestFact]
    public void DuplicateMarkupAttributes_Multiple_IsAnError()
    {
        // Arrange

        // Act
        var generated = CompileToCSharp(@"
<div>
  <a href=""/cool-url"" style="""" disabled href=""/even-cooler-url"" href>Learn the ten cool tricks your compiler author will hate!</a>
</div>");

        // Assert
        AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
        AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);

        Assert.All(generated.RazorDiagnostics, d =>
        {
            Assert.Same(ComponentDiagnosticFactory.DuplicateMarkupAttribute.Id, d.Id);
        });
    }

    [IntegrationTestFact]
    public void DuplicateMarkupAttributes_IsAnError_BindValue()
    {
        // Arrange

        // Act
        var generated = CompileToCSharp(@"
@using Microsoft.AspNetCore.Components.Web
<div>
  <input type=""text"" value=""17"" @bind=""@text""></input>
</div>
@functions {
    private string text = ""hi"";
}
");


        // Assert
        AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
        AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);

        var diagnostic = Assert.Single(generated.RazorDiagnostics);
        Assert.Same(ComponentDiagnosticFactory.DuplicateMarkupAttributeDirective.Id, diagnostic.Id);
    }

    [IntegrationTestFact]
    public void DuplicateMarkupAttributes_DifferentCasing_IsAnError_BindValue()
    {
        // Arrange

        // Act
        var generated = CompileToCSharp(@"
@using Microsoft.AspNetCore.Components.Web
<div>
  <input type=""text"" Value=""17"" @bind=""@text""></input>
</div>
@functions {
    private string text = ""hi"";
}
");

        // Assert
        AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
        AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);

        var diagnostic = Assert.Single(generated.RazorDiagnostics);
        Assert.Same(ComponentDiagnosticFactory.DuplicateMarkupAttributeDirective.Id, diagnostic.Id);
    }

    [IntegrationTestFact]
    public void DuplicateMarkupAttributes_IsAnError_BindOnInput()
    {
        // Arrange

        // Act
        var generated = CompileToCSharp(@"
@using Microsoft.AspNetCore.Components.Web
<div>
  <input type=""text"" @bind-value=""@text"" @bind-value:event=""oninput"" @oninput=""() => {}""></input>
</div>
@functions {
    private string text = ""hi"";
}
");

        // Assert
        AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
        AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);

        var diagnostic = Assert.Single(generated.RazorDiagnostics);
        Assert.Same(ComponentDiagnosticFactory.DuplicateMarkupAttributeDirective.Id, diagnostic.Id);
    }

    [IntegrationTestFact]
    public void DuplicateComponentParameters_IsAnError()
    {
        // Arrange
        AdditionalSyntaxTrees.Add(Parse(@"
using System;
using Microsoft.AspNetCore.Components;

namespace Test
{
    public class MyComponent : ComponentBase
    {
        [Parameter] public string Message { get; set; }
    }
}
"));
        // Act
        var generated = CompileToCSharp(@"
<MyComponent Message=""test"" mESSAGE=""test"" />
");

        // Assert
        AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
        AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);

        var diagnostic = Assert.Single(generated.RazorDiagnostics);
        Assert.Same(ComponentDiagnosticFactory.DuplicateComponentParameter.Id, diagnostic.Id);
    }

    [IntegrationTestFact]
    public void DuplicateComponentParameters_IsAnError_Multiple()
    {
        // Arrange
        AdditionalSyntaxTrees.Add(Parse(@"
using System;
using Microsoft.AspNetCore.Components;

namespace Test
{
    public class MyComponent : ComponentBase
    {
        [Parameter] public string Message { get; set; }
    }
}
"));
        // Act
        var generated = CompileToCSharp(@"
<MyComponent Message=""test"" mESSAGE=""test"" Message=""anotherone"" />
");

        // Assert
        AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
        AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);

        Assert.All(generated.RazorDiagnostics, d =>
        {
            Assert.Same(ComponentDiagnosticFactory.DuplicateComponentParameter.Id, d.Id);
        });
    }

    [IntegrationTestFact]
    public void DuplicateComponentParameters_IsAnError_WeaklyTyped()
    {
        // Arrange
        AdditionalSyntaxTrees.Add(Parse(@"
using System;
using Microsoft.AspNetCore.Components;

namespace Test
{
    public class MyComponent : ComponentBase
    {
        [Parameter] public string Message { get; set; }
    }
}
"));
        // Act
        var generated = CompileToCSharp(@"
<MyComponent Foo=""test"" foo=""test"" />
");

        // Assert
        AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
        AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);

        var diagnostic = Assert.Single(generated.RazorDiagnostics);
        Assert.Same(ComponentDiagnosticFactory.DuplicateComponentParameter.Id, diagnostic.Id);
    }

    [IntegrationTestFact]
    public void DuplicateComponentParameters_IsAnError_BindMessage()
    {
        // Arrange
        AdditionalSyntaxTrees.Add(Parse(@"
using System;
using System.Linq.Expressions;
using Microsoft.AspNetCore.Components;

namespace Test
{
    public class MyComponent : ComponentBase
    {
        [Parameter] public string Message { get; set; }
        [Parameter] public EventCallback<string> MessageChanged { get; set; }
        [Parameter] public Expression<Action<string>> MessageExpression { get; set; }
    }
}
"));
        // Act
        var generated = CompileToCSharp(@"
<MyComponent Message=""@message"" @bind-Message=""@message"" />
@functions {
    string message = ""hi"";
}
");

        // Assert
        AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
        AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);

        var diagnostic = Assert.Single(generated.RazorDiagnostics);
        Assert.Same(ComponentDiagnosticFactory.DuplicateComponentParameterDirective.Id, diagnostic.Id);
    }

    [IntegrationTestFact]
    public void DuplicateComponentParameters_IsAnError_BindMessageChanged()
    {
        // Arrange
        AdditionalSyntaxTrees.Add(Parse(@"
using System;
using System.Linq.Expressions;
using Microsoft.AspNetCore.Components;

namespace Test
{
    public class MyComponent : ComponentBase
    {
        [Parameter] public string Message { get; set; }
        [Parameter] public EventCallback<string> MessageChanged { get; set; }
        [Parameter] public Expression<Action<string>> MessageExpression { get; set; }
    }
}
"));
        // Act
        var generated = CompileToCSharp(@"
<MyComponent MessageChanged=""@((s) => {})"" @bind-Message=""@message"" />
@functions {
    string message = ""hi"";
}
");

        // Assert
        AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
        AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);

        var diagnostic = Assert.Single(generated.RazorDiagnostics);
        Assert.Same(ComponentDiagnosticFactory.DuplicateComponentParameterDirective.Id, diagnostic.Id);
    }

    [IntegrationTestFact]
    public void DuplicateComponentParameters_IsAnError_BindMessageExpression()
    {
        // Arrange
        AdditionalSyntaxTrees.Add(Parse(@"
using System;
using System.Linq.Expressions;
using Microsoft.AspNetCore.Components;

namespace Test
{
    public class MyComponent : ComponentBase
    {
        [Parameter] public string Message { get; set; }
        [Parameter] public EventCallback<string> MessageChanged { get; set; }
        [Parameter] public Expression<Action<string>> MessageExpression { get; set; }
    }
}
"));
        // Act
        var generated = CompileToCSharp(@"
<MyComponent @bind-Message=""@message"" MessageExpression=""@((s) => {})"" />
@functions {
    string message = ""hi"";
}
");

        // Assert
        AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
        AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);

        var diagnostic = Assert.Single(generated.RazorDiagnostics);
        Assert.Same(ComponentDiagnosticFactory.DuplicateComponentParameterDirective.Id, diagnostic.Id);
    }

    [IntegrationTestFact, WorkItem("https://github.com/dotnet/blazor/issues/597")]
    public void Regression_597()
    {
        // Arrange
        AdditionalSyntaxTrees.Add(Parse(@"
using Microsoft.AspNetCore.Components;

namespace Test
{
    public class Counter : ComponentBase
    {
        public int Count { get; set; }
    }
}
"));

        // Act
        var generated = CompileToCSharp(@"
<Counter @bind-v=""y"" />
@code {
    string y = null;
}
");

        // Assert
        AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
        AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
        CompileToAssembly(generated);
    }

    [IntegrationTestFact]
    public void Regression_609()
    {
        // Arrange
        AdditionalSyntaxTrees.Add(Parse(@"
using System;
using Microsoft.AspNetCore.Components;

namespace Test
{
    public class User : ComponentBase
    {
        public string Name { get; set; }
        public Action<string> NameChanged { get; set; }
        public bool IsActive { get; set; }
        public Action<bool> IsActiveChanged { get; set; }
    }
}
"));

        // Act
        var generated = CompileToCSharp(@"
<User @bind-Name=""@UserName"" @bind-IsActive=""@UserIsActive"" />

@code {
    public string UserName { get; set; }
    public bool UserIsActive { get; set; }
}
");

        // Assert
        AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
        AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
        CompileToAssembly(generated);
    }

    [IntegrationTestFact, WorkItem("https://github.com/dotnet/blazor/issues/772")]
    public void Regression_772()
    {
        // Arrange
        AdditionalSyntaxTrees.Add(Parse(@"
using Microsoft.AspNetCore.Components;

namespace Test
{
    public class SurveyPrompt : ComponentBase
    {
        [Parameter] public string Title { get; set; }
    }
}
"));

        // Act
        var generated = CompileToCSharp(@"
@page ""/""

<h1>Hello, world!</h1>

Welcome to your new app.

<SurveyPrompt Title=""
");

        // Assert
        AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
        AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);

        // This has some errors
        Assert.Collection(
            generated.RazorDiagnostics.OrderBy(d => d.Id),
            d => Assert.Equal("RZ1034", d.Id),
            d => Assert.Equal("RZ1035", d.Id));
    }

    [IntegrationTestFact, WorkItem("https://github.com/dotnet/blazor/issues/773")]
    public void Regression_773()
    {
        // Arrange
        AdditionalSyntaxTrees.Add(Parse(@"
using Microsoft.AspNetCore.Components;

namespace Test
{
    public class SurveyPrompt : ComponentBase
    {
        [Parameter] public string Title { get; set; }
    }
}
"));

        // Act
        var generated = CompileToCSharp(@"
@page ""/""

<h1>Hello, world!</h1>

Welcome to your new app.

<SurveyPrompt Title=""<div>Test!</div>"" />
");

        // Assert
        AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
        AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
        CompileToAssembly(generated);
    }

    [IntegrationTestFact]
    public void Regression_784()
    {
        // Arrange

        // Act
        var generated = CompileToCSharp(@"
@using Microsoft.AspNetCore.Components.Web
<p @onmouseover=""OnComponentHover"" style=""background: @ParentBgColor;"" />
@code {
    public string ParentBgColor { get; set; } = ""#FFFFFF"";

    public void OnComponentHover(MouseEventArgs e)
    {
    }
}
");

        // Assert
        AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
        AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
        CompileToAssembly(generated);
    }

    [IntegrationTestFact]
    public void EventHandlerTagHelper_EscapeQuotes()
    {
        // Act
        var generated = CompileToCSharp(@"
<input onfocus='alert(""Test"");' />
<input onfocus=""alert(""Test"");"" />
<input onfocus=""alert('Test');"" />
<p data-options='{direction: ""fromtop"", animation_duration: 25, direction: ""reverse""}'></p>
");

        // Assert
        AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
        AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
        CompileToAssembly(generated);
    }

    [IntegrationTestFact]
    public void Component_ComplexContentInAttribute()
    {
        // Arrange
        AdditionalSyntaxTrees.Add(Parse(@"
using Microsoft.AspNetCore.Components;

namespace Test
{
    public class MyComponent : ComponentBase
    {
        [Parameter] public string StringProperty { get; set; }
    }
}
"));

        // Act
        var generated = CompileToCSharp("""
            <MyComponent StringProperty="@MyEnum." />

            @code {
                public enum MyEnum
                {
                    One,
                    Two
                }
            }
            """);

        // Assert
        AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
        AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
        CompileToAssembly(generated,
            // x:\dir\subdir\Test\TestComponent.cshtml(1,31): error CS0119: 'TestComponent.MyEnum' is a type, which is not valid in the given context
            //                               MyEnum
            Diagnostic(ErrorCode.ERR_BadSKunknown, "MyEnum").WithArguments("Test.TestComponent.MyEnum", "type").WithLocation(1, 31));
        Assert.NotEmpty(generated.RazorDiagnostics);
    }

    [IntegrationTestFact, WorkItem("https://github.com/dotnet/razor/issues/9346")]
    public void Component_ComplexContentInAttribute_02()
    {
        // Arrange
        AdditionalSyntaxTrees.Add(Parse("""
            using Microsoft.AspNetCore.Components;

            namespace Test
            {
                public class MyComponent : ComponentBase
                {
                    [Parameter] public string StringProperty { get; set; }
                }
            }
            """));

        // Act
        var generated = CompileToCSharp("""
            <MyComponent StringProperty="@MyEnum+" />

            @code {
                public enum MyEnum
                {
                    One,
                    Two
                }
            }
            """);

        // Assert
        AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
        AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
        CompileToAssembly(generated,
            // x:\dir\subdir\Test\TestComponent.cshtml(1,31): error CS0119: 'TestComponent.MyEnum' is a type, which is not valid in the given context
            //                               MyEnum
            Diagnostic(ErrorCode.ERR_BadSKunknown, "MyEnum").WithArguments("Test.TestComponent.MyEnum", "type").WithLocation(1, 31));
        Assert.NotEmpty(generated.RazorDiagnostics);
    }

    [IntegrationTestFact, WorkItem("https://github.com/dotnet/razor/issues/9346")]
    public void Component_ComplexContentInAttribute_03()
    {
        // Arrange
        AdditionalSyntaxTrees.Add(Parse("""
            using Microsoft.AspNetCore.Components;

            namespace Test
            {
                public class MyComponent : ComponentBase
                {
                    [Parameter] public string StringProperty { get; set; }
                }
            }
            """));

        // Act
        var generated = CompileToCSharp("""
            <MyComponent StringProperty="@x html @("string")" />

            @code {
                int x = 1;
            }
            """);

        // Assert
        AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
        AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
        CompileToAssembly(generated, DesignTime
            ? [// x:\dir\subdir\Test\TestComponent.cshtml(1,32): error CS1003: Syntax error, ',' expected
              //                               x
              Diagnostic(ErrorCode.ERR_SyntaxError, "").WithArguments(",").WithLocation(1, 32),
              // (27,91): error CS1501: No overload for method 'TypeCheck' takes 2 arguments
              //             __o = global::Microsoft.AspNetCore.Components.CompilerServices.RuntimeHelpers.TypeCheck<global::System.String>(
              Diagnostic(ErrorCode.ERR_BadArgCount, "TypeCheck<global::System.String>").WithArguments("TypeCheck", "2").WithLocation(27, 91)]
            : [// x:\dir\subdir\Test\TestComponent.cshtml(1,32): error CS1003: Syntax error, ',' expected
              //                               x
              Diagnostic(ErrorCode.ERR_SyntaxError, "").WithArguments(",").WithLocation(1, 32),
              // (29,88): error CS1501: No overload for method 'TypeCheck' takes 2 arguments
              //             __o = global::Microsoft.AspNetCore.Components.CompilerServices.RuntimeHelpers.TypeCheck<global::System.String>(
              Diagnostic(ErrorCode.ERR_BadArgCount, "TypeCheck<global::System.String>").WithArguments("TypeCheck", "2").WithLocation(29, 88)]
            );
        Assert.NotEmpty(generated.RazorDiagnostics);
    }

    [IntegrationTestFact]
    public void Component_TextTagsAreNotRendered()
    {
        // Arrange
        AdditionalSyntaxTrees.Add(Parse(@"
using Microsoft.AspNetCore.Components;

namespace Test
{
    public class Counter : ComponentBase
    {
        public int Count { get; set; }
    }
}
"));

        // Act
        var generated = CompileToCSharp(@"
<Counter />
@if (true)
{
    <text>This text is rendered</text>
}
");

        // Assert
        AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
        AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
        CompileToAssembly(generated);
    }

    [IntegrationTestFact]
    public void Component_MatchingIsCaseSensitive()
    {
        // Arrange
        AdditionalSyntaxTrees.Add(Parse(@"
using Microsoft.AspNetCore.Components;

namespace Test
{
    public class MyComponent : ComponentBase
    {
        [Parameter] public int IntProperty { get; set; }
        [Parameter] public bool BoolProperty { get; set; }
    }
}
"));

        // Act
        var generated = CompileToCSharp(@"
<MyComponent />
<mycomponent />
<MyComponent intproperty='1' BoolProperty='true' />");

        // Assert
        AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
        AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
        CompileToAssembly(generated);
    }

    [IntegrationTestFact]
    public void Component_MultipleComponentsDifferByCase()
    {
        // Arrange
        AdditionalSyntaxTrees.Add(Parse(@"
using Microsoft.AspNetCore.Components;

namespace Test
{
    public class MyComponent : ComponentBase
    {
        [Parameter] public int IntProperty { get; set; }
    }

    public class Mycomponent : ComponentBase
    {
        [Parameter] public int IntProperty { get; set; }
    }
}
"));

        // Act
        var generated = CompileToCSharp(@"
<MyComponent IntProperty='1' />
<Mycomponent IntProperty='2' />");

        // Assert
        AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
        AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
        CompileToAssembly(generated);
    }

    [IntegrationTestFact]
    public void ElementWithUppercaseTagName_CanHideWarningWithBang()
    {
        // Arrange & Act
        var generated = CompileToCSharp(@"
<!NotAComponent />
<!DefinitelyNotAComponent></!DefinitelyNotAComponent>");

        // Assert
        AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
        AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
        CompileToAssembly(generated);
    }

    [IntegrationTestTheory, CombinatorialData, WorkItem("https://github.com/dotnet/razor/issues/9584")]
    public void ScriptTag_Razor8([CombinatorialValues("8.0", "latest")] string langVersion)
    {
        var generated = CompileToCSharp("""
            <script>alert("Hello");</script>
            """,
            configuration: Configuration with { LanguageVersion = RazorLanguageVersion.Parse(langVersion) });
        AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
        AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
        CompileToAssembly(generated);
    }

    [IntegrationTestFact, WorkItem("https://github.com/dotnet/razor/issues/9584")]
    public void ScriptTag_Razor7()
    {
        var generated = CompileToCSharp("""
            <script>alert("Hello");</script>
            """,
            configuration: Configuration with { LanguageVersion = RazorLanguageVersion.Version_7_0 });
        AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
        AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
        CompileToAssembly(generated);
    }

    [IntegrationTestFact]
    public void AtTransitions()
    {
        var generated = CompileToCSharp("""
            @{  
                var x = "hello";  
                @x x = "world"; @x  
            }  
            """);

        AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
        AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
        CompileToAssembly(generated);
    }

    [IntegrationTestFact, WorkItem("https://github.com/dotnet/sdk/issues/42730")]
    public void AtAtHandled()
    {
        var generated = CompileToCSharp("""
            @{ var validationMessage = @Html.ValidationMessage("test", "", new { @@class = "invalid-feedback" }, "div"); }
            """);

        AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
        AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
        CompileToAssembly(generated,
                // x:\dir\subdir\Test\TestComponent.cshtml(1,28): error CS0103: The name 'Html' does not exist in the current context
                //    var validationMessage = @Html.ValidationMessage("test", "", new { @@class = "invalid-feedback" }, "div");
                Diagnostic(ErrorCode.ERR_NameNotInContext, "@Html").WithArguments("Html").WithLocation(1, 28));
    }

    #endregion

    #region LinePragmas

    [IntegrationTestFact]
    public void ProducesEnhancedLinePragmaWhenNecessary()
    {
        var generated = CompileToCSharp(@"
<h1>Single line statement</h1>

Time: @DateTime.Now

<h1>Multiline block statement</h1>

@JsonToHtml(@""{
  'key1': 'value1'
  'key2': 'value2'
}"")

@code {
    public string JsonToHtml(string foo)
    {
        return foo;
    }
}
");

        // Assert
        AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
        AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
        CompileToAssembly(generated);
    }

    [IntegrationTestFact]
    public void ProducesStandardLinePragmaForCSharpCode()
    {
        var generated = CompileToCSharp(@"
<h1>Conditional statement</h1>
@for (var i = 0; i < 10; i++)
{
    <p>@i</p>
}

<h1>Statements inside code block</h1>
@{System.Console.WriteLine(1);System.Console.WriteLine(2);}

<h1>Full-on code block</h1>
@code {
    [Parameter]
    public int IncrementAmount { get; set; }
}
");

        // Assert
        AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
        AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
        CompileToAssembly(generated);
    }

    [IntegrationTestFact]
    public void CanProduceLinePragmasForComponentWithRenderFragment_01()
    {
        var code = @"
<div class=""row"">
  <a href=""#"" @onclick=Toggle class=""col-12"">@ActionText</a>
  @if (!Collapsed)
  {
    <div class=""col-12 card card-body"">
      @ChildContent
    </div>
  }
</div>
@code
{
  [Parameter]
  public RenderFragment ChildContent { get; set; } = (context) => <p>@context</p>
  [Parameter]
  public bool Collapsed { get; set; }
  string ActionText { get => Collapsed ? ""Expand"" : ""Collapse""; }
  void Toggle()
  {
    Collapsed = !Collapsed;
  }
}";

        DiagnosticDescription[] expectedDiagnostics = [
            // x:\dir\subdir\Test\TestComponent.cshtml(13,67): error CS1525: Invalid expression term '<'
            //   public RenderFragment ChildContent { get; set; } = (context) => <p>@context</p>
            Diagnostic(ErrorCode.ERR_InvalidExprTerm, "<").WithArguments("<").WithLocation(13, 67),
            // x:\dir\subdir\Test\TestComponent.cshtml(13,67): error CS0201: Only assignment, call, increment, decrement, await, and new object expressions can be used as a statement
            //   public RenderFragment ChildContent { get; set; } = (context) => <p>@context</p>
            Diagnostic(ErrorCode.ERR_IllegalStatement, """
            <p>@context</p>
              [Parameter]
            """.NormalizeLineEndings()).WithLocation(13, 67),
            // x:\dir\subdir\Test\TestComponent.cshtml(13,68): error CS0103: The name 'p' does not exist in the current context
            //   public RenderFragment ChildContent { get; set; } = (context) => <p>@context</p>
            Diagnostic(ErrorCode.ERR_NameNotInContext, "p").WithArguments("p").WithLocation(13, 68),
            // x:\dir\subdir\Test\TestComponent.cshtml(13,79): error CS1525: Invalid expression term '/'
            //   public RenderFragment ChildContent { get; set; } = (context) => <p>@context</p>
            Diagnostic(ErrorCode.ERR_InvalidExprTerm, "/").WithArguments("/").WithLocation(13, 79),
            // x:\dir\subdir\Test\TestComponent.cshtml(13,80): error CS0103: The name 'p' does not exist in the current context
            //   public RenderFragment ChildContent { get; set; } = (context) => <p>@context</p>
            Diagnostic(ErrorCode.ERR_NameNotInContext, "p").WithArguments("p").WithLocation(13, 80),
            // x:\dir\subdir\Test\TestComponent.cshtml(14,4): error CS0103: The name 'Parameter' does not exist in the current context
            //   [Parameter]
            Diagnostic(ErrorCode.ERR_NameNotInContext, "Parameter").WithArguments("Parameter").WithLocation(14, 4),
            // x:\dir\subdir\Test\TestComponent.cshtml(14,14): error CS1002: ; expected
            //   [Parameter]
            Diagnostic(ErrorCode.ERR_SemicolonExpected, "").WithLocation(14, 14)]
                    ;

        var generated = CompileToCSharp(code, expectedDiagnostics);

        // Assert
        AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
        AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
        CompileToAssembly(generated, expectedDiagnostics);
    }

    [IntegrationTestFact]
    public void CanProduceLinePragmasForComponentWithRenderFragment_02()
    {
        var generated = CompileToCSharp(@"
<div class=""row"">
  <a href=""#"" @onclick=Toggle class=""col-12"">@ActionText</a>
  @if (!Collapsed)
  {
    <div class=""col-12 card card-body"">
      @ChildContent
    </div>
  }
</div>
@code
{
  [Parameter]
  public RenderFragment<string> ChildContent { get; set; } = (context) => @<p>@context</p>;
  [Parameter]
  public bool Collapsed { get; set; }
  string ActionText { get => Collapsed ? ""Expand"" : ""Collapse""; }
  void Toggle()
  {
    Collapsed = !Collapsed;
  }
}");

        // Assert
        AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
        AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
        CompileToAssembly(generated);
    }

    [IntegrationTestFact, WorkItem("https://github.com/dotnet/razor/issues/9359")]
    public void LinePragma_Multiline()
    {
        // Act
        var generated = CompileToCSharp("""
            @("text"
            )
            """);

        // Assert
        AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
        AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
        CompileToAssembly(generated);
    }

    #endregion

    #region RenderMode

    [IntegrationTestFact]
    public void RenderMode_Directive_WithTypeParam()
    {
        var generated = CompileToCSharp("""
                @typeparam T
                @rendermode Microsoft.AspNetCore.Components.Web.RenderMode.InteractiveServer
                """);

        // Assert
        AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
        AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
        CompileToAssembly(generated);
    }

    [IntegrationTestFact]
    public void RenderMode_Directive_WithTypeParam_Razor9()
    {
        var generated = CompileToCSharp("""
                @typeparam T
                @rendermode Microsoft.AspNetCore.Components.Web.RenderMode.InteractiveServer
                """,
                configuration: Configuration with { LanguageVersion = RazorLanguageVersion.Version_9_0 },
                expectedCSharpDiagnostics:
                    // (17,19): error CS0305: Using the generic type 'TestComponent<T>' requires 1 type arguments
                    //     [global::Test.TestComponent.__PrivateComponentRenderModeAttribute]
                    Diagnostic(ErrorCode.ERR_BadArity, "TestComponent").WithArguments("Test.TestComponent<T>", "type", "1").WithLocation(17, 19));

        // Assert
        AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
        AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
        CompileToAssembly(generated,
            // (13,19): error CS0305: Using the generic type 'TestComponent<T>' requires 1 type arguments
            //     [global::Test.TestComponent.__PrivateComponentRenderModeAttribute]
            Diagnostic(ErrorCode.ERR_BadArity, "TestComponent").WithArguments("Test.TestComponent<T>", "type", "1").WithLocation(13, 19));
    }

    [IntegrationTestFact]
    public void RenderMode_Directive_WithTypeParam_First()
    {
        var generated = CompileToCSharp("""
                @rendermode Microsoft.AspNetCore.Components.Web.RenderMode.InteractiveServer
                @typeparam T
                """);

        // Assert
        AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
        AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
        CompileToAssembly(generated);
    }

    [IntegrationTestFact]
    public void RenderMode_Directive_FullyQualified()
    {
        var generated = CompileToCSharp("""
                @rendermode Microsoft.AspNetCore.Components.Web.RenderMode.InteractiveServer
                """);

        // Assert
        AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
        AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
        CompileToAssembly(generated);
    }

    [IntegrationTestFact]
    public void RenderMode_Directive_SimpleExpression()
    {
        var generated = CompileToCSharp("""
                @rendermode @(Microsoft.AspNetCore.Components.Web.RenderMode.InteractiveServer)
                """);

        // Assert
        AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
        AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
        CompileToAssembly(generated);
    }

    [IntegrationTestFact]
    public void RenderMode_Directive_SimpleExpression_With_Code()
    {
        var generated = CompileToCSharp("""
                @rendermode @(Microsoft.AspNetCore.Components.Web.RenderMode.InteractiveServer)

                @code
                {
                    [Parameter]
                    public int Count { get; set; }
                }
                """);

        // Assert
        AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
        AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
        CompileToAssembly(generated);
    }

    [IntegrationTestFact]
    public void RenderMode_Directive_SimpleExpression_NotFirst()
    {
        var generated = CompileToCSharp("""
                @code
                {
                    [Parameter]
                    public int Count { get; set; }
                }
                @rendermode @(Microsoft.AspNetCore.Components.Web.RenderMode.InteractiveServer)
                """);

        // Assert
        AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
        AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
        CompileToAssembly(generated);
    }

    [IntegrationTestFact]
    public void RenderMode_Directive_NewExpression()
    {
        var generated = CompileToCSharp("""
                    @rendermode @(new TestComponent.MyRenderMode("This is some text"))

                    @code
                    {
                    #pragma warning disable CS9113
                        public class MyRenderMode(string Text) : Microsoft.AspNetCore.Components.IComponentRenderMode { }
                    }
                    """);

        // Assert
        AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
        AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
        CompileToAssembly(generated);
    }

    [IntegrationTestFact]
    public void RenderMode_Directive_WithNamespaces()
    {
        var generated = CompileToCSharp("""
                @namespace Custom.Namespace

                @rendermode Microsoft.AspNetCore.Components.Web.RenderMode.InteractiveServer
                """);

        // Assert
        AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
        AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
        CompileToAssembly(generated);
    }

    [IntegrationTestFact]
    public void RenderMode_Attribute_With_SimpleIdentifier()
    {
        var generated = CompileToCSharp($"""
                <{ComponentName} @rendermode="Microsoft.AspNetCore.Components.Web.RenderMode.InteractiveServer" />
                """);

        // Assert
        AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
        AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
        CompileToAssembly(generated);
    }

    [IntegrationTestFact]
    public void RenderMode_Attribute_With_Expression()
    {
        var generated = CompileToCSharp($$"""
                <{{ComponentName}} @rendermode="@(new MyRenderMode() { Extra = "Hello" })" />
                @code
                {
                    class MyRenderMode : Microsoft.AspNetCore.Components.IComponentRenderMode
                    {
                        public string Extra {get;set;}
                    }
                }
                """);

        // Assert
        AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
        AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
        CompileToAssembly(generated);
    }

    [IntegrationTestFact]
    public void RenderMode_Attribute_With_Existing_Attributes()
    {
        var generated = CompileToCSharp($$"""
                <{{ComponentName}} P2="abc" @rendermode="Microsoft.AspNetCore.Components.Web.RenderMode.InteractiveServer" P1="def" />

                @code
                {
                    [Parameter]public string P1 {get; set;}

                    [Parameter]public string P2 {get; set;}
                }
                """);

        // Assert
        AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
        AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
        CompileToAssembly(generated);
    }

    [IntegrationTestFact]
    public void Duplicate_RenderMode()
    {
        var generated = CompileToCSharp($$"""
                <{{ComponentName}} @rendermode="Microsoft.AspNetCore.Components.Web.RenderMode.InteractiveServer"
                                   @rendermode="Value2" />
                """);

        // Assert
        AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
        AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
        CompileToAssembly(generated);
    }

    [IntegrationTestFact]
    public void RenderMode_Multiple_Components()
    {
        var generated = CompileToCSharp($$"""
                <{{ComponentName}} @rendermode="Microsoft.AspNetCore.Components.Web.RenderMode.InteractiveServer" />
                <{{ComponentName}} @rendermode="Microsoft.AspNetCore.Components.Web.RenderMode.InteractiveServer" />
                """);

        // Assert
        AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
        AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
        CompileToAssembly(generated);
    }

    [IntegrationTestFact]
    public void RenderMode_Child_Components()
    {
        var generated = CompileToCSharp($$"""
                <{{ComponentName}} @rendermode="Microsoft.AspNetCore.Components.Web.RenderMode.InteractiveServer">
                    <{{ComponentName}} @rendermode="Microsoft.AspNetCore.Components.Web.RenderMode.InteractiveServer">
                        <{{ComponentName}} @rendermode="Microsoft.AspNetCore.Components.Web.RenderMode.InteractiveServer" />
                    </{{ComponentName}}>
                 <{{ComponentName}} @rendermode="Microsoft.AspNetCore.Components.Web.RenderMode.InteractiveServer">
                        <{{ComponentName}} @rendermode="Microsoft.AspNetCore.Components.Web.RenderMode.InteractiveServer" />
                        <{{ComponentName}} @rendermode="Microsoft.AspNetCore.Components.Web.RenderMode.InteractiveServer" />
                    </{{ComponentName}}>
                </{{ComponentName}}>

                @code
                {
                    [Parameter]
                    public RenderFragment ChildContent { get; set; }
                }
                """);

        // Assert
        AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
        AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
        CompileToAssembly(generated);
    }

    [IntegrationTestFact]
    public void RenderMode_With_TypeInference()
    {
        var generated = CompileToCSharp($$"""
                @typeparam TRenderMode where TRenderMode : Microsoft.AspNetCore.Components.IComponentRenderMode

                <{{ComponentName}} @rendermode="RenderModeParam" RenderModeParam="Microsoft.AspNetCore.Components.Web.RenderMode.InteractiveServer" />

                @code
                {
                    [Parameter] public TRenderMode RenderModeParam { get; set;}
                }
                """);

        // Assert
        AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
        AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
        CompileToAssembly(generated);
    }

    [IntegrationTestFact]
    public void RenderMode_With_Ternary()
    {
        var generated = CompileToCSharp($$"""
                <{{ComponentName}} @rendermode="@(true ? Microsoft.AspNetCore.Components.Web.RenderMode.InteractiveServer : null)" />
                """);

        // Assert
        AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
        AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
        CompileToAssembly(generated);
    }

    [IntegrationTestFact, WorkItem("https://github.com/dotnet/razor/issues/9343")]
    public void RenderMode_With_Null_Nullable_Disabled()
    {
        var generated = CompileToCSharp($$"""
                <{{ComponentName}} @rendermode="null" />
                """, nullableEnable: false);

        // Assert
        AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
        AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
        CompileToAssembly(generated);
    }

    [IntegrationTestFact, WorkItem("https://github.com/dotnet/razor/issues/9343")]
    public void RenderMode_With_Null_Nullable_Enabled()
    {
        var generated = CompileToCSharp($$"""
                <{{ComponentName}} @rendermode="null" />
                """, nullableEnable: true);

        // Assert
        AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
        AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
        CompileToAssembly(generated);
    }

    [IntegrationTestFact, WorkItem("https://github.com/dotnet/razor/issues/9343")]
    public void RenderMode_With_Nullable_Receiver()
    {
        var generated = CompileToCSharp($$"""
                @code
                {
                    public class RenderModeContainer
                    {
                        public Microsoft.AspNetCore.Components.IComponentRenderMode RenderMode => Microsoft.AspNetCore.Components.Web.RenderMode.InteractiveServer;
                    }

                    RenderModeContainer? Container => null;
                }
                <{{ComponentName}} @rendermode="@(Container.RenderMode)" />
                """, nullableEnable: true);

        // Assert
        AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
        AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
        CompileToAssembly(generated,
            DesignTime
            // x:\dir\subdir\Test\TestComponent.cshtml(10,29): warning CS8602: Dereference of a possibly null reference.
            //                             Container.RenderMode
            ? Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "Container").WithLocation(10, 29)
            // x:\dir\subdir\Test\TestComponent.cshtml(10,31): warning CS8602: Dereference of a possibly null reference.
            //                             Container.RenderMode
            : Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "Container").WithLocation(10, 31)
            );
    }

    #endregion

    #region FormName

    [IntegrationTestFact, WorkItem("https://github.com/dotnet/razor/issues/9077")]
    public void FormName_HtmlValue()
    {
        // Act
        var generated = CompileToCSharp("""
            @using Microsoft.AspNetCore.Components.Web
            <form method="post" @onsubmit="() => { }" @formname="named-form-handler"></form>
            """);

        // Assert
        AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
        AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
        CompileToAssembly(generated);
    }

    [IntegrationTestFact, WorkItem("https://github.com/dotnet/razor/issues/9077")]
    public void FormName_CSharpValue()
    {
        // Act
        var generated = CompileToCSharp("""
            @using Microsoft.AspNetCore.Components.Web
            <form method="post" @onsubmit="() => { }" @formname="@("named-form-handler")"></form>
            """);

        // Assert
        AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
        AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
        CompileToAssembly(generated);
    }

    [IntegrationTestFact, WorkItem("https://github.com/dotnet/razor/issues/9077")]
    public void FormName_CSharpValue_Integer()
    {
        // Act
        var generated = CompileToCSharp("""
            @using Microsoft.AspNetCore.Components.Web
            <form method="post" @onsubmit="() => { }" @formname="@x"></form>
            @code {
                int x = 1;
            }
            """);

        // Assert
        AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
        AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
        CompileToAssembly(generated,
            // x:\dir\subdir\Test\TestComponent.cshtml(2,55): error CS1503: Argument 1: cannot convert from 'int' to 'string'
            //                                                       x
            Diagnostic(ErrorCode.ERR_BadArgType, "x").WithArguments("1", "int", "string").WithLocation(2, 55));
    }

    [IntegrationTestFact, WorkItem("https://github.com/dotnet/razor/issues/9077")]
    public void FormName_MixedValue()
    {
        // Act
        var generated = CompileToCSharp("""
            @using Microsoft.AspNetCore.Components.Web
            <form method="post" @onsubmit="() => { }" @formname="start @("literal") @x end"></form>
            @code {
                int x = 1;
            }
            """);

        // Assert
        AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
        AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
        CompileToAssembly(generated, DesignTime
            ? [
                // x:\dir\subdir\Test\TestComponent.cshtml(2,74): error CS1503: Argument 1: cannot convert from 'int' to 'string'
                //                                                                          x
                Diagnostic(ErrorCode.ERR_BadArgType, "x").WithArguments("1", "int", "string").WithLocation(2, 74)
               ]
            : []);
    }

    [IntegrationTestFact, WorkItem("https://github.com/dotnet/razor/issues/9077")]
    public void FormName_Nullability()
    {
        // This could report a nullability warning, but that's not currently supported in other places, either.
        // Tracked by https://github.com/dotnet/razor/issues/7398.

        // Act
        var generated = CompileToCSharp("""
            @using Microsoft.AspNetCore.Components.Web
            <form method="post" @onsubmit="() => { }" @formname="@null"></form>
            """,
            nullableEnable: true);

        // Assert
        AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
        AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
        CompileToAssembly(generated);
    }

    [IntegrationTestFact, WorkItem("https://github.com/dotnet/razor/issues/9077")]
    public void FormName_CSharpError()
    {
        // Act
        var generated = CompileToCSharp("""
            @using Microsoft.AspNetCore.Components.Web
            <form method="post" @onsubmit="() => { }" @formname="@x"></form>
            """);

        // Assert
        AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
        AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
        CompileToAssembly(generated,
            // x:\dir\subdir\Test\TestComponent.cshtml(2,55): error CS0103: The name 'x' does not exist in the current context
            //                                                       x
            Diagnostic(ErrorCode.ERR_NameNotInContext, "x").WithArguments("x").WithLocation(2, 55));
    }

    [IntegrationTestFact, WorkItem("https://github.com/dotnet/razor/issues/9077")]
    public void FormName_RazorError()
    {
        // Act
        var generated = CompileToCSharp("""
            @using Microsoft.AspNetCore.Components.Web
            <form method="post" @onsubmit="() => { }" @formname="@{ }"></form>
            """);

        // Assert
        AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
        AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
        CompileToAssembly(generated, DesignTime
            ? [// (41,85): error CS7036: There is no argument given that corresponds to the required parameter 'value' of 'RuntimeHelpers.TypeCheck<T>(T)'
             //             global::Microsoft.AspNetCore.Components.CompilerServices.RuntimeHelpers.TypeCheck<string>();
             Diagnostic(ErrorCode.ERR_NoCorrespondingArgument, "TypeCheck<string>").WithArguments("value", "Microsoft.AspNetCore.Components.CompilerServices.RuntimeHelpers.TypeCheck<T>(T)").WithLocation(41, 85)]
            : [// (41,85): error CS7036: There is no argument given that corresponds to the required parameter 'value' of 'RuntimeHelpers.TypeCheck<T>(T)'
               //             global::Microsoft.AspNetCore.Components.CompilerServices.RuntimeHelpers.TypeCheck<string>();
               Diagnostic(ErrorCode.ERR_NoCorrespondingArgument, "TypeCheck<string>").WithArguments("value", "Microsoft.AspNetCore.Components.CompilerServices.RuntimeHelpers.TypeCheck<T>(T)").WithLocation(37, 105)]
             );
    }

    [IntegrationTestFact, WorkItem("https://github.com/dotnet/razor/issues/9077")]
    public void FormName_NotAForm()
    {
        // Act
        var generated = CompileToCSharp("""
            @using Microsoft.AspNetCore.Components.Web
            <div method="post" @onsubmit="() => { }" @formname="named-form-handler"></div>
            <div method="post" @onsubmit="() => { }" @formname="@("named-form-handler")"></div>
            """);

        // Assert
        AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
        AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
        CompileToAssembly(generated);
    }

    [IntegrationTestFact, WorkItem("https://github.com/dotnet/razor/issues/9077")]
    public void FormName_MissingUsing()
    {
        // Act
        var generated = CompileToCSharp("""
            <form method="post" @onsubmit="() => { }" @formname="named-form-handler"></form>
            <form method="post" @onsubmit="() => { }" @formname="@("named-form-handler")"></form>
            """);

        // Assert
        AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
        AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
    }

    [IntegrationTestFact, WorkItem("https://github.com/dotnet/razor/issues/9077")]
    public void FormName_NotAForm_RazorLangVersion7()
    {
        // Act
        var generated = CompileToCSharp("""
            @using Microsoft.AspNetCore.Components.Web
            <div method="post" @onsubmit="() => { }" @formname="named-form-handler"></div>
            <div method="post" @onsubmit="() => { }" @formname="@("named-form-handler")"></div>
            """,
            configuration: Configuration with { LanguageVersion = RazorLanguageVersion.Version_7_0 });

        // Assert
        AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
        AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
        CompileToAssembly(generated);
    }

    [IntegrationTestFact, WorkItem("https://github.com/dotnet/razor/issues/9077")]
    public void FormName_MissingSubmit()
    {
        // Act
        var generated = CompileToCSharp("""
            @using Microsoft.AspNetCore.Components.Web
            <form method="post" @formname="named-form-handler"></form>
            <form method="post" @formname="@("named-form-handler")"></form>
            """);

        // Assert
        AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
        AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
        CompileToAssembly(generated);
    }

    [IntegrationTestFact, WorkItem("https://github.com/dotnet/razor/issues/9077")]
    public void FormName_FakeSubmit()
    {
        // Act
        var generated = CompileToCSharp("""
            @using Microsoft.AspNetCore.Components.Web
            <form method="post" onsubmit="" @formname="named-form-handler"></form>
            <form method="post" onsubmit="" @formname="@("named-form-handler")"></form>
            """);

        // Assert
        AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
        AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
        CompileToAssembly(generated);
    }

    [IntegrationTestFact, WorkItem("https://github.com/dotnet/razor/issues/9077")]
    public void FormName_Component()
    {
        // Act
        var generated = CompileToCSharp("""
            @using Microsoft.AspNetCore.Components.Web
            <TestComponent method="post" @onsubmit="() => { }" @formname="named-form-handler" />
            <TestComponent method="post" @onsubmit="() => { }" @formname="@("named-form-handler")" />
            """);

        // Assert
        AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
        AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
        CompileToAssembly(generated);
    }

    [IntegrationTestFact, WorkItem("https://github.com/dotnet/razor/issues/9077")]
    public void FormName_Component_RazorLangVersion7()
    {
        // Act
        var generated = CompileToCSharp("""
            @using Microsoft.AspNetCore.Components.Web
            <TestComponent method="post" @onsubmit="() => { }" @formname="named-form-handler" />
            <TestComponent method="post" @onsubmit="() => { }" @formname="@("named-form-handler")" />
            """,
            configuration: Configuration with { LanguageVersion = RazorLanguageVersion.Version_7_0 });

        // Assert
        AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
        AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
        CompileToAssembly(generated);
    }

    [IntegrationTestFact, WorkItem("https://github.com/dotnet/razor/issues/9077")]
    public void FormName_Component_Generic()
    {
        // Act
        var generated = CompileToCSharp("""
            @using Microsoft.AspNetCore.Components.Web
            @typeparam T
            <TestComponent method="post" @onsubmit="() => { }" @formname="named-form-handler" Parameter="1" />
            <TestComponent method="post" @onsubmit="() => { }" @formname="@("named-form-handler")" Parameter="2" />
            @code {
                [Parameter] public T Parameter { get; set; }
            }
            """);

        // Assert
        AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
        AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
    }

    [IntegrationTestFact, WorkItem("https://github.com/dotnet/razor/issues/9077")]
    public void FormName_Component_Generic_RazorLangVersion7()
    {
        // Act
        var generated = CompileToCSharp("""
            @using Microsoft.AspNetCore.Components.Web
            @typeparam T
            <TestComponent method="post" @onsubmit="() => { }" @formname="named-form-handler" Parameter="1" />
            <TestComponent method="post" @onsubmit="() => { }" @formname="@("named-form-handler")" Parameter="2" />
            @code {
                [Parameter] public T Parameter { get; set; }
            }
            """,
            configuration: Configuration with { LanguageVersion = RazorLanguageVersion.Version_7_0 });

        // Assert
        AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
        AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
        CompileToAssembly(generated);
    }

    [IntegrationTestFact, WorkItem("https://github.com/dotnet/razor/issues/9077")]
    public void FormName_Duplicate_HtmlValue()
    {
        // Act
        var generated = CompileToCSharp("""
            @using Microsoft.AspNetCore.Components.Web
            <form method="post" @onsubmit="() => { }" @formname="x" @formname="y"></form>
            """);

        // Assert
        AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
        AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
        CompileToAssembly(generated);
    }

    [IntegrationTestFact, WorkItem("https://github.com/dotnet/razor/issues/9077")]
    public void FormName_Duplicate_CSharpValue()
    {
        // This emits invalid code and no warnings, but that's a pre-existing bug,
        // happens with the following Razor code, too.
        // <input @ref="@a" @ref="@b" />
        // @code {
        //     ElementReference a;
        //     ElementReference b;
        // }

        // Act
        var generated = CompileToCSharp("""
            @using Microsoft.AspNetCore.Components.Web
            <form method="post" @onsubmit="() => { }" @formname="@x" @formname="@y"></form>
            @code {
                string x = "a";
                string y = "b";
            }
            """);

        // Assert
        AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
        AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
        CompileToAssembly(generated);
    }

    [IntegrationTestFact, WorkItem("https://github.com/dotnet/razor/issues/9077")]
    public void FormName_MoreElements_HtmlValue()
    {
        // Act
        var generated = CompileToCSharp("""
            @using Microsoft.AspNetCore.Components.Web
            <form method="post" @onsubmit="() => { }" @formname="x"></form>
            <form method="post" @onsubmit="() => { }" @formname="y"></form>
            """);

        // Assert
        AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
        AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
        CompileToAssembly(generated);
    }

    [IntegrationTestFact, WorkItem("https://github.com/dotnet/razor/issues/9077")]
    public void FormName_MoreElements_CSharpValue()
    {
        // Act
        var generated = CompileToCSharp("""
            @using Microsoft.AspNetCore.Components.Web
            <form method="post" @onsubmit="() => { }" @formname="@x"></form>
            <form method="post" @onsubmit="() => { }" @formname="@y"></form>
            @code {
                string x = "a";
                string y = "b";
            }
            """);

        // Assert
        AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
        AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
        CompileToAssembly(generated);
    }

    [IntegrationTestFact, WorkItem("https://github.com/dotnet/razor/issues/9077")]
    public void FormName_Nested()
    {
        // Act
        var generated = CompileToCSharp("""
            @using Microsoft.AspNetCore.Components.Web
            <form method="post" @onsubmit="() => { }" @formname="1"></form>
            <TestComponent>
                <form method="post" @onsubmit="() => { }" @formname="2"></form>
                <TestComponent>
                    <form method="post" @onsubmit="() => { }" @formname="3"></form>
                </TestComponent>
                <form method="post" @onsubmit="() => { }" @formname="4"></form>
            </TestComponent>
            @code {
                [Parameter] public RenderFragment ChildContent { get; set; }
            }
            """);

        // Assert
        AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
        AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
        CompileToAssembly(generated);
    }

    [IntegrationTestFact, WorkItem("https://github.com/dotnet/razor/issues/9323")]
    public void FormName_ChildContent()
    {
        // Act
        var generated = CompileToCSharp("""
            @using Microsoft.AspNetCore.Components.Web
            <form @formname="myform" class="nice">
                <p>@DateTime.Now</p>
            </form>
            """);

        // Assert
        AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
        AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
        CompileToAssembly(generated);
    }

    [IntegrationTestFact, WorkItem("https://github.com/dotnet/razor/issues/9077")]
    public void FormName_NoAddNamedEventMethod()
    {
        // Arrange
        var componentsDll = BaseCompilation.References.Single(r => r.Display == "Microsoft.AspNetCore.Components (aspnet80)");
        var minimalShim = """
            namespace Microsoft.AspNetCore.Components
            {
                public abstract class ComponentBase
                {
                    protected abstract void BuildRenderTree(Rendering.RenderTreeBuilder __builder);
                }
                namespace Rendering
                {
                    public sealed class RenderTreeBuilder
                    {
                        public void AddMarkupContent(int sequence, string markupContent) { }
                        public void OpenElement(int sequence, string elementName) { }
                        public void AddAttribute(int sequence, string name, string value) { }
                        public void AddAttribute<TArgument>(int sequence, string name, EventCallback<TArgument> value) { }
                        public void CloseElement() { }
                    }
                }
                namespace CompilerServices
                {
                    public static class RuntimeHelpers
                    {
                        public static T TypeCheck<T>(T value) => throw null;
                    }
                }
                namespace Web
                {
                    [EventHandler("onsubmit", typeof(System.EventArgs), true, true)]
                    public static class EventHandlers
                    {
                    }
                }
                [System.AttributeUsage(System.AttributeTargets.Class, AllowMultiple = true, Inherited = true)]
                public sealed class EventHandlerAttribute : System.Attribute
                {
                    public EventHandlerAttribute(string attributeName, System.Type eventArgsType, bool enableStopPropagation, bool enablePreventDefault) { }
                }
                public readonly struct EventCallback
                {
                    public static readonly EventCallbackFactory Factory;
                }
                public readonly struct EventCallback<TValue> { }
                public sealed class EventCallbackFactory
                {
                    public EventCallback<TValue> Create<TValue>(object receiver, System.Action callback) => throw null;
                }
            }
            """;
        var minimalShimRef = CSharpCompilation.Create(
                assemblyName: "Microsoft.AspNetCore.Components",
                options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary))
            .AddSyntaxTrees(Parse(minimalShim))
            .AddReferences(ReferenceUtil.NetLatestAll)
            .EmitToImageReference();
        var baseCompilation = BaseCompilation.ReplaceReference(componentsDll, minimalShimRef);

        // Act
        var generated = CompileToCSharp("""
            @using Microsoft.AspNetCore.Components.Web
            <form method="post" @onsubmit="() => { }" @formname="named-form-handler"></form>
            """,
            baseCompilation: baseCompilation);

        // Assert
        AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
        AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
        CompileToAssembly(generated);
    }

    [IntegrationTestFact, WorkItem("https://github.com/dotnet/razor/issues/9077")]
    public void FormName_RazorLangVersion7()
    {
        // Act
        var generated = CompileToCSharp("""
            @using Microsoft.AspNetCore.Components.Web
            <form method="post" @onsubmit="() => { }" @formname="named-form-handler"></form>
            """,
            configuration: Configuration with { LanguageVersion = RazorLanguageVersion.Version_7_0 });

        // Assert
        AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
        AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
        CompileToAssembly(generated);
    }

    [IntegrationTestFact]
    public void InjectDirective()
    {
        // Act
        var generated = CompileToCSharp("""
            @using System.Text
            @inject string Value1
            @inject       StringBuilder          Value2
            @inject int Value3;
            @inject double Value4

            <div>
                Content
            </div>

            @inject float Value5

            """);

        // Assert
        AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
        AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
        CompileToAssembly(generated);
    }

    [IntegrationTestFact, WorkItem("https://github.com/dotnet/razor/issues/11273")]
    public void SectionDirective_NotAllowed()
    {
        // Verify that @section is not recognized in components and produces appropriate code-gen
        // Act
        var generated = CompileToCSharp("""
            @{ var section = "Section"; }
            @section One { <p>Content</p> }
            """);

        // Assert
        AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
        AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
        CompileToAssembly(generated);
    }

    [IntegrationTestFact, WorkItem("https://github.com/dotnet/razor/issues/11273")]
    public void SectionDirective_NotAllowed_VariableNotDefined()
    {
        // Verify that @section is not recognized in components when the variable is not defined
        // Act
        var generated = CompileToCSharp("""
            @section One { <p>Content</p> }
            """);
                                        
        // Assert
        AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
        AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
        CompileToAssembly(generated,
            DesignTime
                // x:\dir\subdir\Test\TestComponent.cshtml(1,7): error CS0103: The name 'section' does not exist in the current context
                ? [Diagnostic(ErrorCode.ERR_NameNotInContext, "section").WithArguments("section").WithLocation(1, 7)]
                // x:\dir\subdir\Test\TestComponent.cshtml(1,2): error CS0103: The name 'section' does not exist in the current context
                : [Diagnostic(ErrorCode.ERR_NameNotInContext, "section").WithArguments("section").WithLocation(1, 2)]);
    }                                    

    [IntegrationTestFact, WorkItem("https://github.com/dotnet/razor/issues/12663")]
    public void ComponentAttribute_WithDoubleAtEscape()
    {
        // Arrange
        AdditionalSyntaxTrees.Add(Parse(@"
using Microsoft.AspNetCore.Components;

namespace Test
{
    public class MyComponent : ComponentBase
    {
        [Parameter]
        public string Value { get; set; }
    }
}"));

        // Act
        var generated = CompileToCSharp(@"<MyComponent Value=""@@currentCount"" />");

        // Assert
        AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
        AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
        CompileToAssembly(generated);
    }

    #endregion
}
