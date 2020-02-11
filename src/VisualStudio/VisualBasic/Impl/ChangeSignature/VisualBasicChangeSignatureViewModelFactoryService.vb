' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Composition
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Editor
Imports Microsoft.CodeAnalysis.Host.Mef
Imports Microsoft.CodeAnalysis.VisualBasic
Imports Microsoft.VisualStudio.Editor
Imports Microsoft.VisualStudio.LanguageServices.Implementation.ChangeSignature
Imports Microsoft.VisualStudio.LanguageServices.Implementation.IntellisenseControls
Imports Microsoft.VisualStudio.Text
Imports Microsoft.VisualStudio.Text.Classification
Imports Microsoft.VisualStudio.Text.Operations
Imports Microsoft.VisualStudio.Utilities

Namespace Microsoft.VisualStudio.LanguageServices.VisualBasic.ChangeSignature
    <ExportLanguageService(GetType(IChangeSignatureViewModelFactoryService), LanguageNames.VisualBasic), [Shared]>
    Friend Class VisualBasicChangeSignatureViewModelFactoryService
        Inherits ChangeSignatureViewModelFactoryService

        <ImportingConstructor>
        Public Sub New(
            intellisenseTextBoxViewModelFactory As IntellisenseTextBoxViewModelFactory,
            contentTypeRegistryService As IContentTypeRegistryService,
            editorOperationsFactoryService As IEditorOperationsFactoryService,
            classificationFormatMapService As IClassificationFormatMapService,
            editorAdapterFactory As IVsEditorAdaptersFactoryService)
            MyBase.New(intellisenseTextBoxViewModelFactory, contentTypeRegistryService, editorOperationsFactoryService, classificationFormatMapService, editorAdapterFactory)
        End Sub

        Protected Overrides Function CreateSpansMethod(textSnapshot As ITextSnapshot, insertPosition As Integer) As ITrackingSpan()
            ' + 4 to support inserted ', [~'
            Return CreateTrackingSpansHelper(textSnapshot, contextPoint:=insertPosition + 4, spaceBetweenTypeAndName:=5)
        End Function

        ' We insert '[]' so that we're always able to generate the type even if the name field is empty. 
        ' We insert '~' to avoid Intellisense activation for the name field, since completion for names does not apply to VB.
        Protected Overrides ReadOnly Property TextToInsert As String
            Get
                Return ", [~] As "
            End Get
        End Property

        Protected Overrides ReadOnly Property ContentTypeName As String
            Get
                Return ContentTypeNames.VisualBasicContentType
            End Get
        End Property

        Public Overrides Function GeneratePreviewDisplayParts(addedParameterViewModel As ChangeSignatureDialogViewModel.AddedParameterViewModel) As SymbolDisplayPart()
            Dim parts = New List(Of SymbolDisplayPart)
            parts.Add(New SymbolDisplayPart(SymbolDisplayPartKind.ParameterName, Nothing, addedParameterViewModel.ParameterName))
            parts.Add(New SymbolDisplayPart(SymbolDisplayPartKind.Space, Nothing, " "))
            parts.Add(New SymbolDisplayPart(SymbolDisplayPartKind.Keyword, Nothing, "As"))
            parts.Add(New SymbolDisplayPart(SymbolDisplayPartKind.Space, Nothing, " "))
            parts.Add(New SymbolDisplayPart(SymbolDisplayPartKind.ClassName, Nothing, addedParameterViewModel.Type))

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
