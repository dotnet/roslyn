// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.LanguageServices.Implementation.Library.ObjectBrowser;

namespace Microsoft.VisualStudio.LanguageServices.CSharp.ObjectBrowser;

internal sealed class ListItemFactory : AbstractListItemFactory
{
    private static readonly SymbolDisplayFormat s_memberDisplayFormat =
        new(
            typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces,
            genericsOptions: SymbolDisplayGenericsOptions.IncludeTypeParameters | SymbolDisplayGenericsOptions.IncludeVariance,
            memberOptions: SymbolDisplayMemberOptions.IncludeExplicitInterface | SymbolDisplayMemberOptions.IncludeParameters,
            parameterOptions: SymbolDisplayParameterOptions.IncludeType,
            miscellaneousOptions: SymbolDisplayMiscellaneousOptions.UseSpecialTypes);

    private static readonly SymbolDisplayFormat s_memberWithContainingTypeDisplayFormat =
        new(
            typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces,
            genericsOptions: SymbolDisplayGenericsOptions.IncludeTypeParameters | SymbolDisplayGenericsOptions.IncludeVariance,
            memberOptions: SymbolDisplayMemberOptions.IncludeContainingType | SymbolDisplayMemberOptions.IncludeExplicitInterface | SymbolDisplayMemberOptions.IncludeParameters,
            parameterOptions: SymbolDisplayParameterOptions.IncludeType,
            miscellaneousOptions: SymbolDisplayMiscellaneousOptions.UseSpecialTypes);

    protected override string GetMemberDisplayString(ISymbol memberSymbol)
        => memberSymbol.ToDisplayString(s_memberDisplayFormat);

    protected override string GetMemberAndTypeDisplayString(ISymbol memberSymbol)
        => memberSymbol.ToDisplayString(s_memberWithContainingTypeDisplayFormat);
}
