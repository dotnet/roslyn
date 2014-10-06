' Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.Syntax

    Friend Class SyntaxFormatter
        Inherits VBSyntaxRewriter

        Private ReadOnly indentWhitespace As String
        Private ReadOnly useElasticTrivia As Boolean
        Private ReadOnly useDefaultCasing As Boolean

        Private isInStructuredTrivia As Boolean

        Private previousToken As SyntaxToken

        Private afterLineBreak As Boolean
        Private afterIndentation As Boolean

        Private lineBreaksAfterToken As Dictionary(Of SyntaxToken, Integer) = New Dictionary(Of SyntaxToken, Integer)()
        Private lastStatementsInBlocks As HashSet(Of SyntaxNode) = New HashSet(Of SyntaxNode)()

        Private indentationDepth As Integer

        Private indentations As ArrayBuilder(Of SyntaxTrivia)

        ''' <summary>
        ''' Creates a Syntax Formatter visitor
        ''' </summary>
        ''' <param name="indentWhitespace">The whitespace to indent with</param>
        ''' <param name="useElasticTrivia">Whether to use elastic trivia or not</param>
        ''' <param name="useDefaultCasing">Whether to rewrite keywords in default casing or not</param>
        ''' <remarks></remarks>
        Private Sub New(indentWhitespace As String, Optional useElasticTrivia As Boolean = False, Optional useDefaultCasing As Boolean = False)
            : MyBase.New(VisitIntoStructuredTrivia:=True)

            Me.indentWhitespace = indentWhitespace
            Me.useElasticTrivia = useElasticTrivia
            Me.useDefaultCasing = useDefaultCasing

            Me.afterLineBreak = True
        End Sub

        Friend Shared Function Format(Of TNode As SyntaxNode)(node As TNode, indentWhitespace As String, Optional useElasticTrivia As Boolean = False, Optional useDefaultCasing As Boolean = False) As SyntaxNode
            Dim formatter As New SyntaxFormatter(indentWhitespace, useElasticTrivia, useDefaultCasing)
            Dim result As TNode = CType(formatter.Visit(node), TNode)
            formatter.Free()
            Return result
        End Function

        Friend Shared Function Format(token As SyntaxToken, indentWhitespace As String, Optional useElasticTrivia As Boolean = False, Optional useDefaultCasing As Boolean = False) As SyntaxToken
            Dim formatter As New SyntaxFormatter(indentWhitespace, useElasticTrivia, useDefaultCasing)
            Dim result As SyntaxToken = formatter.VisitToken(token)
            formatter.Free()
            Return result
        End Function

        Friend Shared Function Format(trivia As SyntaxTriviaList, indentWhitespace As String, Optional useElasticTrivia As Boolean = False, Optional useDefaultCasing As Boolean = False) As SyntaxTriviaList
            Dim formatter = New SyntaxFormatter(indentWhitespace, useElasticTrivia, useDefaultCasing)
            Dim result As SyntaxTriviaList = formatter.RewriteTrivia(trivia,
                                            formatter.GetIndentationDepth(),
                                            isTrailing:=False,
                                            mustBeIndented:=False,
                                            mustHaveSeparator:=False,
                                            lineBreaksAfter:=0, lineBreaksBefore:=0)
            formatter.Free()
            Return result
        End Function

        Private Sub Free()
            If indentations IsNot Nothing Then
                indentations.Free()
            End If
        End Sub

        Private Function GetIdentation(count As Integer) As SyntaxTrivia
            Dim capacity = count + 1
            If indentations Is Nothing Then
                indentations = ArrayBuilder(Of SyntaxTrivia).GetInstance(capacity)
            Else
                indentations.EnsureCapacity(capacity)
            End If

            For i As Integer = indentations.Count To count
                Dim text As String = If(i = 0, "", indentations(i - 1).ToString() + Me.indentWhitespace)
                indentations.Add(SyntaxFactory.Whitespace(text, Me.useElasticTrivia))
            Next

            Return indentations(count)
        End Function

        ' use leadingTrivia as indentation
        ' use trailingTrivia as separation & newlines if needed
        Public Overrides Function VisitToken(token As SyntaxToken) As SyntaxToken

            ' ignore tokens with no content
            If token.VBKind = SyntaxKind.None Then
                Return token
            End If

            Try
                Dim newToken As SyntaxToken
                If useDefaultCasing AndAlso token.IsKeyword() Then
                    newToken = SyntaxFactory.Token(token.VBKind)
                Else
                    newToken = token
                End If

                Dim indentationDepth = GetIndentationDepth()

                ' check if this token is first on this line
                Dim numLineBreaksBefore As Integer = LineBreaksBetween(previousToken, token)

                Dim needsIndentation As Boolean = (numLineBreaksBefore > 0)

                ' all line breaks except the first will be leading trivia of this token. The first line break
                ' is trailing trivia of the previous token.
                If numLineBreaksBefore > 0 AndAlso IsLastTokenOnLine(previousToken) Then
                    numLineBreaksBefore -= 1
                End If

                newToken = newToken.WithLeadingTrivia(
                            RewriteTrivia(
                                token.LeadingTrivia,
                                indentationDepth,
                                isTrailing:=False,
                                mustBeIndented:=needsIndentation,
                                mustHaveSeparator:=False,
                                lineBreaksAfter:=0,
                                lineBreaksBefore:=numLineBreaksBefore))

                Dim nextToken As SyntaxToken = GetNextRelevantToken(token)

                Me.afterIndentation = False

                ' we only add one of the line breaks to trivia of this token. The remaining ones will be leading trivia 
                ' for the next token
                Dim numLineBreaksAfter As Integer = If(LineBreaksBetween(token, nextToken) > 0, 1, 0)
                Dim needsSeparatorAfter = If(numLineBreaksAfter > 0, False, NeedsSeparator(token, nextToken))

                newToken = newToken.WithTrailingTrivia(
                            RewriteTrivia(
                                token.TrailingTrivia,
                                depth:=0,
                                isTrailing:=True,
                                mustBeIndented:=False,
                                mustHaveSeparator:=needsSeparatorAfter,
                                lineBreaksAfter:=numLineBreaksAfter,
                                lineBreaksBefore:=0))

                If newToken.VBKind = SyntaxKind.DocumentationCommentLineBreakToken Then
                    afterLineBreak = True

                ElseIf newToken.VBKind = SyntaxKind.XmlTextLiteralToken Then
                    If newToken.TrailingTrivia.Count = 0 AndAlso IsNewLineChar(newToken.ValueText.Last) Then
                        afterLineBreak = True
                    End If
                End If

                Return newToken

            Finally
                Me.previousToken = token
            End Try

            Return token
        End Function

        Private Function RewriteTrivia(
            triviaList As SyntaxTriviaList,
            depth As Integer,
            isTrailing As Boolean,
            mustBeIndented As Boolean,
            mustHaveSeparator As Boolean,
            lineBreaksAfter As Integer,
            lineBreaksBefore As Integer) As SyntaxTriviaList

            Dim currentTriviaList As ArrayBuilder(Of SyntaxTrivia) = ArrayBuilder(Of SyntaxTrivia).GetInstance()
            Try
                For i = 1 To lineBreaksBefore
                    currentTriviaList.Add(GetCarriageReturnLineFeed())
                    afterLineBreak = True
                    afterIndentation = False
                Next

                For Each trivia In triviaList

                    ' just keep non whitespace trivia
                    If trivia.VBKind = SyntaxKind.WhitespaceTrivia OrElse
                        trivia.VBKind = SyntaxKind.EndOfLineTrivia OrElse
                        trivia.VBKind = SyntaxKind.LineContinuationTrivia OrElse
                        trivia.FullWidth = 0 Then

                        Continue For
                    End If

                    ' check if there's a separator or a line break needed between the trivia itself
                    Dim tokenParent = trivia.Token.Parent
                    Dim needsSeparator As Boolean =
                        Not (trivia.VBKind = SyntaxKind.ColonTrivia AndAlso tokenParent IsNot Nothing AndAlso tokenParent.VBKind = SyntaxKind.LabelStatement) AndAlso
                        Not (tokenParent IsNot Nothing AndAlso tokenParent.Parent IsNot Nothing AndAlso tokenParent.Parent.VBKind = SyntaxKind.CrefReference) AndAlso
                        (
                            (currentTriviaList.Count > 0 AndAlso NeedsSeparatorBetween(currentTriviaList.Last()) AndAlso Not EndsInLineBreak(currentTriviaList.Last())) OrElse
                            (currentTriviaList.Count = 0 AndAlso isTrailing)
                        )

                    Dim needsLineBreak As Boolean = NeedsLineBreakBefore(trivia) OrElse
                        (currentTriviaList.Count > 0 AndAlso NeedsLineBreakBetween(currentTriviaList.Last(), trivia, isTrailing))

                    If needsLineBreak AndAlso Not afterLineBreak Then
                        currentTriviaList.Add(GetCarriageReturnLineFeed())
                        afterLineBreak = True
                        afterIndentation = False
                    End If

                    If afterLineBreak And Not isTrailing Then
                        If Not afterIndentation AndAlso Me.NeedsIndentAfterLineBreak(trivia) Then
                            currentTriviaList.Add(Me.GetIdentation(GetIndentationDepth(trivia)))
                            afterIndentation = True
                        End If

                    ElseIf needsSeparator Then
                        currentTriviaList.Add(GetSpace())
                        afterLineBreak = False
                        afterIndentation = False
                    End If

                    If trivia.HasStructure Then
                        Dim structuredTrivia As SyntaxTrivia = Me.VisitStructuredTrivia(trivia)
                        currentTriviaList.Add(structuredTrivia)
                    Else
                        ' in structured trivia, the xml doc ''' token contains leading whitespace as text (*yiiks*)
                        If trivia.VBKind = SyntaxKind.DocumentationCommentExteriorTrivia Then
                            trivia = SyntaxFactory.DocumentationCommentExteriorTrivia(SyntaxFacts.GetText(SyntaxKind.DocumentationCommentExteriorTrivia))
                        End If

                        currentTriviaList.Add(trivia)
                    End If

                    If NeedsLineBreakAfter(trivia) Then
                        If Not isTrailing Then
                            currentTriviaList.Add(GetCarriageReturnLineFeed())
                            afterLineBreak = True
                            afterIndentation = False
                        End If
                    Else
                        afterLineBreak = EndsInLineBreak(trivia)
                    End If
                Next

                If lineBreaksAfter > 0 Then
                    If currentTriviaList.Count > 0 AndAlso EndsInLineBreak(currentTriviaList.Last()) Then
                        lineBreaksAfter = lineBreaksAfter - 1
                    End If

                    For i = 0 To lineBreaksAfter - 1
                        currentTriviaList.Add(GetCarriageReturnLineFeed())
                        afterLineBreak = True
                        afterIndentation = False
                    Next i

                ElseIf mustHaveSeparator Then
                    currentTriviaList.Add(GetSpace())
                    afterLineBreak = False
                    afterIndentation = False
                End If

                If mustBeIndented Then
                    currentTriviaList.Add(Me.GetIdentation(depth))
                    afterIndentation = True
                    afterLineBreak = False
                End If

                If currentTriviaList.Count = 0 Then
                    If useElasticTrivia Then
                        Return SyntaxFactory.TriviaList(SyntaxFactory.ElasticMarker)
                    Else
                        Return Nothing
                    End If
                ElseIf currentTriviaList.Count = 1 Then
                    Return SyntaxFactory.TriviaList(currentTriviaList.First())
                Else
                    Return SyntaxFactory.TriviaList(currentTriviaList)
                End If
            Finally
                currentTriviaList.Free()
            End Try
        End Function

        Private Function IsLastTokenOnLine(token As SyntaxToken) As Boolean
            Return (token.HasTrailingTrivia AndAlso token.TrailingTrivia.Last.VBKind = SyntaxKind.ColonTrivia) OrElse
                (token.Parent IsNot Nothing AndAlso token.Parent.GetLastToken() = token)
        End Function

        Private Function LineBreaksBetween(currentToken As SyntaxToken, nextToken As SyntaxToken) As Integer
            ' First and last token may be of kind none
            If currentToken.VBKind = SyntaxKind.None OrElse nextToken.VBKind = SyntaxKind.None Then
                Return 0
            End If

            Dim numLineBreaks As Integer = 0
            If lineBreaksAfterToken.TryGetValue(currentToken, numLineBreaks) Then
                Return numLineBreaks
            End If

            Return 0
        End Function

        ''' <summary>
        ''' indentation depth is the declaration depth for statements within the block. for start/end statements
        ''' of these blocks (e.g. the if statement), it is a level less
        ''' </summary>
        Private Function GetIndentationDepth() As Integer
            Debug.Assert(Me.indentationDepth >= 0)
            Return Me.indentationDepth
        End Function

        Private Function GetIndentationDepth(trivia As SyntaxTrivia) As Integer
            If SyntaxFacts.IsPreprocessorDirective(trivia.VBKind) Then
                Return 0
            End If

            Return GetIndentationDepth()
        End Function

        Private Function GetSpace() As SyntaxTrivia
            Return If(Me.useElasticTrivia, SyntaxFactory.ElasticSpace, SyntaxFactory.Space)
        End Function

        Private Function GetCarriageReturnLineFeed() As SyntaxTrivia
            Return If(Me.useElasticTrivia, SyntaxFactory.ElasticCarriageReturnLineFeed, SyntaxFactory.CarriageReturnLineFeed)
        End Function

        Private Function NeedsSeparatorBetween(trivia As SyntaxTrivia) As Boolean
            Select Case trivia.VBKind
                Case SyntaxKind.None,
                        SyntaxKind.WhitespaceTrivia,
                        SyntaxKind.DocumentationCommentExteriorTrivia,
                        SyntaxKind.LineContinuationTrivia
                    Return False

                Case Else
                    Return Not SyntaxFacts.IsPreprocessorDirective(trivia.VBKind)
            End Select
        End Function

        Private Function NeedsLineBreakBetween(trivia As SyntaxTrivia, nextTrivia As SyntaxTrivia, isTrailingTrivia As Boolean) As Boolean
            If EndsInLineBreak(trivia) Then
                Return False
            End If

            Select Case nextTrivia.VBKind
                Case SyntaxKind.CommentTrivia, SyntaxKind.DocumentationCommentExteriorTrivia, SyntaxKind.EmptyStatement,
                    SyntaxKind.IfDirectiveTrivia,
                    SyntaxKind.ElseIfDirectiveTrivia,
                    SyntaxKind.ElseDirectiveTrivia,
                    SyntaxKind.EndIfDirectiveTrivia,
                    SyntaxKind.RegionDirectiveTrivia,
                    SyntaxKind.EndRegionDirectiveTrivia,
                    SyntaxKind.ConstDirectiveTrivia,
                    SyntaxKind.ExternalSourceDirectiveTrivia,
                    SyntaxKind.EndExternalSourceDirectiveTrivia,
                    SyntaxKind.ExternalChecksumDirectiveTrivia,
                    SyntaxKind.EnableWarningDirectiveTrivia,
                    SyntaxKind.DisableWarningDirectiveTrivia,
                    SyntaxKind.ReferenceDirectiveTrivia,
                    SyntaxKind.BadDirectiveTrivia

                    Return Not isTrailingTrivia

                Case Else
                    Return False
            End Select
        End Function

        Private Function NeedsLineBreakAfter(trivia As SyntaxTrivia) As Boolean
            Return trivia.VBKind = SyntaxKind.CommentTrivia
        End Function

        Private Function NeedsLineBreakBefore(trivia As SyntaxTrivia) As Boolean
            Select Case trivia.VBKind
                Case SyntaxKind.DocumentationCommentExteriorTrivia
                    Return True

                Case Else
                    Return False
            End Select
        End Function

        Private Function NeedsIndentAfterLineBreak(trivia As SyntaxTrivia) As Boolean
            Select Case trivia.VBKind
                Case SyntaxKind.CommentTrivia,
                        SyntaxKind.DocumentationCommentExteriorTrivia,
                        SyntaxKind.DocumentationCommentTrivia
                    Return True

                Case Else
                    Return False
            End Select
        End Function

        Private Function NeedsSeparator(token As SyntaxToken, nextToken As SyntaxToken) As Boolean
            If token.VBKind = SyntaxKind.EndOfFileToken Then
                Return False
            End If

            If token.Parent Is Nothing OrElse nextToken.VBKind = SyntaxKind.None Then
                Return False
            End If

            If nextToken.Parent.VBKind = SyntaxKind.SingleLineFunctionLambdaExpression Then
                Return True
            End If

            If nextToken.VBKind = SyntaxKind.EndOfFileToken Then
                Return False
            End If

            ' +1 instead of + 1
            ' but not a instead of nota ...
            If TypeOf (token.Parent) Is UnaryExpressionSyntax AndAlso
                token.VBKind <> SyntaxKind.NotKeyword AndAlso
                token.VBKind <> SyntaxKind.AddressOfKeyword Then
                Return False
            End If

            ' generally a + b, needs to go here to make it b + (a + b) instead of b +(a + b
            If TypeOf (token.Parent) Is BinaryExpressionSyntax OrElse
                TypeOf (nextToken.Parent) Is BinaryExpressionSyntax Then
                Return True
            End If

            ' (a instead of ( a
            If token.VBKind = SyntaxKind.OpenParenToken Then
                Return False
            End If

            ' a) instead of a )
            If nextToken.VBKind = SyntaxKind.CloseParenToken Then
                Return False
            End If

            ' m( instead of m (
            If token.VBKind <> SyntaxKind.CommaToken AndAlso nextToken.VBKind = SyntaxKind.OpenParenToken Then
                Return False
            End If

            ' (,,,) instead of ( , , ,) or (a As Char, b as Char) instead of (a As Char , b as Char)
            If (token.VBKind = SyntaxKind.CommaToken AndAlso nextToken.VBKind = SyntaxKind.EmptyToken) OrElse
               nextToken.VBKind = SyntaxKind.CommaToken Then
                Return False
            End If

            ' a.b and .b instead of a . b, but keep with {key .field}
            If token.VBKind = SyntaxKind.DotToken Then
                Return False
            ElseIf nextToken.VBKind = SyntaxKind.DotToken AndAlso nextToken.Parent.VBKind <> SyntaxKind.NamedFieldInitializer Then
                Return False
            End If

            ' label: instead of label :
            If nextToken.VBKind = SyntaxKind.ColonToken AndAlso token.Parent.VBKind = SyntaxKind.LabelStatement Then
                Return False
            End If

            ' {1 instead of { 1 and 1} instead of 1 }
            If token.VBKind = SyntaxKind.OpenBraceToken OrElse nextToken.VBKind = SyntaxKind.CloseBraceToken Then
                Return False
            End If

            ' s1(p1:=23, p2:=12) instead of s1(p1 := 23, p2 := 12)
            If token.VBKind = SyntaxKind.ColonEqualsToken OrElse nextToken.VBKind = SyntaxKind.ColonEqualsToken Then
                Return False
            End If

            ' case > 100 should keep separator
            ' need to test before xml analysis below
            If SyntaxFacts.IsRelationalCaseClause(token.Parent.VBKind()) OrElse
                SyntaxFacts.IsRelationalCaseClause(nextToken.Parent.VBKind()) Then
                Return True
            End If

            ' handle closing attribute before XML tokens
            ' sub foo(<obsolete()> ByRef i as Integer) instead of sub foo(<obsolete()>ByRef i as Integer)
            If (token.VBKind = SyntaxKind.GreaterThanToken AndAlso
                token.Parent.VBKind = SyntaxKind.AttributeList) Then
                Return True
            End If

            ' needs to be checked after binary operators
            ' Imports <foo instead of Imports < foo
            If (token.VBKind = SyntaxKind.LessThanToken OrElse
                nextToken.VBKind = SyntaxKind.GreaterThanToken OrElse
                token.VBKind = SyntaxKind.LessThanSlashToken OrElse
                token.VBKind = SyntaxKind.GreaterThanToken OrElse
                nextToken.VBKind = SyntaxKind.LessThanSlashToken) Then
                Return False
            End If

            ' <xmlns:foo instead of <xmlns : foo
            If token.VBKind = SyntaxKind.ColonToken AndAlso token.Parent.VBKind = SyntaxKind.XmlPrefix OrElse
                nextToken.VBKind = SyntaxKind.ColonToken AndAlso nextToken.Parent.VBKind = SyntaxKind.XmlPrefix Then
                Return False
            End If

            ' <e/> instead of <e />
            If nextToken.VBKind = SyntaxKind.SlashGreaterThanToken Then
                Return False
            End If

            ' <!--a--> instead of <!-- a -->
            If token.VBKind = SyntaxKind.LessThanExclamationMinusMinusToken OrElse
                nextToken.VBKind = SyntaxKind.MinusMinusGreaterThanToken Then
                Return False
            End If

            ' <?xml ?> instead of <? xml ?>
            If token.VBKind = SyntaxKind.LessThanQuestionToken Then
                Return False
            End If

            ' <![CDATA[foo]]> instead of <![CDATA[ foo ]]>
            If token.VBKind = SyntaxKind.BeginCDataToken OrElse
                nextToken.VBKind = SyntaxKind.EndCDataToken Then
                Return False
            End If

            ' <Assembly:System.Copyright("(C) 2009")> instead of <Assembly : System.Copyright("(C) 2009")>
            If token.VBKind = SyntaxKind.ColonToken AndAlso token.Parent.VBKind = SyntaxKind.AttributeTarget OrElse
                nextToken.VBKind = SyntaxKind.ColonToken AndAlso nextToken.Parent.VBKind = SyntaxKind.AttributeTarget Then
                Return False
            End If

            ' <foo="bar" instead of <foo = "bar"
            If (token.VBKind = SyntaxKind.EqualsToken AndAlso
                (token.Parent.VBKind = SyntaxKind.XmlAttribute OrElse
                    token.Parent.VBKind = SyntaxKind.XmlCrefAttribute OrElse
                    token.Parent.VBKind = SyntaxKind.XmlNameAttribute OrElse
                    token.Parent.VBKind = SyntaxKind.XmlDeclaration)) OrElse
                (nextToken.VBKind = SyntaxKind.EqualsToken AndAlso
                (nextToken.Parent.VBKind = SyntaxKind.XmlAttribute OrElse
                    nextToken.Parent.VBKind = SyntaxKind.XmlCrefAttribute OrElse
                    nextToken.Parent.VBKind = SyntaxKind.XmlNameAttribute OrElse
                    nextToken.Parent.VBKind = SyntaxKind.XmlDeclaration)) Then
                Return False
            End If

            ' needs to be below binary expression checks
            ' <attrib="foo" instead of <attrib=" foo "
            If token.VBKind = SyntaxKind.DoubleQuoteToken OrElse
                nextToken.VBKind = SyntaxKind.DoubleQuoteToken Then
                Return False
            End If

            ' <x/>@a instead of <x/>@ a
            If token.VBKind = SyntaxKind.AtToken AndAlso token.Parent.VBKind = SyntaxKind.XmlAttributeAccessExpression Then
                Return False
            End If

            ' 'e' instead of ' e '
            If token.VBKind = SyntaxKind.SingleQuoteToken OrElse
                nextToken.VBKind = SyntaxKind.SingleQuoteToken Then
                Return False
            End If

            ' Integer? instead of Integer ?
            If nextToken.VBKind = SyntaxKind.QuestionToken Then
                Return False
            End If

            ' #if instead of # if
            If token.VBKind = SyntaxKind.HashToken AndAlso TypeOf token.Parent Is DirectiveTriviaSyntax Then
                Return False
            End If

            ' "#region" instead of "#region "
            If token.Parent.VBKind = SyntaxKind.RegionDirectiveTrivia AndAlso
                nextToken.VBKind = SyntaxKind.StringLiteralToken AndAlso
                String.IsNullOrEmpty(nextToken.ValueText) Then
                Return False
            End If

            If token.VBKind = SyntaxKind.XmlTextLiteralToken OrElse token.VBKind = SyntaxKind.DocumentationCommentLineBreakToken Then
                Return False
            End If

            Return True
        End Function

        Private Function EndsInLineBreak(trivia As SyntaxTrivia) As Boolean
            If trivia.VBKind = SyntaxKind.EndOfLineTrivia Then
                Return True
            End If

            If trivia.VBKind = SyntaxKind.DisabledTextTrivia Then
                Dim text As String = trivia.ToFullString()
                Return text.Length > 0 AndAlso IsNewLineChar(text.Last())
            End If

            If trivia.HasStructure Then
                If trivia.GetStructure.GetLastToken.HasTrailingTrivia AndAlso
                    trivia.GetStructure.GetLastToken.TrailingTrivia.Last.VBKind = SyntaxKind.EndOfLineTrivia Then

                    Return True
                End If
            End If

            Return False
        End Function

        Private Shared Function IsNewLineChar(ch As Char) As Boolean
            ' new-line-character:
            '   Carriage return character (U+000D)
            '   Line feed character (U+000A)
            '   Next line character (U+0085)
            '   Line separator character (U+2028)
            '   Paragraph separator character (U+2029)

            Return ch = vbLf _
                OrElse ch = vbCr _
                OrElse ch = "\u0085" _
                OrElse ch = "\u2028" _
                OrElse ch = "\u2029"
        End Function

        Private Overloads Function VisitStructuredTrivia(trivia As SyntaxTrivia) As SyntaxTrivia
            Dim oldIsInStructuredTrivia As Boolean = Me.isInStructuredTrivia
            Me.isInStructuredTrivia = True

            Dim oldPreviousToken = Me.previousToken
            Me.previousToken = Nothing

            Dim result As SyntaxTrivia = VisitTrivia(trivia)

            Me.isInStructuredTrivia = oldIsInStructuredTrivia
            Me.previousToken = oldPreviousToken

            Return result
        End Function

        Private Function GetNextRelevantToken(token As SyntaxToken) As SyntaxToken
            Return token.GetNextToken(Function(t As SyntaxToken)
                                          Return t.VBKind <> SyntaxKind.None
                                      End Function, Function(t As SyntaxTrivia) False)
        End Function

        Private Sub AddLinebreaksAfterElementsIfNeeded(Of TNode As SyntaxNode)(
            list As SyntaxList(Of TNode),
            linebreaksBetweenElements As Integer,
            linebreaksAfterLastElement As Integer
        )
            Dim lastElementIndex = list.Count - 1
            For elementIndex = 0 To lastElementIndex
                Dim listElement = list(elementIndex)
                If listElement.VBKind = SyntaxKind.LabelStatement Then
                    ' always add linebreaks after label
                    lineBreaksAfterToken(listElement.GetLastToken()) = 1
                Else
                    AddLinebreaksAfterTokenIfNeeded(listElement.GetLastToken(), If(elementIndex = lastElementIndex,
                                                                                   linebreaksAfterLastElement,
                                                                                   linebreaksBetweenElements))
                End If
            Next
        End Sub

        Private Sub AddLinebreaksAfterTokenIfNeeded(node As SyntaxToken, linebreaksAfterToken As Integer)
            If Not EndsWithColonSeparator(node) Then
                Me.lineBreaksAfterToken(node) = linebreaksAfterToken
            End If
        End Sub

        Private Function EndsWithColonSeparator(node As SyntaxToken) As Boolean
            Return node.HasTrailingTrivia AndAlso
                    node.TrailingTrivia.Last.VBKind = SyntaxKind.ColonTrivia
        End Function

        Private Sub MarkLastStatementIfNeeded(Of TNode As SyntaxNode)(list As SyntaxList(Of TNode))
            If list.Any Then
                lastStatementsInBlocks.Add(list.Last)
            End If
        End Sub

        ''' <summary>
        ''' We each element of option, imports and attributes on a separate line, where the last element of this the list if 
        ''' followed by an empty line:
        ''' Option Strict On
        ''' 
        ''' Imports System
        ''' Imports Foo
        ''' 
        ''' [...]
        ''' 
        ''' Namespace
        ''' [...]
        ''' </summary>
        Public Overrides Function VisitCompilationUnit(node As CompilationUnitSyntax) As SyntaxNode
            Dim hasImports = node.Imports.Any
            Dim hasMembers = node.Members.Any
            Dim hasAttributes = node.Attributes.Any

            If hasImports OrElse hasAttributes OrElse hasMembers Then
                AddLinebreaksAfterElementsIfNeeded(node.Options, 1, 2)
            Else
                AddLinebreaksAfterElementsIfNeeded(node.Options, 1, 1)
            End If

            If hasAttributes OrElse hasMembers Then
                AddLinebreaksAfterElementsIfNeeded(node.Imports, 1, 2)
            Else
                AddLinebreaksAfterElementsIfNeeded(node.Imports, 1, 1)
            End If

            If hasMembers Then
                AddLinebreaksAfterElementsIfNeeded(node.Attributes, 1, 2)
            Else
                AddLinebreaksAfterElementsIfNeeded(node.Attributes, 1, 1)
            End If

            AddLinebreaksAfterElementsIfNeeded(node.Members, 2, 1)

            Return MyBase.VisitCompilationUnit(node)
        End Function

        ''' <summary>
        ''' Add an empty line after the begin, except the first member is a nested namespace.
        ''' Separate each member of a namespace with an empty line. 
        ''' </summary>
        Public Overrides Function VisitNamespaceBlock(node As NamespaceBlockSyntax) As SyntaxNode

            If node.Members.Count > 0 Then
                ' Add an empty line after the namespace begin if there
                ' is not a namespace declaration as first member
                If node.Members(0).Kind <> SyntaxKind.NamespaceBlock Then
                    AddLinebreaksAfterTokenIfNeeded(node.NamespaceStatement.GetLastToken(), 2)
                Else
                    AddLinebreaksAfterTokenIfNeeded(node.NamespaceStatement.GetLastToken(), 1)
                End If

                AddLinebreaksAfterElementsIfNeeded(node.Members, 2, 1)
            Else
                AddLinebreaksAfterTokenIfNeeded(node.NamespaceStatement.GetLastToken(), 1)
            End If

            Return MyBase.VisitNamespaceBlock(node)
        End Function

        Public Overrides Function VisitModuleBlock(ByVal node As ModuleBlockSyntax) As SyntaxNode
            VisitTypeBlockSyntax(node)

            Return MyBase.VisitModuleBlock(node)
        End Function

        Public Overrides Function VisitClassBlock(ByVal node As ClassBlockSyntax) As SyntaxNode
            VisitTypeBlockSyntax(node)

            Return MyBase.VisitClassBlock(node)
        End Function

        Public Overrides Function VisitStructureBlock(ByVal node As StructureBlockSyntax) As SyntaxNode
            VisitTypeBlockSyntax(node)

            Return MyBase.VisitStructureBlock(node)
        End Function

        Public Overrides Function VisitInterfaceBlock(ByVal node As InterfaceBlockSyntax) As SyntaxNode
            VisitTypeBlockSyntax(node)

            Return MyBase.VisitInterfaceBlock(node)
        End Function

        ''' <summary>
        ''' We want to display type blocks (Modules, Classes, Structures and Interfaces) like follows
        ''' Class Foo
        '''   implements IBar1, IBar2
        '''   implements IBar3
        '''   inherits Bar1
        ''' 
        '''   Public Sub Boo()
        '''   End Sub
        ''' End Class
        ''' 
        ''' or
        ''' 
        ''' Class Foo
        ''' 
        '''   Public Sub Boo()
        '''   End Sub
        ''' End Class
        ''' 
        ''' Basically it's an empty line between implements and inherits and between each member. If there are no
        ''' inherits or implements, add an empty line before the first member.
        ''' </summary>
        Private Sub VisitTypeBlockSyntax(ByVal node As TypeBlockSyntax)

            Dim hasImplements As Boolean = node.Implements.Count > 0
            Dim hasInherits As Boolean = node.Inherits.Count > 0

            ' add a line break between begin statement and the ones from the statement list
            If Not hasInherits AndAlso Not hasImplements AndAlso node.Members.Count > 0 Then
                AddLinebreaksAfterTokenIfNeeded(node.Begin.GetLastToken(), 2)
            Else
                AddLinebreaksAfterTokenIfNeeded(node.Begin.GetLastToken(), 1)
            End If

            If hasImplements Then
                AddLinebreaksAfterElementsIfNeeded(node.Inherits, 1, 1)
            Else
                AddLinebreaksAfterElementsIfNeeded(node.Inherits, 1, 2)
            End If

            AddLinebreaksAfterElementsIfNeeded(node.Implements, 1, 2)

            If node.Kind = SyntaxKind.InterfaceBlock Then
                AddLinebreaksAfterElementsIfNeeded(node.Members, 2, 2)
            Else
                AddLinebreaksAfterElementsIfNeeded(node.Members, 2, 1)
            End If
        End Sub

        Public Overrides Function VisitMultiLineIfBlock(node As MultiLineIfBlockSyntax) As SyntaxNode

            AddLinebreaksAfterTokenIfNeeded(node.IfStatement.GetLastToken(), 1)

            AddLinebreaksAfterElementsIfNeeded(node.Statements, 1, 1)

            MarkLastStatementIfNeeded(node.Statements)

            Dim previousNode As VBSyntaxNode

            If node.Statements.Any() Then
                previousNode = node.Statements.Last()
            Else
                previousNode = node.IfStatement
            End If

            For Each elseIfBlock In node.ElseIfBlocks
                AddLinebreaksAfterTokenIfNeeded(previousNode.GetLastToken(), 1)
                previousNode = elseIfBlock
            Next

            If node.ElseBlock IsNot Nothing Then
                AddLinebreaksAfterTokenIfNeeded(previousNode.GetLastToken(), 1)
            End If

            If Not lastStatementsInBlocks.Contains(node) Then
                AddLinebreaksAfterTokenIfNeeded(node.EndIfStatement.GetLastToken(), 2)
            Else
                AddLinebreaksAfterTokenIfNeeded(node.EndIfStatement.GetLastToken(), 1)
            End If

            Return MyBase.VisitMultiLineIfBlock(node)
        End Function

        Public Overrides Function VisitEventBlock(node As EventBlockSyntax) As SyntaxNode

            AddLinebreaksAfterTokenIfNeeded(node.EventStatement.GetLastToken, 1)

            AddLinebreaksAfterElementsIfNeeded(node.Accessors, 2, 1)

            Return MyBase.VisitEventBlock(node)
        End Function

        Public Overrides Function VisitDoLoopBlock(node As DoLoopBlockSyntax) As SyntaxNode
            AddLinebreaksAfterTokenIfNeeded(node.DoStatement.GetLastToken(), 1)

            AddLinebreaksAfterElementsIfNeeded(node.Statements, 1, 1)

            MarkLastStatementIfNeeded(node.Statements)

            If lastStatementsInBlocks.Contains(node) Then
                AddLinebreaksAfterTokenIfNeeded(node.LoopStatement.GetLastToken(), 1)
            Else
                AddLinebreaksAfterTokenIfNeeded(node.LoopStatement.GetLastToken(), 2)
            End If

            Return MyBase.VisitDoLoopBlock(node)
        End Function

        Public Overrides Function VisitSyncLockBlock(node As SyncLockBlockSyntax) As SyntaxNode
            AddLinebreaksAfterTokenIfNeeded(node.SyncLockStatement.GetLastToken(), 1)

            AddLinebreaksAfterElementsIfNeeded(node.Statements, 1, 1)

            If lastStatementsInBlocks.Contains(node) Then
                AddLinebreaksAfterTokenIfNeeded(node.EndSyncLockStatement.GetLastToken(), 1)
            Else
                AddLinebreaksAfterTokenIfNeeded(node.EndSyncLockStatement.GetLastToken(), 2)
            End If

            Return MyBase.VisitSyncLockBlock(node)
        End Function

        Public Overrides Function VisitMethodBlock(node As MethodBlockSyntax) As SyntaxNode
            AddLinebreaksAfterTokenIfNeeded(node.Begin.GetLastToken(), 1)

            AddLinebreaksAfterElementsIfNeeded(node.Statements, 1, 1)

            MarkLastStatementIfNeeded(node.Statements)

            Return MyBase.VisitMethodBlock(node)
        End Function

        Public Overrides Function VisitConstructorBlock(node As ConstructorBlockSyntax) As SyntaxNode
            AddLinebreaksAfterTokenIfNeeded(node.Begin.GetLastToken(), 1)

            AddLinebreaksAfterElementsIfNeeded(node.Statements, 1, 1)

            MarkLastStatementIfNeeded(node.Statements)

            Return MyBase.VisitConstructorBlock(node)
        End Function

        Public Overrides Function VisitOperatorBlock(node As OperatorBlockSyntax) As SyntaxNode
            AddLinebreaksAfterTokenIfNeeded(node.Begin.GetLastToken(), 1)

            AddLinebreaksAfterElementsIfNeeded(node.Statements, 1, 1)

            MarkLastStatementIfNeeded(node.Statements)

            Return MyBase.VisitOperatorBlock(node)
        End Function

        Public Overrides Function VisitAccessorBlock(node As AccessorBlockSyntax) As SyntaxNode
            AddLinebreaksAfterTokenIfNeeded(node.Begin.GetLastToken(), 1)

            AddLinebreaksAfterElementsIfNeeded(node.Statements, 1, 1)

            MarkLastStatementIfNeeded(node.Statements)

            Return MyBase.VisitAccessorBlock(node)
        End Function

        ''' <summary>
        ''' Each statement and the begin will be displayed on a separate line. No empty lines.
        ''' </summary>
        Public Overrides Function VisitEnumBlock(node As EnumBlockSyntax) As SyntaxNode
            AddLinebreaksAfterTokenIfNeeded(node.EnumStatement.GetLastToken(), 1)
            AddLinebreaksAfterElementsIfNeeded(node.Members, 1, 1)

            Return MyBase.VisitEnumBlock(node)
        End Function

        Public Overrides Function VisitWhileBlock(node As WhileBlockSyntax) As SyntaxNode
            AddLinebreaksAfterTokenIfNeeded(node.WhileStatement.GetLastToken(), 1)

            AddLinebreaksAfterElementsIfNeeded(node.Statements, 1, 1)

            MarkLastStatementIfNeeded(node.Statements)

            If Not lastStatementsInBlocks.Contains(node) Then
                AddLinebreaksAfterTokenIfNeeded(node.EndWhileStatement.GetLastToken(), 2)
            End If

            Return MyBase.VisitWhileBlock(node)
        End Function

        Public Overrides Function VisitForBlock(node As ForBlockSyntax) As SyntaxNode
            VisitForOrForEachBlock(node)

            Return MyBase.VisitForBlock(node)
        End Function

        Public Overrides Function VisitForEachBlock(node As ForEachBlockSyntax) As SyntaxNode
            VisitForOrForEachBlock(node)

            Return MyBase.VisitForEachBlock(node)
        End Function

        Private Sub VisitForOrForEachBlock(node As ForOrForEachBlockSyntax)
            AddLinebreaksAfterTokenIfNeeded(node.ForOrForEachStatement.GetLastToken(), 1)

            AddLinebreaksAfterElementsIfNeeded(node.Statements, 1, 1)

            MarkLastStatementIfNeeded(node.Statements)

            If node.NextStatement IsNot Nothing Then
                If Not lastStatementsInBlocks.Contains(node) Then
                    AddLinebreaksAfterTokenIfNeeded(node.NextStatement.GetLastToken(), 2)
                Else
                    AddLinebreaksAfterTokenIfNeeded(node.NextStatement.GetLastToken(), 1)
                End If
            End If
        End Sub

        Public Overrides Function VisitUsingBlock(node As UsingBlockSyntax) As SyntaxNode
            AddLinebreaksAfterTokenIfNeeded(node.UsingStatement.GetLastToken(), 1)

            AddLinebreaksAfterElementsIfNeeded(node.Statements, 1, 1)

            MarkLastStatementIfNeeded(node.Statements)

            If Not lastStatementsInBlocks.Contains(node) Then
                AddLinebreaksAfterTokenIfNeeded(node.EndUsingStatement.GetLastToken(), 2)
            Else
                AddLinebreaksAfterTokenIfNeeded(node.EndUsingStatement.GetLastToken(), 1)
            End If

            Return MyBase.VisitUsingBlock(node)
        End Function

        Public Overrides Function VisitWithBlock(node As WithBlockSyntax) As SyntaxNode
            AddLinebreaksAfterTokenIfNeeded(node.WithStatement.GetLastToken(), 1)

            AddLinebreaksAfterElementsIfNeeded(node.Statements, 1, 1)

            MarkLastStatementIfNeeded(node.Statements)

            Return MyBase.VisitWithBlock(node)
        End Function

        Public Overrides Function VisitSelectBlock(node As SelectBlockSyntax) As SyntaxNode
            AddLinebreaksAfterTokenIfNeeded(node.SelectStatement.GetLastToken(), 1)

            If Not lastStatementsInBlocks.Contains(node) Then
                AddLinebreaksAfterTokenIfNeeded(node.EndSelectStatement.GetLastToken(), 2)
            Else
                AddLinebreaksAfterTokenIfNeeded(node.EndSelectStatement.GetLastToken(), 1)
            End If

            Return MyBase.VisitSelectBlock(node)
        End Function

        Public Overrides Function VisitCaseBlock(node As CaseBlockSyntax) As SyntaxNode
            AddLinebreaksAfterTokenIfNeeded(node.Begin.GetLastToken(), 1)

            AddLinebreaksAfterElementsIfNeeded(node.Statements, 1, 1)

            Dim result = MyBase.VisitCaseBlock(node)
            indentationDepth -= 1

            Return result
        End Function

        Public Overrides Function VisitTryBlock(node As TryBlockSyntax) As SyntaxNode

            AddLinebreaksAfterTokenIfNeeded(node.TryStatement.GetLastToken(), 1)

            AddLinebreaksAfterElementsIfNeeded(node.Statements, 1, 1)

            MarkLastStatementIfNeeded(node.Statements)

            If Not lastStatementsInBlocks.Contains(node) Then
                AddLinebreaksAfterTokenIfNeeded(node.EndTryStatement.GetLastToken(), 2)
            Else
                AddLinebreaksAfterTokenIfNeeded(node.EndTryStatement.GetLastToken(), 1)
            End If

            Return MyBase.VisitTryBlock(node)
        End Function

        Public Overrides Function VisitCatchBlock(node As CatchBlockSyntax) As SyntaxNode
            AddLinebreaksAfterTokenIfNeeded(node.CatchStatement.GetLastToken(), 1)

            AddLinebreaksAfterElementsIfNeeded(node.Statements, 1, 1)

            Return MyBase.VisitCatchBlock(node)
        End Function

        Public Overrides Function VisitFinallyBlock(node As FinallyBlockSyntax) As SyntaxNode
            AddLinebreaksAfterTokenIfNeeded(node.FinallyStatement.GetLastToken(), 1)

            AddLinebreaksAfterElementsIfNeeded(node.Statements, 1, 1)

            MarkLastStatementIfNeeded(node.Statements)

            Return MyBase.VisitFinallyBlock(node)
        End Function

        Public Overrides Function VisitPropertyBlock(node As PropertyBlockSyntax) As SyntaxNode
            AddLinebreaksAfterTokenIfNeeded(node.PropertyStatement.GetLastToken(), 1)

            AddLinebreaksAfterElementsIfNeeded(node.Accessors, 2, 1)

            Return MyBase.VisitPropertyBlock(node)
        End Function

        Public Overrides Function VisitElseBlock(node As ElseBlockSyntax) As SyntaxNode
            AddLinebreaksAfterTokenIfNeeded(node.ElseStatement.GetLastToken(), 1)

            AddLinebreaksAfterElementsIfNeeded(node.Statements, 1, 1)

            MarkLastStatementIfNeeded(node.Statements)

            Return MyBase.VisitElseBlock(node)
        End Function

        Public Overrides Function VisitElseIfBlock(node As ElseIfBlockSyntax) As SyntaxNode
            AddLinebreaksAfterTokenIfNeeded(node.ElseIfStatement.GetLastToken(), 1)

            AddLinebreaksAfterElementsIfNeeded(node.Statements, 1, 1)

            MarkLastStatementIfNeeded(node.Statements)

            Return MyBase.VisitElseIfBlock(node)
        End Function

        Public Overrides Function VisitMultiLineLambdaExpression(node As MultiLineLambdaExpressionSyntax) As SyntaxNode
            AddLinebreaksAfterTokenIfNeeded(node.Begin.GetLastToken(), 1)

            ' one statement per line
            AddLinebreaksAfterElementsIfNeeded(node.Statements, 1, 1)

            MarkLastStatementIfNeeded(node.Statements)

            indentationDepth += 1
            Dim result = MyBase.VisitMultiLineLambdaExpression(node)

            Return result
        End Function

        Public Overrides Function VisitConstDirectiveTrivia(node As ConstDirectiveTriviaSyntax) As SyntaxNode
            AddLinebreaksAfterTokenIfNeeded(node.GetLastToken(), 1)

            Return MyBase.VisitConstDirectiveTrivia(node)
        End Function

        Public Overrides Function VisitIfDirectiveTrivia(node As IfDirectiveTriviaSyntax) As SyntaxNode
            AddLinebreaksAfterTokenIfNeeded(node.GetLastToken(), 1)

            Return MyBase.VisitIfDirectiveTrivia(node)
        End Function

        Public Overrides Function VisitElseDirectiveTrivia(node As ElseDirectiveTriviaSyntax) As SyntaxNode
            AddLinebreaksAfterTokenIfNeeded(node.GetLastToken(), 1)

            Return MyBase.VisitElseDirectiveTrivia(node)
        End Function

        Public Overrides Function VisitEndIfDirectiveTrivia(node As EndIfDirectiveTriviaSyntax) As SyntaxNode
            AddLinebreaksAfterTokenIfNeeded(node.GetLastToken(), 1)

            Return MyBase.VisitEndIfDirectiveTrivia(node)
        End Function

        Public Overrides Function VisitRegionDirectiveTrivia(node As RegionDirectiveTriviaSyntax) As SyntaxNode
            AddLinebreaksAfterTokenIfNeeded(node.GetLastToken(), 1)

            Return MyBase.VisitRegionDirectiveTrivia(node)
        End Function

        Public Overrides Function VisitEndRegionDirectiveTrivia(node As EndRegionDirectiveTriviaSyntax) As SyntaxNode
            AddLinebreaksAfterTokenIfNeeded(node.GetLastToken(), 1)

            Return MyBase.VisitEndRegionDirectiveTrivia(node)
        End Function

        Public Overrides Function VisitExternalSourceDirectiveTrivia(node As ExternalSourceDirectiveTriviaSyntax) As SyntaxNode
            AddLinebreaksAfterTokenIfNeeded(node.GetLastToken(), 1)

            Return MyBase.VisitExternalSourceDirectiveTrivia(node)
        End Function

        Public Overrides Function VisitEndExternalSourceDirectiveTrivia(node As EndExternalSourceDirectiveTriviaSyntax) As SyntaxNode
            AddLinebreaksAfterTokenIfNeeded(node.GetLastToken(), 1)

            Return MyBase.VisitEndExternalSourceDirectiveTrivia(node)
        End Function

        Public Overrides Function VisitExternalChecksumDirectiveTrivia(node As ExternalChecksumDirectiveTriviaSyntax) As SyntaxNode
            AddLinebreaksAfterTokenIfNeeded(node.GetLastToken(), 1)

            Return MyBase.VisitExternalChecksumDirectiveTrivia(node)
        End Function

        Public Overrides Function VisitEnableWarningDirectiveTrivia(node As EnableWarningDirectiveTriviaSyntax) As SyntaxNode
            AddLinebreaksAfterTokenIfNeeded(node.GetLastToken(), 1)

            Return MyBase.VisitEnableWarningDirectiveTrivia(node)
        End Function

        Public Overrides Function VisitDisableWarningDirectiveTrivia(node As DisableWarningDirectiveTriviaSyntax) As SyntaxNode
            AddLinebreaksAfterTokenIfNeeded(node.GetLastToken(), 1)

            Return MyBase.VisitDisableWarningDirectiveTrivia(node)
        End Function

        Public Overrides Function VisitReferenceDirectiveTrivia(node As ReferenceDirectiveTriviaSyntax) As SyntaxNode
            AddLinebreaksAfterTokenIfNeeded(node.GetLastToken(), 1)

            Return MyBase.VisitReferenceDirectiveTrivia(node)
        End Function

        Public Overrides Function VisitBadDirectiveTrivia(node As BadDirectiveTriviaSyntax) As SyntaxNode
            AddLinebreaksAfterTokenIfNeeded(node.GetLastToken(), 1)

            Return MyBase.VisitBadDirectiveTrivia(node)
        End Function

        Public Overrides Function VisitAttributeList(node As AttributeListSyntax) As SyntaxNode
            ' do not add linebreaks for attributes of parameters or return types
            If node.Parent Is Nothing OrElse
                (node.Parent.Kind <> SyntaxKind.Parameter AndAlso node.Parent.Kind <> SyntaxKind.SimpleAsClause) Then

                AddLinebreaksAfterTokenIfNeeded(node.GetLastToken(), 1)
            End If

            Return MyBase.VisitAttributeList(node)
        End Function

        Public Overrides Function VisitEndBlockStatement(node As EndBlockStatementSyntax) As SyntaxNode
            indentationDepth -= 1

            Return MyBase.VisitEndBlockStatement(node)
        End Function

        Public Overrides Function VisitElseStatement(node As ElseStatementSyntax) As SyntaxNode
            indentationDepth -= 1
            Dim result = MyBase.VisitElseStatement(node)
            indentationDepth += 1

            Return result
        End Function

        Public Overrides Function VisitElseIfStatement(node As ElseIfStatementSyntax) As SyntaxNode
            indentationDepth -= 1
            Dim result = MyBase.VisitElseIfStatement(node)
            indentationDepth += 1

            Return result
        End Function

        Public Overrides Function VisitIfStatement(node As IfStatementSyntax) As SyntaxNode
            Dim result = MyBase.VisitIfStatement(node)
            indentationDepth += 1

            Return result
        End Function

        Public Overrides Function VisitWithStatement(node As WithStatementSyntax) As SyntaxNode
            Dim result = MyBase.VisitWithStatement(node)
            indentationDepth += 1

            Return result
        End Function

        Public Overrides Function VisitSyncLockStatement(node As SyncLockStatementSyntax) As SyntaxNode
            Dim result = MyBase.VisitSyncLockStatement(node)
            indentationDepth += 1

            Return result
        End Function

        Public Overrides Function VisitModuleStatement(node As ModuleStatementSyntax) As SyntaxNode
            Dim result = MyBase.VisitModuleStatement(node)
            indentationDepth += 1

            Return result
        End Function

        Public Overrides Function VisitNamespaceStatement(node As NamespaceStatementSyntax) As SyntaxNode
            Dim result = MyBase.VisitNamespaceStatement(node)
            indentationDepth += 1

            Return result
        End Function

        Public Overrides Function VisitInterfaceStatement(node As InterfaceStatementSyntax) As SyntaxNode
            Dim result = MyBase.VisitInterfaceStatement(node)
            indentationDepth += 1

            Return result
        End Function

        Public Overrides Function VisitStructureStatement(node As StructureStatementSyntax) As SyntaxNode
            Dim result = MyBase.VisitStructureStatement(node)
            indentationDepth += 1

            Return result
        End Function

        Public Overrides Function VisitEnumStatement(node As EnumStatementSyntax) As SyntaxNode
            Dim result = MyBase.VisitEnumStatement(node)
            indentationDepth += 1

            Return result
        End Function

        Public Overrides Function VisitClassStatement(node As ClassStatementSyntax) As SyntaxNode
            Dim result = MyBase.VisitClassStatement(node)
            indentationDepth += 1

            Return result
        End Function

        Public Overrides Function VisitWhileStatement(node As WhileStatementSyntax) As SyntaxNode
            Dim result = MyBase.VisitWhileStatement(node)
            indentationDepth += 1

            Return result
        End Function

        Public Overrides Function VisitDoStatement(node As DoStatementSyntax) As SyntaxNode
            Dim result = MyBase.VisitDoStatement(node)
            indentationDepth += 1

            Return result
        End Function

        Public Overrides Function VisitSelectStatement(node As SelectStatementSyntax) As SyntaxNode
            Dim result = MyBase.VisitSelectStatement(node)
            indentationDepth += 1

            Return result
        End Function

        Public Overrides Function VisitCaseStatement(node As CaseStatementSyntax) As SyntaxNode
            Dim result = MyBase.VisitCaseStatement(node)
            indentationDepth += 1

            Return result
        End Function

        Public Overrides Function VisitLoopStatement(node As LoopStatementSyntax) As SyntaxNode
            indentationDepth -= 1

            Return MyBase.VisitLoopStatement(node)
        End Function

        Public Overrides Function VisitForStatement(node As ForStatementSyntax) As SyntaxNode
            Dim result = MyBase.VisitForStatement(node)
            indentationDepth += 1

            Return result
        End Function

        Public Overrides Function VisitForEachStatement(node As ForEachStatementSyntax) As SyntaxNode
            Dim result = MyBase.VisitForEachStatement(node)
            indentationDepth += 1

            Return result
        End Function

        Public Overrides Function VisitTryStatement(node As TryStatementSyntax) As SyntaxNode
            Dim result = MyBase.VisitTryStatement(node)
            indentationDepth += 1

            Return result
        End Function

        Public Overrides Function VisitCatchStatement(node As CatchStatementSyntax) As SyntaxNode
            indentationDepth -= 1
            Dim result = MyBase.VisitCatchStatement(node)
            indentationDepth += 1

            Return result
        End Function

        Public Overrides Function VisitFinallyStatement(node As FinallyStatementSyntax) As SyntaxNode
            indentationDepth -= 1
            Dim result = MyBase.VisitFinallyStatement(node)
            indentationDepth += 1

            Return result
        End Function

        Public Overrides Function VisitUsingStatement(node As UsingStatementSyntax) As SyntaxNode
            Dim result = MyBase.VisitUsingStatement(node)
            indentationDepth += 1

            Return result
        End Function

        Public Overrides Function VisitMethodStatement(node As MethodStatementSyntax) As SyntaxNode
            Dim result = MyBase.VisitMethodStatement(node)

            ' only indent if this is a block
            If node.Parent IsNot Nothing AndAlso
                (node.Parent.Kind = SyntaxKind.SubBlock OrElse node.Parent.Kind = SyntaxKind.FunctionBlock) Then
                indentationDepth += 1
            End If

            Return result
        End Function

        Public Overrides Function VisitSubNewStatement(node As SubNewStatementSyntax) As SyntaxNode
            Dim result = MyBase.VisitSubNewStatement(node)
            indentationDepth += 1

            Return result
        End Function

        Public Overrides Function VisitAccessorStatement(node As AccessorStatementSyntax) As SyntaxNode
            Dim result = MyBase.VisitAccessorStatement(node)
            indentationDepth += 1

            Return result
        End Function

        Public Overrides Function VisitOperatorStatement(node As OperatorStatementSyntax) As SyntaxNode
            Dim result = MyBase.VisitOperatorStatement(node)
            indentationDepth += 1

            Return result
        End Function

        Public Overrides Function VisitEventStatement(node As EventStatementSyntax) As SyntaxNode
            Dim result = MyBase.VisitEventStatement(node)

            ' only indent if this is a block
            If node.Parent IsNot Nothing AndAlso node.Parent.Kind = SyntaxKind.EventBlock Then
                indentationDepth += 1
            End If

            Return result
        End Function

        Public Overrides Function VisitPropertyStatement(node As PropertyStatementSyntax) As SyntaxNode
            Dim result = MyBase.VisitPropertyStatement(node)

            ' only indent if this is a block
            If node.Parent IsNot Nothing AndAlso node.Parent.Kind = SyntaxKind.PropertyBlock Then
                indentationDepth += 1
            End If

            Return result
        End Function

        Public Overrides Function VisitLabelStatement(node As LabelStatementSyntax) As SyntaxNode
            ' labels are never indented.
            Dim previousIndentationDepth = indentationDepth
            indentationDepth = 0
            Dim result = MyBase.VisitLabelStatement(node)
            indentationDepth = previousIndentationDepth

            Return result
        End Function

        Public Overrides Function VisitNextStatement(node As NextStatementSyntax) As SyntaxNode
            ' next statements with multiple control variables are attached to the inner most for statement,
            ' but it should be indented as it is attached to the outer most one.
            Dim variableCount = node.ControlVariables.Count
            If variableCount = 0 Then
                variableCount = 1
            End If
            indentationDepth -= variableCount

            Return MyBase.VisitNextStatement(node)
        End Function
    End Class
End Namespace