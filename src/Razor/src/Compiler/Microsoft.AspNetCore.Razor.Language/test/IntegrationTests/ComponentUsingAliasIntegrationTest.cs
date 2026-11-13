// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.AspNetCore.Razor.Language.Components;
using Microsoft.AspNetCore.Razor.Language.Syntax;
using Microsoft.AspNetCore.Razor.Test.Common;
using Microsoft.CodeAnalysis.Razor;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.AspNetCore.Razor.Language.IntegrationTests;

public class ComponentUsingAliasIntegrationTest()
    : RazorBaselineIntegrationTestBase(TestProject.Layer.Compiler)
{
    internal override RazorFileKind? FileKind => RazorFileKind.Component;

    internal override bool UseTwoPhaseCompilation => true;

    protected override string GetDirectoryPath(string testName)
        => $"TestFiles/IntegrationTests/ComponentUsingAliasIntegrationTest/{testName}";

    [Fact]
    [WorkItem("https://github.com/dotnet/roslyn/issues/85411")]
    public void ComponentDiscovery_UsingAlias_ExplicitDescriptorsWithoutReferences()
    {
        var builder = TagHelperDescriptorBuilder.Create(TagHelperKind.Component, "Library.Button", "Library");
        builder.SetTypeName("Library.Button", "Library", "Button");
        builder.TagMatchingRule(rule => rule.TagName = "Button");
        var descriptors = TagHelperCollection.Create([builder.Build()]);
        var engine = RazorProjectEngine.Create(new RazorConfiguration(RazorLanguageVersion.Version_12_0, "test", Extensions: []), RazorProjectFileSystem.Empty, builder =>
        {
            builder.Features.Add(new CompilationTagHelperFeature());
        });
        var source = RazorSourceDocument.Create("""
            @using UI = Library
            <UI.Button />
            """, "Index.razor");

        var document = engine.Process(source, RazorFileKind.Component, [], descriptors);

        Assert.Contains("OpenComponent<global::Library.Button>", document.GetRequiredCSharpDocument(declarationDocument: false).Text.ToString());
        Assert.Null(document.GetComponentAliasTargets());
    }

    [Fact]
    [WorkItem("https://github.com/dotnet/roslyn/issues/85411")]
    public void ComponentDiscovery_UsingTypeAlias_EscapedTarget_ExplicitDescriptorsWithoutReferences()
    {
        var builder = TagHelperDescriptorBuilder.Create(TagHelperKind.Component, "Library.@namespace.@class", "Library");
        builder.SetTypeName("Library.@namespace.@class", "Library.@namespace", "class");
        builder.TagMatchingRule(rule => rule.TagName = "class");
        var descriptors = TagHelperCollection.Create([builder.Build()]);
        var engine = RazorProjectEngine.Create(new RazorConfiguration(RazorLanguageVersion.Version_12_0, "test", Extensions: []), RazorProjectFileSystem.Empty, builder =>
        {
            builder.Features.Add(new CompilationTagHelperFeature());
        });
        var source = RazorSourceDocument.Create("""
            @using Button = Library.@namespace.@class
            <Button />
            """, "Index.razor");

        var document = engine.Process(source, RazorFileKind.Component, [], descriptors);

        Assert.Null(document.GetComponentAliasTargets());
        Assert.Contains("OpenComponent<global::Library.@namespace.@class>", document.GetRequiredCSharpDocument(declarationDocument: false).Text.ToString());
    }

    [Fact]
    [WorkItem("https://github.com/dotnet/roslyn/issues/85411")]
    public void ComponentDiscovery_UsingAlias_UsageDoesNotLeakBetweenDocuments()
    {
        var builder = TagHelperDescriptorBuilder.Create(TagHelperKind.Component, "Library.Button", "Library");
        builder.SetTypeName("Library.Button", "Library", "Button");
        builder.TagMatchingRule(rule => rule.TagName = "Button");
        var descriptors = TagHelperCollection.Create([builder.Build()]);
        var engine = RazorProjectEngine.Create(new RazorConfiguration(RazorLanguageVersion.Version_12_0, "test", Extensions: []), RazorProjectFileSystem.Empty);
        var used = engine.Process(RazorSourceDocument.Create("""
            @using Library
            @using UI = Library
            <UI.Button />
            """, "Used.razor"), RazorFileKind.Component, [], descriptors);
        var unused = engine.Process(RazorSourceDocument.Create("""
            @using Library
            @using UI = Library
            <Button />
            """, "Unused.razor"), RazorFileKind.Component, [], descriptors);
        var usedDirective = used.GetRequiredSyntaxTree().Root.DescendantNodes().OfType<RazorUsingDirectiveSyntax>().ToImmutableArray()[^1];
        var unusedDirective = unused.GetRequiredSyntaxTree().Root.DescendantNodes().OfType<RazorUsingDirectiveSyntax>().ToImmutableArray()[^1];

        // Sharing immutable binding inputs must not share evidence about which tags a document actually uses.
        Assert.Same(used.GetRequiredTagHelperContext(), unused.GetRequiredTagHelperContext());
        Assert.True(used.IsDirectiveUsed(usedDirective));
        Assert.False(unused.IsDirectiveUsed(unusedDirective));
    }

    [Fact]
    [WorkItem("https://github.com/dotnet/roslyn/issues/85411")]
    public void ComponentDiscovery_UsingAlias_DoesNotMarkOrdinaryImportUsed()
    {
        var document = CreateAliasUsageTestDocument("""
            @using Library
            @using UI = global::Library
            <UI.Button />
            """);
        var directive = document.GetRequiredSyntaxTree().Root.DescendantNodes().OfType<RazorUsingDirectiveSyntax>().ToImmutableArray()[0];

        Assert.False(document.IsDirectiveUsed(directive));
    }

    [Fact]
    [WorkItem("https://github.com/dotnet/roslyn/issues/85411")]
    public void ComponentDiscovery_UsingAlias_SameNameTypeAliasDoesNotMarkOrdinaryImportUsed()
    {
        var document = CreateAliasUsageTestDocument("""
            @using Library
            @using Button = global::Library.Button
            <Button />
            """);
        var directive = document.GetRequiredSyntaxTree().Root.DescendantNodes().OfType<RazorUsingDirectiveSyntax>().ToImmutableArray()[0];

        Assert.False(document.IsDirectiveUsed(directive));
    }

    [Fact]
    [WorkItem("https://github.com/dotnet/roslyn/issues/85411")]
    public void ComponentDiscovery_UsingAlias_OrdinaryTagStillMarksImportUsed()
    {
        var document = CreateAliasUsageTestDocument("""
            @using Library
            @using UI = global::Library
            <UI.Button />
            <Button />
            """);
        var directive = document.GetRequiredSyntaxTree().Root.DescendantNodes().OfType<RazorUsingDirectiveSyntax>().ToImmutableArray()[0];

        Assert.True(document.IsDirectiveUsed(directive));
    }

    [Fact]
    [WorkItem("https://github.com/dotnet/roslyn/issues/85411")]
    public void ComponentDiscovery_UsingAlias_ToolingQueriesDoNotMarkDirectiveUsed()
    {
        var builder = TagHelperDescriptorBuilder.Create(TagHelperKind.Component, "Library.Button", "Library");
        builder.SetTypeName("Library.Button", "Library", "Button");
        builder.TagMatchingRule(rule => rule.TagName = "Button");
        var descriptors = TagHelperCollection.Create([builder.Build()]);
        var engine = RazorProjectEngine.Create(new RazorConfiguration(RazorLanguageVersion.Version_12_0, "test", Extensions: []), RazorProjectFileSystem.Empty);
        var document = engine.Process(RazorSourceDocument.Create("""
            @using Library
            @using UI = Library
            <Button />
            """, "Index.razor"), RazorFileKind.Component, [], descriptors);
        var directive = document.GetRequiredSyntaxTree().Root.DescendantNodes().OfType<RazorUsingDirectiveSyntax>().ToImmutableArray()[^1];

        // Completion and other tooling can ask about a tag that isn't present in the document.
        Assert.NotNull(document.GetRequiredTagHelperContext().GetBinder().GetBinding("UI.Button", [], parentTagName: null, parentIsTagHelper: false));
        Assert.False(document.IsDirectiveUsed(directive));
    }

    [Fact]
    [WorkItem("https://github.com/dotnet/roslyn/issues/85411")]
    public void ComponentDiscovery_UsingAlias_RemovingUsageDoesNotKeepDirectiveUsed()
    {
        var builder = TagHelperDescriptorBuilder.Create(TagHelperKind.Component, "Library.Button", "Library");
        builder.SetTypeName("Library.Button", "Library", "Button");
        builder.TagMatchingRule(rule => rule.TagName = "Button");
        var descriptors = TagHelperCollection.Create([builder.Build()]);
        var engine = RazorProjectEngine.Create(new RazorConfiguration(RazorLanguageVersion.Version_12_0, "test", Extensions: []), RazorProjectFileSystem.Empty);
        var original = engine.Process(RazorSourceDocument.Create("""
            @using Library
            @using UI = Library
            <UI.Button />
            """, "Index.razor"), RazorFileKind.Component, [], descriptors);
        var updated = engine.Process(RazorSourceDocument.Create("""
            @using Library
            @using UI = Library
            <Button />
            """, "Index.razor"), RazorFileKind.Component, [], descriptors);
        var originalDirective = original.GetRequiredSyntaxTree().Root.DescendantNodes().OfType<RazorUsingDirectiveSyntax>().ToImmutableArray()[^1];
        var updatedDirective = updated.GetRequiredSyntaxTree().Root.DescendantNodes().OfType<RazorUsingDirectiveSyntax>().ToImmutableArray()[^1];

        Assert.True(original.IsDirectiveUsed(originalDirective));
        Assert.False(updated.IsDirectiveUsed(updatedDirective));
    }

    [Fact]
    [WorkItem("https://github.com/dotnet/roslyn/issues/85411")]
    public void ComponentDiscovery_UsingAlias_ExplicitDescriptorsDoNotOverrideUnresolvedTargets()
    {
        var builder = TagHelperDescriptorBuilder.Create(TagHelperKind.Component, "Library.Button", "Library");
        builder.SetTypeName("Library.Button", "Library", "Button");
        builder.TagMatchingRule(rule => rule.TagName = "Button");
        var descriptors = TagHelperCollection.Create([builder.Build()]);
        var engine = RazorProjectEngine.Create(new RazorConfiguration(RazorLanguageVersion.Version_12_0, "test", Extensions: []), RazorProjectFileSystem.Empty, builder =>
        {
            builder.Features.Add(new CompilationTagHelperFeature());
            builder.Features.Add(new DefaultMetadataReferenceFeature { References = ReferenceUtil.NetLatestAll });
        });
        var source = RazorSourceDocument.Create("""
            @using UI = Library
            <UI.Button />
            """, "Index.razor");

        var document = engine.Process(source, RazorFileKind.Component, [], descriptors);

        Assert.DoesNotContain("OpenComponent<global::Library.Button>", document.GetRequiredCSharpDocument(declarationDocument: false).Text.ToString());
        Assert.NotNull(document.GetComponentAliasTargets());
    }

    [Fact]
    [WorkItem("https://github.com/dotnet/roslyn/issues/85411")]
    public void ComponentDiscovery_UsingAlias_NonComponentNamespaceShadowsOuterComponent()
    {
        AdditionalSyntaxTrees.Add(Parse("""
            namespace Library { public class Button : Microsoft.AspNetCore.Components.ComponentBase { } }
            namespace MyApp.Library { public class Helper { } }
            """));

        var result = CompileToCSharp("Index.razor", cshtmlContent: """
            @namespace MyApp
            @using UI = Library
            <UI.Button />
            """);

        Assert.DoesNotContain("OpenComponent<global::Library.Button>", result.Code);
    }

    [Fact]
    [WorkItem("https://github.com/dotnet/roslyn/issues/85411")]
    public void ComponentDiscovery_UsingAlias_NonComponentTypeShadowsOuterComponent()
    {
        AdditionalSyntaxTrees.Add(Parse("""
            namespace Library { public class Button : Microsoft.AspNetCore.Components.ComponentBase { } }
            namespace MyApp { public class Library { } }
            """));

        var result = CompileToCSharp("Index.razor", cshtmlContent: """
            @namespace MyApp
            @using UI = Library
            <UI.Button />
            """);

        Assert.DoesNotContain("OpenComponent<global::Library.Button>", result.Code);
    }

    [Fact]
    [WorkItem("https://github.com/dotnet/roslyn/issues/85411")]
    public void ComponentDiscovery_UsingAlias_GlobalTargetBypassesNonComponentNamespace()
    {
        AdditionalSyntaxTrees.Add(Parse("""
            namespace Library { public class Button : Microsoft.AspNetCore.Components.ComponentBase { } }
            namespace MyApp.Library { public class Helper { } }
            """));

        var result = CompileToCSharp("Index.razor", cshtmlContent: """
            @namespace MyApp
            @using UI = global::Library
            <UI.Button />
            """);

        Assert.Contains("OpenComponent<global::Library.Button>", result.Code);
    }

    [Fact]
    [WorkItem("https://github.com/dotnet/roslyn/issues/85411")]
    public void ComponentDiscovery_UsingTypeAlias()
    {
        var result = CompileAliasedButton("""
            @using Button = Components.Library.MyButton
            <Button @bind-Title="title">
                <ChildContent Context="text">@text</ChildContent>
            </Button>
            @code {
                private string title = "Hello";
            }
            """);

        Assert.Contains("OpenComponent<global::Components.Library.MyButton>", result.Code);
        AssertUsingAliasIsUsed(result.CodeDocument);
        AssertDocumentNodeMatchesBaseline(result.CodeDocument);
        AssertCSharpDocumentMatchesBaseline(result.CodeDocument);
    }

    [Fact]
    [WorkItem("https://github.com/dotnet/roslyn/issues/85411")]
    public void ComponentDiscovery_UsingTypeAlias_EscapedTarget()
    {
        AdditionalSyntaxTrees.Add(Parse("""
            namespace Library
            {
                public class @class : Microsoft.AspNetCore.Components.ComponentBase { }
            }
            """));

        var result = CompileToCSharp("""
            @using Button = Library.@class
            <Button />
            """);

        Assert.NotNull(result.CodeDocument.GetComponentAliasTargets());
        Assert.Empty(result.RazorDiagnostics);
        Assert.Contains("OpenComponent<global::Library.@class>", result.Code);
        AssertUsingAliasIsUsed(result.CodeDocument);
        CompileToAssembly(result);
    }

    [Fact]
    [WorkItem("https://github.com/dotnet/roslyn/issues/85411")]
    public void ComponentDiscovery_UsingTypeAlias_RelativeTarget()
    {
        var result = CompileAliasedButton("""
            @namespace Components
            @using Button = Library.MyButton
            <Button @bind-Title="title">
                <ChildContent Context="text">@text</ChildContent>
            </Button>
            @code {
                private string title = "Hello";
            }
            """);

        AssertUsingAliasIsUsed(result.CodeDocument);
    }

    [Fact]
    [WorkItem("https://github.com/dotnet/roslyn/issues/85411")]
    public void ComponentDiscovery_UsingNamespaceAlias_RelativeTargetInEnclosingNamespace()
    {
        var result = CompileAliasedButton("""
            @namespace Components.Pages
            @using UI = Library
            <UI.MyButton @bind-Title="title">
                <ChildContent Context="text">@text</ChildContent>
            </UI.MyButton>
            @code {
                private string title = "Hello";
            }
            """);

        AssertUsingAliasIsUsed(result.CodeDocument);
    }

    [Fact]
    [WorkItem("https://github.com/dotnet/roslyn/issues/85411")]
    public void ComponentDiscovery_UsingTypeAlias_GlobalTarget()
    {
        var result = CompileAliasedButton("""
            @using Button = global::Components.Library.MyButton
            <Button @bind-Title="title">
                <ChildContent Context="text">@text</ChildContent>
            </Button>
            @code {
                private string title = "Hello";
            }
            """);

        Assert.Contains("OpenComponent<global::Components.Library.MyButton>", result.Code);
        AssertUsingAliasIsUsed(result.CodeDocument);
    }

    [Fact]
    [WorkItem("https://github.com/dotnet/roslyn/issues/85411")]
    public void ComponentDiscovery_UsingNamespaceAlias()
    {
        var result = CompileAliasedButton("""
            @using UI = Components.Library
            <UI.MyButton @bind-Title="title">
                <ChildContent Context="text">@text</ChildContent>
            </UI.MyButton>
            @code {
                private string title = "Hello";
            }
            """);

        Assert.Contains("OpenComponent<global::Components.Library.MyButton>", result.Code);
        AssertUsingAliasIsUsed(result.CodeDocument);
        AssertDocumentNodeMatchesBaseline(result.CodeDocument);
        AssertCSharpDocumentMatchesBaseline(result.CodeDocument);
    }

    [Fact]
    [WorkItem("https://github.com/dotnet/roslyn/issues/85411")]
    public void ComponentDiscovery_UsingNamespaceAlias_GlobalTargetWithSubnamespace()
    {
        var result = CompileAliasedButton("""
            @using UI = global::Components
            <UI.Library.MyButton @bind-Title="title">
                <ChildContent Context="text">@text</ChildContent>
            </UI.Library.MyButton>
            @code {
                private string title = "Hello";
            }
            """);

        Assert.Contains("OpenComponent<global::Components.Library.MyButton>", result.Code);
        AssertUsingAliasIsUsed(result.CodeDocument);
    }

    [Fact]
    [WorkItem("https://github.com/dotnet/roslyn/issues/85411")]
    public void ComponentDiscovery_UsingTypeAlias_FromImports()
    {
        ImportItems.Add(CreateProjectItem("_Imports.razor", "@using Button = Components.Library.MyButton", RazorFileKind.ComponentImport));
        var result = CompileAliasedButton("""
            <Button @bind-Title="title">
                <ChildContent Context="text">@text</ChildContent>
            </Button>
            @code {
                private string title = "Hello";
            }
            """);

        Assert.Contains("OpenComponent<global::Components.Library.MyButton>", result.Code);
    }

    [Fact]
    [WorkItem("https://github.com/dotnet/roslyn/issues/85411")]
    public void ComponentDiscovery_UsingNamespaceAlias_FromImports()
    {
        ImportItems.Add(CreateProjectItem("_Imports.razor", "@using UI = Components.Library", RazorFileKind.ComponentImport));
        var result = CompileAliasedButton("""
            <UI.MyButton @bind-Title="title">
                <ChildContent Context="text">@text</ChildContent>
            </UI.MyButton>
            @code {
                private string title = "Hello";
            }
            """);

        Assert.Contains("OpenComponent<global::Components.Library.MyButton>", result.Code);
    }

    [Fact]
    [WorkItem("https://github.com/dotnet/roslyn/issues/85411")]
    public void ComponentDiscovery_UsingAlias_MultipleAliasesPreserveOrdinaryNames()
    {
        AdditionalSyntaxTrees.Add(Parse("""
            namespace Components.Library
            {
                public class MyButton : Microsoft.AspNetCore.Components.ComponentBase { }
            }
            """));

        var result = CompileToCSharp("""
            @using Components.Library
            @using First = Components.Library.MyButton
            @using Second = Components.Library.MyButton
            @using UI = Components.Library
            @using Unused = Components.Library.MyButton

            <First />
            <Second />
            <UI.MyButton />
            <MyButton />
            <Components.Library.MyButton />
            """);

        Assert.Empty(result.RazorDiagnostics);
        AssertCSharpDocumentMatchesBaseline(result.CodeDocument);
        CompileToAssembly(result);

        var directives = result.CodeDocument.GetRequiredSyntaxTree().Root.DescendantNodes().OfType<RazorUsingDirectiveSyntax>().ToImmutableArray();
        Assert.All(directives.Take(4), directive => Assert.True(result.CodeDocument.IsDirectiveUsed(directive)));
        Assert.False(result.CodeDocument.IsDirectiveUsed(directives[4]));
    }

    [Fact]
    [WorkItem("https://github.com/dotnet/roslyn/issues/85411")]
    public void ComponentDiscovery_UsingTypeAlias_TakesPrecedenceBeforeNamespaceImport()
    {
        AddConflictingButtonComponents();
        var result = CompileToCSharp("""
            @using MyButton = OtherLibrary.MyButton
            @using UI
            <MyButton />
            """);

        Assert.Empty(result.RazorDiagnostics);
        Assert.Contains("OpenComponent<global::OtherLibrary.MyButton>", result.Code);
        CompileToAssembly(result);
    }

    [Fact]
    [WorkItem("https://github.com/dotnet/roslyn/issues/85411")]
    public void ComponentDiscovery_UsingTypeAlias_TakesPrecedenceAfterNamespaceImport()
    {
        AddConflictingButtonComponents();
        var result = CompileToCSharp("""
            @using UI
            @using MyButton = OtherLibrary.MyButton
            <MyButton />
            """);

        Assert.Empty(result.RazorDiagnostics);
        Assert.Contains("OpenComponent<global::OtherLibrary.MyButton>", result.Code);
        CompileToAssembly(result);
    }

    [Fact]
    [WorkItem("https://github.com/dotnet/roslyn/issues/85411")]
    public void ComponentDiscovery_UsingNamespaceAlias_TakesPrecedenceBeforeNamespaceImport()
    {
        AddConflictingButtonComponents();
        var result = CompileToCSharp("""
            @using UI = OtherLibrary
            @using UI
            <UI.MyButton />
            """);

        Assert.Empty(result.RazorDiagnostics);
        Assert.Contains("OpenComponent<global::OtherLibrary.MyButton>", result.Code);
        CompileToAssembly(result);
    }

    [Fact]
    [WorkItem("https://github.com/dotnet/roslyn/issues/85411")]
    public void ComponentDiscovery_UsingNamespaceAlias_TakesPrecedenceAfterNamespaceImport()
    {
        AddConflictingButtonComponents();
        var result = CompileToCSharp("""
            @using UI
            @using UI = OtherLibrary
            <UI.MyButton />
            """);

        Assert.Empty(result.RazorDiagnostics);
        Assert.Contains("OpenComponent<global::OtherLibrary.MyButton>", result.Code);
        CompileToAssembly(result);
    }

    [Fact]
    [WorkItem("https://github.com/dotnet/roslyn/issues/85411")]
    public void ComponentDiscovery_UsingAlias_GenericComponentWithExplicitType()
    {
        CompileAliasedGenericComponent("""
            @using UI = Components.Library
            <UI.Generic TItem="string" Item="@("Hello")" />
            """);
    }

    [Fact]
    [WorkItem("https://github.com/dotnet/roslyn/issues/85411")]
    public void ComponentDiscovery_UsingAlias_GenericComponentWithInferredType()
    {
        CompileAliasedGenericComponent("""
            @using UI = Components.Library
            <UI.Generic Item="@("Hello")" />
            """);
    }

    [Fact]
    [WorkItem("https://github.com/dotnet/roslyn/issues/85411")]
    public void ComponentDiscovery_UsingAlias_DoesNotShadowHtmlBinding()
    {
        AdditionalSyntaxTrees.Add(Parse("""
            namespace Test
            {
                [Microsoft.AspNetCore.Components.BindElement("input", null, "value", "onchange")]
                public static class BindAttributes { }
            }
            """));

        var result = CompileToCSharp("""
            @using input = System.String
            <input @bind="Value" />
            @code {
                private string Value { get; set; } = "Hello";
            }
            """);

        Assert.Empty(result.RazorDiagnostics);
        Assert.Contains("CreateBinder", result.Code);
        CompileToAssembly(result);
    }

    [Fact]
    [WorkItem("https://github.com/dotnet/roslyn/issues/85411")]
    public void ComponentDiscovery_UsingAlias_SelectsNonGenericType()
    {
        AdditionalSyntaxTrees.Add(Parse("""
            namespace Components.Library
            {
                public abstract class WidgetBase : Microsoft.AspNetCore.Components.ComponentBase
                {
                    [Microsoft.AspNetCore.Components.Parameter] public string Value { get; set; }
                    [Microsoft.AspNetCore.Components.Parameter] public Microsoft.AspNetCore.Components.EventCallback<string> ValueChanged { get; set; }
                    [Microsoft.AspNetCore.Components.Parameter] public Microsoft.AspNetCore.Components.RenderFragment ChildContent { get; set; }
                }
                public class Widget : WidgetBase { }
                public class Widget<TItem> : WidgetBase { }
                public class Subsequent : Microsoft.AspNetCore.Components.ComponentBase { }
            }
            """));

        var result = CompileToCSharp("""
            @using Alias = Components.Library.Widget
            <Alias @bind-Value="value"><ChildContent>Hello</ChildContent></Alias>
            <Components.Library.Subsequent />
            @code {
                private string value = "Hello";
            }
            """);

        Assert.Empty(result.RazorDiagnostics);
        var context = result.CodeDocument.GetRequiredTagHelperContext();
        Assert.DoesNotContain(context.TagHelpers, descriptor =>
            descriptor.TypeName.Contains('<') &&
            descriptor.TagMatchingRules.Any(rule => rule.TagName == "Alias" || rule.ParentTag == "Alias"));
        Assert.Contains("ValueChanged", result.Code);
        Assert.Contains("ChildContent", result.Code);
        Assert.Contains("OpenComponent<global::Components.Library.Subsequent>", result.Code);
        CompileToAssembly(result);
    }

    [Fact]
    [WorkItem("https://github.com/dotnet/roslyn/issues/85411")]
    public void ComponentDiscovery_UsingAlias_DisambiguatesGenericArity()
    {
        AddGenericArityComponents();
        var result = CompileToCSharp("""
            @using UI = Components.Library
            <UI.Widget TItem="string" />
            <UI.Subsequent />
            """);

        Assert.Empty(result.RazorDiagnostics);
        Assert.Contains("OpenComponent<global::Components.Library.Widget<", result.Code);
        Assert.Contains("OpenComponent<global::Components.Library.Subsequent>", result.Code);
        CompileToAssembly(result);
    }

    [Fact]
    [WorkItem("https://github.com/dotnet/roslyn/issues/85411")]
    public void ComponentDiscovery_UsingAlias_AmbiguousGenericArityDoesNotStopLowering()
    {
        AddGenericArityComponents();
        var result = CompileToCSharp("""
            @using UI = Components.Library
            <UI.Widget />
            <UI.Subsequent />
            """);

        Assert.Contains(result.RazorDiagnostics, diagnostic => diagnostic.Id == ComponentDiagnosticFactory.MultipleComponents.Id);
        Assert.Contains("OpenComponent<global::Components.Library.Subsequent>", result.Code);
    }

    [Fact]
    [WorkItem("https://github.com/dotnet/roslyn/issues/85411")]
    public void ComponentDiscovery_UsingTypeAlias_SourceMappings()
    {
        var result = CompileAndVerifyAliasSourceMappings(
            "Button = Components.Library.MyButton",
            "Button",
            mappedIdentifier: "Button",
            generatedIdentifier: "MyButton");
        Assert.Contains("OpenComponent<global::Components.Library.MyButton>", result.Code);
    }

    [Fact]
    [WorkItem("https://github.com/dotnet/roslyn/issues/85411")]
    public void ComponentDiscovery_UsingLongTypeAlias_SourceMappings()
    {
        var result = CompileAndVerifyAliasSourceMappings(
            "VeryDescriptiveButtonAliasName = Components.Library.MyButton",
            "VeryDescriptiveButtonAliasName",
            mappedIdentifier: "VeryDescriptiveButtonAliasName",
            generatedIdentifier: "MyButton");
        Assert.Contains("OpenComponent<global::Components.Library.MyButton>", result.Code);
    }

    [Fact]
    [WorkItem("https://github.com/dotnet/roslyn/issues/85411")]
    public void ComponentDiscovery_UsingTypeAliasWithOriginalName_SourceMappings()
    {
        var result = CompileAndVerifyAliasSourceMappings("MyButton = Components.Library.MyButton", "MyButton", mappedIdentifier: "MyButton");
        Assert.Contains("OpenComponent<global::Components.Library.MyButton>", result.Code);
    }

    [Fact]
    [WorkItem("https://github.com/dotnet/roslyn/issues/85411")]
    public void ComponentDiscovery_UsingTypeAliasWithCommentAndSemicolon_SourceMappings()
    {
        var result = CompileAndVerifyAliasSourceMappings(
            "Button /* alias */ = Components.Library.MyButton;",
            "Button",
            mappedIdentifier: "Button",
            generatedIdentifier: "MyButton");
        Assert.Contains("OpenComponent<global::Components.Library.MyButton>", result.Code);
    }

    [Fact]
    [WorkItem("https://github.com/dotnet/roslyn/issues/85411")]
    public void ComponentDiscovery_UsingNamespaceAlias_SourceMappings()
    {
        var result = CompileAndVerifyAliasSourceMappings("UI = Components.Library", "UI.MyButton", mappedIdentifier: "MyButton");
        Assert.Contains("OpenComponent<global::Components.Library.MyButton>", result.Code);
    }

    [Fact]
    [WorkItem("https://github.com/dotnet/roslyn/issues/85411")]
    public void ComponentDiscovery_UsingLongNamespaceAlias_SourceMappings()
    {
        var result = CompileAndVerifyAliasSourceMappings(
            "VeryDescriptiveNamespaceAlias = Components.Library",
            "VeryDescriptiveNamespaceAlias.MyButton",
            mappedIdentifier: "MyButton");
        Assert.Contains("OpenComponent<global::Components.Library.MyButton>", result.Code);
    }

    private void AddButtonComponent()
    {
        AdditionalSyntaxTrees.Add(Parse("""
            using Microsoft.AspNetCore.Components;

            namespace Components.Library
            {
                public class MyButton : ComponentBase
                {
                    [Parameter] public string Title { get; set; }
                    [Parameter] public EventCallback<string> TitleChanged { get; set; }
                    [Parameter] public RenderFragment<string> ChildContent { get; set; }
                }
            }
            """));
    }

    private CompileToCSharpResult CompileAliasedButton(string source)
    {
        AddButtonComponent();
        var result = CompileToCSharp(source);
        Assert.Empty(result.RazorDiagnostics);
        Assert.Contains("TitleChanged", result.Code);
        Assert.Contains("ChildContent", result.Code);
        CompileToAssembly(result);

        // Aliasing must not reparent cached descriptors shared with other documents.
        var context = result.CodeDocument.GetRequiredTagHelperContext();
        foreach (var descriptor in context.TagHelpers)
        {
            foreach (var rule in descriptor.TagMatchingRules)
            {
                Assert.Same(descriptor, rule.Parent);
                foreach (var attribute in rule.Attributes)
                {
                    Assert.Same(rule, attribute.Parent);
                }
            }

            foreach (var attribute in descriptor.BoundAttributes)
            {
                Assert.Same(descriptor, attribute.Parent);
                foreach (var parameter in attribute.Parameters)
                {
                    Assert.Same(attribute, parameter.Parent);
                }
            }
        }

        Assert.DoesNotContain(context.TagHelpers, t => t.TagMatchingRules.Any(r => r.TagName == "MyButton"));
        return result;
    }

    private static void AssertUsingAliasIsUsed(RazorCodeDocument codeDocument)
    {
        var usingDirective = Assert.Single(codeDocument.GetRequiredSyntaxTree().Root.DescendantNodes().OfType<RazorUsingDirectiveSyntax>().ToImmutableArray());
        Assert.True(codeDocument.IsDirectiveUsed(usingDirective));
    }

    private static RazorCodeDocument CreateAliasUsageTestDocument(string content)
    {
        var builder = TagHelperDescriptorBuilder.Create(TagHelperKind.Component, "Library.Button", "Library");
        builder.SetTypeName("Library.Button", "Library", "Button");
        builder.TagMatchingRule(rule => rule.TagName = "Button");
        var descriptors = TagHelperCollection.Create([builder.Build()]);
        var engine = RazorProjectEngine.Create(new RazorConfiguration(RazorLanguageVersion.Version_12_0, "test", Extensions: []), RazorProjectFileSystem.Empty);
        return engine.Process(RazorSourceDocument.Create(content, "Index.razor"), RazorFileKind.Component, [], descriptors);
    }

    private void AddConflictingButtonComponents()
    {
        AdditionalSyntaxTrees.Add(Parse("""
            namespace UI
            {
                public class MyButton : Microsoft.AspNetCore.Components.ComponentBase { }
            }
            namespace OtherLibrary
            {
                public class MyButton : Microsoft.AspNetCore.Components.ComponentBase { }
            }
            """));
    }

    private void CompileAliasedGenericComponent(string source)
    {
        AdditionalSyntaxTrees.Add(Parse("""
            namespace Components.Library
            {
                public class Generic<TItem> : Microsoft.AspNetCore.Components.ComponentBase
                {
                    [Microsoft.AspNetCore.Components.Parameter] public TItem Item { get; set; }
                }
            }
            """));

        var result = CompileToCSharp(source);
        Assert.Empty(result.RazorDiagnostics);
        var context = result.CodeDocument.GetRequiredTagHelperContext();
        var binding = context.GetBinder().GetBinding("UI.Generic", [], parentTagName: null, parentIsTagHelper: false);
        Assert.NotNull(binding);
        Assert.Contains(binding.TagHelpers, t =>
            t.TypeName == "Components.Library.Generic<TItem>" &&
            t.TagMatchingRules.Any(r => r.TagName == "Generic"));
        CompileToAssembly(result);
    }

    private void AddGenericArityComponents()
    {
        AdditionalSyntaxTrees.Add(Parse("""
            namespace Components.Library
            {
                public class Widget<TItem> : Microsoft.AspNetCore.Components.ComponentBase { }
                public class Widget<TItem, TOther> : Microsoft.AspNetCore.Components.ComponentBase { }
                public class Subsequent : Microsoft.AspNetCore.Components.ComponentBase { }
            }
            """));
    }

    private CompileToCSharpResult CompileAndVerifyAliasSourceMappings(
        string alias,
        string tagName,
        string mappedIdentifier,
        string? generatedIdentifier = null)
    {
        AddButtonComponent();
        var result = CompileToCSharp($"@using {alias}\n<{tagName} />");
        Assert.Empty(result.RazorDiagnostics);
        CompileToAssembly(result);

        // Baseline tests normalise line endings, so offsets must use the source the compiler saw.
        var source = result.CodeDocument.Source.Text.ToString();
        var tagStart = source.IndexOf('<') + 1;
        var generated = result.CodeDocument.GetRequiredImplCSharpDocument();
        var directiveMapping = Assert.Single(generated.SourceMappingsSortedByOriginal,
            mapping => mapping.OriginalSpan.AbsoluteIndex < tagStart);
        Assert.Equal(
            source.Substring(directiveMapping.OriginalSpan.AbsoluteIndex, directiveMapping.OriginalSpan.Length),
            result.Code.Substring(directiveMapping.GeneratedSpan.AbsoluteIndex, directiveMapping.GeneratedSpan.Length));
        var tagMappings = generated.SourceMappingsSortedByOriginal.Where(mapping =>
            mapping.OriginalSpan.AbsoluteIndex >= tagStart &&
            mapping.OriginalSpan.AbsoluteIndex + mapping.OriginalSpan.Length <= tagStart + tagName.Length).ToArray();
        if (!string.Equals(mappedIdentifier, generatedIdentifier ?? mappedIdentifier, StringComparison.Ordinal))
        {
            Assert.Empty(tagMappings);
            return result;
        }

        var tagMapping = Assert.Single(tagMappings);
        Assert.Equal(
            mappedIdentifier[^tagMapping.OriginalSpan.Length..],
            source.Substring(tagMapping.OriginalSpan.AbsoluteIndex, tagMapping.OriginalSpan.Length));
        Assert.Equal(
            (generatedIdentifier ?? mappedIdentifier)[..tagMapping.GeneratedSpan.Length],
            result.Code.Substring(tagMapping.GeneratedSpan.AbsoluteIndex, tagMapping.GeneratedSpan.Length));
        return result;
    }

}
