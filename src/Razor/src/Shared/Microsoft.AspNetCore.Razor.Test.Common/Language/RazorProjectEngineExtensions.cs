// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Immutable;
using Microsoft.AspNetCore.Razor.Language.Intermediate;
using Xunit;

namespace Microsoft.AspNetCore.Razor.Language;

public static class RazorProjectEngineExtensions
{
    private static RazorFileKind DefaultFileKind => RazorFileKind.Legacy;

    public static RazorCodeDocument CreateEmptyCodeDocument(this RazorProjectEngine projectEngine)
        => projectEngine.CreateEmptyCodeDocumentCore();

    public static RazorCodeDocument CreateEmptyCodeDocument(this RazorProjectEngine projectEngine, RazorFileKind fileKind)
        => projectEngine.CreateEmptyCodeDocumentCore(fileKind);

    public static RazorCodeDocument CreateEmptyCodeDocument(
        this RazorProjectEngine projectEngine,
        ImmutableArray<RazorSourceDocument> importSources)
        => projectEngine.CreateEmptyCodeDocumentCore(importSources: importSources);

    public static RazorCodeDocument CreateEmptyCodeDocument(
        this RazorProjectEngine projectEngine,
        RazorFileKind fileKind,
        ImmutableArray<RazorSourceDocument> importSources)
        => projectEngine.CreateEmptyCodeDocumentCore(fileKind, importSources);

    public static RazorCodeDocument CreateEmptyCodeDocument(
        this RazorProjectEngine projectEngine,
        TagHelperCollection tagHelpers)
        => projectEngine.CreateEmptyCodeDocumentCore(tagHelpers: tagHelpers);

    public static RazorCodeDocument CreateEmptyCodeDocument(
        this RazorProjectEngine projectEngine,
        RazorFileKind fileKind,
        TagHelperCollection tagHelpers)
        => projectEngine.CreateEmptyCodeDocumentCore(fileKind, tagHelpers: tagHelpers);

    public static RazorCodeDocument CreateEmptyCodeDocument(
        this RazorProjectEngine projectEngine,
        ImmutableArray<RazorSourceDocument> importSources,
        TagHelperCollection tagHelpers)
        => projectEngine.CreateEmptyCodeDocumentCore(importSources: importSources, tagHelpers: tagHelpers);

    public static RazorCodeDocument CreateEmptyCodeDocument(
        this RazorProjectEngine projectEngine,
        RazorFileKind fileKind,
        ImmutableArray<RazorSourceDocument> importSources,
        TagHelperCollection tagHelpers)
        => projectEngine.CreateEmptyCodeDocumentCore(fileKind, importSources, tagHelpers);

    private static RazorCodeDocument CreateEmptyCodeDocumentCore(
        this RazorProjectEngine projectEngine,
        RazorFileKind? fileKind = null,
        ImmutableArray<RazorSourceDocument> importSources = default,
        TagHelperCollection? tagHelpers = null)
        => projectEngine.CreateCodeDocumentCore(string.Empty, fileKind, importSources, tagHelpers);

    public static RazorCodeDocument CreateEmptyDesignTimeCodeDocument(this RazorProjectEngine projectEngine)
        => projectEngine.CreateEmptyDesignTimeCodeDocumentCore();

    public static RazorCodeDocument CreateEmptyDesignTimeCodeDocument(this RazorProjectEngine projectEngine, RazorFileKind fileKind)
        => projectEngine.CreateEmptyDesignTimeCodeDocumentCore(fileKind);

    public static RazorCodeDocument CreateEmptyDesignTimeCodeDocument(
        this RazorProjectEngine projectEngine,
        ImmutableArray<RazorSourceDocument> importSources)
        => projectEngine.CreateEmptyDesignTimeCodeDocumentCore(importSources: importSources);

    public static RazorCodeDocument CreateEmptyDesignTimeCodeDocument(
        this RazorProjectEngine projectEngine,
        RazorFileKind fileKind,
        ImmutableArray<RazorSourceDocument> importSources)
        => projectEngine.CreateEmptyDesignTimeCodeDocumentCore(fileKind, importSources);

    public static RazorCodeDocument CreateEmptyDesignTimeCodeDocument(
        this RazorProjectEngine projectEngine,
        TagHelperCollection tagHelpers)
        => projectEngine.CreateEmptyDesignTimeCodeDocumentCore(tagHelpers: tagHelpers);

    public static RazorCodeDocument CreateEmptyDesignTimeCodeDocument(
        this RazorProjectEngine projectEngine,
        RazorFileKind fileKind,
        TagHelperCollection tagHelpers)
        => projectEngine.CreateEmptyDesignTimeCodeDocumentCore(fileKind, tagHelpers: tagHelpers);

