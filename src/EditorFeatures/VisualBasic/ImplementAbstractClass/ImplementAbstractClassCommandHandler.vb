﻿' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.ComponentModel.Composition
Imports System.Threading
Imports Microsoft.CodeAnalysis.Editor.VisualBasic.Utilities.CommandHandlers
Imports Microsoft.CodeAnalysis.ImplementAbstractClass
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Microsoft.VisualStudio.Text.Operations
Imports Microsoft.VisualStudio.Utilities
Imports VSCommanding = Microsoft.VisualStudio.Commanding

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.ImplementAbstractClass
    <Export(GetType(VSCommanding.ICommandHandler))>
    <ContentType(ContentTypeNames.VisualBasicContentType)>
    <Name("ImplementAbstractClassCommandHandler")>
    <Order(Before:=PredefinedCommandHandlerNames.EndConstruct)>
    <Order(After:=PredefinedCommandHandlerNames.Completion)>
    Friend Class ImplementAbstractClassCommandHandler
        Inherits AbstractImplementAbstractClassOrInterfaceCommandHandler

        <ImportingConstructor>
        Public Sub New(editorOperationsFactoryService As IEditorOperationsFactoryService)
            MyBase.New(editorOperationsFactoryService)
        End Sub

        Protected Overrides Function TryGetNewDocument(
            document As Document,
            typeSyntax As TypeSyntax,
            cancellationToken As CancellationToken
        ) As Document

            If typeSyntax.Parent.Kind <> SyntaxKind.InheritsStatement Then
                Return Nothing
            End If

            Dim classBlock = TryCast(typeSyntax.Parent.Parent, ClassBlockSyntax)
            If classBlock Is Nothing Then
                Return Nothing
            End If

            Dim service = document.GetLanguageService(Of IImplementAbstractClassService)()
            Dim updatedDocument = service.ImplementAbstractClassAsync(document, classBlock, cancellationToken).WaitAndGetResult(cancellationToken)
            If updatedDocument IsNot Nothing AndAlso
                updatedDocument.GetTextChangesAsync(document, cancellationToken).WaitAndGetResult(cancellationToken).Count = 0 Then
                Return Nothing
            End If

            Return updatedDocument
        End Function
    End Class
End Namespace
