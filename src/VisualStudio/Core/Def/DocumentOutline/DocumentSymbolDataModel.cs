// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using Microsoft.VisualStudio.Text;

namespace Microsoft.VisualStudio.LanguageServices.DocumentOutline
{
    internal sealed class DocumentSymbolDataModel
    {
        public ImmutableArray<DocumentSymbolData> DocumentSymbolData { get; }
        public ITextSnapshot OriginalSnapshot { get; }

        public DocumentSymbolDataModel(ImmutableArray<DocumentSymbolData> documentSymbolData, ITextSnapshot originalSnapshot)
        {
            DocumentSymbolData = documentSymbolData;
            OriginalSnapshot = originalSnapshot;
        }
    }
}