    public static RazorCodeDocument CreateEmptyDesignTimeCodeDocument(
        this RazorProjectEngine projectEngine,
        ImmutableArray<RazorSourceDocument> importSources,
        TagHelperCollection tagHelpers)
        => projectEngine.CreateEmptyDesignTimeCodeDocumentCore(importSources: importSources, tagHelpers: tagHelpers);

    public static RazorCodeDocument CreateEmptyDesignTimeCodeDocument(
        this RazorProjectEngine projectEngine,
        RazorFileKind fileKind,
        ImmutableArray<RazorSourceDocument> importSources,
        TagHelperCollection tagHelpers)
        => projectEngine.CreateEmptyDesignTimeCodeDocumentCore(fileKind, importSources, tagHelpers);

    private static RazorCodeDocument CreateEmptyDesignTimeCodeDocumentCore(
        this RazorProjectEngine projectEngine,
        RazorFileKind? fileKind = null,
        ImmutableArray<RazorSourceDocument> importSources = default,
        TagHelperCollection? tagHelpers = null)
        => projectEngine.CreateDesignTimeCodeDocumentCore(string.Empty, fileKind, importSources, tagHelpers);

    public static RazorCodeDocument CreateCodeDocument(this RazorProjectEngine projectEngine, string content)
        => projectEngine.CreateCodeDocumentCore(content);

    public static RazorCodeDocument CreateCodeDocument(this RazorProjectEngine projectEngine, string content, RazorFileKind fileKind)
        => projectEngine.CreateCodeDocumentCore(content, fileKind);

    public static RazorCodeDocument CreateCodeDocument(
        this RazorProjectEngine projectEngine,
        string content,
        ImmutableArray<RazorSourceDocument> importSources)
        => projectEngine.CreateCodeDocumentCore(content, importSources: importSources);

    public static RazorCodeDocument CreateCodeDocument(
        this RazorProjectEngine projectEngine,
        string content,
        RazorFileKind fileKind,
        ImmutableArray<RazorSourceDocument> importSources)
        => projectEngine.CreateCodeDocumentCore(content, fileKind, importSources);

    public static RazorCodeDocument CreateCodeDocument(
        this RazorProjectEngine projectEngine,
        string content,
        TagHelperCollection tagHelpers)
        => projectEngine.CreateCodeDocumentCore(content, tagHelpers: tagHelpers);

    public static RazorCodeDocument CreateCodeDocument(
        this RazorProjectEngine projectEngine,
        string content,
        RazorFileKind fileKind,
        TagHelperCollection tagHelpers)
        => projectEngine.CreateCodeDocumentCore(content, fileKind, tagHelpers: tagHelpers);

    public static RazorCodeDocument CreateCodeDocument(
        this RazorProjectEngine projectEngine,
        string content,
        ImmutableArray<RazorSourceDocument> importSources,
        TagHelperCollection tagHelpers)
        => projectEngine.CreateCodeDocumentCore(content, importSources: importSources, tagHelpers: tagHelpers);

    public static RazorCodeDocument CreateCodeDocument(
        this RazorProjectEngine projectEngine,
        string content,
        RazorFileKind fileKind,
        ImmutableArray<RazorSourceDocument> importSources,
        TagHelperCollection tagHelpers)
        => projectEngine.CreateCodeDocumentCore(content, fileKind, importSources, tagHelpers);

    private static RazorCodeDocument CreateCodeDocumentCore(
        this RazorProjectEngine projectEngine,
        string content,
        RazorFileKind? fileKind = null,
        ImmutableArray<RazorSourceDocument> importSources = default,
        TagHelperCollection? tagHelpers = null)
    {
        var source = TestRazorSourceDocument.Create(content);

        return projectEngine.CreateCodeDocument(source, fileKind ?? DefaultFileKind, importSources, tagHelpers, cssScope: null);
    }

    public static RazorCodeDocument CreateDesignTimeCodeDocument(this RazorProjectEngine projectEngine, string content)
        => projectEngine.CreateDesignTimeCodeDocumentCore(content);

    public static RazorCodeDocument CreateDesignTimeCodeDocument(
        this RazorProjectEngine projectEngine,
        string content,
        TagHelperCollection? tagHelpers)
        => projectEngine.CreateDesignTimeCodeDocumentCore(content, tagHelpers: tagHelpers);

    public static RazorCodeDocument CreateDesignTimeCodeDocument(this RazorProjectEngine projectEngine, string content, RazorFileKind fileKind)
        => projectEngine.CreateDesignTimeCodeDocumentCore(content, fileKind);

