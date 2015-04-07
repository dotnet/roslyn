' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading
Imports System.Windows.Threading
Imports System.Xml.Linq
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Completion
Imports Microsoft.CodeAnalysis.Completion.Providers
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Completion
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces
Imports Microsoft.CodeAnalysis.Host
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic
Imports Microsoft.CodeAnalysis.VisualBasic.Completion.Providers
Imports Microsoft.CodeAnalysis.VisualBasic.Extensions
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Microsoft.VisualStudio.Language.Intellisense
Imports Microsoft.VisualStudio.Text
Imports Moq
Imports Roslyn.Test.EditorUtilities
Imports Roslyn.Test.Utilities

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Completion.CompletionProviders
    Public MustInherit Class AbstractVisualBasicCompletionProviderTests
        Inherits AbstractCompletionProviderTests(Of VisualBasicTestWorkspaceFixture)

        Protected Overrides Sub VerifyWorker(code As String, position As Integer, expectedItemOrNull As String, expectedDescriptionOrNull As String, sourceCodeKind As SourceCodeKind, usePreviousCharAsTrigger As Boolean, checkForAbsence As Boolean, experimental As Boolean, glyph As Integer?)
            ' Script/interactive support removed for now.
            ' TODO: Reenable these when interactive is back in the product.
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
            ' TODO: Reenable these when interactive is back in the product.
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

        Protected Sub TestCommonSendEnterThroughToEditor()
            Assert.True(CompletionProvider.SendEnterThroughToEditor(Nothing, Nothing), "Expected hardcoded 'true' from SendEnterThroughToEditor")
        End Sub

        Protected Sub TestCommonIsCommitCharacter()
            Dim commitChararacters = {" "c, ";"c, "("c, ")"c, "["c, "]"c, "{"c, "}"c, "."c, ","c, ":"c, "+"c, "-"c, "*"c, "/"c, "\"c, "^"c, "<"c, ">"c, "'"c, "="c}

            For Each ch In commitChararacters
                Assert.True(CompletionProvider.IsCommitCharacter(Nothing, ch, Nothing), "Expected '" + ch + "' to be a commit character")
            Next

            Dim chr = "x"c
            Assert.False(CompletionProvider.IsCommitCharacter(Nothing, chr, Nothing), "Expected '" + chr + "' NOT to be a commit character")
        End Sub

        Protected Sub TestCommonIsTextualTriggerCharacter()
            Dim alwaysTriggerList =
            {
                "foo$$.",
                "foo$$[",
                "foo$$#",
                "foo$$ ",
                "foo$$="
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
                "foo$$x",
                "foo$$_"
            }

            For Each markup In neverTriggerList
                VerifyTextualTriggerCharacter(markup, shouldTriggerWithTriggerOnLettersEnabled:=False, shouldTriggerWithTriggerOnLettersDisabled:=False)
            Next
        End Sub

        Protected Sub VerifyTextualTriggerCharacter(markup As String, shouldTriggerWithTriggerOnLettersEnabled As Boolean, shouldTriggerWithTriggerOnLettersDisabled As Boolean)
            VerifyTextualTriggerCharacterWorker(markup, expectedTriggerCharacter:=shouldTriggerWithTriggerOnLettersEnabled, triggerOnLetter:=True)
            VerifyTextualTriggerCharacterWorker(markup, expectedTriggerCharacter:=shouldTriggerWithTriggerOnLettersDisabled, triggerOnLetter:=False)
        End Sub

        Private Sub VerifyTextualTriggerCharacterWorker(markup As String, expectedTriggerCharacter As Boolean, triggerOnLetter As Boolean)
            Dim code As String = Nothing
            Dim position As Integer

            MarkupTestFile.GetPosition(markup, code, position)

            Using workspace = VisualBasicWorkspaceFactory.CreateWorkspaceFromFile(code)
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
        End Sub

        Protected Overrides Function CreateExperimentalParseOptions(parseOptions As ParseOptions) As ParseOptions
            ' There are no experimental parse options at this time.
            Return parseOptions
        End Function
    End Class
End Namespace
