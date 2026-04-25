// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using Microsoft.AspNetCore.Razor.Language.Components;
using Microsoft.AspNetCore.Razor.Language.Extensions;
using Microsoft.AspNetCore.Razor.PooledObjects;
using Moq;
using Xunit;

namespace Microsoft.AspNetCore.Razor.Language;

public class RazorProjectEngineTest
{
    [Fact]
    public void CreateDesignTime_Lambda_AddsFeaturesAndPhases()
    {
        // Arrange

        // Act
        var engine = RazorProjectEngine.Create(RazorConfiguration.Default, Mock.Of<RazorProjectFileSystem>());

        // Assert
        AssertDefaultPhases(engine);
        AssertDefaultFeatures(engine);
        AssertDefaultDirectives(engine);
        AssertDefaultTargetExtensions(engine);
    }

    private static void AssertDefaultPhases(RazorProjectEngine engine)
    {
        Assert.Collection(
            engine.Phases,
            phase => Assert.IsType<DefaultRazorParsingPhase>(phase),
            phase => Assert.IsType<DefaultRazorSyntaxTreePhase>(phase),
            phase => Assert.IsType<DefaultRazorTagHelperContextDiscoveryPhase>(phase),
            phase => Assert.IsType<DefaultRazorIntermediateNodeLoweringPhase>(phase),
            phase => Assert.IsType<DefaultTagHelperResolutionPhase>(phase),
            phase => Assert.IsType<DefaultRazorTagHelperRewritePhase>(phase),
            phase => Assert.IsType<DefaultRazorDocumentClassifierPhase>(phase),
            phase => Assert.IsType<DefaultRazorDirectiveClassifierPhase>(phase),
            phase => Assert.IsType<DefaultRazorOptimizationPhase>(phase),
            phase => Assert.IsType<DefaultRazorCSharpLoweringPhase>(phase));
    }

    private static void AssertDefaultFeatures(RazorProjectEngine engine)
    {
        var features = engine.Engine.Features.OrderByAsArray(static x => x.GetType().Name);
        Assert.Collection(
            features,
            feature => Assert.IsType<AttributeDirectivePass>(feature),
            feature => Assert.IsType<ComponentBindLoweringPass>(feature),
            feature => Assert.IsType<ComponentChildContentDiagnosticPass>(feature),
            feature => Assert.IsType<ComponentComplexAttributeContentPass>(feature),
            feature => Assert.IsType<ComponentCssScopePass>(feature),
            feature => Assert.IsType<ComponentDocumentClassifierPass>(feature),
            feature => Assert.IsType<ComponentEventHandlerLoweringPass>(feature),
            feature => Assert.IsType<ComponentFormNameLoweringPass>(feature),
            feature => Assert.IsType<ComponentGenericTypePass>(feature),
            feature => Assert.IsType<ComponentInjectDirectivePass>(feature),
            feature => Assert.IsType<ComponentKeyLoweringPass>(feature),
            feature => Assert.IsType<ComponentLayoutDirectivePass>(feature),
            feature => Assert.IsType<ComponentLoweringPass>(feature),
            feature => Assert.IsType<ComponentMarkupBlockPass>(feature),
            feature => Assert.IsType<ComponentMarkupDiagnosticPass>(feature),
            feature => Assert.IsType<ComponentMarkupEncodingPass>(feature),
            feature => Assert.IsType<ComponentPageDirectivePass>(feature),
            feature => Assert.IsType<ComponentReferenceCaptureLoweringPass>(feature),
            feature => Assert.IsType<ComponentRenderModeDirectivePass>(feature),
            feature => Assert.IsType<ComponentRenderModeLoweringPass>(feature),
            feature => Assert.IsType<ComponentSplatLoweringPass>(feature),
            feature => Assert.IsType<ComponentTemplateDiagnosticPass>(feature),
            feature => Assert.IsType<ComponentWhitespacePass>(feature),
            feature => Assert.IsType<ConfigureDirectivesFeature>(feature),
            feature => Assert.IsType<DefaultDirectiveSyntaxTreePass>(feature),
            feature => Assert.IsType<DefaultDocumentClassifierPass>(feature),
            feature => Assert.IsType<DefaultDocumentClassifierPassFeature>(feature),
            feature => Assert.IsType<DefaultMetadataIdentifierFeature>(feature),
            feature => Assert.IsType<DefaultRazorTargetExtensionFeature>(feature),
            feature => Assert.IsType<DefaultTagHelperOptimizationPass>(feature),
            feature => Assert.IsType<DesignTimeDirectivePass>(feature),
            feature => Assert.IsType<DirectiveRemovalOptimizationPass>(feature),
            feature => Assert.IsType<EliminateMethodBodyPass>(feature),
            feature => Assert.IsType<FunctionsDirectivePass>(feature),
            feature => Assert.IsType<HtmlNodeOptimizationPass>(feature),
            feature => Assert.IsType<ImplementsDirectivePass>(feature),
            feature => Assert.IsType<InheritsDirectivePass>(feature),
            feature => Assert.IsType<MetadataAttributePass>(feature),
            feature => Assert.IsType<PreallocatedTagHelperAttributeOptimizationPass>(feature),
            feature => Assert.IsType<TagHelperDiscoveryService>(feature),
            feature => Assert.IsType<ViewCssScopePass>(feature));
    }

