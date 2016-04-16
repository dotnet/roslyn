' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Composition
Imports Microsoft.CodeAnalysis.Formatting.Rules
Imports Microsoft.CodeAnalysis.Options
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.Formatting
    <ExportFormattingRule(AdjustSpaceFormattingRule.Name, LanguageNames.VisualBasic), [Shared]>
    <ExtensionOrder(After:=ElasticTriviaFormattingRule.Name)>
    Friend Class AdjustSpaceFormattingRule
        Inherits BaseFormattingRule
        Friend Const Name As String = "VisualBasic Adjust Space Formatting Rule"

        Public Sub New()
        End Sub

        Public Overrides Function GetAdjustSpacesOperation(previousToken As SyntaxToken, currentToken As SyntaxToken, optionSet As OptionSet, nextFunc As NextOperation(Of AdjustSpacesOperation)) As AdjustSpacesOperation
            ' * <end of file token>
            If currentToken.Kind = SyntaxKind.EndOfFileToken Then
                Return CreateAdjustSpacesOperation(0, AdjustSpacesOption.ForceSpacesIfOnSingleLine)
            End If

            ' ()
            If previousToken.Kind = SyntaxKind.OpenParenToken AndAlso currentToken.Kind = SyntaxKind.CloseParenToken Then
                Return CreateAdjustSpacesOperation(0, AdjustSpacesOption.ForceSpacesIfOnSingleLine)
            End If

            ' ( < case
            If previousToken.Kind = SyntaxKind.OpenParenToken AndAlso
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
            If previousToken.Kind = SyntaxKind.LessThanQuestionToken AndAlso FormattingHelpers.IsXmlTokenInXmlDeclaration(previousToken) Then
                Return CreateAdjustSpacesOperation(0, AdjustSpacesOption.ForceSpacesIfOnSingleLine)
            End If

            ' * ?> case
            If currentToken.Kind = SyntaxKind.QuestionGreaterThanToken AndAlso FormattingHelpers.IsXmlTokenInXmlDeclaration(currentToken) Then
                Return CreateAdjustSpacesOperation(0, AdjustSpacesOption.ForceSpacesIfOnSingleLine)
            End If

            ' except <%= [xml token] or [xml token] %> case
            If (previousToken.Kind <> SyntaxKind.LessThanPercentEqualsToken AndAlso FormattingHelpers.IsXmlToken(currentToken)) AndAlso
               (FormattingHelpers.IsXmlToken(previousToken) AndAlso currentToken.Kind <> SyntaxKind.PercentGreaterThanToken) Then

                ' [xml token] [xml token]
                If FormattingHelpers.IsXmlToken(previousToken) AndAlso FormattingHelpers.IsXmlToken(currentToken) Then
                    Return CreateAdjustSpacesOperation(0, AdjustSpacesOption.ForceSpacesIfOnSingleLine)
                End If
            End If

            ' %> [xml name token]
            If previousToken.Kind = SyntaxKind.PercentGreaterThanToken AndAlso currentToken.Kind = SyntaxKind.XmlNameToken Then
                Return CreateAdjustSpacesOperation(1, AdjustSpacesOption.ForceSpacesIfOnSingleLine)
            End If

            ' [xml token] [xml name token]
            If FormattingHelpers.IsXmlToken(previousToken) AndAlso currentToken.Kind = SyntaxKind.XmlNameToken Then
                Return CreateAdjustSpacesOperation(0, AdjustSpacesOption.ForceSpacesIfOnSingleLine)
            End If

            ' [xml name token] <%=
            If previousToken.Kind = SyntaxKind.XmlNameToken AndAlso currentToken.Kind = SyntaxKind.LessThanPercentEqualsToken Then
                Return CreateAdjustSpacesOperation(1, AdjustSpacesOption.ForceSpacesIfOnSingleLine)
            End If

            ' [xml name token] %>
            If previousToken.Kind = SyntaxKind.XmlNameToken AndAlso currentToken.Kind = SyntaxKind.PercentGreaterThanToken Then
                Return CreateAdjustSpacesOperation(1, AdjustSpacesOption.ForceSpacesIfOnSingleLine)
            End If

            ' [xml name token] [xml token]
            If previousToken.Kind = SyntaxKind.XmlNameToken AndAlso FormattingHelpers.IsXmlToken(currentToken) Then
                Return CreateAdjustSpacesOperation(0, AdjustSpacesOption.ForceSpacesIfOnSingleLine)
            End If

            ' [xml name token] =
            If previousToken.Kind = SyntaxKind.XmlNameToken AndAlso currentToken.Kind = SyntaxKind.EqualsToken Then
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
            If previousToken.Kind = SyntaxKind.EqualsToken AndAlso FormattingHelpers.IsQuoteInXmlString(currentToken) Then
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
            If (previousToken.Kind = SyntaxKind.XmlTextLiteralToken AndAlso currentToken.Kind <> SyntaxKind.XmlNameToken) OrElse
               (previousToken.Kind <> SyntaxKind.XmlNameToken AndAlso currentToken.Kind = SyntaxKind.XmlTextLiteralToken) Then
                Return CreateAdjustSpacesOperation(0, AdjustSpacesOption.ForceSpacesIfOnSingleLine)
            End If

            ' xml entity literal
            If previousToken.Kind = SyntaxKind.XmlEntityLiteralToken OrElse
                currentToken.Kind = SyntaxKind.XmlEntityLiteralToken Then
                Return CreateAdjustSpacesOperation(0, AdjustSpacesOption.PreserveSpaces)
            End If

            ' (( case
            If previousToken.Kind = SyntaxKind.OpenParenToken AndAlso
               currentToken.Kind = SyntaxKind.OpenParenToken Then
                Return CreateAdjustSpacesOperation(0, AdjustSpacesOption.ForceSpacesIfOnSingleLine)
            End If

            ' [identifier] ( case
            If previousToken.Kind = SyntaxKind.IdentifierToken AndAlso
               currentToken.Kind = SyntaxKind.OpenParenToken Then
                Return CreateAdjustSpacesOperation(0, AdjustSpacesOption.ForceSpacesIfOnSingleLine)
            End If

            ' [some keywords] ( case
            If currentToken.Kind = SyntaxKind.OpenParenToken Then
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
            If currentToken.Kind = SyntaxKind.OpenParenToken AndAlso TypeOf currentToken.Parent Is ArrayRankSpecifierSyntax Then
                Return CreateAdjustSpacesOperation(0, AdjustSpacesOption.ForceSpacesIfOnSingleLine)
            End If

            ' [overloadable operator] ( case
            If currentToken.Kind = SyntaxKind.OpenParenToken AndAlso FormattingHelpers.IsOverloadableOperator(previousToken) Then
                Return CreateAdjustSpacesOperation(0, AdjustSpacesOption.ForceSpacesIfOnSingleLine)
            End If

            ' [type parameter list] [parameter list] )( case
            If previousToken.Kind = SyntaxKind.CloseParenToken AndAlso TypeOf previousToken.Parent Is TypeParameterListSyntax AndAlso
               currentToken.Kind = SyntaxKind.OpenParenToken AndAlso TypeOf currentToken.Parent Is ParameterListSyntax Then
                Return CreateAdjustSpacesOperation(0, AdjustSpacesOption.ForceSpacesIfOnSingleLine)
            End If

            ' , [named field initializer dot]
            If previousToken.Kind = SyntaxKind.CommaToken AndAlso FormattingHelpers.IsNamedFieldInitializerDot(currentToken) Then
                Return CreateAdjustSpacesOperation(1, AdjustSpacesOption.ForceSpacesIfOnSingleLine)
            End If

            ' ? . [conditional access operator]
            If previousToken.Kind = SyntaxKind.QuestionToken AndAlso currentToken.Kind = SyntaxKind.DotToken AndAlso
               previousToken.Parent.IsKind(SyntaxKind.ConditionalAccessExpression) Then
                Return CreateAdjustSpacesOperation(0, AdjustSpacesOption.ForceSpacesIfOnSingleLine)
            End If

            ' * ?
            If currentToken.Kind = SyntaxKind.QuestionToken AndAlso
               currentToken.Parent.Kind = SyntaxKind.ConditionalAccessExpression Then
                Return CreateAdjustSpacesOperation(0, AdjustSpacesOption.ForceSpacesIfOnSingleLine)
            End If

            ' * [member access dot without expression]
            If previousToken.Kind <> SyntaxKind.OpenParenToken AndAlso FormattingHelpers.IsMemberAccessDotWithoutExpression(currentToken) Then

                ' label:     .X
                If previousToken.Kind = SyntaxKind.ColonToken AndAlso TypeOf previousToken.Parent Is LabelStatementSyntax Then
                    Return FormattingOperations.CreateAdjustSpacesOperation(1, AdjustSpacesOption.DynamicSpaceToIndentationIfOnSingleLine)
                End If

                Return CreateAdjustSpacesOperation(1, AdjustSpacesOption.ForceSpacesIfOnSingleLine)
            End If

            ' No space after $" at the start of an interpolated string
            If previousToken.Kind = SyntaxKind.DollarSignDoubleQuoteToken AndAlso previousToken.Parent.IsKind(SyntaxKind.InterpolatedStringExpression) Then
                Return CreateAdjustSpacesOperation(0, AdjustSpacesOption.ForceSpaces)
            End If

            ' No space before " at the end of an interpolated string
            If currentToken.Kind = SyntaxKind.DoubleQuoteToken AndAlso currentToken.Parent.IsKind(SyntaxKind.InterpolatedStringExpression) Then
                Return CreateAdjustSpacesOperation(0, AdjustSpacesOption.ForceSpaces)
            End If

            ' No space before { Or after } in interpolations
            If (currentToken.Kind = SyntaxKind.OpenBraceToken AndAlso currentToken.Parent.IsKind(SyntaxKind.Interpolation)) OrElse
               (previousToken.Kind = SyntaxKind.CloseBraceToken AndAlso previousToken.Parent.IsKind(SyntaxKind.Interpolation)) Then
                Return CreateAdjustSpacesOperation(0, AdjustSpacesOption.ForceSpaces)
            End If

            ' Preserve space after { Or before } in interpolations (i.e. between the braces And the expression)
            If (previousToken.Kind = SyntaxKind.OpenBraceToken AndAlso previousToken.Parent.IsKind(SyntaxKind.Interpolation)) OrElse
               (currentToken.Kind = SyntaxKind.CloseBraceToken AndAlso currentToken.Parent.IsKind(SyntaxKind.Interpolation)) Then
                Return CreateAdjustSpacesOperation(0, AdjustSpacesOption.PreserveSpaces)
            End If

            ' No space before Or after , in interpolation alignment clause
            If (previousToken.Kind = SyntaxKind.CommaToken AndAlso previousToken.Parent.IsKind(SyntaxKind.InterpolationAlignmentClause)) OrElse
               (currentToken.Kind = SyntaxKind.CommaToken AndAlso currentToken.Parent.IsKind(SyntaxKind.InterpolationAlignmentClause)) Then
                Return CreateAdjustSpacesOperation(0, AdjustSpacesOption.ForceSpaces)
            End If

            ' No space before Or after : in interpolation format clause
            If (previousToken.Kind = SyntaxKind.ColonToken AndAlso previousToken.Parent.IsKind(SyntaxKind.InterpolationFormatClause)) OrElse
               (currentToken.Kind = SyntaxKind.ColonToken AndAlso currentToken.Parent.IsKind(SyntaxKind.InterpolationFormatClause)) Then
                Return CreateAdjustSpacesOperation(0, AdjustSpacesOption.ForceSpaces)
            End If

            ' * }
            ' * )
            ' * ,
            ' * .
            ' * :=
            Select Case currentToken.Kind
                Case SyntaxKind.CloseParenToken, SyntaxKind.CommaToken
                    Return If(previousToken.Kind = SyntaxKind.EmptyToken AndAlso PrecedingTriviaContainsLineBreak(previousToken),
                        CreateAdjustSpacesOperation(0, AdjustSpacesOption.PreserveSpaces),
                        CreateAdjustSpacesOperation(0, AdjustSpacesOption.ForceSpacesIfOnSingleLine))

                Case SyntaxKind.CloseBraceToken, SyntaxKind.ColonEqualsToken
                    Return CreateAdjustSpacesOperation(0, AdjustSpacesOption.ForceSpacesIfOnSingleLine)

                Case SyntaxKind.DotToken
                    Dim space = If(previousToken.Kind = SyntaxKind.CallKeyword OrElse
                               previousToken.Kind = SyntaxKind.KeyKeyword, 1, 0)
                    Return CreateAdjustSpacesOperation(space, AdjustSpacesOption.ForceSpacesIfOnSingleLine)
            End Select

            ' { *
            ' ( *
            ' ) *
            ' . *
            ' := *
            Select Case previousToken.Kind
                Case SyntaxKind.OpenBraceToken, SyntaxKind.OpenParenToken, SyntaxKind.DotToken, SyntaxKind.ColonEqualsToken
                    Return CreateAdjustSpacesOperation(0, AdjustSpacesOption.ForceSpacesIfOnSingleLine)

                Case SyntaxKind.CloseParenToken
                    Dim space = If(previousToken.Kind = currentToken.Kind, 0, 1)
                    Return CreateAdjustSpacesOperation(space, AdjustSpacesOption.ForceSpacesIfOnSingleLine)
            End Select

            ' dictionary member access ! case
            If IsExclamationInDictionaryAccess(previousToken) Then
                Return CreateAdjustSpacesOperation(0, AdjustSpacesOption.ForceSpacesIfOnSingleLine)
            End If

            If IsExclamationInDictionaryAccess(currentToken) Then
                If Not currentToken.TrailingTrivia.Any(SyntaxKind.LineContinuationTrivia) AndAlso
                   previousToken.Kind <> SyntaxKind.WithKeyword AndAlso
                   previousToken.Kind <> SyntaxKind.EqualsToken Then
                    Return CreateAdjustSpacesOperation(0, AdjustSpacesOption.ForceSpacesIfOnSingleLine)
                End If
            End If

            ' * </
            If currentToken.Kind = SyntaxKind.LessThanSlashToken AndAlso
               FormattingHelpers.IsXmlToken(previousToken) Then
                Return CreateAdjustSpacesOperation(0, AdjustSpacesOption.ForceSpacesIfOnSingleLine)
            End If

            ' * />
            If currentToken.Kind = SyntaxKind.SlashGreaterThanToken Then
                Return CreateAdjustSpacesOperation(0, AdjustSpacesOption.ForceSpacesIfOnSingleLine)
            End If

            ' * > in xml literal
            If (currentToken.Kind = SyntaxKind.GreaterThanToken AndAlso
                FormattingHelpers.IsXmlToken(currentToken)) Then
                Return CreateAdjustSpacesOperation(0, AdjustSpacesOption.ForceSpacesIfOnSingleLine)
            End If

            ' +1 or -1
            If (previousToken.Kind = SyntaxKind.PlusToken OrElse
                previousToken.Kind = SyntaxKind.MinusToken) AndAlso
                TypeOf previousToken.Parent Is UnaryExpressionSyntax Then
                Return CreateAdjustSpacesOperation(0, AdjustSpacesOption.ForceSpacesIfOnSingleLine)
            End If

            ' nullable ? case
            If FormattingHelpers.IsQuestionInNullableType(currentToken) Then
                Return CreateAdjustSpacesOperation(0, AdjustSpacesOption.ForceSpacesIfOnSingleLine)
            End If

            ' <AttributeTarget : case
            If FormattingHelpers.IsColonAfterAttributeTarget(previousToken, currentToken) Then
                Return CreateAdjustSpacesOperation(0, AdjustSpacesOption.ForceSpacesIfOnSingleLine)
            End If

            If previousToken.Kind = SyntaxKind.EmptyToken OrElse currentToken.Kind = SyntaxKind.EmptyToken Then
                Return CreateAdjustSpacesOperation(0, AdjustSpacesOption.PreserveSpaces)
            End If

            ' Else If
            If previousToken.Kind = SyntaxKind.ElseKeyword AndAlso
               currentToken.Kind = SyntaxKind.IfKeyword AndAlso
               previousToken.Parent Is currentToken.Parent Then
                Return CreateAdjustSpacesOperation(0, AdjustSpacesOption.ForceSpacesIfOnSingleLine)
            End If

            ' label
            Dim labelStatement = TryCast(previousToken.Parent, LabelStatementSyntax)
            If labelStatement IsNot Nothing AndAlso
               labelStatement.LabelToken = previousToken AndAlso
               currentToken.Kind = SyntaxKind.ColonToken Then
                Return CreateAdjustSpacesOperation(0, AdjustSpacesOption.ForceSpacesIfOnSingleLine)
            End If

            Return nextFunc.Invoke()
        End Function

        Private Function PrecedingTriviaContainsLineBreak(previousToken As SyntaxToken) As Boolean
            Return ContainsLineBreak(previousToken.LeadingTrivia) OrElse ContainsLineBreak(previousToken.GetPreviousToken(includeZeroWidth:=True).TrailingTrivia)
        End Function

        Private Function ContainsLineBreak(triviaList As SyntaxTriviaList) As Boolean
            Return triviaList.Any(Function(t) t.Kind = SyntaxKind.EndOfLineTrivia)
        End Function
    End Class
End Namespace
