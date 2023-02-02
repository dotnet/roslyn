// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using Microsoft.VisualStudio.Text;

namespace Microsoft.VisualStudio.LanguageServices.DocumentOutline
{
    /// <param name="DocumentSymbolData">The symbol data that we received from an LSP request.</param>
    /// <param name="OriginalSnapshot">The snapshot this data was generated from. Used to translate positions across edits.</param>
    internal sealed record DocumentSymbolDataModel(ImmutableArray<DocumentSymbolData> DocumentSymbolData, ITextSnapshot OriginalSnapshot);
}
