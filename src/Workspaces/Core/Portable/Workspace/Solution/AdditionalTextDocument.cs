// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.IO;
using System.Threading;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Diagnostics
{
    /// <summary>
    /// Represents a non source code file.
    /// </summary>
    internal sealed class AdditionalTextDocument : AdditionalText
    {
        private readonly TextDocumentState document;

        /// <summary>
        /// Create a <see cref="SourceText"/> from a <see cref="TextDocumentState"/>. <paramref name="document"/> should be non-null.
        /// </summary>
        public AdditionalTextDocument(TextDocumentState document)
        {
            if (document == null)
            {
                throw new ArgumentNullException(nameof(document));
            }

            this.document = document;
        }

        /// <summary>
        /// Resolved path of the document.
        /// </summary>
        public override string Path => this.document.FilePath ?? this.document.Name;

        /// <summary>
        /// Retrieves a <see cref="SourceText"/> with the contents of this file.
        /// </summary>
        public override SourceText GetText(CancellationToken cancellationToken = default(CancellationToken))
        {
            var text = document.GetTextAsync(cancellationToken).WaitAndGetResult(cancellationToken);
            return text;
        }
    }
}
