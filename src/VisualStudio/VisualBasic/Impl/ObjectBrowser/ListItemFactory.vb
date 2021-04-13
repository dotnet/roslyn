' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis
Imports Microsoft.VisualStudio.LanguageServices.Implementation.Library.ObjectBrowser

Namespace Microsoft.VisualStudio.LanguageServices.VisualBasic.ObjectBrowser
    Friend Class ListItemFactory
        Inherits AbstractListItemFactory

        Private Shared ReadOnly s_memberDisplayFormat As SymbolDisplayFormat =
            New SymbolDisplayFormat(
                typeQualificationStyle:=SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces,
                genericsOptions:=SymbolDisplayGenericsOptions.IncludeTypeParameters Or SymbolDisplayGenericsOptions.IncludeVariance,
                memberOptions:=SymbolDisplayMemberOptions.IncludeExplicitInterface Or SymbolDisplayMemberOptions.IncludeParameters Or SymbolDisplayMemberOptions.IncludeType,
                parameterOptions:=SymbolDisplayParameterOptions.IncludeType,
                miscellaneousOptions:=SymbolDisplayMiscellaneousOptions.UseSpecialTypes)

        Private Shared ReadOnly s_memberWithContainingTypeDisplayFormat As SymbolDisplayFormat =
            New SymbolDisplayFormat(
                typeQualificationStyle:=SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces,
                genericsOptions:=SymbolDisplayGenericsOptions.IncludeTypeParameters Or SymbolDisplayGenericsOptions.IncludeVariance,
                memberOptions:=SymbolDisplayMemberOptions.IncludeContainingType Or SymbolDisplayMemberOptions.IncludeExplicitInterface Or SymbolDisplayMemberOptions.IncludeParameters Or SymbolDisplayMemberOptions.IncludeType,
                parameterOptions:=SymbolDisplayParameterOptions.IncludeType,
                miscellaneousOptions:=SymbolDisplayMiscellaneousOptions.UseSpecialTypes)

        Protected Overrides Function GetMemberAndTypeDisplayString(memberSymbol As ISymbol) As String
            Return memberSymbol.ToDisplayString(s_memberWithContainingTypeDisplayFormat)
        End Function

        Protected Overrides Function GetMemberDisplayString(memberSymbol As ISymbol) As String
            Return memberSymbol.ToDisplayString(s_memberDisplayFormat)
        End Function
    End Class
End Namespace
