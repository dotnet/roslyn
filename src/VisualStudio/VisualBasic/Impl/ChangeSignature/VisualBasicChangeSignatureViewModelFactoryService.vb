' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Composition
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Host.Mef
Imports Microsoft.CodeAnalysis.VisualBasic
Imports Microsoft.VisualStudio.LanguageServices.Implementation.ChangeSignature

Namespace Microsoft.VisualStudio.LanguageServices.VisualBasic.ChangeSignature
    <ExportLanguageService(GetType(IChangeSignatureViewModelFactoryService), LanguageNames.VisualBasic), [Shared]>
    Friend Class VisualBasicChangeSignatureViewModelFactoryService
        Inherits ChangeSignatureViewModelFactoryService

        <ImportingConstructor>
        Public Sub New()
        End Sub

        Public Overrides Function GeneratePreviewDisplayParts(addedParameterViewModel As ChangeSignatureDialogViewModel.AddedParameterViewModel) As SymbolDisplayPart()
            Return {
                New SymbolDisplayPart(SymbolDisplayPartKind.ParameterName, Nothing, addedParameterViewModel.ParameterName),
                New SymbolDisplayPart(SymbolDisplayPartKind.Keyword, Nothing, " As " + addedParameterViewModel.Type)}
        End Function

        Public Overrides Function IsTypeNameValid(typeName As String) As Boolean
            Return Not SyntaxFactory.ParseTypeName(typeName).ContainsDiagnostics
        End Function

        Public Overrides Function GetTypeNode(typeName As String) As SyntaxNode
            Return SyntaxFactory.ParseTypeName(typeName)
        End Function
    End Class
End Namespace
