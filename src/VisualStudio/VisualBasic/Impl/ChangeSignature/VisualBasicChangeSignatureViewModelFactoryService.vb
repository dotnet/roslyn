' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
            Dim parts = New List(Of SymbolDisplayPart)
            parts.Add(New SymbolDisplayPart(SymbolDisplayPartKind.ParameterName, Nothing, addedParameterViewModel.ParameterName))
            parts.Add(New SymbolDisplayPart(SymbolDisplayPartKind.Space, Nothing, " "))
            parts.Add(New SymbolDisplayPart(SymbolDisplayPartKind.Keyword, Nothing, "As"))
            parts.Add(New SymbolDisplayPart(SymbolDisplayPartKind.Space, Nothing, " "))

            Dim isPredefinedType = SyntaxFactory.ParseExpression(addedParameterViewModel.TypeWithoutErrorIndicator).Kind() = SyntaxKind.PredefinedType
            Dim typePartKind = If(isPredefinedType, SymbolDisplayPartKind.Keyword, SymbolDisplayPartKind.ClassName)

            parts.Add(New SymbolDisplayPart(typePartKind, Nothing, addedParameterViewModel.Type))

            If Not String.IsNullOrWhiteSpace(addedParameterViewModel.Default) Then
                parts.Add(New SymbolDisplayPart(SymbolDisplayPartKind.Space, Nothing, " "))
                parts.Add(New SymbolDisplayPart(SymbolDisplayPartKind.Punctuation, Nothing, "="))
                parts.Add(New SymbolDisplayPart(SymbolDisplayPartKind.Space, Nothing, " "))
                parts.Add(New SymbolDisplayPart(SymbolDisplayPartKind.Text, Nothing, addedParameterViewModel.Default))
            End If

            Return parts.ToArray()
        End Function

        Public Overrides Function IsTypeNameValid(typeName As String) As Boolean
            Return Not SyntaxFactory.ParseTypeName(typeName).ContainsDiagnostics
        End Function

        Public Overrides Function GetTypeNode(typeName As String) As SyntaxNode
            Return SyntaxFactory.ParseTypeName(typeName)
        End Function
    End Class
End Namespace
