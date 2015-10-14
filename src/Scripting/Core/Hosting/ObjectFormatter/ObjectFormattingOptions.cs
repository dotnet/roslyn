// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;

namespace Microsoft.CodeAnalysis.Scripting.Hosting
{
    internal sealed class ObjectFormattingOptions
    {
        public static ObjectFormattingOptions Default { get; } = new ObjectFormattingOptions();

        internal bool QuoteStrings { get; }
        internal MemberDisplayFormat MemberFormat { get; }
        internal int MaxLineLength { get; }
        internal int MaxOutputLength { get; }
        internal bool UseHexadecimalNumbers { get; }
        internal string MemberIndentation { get; }
        internal string Ellipsis { get; }
        internal string NewLine { get; }
        internal bool IncludeCodePoints { get; }

        internal ObjectFormattingOptions(
            MemberDisplayFormat memberFormat = MemberDisplayFormat.Inline,
            bool quoteStrings = true,
            bool useHexadecimalNumbers = false,
            bool includeCodePoints = false,
            int maxLineLength = int.MaxValue,
            int maxOutputLength = 1024,
            string memberIndentation = null,
            string ellipsis = null,
            string lineBreak = null)
        {
            if (!memberFormat.IsValid())
            {
                throw new ArgumentOutOfRangeException(nameof(memberFormat));
            }

            this.MemberFormat = memberFormat;
            this.QuoteStrings = quoteStrings;
            this.IncludeCodePoints = includeCodePoints;
            this.MaxOutputLength = maxOutputLength;
            this.MaxLineLength = maxLineLength;
            this.UseHexadecimalNumbers = useHexadecimalNumbers;
            this.MemberIndentation = memberIndentation ?? "  ";
            this.Ellipsis = ellipsis ?? "...";
            this.NewLine = lineBreak ?? Environment.NewLine;
        }

        internal ObjectFormattingOptions Copy(
            MemberDisplayFormat? memberFormat = null,
            bool? quoteStrings = null,
            bool? useHexadecimalNumbers = null,
            bool? includeCodePoints = null,
            int? maxLineLength = null,
            int? maxOutputLength = null,
            string memberIndentation = null,
            string ellipsis = null,
            string newLine = null)
        {
            return new ObjectFormattingOptions(
                memberFormat ?? this.MemberFormat,
                quoteStrings ?? this.QuoteStrings,
                useHexadecimalNumbers ?? this.UseHexadecimalNumbers,
                includeCodePoints ?? this.IncludeCodePoints,
                maxLineLength ?? this.MaxLineLength,
                maxOutputLength ?? this.MaxOutputLength,
                memberIndentation ?? this.MemberIndentation,
                ellipsis ?? this.Ellipsis,
                newLine ?? this.NewLine);
        }
    }
}
