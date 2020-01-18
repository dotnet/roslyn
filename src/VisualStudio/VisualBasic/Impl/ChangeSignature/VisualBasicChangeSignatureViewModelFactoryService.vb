' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Composition
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Editor
Imports Microsoft.CodeAnalysis.Host.Mef
Imports Microsoft.CodeAnalysis.VisualBasic
Imports Microsoft.VisualStudio.LanguageServices.Implementation.ChangeSignature
Imports Microsoft.VisualStudio.Text

Namespace Microsoft.VisualStudio.LanguageServices.VisualBasic.ChangeSignature
    <ExportLanguageService(GetType(IChangeSignatureViewModelFactoryService), LanguageNames.VisualBasic), [Shared]>
    Friend Class VisualBasicChangeSignatureViewModelFactoryService
        Inherits ChangeSignatureViewModelFactoryService

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
            Return {
                New SymbolDisplayPart(SymbolDisplayPartKind.ParameterName, Nothing, addedParameterViewModel.ParameterName),
                New SymbolDisplayPart(SymbolDisplayPartKind.Keyword, Nothing, " As " + addedParameterViewModel.Type)}
        End Function

        Public Overrides Function IsTypeNameValid(typeName As String) As Boolean
            Return Not SyntaxFactory.ParseTypeName(typeName).ContainsDiagnostics
        End Function
    End Class
End Namespace
