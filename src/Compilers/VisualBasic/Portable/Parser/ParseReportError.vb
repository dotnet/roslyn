' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.Syntax.InternalSyntax
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports InternalSyntaxFactory = Microsoft.CodeAnalysis.VisualBasic.Syntax.InternalSyntax.SyntaxFactory

Namespace Microsoft.CodeAnalysis.VisualBasic.Syntax.InternalSyntax

    Partial Friend Class Parser

        ' /*********************************************************************
        ' *
        ' * Function:
        ' *     Parser::ReportSyntaxError
        ' *
        ' * Purpose:
        ' *     Creates and adds a general parse error to the error table, getting
        ' *     the error location info from a span of Tokens.
        ' *
        ' **********************************************************************/

        ' // [in] error id
        ' // [in] token beginning error (inclusive)
        ' // [in] token ending error (non-inclusive)
        ' // [in] indicates whether the statement containing the error should be marked bad or not

        ' File: Parser.cpp
        ' Lines: 17543 - 17543
        ' .Parser::ReportSyntaxError( [ unsigned Errid ] [ _In_ Token* Beg ] [ _In_ Token* End ] [ bool MarkErrorStatementBad ] [ _Inout_ bool& ErrorInConstruct ] )

        Friend Shared Function ReportSyntaxError(Of T As GreenNode)(syntax As T, ErrorId As ERRID) As T
            Return DirectCast(syntax.AddError(ErrorFactory.ErrorInfo(ErrorId)), T)
        End Function

        Friend Shared Function ReportSyntaxError(Of T As VisualBasicSyntaxNode)(syntax As T, ErrorId As ERRID, ParamArray args() As Object) As T
            Return DirectCast(syntax.AddError(ErrorFactory.ErrorInfo(ErrorId, args)), T)
        End Function

        ' // Create an error for a statement not recognized as anything meaningful in the
        ' // current context. Return a statement for the erroneous text.

        ' File: Parser.cpp
        ' Lines: 17749 - 17749
        ' Statement* .Parser::ReportUnrecognizedStatementError( [ unsigned ErrorId ] [ _Inout_ bool& ErrorInConstruct ] )

        Private Function ReportUnrecognizedStatementError(ErrorId As ERRID) As StatementSyntax
            Return ReportUnrecognizedStatementError(ErrorId, Nothing, Nothing)
        End Function

        ''' <summary>
        ''' Create a bad statement.  Report an error only if the statement doesn't have one already
        ''' </summary>
        ''' <param name="ErrorId"></param>
        ''' <param name="attributes"></param>
        ''' <param name="modifiers"></param>
        ''' <param name="createMissingIdentifier">If set to true a new missing identifier will be created and added to the incomplete member.</param>
        ''' <param name="forceErrorOnFirstToken">If set to true the error will be attached to the first skipped token of the incomplete member.</param>
        ''' <returns></returns>
        ''' <remarks></remarks>
        Private Function ReportUnrecognizedStatementError(
            ErrorId As ERRID,
            attributes As CodeAnalysis.Syntax.InternalSyntax.SyntaxList(Of AttributeListSyntax),
            modifiers As CodeAnalysis.Syntax.InternalSyntax.SyntaxList(Of KeywordSyntax),
            Optional createMissingIdentifier As Boolean = False,
            Optional forceErrorOnFirstToken As Boolean = False) As StatementSyntax
            ' // Create a statement with no operands. It will end up with its error flag set.

            Dim tokens = ResyncAt()

            ' only use this flag if there is a token following the modifiers and attributes, otherwise there's nothing to attach
            ' this error to.
            Debug.Assert(Not forceErrorOnFirstToken OrElse tokens.Any AndAlso (modifiers.Any OrElse attributes.Any))

            Dim missingIdentifier As IdentifierTokenSyntax = Nothing
            If createMissingIdentifier Then
                missingIdentifier = InternalSyntaxFactory.MissingIdentifier()
                missingIdentifier = ReportSyntaxError(missingIdentifier, ErrorId)
            End If

            Dim badStmt As StatementSyntax

            If modifiers.Any OrElse attributes.Any Then
                badStmt = SyntaxFactory.IncompleteMember(attributes, modifiers, missingIdentifier)

                If forceErrorOnFirstToken Then
                    badStmt = badStmt.AddTrailingSyntax(tokens, ErrorId)
                Else
                    ' Just add the skipped text. The error will be put on the statement below.
                    badStmt = badStmt.AddTrailingSyntax(tokens)
                End If
            Else
                badStmt = InternalSyntaxFactory.EmptyStatement()

                ' The statement is empty so put the error on the skipped tokens.
                If Not tokens.ContainsDiagnostics Then
                    badStmt = badStmt.AddTrailingSyntax(tokens, ErrorId)
                Else
                    badStmt = badStmt.AddTrailingSyntax(tokens)
                End If

            End If

            ' Add error only if the stmt doesn't have a diagnostic already
            If Not badStmt.ContainsDiagnostics Then
                badStmt = badStmt.AddError(ErrorId)
            End If

            Return badStmt
        End Function

        Private Function ReportModifiersOnStatementError(attributes As CodeAnalysis.Syntax.InternalSyntax.SyntaxList(Of AttributeListSyntax), modifiers As CodeAnalysis.Syntax.InternalSyntax.SyntaxList(Of KeywordSyntax), keyword As KeywordSyntax) As KeywordSyntax
            Return ReportModifiersOnStatementError(ERRID.ERR_SpecifiersInvalidOnInheritsImplOpt, attributes, modifiers, keyword)
        End Function

        Private Function ReportModifiersOnStatementError(errorId As ERRID, attributes As CodeAnalysis.Syntax.InternalSyntax.SyntaxList(Of AttributeListSyntax), modifiers As CodeAnalysis.Syntax.InternalSyntax.SyntaxList(Of KeywordSyntax), keyword As KeywordSyntax) As KeywordSyntax
            If modifiers.Any Then
                keyword = keyword.AddLeadingSyntax(modifiers.Node, errorId)
            End If

            If attributes.Any Then
                keyword = keyword.AddLeadingSyntax(attributes.Node, errorId)
            End If

            Return keyword
        End Function
    End Class

End Namespace
