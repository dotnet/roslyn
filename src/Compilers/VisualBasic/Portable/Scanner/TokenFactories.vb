' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

'-----------------------------------------------------------------------------
' Contains the definition of the Scanner, which produces tokens from text 
'-----------------------------------------------------------------------------
Option Compare Binary

Imports System.Runtime.CompilerServices
Imports Microsoft.CodeAnalysis.Syntax.InternalSyntax
Imports Microsoft.CodeAnalysis.VisualBasic.SyntaxFacts
Imports CoreInternalSyntax = Microsoft.CodeAnalysis.Syntax.InternalSyntax

Namespace Microsoft.CodeAnalysis.VisualBasic.Syntax.InternalSyntax
    Partial Friend Class Scanner

#Region "Caches"

#Region "Trivia"
        Private Structure TriviaKey
            Public ReadOnly spelling As String
            Public ReadOnly kind As SyntaxKind

            Public Sub New(spelling As String, kind As SyntaxKind)
                Me.spelling = spelling
                Me.kind = kind
            End Sub
        End Structure

        Private Shared ReadOnly s_triviaKeyHasher As Func(Of TriviaKey, Integer) =
            Function(key) RuntimeHelpers.GetHashCode(key.spelling) Xor key.kind

        Private Shared ReadOnly s_triviaKeyEquality As Func(Of TriviaKey, SyntaxTrivia, Boolean) =
            Function(key, value) (key.spelling Is value.Text) AndAlso (key.kind = value.Kind)

        Private Shared ReadOnly s_singleSpaceWhitespaceTrivia As SyntaxTrivia = SyntaxFactory.WhitespaceTrivia(" ")
        Private Shared ReadOnly s_fourSpacesWhitespaceTrivia As SyntaxTrivia = SyntaxFactory.WhitespaceTrivia("    ")
        Private Shared ReadOnly s_eightSpacesWhitespaceTrivia As SyntaxTrivia = SyntaxFactory.WhitespaceTrivia("        ")
        Private Shared ReadOnly s_twelveSpacesWhitespaceTrivia As SyntaxTrivia = SyntaxFactory.WhitespaceTrivia("            ")
        Private Shared ReadOnly s_sixteenSpacesWhitespaceTrivia As SyntaxTrivia = SyntaxFactory.WhitespaceTrivia("                ")

        Private Shared Function CreateWsTable() As CachingFactory(Of TriviaKey, SyntaxTrivia)
            Dim table = New CachingFactory(Of TriviaKey, SyntaxTrivia)(TABLE_LIMIT, Nothing, s_triviaKeyHasher, s_triviaKeyEquality)

            ' Prepopulate the table with some common values
            table.Add(New TriviaKey(" ", SyntaxKind.WhitespaceTrivia), s_singleSpaceWhitespaceTrivia)
            table.Add(New TriviaKey("    ", SyntaxKind.WhitespaceTrivia), s_fourSpacesWhitespaceTrivia)
            table.Add(New TriviaKey("        ", SyntaxKind.WhitespaceTrivia), s_eightSpacesWhitespaceTrivia)
            table.Add(New TriviaKey("            ", SyntaxKind.WhitespaceTrivia), s_twelveSpacesWhitespaceTrivia)
            table.Add(New TriviaKey("                ", SyntaxKind.WhitespaceTrivia), s_sixteenSpacesWhitespaceTrivia)

            Return table
        End Function

#End Region

#Region "WhiteSpaceList"

        Private Shared ReadOnly s_wsListKeyHasher As Func(Of SyntaxListBuilder, Integer) =
            Function(builder)
                Dim code = 0
                For i = 0 To builder.Count - 1
                    Dim value = builder(i)
                    ' shift because there could be the same trivia nodes in the list
                    code = (code << 1) Xor RuntimeHelpers.GetHashCode(value)
                Next
                Return code
            End Function

        Private Shared ReadOnly s_wsListKeyEquality As Func(Of SyntaxListBuilder, CoreInternalSyntax.SyntaxList(Of VisualBasicSyntaxNode), Boolean) =
            Function(builder, list)
                If builder.Count <> list.Count Then
                    Return False
                End If

                For i = 0 To builder.Count - 1
                    If builder(i) IsNot list.ItemUntyped(i) Then
                        Return False
                    End If
                Next
                Return True
            End Function

        Private Shared ReadOnly s_wsListFactory As Func(Of SyntaxListBuilder, CoreInternalSyntax.SyntaxList(Of VisualBasicSyntaxNode)) =
            Function(builder)
                Return builder.ToList(Of VisualBasicSyntaxNode)()
            End Function

#End Region

