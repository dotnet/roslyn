// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Debugger.Contracts.EditAndContinue;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.EditAndContinue
{
    internal static class Extensions
    {
        internal static LinePositionSpan AddLineDelta(this LinePositionSpan span, int lineDelta)
            => new(new LinePosition(span.Start.Line + lineDelta, span.Start.Character), new LinePosition(span.End.Line + lineDelta, span.End.Character));

        internal static SourceFileSpan AddLineDelta(this SourceFileSpan span, int lineDelta)
            => new(span.Path, span.Span.AddLineDelta(lineDelta));

        internal static int GetLineDelta(this LinePositionSpan oldSpan, LinePositionSpan newSpan)
            => newSpan.Start.Line - oldSpan.Start.Line;

        internal static bool Contains(this LinePositionSpan container, LinePositionSpan span)
            => span.Start >= container.Start && span.End <= container.End;

        public static LinePositionSpan ToLinePositionSpan(this SourceSpan span)
            => new(new(span.StartLine, span.StartColumn), new(span.EndLine, span.EndColumn));

        public static SourceSpan ToSourceSpan(this LinePositionSpan span)
            => new(span.Start.Line, span.Start.Character, span.End.Line, span.End.Character);

        public static ActiveStatement GetStatement(this ImmutableArray<ActiveStatement> statements, int ordinal)
        {
            foreach (var item in statements)
            {
                if (item.Ordinal == ordinal)
                {
                    return item;
                }
            }

            throw ExceptionUtilities.UnexpectedValue(ordinal);
        }

        public static ActiveStatementSpan GetStatement(this ImmutableArray<ActiveStatementSpan> statements, int ordinal)
        {
            foreach (var item in statements)
            {
                if (item.Ordinal == ordinal)
                {
                    return item;
                }
            }

            throw ExceptionUtilities.UnexpectedValue(ordinal);
        }


        public static UnmappedActiveStatement GetStatement(this ImmutableArray<UnmappedActiveStatement> statements, int ordinal)
        {
            foreach (var item in statements)
            {
                if (item.Statement.Ordinal == ordinal)
                {
                    return item;
                }
            }

            throw ExceptionUtilities.UnexpectedValue(ordinal);
        }
    }
}
