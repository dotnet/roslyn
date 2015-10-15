' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.Completion
Imports Microsoft.CodeAnalysis.Completion.Providers
Imports Microsoft.CodeAnalysis.LanguageServices
Imports Microsoft.CodeAnalysis.Options
Imports Microsoft.CodeAnalysis.Shared.Extensions.ContextQuery
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Extensions.ContextQuery
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols

Namespace Microsoft.CodeAnalysis.VisualBasic.Completion.Providers
    Friend Module CompletionUtilities

        Private ReadOnly s_defaultTriggerChars As Char() = {"."c, "["c, "#"c, " "c, "="c, "<"c, "{"c}

        Public Function GetTextChangeSpan(text As SourceText, position As Integer) As TextSpan
            Return CommonCompletionUtilities.GetTextChangeSpan(
                text, position,
                AddressOf IsTextChangeSpanStartCharacter,
                AddressOf IsTextChangeSpanEndCharacter)
        End Function

        Private Function IsWordStartCharacter(ch As Char) As Boolean
            Return SyntaxFacts.IsIdentifierStartCharacter(ch)
        End Function

        Private Function IsWordCharacter(ch As Char) As Boolean
            Return SyntaxFacts.IsIdentifierStartCharacter(ch) OrElse SyntaxFacts.IsIdentifierPartCharacter(ch)
        End Function

        Private Function IsTextChangeSpanStartCharacter(ch As Char) As Boolean
            Return ch = "#"c OrElse ch = "["c OrElse IsWordCharacter(ch)
        End Function

        Private Function IsTextChangeSpanEndCharacter(ch As Char) As Boolean
            Return ch = "]"c OrElse IsWordCharacter(ch)
        End Function

        Public Function IsDefaultTriggerCharacter(text As SourceText, characterPosition As Integer, options As OptionSet) As Boolean
            Dim ch = text(characterPosition)
            If s_defaultTriggerChars.Contains(ch) Then
                Return True
            End If

            Return IsStartingNewWord(text, characterPosition, options)
        End Function

        Public Function IsDefaultTriggerCharacterOrParen(text As SourceText, characterPosition As Integer, options As OptionSet) As Boolean
            Dim ch = text(characterPosition)

            Return _
                ch = "("c OrElse
                s_defaultTriggerChars.Contains(ch) OrElse
                IsStartingNewWord(text, characterPosition, options)
        End Function

        Public Function IsTriggerAfterSpaceOrStartOfWordCharacter(text As SourceText, characterPosition As Integer, options As OptionSet) As Boolean
            ' Bring up on space or at the start of a word.
            Dim ch = text(characterPosition)

            Return ch = " "c OrElse IsStartingNewWord(text, characterPosition, options)
        End Function

        Private Function IsStartingNewWord(text As SourceText, characterPosition As Integer, options As OptionSet) As Boolean
            If Not options.GetOption(CompletionOptions.TriggerOnTypingLetters, LanguageNames.VisualBasic) Then
                Return False
            End If

            Return CommonCompletionUtilities.IsStartingNewWord(
                text, characterPosition, AddressOf IsWordStartCharacter, AddressOf IsWordCharacter)
        End Function

        Public Function GetDisplayAndInsertionText(
            symbol As ISymbol,
            isAttributeNameContext As Boolean, isAfterDot As Boolean, isWithinAsyncMethod As Boolean,
            syntaxFacts As ISyntaxFactsService
        ) As ValueTuple(Of String, String)

            Dim name As String = Nothing
            If Not CommonCompletionUtilities.TryRemoveAttributeSuffix(symbol, isAttributeNameContext, syntaxFacts, name) Then
                name = symbol.Name
            End If

            Dim insertionText = GetInsertionText(name, symbol, isAfterDot, isWithinAsyncMethod)
            Dim displayText = GetDisplayText(name, symbol)

            If symbol.GetArity() > 0 Then
                Const UnicodeEllipsis = ChrW(&H2026)
                displayText += " " & UnicodeEllipsis & ")"
            End If

            Return ValueTuple.Create(displayText, insertionText)
        End Function

        Public Function GetDisplayText(name As String, symbol As ISymbol) As String
            If symbol.IsConstructor() Then
                name = "New"
            ElseIf symbol.GetArity() > 0 Then
                name += "(Of"
            End If

            Return name
        End Function

        Public Function GetInsertionText(
            name As String, symbol As ISymbol,
            isAfterDot As Boolean, isWithinAsyncMethod As Boolean,
            Optional typedChar As Char? = Nothing
        ) As String

            name = name.EscapeIdentifier(afterDot:=isAfterDot, symbol:=symbol, withinAsyncMethod:=isWithinAsyncMethod)

            If symbol.IsConstructor() Then
                name = "New"
            ElseIf symbol.GetArity() > 0 Then
                name += GetOfText(symbol, typedChar.GetValueOrDefault())
            End If

            If typedChar.HasValue AndAlso typedChar = "]"c AndAlso name(0) <> "["c Then
                name = String.Format("[{0}", name)
            End If

            Return name
        End Function

        Private Function GetOfText(symbol As ISymbol, typedChar As Char) As String
            If symbol.Kind = SymbolKind.NamedType Then
                If typedChar = "("c Then
                    Return "("
                Else
                    Return "(Of"
                End If
            End If

            If typedChar = " "c Then
                Return "(Of"
            End If

            Return ""
        End Function

        Public Function GetInsertionTextAtInsertionTime(symbol As ISymbol, context As AbstractSyntaxContext, ch As Char) As String
            Dim name As String = Nothing
            If Not CommonCompletionUtilities.TryRemoveAttributeSuffix(symbol, context.IsAttributeNameContext, context.GetLanguageService(Of ISyntaxFactsService), name) Then
                name = symbol.Name
            End If

            Return GetInsertionText(name, symbol, context.IsRightOfNameSeparator, DirectCast(context, VisualBasicSyntaxContext).WithinAsyncMethod, ch)
        End Function

        Public Function GetTextChange(symbolItem As SymbolCompletionItem, Optional ch As Char? = Nothing, Optional textTypedSoFar As String = Nothing) As TextChange
            Dim insertionText As String = If(ch Is Nothing,
                                            symbolItem.InsertionText,
                                            GetInsertionTextAtInsertionTime(
                                                symbolItem.Symbols.First(),
                                                symbolItem.Context,
                                                ch.Value))
            Return New TextChange(symbolItem.FilterSpan, insertionText)
        End Function
    End Module
End Namespace
