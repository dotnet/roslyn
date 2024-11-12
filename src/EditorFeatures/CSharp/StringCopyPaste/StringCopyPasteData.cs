// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.ErrorReporting;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.CodeAnalysis.EmbeddedLanguages.VirtualChars;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;
using Microsoft.CodeAnalysis.PooledObjects;

namespace Microsoft.CodeAnalysis.Editor.CSharp.StringCopyPaste;

/// <summary>
/// Data about a string that a user has copied a subsection of. This will itself be placed on the clipboard so that
/// it can be retrieved later on if the user pastes.
/// </summary>
[method: JsonConstructor]
internal class StringCopyPasteData(ImmutableArray<StringCopyPasteContent> contents)
{
    public ImmutableArray<StringCopyPasteContent> Contents { get; } = contents;

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
    /// name="selectionSpan"/> the user has selected in it, tries to determine the interpreted content within that
    /// expression that has been copied.  "interpreted" in this context means the actual value of the content that
    /// was selected, with things like escape characters embedded as the actual characters they represent.
    /// </summary>
    public static StringCopyPasteData? TryCreate(IVirtualCharLanguageService virtualCharService, ExpressionSyntax stringExpression, TextSpan selectionSpan)
        => stringExpression switch
        {
            LiteralExpressionSyntax literal => TryCreateForLiteral(virtualCharService, literal, selectionSpan),
            InterpolatedStringExpressionSyntax interpolatedString => TryCreateForInterpolatedString(virtualCharService, interpolatedString, selectionSpan),
            _ => throw ExceptionUtilities.UnexpectedValue(stringExpression.Kind()),
        };

    private static StringCopyPasteData? TryCreateForLiteral(IVirtualCharLanguageService virtualCharService, LiteralExpressionSyntax literal, TextSpan span)
        => TryGetContentForSpan(virtualCharService, literal.Token, span, out var content)
            ? new StringCopyPasteData([content])
            : null;

    /// <summary>
    /// Given a string <paramref name="token"/>, and the <paramref name="selectionSpan"/> the user has selected that
    /// overlaps with it, tries to determine the interpreted content within that token that has been copied.
    /// "interpreted" in this context means the actual value of the content that was selected, with things like
    /// escape characters embedded as the actual characters they represent.
    /// </summary>
    private static bool TryGetNormalizedStringForSpan(
        IVirtualCharLanguageService virtualCharService,
        SyntaxToken token,
        TextSpan selectionSpan,
        [NotNullWhen(true)] out string? normalizedText)
    {
        normalizedText = null;

        // First, try to convert this token to a sequence of virtual chars.
        var virtualChars = virtualCharService.TryConvertToVirtualChars(token);
        if (virtualChars.IsDefaultOrEmpty)
            return false;

        // Then find the start/end of the token's characters that overlap with the selection span.
        var firstOverlappingChar = virtualChars.FirstOrNull(vc => vc.Span.OverlapsWith(selectionSpan));
        var lastOverlappingChar = virtualChars.LastOrNull(vc => vc.Span.OverlapsWith(selectionSpan));

        if (firstOverlappingChar is null || lastOverlappingChar is null)
            return false;

        // Don't allow partial selection of an escaped character.  e.g. if they select 'n' in '\n'
        if (selectionSpan.Start > firstOverlappingChar.Value.Span.Start)
            return false;

        if (selectionSpan.End < lastOverlappingChar.Value.Span.End)
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
        TextSpan selectionSpan,
        out StringCopyPasteContent content)
    {
        if (!TryGetNormalizedStringForSpan(virtualCharService, token, selectionSpan, out var text))
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
        TextSpan selectionSpan)
    {
        using var _ = ArrayBuilder<StringCopyPasteContent>.GetInstance(out var result);

        foreach (var interpolatedContent in interpolatedString.Contents)
        {
            // Only consider portions of the interpolated string that overlap the selection.
            if (interpolatedContent.Span.OverlapsWith(selectionSpan))
            {
                if (interpolatedContent is InterpolationSyntax interpolation)
                {
                    // If the user copies a portion of an interpolation, just treat this as a non-smart copy paste
                    // for simplicity.
                    if (!selectionSpan.Contains(interpolation.Span))
                        return null;

                    // The format clause needs to be written differently depending on what sort of interpolated
                    // string we have (normal, verbatim, raw).  So grab the token for it and determine it's actual
                    // interpreted value so we can paste it properly at the destination side.
                    var formatClause = (string?)null;
                    if (interpolation.FormatClause != null &&
                        !TryGetNormalizedStringForSpan(virtualCharService, interpolation.FormatClause.FormatStringToken, selectionSpan, out formatClause))
                    {
                        return null;
                    }

                    // Can grab the expression and alignment-clause as is.  That's just normal C# code, and will
                    // remain the same no matter what we past into.
                    result.Add(StringCopyPasteContent.ForInterpolation(
                        interpolation.Expression.ToFullString(),
                        interpolation.AlignmentClause?.ToFullString(),
                        formatClause));
                }
                else if (interpolatedContent is InterpolatedStringTextSyntax stringText)
                {
                    if (!TryGetContentForSpan(virtualCharService, stringText.TextToken, selectionSpan, out var content))
                        return null;

                    result.Add(content);
                }
            }
        }

        return new StringCopyPasteData(result.ToImmutable());
    }
}
