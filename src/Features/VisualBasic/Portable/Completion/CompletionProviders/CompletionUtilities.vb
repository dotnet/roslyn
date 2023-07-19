' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports Microsoft.CodeAnalysis.Completion
Imports Microsoft.CodeAnalysis.Completion.Providers
Imports Microsoft.CodeAnalysis.Options
Imports Microsoft.CodeAnalysis.Shared.Extensions.ContextQuery
Imports Microsoft.CodeAnalysis.Text

Namespace Microsoft.CodeAnalysis.VisualBasic.Completion.Providers
    Friend Module CompletionUtilities
        Private Const UnicodeEllipsis = ChrW(&H2026)
        Private Const OfSuffix = "(Of"
        Private Const GenericSuffix = OfSuffix + " " & UnicodeEllipsis & ")"

        Friend ReadOnly CommonTriggerChars As ImmutableHashSet(Of Char) = ImmutableHashSet.Create("."c, "["c, "#"c, " "c, "="c, "<"c, "{"c)

        Friend ReadOnly CommonTriggerCharsAndParen As ImmutableHashSet(Of Char) = CommonTriggerChars.Add("("c)

        Friend ReadOnly SpaceTriggerChar As ImmutableHashSet(Of Char) = CommonTriggerChars.Add(" "c)

        Public Function GetCompletionItemSpan(text As SourceText, position As Integer) As TextSpan
            Return CommonCompletionUtilities.GetWordSpan(
                text, position,
                AddressOf IsCompletionItemStartCharacter,
                AddressOf IsCompletionItemCharacter)
        End Function

        Public Function IsWordStartCharacter(ch As Char) As Boolean
            Return SyntaxFacts.IsIdentifierStartCharacter(ch)
        End Function

        Private Function IsWordCharacter(ch As Char) As Boolean
            Return SyntaxFacts.IsIdentifierStartCharacter(ch) OrElse SyntaxFacts.IsIdentifierPartCharacter(ch)
        End Function

        Private Function IsCompletionItemStartCharacter(ch As Char) As Boolean
            Return ch = "#"c OrElse ch = "["c OrElse IsWordCharacter(ch)
        End Function

        Private Function IsCompletionItemCharacter(ch As Char) As Boolean
            Return ch = "]"c OrElse IsWordCharacter(ch)
        End Function

        Public Function IsDefaultTriggerCharacter(text As SourceText, characterPosition As Integer, options As CompletionOptions) As Boolean
            Dim ch = text(characterPosition)
            If CommonTriggerChars.Contains(ch) Then
                Return True
            End If

            Return IsStartingNewWord(text, characterPosition, options)
        End Function

        Public Function IsDefaultTriggerCharacterOrParen(text As SourceText, characterPosition As Integer, options As CompletionOptions) As Boolean
            Dim ch = text(characterPosition)

            Return _
                ch = "("c OrElse
                CommonTriggerChars.Contains(ch) OrElse
                IsStartingNewWord(text, characterPosition, options)
        End Function

        Public Function IsTriggerAfterSpaceOrStartOfWordCharacter(text As SourceText, characterPosition As Integer, options As CompletionOptions) As Boolean
            ' Bring up on space or at the start of a word.
            Dim ch = text(characterPosition)

            Return ch = " "c OrElse IsStartingNewWord(text, characterPosition, options)
        End Function

        Private Function IsStartingNewWord(text As SourceText, characterPosition As Integer, options As CompletionOptions) As Boolean
            If Not options.TriggerOnTypingLetters Then
                Return False
            End If

            Return CommonCompletionUtilities.IsStartingNewWord(
                text, characterPosition, AddressOf IsWordStartCharacter, AddressOf IsWordCharacter)
        End Function

        Public Function GetDisplayAndSuffixAndInsertionText(
            symbol As ISymbol,
            context As SyntaxContext) As (displayText As String, suffix As String, insertionText As String)

            Dim name As String = Nothing
            If Not CommonCompletionUtilities.TryRemoveAttributeSuffix(symbol, context, name) Then
                name = symbol.Name
            End If

            Dim displayText = GetDisplayText(name, symbol)
            Dim insertionText = GetInsertionText(name, symbol, context)
            Dim suffix = GetSuffix(symbol)

            Return (displayText, suffix, insertionText)
        End Function

        Private Function GetDisplayText(name As String, symbol As ISymbol) As String
            If symbol.IsConstructor() Then
                Return "New"
            Else
                Return name
            End If
        End Function

        Private Function GetSuffix(symbol As ISymbol) As String
            If symbol.IsConstructor() Then
                Return ""
            ElseIf symbol.GetArity() > 0 Then
                Return GenericSuffix
            Else
                Return ""
            End If
        End Function

        Private Function GetInsertionText(name As String, symbol As ISymbol, context As SyntaxContext) As String
            name = name.EscapeIdentifier(context.IsRightOfNameSeparator, symbol, context.IsWithinAsyncMethod)

            If symbol.IsConstructor() Then
                name = "New"
            ElseIf symbol.GetArity() > 0 Then
                name += OfSuffix
            End If

            Return name
        End Function

        Public Function GetInsertionTextAtInsertionTime(item As CompletionItem, ch As Char) As String
            Dim insertionText = SymbolCompletionItem.GetInsertionText(item)

            ' If this item was generic, customize what we insert depending on if the user typed
            ' open paren or not.
            If ch = "("c AndAlso item.DisplayTextSuffix = GenericSuffix Then
                Return insertionText.Substring(0, insertionText.IndexOf("("c))
            End If

            If ch = "]"c Then
                If insertionText(0) <> "["c Then
                    ' user is committing with ].  If the item doesn't start with '['
                    ' then add that to the beginning so [ and ] properly pair up.
                    Return "[" + insertionText
                End If

                If insertionText.EndsWith("]") Then
                    ' If the user commits with "]" and the item already ends with "]"
                    ' then trim "]" off the end so we don't have ]] inserted into the
                    ' document.
                    Return insertionText.Substring(0, insertionText.Length - 1)
                End If
            End If

            Return insertionText
        End Function
    End Module
End Namespace
