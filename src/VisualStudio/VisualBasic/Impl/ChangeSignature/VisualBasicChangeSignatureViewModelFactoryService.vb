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
        <Obsolete(MefConstruction.ImportingConstructorMessage, True)>
        Public Sub New()
        End Sub

        Public Overrides Function GeneratePreviewDisplayParts(addedParameterViewModel As ChangeSignatureDialogViewModel.AddedParameterViewModel) As SymbolDisplayPart()
            Dim parts = New List(Of SymbolDisplayPart)
            parts.Add(New SymbolDisplayPart(SymbolDisplayPartKind.ParameterName, Nothing, addedParameterViewModel.ParameterName))
            parts.Add(New SymbolDisplayPart(SymbolDisplayPartKind.Space, Nothing, " "))
            parts.Add(New SymbolDisplayPart(SymbolDisplayPartKind.Keyword, Nothing, "As"))
            parts.Add(New SymbolDisplayPart(SymbolDisplayPartKind.Space, Nothing, " "))

            ' TO-DO We need to add proper colorization for added parameters: 
            ' https://github.com/dotnet/roslyn/issues/47986
            Dim isPredefinedType = SyntaxFactory.ParseExpression(addedParameterViewModel.Type).Kind() = SyntaxKind.PredefinedType
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
            Static visualBasicParseOptions As New VisualBasicParseOptions(LanguageVersion.Latest)
            Return Not SyntaxFactory.ParseTypeName(typeName, options:=visualBasicParseOptions).ContainsDiagnostics
        End Function

        Public Overrides Function GetTypeNode(typeName As String) As SyntaxNode
            Return SyntaxFactory.ParseTypeName(typeName)
        End Function
    End Class
End Namespace
