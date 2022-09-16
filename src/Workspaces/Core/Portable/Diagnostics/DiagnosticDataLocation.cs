// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.IO;
using System.Runtime.Serialization;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Diagnostics
{
    [DataContract]
    internal sealed class DiagnosticDataLocation
    {
        /// <summary>
        /// Path to where the diagnostic was originally reported.  May be a path to a document in a project, or the
        /// project file itself. This should only be used by clients that truly need to know the original location a
        /// diagnostic was reported at, ignoring things like <c>#line</c> directives or other systems that would map the
        /// diagnostic to a different file or location.  Most clients should instead use <see cref="MappedFileSpan"/>,
        /// which contains the final location (file and span) that the diagnostic should be considered at.
        /// </summary>
        [DataMember(Order = 0)]
        public readonly FileLinePositionSpan UnmappedFileSpan;

        /// <summary>
        /// Document the diagnostic is associated with.  May be null if this is a project diagnostic.
        /// </summary>
        [DataMember(Order = 1)]
        public readonly DocumentId? DocumentId;

        /// <summary>
        /// Path and span where the diagnostic has been finally mapped to.  If no mapping happened, this will be equal
        /// to <see cref="UnmappedFileSpan"/>.  The <see cref="FileLinePositionSpan.Path"/> of this value will be the
        /// fully normalized file path where the diagnostic is located at.
        /// </summary>
        [DataMember(Order = 3)]
        public readonly FileLinePositionSpan MappedFileSpan;

        public DiagnosticDataLocation(
            FileLinePositionSpan unmappedFileSpan,
            DocumentId? documentId = null,
            FileLinePositionSpan? mappedFileSpan = null)
            : this(unmappedFileSpan, documentId, mappedFileSpan, forceMappedPath: false)
        {
        }

        private DiagnosticDataLocation(
            FileLinePositionSpan unmappedFileSpan,
            DocumentId? documentId,
            FileLinePositionSpan? mappedFileSpan,
            bool forceMappedPath)
        {
            Contract.ThrowIfNull(unmappedFileSpan.Path);

            UnmappedFileSpan = unmappedFileSpan;
            DocumentId = documentId;

            // If we were passed in a mapped span use it with the original span to determine the true final mapped
            // location. If forceMappedSpan is true, then this is a test which is explicitly making a mapped span that
            // it wants us to not mess with.  In that case, just hold onto that value directly.
            if (mappedFileSpan is { } mappedSpan &&
                (mappedSpan.HasMappedPath || forceMappedPath))
            {
                MappedFileSpan = new FileLinePositionSpan(GetNormalizedFilePath(unmappedFileSpan.Path, mappedSpan.Path), mappedSpan.Span);
            }
            else
            {
                MappedFileSpan = mappedFileSpan ?? unmappedFileSpan;
            }

            return;

            static string GetNormalizedFilePath(string original, string mapped)
            {
                if (RoslynString.IsNullOrEmpty(mapped))
                    return original;

                var combined = PathUtilities.CombinePaths(PathUtilities.GetDirectoryName(original), mapped);
                try
                {
                    return Path.GetFullPath(combined);
                }
                catch
                {
                    return combined;
                }
            }
        }

        internal DiagnosticDataLocation WithSpan(TextSpan newSourceSpan, SyntaxTree tree)
            => new(
                tree.GetLineSpan(newSourceSpan),
                DocumentId,
                tree.GetMappedLineSpan(newSourceSpan));

        public TextSpan GetUnmappedTextSpan(SourceText text)
        {
            var linePositionSpan = this.GetClampedLinePositionSpan(text);

            var span = text.Lines.GetTextSpan(linePositionSpan);
            return EnsureInBounds(TextSpan.FromBounds(Math.Max(span.Start, 0), Math.Max(span.End, 0)), text);
        }

        private static TextSpan EnsureInBounds(TextSpan textSpan, SourceText text)
            => TextSpan.FromBounds(
                Math.Min(textSpan.Start, text.Length),
                Math.Min(textSpan.End, text.Length));

        public LinePositionSpan GetClampedLinePositionSpan(SourceText text)
        {
            var lines = text.Lines;
            if (lines.Count == 0)
                return default;

            var fileSpan = this.UnmappedFileSpan;

            var startLine = fileSpan.StartLinePosition.Line;
            var endLine = fileSpan.EndLinePosition.Line;

            // Make sure the starting columns are never negative.
            var startColumn = Math.Max(fileSpan.StartLinePosition.Character, 0);
            var endColumn = Math.Max(fileSpan.EndLinePosition.Character, 0);

            if (startLine < 0)
            {
                // If the start line is negative (e.g. before the start of the actual document) then move the start to the 0,0 position.
                startLine = 0;
                startColumn = 0;
            }
            else if (startLine >= lines.Count)
            {
                // if the start line is after the end of the document, move the start to the last location in the document.
                startLine = lines.Count - 1;
                startColumn = lines[startLine].SpanIncludingLineBreak.Length;
            }

            if (endLine < 0)
            {
                // if the end is before the start of the document, then move the end to wherever the start position was determined to be.
                endLine = startLine;
                endColumn = startColumn;
            }
            else if (endLine >= lines.Count)
            {
                // if the end line is after the end of the document, move the end to the last location in the document.
                endLine = lines.Count - 1;
                endColumn = lines[endLine].SpanIncludingLineBreak.Length;
            }

            // now, ensure that the column of the start/end positions is within the length of its line.
            startColumn = Math.Min(startColumn, lines[startLine].SpanIncludingLineBreak.Length);
            endColumn = Math.Min(endColumn, lines[endLine].SpanIncludingLineBreak.Length);

            var start = new LinePosition(startLine, startColumn);
            var end = new LinePosition(endLine, endColumn);

            // swap if necessary
            if (end < start)
                (start, end) = (end, start);

            return new LinePositionSpan(start, end);
        }

        public static class TestAccessor
        {
            public static DiagnosticDataLocation Create(
                FileLinePositionSpan originalFileSpan,
                DocumentId? documentId,
                FileLinePositionSpan mappedFileSpan,
                bool forceMappedPath)
            {
                return new DiagnosticDataLocation(originalFileSpan, documentId, mappedFileSpan, forceMappedPath);
            }
        }
    }
}
