' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

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

        Protected Overrides Function CreateWorkspace(fileContents As String) As EditorTestWorkspace
            Return EditorTestWorkspace.CreateVisualBasic(fileContents, composition:=GetComposition())
        End Function

        Friend Overrides Function GetCompletionService(project As Project) As CompletionService
            Return Assert.IsType(Of VisualBasicCompletionService)(MyBase.GetCompletionService(project))
        End Function

        Private Protected Overrides Function BaseVerifyWorkerAsync(
                code As String, position As Integer,
                expectedItemOrNull As String, expectedDescriptionOrNull As String,
                sourceCodeKind As SourceCodeKind, usePreviousCharAsTrigger As Boolean,
                checkForAbsence As Boolean, glyph As Integer?, matchPriority As Integer?,
                hasSuggestionItem As Boolean?, displayTextSuffix As String, displayTextPrefix As String, inlineDescription As String,
                isComplexTextEdit As Boolean?, matchingFilters As List(Of CompletionFilter), flags As CompletionItemFlags?, options As CompletionOptions, Optional skipSpeculation As Boolean = False) As Task
            Return MyBase.VerifyWorkerAsync(
                code, position, expectedItemOrNull, expectedDescriptionOrNull,
                sourceCodeKind, usePreviousCharAsTrigger, checkForAbsence,
                glyph, matchPriority, hasSuggestionItem, displayTextSuffix, displayTextPrefix, inlineDescription,
                isComplexTextEdit, matchingFilters, flags, options, skipSpeculation)
        End Function

        Private Protected Overrides Async Function VerifyWorkerAsync(
                code As String, position As Integer,
                expectedItemOrNull As String, expectedDescriptionOrNull As String,
                sourceCodeKind As SourceCodeKind, usePreviousCharAsTrigger As Boolean,
                checkForAbsence As Boolean, glyph As Integer?, matchPriority As Integer?,
                hasSuggestionItem As Boolean?, displayTextSuffix As String, displayTextPrefix As String, inlineDescription As String,
                isComplexTextEdit As Boolean?, matchingFilters As List(Of CompletionFilter), flags As CompletionItemFlags?, options As CompletionOptions, Optional skipSpeculation As Boolean = False) As Task
            ' Script/interactive support removed for now.
            ' TODO: Re-enable these when interactive is back in the product.
            If sourceCodeKind <> SourceCodeKind.Regular Then
                Return
            End If

            Await VerifyAtPositionAsync(
                code, position, usePreviousCharAsTrigger, expectedItemOrNull, expectedDescriptionOrNull, sourceCodeKind,
                checkForAbsence, glyph, matchPriority, hasSuggestionItem, displayTextSuffix, displayTextPrefix, inlineDescription,
                isComplexTextEdit, matchingFilters, flags, options, skipSpeculation)

            Await VerifyAtEndOfFileAsync(
                code, position, usePreviousCharAsTrigger, expectedItemOrNull, expectedDescriptionOrNull, sourceCodeKind,
                checkForAbsence, glyph, matchPriority, hasSuggestionItem, displayTextSuffix, displayTextPrefix, inlineDescription,
                isComplexTextEdit, matchingFilters, flags, options)

            ' Items cannot be partially written if we're checking for their absence,
            ' or if we're verifying that the list will show up (without specifying an actual item)
            If Not checkForAbsence AndAlso expectedItemOrNull <> Nothing Then
                Await VerifyAtPosition_ItemPartiallyWrittenAsync(
                    code, position, usePreviousCharAsTrigger, expectedItemOrNull, expectedDescriptionOrNull,
                    sourceCodeKind, checkForAbsence, glyph, matchPriority, hasSuggestionItem,
                    displayTextSuffix, displayTextPrefix, inlineDescription, isComplexTextEdit, matchingFilters, flags:=Nothing, options, skipSpeculation:=skipSpeculation)

                Await VerifyAtEndOfFile_ItemPartiallyWrittenAsync(
                    code, position, usePreviousCharAsTrigger, expectedItemOrNull, expectedDescriptionOrNull,
                    sourceCodeKind, checkForAbsence, glyph, matchPriority, hasSuggestionItem,
                    displayTextSuffix, displayTextPrefix, inlineDescription, isComplexTextEdit, matchingFilters, flags:=Nothing, options)
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

        Protected Shared Function AddInsideMethod(text As String) As String
            Return "Class C" & vbCrLf &
                   "    Function F()" & vbCrLf &
                   "        " & text & vbCrLf &
                   "    End Function" & vbCrLf &
                   "End Class"
        End Function

        Protected Shared Function CreateContent(ParamArray contents As String()) As String
            Return String.Join(vbCrLf, contents)
        End Function

        Protected Shared Function AddImportsStatement(importsStatement As String, text As String) As String
            Return importsStatement & vbCrLf & vbCrLf & text
        End Function

        Protected Async Function VerifySendEnterThroughToEditorAsync(
                initialMarkup As String, textTypedSoFar As String, expected As Boolean, Optional sourceCodeKind As SourceCodeKind = SourceCodeKind.Regular) As Task
            Using workspace = TestWorkspace.CreateVisualBasic(initialMarkup, composition:=GetComposition())
                Dim hostDocument = workspace.DocumentWithCursor
                workspace.OnDocumentSourceCodeKindChanged(hostDocument.Id, sourceCodeKind)
                Dim documentId = workspace.GetDocumentId(hostDocument)
                Dim document = workspace.CurrentSolution.GetDocument(documentId)
                Dim position = hostDocument.CursorPosition.Value

                Dim service = GetCompletionService(document.Project)
                Dim completionList = Await GetCompletionListAsync(service, document, position, RoslynCompletion.CompletionTrigger.Invoke)
                Dim item = completionList.ItemsList.First(Function(i) i.DisplayText.StartsWith(textTypedSoFar))

                Assert.Equal(expected, CommitManager.SendEnterThroughToEditor(service.GetRules(CompletionOptions.Default), item, textTypedSoFar))
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
