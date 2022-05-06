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
