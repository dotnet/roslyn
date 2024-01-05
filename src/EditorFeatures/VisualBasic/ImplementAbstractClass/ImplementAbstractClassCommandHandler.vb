' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.ComponentModel.Composition
Imports System.Diagnostics.CodeAnalysis
Imports System.Threading
Imports Microsoft.CodeAnalysis.Editor.VisualBasic.Utilities.CommandHandlers
Imports Microsoft.CodeAnalysis.ImplementAbstractClass
Imports Microsoft.CodeAnalysis.ImplementType
Imports Microsoft.CodeAnalysis.Options
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Microsoft.VisualStudio.Commanding
Imports Microsoft.VisualStudio.Language.Intellisense.AsyncCompletion
Imports Microsoft.VisualStudio.Text.Operations
Imports Microsoft.VisualStudio.Utilities

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.ImplementAbstractClass
    <Export(GetType(ICommandHandler))>
    <ContentType(ContentTypeNames.VisualBasicContentType)>
    <Name("ImplementAbstractClassCommandHandler")>
    <Order(Before:=PredefinedCommandHandlerNames.EndConstruct)>
    <Order(After:=PredefinedCompletionNames.CompletionCommandHandler)>
    Friend Class ImplementAbstractClassCommandHandler
        Inherits AbstractImplementAbstractClassOrInterfaceCommandHandler

        <ImportingConstructor>
        <SuppressMessage("RoslynDiagnosticsReliability", "RS0033:Importing constructor should be [Obsolete]", Justification:="Used in test code: https://github.com/dotnet/roslyn/issues/42814")>
        Public Sub New(editorOperationsFactoryService As IEditorOperationsFactoryService,
                       globalOptions As IGlobalOptionService)
            MyBase.New(editorOperationsFactoryService, globalOptions)
        End Sub

        Protected Overrides Function TryGetNewDocument(
            document As Document,
            options As ImplementTypeGenerationOptions,
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

            Dim updatedDocument = ImplementAbstractClassData.TryImplementAbstractClassAsync(
                document, classBlock, classBlock.ClassStatement.Identifier, options, cancellationToken).WaitAndGetResult(cancellationToken)
            If updatedDocument IsNot Nothing AndAlso
                updatedDocument.GetTextChangesAsync(document, cancellationToken).WaitAndGetResult(cancellationToken).Count = 0 Then
                Return Nothing
            End If

            Return updatedDocument
        End Function
    End Class
End Namespace
