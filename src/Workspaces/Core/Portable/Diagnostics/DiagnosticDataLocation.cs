// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Diagnostics
{
    internal sealed class DiagnosticDataLocation
    {
        public readonly DocumentId DocumentId;

        // text can be either given or calculated from original line/column
        public readonly TextSpan? SourceSpan;

        /// <summary>
        /// Null if path is not mapped and <see cref="OriginalFilePath"/> contains the actual path.
        /// Note that the value might be a relative path. In that case <see cref="OriginalFilePath"/> should be used
        /// as a base path for path resolution.
        /// </summary>
        public readonly string MappedFilePath;
        public readonly int MappedStartLine;
        public readonly int MappedStartColumn;
        public readonly int MappedEndLine;
        public readonly int MappedEndColumn;
        public readonly string OriginalFilePath;
        public readonly int OriginalStartLine;
        public readonly int OriginalStartColumn;
        public readonly int OriginalEndLine;
        public readonly int OriginalEndColumn;

        public DiagnosticDataLocation(
            DocumentId documentId = null,
            TextSpan? sourceSpan = null,
            string originalFilePath = null,
            int originalStartLine = 0,
            int originalStartColumn = 0,
            int originalEndLine = 0,
            int originalEndColumn = 0,
            string mappedFilePath = null,
            int mappedStartLine = 0,
            int mappedStartColumn = 0,
            int mappedEndLine = 0,
            int mappedEndColumn = 0)
        {
            DocumentId = documentId;
            SourceSpan = sourceSpan;
            MappedFilePath = mappedFilePath;
            MappedStartLine = mappedStartLine;
            MappedStartColumn = mappedStartColumn;
            MappedEndLine = mappedEndLine;
            MappedEndColumn = mappedEndColumn;
            OriginalFilePath = originalFilePath;
            OriginalStartLine = originalStartLine;
            OriginalStartColumn = originalStartColumn;
            OriginalEndLine = originalEndLine;
            OriginalEndColumn = originalEndColumn;
        }

        internal DiagnosticDataLocation WithCalculatedSpan(TextSpan newSourceSpan)
        {
            Contract.ThrowIfTrue(SourceSpan.HasValue);

            return new DiagnosticDataLocation(DocumentId,
                newSourceSpan, OriginalFilePath,
                OriginalStartLine, OriginalStartColumn,
                OriginalEndLine, OriginalEndColumn,
                MappedFilePath, MappedStartLine, MappedStartColumn,
                MappedEndLine, MappedEndColumn);
        }
    }
}
