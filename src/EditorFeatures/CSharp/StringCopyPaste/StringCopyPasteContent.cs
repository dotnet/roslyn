// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Microsoft.CodeAnalysis.Editor.CSharp.StringCopyPaste;

/// <summary>
/// Content from a string literal or interpolated string that has been copied.
/// </summary>
[method: JsonConstructor]
internal readonly struct StringCopyPasteContent(
    StringCopyPasteContentKind kind,
    string? textValue,
    string? interpolationExpression,
    string? interpolationAlignmentClause,
    string? interpolationFormatClause)
{
    public StringCopyPasteContentKind Kind { get; } = kind;

    /// <summary>
    /// The actual string value for <see cref="StringCopyPasteContentKind.Text"/>.  <see langword="null"/> for <see
    /// cref="StringCopyPasteContentKind.Interpolation"/>.
    /// </summary>
    public string? TextValue { get; } = textValue;

    /// <summary>
    /// The actual string value for <see cref="InterpolationSyntax.Expression"/> for <see
    /// cref="StringCopyPasteContentKind.Interpolation"/>.  <see langword="null"/> for <see
    /// cref="StringCopyPasteContentKind.Text"/>.
    /// </summary>
    public string? InterpolationExpression { get; } = interpolationExpression;

    /// <summary>
    /// The actual string value for <see cref="InterpolationSyntax.AlignmentClause"/> for <see
    /// cref="StringCopyPasteContentKind.Interpolation"/>.  <see langword="null"/> for <see
    /// cref="StringCopyPasteContentKind.Text"/>.
    /// </summary>
    public string? InterpolationAlignmentClause { get; } = interpolationAlignmentClause;

    /// <summary>
    /// The actual string value for <see cref="InterpolationSyntax.FormatClause"/> for <see
    /// cref="StringCopyPasteContentKind.Interpolation"/>.  <see langword="null"/> for <see
    /// cref="StringCopyPasteContentKind.Text"/>.
    /// </summary>
    public string? InterpolationFormatClause { get; } = interpolationFormatClause;

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
