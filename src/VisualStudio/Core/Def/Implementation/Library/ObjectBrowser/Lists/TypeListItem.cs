// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.Library.ObjectBrowser.Lists
{
    internal class TypeListItem : SymbolListItem<INamedTypeSymbol>
    {
        private readonly TypeKind _typeKind;

        internal TypeListItem(ProjectId projectId, INamedTypeSymbol typeSymbol, string displayText, string fullNameText, string searchText, bool isHidden)
            : base(projectId, typeSymbol, displayText, fullNameText, searchText, isHidden)
        {
            _typeKind = typeSymbol.TypeKind;
        }

        public TypeKind Kind
        {
            get { return _typeKind; }
        }
    }
}
