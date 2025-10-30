// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.Library.ObjectBrowser.Lists;

internal sealed class TypeListItem : SymbolListItem<INamedTypeSymbol>
{
    internal TypeListItem(ProjectId projectId, INamedTypeSymbol typeSymbol, string displayText, string fullNameText, string searchText, bool isHidden)
        : base(projectId, typeSymbol, displayText, fullNameText, searchText, isHidden)
    {
        Kind = typeSymbol.TypeKind;
    }

    public TypeKind Kind { get; }
}
