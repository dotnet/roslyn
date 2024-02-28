// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Diagnostics;
using System.Text.RegularExpressions;
using static Microsoft.CodeAnalysis.EditorConfig.LanguageConstants;

namespace Microsoft.CodeAnalysis.EditorConfig.Parsing;

internal readonly partial struct SectionMatcher
{
    private readonly ImmutableArray<(int minValue, int maxValue)> _numberRangePairs;
    private readonly string _headerText;

    private static readonly Regex s_multiFileWithDotOutside = new(@"\*\.\{(.*)\}", RegexOptions.Compiled);
    private static readonly Regex s_multiFileWithDotInside = new(@"\*\{(.*)\}", RegexOptions.Compiled);
    private static readonly Regex s_fileExtensionMatcher = new(@"([^,]+)", RegexOptions.Compiled);

    private Regex Regex { get; }

    private SectionMatcher(
            Regex regex,
            string headerText,
            ImmutableArray<(int minValue, int maxValue)> numberRangePairs)
    {
        Regex = regex;
        _numberRangePairs = numberRangePairs;
        _headerText = headerText;
    }

    public bool IsLanguageMatch(Language language, SectionMatch matchKind = default)
        => GetLanguageMatchKind(language) == matchKind;

    public bool IsPathMatch(string relativePath, SectionMatch matchKind = default)
    {
        var lowestMatch = (int)matchKind;
        return lowestMatch >= (int)GetPathMatchKind(relativePath);
    }

    public SectionMatch GetLanguageMatchKind(Language language)
    {
        if (IsExactLanguageMatch(language))
        {
            return SectionMatch.ExactLanguageMatch;
        }

        if (IsExactLanguageMatchWithOthers(language))
        {
            return SectionMatch.ExactLanguageMatchWithOthers;
        }

        if (IsAnyLanguageMatch(language))
        {
            return SectionMatch.AnyLanguageMatch;
        }

        if (IsFilePatternMatch(language))
        {
            if (IsSuperSet(language, _headerText))
            {
                return SectionMatch.SupersetFilePatternMatch;
            }

            return SectionMatch.FilePatternMatch;
        }

        return SectionMatch.NoMatch;
    }

    public SectionMatch GetPathMatchKind(string relativePath)
    {
        if (!relativePath.TryGetLanguageFromFilePath(out var language))
        {
            return SectionMatch.NoMatch;
        }

        if (IsExactLanguageMatch(language))
        {
            return SectionMatch.ExactLanguageMatch;
        }

        if (IsExactLanguageMatchWithOthers(language))
        {
            return SectionMatch.ExactLanguageMatchWithOthers;
        }

        if (IsAnyLanguageMatch(language))
        {
            return SectionMatch.AnyLanguageMatch;
        }

        if (IsPathMatch(relativePath))
        {
            if (IsSuperSet(language, _headerText))
            {
                return SectionMatch.SupersetFilePatternMatch;
            }

            return SectionMatch.FilePatternMatch;
        }

        return SectionMatch.NoMatch;
    }

    private bool IsExactLanguageMatch(Language language)
        => IsExactLanguageMatchForCSharp(language) ||
           IsExactLanguageMatchForVisualBasic(language) ||
           IsExactLanguageMatchForBothVisualBasicAndCSharp(language);

    private bool IsExactLanguageMatchForCSharp(Language language)
        => language.HasFlag(Language.CSharp) &&
           !language.HasFlag(Language.VisualBasic) &&
           _headerText == DefaultCSharpSplat;

    private bool IsExactLanguageMatchForVisualBasic(Language language)
        => language.HasFlag(Language.VisualBasic) &&
           !language.HasFlag(Language.CSharp) &&
           _headerText == DefaultVisualBasicSplat;

    private bool IsExactLanguageMatchForBothVisualBasicAndCSharp(Language language)
        => language.HasFlag(Language.CSharp) &&
           language.HasFlag(Language.VisualBasic) &&
           (MatchesBothLanguages(_headerText, s_multiFileWithDotOutside, DefaultCSharpExtensionWithoutDot, DefaultVisualBasicExtensionWithoutDot) ||
            MatchesBothLanguages(_headerText, s_multiFileWithDotInside, DefaultCSharpExtension, DefaultVisualBasicExtension));

    private static bool MatchesBothLanguages(
        string text,
        Regex pattern,
        string firstFileExtension,
        string secondFileExtension)
    {
        var matchCollection = pattern.Matches(text);
        if (matchCollection.Count == 1)
        {
            var singleMatch = matchCollection[0];
            if (singleMatch.Groups.Count == 2)
            {
                var innerText = singleMatch.Groups[1].Value;
                var fileExtensionMatches = s_fileExtensionMatcher.Matches(innerText);
                if (fileExtensionMatches.Count == 2)
                {
                    var firstExtension = fileExtensionMatches[0].Value;
                    var secondExtension = fileExtensionMatches[1].Value;
                    return (firstExtension == firstFileExtension && secondExtension == secondFileExtension) ||
                           (firstExtension == secondFileExtension && secondExtension == firstFileExtension);
                }
            }
        }

        return false;
    }

    private bool IsExactLanguageMatchWithOthers(Language language)
        => IsExactMatchForCSharpWithOthers(language) ||
           IsExactMatchForVisualBasicWithOthers(language) ||
           IsExactMatchForBothVisualBasicAndCSharpWithOthers(language);

    private bool IsExactMatchForCSharpWithOthers(Language language)
        => language.HasFlag(Language.CSharp) &&
           !language.HasFlag(Language.VisualBasic) &&
           (IsMatchWithOthers(_headerText, s_multiFileWithDotOutside, DefaultCSharpExtensionWithoutDot, DefaultVisualBasicExtensionWithoutDot) ||
            IsMatchWithOthers(_headerText, s_multiFileWithDotInside, DefaultCSharpExtension, DefaultVisualBasicExtension));

    private bool IsExactMatchForVisualBasicWithOthers(Language language)
        => language.HasFlag(Language.VisualBasic) &&
           !language.HasFlag(Language.CSharp) &&
           (IsMatchWithOthers(_headerText, s_multiFileWithDotOutside, DefaultVisualBasicExtensionWithoutDot, DefaultCSharpExtensionWithoutDot) ||
            IsMatchWithOthers(_headerText, s_multiFileWithDotInside, DefaultVisualBasicExtension, DefaultCSharpExtension));

    private static bool IsMatchWithOthers(
        string text,
        Regex pattern,
        string mustMatchFileExtension,
        string? mustNotMatchFileExtension = null)
    {
        var matchCollection = pattern.Matches(text);
        if (matchCollection.Count == 1)
        {
            var singleMatch = matchCollection[0];
            if (singleMatch.Groups.Count == 2)
            {
                var innerText = singleMatch.Groups[1].Value;
                var fileExtensionMatches = s_fileExtensionMatcher.Matches(innerText);
                var matchedRequiredExtension = false;
                var matchedForbiddenExtension = false;
                foreach (Match? fileExtensionMatch in fileExtensionMatches)
                {
                    if (fileExtensionMatch?.Value == mustMatchFileExtension)
                    {
                        matchedRequiredExtension = true;
                    }

                    if (mustNotMatchFileExtension is not null &&
                        fileExtensionMatch?.Value == mustNotMatchFileExtension)
                    {
                        matchedForbiddenExtension = true;
                    }
                }

                return matchedRequiredExtension && !matchedForbiddenExtension;
            }
        }

        return false;
    }

    private bool IsExactMatchForBothVisualBasicAndCSharpWithOthers(Language language)
        => language.HasFlag(Language.CSharp) &&
           language.HasFlag(Language.VisualBasic) &&
           (MatchesBothLanguagesWithOthers(_headerText, s_multiFileWithDotOutside, DefaultVisualBasicExtensionWithoutDot, DefaultCSharpExtensionWithoutDot) ||
            MatchesBothLanguagesWithOthers(_headerText, s_multiFileWithDotInside, DefaultVisualBasicExtension, DefaultCSharpExtension));

    private static bool MatchesBothLanguagesWithOthers(
        string text,
        Regex pattern,
        string firstFileExtension,
        string secondFileExtension)
    {
        var matchCollection = pattern.Matches(text);
        if (matchCollection.Count == 1)
        {
            var singleMatch = matchCollection[0];
            if (singleMatch.Groups.Count == 2)
            {
                var innerText = singleMatch.Groups[1].Value;
                var fileExtensionMatches = s_fileExtensionMatcher.Matches(innerText);
                var firstExtensionMatched = false;
                var secondExtensionMatched = false;
                foreach (Match? match in fileExtensionMatches)
                {
                    if (match?.Value == firstFileExtension)
                    {
                        firstExtensionMatched = true;
                    }

                    if (match?.Value == secondFileExtension)
                    {
                        secondExtensionMatched = true;
                    }
                }

                return firstExtensionMatched && secondExtensionMatched;
            }
        }

        return false;
    }

    private bool IsAnyLanguageMatch(Language language)
        => IsAnyLanguageMatchForCSharp(language) ||
           IsAnyLanguageMatchForVisualBasic(language) ||
           IsExactMatchForBothVisualBasicAndCSharpWithOthers(language);

    private bool IsAnyLanguageMatchForCSharp(Language language)
        => language.HasFlag(Language.CSharp) && !language.HasFlag(Language.VisualBasic) &&
           (IsMatchWithOthers(_headerText, s_multiFileWithDotOutside, DefaultCSharpExtensionWithoutDot) ||
            IsMatchWithOthers(_headerText, s_multiFileWithDotInside, DefaultCSharpExtension));

    private bool IsAnyLanguageMatchForVisualBasic(Language language)
        => language.HasFlag(Language.VisualBasic) && !language.HasFlag(Language.CSharp) &&
           (IsMatchWithOthers(_headerText, s_multiFileWithDotOutside, DefaultVisualBasicExtensionWithoutDot) ||
            IsMatchWithOthers(_headerText, s_multiFileWithDotInside, DefaultVisualBasicExtension));

    private bool IsFilePatternMatch(Language language)
        => IsCSharpFilePatternMatch(language) ||
           IsVisualBasicFilePatternMatch(language) ||
           IsPatternMatchForBothVisualBasicAndCSharp(language);

    private bool IsCSharpFilePatternMatch(Language language)
        => language.HasFlag(Language.CSharp) && !language.HasFlag(Language.VisualBasic) &&
           IsPathMatch(DefaultCSharpPath);

    private bool IsVisualBasicFilePatternMatch(Language language)
        => language.HasFlag(Language.VisualBasic) && !language.HasFlag(Language.CSharp) &&
           IsPathMatch(DefaultVisualBasicPath);

    private bool IsPatternMatchForBothVisualBasicAndCSharp(Language language)
        => language.HasFlag(Language.VisualBasic) && language.HasFlag(Language.CSharp) &&
           IsPathMatch(DefaultVisualBasicPath) && IsPathMatch(DefaultCSharpPath);

    private static bool IsSuperSet(Language language, string pattern)
        => IsCSharpSuperSet(language, pattern) ||
           IsVisualBasicSuperSet(language, pattern) ||
           IsCSharpOrVisualBasicSuperSet(language, pattern);

    private static bool IsCSharpSuperSet(Language language, string pattern)
        => language.HasFlag(Language.CSharp) && !language.HasFlag(Language.VisualBasic) &&
           !pattern.Contains(DefaultCSharpExtensionWithoutDot);

    private static bool IsVisualBasicSuperSet(Language language, string pattern)
        => language.HasFlag(Language.VisualBasic) && !language.HasFlag(Language.CSharp) &&
           !pattern.Contains(DefaultVisualBasicExtensionWithoutDot);

    private static bool IsCSharpOrVisualBasicSuperSet(Language language, string pattern)
        => language.HasFlag(Language.VisualBasic) && language.HasFlag(Language.VisualBasic) &&
           !(pattern.Contains(DefaultCSharpExtensionWithoutDot) && pattern.Contains(DefaultVisualBasicExtensionWithoutDot));

    private bool IsPathMatch(string s)
    {
        if (_numberRangePairs.IsEmpty)
        {
            return Regex.IsMatch(s);
        }

        var match = Regex.Match(s);
        if (!match.Success)
        {
            return false;
        }

        Debug.Assert(match.Groups.Count - 1 == _numberRangePairs.Length);
        for (var i = 0; i < _numberRangePairs.Length; i++)
        {
            var (minValue, maxValue) = _numberRangePairs[i];
            // Index 0 is the whole regex
            if (!int.TryParse(match.Groups[i + 1].Value, out var matchedNum) ||
                matchedNum < minValue ||
                matchedNum > maxValue)
            {
                return false;
            }
        }

        return true;
    }
}
