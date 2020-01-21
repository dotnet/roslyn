' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Immutable
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Completion
Imports Microsoft.CodeAnalysis.Editor.Implementation.IntelliSense.AsyncCompletion
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Completion
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces
Imports Microsoft.CodeAnalysis.VisualBasic.Completion
Imports Microsoft.VisualStudio.Language.Intellisense.AsyncCompletion.Data
Imports RoslynCompletion = Microsoft.CodeAnalysis.Completion

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Completion.CompletionProviders
    Public MustInherit Class AbstractVisualBasicCompletionProviderTests
        Inherits AbstractCompletionProviderTests(Of VisualBasicTestWorkspaceFixture)

        Public Sub New(workspaceFixture As VisualBasicTestWorkspaceFixture)
            MyBase.New(workspaceFixture)
        End Sub

        Protected Overrides Function CreateWorkspace(fileContents As String) As TestWorkspace
            Return TestWorkspace.CreateVisualBasic(fileContents)
        End Function

        Friend Overrides Function CreateCompletionService(workspace As Workspace, exclusiveProviders As ImmutableArray(Of CompletionProvider)) As CompletionServiceWithProviders
            Return New VisualBasicCompletionService(workspace, exclusiveProviders)
        End Function

        Private Protected Overrides Function BaseVerifyWorkerAsync(
                code As String, position As Integer,
                expectedItemOrNull As String, expectedDescriptionOrNull As String,
                sourceCodeKind As SourceCodeKind, usePreviousCharAsTrigger As Boolean,
                checkForAbsence As Boolean, glyph As Integer?, matchPriority As Integer?,
                hasSuggestionItem As Boolean?, displayTextSuffix As String, inlineDescription As String,
                matchingFilters As List(Of CompletionFilter)) As Task
            Return MyBase.VerifyWorkerAsync(
                code, position, expectedItemOrNull, expectedDescriptionOrNull,
                sourceCodeKind, usePreviousCharAsTrigger, checkForAbsence,
                glyph, matchPriority, hasSuggestionItem, displayTextSuffix, inlineDescription,
                matchingFilters)
        End Function

        Private Protected Overrides Async Function VerifyWorkerAsync(
                code As String, position As Integer,
                expectedItemOrNull As String, expectedDescriptionOrNull As String,
                sourceCodeKind As SourceCodeKind, usePreviousCharAsTrigger As Boolean,
                checkForAbsence As Boolean, glyph As Integer?, matchPriority As Integer?,
                hasSuggestionItem As Boolean?, displayTextSuffix As String, inlineDescription As String,
                matchingFilters As List(Of CompletionFilter)) As Task
            ' Script/interactive support removed for now.
            ' TODO: Re-enable these when interactive is back in the product.
            If sourceCodeKind <> Microsoft.CodeAnalysis.SourceCodeKind.Regular Then
                Return
            End If

            Await VerifyAtPositionAsync(
                code, position, usePreviousCharAsTrigger, expectedItemOrNull, expectedDescriptionOrNull, sourceCodeKind,
                checkForAbsence, glyph, matchPriority, hasSuggestionItem, displayTextSuffix, inlineDescription,
                matchingFilters)

            Await VerifyAtEndOfFileAsync(
                code, position, usePreviousCharAsTrigger, expectedItemOrNull, expectedDescriptionOrNull, sourceCodeKind,
                checkForAbsence, glyph, matchPriority, hasSuggestionItem, displayTextSuffix, inlineDescription,
                matchingFilters)

            ' Items cannot be partially written if we're checking for their absence,
            ' or if we're verifying that the list will show up (without specifying an actual item)
            If Not checkForAbsence AndAlso expectedItemOrNull <> Nothing Then
                Await VerifyAtPosition_ItemPartiallyWrittenAsync(
                    code, position, usePreviousCharAsTrigger, expectedItemOrNull, expectedDescriptionOrNull,
                    sourceCodeKind, checkForAbsence, glyph, matchPriority, hasSuggestionItem, displayTextSuffix,
                    inlineDescription, matchingFilters)

                Await VerifyAtEndOfFile_ItemPartiallyWrittenAsync(
                    code, position, usePreviousCharAsTrigger, expectedItemOrNull, expectedDescriptionOrNull,
                    sourceCodeKind, checkForAbsence, glyph, matchPriority, hasSuggestionItem, displayTextSuffix,
                    inlineDescription, matchingFilters)
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
            Using workspace = TestWorkspace.CreateVisualBasic(initialMarkup)
                Dim hostDocument = workspace.DocumentWithCursor
                Dim documentId = workspace.GetDocumentId(hostDocument)
                Dim document = workspace.CurrentSolution.GetDocument(documentId)
                Dim position = hostDocument.CursorPosition.Value

                Dim service = GetCompletionService(workspace)
                Dim completionList = Await GetCompletionListAsync(service, document, position, RoslynCompletion.CompletionTrigger.Invoke)
                Dim item = completionList.Items.First(Function(i) i.DisplayText.StartsWith(textTypedSoFar))

                Assert.Equal(expected, CommitManager.SendEnterThroughToEditor(service.GetRules(), item, textTypedSoFar))
            End Using
        End Function

        Protected Sub TestCommonIsTextualTriggerCharacter()
            Dim alwaysTriggerList =
            {
                "goo$$.",
                "goo$$[",
                "goo$$#",
                "goo$$ ",
                "goo$$="
            }

            For Each markup In alwaysTriggerList
                VerifyTextualTriggerCharacter(markup, shouldTriggerWithTriggerOnLettersEnabled:=True, shouldTriggerWithTriggerOnLettersDisabled:=True)
            Next

            Dim triggerOnlyWithLettersList =
            {
                "$$a",
                "$$_"
            }

            For Each markup In triggerOnlyWithLettersList
                VerifyTextualTriggerCharacter(markup, shouldTriggerWithTriggerOnLettersEnabled:=True, shouldTriggerWithTriggerOnLettersDisabled:=False)
            Next

            Dim neverTriggerList =
            {
                "goo$$x",
                "goo$$_"
            }

            For Each markup In neverTriggerList
                VerifyTextualTriggerCharacter(markup, shouldTriggerWithTriggerOnLettersEnabled:=False, shouldTriggerWithTriggerOnLettersDisabled:=False)
            Next
        End Sub
    End Class
End Namespace
