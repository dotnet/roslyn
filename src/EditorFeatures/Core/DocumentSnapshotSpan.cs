﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.VisualStudio.Text;

namespace Microsoft.CodeAnalysis.Editor
{
    /// <summary>
    /// Represents an editor <see cref="VisualStudio.Text.SnapshotSpan"/> and the <see cref="CodeAnalysis.Document"/>
    /// the span was produced from.
    /// </summary>
    internal readonly struct DocumentSnapshotSpan
    {
        /// <summary>
        /// The <see cref="CodeAnalysis.Document"/> the span was produced from.
        /// </summary>
        public Document? Document { get; }

        /// <summary>
        /// The editor <see cref="VisualStudio.Text.SnapshotSpan"/>.
        /// </summary>
        public SnapshotSpan SnapshotSpan { get; }

        /// <summary>
        /// Creates a new <see cref="DocumentSnapshotSpan"/>.
        /// </summary>
        public DocumentSnapshotSpan(Document? document, SnapshotSpan snapshotSpan)
        {
            this.Document = document;
            this.SnapshotSpan = snapshotSpan;
        }
    }
}
