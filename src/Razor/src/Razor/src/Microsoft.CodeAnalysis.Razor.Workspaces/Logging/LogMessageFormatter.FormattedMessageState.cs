// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.AspNetCore.Razor;
using static System.StringExtensions;

namespace Microsoft.CodeAnalysis.Razor.Logging;

internal static partial class LogMessageFormatter
{
    private readonly struct FormattedMessageState
    {
        // The leading whitespace matches the time space length            "[hh:mm:ss.fffffff] "
        private static readonly ReadOnlyMemory<char> s_leadingWhiteSpace = "                   ".AsMemory();
        private static readonly ReadOnlyMemory<char> s_newLine = Environment.NewLine.AsMemory();

        private readonly ReadOnlyMemory<char> _message;
        private readonly ReadOnlyMemory<Range> _messageLineRanges;
        private readonly ReadOnlyMemory<char> _exceptionText;
        private readonly ReadOnlyMemory<Range> _exceptionLineRanges;
        private readonly ReadOnlyMemory<char> _categoryNamePart;
        private readonly ReadOnlyMemory<char> _timeStampPart;
        private readonly ReadOnlyMemory<char> _newLine;
        private readonly ReadOnlyMemory<char> _leadingWhiteSpace;

        public ReadOnlySpan<char> MessageText => _message.Span;
        public ReadOnlySpan<Range> MessageLineRanges => _messageLineRanges.Span;
        public ReadOnlySpan<char> ExceptionText => _exceptionText.Span;
        public ReadOnlySpan<Range> ExceptionLineRanges => _exceptionLineRanges.Span;
        public ReadOnlySpan<char> CategoryNamePart => _categoryNamePart.Span;
        public ReadOnlySpan<char> TimeStampPart => _timeStampPart.Span;
        public ReadOnlySpan<char> NewLine => _newLine.Span;
        public ReadOnlySpan<char> LeadingWhiteSpace => _leadingWhiteSpace.Span;

        public int Length { get; }

        private FormattedMessageState(
            ReadOnlyMemory<char> messageText, ReadOnlyMemory<Range> messageLineRanges,
            ReadOnlyMemory<char> exceptionText, ReadOnlyMemory<Range> exceptionLineRanges,
            ReadOnlyMemory<char> categoryNamePart,
            ReadOnlyMemory<char> timeStampPart,
            ReadOnlyMemory<char> newLine,
            ReadOnlyMemory<char> leadingWhiteSpace)
        {
            _message = messageText;
            _messageLineRanges = messageLineRanges;
            _exceptionText = exceptionText;
            _exceptionLineRanges = exceptionLineRanges;
            _categoryNamePart = categoryNamePart;
            _timeStampPart = timeStampPart;
            _newLine = newLine;
            _leadingWhiteSpace = leadingWhiteSpace;

            Length = ComputeLength();
        }

        private int ComputeLength()
        {
            // Calculate the length of the final formatted string.
            var isFirst = true;
            var length = 0;

            length += CategoryNamePart.Length;

            foreach (var range in MessageLineRanges)
            {
                if (isFirst)
                {
                    length += TimeStampPart.Length;
                    isFirst = false;
                }
                else
                {
                    length += NewLine.Length;
                    length += LeadingWhiteSpace.Length;
                }

                var (_, lineLength) = range.GetOffsetAndLength(MessageText.Length);
                length += lineLength;
            }

            foreach (var range in ExceptionLineRanges)
            {
                length += TimeStampPart.Length;

                var (_, lineLength) = range.GetOffsetAndLength(ExceptionText.Length);
                length += lineLength;
            }

            return length;
        }

        public static FormattedMessageState Create(
            string message,
            string categoryName,
            Exception? exception,
            bool includeTimeStamp,
            ref MemoryBuilder<Range> messageLineRangeBuilder,
            ref MemoryBuilder<Range> exceptionLineRangeBuilder)
        {
            var messageText = message.AsMemory();
            var newLine = s_newLine;

            var categoryNamePart = ('[' + categoryName + "] ").AsMemory();

            ReadOnlyMemory<char> timeStampPart, leadingWhiteSpace;

            if (includeTimeStamp)
            {
                timeStampPart = ('[' + DateTime.Now.TimeOfDay.ToString("hh\\:mm\\:ss\\.fffffff") + "] ").AsMemory();
                leadingWhiteSpace = s_leadingWhiteSpace;
            }
            else
            {
                timeStampPart = default;
                leadingWhiteSpace = default;
            }

            // Collect the range of each line in the message text.
            CollectLineRanges(messageText.Span, newLine.Span, ref messageLineRangeBuilder);

            var exceptionText = exception is not null
                ? exception.ToString().AsMemory()
                : default;

            // If specified, Collect the range of each line in the exception text.
            if (exceptionText.Length > 0)
            {
                CollectLineRanges(exceptionText.Span, newLine.Span, ref exceptionLineRangeBuilder);
            }

            return new(
                messageText, messageLineRangeBuilder.AsMemory(),
                exceptionText, exceptionLineRangeBuilder.AsMemory(),
                categoryNamePart, timeStampPart, newLine, leadingWhiteSpace);
        }

        private static void CollectLineRanges(ReadOnlySpan<char> source, ReadOnlySpan<char> newLine, ref MemoryBuilder<Range> builder)
        {
            var startIndex = 0;

            while (startIndex < source.Length)
            {
                // Find the index of the next new line.
                var endIndex = source[startIndex..].IndexOf(newLine);

                // If endIndex == -1, there isn't another new line.
                // So, add the remaining range and break.
                if (endIndex == -1)
                {
                    builder.Append(startIndex..);
                    break;
                }

                var realEndIndex = startIndex + endIndex;

                builder.Append(startIndex..realEndIndex);
                startIndex = realEndIndex + newLine.Length;
            }
        }
    }
}
