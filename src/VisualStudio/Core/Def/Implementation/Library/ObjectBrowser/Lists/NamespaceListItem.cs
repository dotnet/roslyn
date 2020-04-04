// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.Library.ObjectBrowser.Lists
{
    internal class NamespaceListItem : SymbolListItem<INamespaceSymbol>
    {
        public NamespaceListItem(Project project, INamespaceSymbol namespaceSymbol, string displayText, string fullNameText, string searchText)
            : base(project, namespaceSymbol, displayText, fullNameText, searchText, isHidden: false)
        {
        }
    }
}
