// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor;
using Microsoft.CodeAnalysis.ExternalAccess.Razor;
using Microsoft.CodeAnalysis.Razor;

namespace Microsoft.CodeAnalysis;

internal static class SolutionExtensions
{
    public static ImmutableArray<DocumentId> GetDocumentIdsWithUri(this Solution solution, Uri uri)
    {
        Debug.Assert(RazorUri.IsGeneratedDocumentUri(uri) == false, "This won't work with source generated Uris");
        return solution.GetDocumentIdsWithFilePath(uri.GetDocumentFilePath());
    }

    public static bool TryGetRazorDocument(this Solution solution, Uri razorDocumentUri, [NotNullWhen(true)] out TextDocument? razorDocument)
    {
        var razorDocumentId = solution.GetDocumentIdsWithUri(razorDocumentUri).FirstOrDefault();

        // If we couldn't locate the .razor file, just return the generated file.
        if (razorDocumentId is null ||
            solution.GetAdditionalDocument(razorDocumentId) is not TextDocument document)
        {
            razorDocument = null;
            return false;
        }

        razorDocument = document;
        return true;
    }

    public static bool TryGetRazorDocument(this Solution solution, string filePath, [NotNullWhen(true)] out TextDocument? razorDocument)
    {
        var razorDocumentId = solution.GetDocumentIdsWithFilePath(filePath).FirstOrDefault();

        // If we couldn't locate the .razor file, just return the generated file.
        if (razorDocumentId is null ||
            solution.GetAdditionalDocument(razorDocumentId) is not TextDocument document)
        {
            razorDocument = null;
            return false;
        }

        razorDocument = document;
        return true;
    }

    public static bool TryGetProject(this Solution solution, ProjectId projectId, [NotNullWhen(true)] out Project? result)
    {
        result = solution.GetProject(projectId);
        return result is not null;
    }

    public static Project GetRequiredProject(this Solution solution, ProjectId projectId)
    {
        return solution.GetProject(projectId)
            ?? ThrowHelper.ThrowInvalidOperationException<Project>($"The project {projectId} did not exist in {solution}.");
    }

    public static bool TryGetDocument(this Solution solution, DocumentId documentId, [NotNullWhen(true)] out Document? result)
    {
        result = solution.GetDocument(documentId);
        return result is not null;
    }

    public static Document GetRequiredDocument(this Solution solution, DocumentId documentId)
    {
        return solution.GetDocument(documentId)
            ?? ThrowHelper.ThrowInvalidOperationException<Document>($"The document {documentId} did not exist in {solution.FilePath ?? "solution"}.");
    }

    public static bool TryGetSourceGeneratedDocumentIdentity(this Solution solution, Uri generatedDocumentUri, out RazorGeneratedDocumentIdentity identity)
    {
        identity = default;
        if (!RazorUri.IsGeneratedDocumentUri(generatedDocumentUri))
        {
            return false;
        }

        identity = RazorUri.GetIdentityOfGeneratedDocument(solution, generatedDocumentUri);

        if (!identity.IsRazorSourceGeneratedDocument())
        {
            return false;
        }

        return true;
    }

    public static async Task<SourceGeneratedDocument?> TryGetSourceGeneratedDocumentAsync(this Solution solution, Uri generatedDocumentUri, CancellationToken cancellationToken)
    {
        if (!solution.TryGetSourceGeneratedDocumentIdentity(generatedDocumentUri, out var identity) ||
            !solution.TryGetProject(identity.DocumentId.ProjectId, out var project))
        {
            return null;
        }

        return await project.TryGetCSharpDocumentForGeneratedDocumentAsync(identity, cancellationToken).ConfigureAwait(false);
    }
}
