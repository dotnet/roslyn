' Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.Formatting.Rules
Imports Microsoft.CodeAnalysis.Options
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.Formatting
#If MEF Then
    <ExportFormattingRule(AdjustSpaceFormattingRule.Name, LanguageNames.VisualBasic)>
    <ExtensionOrder(After:=ElasticTriviaFormattingRule.Name)>
    Friend Class AdjustSpaceFormattingRule
#Else
    Friend Class AdjustSpaceFormattingRule
#End If
        Inherits BaseFormattingRule
        Friend Const Name As String = "VisualBasic Adjust Space Formatting Rule"

        Public Sub New()
        End Sub

        Public Overrides Function GetAdjustSpacesOperation(previousToken As SyntaxToken, currentToken As SyntaxToken, optionSet As OptionSet, nextFunc As NextOperation(Of AdjustSpacesOperation)) As AdjustSpacesOperation
            ' * <end of file token>
            If currentToken.VisualBasicKind = SyntaxKind.EndOfFileToken Then
                Return CreateAdjustSpacesOperation(0, AdjustSpacesOption.ForceSpacesIfOnSingleLine)
            End If

            ' ()
            If previousToken.VisualBasicKind = SyntaxKind.OpenParenToken AndAlso currentToken.VisualBasicKind = SyntaxKind.CloseParenToken Then
                Return CreateAdjustSpacesOperation(0, AdjustSpacesOption.ForceSpacesIfOnSingleLine)
            End If

            ' ,,
            If previousToken.VisualBasicKind = SyntaxKind.CommaToken AndAlso currentToken.VisualBasicKind = SyntaxKind.CommaToken Then
                Return CreateAdjustSpacesOperation(0, AdjustSpacesOption.ForceSpacesIfOnSingleLine)
            End If

            ' ( < case
            If previousToken.VisualBasicKind = SyntaxKind.OpenParenToken AndAlso
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
            If previousToken.VisualBasicKind = SyntaxKind.LessThanQuestionToken AndAlso FormattingHelpers.IsXmlTokenInXmlDeclaration(previousToken) Then
                Return CreateAdjustSpacesOperation(0, AdjustSpacesOption.ForceSpacesIfOnSingleLine)
            End If

            ' * ?> case
            If currentToken.VisualBasicKind = SyntaxKind.QuestionGreaterThanToken AndAlso FormattingHelpers.IsXmlTokenInXmlDeclaration(currentToken) Then
                Return CreateAdjustSpacesOperation(0, AdjustSpacesOption.ForceSpacesIfOnSingleLine)
            End If

            ' except <%= [xml token] or [xml token] %> case
            If (previousToken.VisualBasicKind <> SyntaxKind.LessThanPercentEqualsToken AndAlso FormattingHelpers.IsXmlToken(currentToken)) AndAlso
               (FormattingHelpers.IsXmlToken(previousToken) AndAlso currentToken.VisualBasicKind <> SyntaxKind.PercentGreaterThanToken) Then

                ' [xml token] [xml token]
                If FormattingHelpers.IsXmlToken(previousToken) AndAlso FormattingHelpers.IsXmlToken(currentToken) Then
                    Return CreateAdjustSpacesOperation(0, AdjustSpacesOption.ForceSpacesIfOnSingleLine)
                End If
            End If

            ' %> [xml name token]
            If previousToken.VisualBasicKind = SyntaxKind.PercentGreaterThanToken AndAlso currentToken.VisualBasicKind = SyntaxKind.XmlNameToken Then
                Return CreateAdjustSpacesOperation(1, AdjustSpacesOption.ForceSpacesIfOnSingleLine)
            End If

            ' [xml token] [xml name token]
            If FormattingHelpers.IsXmlToken(previousToken) AndAlso currentToken.VisualBasicKind = SyntaxKind.XmlNameToken Then
                Return CreateAdjustSpacesOperation(0, AdjustSpacesOption.ForceSpacesIfOnSingleLine)
            End If

            ' [xml name token] <%=
            If previousToken.VisualBasicKind = SyntaxKind.XmlNameToken AndAlso currentToken.VisualBasicKind = SyntaxKind.LessThanPercentEqualsToken Then
                Return CreateAdjustSpacesOperation(1, AdjustSpacesOption.ForceSpacesIfOnSingleLine)
            End If

            ' [xml name token] %>
            If previousToken.VisualBasicKind = SyntaxKind.XmlNameToken AndAlso currentToken.VisualBasicKind = SyntaxKind.PercentGreaterThanToken Then
                Return CreateAdjustSpacesOperation(1, AdjustSpacesOption.ForceSpacesIfOnSingleLine)
            End If

            ' [xml name token] [xml token]
            If previousToken.VisualBasicKind = SyntaxKind.XmlNameToken AndAlso FormattingHelpers.IsXmlToken(currentToken) Then
                Return CreateAdjustSpacesOperation(0, AdjustSpacesOption.ForceSpacesIfOnSingleLine)
            End If

            ' [xml name token] =
            If previousToken.VisualBasicKind = SyntaxKind.XmlNameToken AndAlso currentToken.VisualBasicKind = SyntaxKind.EqualsToken Then
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
            If previousToken.VisualBasicKind = SyntaxKind.EqualsToken AndAlso FormattingHelpers.IsQuoteInXmlString(currentToken) Then
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
            If (previousToken.VisualBasicKind = SyntaxKind.XmlTextLiteralToken AndAlso currentToken.VisualBasicKind <> SyntaxKind.XmlNameToken) OrElse
               (previousToken.VisualBasicKind <> SyntaxKind.XmlNameToken AndAlso currentToken.VisualBasicKind = SyntaxKind.XmlTextLiteralToken) Then
                Return CreateAdjustSpacesOperation(0, AdjustSpacesOption.ForceSpacesIfOnSingleLine)
            End If

            ' xml entity literal
            If previousToken.VisualBasicKind = SyntaxKind.XmlEntityLiteralToken OrElse
                currentToken.VisualBasicKind = SyntaxKind.XmlEntityLiteralToken Then
                Return CreateAdjustSpacesOperation(0, AdjustSpacesOption.PreserveSpaces)
            End If

            ' (( case
            If previousToken.VisualBasicKind = SyntaxKind.OpenParenToken AndAlso
               currentToken.VisualBasicKind = SyntaxKind.OpenParenToken Then
                Return CreateAdjustSpacesOperation(0, AdjustSpacesOption.ForceSpacesIfOnSingleLine)
            End If

            ' [identifier] ( case
            If previousToken.VisualBasicKind = SyntaxKind.IdentifierToken AndAlso
               currentToken.VisualBasicKind = SyntaxKind.OpenParenToken Then
                Return CreateAdjustSpacesOperation(0, AdjustSpacesOption.ForceSpacesIfOnSingleLine)
            End If

            ' [some keywords] ( case
            If currentToken.VisualBasicKind = SyntaxKind.OpenParenToken Then
                Select Case previousToken.VisualBasicKind
                    Case SyntaxKind.NewKeyword, SyntaxKind.FunctionKeyword, SyntaxKind.SubKeyword, SyntaxKind.SetKeyword,
                         SyntaxKind.AddHandlerKeyword, SyntaxKind.RemoveHandlerKeyword, SyntaxKind.RaiseEventKeyword,
                         SyntaxKind.GetTypeKeyword, SyntaxKind.CTypeKeyword, SyntaxKind.TryCastKeyword,
                         SyntaxKind.DirectCastKeyword, SyntaxKind.GetXmlNamespaceKeyword
                        Return CreateAdjustSpacesOperation(0, AdjustSpacesOption.ForceSpacesIfOnSingleLine)
                End Select

                If SyntaxFacts.IsPredefinedCastExpressionKeyword(previousToken.VisualBasicKind) Then
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
            If currentToken.VisualBasicKind = SyntaxKind.OpenParenToken AndAlso TypeOf currentToken.Parent Is ArrayRankSpecifierSyntax Then
                Return CreateAdjustSpacesOperation(0, AdjustSpacesOption.ForceSpacesIfOnSingleLine)
            End If

            ' [overloadable operator] ( case
            If currentToken.VisualBasicKind = SyntaxKind.OpenParenToken AndAlso FormattingHelpers.IsOverloadableOperator(previousToken) Then
                Return CreateAdjustSpacesOperation(0, AdjustSpacesOption.ForceSpacesIfOnSingleLine)
            End If

            ' [type parameter list] [parameter list] )( case
            If previousToken.VisualBasicKind = SyntaxKind.CloseParenToken AndAlso TypeOf previousToken.Parent Is TypeParameterListSyntax AndAlso
               currentToken.VisualBasicKind = SyntaxKind.OpenParenToken AndAlso TypeOf currentToken.Parent Is ParameterListSyntax Then
                Return CreateAdjustSpacesOperation(0, AdjustSpacesOption.ForceSpacesIfOnSingleLine)
            End If

            ' , [named field initializer dot]
            If previousToken.VisualBasicKind = SyntaxKind.CommaToken AndAlso FormattingHelpers.IsNamedFieldInitializerDot(currentToken) Then
                Return CreateAdjustSpacesOperation(1, AdjustSpacesOption.ForceSpacesIfOnSingleLine)
            End If


            ' ? . [conditional access operator]
            If previousToken.VisualBasicKind = SyntaxKind.QuestionToken AndAlso currentToken.VisualBasicKind = SyntaxKind.DotToken AndAlso
                previousToken.Parent.IsKind(SyntaxKind.ConditionalAccessExpression) Then
                Return CreateAdjustSpacesOperation(0, AdjustSpacesOption.ForceSpacesIfOnSingleLine)
            End If

            ' identifier ? [conditional access operator]
            If previousToken.VisualBasicKind = SyntaxKind.IdentifierToken AndAlso currentToken.VisualBasicKind = SyntaxKind.QuestionToken AndAlso
                    currentToken.Parent.VisualBasicKind = SyntaxKind.ConditionalAccessExpression Then

                Return CreateAdjustSpacesOperation(0, AdjustSpacesOption.ForceSpacesIfOnSingleLine)
            End If

            ' ) ? [conditional access off invocation]
            If previousToken.VisualBasicKind = SyntaxKind.CloseParenToken AndAlso currentToken.VisualBasicKind = SyntaxKind.QuestionToken AndAlso
                    currentToken.Parent.VisualBasicKind = SyntaxKind.ConditionalAccessExpression Then

                Return CreateAdjustSpacesOperation(0, AdjustSpacesOption.ForceSpacesIfOnSingleLine)
            End If


            ' * [member access dot without expression]
            If previousToken.VisualBasicKind <> SyntaxKind.OpenParenToken AndAlso FormattingHelpers.IsMemberAccessDotWithoutExpression(currentToken) Then
                Return CreateAdjustSpacesOperation(1, AdjustSpacesOption.ForceSpacesIfOnSingleLine)
            End If

            ' * }
            ' * )
            ' * ,
            ' * .
            ' * :=
            Select Case currentToken.VisualBasicKind
                Case SyntaxKind.CloseBraceToken, SyntaxKind.CloseParenToken, SyntaxKind.CommaToken, SyntaxKind.ColonEqualsToken
                    Return CreateAdjustSpacesOperation(0, AdjustSpacesOption.ForceSpacesIfOnSingleLine)

                Case SyntaxKind.DotToken
                    Dim space = If(previousToken.VisualBasicKind = SyntaxKind.CallKeyword OrElse
                                   previousToken.VisualBasicKind = SyntaxKind.KeyKeyword, 1, 0)
                    Return CreateAdjustSpacesOperation(space, AdjustSpacesOption.ForceSpacesIfOnSingleLine)
            End Select

            ' { *
            ' ( *
            ' ) *
            ' . *
            ' := *
            Select Case previousToken.VisualBasicKind
                Case SyntaxKind.OpenBraceToken, SyntaxKind.OpenParenToken, SyntaxKind.DotToken, SyntaxKind.ColonEqualsToken
                    Return CreateAdjustSpacesOperation(0, AdjustSpacesOption.ForceSpacesIfOnSingleLine)

                Case SyntaxKind.CloseParenToken
                    Dim space = If(previousToken.VisualBasicKind = currentToken.VisualBasicKind, 0, 1)
                    Return CreateAdjustSpacesOperation(space, AdjustSpacesOption.ForceSpacesIfOnSingleLine)
            End Select

            ' dictionary member access ! case
            If IsExclamationInDictionaryAccess(previousToken) Then
                Return CreateAdjustSpacesOperation(0, AdjustSpacesOption.ForceSpacesIfOnSingleLine)
            End If

            If IsExclamationInDictionaryAccess(currentToken) Then
                If Not currentToken.TrailingTrivia.Any(SyntaxKind.LineContinuationTrivia) AndAlso
                   previousToken.VisualBasicKind <> SyntaxKind.WithKeyword AndAlso
                   previousToken.VisualBasicKind <> SyntaxKind.EqualsToken Then
                    Return CreateAdjustSpacesOperation(0, AdjustSpacesOption.ForceSpacesIfOnSingleLine)
                End If
            End If

            ' * </
            If currentToken.VisualBasicKind = SyntaxKind.LessThanSlashToken AndAlso
               FormattingHelpers.IsXmlToken(previousToken) Then
                Return CreateAdjustSpacesOperation(0, AdjustSpacesOption.ForceSpacesIfOnSingleLine)
            End If

            ' * />
            If currentToken.VisualBasicKind = SyntaxKind.SlashGreaterThanToken Then
                Return CreateAdjustSpacesOperation(0, AdjustSpacesOption.ForceSpacesIfOnSingleLine)
            End If

            ' * > in xml literal
            If (currentToken.VisualBasicKind = SyntaxKind.GreaterThanToken AndAlso
                FormattingHelpers.IsXmlToken(currentToken)) Then
                Return CreateAdjustSpacesOperation(0, AdjustSpacesOption.ForceSpacesIfOnSingleLine)
            End If

            ' +1 or -1
            If (previousToken.VisualBasicKind = SyntaxKind.PlusToken OrElse
                previousToken.VisualBasicKind = SyntaxKind.MinusToken) AndAlso
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

            If previousToken.VisualBasicKind = SyntaxKind.EmptyToken OrElse currentToken.VisualBasicKind = SyntaxKind.EmptyToken Then
                Return CreateAdjustSpacesOperation(0, AdjustSpacesOption.PreserveSpaces)
            End If

            ' Else If
            If previousToken.VisualBasicKind = SyntaxKind.ElseKeyword AndAlso
               currentToken.VisualBasicKind = SyntaxKind.IfKeyword AndAlso
               previousToken.Parent Is currentToken.Parent Then
                Return CreateAdjustSpacesOperation(0, AdjustSpacesOption.ForceSpacesIfOnSingleLine)
            End If

            ' label
            Dim labelStatement = TryCast(previousToken.Parent, LabelStatementSyntax)
            If labelStatement IsNot Nothing AndAlso
               labelStatement.LabelToken = previousToken AndAlso
               currentToken.VisualBasicKind = SyntaxKind.ColonToken Then
                Return CreateAdjustSpacesOperation(0, AdjustSpacesOption.ForceSpacesIfOnSingleLine)
            End If


            Return nextFunc.Invoke()
        End Function
    End Class
End Namespace