#Region "Tokens"

        Private Structure TokenParts
            Friend ReadOnly spelling As String
            Friend ReadOnly pTrivia As GreenNode
            Friend ReadOnly fTrivia As GreenNode

            Friend Sub New(pTrivia As CoreInternalSyntax.SyntaxList(Of VisualBasicSyntaxNode), fTrivia As CoreInternalSyntax.SyntaxList(Of VisualBasicSyntaxNode), spelling As String)
                Me.spelling = spelling
                Me.pTrivia = pTrivia.Node
                Me.fTrivia = fTrivia.Node
            End Sub
        End Structure

        Private Shared ReadOnly s_tokenKeyHasher As Func(Of TokenParts, Integer) =
            Function(key)
                Dim code = RuntimeHelpers.GetHashCode(key.spelling)
                Dim trivia = key.pTrivia
                If trivia IsNot Nothing Then
                    code = code Xor (RuntimeHelpers.GetHashCode(trivia) << 1)
                End If

                trivia = key.fTrivia
                If trivia IsNot Nothing Then
                    code = code Xor RuntimeHelpers.GetHashCode(trivia)
                End If

                Return code
            End Function

        Private Shared ReadOnly s_tokenKeyEquality As Func(Of TokenParts, SyntaxToken, Boolean) =
            Function(x, y)
                If y Is Nothing OrElse
                    x.spelling IsNot y.Text OrElse
                    x.fTrivia IsNot y.GetTrailingTrivia OrElse
                    x.pTrivia IsNot y.GetLeadingTrivia Then

                    Return False
                End If

                Return True
            End Function

#End Region

        Private Shared Function CanCache(trivia As SyntaxListBuilder) As Boolean
            For i = 0 To trivia.Count - 1
                Dim t = trivia(i)
                Select Case t.RawKind
                    Case SyntaxKind.WhitespaceTrivia,
                        SyntaxKind.EndOfLineTrivia,
                        SyntaxKind.LineContinuationTrivia,
                        SyntaxKind.DocumentationCommentExteriorTrivia

                        'do nothing
                    Case Else
                        Return False
                End Select
            Next
            Return True
        End Function

#End Region

#Region "Trivia"
        Friend Function MakeWhiteSpaceTrivia(text As String) As SyntaxTrivia
            Debug.Assert(text.Length > 0)
            Debug.Assert(text.All(AddressOf IsWhitespace))

            Dim ws As SyntaxTrivia = Nothing
            Dim key = New TriviaKey(text, SyntaxKind.WhitespaceTrivia)
            If Not _wsTable.TryGetValue(key, ws) Then
                ws = SyntaxFactory.WhitespaceTrivia(text)
                _wsTable.Add(key, ws)
            End If
            Return ws
        End Function

        Friend Function MakeEndOfLineTrivia(text As String) As SyntaxTrivia
            Dim ws As SyntaxTrivia = Nothing
            Dim key = New TriviaKey(text, SyntaxKind.EndOfLineTrivia)
            If Not _wsTable.TryGetValue(key, ws) Then
                ws = SyntaxFactory.EndOfLineTrivia(text)
                _wsTable.Add(key, ws)
            End If
            Return ws
        End Function

        Friend Function MakeColonTrivia(text As String) As SyntaxTrivia
            Debug.Assert(text.Length = 1)
            Debug.Assert(IsColon(text(0)))

            Dim ct As SyntaxTrivia = Nothing
            Dim key = New TriviaKey(text, SyntaxKind.ColonTrivia)
            If Not _wsTable.TryGetValue(key, ct) Then
                ct = SyntaxFactory.ColonTrivia(text)
                _wsTable.Add(key, ct)
            End If
            Return ct
        End Function

        Private Shared ReadOnly s_crLfTrivia As SyntaxTrivia = SyntaxFactory.EndOfLineTrivia(vbCrLf)
        Friend Function MakeEndOfLineTriviaCRLF() As SyntaxTrivia
            AdvanceChar(2)
            Return s_crLfTrivia
        End Function

        Friend Function MakeLineContinuationTrivia(text As String) As SyntaxTrivia
            Debug.Assert(text.Length = 1)
            Debug.Assert(IsUnderscore(text(0)))

            Dim ws As SyntaxTrivia = Nothing
            Dim key = New TriviaKey(text, SyntaxKind.LineContinuationTrivia)
            If Not _wsTable.TryGetValue(key, ws) Then
                ws = SyntaxFactory.LineContinuationTrivia(text)
                _wsTable.Add(key, ws)
            End If
            Return ws
        End Function

        Friend Function MakeDocumentationCommentExteriorTrivia(text As String) As SyntaxTrivia
            Dim ws As SyntaxTrivia = Nothing
            Dim key = New TriviaKey(text, SyntaxKind.DocumentationCommentExteriorTrivia)
            If Not _wsTable.TryGetValue(key, ws) Then
                ws = SyntaxFactory.DocumentationCommentExteriorTrivia(text)
                _wsTable.Add(key, ws)
            End If
            Return ws
        End Function

        Friend Shared Function MakeCommentTrivia(text As String) As SyntaxTrivia
            Return SyntaxFactory.SyntaxTrivia(SyntaxKind.CommentTrivia, text)
        End Function

        Friend Function MakeTriviaArray(builder As SyntaxListBuilder) As CoreInternalSyntax.SyntaxList(Of VisualBasicSyntaxNode)
            If builder.Count = 0 Then
                Return Nothing
            End If
            Dim foundTrivia As CoreInternalSyntax.SyntaxList(Of VisualBasicSyntaxNode) = Nothing
            Dim useCache = CanCache(builder)

            If useCache Then
                Return _wslTable.GetOrMakeValue(builder)
            Else
                Return builder.ToList
            End If
        End Function

