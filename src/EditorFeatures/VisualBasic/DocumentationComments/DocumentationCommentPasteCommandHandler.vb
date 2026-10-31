' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.ComponentModel.Composition
Imports Microsoft.CodeAnalysis.Host.Mef
Imports Microsoft.VisualStudio.Commanding
Imports Microsoft.VisualStudio.Text.Editor.Commanding.Commands
Imports Microsoft.VisualStudio.Utilities

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.DocumentationComments
    ' Line commit wraps paste in an undo transaction. Run outside that handler so its transaction is complete
    ' before the documentation-comment adjustment starts, allowing the adjustment to be undone separately.
    ' Keep this export separate from the typing/Return handlers, which must run after completion.
    <Export(GetType(ICommandHandler))>
    <ContentType(ContentTypeNames.VisualBasicContentType)>
    <Name(PredefinedCommandHandlerNames.DocumentationCommentsPaste)>
    <Order(After:=PredefinedCommandHandlerNames.Rename)>
    <Order(Before:=PredefinedCommandHandlerNames.Commit)>
    Friend Class DocumentationCommentPasteCommandHandler
        Implements IChainedCommandHandler(Of PasteCommandArgs)

        Private ReadOnly _handler As DocumentationCommentCommandHandler

        <ImportingConstructor>
        <Obsolete(MefConstruction.ImportingConstructorMessage, True)>
        Public Sub New(handler As DocumentationCommentCommandHandler)
            _handler = handler
        End Sub

        Public ReadOnly Property DisplayName As String Implements INamed.DisplayName
            Get
                Return _handler.DisplayName
            End Get
        End Property

        Public Function GetCommandState(args As PasteCommandArgs, nextHandler As Func(Of CommandState)) As CommandState Implements IChainedCommandHandler(Of PasteCommandArgs).GetCommandState
            Return nextHandler()
        End Function

        Public Sub ExecuteCommand(args As PasteCommandArgs, nextHandler As Action, context As CommandExecutionContext) Implements IChainedCommandHandler(Of PasteCommandArgs).ExecuteCommand
            _handler.ExecutePasteCommand(args, nextHandler)
        End Sub
    End Class
End Namespace
