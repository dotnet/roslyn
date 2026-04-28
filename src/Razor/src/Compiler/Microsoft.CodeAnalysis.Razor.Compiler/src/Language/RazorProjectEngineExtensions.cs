// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;

namespace Microsoft.AspNetCore.Razor.Language;

public static class RazorProjectEngineExtensions
{
    private static RazorFileKind DefaultFileKind => RazorFileKind.Legacy;

    public static RazorCodeDocument CreateCodeDocument(this RazorProjectEngine projectEngine, RazorSourceDocument source)
        => projectEngine.CreateCodeDocumentCore(source);

    public static RazorCodeDocument CreateCodeDocument(this RazorProjectEngine projectEngine, RazorSourceDocument source, RazorFileKind fileKind)
        => projectEngine.CreateCodeDocumentCore(source, fileKind);

    public static RazorCodeDocument CreateCodeDocument(
        this RazorProjectEngine projectEngine,
        RazorSourceDocument source,
        ImmutableArray<RazorSourceDocument> importSources)
        => projectEngine.CreateCodeDocumentCore(source, importSources: importSources);

    public static RazorCodeDocument CreateCodeDocument(
        this RazorProjectEngine projectEngine,
        RazorSourceDocument source,
        RazorFileKind fileKind,
        ImmutableArray<RazorSourceDocument> importSources)
        => projectEngine.CreateCodeDocumentCore(source, fileKind, importSources);

    public static RazorCodeDocument CreateCodeDocument(
        this RazorProjectEngine projectEngine,
        RazorSourceDocument source,
        TagHelperCollection tagHelpers)
        => projectEngine.CreateCodeDocumentCore(source, tagHelpers: tagHelpers);

    public static RazorCodeDocument CreateCodeDocument(
        this RazorProjectEngine projectEngine,
        RazorSourceDocument source,
        RazorFileKind fileKind,
        TagHelperCollection tagHelpers)
        => projectEngine.CreateCodeDocumentCore(source, fileKind, tagHelpers: tagHelpers);

    public static RazorCodeDocument CreateCodeDocument(
        this RazorProjectEngine projectEngine,
        RazorSourceDocument source,
        ImmutableArray<RazorSourceDocument> importSources,
        TagHelperCollection tagHelpers)
        => projectEngine.CreateCodeDocumentCore(source, importSources: importSources, tagHelpers: tagHelpers);

    public static RazorCodeDocument CreateCodeDocument(
        this RazorProjectEngine projectEngine,
        RazorSourceDocument source,
        RazorFileKind fileKind,
        ImmutableArray<RazorSourceDocument> importSources,
        TagHelperCollection tagHelpers)
        => projectEngine.CreateCodeDocumentCore(source, fileKind, importSources, tagHelpers);

    private static RazorCodeDocument CreateCodeDocumentCore(
        this RazorProjectEngine projectEngine,
        RazorSourceDocument source,
        RazorFileKind? fileKind = null,
        ImmutableArray<RazorSourceDocument> importSources = default,
        TagHelperCollection? tagHelpers = null)
    {
        var fileKindValue = fileKind ?? (source.FilePath is string filePath
            ? FileKinds.GetFileKindFromPath(filePath)
            : DefaultFileKind);

        return projectEngine.CreateCodeDocument(source, fileKindValue, importSources, tagHelpers, cssScope: null);
    }

    public static RazorCodeDocument CreateDesignTimeCodeDocument(this RazorProjectEngine projectEngine, RazorSourceDocument source)
        => projectEngine.CreateDesignTimeCodeDocumentCore(source);

    public static RazorCodeDocument CreateDesignTimeCodeDocument(this RazorProjectEngine projectEngine, RazorSourceDocument source, RazorFileKind fileKind)
        => projectEngine.CreateDesignTimeCodeDocumentCore(source, fileKind);

    public static RazorCodeDocument CreateDesignTimeCodeDocument(
        this RazorProjectEngine projectEngine,
        RazorSourceDocument source,
        ImmutableArray<RazorSourceDocument> importSources)
        => projectEngine.CreateDesignTimeCodeDocumentCore(source, importSources: importSources);

    public static RazorCodeDocument CreateDesignTimeCodeDocument(
        this RazorProjectEngine projectEngine,
        RazorSourceDocument source,
        RazorFileKind fileKind,
        ImmutableArray<RazorSourceDocument> importSources)
        => projectEngine.CreateDesignTimeCodeDocumentCore(source, fileKind, importSources);

    public static RazorCodeDocument CreateDesignTimeCodeDocument(
        this RazorProjectEngine projectEngine,
        RazorSourceDocument source,
        TagHelperCollection tagHelpers)
        => projectEngine.CreateDesignTimeCodeDocumentCore(source, tagHelpers: tagHelpers);

    public static RazorCodeDocument CreateDesignTimeCodeDocument(
        this RazorProjectEngine projectEngine,
        RazorSourceDocument source,
        RazorFileKind fileKind,
        TagHelperCollection tagHelpers)
        => projectEngine.CreateDesignTimeCodeDocumentCore(source, fileKind, tagHelpers: tagHelpers);

    public static RazorCodeDocument CreateDesignTimeCodeDocument(
        this RazorProjectEngine projectEngine,
        RazorSourceDocument source,
        ImmutableArray<RazorSourceDocument> importSources,
        TagHelperCollection tagHelpers)
        => projectEngine.CreateDesignTimeCodeDocumentCore(source, importSources: importSources, tagHelpers: tagHelpers);

    public static RazorCodeDocument CreateDesignTimeCodeDocument(
        this RazorProjectEngine projectEngine,
        RazorSourceDocument source,
        RazorFileKind fileKind,
        ImmutableArray<RazorSourceDocument> importSources,
        TagHelperCollection tagHelpers)
        => projectEngine.CreateDesignTimeCodeDocumentCore(source, fileKind, importSources, tagHelpers);

    private static RazorCodeDocument CreateDesignTimeCodeDocumentCore(
        this RazorProjectEngine projectEngine,
        RazorSourceDocument source,
        RazorFileKind? fileKind = null,
        ImmutableArray<RazorSourceDocument> importSources = default,
        TagHelperCollection? tagHelpers = null)
    {
        var fileKindValue = fileKind ?? (source.FilePath is string filePath
            ? FileKinds.GetFileKindFromPath(filePath)
            : DefaultFileKind);

        return projectEngine.CreateDesignTimeCodeDocument(source, fileKindValue, importSources, tagHelpers);
    }
}
