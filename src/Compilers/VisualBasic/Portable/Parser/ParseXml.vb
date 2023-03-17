' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Runtime.CompilerServices
Imports System.Runtime.InteropServices
Imports Microsoft.CodeAnalysis.Syntax.InternalSyntax
Imports InternalSyntaxFactory = Microsoft.CodeAnalysis.VisualBasic.Syntax.InternalSyntax.SyntaxFactory

'
'============ Methods for parsing portions of executable statements ==
'

Namespace Microsoft.CodeAnalysis.VisualBasic.Syntax.InternalSyntax

    Partial Friend Class Parser

        ' File: Parser.cpp
        ' Lines: 13261 - 13261
        ' Expression* .Parser::ParseXmlExpression( [ _Inout_ bool& ErrorInConstruct ] )
        Private Function ParseXmlExpression() As XmlNodeSyntax
            Debug.Assert(CurrentToken.Kind = SyntaxKind.LessThanToken OrElse
                 CurrentToken.Kind = SyntaxKind.LessThanGreaterThanToken OrElse
                 CurrentToken.Kind = SyntaxKind.LessThanSlashToken OrElse
                 CurrentToken.Kind = SyntaxKind.BeginCDataToken OrElse
                 CurrentToken.Kind = SyntaxKind.LessThanExclamationMinusMinusToken OrElse
                 CurrentToken.Kind = SyntaxKind.LessThanQuestionToken, "ParseXmlMarkup called on the wrong token.")

            ' The < token must be reset because a VB scanned < might following trivia attached to it.
            ResetCurrentToken(ScannerState.Content)

            Dim Result As XmlNodeSyntax = Nothing

            If CurrentToken.Kind = SyntaxKind.LessThanQuestionToken Then
                Result = ParseXmlDocument()
            Else
                Result = ParseXmlElement(ScannerState.VB)
            End If

            Result = AdjustTriviaForMissingTokens(Result)
            Result = TransitionFromXmlToVB(Result)
            Return Result
        End Function

        ' File: Parser.cpp
        ' Lines: 13370 - 13370
        ' Expression* .Parser::ParseXmlDocument( [ _Inout_ bool& ErrorInConstruct ] )
        Private Function ParseXmlDocument() As XmlNodeSyntax
            Debug.Assert(CurrentToken.Kind = SyntaxKind.LessThanQuestionToken, "ParseXmlDocument called on wrong token")
            Dim whitespaceChecker As New XmlWhitespaceChecker()

            Dim nextToken = PeekNextToken(ScannerState.Element)

            If nextToken.Kind = SyntaxKind.XmlNameToken AndAlso DirectCast(nextToken, XmlNameTokenSyntax).PossibleKeywordKind = SyntaxKind.XmlKeyword Then

                ' // Read the document version and encoding
                Dim prologue = ParseXmlDeclaration()
                prologue = DirectCast(whitespaceChecker.Visit(prologue), XmlDeclarationSyntax)

                ' // Read PI's and comments

                Dim node As VisualBasicSyntaxNode = prologue
                Dim precedingMisc = ParseXmlMisc(True, whitespaceChecker, node)
                prologue = DirectCast(node, XmlDeclarationSyntax)
                Dim body As XmlNodeSyntax

                ' // Get root element
                ' // This is either a single xml expression hole or an xml element
                Select Case CurrentToken.Kind
                    Case SyntaxKind.LessThanToken
                        body = ParseXmlElement(ScannerState.Misc)

                    Case SyntaxKind.LessThanPercentEqualsToken
                        body = ParseXmlEmbedded(ScannerState.Misc)

                    Case Else
                        ' Expected a root element or an embedded expression
                        body = SyntaxFactory.XmlEmptyElement(DirectCast(HandleUnexpectedToken(SyntaxKind.LessThanToken), PunctuationSyntax),
                                                           SyntaxFactory.XmlName(Nothing, DirectCast(InternalSyntaxFactory.MissingToken(SyntaxKind.XmlNameToken), XmlNameTokenSyntax)),
                                                           Nothing,
                                                           InternalSyntaxFactory.MissingPunctuation(SyntaxKind.SlashGreaterThanToken))

                End Select

                ' // More PI's and comments
                node = body
                Dim followingMisc = ParseXmlMisc(False, whitespaceChecker, node)
                body = DirectCast(node, XmlNodeSyntax)

                Return SyntaxFactory.XmlDocument(prologue, precedingMisc, body, followingMisc)
            Else

                ' // Parse Xml Processing Instruction
                Return ParseXmlProcessingInstruction(ScannerState.VB, whitespaceChecker)

            End If
        End Function

        ' File: Parser.cpp
        ' Lines: 13425 - 13425
        ' XmlDocumentExpression* .Parser::ParseXmlDecl( [ _Inout_ bool& ErrorInConstruct ] )
        Private Function ParseXmlDeclaration() As XmlDeclarationSyntax
            Debug.Assert(CurrentToken.Kind = SyntaxKind.LessThanQuestionToken AndAlso
                              PeekNextToken(ScannerState.Element).Kind = SyntaxKind.XmlNameToken AndAlso
                              DirectCast(PeekNextToken(ScannerState.Element), XmlNameTokenSyntax).PossibleKeywordKind = SyntaxKind.XmlKeyword, "ParseXmlDecl called on the wrong token.")

            Dim beginPrologue = DirectCast(CurrentToken, PunctuationSyntax)
            GetNextToken(ScannerState.Element)

            Dim nameToken As XmlNameTokenSyntax = Nothing
            VerifyExpectedToken(SyntaxKind.XmlNameToken, nameToken, ScannerState.Element)

            Dim encodingIndex = 0
            Dim standaloneIndex = 0
            Dim foundVersion = False
            Dim foundEncoding = False
            Dim foundStandalone = False
            Dim nodes(3) As VisualBasicSyntaxNode
            Dim i As Integer = 0

            nodes(i) = _scanner.MakeKeyword(nameToken)
            i += 1

            Do
                Dim nextOption As XmlDeclarationOptionSyntax

                Select Case CurrentToken.Kind
                    Case SyntaxKind.XmlNameToken
                        Dim optionName = DirectCast(CurrentToken, XmlNameTokenSyntax)

                        Select Case optionName.ToString
                            Case "version"
                                nextOption = ParseXmlDeclarationOption()
                                If foundVersion Then
                                    nextOption = ReportSyntaxError(nextOption, ERRID.ERR_DuplicateXmlAttribute, optionName.ToString)
                                    nodes(i - 1) = nodes(i - 1).AddTrailingSyntax(nextOption)
                                    Exit Select
                                End If

                                foundVersion = True
                                Debug.Assert(i = 1)

                                If foundEncoding OrElse foundStandalone Then
                                    nextOption = ReportSyntaxError(nextOption, ERRID.ERR_VersionMustBeFirstInXmlDecl, "", "", optionName.ToString)
                                End If

                                If nextOption.Value.TextTokens.Node Is Nothing OrElse nextOption.Value.TextTokens.Node.ToFullString <> "1.0" Then
                                    nextOption = ReportSyntaxError(nextOption, ERRID.ERR_InvalidAttributeValue1, "1.0")
                                End If

                                nodes(i) = nextOption
                                i += 1

                            Case "encoding"
                                nextOption = ParseXmlDeclarationOption()
                                If foundEncoding Then
                                    nextOption = ReportSyntaxError(nextOption, ERRID.ERR_DuplicateXmlAttribute, optionName.ToString)
                                    nodes(i - 1) = nodes(i - 1).AddTrailingSyntax(nextOption)
                                    Exit Select
                                End If

                                foundEncoding = True

                                If foundStandalone Then
                                    nextOption = ReportSyntaxError(nextOption, ERRID.ERR_AttributeOrder, "encoding", "standalone")
                                    nodes(i - 1) = nodes(i - 1).AddTrailingSyntax(nextOption)
                                    Exit Select
                                ElseIf Not foundVersion Then
                                    nodes(i - 1) = nodes(i - 1).AddTrailingSyntax(nextOption)
                                    Exit Select
                                End If

                                Debug.Assert(i = 2)
                                encodingIndex = i
                                nodes(i) = nextOption
                                i += 1

                            Case "standalone"
                                nextOption = ParseXmlDeclarationOption()
                                If foundStandalone Then
                                    nextOption = ReportSyntaxError(nextOption, ERRID.ERR_DuplicateXmlAttribute, optionName.ToString)
                                    nodes(i - 1) = nodes(i - 1).AddTrailingSyntax(nextOption)
                                    Exit Select
                                End If

                                foundStandalone = True

                                If Not foundVersion Then
                                    nodes(i - 1) = nodes(i - 1).AddTrailingSyntax(nextOption)
                                    Exit Select
                                End If

                                Dim stringValue = If(nextOption.Value.TextTokens.Node IsNot Nothing, nextOption.Value.TextTokens.Node.ToFullString, "")
                                If stringValue <> "yes" AndAlso stringValue <> "no" Then
                                    nextOption = ReportSyntaxError(nextOption, ERRID.ERR_InvalidAttributeValue2, "yes", "no")
                                End If

                                Debug.Assert(i = 2 OrElse i = 3)
                                standaloneIndex = i
                                nodes(i) = nextOption
                                i += 1

                            Case Else
                                nextOption = ParseXmlDeclarationOption()
                                nextOption = ReportSyntaxError(nextOption, ERRID.ERR_IllegalAttributeInXmlDecl, "", "", nextOption.Name.ToString)
                                nodes(i - 1) = nodes(i - 1).AddTrailingSyntax(nextOption)

                        End Select

                    Case SyntaxKind.LessThanPercentEqualsToken
                        nextOption = ParseXmlDeclarationOption()
                        nodes(i - 1) = nodes(i - 1).AddTrailingSyntax(nextOption)

                    Case Else
                        Exit Do

                End Select
            Loop

            Dim unexpected As CodeAnalysis.Syntax.InternalSyntax.SyntaxList(Of SyntaxToken) = Nothing
            If CurrentToken.Kind <> SyntaxKind.QuestionGreaterThanToken Then
                unexpected = ResyncAt(ScannerState.Element, {SyntaxKind.EndOfXmlToken,
                                                             SyntaxKind.QuestionGreaterThanToken,
                                                             SyntaxKind.LessThanToken,
                                                             SyntaxKind.LessThanPercentEqualsToken,
                                                             SyntaxKind.LessThanExclamationMinusMinusToken})
            End If

            Dim endPrologue As PunctuationSyntax = Nothing
            VerifyExpectedToken(SyntaxKind.QuestionGreaterThanToken, endPrologue, ScannerState.Content)

            If unexpected.Node IsNot Nothing Then
                endPrologue = endPrologue.AddLeadingSyntax(unexpected, ERRID.ERR_ExpectedXmlName)
            End If

            Debug.Assert(foundVersion = (nodes(1) IsNot Nothing))

            If nodes(1) Is Nothing Then
                Dim version = SyntaxFactory.XmlDeclarationOption(DirectCast(InternalSyntaxFactory.MissingToken(SyntaxKind.XmlNameToken), XmlNameTokenSyntax),
                                                    InternalSyntaxFactory.MissingPunctuation(SyntaxKind.EqualsToken),
                                                    CreateMissingXmlString())
                nodes(1) = ReportSyntaxError(version, ERRID.ERR_MissingVersionInXmlDecl)
            End If

            Return SyntaxFactory.XmlDeclaration(beginPrologue,
                                              TryCast(nodes(0), KeywordSyntax),
                                              TryCast(nodes(1), XmlDeclarationOptionSyntax),
                                              If(encodingIndex = 0, Nothing, TryCast(nodes(encodingIndex), XmlDeclarationOptionSyntax)),
                                              If(standaloneIndex = 0, Nothing, TryCast(nodes(standaloneIndex), XmlDeclarationOptionSyntax)),
                                              endPrologue)
        End Function

        ' File: Parser.cpp
        ' Lines: 13813 - 13813
        ' Expression* .Parser::ParseXmlAttribute( [ bool AllowNameAsExpression ] [ _Inout_ bool& ErrorInConstruct ] )
        Private Function ParseXmlDeclarationOption() As XmlDeclarationOptionSyntax
            Debug.Assert(IsToken(CurrentToken,
                                 SyntaxKind.XmlNameToken,
                                 SyntaxKind.LessThanPercentEqualsToken,
                                 SyntaxKind.EqualsToken,
                                 SyntaxKind.SingleQuoteToken,
                                 SyntaxKind.DoubleQuoteToken),
                             "ParseXmlPrologueOption called on wrong token.")

            Dim result As XmlDeclarationOptionSyntax = Nothing
            Dim name As XmlNameTokenSyntax = Nothing
            Dim equals As PunctuationSyntax = Nothing
            Dim value As XmlStringSyntax = Nothing

            Dim hasPrecedingWhitespace = PrevToken.GetTrailingTrivia.ContainsWhitespaceTrivia() OrElse CurrentToken.GetLeadingTrivia.ContainsWhitespaceTrivia

            VerifyExpectedToken(SyntaxKind.XmlNameToken, name, ScannerState.Element)

            If Not hasPrecedingWhitespace Then
                name = ReportSyntaxError(name, ERRID.ERR_ExpectedXmlWhiteSpace)
            End If

            If CurrentToken.Kind = SyntaxKind.LessThanPercentEqualsToken Then
                ' // <%= expr %>
                Dim exp = ParseXmlEmbedded(ScannerState.Element)
                name = name.AddTrailingSyntax(exp, ERRID.ERR_EmbeddedExpression)
            End If

            Dim skipped As CodeAnalysis.Syntax.InternalSyntax.SyntaxList(Of SyntaxToken) = Nothing
            If Not VerifyExpectedToken(SyntaxKind.EqualsToken, equals, ScannerState.Element) Then
                skipped = ResyncAt(ScannerState.Element,
                                   {SyntaxKind.SingleQuoteToken,
                                    SyntaxKind.DoubleQuoteToken,
                                    SyntaxKind.LessThanPercentEqualsToken,
                                    SyntaxKind.QuestionGreaterThanToken,
                                    SyntaxKind.EndOfXmlToken})

                equals = equals.AddTrailingSyntax(skipped)
            End If

            Select Case CurrentToken.Kind
                Case SyntaxKind.SingleQuoteToken,
                      SyntaxKind.DoubleQuoteToken
                    value = ParseXmlString(ScannerState.Element)

                Case SyntaxKind.LessThanPercentEqualsToken
                    ' // <%= expr %>
                    Dim exp = ParseXmlEmbedded(ScannerState.Element)
                    value = AddLeadingSyntax(CreateMissingXmlString(), exp, ERRID.ERR_EmbeddedExpression)

                Case Else
                    value = CreateMissingXmlString()
            End Select

            result = SyntaxFactory.XmlDeclarationOption(name, equals, value)

            Return result
        End Function

        ' File: Parser.cpp
        ' Lines: 13452 - 13452
        ' ExpressionList** .Parser::ParseXmlMisc( [ _Inout_ ParseTree::ExpressionList** Prev ] [ bool IsProlog ] [ _Inout_ bool& ErrorInConstruct ] )
        Private Function ParseXmlMisc(IsProlog As Boolean, whitespaceChecker As XmlWhitespaceChecker, ByRef outerNode As VisualBasicSyntaxNode) As CodeAnalysis.Syntax.InternalSyntax.SyntaxList(Of XmlNodeSyntax)
            Dim Content = Me._pool.Allocate(Of XmlNodeSyntax)()

            While True
                Dim result As XmlNodeSyntax = Nothing

                Select Case (CurrentToken.Kind)

                    Case SyntaxKind.BadToken
                        Dim badToken = DirectCast(CurrentToken, BadTokenSyntax)
                        Dim skipped As GreenNode
                        If badToken.SubKind = SyntaxSubKind.BeginDocTypeToken Then
                            skipped = ParseXmlDocType(ScannerState.Misc)
                        Else
                            skipped = badToken
                            GetNextToken(ScannerState.Misc)
                        End If
                        Dim count = Content.Count
                        If count > 0 Then
                            Content(count - 1) = Content(count - 1).AddTrailingSyntax(skipped, ERRID.ERR_DTDNotSupported)
                        Else
                            outerNode = outerNode.AddTrailingSyntax(skipped, ERRID.ERR_DTDNotSupported)
                        End If

                    Case SyntaxKind.LessThanExclamationMinusMinusToken
                        result = ParseXmlComment(ScannerState.Misc)

                    Case SyntaxKind.LessThanQuestionToken
                        result = ParseXmlProcessingInstruction(ScannerState.Misc, whitespaceChecker)

                    Case Else
                        Exit While

                End Select

                If result IsNot Nothing Then
                    Content.Add(result)
                End If

            End While

            Dim ContentList = Content.ToList
            Me._pool.Free(Content)

            Return ContentList
        End Function

        Private Function ParseXmlDocType(enclosingState As ScannerState) As GreenNode
            Debug.Assert(CurrentToken.Kind = SyntaxKind.BadToken AndAlso
                         DirectCast(CurrentToken, BadTokenSyntax).SubKind = SyntaxSubKind.BeginDocTypeToken, "ParseDTD called on wrong token.")

            Dim builder = SyntaxListBuilder(Of GreenNode).Create()

            Dim beginDocType = DirectCast(CurrentToken, BadTokenSyntax)
            builder.Add(beginDocType)

            Dim name As XmlNameTokenSyntax = Nothing

            GetNextToken(ScannerState.DocType)
            VerifyExpectedToken(SyntaxKind.XmlNameToken, name, ScannerState.DocType)
            builder.Add(name)

            ParseExternalID(builder)

            ParseInternalSubSet(builder)

            Dim greaterThan As PunctuationSyntax = Nothing
            VerifyExpectedToken(SyntaxKind.GreaterThanToken, greaterThan, enclosingState)
            builder.Add(greaterThan)

            Return builder.ToList().Node
        End Function

        Private Sub ParseExternalID(builder As SyntaxListBuilder(Of GreenNode))

            If CurrentToken.Kind = SyntaxKind.XmlNameToken Then

                Dim name = DirectCast(CurrentToken, XmlNameTokenSyntax)

                Select Case name.ToString
                    Case "SYSTEM"
                        builder.Add(name)
                        GetNextToken(ScannerState.DocType)
                        Dim systemLiteral = ParseXmlString(ScannerState.DocType)
                        builder.Add(systemLiteral)

                    Case "PUBLIC"
                        builder.Add(name)
                        GetNextToken(ScannerState.DocType)
                        Dim publicLiteral = ParseXmlString(ScannerState.DocType)
                        builder.Add(publicLiteral)
                        Dim systemLiteral = ParseXmlString(ScannerState.DocType)
                        builder.Add(systemLiteral)

                End Select
            End If

        End Sub

        Private Sub ParseInternalSubSet(builder As SyntaxListBuilder(Of GreenNode))
            Dim unexpected As CodeAnalysis.Syntax.InternalSyntax.SyntaxList(Of SyntaxToken) = Nothing
            If CurrentToken.Kind <> SyntaxKind.BadToken OrElse DirectCast(CurrentToken, BadTokenSyntax).SubKind <> SyntaxSubKind.OpenBracketToken Then
                unexpected = ResyncAt(ScannerState.DocType, {SyntaxKind.BadToken,
                                                            SyntaxKind.GreaterThanToken,
                                                            SyntaxKind.LessThanToken,
                                                            SyntaxKind.LessThanExclamationMinusMinusToken,
                                                            SyntaxKind.BeginCDataToken,
                                                            SyntaxKind.LessThanPercentEqualsToken,
                                                            SyntaxKind.EndOfXmlToken})

                If unexpected.Node IsNot Nothing Then
                    builder.Add(unexpected.Node)
                End If
            End If

            If CurrentToken.Kind = SyntaxKind.BadToken AndAlso DirectCast(CurrentToken, BadTokenSyntax).SubKind = SyntaxSubKind.OpenBracketToken Then

                'Assume we're on the '['

                builder.Add(CurrentToken)

                GetNextToken(ScannerState.DocType)

                If CurrentToken.Kind = SyntaxKind.BadToken AndAlso DirectCast(CurrentToken, BadTokenSyntax).SubKind = SyntaxSubKind.LessThanExclamationToken Then
                    builder.Add(CurrentToken)
                    GetNextToken(ScannerState.DocType)
                    ParseXmlMarkupDecl(builder)
                End If

                If CurrentToken.Kind <> SyntaxKind.BadToken OrElse DirectCast(CurrentToken, BadTokenSyntax).SubKind <> SyntaxSubKind.CloseBracketToken Then
                    unexpected = ResyncAt(ScannerState.DocType, {SyntaxKind.BadToken,
                                                                SyntaxKind.GreaterThanToken,
                                                                SyntaxKind.LessThanToken,
                                                                SyntaxKind.LessThanExclamationMinusMinusToken,
                                                                SyntaxKind.BeginCDataToken,
                                                                SyntaxKind.LessThanPercentEqualsToken,
                                                                SyntaxKind.EndOfXmlToken})
                    If unexpected.Node IsNot Nothing Then
                        builder.Add(unexpected.Node)
                    End If
                End If

                'Assume we're on the ']'
                builder.Add(CurrentToken)
                GetNextToken(ScannerState.DocType)

            End If

        End Sub

        Private Sub ParseXmlMarkupDecl(builder As SyntaxListBuilder(Of GreenNode))

            Do
                Select Case CurrentToken.Kind
                    Case SyntaxKind.BadToken
                        builder.Add(CurrentToken)
                        Dim badToken = DirectCast(CurrentToken, BadTokenSyntax)
                        GetNextToken(ScannerState.DocType)
                        If badToken.SubKind = SyntaxSubKind.LessThanExclamationToken Then
                            ParseXmlMarkupDecl(builder)
                        End If

                    Case SyntaxKind.LessThanQuestionToken
                        Dim xmlPI = ParseXmlProcessingInstruction(ScannerState.DocType, Nothing)
                        builder.Add(xmlPI)

                    Case SyntaxKind.LessThanExclamationMinusMinusToken
                        Dim xmlComment = ParseXmlComment(ScannerState.DocType)
                        builder.Add(xmlComment)

                    Case SyntaxKind.GreaterThanToken
                        builder.Add(CurrentToken)
                        GetNextToken(ScannerState.DocType)
                        Return

                    Case SyntaxKind.EndOfFileToken,
                         SyntaxKind.EndOfXmlToken
                        Return

                    Case Else
                        builder.Add(CurrentToken)
                        GetNextToken(ScannerState.DocType)
                End Select

            Loop
        End Sub

        ' File: Parser.cpp
        ' Lines: 13624 - 13624
        ' XmlElementExpression* .Parser::ParseXmlElement( [ _In_opt_ ParseTree::XmlElementExpression* Parent ] [ _Inout_ bool& ErrorInConstruct ] )
        Private Function ParseXmlElementStartTag(enclosingState As ScannerState) As XmlNodeSyntax
            Debug.Assert(CurrentToken.Kind = SyntaxKind.LessThanToken, "ParseXmlElement call on wrong token.")

            Dim lessThan As PunctuationSyntax = DirectCast(CurrentToken, PunctuationSyntax)
            GetNextToken(ScannerState.Element)

            ' /* AllowExpr */' /* IsBracketed */' 
            Dim Name = ParseXmlQualifiedName(False, True, ScannerState.Element, ScannerState.Element)

            Dim nameIsFollowedByWhitespace = Name.HasTrailingTrivia

            Dim Attributes = ParseXmlAttributes(Not nameIsFollowedByWhitespace, Name)
            Dim greaterThan As PunctuationSyntax = Nothing
            Dim endEmptyElementToken As PunctuationSyntax = Nothing

            Select Case (CurrentToken.Kind)

                Case SyntaxKind.GreaterThanToken
                    ' Element with content
                    greaterThan = DirectCast(CurrentToken, PunctuationSyntax)
                    GetNextToken(ScannerState.Content)

                    Return SyntaxFactory.XmlElementStartTag(lessThan, Name, Attributes, greaterThan)

                Case SyntaxKind.SlashGreaterThanToken
                    ' Empty element
                    endEmptyElementToken = DirectCast(CurrentToken, PunctuationSyntax)
                    GetNextToken(enclosingState)

                    Return SyntaxFactory.XmlEmptyElement(lessThan, Name, Attributes, endEmptyElementToken)

                Case SyntaxKind.SlashToken
                    ' Looks like an empty element but  / followed by '>' is an error when there is whitespace between the tokens.
                    If PeekNextToken(ScannerState.Element).Kind = SyntaxKind.GreaterThanToken Then

                        Dim divideToken As SyntaxToken = CurrentToken

                        GetNextToken(ScannerState.Element)

                        greaterThan = DirectCast(CurrentToken, PunctuationSyntax)

                        GetNextToken(enclosingState)

                        Dim unexpectedSyntax = New CodeAnalysis.Syntax.InternalSyntax.SyntaxList(Of SyntaxToken)(SyntaxList.List(divideToken, greaterThan))

                        endEmptyElementToken = AddLeadingSyntax(New PunctuationSyntax(SyntaxKind.SlashGreaterThanToken, "", Nothing, Nothing),
                                                                unexpectedSyntax,
                                                                ERRID.ERR_IllegalXmlWhiteSpace)

                        Return SyntaxFactory.XmlEmptyElement(lessThan, Name, Attributes, endEmptyElementToken)
                    Else
                        ' Try to resync to recovery from a bad parse
                        Return ResyncXmlElement(enclosingState, lessThan, Name, Attributes)
                    End If

                Case Else
                    ' Try to resync to recovery from a bad parse
                    Return ResyncXmlElement(enclosingState, lessThan, Name, Attributes)

            End Select

        End Function

        ' File: Parser.cpp
        ' Lines: 13624 - 13624
        ' XmlElementExpression* .Parser::ParseXmlElement( [ _In_opt_ ParseTree::XmlElementExpression* Parent ] [ _Inout_ bool& ErrorInConstruct ] )
        Private Function ParseXmlElement(enclosingState As ScannerState) As XmlNodeSyntax
            Debug.Assert(IsToken(CurrentToken,
                                 SyntaxKind.LessThanToken,
                                 SyntaxKind.LessThanGreaterThanToken,
                                 SyntaxKind.LessThanSlashToken,
                                 SyntaxKind.BeginCDataToken,
                                 SyntaxKind.LessThanExclamationMinusMinusToken,
                                 SyntaxKind.LessThanQuestionToken,
                                 SyntaxKind.LessThanPercentEqualsToken,
                                 SyntaxKind.XmlTextLiteralToken,
                                 SyntaxKind.BadToken),
                             "ParseXmlElement call on wrong token.")

            Dim xml As XmlNodeSyntax = Nothing
            Dim contexts As New List(Of XmlContext)
            Dim endElement As XmlElementEndTagSyntax
            Dim nextState = enclosingState
            Dim whitespaceChecker As New XmlWhitespaceChecker()

            Do

                Select Case CurrentToken.Kind

                    Case SyntaxKind.LessThanToken
                        Dim nextTokenIsSlash As Boolean = PeekNextToken(ScannerState.Element).Kind = SyntaxKind.SlashToken

                        If nextTokenIsSlash Then
                            ' While </ is a single token, parse this as </ and report an error.
                            GoTo LessThanSlashTokenCase
                        End If

                        xml = ParseXmlElementStartTag(nextState)
                        xml = DirectCast(whitespaceChecker.Visit(xml), XmlNodeSyntax)

                        If xml.Kind = SyntaxKind.XmlElementStartTag Then
                            Dim startElement = DirectCast(xml, XmlElementStartTagSyntax)
                            contexts.Push(New XmlContext(_pool, startElement))
                            nextState = ScannerState.Content
                            Continue Do
                        End If

                    Case SyntaxKind.LessThanSlashToken
