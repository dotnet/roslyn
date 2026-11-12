// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Razor;

namespace Microsoft.VisualStudio.Razor;

#pragma warning disable RS0030 // Do not use banned APIs
[ExportWorkspaceService(typeof(ISourceGeneratedDocumentAnalyzerConfigOptionsProvider), ServiceLayer.Host), Shared]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal sealed class RazorSourceGeneratedDocumentAnalyzerConfigOptionsProvider() : ISourceGeneratedDocumentAnalyzerConfigOptionsProvider
#pragma warning restore RS0030 // Do not use banned APIs
{
    public ValueTask<AnalyzerConfigOptions?> GetOptionsAsync(SourceGeneratedDocument sourceGeneratedDocument, CancellationToken cancellationToken)
    {
        if (!sourceGeneratedDocument.IsRazorSourceGeneratedDocument())
        {
            return default;
        }

        var razorDocument = TryGetRazorDocumentForGeneratedDocument(sourceGeneratedDocument);
        if (razorDocument?.FilePath is not { } filePath)
        {
            return default;
        }

        var options = razorDocument.Project.State.GetAnalyzerOptionsForPath(filePath, cancellationToken).ConfigOptionsWithFallback;

        return new(options);
    }

    /// <summary>
    /// Performs the inverse of <see cref="ProjectExtensions.TryGetSourceGeneratedDocumentsForRazorDocumentAsync"/>,
    /// using the same full-path and project-relative hint-name matching.
    /// </summary>
    private static TextDocument? TryGetRazorDocumentForGeneratedDocument(SourceGeneratedDocument generatedDocument)
    {
        // Razor SDK projects use project-relative hint names, while miscellaneous and non-Razor SDK
        // projects use full paths.
        var project = generatedDocument.Project;
        TextDocument? fullPathMatchedDocument = null;
        TextDocument? candidateDocument = null;
        var hasMultipleFullPathMatches = false;
        var hasMultipleCandidates = false;

        foreach (var razorDocument in project.AdditionalDocuments)
        {
            if (razorDocument.FilePath is not { } filePath ||
                !filePath.IsRazorFilePath())
            {
                continue;
            }

            var (fullPathHintName, fullPathDeclHintName, projectRelativeHintName, projectRelativeDeclHintName) = ProjectExtensions.GetGeneratedDocumentHintNames(razorDocument);
            if (generatedDocument.HintName == fullPathHintName ||
                generatedDocument.HintName == fullPathDeclHintName)
            {
                hasMultipleFullPathMatches = fullPathMatchedDocument is not null;
                fullPathMatchedDocument ??= razorDocument;
            }
            else if (generatedDocument.HintName == projectRelativeHintName ||
                     generatedDocument.HintName == projectRelativeDeclHintName)
            {
                hasMultipleCandidates = candidateDocument is not null;
                candidateDocument ??= razorDocument;
            }
        }

        // Prefer a full-path match because project-relative hint names can collide. For either form,
        // refuse to choose arbitrarily when multiple Razor documents match.
        if (fullPathMatchedDocument is not null)
        {
            return hasMultipleFullPathMatches ? null : fullPathMatchedDocument;
        }

        return hasMultipleCandidates ? null : candidateDocument;
    }
}
