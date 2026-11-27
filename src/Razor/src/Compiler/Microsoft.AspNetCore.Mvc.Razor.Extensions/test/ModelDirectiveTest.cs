// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.Language.Extensions;
using Microsoft.AspNetCore.Razor.Language.Intermediate;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.AspNetCore.Mvc.Razor.Extensions;

public class ModelDirectiveTest : RazorProjectEngineTestBase
{
    protected override RazorLanguageVersion Version => RazorLanguageVersion.Version_3_0;

    protected override void ConfigureProjectEngine(RazorProjectEngineBuilder builder)
    {
        builder.AddDirective(ModelDirective.Directive);
        builder.AddDirective(PageDirective.Directive);
        builder.Features.Add(new ModelDirective.Pass());

        builder.Features.Add(new RazorPageDocumentClassifierPass());
        builder.Features.Add(new MvcViewDocumentClassifierPass());
    }

    protected override void ConfigureCodeDocumentProcessor(RazorCodeDocumentProcessor processor)
    {
        processor.ExecutePhasesThrough<IRazorDocumentClassifierPhase>();

        // Note: InheritsDirectivePass needs to run before ModelDirective.Pass.
        processor.ExecutePass<InheritsDirectivePass>();
    }

    [Fact]
    public void ModelDirective_GetModelType_GetsTypeFromFirstWellFormedDirective()
    {
        // Arrange
        var codeDocument = ProjectEngine.CreateCodeDocument(@"
@model Type1
@model Type2
@model
");

        var processor = CreateCodeDocumentProcessor(codeDocument);
        var documentNode = processor.GetDocumentNode();

        // Act
        var result = ModelDirective.GetModelType(documentNode).Content;

        // Assert
        Assert.Equal("Type1", result);
    }

    [Fact]
    public void ModelDirective_GetModelType_DefaultsToDynamic()
    {
        // Arrange
        var codeDocument = ProjectEngine.CreateCodeDocument(@" ");
        var processor = CreateCodeDocumentProcessor(codeDocument);
        var documentNode = processor.GetDocumentNode();

        // Act
        var result = ModelDirective.GetModelType(documentNode).Content;

        // Assert
        Assert.Equal("dynamic", result);
    }

    [Fact]
    public void ModelDirectivePass_Execute_ReplacesTModelInBaseType()
    {
        // Arrange
        var codeDocument = ProjectEngine.CreateCodeDocument(@"
@inherits BaseType<TModel>
@model Type1
");

        var processor = CreateCodeDocumentProcessor(codeDocument);

        // Act
        processor.ExecutePass<ModelDirective.Pass>();

        // Assert
        var documentNode = processor.GetDocumentNode();
        var classNode = documentNode.GetClassNode();
        var baseType = classNode.BaseType;

        Assert.NotNull(baseType);
        Assert.Equal("BaseType", baseType.BaseType.Content);
        Assert.NotNull(baseType.BaseType.Source);

        Assert.NotNull(baseType.ModelType);
        Assert.Equal("Type1", baseType.ModelType.Content);
        Assert.NotNull(baseType.ModelType.Source);
    }

    [Fact]
    public void ModelDirectivePass_Execute_ReplacesTModelInBaseType_DifferentOrdering()
    {
        // Arrange
        var codeDocument = ProjectEngine.CreateCodeDocument(@"
@model Type1
@inherits BaseType<TModel>
@model Type2
");

        var processor = CreateCodeDocumentProcessor(codeDocument);

        // Act
        processor.ExecutePass<ModelDirective.Pass>();

        // Assert
        var documentNode = processor.GetDocumentNode();
        var classNode = documentNode.GetClassNode();
        var baseType = classNode.BaseType;

        Assert.NotNull(baseType);
        Assert.Equal("BaseType", baseType.BaseType.Content);
        Assert.NotNull(baseType.BaseType.Source);

        Assert.NotNull(baseType.ModelType);
        Assert.Equal("Type1", baseType.ModelType.Content);
        Assert.NotNull(baseType.ModelType.Source);
    }

    [Fact]
    public void ModelDirectivePass_Execute_NoOpWithoutTModel()
    {
        // Arrange
        var codeDocument = ProjectEngine.CreateCodeDocument(@"
@inherits BaseType
@model Type1
");

        var processor = CreateCodeDocumentProcessor(codeDocument);

        // Act
        processor.ExecutePass<ModelDirective.Pass>();

        // Assert
        var documentNode = processor.GetDocumentNode();
        var classNode = documentNode.GetClassNode();
        var baseType = classNode.BaseType;

        Assert.NotNull(baseType);
        Assert.Equal("BaseType", baseType.BaseType.Content);
        Assert.NotNull(baseType.BaseType.Source);

        Assert.Null(baseType.ModelType);
    }

    [Fact]
    public void ModelDirectivePass_Execute_ReplacesTModelInBaseType_DefaultDynamic()
    {
        // Arrange
        var codeDocument = ProjectEngine.CreateCodeDocument(@"
@inherits BaseType<TModel>
");

        var processor = CreateCodeDocumentProcessor(codeDocument);

        // Act
        processor.ExecutePass<ModelDirective.Pass>();

        // Assert
        var documentNode = processor.GetDocumentNode();
        var classNode = documentNode.GetClassNode();
        var baseType = classNode.BaseType;

        Assert.NotNull(baseType);
        Assert.Equal("BaseType", baseType.BaseType.Content);
        Assert.NotNull(baseType.BaseType.Source);

        Assert.NotNull(baseType.ModelType);
        Assert.Equal("dynamic", baseType.ModelType.Content);
        Assert.Null(baseType.ModelType.Source);
    }

    [Fact, WorkItem("https://github.com/dotnet/razor/issues/10987")]
    public void ModelDirectivePass_Execute_DoesNotReportDiagnosticAtWarningLevel10()
    {
        // Arrange
        var source = """
            @inherits BaseType
            @model Type1
            """;

        // Act
        var document = ProcessToCSharp(source, warningLevel: 10);

        // Assert
        Assert.Empty(document.Diagnostics);
    }

    [ConditionalFact(typeof(IsEnglishLocal)), WorkItem("https://github.com/dotnet/razor/issues/10987")]
    public void ModelDirectivePass_Execute_ReportsExpectedDiagnosticAtWarningLevel11()
    {
        // Arrange
        var source = """
            @inherits BaseType
            @model Type1
            """;

        // Act
        var diagnostic = Assert.Single(ProcessToCSharp(source, warningLevel: 11).Diagnostics);

        // Assert
        Assert.Equal("RZ3907", diagnostic.Id);
        Assert.Equal(RazorDiagnosticSeverity.Warning, diagnostic.Severity);
        Assert.Equal(11, diagnostic.WarningLevel);
        Assert.Equal(
            "The '@model' directive is not applied to the generated base class because the '@inherits' directive does not contain '<TModel>'.",
            diagnostic.GetMessage());

        var modelTypeIndex = source.IndexOf("Type1", StringComparison.Ordinal);
        Assert.Equal(new SourceSpan("test.cshtml", modelTypeIndex, 1, 7, 5), diagnostic.Span);
    }

    [Fact, WorkItem("https://github.com/dotnet/razor/issues/10987")]
    public void ModelDirectivePass_Execute_UnsupportedGenericParameterWarns()
    {
        // Arrange
        var source = """
            @inherits BaseType<TSomething>
            @model Type1
            """;

        // Act
        var diagnostic = Assert.Single(ProcessToCSharp(source, warningLevel: 11).Diagnostics);

        // Assert
        Assert.Equal("RZ3907", diagnostic.Id);
    }

    [Theory, WorkItem("https://github.com/dotnet/razor/issues/10987")]
    [InlineData("@model Type1")]
    [InlineData("@inherits BaseType<TModel>\r\n@model Type1")]
    [InlineData("@inherits BaseType")]
    [InlineData("@inherits BaseType<TModel>")]
    [InlineData("@model Type1\r\n@inherits BaseType<TModel>")]
    public void ModelDirectivePass_Execute_ValidFormsDoNotWarn(string source)
    {
        // Act
        var document = ProcessToCSharp(source, warningLevel: 11);

        // Assert
        Assert.Empty(document.Diagnostics);
    }

    [Fact, WorkItem("https://github.com/dotnet/razor/issues/10987")]
    public void ModelDirectivePass_Execute_RazorPageWithInheritsWithoutTModelDoesNotWarn()
    {
        // Arrange
        var source = """
            @page
            @inherits BaseType
            @model Type1
            """;

        // Act
        var document = ProcessToCSharp(source, warningLevel: 11);

        // Assert
        Assert.Empty(document.Diagnostics);
    }

    private RazorCSharpDocument ProcessToCSharp(string source, int warningLevel)
    {
        var projectEngine = RazorProjectEngine.Create(
            Configuration with { RazorWarningLevel = warningLevel },
            RazorProjectFileSystem.Empty,
            ConfigureProjectEngine);

        var codeDocument = projectEngine.Process(
            TestRazorSourceDocument.Create(source),
            RazorFileKind.Legacy,
            importSources: [],
            tagHelpers: null);

        return codeDocument.GetRequiredCSharpDocument(declarationDocument: false);
    }
}