LessThanSlashTokenCase:
                        endElement = ParseXmlElementEndTag(nextState)
                        endElement = DirectCast(whitespaceChecker.Visit(endElement), XmlElementEndTagSyntax)

                        If contexts.Count > 0 Then
                            xml = CreateXmlElement(contexts, endElement)

                        Else
                            Dim missingLessThan = InternalSyntaxFactory.MissingPunctuation(SyntaxKind.LessThanToken)
                            Dim missingXmlNameToken = DirectCast(InternalSyntaxFactory.MissingToken(SyntaxKind.XmlNameToken), XmlNameTokenSyntax)
                            Dim missingName = SyntaxFactory.XmlName(Nothing, missingXmlNameToken)
                            Dim missingGreaterThan = InternalSyntaxFactory.MissingPunctuation(SyntaxKind.GreaterThanToken)
                            Dim startElement = SyntaxFactory.XmlElementStartTag(missingLessThan, missingName, Nothing, missingGreaterThan)

                            contexts.Push(New XmlContext(_pool, startElement))
                            xml = contexts.Peek.CreateElement(endElement)
                            xml = ReportSyntaxError(xml, ERRID.ERR_XmlEndElementNoMatchingStart)
                            contexts.Pop()
                        End If

                    Case SyntaxKind.LessThanExclamationMinusMinusToken
                        xml = ParseXmlComment(nextState)

                    Case SyntaxKind.LessThanQuestionToken
                        xml = ParseXmlProcessingInstruction(nextState, whitespaceChecker)
                        xml = DirectCast(whitespaceChecker.Visit(xml), XmlProcessingInstructionSyntax)

                    Case SyntaxKind.BeginCDataToken
                        xml = ParseXmlCData(nextState)

                    Case SyntaxKind.LessThanPercentEqualsToken
                        xml = ParseXmlEmbedded(nextState)
                        If contexts.Count = 0 Then
                            xml = ReportSyntaxError(xml, ERRID.ERR_EmbeddedExpression)
                        End If

                    Case SyntaxKind.XmlTextLiteralToken,
                        SyntaxKind.XmlEntityLiteralToken,
                        SyntaxKind.DocumentationCommentLineBreakToken

                        Dim newKind As SyntaxKind
                        Dim textTokens = _pool.Allocate(Of XmlTextTokenSyntax)()
                        Do
                            textTokens.Add(DirectCast(CurrentToken, XmlTextTokenSyntax))
                            GetNextToken(nextState)

                            newKind = CurrentToken.Kind
                        Loop While newKind = SyntaxKind.XmlTextLiteralToken OrElse
                            newKind = SyntaxKind.XmlEntityLiteralToken OrElse
                            newKind = SyntaxKind.DocumentationCommentLineBreakToken

                        Dim textResult = textTokens.ToList
                        _pool.Free(textTokens)
                        xml = SyntaxFactory.XmlText(textResult)

                    Case SyntaxKind.BadToken
                        Dim badToken = DirectCast(CurrentToken, BadTokenSyntax)

                        If badToken.SubKind = SyntaxSubKind.BeginDocTypeToken Then
                            Dim docTypeTrivia = ParseXmlDocType(ScannerState.Element)
                            xml = SyntaxFactory.XmlText(InternalSyntaxFactory.MissingToken(SyntaxKind.XmlTextLiteralToken))
                            xml = xml.AddLeadingSyntax(docTypeTrivia, ERRID.ERR_DTDNotSupported)
                        Else
                            ' Let ParseXmlEndELement do the resync
                            Exit Do
                        End If

                    Case Else
                        ' Let ParseXmlEndELement do the resync
                        Exit Do

                End Select

                If contexts.Count > 0 Then
                    contexts.Peek.Add(xml)
                Else
                    Exit Do
                End If
            Loop

            ' Recover from improperly terminated element.
            ' Close all contexts and return

            If contexts.Count > 0 Then
                Do
                    endElement = ParseXmlElementEndTag(nextState)
                    xml = CreateXmlElement(contexts, endElement)
                    If contexts.Count > 0 Then
                        contexts.Peek().Add(xml)
                    Else
                        Exit Do
                    End If
                Loop
            End If

            ResetCurrentToken(enclosingState)
            Return xml
        End Function

        Private Function CreateXmlElement(contexts As List(Of XmlContext), endElement As XmlElementEndTagSyntax) As XmlNodeSyntax

            Dim i = contexts.MatchEndElement(endElement.Name)
            Dim element As XmlNodeSyntax

            If i >= 0 Then

                ' Close any xml element that was not matched
                Dim last = contexts.Count - 1
                Do While last > i
                    Dim missingEndElement = SyntaxFactory.XmlElementEndTag(DirectCast(HandleUnexpectedToken(SyntaxKind.LessThanSlashToken), PunctuationSyntax),
                                                                 ReportSyntaxError(InternalSyntaxFactory.XmlName(Nothing, SyntaxFactory.XmlNameToken("", SyntaxKind.XmlNameToken, Nothing, Nothing)), ERRID.ERR_ExpectedXmlName),
                                                                 DirectCast(HandleUnexpectedToken(SyntaxKind.GreaterThanToken), PunctuationSyntax))

                    Dim xml = contexts.Peek.CreateElement(missingEndElement, ErrorFactory.ErrorInfo(ERRID.ERR_MissingXmlEndTag))
                    contexts.Pop()
                    If contexts.Count > 0 Then
                        contexts.Peek().Add(xml)
                    Else
                        Exit Do
                    End If
                    last -= 1
                Loop

                If endElement.IsMissing Then
                    element = contexts.Peek.CreateElement(endElement, ErrorFactory.ErrorInfo(ERRID.ERR_MissingXmlEndTag))
                Else
                    element = contexts.Peek.CreateElement(endElement)
                End If

            Else
                ' Not match was found
                ' Just close the current xml element

                'TODO - Consider whether the current element should be closed or create a missing start tag to match this dangling end tag.

                Dim prefix = ""
                Dim colon = ""
                Dim localName = ""

                Dim nameExpr = contexts.Peek().StartElement.Name
                If nameExpr.Kind = SyntaxKind.XmlName Then
                    Dim name = DirectCast(nameExpr, XmlNameSyntax)
                    If name.Prefix IsNot Nothing Then
                        prefix = name.Prefix.Name.Text
                        colon = ":"
                    End If
                    localName = name.LocalName.Text
                End If

                endElement = ReportSyntaxError(endElement, ERRID.ERR_MismatchedXmlEndTag, prefix, colon, localName)
                element = contexts.Peek.CreateElement(endElement, ErrorFactory.ErrorInfo(ERRID.ERR_MissingXmlEndTag))
            End If

            contexts.Pop()
            Return element
        End Function

        Private Function ResyncXmlElement(state As ScannerState, lessThan As PunctuationSyntax, Name As XmlNodeSyntax, attributes As CodeAnalysis.Syntax.InternalSyntax.SyntaxList(Of XmlNodeSyntax)) As XmlNodeSyntax

            Dim unexpectedSyntax = ResyncAt(ScannerState.Element,
                                            {SyntaxKind.SlashGreaterThanToken,
                                             SyntaxKind.GreaterThanToken,
                                             SyntaxKind.LessThanToken,
                                             SyntaxKind.LessThanSlashToken,
                                             SyntaxKind.LessThanPercentEqualsToken,
                                             SyntaxKind.BeginCDataToken,
                                             SyntaxKind.LessThanExclamationMinusMinusToken,
                                             SyntaxKind.LessThanQuestionToken,
                                             SyntaxKind.EndOfXmlToken})

            Dim greaterThan As PunctuationSyntax

            'TODO - Don't add an error if the unexpectedSyntax already has errors.
            Select Case CurrentToken.Kind
                Case SyntaxKind.SlashGreaterThanToken
                    Dim endEmptyElementToken = DirectCast(CurrentToken, PunctuationSyntax)
                    If unexpectedSyntax.Node IsNot Nothing Then
                        endEmptyElementToken = AddLeadingSyntax(endEmptyElementToken, unexpectedSyntax, ERRID.ERR_ExpectedGreater)
                    End If
                    GetNextToken(state)

                    Return SyntaxFactory.XmlEmptyElement(lessThan, Name, attributes, endEmptyElementToken)

                Case SyntaxKind.GreaterThanToken
                    greaterThan = DirectCast(CurrentToken, PunctuationSyntax)
                    GetNextToken(ScannerState.Content)
                    If unexpectedSyntax.Node IsNot Nothing Then
                        greaterThan = AddLeadingSyntax(greaterThan, unexpectedSyntax, ERRID.ERR_ExpectedGreater)
                    End If

                    Return SyntaxFactory.XmlElementStartTag(lessThan, Name, attributes, greaterThan)

                Case Else
                    ' Try to avoid spurious missing '>' error message. Only report error if no skipped text
                    ' and attributes are error free.
                    greaterThan = InternalSyntaxFactory.MissingPunctuation(SyntaxKind.GreaterThanToken)

                    If unexpectedSyntax.Node Is Nothing Then
                        If Not (attributes.Node IsNot Nothing AndAlso attributes.Node.ContainsDiagnostics) Then
                            greaterThan = ReportSyntaxError(greaterThan, ERRID.ERR_ExpectedGreater)
                        End If
                    Else
                        greaterThan = AddLeadingSyntax(greaterThan, unexpectedSyntax, ERRID.ERR_Syntax)
                    End If

                    Return SyntaxFactory.XmlElementStartTag(lessThan, Name, attributes, greaterThan)
            End Select

        End Function

        Private Function ResyncXmlContent() As XmlNodeSyntax
            Dim result As XmlTextSyntax

            Dim unexpectedSyntax = ResyncAt(ScannerState.Content,
                                            {SyntaxKind.LessThanToken,
                                             SyntaxKind.LessThanSlashToken,
                                             SyntaxKind.LessThanPercentEqualsToken,
                                             SyntaxKind.BeginCDataToken,
                                             SyntaxKind.LessThanExclamationMinusMinusToken,
                                             SyntaxKind.LessThanQuestionToken,
                                             SyntaxKind.EndOfXmlToken,
                                             SyntaxKind.XmlTextLiteralToken,
                                             SyntaxKind.XmlEntityLiteralToken})
            Dim currentKind = CurrentToken.Kind
            If currentKind = SyntaxKind.XmlTextLiteralToken OrElse
                currentKind = SyntaxKind.DocumentationCommentLineBreakToken OrElse
                currentKind = SyntaxKind.XmlEntityLiteralToken Then

                result = SyntaxFactory.XmlText(CurrentToken)
                GetNextToken(ScannerState.Content)
            Else
                result = SyntaxFactory.XmlText(HandleUnexpectedToken(SyntaxKind.XmlTextLiteralToken))
            End If

            If result.ContainsDiagnostics Then
                result = AddLeadingSyntax(result, unexpectedSyntax)
            Else
                result = AddLeadingSyntax(result, unexpectedSyntax, ERRID.ERR_Syntax)
            End If
            Return result
        End Function

        ' File: Parser.cpp
        ' Lines: 13695 - 13695
        ' XmlElementExpression* .Parser::ParseXmlEndElement( [ _Inout_ ParseTree::XmlElementExpression* Result ] [ Token* StartToken ] [ _Inout_ bool& ErrorInConstruct ] )
        Private Function ParseXmlElementEndTag(nextState As ScannerState) As XmlElementEndTagSyntax

            Dim beginEndElement As PunctuationSyntax = Nothing
            Dim name As XmlNameSyntax = Nothing
            Dim greaterToken As PunctuationSyntax = Nothing
            Dim unexpected As CodeAnalysis.Syntax.InternalSyntax.SyntaxList(Of SyntaxToken) = Nothing

            If CurrentToken.Kind <> SyntaxKind.LessThanSlashToken Then
                unexpected = ResyncAt(ScannerState.Content,
                                      {SyntaxKind.LessThanToken,
                                      SyntaxKind.LessThanSlashToken,
                                      SyntaxKind.EndOfXmlToken})
            End If

            If Not VerifyExpectedToken(SyntaxKind.LessThanSlashToken, beginEndElement, ScannerState.EndElement) Then

                ' Check for '<' followed by '/'.  This is an error because whitespace is not allowed between the tokens.
                If CurrentToken.Kind = SyntaxKind.LessThanToken Then
                    Dim lessThan = DirectCast(CurrentToken, PunctuationSyntax)
                    Dim slashToken As SyntaxToken = PeekNextToken(ScannerState.EndElement)

                    If slashToken.Kind = SyntaxKind.SlashToken Then
                        If lessThan.HasTrailingTrivia Or slashToken.HasLeadingTrivia Then
                            beginEndElement = AddLeadingSyntax(beginEndElement,
                                SyntaxList.List(lessThan, slashToken),
                                ERRID.ERR_IllegalXmlWhiteSpace)
                        Else
                            beginEndElement = DirectCast(InternalSyntaxFactory.Token(lessThan.GetLeadingTrivia,
                                                                      SyntaxKind.LessThanSlashToken,
                                                                      slashToken.GetTrailingTrivia,
                                                                      lessThan.Text & slashToken.Text),
                                                                  PunctuationSyntax)
                        End If

                        GetNextToken(ScannerState.EndElement)
                        GetNextToken(ScannerState.EndElement)
                    End If
                End If
            End If

            If unexpected.Node IsNot Nothing Then
                If unexpected.Node.ContainsDiagnostics Then
                    beginEndElement = beginEndElement.AddLeadingSyntax(unexpected)
                Else
                    beginEndElement = AddLeadingSyntax(beginEndElement, unexpected, ERRID.ERR_ExpectedLT)
                End If
            End If

            If CurrentToken.Kind = SyntaxKind.XmlNameToken Then
                ' /* AllowExpr */' /* IsBracketed */' 
                name = DirectCast(ParseXmlQualifiedName(False, False, ScannerState.EndElement, ScannerState.EndElement), XmlNameSyntax)
            End If

            VerifyExpectedToken(SyntaxKind.GreaterThanToken, greaterToken, nextState)

            Return SyntaxFactory.XmlElementEndTag(beginEndElement, name, greaterToken)

        End Function

        ' File: Parser.cpp
        ' Lines: 13770 - 13770
        ' ExpressionList* .Parser::ParseXmlAttributes( [ bool AllowNameAsExpression ] [ _Inout_ bool& ErrorInConstruct ] )

        Private Function ParseXmlAttributes(requireLeadingWhitespace As Boolean, xmlElementName As XmlNodeSyntax) As CodeAnalysis.Syntax.InternalSyntax.SyntaxList(Of XmlNodeSyntax)

            Dim Attributes = Me._pool.Allocate(Of XmlNodeSyntax)()

            Do
                Select Case CurrentToken.Kind
                    Case SyntaxKind.XmlNameToken,
                        SyntaxKind.LessThanPercentEqualsToken,
                        SyntaxKind.EqualsToken,
                        SyntaxKind.SingleQuoteToken,
                        SyntaxKind.DoubleQuoteToken

                        Dim attribute = ParseXmlAttribute(requireLeadingWhitespace, AllowNameAsExpression:=True, xmlElementName:=xmlElementName)
                        Debug.Assert(attribute IsNot Nothing)

                        requireLeadingWhitespace = Not attribute.HasTrailingTrivia
                        Attributes.Add(attribute)

                    Case Else
                        Exit Do

                End Select
            Loop

            Dim result = Attributes.ToList
            Me._pool.Free(Attributes)
            Return result
        End Function

        ' File: Parser.cpp
        ' Lines: 13813 - 13813
        ' Expression* .Parser::ParseXmlAttribute( [ bool AllowNameAsExpression ] [ _Inout_ bool& ErrorInConstruct ] )
        Friend Function ParseXmlAttribute(requireLeadingWhitespace As Boolean, AllowNameAsExpression As Boolean, xmlElementName As XmlNodeSyntax) As XmlNodeSyntax
            Debug.Assert(IsToken(CurrentToken,
                                 SyntaxKind.XmlNameToken,
                                 SyntaxKind.LessThanPercentEqualsToken,
                                 SyntaxKind.EqualsToken,
                                 SyntaxKind.SingleQuoteToken,
                                 SyntaxKind.DoubleQuoteToken),
                             "ParseXmlAttribute called on wrong token.")

            Dim Result As XmlNodeSyntax = Nothing

            If CurrentToken.Kind = SyntaxKind.XmlNameToken OrElse
                (AllowNameAsExpression AndAlso CurrentToken.Kind = SyntaxKind.LessThanPercentEqualsToken) OrElse
                CurrentToken.Kind = SyntaxKind.EqualsToken OrElse
                CurrentToken.Kind = SyntaxKind.SingleQuoteToken OrElse
                CurrentToken.Kind = SyntaxKind.DoubleQuoteToken Then

                ' /* AllowExpr */' /* IsBracketed */' 
                Dim Name = ParseXmlQualifiedName(requireLeadingWhitespace, True, ScannerState.Element, ScannerState.Element)

                If CurrentToken.Kind = SyntaxKind.EqualsToken Then

                    Dim equals = DirectCast(CurrentToken, PunctuationSyntax)

                    GetNextToken(ScannerState.Element)

                    Dim value As XmlNodeSyntax = Nothing
                    If CurrentToken.Kind = SyntaxKind.LessThanPercentEqualsToken Then
                        ' // <%= expr %>
                        value = ParseXmlEmbedded(ScannerState.Element)
                        Result = SyntaxFactory.XmlAttribute(Name, equals, value)

                    ElseIf Not Me._scanner.IsScanningXmlDoc OrElse
                                Not TryParseXmlCrefAttributeValue(Name, equals, Result) AndAlso
                                Not TryParseXmlNameAttributeValue(Name, equals, Result, xmlElementName) Then

                        ' Try parsing as a string (quoted or unquoted)
                        value = ParseXmlString(ScannerState.Element)
                        Result = SyntaxFactory.XmlAttribute(Name, equals, value)
                    End If

                ElseIf Name.Kind = SyntaxKind.XmlEmbeddedExpression Then
                    ' // In this case, the Name is some expression which may evaluate to an attribute
                    Result = Name

                Else
                    ' // Names must be followed by an "="

                    Dim value As XmlNodeSyntax
                    If CurrentToken.Kind <> SyntaxKind.SingleQuoteToken AndAlso
                        CurrentToken.Kind <> SyntaxKind.DoubleQuoteToken Then

                        Dim missingQuote = DirectCast(InternalSyntaxFactory.MissingToken(SyntaxKind.SingleQuoteToken), PunctuationSyntax)
                        value = SyntaxFactory.XmlString(missingQuote, Nothing, missingQuote)
                    Else
                        ' Case of quoted string without attribute name
                        ' Try parsing as a string (quoted or unquoted)
                        value = ParseXmlString(ScannerState.Element)
                    End If

                    Result = SyntaxFactory.XmlAttribute(Name, DirectCast(HandleUnexpectedToken(SyntaxKind.EqualsToken), PunctuationSyntax), value)
                End If

            End If

            Return Result
        End Function

        Private Function ElementNameIsOneFromTheList(xmlElementName As XmlNodeSyntax, ParamArray names() As String) As Boolean
            If xmlElementName Is Nothing OrElse xmlElementName.Kind <> SyntaxKind.XmlName Then
                Return False
            End If

            Dim xmlName = DirectCast(xmlElementName, XmlNameSyntax)
            If xmlName.Prefix IsNot Nothing Then
                Return False
            End If

            For Each name In names
                If DocumentationCommentXmlNames.ElementEquals(xmlName.LocalName.Text, name, True) Then
                    Return True
                End If
            Next
            Return False
        End Function

        Private Function TryParseXmlCrefAttributeValue(name As XmlNodeSyntax, equals As PunctuationSyntax, <Out> ByRef crefAttribute As XmlNodeSyntax) As Boolean
            Debug.Assert(Me._scanner.IsScanningXmlDoc)

            If name.Kind <> SyntaxKind.XmlName Then
                Return False
            End If

            Dim xmlName = DirectCast(name, XmlNameSyntax)
            If xmlName.Prefix IsNot Nothing OrElse
                    Not DocumentationCommentXmlNames.AttributeEquals(xmlName.LocalName.Text,
                                                                     DocumentationCommentXmlNames.CrefAttributeName) Then
                Return False
            End If

            ' NOTE: we don't check the parent, seems 'cref' attribute is supported 
            ' NOTE: for any nodes even user-defined ones

            Dim state As ScannerState
            Dim startQuote As PunctuationSyntax

            If CurrentToken.Kind = SyntaxKind.SingleQuoteToken Then
                state = If(CurrentToken.Text = "'"c, ScannerState.SingleQuotedString, ScannerState.SmartSingleQuotedString)
                startQuote = DirectCast(CurrentToken, PunctuationSyntax)
            ElseIf CurrentToken.Kind = SyntaxKind.DoubleQuoteToken Then
                state = If(CurrentToken.Text = """"c, ScannerState.QuotedString, ScannerState.SmartQuotedString)
                startQuote = DirectCast(CurrentToken, PunctuationSyntax)
            Else
                Return False
            End If

            ' If we have any problems with parsing the name we want to restore the scanner and 
            ' fall back to a regular attribute scenario
            Dim restorePoint As Scanner.RestorePoint = Me._scanner.CreateRestorePoint()

            Dim nextToken = Me.PeekNextToken(state)
            If Not (nextToken.Kind = SyntaxKind.XmlTextLiteralToken OrElse nextToken.Kind = SyntaxKind.XmlEntityLiteralToken) Then
                GoTo lFailed
            End If

            Dim text As String = nextToken.Text.Trim()
            If text.Length >= 2 AndAlso text(0) <> ":" AndAlso text(1) = ":" Then
                GoTo lFailed
            End If

            ' The code above is supposed to reflect how Dev11 detects and verifies 'cref' attribute on the node
            ' See also: XmlDocFile.cpp, XMLDocNode::VerifyCRefAttributeOnNode(...)

            ' Eat the quotation mark, note that default context is being used here...
            GetNextToken()

            ' The next portion of attribute value should be parsed as Name

            Dim crefReference As CrefReferenceSyntax

            ' If can be either a VB intrinsic type or a proper optionally qualified name
            ' See also: XmlDocFile.cpp, XMLDocNode::PerformActualBinding
            If SyntaxFacts.IsPredefinedTypeKeyword(Me.CurrentToken.Kind) Then
                Dim type As PredefinedTypeSyntax = SyntaxFactory.PredefinedType(DirectCast(CurrentToken, KeywordSyntax))

                ' We need to move to the next token as ParseName(...) does 
                GetNextToken()

                crefReference = SyntaxFactory.CrefReference(type, Nothing, Nothing)

            Else
                crefReference = Me.TryParseCrefReference()
            End If

            If crefReference Is Nothing Then
                GoTo lFailed
            End If

            ' We need to reset the current token and possibly peeked tokens because those 
            ' were created using default scanner state, but we want to see from this 
            ' point tokens received using custom scanner state saved in 'state'
            Me.ResetCurrentToken(state)

            Do
                Select Case Me.CurrentToken.Kind
                    Case SyntaxKind.SingleQuoteToken,
                         SyntaxKind.DoubleQuoteToken

                        Dim endQuote = DirectCast(CurrentToken, PunctuationSyntax)
                        GetNextToken(ScannerState.Element)
                        crefAttribute = SyntaxFactory.XmlCrefAttribute(xmlName, equals, startQuote, crefReference, endQuote)
                        Return True

                    Case SyntaxKind.XmlTextLiteralToken,
                         SyntaxKind.XmlEntityLiteralToken

                        Dim token As SyntaxToken = CurrentToken

                        If TriviaChecker.HasInvalidTrivia(token) Then
                            GoTo lFailed
                        End If

                        crefReference = crefReference.AddTrailingSyntax(token)
                        crefReference.ClearFlags(GreenNode.NodeFlags.ContainsDiagnostics)
                        GetNextToken(state)
                        Continue Do

                    Case SyntaxKind.EndOfXmlToken,
                         SyntaxKind.EndOfFileToken

                        Dim endQuote = DirectCast(InternalSyntaxFactory.MissingToken(startQuote.Kind), PunctuationSyntax)
                        crefAttribute = SyntaxFactory.XmlCrefAttribute(xmlName, equals, startQuote, crefReference, endQuote)
                        Return True

                End Select
                Exit Do
            Loop

lFailed:
            restorePoint.Restore()
            Me.ResetCurrentToken(ScannerState.Element)
            Return False
        End Function

        Friend Function TryParseCrefReference() As CrefReferenceSyntax
            ' Mandatory name part
            Dim name As TypeSyntax = TryParseCrefOptionallyQualifiedName()
            Debug.Assert(name IsNot Nothing)

            Dim signature As CrefSignatureSyntax = Nothing
            Dim asClause As SimpleAsClauseSyntax = Nothing

            If CurrentToken.Kind = SyntaxKind.OpenParenToken AndAlso name.Kind <> SyntaxKind.PredefinedType Then
                ' Optional signature and As clause
                signature = TryParseCrefReferenceSignature()
                Debug.Assert(signature IsNot Nothing)

                ' NOTE: 'As' clause is only allowed if signature specified
                If CurrentToken.Kind = SyntaxKind.AsKeyword Then
                    Dim asKeyword As KeywordSyntax = DirectCast(CurrentToken, KeywordSyntax)
                    GetNextToken()

                    Dim returnType As TypeSyntax = ParseGeneralType()
                    Debug.Assert(returnType IsNot Nothing)

                    asClause = SyntaxFactory.SimpleAsClause(asKeyword, Nothing, returnType)
                End If
            End If

            Dim result As CrefReferenceSyntax = SyntaxFactory.CrefReference(name, signature, asClause)

            ' Even if there are diagnostics in name we don't report them, they will be 
            ' reported later in Documentation comment binding
            If result.ContainsDiagnostics Then
                result.ClearFlags(GreenNode.NodeFlags.ContainsDiagnostics)
            End If

            Return result
        End Function

        Friend Function TryParseCrefReferenceSignature() As CrefSignatureSyntax
            '('
            Debug.Assert(CurrentToken.Kind = SyntaxKind.OpenParenToken)
            Dim openParen As PunctuationSyntax = DirectCast(CurrentToken, PunctuationSyntax)
            GetNextToken()

            Dim signatureTypes = _pool.AllocateSeparated(Of CrefSignaturePartSyntax)()
            Dim firstType As Boolean = True

            Do
                Dim currToken As SyntaxToken = Me.CurrentToken

                If currToken.Kind <> SyntaxKind.CloseParenToken AndAlso currToken.Kind <> SyntaxKind.CommaToken AndAlso Not firstType Then
                    ' In case we expect ')' or ',' but don't find one, we consider this an end of 
                    ' the signature, add a missing '(' and exit parsing 
                    currToken = InternalSyntaxFactory.MissingToken(SyntaxKind.CloseParenToken)
                End If

                If currToken.Kind = SyntaxKind.CloseParenToken Then
                    ' ')', we are done
                    Dim closeParen As PunctuationSyntax = DirectCast(currToken, PunctuationSyntax)
                    If Not currToken.IsMissing Then
                        ' Only move to the next token if this is a non-missing one
                        GetNextToken()
                    End If

                    Dim typeArguments As CodeAnalysis.Syntax.InternalSyntax.SeparatedSyntaxList(Of CrefSignaturePartSyntax) = signatureTypes.ToList
                    _pool.Free(signatureTypes)

                    Return SyntaxFactory.CrefSignature(openParen, typeArguments, closeParen)
                End If

                If firstType Then
                    firstType = False
                Else
                    Debug.Assert(CurrentToken.Kind = SyntaxKind.CommaToken)
                    signatureTypes.AddSeparator(CurrentToken)
                    GetNextToken()
                End If

                Dim modifier As KeywordSyntax = Nothing
                While CurrentToken.Kind = SyntaxKind.ByValKeyword OrElse CurrentToken.Kind = SyntaxKind.ByRefKeyword
                    If modifier Is Nothing Then
                        ' The first one
                        modifier = DirectCast(CurrentToken, KeywordSyntax)
                    Else
                        ' Add diagnostics for all other modifiers
                        modifier = modifier.AddTrailingSyntax(CurrentToken, ERRID.ERR_InvalidParameterSyntax)
                    End If

                    GetNextToken()
                End While

                Dim typeName As TypeSyntax = ParseGeneralType(allowEmptyGenericArguments:=False)
                Debug.Assert(typeName IsNot Nothing)

                signatureTypes.Add(SyntaxFactory.CrefSignaturePart(modifier, typeName))
            Loop

            Throw ExceptionUtilities.Unreachable
        End Function

        Friend Function TryParseCrefOperatorName() As CrefOperatorReferenceSyntax
            Dim operatorKeyword As KeywordSyntax = DirectCast(CurrentToken, KeywordSyntax)
            Debug.Assert(operatorKeyword.Kind = SyntaxKind.OperatorKeyword)
            GetNextToken()

            Dim keyword As KeywordSyntax = Nothing
            Dim operatorToken As SyntaxToken

            If TryTokenAsContextualKeyword(CurrentToken, keyword) Then
                operatorToken = keyword
            Else
                operatorToken = CurrentToken
            End If

            Dim operatorKind As SyntaxKind = operatorToken.Kind

            If SyntaxFacts.IsOperatorStatementOperatorToken(operatorKind) Then
                GetNextToken()
            Else
                operatorToken = ReportSyntaxError(InternalSyntaxFactory.MissingToken(SyntaxKind.PlusToken), ERRID.ERR_UnknownOperator)
            End If

            Return SyntaxFactory.CrefOperatorReference(operatorKeyword, operatorToken)
        End Function

        Friend Function TryParseCrefOptionallyQualifiedName() As TypeSyntax
            Dim result As NameSyntax = Nothing

            ' Parse head: Either a GlobalName or a SimpleName.
            If CurrentToken.Kind = SyntaxKind.GlobalKeyword Then
                result = SyntaxFactory.GlobalName(DirectCast(CurrentToken, KeywordSyntax))
                GetNextToken()

            ElseIf CurrentToken.Kind = SyntaxKind.ObjectKeyword Then
                ' Dev11 treats type 'object' quite in a weird way, thus [cref="object"] and 
                ' [cref="system.object"] will both be resolved into "T:System.Object", but 
                ' while [cref="system.object.tostring"] is resolved into "M:System.Object.ToString", 
                ' [cref="object.tostring"] produces an error. We fix this in Roslyn
                result = SyntaxFactory.IdentifierName(
                            Me._scanner.MakeIdentifier(
                                DirectCast(Me.CurrentToken, KeywordSyntax)))
                GetNextToken()

            ElseIf CurrentToken.Kind = SyntaxKind.OperatorKeyword Then
                ' Operator reference like 'Operator+(...)' cannot be followed by 'dot'
                Return TryParseCrefOperatorName()

            ElseIf CurrentToken.Kind = SyntaxKind.NewKeyword Then
                ' Constructor reference like 'New(...)' cannot be followed by 'dot'
                Return ParseSimpleName(
                    allowGenericArguments:=False,
                    allowGenericsWithoutOf:=False,
                    disallowGenericArgumentsOnLastQualifiedName:=True,
                    allowKeyword:=True,
                    nonArrayName:=False,
                    allowEmptyGenericArguments:=False,
                    allowNonEmptyGenericArguments:=True)

            Else
                Debug.Assert(Not SyntaxFacts.IsPredefinedTypeKeyword(CurrentToken.Kind))

                result = ParseSimpleName(
                    allowGenericArguments:=True,
                    allowGenericsWithoutOf:=False,
                    disallowGenericArgumentsOnLastQualifiedName:=False,
                    allowKeyword:=False,
                    nonArrayName:=False,
                    allowEmptyGenericArguments:=False,
                    allowNonEmptyGenericArguments:=True)
            End If

            Debug.Assert(result IsNot Nothing)

            ' Parse tail: A sequence of zero or more [dot SimpleName].
            While CurrentToken.Kind = SyntaxKind.DotToken
                Dim dotToken As PunctuationSyntax = DirectCast(CurrentToken, PunctuationSyntax)
                GetNextToken()

                If CurrentToken.Kind = SyntaxKind.OperatorKeyword Then
                    ' Operator reference like 'Clazz.Operator+(...)' cannot be followed by 'dot'
                    Dim operatorReference As CrefOperatorReferenceSyntax = TryParseCrefOperatorName()
                    Debug.Assert(operatorReference IsNot Nothing)
                    Return SyntaxFactory.QualifiedCrefOperatorReference(result, dotToken, operatorReference)

                Else
                    Dim simpleName As SimpleNameSyntax =
                        ParseSimpleName(
                            allowGenericArguments:=True,
                            allowGenericsWithoutOf:=False,
                            disallowGenericArgumentsOnLastQualifiedName:=False,
                            allowKeyword:=True,
                            nonArrayName:=False,
                            allowEmptyGenericArguments:=False,
                            allowNonEmptyGenericArguments:=True)

                    result = SyntaxFactory.QualifiedName(result, dotToken, simpleName)
                End If
            End While

            Return result
        End Function

        Private Function TryParseXmlNameAttributeValue(name As XmlNodeSyntax, equals As PunctuationSyntax, <Out> ByRef nameAttribute As XmlNodeSyntax, xmlElementName As XmlNodeSyntax) As Boolean
            Debug.Assert(Me._scanner.IsScanningXmlDoc)

            If name.Kind <> SyntaxKind.XmlName Then
                Return False
            End If

            Dim xmlName = DirectCast(name, XmlNameSyntax)
            If xmlName.Prefix IsNot Nothing OrElse
                    Not DocumentationCommentXmlNames.AttributeEquals(xmlName.LocalName.Text,
                                                                     DocumentationCommentXmlNames.NameAttributeName) Then
                Return False
            End If

            If Not ElementNameIsOneFromTheList(xmlElementName,
                                             DocumentationCommentXmlNames.ParameterElementName,
                                             DocumentationCommentXmlNames.ParameterReferenceElementName,
                                             DocumentationCommentXmlNames.TypeParameterElementName,
                                             DocumentationCommentXmlNames.TypeParameterReferenceElementName) Then
                Return False
            End If

            Dim state As ScannerState
            Dim startQuote As PunctuationSyntax

            If CurrentToken.Kind = SyntaxKind.SingleQuoteToken Then
                state = If(CurrentToken.Text = "'"c, ScannerState.SingleQuotedString, ScannerState.SmartSingleQuotedString)
                startQuote = DirectCast(CurrentToken, PunctuationSyntax)
            ElseIf CurrentToken.Kind = SyntaxKind.DoubleQuoteToken Then
                state = If(CurrentToken.Text = """"c, ScannerState.QuotedString, ScannerState.SmartQuotedString)
                startQuote = DirectCast(CurrentToken, PunctuationSyntax)
            Else
                Return False
            End If

            ' If we have any problems with parsing the name we want to restore the scanner and 
            ' fall back to a regular attribute scenario
            Dim restorePoint As Scanner.RestorePoint = Me._scanner.CreateRestorePoint()

            ' Eat the quotation mark, note that default context is being used here...
            GetNextToken()

            Dim identToken As SyntaxToken = CurrentToken
            If identToken.Kind <> SyntaxKind.IdentifierToken Then
                If identToken.IsKeyword Then
                    identToken = Me._scanner.MakeIdentifier(DirectCast(Me.CurrentToken, KeywordSyntax))
                Else
                    GoTo lFailed
                End If
            End If

            If identToken.ContainsDiagnostics() Then
                GoTo lFailed
            End If

            If TriviaChecker.HasInvalidTrivia(identToken) Then
                GoTo lFailed
            End If

            ' Move to the next token which is supposed to be a closing quote
            GetNextToken(state)

            Dim closingToken As SyntaxToken = Me.CurrentToken
            If closingToken.Kind = SyntaxKind.SingleQuoteToken OrElse closingToken.Kind = SyntaxKind.DoubleQuoteToken Then
                Dim endQuote = DirectCast(CurrentToken, PunctuationSyntax)
                GetNextToken(ScannerState.Element)
                nameAttribute = SyntaxFactory.XmlNameAttribute(xmlName,
                                                               equals,
                                                               startQuote,
                                                               SyntaxFactory.IdentifierName(DirectCast(identToken, IdentifierTokenSyntax)),
                                                               endQuote)
                Return True
            End If

lFailed:
            restorePoint.Restore()
            Me.ResetCurrentToken(ScannerState.Element)
            Return False
        End Function

        ''' <summary>
        ''' Checks if the resulting Cref or Name attribute value has valid trivia
        ''' Note, this may be applicable not only to regular trivia, but also to syntax 
        ''' nodes added to trivia when the parser was recovering from errors
        ''' </summary>
        Private Class TriviaChecker

            Private Sub New()
            End Sub

            Public Shared Function HasInvalidTrivia(node As GreenNode) As Boolean
                Return SyntaxNodeOrTokenHasInvalidTrivia(node)
            End Function

            Private Shared Function SyntaxNodeOrTokenHasInvalidTrivia(node As GreenNode) As Boolean
                If node IsNot Nothing Then
                    Dim token As SyntaxToken = TryCast(node, SyntaxToken)
                    If token IsNot Nothing Then
                        If IsInvalidTrivia(token.GetLeadingTrivia) OrElse IsInvalidTrivia(token.GetTrailingTrivia) Then
                            Return True
                        End If

                    ElseIf SyntaxNodeHasInvalidTrivia(node) Then
                        Return True
                    End If
                End If
                Return False
            End Function

            Private Shared Function SyntaxNodeHasInvalidTrivia(node As GreenNode) As Boolean
                For index = 0 To node.SlotCount - 1
                    If SyntaxNodeOrTokenHasInvalidTrivia(node.GetSlot(index)) Then
                        Return True
                    End If
                Next
                Return False
            End Function

            Private Shared Function IsInvalidTrivia(node As GreenNode) As Boolean
                If node IsNot Nothing Then
                    Select Case node.RawKind
                        Case SyntaxKind.List
                            For index = 0 To node.SlotCount - 1
                                If IsInvalidTrivia(node.GetSlot(index)) Then
                                    Return True
                                End If
                            Next

                        Case SyntaxKind.WhitespaceTrivia
                            ' TODO: The following is simplified, need to be revised
                            Dim whitespace As String = DirectCast(node, SyntaxTrivia).Text
                            For Each ch In whitespace
                                If ch <> " "c AndAlso ch <> ChrW(9) Then
                                    Return True
                                End If
                            Next

                        Case SyntaxKind.SkippedTokensTrivia
                            If SyntaxNodeOrTokenHasInvalidTrivia(DirectCast(node, SkippedTokensTriviaSyntax).Tokens.Node) Then
                                Return True
                            End If

                        Case Else
                            Return True

                    End Select

                End If
                Return False
            End Function
        End Class

        ' File: Parser.cpp
        ' Lines: 13931 - 13931
        ' Expression* .Parser::ParseXmlQualifiedName( [ bool AllowExpr ] [ bool IsBracketed ] [ bool IsElementName ] [ _Inout_ bool& ErrorInConstruct ] )
        Private Function ParseXmlQualifiedName(
            requireLeadingWhitespace As Boolean,
            allowExpr As Boolean,
            stateForName As ScannerState,
            nextState As ScannerState
        ) As XmlNodeSyntax
            Select Case (CurrentToken.Kind)

                Case SyntaxKind.XmlNameToken
                    Return ParseXmlQualifiedName(requireLeadingWhitespace, stateForName, nextState)

                Case SyntaxKind.LessThanPercentEqualsToken
                    If allowExpr Then
                        ' // <%= expr %>
                        Return ParseXmlEmbedded(nextState)
                    End If

            End Select

            ResetCurrentToken(nextState)
            Return ReportExpectedXmlName()
        End Function

        Private Function ParseXmlQualifiedName(requireLeadingWhitespace As Boolean, stateForName As ScannerState, nextState As ScannerState) As XmlNodeSyntax

            Dim hasPrecedingWhitespace = requireLeadingWhitespace AndAlso
                (PrevToken.GetTrailingTrivia.ContainsWhitespaceTrivia() OrElse CurrentToken.GetLeadingTrivia.ContainsWhitespaceTrivia)

            Dim localName = DirectCast(CurrentToken, XmlNameTokenSyntax)
            GetNextToken(stateForName)

            If requireLeadingWhitespace AndAlso Not hasPrecedingWhitespace Then
                localName = ReportSyntaxError(localName, ERRID.ERR_ExpectedXmlWhiteSpace)
            End If

            Dim prefix As XmlPrefixSyntax = Nothing

            If CurrentToken.Kind = SyntaxKind.ColonToken Then

                Dim colon As PunctuationSyntax = DirectCast(CurrentToken, PunctuationSyntax)
                GetNextToken(stateForName)
                prefix = SyntaxFactory.XmlPrefix(localName, colon)

                If CurrentToken.Kind = SyntaxKind.XmlNameToken Then

                    localName = DirectCast(CurrentToken, XmlNameTokenSyntax)
                    GetNextToken(stateForName)

                    If colon.HasTrailingTrivia OrElse
                        localName.HasLeadingTrivia Then

                        localName = ReportSyntaxError(localName, ERRID.ERR_ExpectedXmlName)
                    End If

                Else
                    localName = ReportSyntaxError(InternalSyntaxFactory.XmlNameToken("", SyntaxKind.XmlNameToken, Nothing, Nothing), ERRID.ERR_ExpectedXmlName)

                End If

            End If

            Dim name = SyntaxFactory.XmlName(prefix, localName)
            ResetCurrentToken(nextState)
            Return name
        End Function

        ''' <summary>
        ''' Checks if the given <paramref name="node"/> is a colon trivia whose string representation is the COLON (U+003A) character from ASCII range
        ''' (specifically excluding cases when it is the FULLWIDTH COLON (U+FF1A) character).
        ''' See also: http://fileformat.info/info/unicode/char/FF1A
        ''' </summary>
        ''' <param name="node">A VB syntax node to check.</param>
        Private Shared Function IsAsciiColonTrivia(node As VisualBasicSyntaxNode) As Boolean
            Return node.Kind = SyntaxKind.ColonTrivia AndAlso node.ToString() = ":"
        End Function

        Private Function ParseXmlQualifiedNameVB() As XmlNameSyntax

            If Not IsValidXmlQualifiedNameToken(CurrentToken) Then
                Return ReportExpectedXmlName()
            End If

            Dim localName = ToXmlNameToken(CurrentToken)
            GetNextToken(ScannerState.VB)

            Dim prefix As XmlPrefixSyntax = Nothing

            ' Because this token was scanned in VB mode, it may have colon token trivia on the end. Because the colon may
            ' be part of an Xml name remove the colon from the trivia if it exists. Also, get the next token in Element state
            ' so that the colon appears as a normal token. A colon may come after the identifier if and only if it is the only
            ' trivia following the identifier.  If there is any trivia before the colon then the colon should stay as trivia
            ' and be interpreted as a colon token terminator.  If there is any trivia following the colon, this is an error.
            ' Note that only the COLON (U+003A) character, but not the FULLWIDTH COLON (U+FF1A), may be a part of an XML name, 
            ' although they both may be represented by a node with kind SyntaxKind.ColonTrivia.

            Dim trailingTrivia = New CodeAnalysis.Syntax.InternalSyntax.SyntaxList(Of VisualBasicSyntaxNode)(localName.GetTrailingTrivia())
            If trailingTrivia.Count > 0 AndAlso IsAsciiColonTrivia(trailingTrivia(0)) Then

                Debug.Assert(trailingTrivia.Last.Kind = SyntaxKind.ColonTrivia)
                Debug.Assert(CurrentToken.FullWidth = 0)

                ' Remove trailing colon trivia from the identifier
                trailingTrivia = trailingTrivia.GetStartOfTrivia(trailingTrivia.Count - 1)
                localName = SyntaxFactory.XmlNameToken(localName.Text, localName.PossibleKeywordKind, localName.GetLeadingTrivia(), trailingTrivia.Node).WithDiagnostics(localName.GetDiagnostics())

                ' Get the colon as an Xml colon token, then transition back to VB.
                ResetCurrentToken(ScannerState.Element)
                Dim colon = DirectCast(CurrentToken, PunctuationSyntax)
                GetNextToken(ScannerState.Element)
                colon = TransitionFromXmlToVB(colon)

                prefix = SyntaxFactory.XmlPrefix(localName, colon)
                localName = Nothing

                If trailingTrivia.Count = 0 AndAlso Not colon.HasTrailingTrivia() AndAlso
                    IsValidXmlQualifiedNameToken(CurrentToken) Then

                    localName = ToXmlNameToken(CurrentToken)
                    GetNextToken(ScannerState.VB)
                End If

                If localName Is Nothing Then
                    localName = ReportSyntaxError(InternalSyntaxFactory.XmlNameToken("", SyntaxKind.XmlNameToken, Nothing, Nothing), ERRID.ERR_ExpectedXmlName)
                End If
            End If

            Return SyntaxFactory.XmlName(prefix, localName)
        End Function

        Private Shared Function IsValidXmlQualifiedNameToken(token As SyntaxToken) As Boolean
            Return token.Kind = SyntaxKind.IdentifierToken OrElse TryCast(token, KeywordSyntax) IsNot Nothing
        End Function

        Private Function ToXmlNameToken(token As SyntaxToken) As XmlNameTokenSyntax

            If token.Kind = SyntaxKind.IdentifierToken Then
                Dim id = DirectCast(token, IdentifierTokenSyntax)
                Dim name = SyntaxFactory.XmlNameToken(id.Text, id.PossibleKeywordKind, token.GetLeadingTrivia(), token.GetTrailingTrivia())

                ' Xml names should not be escaped
                If id.IsBracketed Then
                    name = ReportSyntaxError(name, ERRID.ERR_ExpectedXmlName)
                Else
                    name = VerifyXmlNameToken(name)
                End If

                Return name

            Else
                ' This is the keyword case
                Debug.Assert(TryCast(token, KeywordSyntax) IsNot Nothing)
                Return SyntaxFactory.XmlNameToken(token.Text, token.Kind, token.GetLeadingTrivia(), token.GetTrailingTrivia())

            End If
        End Function

        Private Shared Function VerifyXmlNameToken(tk As XmlNameTokenSyntax) As XmlNameTokenSyntax
            Dim text = tk.ValueText

            If Not String.IsNullOrEmpty(text) Then
                If Not isStartNameChar(text(0)) Then
                    Dim ch = text(0)
                    Return ReportSyntaxError(tk,
                        ERRID.ERR_IllegalXmlStartNameChar,
                        ch,
                        "0x" & Convert.ToInt32(ch).ToString("X4"))
                End If

                For Each ch In text
                    If Not isNameChar(ch) Then
                        Return ReportSyntaxError(tk,
                            ERRID.ERR_IllegalXmlNameChar,
                            ch,
                            "0x" & Convert.ToInt32(ch).ToString("X4"))
                    End If
                Next
            End If

            Return tk
        End Function

        Friend Function ParseRestOfDocCommentContent(nodesSoFar As CodeAnalysis.Syntax.InternalSyntax.SyntaxList(Of GreenNode)) As CodeAnalysis.Syntax.InternalSyntax.SyntaxList(Of XmlNodeSyntax)
            Dim content = Me._pool.Allocate(Of XmlNodeSyntax)()

            For Each node In nodesSoFar.Nodes
                content.Add(DirectCast(node, XmlNodeSyntax))
            Next

            If CurrentToken.Kind = SyntaxKind.EndOfXmlToken Then
                GetNextToken(ScannerState.Content)

                If CurrentToken.Kind = SyntaxKind.DocumentationCommentLineBreakToken Then
                    Dim tempNodes = ParseXmlContent(ScannerState.Content)
                    Debug.Assert(tempNodes.Nodes.Length = 1)

                    For Each node In tempNodes.Nodes
                        content.Add(node)
                    Next
                End If
            End If

            Dim result = content.ToList
            Me._pool.Free(content)

            Return result
        End Function

        ' File: Parser.cpp
        ' Lines: 14004 - 14004
        ' ExpressionList* .Parser::ParseXmlContent( [ _Inout_ ParseTree::XmlElementExpression* Parent ] [ _Inout_ bool& ErrorInConstruct ] )
        Friend Function ParseXmlContent(state As ScannerState) As CodeAnalysis.Syntax.InternalSyntax.SyntaxList(Of XmlNodeSyntax)
            Debug.Assert(IsToken(CurrentToken,
                                 SyntaxKind.XmlTextLiteralToken,
                                 SyntaxKind.DocumentationCommentLineBreakToken,
                                 SyntaxKind.XmlEntityLiteralToken,
                                 SyntaxKind.LessThanToken,
                                 SyntaxKind.LessThanSlashToken,
                                 SyntaxKind.LessThanExclamationMinusMinusToken,
                                 SyntaxKind.LessThanQuestionToken,
                                 SyntaxKind.LessThanPercentEqualsToken,
                                 SyntaxKind.BeginCDataToken,
                                 SyntaxKind.EndCDataToken,
                                 SyntaxKind.EndOfFileToken,
                                 SyntaxKind.EndOfXmlToken,
                                 SyntaxKind.BadToken),
                             "ParseXmlContent called on wrong token.")

            Dim Content = Me._pool.Allocate(Of XmlNodeSyntax)()
            Dim whitespaceChecker As New XmlWhitespaceChecker()
            Dim xml As XmlNodeSyntax

            Do
                Select Case (CurrentToken.Kind)

                    Case SyntaxKind.LessThanToken
                        xml = ParseXmlElement(ScannerState.Content)

                    Case SyntaxKind.LessThanSlashToken
                        xml = ReportSyntaxError(ParseXmlElementEndTag(ScannerState.Content), ERRID.ERR_XmlEndElementNoMatchingStart)

                    Case SyntaxKind.LessThanExclamationMinusMinusToken
                        xml = ParseXmlComment(ScannerState.Content)

                    Case SyntaxKind.LessThanQuestionToken
                        xml = ParseXmlProcessingInstruction(ScannerState.Content, whitespaceChecker)

                    Case SyntaxKind.BeginCDataToken
                        xml = ParseXmlCData(ScannerState.Content)

                    Case SyntaxKind.LessThanPercentEqualsToken
                        xml = ParseXmlEmbedded(ScannerState.Content)

                    Case SyntaxKind.XmlTextLiteralToken,
                        SyntaxKind.XmlEntityLiteralToken,
                        SyntaxKind.DocumentationCommentLineBreakToken

                        Dim newKind As SyntaxKind
                        Dim textTokens = _pool.Allocate(Of XmlTextTokenSyntax)()
                        Do
                            textTokens.Add(DirectCast(CurrentToken, XmlTextTokenSyntax))
                            GetNextToken(ScannerState.Content)

                            newKind = CurrentToken.Kind
                        Loop While newKind = SyntaxKind.XmlTextLiteralToken OrElse
                            newKind = SyntaxKind.XmlEntityLiteralToken OrElse
                            newKind = SyntaxKind.DocumentationCommentLineBreakToken

                        Dim textResult = textTokens.ToList
                        _pool.Free(textTokens)
                        xml = SyntaxFactory.XmlText(textResult)

                    Case SyntaxKind.EndOfFileToken,
                         SyntaxKind.EndOfXmlToken
                        Exit Do

                    Case SyntaxKind.BadToken
                        Dim badToken = DirectCast(CurrentToken, BadTokenSyntax)

                        If badToken.SubKind = SyntaxSubKind.BeginDocTypeToken Then
                            Dim docTypeTrivia = ParseXmlDocType(ScannerState.Element)
                            xml = SyntaxFactory.XmlText(InternalSyntaxFactory.MissingToken(SyntaxKind.XmlTextLiteralToken))
                            xml = xml.AddLeadingSyntax(docTypeTrivia, ERRID.ERR_DTDNotSupported)
                        Else
                            ' Let resync handle it
                            GoTo TryResync
                        End If

                    Case Else
TryResync:
                        ' when parsing XmlDoc there may not be outer context to take care of garbage
                        If state = ScannerState.Content Then
                            xml = ResyncXmlContent()
                        Else
                            Exit Do
                        End If

                End Select

                Content.Add(xml)
            Loop

            Dim result = Content.ToList
            Me._pool.Free(Content)

            Return result
        End Function

        ' File: Parser.cpp
        ' Lines: 14078 - 14078
        ' Expression* .Parser::ParseXmlPI( [ _Inout_ bool& ErrorInConstruct ] )
        Private Function ParseXmlProcessingInstruction(nextState As ScannerState, whitespaceChecker As XmlWhitespaceChecker) As XmlProcessingInstructionSyntax
            Debug.Assert(CurrentToken.Kind = SyntaxKind.LessThanQuestionToken, "ParseXmlPI called on the wrong token.")

            Dim beginProcessingInstruction = DirectCast(CurrentToken, PunctuationSyntax)

            GetNextToken(ScannerState.Element)

            'TODO - handle whitespace between begin and name. This check is also needed for xml document declaration
            ' Consider adding a special state for the xml document/pi that does not add trivia to tokens.

            Dim name As XmlNameTokenSyntax = Nothing

            'TODO - name has to allow :. Dev10 puts a fully qualified name here.
            If Not VerifyExpectedToken(SyntaxKind.XmlNameToken, name, ScannerState.StartProcessingInstruction) Then

                ' In case there wasn't a name in the PI and the scanner returned another token from the element state,
                ' the current token must be reset to a processing instruction token.
                ResetCurrentToken(ScannerState.StartProcessingInstruction)

            End If

            If name.Text.Length = 3 AndAlso String.Equals(name.Text, "xml", StringComparison.OrdinalIgnoreCase) Then
                name = ReportSyntaxError(name, ERRID.ERR_IllegalProcessingInstructionName, name.Text)
            End If

            Dim textToken As XmlTextTokenSyntax = Nothing
            Dim values = _pool.Allocate(Of XmlTextTokenSyntax)()

            If CurrentToken.Kind = SyntaxKind.XmlTextLiteralToken OrElse CurrentToken.Kind = SyntaxKind.DocumentationCommentLineBreakToken Then
                textToken = DirectCast(CurrentToken, XmlTextTokenSyntax)

                If Not name.IsMissing AndAlso
                    Not name.GetTrailingTrivia.ContainsWhitespaceTrivia() AndAlso
                    Not textToken.GetLeadingTrivia.ContainsWhitespaceTrivia() Then
                    textToken = ReportSyntaxError(textToken, ERRID.ERR_ExpectedXmlWhiteSpace)
                End If

                Do
                    values.Add(textToken)
                    GetNextToken(ScannerState.ProcessingInstruction)
                    If CurrentToken.Kind <> SyntaxKind.XmlTextLiteralToken AndAlso CurrentToken.Kind <> SyntaxKind.DocumentationCommentLineBreakToken Then
                        Exit Do
                    End If
                    textToken = DirectCast(CurrentToken, XmlTextTokenSyntax)
                Loop
            End If

            Dim endProcessingInstruction As PunctuationSyntax = Nothing
            VerifyExpectedToken(SyntaxKind.QuestionGreaterThanToken, endProcessingInstruction, nextState)

            Dim result = SyntaxFactory.XmlProcessingInstruction(beginProcessingInstruction, name, values.ToList, endProcessingInstruction)

            result = DirectCast(whitespaceChecker.Visit(result), XmlProcessingInstructionSyntax)

            _pool.Free(values)

            Return result
        End Function

        ' File: Parser.cpp
        ' Lines: 14119 - 14119
        ' Expression* .Parser::ParseXmlCData( [ _Inout_ bool& ErrorInConstruct ] )
        Private Function ParseXmlCData(nextState As ScannerState) As XmlCDataSectionSyntax
            Debug.Assert(CurrentToken.Kind = SyntaxKind.BeginCDataToken, "ParseXmlCData called on the wrong token.")

            Dim beginCData = DirectCast(CurrentToken, PunctuationSyntax)

            GetNextToken(ScannerState.CData)

            Dim values = _pool.Allocate(Of XmlTextTokenSyntax)()

            Do While CurrentToken.Kind = SyntaxKind.XmlTextLiteralToken OrElse CurrentToken.Kind = SyntaxKind.DocumentationCommentLineBreakToken
                values.Add(DirectCast(CurrentToken, XmlTextTokenSyntax))
                GetNextToken(ScannerState.CData)
            Loop

            Dim endCData As PunctuationSyntax = Nothing
            VerifyExpectedToken(SyntaxKind.EndCDataToken, endCData, nextState)

            Dim result = values.ToList
            _pool.Free(values)

            Return SyntaxFactory.XmlCDataSection(beginCData, result, endCData)
        End Function

        ' File: Parser.cpp
        ' Lines: 14134 - 14134
        ' Expression* .Parser::ParseXmlComment( [ _Inout_ bool& ErrorInConstruct ] )
        Private Function ParseXmlComment(nextState As ScannerState) As XmlNodeSyntax
            Debug.Assert(CurrentToken.Kind = SyntaxKind.LessThanExclamationMinusMinusToken, "ParseXmlComment called on wrong token.")

            Dim beginComment As PunctuationSyntax = DirectCast(CurrentToken, PunctuationSyntax)
            GetNextToken(ScannerState.Comment)

            Dim values = _pool.Allocate(Of XmlTextTokenSyntax)()

            Do While CurrentToken.Kind = SyntaxKind.XmlTextLiteralToken OrElse CurrentToken.Kind = SyntaxKind.DocumentationCommentLineBreakToken
                Dim textToken = DirectCast(CurrentToken, XmlTextTokenSyntax)
                If textToken.Text.Length = 2 AndAlso textToken.Text = "--" Then
                    textToken = ReportSyntaxError(textToken, ERRID.ERR_IllegalXmlCommentChar)
                End If
                values.Add(textToken)
                GetNextToken(ScannerState.Comment)
            Loop

            Dim endComment As PunctuationSyntax = Nothing
            VerifyExpectedToken(SyntaxKind.MinusMinusGreaterThanToken, endComment, nextState)

            Dim result = values.ToList
            _pool.Free(values)

            Return SyntaxFactory.XmlComment(beginComment, result, endComment)
        End Function

        ' File: Parser.cpp
        ' Lines: 14209 - 14209
        ' Expression* .Parser::ParseXmlString( [ _Out_ ParseTree::XmlAttributeExpression* Attribute ] [ _Inout_ bool& ErrorInConstruct ] )
        Friend Function ParseXmlString(nextState As ScannerState) As XmlStringSyntax

            Dim state As ScannerState
            Dim startQuote As PunctuationSyntax

            If CurrentToken.Kind = SyntaxKind.SingleQuoteToken Then
                state = If(CurrentToken.Text = "'"c, ScannerState.SingleQuotedString, ScannerState.SmartSingleQuotedString)
                startQuote = DirectCast(CurrentToken, PunctuationSyntax)
                GetNextToken(state)
            ElseIf CurrentToken.Kind = SyntaxKind.DoubleQuoteToken Then
                state = If(CurrentToken.Text = """"c, ScannerState.QuotedString, ScannerState.SmartQuotedString)
                startQuote = DirectCast(CurrentToken, PunctuationSyntax)
                GetNextToken(state)
            Else
                ' this is not a quote.
                ' Let's parse the stuff as if it is quoted, but complain that quote is missing
                state = ScannerState.UnQuotedString
                startQuote = DirectCast(InternalSyntaxFactory.MissingToken(SyntaxKind.SingleQuoteToken), PunctuationSyntax)
                startQuote = ReportSyntaxError(startQuote, ERRID.ERR_StartAttributeValue)
                ResetCurrentToken(state)
            End If

            Dim list = _pool.Allocate(Of XmlTextTokenSyntax)()
            Do
                Dim kind = CurrentToken.Kind

                Select Case kind
                    Case SyntaxKind.SingleQuoteToken,
                        SyntaxKind.DoubleQuoteToken

                        Dim endQuote = DirectCast(CurrentToken, PunctuationSyntax)

                        GetNextToken(nextState)

                        Dim result = SyntaxFactory.XmlString(startQuote, list.ToList, endQuote)
                        _pool.Free(list)

                        Return result

                    Case SyntaxKind.XmlTextLiteralToken,
                        SyntaxKind.XmlEntityLiteralToken,
                        SyntaxKind.DocumentationCommentLineBreakToken

                        list.Add(DirectCast(CurrentToken, XmlTextTokenSyntax))

                    Case Else
                        ' error. This happens on EndOfText and BadChar. Let the caller deal with this.
                        ' TODO: is this ok?
                        Dim endQuote = HandleUnexpectedToken(startQuote.Kind)

                        Dim result = SyntaxFactory.XmlString(startQuote, list.ToList, DirectCast(endQuote, PunctuationSyntax))
                        _pool.Free(list)

                        Return result
                End Select
                GetNextToken(state)
            Loop
        End Function

        ' File: Parser.cpp
        ' Lines: 14379 - 14379
        ' Expression* .Parser::ParseXmlEmbedded( [ bool AllowEmbedded ] [ _Inout_ bool& ErrorInConstruct ] )
        Private Function ParseXmlEmbedded(enclosingState As ScannerState) As XmlEmbeddedExpressionSyntax
            Debug.Assert(CurrentToken.Kind = SyntaxKind.LessThanPercentEqualsToken, "ParseXmlEmbedded called on wrong token")

            Dim beginXmlEmbedded As PunctuationSyntax = DirectCast(CurrentToken, PunctuationSyntax)
            GetNextToken(enclosingState)
            beginXmlEmbedded = TransitionFromXmlToVB(beginXmlEmbedded)
            TryEatNewLine(ScannerState.VB)

            Dim value = ParseExpressionCore()

            Dim endXmlEmbedded As PunctuationSyntax = Nothing
            If Not TryEatNewLineAndGetToken(SyntaxKind.PercentGreaterThanToken, endXmlEmbedded, createIfMissing:=False, state:=enclosingState) Then
                Dim skippedTokens = Me._pool.Allocate(Of SyntaxToken)()

                ResyncAt(skippedTokens, ScannerState.VB, {SyntaxKind.PercentGreaterThanToken,
                                        SyntaxKind.GreaterThanToken,
                                        SyntaxKind.LessThanToken,
                                        SyntaxKind.LessThanPercentEqualsToken,
                                        SyntaxKind.LessThanQuestionToken,
                                        SyntaxKind.BeginCDataToken,
                                        SyntaxKind.LessThanExclamationMinusMinusToken,
                                        SyntaxKind.LessThanSlashToken})

                If CurrentToken.Kind = SyntaxKind.PercentGreaterThanToken Then
                    endXmlEmbedded = DirectCast(CurrentToken, PunctuationSyntax)
                    GetNextToken(enclosingState)
                Else
                    endXmlEmbedded = DirectCast(HandleUnexpectedToken(SyntaxKind.PercentGreaterThanToken), PunctuationSyntax)
                End If

                Dim unexpectedSyntax = skippedTokens.ToList()
                Me._pool.Free(skippedTokens)

                If unexpectedSyntax.Node IsNot Nothing Then
                    endXmlEmbedded = AddLeadingSyntax(endXmlEmbedded, unexpectedSyntax, ERRID.ERR_Syntax)
                End If
            End If

            Dim result = SyntaxFactory.XmlEmbeddedExpression(beginXmlEmbedded, value, endXmlEmbedded)
            result = AdjustTriviaForMissingTokens(result)
            result = TransitionFromVBToXml(enclosingState, result)
            Return result
        End Function

    End Class

    Friend Class XmlWhitespaceChecker
        Inherits VisualBasicSyntaxRewriter

        <Flags()>
        Friend Enum TriviaCheck
            None = 0
            ProhibitLeadingTrivia = 1
            ProhibitTrailingTrivia = 2
        End Enum

        Friend Structure WhiteSpaceOptions
            Friend _parentKind As SyntaxKind
            Friend _triviaCheck As TriviaCheck
        End Structure

        Private _options As WhiteSpaceOptions

        Public Sub New()
        End Sub

        Public Overrides Function VisitXmlDeclaration(node As XmlDeclarationSyntax) As VisualBasicSyntaxNode
            Dim anyChanges As Boolean = False
            Dim saveOptions = _options

            _options._triviaCheck = TriviaCheck.ProhibitTrailingTrivia
            Dim lessThanQuestionToken = DirectCast(Visit(node.LessThanQuestionToken), PunctuationSyntax)
            If node._lessThanQuestionToken IsNot lessThanQuestionToken Then anyChanges = True

            _options._triviaCheck = TriviaCheck.ProhibitLeadingTrivia
            Dim xmlKeyword = DirectCast(Visit(node.XmlKeyword), KeywordSyntax)
            If node._xmlKeyword IsNot xmlKeyword Then anyChanges = True

            _options = saveOptions

            If anyChanges Then
                Return New XmlDeclarationSyntax(node.Kind, node.GetDiagnostics, node.GetAnnotations, lessThanQuestionToken, xmlKeyword, node.Version, node.Encoding, node.Standalone, node.QuestionGreaterThanToken)
            Else
                Return node
            End If
        End Function

        Public Overrides Function VisitXmlElementStartTag(node As XmlElementStartTagSyntax) As VisualBasicSyntaxNode
            Dim anyChanges As Boolean = False

            Dim saveOptions = _options

            _options._parentKind = SyntaxKind.XmlElementStartTag
            _options._triviaCheck = TriviaCheck.ProhibitTrailingTrivia

            Dim lessThanToken = DirectCast(VisitSyntaxToken(node.LessThanToken), PunctuationSyntax)
            If node.LessThanToken IsNot lessThanToken Then anyChanges = True

            _options._triviaCheck = TriviaCheck.ProhibitLeadingTrivia
            Dim name As XmlNodeSyntax = DirectCast(Visit(node.Name), XmlNodeSyntax)
            If node.Name IsNot name Then anyChanges = True

            _options._triviaCheck = TriviaCheck.None

            Dim attributes = VisitList(node.Attributes)
            If node.Attributes.Node IsNot attributes.Node Then anyChanges = True

            _options = saveOptions

            If anyChanges Then
                Return InternalSyntaxFactory.XmlElementStartTag(lessThanToken, name, attributes, node.GreaterThanToken)
            End If

            Return node
        End Function

        Public Overrides Function VisitXmlEmptyElement(node As XmlEmptyElementSyntax) As VisualBasicSyntaxNode
            Dim anyChanges As Boolean = False

            Dim saveOptions = _options

            _options._parentKind = SyntaxKind.XmlElementStartTag
            _options._triviaCheck = TriviaCheck.ProhibitTrailingTrivia

            Dim lessThanToken = DirectCast(VisitSyntaxToken(node.LessThanToken), PunctuationSyntax)
            If node.LessThanToken IsNot lessThanToken Then anyChanges = True

            _options._triviaCheck = TriviaCheck.ProhibitLeadingTrivia

            Dim name As XmlNodeSyntax = DirectCast(Visit(node.Name), XmlNodeSyntax)
            If node.Name IsNot name Then anyChanges = True

            _options._triviaCheck = TriviaCheck.None

            Dim attributes = VisitList(node.Attributes)
            If node.Attributes.Node IsNot attributes.Node Then anyChanges = True

            _options = saveOptions

            If anyChanges Then
                Return InternalSyntaxFactory.XmlEmptyElement(lessThanToken, name, attributes, node.SlashGreaterThanToken)
            End If

            Return node
        End Function

        Public Overrides Function VisitXmlElementEndTag(node As XmlElementEndTagSyntax) As VisualBasicSyntaxNode
            Dim anyChanges As Boolean = False

            Dim saveOptions = _options

            _options._parentKind = SyntaxKind.XmlElementStartTag
            _options._triviaCheck = TriviaCheck.ProhibitTrailingTrivia

            Dim LessThanSlashToken = DirectCast(VisitSyntaxToken(node.LessThanSlashToken), PunctuationSyntax)
            If node.LessThanSlashToken IsNot LessThanSlashToken Then anyChanges = True

            Dim name As XmlNameSyntax = DirectCast(Visit(node.Name), XmlNameSyntax)
            If node.Name IsNot name Then anyChanges = True

            _options = saveOptions

            If anyChanges Then
                Return InternalSyntaxFactory.XmlElementEndTag(LessThanSlashToken, name, node.GreaterThanToken)
            End If

            Return node
        End Function

        Public Overrides Function VisitXmlProcessingInstruction(node As XmlProcessingInstructionSyntax) As VisualBasicSyntaxNode
            Dim anyChanges As Boolean = False

            Dim saveOptions = _options
            _options._triviaCheck = TriviaCheck.ProhibitTrailingTrivia
            Dim lessThanQuestionToken = DirectCast(VisitSyntaxToken(node.LessThanQuestionToken), PunctuationSyntax)
            If node.LessThanQuestionToken IsNot lessThanQuestionToken Then anyChanges = True

            'Prohibit trivia before pi name
            _options._triviaCheck = TriviaCheck.ProhibitLeadingTrivia
            Dim nameNew As XmlNameTokenSyntax = DirectCast(VisitSyntaxToken(node.Name), XmlNameTokenSyntax)
            If node.Name IsNot nameNew Then anyChanges = True
            _options = saveOptions

            If anyChanges Then
                Return New XmlProcessingInstructionSyntax(node.Kind,
                                                          node.GetDiagnostics,
                                                          node.GetAnnotations,
                                                          lessThanQuestionToken,
                                                          nameNew,
                                                          node.TextTokens.Node,
                                                          node.QuestionGreaterThanToken)
            Else
                Return node
            End If
        End Function

        Public Overrides Function VisitXmlNameAttribute(node As XmlNameAttributeSyntax) As VisualBasicSyntaxNode
            Dim anyChanges As Boolean = False
            ' Only check the name for trivia.

            Dim saveOptions = _options
            _options._parentKind = SyntaxKind.XmlNameAttribute
            Dim nameNew As XmlNameSyntax = DirectCast(Visit(node.Name), XmlNameSyntax)
            If node.Name IsNot nameNew Then anyChanges = True
            _options = saveOptions

            If anyChanges Then
                Return New XmlNameAttributeSyntax(node.Kind, node.GetDiagnostics, node.GetAnnotations, nameNew, node.EqualsToken, node.StartQuoteToken, node.Reference, node.EndQuoteToken)
            Else
                Return node
            End If
        End Function

        Public Overrides Function VisitXmlCrefAttribute(node As XmlCrefAttributeSyntax) As VisualBasicSyntaxNode
            Dim anyChanges As Boolean = False
            ' Only check the name for trivia.

            Dim saveOptions = _options
            _options._parentKind = SyntaxKind.XmlCrefAttribute
            Dim nameNew As XmlNameSyntax = DirectCast(Visit(node.Name), XmlNameSyntax)
            If node.Name IsNot nameNew Then anyChanges = True
            _options = saveOptions

            If anyChanges Then
                Return New XmlCrefAttributeSyntax(node.Kind, node.GetDiagnostics, node.GetAnnotations, nameNew, node.EqualsToken, node.StartQuoteToken, node.Reference, node.EndQuoteToken)
            Else
                Return node
            End If
        End Function

        Public Overrides Function VisitXmlAttribute(node As XmlAttributeSyntax) As VisualBasicSyntaxNode
            Dim anyChanges As Boolean = False
            ' Only check the name for trivia.

            Dim saveOptions = _options
            _options._parentKind = SyntaxKind.XmlAttribute
            Dim nameNew As XmlNodeSyntax = DirectCast(Visit(node.Name), XmlNodeSyntax)
            If node.Name IsNot nameNew Then anyChanges = True
            _options = saveOptions

            If anyChanges Then
                Return New XmlAttributeSyntax(node.Kind, node.GetDiagnostics, node.GetAnnotations, nameNew, node.EqualsToken, node.Value)
            Else
                Return node
            End If
        End Function

        Public Overrides Function VisitXmlBracketedName(node As XmlBracketedNameSyntax) As VisualBasicSyntaxNode
            Dim anyChanges As Boolean = False

            Dim saveOptions = _options

            _options._parentKind = SyntaxKind.XmlBracketedName
            _options._triviaCheck = TriviaCheck.ProhibitTrailingTrivia

            Dim lessThanToken = DirectCast(VisitSyntaxToken(node.LessThanToken), PunctuationSyntax)
            If node.LessThanToken IsNot lessThanToken Then anyChanges = True

            Dim name As XmlNodeSyntax = DirectCast(Visit(node.Name), XmlNodeSyntax)
            If node.Name IsNot name Then anyChanges = True

            _options._triviaCheck = TriviaCheck.ProhibitLeadingTrivia
            Dim greaterThanToken = DirectCast(VisitSyntaxToken(node.GreaterThanToken), PunctuationSyntax)
            If node.GreaterThanToken IsNot greaterThanToken Then anyChanges = True
            _options = saveOptions

            If anyChanges Then
                Return InternalSyntaxFactory.XmlBracketedName(lessThanToken, DirectCast(name, XmlNameSyntax), greaterThanToken)
            Else
                Return node
            End If
        End Function

        Public Overrides Function VisitXmlName(node As XmlNameSyntax) As VisualBasicSyntaxNode
            Dim anyChanges As Boolean = False

            Dim prefix As XmlPrefixSyntax = DirectCast(Visit(node.Prefix), XmlPrefixSyntax)
            If node.Prefix IsNot prefix Then anyChanges = True
            Dim localName As XmlNameTokenSyntax
            Dim saveOptions = _options

            ' Prohibit trivia depending on parent context
            Select Case _options._parentKind
                Case SyntaxKind.XmlAttribute,
                     SyntaxKind.XmlCrefAttribute,
                     SyntaxKind.XmlNameAttribute

                    If node.Prefix IsNot Nothing Then
                        _options._triviaCheck = TriviaCheck.ProhibitLeadingTrivia
                    Else
                        _options._triviaCheck = TriviaCheck.None
                    End If

                Case SyntaxKind.XmlBracketedName
                    _options._triviaCheck = TriviaCheck.ProhibitLeadingTrivia Or TriviaCheck.ProhibitTrailingTrivia

                Case Else
                    _options._triviaCheck = TriviaCheck.ProhibitLeadingTrivia
            End Select

            If _options._triviaCheck <> TriviaCheck.None Then
                localName = DirectCast(VisitSyntaxToken(node.LocalName), XmlNameTokenSyntax)
                If node.LocalName IsNot localName Then anyChanges = True
            Else
                localName = node.LocalName
            End If

            _options = saveOptions

            If anyChanges Then
                Return New XmlNameSyntax(node.Kind, node.GetDiagnostics, node.GetAnnotations, prefix, localName)
            Else
                Return node
            End If
        End Function

        Public Overrides Function VisitXmlPrefix(node As XmlPrefixSyntax) As VisualBasicSyntaxNode
            Dim anyChanges As Boolean = False

            ' Prohibit trailing trivia if prefix is an attribute name and both leading and trailing trivia if prefix is an element name
            Dim saveOptions = _options
            _options._triviaCheck = If(_options._parentKind = SyntaxKind.XmlAttribute, TriviaCheck.ProhibitTrailingTrivia, TriviaCheck.ProhibitLeadingTrivia Or TriviaCheck.ProhibitTrailingTrivia)
            Dim name = DirectCast(VisitSyntaxToken(node.Name), XmlNameTokenSyntax)
            If node.Name IsNot name Then anyChanges = True
            ' Prohibit both leading and trailing trivia around the ':'
            _options._triviaCheck = TriviaCheck.ProhibitLeadingTrivia Or TriviaCheck.ProhibitTrailingTrivia
            Dim colon As PunctuationSyntax = DirectCast(VisitSyntaxToken(node.ColonToken), PunctuationSyntax)
            If node.ColonToken IsNot colon Then anyChanges = True
            _options = saveOptions

            If anyChanges Then
                Return New XmlPrefixSyntax(node.Kind, node.GetDiagnostics, node.GetAnnotations, name, colon)
            Else
                Return node
            End If
        End Function

        Public Overrides Function VisitSyntaxToken(token As SyntaxToken) As SyntaxToken
            If token Is Nothing Then
                Return Nothing
            End If

            Dim anyChanges As Boolean = False
            Dim leadingTrivia As GreenNode = Nothing
            Dim trailingTrivia As GreenNode = Nothing

            ' For whitespace checking, only look at '<', '</', <%= and ':' tokens.
            ' i.e.
            '   < a >
            '   <a :b>
            '   <a: b>
            '   </ a>
            '   <<%=

            Select Case token.Kind
                Case SyntaxKind.XmlNameToken,
                     SyntaxKind.XmlKeyword,
                     SyntaxKind.LessThanToken,
                     SyntaxKind.LessThanSlashToken,
                     SyntaxKind.LessThanQuestionToken,
                     SyntaxKind.LessThanPercentEqualsToken,
                     SyntaxKind.ColonToken

                    leadingTrivia = token.GetLeadingTrivia
                    If (_options._triviaCheck And TriviaCheck.ProhibitLeadingTrivia) = TriviaCheck.ProhibitLeadingTrivia Then
                        Dim newleadingTrivia = VisitList(New CodeAnalysis.Syntax.InternalSyntax.SyntaxList(Of VisualBasicSyntaxNode)(leadingTrivia)).Node
                        If newleadingTrivia IsNot leadingTrivia Then
                            anyChanges = True
                            leadingTrivia = newleadingTrivia
                        End If
                    End If

                    trailingTrivia = token.GetTrailingTrivia
                    If (_options._triviaCheck And TriviaCheck.ProhibitTrailingTrivia) = TriviaCheck.ProhibitTrailingTrivia Then
                        Dim newTrailingTrivia = VisitList(New CodeAnalysis.Syntax.InternalSyntax.SyntaxList(Of VisualBasicSyntaxNode)(trailingTrivia)).Node
                        If newTrailingTrivia IsNot trailingTrivia Then
                            anyChanges = True
                            trailingTrivia = newTrailingTrivia
                        End If
                    End If

            End Select

            If anyChanges Then
                Select Case token.Kind
                    Case SyntaxKind.XmlNameToken

                        Dim node = DirectCast(token, XmlNameTokenSyntax)
                        Return New XmlNameTokenSyntax(node.Kind, node.GetDiagnostics, node.GetAnnotations, node.Text, leadingTrivia, trailingTrivia, node.PossibleKeywordKind)

                    Case SyntaxKind.XmlKeyword

                        Dim node = DirectCast(token, KeywordSyntax)
                        Return New KeywordSyntax(node.Kind, node.GetDiagnostics, node.GetAnnotations, node.Text, leadingTrivia, trailingTrivia)

                    Case SyntaxKind.LessThanToken,
                         SyntaxKind.LessThanSlashToken,
                         SyntaxKind.LessThanQuestionToken,
                         SyntaxKind.LessThanPercentEqualsToken,
                         SyntaxKind.ColonToken

                        Dim node = DirectCast(token, PunctuationSyntax)
                        Return New PunctuationSyntax(node.Kind, node.GetDiagnostics, node.GetAnnotations, node.Text, leadingTrivia, trailingTrivia)
                End Select
            End If

            Return token
        End Function

        Public Overrides Function VisitSyntaxTrivia(trivia As SyntaxTrivia) As SyntaxTrivia
            If trivia.Kind = SyntaxKind.WhitespaceTrivia OrElse trivia.Kind = SyntaxKind.EndOfLineTrivia Then
                Return DirectCast(trivia.AddError(ErrorFactory.ErrorInfo(ERRID.ERR_IllegalXmlWhiteSpace)), SyntaxTrivia)
            End If
            Return trivia
        End Function
    End Class

    Friend Structure XmlContext
        Private ReadOnly _start As XmlElementStartTagSyntax
        Private ReadOnly _content As SyntaxListBuilder(Of XmlNodeSyntax)
        Private ReadOnly _pool As SyntaxListPool

        Public Sub New(pool As SyntaxListPool, start As XmlElementStartTagSyntax)
            _pool = pool
            _start = start
            _content = _pool.Allocate(Of XmlNodeSyntax)()
        End Sub

        Public Sub Add(xml As XmlNodeSyntax)
            _content.Add(xml)
        End Sub

        Public ReadOnly Property StartElement As XmlElementStartTagSyntax
            Get
                Return _start
            End Get
        End Property

        Public Function CreateElement(endElement As XmlElementEndTagSyntax) As XmlNodeSyntax
            Debug.Assert(endElement IsNot Nothing)

            Dim contentList = _content.ToList
            _pool.Free(_content)

            Return InternalSyntaxFactory.XmlElement(_start, contentList, endElement)
        End Function

        Public Function CreateElement(endElement As XmlElementEndTagSyntax, diagnostic As DiagnosticInfo) As XmlNodeSyntax
            Debug.Assert(endElement IsNot Nothing)

            Dim contentList = _content.ToList
            _pool.Free(_content)

            Return InternalSyntaxFactory.XmlElement(DirectCast(_start.AddError(diagnostic), XmlElementStartTagSyntax), contentList, endElement)
        End Function

    End Structure

    Friend Module XmlContextExtensions
        <Extension()>
        Friend Sub Push(this As List(Of XmlContext), context As XmlContext)
            this.Add(context)
        End Sub

        <Extension()>
        Friend Function Pop(this As List(Of XmlContext)) As XmlContext
            Dim last = this.Count - 1
            Dim context = this(last)
            this.RemoveAt(last)
            Return context
        End Function

        <Extension()>
        Friend Function Peek(this As List(Of XmlContext), Optional i As Integer = 0) As XmlContext
            Dim last = this.Count - 1
            Return this(last - i)
        End Function

        <Extension()>
        Friend Function MatchEndElement(this As List(Of XmlContext), name As XmlNameSyntax) As Integer
            Debug.Assert(name Is Nothing OrElse name.Kind = SyntaxKind.XmlName)

            Dim last = this.Count - 1
            If name Is Nothing Then
                ' An empty name matches anything
                Return last
            End If

            Dim i = last
            Do While i >= 0
                Dim context = this(i)
                Dim nameExpr = context.StartElement.Name

                If nameExpr.Kind = SyntaxKind.XmlName Then

                    Dim startName = DirectCast(nameExpr, XmlNameSyntax)

                    If startName.LocalName.Text = name.LocalName.Text Then

                        Dim startPrefix = startName.Prefix
                        Dim endPrefix = name.Prefix

                        If startPrefix Is endPrefix Then
                            Exit Do
                        End If

                        If startPrefix IsNot Nothing AndAlso endPrefix IsNot Nothing Then
                            If startPrefix.Name.Text = endPrefix.Name.Text Then
                                Exit Do
                            End If
                        End If

                    End If
                End If

                i -= 1
            Loop

            Return i
        End Function
    End Module

End Namespace