#End Region

#Region "Identifiers"
        Private Function MakeIdentifier(spelling As String,
                                       contextualKind As SyntaxKind,
                                       isBracketed As Boolean,
                                       BaseSpelling As String,
                                       TypeCharacter As TypeCharacter,
                                       leadingTrivia As CoreInternalSyntax.SyntaxList(Of VisualBasicSyntaxNode)) As IdentifierTokenSyntax

            Dim followingTrivia = ScanSingleLineTrivia()

            Return MakeIdentifier(spelling,
                               contextualKind,
                               isBracketed,
                               BaseSpelling,
                               TypeCharacter,
                               leadingTrivia,
                               followingTrivia)

        End Function

        Friend Function MakeIdentifier(keyword As KeywordSyntax) As IdentifierTokenSyntax
            Return MakeIdentifier(keyword.Text,
                                  keyword.Kind,
                                  False,
                                  keyword.Text,
                                  TypeCharacter.None,
                                  keyword.GetLeadingTrivia,
                                  keyword.GetTrailingTrivia)
        End Function

        Private Function MakeIdentifier(spelling As String,
                               contextualKind As SyntaxKind,
                               isBracketed As Boolean,
                               BaseSpelling As String,
                               TypeCharacter As TypeCharacter,
                               precedingTrivia As CoreInternalSyntax.SyntaxList(Of GreenNode),
                               followingTrivia As CoreInternalSyntax.SyntaxList(Of GreenNode)) As IdentifierTokenSyntax

            Dim tp As New TokenParts(precedingTrivia, followingTrivia, spelling)

            Dim id As IdentifierTokenSyntax = Nothing
            If _idTable.TryGetValue(tp, id) Then
                Return id
            End If

            If contextualKind <> SyntaxKind.IdentifierToken OrElse
                isBracketed = True OrElse
                TypeCharacter <> TypeCharacter.None Then

                id = SyntaxFactory.Identifier(spelling, contextualKind, isBracketed, BaseSpelling, TypeCharacter, precedingTrivia.Node, followingTrivia.Node)
            Else
                id = SyntaxFactory.Identifier(spelling, precedingTrivia.Node, followingTrivia.Node)
            End If

            _idTable.Add(tp, id)

            Return id
        End Function
#End Region

#Region "Keywords"

        Private Function MakeKeyword(tokenType As SyntaxKind,
                                     spelling As String,
                                     precedingTrivia As CoreInternalSyntax.SyntaxList(Of VisualBasicSyntaxNode)) As KeywordSyntax

            Dim followingTrivia = ScanSingleLineTrivia()

            Return MakeKeyword(tokenType,
                               spelling,
                               precedingTrivia,
                               followingTrivia)
        End Function

        Friend Function MakeKeyword(identifier As IdentifierTokenSyntax) As KeywordSyntax
            Debug.Assert(identifier.PossibleKeywordKind <> SyntaxKind.IdentifierToken AndAlso
                         Not identifier.IsBracketed AndAlso
                         (identifier.TypeCharacter = TypeCharacter.None OrElse identifier.PossibleKeywordKind = SyntaxKind.MidKeyword))

            Return MakeKeyword(identifier.PossibleKeywordKind,
                               identifier.Text,
                               identifier.GetLeadingTrivia,
                               identifier.GetTrailingTrivia)
        End Function

        Friend Function MakeKeyword(xmlName As XmlNameTokenSyntax) As KeywordSyntax
            Debug.Assert(xmlName.PossibleKeywordKind <> SyntaxKind.XmlNameToken)

            Return MakeKeyword(xmlName.PossibleKeywordKind,
                               xmlName.Text,
                               xmlName.GetLeadingTrivia,
                               xmlName.GetTrailingTrivia)
        End Function

        Private Function MakeKeyword(tokenType As SyntaxKind,
                              spelling As String,
                              precedingTrivia As CoreInternalSyntax.SyntaxList(Of GreenNode),
                              followingTrivia As CoreInternalSyntax.SyntaxList(Of GreenNode)) As KeywordSyntax

            Dim tp As New TokenParts(precedingTrivia, followingTrivia, spelling)

            Dim kw As KeywordSyntax = Nothing
            If _kwTable.TryGetValue(tp, kw) Then
                Return kw
            End If

            kw = New KeywordSyntax(tokenType, spelling, precedingTrivia.Node, followingTrivia.Node)
            _kwTable.Add(tp, kw)
            Return kw
        End Function
