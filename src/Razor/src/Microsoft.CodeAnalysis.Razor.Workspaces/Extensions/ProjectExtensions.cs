// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.CodeAnalysis.ExternalAccess.Razor;
using Microsoft.CodeAnalysis.Razor;
using Microsoft.NET.Sdk.Razor.SourceGenerators;

namespace Microsoft.CodeAnalysis;

internal static class ProjectExtensions
{
    /// <summary>
    ///  Gets the available <see cref="TagHelperDescriptor">tag helpers</see> from the specified
    ///  <see cref="Project"/> using the given <see cref="RazorProjectEngine"/>.
    /// </summary>
    public static async ValueTask<TagHelperCollection> GetTagHelpersAsync(
        this Project project,
        RazorProjectEngine projectEngine,
        CancellationToken cancellationToken)
    {
        if (!projectEngine.Engine.TryGetFeature(out ITagHelperDiscoveryService? discoveryService))
        {
            return [];
        }

        var compilation = await project.GetCompilationAsync(cancellationToken).ConfigureAwait(false);
        if (compilation is null || !CompilationTagHelperFeature.IsValidCompilation(compilation))
        {
            return [];
        }

        const TagHelperDiscoveryOptions Options = TagHelperDiscoveryOptions.ExcludeHidden |
                                                  TagHelperDiscoveryOptions.IncludeDocumentation;

        return discoveryService.GetTagHelpers(compilation, Options, cancellationToken);
    }

    public static Task<SourceGeneratedDocument?> TryGetCSharpDocumentForGeneratedDocumentAsync(this Project project, RazorGeneratedDocumentIdentity identity, CancellationToken cancellationToken)
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

    /// <summary>
    /// Finds source generated documents by iterating through all of them. In OOP there are better options!
    /// </summary>
    public static async Task<SourceGeneratedDocument?> TryGetSourceGeneratedDocumentForRazorDocumentAsync(this Project project, TextDocument razorDocument, CancellationToken cancellationToken)
    {
        if (razorDocument.FilePath is null)
        {
            return null;
        }

        var generatedDocuments = await project.GetSourceGeneratedDocumentsAsync(cancellationToken).ConfigureAwait(false);

        // For misc files, and projects that don't have a globalconfig file (eg, non Razor SDK projects), the hint name will be based
        // on the full path of the file.
        var fullPathHintName = RazorSourceGenerator.GetIdentifierFromPath(razorDocument.FilePath);
        // For normal Razor SDK projects, the hint name will be based on the project-relative path of the file.
        var projectRelativeHintName = GetProjectRelativeHintName(razorDocument);

        SourceGeneratedDocument? candidateDoc = null;
        foreach (var doc in generatedDocuments)
        {
            if (!doc.IsRazorSourceGeneratedDocument())
            {
                continue;
            }

            if (doc.HintName == fullPathHintName)
            {
                // If the full path matches, we've found it for sure
                return doc;
            }
            else if (doc.HintName == projectRelativeHintName)
            {
                if (candidateDoc is not null)
                {
                    // Multiple documents with the same hint name found, can't be sure which one to return
                    // This can happen as a result of a bug in the source generator: https://github.com/dotnet/razor/issues/11578
                    candidateDoc = null;
                    break;
                }

                candidateDoc = doc;
            }
        }

        return candidateDoc;

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
