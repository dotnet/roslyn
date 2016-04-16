' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.ComponentModel.Composition
Imports System.Threading
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Editor
Imports Microsoft.CodeAnalysis.Editor.Shared.Extensions
Imports Microsoft.CodeAnalysis.Shared.Extensions
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Extensions
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Microsoft.VisualStudio.Editor
Imports Microsoft.VisualStudio.LanguageServices.Implementation.Snippets
Imports Microsoft.VisualStudio.Shell
Imports Microsoft.VisualStudio.Text
Imports Microsoft.VisualStudio.Text.Editor
Imports Microsoft.VisualStudio.TextManager.Interop
Imports Microsoft.VisualStudio.Utilities

Namespace Microsoft.VisualStudio.LanguageServices.VisualBasic.Snippets
    <ExportCommandHandler("VB Snippets", ContentTypeNames.VisualBasicContentType)>
    <Order(After:=PredefinedCommandHandlerNames.Completion)>
    <Order(After:=PredefinedCommandHandlerNames.IntelliSense)>
    Friend NotInheritable Class SnippetCommandHandler
        Inherits AbstractSnippetCommandHandler

        <ImportingConstructor>
        Public Sub New(editorAdaptersFactoryService As IVsEditorAdaptersFactoryService, serviceProvider As SVsServiceProvider)
            MyBase.New(editorAdaptersFactoryService, serviceProvider)
        End Sub

        Protected Overrides Function IsSnippetExpansionContext(document As Document, startPosition As Integer, cancellationToken As CancellationToken) As Boolean
            Dim syntaxTree = document.GetSyntaxTreeAsync(CancellationToken.None).WaitAndGetResult(cancellationToken)

            Return Not syntaxTree.IsEntirelyWithinStringOrCharOrNumericLiteral(startPosition, cancellationToken) AndAlso
                Not syntaxTree.IsEntirelyWithinComment(startPosition, cancellationToken) AndAlso
                Not syntaxTree.FindTokenOnRightOfPosition(startPosition, cancellationToken).HasAncestor(Of XmlElementSyntax)()
        End Function

        Protected Overrides Function GetSnippetExpansionClient(textView As ITextView, subjectBuffer As ITextBuffer) As AbstractSnippetExpansionClient
            Return SnippetExpansionClient.GetSnippetExpansionClient(textView, subjectBuffer, EditorAdaptersFactoryService)
        End Function

        Protected Overrides Sub InvokeInsertionUI(textView As ITextView, subjectBuffer As ITextBuffer, nextHandler As Action, Optional surroundWith As Boolean = False)
            Debug.Assert(Not surroundWith)

            Dim expansionManager As IVsExpansionManager = Nothing
            If Not TryGetExpansionManager(expansionManager) Then
                nextHandler()
                Return
            End If

            expansionManager.InvokeInsertionUI(
                EditorAdaptersFactoryService.GetViewAdapter(textView),
                GetSnippetExpansionClient(textView, subjectBuffer),
                Guids.VisualBasicDebuggerLanguageId,
                bstrTypes:=Nothing,
                iCountTypes:=0,
                fIncludeNULLType:=1,
                bstrKinds:=Nothing,
                iCountKinds:=0,
                fIncludeNULLKind:=1,
                bstrPrefixText:=BasicVSResources.InsertSnippet,
                bstrCompletionChar:=">"c)

        End Sub

        Protected Overrides Function TryInvokeSnippetPickerOnQuestionMark(textView As ITextView, subjectBuffer As ITextBuffer) As Boolean
            Dim text = subjectBuffer.AsTextContainer().CurrentText
            Dim caretPosition = textView.GetCaretPoint(subjectBuffer).Value.Position

            If (caretPosition > 1 AndAlso text(caretPosition - 1) = "?"c AndAlso CodeAnalysis.VisualBasic.SyntaxFacts.IsWhitespace(text(caretPosition - 2))) OrElse
                (caretPosition = 1 AndAlso text(0) = "?"c) Then

                DeleteQuestionMark(textView, subjectBuffer, caretPosition)
                InvokeInsertionUI(textView, subjectBuffer, Sub() Return)
                Return True
            End If

            Return False
        End Function

        Private Sub DeleteQuestionMark(textView As ITextView, subjectBuffer As ITextBuffer, caretPosition As Integer)
            Dim currentSnapshot = subjectBuffer.CurrentSnapshot
            Dim document = currentSnapshot.GetOpenDocumentInCurrentContextWithChanges()
            If document IsNot Nothing Then
                Dim editorWorkspace = document.Project.Solution.Workspace
                Dim text = currentSnapshot.AsText()
                Dim change = New TextChange(Microsoft.CodeAnalysis.Text.TextSpan.FromBounds(caretPosition - 1, caretPosition), String.Empty)
                editorWorkspace.ApplyTextChanges(document.Id, change, CancellationToken.None)
            End If
        End Sub

    End Class
End Namespace
