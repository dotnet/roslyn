// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.AspNetCore.Razor.Language.Components;
using Microsoft.AspNetCore.Razor.Language.Syntax;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.AspNetCore.Razor.Language.IntegrationTests;

public class ComponentDiscoveryIntegrationTest()
    : RazorBaselineIntegrationTestBase(layer: TestProject.Layer.Compiler)
{
    internal override RazorFileKind? FileKind => RazorFileKind.Component;

    internal override bool UseTwoPhaseCompilation => true;

    protected override string GetDirectoryPath(string testName)
        => $"TestFiles/IntegrationTests/{GetType().Name}/{testName}";

    [Fact]
    public void ComponentDiscovery_CanFindComponent_DefinedinCSharp()
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
        var result = CompileToCSharp(string.Empty);

        // Assert
        var context = result.CodeDocument.GetTagHelperContext();
        Assert.NotNull(context);
        Assert.Contains(context.TagHelpers, t => t.Name == "Test.MyComponent");
    }

    [Fact]
    public void ComponentDiscovery_CanFindComponent_WithNamespace_DefinedinCSharp()
    {
        // Arrange
        AdditionalSyntaxTrees.Add(Parse(@"
using Microsoft.AspNetCore.Components;

namespace Test.AnotherNamespace
{
    public class MyComponent : ComponentBase
    {
    }
}
"));

        // Act
        var result = CompileToCSharp(string.Empty);

        // Assert
        var context = result.CodeDocument.GetTagHelperContext();
        Assert.NotNull(context);

        Assert.Contains(context.TagHelpers, t =>
        {
            return t.Name == "Test.AnotherNamespace.MyComponent" &&
                t.IsFullyQualifiedNameMatch;
        });

        Assert.DoesNotContain(context.TagHelpers, t =>
        {
            return t.Name == "Test.AnotherNamespace.MyComponent" &&
                !t.IsFullyQualifiedNameMatch;
        });
    }

    [Fact]
    public void ComponentDiscovery_CanFindComponent_DefinedinCshtml()
    {
        // Arrange

        // Act
        var result = CompileToCSharp("UniqueName.cshtml", cshtmlContent: string.Empty);

        // Assert
        var context = result.CodeDocument.GetTagHelperContext();
        Assert.NotNull(context);

        Assert.Contains(context.TagHelpers, t => t.Name == "Test.UniqueName");
    }

    [Fact]
    public void ComponentDiscovery_CanFindComponent_WithTypeParameter()
    {
        // Arrange

        // Act
        var result = CompileToCSharp("UniqueName.cshtml", cshtmlContent: @"
@typeparam TItem
@functions {
    [Parameter] public TItem Item { get; set; }
}");

        // Assert
        var context = result.CodeDocument.GetTagHelperContext();
        Assert.NotNull(context);

        Assert.Contains(context.TagHelpers, t => t.Name == "Test.UniqueName<TItem>");
    }

    [Fact]
    public void ComponentDiscovery_CanFindComponent_WithTypeParameterAndSemicolon()
    {
        // Arrange

        // Act
        var result = CompileToCSharp("UniqueName.cshtml", cshtmlContent: @"
@typeparam TItem;
@functions {
    [Parameter] public TItem Item { get; set; }
}");

        // Assert
        var context = result.CodeDocument.GetTagHelperContext();
        Assert.NotNull(context);

        Assert.Contains(context.TagHelpers, t => t.Name == "Test.UniqueName<TItem>");
    }

    [Fact]
    public void ComponentDiscovery_CanFindComponent_WithMultipleTypeParameters()
    {
        // Arrange

        // Act
        var result = CompileToCSharp("UniqueName.cshtml", cshtmlContent: @"
@typeparam TItem1
@typeparam TItem2
@typeparam TItem3
@functions {
    [Parameter] public TItem1 Item { get; set; }
}");

        // Assert
        var context = result.CodeDocument.GetTagHelperContext();
        Assert.NotNull(context);

        Assert.Contains(context.TagHelpers, t => t.Name == "Test.UniqueName<TItem1, TItem2, TItem3>");
    }

    [Fact]
    public void ComponentDiscovery_CanFindComponent_WithMultipleTypeParametersAndMixedSemicolons()
    {
        // Arrange

        // Act
        var result = CompileToCSharp("UniqueName.cshtml", cshtmlContent: @"
@typeparam TItem1
@typeparam TItem2;
@typeparam TItem3
@functions {
    [Parameter] public TItem1 Item { get; set; }
}");

        // Assert
        var context = result.CodeDocument.GetTagHelperContext();
        Assert.NotNull(context);

        Assert.Contains(context.TagHelpers, t => t.Name == "Test.UniqueName<TItem1, TItem2, TItem3>");
    }

    [Fact]
    public void ComponentDiscovery_ImportPrecedence_OverExactDocumentNamespace()
    {
        AddImportPrecedenceComponents();

        // Razor prefers an imported component over one in the document's namespace.
        var result = CompileToCSharp("""
            @namespace Test
            @using OtherLibrary

            <MyButton />
            """);

        Assert.Empty(result.RazorDiagnostics);
        AssertCSharpDocumentMatchesBaseline(result.CodeDocument);
        CompileToAssembly(result);
    }

    [Fact]
    public void ComponentDiscovery_ImportPrecedence_OverEnclosingNamespace()
    {
        AddImportPrecedenceComponents();

        // Razor prefers an imported component over one in an enclosing namespace.
        var result = CompileToCSharp("""
            @namespace Test.Nested
            @using OtherLibrary

            <MyButton />
            """);

        Assert.Empty(result.RazorDiagnostics);
        AssertCSharpDocumentMatchesBaseline(result.CodeDocument);
        CompileToAssembly(result);
    }

    [Fact]
    public void ComponentDiscovery_ImportPrecedence_BeforeGenericDisambiguation()
    {
        AdditionalSyntaxTrees.Add(Parse("""
            using Microsoft.AspNetCore.Components;

            namespace Test
            {
                public class GenericButton : ComponentBase
                {
                }
            }

            namespace OtherLibrary
            {
                public class GenericButton<TItem> : ComponentBase
                {
                    [Parameter]
                    public TItem Item { get; set; }
                }
            }
            """));

        // Razor applies namespace import precedence before generic/non-generic disambiguation.
        var result = CompileToCSharp("""
            @namespace Test
            @using OtherLibrary

            <GenericButton Item="@("Hello")" />
            """);

        Assert.Empty(result.RazorDiagnostics);
        AssertCSharpDocumentMatchesBaseline(result.CodeDocument);
        CompileToAssembly(result);
    }

    [Fact]
    public void ComponentDiscovery_ImportPrecedence_MultipleImportsRemainAmbiguous()
    {
        AddImportPrecedenceComponents();

        // Razor reports ambiguity between imports even when the document's namespace has a matching component.
        var result = CompileToCSharp("""
            @namespace Test
            @using OtherLibrary
            @using AnotherLibrary

            <MyButton />
            """);

        var diagnostic = Assert.Single(result.RazorDiagnostics);
        Assert.Equal(ComponentDiagnosticFactory.MultipleComponents.Id, diagnostic.Id);
        AssertCSharpDocumentMatchesBaseline(result.CodeDocument);
    }

    [Fact]
    public void ComponentDiscovery_AmbiguousGenericArity_DoesNotStopLowering()
    {
        AdditionalSyntaxTrees.Add(Parse("""
            using Microsoft.AspNetCore.Components;

            namespace Components.Library
            {
                public class Widget<TItem> : ComponentBase
                {
                }

                public class Widget<TItem, TOther> : ComponentBase
                {
                }

                public class Subsequent : ComponentBase
                {
                }
            }
            """));

        var result = CompileToCSharp("""
            @using Components.Library

            <Widget />
            <Subsequent />
            """);

        var diagnostic = Assert.Single(result.RazorDiagnostics);
        Assert.Equal(ComponentDiagnosticFactory.MultipleComponents.Id, diagnostic.Id);

        // An ambiguous tag must not prevent unrelated components from being lowered.
        Assert.Contains("OpenComponent<global::Components.Library.Subsequent>", result.Code);
    }

    [Fact]
    public void ComponentDiscovery_DisambiguatedGenericArity_LowersSubsequentComponent()
    {
        AdditionalSyntaxTrees.Add(Parse("""
            using Microsoft.AspNetCore.Components;

            namespace Components.Library
            {
                public class Widget<TItem> : ComponentBase
                {
                }

                public class Widget<TItem, TOther> : ComponentBase
                {
                }

                public class Subsequent : ComponentBase
                {
                }
            }
            """));

        // Explicit type parameters provide a valid control for the ambiguous component case.
        var result = CompileToCSharp("""
            @using Components.Library

            <Widget TItem="string" TOther="int" />
            <Subsequent />
            """);

        Assert.Empty(result.RazorDiagnostics);
        Assert.Contains("OpenComponent<global::Components.Library.Subsequent>", result.Code);
        CompileToAssembly(result);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/86373")]
    public void ComponentDiscovery_GenericComponentInheritance_DoesNotReportDuplicateParameters()
    {
        AdditionalRazorItems.Add(CreateProjectItem("Foo.razor", """
            @using Microsoft.AspNetCore.Components
            @typeparam T1
            @typeparam T2

            @code
            {
                [Parameter]
                public T1 First { get; set; }

                [Parameter]
                public T2 Second { get; set; }
            }
            """, RazorFileKind.Component));

        AdditionalSyntaxTrees.Add(Parse("""
            namespace Test
            {
                public class Foo<T> : Foo<T, T>
                {
                }
            }
            """));

        var result = CompileToCSharp(cshtmlRelativePath: "Home.razor", cshtmlContent: """
            @using Test

            <Foo T="int" First="42" Second="600" />
            """);

        Assert.Empty(result.RazorDiagnostics);
        CompileToAssembly(result);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/86373")]
    public void ComponentDiscovery_GenericComponentInheritance_ReportsParametersFromUnselectedCandidate()
    {
        AdditionalRazorItems.Add(CreateProjectItem("Foo.razor", """
            @using Microsoft.AspNetCore.Components
            @typeparam T1
            @typeparam T2

            @code
            {
                [Parameter]
                public T1 First { get; set; }

                [Parameter]
                public T2 Second { get; set; }
            }
            """, RazorFileKind.Component));

        AdditionalSyntaxTrees.Add(Parse("""
            using Microsoft.AspNetCore.Components;

            namespace Test
            {
                public class Foo<T> : Foo<T, T>
                {
                    [Parameter]
                    public string Extra { get; set; }
                }
            }
            """));

        var result = CompileToCSharp(
            cshtmlRelativePath: "Home.razor",
            cshtmlContent: """
                @using Test

                <Foo T1="int" T2="int" First="42" Second="600" Extra="extra" />
                """,
            configuration: Configuration with { RazorWarningLevel = 11 });

        var diagnostic = Assert.Single(result.RazorDiagnostics);
        Assert.Same(ComponentDiagnosticFactory.UnknownComponentParameter.Id, diagnostic.Id);
        CompileToAssembly(result);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/86373")]
    public void ComponentDiscovery_GenericComponentInheritance_CapturesUnmatchedAttributeValues()
    {
        AdditionalSyntaxTrees.Add(Parse("""
            using System.Collections.Generic;
            using Microsoft.AspNetCore.Components;

            namespace Test
            {
                public class Foo<T1, T2> : ComponentBase
                {
                    [Parameter(CaptureUnmatchedValues = true)]
                    public Dictionary<string, object> Attributes { get; set; }
                }

                public class Foo<T> : Foo<T, T>
                {
                    [Parameter] public int Extra { get; set; }
                    [Parameter] public int Number { get; set; }
                    [Parameter] public int Empty { get; set; }
                    [Parameter] public bool Flag { get; set; }
                }
            }
            """));

        var result = CompileToCSharp("""
            @using Test

            <Foo T1="int" T2="string" Extra="hello" Number="@answer" Empty="" Flag />

            @code {
                private int answer = 42;
            }
            """);

        Assert.Empty(result.RazorDiagnostics);
        Assert.Contains("\"hello\"", result.Code);
        CompileToAssembly(result);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/86373")]
    public void ComponentDiscovery_GenericComponentInheritance_CapturesAttributeMatchedByMultipleCandidates()
    {
        AdditionalSyntaxTrees.Add(Parse("""
            using System.Collections.Generic;
            using Microsoft.AspNetCore.Components;

            namespace Test
            {
                public class Foo<T1, T2, T3> : ComponentBase
                {
                    [Parameter(CaptureUnmatchedValues = true)]
                    public Dictionary<string, object> Attributes { get; set; }
                }

                public class Foo<T1, T2> : Foo<T1, T2, T2>
                {
                    [Parameter] public string Extra { get; set; }
                }

                public class Foo<T> : Foo<T, T>
                {
                }
            }
            """));

        var result = CompileToCSharp("""
            @using Test

            <Foo T1="int" T2="string" T3="bool" Extra="hello" />
            """);

        Assert.Empty(result.RazorDiagnostics);
        CompileToAssembly(result);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/86373")]
    public void ComponentDiscovery_GenericComponentInheritance_ReportsRepeatedUnmatchedAttribute()
    {
        AdditionalSyntaxTrees.Add(Parse("""
            using System.Collections.Generic;
            using Microsoft.AspNetCore.Components;

            namespace Test
            {
                public class Foo<T1, T2, T3> : ComponentBase
                {
                    [Parameter(CaptureUnmatchedValues = true)]
                    public Dictionary<string, object> Attributes { get; set; }
                }

                public class Foo<T1, T2> : Foo<T1, T2, T2>
                {
                    [Parameter] public string Extra { get; set; }
                }

                public class Foo<T> : Foo<T, T>
                {
                }
            }
            """));

        var result = CompileToCSharp("""
            @using Test

            <Foo T1="int" T2="string" T3="bool" Extra="hello" Extra="again" />
            """);

        var diagnostic = Assert.Single(result.RazorDiagnostics);
        Assert.Same(ComponentDiagnosticFactory.DuplicateComponentParameter.Id, diagnostic.Id);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/86373")]
    public void ComponentDiscovery_GenericComponentInheritance_ReportsComplexUnmatchedAttribute()
    {
        AdditionalSyntaxTrees.Add(Parse("""
            using System.Collections.Generic;
            using Microsoft.AspNetCore.Components;

            namespace Test
            {
                public class Foo<T1, T2> : ComponentBase
                {
                    [Parameter(CaptureUnmatchedValues = true)]
                    public Dictionary<string, object> Attributes { get; set; }
                }

                public class Foo<T> : Foo<T, T>
                {
                    [Parameter] public string Extra { get; set; }
                }
            }
            """));

        var content = """
            @using Test

            <Foo T1="int" T2="string" Extra="prefix @answer" />

            @code {
                private int answer = 42;
            }
            """;
        var result = CompileToCSharp(content);

        var diagnostic = Assert.Single(result.RazorDiagnostics);
        Assert.Same(ComponentDiagnosticFactory.UnsupportedComplexContent.Id, diagnostic.Id);
        Assert.NotNull(diagnostic.Span.FilePath);
        Assert.Equal(result.CodeDocument.Source.Text.ToString().IndexOf("prefix @answer", StringComparison.Ordinal), diagnostic.Span.AbsoluteIndex);
        Assert.Equal("prefix @answer".Length, diagnostic.Span.Length);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/86373")]
    public void ComponentDiscovery_GenericComponentInheritance_ReportsCodeBlockInUnmatchedAttribute()
    {
        AdditionalSyntaxTrees.Add(Parse("""
            using System.Collections.Generic;
            using Microsoft.AspNetCore.Components;

            namespace Test
            {
                public class Foo<T1, T2> : ComponentBase
                {
                    [Parameter(CaptureUnmatchedValues = true)]
                    public Dictionary<string, object> Attributes { get; set; }
                }

                public class Foo<T> : Foo<T, T>
                {
                    [Parameter] public string Extra { get; set; }
                }
            }
            """));

        var content = """
            @using Test

            <Foo T1="int" T2="string" Extra="@{ var x = 1; }" />
            """;
        var result = CompileToCSharp(content);

        var diagnostic = Assert.Single(result.RazorDiagnostics);
        Assert.Same(ComponentDiagnosticFactory.UnsupportedComplexContent.Id, diagnostic.Id);
        Assert.NotNull(diagnostic.Span.FilePath);
        Assert.Equal(result.CodeDocument.Source.Text.ToString().IndexOf("@{ var x = 1; }", StringComparison.Ordinal), diagnostic.Span.AbsoluteIndex);
        Assert.Equal("@{ var x = 1; }".Length, diagnostic.Span.Length);
    }

    [Fact]
    public void UnusedUsingDirectives_IsDirectiveUsed_ReturnsFalseForUnusedUsing()
    {
        // Arrange
        AdditionalSyntaxTrees.Add(Parse("""
            using Microsoft.AspNetCore.Components;

            namespace Components.Library
            {
                public class MyButton : ComponentBase
                {
                }
            }

            namespace Some.Unrelated.Namespace
            {
                public class Placeholder { }
            }
            """));

        // Act
        var result = CompileToCSharp("""
            @using Components.Library
            @using Some.Unrelated.Namespace

            <MyButton />
            """);

        // Assert
        var directives = result.CodeDocument.GetRequiredSyntaxTree().Root.DescendantNodes().OfType<BaseRazorDirectiveSyntax>().ToImmutableArray();
        Assert.True(result.CodeDocument.IsDirectiveUsed(directives[0]));
        Assert.False(result.CodeDocument.IsDirectiveUsed(directives[1]));
    }

    [Fact]
    public void DirectiveTagHelperContributions_AreStoredOnCodeDocument_ForUsings()
    {
        // Arrange
        AdditionalSyntaxTrees.Add(Parse("""
            using Microsoft.AspNetCore.Components;

            namespace Components.Library
            {
                public class MyButton : ComponentBase
                {
                }
            }
            """));

        // Act
        var result = CompileToCSharp("""
            @using Components.Library
            @using System.Text

            <MyButton />
            """);

        // Assert
        var contributions = result.CodeDocument.GetDirectiveTagHelperContributions();
        Assert.Equal(2, contributions.Length);
        var directives = result.CodeDocument.GetRequiredSyntaxTree().Root.DescendantNodes().OfType<BaseRazorDirectiveSyntax>().ToImmutableArray();
        Assert.Equal([directives[0].SpanStart, directives[1].SpanStart], contributions.Select(c => c.DirectiveSpanStart));
        Assert.Single(contributions, c => !c.ContributedTagHelpers.IsEmpty);
        Assert.Single(contributions, c => c.ContributedTagHelpers.IsEmpty);
    }

    [Fact]
    public void UnusedUsingDirectives_FullyQualifiedComponent_AllUsingsUnused()
    {
        // Arrange
        AdditionalSyntaxTrees.Add(Parse("""
            using Microsoft.AspNetCore.Components;

            namespace Components.Library
            {
                public class MyButton : ComponentBase
                {
                }
            }

            namespace Some.Unrelated.Namespace
            {
                public class Placeholder { }
            }
            """));

        // Act
        // When the component is fully qualified, the @using directives are not needed
        // for resolution — the FQ tag helper descriptor is used instead.
        var result = CompileToCSharp("""
            @using Components.Library
            @using Some.Unrelated.Namespace

            <Components.Library.MyButton />
            """);

        // Assert
        var directives = result.CodeDocument.GetRequiredSyntaxTree().Root.DescendantNodes().OfType<BaseRazorDirectiveSyntax>().ToImmutableArray();
        Assert.False(result.CodeDocument.IsDirectiveUsed(directives[0]));
        Assert.False(result.CodeDocument.IsDirectiveUsed(directives[1]));
    }

    private void AddImportPrecedenceComponents()
    {
        AdditionalSyntaxTrees.Add(Parse("""
            using Microsoft.AspNetCore.Components;

            namespace Test
            {
                public class MyButton : ComponentBase
                {
                }
            }

            namespace OtherLibrary
            {
                public class MyButton : ComponentBase
                {
                }
            }

            namespace AnotherLibrary
            {
                public class MyButton : ComponentBase
                {
                }
            }
            """));
    }
}
