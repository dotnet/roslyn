' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Composition
Imports System.Threading
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Host
Imports Microsoft.CodeAnalysis.Host.Mef
Imports Microsoft.CodeAnalysis.Shared.Extensions
Imports Microsoft.CodeAnalysis.VisualBasic
Imports Microsoft.VisualStudio.LanguageServices.Implementation.Progression

Namespace Microsoft.VisualStudio.LanguageServices.VisualBasic.Progression
    <ExportLanguageService(GetType(IProgressionLanguageService), LanguageNames.VisualBasic), [Shared]>
    Partial Friend Class VisualBasicProgressionLanguageService
        Implements IProgressionLanguageService

        <ImportingConstructor>
        <Obsolete(MefConstruction.ImportingConstructorMessage, True)>
        Public Sub New()
        End Sub

        Private Shared ReadOnly s_descriptionFormat As SymbolDisplayFormat = New SymbolDisplayFormat(
            globalNamespaceStyle:=SymbolDisplayGlobalNamespaceStyle.OmittedAsContaining,
            typeQualificationStyle:=SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces,
            genericsOptions:=SymbolDisplayGenericsOptions.IncludeTypeParameters,
            memberOptions:=SymbolDisplayMemberOptions.IncludeParameters Or SymbolDisplayMemberOptions.IncludeContainingType,
            parameterOptions:=SymbolDisplayParameterOptions.IncludeType Or SymbolDisplayParameterOptions.IncludeParamsRefOut Or SymbolDisplayParameterOptions.IncludeOptionalBrackets,
            miscellaneousOptions:=SymbolDisplayMiscellaneousOptions.UseSpecialTypes)

        Public Function GetDescriptionForSymbol(symbol As ISymbol, includeContainingSymbol As Boolean) As String Implements IProgressionLanguageService.GetDescriptionForSymbol
            Return GetSymbolText(symbol, False, s_descriptionFormat)
        End Function

        Private Shared ReadOnly s_labelFormat As SymbolDisplayFormat = New SymbolDisplayFormat(
            genericsOptions:=SymbolDisplayGenericsOptions.IncludeTypeParameters,
            memberOptions:=SymbolDisplayMemberOptions.IncludeParameters Or SymbolDisplayMemberOptions.IncludeType,
            parameterOptions:=SymbolDisplayParameterOptions.IncludeType Or SymbolDisplayParameterOptions.IncludeParamsRefOut Or SymbolDisplayParameterOptions.IncludeOptionalBrackets,
            miscellaneousOptions:=SymbolDisplayMiscellaneousOptions.UseSpecialTypes)

        Public Function GetLabelForSymbol(symbol As ISymbol, includeContainingSymbol As Boolean) As String Implements IProgressionLanguageService.GetLabelForSymbol
            Return GetSymbolText(symbol, includeContainingSymbol, s_labelFormat)
        End Function

        Private Shared Function GetSymbolText(symbol As ISymbol, includeContainingSymbol As Boolean, displayFormat As SymbolDisplayFormat) As String
            If symbol.Kind = SymbolKind.Field AndAlso symbol.ContainingType.TypeKind = TypeKind.Enum Then
                displayFormat = displayFormat.RemoveMemberOptions(SymbolDisplayMemberOptions.IncludeType)
            End If

            Dim label As String = symbol.ToDisplayString(displayFormat)

            If includeContainingSymbol AndAlso symbol.ContainingSymbol IsNot Nothing Then
                label += " (" + symbol.ContainingSymbol.ToDisplayString(displayFormat) + ")"
            End If

            Return label
        End Function
    End Class
End Namespace