#End Region

#Region "Punctuation"
        Friend Function MakePunctuationToken(precedingTrivia As CoreInternalSyntax.SyntaxList(Of VisualBasicSyntaxNode),
                                              spelling As String,
                                              kind As SyntaxKind) As PunctuationSyntax

            Dim followingTrivia = ScanSingleLineTrivia()
            Return MakePunctuationToken(kind, spelling, precedingTrivia, followingTrivia)
        End Function

        Private Function MakePunctuationToken(precedingTrivia As CoreInternalSyntax.SyntaxList(Of VisualBasicSyntaxNode),
                                              length As Integer,
                                              kind As SyntaxKind) As PunctuationSyntax

            Dim spelling = GetText(length)
            Dim followingTrivia = ScanSingleLineTrivia()
            Return MakePunctuationToken(kind, spelling, precedingTrivia, followingTrivia)
        End Function

        Friend Function MakePunctuationToken(kind As SyntaxKind,
                                      spelling As String,
                                      precedingTrivia As CoreInternalSyntax.SyntaxList(Of GreenNode),
                                      followingTrivia As CoreInternalSyntax.SyntaxList(Of GreenNode)
                      ) As PunctuationSyntax

            Dim tp As New TokenParts(precedingTrivia, followingTrivia, spelling)

            Dim p As PunctuationSyntax = Nothing
            If _punctTable.TryGetValue(tp, p) Then
                Return p
            End If

            p = New PunctuationSyntax(kind, spelling, precedingTrivia.Node, followingTrivia.Node)
            _punctTable.Add(tp, p)
            Return p
        End Function

        Private Function MakeOpenParenToken(precedingTrivia As CoreInternalSyntax.SyntaxList(Of VisualBasicSyntaxNode), charIsFullWidth As Boolean) As PunctuationSyntax
            Dim spelling = If(charIsFullWidth, FULLWIDTH_LEFT_PARENTHESIS_STRING, "(")
            AdvanceChar()

            Return MakePunctuationToken(precedingTrivia, spelling, SyntaxKind.OpenParenToken)
        End Function

        Private Function MakeCloseParenToken(precedingTrivia As CoreInternalSyntax.SyntaxList(Of VisualBasicSyntaxNode), charIsFullWidth As Boolean) As PunctuationSyntax
            Dim spelling = If(charIsFullWidth, FULLWIDTH_RIGHT_PARENTHESIS_STRING, ")")
            AdvanceChar()

            Return MakePunctuationToken(precedingTrivia, spelling, SyntaxKind.CloseParenToken)
        End Function

        Private Function MakeDotToken(precedingTrivia As CoreInternalSyntax.SyntaxList(Of VisualBasicSyntaxNode), charIsFullWidth As Boolean) As PunctuationSyntax
            Dim spelling = If(charIsFullWidth, FULLWIDTH_FULL_STOP_STRING, ".")
            AdvanceChar()

            Return MakePunctuationToken(precedingTrivia, spelling, SyntaxKind.DotToken)
        End Function

        Private Function MakeCommaToken(precedingTrivia As CoreInternalSyntax.SyntaxList(Of VisualBasicSyntaxNode), charIsFullWidth As Boolean) As PunctuationSyntax
            Dim spelling = If(charIsFullWidth, FULLWIDTH_COMMA_STRING, ",")
            AdvanceChar()

            Return MakePunctuationToken(precedingTrivia, spelling, SyntaxKind.CommaToken)
        End Function

        Private Function MakeEqualsToken(precedingTrivia As CoreInternalSyntax.SyntaxList(Of VisualBasicSyntaxNode), charIsFullWidth As Boolean) As PunctuationSyntax
            Dim spelling = If(charIsFullWidth, FULLWIDTH_EQUALS_SIGN_STRING, "=")
            AdvanceChar()

            Return MakePunctuationToken(precedingTrivia, spelling, SyntaxKind.EqualsToken)
        End Function

        Private Function MakeHashToken(precedingTrivia As CoreInternalSyntax.SyntaxList(Of VisualBasicSyntaxNode), charIsFullWidth As Boolean) As PunctuationSyntax
            Dim spelling = If(charIsFullWidth, FULLWIDTH_NUMBER_SIGN_STRING, "#")
            AdvanceChar()

            Return MakePunctuationToken(precedingTrivia, spelling, SyntaxKind.HashToken)
        End Function

        Private Function MakeAmpersandToken(precedingTrivia As CoreInternalSyntax.SyntaxList(Of VisualBasicSyntaxNode), charIsFullWidth As Boolean) As PunctuationSyntax
            Dim spelling = If(charIsFullWidth, FULLWIDTH_AMPERSAND_STRING, "&")
            AdvanceChar()

            Return MakePunctuationToken(precedingTrivia, spelling, SyntaxKind.AmpersandToken)
        End Function

        Private Function MakeOpenBraceToken(precedingTrivia As CoreInternalSyntax.SyntaxList(Of VisualBasicSyntaxNode), charIsFullWidth As Boolean) As PunctuationSyntax
            Dim spelling = If(charIsFullWidth, FULLWIDTH_LEFT_CURLY_BRACKET_STRING, "{")
            AdvanceChar()

            Return MakePunctuationToken(precedingTrivia, spelling, SyntaxKind.OpenBraceToken)
        End Function

        Private Function MakeCloseBraceToken(precedingTrivia As CoreInternalSyntax.SyntaxList(Of VisualBasicSyntaxNode), charIsFullWidth As Boolean) As PunctuationSyntax
            Dim spelling = If(charIsFullWidth, FULLWIDTH_RIGHT_CURLY_BRACKET_STRING, "}")
            AdvanceChar()

            Return MakePunctuationToken(precedingTrivia, spelling, SyntaxKind.CloseBraceToken)
        End Function

        Private Function MakeColonToken(precedingTrivia As CoreInternalSyntax.SyntaxList(Of VisualBasicSyntaxNode), charIsFullWidth As Boolean) As PunctuationSyntax
            Debug.Assert(Peek() = If(charIsFullWidth, FULLWIDTH_COLON, ":"c))
            Debug.Assert(Not precedingTrivia.Any())

            Dim width = _endOfTerminatorTrivia - _lineBufferOffset
            Debug.Assert(width = 1)

            AdvanceChar(width)

            ' Colon does not consume trailing trivia
            Return SyntaxFactory.ColonToken
        End Function

        Private Function MakeEmptyToken(precedingTrivia As CoreInternalSyntax.SyntaxList(Of VisualBasicSyntaxNode)) As PunctuationSyntax
            Return MakePunctuationToken(precedingTrivia, "", SyntaxKind.EmptyToken)
        End Function

        Private Function MakePlusToken(precedingTrivia As CoreInternalSyntax.SyntaxList(Of VisualBasicSyntaxNode), charIsFullWidth As Boolean) As PunctuationSyntax
            Dim spelling = If(charIsFullWidth, FULLWIDTH_PLUS_SIGN_STRING, "+")
            AdvanceChar()

            Return MakePunctuationToken(precedingTrivia, spelling, SyntaxKind.PlusToken)
        End Function

        Private Function MakeMinusToken(precedingTrivia As CoreInternalSyntax.SyntaxList(Of VisualBasicSyntaxNode), charIsFullWidth As Boolean) As PunctuationSyntax
            Dim spelling = If(charIsFullWidth, FULLWIDTH_HYPHEN_MINUS_STRING, "-")
            AdvanceChar()

            Return MakePunctuationToken(precedingTrivia, spelling, SyntaxKind.MinusToken)
        End Function

        Private Function MakeAsteriskToken(precedingTrivia As CoreInternalSyntax.SyntaxList(Of VisualBasicSyntaxNode), charIsFullWidth As Boolean) As PunctuationSyntax
            Dim spelling = If(charIsFullWidth, FULLWIDTH_ASTERISK_STRING, "*")
            AdvanceChar()

            Return MakePunctuationToken(precedingTrivia, spelling, SyntaxKind.AsteriskToken)
        End Function

        Private Function MakeSlashToken(precedingTrivia As CoreInternalSyntax.SyntaxList(Of VisualBasicSyntaxNode), charIsFullWidth As Boolean) As PunctuationSyntax
            Dim spelling = If(charIsFullWidth, FULLWIDTH_SOLIDUS_STRING, "/")
            AdvanceChar()

            Return MakePunctuationToken(precedingTrivia, spelling, SyntaxKind.SlashToken)
        End Function

        Private Function MakeBackslashToken(precedingTrivia As CoreInternalSyntax.SyntaxList(Of VisualBasicSyntaxNode), charIsFullWidth As Boolean) As PunctuationSyntax
            Dim spelling = If(charIsFullWidth, FULLWIDTH_REVERSE_SOLIDUS_STRING, "\")
            AdvanceChar()

            Return MakePunctuationToken(precedingTrivia, spelling, SyntaxKind.BackslashToken)
        End Function

        Private Function MakeCaretToken(precedingTrivia As CoreInternalSyntax.SyntaxList(Of VisualBasicSyntaxNode), charIsFullWidth As Boolean) As PunctuationSyntax
            Dim spelling = If(charIsFullWidth, FULLWIDTH_CIRCUMFLEX_ACCENT_STRING, "^")
            AdvanceChar()

            Return MakePunctuationToken(precedingTrivia, spelling, SyntaxKind.CaretToken)
        End Function

        Private Function MakeExclamationToken(precedingTrivia As CoreInternalSyntax.SyntaxList(Of VisualBasicSyntaxNode), charIsFullWidth As Boolean) As PunctuationSyntax
            Dim spelling = If(charIsFullWidth, FULLWIDTH_EXCLAMATION_MARK_STRING, "!")
            AdvanceChar()

            Return MakePunctuationToken(precedingTrivia, spelling, SyntaxKind.ExclamationToken)
        End Function

        Private Function MakeQuestionToken(precedingTrivia As CoreInternalSyntax.SyntaxList(Of VisualBasicSyntaxNode), charIsFullWidth As Boolean) As PunctuationSyntax
            Dim spelling = If(charIsFullWidth, FULLWIDTH_QUESTION_MARK_STRING, "?")
            AdvanceChar()

            Return MakePunctuationToken(precedingTrivia, spelling, SyntaxKind.QuestionToken)
        End Function

        Private Function MakeGreaterThanToken(precedingTrivia As CoreInternalSyntax.SyntaxList(Of VisualBasicSyntaxNode), charIsFullWidth As Boolean) As PunctuationSyntax
            Dim spelling = If(charIsFullWidth, FULLWIDTH_GREATER_THAN_SIGN_STRING, ">")
            AdvanceChar()

            Return MakePunctuationToken(precedingTrivia, spelling, SyntaxKind.GreaterThanToken)
        End Function

        Private Function MakeLessThanToken(precedingTrivia As CoreInternalSyntax.SyntaxList(Of VisualBasicSyntaxNode), charIsFullWidth As Boolean) As PunctuationSyntax
            Dim spelling = If(charIsFullWidth, FULLWIDTH_LESS_THAN_SIGN_STRING, "<")
            AdvanceChar()

            Return MakePunctuationToken(precedingTrivia, spelling, SyntaxKind.LessThanToken)
        End Function

        ' ==== TOKENS WITH NOT FIXED SPELLING

        Private Function MakeStatementTerminatorToken(precedingTrivia As CoreInternalSyntax.SyntaxList(Of VisualBasicSyntaxNode), width As Integer) As PunctuationSyntax
            Debug.Assert(_endOfTerminatorTrivia = _lineBufferOffset + width)
            Debug.Assert(width = 1 OrElse width = 2)
            Debug.Assert(Not precedingTrivia.Any())

            AdvanceChar(width)

            ' Statement terminator does not consume trailing trivia
            Return SyntaxFactory.StatementTerminatorToken
        End Function

        Private Function MakeAmpersandEqualsToken(precedingTrivia As CoreInternalSyntax.SyntaxList(Of VisualBasicSyntaxNode), length As Integer) As PunctuationSyntax
            Return MakePunctuationToken(precedingTrivia, length, SyntaxKind.AmpersandEqualsToken)
        End Function

        Private Function MakeColonEqualsToken(precedingTrivia As CoreInternalSyntax.SyntaxList(Of VisualBasicSyntaxNode), length As Integer) As PunctuationSyntax
            Return MakePunctuationToken(precedingTrivia, length, SyntaxKind.ColonEqualsToken)
        End Function

        Private Function MakePlusEqualsToken(precedingTrivia As CoreInternalSyntax.SyntaxList(Of VisualBasicSyntaxNode), length As Integer) As PunctuationSyntax
            Return MakePunctuationToken(precedingTrivia, length, SyntaxKind.PlusEqualsToken)
        End Function

        Private Function MakeMinusEqualsToken(precedingTrivia As CoreInternalSyntax.SyntaxList(Of VisualBasicSyntaxNode), length As Integer) As PunctuationSyntax
            Return MakePunctuationToken(precedingTrivia, length, SyntaxKind.MinusEqualsToken)
        End Function

        Private Function MakeAsteriskEqualsToken(precedingTrivia As CoreInternalSyntax.SyntaxList(Of VisualBasicSyntaxNode), length As Integer) As PunctuationSyntax
            Return MakePunctuationToken(precedingTrivia, length, SyntaxKind.AsteriskEqualsToken)
        End Function

        Private Function MakeSlashEqualsToken(precedingTrivia As CoreInternalSyntax.SyntaxList(Of VisualBasicSyntaxNode), length As Integer) As PunctuationSyntax
            Return MakePunctuationToken(precedingTrivia, length, SyntaxKind.SlashEqualsToken)
        End Function

        Private Function MakeBackSlashEqualsToken(precedingTrivia As CoreInternalSyntax.SyntaxList(Of VisualBasicSyntaxNode), length As Integer) As PunctuationSyntax
            Return MakePunctuationToken(precedingTrivia, length, SyntaxKind.BackslashEqualsToken)
        End Function

        Private Function MakeCaretEqualsToken(precedingTrivia As CoreInternalSyntax.SyntaxList(Of VisualBasicSyntaxNode), length As Integer) As PunctuationSyntax
            Return MakePunctuationToken(precedingTrivia, length, SyntaxKind.CaretEqualsToken)
        End Function

        Private Function MakeGreaterThanEqualsToken(precedingTrivia As CoreInternalSyntax.SyntaxList(Of VisualBasicSyntaxNode), length As Integer) As PunctuationSyntax
            Return MakePunctuationToken(precedingTrivia, length, SyntaxKind.GreaterThanEqualsToken)
        End Function

        Private Function MakeLessThanEqualsToken(precedingTrivia As CoreInternalSyntax.SyntaxList(Of VisualBasicSyntaxNode), length As Integer) As PunctuationSyntax
            Return MakePunctuationToken(precedingTrivia, length, SyntaxKind.LessThanEqualsToken)
        End Function

        Private Function MakeLessThanGreaterThanToken(precedingTrivia As CoreInternalSyntax.SyntaxList(Of VisualBasicSyntaxNode), length As Integer) As PunctuationSyntax
            Return MakePunctuationToken(precedingTrivia, length, SyntaxKind.LessThanGreaterThanToken)
        End Function

        Private Function MakeLessThanLessThanToken(precedingTrivia As CoreInternalSyntax.SyntaxList(Of VisualBasicSyntaxNode), length As Integer) As PunctuationSyntax
            Return MakePunctuationToken(precedingTrivia, length, SyntaxKind.LessThanLessThanToken)
        End Function

        Private Function MakeGreaterThanGreaterThanToken(precedingTrivia As CoreInternalSyntax.SyntaxList(Of VisualBasicSyntaxNode), length As Integer) As PunctuationSyntax
            Return MakePunctuationToken(precedingTrivia, length, SyntaxKind.GreaterThanGreaterThanToken)
        End Function

        Private Function MakeLessThanLessThanEqualsToken(precedingTrivia As CoreInternalSyntax.SyntaxList(Of VisualBasicSyntaxNode), length As Integer) As PunctuationSyntax
            Return MakePunctuationToken(precedingTrivia, length, SyntaxKind.LessThanLessThanEqualsToken)
        End Function

        Private Function MakeGreaterThanGreaterThanEqualsToken(precedingTrivia As CoreInternalSyntax.SyntaxList(Of VisualBasicSyntaxNode), length As Integer) As PunctuationSyntax
            Return MakePunctuationToken(precedingTrivia, length, SyntaxKind.GreaterThanGreaterThanEqualsToken)
        End Function

        Private Function MakeAtToken(precedingTrivia As CoreInternalSyntax.SyntaxList(Of VisualBasicSyntaxNode), charIsFullWidth As Boolean) As PunctuationSyntax
            Dim spelling = If(charIsFullWidth, FULLWIDTH_COMMERCIAL_AT_STRING, "@")
            AdvanceChar()

            Return MakePunctuationToken(precedingTrivia, spelling, SyntaxKind.AtToken)
        End Function

#End Region

#Region "Literals"
        Private Function MakeIntegerLiteralToken(precedingTrivia As CoreInternalSyntax.SyntaxList(Of VisualBasicSyntaxNode),
                                         base As LiteralBase,
                                         typeCharacter As TypeCharacter,
                                         integralValue As ULong,
                                         length As Integer) As SyntaxToken

            Dim spelling = GetText(length)
            Dim followingTrivia = ScanSingleLineTrivia()

            Dim tp As New TokenParts(precedingTrivia, followingTrivia, spelling)

            Dim p As SyntaxToken = Nothing
            If _literalTable.TryGetValue(tp, p) Then
                Return p
            End If

            p = SyntaxFactory.IntegerLiteralToken(
                        spelling,
                        base,
                        typeCharacter,
                        integralValue,
                        precedingTrivia.Node,
                        followingTrivia.Node)

            _literalTable.Add(tp, p)
            Return p
        End Function

        Private Function MakeCharacterLiteralToken(precedingTrivia As CoreInternalSyntax.SyntaxList(Of VisualBasicSyntaxNode), value As Char, length As Integer) As SyntaxToken
            Dim spelling = GetText(length)
            Dim followingTrivia = ScanSingleLineTrivia()

            Dim tp As New TokenParts(precedingTrivia, followingTrivia, spelling)

            Dim p As SyntaxToken = Nothing
            If _literalTable.TryGetValue(tp, p) Then
                Return p
            End If

            p = SyntaxFactory.CharacterLiteralToken(spelling, value, precedingTrivia.Node, followingTrivia.Node)
            _literalTable.Add(tp, p)
            Return p
        End Function

        Private Function MakeDateLiteralToken(precedingTrivia As CoreInternalSyntax.SyntaxList(Of VisualBasicSyntaxNode), value As DateTime, length As Integer) As SyntaxToken
            Dim spelling = GetText(length)
            Dim followingTrivia = ScanSingleLineTrivia()

            Dim tp As New TokenParts(precedingTrivia, followingTrivia, spelling)

            Dim p As SyntaxToken = Nothing
            If _literalTable.TryGetValue(tp, p) Then
                Return p
            End If

            p = SyntaxFactory.DateLiteralToken(spelling, value, precedingTrivia.Node, followingTrivia.Node)
            _literalTable.Add(tp, p)
            Return p
        End Function

        Private Function MakeFloatingLiteralToken(precedingTrivia As CoreInternalSyntax.SyntaxList(Of VisualBasicSyntaxNode),
                                         typeCharacter As TypeCharacter,
                                         floatingValue As Double,
                                         length As Integer) As SyntaxToken

            Dim spelling = GetText(length)
            Dim followingTrivia = ScanSingleLineTrivia()

            Dim tp As New TokenParts(precedingTrivia, followingTrivia, spelling)

            Dim p As SyntaxToken = Nothing
            If _literalTable.TryGetValue(tp, p) Then
                Return p
            End If

            p = SyntaxFactory.FloatingLiteralToken(
                        spelling,
                        typeCharacter,
                        floatingValue,
                        precedingTrivia.Node,
                        followingTrivia.Node)

            _literalTable.Add(tp, p)
            Return p
        End Function

        Private Function MakeDecimalLiteralToken(precedingTrivia As CoreInternalSyntax.SyntaxList(Of VisualBasicSyntaxNode),
                                 typeCharacter As TypeCharacter,
                                 decimalValue As Decimal,
                                 length As Integer) As SyntaxToken

            Dim spelling = GetText(length)
            Dim followingTrivia = ScanSingleLineTrivia()

            Dim tp As New TokenParts(precedingTrivia, followingTrivia, spelling)

            Dim p As SyntaxToken = Nothing
            If _literalTable.TryGetValue(tp, p) Then
                Return p
            End If

            p = SyntaxFactory.DecimalLiteralToken(
                        spelling,
                        typeCharacter,
                        decimalValue,
                        precedingTrivia.Node,
                        followingTrivia.Node)

            _literalTable.Add(tp, p)
            Return p
        End Function
#End Region

        ' BAD TOKEN

        Private Function MakeBadToken(precedingTrivia As CoreInternalSyntax.SyntaxList(Of VisualBasicSyntaxNode), length As Integer, errId As ERRID) As SyntaxToken
            Dim spelling = GetTextNotInterned(length)
            Dim followingTrivia = ScanSingleLineTrivia()

            Dim result1 = SyntaxFactory.BadToken(SyntaxSubKind.None, spelling, precedingTrivia.Node, followingTrivia.Node)
            Dim errResult1 = result1.AddError(ErrorFactory.ErrorInfo(errId))
            Return DirectCast(errResult1, SyntaxToken)
        End Function

        Private Shared Function MakeEofToken(precedingTrivia As CoreInternalSyntax.SyntaxList(Of VisualBasicSyntaxNode)) As SyntaxToken
            Return SyntaxFactory.Token(precedingTrivia.Node, SyntaxKind.EndOfFileToken, Nothing, String.Empty)
        End Function

        Private ReadOnly _simpleEof As SyntaxToken = SyntaxFactory.Token(Nothing, SyntaxKind.EndOfFileToken, Nothing, String.Empty)
        Private Function MakeEofToken() As SyntaxToken
            Return _simpleEof
        End Function
    End Class
End Namespace