    private static void AssertDefaultDirectives(RazorProjectEngine engine)
    {
        var feature = engine.Engine.GetFeatures<ConfigureDirectivesFeature>().FirstOrDefault();
        Assert.NotNull(feature);
        Assert.Collection(
            feature.GetDirectives(),
            directive => Assert.Same(FunctionsDirective.Directive, directive),
            directive => Assert.Same(ImplementsDirective.Directive, directive),
            directive => Assert.Same(InheritsDirective.Directive, directive),
            directive => Assert.Same(NamespaceDirective.Directive, directive),
            directive => Assert.Same(AttributeDirective.Directive, directive));
    }

    private static void AssertDefaultTargetExtensions(RazorProjectEngine engine)
    {
        var feature = engine.Engine.GetFeatures<IRazorTargetExtensionFeature>().FirstOrDefault();
        Assert.NotNull(feature);

        var extensions = feature.TargetExtensions.OrderByAsArray(static f => f.GetType().Name);
        Assert.Collection(
            extensions,
            extension => Assert.IsType<DefaultTagHelperTargetExtension>(extension),
            extension => Assert.IsType<DesignTimeDirectiveTargetExtension>(extension),
            extension => Assert.IsType<MetadataAttributeTargetExtension>(extension),
            extension => Assert.IsType<PreallocatedAttributeTargetExtension>(extension));
    }

    [Fact]
    public void GetImportSourceDocuments_DoesNotIncludeNonExistentItems()
    {
        // Arrange
        var existingItem = new TestRazorProjectItem("Index.cshtml");
        var nonExistentItem = Mock.Of<RazorProjectItem>(item => item.Exists == false);
        using PooledArrayBuilder<RazorProjectItem> items = [existingItem, nonExistentItem];

        // Act
        var sourceDocuments = RazorProjectEngine.GetImportSourceDocuments(in items);

        // Assert
        var sourceDocument = Assert.Single(sourceDocuments);
        Assert.Equal(existingItem.FilePath, sourceDocument.FilePath);
    }

    [Fact]
    public void GetImportSourceDocuments_UnreadableItem_Throws()
    {
        // Arrange
        var projectItem = new TestRazorProjectItem(
            filePath: "path/to/file.cshtml",
            physicalPath: "path/to/file.cshtml",
            relativePhysicalPath: "path/to/file.cshtml",
            onRead: () => throw new IOException("Couldn't read file."));
        using PooledArrayBuilder<RazorProjectItem> items = [projectItem];

        // Act & Assert
        var exception = Assert.Throws<IOException>(() => RazorProjectEngine.GetImportSourceDocuments(in items));
        Assert.Equal("Couldn't read file.", exception.Message);
    }

    [Fact]
    public void GetImportSourceDocuments_WithSuppressExceptions_UnreadableItem_DoesNotThrow()
    {
        // Arrange
        var projectItem = new TestRazorProjectItem(
            filePath: "path/to/file.cshtml",
            physicalPath: "path/to/file.cshtml",
            relativePhysicalPath: "path/to/file.cshtml",
            onRead: () => throw new IOException("Couldn't read file."));
        using PooledArrayBuilder<RazorProjectItem> items = [projectItem];

        // Act
        var sourceDocuments = RazorProjectEngine.GetImportSourceDocuments(in items, suppressExceptions: true);

        // Assert - Does not throw
        Assert.Empty(sourceDocuments);
    }
}
