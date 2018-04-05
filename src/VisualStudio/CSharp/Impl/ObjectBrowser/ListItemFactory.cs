// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.LanguageServices.Implementation.Library.ObjectBrowser;

namespace Microsoft.VisualStudio.LanguageServices.CSharp.ObjectBrowser
{
    internal class ListItemFactory : AbstractListItemFactory
    {
        private static readonly SymbolDisplayFormat s_memberDisplayFormat =
            new SymbolDisplayFormat(
                typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces,
                genericsOptions: SymbolDisplayGenericsOptions.IncludeTypeParameters | SymbolDisplayGenericsOptions.IncludeVariance,
                memberOptions: SymbolDisplayMemberOptions.IncludeExplicitInterface | SymbolDisplayMemberOptions.IncludeParameters,
                parameterOptions: SymbolDisplayParameterOptions.IncludeType,
                miscellaneousOptions: SymbolDisplayMiscellaneousOptions.UseSpecialTypes);

        private static readonly SymbolDisplayFormat s_memberWithContainingTypeDisplayFormat =
            new SymbolDisplayFormat(
                typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces,
                genericsOptions: SymbolDisplayGenericsOptions.IncludeTypeParameters | SymbolDisplayGenericsOptions.IncludeVariance,
                memberOptions: SymbolDisplayMemberOptions.IncludeContainingType | SymbolDisplayMemberOptions.IncludeExplicitInterface | SymbolDisplayMemberOptions.IncludeParameters,
                parameterOptions: SymbolDisplayParameterOptions.IncludeType,
                miscellaneousOptions: SymbolDisplayMiscellaneousOptions.UseSpecialTypes);

        protected override string GetMemberDisplayString(ISymbol memberSymbol)
        {
            return memberSymbol.ToDisplayString(s_memberDisplayFormat);
        }

        protected override string GetMemberAndTypeDisplayString(ISymbol memberSymbol)
        {
            return memberSymbol.ToDisplayString(s_memberWithContainingTypeDisplayFormat);
        }
    }
}
