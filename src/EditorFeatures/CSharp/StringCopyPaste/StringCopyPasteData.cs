// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text;
using Microsoft.CodeAnalysis.ErrorReporting;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.CodeAnalysis.EmbeddedLanguages.VirtualChars;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;
using Microsoft.CodeAnalysis.PooledObjects;

namespace Microsoft.CodeAnalysis.Editor.CSharp.StringCopyPaste
{
    internal class StringCopyPasteData
    {
        public ImmutableArray<StringCopyPasteContent> Contents { get; }

        [JsonConstructor]
        public StringCopyPasteData(ImmutableArray<StringCopyPasteContent> contents)
        {
            Contents = contents;
        }

        public string? ToJson()
        {
            try
            {
                return JsonSerializer.Serialize(this, typeof(StringCopyPasteData));
            }
            catch (Exception ex) when (FatalError.ReportAndCatch(ex, ErrorSeverity.Critical))
            {
            }

            return null;
        }

        public static StringCopyPasteData? FromJson(string? json)
        {
            if (string.IsNullOrWhiteSpace(json))
                return null;

            try
            {
                var value = JsonSerializer.Deserialize(JsonDocument.Parse(json), typeof(StringCopyPasteData));
                if (value is null)
                    return null;

                return (StringCopyPasteData)value;
            }
            catch (Exception ex) when (FatalError.ReportAndCatch(ex, ErrorSeverity.Critical))
            {
            }

            return null;
        }

        /// <summary>
        /// Given a <paramref name="stringExpression"/> for a string literal or interpolated string, and the <paramref
        /// name="span"/> the user has selected in it, tries to determine the interpreted content within that expression
        /// that has been copied.  "interpreted" in this context means the actual value of the content that was selected,
        /// with things like escape characters embedded as the actual characters they represent.
        /// </summary>
        public static StringCopyPasteData? TryCreate(IVirtualCharLanguageService virtualCharService, ExpressionSyntax stringExpression, TextSpan span)
            => stringExpression switch
            {
                LiteralExpressionSyntax literal => TryCreateForLiteral(virtualCharService, literal, span),
                InterpolatedStringExpressionSyntax interpolatedString => TryCreateForInterpolatedString(virtualCharService, interpolatedString, span),
                _ => throw ExceptionUtilities.UnexpectedValue(stringExpression.Kind()),
            };

        private static StringCopyPasteData? TryCreateForLiteral(IVirtualCharLanguageService virtualCharService, LiteralExpressionSyntax literal, TextSpan span)
            => TryGetContentForSpan(virtualCharService, literal.Token, span, out var content)
                ? new StringCopyPasteData(ImmutableArray.Create(content))
                : null;

        /// <summary>
        /// Given a string <paramref name="token"/>, and the <paramref name="span"/> the user has selected that overlaps
        /// with it, tries to determine the interpreted content within that token that has been copied. "interpreted" in
        /// this context means the actual value of the content that was selected, with things like escape characters
        /// embedded as the actual characters they represent.
        /// </summary>
        private static bool TryGetNormalizedStringForSpan(
            IVirtualCharLanguageService virtualCharService,
            SyntaxToken token,
            TextSpan span,
            [NotNullWhen(true)] out string? normalizedText)
        {
            normalizedText = null;

            // First, try to convert this token to a sequence of virtual chars.
            var virtualChars = virtualCharService.TryConvertToVirtualChars(token);
            if (virtualChars.IsDefaultOrEmpty)
                return false;

            // Then find the start/end of the token's characters that overlap with the selection span.
            var firstOverlappingChar = virtualChars.FirstOrNull(vc => vc.Span.OverlapsWith(span));
            var lastOverlappingChar = virtualChars.LastOrNull(vc => vc.Span.OverlapsWith(span));

            if (firstOverlappingChar is null || lastOverlappingChar is null)
                return false;

            // Don't allow partial selection of an escaped character.  e.g. if they select 'n' in '\n'
            if (span.Start > firstOverlappingChar.Value.Span.Start)
                return false;

            if (span.End < lastOverlappingChar.Value.Span.End)
                return false;

            var firstCharIndexInclusive = virtualChars.IndexOf(firstOverlappingChar.Value);
            var lastCharIndexInclusive = virtualChars.IndexOf(lastOverlappingChar.Value);

            // Grab that subsequence of characters and get the final interpreted string for it.
            var subsequence = virtualChars.GetSubSequence(TextSpan.FromBounds(firstCharIndexInclusive, lastCharIndexInclusive + 1));
            normalizedText = subsequence.CreateString();
            return true;
        }

