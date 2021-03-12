' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.Formatting.Rules
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.Formatting
    Friend Class AdjustSpaceFormattingRule
        Inherits BaseFormattingRule
        Friend Const Name As String = "VisualBasic Adjust Space Formatting Rule"

        Public Sub New()
        End Sub

        Public Overrides Function GetAdjustSpacesOperationSlow(ByRef previousToken As SyntaxToken, ByRef currentToken As SyntaxToken, ByRef nextFunc As NextGetAdjustSpacesOperation) As AdjustSpacesOperation
            ' * <end of file token>
            If currentToken.IsKind(SyntaxKind.EndOfFileToken) Then
                Return CreateAdjustSpacesOperation(0, AdjustSpacesOption.ForceSpacesIfOnSingleLine)
            End If

            ' ()
            If previousToken.IsKind(SyntaxKind.OpenParenToken) AndAlso currentToken.IsKind(SyntaxKind.CloseParenToken) Then
                Return CreateAdjustSpacesOperation(0, AdjustSpacesOption.ForceSpacesIfOnSingleLine)
            End If

            ' ( < case
            If previousToken.IsKind(SyntaxKind.OpenParenToken) AndAlso
               FormattingHelpers.IsLessThanInAttribute(currentToken) Then
                Return CreateAdjustSpacesOperation(0, AdjustSpacesOption.ForceSpacesIfOnSingleLine)
            End If

            ' < * * > in attribute
            If FormattingHelpers.IsLessThanInAttribute(previousToken) OrElse
               FormattingHelpers.IsGreaterThanInAttribute(currentToken) Then
                Return CreateAdjustSpacesOperation(0, AdjustSpacesOption.ForceSpacesIfOnSingleLine)
            End If

            ' * < > * in attribute
            If FormattingHelpers.IsGreaterThanInAttribute(previousToken) OrElse
               FormattingHelpers.IsLessThanInAttribute(currentToken) Then
                Return CreateAdjustSpacesOperation(1, AdjustSpacesOption.ForceSpacesIfOnSingleLine)
            End If

            ' <? * 
            If previousToken.IsKind(SyntaxKind.LessThanQuestionToken) AndAlso FormattingHelpers.IsXmlTokenInXmlDeclaration(previousToken) Then
                Return CreateAdjustSpacesOperation(0, AdjustSpacesOption.ForceSpacesIfOnSingleLine)
            End If

            ' * ?> case
            If currentToken.IsKind(SyntaxKind.QuestionGreaterThanToken) AndAlso FormattingHelpers.IsXmlTokenInXmlDeclaration(currentToken) Then
                Return CreateAdjustSpacesOperation(0, AdjustSpacesOption.ForceSpacesIfOnSingleLine)
            End If

            ' except <%= [xml token] or [xml token] %> case
            If (Not previousToken.IsKind(SyntaxKind.LessThanPercentEqualsToken) AndAlso FormattingHelpers.IsXmlToken(currentToken)) AndAlso
               (FormattingHelpers.IsXmlToken(previousToken) AndAlso Not currentToken.IsKind(SyntaxKind.PercentGreaterThanToken)) Then

                ' [xml token] [xml token]
                If FormattingHelpers.IsXmlToken(previousToken) AndAlso FormattingHelpers.IsXmlToken(currentToken) Then
                    Return CreateAdjustSpacesOperation(0, AdjustSpacesOption.ForceSpacesIfOnSingleLine)
                End If
            End If

            ' %> [xml name token]
            If previousToken.IsKind(SyntaxKind.PercentGreaterThanToken) AndAlso currentToken.IsKind(SyntaxKind.XmlNameToken) Then
                Return CreateAdjustSpacesOperation(1, AdjustSpacesOption.ForceSpacesIfOnSingleLine)
            End If

            ' [xml token] [xml name token]
            If FormattingHelpers.IsXmlToken(previousToken) AndAlso currentToken.IsKind(SyntaxKind.XmlNameToken) Then
                Return CreateAdjustSpacesOperation(0, AdjustSpacesOption.ForceSpacesIfOnSingleLine)
            End If

            ' [xml name token] <%=
            If previousToken.IsKind(SyntaxKind.XmlNameToken) AndAlso currentToken.IsKind(SyntaxKind.LessThanPercentEqualsToken) Then
                Return CreateAdjustSpacesOperation(1, AdjustSpacesOption.ForceSpacesIfOnSingleLine)
            End If

            ' [xml name token] %>
            If previousToken.IsKind(SyntaxKind.XmlNameToken) AndAlso currentToken.IsKind(SyntaxKind.PercentGreaterThanToken) Then
                Return CreateAdjustSpacesOperation(1, AdjustSpacesOption.ForceSpacesIfOnSingleLine)
            End If

            ' [xml name token] [xml token]
            If previousToken.IsKind(SyntaxKind.XmlNameToken) AndAlso FormattingHelpers.IsXmlToken(currentToken) Then
                Return CreateAdjustSpacesOperation(0, AdjustSpacesOption.ForceSpacesIfOnSingleLine)
            End If

            ' [xml name token] =
            If previousToken.IsKind(SyntaxKind.XmlNameToken) AndAlso currentToken.IsKind(SyntaxKind.EqualsToken) Then
                ' [XmlAttributeAccessExpression] =
                If TypeOf currentToken.Parent Is BinaryExpressionSyntax AndAlso DirectCast(currentToken.Parent, BinaryExpressionSyntax).Left.IsKind(SyntaxKind.XmlAttributeAccessExpression) OrElse
                    currentToken.Parent.IsKind(SyntaxKind.SimpleAssignmentStatement) AndAlso DirectCast(currentToken.Parent, AssignmentStatementSyntax).Left.IsKind(SyntaxKind.XmlAttributeAccessExpression) Then
                    Return CreateAdjustSpacesOperation(1, AdjustSpacesOption.ForceSpacesIfOnSingleLine)
                End If

                ' [XmlDeclarationOption]
                If currentToken.Parent.IsKind(SyntaxKind.XmlDeclarationOption) Then
                    Return CreateAdjustSpacesOperation(0, AdjustSpacesOption.ForceSpacesIfOnSingleLine)
                End If
            End If

            ' = ' [xml string]
            If previousToken.IsKind(SyntaxKind.EqualsToken) AndAlso FormattingHelpers.IsQuoteInXmlString(currentToken) Then
                Return CreateAdjustSpacesOperation(0, AdjustSpacesOption.ForceSpacesIfOnSingleLine)
            End If

            ' " * * " or ' * * ' or "" or '' in xml string
            If (FormattingHelpers.IsQuoteInXmlString(previousToken) AndAlso
                FormattingHelpers.IsContentInXmlString(currentToken)) OrElse
               (FormattingHelpers.IsQuoteInXmlString(currentToken) AndAlso
                (FormattingHelpers.IsContentInXmlString(previousToken) OrElse
                 FormattingHelpers.IsQuoteInXmlString(previousToken))) Then
                Return CreateAdjustSpacesOperation(0, AdjustSpacesOption.ForceSpacesIfOnSingleLine)
            End If

            ' xml text literal 
            If (previousToken.IsKind(SyntaxKind.XmlTextLiteralToken) AndAlso Not currentToken.IsKind(SyntaxKind.XmlNameToken)) OrElse
               (Not previousToken.IsKind(SyntaxKind.XmlNameToken) AndAlso currentToken.IsKind(SyntaxKind.XmlTextLiteralToken)) Then
                Return CreateAdjustSpacesOperation(0, AdjustSpacesOption.ForceSpacesIfOnSingleLine)
            End If

            ' xml entity literal
            If previousToken.IsKind(SyntaxKind.XmlEntityLiteralToken) OrElse
                currentToken.IsKind(SyntaxKind.XmlEntityLiteralToken) Then
                Return CreateAdjustSpacesOperation(0, AdjustSpacesOption.PreserveSpaces)
            End If

            ' (( case
            If previousToken.IsKind(SyntaxKind.OpenParenToken) AndAlso
               currentToken.IsKind(SyntaxKind.OpenParenToken) Then
                Return CreateAdjustSpacesOperation(0, AdjustSpacesOption.ForceSpacesIfOnSingleLine)
            End If

            ' [identifier] ( case
            If previousToken.IsKind(SyntaxKind.IdentifierToken) AndAlso
               currentToken.IsKind(SyntaxKind.OpenParenToken) Then
                Return CreateAdjustSpacesOperation(0, AdjustSpacesOption.ForceSpacesIfOnSingleLine)
            End If

            ' [some keywords] ( case
            If currentToken.IsKind(SyntaxKind.OpenParenToken) Then
                Select Case previousToken.Kind
                    Case SyntaxKind.NewKeyword, SyntaxKind.FunctionKeyword, SyntaxKind.SubKeyword, SyntaxKind.SetKeyword,
                         SyntaxKind.AddHandlerKeyword, SyntaxKind.RemoveHandlerKeyword, SyntaxKind.RaiseEventKeyword,
                         SyntaxKind.GetTypeKeyword, SyntaxKind.CTypeKeyword, SyntaxKind.TryCastKeyword,
                         SyntaxKind.DirectCastKeyword, SyntaxKind.GetXmlNamespaceKeyword, SyntaxKind.NameOfKeyword
                        Return CreateAdjustSpacesOperation(0, AdjustSpacesOption.ForceSpacesIfOnSingleLine)
                End Select

                If SyntaxFacts.IsPredefinedCastExpressionKeyword(previousToken.Kind) Then
                    Return CreateAdjustSpacesOperation(0, AdjustSpacesOption.ForceSpacesIfOnSingleLine)
                End If
            End If

            ' [parent in argument list] ( case
            If FormattingHelpers.IsParenInArgumentList(currentToken) Then
                Return CreateAdjustSpacesOperation(0, AdjustSpacesOption.ForceSpacesIfOnSingleLine)
            End If

            ' [binary condition] ( case
            If FormattingHelpers.IsParenInBinaryCondition(currentToken) Then
                Return CreateAdjustSpacesOperation(0, AdjustSpacesOption.ForceSpacesIfOnSingleLine)
            End If

            ' [ternary condition] ( case
            If FormattingHelpers.IsParenInTernaryCondition(currentToken) Then
                Return CreateAdjustSpacesOperation(0, AdjustSpacesOption.ForceSpacesIfOnSingleLine)
            End If

            ' array rank specifier ( case
            If currentToken.IsKind(SyntaxKind.OpenParenToken) AndAlso TypeOf currentToken.Parent Is ArrayRankSpecifierSyntax Then
                Return CreateAdjustSpacesOperation(0, AdjustSpacesOption.ForceSpacesIfOnSingleLine)
            End If

            ' [overloadable operator] ( case
            If currentToken.IsKind(SyntaxKind.OpenParenToken) AndAlso FormattingHelpers.IsOverloadableOperator(previousToken) Then
                Return CreateAdjustSpacesOperation(0, AdjustSpacesOption.ForceSpacesIfOnSingleLine)
            End If

            ' [type parameter list] [parameter list] )( case
            If previousToken.IsKind(SyntaxKind.CloseParenToken) AndAlso TypeOf previousToken.Parent Is TypeParameterListSyntax AndAlso
               currentToken.IsKind(SyntaxKind.OpenParenToken) AndAlso TypeOf currentToken.Parent Is ParameterListSyntax Then
                Return CreateAdjustSpacesOperation(0, AdjustSpacesOption.ForceSpacesIfOnSingleLine)
            End If

            ' , [named field initializer dot]
            If previousToken.IsKind(SyntaxKind.CommaToken) AndAlso FormattingHelpers.IsNamedFieldInitializerDot(currentToken) Then
                Return CreateAdjustSpacesOperation(1, AdjustSpacesOption.ForceSpacesIfOnSingleLine)
            End If

            ' ? . [conditional access operator]
            ' ? ! [conditional access operator]
            If previousToken.IsKind(SyntaxKind.QuestionToken) AndAlso currentToken.IsKind(SyntaxKind.DotToken, SyntaxKind.ExclamationToken) AndAlso
               previousToken.Parent.IsKind(SyntaxKind.ConditionalAccessExpression) Then
                Return CreateAdjustSpacesOperation(0, AdjustSpacesOption.ForceSpacesIfOnSingleLine)
            End If

            ' * ?
            If currentToken.IsKind(SyntaxKind.QuestionToken) AndAlso
               currentToken.Parent.IsKind(SyntaxKind.ConditionalAccessExpression) Then
                Return CreateAdjustSpacesOperation(0, AdjustSpacesOption.ForceSpacesIfOnSingleLine)
            End If

            ' * [member access dot without expression]
            If Not previousToken.IsKind(SyntaxKind.OpenParenToken) AndAlso FormattingHelpers.IsMemberAccessDotWithoutExpression(currentToken) Then

                ' label:     .X
                If previousToken.IsKind(SyntaxKind.ColonToken) AndAlso TypeOf previousToken.Parent Is LabelStatementSyntax Then
                    Return FormattingOperations.CreateAdjustSpacesOperation(1, AdjustSpacesOption.DynamicSpaceToIndentationIfOnSingleLine)
                End If

                Return CreateAdjustSpacesOperation(1, AdjustSpacesOption.ForceSpacesIfOnSingleLine)
            End If

            ' * [dictionary access exclamation without expression]
            If Not previousToken.IsKind(SyntaxKind.OpenParenToken) AndAlso FormattingHelpers.IsDictionaryAccessExclamationWithoutExpression(currentToken) Then
                Return CreateAdjustSpacesOperation(1, AdjustSpacesOption.ForceSpacesIfOnSingleLine)
            End If

            ' No space after $" at the start of an interpolated string
            If previousToken.IsKind(SyntaxKind.DollarSignDoubleQuoteToken) AndAlso previousToken.Parent.IsKind(SyntaxKind.InterpolatedStringExpression) Then
                Return CreateAdjustSpacesOperation(0, AdjustSpacesOption.ForceSpaces)
            End If

            ' No space before " at the end of an interpolated string
            If currentToken.IsKind(SyntaxKind.DoubleQuoteToken) AndAlso currentToken.Parent.IsKind(SyntaxKind.InterpolatedStringExpression) Then
                Return CreateAdjustSpacesOperation(0, AdjustSpacesOption.ForceSpaces)
            End If

            ' No space before { Or after } in interpolations
            If (currentToken.IsKind(SyntaxKind.OpenBraceToken) AndAlso currentToken.Parent.IsKind(SyntaxKind.Interpolation)) OrElse
               (previousToken.IsKind(SyntaxKind.CloseBraceToken) AndAlso previousToken.Parent.IsKind(SyntaxKind.Interpolation)) Then
                Return CreateAdjustSpacesOperation(0, AdjustSpacesOption.ForceSpaces)
            End If

            ' Preserve space after { Or before } in interpolations (i.e. between the braces And the expression)
            If (previousToken.IsKind(SyntaxKind.OpenBraceToken) AndAlso previousToken.Parent.IsKind(SyntaxKind.Interpolation)) OrElse
               (currentToken.IsKind(SyntaxKind.CloseBraceToken) AndAlso currentToken.Parent.IsKind(SyntaxKind.Interpolation)) Then
                Return CreateAdjustSpacesOperation(0, AdjustSpacesOption.PreserveSpaces)
            End If

            ' No space before Or after , in interpolation alignment clause
            If (previousToken.IsKind(SyntaxKind.CommaToken) AndAlso previousToken.Parent.IsKind(SyntaxKind.InterpolationAlignmentClause)) OrElse
               (currentToken.IsKind(SyntaxKind.CommaToken) AndAlso currentToken.Parent.IsKind(SyntaxKind.InterpolationAlignmentClause)) Then
                Return CreateAdjustSpacesOperation(0, AdjustSpacesOption.ForceSpaces)
            End If

            ' No space before Or after : in interpolation format clause
            If (previousToken.IsKind(SyntaxKind.ColonToken) AndAlso previousToken.Parent.IsKind(SyntaxKind.InterpolationFormatClause)) OrElse
               (currentToken.IsKind(SyntaxKind.ColonToken) AndAlso currentToken.Parent.IsKind(SyntaxKind.InterpolationFormatClause)) Then
                Return CreateAdjustSpacesOperation(0, AdjustSpacesOption.ForceSpaces)
            End If

            ' * }
            ' * )
            ' * ,
            ' * .
            ' * :=
            ' * !
            Select Case currentToken.Kind
                Case SyntaxKind.CloseParenToken, SyntaxKind.CommaToken
                    Return If(previousToken.IsKind(SyntaxKind.EmptyToken) AndAlso PrecedingTriviaContainsLineBreak(previousToken),
                        CreateAdjustSpacesOperation(0, AdjustSpacesOption.PreserveSpaces),
                        CreateAdjustSpacesOperation(0, AdjustSpacesOption.ForceSpacesIfOnSingleLine))

                Case SyntaxKind.CloseBraceToken, SyntaxKind.ColonEqualsToken
                    Return CreateAdjustSpacesOperation(0, AdjustSpacesOption.ForceSpacesIfOnSingleLine)

                Case SyntaxKind.DotToken
                    Dim space = If(previousToken.IsKind(SyntaxKind.CallKeyword) OrElse
                                   previousToken.IsKind(SyntaxKind.KeyKeyword),
                                   1, 0)

                    Return CreateAdjustSpacesOperation(space, AdjustSpacesOption.ForceSpacesIfOnSingleLine)

                Case SyntaxKind.ExclamationToken
                    If IsExclamationInDictionaryAccess(currentToken) Then
                        Dim space = If(currentToken.TrailingTrivia.Any(SyntaxKind.LineContinuationTrivia), 1, 0)

                        Return CreateAdjustSpacesOperation(space, AdjustSpacesOption.ForceSpacesIfOnSingleLine)
                    End If
            End Select

            ' nullable ? case
            If FormattingHelpers.IsQuestionInNullableType(currentToken) Then
                Return CreateAdjustSpacesOperation(0, AdjustSpacesOption.ForceSpacesIfOnSingleLine)
            End If

            ' { *
            ' ( *
            ' ) *
            ' . *
            ' := *
            ' ! *
            Select Case previousToken.Kind
                Case SyntaxKind.OpenBraceToken, SyntaxKind.OpenParenToken, SyntaxKind.DotToken, SyntaxKind.ColonEqualsToken
                    Return CreateAdjustSpacesOperation(0, AdjustSpacesOption.ForceSpacesIfOnSingleLine)

                Case SyntaxKind.CloseParenToken
                    Dim space = If(previousToken.IsKind(currentToken.Kind), 0, 1)
                    Return CreateAdjustSpacesOperation(space, AdjustSpacesOption.ForceSpacesIfOnSingleLine)

                Case SyntaxKind.ExclamationToken
                    If IsExclamationInDictionaryAccess(previousToken) Then
                        Return CreateAdjustSpacesOperation(0, AdjustSpacesOption.ForceSpacesIfOnSingleLine)
                    End If
            End Select

            ' * </
            If currentToken.IsKind(SyntaxKind.LessThanSlashToken) AndAlso
               FormattingHelpers.IsXmlToken(previousToken) Then
                Return CreateAdjustSpacesOperation(0, AdjustSpacesOption.ForceSpacesIfOnSingleLine)
            End If

            ' * />
            If currentToken.IsKind(SyntaxKind.SlashGreaterThanToken) Then
                Return CreateAdjustSpacesOperation(0, AdjustSpacesOption.ForceSpacesIfOnSingleLine)
            End If

            ' * > in xml literal
            If (currentToken.IsKind(SyntaxKind.GreaterThanToken) AndAlso
                FormattingHelpers.IsXmlToken(currentToken)) Then
                Return CreateAdjustSpacesOperation(0, AdjustSpacesOption.ForceSpacesIfOnSingleLine)
            End If

            ' +1 or -1
            If (previousToken.IsKind(SyntaxKind.PlusToken) OrElse
                previousToken.IsKind(SyntaxKind.MinusToken)) AndAlso
                TypeOf previousToken.Parent Is UnaryExpressionSyntax Then
                Return CreateAdjustSpacesOperation(0, AdjustSpacesOption.ForceSpacesIfOnSingleLine)
            End If

            ' <AttributeTarget : case
            If FormattingHelpers.IsColonAfterAttributeTarget(previousToken, currentToken) Then
                Return CreateAdjustSpacesOperation(0, AdjustSpacesOption.ForceSpacesIfOnSingleLine)
            End If

            If previousToken.IsKind(SyntaxKind.EmptyToken) OrElse currentToken.IsKind(SyntaxKind.EmptyToken) Then
                Return CreateAdjustSpacesOperation(0, AdjustSpacesOption.PreserveSpaces)
            End If

            ' Else If
            If previousToken.IsKind(SyntaxKind.ElseKeyword) AndAlso
               currentToken.IsKind(SyntaxKind.IfKeyword) AndAlso
               previousToken.Parent Is currentToken.Parent Then
                Return CreateAdjustSpacesOperation(0, AdjustSpacesOption.ForceSpacesIfOnSingleLine)
            End If

            ' label
            Dim labelStatement = TryCast(previousToken.Parent, LabelStatementSyntax)
            If labelStatement IsNot Nothing AndAlso
               labelStatement.LabelToken = previousToken AndAlso
               currentToken.IsKind(SyntaxKind.ColonToken) Then
                Return CreateAdjustSpacesOperation(0, AdjustSpacesOption.ForceSpacesIfOnSingleLine)
            End If

            Return nextFunc.Invoke(previousToken, currentToken)
        End Function

        Private Shared Function PrecedingTriviaContainsLineBreak(previousToken As SyntaxToken) As Boolean
            Return ContainsLineBreak(previousToken.LeadingTrivia) OrElse ContainsLineBreak(previousToken.GetPreviousToken(includeZeroWidth:=True).TrailingTrivia)
        End Function

        Private Shared Function ContainsLineBreak(triviaList As SyntaxTriviaList) As Boolean
            Return triviaList.Any(Function(t) t.IsKind(SyntaxKind.EndOfLineTrivia))
        End Function
    End Class
End Namespace
