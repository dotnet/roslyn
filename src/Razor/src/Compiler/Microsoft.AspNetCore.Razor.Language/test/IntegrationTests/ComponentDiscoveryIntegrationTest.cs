// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Linq;
using Microsoft.AspNetCore.Razor.Language.Syntax;
using Xunit;

namespace Microsoft.AspNetCore.Razor.Language.IntegrationTests;

public class ComponentDiscoveryIntegrationTest : RazorIntegrationTestBase
{
    internal override RazorFileKind? FileKind => RazorFileKind.Component;

    internal override bool UseTwoPhaseCompilation => true;

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
        var directives = result.CodeDocument.GetRequiredSyntaxTree().Root.DescendantNodes().OfType<BaseRazorDirectiveSyntax>().ToArray();
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
        var directives = result.CodeDocument.GetRequiredSyntaxTree().Root.DescendantNodes().OfType<BaseRazorDirectiveSyntax>().ToArray();
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
        var directives = result.CodeDocument.GetRequiredSyntaxTree().Root.DescendantNodes().OfType<BaseRazorDirectiveSyntax>().ToArray();
        Assert.False(result.CodeDocument.IsDirectiveUsed(directives[0]));
        Assert.False(result.CodeDocument.IsDirectiveUsed(directives[1]));
    }

}
