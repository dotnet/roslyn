// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

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
