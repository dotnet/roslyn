// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;
using System.Linq;
using Microsoft.AspNetCore.Razor.Language.Components;
using Microsoft.AspNetCore.Razor.Language.Syntax;
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