        private static bool TryGetContentForSpan(
            IVirtualCharLanguageService virtualCharService,
            SyntaxToken token,
            TextSpan span,
            out StringCopyPasteContent content)
        {
            if (!TryGetNormalizedStringForSpan(virtualCharService, token, span, out var text))
            {
                content = default;
                return false;
            }
            else
            {
                content = StringCopyPasteContent.ForText(text);
                return true;
            }
        }

        private static StringCopyPasteData? TryCreateForInterpolatedString(
            IVirtualCharLanguageService virtualCharService,
            InterpolatedStringExpressionSyntax interpolatedString,
            TextSpan span)
        {
            using var _ = ArrayBuilder<StringCopyPasteContent>.GetInstance(out var result);

            foreach (var interpolatedContent in interpolatedString.Contents)
            {
                if (interpolatedContent.Span.OverlapsWith(span))
                {
                    if (interpolatedContent is InterpolationSyntax interpolation)
                    {
                        // If the user copies a portion of an interpolation, just treat this as a non-smart copy paste for simplicity.
                        if (!span.Contains(interpolation.Span))
                            return null;

                        var formatClause = (string?)null;
                        if (interpolation.FormatClause != null &&
                            !TryGetNormalizedStringForSpan(virtualCharService, interpolation.FormatClause.FormatStringToken, span, out formatClause))
                        {
                            return null;
                        }

                        result.Add(StringCopyPasteContent.ForInterpolation(
                            interpolation.Expression.ToFullString(),
                            interpolation.AlignmentClause?.ToFullString(),
                            formatClause));
                    }
                    else if (interpolatedContent is InterpolatedStringTextSyntax stringText)
                    {
                        if (!TryGetContentForSpan(virtualCharService, stringText.TextToken, span, out var content))
                            return null;

                        result.Add(content);
                    }
                }
            }

            return new StringCopyPasteData(result.ToImmutable());
        }
    }

    internal enum StringCopyPasteContentKind
    {
        Text,           // When text content is copied.
        Interpolation,  // When an interpolation is copied.
    }

    internal readonly struct StringCopyPasteContent
    {
        public StringCopyPasteContentKind Kind { get; }

        /// <summary>
        /// The actual string value for <see cref="StringCopyPasteContentKind.Text"/>.  <see langword="null"/> for <see
        /// cref="StringCopyPasteContentKind.Interpolation"/>.
        /// </summary>
        public string? TextValue { get; }

        /// <summary>
        /// The actual string value for <see cref="InterpolationSyntax.Expression"/> for <see
        /// cref="StringCopyPasteContentKind.Interpolation"/>.  <see langword="null"/> for <see
        /// cref="StringCopyPasteContentKind.Text"/>.
        /// </summary>
        public string? InterpolationExpression { get; }

        /// <summary>
        /// The actual string value for <see cref="InterpolationSyntax.AlignmentClause"/> for <see
        /// cref="StringCopyPasteContentKind.Interpolation"/>.  <see langword="null"/> for <see
        /// cref="StringCopyPasteContentKind.Text"/>.
        /// </summary>
        public string? InterpolationAlignmentClause { get; }

        /// <summary>
        /// The actual string value for <see cref="InterpolationSyntax.FormatClause"/> for <see
        /// cref="StringCopyPasteContentKind.Interpolation"/>.  <see langword="null"/> for <see
        /// cref="StringCopyPasteContentKind.Text"/>.
        /// </summary>
        public string? InterpolationFormatClause { get; }

        [JsonConstructor]
        public StringCopyPasteContent(
            StringCopyPasteContentKind kind,
            string? textValue,
            string? interpolationExpression,
            string? interpolationAlignmentClause,
            string? interpolationFormatClause)
        {
            Kind = kind;
            TextValue = textValue;
            InterpolationExpression = interpolationExpression;
            InterpolationAlignmentClause = interpolationAlignmentClause;
            InterpolationFormatClause = interpolationFormatClause;
        }

        [JsonIgnore]
        [MemberNotNullWhen(true, nameof(TextValue))]
        public bool IsText => Kind == StringCopyPasteContentKind.Text;

        [JsonIgnore]
        [MemberNotNullWhen(true, nameof(InterpolationExpression))]
        public bool IsInterpolation => Kind == StringCopyPasteContentKind.Interpolation;

        public static StringCopyPasteContent ForText(string text)
            => new(StringCopyPasteContentKind.Text, text, null, null, null);

        public static StringCopyPasteContent ForInterpolation(string expression, string? alignmentClause, string? formatClause)
            => new(StringCopyPasteContentKind.Interpolation, null, expression, alignmentClause, formatClause);
    }
}
