// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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

        // text can be either given or calculated from original line/column
        [DataMember(Order = 2)]
        public readonly TextSpan? SourceSpan;

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
            TextSpan? sourceSpan = null,
            FileLinePositionSpan? mappedFileSpan = null)
            : this(unmappedFileSpan, documentId, sourceSpan, mappedFileSpan, forceMappedPath: false)
        {
        }

        private DiagnosticDataLocation(
            FileLinePositionSpan unmappedFileSpan,
            DocumentId? documentId,
            TextSpan? sourceSpan,
            FileLinePositionSpan? mappedFileSpan,
            bool forceMappedPath)
        {
            Contract.ThrowIfNull(unmappedFileSpan.Path);

            UnmappedFileSpan = unmappedFileSpan;
            DocumentId = documentId;
            SourceSpan = sourceSpan;

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
            => new DiagnosticDataLocation(
                tree.GetLineSpan(newSourceSpan),
                DocumentId,
                newSourceSpan,
                tree.GetMappedLineSpan(newSourceSpan));

        public static class TestAccessor
        {
            public static DiagnosticDataLocation Create(
                FileLinePositionSpan originalFileSpan,
                DocumentId? documentId,
                TextSpan? sourceSpan,
                FileLinePositionSpan mappedFileSpan,
                bool forceMappedPath)
            {
                return new DiagnosticDataLocation(originalFileSpan, documentId, sourceSpan, mappedFileSpan, forceMappedPath);
            }
        }
    }
}
