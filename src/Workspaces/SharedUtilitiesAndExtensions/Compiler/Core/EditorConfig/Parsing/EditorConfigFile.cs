// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.EditorConfig.Parsing;

/// <summary>
/// Base representation of an editorconfig file that has been parsed
/// </summary>
/// <typeparam name="T">The kind of options that we expect to encounter in the editorconfig file.</typeparam>
/// <param name="FilePath">The full path to the editorconfig file on disk. Optional if not doing pathwise comparisons</param>
/// <param name="Options">The set of options that were discovered in the file.</param>
internal record class EditorConfigFile<T>(string? FilePath, ImmutableArray<T> Options)
    where T : EditorConfigOption
{

    private readonly Lazy<ImmutableArray<Section>> _sections = new(() => Options.SelectAsArray(x => x.Section).Distinct());

    public ImmutableArray<Section> Sections => _sections.Value;

    /// <summary>
    /// Attempts to find a section of the editorconfig file that is an exact match for the given language.
    /// </summary>
    public bool TryGetSectionForLanguage(
        Language language,
        [NotNullWhen(true)] out Section? sectionResult)
        => TryGetSectionForLanguage(language, SectionMatch.ExactLanguageMatch, out sectionResult);

    /// <summary>
    /// Attempts to find a section of the editorconfig file that applies to the given language for the given criteria.
    /// </summary>
    public bool TryGetSectionForLanguage(
        Language language,
        SectionMatch matchKind,
        [NotNullWhen(true)] out Section? sectionResult)
    {
        sectionResult = Sections
            .Select(section => (matchKind: section.GetMatchKind(language), section))
            .Where(tuple => tuple.matchKind.IsBetterOrEqualMatchThan(matchKind))
            .OrderBy(x => x.matchKind)
            .ThenByDescending(x => x.section.Span.Start)
            .Select(x => x.section)
            .FirstOrDefault();
        return sectionResult is not null;
    }

    /// <summary>
    /// Attempts to find a section of the editorconfig file that applies to the given file.
    /// </summary>
    public bool TryGetSectionForFilePath(
        string filePath,
        [NotNullWhen(true)] out Section? sectionResult)
    {
        if (FilePath is null)
        {
            throw new InvalidOperationException("No path was given for this editorconfig file");
        }

        return TryGetSectionForFilePath(filePath, SectionMatch.ExactLanguageMatch, out sectionResult);
    }

    /// <summary>
    /// Attempts to find a section of the editorconfig file that applies to the given file for the given criteria.
    /// </summary>
    public bool TryGetSectionForFilePath(
        string filePath,
        SectionMatch matchKind,
        [NotNullWhen(true)] out Section? sectionResult)
    {
        if (FilePath is null)
        {
            throw new InvalidOperationException("No path was given for this editorconfig file");
        }

        sectionResult = Sections
            .SelectAsArray(section => (matchKind: section.GetMatchKind(filePath), section))
            .WhereAsArray(tuple => tuple.matchKind.IsBetterOrEqualMatchThan(matchKind))
            .OrderBy(x => x.matchKind) // Sort by best match kind
            .ThenByDescending(x => x.section.Span.Start) // in event of a further tie, pick entry at the bottom of the file
            .Select(x => x.section)
            .FirstOrDefault();
        return sectionResult is not null;
    }
}
