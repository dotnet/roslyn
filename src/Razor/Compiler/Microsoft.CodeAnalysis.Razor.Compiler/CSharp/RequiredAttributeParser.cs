// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using Microsoft.AspNetCore.Razor;
using Microsoft.AspNetCore.Razor.Language;

namespace Microsoft.CodeAnalysis.Razor;

internal static class RequiredAttributeParser
{
    private const char RequiredAttributeWildcardSuffix = '*';

    private static readonly FrozenDictionary<char, RequiredAttributeValueComparison> s_cssValueComparisons =
        new Dictionary<char, RequiredAttributeValueComparison>
        {
            ['='] = RequiredAttributeValueComparison.FullMatch,
            ['^'] = RequiredAttributeValueComparison.PrefixMatch,
            ['$'] = RequiredAttributeValueComparison.SuffixMatch
        }.ToFrozenDictionary();

    private static readonly char[] s_whitespaceCharacters = [' ', '\t'];
    private static readonly char[] s_invalidPlainAttributeNameCharacters = [.. s_whitespaceCharacters, ',', RequiredAttributeWildcardSuffix];
    private static readonly char[] s_invalidCssAttributeNameCharacters = [.. s_whitespaceCharacters, ',', ']', .. s_cssValueComparisons.Keys];
    private static readonly char[] s_invalidCssQuotelessValueCharacters = [.. s_whitespaceCharacters, ']'];

    public static void AddRequiredAttributes(string input, TagMatchingRuleDescriptorBuilder ruleBuilder)
    {
        using var parser = new Parser(input);
        ParseResult result;

        do
        {
            result = parser.TryParseNextRequiredAttribute();

            if (result.Success || result.HasDiagnostics)
            {
                // If we failed to parse, we still want to add the attribute with the diagnostics.
                ruleBuilder.Attribute(builder =>
                {
                    builder.Name = result.Name;
                    builder.NameComparison = result.NameComparison;
                    builder.Value = result.Value;
                    builder.ValueComparison = result.ValueComparison;

                    if (result.HasDiagnostics)
                    {
                        builder.Diagnostics.AddRange(result.Diagnostics);
                    }
                });
            }
        }
        while (result.Success);
    }

    private readonly record struct ParseResult(
        bool Success,
        string Name,
        RequiredAttributeNameComparison NameComparison,
        string? Value,
        RequiredAttributeValueComparison ValueComparison,
        ImmutableArray<RazorDiagnostic> Diagnostics)
    {
        public bool HasDiagnostics => !Diagnostics.IsDefaultOrEmpty;

        public static ParseResult Failed(
            string name, RequiredAttributeNameComparison nameComparison,
            string? value, RequiredAttributeValueComparison valueComparison,
            ReadOnlySpan<RazorDiagnostic> diagnostics)
            => new(Success: false, name, nameComparison, value, valueComparison, [.. diagnostics]);

        public static ParseResult Succeeded(
            string name, RequiredAttributeNameComparison nameComparison,
            string? value, RequiredAttributeValueComparison valueComparison)
            => new(Success: true, name, nameComparison, value, valueComparison, Diagnostics: default);
    }

