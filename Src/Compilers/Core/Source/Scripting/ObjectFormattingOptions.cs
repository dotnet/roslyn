using System;

namespace Roslyn.Scripting
{
    [Serializable]
    public sealed class ObjectFormattingOptions
    {
        public static readonly ObjectFormattingOptions Default = new ObjectFormattingOptions();

        public bool QuoteStrings { get; private set; }
        public MemberDisplayFormat MemberFormat { get; private set; }
        public int MaxLineLength { get; private set; }
        public int MaxOutputLength { get; private set; }
        public bool UseHexadecimalNumbers { get; private set; }
        public string MemberIndentation { get; private set; }
        public string Ellipsis { get; private set; }
        public string NewLine { get; private set; }
        public bool IncludeCodePoints { get; private set; }

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
                throw new ArgumentOutOfRangeException("memberFormat");
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