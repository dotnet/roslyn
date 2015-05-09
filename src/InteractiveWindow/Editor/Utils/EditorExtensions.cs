// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.VisualStudio.Text;

namespace Microsoft.VisualStudio.InteractiveWindow
{
    internal static class EditorExtensions
    {
        internal static bool EndsWithLineBreak(this ITextSnapshot snapshot)
        {
            int length = snapshot.Length;
            return length > 0 && (snapshot[length - 1] == '\n' || snapshot[length - 1] == '\r');
        }

        internal static bool EndsWith(this ITextSnapshot snapshot, char c)
        {
            int length = snapshot.Length;
            return length > 0 && snapshot[length - 1] == c;
        }

        internal static bool EndsWithLineBreak(this string str)
        {
            int length = str.Length;
            return length > 0 && (str[length - 1] == '\n' || str[length - 1] == '\r');
        }

        internal static bool StartsWith(this SnapshotSpan span, string prefix)
        {
            if (span.Length < prefix.Length)
            {
                return false;
            }

            var snapshot = span.Snapshot;
            int start = span.Start.Position;
            for (int i = 0; i < prefix.Length; i++)
            {
                if (snapshot[start + i] != prefix[i])
                {
                    return false;
                }
            }

            return true;
        }

        internal static SnapshotSpan Trim(this SnapshotSpan snapshotSpan)
        {
            return snapshotSpan.TrimStart().TrimEnd();
        }

        internal static SnapshotSpan TrimStart(this SnapshotSpan snapshotSpan)
        {
            var snapshot = snapshotSpan.Snapshot;

            int i = snapshotSpan.Start.Position;
            int end = snapshotSpan.End.Position;
            while (i < end && char.IsWhiteSpace(snapshot[i]))
            {
                i++;
            }

            return new SnapshotSpan(snapshotSpan.Snapshot, Span.FromBounds(i, end));
        }

        internal static SnapshotSpan TrimEnd(this SnapshotSpan snapshotSpan)
        {
            var snapshot = snapshotSpan.Snapshot;

            int i = snapshotSpan.End - 1;
            int start = snapshotSpan.Start;
            while (i >= start && char.IsWhiteSpace(snapshot[i]))
            {
                i--;
            }

            return new SnapshotSpan(snapshotSpan.Snapshot, Span.FromBounds(start, i + 1));
        }

        internal static int IndexOfAnyWhiteSpace(this string str)
        {
            for (int i = 0; i < str.Length; i++)
            {
                if (char.IsWhiteSpace(str[i]))
                {
                    return i;
                }
            }

            return -1;
        }

        internal static SnapshotPoint? IndexOfAnyWhiteSpace(this SnapshotSpan snapshotSpan)
        {
            var snapshot = snapshotSpan.Snapshot;
            int i = snapshotSpan.Start;
            int end = snapshotSpan.End;
            while (i < end)
            {
                if (char.IsWhiteSpace(snapshot[i]))
                {
                    return new SnapshotPoint(snapshotSpan.Snapshot, i);
                }

                i++;
            }

            return null;
        }

        internal static SnapshotSpan GetExtent(this ITextSnapshot snapshot)
        {
            return new SnapshotSpan(snapshot, 0, snapshot.Length);
        }

        internal static SnapshotSpan SubSpan(this SnapshotSpan span, int start)
        {
            return new SnapshotSpan(span.Snapshot, Span.FromBounds(span.Start.Position + start, span.End.Position));
        }

        internal static SnapshotSpan SubSpan(this SnapshotSpan span, int start, int length)
        {
            return new SnapshotSpan(span.Snapshot, span.Start.Position + start, length);
        }

        internal static void GetLineAndColumn(this VirtualSnapshotPoint point, out ITextSnapshotLine line, out int column)
        {
            line = point.Position.GetContainingLine();
            column = point.Position.Position - line.Start.Position + point.VirtualSpaces;
        }

        internal static void GetLineAndColumn(this SnapshotPoint point, out ITextSnapshotLine line, out int column)
        {
            line = point.GetContainingLine();
            column = point.Position - line.Start.Position;
        }
    }
}
