// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Runtime.Serialization;
using Microsoft.CodeAnalysis.Shared.Extensions;
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
        public readonly FileLinePositionSpan OriginalFileSpan1;

        /// <summary>
        /// Document the diagnostic is associated with.  May be null if this is a project diagnostic.
        /// </summary>
        [DataMember(Order = 1)]
        public readonly DocumentId? DocumentId;

        // text can be either given or calculated from original line/column
        [DataMember(Order = 2)]
        public readonly TextSpan? SourceSpan;

        /// <summary>
        /// Null if path is not mapped and <see cref="OriginalFileSpan"/> contains the actual path. Note that the value
        /// might be a relative path. In that case <see cref="OriginalFileSpan"/> should be used as a base path for path
        /// resolution.
        /// </summary>
        [DataMember(Order = 3)]
        public readonly FileLinePositionSpan MappedFileSpan;

        public DiagnosticDataLocation(
            FileLinePositionSpan originalFileSpan,
            DocumentId? documentId = null,
            TextSpan? sourceSpan = null,
            FileLinePositionSpan? mappedFileSpan = null)
            : this(originalFileSpan, documentId, sourceSpan, mappedFileSpan, checkMappedFileSpan: true)
        {
        }

        private DiagnosticDataLocation(
            FileLinePositionSpan originalFileSpan,
            DocumentId? documentId,
            TextSpan? sourceSpan,
            FileLinePositionSpan? mappedFileSpan,
            bool checkMappedFileSpan)
        {
            Contract.ThrowIfNull(originalFileSpan.Path);

            OriginalFileSpan1 = originalFileSpan;
            DocumentId = documentId;
            SourceSpan = sourceSpan;

            // If we were passed in a mapped span use it with the original span to determine the true final mapped
            // location. If checkMappedFileSpan is false, then this is a test which is explicitly making a mapped span
            // that it wants us to not mess with.  In that case, just hold onto that value directly.
            if (checkMappedFileSpan && mappedFileSpan is { HasMappedPath: false } mappedSpan)
            {
                MappedFileSpan = new FileLinePositionSpan(GetNormalizedFilePath(originalFileSpan.Path, mappedSpan.Path), mappedSpan.Span);
            }
            else
            {
                MappedFileSpan = mappedFileSpan ?? originalFileSpan;
            }
        }

        //[MemberNotNullWhen(true, nameof(MappedFileSpan))]
        //public bool IsMapped => MappedFileSpan != null;

        internal DiagnosticDataLocation WithSpan(TextSpan newSourceSpan, SyntaxTree tree)
        {
            var mappedLineInfo = tree.GetMappedLineSpan(newSourceSpan);
            var originalLineInfo = tree.GetLineSpan(newSourceSpan);

            return new DiagnosticDataLocation(
                originalLineInfo,
                DocumentId,
                newSourceSpan,
                mappedLineInfo);
        }

        ///// <summary>
        ///// Returns the <see cref="FileLinePositionSpan"/> that this diagnostic is located at.  If this is a mapped
        ///// location, the <see cref="FileLinePositionSpan.Path"/> will be normalized to the final full path indicated by
        ///// the mapped span.
        ///// </summary>
        //internal FileLinePositionSpan GetNormalizedFilePathLinePositionSpan()
        //    => IsMapped ? new FileLinePositionSpan(GetNormalizedFilePath(), MappedFileSpan.Value.Span) : OriginalFileSpan;

        ///// <summary>
        ///// Returns the path that this diagnostic is located at.  If this is a mapped location, path will be normalized
        ///// to the final full path indicated by the mapped span.
        ///// </summary>
        //private string GetNormalizedFilePath()
        //    => MappedFileSpan == null ? OriginalFileSpan.Path : GetNormalizedFilePath(OriginalFileSpan.Path, MappedFileSpan.Value.Path);

        private static string GetNormalizedFilePath(string original, string mapped)
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

        public static class TestAccessor
        {
            public static DiagnosticDataLocation Create(
                FileLinePositionSpan originalFileSpan,
                DocumentId? documentId,
                TextSpan? sourceSpan,
                FileLinePositionSpan? mappedFileSpan,
                bool checkMappedFileSpan)
            {
                return new DiagnosticDataLocation(originalFileSpan, documentId, sourceSpan, mappedFileSpan, checkMappedFileSpan);
            }
        }
    }
}
