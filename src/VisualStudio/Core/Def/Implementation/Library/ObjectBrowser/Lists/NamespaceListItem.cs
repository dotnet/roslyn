// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.Library.ObjectBrowser.Lists
{
    internal class NamespaceListItem : SymbolListItem<INamespaceSymbol>
    {
        public NamespaceListItem(ProjectId projectId, INamespaceSymbol namespaceSymbol, string displayText, string fullNameText, string searchText)
            : base(projectId, namespaceSymbol, displayText, fullNameText, searchText, isHidden: false)
        {
        }
    }
}
