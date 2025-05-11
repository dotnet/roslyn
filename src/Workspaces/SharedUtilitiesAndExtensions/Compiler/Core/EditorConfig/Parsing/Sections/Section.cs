// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.IO;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.EditorConfig.Parsing;

internal sealed record class Section
{
    public string? FilePath { get; init; }
    public bool IsGlobal { get; init; }
    public TextSpan Span { get; init; }
    public string Text { get; init; }
    public string FullText { get; init; }

    private bool IsSplatHeader => Text.Equals("*", StringComparison.Ordinal);
    private readonly SectionMatcher? _matcher;
    private readonly string? _containingDirectory;

    public Section(string? filePath, bool isGlobal, TextSpan span, string text, string fullText)
    {
        FilePath = filePath;
        IsGlobal = isGlobal;
        Span = span;
        Text = text;
        FullText = fullText;

        if (SectionMatcher.TryParseSection(Text, out var matcher))
        {
            _matcher = matcher;
        }

        if (FilePath is not null)
        {
            var containingDirectory = Path.GetDirectoryName(FilePath);
            RoslynDebug.AssertNotNull(containingDirectory);
            _containingDirectory = containingDirectory.ToLowerInvariant();
        }
    }

    /// <summary>
    /// Returns the default section header text for the given language combination
    /// </summary>
    /// <param name="language">The language combination to find the default header text for.</param>
    /// <returns>the default header text.</returns>
    public static string? GetHeaderTextForLanguage(Language language)
    {
        if (language.HasFlag(Language.CSharp) && language.HasFlag(Language.VisualBasic))
        {
            return "[*.{cs,vb}]\r\n";
        }
        else if (language.HasFlag(Language.CSharp))
        {
            return "[*.cs]\r\n";
        }
        else if (language.HasFlag(Language.VisualBasic))
        {
            return "[*.vb]\r\n";
        }

        return null;
    }

    /// <summary>
    /// Checks where this header supports the given language for the given match criteria
    /// </summary>
    /// <param name="language">The language to check support for.</param>
    /// <param name="matchKind">The criteria for which we consider a language a mache the default is <see cref="SectionMatch.ExactLanguageMatch"/>.</param>
    /// <returns>If this section is a match for the given language, meaning options can be added here.</returns>
    public bool SupportsLanguage(Language language, SectionMatch matchKind = default)
        => GetMatchKind(language).IsBetterOrEqualMatchThan(matchKind);

    /// <summary>
    /// Checks where this header supports the given file path for the given match criteria
    /// </summary>
    /// <param name="codeFilePath">full path to a file</param>
    /// <param name="matchKind">The criteria for which we consider a language a mache the default is <see cref="SectionMatch.ExactLanguageMatch"/>.</param>
    /// <remarks>
    /// If the section header cannot be parsed because it it invalid this method will always return no match.
    /// If no file path was given in the operation that produces this section and a relative path comparison is required to check for support this method will return no match.
    /// </remarks>
    /// <returns>If this section is a match for the given file, meaning options can be added here.</returns>
    public bool SupportsFilePath(string codeFilePath, SectionMatch matchKind = default)
        => GetMatchKind(codeFilePath).IsBetterOrEqualMatchThan(matchKind);

    public SectionMatch GetMatchKind(Language language)
    {
        if (IsGlobal)
        {
            return SectionMatch.GlobalSectionMatch;
        }

        if (IsSplatHeader)
        {
            return SectionMatch.SplatMatch;
        }

        if (_matcher is not { } matcher)
        {
            // the header could not be parsed
            return SectionMatch.NoMatch;
        }

        return matcher.GetLanguageMatchKind(language);
    }

    public SectionMatch GetMatchKind(string codeFilePath)
    {
        if (IsGlobal)
        {
            return SectionMatch.GlobalSectionMatch;
        }

        if (IsSplatHeader)
        {
            return SectionMatch.SplatMatch;
        }

        if (_matcher is not { } matcher)
        {
            // the header could not be parsed
            return SectionMatch.NoMatch;
        }

        if (!codeFilePath.TryGetLanguageFromFilePath(out var language))
        {
            // the file is from an unknown language
            return SectionMatch.NoMatch;
        }

        var languageMatchKind = matcher.GetLanguageMatchKind(language);
        if (_containingDirectory is null)
        {
            if (languageMatchKind.IsWorseMatchThan(SectionMatch.AnyLanguageMatch))
            {
                // no editorconfig path was given and we need to
                // evaluate paths relative to each other to give an answer
                throw new InvalidOperationException("No path given for editorconfig");
            }

            return languageMatchKind;
        }

        var relativePath = GetPathRelativeToEditorconfig(_containingDirectory, codeFilePath);
        if (relativePath.IsEmpty())
        {
            // we _are_ the editorconfig file so we match
            return SectionMatch.SupersetFilePatternMatch;
        }

        return matcher.GetPathMatchKind(relativePath);

        static string GetPathRelativeToEditorconfig(string directoryContainingEditorconfig, string codeFilePath)
        {
            var relativePath = PathUtilities.GetRelativePath(directoryContainingEditorconfig, codeFilePath);
            relativePath = relativePath.Replace("\\", "/");
            if (!relativePath.StartsWith("/"))
            {
                relativePath = "/" + relativePath;
            }

            return relativePath;
        }
    }

    public bool Equals(Section? other)
        => ReferenceEquals(this, other) ||
                      (other is not null &&
                       StringComparer.OrdinalIgnoreCase.Equals(FilePath, other.FilePath) &&
                       Span == other.Span &&
                       Text == other.Text);

    public override int GetHashCode()
        => Hash.Combine(
            StringComparer.OrdinalIgnoreCase.GetHashCode(FilePath ?? string.Empty),
            Hash.Combine(
                Span.GetHashCode(),
                Text.GetHashCode()));
}
