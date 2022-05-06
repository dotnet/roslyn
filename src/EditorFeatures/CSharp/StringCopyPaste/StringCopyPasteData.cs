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

namespace Microsoft.CodeAnalysis.Editor.CSharp.StringCopyPaste
{
    [DataContract]
    internal class StringCopyPasteData
    {
        private static readonly DataContractJsonSerializer s_serializer = new(typeof(StringCopyPasteData), new[] { typeof(StringCopyPasteContent) });

        [DataMember(Order = 0)]
        public readonly ImmutableArray<StringCopyPasteContent> Contents;

        public StringCopyPasteData(ImmutableArray<StringCopyPasteContent> contents)
        {
            Contents = contents;
        }

        public string ToJson()
        {
            using var stream = new MemoryStream();
            s_serializer.WriteObject(stream, this);

            stream.Position = 0;
            using var reader = new StreamReader(stream);
            return reader.ReadToEnd();
        }

        public static StringCopyPasteData? FromJson(string? json)
        {
            if (json == null)
                return null;

            using var stringStream = new MemoryStream(Encoding.UTF8.GetBytes(json));
            try
            {
                return (StringCopyPasteData)s_serializer.ReadObject(stringStream);
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

    [DataContract]
    internal readonly struct StringCopyPasteContent
    {
        [DataMember(Order = 0)]
        public readonly StringCopyPasteContentKind Kind;

        /// <summary>
        /// The actual string value for <see cref="StringCopyPasteContentKind.Text"/>.  <see langword="null"/> for <see
        /// cref="StringCopyPasteContentKind.Interpolation"/>.
        /// </summary>
        [DataMember(Order = 1)]
        public readonly string? TextValue;

        /// <summary>
        /// The actual string value for <see cref="InterpolationSyntax.Expression"/> for <see
        /// cref="StringCopyPasteContentKind.Interpolation"/>.  <see langword="null"/> for <see
        /// cref="StringCopyPasteContentKind.Text"/>.
        /// </summary>
        [DataMember(Order = 2)]
        public readonly string? InterpolationExpression;

        /// <summary>
        /// The actual string value for <see cref="InterpolationSyntax.AlignmentClause"/> for <see
        /// cref="StringCopyPasteContentKind.Interpolation"/>.  <see langword="null"/> for <see
        /// cref="StringCopyPasteContentKind.Text"/>.
        /// </summary>
        [DataMember(Order = 3)]
        public readonly string? InterpolationAlignmentClause;

        /// <summary>
        /// The actual string value for <see cref="InterpolationSyntax.FormatClause"/> for <see
        /// cref="StringCopyPasteContentKind.Interpolation"/>.  <see langword="null"/> for <see
        /// cref="StringCopyPasteContentKind.Text"/>.
        /// </summary>
        [DataMember(Order = 4)]
        public readonly string? InterpolationFormatClause;

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

        [MemberNotNullWhen(true, nameof(TextValue))]
        public bool IsText => Kind == StringCopyPasteContentKind.Text;

        [MemberNotNullWhen(true, nameof(InterpolationExpression))]
        public bool IsInterpolation => Kind == StringCopyPasteContentKind.Interpolation;

        public static StringCopyPasteContent ForText(string text)
            => new(StringCopyPasteContentKind.Text, text, null, null, null);

        public static StringCopyPasteContent ForInterpolation(string expression, string? alignmentClause, string? formatClause)
            => new(StringCopyPasteContentKind.Interpolation, null, expression, alignmentClause, formatClause);
    }
}
