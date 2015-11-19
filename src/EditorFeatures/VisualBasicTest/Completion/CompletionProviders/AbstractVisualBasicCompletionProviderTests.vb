' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Completion
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Completion
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces
Imports Microsoft.CodeAnalysis.Text

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Completion.CompletionProviders
    Public MustInherit Class AbstractVisualBasicCompletionProviderTests
        Inherits AbstractCompletionProviderTests(Of VisualBasicTestWorkspaceFixture)

        Public Sub New(workspaceFixture As VisualBasicTestWorkspaceFixture)
            MyBase.New(workspaceFixture)
        End Sub

        Protected Overrides Sub VerifyWorker(code As String, position As Integer, expectedItemOrNull As String, expectedDescriptionOrNull As String, sourceCodeKind As SourceCodeKind, usePreviousCharAsTrigger As Boolean, checkForAbsence As Boolean, experimental As Boolean, glyph As Integer?)
            ' Script/interactive support removed for now.
            ' TODO: Re-enable these when interactive is back in the product.
            If sourceCodeKind <> Microsoft.CodeAnalysis.SourceCodeKind.Regular Then
                Return
            End If

            VerifyAtPosition(code, position, expectedItemOrNull, expectedDescriptionOrNull, sourceCodeKind, usePreviousCharAsTrigger, checkForAbsence, glyph, experimental)
            VerifyAtEndOfFile(code, position, expectedItemOrNull, expectedDescriptionOrNull, sourceCodeKind, usePreviousCharAsTrigger, checkForAbsence, glyph, experimental)

            ' Items cannot be partially written if we're checking for their absence,
            ' or if we're verifying that the list will show up (without specifying an actual item)
            If Not checkForAbsence AndAlso expectedItemOrNull <> Nothing Then
                VerifyAtPosition_ItemPartiallyWritten(code, position, expectedItemOrNull, expectedDescriptionOrNull, sourceCodeKind, usePreviousCharAsTrigger, checkForAbsence, glyph, experimental)
                VerifyAtEndOfFile_ItemPartiallyWritten(code, position, expectedItemOrNull, expectedDescriptionOrNull, sourceCodeKind, usePreviousCharAsTrigger, checkForAbsence, glyph, experimental)
            End If
        End Sub

        Protected Overridable Sub BaseVerifyWorker(code As String, position As Integer, expectedItemOrNull As String, expectedDescriptionOrNull As String, sourceCodeKind As SourceCodeKind, usePreviousCharAsTrigger As Boolean, checkForAbsence As Boolean, glyph As Integer?, experimental As Boolean)
            MyBase.VerifyWorker(code, position, expectedItemOrNull, expectedDescriptionOrNull, sourceCodeKind, usePreviousCharAsTrigger, checkForAbsence, experimental:=experimental, glyph:=glyph)
        End Sub

        Protected Overrides Sub VerifyCustomCommitProviderWorker(codeBeforeCommit As String, position As Integer, itemToCommit As String, expectedCodeAfterCommit As String, sourceCodeKind As SourceCodeKind, Optional commitChar As Char? = Nothing)
            ' Script/interactive support removed for now.
            ' TODO: Re-enable these when interactive is back in the product.
            If sourceCodeKind <> Microsoft.CodeAnalysis.SourceCodeKind.Regular Then
                Return
            End If

            MyBase.VerifyCustomCommitProviderWorker(codeBeforeCommit, position, itemToCommit, expectedCodeAfterCommit, sourceCodeKind, commitChar)
        End Sub

        Private Sub VerifyAtPosition(code As String, position As Integer, insertText As String, expectedItemOrNull As String, expectedDescriptionOrNull As String, sourceCodeKind As SourceCodeKind, usePreviousCharAsTrigger As Boolean, checkForAbsence As Boolean, glyph As Integer?, experimental As Boolean)
            code = code.Substring(0, position) & insertText & code.Substring(position)
            position += insertText.Length

            MyBase.VerifyWorker(code, position, expectedItemOrNull, expectedDescriptionOrNull, sourceCodeKind, usePreviousCharAsTrigger, checkForAbsence, experimental:=experimental, glyph:=glyph)
        End Sub

        Protected Sub VerifyAtPosition(code As String, position As Integer, expectedItemOrNull As String, expectedDescriptionOrNull As String, sourceCodeKind As SourceCodeKind, usePreviousCharAsTrigger As Boolean, checkForAbsence As Boolean, glyph As Integer?, experimental As Boolean)
            VerifyAtPosition(code, position, String.Empty, expectedItemOrNull, expectedDescriptionOrNull, sourceCodeKind, usePreviousCharAsTrigger, checkForAbsence, glyph:=glyph, experimental:=experimental)
        End Sub

        Private Sub VerifyAtPosition_ItemPartiallyWritten(code As String, position As Integer, expectedItemOrNull As String, expectedDescriptionOrNull As String, sourceCodeKind As SourceCodeKind, usePreviousCharAsTrigger As Boolean, checkForAbsence As Boolean, glyph As Integer?, experimental As Boolean)
            VerifyAtPosition(code, position, ItemPartiallyWritten(expectedItemOrNull), expectedItemOrNull, expectedDescriptionOrNull, sourceCodeKind, usePreviousCharAsTrigger, checkForAbsence, glyph:=glyph, experimental:=experimental)
        End Sub

        Private Sub VerifyAtEndOfFile(code As String, position As Integer, insertText As String, expectedItemOrNull As String, expectedDescriptionOrNull As String, sourceCodeKind As SourceCodeKind, usePreviousCharAsTrigger As Boolean, checkForAbsence As Boolean, glyph As Integer?, experimental As Boolean)
            ' only do this if the placeholder was at the end of the text.
            If code.Length <> position Then
                Return
            End If

            code = code.Substring(startIndex:=0, length:=position) & insertText
            position += insertText.Length

            MyBase.VerifyWorker(code, position, expectedItemOrNull, expectedDescriptionOrNull, sourceCodeKind, usePreviousCharAsTrigger, checkForAbsence, experimental:=experimental, glyph:=glyph)
        End Sub

        Private Sub VerifyAtEndOfFile(code As String, position As Integer, expectedItemOrNull As String, expectedDescriptionOrNull As String, sourceCodeKind As SourceCodeKind, usePreviousCharAsTrigger As Boolean, checkForAbsence As Boolean, glyph As Integer?, experimental As Boolean)
            VerifyAtEndOfFile(code, position, String.Empty, expectedItemOrNull, expectedDescriptionOrNull, sourceCodeKind, usePreviousCharAsTrigger, checkForAbsence, glyph, experimental)
        End Sub

        Private Sub VerifyAtEndOfFile_ItemPartiallyWritten(code As String, position As Integer, expectedItemOrNull As String, expectedDescriptionOrNull As String, sourceCodeKind As SourceCodeKind, usePreviousCharAsTrigger As Boolean, checkForAbsence As Boolean, glyph As Integer?, experimental As Boolean)
            VerifyAtEndOfFile(code, position, ItemPartiallyWritten(expectedItemOrNull), expectedItemOrNull, expectedDescriptionOrNull, sourceCodeKind, usePreviousCharAsTrigger, checkForAbsence, glyph, experimental)
        End Sub

        Private Shared Function ItemPartiallyWritten(expectedItemOrNull As String) As String
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

        Protected Async Function VerifySendEnterThroughToEditorAsync(initialMarkup As String, textTypedSoFar As String, expected As Boolean) As Threading.Tasks.Task
            Using workspace = Await VisualBasicWorkspaceFactory.CreateWorkspaceFromFileAsync(initialMarkup)
                Dim hostDocument = workspace.DocumentWithCursor
                Dim documentId = workspace.GetDocumentId(hostDocument)
                Dim document = workspace.CurrentSolution.GetDocument(documentId)
                Dim position = hostDocument.CursorPosition.Value

                Dim completionList = GetCompletionList(document, position, CompletionTriggerInfo.CreateInvokeCompletionTriggerInfo())
                Dim item = completionList.Items.First(Function(i) i.DisplayText.StartsWith(textTypedSoFar))

                Dim completionService = document.Project.LanguageServices.GetService(Of ICompletionService)()
                Dim completionRules = completionService.GetCompletionRules()

                Assert.Equal(expected, completionRules.SendEnterThroughToEditor(item, textTypedSoFar, workspace.Options))
            End Using
        End Function

        Protected Async Function VerifyCommonCommitCharactersAsync(initialMarkup As String, textTypedSoFar As String) As Threading.Tasks.Task
            Dim commitCharacters = {" "c, ";"c, "("c, ")"c, "["c, "]"c, "{"c, "}"c, "."c, ","c, ":"c, "+"c, "-"c, "*"c, "/"c, "\"c, "^"c, "<"c, ">"c, "'"c, "="c}
            Await VerifyCommitCharactersAsync(initialMarkup, textTypedSoFar, commitCharacters)
        End Function

        Protected Async Function VerifyCommitCharactersAsync(initialMarkup As String, textTypedSoFar As String, ParamArray chars As Char()) As Threading.Tasks.Task
            Using workspace = Await VisualBasicWorkspaceFactory.CreateWorkspaceFromFileAsync(initialMarkup)
                Dim hostDocument = workspace.DocumentWithCursor
                Dim documentId = workspace.GetDocumentId(hostDocument)
                Dim document = workspace.CurrentSolution.GetDocument(documentId)
                Dim position = hostDocument.CursorPosition.Value

                Dim completionList = GetCompletionList(document, position, CompletionTriggerInfo.CreateInvokeCompletionTriggerInfo())
                Dim item = completionList.Items.First()

                Dim completionService = document.Project.LanguageServices.GetService(Of ICompletionService)()
                Dim completionRules = completionService.GetCompletionRules()

                For Each ch In chars
                    Assert.True(completionRules.IsCommitCharacter(item, ch, textTypedSoFar), $"Expected '{ch}' to be a commit character")
                Next

                Dim chr = "x"c
                Assert.False(completionRules.IsCommitCharacter(item, chr, textTypedSoFar), $"Expected '{chr}' NOT to be a commit character")
            End Using

        End Function

        Protected Async Function TestCommonIsTextualTriggerCharacterAsync() As Threading.Tasks.Task
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

        Protected Async Function VerifyTextualTriggerCharacterAsync(markup As String, shouldTriggerWithTriggerOnLettersEnabled As Boolean, shouldTriggerWithTriggerOnLettersDisabled As Boolean) As Threading.Tasks.Task
            Await VerifyTextualTriggerCharacterWorkerAsync(markup, expectedTriggerCharacter:=shouldTriggerWithTriggerOnLettersEnabled, triggerOnLetter:=True)
            Await VerifyTextualTriggerCharacterWorkerAsync(markup, expectedTriggerCharacter:=shouldTriggerWithTriggerOnLettersDisabled, triggerOnLetter:=False)
        End Function

        Private Async Function VerifyTextualTriggerCharacterWorkerAsync(markup As String, expectedTriggerCharacter As Boolean, triggerOnLetter As Boolean) As Threading.Tasks.Task
            Dim code As String = Nothing
            Dim position As Integer

            MarkupTestFile.GetPosition(markup, code, position)

            Using workspace = Await VisualBasicWorkspaceFactory.CreateWorkspaceFromFileAsync(code)
                Dim document = workspace.Documents.First()
                Dim text = document.TextBuffer.CurrentSnapshot.AsText()
                Dim options = workspace.Options.WithChangedOption(CompletionOptions.TriggerOnTypingLetters, LanguageNames.VisualBasic, triggerOnLetter)

                Dim isTextualTriggerCharacterResult = CompletionProvider.IsTriggerCharacter(text, position, options)

                If expectedTriggerCharacter Then
                    Dim assertText = "'" & text.ToString(New TextSpan(position, 1)) & "' expected to be textual trigger character"
                    Assert.True(isTextualTriggerCharacterResult, assertText)
                Else
                    Dim assertText = "'" & text.ToString(New TextSpan(position, 1)) & "' expected to NOT be textual trigger character"
                    Assert.False(isTextualTriggerCharacterResult, assertText)
                End If
            End Using
        End Function

        Protected Overrides Function CreateExperimentalParseOptions(parseOptions As ParseOptions) As ParseOptions
            ' There are no experimental parse options at this time.
            Return parseOptions
        End Function
    End Class
End Namespace
