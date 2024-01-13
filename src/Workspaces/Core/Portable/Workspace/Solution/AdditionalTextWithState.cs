// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Diagnostics
{
    /// <summary>
    /// An implementation of <see cref="AdditionalText"/> for the compiler that wraps a <see cref="AdditionalDocumentState"/>.
    /// </summary>
    /// <remarks>
    /// Create a <see cref="SourceText"/> from a <see cref="AdditionalDocumentState"/>.
    /// </remarks>
    internal sealed class AdditionalTextWithState(AdditionalDocumentState documentState) : AdditionalText
    {
        private readonly AdditionalDocumentState _documentState = documentState ?? throw new ArgumentNullException(nameof(documentState));

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
