// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using Microsoft.CodeAnalysis;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.Library.ObjectBrowser.Lists
{
    internal abstract class SymbolListItem<TSymbol> : SymbolListItem
        where TSymbol : ISymbol
    {
        protected SymbolListItem(ProjectId projectId, TSymbol symbol, string displayText, string fullNameText, string searchText, bool isHidden)
           : base(projectId, symbol, displayText, fullNameText, searchText, isHidden)
        {
        }

        public TSymbol ResolveTypedSymbol(Compilation compilation)
            => (TSymbol)ResolveSymbol(compilation);
    }
}
