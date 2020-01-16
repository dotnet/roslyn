' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Composition
Imports System.Threading
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Host.Mef
Imports Microsoft.CodeAnalysis.VisualBasic
Imports Microsoft.VisualStudio.LanguageServices.Implementation.ChangeSignature
Imports Microsoft.VisualStudio.LanguageServices.Implementation.IntellisenseControls
Imports Microsoft.VisualStudio.Text
Imports Microsoft.VisualStudio.Text.Projection
Imports Microsoft.VisualStudio.Utilities

Namespace Microsoft.VisualStudio.LanguageServices.VisualBasic.ChangeSignature
    <ExportLanguageService(GetType(IChangeSignatureLanguageService), LanguageNames.VisualBasic), [Shared]>
    Friend Class VisualBasicChangeSignatureLanguageService
        Inherits ChangeSignatureLanguageService
        Public Overrides Async Function CreateViewModelsAsync(rolesCollectionType() As String,
                                              rolesCollectionName() As String,
                                              insertPosition As Integer, document As Document,
                                              documentText As String, contentType As IContentType,
                                              intellisenseTextBoxViewModelFactory As IntellisenseTextBoxViewModelFactory,
                                              cancellationToken As CancellationToken) As Task(Of IntellisenseTextBoxViewModel())
            Dim rolesCollections = {rolesCollectionName, rolesCollectionType}

            ' We insert '[]' so that we're always able to generate the type even if the name field is empty. 
            ' We insert '~' to avoid Intellisense activation for the name field, since completion for names does not apply to VB.
            Dim test = documentText.Insert(insertPosition, ", [~] As ")
            Return Await intellisenseTextBoxViewModelFactory.CreateIntellisenseTextBoxViewModelsAsync(
                document,
                contentType,
                documentText.Insert(insertPosition, ", [~] As "),
                Function(snapshot As ITextSnapshot)
                    ' + 4 to support inserted ', [~'
                    Return CreateTrackingSpansHelper(snapshot, contextPoint:=insertPosition + 4, spaceBetweenTypeAndName:=5)
                End Function,
                rolesCollections,
                cancellationToken).ConfigureAwait(False)
        End Function

        Public Overrides Sub GeneratePreviewDisplayParts(addedParameterViewModel As ChangeSignatureDialogViewModel.AddedParameterViewModel, displayParts As List(Of SymbolDisplayPart))
            displayParts.Add(New SymbolDisplayPart(SymbolDisplayPartKind.ParameterName, Nothing, addedParameterViewModel.Parameter))
            displayParts.Add(New SymbolDisplayPart(SymbolDisplayPartKind.Keyword, Nothing, " As " + addedParameterViewModel.Type))
        End Sub

        Public Overrides Function IsTypeNameValid(typeName As String) As Boolean
            Return Not SyntaxFactory.ParseTypeName(typeName).ContainsDiagnostics
        End Function
    End Class
End Namespace
