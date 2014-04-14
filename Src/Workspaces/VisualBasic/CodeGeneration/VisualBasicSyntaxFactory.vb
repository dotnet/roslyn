' Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.CodeGeneration
Imports Microsoft.CodeAnalysis.Simplification
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Microsoft.CodeAnalysis.Host
Imports Microsoft.CodeAnalysis.Host.Mef

Namespace Microsoft.CodeAnalysis.VisualBasic.CodeGeneration
    <ExportLanguageService(GetType(ISyntaxFactoryService), LanguageNames.VisualBasic)>
    Friend Class VisualBasicSyntaxFactory
        Inherits AbstractSyntaxFactory

        Private Function Parenthesize(expression As SyntaxNode) As ParenthesizedExpressionSyntax
            Return DirectCast(expression, ExpressionSyntax).Parenthesize()
        End Function

        Public Overrides Function CreateAddExpression(left As SyntaxNode, right As SyntaxNode) As SyntaxNode
            Return SyntaxFactory.AddExpression(Parenthesize(left), Parenthesize(right))
        End Function

        Public Overloads Overrides Function CreateArgument(nameOpt As String, refKind As RefKind, expression As SyntaxNode) As SyntaxNode
            If TypeOf expression Is ArgumentSyntax Then
                Return expression
            End If

            If nameOpt Is Nothing Then
                Return SyntaxFactory.SimpleArgument(DirectCast(expression, ExpressionSyntax))
            End If

            Return SyntaxFactory.NamedArgument(nameOpt.ToIdentifierName,
                DirectCast(expression, ExpressionSyntax))
        End Function

        Public Overrides Function CreateAsExpression(expression As SyntaxNode, type As ITypeSymbol) As SyntaxNode
            Return SyntaxFactory.TryCastExpression(DirectCast(expression, ExpressionSyntax), type.GenerateTypeSyntax())
        End Function

        Public Overrides Function CreateAssignExpression(left As SyntaxNode, right As SyntaxNode) As SyntaxNode
            Return SyntaxFactory.SimpleAssignmentStatement(
                DirectCast(left, ExpressionSyntax),
                SyntaxFactory.Token(SyntaxKind.EqualsToken),
                DirectCast(right, ExpressionSyntax))
        End Function

        Public Overrides Function CreateBaseExpression() As SyntaxNode
            Return SyntaxFactory.MyBaseExpression()
        End Function

        Public Overrides Function CreateBinaryAndExpression(left As SyntaxNode, right As SyntaxNode) As SyntaxNode
            Return SyntaxFactory.AndExpression(Parenthesize(left), Parenthesize(right))
        End Function

        Public Overrides Function CreateBinaryOrExpression(left As SyntaxNode, right As SyntaxNode) As SyntaxNode
            Return SyntaxFactory.OrExpression(Parenthesize(left), Parenthesize(right))
        End Function

        Public Overrides Function CreateCastExpression(type As ITypeSymbol, expression As SyntaxNode) As SyntaxNode
            Return SyntaxFactory.DirectCastExpression(DirectCast(expression, ExpressionSyntax), type.GenerateTypeSyntax())
        End Function

        Public Overrides Function CreateConvertExpression(type As ITypeSymbol, expression As SyntaxNode) As SyntaxNode
            Return SyntaxFactory.CTypeExpression(DirectCast(expression, ExpressionSyntax), type.GenerateTypeSyntax())
        End Function

        Public Overrides Function CreateConditionalExpression(condition As SyntaxNode, whenTrue As SyntaxNode, whenFalse As SyntaxNode) As SyntaxNode
            Return SyntaxFactory.TernaryConditionalExpression(
                Parenthesize(condition),
                Parenthesize(whenTrue),
                Parenthesize(whenFalse))
        End Function

        Public Overrides Function CreateConstantExpression(value As Object) As SyntaxNode
            Return ExpressionGenerator.GenerateNonEnumValueExpression(Nothing, value, canUseFieldReference:=True)
        End Function

        Public Overrides Function CreateDefaultExpression(type As ITypeSymbol) As SyntaxNode
            Return SyntaxFactory.NothingLiteralExpression(SyntaxFactory.Token(SyntaxKind.NothingKeyword))
        End Function

        Public Overloads Overrides Function CreateElementAccessExpression(expression As SyntaxNode, arguments As IList(Of SyntaxNode)) As SyntaxNode
            Return SyntaxFactory.InvocationExpression(
                Parenthesize(DirectCast(expression, ExpressionSyntax)),
                CreateArgumentList(arguments))
        End Function

        Public Overrides Function CreateExpressionStatement(expression As SyntaxNode) As SyntaxNode
            If TypeOf expression Is InvocationExpressionSyntax Then
                Return SyntaxFactory.CallStatement(SyntaxFactory.Token(SyntaxKind.CallKeyword), DirectCast(expression, ExpressionSyntax)).
                    WithAdditionalAnnotations(Simplifier.Annotation)
            End If

            If TypeOf expression Is StatementSyntax Then
                Return expression
            End If

            Throw New NotImplementedException()
        End Function

        Public Overloads Overrides Function CreateGenericName(identifier As String, typeArguments As IList(Of ITypeSymbol)) As SyntaxNode
            Return SyntaxFactory.GenericName(
                identifier.ToIdentifierToken,
                SyntaxFactory.TypeArgumentList(
                    SyntaxFactory.SeparatedList(typeArguments.Select(Function(t) t.GenerateTypeSyntax())))).WithAdditionalAnnotations(Simplifier.Annotation)
        End Function

        Public Overrides Function CreateIdentifierName(identifier As String) As SyntaxNode
            Return identifier.ToIdentifierName()
        End Function

        Public Overrides Function CreateIfStatement(condition As SyntaxNode, trueStatements As IList(Of SyntaxNode), Optional falseStatementsOpt As IList(Of SyntaxNode) = Nothing) As SyntaxNode
            Return SyntaxFactory.MultiLineIfBlock(
                SyntaxFactory.IfPart(SyntaxFactory.IfStatement(SyntaxFactory.Token(SyntaxKind.IfKeyword), DirectCast(condition, ExpressionSyntax)),
                    SyntaxFactory.List(trueStatements.Cast(Of StatementSyntax))),
                Nothing,
                If(falseStatementsOpt Is Nothing,
                    Nothing,
                    SyntaxFactory.ElsePart(SyntaxFactory.List(falseStatementsOpt.Cast(Of StatementSyntax)))))
        End Function

        Public Overloads Overrides Function CreateInvocationExpression(expression As SyntaxNode, arguments As IList(Of SyntaxNode)) As SyntaxNode
            Return SyntaxFactory.InvocationExpression(DirectCast(expression, ExpressionSyntax), CreateArgumentList(arguments))
        End Function

        Public Overrides Function CreateIsExpression(expression As SyntaxNode, type As ITypeSymbol) As SyntaxNode
            Return SyntaxFactory.TypeOfIsExpression(Parenthesize(expression), type.GenerateTypeSyntax())
        End Function

        Public Overrides Function CreateLogicalAndExpression(left As SyntaxNode, right As SyntaxNode) As SyntaxNode
            Return SyntaxFactory.AndAlsoExpression(Parenthesize(left), Parenthesize(right))
        End Function

        Public Overrides Function CreateLogicalNotExpression(expression As SyntaxNode) As SyntaxNode
            Return SyntaxFactory.NotExpression(Parenthesize(expression))
        End Function

        Public Overrides Function CreateLogicalOrExpression(left As SyntaxNode, right As SyntaxNode) As SyntaxNode
            Return SyntaxFactory.OrElseExpression(Parenthesize(left), Parenthesize(right))
        End Function

        Public Overrides Function CreateMemberAccessExpression(expression As SyntaxNode, simpleName As SyntaxNode) As SyntaxNode
            Dim expressionSyntax = DirectCast(expression, expressionSyntax)
            If Not expressionSyntax.IsMeMyBaseOrMyClass() Then
                expressionSyntax = expressionSyntax.Parenthesize()
            End If

            Return SyntaxFactory.SimpleMemberAccessExpression(
                expressionSyntax,
                SyntaxFactory.Token(SyntaxKind.DotToken),
                DirectCast(simpleName, SimpleNameSyntax))
        End Function

        Public Overrides Function CreateMultiplyExpression(left As SyntaxNode, right As SyntaxNode) As SyntaxNode
            Return SyntaxFactory.MultiplyExpression(Parenthesize(left), Parenthesize(right))
        End Function

        Public Overrides Function CreateNegateExpression(expression As SyntaxNode) As SyntaxNode
            Return SyntaxFactory.UnaryMinusExpression(Parenthesize(expression))
        End Function

        Public Overloads Overrides Function CreateObjectCreationExpression(typeName As ITypeSymbol, arguments As IList(Of SyntaxNode)) As SyntaxNode
            Return SyntaxFactory.ObjectCreationExpression(
                Nothing,
                typeName.GenerateTypeSyntax(),
                CreateArgumentList(arguments),
                Nothing)
        End Function

        Public Overrides Function CreateQualifiedName(left As SyntaxNode, right As SyntaxNode) As SyntaxNode
            Return SyntaxFactory.QualifiedName(DirectCast(left, NameSyntax), DirectCast(right, SimpleNameSyntax))
        End Function

        Public Overrides Function CreateRawExpression(text As String) As SyntaxNode
            Throw New NotImplementedException()
        End Function

        Public Overrides Function CreateReferenceEqualsExpression(left As SyntaxNode, right As SyntaxNode) As SyntaxNode
            Return SyntaxFactory.IsExpression(Parenthesize(left), Parenthesize(right))
        End Function

        Public Overrides Function CreateReferenceNotEqualsExpression(left As SyntaxNode, right As SyntaxNode) As SyntaxNode
            Return SyntaxFactory.IsNotExpression(Parenthesize(left), Parenthesize(right))
        End Function

        Public Overrides Function CreateReturnStatement(Optional expressionOpt As SyntaxNode = Nothing) As SyntaxNode
            Return SyntaxFactory.ReturnStatement(DirectCast(expressionOpt, ExpressionSyntax))
        End Function

        Public Overrides Function CreateThisExpression() As SyntaxNode
            Return SyntaxFactory.MeExpression()
        End Function

        Public Overrides Function CreateThrowStatement(Optional expressionOpt As SyntaxNode = Nothing) As SyntaxNode
            Return SyntaxFactory.ThrowStatement(DirectCast(expressionOpt, ExpressionSyntax))
        End Function

        Public Overrides Function CreateTypeReferenceExpression(typeSymbol As INamedTypeSymbol) As SyntaxNode
            Return typeSymbol.GenerateExpressionSyntax()
        End Function

        Public Overloads Overrides Function CreateUsingStatement(variableDeclarationOrExpression As SyntaxNode, statements As IList(Of SyntaxNode)) As SyntaxNode
            Return SyntaxFactory.UsingBlock(
                SyntaxFactory.UsingStatement(
                    TryCast(variableDeclarationOrExpression, ExpressionSyntax),
                    If(TypeOf variableDeclarationOrExpression Is VariableDeclaratorSyntax,
                       SyntaxFactory.SingletonSeparatedList(DirectCast(variableDeclarationOrExpression, VariableDeclaratorSyntax)),
                       Nothing)),
                SyntaxFactory.List(statements.Cast(Of StatementSyntax)))
        End Function

        Public Overrides Function CreateValueEqualsExpression(left As SyntaxNode, right As SyntaxNode) As SyntaxNode
            Return SyntaxFactory.EqualsExpression(Parenthesize(left), Parenthesize(right))
        End Function

        Public Overrides Function CreateValueNotEqualsExpression(left As SyntaxNode, right As SyntaxNode) As SyntaxNode
            Return SyntaxFactory.NotEqualsExpression(Parenthesize(left), Parenthesize(right))
        End Function

        Private Function CreateArgumentList(arguments As IList(Of SyntaxNode)) As ArgumentListSyntax
            Return SyntaxFactory.ArgumentList(CreateArguments(arguments))
        End Function

        Private Function CreateArguments(arguments As IList(Of SyntaxNode)) As SeparatedSyntaxList(Of ArgumentSyntax)
            Return SyntaxFactory.SeparatedList(arguments.Select(AddressOf CreateArgument).Cast(Of ArgumentSyntax))
        End Function

        Public Overloads Overrides Function CreateLocalDeclarationStatement(isConst As Boolean, type As ITypeSymbol, variableDeclarator As SyntaxNode) As SyntaxNode
            Return SyntaxFactory.LocalDeclarationStatement(
                SyntaxFactory.TokenList(SyntaxFactory.Token(If(isConst, SyntaxKind.ConstKeyword, SyntaxKind.DimKeyword))),
                SyntaxFactory.SingletonSeparatedList(DirectCast(variableDeclarator, VariableDeclaratorSyntax)))
        End Function

        Public Overloads Overrides Function CreateVariableDeclarator(type As ITypeSymbol, name As String, Optional expressionOpt As SyntaxNode = Nothing) As SyntaxNode
            Return SyntaxFactory.VariableDeclarator(
                SyntaxFactory.SingletonSeparatedList(name.ToModifiedIdentifier),
                If(type Is Nothing, Nothing, SyntaxFactory.SimpleAsClause(type.GenerateTypeSyntax())),
                If(expressionOpt Is Nothing,
                   Nothing,
                   SyntaxFactory.EqualsValue(DirectCast(expressionOpt, ExpressionSyntax))))
        End Function

        Public Overrides Function CreateSwitchLabel(Optional expressionOpt As SyntaxNode = Nothing) As SyntaxNode
            If expressionOpt Is Nothing Then
                Return SyntaxFactory.CaseElseStatement(SyntaxFactory.CaseElseClause())
            Else
                Return SyntaxFactory.CaseStatement(SyntaxFactory.CaseValueClause(DirectCast(expressionOpt, ExpressionSyntax)))
            End If
        End Function

        Public Overloads Overrides Function CreateSwitchSection(switchLabel As SyntaxNode, statements As IList(Of SyntaxNode)) As SyntaxNode
            Return SyntaxFactory.CaseBlock(
                DirectCast(switchLabel, CaseStatementSyntax),
                SyntaxFactory.List(statements.Cast(Of StatementSyntax)))
        End Function

        Public Overloads Overrides Function CreateSwitchStatement(expression As SyntaxNode, switchSections As IList(Of SyntaxNode)) As SyntaxNode
            Return SyntaxFactory.SelectBlock(
                SyntaxFactory.SelectStatement(DirectCast(expression, ExpressionSyntax)),
                SyntaxFactory.List(switchSections.Cast(Of CaseBlockSyntax)))
        End Function

        Public Overloads Overrides Function CreateLambdaExpression(parameters As IList(Of IParameterSymbol), body As SyntaxNode) As SyntaxNode
            If TypeOf body Is ExpressionSyntax Then
                Return SyntaxFactory.SingleLineFunctionLambdaExpression(
                    SyntaxFactory.FunctionLambdaHeader().WithParameterList(
                        ParameterGenerator.GenerateParameterList(parameters, options:=CodeGenerationOptions.Default)),
                    DirectCast(body, ExpressionSyntax))
            Else
                Return SyntaxFactory.SingleLineSubLambdaExpression(
                    SyntaxFactory.SubLambdaHeader().WithParameterList(
                        ParameterGenerator.GenerateParameterList(parameters, options:=CodeGenerationOptions.Default)),
                    DirectCast(body, StatementSyntax))
            End If
        End Function

        Public Overloads Overrides Function CreateLambdaExpression(parameters As IList(Of IParameterSymbol), statements As IList(Of SyntaxNode)) As SyntaxNode
            If statements.Count = 1 Then
                Return CreateLambdaExpression(parameters, statements(0))
            Else
                Return SyntaxFactory.MultiLineSubLambdaExpression(
                    SyntaxFactory.SubLambdaHeader().WithParameterList(ParameterGenerator.GenerateParameterList(parameters, options:=CodeGenerationOptions.Default)),
                    SyntaxFactory.List(statements.Cast(Of StatementSyntax)),
                    SyntaxFactory.EndSubStatement())
            End If
        End Function

        Public Overrides Function CreateExitSwitchStatement() As SyntaxNode
            Return SyntaxFactory.ExitSelectStatement()
        End Function
    End Class
End Namespace