' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.ComponentModel.Composition
Imports Microsoft.CodeAnalysis.Editor.Implementation.SplitComment
Imports Microsoft.CodeAnalysis.Host.Mef
Imports Microsoft.VisualStudio.Commanding
Imports Microsoft.VisualStudio.Language.Intellisense.AsyncCompletion
Imports Microsoft.VisualStudio.Text.Operations
Imports Microsoft.VisualStudio.Utilities

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.SplitComment
    <Export(GetType(ICommandHandler))>
    <ContentType(ContentTypeNames.VisualBasicContentType)>
    <Name(NameOf(VisualBasicSplitCommentCommandHandler))>
    <Order(After:=PredefinedCompletionNames.CompletionCommandHandler)>
    Partial Friend Class VisualBasicSplitCommentCommandHandler
        Inherits AbstractSplitCommentCommandHandler

        <ImportingConstructor>
        <Obsolete(MefConstruction.ImportingConstructorMessage, True)>
        Public Sub New(undoHistoryRegistry As ITextUndoHistoryRegistry,
                       editorOperationsFactoryService As IEditorOperationsFactoryService)
            MyBase.New(undoHistoryRegistry, editorOperationsFactoryService)
        End Sub

        Protected Overrides ReadOnly Property CommentStart As String = "'"
    End Class
End Namespace
