// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Threading;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Diagnostics
{
    /// <summary>
    /// An implementation of <see cref="AdditionalText"/> for the compiler that wraps a <see cref="TextDocumentState"/>.
    /// </summary>
    internal sealed class AdditionalTextWithState : AdditionalText
    {
        private readonly TextDocumentState _documentState;

        /// <summary>
        /// Create a <see cref="SourceText"/> from a <see cref="TextDocumentState"/>. <paramref name="documentState"/> should be non-null.
        /// </summary>
        public AdditionalTextWithState(TextDocumentState documentState)
        {
            _documentState = documentState ?? throw new ArgumentNullException(nameof(documentState));
        }

        /// <summary>
        /// Resolved path of the document.
        /// </summary>
        public override string Path => _documentState.FilePath ?? _documentState.Name;

        /// <summary>
        /// Retrieves a <see cref="SourceText"/> with the contents of this file.
        /// </summary>
        public override SourceText GetText(CancellationToken cancellationToken = default)
        {
            var text = _documentState.GetTextSynchronously(cancellationToken);
            return text;
        }
    }
}
