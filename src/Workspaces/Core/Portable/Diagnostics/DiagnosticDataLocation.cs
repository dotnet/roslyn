﻿// Licensed to the .NET Foundation under one or more agreements.
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
        /// project file itself.
        /// </summary>
        [DataMember(Order = 0)]
        public readonly string OriginalFilePath;

        /// <summary>
        /// Document the diagnostic is associated with.  May be null if this is a project diagnostic.
        /// </summary>
        [DataMember(Order = 1)]
        public readonly DocumentId? DocumentId;

        // text can be either given or calculated from original line/column
        [DataMember(Order = 2)]
        public readonly TextSpan? SourceSpan;

        [DataMember(Order = 3)]
        public readonly int OriginalStartLine;

        [DataMember(Order = 4)]
        public readonly int OriginalStartColumn;

        [DataMember(Order = 5)]
        public readonly int OriginalEndLine;

        [DataMember(Order = 6)]
        public readonly int OriginalEndColumn;

        /// <summary>
        /// Null if path is not mapped and <see cref="OriginalFilePath"/> contains the actual path.
        /// Note that the value might be a relative path. In that case <see cref="OriginalFilePath"/> should be used
        /// as a base path for path resolution.
        /// </summary>
        [DataMember(Order = 7)]
        public readonly string? MappedFilePath;

        [DataMember(Order = 8)]
        public readonly int MappedStartLine;

        [DataMember(Order = 9)]
        public readonly int MappedStartColumn;

        [DataMember(Order = 10)]
        public readonly int MappedEndLine;

        [DataMember(Order = 11)]
        public readonly int MappedEndColumn;

        public DiagnosticDataLocation(
            string originalFilePath,
            DocumentId? documentId = null,
            TextSpan? sourceSpan = null,
            int originalStartLine = 0,
            int originalStartColumn = 0,
            int originalEndLine = 0,
            int originalEndColumn = 0,
            string? mappedFilePath = null,
            int mappedStartLine = 0,
            int mappedStartColumn = 0,
            int mappedEndLine = 0,
            int mappedEndColumn = 0)
        {
            Contract.ThrowIfNull(originalFilePath);

            OriginalFilePath = originalFilePath;
            DocumentId = documentId;
            SourceSpan = sourceSpan;
            MappedFilePath = mappedFilePath;
            MappedStartLine = mappedStartLine;
            MappedStartColumn = mappedStartColumn;
            MappedEndLine = mappedEndLine;
            MappedEndColumn = mappedEndColumn;
            OriginalStartLine = originalStartLine;
            OriginalStartColumn = originalStartColumn;
            OriginalEndLine = originalEndLine;
            OriginalEndColumn = originalEndColumn;
        }

        [MemberNotNullWhen(true, nameof(MappedFilePath))]
        public bool IsMapped => MappedFilePath != null;

        internal DiagnosticDataLocation WithSpan(TextSpan newSourceSpan, SyntaxTree tree)
        {
            var mappedLineInfo = tree.GetMappedLineSpan(newSourceSpan);
            var originalLineInfo = tree.GetLineSpan(newSourceSpan);

            return new DiagnosticDataLocation(
                originalLineInfo.Path,
                DocumentId,
                newSourceSpan,
                originalStartLine: originalLineInfo.StartLinePosition.Line,
                originalStartColumn: originalLineInfo.StartLinePosition.Character,
                originalEndLine: originalLineInfo.EndLinePosition.Line,
                originalEndColumn: originalLineInfo.EndLinePosition.Character,
                mappedFilePath: mappedLineInfo.GetMappedFilePathIfExist(),
                mappedStartLine: mappedLineInfo.StartLinePosition.Line,
                mappedStartColumn: mappedLineInfo.StartLinePosition.Character,
                mappedEndLine: mappedLineInfo.EndLinePosition.Line,
                mappedEndColumn: mappedLineInfo.EndLinePosition.Character);
        }

        internal FileLinePositionSpan GetFileLinePositionSpan()
        {
            var filePath = GetFilePath();

            return IsMapped
                ? new(filePath, new(MappedStartLine, MappedStartColumn), new(MappedEndLine, MappedEndColumn))
                : new(filePath, new(OriginalStartLine, OriginalStartColumn), new(OriginalEndLine, OriginalEndColumn));
        }

        internal string GetFilePath()
            => IsMapped ? GetFilePath(OriginalFilePath, MappedFilePath) : OriginalFilePath;

        private static string GetFilePath(string original, string mapped)
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
}