    private ref struct Parser(string input)
    {
        private readonly string _input = input;
        private ReadOnlySpan<char> _span = input.AsSpan();

        private MemoryBuilder<RazorDiagnostic> _diagnostics = new();
        public void Dispose()
        {
            _diagnostics.Dispose();
        }

        private readonly bool AtEnd
            => _span.IsEmpty;

        private readonly bool At(char c)
            => _span is [var ch, ..] && ch == c;

        private readonly char Current
            => _span[0];

        private void MoveNext()
        {
            Debug.Assert(!AtEnd, "Cannot move past the end of the input.");
            _span = _span[1..];
        }

        private void SkipWhitespace()
        {
            while (_span is [' ' or '\t', .. var remaining])
            {
                _span = remaining;
            }
        }

        private bool EnsureNotAtEnd()
        {
            if (!AtEnd)
            {
                return true;
            }

            _diagnostics.Append(
                RazorDiagnosticFactory.CreateTagHelper_CouldNotFindMatchingEndBrace(_input));

            return false;
        }

        public ParseResult TryParseNextRequiredAttribute()
        {
            if (AtEnd)
            {
                return default;
            }

            SkipWhitespace();

            string? name, value = null;
            RequiredAttributeNameComparison nameComparison = default;
            RequiredAttributeValueComparison valueComparison = default;

            _diagnostics.Length = 0;

            if (At('['))
            {
                if (!TryParseCssSelector(out name, out value, out valueComparison))
                {
                    return ParseResult.Failed(name, nameComparison, value, valueComparison, _diagnostics.AsMemory().Span);
                }
            }
            else
            {
                (name, nameComparison) = ParsePlainSelector();
            }

            SkipWhitespace();

            if (At(','))
            {
                // Move past the comma
                MoveNext();

                if (!EnsureNotAtEnd())
                {
                    return ParseResult.Failed(name, nameComparison, value, valueComparison, _diagnostics.AsMemory().Span);
                }
            }
            else if (!AtEnd)
            {
                _diagnostics.Append(
                    RazorDiagnosticFactory.CreateTagHelper_InvalidRequiredAttributeCharacter(Current, _input));

                return ParseResult.Failed(name, nameComparison, value, valueComparison, _diagnostics.AsMemory().Span);
            }

            Debug.Assert(_diagnostics.Length == 0, "Diagnostics should only be added if we fail to parse.");
            return ParseResult.Succeeded(name, nameComparison, value, valueComparison);
        }

        private (string name, RequiredAttributeNameComparison nameComparison) ParsePlainSelector()
        {
            string name;
            var nameComparison = RequiredAttributeNameComparison.FullMatch;

            var nameEndIndex = _span.IndexOfAny(s_invalidPlainAttributeNameCharacters);

            if (nameEndIndex == -1)
            {
                name = _span.ToString();
                _span = [];

                return (name, nameComparison);
            }

            name = _span[..nameEndIndex].ToString();
            _span = _span[nameEndIndex..];

            if (Current == RequiredAttributeWildcardSuffix)
            {
                nameComparison = RequiredAttributeNameComparison.PrefixMatch;

                // Move past wild card
                MoveNext();
            }

            return (name, nameComparison);
        }

        private bool TryParseCssValue(out string? value)
        {
            if (At('\'') || At('"'))
            {
                var quote = Current;

                // Move past the quote
                MoveNext();

                // Find the next quote
                var nextQuoteIndex = _span.IndexOf(quote);

                if (nextQuoteIndex == -1)
                {
                    _diagnostics.Append(
                        RazorDiagnosticFactory.CreateTagHelper_InvalidRequiredAttributeMismatchedQuotes(quote, _input));

                    value = null;
                    return false;
                }

                value = _span[..nextQuoteIndex].ToString();
                _span = _span[(nextQuoteIndex + 1)..];
                return true;
            }

            var valueEndIndex = _span.IndexOfAny(s_invalidCssQuotelessValueCharacters);

            if (valueEndIndex == -1)
            {
                valueEndIndex = _span.Length;
            }

            value = _span[..valueEndIndex].ToString();
            _span = _span[valueEndIndex..];

            return true;
        }

        private bool TryParseCssSelector(out string name, out string? value, out RequiredAttributeValueComparison valueComparison)
        {
            Debug.Assert(At('['));

            // Move past '['.
            MoveNext();
            SkipWhitespace();

            name = ParseCssAttributeName();

            value = null;
            valueComparison = default;

            SkipWhitespace();

            if (!EnsureNotAtEnd())
            {
                return false;
            }

            if (!TryParseCssValueComparison(out valueComparison))
            {
                return false;
            }

            SkipWhitespace();

            if (!EnsureNotAtEnd())
            {
                return false;
            }

            if (valueComparison != RequiredAttributeValueComparison.None &&
                !TryParseCssValue(out value))
            {
                return false;
            }

            SkipWhitespace();

            if (At(']'))
            {
                // Move past the ending bracket.
                MoveNext();
                return true;
            }

            else if (AtEnd)
            {
                _diagnostics.Append(
                    RazorDiagnosticFactory.CreateTagHelper_CouldNotFindMatchingEndBrace(_input));
            }
            else
            {
                _diagnostics.Append(
                    RazorDiagnosticFactory.CreateTagHelper_InvalidRequiredAttributeCharacter(Current, _input));
            }

            return false;
        }

        private string ParseCssAttributeName()
        {
            var nameEndIndex = _span.IndexOfAny(s_invalidCssAttributeNameCharacters);

            if (nameEndIndex == -1)
            {
                nameEndIndex = _span.Length;
            }

            var result = _span[..nameEndIndex].ToString();
            _span = _span[nameEndIndex..];

            return result;
        }

        /// <summary>
        ///  Parse ^=, $=, or just = as a required attribute value comparison.
        /// </summary>
        private bool TryParseCssValueComparison(out RequiredAttributeValueComparison valueComparison)
        {
            Debug.Assert(!AtEnd);

            var ch = Current;

            if (s_cssValueComparisons.TryGetValue(ch, out valueComparison))
            {
                MoveNext();

                // If the character was an '=', we're done.
                if (ch == '=')
                {
                    return true;
                }

                // If the character was an '^' or '$' and the second character is an '=',
                // then we have a two-character operator (ex: ^= or $=).
                if (At('='))
                {
                    MoveNext();
                    return true;
                }

                // We're at an incomplete operator (ex: [foo^]
                _diagnostics.Append(
                    RazorDiagnosticFactory.CreateTagHelper_PartialRequiredAttributeOperator(ch, _input));

                valueComparison = default;
                return false;
            }

            if (!At(']'))
            {
                _diagnostics.Append(
                    RazorDiagnosticFactory.CreateTagHelper_InvalidRequiredAttributeOperator(ch, _input));

                valueComparison = default;
                return false;
            }

            valueComparison = RequiredAttributeValueComparison.None;
            return true;
        }
    }
}
