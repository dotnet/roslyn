' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Immutable
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Completion
Imports Microsoft.CodeAnalysis.Editor.Implementation.IntelliSense.Completion
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Completion
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces
Imports Microsoft.CodeAnalysis.VisualBasic.Completion

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Completion.CompletionProviders
    Public MustInherit Class AbstractVisualBasicCompletionProviderTests
        Inherits AbstractCompletionProviderTests(Of VisualBasicTestWorkspaceFixture)

        Public Sub New(workspaceFixture As VisualBasicTestWorkspaceFixture)
            MyBase.New(workspaceFixture)
        End Sub

        Protected Overrides Function CreateWorkspaceAsync(fileContents As String) As Task(Of TestWorkspace)
            Return TestWorkspace.CreateVisualBasicAsync(fileContents)
        End Function

        Friend Overrides Function CreateCompletionService(workspace As Workspace, exclusiveProviders As ImmutableArray(Of CompletionProvider)) As CompletionServiceWithProviders
            Return New VisualBasicCompletionService(workspace, exclusiveProviders)
        End Function

        Protected Overrides Function BaseVerifyWorkerAsync(
                code As String, position As Integer,
                expectedItemOrNull As String, expectedDescriptionOrNull As String,
                sourceCodeKind As SourceCodeKind, usePreviousCharAsTrigger As Boolean,
                checkForAbsence As Boolean, glyph As Integer?, matchPriority As Integer?) As Task
            Return MyBase.VerifyWorkerAsync(
                code, position, expectedItemOrNull, expectedDescriptionOrNull,
                sourceCodeKind, usePreviousCharAsTrigger, checkForAbsence,
                glyph, matchPriority)
        End Function

        Protected Overrides Async Function VerifyWorkerAsync(
                code As String, position As Integer,
                expectedItemOrNull As String, expectedDescriptionOrNull As String,
                sourceCodeKind As SourceCodeKind, usePreviousCharAsTrigger As Boolean,
                checkForAbsence As Boolean, glyph As Integer?, matchPriority As Integer?) As Task
            ' Script/interactive support removed for now.
            ' TODO: Re-enable these when interactive is back in the product.
            If sourceCodeKind <> Microsoft.CodeAnalysis.SourceCodeKind.Regular Then
                Return
            End If

            Await VerifyAtPositionAsync(code, position, usePreviousCharAsTrigger, expectedItemOrNull, expectedDescriptionOrNull, sourceCodeKind, checkForAbsence, glyph, matchPriority)
            Await VerifyAtEndOfFileAsync(code, position, usePreviousCharAsTrigger, expectedItemOrNull, expectedDescriptionOrNull, sourceCodeKind, checkForAbsence, glyph, matchPriority)

            ' Items cannot be partially written if we're checking for their absence,
            ' or if we're verifying that the list will show up (without specifying an actual item)
            If Not checkForAbsence AndAlso expectedItemOrNull <> Nothing Then
                Await VerifyAtPosition_ItemPartiallyWrittenAsync(code, position, usePreviousCharAsTrigger, expectedItemOrNull, expectedDescriptionOrNull, sourceCodeKind, checkForAbsence, glyph, matchPriority)
                Await VerifyAtEndOfFile_ItemPartiallyWrittenAsync(code, position, usePreviousCharAsTrigger, expectedItemOrNull, expectedDescriptionOrNull, sourceCodeKind, checkForAbsence, glyph, matchPriority)
            End If
        End Function

        Protected Overrides Async Function VerifyCustomCommitProviderWorkerAsync(codeBeforeCommit As String, position As Integer, itemToCommit As String, expectedCodeAfterCommit As String, sourceCodeKind As SourceCodeKind, Optional commitChar As Char? = Nothing) As Task
            ' Script/interactive support removed for now.
            ' TODO: Re-enable these when interactive is back in the product.
            If sourceCodeKind <> Microsoft.CodeAnalysis.SourceCodeKind.Regular Then
                Return
            End If

            Await MyBase.VerifyCustomCommitProviderWorkerAsync(codeBeforeCommit, position, itemToCommit, expectedCodeAfterCommit, sourceCodeKind, commitChar)
        End Function

        Protected Overrides Function ItemPartiallyWritten(expectedItemOrNull As String) As String
            If expectedItemOrNull(0) = "[" Then
                Return expectedItemOrNull.Substring(1, 1)
            End If
            Return expectedItemOrNull.Substring(0, 1)
        End Function

        Protected Function AddInsideMethod(text As String) As String
            Return "Class C" & vbCrLf &
                   "    Function F()" & vbCrLf &
                   "        " & text & vbCrLf &
                   "    End Function" & vbCrLf &
                   "End Class"
        End Function

        Protected Function CreateContent(ParamArray contents As String()) As String
            Return String.Join(vbCrLf, contents)
        End Function

        Protected Function AddImportsStatement(importsStatement As String, text As String) As String
            Return importsStatement & vbCrLf & vbCrLf & text
        End Function

        Protected Async Function VerifySendEnterThroughToEditorAsync(
                initialMarkup As String, textTypedSoFar As String, expected As Boolean) As Task
            Using workspace = Await TestWorkspace.CreateVisualBasicAsync(initialMarkup)
                Dim hostDocument = workspace.DocumentWithCursor
                Dim documentId = workspace.GetDocumentId(hostDocument)
                Dim document = workspace.CurrentSolution.GetDocument(documentId)
                Dim position = hostDocument.CursorPosition.Value

                Dim service = GetCompletionService(workspace)
                Dim completionList = Await GetCompletionListAsync(service, document, position, CompletionTrigger.Default)
                Dim item = completionList.Items.First(Function(i) i.DisplayText.StartsWith(textTypedSoFar))

                Assert.Equal(expected, Controller.SendEnterThroughToEditor(service.GetRules(), item, textTypedSoFar))
            End Using
        End Function

        Protected Async Function TestCommonIsTextualTriggerCharacterAsync() As Task
            Dim alwaysTriggerList =
            {
                "foo$$.",
                "foo$$[",
                "foo$$#",
                "foo$$ ",
                "foo$$="
            }

            For Each markup In alwaysTriggerList
                Await VerifyTextualTriggerCharacterAsync(markup, shouldTriggerWithTriggerOnLettersEnabled:=True, shouldTriggerWithTriggerOnLettersDisabled:=True)
            Next

            Dim triggerOnlyWithLettersList =
            {
                "$$a",
                "$$_"
            }

            For Each markup In triggerOnlyWithLettersList
                Await VerifyTextualTriggerCharacterAsync(markup, shouldTriggerWithTriggerOnLettersEnabled:=True, shouldTriggerWithTriggerOnLettersDisabled:=False)
            Next

            Dim neverTriggerList =
            {
                "foo$$x",
                "foo$$_"
            }

            For Each markup In neverTriggerList
                Await VerifyTextualTriggerCharacterAsync(markup, shouldTriggerWithTriggerOnLettersEnabled:=False, shouldTriggerWithTriggerOnLettersDisabled:=False)
            Next
        End Function
    End Class
End Namespace
