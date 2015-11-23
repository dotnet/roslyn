// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.VisualStudio.Text;

namespace Microsoft.CodeAnalysis.Editor
{
    /// <summary>
    /// Represents an editor <see cref="VisualStudio.Text.SnapshotSpan"/> and the <see cref="CodeAnalysis.Document"/> the span was produced from.
    /// </summary>
    internal struct DocumentSnapshotSpan
    {
        /// <summary>
        /// The <see cref="CodeAnalysis.Document"/> the span was produced from.
        /// </summary>
        public Document Document { get; }

        /// <summary>
        /// The editor <see cref="VisualStudio.Text.SnapshotSpan"/>.
        /// </summary>
        public SnapshotSpan SnapshotSpan { get; }

        /// <summary>
        /// Creates a new <see cref="DocumentSnapshotSpan"/>.
        /// </summary>
        public DocumentSnapshotSpan(Document document, SnapshotSpan snapshotSpan) : this()
        {
            this.Document = document;
            this.SnapshotSpan = snapshotSpan;
        }
    }
}
