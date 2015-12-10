' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

' //
' //============ Methods related to conditional compilation. ==============
' //

' // Parse a line containing a conditional compilation directive.
Imports System.Globalization
Imports InternalSyntaxFactory = Microsoft.CodeAnalysis.VisualBasic.Syntax.InternalSyntax.SyntaxFactory

Namespace Microsoft.CodeAnalysis.VisualBasic.Syntax.InternalSyntax

    Friend Partial Class Parser

        ' File: Parser.cpp
        ' Lines: 18978 - 18978
        ' .Parser::ParseConditionalCompilationStatement( [ bool SkippingMethodBody ] )

        Friend Function ParseConditionalCompilationStatement() As DirectiveTriviaSyntax
            ' # may be actually scanned as a date literal. This is an error.
            If CurrentToken.Kind = SyntaxKind.DateLiteralToken OrElse
                CurrentToken.Kind = SyntaxKind.BadToken Then

                Dim missingHash = InternalSyntaxFactory.MissingPunctuation(SyntaxKind.HashToken)
                missingHash = missingHash.AddLeadingSyntax(New SyntaxList(Of SyntaxToken)(CurrentToken))
                GetNextToken()
                Return (ParseBadDirective(missingHash))
            End If

            Debug.Assert(CurrentToken.Kind = SyntaxKind.HashToken, "Conditional compilation lines start with '#'.")

            ' // First parse the conditional line, then interpret it.

            Dim statement As DirectiveTriviaSyntax = Nothing
            Dim hashToken = DirectCast(CurrentToken, PunctuationSyntax)
            GetNextToken()

            Select Case CurrentToken.Kind

                Case SyntaxKind.ElseKeyword

                    statement = ParseElseDirective(hashToken)

                Case SyntaxKind.IfKeyword

                    statement = ParseIfDirective(hashToken, Nothing)

                Case SyntaxKind.ElseIfKeyword

                    statement = ParseElseIfDirective(hashToken)

                Case SyntaxKind.EndKeyword

                    statement = ParseEndDirective(hashToken)

                Case SyntaxKind.EndIfKeyword

                    statement = ParseAnachronisticEndIfDirective(hashToken)

                Case SyntaxKind.ConstKeyword

                    statement = ParseConstDirective(hashToken)

                Case SyntaxKind.IdentifierToken
                    Select Case DirectCast(CurrentToken, IdentifierTokenSyntax).PossibleKeywordKind

                        Case SyntaxKind.ExternalSourceKeyword
                            statement = ParseExternalSourceDirective(hashToken)

                        Case SyntaxKind.ExternalChecksumKeyword
                            statement = ParseExternalChecksumDirective(hashToken)

                        Case SyntaxKind.RegionKeyword
                            statement = ParseRegionDirective(hashToken)

                        Case SyntaxKind.EnableKeyword, SyntaxKind.DisableKeyword
                            statement = ParseWarningDirective(hashToken)

                        Case SyntaxKind.ReferenceKeyword
                            statement = ParseReferenceDirective(hashToken)

                        Case Else
                            statement = ParseBadDirective(hashToken)
                    End Select

                Case Else
                    statement = ParseBadDirective(hashToken)

            End Select

            Return statement
        End Function

        ' File: Parser.cpp
        ' Lines: 19739 - 19739
        ' Expression* .Parser::ParseConditionalCompilationExpression( [ _Inout_ bool& ErrorInConstruct ] )

        Friend Function ParseConditionalCompilationExpression() As ExpressionSyntax
            Dim PrevEvaluatingConditionCompilationExpressions As Boolean = _evaluatingConditionCompilationExpression
            _evaluatingConditionCompilationExpression = True

            Dim Expr = ParseExpressionCore()

            _evaluatingConditionCompilationExpression = PrevEvaluatingConditionCompilationExpressions

            Return Expr
        End Function

        Private Function ParseElseDirective(hashToken As PunctuationSyntax) As DirectiveTriviaSyntax
            Debug.Assert(CurrentToken.Kind = SyntaxKind.ElseKeyword)

            Dim elseKeyword = DirectCast(CurrentToken, KeywordSyntax)
            GetNextToken()

            If CurrentToken.Kind <> SyntaxKind.IfKeyword Then

                Dim statement = SyntaxFactory.ElseDirectiveTrivia(hashToken, elseKeyword)

                Return statement

            Else
                ' // Accept Else If as a synonym for ElseIf.

                Return ParseIfDirective(hashToken, elseKeyword)
            End If

        End Function

        Private Function ParseElseIfDirective(hashToken As PunctuationSyntax) As DirectiveTriviaSyntax
            Debug.Assert(CurrentToken.Kind = SyntaxKind.ElseIfKeyword)

            Return ParseIfDirective(hashToken, Nothing)

        End Function

        Private Function ParseIfDirective(hashToken As PunctuationSyntax, elseKeyword As KeywordSyntax) As IfDirectiveTriviaSyntax
            Debug.Assert(CurrentToken.Kind = SyntaxKind.IfKeyword OrElse CurrentToken.Kind = SyntaxKind.ElseIfKeyword)

            Dim ifKeyword As KeywordSyntax = DirectCast(CurrentToken, KeywordSyntax)
            GetNextToken()

            Dim expression = ParseConditionalCompilationExpression()

            If expression.ContainsDiagnostics Then
                expression = ResyncAt(expression)
            End If

            Dim thenKeyword As KeywordSyntax = Nothing
            If CurrentToken.Kind = SyntaxKind.ThenKeyword Then
                thenKeyword = DirectCast(CurrentToken, KeywordSyntax)
                GetNextToken()
            End If

            Dim statement As IfDirectiveTriviaSyntax

            If ifKeyword.Kind = SyntaxKind.IfKeyword AndAlso elseKeyword Is Nothing Then
                statement = SyntaxFactory.IfDirectiveTrivia(hashToken, elseKeyword, ifKeyword, expression, thenKeyword)
            Else
                statement = SyntaxFactory.ElseIfDirectiveTrivia(hashToken, elseKeyword, ifKeyword, expression, thenKeyword)
            End If

            Return statement
        End Function

        Private Function ParseEndDirective(hashToken As PunctuationSyntax) As DirectiveTriviaSyntax
            Debug.Assert(CurrentToken.Kind = SyntaxKind.EndKeyword)

            Dim endKeyword = DirectCast(CurrentToken, KeywordSyntax)
            GetNextToken()

            Dim statement As DirectiveTriviaSyntax = Nothing

            If CurrentToken.Kind = SyntaxKind.IfKeyword Then

                Dim ifKeyword = DirectCast(CurrentToken, KeywordSyntax)
                GetNextToken()

                statement = SyntaxFactory.EndIfDirectiveTrivia(hashToken, endKeyword, ifKeyword)

            ElseIf CurrentToken.Kind = SyntaxKind.IdentifierToken Then

                Dim identifier = DirectCast(CurrentToken, IdentifierTokenSyntax)

                If identifier.PossibleKeywordKind = SyntaxKind.RegionKeyword Then
                    Dim regionKeyword = _scanner.MakeKeyword(DirectCast(CurrentToken, IdentifierTokenSyntax))
                    GetNextToken()

                    statement = SyntaxFactory.EndRegionDirectiveTrivia(hashToken, endKeyword, regionKeyword)

                ElseIf identifier.PossibleKeywordKind = SyntaxKind.ExternalSourceKeyword Then
                    Dim externalSourceKeyword = _scanner.MakeKeyword(DirectCast(CurrentToken, IdentifierTokenSyntax))
                    GetNextToken()

                    statement = SyntaxFactory.EndExternalSourceDirectiveTrivia(hashToken, endKeyword, externalSourceKeyword)
                End If

            End If

            If statement Is Nothing Then
                hashToken = hashToken.AddTrailingSyntax(endKeyword, ERRID.ERR_Syntax)
                statement = SyntaxFactory.BadDirectiveTrivia(hashToken)
            End If

            Return statement
        End Function

        Private Function ParseAnachronisticEndIfDirective(hashToken As PunctuationSyntax) As DirectiveTriviaSyntax
            Debug.Assert(CurrentToken.Kind = SyntaxKind.EndIfKeyword)

            Dim endIfKeyword = DirectCast(CurrentToken, KeywordSyntax)
            GetNextToken()

            Dim endKeyword = InternalSyntaxFactory.MissingKeyword(SyntaxKind.EndKeyword)
            endKeyword = endKeyword.AddLeadingSyntax(endIfKeyword, ERRID.ERR_ObsoleteEndIf)

            Dim statement = SyntaxFactory.EndIfDirectiveTrivia(hashToken, endKeyword, InternalSyntaxFactory.MissingKeyword(SyntaxKind.IfKeyword))

            Return statement

        End Function

        Private Function ParseConstDirective(hashToken As PunctuationSyntax) As ConstDirectiveTriviaSyntax
            Debug.Assert(CurrentToken.Kind = SyntaxKind.ConstKeyword)

            Dim constKeyword = DirectCast(CurrentToken, KeywordSyntax)
            GetNextToken()

            Dim Name = ParseIdentifier()

            Dim unexpected As SyntaxList(Of SyntaxToken) = Nothing
            If Name.ContainsDiagnostics Then
                unexpected = ResyncAt({SyntaxKind.EqualsToken})
            End If

            Dim equalsToken As PunctuationSyntax = Nothing
            VerifyExpectedToken(SyntaxKind.EqualsToken, equalsToken)
            If unexpected.Node IsNot Nothing Then
                equalsToken = equalsToken.AddLeadingSyntax(unexpected, ERRID.ERR_Syntax)
            End If

            Dim expression = ParseConditionalCompilationExpression()

            If expression.ContainsDiagnostics Then
                expression = ResyncAt(expression)
            End If

            Dim statement = SyntaxFactory.ConstDirectiveTrivia(hashToken, constKeyword, Name, equalsToken, expression)

            Return statement
        End Function

        Private Function ParseRegionDirective(hashToken As PunctuationSyntax) As RegionDirectiveTriviaSyntax
            Debug.Assert(CurrentToken.Kind = SyntaxKind.IdentifierToken AndAlso DirectCast(CurrentToken, IdentifierTokenSyntax).PossibleKeywordKind = SyntaxKind.RegionKeyword,
                         NameOf(ParseRegionDirective) & " called with wrong token.")

            Dim identifier = DirectCast(CurrentToken, IdentifierTokenSyntax)
            GetNextToken()
            Dim regionKeyword = _scanner.MakeKeyword(identifier)
            Dim title As StringLiteralTokenSyntax = Nothing
            VerifyExpectedToken(SyntaxKind.StringLiteralToken, title)

            Dim statement = SyntaxFactory.RegionDirectiveTrivia(hashToken, regionKeyword, title)
            Return statement
        End Function

        Private Function ParseExternalSourceDirective(hashToken As PunctuationSyntax) As ExternalSourceDirectiveTriviaSyntax
            Debug.Assert(CurrentToken.Kind = SyntaxKind.IdentifierToken AndAlso DirectCast(CurrentToken, IdentifierTokenSyntax).PossibleKeywordKind = SyntaxKind.ExternalSourceKeyword,
                         NameOf(ParseExternalSourceDirective) & " called with wrong token")

            Dim identifier = DirectCast(CurrentToken, IdentifierTokenSyntax)
            Dim externalSourceKeyword = _scanner.MakeKeyword(identifier)

            GetNextToken()

            Dim openParen As PunctuationSyntax = Nothing
            Dim closeParen As PunctuationSyntax = Nothing

            VerifyExpectedToken(SyntaxKind.OpenParenToken, openParen)

            Dim externalSource As StringLiteralTokenSyntax = Nothing
            VerifyExpectedToken(SyntaxKind.StringLiteralToken, externalSource)

            Dim comma As PunctuationSyntax = Nothing
            VerifyExpectedToken(SyntaxKind.CommaToken, comma)

            Dim externalSourceFileStartLine As IntegerLiteralTokenSyntax = Nothing
            VerifyExpectedToken(SyntaxKind.IntegerLiteralToken, externalSourceFileStartLine)

            VerifyExpectedToken(SyntaxKind.CloseParenToken, closeParen)

            'TODO - Need to keep track of external source nesting and report and error

            'If m_CurrentExternalSourceDirective IsNot Nothing Then

            '    ReportSyntaxError(ERRID.ERR_NestedExternalSource, Start, m_CurrentToken, ErrorInConstruct)
            '    hasErrors = True

            'ElseIf m_ExternalSourceDirectiveTarget IsNot Nothing Then

            'End If

            Dim statement = SyntaxFactory.ExternalSourceDirectiveTrivia(hashToken,
                                                  externalSourceKeyword,
                                                  openParen,
                                                  externalSource,
                                                  comma,
                                                  externalSourceFileStartLine,
                                                  closeParen)

            Return statement
        End Function

        Private Function ParseExternalChecksumDirective(hashToken As PunctuationSyntax) As ExternalChecksumDirectiveTriviaSyntax
            Debug.Assert(CurrentToken.Kind = SyntaxKind.IdentifierToken AndAlso DirectCast(CurrentToken, IdentifierTokenSyntax).PossibleKeywordKind = SyntaxKind.ExternalChecksumKeyword,
                          NameOf(ParseExternalChecksumDirective) & " called with wrong token")

            Dim identifier = DirectCast(CurrentToken, IdentifierTokenSyntax)
            Dim externalChecksumKeyword = _scanner.MakeKeyword(identifier)

            GetNextToken()

            Dim openParen As PunctuationSyntax = Nothing
            Dim closeParen As PunctuationSyntax = Nothing

            VerifyExpectedToken(SyntaxKind.OpenParenToken, openParen)

            Dim externalSource As StringLiteralTokenSyntax = Nothing
            VerifyExpectedToken(SyntaxKind.StringLiteralToken, externalSource)

            Dim firstComma As PunctuationSyntax = Nothing
            VerifyExpectedToken(SyntaxKind.CommaToken, firstComma)

            Dim guid As StringLiteralTokenSyntax = Nothing
            VerifyExpectedToken(SyntaxKind.StringLiteralToken, guid)

            If Not guid.IsMissing Then
                Dim tmp As System.Guid
                If Not System.Guid.TryParse(guid.ValueText, tmp) Then
                    guid = guid.WithDiagnostics(ErrorFactory.ErrorInfo(ERRID.WRN_BadGUIDFormatExtChecksum))
                End If
            End If

            Dim secondComma As PunctuationSyntax = Nothing
            VerifyExpectedToken(SyntaxKind.CommaToken, secondComma)

            Dim checksum As StringLiteralTokenSyntax = Nothing
            VerifyExpectedToken(SyntaxKind.StringLiteralToken, checksum)

            If Not checksum.IsMissing Then
                Dim checksumText = checksum.ValueText

                If checksumText.Length Mod 2 <> 0 Then
                    checksum = checksum.WithDiagnostics(ErrorFactory.ErrorInfo(ERRID.WRN_BadChecksumValExtChecksum))

                Else
                    For Each ch In checksumText
                        If Not SyntaxFacts.IsHexDigit(ch) Then
                            checksum = checksum.WithDiagnostics(ErrorFactory.ErrorInfo(ERRID.WRN_BadChecksumValExtChecksum))
                            Exit For
                        End If
                    Next
                End If
            End If

            VerifyExpectedToken(SyntaxKind.CloseParenToken, closeParen)

            Dim statement = SyntaxFactory.ExternalChecksumDirectiveTrivia(hashToken,
                                                    externalChecksumKeyword,
                                                    openParen,
                                                    externalSource,
                                                    firstComma,
                                                    guid,
                                                    secondComma,
                                                    checksum,
                                                    closeParen)

            Return statement

        End Function

        Private Function ParseWarningDirective(hashToken As PunctuationSyntax) As DirectiveTriviaSyntax
            Debug.Assert(CurrentToken.Kind = SyntaxKind.IdentifierToken,
                         NameOf(ParseWarningDirective) & " called with token that is not an {NameOf(SyntaxKind.IdentifierToken)}")
            Dim identifier = DirectCast(CurrentToken, IdentifierTokenSyntax)

            Debug.Assert((identifier.PossibleKeywordKind = SyntaxKind.EnableKeyword) OrElse
                         (identifier.PossibleKeywordKind = SyntaxKind.DisableKeyword),
                         NameOf(ParseWarningDirective) & " called with token that is neither {NameOf(SyntaxKind.EnableKeyword)} nor {NameOf(SyntaxKind.DisableKeyword)}")
            Dim enableOrDisableKeyword = _scanner.MakeKeyword(identifier)

            GetNextToken()

            Dim warningKeyword As KeywordSyntax = Nothing
            TryGetContextualKeyword(SyntaxKind.WarningKeyword, warningKeyword, createIfMissing:=True)
            If warningKeyword.ContainsDiagnostics Then
                warningKeyword = ResyncAt(warningKeyword)
            End If

            Dim errorCodes = Me._pool.AllocateSeparated(Of IdentifierNameSyntax)()
            If Not SyntaxFacts.IsTerminator(CurrentToken.Kind) Then
                Do
                    Dim errorCode = SyntaxFactory.IdentifierName(ParseIdentifier())
                    If errorCode.ContainsDiagnostics Then
                        errorCode = ResyncAt(errorCode, SyntaxKind.CommaToken)
                    ElseIf errorCode.Identifier.TypeCharacter <> TypeCharacter.None Then
                        ' Disallow type characters at the end of diagnostic ids.
                        errorCode = ReportSyntaxError(errorCode, ERRID.ERR_TypecharNotallowed)
                    End If
                    errorCodes.Add(errorCode)

                    Dim comma As PunctuationSyntax = Nothing
                    If Not TryGetToken(SyntaxKind.CommaToken, comma) Then
                        If SyntaxFacts.IsTerminator(CurrentToken.Kind) Then
                            Exit Do
                        Else
                            comma = InternalSyntaxFactory.MissingPunctuation(SyntaxKind.CommaToken)
                            comma = ReportSyntaxError(comma, ERRID.ERR_ExpectedComma)
                        End If
                    End If

                    If comma.ContainsDiagnostics Then
                        comma = ResyncAt(comma)
                    End If
                    errorCodes.AddSeparator(comma)
                Loop
            End If

            Dim statement As DirectiveTriviaSyntax = Nothing
            If enableOrDisableKeyword.Kind = SyntaxKind.EnableKeyword Then
                statement = SyntaxFactory.EnableWarningDirectiveTrivia(
                    hashToken, enableOrDisableKeyword, warningKeyword, errorCodes.ToList)
            ElseIf enableOrDisableKeyword.Kind = SyntaxKind.DisableKeyword Then
                statement = SyntaxFactory.DisableWarningDirectiveTrivia(
                    hashToken, enableOrDisableKeyword, warningKeyword, errorCodes.ToList)
            End If

            If statement IsNot Nothing Then
                statement = CheckFeatureAvailability(Feature.WarningDirectives, statement)
            End If

            Me._pool.Free(errorCodes)
            Return statement
        End Function

        Private Function ParseReferenceDirective(hashToken As PunctuationSyntax) As DirectiveTriviaSyntax
            Debug.Assert(CurrentToken.Kind = SyntaxKind.IdentifierToken AndAlso DirectCast(CurrentToken, IdentifierTokenSyntax).PossibleKeywordKind = SyntaxKind.ReferenceKeyword,
                         NameOf(ParseReferenceDirective) & " called with wrong token.")

            Dim identifier = DirectCast(CurrentToken, IdentifierTokenSyntax)
            GetNextToken()
            Dim referenceKeyword = _scanner.MakeKeyword(identifier)

            If Not IsScript Then
                referenceKeyword = AddError(referenceKeyword, ERRID.ERR_ReferenceDirectiveOnlyAllowedInScripts)
            End If

            Dim file As StringLiteralTokenSyntax = Nothing
            VerifyExpectedToken(SyntaxKind.StringLiteralToken, file)

            Return SyntaxFactory.ReferenceDirectiveTrivia(hashToken, referenceKeyword, file)
        End Function

        Private Shared Function ParseBadDirective(hashToken As PunctuationSyntax) As BadDirectiveTriviaSyntax
            Dim badDirective = InternalSyntaxFactory.BadDirectiveTrivia(hashToken)

            If Not badDirective.ContainsDiagnostics Then
                badDirective = ReportSyntaxError(badDirective, ERRID.ERR_ExpectedConditionalDirective)
            End If

            Return badDirective
        End Function

    End Class

End Namespace
