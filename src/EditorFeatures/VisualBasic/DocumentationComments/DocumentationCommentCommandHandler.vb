' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.ComponentModel.Composition
Imports System.Diagnostics.CodeAnalysis
Imports Microsoft.CodeAnalysis.Editor.Host
Imports Microsoft.CodeAnalysis.Editor.Implementation.DocumentationComments
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Microsoft.VisualStudio.Commanding
Imports Microsoft.VisualStudio.Language.Intellisense.AsyncCompletion
Imports Microsoft.VisualStudio.Text.Operations
Imports Microsoft.VisualStudio.Utilities

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.DocumentationComments
    <Export(GetType(ICommandHandler))>
    <ContentType(ContentTypeNames.VisualBasicContentType)>
    <Name(PredefinedCommandHandlerNames.DocumentationComments)>
    <Order(After:=PredefinedCommandHandlerNames.Rename)>
    <Order(After:=PredefinedCompletionNames.CompletionCommandHandler)>
    Friend Class DocumentationCommentCommandHandler
        Inherits AbstractDocumentationCommentCommandHandler(Of DocumentationCommentTriviaSyntax, DeclarationStatementSyntax)

        <ImportingConstructor()>
        <SuppressMessage("RoslynDiagnosticsReliability", "RS0033:Importing constructor should be [Obsolete]", Justification:="Used in test code: https://github.com/dotnet/roslyn/issues/42814")>
        Public Sub New(
            waitIndicator As IWaitIndicator,
            undoHistoryRegistry As ITextUndoHistoryRegistry,
            editorOperationsFactoryService As IEditorOperationsFactoryService)

            MyBase.New(waitIndicator, undoHistoryRegistry, editorOperationsFactoryService)
        End Sub

        Protected Overrides ReadOnly Property ExteriorTriviaText As String
            Get
                Return "'''"
            End Get
        End Property
    End Class
End Namespace
