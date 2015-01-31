' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis
Imports Microsoft.VisualStudio.LanguageServices.Implementation.Library.ObjectBrowser

Namespace Microsoft.VisualStudio.LanguageServices.VisualBasic.ObjectBrowser
    Friend Class ListItemFactory
        Inherits AbstractListItemFactory

        Private Shared ReadOnly MemberDisplayFormat As SymbolDisplayFormat =
            New SymbolDisplayFormat(
                typeQualificationStyle:=SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces,
                genericsOptions:=SymbolDisplayGenericsOptions.IncludeTypeParameters Or SymbolDisplayGenericsOptions.IncludeVariance,
                memberOptions:=SymbolDisplayMemberOptions.IncludeExplicitInterface Or SymbolDisplayMemberOptions.IncludeParameters Or SymbolDisplayMemberOptions.IncludeType,
                parameterOptions:=SymbolDisplayParameterOptions.IncludeType,
                miscellaneousOptions:=SymbolDisplayMiscellaneousOptions.UseSpecialTypes)

        Private Shared ReadOnly MemberWithContainingTypeDisplayFormat As SymbolDisplayFormat =
            New SymbolDisplayFormat(
                typeQualificationStyle:=SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces,
                genericsOptions:=SymbolDisplayGenericsOptions.IncludeTypeParameters Or SymbolDisplayGenericsOptions.IncludeVariance,
                memberOptions:=SymbolDisplayMemberOptions.IncludeContainingType Or SymbolDisplayMemberOptions.IncludeExplicitInterface Or SymbolDisplayMemberOptions.IncludeParameters Or SymbolDisplayMemberOptions.IncludeType,
                parameterOptions:=SymbolDisplayParameterOptions.IncludeType,
                miscellaneousOptions:=SymbolDisplayMiscellaneousOptions.UseSpecialTypes)

        Protected Overrides Function GetMemberAndTypeDisplayString(memberSymbol As ISymbol) As String
            Return memberSymbol.ToDisplayString(MemberWithContainingTypeDisplayFormat)
        End Function

        Protected Overrides Function GetMemberDisplayString(memberSymbol As ISymbol) As String
            Return memberSymbol.ToDisplayString(MemberDisplayFormat)
        End Function
    End Class
End Namespace