    public static RazorCodeDocument CreateDesignTimeCodeDocument(
        this RazorProjectEngine projectEngine,
        string content,
        RazorFileKind fileKind,
        TagHelperCollection? tagHelpers)
        => projectEngine.CreateDesignTimeCodeDocumentCore(content, fileKind, tagHelpers: tagHelpers);

    public static RazorCodeDocument CreateDesignTimeCodeDocument(
        this RazorProjectEngine projectEngine,
        string content,
        ImmutableArray<RazorSourceDocument> importSources)
        => projectEngine.CreateDesignTimeCodeDocumentCore(content, importSources: importSources);

    public static RazorCodeDocument CreateDesignTimeCodeDocument(
        this RazorProjectEngine projectEngine,
        string content,
        ImmutableArray<RazorSourceDocument> importSources,
        TagHelperCollection tagHelpers)
        => projectEngine.CreateDesignTimeCodeDocumentCore(content, importSources: importSources, tagHelpers: tagHelpers);

    public static RazorCodeDocument CreateDesignTimeCodeDocument(
        this RazorProjectEngine projectEngine,
        string content,
        RazorFileKind fileKind,
        ImmutableArray<RazorSourceDocument> importSources)
        => projectEngine.CreateDesignTimeCodeDocumentCore(content, fileKind, importSources);

    public static RazorCodeDocument CreateDesignTimeCodeDocument(
        this RazorProjectEngine projectEngine,
        string content,
        RazorFileKind fileKind,
        ImmutableArray<RazorSourceDocument> importSources,
        TagHelperCollection tagHelpers)
        => projectEngine.CreateDesignTimeCodeDocumentCore(content, fileKind, importSources, tagHelpers);

    private static RazorCodeDocument CreateDesignTimeCodeDocumentCore(
        this RazorProjectEngine projectEngine,
        string content,
        RazorFileKind? fileKind = null,
        ImmutableArray<RazorSourceDocument> importSources = default,
        TagHelperCollection? tagHelpers = null)
    {
        var source = TestRazorSourceDocument.Create(content);

        return projectEngine.CreateDesignTimeCodeDocument(source, fileKind ?? DefaultFileKind, importSources, tagHelpers);
    }

    public static RazorCodeDocument ExecutePhasesThrough<T>(
        this RazorProjectEngine projectEngine,
        RazorCodeDocument codeDocument)
        where T : IRazorEnginePhase
    {
        foreach (var phase in projectEngine.Engine.Phases)
        {
            codeDocument = phase.Execute(codeDocument);

            if (phase is T)
            {
                break;
            }
        }
        
        return codeDocument;
    }

    public static RazorCodeDocument ExecutePhase<T>(
        this RazorProjectEngine projectEngine,
        RazorCodeDocument codeDocument)
        where T : IRazorEnginePhase, new()
    {
        return projectEngine.ExecutePhase<T>(codeDocument, () => new());
    }

    public static RazorCodeDocument ExecutePhase<T>(
        this RazorProjectEngine projectEngine,
        RazorCodeDocument codeDocument,
        Func<T> phaseFactory)
        where T : IRazorEnginePhase
    {
        var pass = phaseFactory();
        pass.Initialize(projectEngine.Engine);

        return pass.Execute(codeDocument);
    }

    public static void ExecutePass<T>(
        this RazorProjectEngine projectEngine,
        RazorCodeDocument codeDocument)
        where T : IntermediateNodePassBase, new()
    {
        var documentNode = codeDocument.GetDocumentNode();
        Assert.NotNull(documentNode);

        projectEngine.ExecutePass<T>(codeDocument, documentNode);
    }

    public static void ExecutePass<T>(
        this RazorProjectEngine projectEngine,
        RazorCodeDocument codeDocument,
        Func<T> passFactory)
        where T : IntermediateNodePassBase
    {
        var documentNode = codeDocument.GetDocumentNode();
        Assert.NotNull(documentNode);

        projectEngine.ExecutePass<T>(codeDocument, documentNode, passFactory);
    }

    public static void ExecutePass<T>(
        this RazorProjectEngine projectEngine,
        RazorCodeDocument codeDocument,
        DocumentIntermediateNode documentNode)
        where T : IntermediateNodePassBase, new()
        => projectEngine.ExecutePass<T>(codeDocument, documentNode, () => new());

    public static void ExecutePass<T>(
        this RazorProjectEngine projectEngine,
        RazorCodeDocument codeDocument,
        DocumentIntermediateNode documentNode,
        Func<T> passFactory)
        where T : IntermediateNodePassBase
    {
        var pass = passFactory();
        pass.Initialize(projectEngine.Engine);

        pass.Execute(codeDocument, documentNode);
    }
}
