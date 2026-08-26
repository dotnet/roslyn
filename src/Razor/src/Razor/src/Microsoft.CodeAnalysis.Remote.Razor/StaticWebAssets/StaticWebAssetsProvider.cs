// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Immutable;
using System.Composition;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Remote.Razor.ProjectSystem;

namespace Microsoft.CodeAnalysis.Remote.Razor.StaticWebAssets;

/// <summary>
/// Supplies the static web asset keys available to a project, as declared by the IntelliSense
/// manifest the SDK flows in as an additional file.
/// </summary>
/// <remarks>
/// Arriving as an additional file rather than a file read off disk is what makes this cheap to keep
/// current: the manifest is part of the workspace snapshot, so a rebuild that changes it produces a
/// new document version and invalidates the cached parse with no watcher or polling involved.
/// </remarks>
[Export(typeof(StaticWebAssetsProvider)), Shared]
internal sealed class StaticWebAssetsProvider
{
    private const string ManifestFileName = "staticwebassets.intellisense.json";
    private const string ManifestMetadataKey = "build_metadata.AdditionalFiles.IsStaticWebAssetsManifest";

    private readonly ConditionalWeakTable<TextDocument, StrongBox<ImmutableArray<string>>> _cache = new();

    /// <summary>
    /// Returns the asset keys for <paramref name="projectSnapshot"/>, or an empty array when the
    /// project has no manifest -- an older SDK, a project that has never been built, or one that
    /// isn't a web project at all.
    /// </summary>
    public async ValueTask<ImmutableArray<string>> GetAssetsAsync(RemoteProjectSnapshot projectSnapshot, CancellationToken cancellationToken)
    {
        if (TryFindManifestDocument(projectSnapshot.Project) is not { } manifestDocument)
        {
            return [];
        }

        // Keyed on the document instance because Roslyn documents are immutable: an edit or rebuild
        // produces a different instance, which misses the cache and reparses.
        if (_cache.TryGetValue(manifestDocument, out var cached))
        {
            return cached.Value;
        }

        var text = await manifestDocument.GetTextAsync(cancellationToken).ConfigureAwait(false);
        var assets = StaticWebAssetsManifestReader.Read(text);

        return _cache.GetValue(manifestDocument, _ => new StrongBox<ImmutableArray<string>>(assets)).Value;
    }

    private static TextDocument? TryFindManifestDocument(Project project)
    {
        var optionsProvider = project.AnalyzerOptions.AnalyzerConfigOptionsProvider;

        foreach (var document in project.AdditionalDocuments)
        {
            // Checking the name first keeps the common case -- a project whose additional files are
            // all .razor -- from querying analyzer config options once per document.
            if (document.FilePath is not { } filePath ||
                !string.Equals(Path.GetFileName(filePath), ManifestFileName, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            foreach (var additionalText in project.AnalyzerOptions.AdditionalFiles)
            {
                if (!string.Equals(additionalText.Path, filePath, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                if (optionsProvider.GetOptions(additionalText).TryGetValue(ManifestMetadataKey, out var value) &&
                    string.Equals(value, "true", StringComparison.OrdinalIgnoreCase))
                {
                    return document;
                }
            }
        }

        return null;
    }
}
