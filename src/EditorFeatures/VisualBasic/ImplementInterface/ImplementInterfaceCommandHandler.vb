' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.ComponentModel.Composition
Imports System.Diagnostics.CodeAnalysis
Imports System.Threading
Imports Microsoft.CodeAnalysis.Collections
Imports Microsoft.CodeAnalysis.Editor.[Shared].Utilities
Imports Microsoft.CodeAnalysis.Editor.VisualBasic.Utilities.CommandHandlers
Imports Microsoft.CodeAnalysis.ImplementInterface
Imports Microsoft.CodeAnalysis.ImplementType
Imports Microsoft.CodeAnalysis.Options
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Microsoft.VisualStudio.Commanding
Imports Microsoft.VisualStudio.Language.Intellisense.AsyncCompletion
Imports Microsoft.VisualStudio.Text.Operations
Imports Microsoft.VisualStudio.Utilities

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.ImplementInterface
    <Export(GetType(ICommandHandler))>
    <ContentType(ContentTypeNames.VisualBasicContentType)>
    <Name("ImplementInterfaceCommandHandler")>
    <Order(Before:=PredefinedCommandHandlerNames.EndConstruct)>
    <Order(After:=PredefinedCompletionNames.CompletionCommandHandler)>
    Friend Class ImplementInterfaceCommandHandler
        Inherits AbstractImplementAbstractClassOrInterfaceCommandHandler

        <ImportingConstructor>
        <SuppressMessage("RoslynDiagnosticsReliability", "RS0033:Importing constructor should be [Obsolete]", Justification:="Used in test code: https://github.com/dotnet/roslyn/issues/42814")>
        Public Sub New(
                threadingContext As IThreadingContext,
                editorOperationsFactoryService As IEditorOperationsFactoryService,
                globalOptions As IGlobalOptionService)
            MyBase.New(threadingContext, editorOperationsFactoryService, globalOptions)
        End Sub

        Protected Overrides Async Function TryGetNewDocumentAsync(
            document As Document,
            typeSyntax As TypeSyntax,
            cancellationToken As CancellationToken) As Task(Of Document)

            If typeSyntax.Parent.Kind <> SyntaxKind.ImplementsStatement Then
                Return Nothing
            End If

            Dim service = document.GetLanguageService(Of IImplementInterfaceService)()
            Dim options = Await document.GetImplementTypeOptionsAsync(cancellationToken).ConfigureAwait(True)

            Dim updatedDocument = Await service.ImplementInterfaceAsync(
                document,
                options,
                typeSyntax.Parent,
                cancellationToken).ConfigureAwait(True)

            Dim changes = Await updatedDocument.GetTextChangesAsync(document, cancellationToken).ConfigureAwait(True)
            If changes.Any() Then
                Return updatedDocument
            End If

            Return Nothing
        End Function
    End Class
End Namespace
