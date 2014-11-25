' Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
            If currentToken.VBKind = SyntaxKind.EndOfFileToken Then
                Return CreateAdjustSpacesOperation(0, AdjustSpacesOption.ForceSpacesIfOnSingleLine)
            End If

            ' ()
            If previousToken.VBKind = SyntaxKind.OpenParenToken AndAlso currentToken.VBKind = SyntaxKind.CloseParenToken Then
                Return CreateAdjustSpacesOperation(0, AdjustSpacesOption.ForceSpacesIfOnSingleLine)
            End If

            ' ,,
            If previousToken.VBKind = SyntaxKind.CommaToken AndAlso currentToken.VBKind = SyntaxKind.CommaToken Then
                Return CreateAdjustSpacesOperation(0, AdjustSpacesOption.ForceSpacesIfOnSingleLine)
            End If

            ' ( < case
            If previousToken.VBKind = SyntaxKind.OpenParenToken AndAlso
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
            If previousToken.VBKind = SyntaxKind.LessThanQuestionToken AndAlso FormattingHelpers.IsXmlTokenInXmlDeclaration(previousToken) Then
                Return CreateAdjustSpacesOperation(0, AdjustSpacesOption.ForceSpacesIfOnSingleLine)
            End If

            ' * ?> case
            If currentToken.VBKind = SyntaxKind.QuestionGreaterThanToken AndAlso FormattingHelpers.IsXmlTokenInXmlDeclaration(currentToken) Then
                Return CreateAdjustSpacesOperation(0, AdjustSpacesOption.ForceSpacesIfOnSingleLine)
            End If

            ' except <%= [xml token] or [xml token] %> case
            If (previousToken.VBKind <> SyntaxKind.LessThanPercentEqualsToken AndAlso FormattingHelpers.IsXmlToken(currentToken)) AndAlso
               (FormattingHelpers.IsXmlToken(previousToken) AndAlso currentToken.VBKind <> SyntaxKind.PercentGreaterThanToken) Then

                ' [xml token] [xml token]
                If FormattingHelpers.IsXmlToken(previousToken) AndAlso FormattingHelpers.IsXmlToken(currentToken) Then
                    Return CreateAdjustSpacesOperation(0, AdjustSpacesOption.ForceSpacesIfOnSingleLine)
                End If
            End If

            ' %> [xml name token]
            If previousToken.VBKind = SyntaxKind.PercentGreaterThanToken AndAlso currentToken.VBKind = SyntaxKind.XmlNameToken Then
                Return CreateAdjustSpacesOperation(1, AdjustSpacesOption.ForceSpacesIfOnSingleLine)
            End If

            ' [xml token] [xml name token]
            If FormattingHelpers.IsXmlToken(previousToken) AndAlso currentToken.VBKind = SyntaxKind.XmlNameToken Then
                Return CreateAdjustSpacesOperation(0, AdjustSpacesOption.ForceSpacesIfOnSingleLine)
            End If

            ' [xml name token] <%=
            If previousToken.VBKind = SyntaxKind.XmlNameToken AndAlso currentToken.VBKind = SyntaxKind.LessThanPercentEqualsToken Then
                Return CreateAdjustSpacesOperation(1, AdjustSpacesOption.ForceSpacesIfOnSingleLine)
            End If

            ' [xml name token] %>
            If previousToken.VBKind = SyntaxKind.XmlNameToken AndAlso currentToken.VBKind = SyntaxKind.PercentGreaterThanToken Then
                Return CreateAdjustSpacesOperation(1, AdjustSpacesOption.ForceSpacesIfOnSingleLine)
            End If

            ' [xml name token] [xml token]
            If previousToken.VBKind = SyntaxKind.XmlNameToken AndAlso FormattingHelpers.IsXmlToken(currentToken) Then
                Return CreateAdjustSpacesOperation(0, AdjustSpacesOption.ForceSpacesIfOnSingleLine)
            End If

            ' [xml name token] =
            If previousToken.VBKind = SyntaxKind.XmlNameToken AndAlso currentToken.VBKind = SyntaxKind.EqualsToken Then
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
            If previousToken.VBKind = SyntaxKind.EqualsToken AndAlso FormattingHelpers.IsQuoteInXmlString(currentToken) Then
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
            If (previousToken.VBKind = SyntaxKind.XmlTextLiteralToken AndAlso currentToken.VBKind <> SyntaxKind.XmlNameToken) OrElse
               (previousToken.VBKind <> SyntaxKind.XmlNameToken AndAlso currentToken.VBKind = SyntaxKind.XmlTextLiteralToken) Then
                Return CreateAdjustSpacesOperation(0, AdjustSpacesOption.ForceSpacesIfOnSingleLine)
            End If

            ' xml entity literal
            If previousToken.VBKind = SyntaxKind.XmlEntityLiteralToken OrElse
                currentToken.VBKind = SyntaxKind.XmlEntityLiteralToken Then
                Return CreateAdjustSpacesOperation(0, AdjustSpacesOption.PreserveSpaces)
            End If

            ' (( case
            If previousToken.VBKind = SyntaxKind.OpenParenToken AndAlso
               currentToken.VBKind = SyntaxKind.OpenParenToken Then
                Return CreateAdjustSpacesOperation(0, AdjustSpacesOption.ForceSpacesIfOnSingleLine)
            End If

            ' [identifier] ( case
            If previousToken.VBKind = SyntaxKind.IdentifierToken AndAlso
               currentToken.VBKind = SyntaxKind.OpenParenToken Then
                Return CreateAdjustSpacesOperation(0, AdjustSpacesOption.ForceSpacesIfOnSingleLine)
            End If

            ' [some keywords] ( case
            If currentToken.VBKind = SyntaxKind.OpenParenToken Then
                Select Case previousToken.VBKind
                    Case SyntaxKind.NewKeyword, SyntaxKind.FunctionKeyword, SyntaxKind.SubKeyword, SyntaxKind.SetKeyword,
                         SyntaxKind.AddHandlerKeyword, SyntaxKind.RemoveHandlerKeyword, SyntaxKind.RaiseEventKeyword,
                         SyntaxKind.GetTypeKeyword, SyntaxKind.CTypeKeyword, SyntaxKind.TryCastKeyword,
                         SyntaxKind.DirectCastKeyword, SyntaxKind.GetXmlNamespaceKeyword, SyntaxKind.NameOfKeyword
                        Return CreateAdjustSpacesOperation(0, AdjustSpacesOption.ForceSpacesIfOnSingleLine)
                End Select

                If SyntaxFacts.IsPredefinedCastExpressionKeyword(previousToken.VBKind) Then
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
            If currentToken.VBKind = SyntaxKind.OpenParenToken AndAlso TypeOf currentToken.Parent Is ArrayRankSpecifierSyntax Then
                Return CreateAdjustSpacesOperation(0, AdjustSpacesOption.ForceSpacesIfOnSingleLine)
            End If

            ' [overloadable operator] ( case
            If currentToken.VBKind = SyntaxKind.OpenParenToken AndAlso FormattingHelpers.IsOverloadableOperator(previousToken) Then
                Return CreateAdjustSpacesOperation(0, AdjustSpacesOption.ForceSpacesIfOnSingleLine)
            End If

            ' [type parameter list] [parameter list] )( case
            If previousToken.VBKind = SyntaxKind.CloseParenToken AndAlso TypeOf previousToken.Parent Is TypeParameterListSyntax AndAlso
               currentToken.VBKind = SyntaxKind.OpenParenToken AndAlso TypeOf currentToken.Parent Is ParameterListSyntax Then
                Return CreateAdjustSpacesOperation(0, AdjustSpacesOption.ForceSpacesIfOnSingleLine)
            End If

            ' , [named field initializer dot]
            If previousToken.VBKind = SyntaxKind.CommaToken AndAlso FormattingHelpers.IsNamedFieldInitializerDot(currentToken) Then
                Return CreateAdjustSpacesOperation(1, AdjustSpacesOption.ForceSpacesIfOnSingleLine)
            End If


            ' ? . [conditional access operator]
            If previousToken.VBKind = SyntaxKind.QuestionToken AndAlso currentToken.VBKind = SyntaxKind.DotToken AndAlso
                previousToken.Parent.IsKind(SyntaxKind.ConditionalAccessExpression) Then
                Return CreateAdjustSpacesOperation(0, AdjustSpacesOption.ForceSpacesIfOnSingleLine)
            End If

            ' identifier ? [conditional access operator]
            If previousToken.VBKind = SyntaxKind.IdentifierToken AndAlso currentToken.VBKind = SyntaxKind.QuestionToken AndAlso
                    currentToken.Parent.VBKind = SyntaxKind.ConditionalAccessExpression Then

                Return CreateAdjustSpacesOperation(0, AdjustSpacesOption.ForceSpacesIfOnSingleLine)
            End If

            ' Me ? [conditional access operator]
            If previousToken.VBKind = SyntaxKind.MeKeyword AndAlso currentToken.VBKind = SyntaxKind.QuestionToken AndAlso
                    currentToken.Parent.VBKind = SyntaxKind.ConditionalAccessExpression Then

                Return CreateAdjustSpacesOperation(0, AdjustSpacesOption.ForceSpacesIfOnSingleLine)
            End If

            ' MyBase ? [conditional access operator]
            If previousToken.VBKind = SyntaxKind.MyBaseKeyword AndAlso currentToken.VBKind = SyntaxKind.QuestionToken AndAlso
                    currentToken.Parent.VBKind = SyntaxKind.ConditionalAccessExpression Then

                Return CreateAdjustSpacesOperation(0, AdjustSpacesOption.ForceSpacesIfOnSingleLine)
            End If

            ' MyClass ? [conditional access operator]
            If previousToken.VBKind = SyntaxKind.MyClassExpression AndAlso currentToken.VBKind = SyntaxKind.QuestionToken AndAlso
                    currentToken.Parent.VBKind = SyntaxKind.ConditionalAccessExpression Then

                Return CreateAdjustSpacesOperation(0, AdjustSpacesOption.ForceSpacesIfOnSingleLine)
            End If

            ' } ? [conditional access operator after initializer]
            If previousToken.VBKind = SyntaxKind.CloseBraceToken AndAlso currentToken.VBKind = SyntaxKind.QuestionToken AndAlso
                    currentToken.Parent.VBKind = SyntaxKind.ConditionalAccessExpression Then

                Return CreateAdjustSpacesOperation(0, AdjustSpacesOption.ForceSpacesIfOnSingleLine)
            End If

            ' " ? [conditional access operator after string literal]
            If previousToken.VBKind = SyntaxKind.StringLiteralToken AndAlso currentToken.VBKind = SyntaxKind.QuestionToken AndAlso
                    currentToken.Parent.VBKind = SyntaxKind.ConditionalAccessExpression Then

                Return CreateAdjustSpacesOperation(0, AdjustSpacesOption.ForceSpacesIfOnSingleLine)
            End If

            ' ) ? [conditional access off invocation]
            If previousToken.VBKind = SyntaxKind.CloseParenToken AndAlso currentToken.VBKind = SyntaxKind.QuestionToken AndAlso
                    currentToken.Parent.VBKind = SyntaxKind.ConditionalAccessExpression Then

                Return CreateAdjustSpacesOperation(0, AdjustSpacesOption.ForceSpacesIfOnSingleLine)
            End If

            ' * [member access dot without expression]
            If previousToken.VBKind <> SyntaxKind.OpenParenToken AndAlso FormattingHelpers.IsMemberAccessDotWithoutExpression(currentToken) Then
                Return CreateAdjustSpacesOperation(1, AdjustSpacesOption.ForceSpacesIfOnSingleLine)
            End If

            ' * }
            ' * )
            ' * ,
            ' * .
            ' * :=
            Select Case currentToken.VBKind
                Case SyntaxKind.CloseBraceToken, SyntaxKind.CloseParenToken, SyntaxKind.CommaToken, SyntaxKind.ColonEqualsToken
                    Return CreateAdjustSpacesOperation(0, AdjustSpacesOption.ForceSpacesIfOnSingleLine)

                Case SyntaxKind.DotToken
                    Dim space = If(previousToken.VBKind = SyntaxKind.CallKeyword OrElse
                                   previousToken.VBKind = SyntaxKind.KeyKeyword, 1, 0)
                    Return CreateAdjustSpacesOperation(space, AdjustSpacesOption.ForceSpacesIfOnSingleLine)
            End Select

            ' { *
            ' ( *
            ' ) *
            ' . *
            ' := *
            Select Case previousToken.VBKind
                Case SyntaxKind.OpenBraceToken, SyntaxKind.OpenParenToken, SyntaxKind.DotToken, SyntaxKind.ColonEqualsToken
                    Return CreateAdjustSpacesOperation(0, AdjustSpacesOption.ForceSpacesIfOnSingleLine)

                Case SyntaxKind.CloseParenToken
                    Dim space = If(previousToken.VBKind = currentToken.VBKind, 0, 1)
                    Return CreateAdjustSpacesOperation(space, AdjustSpacesOption.ForceSpacesIfOnSingleLine)
            End Select

            ' dictionary member access ! case
            If IsExclamationInDictionaryAccess(previousToken) Then
                Return CreateAdjustSpacesOperation(0, AdjustSpacesOption.ForceSpacesIfOnSingleLine)
            End If

            If IsExclamationInDictionaryAccess(currentToken) Then
                If Not currentToken.TrailingTrivia.Any(SyntaxKind.LineContinuationTrivia) AndAlso
                   previousToken.VBKind <> SyntaxKind.WithKeyword AndAlso
                   previousToken.VBKind <> SyntaxKind.EqualsToken Then
                    Return CreateAdjustSpacesOperation(0, AdjustSpacesOption.ForceSpacesIfOnSingleLine)
                End If
            End If

            ' * </
            If currentToken.VBKind = SyntaxKind.LessThanSlashToken AndAlso
               FormattingHelpers.IsXmlToken(previousToken) Then
                Return CreateAdjustSpacesOperation(0, AdjustSpacesOption.ForceSpacesIfOnSingleLine)
            End If

            ' * />
            If currentToken.VBKind = SyntaxKind.SlashGreaterThanToken Then
                Return CreateAdjustSpacesOperation(0, AdjustSpacesOption.ForceSpacesIfOnSingleLine)
            End If

            ' * > in xml literal
            If (currentToken.VBKind = SyntaxKind.GreaterThanToken AndAlso
                FormattingHelpers.IsXmlToken(currentToken)) Then
                Return CreateAdjustSpacesOperation(0, AdjustSpacesOption.ForceSpacesIfOnSingleLine)
            End If

            ' +1 or -1
            If (previousToken.VBKind = SyntaxKind.PlusToken OrElse
                previousToken.VBKind = SyntaxKind.MinusToken) AndAlso
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

            If previousToken.VBKind = SyntaxKind.EmptyToken OrElse currentToken.VBKind = SyntaxKind.EmptyToken Then
                Return CreateAdjustSpacesOperation(0, AdjustSpacesOption.PreserveSpaces)
            End If

            ' Else If
            If previousToken.VBKind = SyntaxKind.ElseKeyword AndAlso
               currentToken.VBKind = SyntaxKind.IfKeyword AndAlso
               previousToken.Parent Is currentToken.Parent Then
                Return CreateAdjustSpacesOperation(0, AdjustSpacesOption.ForceSpacesIfOnSingleLine)
            End If

            ' label
            Dim labelStatement = TryCast(previousToken.Parent, LabelStatementSyntax)
            If labelStatement IsNot Nothing AndAlso
               labelStatement.LabelToken = previousToken AndAlso
               currentToken.VBKind = SyntaxKind.ColonToken Then
                Return CreateAdjustSpacesOperation(0, AdjustSpacesOption.ForceSpacesIfOnSingleLine)
            End If


            Return nextFunc.Invoke()
        End Function
    End Class
End Namespace