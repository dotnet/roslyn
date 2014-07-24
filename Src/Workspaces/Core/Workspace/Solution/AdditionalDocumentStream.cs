// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.IO;
using System.Text;
using System.Threading;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Diagnostics
{
    /// <summary>
    /// Represents a non source code file.
    /// </summary>
    internal sealed class AdditionalDocumentStream : AdditionalStream
    {
        private readonly TextDocumentState document;

        /// <summary>
        /// Create a stream from a <see cref="Document"/>. <paramref name="document"/> should be non-null.
        /// </summary>
        public AdditionalDocumentStream(TextDocumentState document)
        {
            if (document == null)
            {
                throw new ArgumentNullException("document");
            }

            this.document = document;
        }

        /// <summary>
        /// Resolved path of the stream.
        /// </summary>
        public override string Path
        {
            get
            {
                return this.document.FilePath ?? this.document.Name;
            }
        }

        /// <summary>
        /// Opens a <see cref="Stream"/> that allows reading the content of this file.
        /// </summary>
        public override Stream OpenRead(CancellationToken cancellationToken = default(CancellationToken))
        {
            var text = document.GetTextAsync(cancellationToken).WaitAndGetResult(cancellationToken);
            var encoding = text.Encoding ?? Encoding.UTF8;
            var stream = new MemoryStream(encoding.GetBytes(text.ToString()), writable: false);
            return stream;
        }
    }
}