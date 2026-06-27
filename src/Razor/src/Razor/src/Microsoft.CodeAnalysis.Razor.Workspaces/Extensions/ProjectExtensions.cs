// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor;
using Microsoft.NET.Sdk.Razor.SourceGenerators;

namespace Microsoft.CodeAnalysis;

internal static class ProjectExtensions
{
    public static Task<SourceGeneratedDocument?> TryGetCSharpDocumentForGeneratedDocumentAsync(this Project project, SourceGeneratedDocumentIdentity identity, CancellationToken cancellationToken)
    {
        Debug.Assert(identity.DocumentId.ProjectId == project.Id, "Generated document URI does not belong to this project.");
        var hintName = identity.HintName;

        return TryGetSourceGeneratedDocumentFromHintNameAsync(project, hintName, cancellationToken);
    }

    /// <summary>
    /// Finds source generated documents by iterating through all of them. In OOP there are better options!
    /// </summary>
    public static async Task<SourceGeneratedDocument?> TryGetSourceGeneratedDocumentFromHintNameAsync(this Project project, string? hintName, CancellationToken cancellationToken)
    {
        // TODO: use this when the location is case-insensitive on windows (https://github.com/dotnet/roslyn/issues/76869)
        //var generator = typeof(RazorSourceGenerator);
        //var generatorAssembly = generator.Assembly;
        //var generatorName = generatorAssembly.GetName();
        //var generatedDocuments = await _project.GetSourceGeneratedDocumentsForGeneratorAsync(generatorName.Name!, generatorAssembly.Location, generatorName.Version!, generator.Name, cancellationToken).ConfigureAwait(false);

        var generatedDocuments = await project.GetSourceGeneratedDocumentsAsync(cancellationToken).ConfigureAwait(false);
        return generatedDocuments.SingleOrDefault(d => d.HintName == hintName);
    }

#if SONICDEV
    [System.Obsolete("PROTOTYPE(sonic): Call the overload that takes a bool to prove that you thought about which document to get")]
#endif
    public static async Task<SourceGeneratedDocument?> TryGetSourceGeneratedDocumentForRazorDocumentAsync(this Project project, TextDocument razorDocument, CancellationToken cancellationToken)
    {
        return (await TryGetSourceGeneratedDocumentsForRazorDocumentAsync(project, razorDocument, cancellationToken).ConfigureAwait(false))?.ImplDoc;
    }

    /// <summary>
    /// Finds source generated documents by iterating through all of them. In OOP there are better options!
    /// </summary>
    public static async Task<SourceGeneratedDocuments?> TryGetSourceGeneratedDocumentsForRazorDocumentAsync(this Project project, TextDocument razorDocument, CancellationToken cancellationToken)
    {
        if (razorDocument.FilePath is null)
        {
            return null;
        }

        var generatedDocuments = await project.GetSourceGeneratedDocumentsAsync(cancellationToken).ConfigureAwait(false);

        // There are two possible ways hint names come about: The Razor SDK will produce project relative paths for projects that use it
        // and for non-Razor SDK projects we fallback to using the full path for hint names. We can't easily detect which situation we're
        // in here, so the loop below handles both cases.

        // For misc files, and projects that don't have a globalconfig file (eg, non Razor SDK projects), the hint name will be based
        // on the full path of the file.
        var fullPathHintName = RazorSourceGenerator.GetIdentifierFromPath(razorDocument.FilePath);
        var fullPathDeclHintName = RazorSourceGenerator.GetDeclIdentifierFromHintName(fullPathHintName);

        // For normal Razor SDK projects, the hint name will be based on the project-relative path of the file.
        var projectRelativeHintName = GetProjectRelativeHintName(razorDocument);
        var projectRelativeDeclHintName = projectRelativeHintName is null ? null : RazorSourceGenerator.GetDeclIdentifierFromHintName(projectRelativeHintName);

        SourceGeneratedDocument? fullPathMatchedDoc = null;
        SourceGeneratedDocument? fullPathMatchedDeclDoc = null;

        SourceGeneratedDocument? candidateDoc = null;
        SourceGeneratedDocument? candidateDeclDoc = null;
        foreach (var doc in generatedDocuments)
        {
            if (!doc.IsRazorSourceGeneratedDocument())
            {
                continue;
            }

            if (doc.HintName == fullPathHintName)
            {
                // If the full path matches, we've found it for sure
                fullPathMatchedDoc = doc;
                // Can we stop looping?
                if (fullPathMatchedDeclDoc is not null)
                {
                    break;
                }
            }
            else if (doc.HintName == fullPathDeclHintName)
            {
                fullPathMatchedDeclDoc = doc;
                if (fullPathMatchedDoc is not null)
                {
                    break;
                }
            }
            else if (doc.HintName == projectRelativeHintName)
            {
                if (candidateDoc is not null)
                {
                    // Multiple documents with the same hint name found, can't be sure which one to return
                    // This can happen as a result of a bug in the source generator: https://github.com/dotnet/razor/issues/11578
                    // We can break the loop safely here, because if there are any project relative hint names, it means the project
                    // is using the Razor SDK, which will never produce full path hint names. Or someone is doing something _very_ weird.
                    Debug.Assert(fullPathMatchedDoc is null && fullPathMatchedDeclDoc is null, "We don't expect full matches and partial matches in the same project");
                    candidateDoc = null;
                    break;
                }

                candidateDoc = doc;
            }
            else if (doc.HintName == projectRelativeDeclHintName)
            {
                if (candidateDeclDoc is not null)
                {
                    candidateDoc = null;
                    break;
                }

                candidateDeclDoc = doc;
            }
        }

        // Full path matches take precedence over candidates
        if (fullPathMatchedDoc is not null)
        {
            return new(fullPathMatchedDoc, fullPathMatchedDeclDoc);
        }

        // If we didn't find a candidate impl doc, or found multiple, bail out so we don't confuse callers in case there is a weird
        // bug where it's possible to have duplicate impl docs and one decl doc
        if (candidateDoc is null)
        {
            return null;
        }

        return new(candidateDoc, candidateDeclDoc);

        static string? GetProjectRelativeHintName(TextDocument razorDocument)
        {
            var filePath = razorDocument.FilePath.AsSpanOrDefault();
            if (string.IsNullOrEmpty(razorDocument.Project.FilePath))
            {
                // Misc file - no project info to get a relative path
                return null;
            }

            var projectFilePath = razorDocument.Project.FilePath.AsSpanOrDefault();
            var projectBasePath = PathUtilities.GetDirectoryName(projectFilePath);
            if (filePath.Length <= projectBasePath.Length)
            {
                // File must be from outside the project directory
                return null;
            }

            var relativeDocumentPath = filePath[projectBasePath.Length..].TrimStart(['/', '\\']);

            return RazorSourceGenerator.GetIdentifierFromPath(relativeDocumentPath);
        }
    }
}

internal record struct SourceGeneratedDocuments(SourceGeneratedDocument ImplDoc, SourceGeneratedDocument? DeclDoc);
