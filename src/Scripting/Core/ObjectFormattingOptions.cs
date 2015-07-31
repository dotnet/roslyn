// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;

namespace Microsoft.CodeAnalysis.Scripting
{
    public sealed class ObjectFormattingOptions
    {
        public static readonly ObjectFormattingOptions Default = new ObjectFormattingOptions();

        public bool QuoteStrings { get; }
        public MemberDisplayFormat MemberFormat { get; }
        public int MaxLineLength { get; }
        public int MaxOutputLength { get; }
        public bool UseHexadecimalNumbers { get; }
        public string MemberIndentation { get; }
        public string Ellipsis { get; }
        public string NewLine { get; }
        public bool IncludeCodePoints { get; }

        public ObjectFormattingOptions(
            MemberDisplayFormat memberFormat = MemberDisplayFormat.NoMembers,
            bool quoteStrings = true,
            bool useHexadecimalNumbers = false,
            bool includeCodePoints = false,
            int maxLineLength = Int32.MaxValue,
            int maxOutputLength = Int32.MaxValue,
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
            this.MaxOutputLength = (maxOutputLength >= 0) ? maxOutputLength : int.MaxValue;
            this.MaxLineLength = (maxLineLength >= 0) ? maxLineLength : int.MaxValue;
            this.UseHexadecimalNumbers = useHexadecimalNumbers;
            this.MemberIndentation = memberIndentation ?? "  ";
            this.Ellipsis = ellipsis ?? "...";
            this.NewLine = lineBreak ?? Environment.NewLine;
        }

        public ObjectFormattingOptions Copy(
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
