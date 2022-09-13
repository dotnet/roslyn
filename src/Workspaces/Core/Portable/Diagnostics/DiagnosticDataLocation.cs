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
        [DataMember(Order = 0)]
        public readonly DocumentId? DocumentId;

        // text can be either given or calculated from original line/column
        [DataMember(Order = 1)]
        public readonly TextSpan? SourceSpan;

        [DataMember(Order = 2)]
        public readonly FileLinePositionSpan? OriginalFileSpan;

        /// <summary>
        /// Null if path is not mapped and <see cref="OriginalFileSpan"/> contains the actual path. Note that the value
        /// might be a relative path. In that case <see cref="OriginalFileSpan"/> should be used as a base path for path
        /// resolution.
        /// </summary>
        [DataMember(Order = 3)]
        public readonly FileLinePositionSpan? MappedFileSpan;

        public DiagnosticDataLocation(
            DocumentId? documentId = null,
            TextSpan? sourceSpan = null,
            FileLinePositionSpan? originalFileSpan = null,
            FileLinePositionSpan? mappedFileSpan = null)
        {
            // If the original source location path is not available then mapped must be as well.
            Contract.ThrowIfTrue(originalFileSpan == null && mappedFileSpan != null);

            DocumentId = documentId;
            SourceSpan = sourceSpan;
            OriginalFileSpan = originalFileSpan;
            MappedFileSpan = mappedFileSpan;
        }

        [MemberNotNullWhen(true, nameof(MappedFileSpan))]
        public bool IsMapped => MappedFileSpan != null;

        internal DiagnosticDataLocation WithSpan(TextSpan newSourceSpan, SyntaxTree tree)
        {
            var mappedLineInfo = tree.GetMappedLineSpan(newSourceSpan);
            var originalLineInfo = tree.GetLineSpan(newSourceSpan);

            return new DiagnosticDataLocation(
                DocumentId,
                newSourceSpan,
                originalFileSpan: originalLineInfo,
                mappedFileSpan: mappedLineInfo.GetMappedFilePathIfExist() == null ? null : mappedLineInfo);
        }

        internal FileLinePositionSpan GetFileLinePositionSpan()
        {
            var filePath = GetFilePath();
            if (filePath == null)
            {
                return default;
            }

            return IsMapped
                ? new(filePath, MappedFileSpan.Value.StartLinePosition, MappedFileSpan.Value.EndLinePosition)
                : new(filePath, OriginalFileSpan!.Value.StartLinePosition, OriginalFileSpan!.Value.EndLinePosition);
        }

        internal string? GetFilePath()
            => GetFilePath(OriginalFileSpan?.Path, MappedFileSpan?.Path);

        private static string? GetFilePath(string? original, string? mapped)
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
