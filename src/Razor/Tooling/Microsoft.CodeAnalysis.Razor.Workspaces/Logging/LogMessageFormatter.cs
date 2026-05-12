// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using Microsoft.AspNetCore.Razor;

namespace Microsoft.CodeAnalysis.Razor.Logging;

internal static partial class LogMessageFormatter
{
    public static string FormatMessage(string message, string categoryName, Exception? exception, bool includeTimeStamp = true)
    {
        // Note
        MemoryBuilder<Range> messageLineRangeBuilder = new(initialCapacity: 4);
        MemoryBuilder<Range> exceptionLineRangeBuilder = exception is not null ? new(initialCapacity: 64) : default;
        try
        {
            var state = FormattedMessageState.Create(
                message, categoryName, exception, includeTimeStamp,
                ref messageLineRangeBuilder, ref exceptionLineRangeBuilder);

            // Create the final string.
            return string.Create(state.Length, state, static (span, state) =>
            {
                Write(state.CategoryNamePart, ref span);

                var isFirst = true;

                foreach (var range in state.MessageLineRanges)
                {
                    if (isFirst)
                    {
                        // Write the time stamp if this is the first line.
                        Write(state.TimeStampPart, ref span);
                        isFirst = false;
                    }
                    else
                    {
                        // Otherwise, write a new line and the leading whitespace.
                        Write(state.NewLine, ref span);
                        Write(state.LeadingWhiteSpace, ref span);
                    }

                    Write(state.MessageText[range], ref span);
                }

                foreach (var range in state.ExceptionLineRanges)
                {
                    Write(state.LeadingWhiteSpace, ref span);
                    Write(state.ExceptionText[range], ref span);
                }

                Debug.Assert(span.Length == 0, "We didn't fill the whole span!");

                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                static void Write(ReadOnlySpan<char> source, ref Span<char> destination)
                {
                    if (source.IsEmpty)
                    {
                        return;
                    }

                    source.CopyTo(destination);
                    destination = destination[source.Length..];

                    Debug.Assert(destination.Length >= 0);
                }
            });
        }
        finally
        {
            messageLineRangeBuilder.Dispose();
            exceptionLineRangeBuilder.Dispose();
        }
    }
}
