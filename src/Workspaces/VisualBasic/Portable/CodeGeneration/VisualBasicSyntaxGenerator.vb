' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Composition
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Editing
Imports Microsoft.CodeAnalysis.Host.Mef
Imports Microsoft.CodeAnalysis.Simplification
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.CodeGeneration
    <ExportLanguageService(GetType(SyntaxGenerator), LanguageNames.VisualBasic), [Shared]>
    Friend Class VisualBasicSyntaxGenerator
        Inherits SyntaxGenerator

#Region "Expressions and Statements"

        Public Overrides Function AwaitExpression(expression As SyntaxNode) As SyntaxNode
            Return SyntaxFactory.AwaitExpression(DirectCast(expression, ExpressionSyntax))
        End Function

        Private Function Parenthesize(expression As SyntaxNode) As ParenthesizedExpressionSyntax
            Return DirectCast(expression, ExpressionSyntax).Parenthesize()
        End Function

        Public Overrides Function AddExpression(left As SyntaxNode, right As SyntaxNode) As SyntaxNode
            Return SyntaxFactory.AddExpression(Parenthesize(left), Parenthesize(right))
        End Function

        Public Overloads Overrides Function Argument(name As String, refKind As RefKind, expression As SyntaxNode) As SyntaxNode
            If name Is Nothing Then
                Return SyntaxFactory.SimpleArgument(DirectCast(expression, ExpressionSyntax))
            Else
                Return SyntaxFactory.SimpleArgument(SyntaxFactory.NameColonEquals(name.ToIdentifierName()), DirectCast(expression, ExpressionSyntax))
            End If
        End Function

        Public Overrides Function TryCastExpression(expression As SyntaxNode, type As SyntaxNode) As SyntaxNode
            Return SyntaxFactory.TryCastExpression(DirectCast(expression, ExpressionSyntax), DirectCast(type, TypeSyntax))
        End Function

        Public Overrides Function AssignmentStatement(left As SyntaxNode, right As SyntaxNode) As SyntaxNode
            Return SyntaxFactory.SimpleAssignmentStatement(
                DirectCast(left, ExpressionSyntax),
                SyntaxFactory.Token(SyntaxKind.EqualsToken),
                DirectCast(right, ExpressionSyntax))
        End Function

        Public Overrides Function BaseExpression() As SyntaxNode
            Return SyntaxFactory.MyBaseExpression()
        End Function

        Public Overrides Function BitwiseAndExpression(left As SyntaxNode, right As SyntaxNode) As SyntaxNode
            Return SyntaxFactory.AndExpression(Parenthesize(left), Parenthesize(right))
        End Function

        Public Overrides Function BitwiseOrExpression(left As SyntaxNode, right As SyntaxNode) As SyntaxNode
            Return SyntaxFactory.OrExpression(Parenthesize(left), Parenthesize(right))
        End Function

        Public Overrides Function CastExpression(type As SyntaxNode, expression As SyntaxNode) As SyntaxNode
            Return SyntaxFactory.DirectCastExpression(DirectCast(expression, ExpressionSyntax), DirectCast(type, TypeSyntax)).WithAdditionalAnnotations(Simplifier.Annotation)
        End Function

        Public Overrides Function ConvertExpression(type As SyntaxNode, expression As SyntaxNode) As SyntaxNode
            Return SyntaxFactory.CTypeExpression(DirectCast(expression, ExpressionSyntax), DirectCast(type, TypeSyntax)).WithAdditionalAnnotations(Simplifier.Annotation)
        End Function

        Public Overrides Function ConditionalExpression(condition As SyntaxNode, whenTrue As SyntaxNode, whenFalse As SyntaxNode) As SyntaxNode
            Return SyntaxFactory.TernaryConditionalExpression(
                DirectCast(condition, ExpressionSyntax),
                DirectCast(whenTrue, ExpressionSyntax),
                DirectCast(whenFalse, ExpressionSyntax))
        End Function

        Public Overrides Function LiteralExpression(value As Object) As SyntaxNode
            Return ExpressionGenerator.GenerateNonEnumValueExpression(Nothing, value, canUseFieldReference:=True)
        End Function

        Public Overrides Function TypedConstantExpression(value As TypedConstant) As SyntaxNode
            Return ExpressionGenerator.GenerateExpression(value)
        End Function

        Public Overrides Function DefaultExpression(type As ITypeSymbol) As SyntaxNode
            Return SyntaxFactory.NothingLiteralExpression(SyntaxFactory.Token(SyntaxKind.NothingKeyword))
        End Function

        Public Overrides Function DefaultExpression(type As SyntaxNode) As SyntaxNode
            Return SyntaxFactory.NothingLiteralExpression(SyntaxFactory.Token(SyntaxKind.NothingKeyword))
        End Function

        Public Overloads Overrides Function ElementAccessExpression(expression As SyntaxNode, arguments As IEnumerable(Of SyntaxNode)) As SyntaxNode
            Return SyntaxFactory.InvocationExpression(ParenthesizeLeft(expression), CreateArgumentList(arguments))
        End Function

        Public Overrides Function ExpressionStatement(expression As SyntaxNode) As SyntaxNode
            If TypeOf expression Is StatementSyntax Then
                Return expression
            End If

            Return SyntaxFactory.ExpressionStatement(DirectCast(expression, ExpressionSyntax))
        End Function

        Public Overloads Overrides Function GenericName(identifier As String, typeArguments As IEnumerable(Of SyntaxNode)) As SyntaxNode
            Return SyntaxFactory.GenericName(
                identifier.ToIdentifierToken,
                SyntaxFactory.TypeArgumentList(
                    SyntaxFactory.SeparatedList(typeArguments.Cast(Of TypeSyntax)()))).WithAdditionalAnnotations(Simplifier.Annotation)
        End Function

        Public Overrides Function IdentifierName(identifier As String) As SyntaxNode
            Return identifier.ToIdentifierName()
        End Function

        Public Overrides Function IfStatement(condition As SyntaxNode, trueStatements As IEnumerable(Of SyntaxNode), Optional falseStatements As IEnumerable(Of SyntaxNode) = Nothing) As SyntaxNode

            Dim ifStmt = SyntaxFactory.IfStatement(SyntaxFactory.Token(SyntaxKind.IfKeyword),
                                                   DirectCast(condition, ExpressionSyntax),
                                                   SyntaxFactory.Token(SyntaxKind.ThenKeyword))

            If falseStatements Is Nothing Then
                Return SyntaxFactory.MultiLineIfBlock(
                           ifStmt,
                           GetStatementList(trueStatements),
                           Nothing,
                           Nothing
                       )
            End If

            ' convert nested if-blocks into else-if parts
            Dim statements = falseStatements.ToList()
            If statements.Count = 1 AndAlso TypeOf statements(0) Is MultiLineIfBlockSyntax Then
                Dim mifBlock = DirectCast(statements(0), MultiLineIfBlockSyntax)

                ' insert block's if-part onto head of elseIf-parts
                Dim elseIfBlocks = mifBlock.ElseIfBlocks.Insert(0,
                    SyntaxFactory.ElseIfBlock(
                        SyntaxFactory.ElseIfStatement(SyntaxFactory.Token(SyntaxKind.ElseIfKeyword), mifBlock.IfStatement.Condition, SyntaxFactory.Token(SyntaxKind.ThenKeyword)),
                        mifBlock.Statements)
                    )

                Return SyntaxFactory.MultiLineIfBlock(
                           ifStmt,
                           GetStatementList(trueStatements),
                           elseIfBlocks,
                           mifBlock.ElseBlock
                       )
            End If

            Return SyntaxFactory.MultiLineIfBlock(
                       ifStmt,
                       GetStatementList(trueStatements),
                       Nothing,
                       SyntaxFactory.ElseBlock(GetStatementList(falseStatements))
                   )
        End Function

        Private Function GetStatementList(nodes As IEnumerable(Of SyntaxNode)) As SyntaxList(Of StatementSyntax)
            If nodes Is Nothing Then
                Return Nothing
            Else
                Return SyntaxFactory.List(nodes.Select(AddressOf AsStatement))
            End If
        End Function

        Private Function AsStatement(node As SyntaxNode) As StatementSyntax
            Dim expr = TryCast(node, ExpressionSyntax)
            If expr IsNot Nothing Then
                Return SyntaxFactory.ExpressionStatement(expr)
            Else
                Return DirectCast(node, StatementSyntax)
            End If
        End Function

        Public Overloads Overrides Function InvocationExpression(expression As SyntaxNode, arguments As IEnumerable(Of SyntaxNode)) As SyntaxNode
            Return SyntaxFactory.InvocationExpression(ParenthesizeLeft(expression), CreateArgumentList(arguments))
        End Function

        Public Overrides Function IsTypeExpression(expression As SyntaxNode, type As SyntaxNode) As SyntaxNode
            Return SyntaxFactory.TypeOfIsExpression(Parenthesize(expression), DirectCast(type, TypeSyntax))
        End Function

        Public Overrides Function TypeOfExpression(type As SyntaxNode) As SyntaxNode
            Return SyntaxFactory.GetTypeExpression(DirectCast(type, TypeSyntax))
        End Function

        Public Overrides Function LogicalAndExpression(left As SyntaxNode, right As SyntaxNode) As SyntaxNode
            Return SyntaxFactory.AndAlsoExpression(Parenthesize(left), Parenthesize(right))
        End Function

        Public Overrides Function LogicalNotExpression(expression As SyntaxNode) As SyntaxNode
            Return SyntaxFactory.NotExpression(Parenthesize(expression))
        End Function

        Public Overrides Function LogicalOrExpression(left As SyntaxNode, right As SyntaxNode) As SyntaxNode
            Return SyntaxFactory.OrElseExpression(Parenthesize(left), Parenthesize(right))
        End Function

        Public Overrides Function MemberAccessExpression(expression As SyntaxNode, simpleName As SyntaxNode) As SyntaxNode
            Return SyntaxFactory.SimpleMemberAccessExpression(
                ParenthesizeLeft(expression),
                SyntaxFactory.Token(SyntaxKind.DotToken),
                DirectCast(simpleName, SimpleNameSyntax))
        End Function

        ' parenthesize the left-side of a dot or target of an invocation if not unnecessary
        Private Function ParenthesizeLeft(expression As SyntaxNode) As ExpressionSyntax
            Dim expressionSyntax = DirectCast(expression, ExpressionSyntax)
            If TypeOf expressionSyntax Is TypeSyntax _
               OrElse expressionSyntax.IsMeMyBaseOrMyClass() _
               OrElse expressionSyntax.IsKind(SyntaxKind.ParenthesizedExpression) _
               OrElse expressionSyntax.IsKind(SyntaxKind.InvocationExpression) _
               OrElse expressionSyntax.IsKind(SyntaxKind.SimpleMemberAccessExpression) Then
                Return expressionSyntax
            Else
                Return expressionSyntax.Parenthesize()
            End If
        End Function

        Public Overrides Function MultiplyExpression(left As SyntaxNode, right As SyntaxNode) As SyntaxNode
            Return SyntaxFactory.MultiplyExpression(Parenthesize(left), Parenthesize(right))
        End Function

        Public Overrides Function NegateExpression(expression As SyntaxNode) As SyntaxNode
            Return SyntaxFactory.UnaryMinusExpression(Parenthesize(expression))
        End Function

        Private Function AsExpressionList(expressions As IEnumerable(Of SyntaxNode)) As SeparatedSyntaxList(Of ExpressionSyntax)
            Return SyntaxFactory.SeparatedList(Of ExpressionSyntax)(expressions.OfType(Of ExpressionSyntax)())
        End Function

        Public Overrides Function ArrayCreationExpression(elementType As SyntaxNode, size As SyntaxNode) As SyntaxNode
            Dim sizes = SyntaxFactory.ArgumentList(SyntaxFactory.SingletonSeparatedList(AsArgument(size)))
            Dim initializer = SyntaxFactory.CollectionInitializer()
            Return SyntaxFactory.ArrayCreationExpression(Nothing, DirectCast(elementType, TypeSyntax), sizes, initializer)
        End Function

        Public Overrides Function ArrayCreationExpression(elementType As SyntaxNode, elements As IEnumerable(Of SyntaxNode)) As SyntaxNode
            Dim sizes = SyntaxFactory.ArgumentList()
            Dim initializer = SyntaxFactory.CollectionInitializer(AsExpressionList(elements))
            Return SyntaxFactory.ArrayCreationExpression(Nothing, DirectCast(elementType, TypeSyntax), sizes, initializer)
        End Function

        Public Overloads Overrides Function ObjectCreationExpression(typeName As SyntaxNode, arguments As IEnumerable(Of SyntaxNode)) As SyntaxNode
            Return SyntaxFactory.ObjectCreationExpression(
                Nothing,
                DirectCast(typeName, TypeSyntax),
                CreateArgumentList(arguments),
                Nothing)
        End Function

        Public Overrides Function QualifiedName(left As SyntaxNode, right As SyntaxNode) As SyntaxNode
            Return SyntaxFactory.QualifiedName(DirectCast(left, NameSyntax), DirectCast(right, SimpleNameSyntax))
        End Function

        Public Overrides Function ReferenceEqualsExpression(left As SyntaxNode, right As SyntaxNode) As SyntaxNode
            Return SyntaxFactory.IsExpression(Parenthesize(left), Parenthesize(right))
        End Function

        Public Overrides Function ReferenceNotEqualsExpression(left As SyntaxNode, right As SyntaxNode) As SyntaxNode
            Return SyntaxFactory.IsNotExpression(Parenthesize(left), Parenthesize(right))
        End Function

        Public Overrides Function ReturnStatement(Optional expressionOpt As SyntaxNode = Nothing) As SyntaxNode
            Return SyntaxFactory.ReturnStatement(DirectCast(expressionOpt, ExpressionSyntax))
        End Function

        Public Overrides Function ThisExpression() As SyntaxNode
            Return SyntaxFactory.MeExpression()
        End Function

        Public Overrides Function ThrowStatement(Optional expressionOpt As SyntaxNode = Nothing) As SyntaxNode
            Return SyntaxFactory.ThrowStatement(DirectCast(expressionOpt, ExpressionSyntax))
        End Function

        Public Overrides Function TypeExpression(typeSymbol As ITypeSymbol) As SyntaxNode
            Return typeSymbol.GenerateTypeSyntax()
        End Function

        Public Overrides Function TypeExpression(specialType As SpecialType) As SyntaxNode
            Select Case specialType
                Case SpecialType.System_Boolean
                    Return SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.BooleanKeyword))
                Case SpecialType.System_Byte
                    Return SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.ByteKeyword))
                Case SpecialType.System_Char
                    Return SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.CharKeyword))
                Case SpecialType.System_Decimal
                    Return SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.DecimalKeyword))
                Case SpecialType.System_Double
                    Return SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.DoubleKeyword))
                Case SpecialType.System_Int16
                    Return SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.ShortKeyword))
                Case SpecialType.System_Int32
                    Return SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.IntegerKeyword))
                Case SpecialType.System_Int64
                    Return SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.LongKeyword))
                Case SpecialType.System_Object
                    Return SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.ObjectKeyword))
                Case SpecialType.System_SByte
                    Return SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.SByteKeyword))
                Case SpecialType.System_Single
                    Return SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.SingleKeyword))
                Case SpecialType.System_String
                    Return SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.StringKeyword))
                Case SpecialType.System_UInt16
                    Return SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.UShortKeyword))
                Case SpecialType.System_UInt32
                    Return SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.UIntegerKeyword))
                Case SpecialType.System_UInt64
                    Return SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.ULongKeyword))
                Case SpecialType.System_DateTime
                    Return SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.DateKeyword))
                Case Else
                    Throw New NotSupportedException("Unsupported SpecialType")
            End Select
        End Function

        Public Overloads Overrides Function UsingStatement(type As SyntaxNode, identifier As String, expression As SyntaxNode, statements As IEnumerable(Of SyntaxNode)) As SyntaxNode
            Return SyntaxFactory.UsingBlock(
                SyntaxFactory.UsingStatement(
                    expression:=Nothing,
                    variables:=SyntaxFactory.SingletonSeparatedList(VariableDeclarator(type, identifier, expression))),
                GetStatementList(statements))
        End Function

        Public Overloads Overrides Function UsingStatement(expression As SyntaxNode, statements As IEnumerable(Of SyntaxNode)) As SyntaxNode
            Return SyntaxFactory.UsingBlock(
                SyntaxFactory.UsingStatement(
                    expression:=DirectCast(expression, ExpressionSyntax),
                    variables:=Nothing),
                GetStatementList(statements))
        End Function

        Public Overrides Function ValueEqualsExpression(left As SyntaxNode, right As SyntaxNode) As SyntaxNode
            Return SyntaxFactory.EqualsExpression(Parenthesize(left), Parenthesize(right))
        End Function

        Public Overrides Function ValueNotEqualsExpression(left As SyntaxNode, right As SyntaxNode) As SyntaxNode
            Return SyntaxFactory.NotEqualsExpression(Parenthesize(left), Parenthesize(right))
        End Function

        Private Function CreateArgumentList(arguments As IEnumerable(Of SyntaxNode)) As ArgumentListSyntax
            Return SyntaxFactory.ArgumentList(CreateArguments(arguments))
        End Function

        Private Function CreateArguments(arguments As IEnumerable(Of SyntaxNode)) As SeparatedSyntaxList(Of ArgumentSyntax)
            Return SyntaxFactory.SeparatedList(arguments.Select(AddressOf AsArgument))
        End Function

        Private Function AsArgument(argOrExpression As SyntaxNode) As ArgumentSyntax
            Dim arg = TryCast(argOrExpression, ArgumentSyntax)
            If arg IsNot Nothing Then
                Return arg
            Else
                Return SyntaxFactory.SimpleArgument(DirectCast(argOrExpression, ExpressionSyntax))
            End If
        End Function

        Public Overloads Overrides Function LocalDeclarationStatement(type As SyntaxNode, identifier As String, Optional initializer As SyntaxNode = Nothing, Optional isConst As Boolean = False) As SyntaxNode
            Return SyntaxFactory.LocalDeclarationStatement(
                SyntaxFactory.TokenList(SyntaxFactory.Token(If(isConst, SyntaxKind.ConstKeyword, SyntaxKind.DimKeyword))),
                SyntaxFactory.SingletonSeparatedList(VariableDeclarator(type, identifier, initializer)))
        End Function

        Private Function VariableDeclarator(type As SyntaxNode, name As String, Optional expression As SyntaxNode = Nothing) As VariableDeclaratorSyntax
            Return SyntaxFactory.VariableDeclarator(
                SyntaxFactory.SingletonSeparatedList(name.ToModifiedIdentifier),
                If(type Is Nothing, Nothing, SyntaxFactory.SimpleAsClause(DirectCast(type, TypeSyntax))),
                If(expression Is Nothing,
                   Nothing,
                   SyntaxFactory.EqualsValue(DirectCast(expression, ExpressionSyntax))))
        End Function

        Public Overloads Overrides Function SwitchStatement(expression As SyntaxNode, caseClauses As IEnumerable(Of SyntaxNode)) As SyntaxNode
            Return SyntaxFactory.SelectBlock(
                SyntaxFactory.SelectStatement(DirectCast(expression, ExpressionSyntax)),
                SyntaxFactory.List(caseClauses.Cast(Of CaseBlockSyntax)))
        End Function

        Public Overloads Overrides Function SwitchSection(expressions As IEnumerable(Of SyntaxNode), statements As IEnumerable(Of SyntaxNode)) As SyntaxNode
            Return SyntaxFactory.CaseBlock(
                SyntaxFactory.CaseStatement(GetCaseClauses(expressions)),
                GetStatementList(statements))
        End Function

        Public Overrides Function DefaultSwitchSection(statements As IEnumerable(Of SyntaxNode)) As SyntaxNode
            Return SyntaxFactory.CaseElseBlock(
                SyntaxFactory.CaseElseStatement(SyntaxFactory.ElseCaseClause()),
                GetStatementList(statements))
        End Function

        Private Function GetCaseClauses(expressions As IEnumerable(Of SyntaxNode)) As SeparatedSyntaxList(Of CaseClauseSyntax)
            Dim cases = SyntaxFactory.SeparatedList(Of CaseClauseSyntax)

            If expressions IsNot Nothing Then
                cases = cases.AddRange(expressions.Select(Function(e) SyntaxFactory.SimpleCaseClause(DirectCast(e, ExpressionSyntax))))
            End If

            Return cases
        End Function

        Private Function AsCaseClause(expression As SyntaxNode) As CaseClauseSyntax
            Return SyntaxFactory.SimpleCaseClause(DirectCast(expression, ExpressionSyntax))
        End Function

        Public Overrides Function ExitSwitchStatement() As SyntaxNode
            Return SyntaxFactory.ExitSelectStatement()
        End Function

        Public Overloads Overrides Function ValueReturningLambdaExpression(parameters As IEnumerable(Of SyntaxNode), expression As SyntaxNode) As SyntaxNode
            Return SyntaxFactory.SingleLineFunctionLambdaExpression(
                SyntaxFactory.FunctionLambdaHeader().WithParameterList(GetParameterList(parameters)),
                DirectCast(expression, ExpressionSyntax))
        End Function

        Public Overrides Function VoidReturningLambdaExpression(lambdaParameters As IEnumerable(Of SyntaxNode), expression As SyntaxNode) As SyntaxNode
            Return SyntaxFactory.SingleLineSubLambdaExpression(
                    SyntaxFactory.SubLambdaHeader().WithParameterList(GetParameterList(lambdaParameters)),
                    AsStatement(expression))
        End Function

        Public Overloads Overrides Function ValueReturningLambdaExpression(lambdaParameters As IEnumerable(Of SyntaxNode), statements As IEnumerable(Of SyntaxNode)) As SyntaxNode
            Return SyntaxFactory.MultiLineFunctionLambdaExpression(
                SyntaxFactory.FunctionLambdaHeader().WithParameterList(GetParameterList(lambdaParameters)),
                GetStatementList(statements),
                SyntaxFactory.EndFunctionStatement())
        End Function

        Public Overrides Function VoidReturningLambdaExpression(lambdaParameters As IEnumerable(Of SyntaxNode), statements As IEnumerable(Of SyntaxNode)) As SyntaxNode
            Return SyntaxFactory.MultiLineSubLambdaExpression(
                        SyntaxFactory.SubLambdaHeader().WithParameterList(GetParameterList(lambdaParameters)),
                        GetStatementList(statements),
                        SyntaxFactory.EndSubStatement())
        End Function

        Public Overrides Function LambdaParameter(identifier As String, Optional type As SyntaxNode = Nothing) As SyntaxNode
            Return ParameterDeclaration(identifier, type)
        End Function

        Public Overrides Function ArrayTypeExpression(type As SyntaxNode) As SyntaxNode
            Dim arrayType = TryCast(type, ArrayTypeSyntax)
            If arrayType IsNot Nothing Then
                Return arrayType.WithRankSpecifiers(arrayType.RankSpecifiers.Add(SyntaxFactory.ArrayRankSpecifier()))
            Else
                Return SyntaxFactory.ArrayType(DirectCast(type, TypeSyntax), SyntaxFactory.SingletonList(SyntaxFactory.ArrayRankSpecifier()))
            End If
        End Function

        Public Overrides Function NullableTypeExpression(type As SyntaxNode) As SyntaxNode
            Dim nullableType = TryCast(type, NullableTypeSyntax)
            If nullableType IsNot Nothing Then
                Return nullableType
            Else
                Return SyntaxFactory.NullableType(DirectCast(type, TypeSyntax))
            End If
        End Function

        Public Overrides Function WithTypeArguments(name As SyntaxNode, typeArguments As IEnumerable(Of SyntaxNode)) As SyntaxNode
            If name.IsKind(SyntaxKind.IdentifierName) OrElse name.IsKind(SyntaxKind.GenericName) Then
                Dim sname = DirectCast(name, SimpleNameSyntax)
                Return SyntaxFactory.GenericName(sname.Identifier, SyntaxFactory.TypeArgumentList(SyntaxFactory.SeparatedList(typeArguments.Cast(Of TypeSyntax)())))
            ElseIf name.IsKind(SyntaxKind.QualifiedName) Then
                Dim qname = DirectCast(name, QualifiedNameSyntax)
                Return SyntaxFactory.QualifiedName(qname.Left, DirectCast(WithTypeArguments(qname.Right, typeArguments), SimpleNameSyntax))
            ElseIf name.IsKind(SyntaxKind.SimpleMemberAccessExpression) Then
                Dim sma = DirectCast(name, MemberAccessExpressionSyntax)
                Return SyntaxFactory.MemberAccessExpression(name.Kind(), sma.Expression, sma.OperatorToken, DirectCast(WithTypeArguments(sma.Name, typeArguments), SimpleNameSyntax))
            Else
                Throw New NotSupportedException()
            End If
        End Function

        Public Overrides Function SubtractExpression(left As SyntaxNode, right As SyntaxNode) As SyntaxNode
            Return SyntaxFactory.SubtractExpression(Parenthesize(left), Parenthesize(right))
        End Function

        Public Overrides Function DivideExpression(left As SyntaxNode, right As SyntaxNode) As SyntaxNode
            Return SyntaxFactory.DivideExpression(Parenthesize(left), Parenthesize(right))
        End Function

        Public Overrides Function ModuloExpression(left As SyntaxNode, right As SyntaxNode) As SyntaxNode
            Return SyntaxFactory.ModuloExpression(Parenthesize(left), Parenthesize(right))
        End Function

        Public Overrides Function BitwiseNotExpression(operand As SyntaxNode) As SyntaxNode
            Return SyntaxFactory.NotExpression(Parenthesize(operand))
        End Function

        Public Overrides Function CoalesceExpression(left As SyntaxNode, right As SyntaxNode) As SyntaxNode
            Return SyntaxFactory.BinaryConditionalExpression(DirectCast(left, ExpressionSyntax), DirectCast(right, ExpressionSyntax))
        End Function

        Public Overrides Function LessThanExpression(left As SyntaxNode, right As SyntaxNode) As SyntaxNode
            Return SyntaxFactory.LessThanExpression(Parenthesize(left), Parenthesize(right))
        End Function

        Public Overrides Function LessThanOrEqualExpression(left As SyntaxNode, right As SyntaxNode) As SyntaxNode
            Return SyntaxFactory.LessThanOrEqualExpression(Parenthesize(left), Parenthesize(right))
        End Function

        Public Overrides Function GreaterThanExpression(left As SyntaxNode, right As SyntaxNode) As SyntaxNode
            Return SyntaxFactory.GreaterThanExpression(Parenthesize(left), Parenthesize(right))
        End Function

        Public Overrides Function GreaterThanOrEqualExpression(left As SyntaxNode, right As SyntaxNode) As SyntaxNode
            Return SyntaxFactory.GreaterThanOrEqualExpression(Parenthesize(left), Parenthesize(right))
        End Function

        Public Overrides Function TryCatchStatement(tryStatements As IEnumerable(Of SyntaxNode), catchClauses As IEnumerable(Of SyntaxNode), Optional finallyStatements As IEnumerable(Of SyntaxNode) = Nothing) As SyntaxNode
            Return SyntaxFactory.TryBlock(
                       GetStatementList(tryStatements),
                       If(catchClauses IsNot Nothing, SyntaxFactory.List(catchClauses.Cast(Of CatchBlockSyntax)()), Nothing),
                       If(finallyStatements IsNot Nothing, SyntaxFactory.FinallyBlock(GetStatementList(finallyStatements)), Nothing)
                   )
        End Function

        Public Overrides Function CatchClause(type As SyntaxNode, identifier As String, statements As IEnumerable(Of SyntaxNode)) As SyntaxNode
            Return SyntaxFactory.CatchBlock(
                SyntaxFactory.CatchStatement(
                    SyntaxFactory.IdentifierName(identifier),
                    SyntaxFactory.SimpleAsClause(DirectCast(type, TypeSyntax)),
                           whenClause:=Nothing
                       ),
                       GetStatementList(statements)
                   )
        End Function

        Public Overrides Function WhileStatement(condition As SyntaxNode, statements As IEnumerable(Of SyntaxNode)) As SyntaxNode
            Return SyntaxFactory.WhileBlock(
                SyntaxFactory.WhileStatement(DirectCast(condition, ExpressionSyntax)),
                GetStatementList(statements))
        End Function
#End Region

#Region "Declarations"
        Private Function AsReadOnlyList(Of T)(sequence As IEnumerable(Of T)) As IReadOnlyList(Of T)
            Dim list = TryCast(sequence, IReadOnlyList(Of T))

            If list Is Nothing Then
                list = sequence.ToImmutableReadOnlyListOrEmpty()
            End If

            Return list
        End Function

        Private Shared s_fieldModifiers As DeclarationModifiers = DeclarationModifiers.Const Or DeclarationModifiers.[New] Or DeclarationModifiers.ReadOnly Or DeclarationModifiers.Static
        Private Shared s_methodModifiers As DeclarationModifiers = DeclarationModifiers.Abstract Or DeclarationModifiers.Async Or DeclarationModifiers.[New] Or DeclarationModifiers.Override Or DeclarationModifiers.Partial Or DeclarationModifiers.Sealed Or DeclarationModifiers.Static Or DeclarationModifiers.Virtual
        Private Shared s_constructorModifiers As DeclarationModifiers = DeclarationModifiers.Static
        Private Shared s_propertyModifiers As DeclarationModifiers = DeclarationModifiers.Abstract Or DeclarationModifiers.[New] Or DeclarationModifiers.Override Or DeclarationModifiers.ReadOnly Or DeclarationModifiers.WriteOnly Or DeclarationModifiers.Sealed Or DeclarationModifiers.Static Or DeclarationModifiers.Virtual
        Private Shared s_indexerModifiers As DeclarationModifiers = DeclarationModifiers.Abstract Or DeclarationModifiers.[New] Or DeclarationModifiers.Override Or DeclarationModifiers.ReadOnly Or DeclarationModifiers.WriteOnly Or DeclarationModifiers.Sealed Or DeclarationModifiers.Static Or DeclarationModifiers.Virtual
        Private Shared s_classModifiers As DeclarationModifiers = DeclarationModifiers.Abstract Or DeclarationModifiers.[New] Or DeclarationModifiers.Partial Or DeclarationModifiers.Sealed Or DeclarationModifiers.Static
        Private Shared s_structModifiers As DeclarationModifiers = DeclarationModifiers.[New] Or DeclarationModifiers.Partial
        Private Shared s_interfaceModifiers As DeclarationModifiers = DeclarationModifiers.[New] Or DeclarationModifiers.Partial
        Private Shared s_accessorModifiers As DeclarationModifiers = DeclarationModifiers.Abstract Or DeclarationModifiers.[New] Or DeclarationModifiers.Override Or DeclarationModifiers.Virtual

        Private Function GetAllowedModifiers(kind As SyntaxKind) As DeclarationModifiers
            Select Case kind
                Case SyntaxKind.ClassBlock, SyntaxKind.ClassStatement
                    Return s_classModifiers

                Case SyntaxKind.EnumBlock, SyntaxKind.EnumStatement
                    Return DeclarationModifiers.[New]

                Case SyntaxKind.DelegateFunctionStatement, SyntaxKind.DelegateSubStatement
                    Return DeclarationModifiers.[New]

                Case SyntaxKind.InterfaceBlock, SyntaxKind.InterfaceStatement
                    Return s_interfaceModifiers

                Case SyntaxKind.StructureBlock, SyntaxKind.StructureStatement
                    Return s_structModifiers

                Case SyntaxKind.FunctionBlock,
                     SyntaxKind.FunctionStatement,
                     SyntaxKind.SubBlock,
                     SyntaxKind.SubStatement,
                     SyntaxKind.OperatorBlock,
                     SyntaxKind.OperatorStatement
                    Return s_methodModifiers

                Case SyntaxKind.ConstructorBlock,
                     SyntaxKind.SubNewStatement
                    Return s_constructorModifiers

                Case SyntaxKind.FieldDeclaration
                    Return s_fieldModifiers

                Case SyntaxKind.PropertyBlock,
                     SyntaxKind.PropertyStatement
                    Return s_propertyModifiers

                Case SyntaxKind.EventBlock,
                     SyntaxKind.EventStatement
                    Return s_propertyModifiers

                Case SyntaxKind.GetAccessorBlock,
                     SyntaxKind.GetAccessorStatement,
                     SyntaxKind.SetAccessorBlock,
                     SyntaxKind.SetAccessorStatement,
                     SyntaxKind.AddHandlerAccessorBlock,
                     SyntaxKind.AddHandlerAccessorStatement,
                     SyntaxKind.RemoveHandlerAccessorBlock,
                     SyntaxKind.RemoveHandlerAccessorStatement,
                     SyntaxKind.RaiseEventAccessorBlock,
                     SyntaxKind.RaiseEventAccessorStatement
                    Return s_accessorModifiers

                Case SyntaxKind.EnumMemberDeclaration
                Case SyntaxKind.Parameter
                Case SyntaxKind.LocalDeclarationStatement
                Case Else
                    Return DeclarationModifiers.None
            End Select
        End Function

        Public Overrides Function FieldDeclaration(name As String, type As SyntaxNode, Optional accessibility As Accessibility = Nothing, Optional modifiers As DeclarationModifiers = Nothing, Optional initializer As SyntaxNode = Nothing) As SyntaxNode
            Return SyntaxFactory.FieldDeclaration(
                attributeLists:=Nothing,
                modifiers:=GetModifierList(accessibility, modifiers And s_fieldModifiers, DeclarationKind.Field),
                declarators:=SyntaxFactory.SingletonSeparatedList(VariableDeclarator(type, name, initializer)))
        End Function

        Public Overrides Function MethodDeclaration(
            identifier As String,
            Optional parameters As IEnumerable(Of SyntaxNode) = Nothing,
            Optional typeParameters As IEnumerable(Of String) = Nothing,
            Optional returnType As SyntaxNode = Nothing,
            Optional accessibility As Accessibility = Nothing,
            Optional modifiers As DeclarationModifiers = Nothing,
            Optional statements As IEnumerable(Of SyntaxNode) = Nothing) As SyntaxNode

            Dim statement = SyntaxFactory.MethodStatement(
                kind:=If(returnType Is Nothing, SyntaxKind.SubStatement, SyntaxKind.FunctionStatement),
                attributeLists:=Nothing,
                modifiers:=GetModifierList(accessibility, modifiers And s_methodModifiers, DeclarationKind.Method),
                subOrFunctionKeyword:=If(returnType Is Nothing, SyntaxFactory.Token(SyntaxKind.SubKeyword), SyntaxFactory.Token(SyntaxKind.FunctionKeyword)),
                identifier:=identifier.ToIdentifierToken(),
                typeParameterList:=GetTypeParameters(typeParameters),
                parameterList:=GetParameterList(parameters),
                asClause:=If(returnType IsNot Nothing, SyntaxFactory.SimpleAsClause(DirectCast(returnType, TypeSyntax)), Nothing),
                handlesClause:=Nothing,
                implementsClause:=Nothing)

            If modifiers.IsAbstract Then
                Return statement
            Else
                Return SyntaxFactory.MethodBlock(
                    kind:=If(returnType Is Nothing, SyntaxKind.SubBlock, SyntaxKind.FunctionBlock),
                    subOrFunctionStatement:=statement,
                    statements:=GetStatementList(statements),
                    endSubOrFunctionStatement:=If(returnType Is Nothing, SyntaxFactory.EndSubStatement(), SyntaxFactory.EndFunctionStatement()))
            End If
        End Function

        Public Overrides Function OperatorDeclaration(kind As OperatorKind,
                                                      Optional parameters As IEnumerable(Of SyntaxNode) = Nothing,
                                                      Optional returnType As SyntaxNode = Nothing,
                                                      Optional accessibility As Accessibility = Accessibility.NotApplicable,
                                                      Optional modifiers As DeclarationModifiers = Nothing,
                                                      Optional statements As IEnumerable(Of SyntaxNode) = Nothing) As SyntaxNode

            Dim statement As OperatorStatementSyntax
            Dim asClause = If(returnType IsNot Nothing, SyntaxFactory.SimpleAsClause(DirectCast(returnType, TypeSyntax)), Nothing)
            Dim parameterList = GetParameterList(parameters)
            Dim operatorToken = SyntaxFactory.Token(GetTokenKind(kind))
            Dim modifierList As SyntaxTokenList = GetModifierList(accessibility, modifiers And s_methodModifiers, DeclarationKind.Operator)

            If kind = OperatorKind.ImplicitConversion OrElse kind = OperatorKind.ExplicitConversion Then
                modifierList = modifierList.Add(SyntaxFactory.Token(
                    If(kind = OperatorKind.ImplicitConversion, SyntaxKind.WideningKeyword, SyntaxKind.NarrowingKeyword)))
                statement = SyntaxFactory.OperatorStatement(
                    attributeLists:=Nothing, modifiers:=modifierList, operatorToken:=operatorToken,
                    parameterList:=parameterList, asClause:=asClause)
            Else
                statement = SyntaxFactory.OperatorStatement(
                    attributeLists:=Nothing, modifiers:=modifierList,
                    operatorToken:=operatorToken, parameterList:=parameterList,
                    asClause:=asClause)
            End If


            If modifiers.IsAbstract Then
                Return statement
            Else
                Return SyntaxFactory.OperatorBlock(
                    operatorStatement:=statement,
                    statements:=GetStatementList(statements),
                    endOperatorStatement:=SyntaxFactory.EndOperatorStatement())
            End If
        End Function

        Private Function GetTokenKind(kind As OperatorKind) As SyntaxKind
            Select Case kind
                Case OperatorKind.ImplicitConversion,
                     OperatorKind.ExplicitConversion
                    Return SyntaxKind.CTypeKeyword
                Case OperatorKind.Addition
                    Return SyntaxKind.PlusToken
                Case OperatorKind.BitwiseAnd
                    Return SyntaxKind.AndKeyword
                Case OperatorKind.BitwiseOr
                    Return SyntaxKind.OrKeyword
                Case OperatorKind.Division
                    Return SyntaxKind.SlashToken
                Case OperatorKind.Equality
                    Return SyntaxKind.EqualsToken
                Case OperatorKind.ExclusiveOr
                    Return SyntaxKind.XorKeyword
                Case OperatorKind.False
                    Return SyntaxKind.IsFalseKeyword
                Case OperatorKind.GreaterThan
                    Return SyntaxKind.GreaterThanToken
                Case OperatorKind.GreaterThanOrEqual
                    Return SyntaxKind.GreaterThanEqualsToken
                Case OperatorKind.Inequality
                    Return SyntaxKind.LessThanGreaterThanToken
                Case OperatorKind.LeftShift
                    Return SyntaxKind.LessThanLessThanToken
                Case OperatorKind.LessThan
                    Return SyntaxKind.LessThanToken
                Case OperatorKind.LessThanOrEqual
                    Return SyntaxKind.LessThanEqualsToken
                Case OperatorKind.LogicalNot
                    Return SyntaxKind.NotKeyword
                Case OperatorKind.Modulus
                    Return SyntaxKind.ModKeyword
                Case OperatorKind.Multiply
                    Return SyntaxKind.AsteriskToken
                Case OperatorKind.RightShift
                    Return SyntaxKind.GreaterThanGreaterThanToken
                Case OperatorKind.Subtraction
                    Return SyntaxKind.MinusToken
                Case OperatorKind.True
                    Return SyntaxKind.IsTrueKeyword
                Case OperatorKind.UnaryNegation
                    Return SyntaxKind.MinusToken
                Case OperatorKind.UnaryPlus
                    Return SyntaxKind.PlusToken
                Case Else
                    Throw New ArgumentException($"Operator {kind} cannot be generated in Visual Basic.")
            End Select
        End Function

        Private Function GetParameterList(parameters As IEnumerable(Of SyntaxNode)) As ParameterListSyntax
            Return If(parameters IsNot Nothing, SyntaxFactory.ParameterList(SyntaxFactory.SeparatedList(parameters.Cast(Of ParameterSyntax)())), SyntaxFactory.ParameterList())
        End Function

        Public Overrides Function ParameterDeclaration(name As String, Optional type As SyntaxNode = Nothing, Optional initializer As SyntaxNode = Nothing, Optional refKind As RefKind = Nothing) As SyntaxNode
            Return SyntaxFactory.Parameter(
                attributeLists:=Nothing,
                modifiers:=GetParameterModifiers(refKind, initializer),
                identifier:=name.ToModifiedIdentifier(),
                asClause:=If(type IsNot Nothing, SyntaxFactory.SimpleAsClause(DirectCast(type, TypeSyntax)), Nothing),
                [default]:=If(initializer IsNot Nothing, SyntaxFactory.EqualsValue(DirectCast(initializer, ExpressionSyntax)), Nothing))
        End Function

        Private Function GetParameterModifiers(refKind As RefKind, initializer As SyntaxNode) As SyntaxTokenList
            Dim tokens As SyntaxTokenList = Nothing
            If initializer IsNot Nothing Then
                tokens = tokens.Add(SyntaxFactory.Token(SyntaxKind.OptionalKeyword))
            End If
            If refKind <> RefKind.None Then
                tokens = tokens.Add(SyntaxFactory.Token(SyntaxKind.ByRefKeyword))
            End If
            Return tokens
        End Function

        Public Overrides Function PropertyDeclaration(
            identifier As String,
            type As SyntaxNode,
            Optional accessibility As Accessibility = Nothing,
            Optional modifiers As DeclarationModifiers = Nothing,
            Optional getAccessorStatements As IEnumerable(Of SyntaxNode) = Nothing,
            Optional setAccessorStatements As IEnumerable(Of SyntaxNode) = Nothing) As SyntaxNode

            Dim asClause = SyntaxFactory.SimpleAsClause(DirectCast(type, TypeSyntax))
            Dim statement = SyntaxFactory.PropertyStatement(
                attributeLists:=Nothing,
                modifiers:=GetModifierList(accessibility, modifiers And s_propertyModifiers, DeclarationKind.Property),
                identifier:=identifier.ToIdentifierToken(),
                parameterList:=Nothing,
                asClause:=asClause,
                initializer:=Nothing,
                implementsClause:=Nothing)

            If modifiers.IsAbstract Then
                Return statement
            Else
                Dim accessors = New List(Of AccessorBlockSyntax)

                If Not modifiers.IsWriteOnly Then
                    accessors.Add(CreateGetAccessorBlock(getAccessorStatements))
                End If

                If Not modifiers.IsReadOnly Then
                    accessors.Add(CreateSetAccessorBlock(type, setAccessorStatements))
                End If

                Return SyntaxFactory.PropertyBlock(
                    propertyStatement:=statement,
                    accessors:=SyntaxFactory.List(accessors),
                    endPropertyStatement:=SyntaxFactory.EndPropertyStatement())
            End If
        End Function

        Public Overrides Function IndexerDeclaration(
            parameters As IEnumerable(Of SyntaxNode),
            type As SyntaxNode,
            Optional accessibility As Accessibility = Nothing,
            Optional modifiers As DeclarationModifiers = Nothing,
            Optional getAccessorStatements As IEnumerable(Of SyntaxNode) = Nothing,
            Optional setAccessorStatements As IEnumerable(Of SyntaxNode) = Nothing) As SyntaxNode

            Dim asClause = SyntaxFactory.SimpleAsClause(DirectCast(type, TypeSyntax))
            Dim statement = SyntaxFactory.PropertyStatement(
                attributeLists:=Nothing,
                modifiers:=GetModifierList(accessibility, modifiers And s_indexerModifiers, DeclarationKind.Indexer, isDefault:=True),
                identifier:=SyntaxFactory.Identifier("Item"),
                parameterList:=SyntaxFactory.ParameterList(SyntaxFactory.SeparatedList(parameters.Cast(Of ParameterSyntax))),
                asClause:=asClause,
                initializer:=Nothing,
                implementsClause:=Nothing)

            If modifiers.IsAbstract Then
                Return statement
            Else
                Dim accessors = New List(Of AccessorBlockSyntax)

                If Not modifiers.IsWriteOnly Then
                    accessors.Add(CreateGetAccessorBlock(getAccessorStatements))
                End If

                If Not modifiers.IsReadOnly Then
                    accessors.Add(CreateSetAccessorBlock(type, setAccessorStatements))
                End If

                Return SyntaxFactory.PropertyBlock(
                    propertyStatement:=statement,
                    accessors:=SyntaxFactory.List(accessors),
                    endPropertyStatement:=SyntaxFactory.EndPropertyStatement())
            End If
        End Function

        Private Function AccessorBlock(kind As SyntaxKind, statements As IEnumerable(Of SyntaxNode), type As SyntaxNode) As AccessorBlockSyntax
            Select Case kind
                Case SyntaxKind.GetAccessorBlock
                    Return CreateGetAccessorBlock(statements)
                Case SyntaxKind.SetAccessorBlock
                    Return CreateSetAccessorBlock(type, statements)
                Case SyntaxKind.AddHandlerAccessorBlock
                    Return CreateAddHandlerAccessorBlock(type, statements)
                Case SyntaxKind.RemoveHandlerAccessorBlock
                    Return CreateRemoveHandlerAccessorBlock(type, statements)
                Case Else
                    Return Nothing
            End Select
        End Function

        Private Function CreateGetAccessorBlock(statements As IEnumerable(Of SyntaxNode)) As AccessorBlockSyntax
            Return SyntaxFactory.AccessorBlock(
                SyntaxKind.GetAccessorBlock,
                SyntaxFactory.AccessorStatement(SyntaxKind.GetAccessorStatement, SyntaxFactory.Token(SyntaxKind.GetKeyword)),
                GetStatementList(statements),
                SyntaxFactory.EndGetStatement())
        End Function

        Private Function CreateSetAccessorBlock(type As SyntaxNode, statements As IEnumerable(Of SyntaxNode)) As AccessorBlockSyntax
            Dim asClause = SyntaxFactory.SimpleAsClause(DirectCast(type, TypeSyntax))

            Dim valueParameter = SyntaxFactory.Parameter(
                        attributeLists:=Nothing,
                        modifiers:=Nothing,
                        identifier:=SyntaxFactory.ModifiedIdentifier("value"),
                        asClause:=asClause,
                        [default]:=Nothing)

            Return SyntaxFactory.AccessorBlock(
                SyntaxKind.SetAccessorBlock,
                SyntaxFactory.AccessorStatement(
                    kind:=SyntaxKind.SetAccessorStatement,
                    attributeLists:=Nothing,
                    modifiers:=Nothing,
                    accessorKeyword:=SyntaxFactory.Token(SyntaxKind.SetKeyword),
                    parameterList:=SyntaxFactory.ParameterList(SyntaxFactory.SingletonSeparatedList(valueParameter))),
                GetStatementList(statements),
                SyntaxFactory.EndSetStatement())
        End Function

        Private Function CreateAddHandlerAccessorBlock(delegateType As SyntaxNode, statements As IEnumerable(Of SyntaxNode)) As AccessorBlockSyntax
            Dim asClause = SyntaxFactory.SimpleAsClause(DirectCast(delegateType, TypeSyntax))

            Dim valueParameter = SyntaxFactory.Parameter(
                attributeLists:=Nothing,
                modifiers:=Nothing,
                identifier:=SyntaxFactory.ModifiedIdentifier("value"),
                asClause:=asClause,
                [default]:=Nothing)

            Return SyntaxFactory.AccessorBlock(
                SyntaxKind.AddHandlerAccessorBlock,
                SyntaxFactory.AccessorStatement(
                    kind:=SyntaxKind.AddHandlerAccessorStatement,
                    attributeLists:=Nothing,
                    modifiers:=Nothing,
                    accessorKeyword:=SyntaxFactory.Token(SyntaxKind.AddHandlerKeyword),
                    parameterList:=SyntaxFactory.ParameterList(SyntaxFactory.SingletonSeparatedList(valueParameter))),
                GetStatementList(statements),
                SyntaxFactory.EndAddHandlerStatement())
        End Function

        Private Function CreateRemoveHandlerAccessorBlock(delegateType As SyntaxNode, statements As IEnumerable(Of SyntaxNode)) As AccessorBlockSyntax
            Dim asClause = SyntaxFactory.SimpleAsClause(DirectCast(delegateType, TypeSyntax))

            Dim valueParameter = SyntaxFactory.Parameter(
                attributeLists:=Nothing,
                modifiers:=Nothing,
                identifier:=SyntaxFactory.ModifiedIdentifier("value"),
                asClause:=asClause,
                [default]:=Nothing)

            Return SyntaxFactory.AccessorBlock(
                SyntaxKind.RemoveHandlerAccessorBlock,
                SyntaxFactory.AccessorStatement(
                    kind:=SyntaxKind.RemoveHandlerAccessorStatement,
                    attributeLists:=Nothing,
                    modifiers:=Nothing,
                    accessorKeyword:=SyntaxFactory.Token(SyntaxKind.RemoveHandlerKeyword),
                    parameterList:=SyntaxFactory.ParameterList(SyntaxFactory.SingletonSeparatedList(valueParameter))),
                GetStatementList(statements),
                SyntaxFactory.EndRemoveHandlerStatement())
        End Function

        Private Function CreateRaiseEventAccessorBlock(parameters As IEnumerable(Of SyntaxNode), statements As IEnumerable(Of SyntaxNode)) As AccessorBlockSyntax
            Dim parameterList = GetParameterList(parameters)

            Return SyntaxFactory.AccessorBlock(
                SyntaxKind.RaiseEventAccessorBlock,
                SyntaxFactory.AccessorStatement(
                    kind:=SyntaxKind.RaiseEventAccessorStatement,
                    attributeLists:=Nothing,
                    modifiers:=Nothing,
                    accessorKeyword:=SyntaxFactory.Token(SyntaxKind.RaiseEventKeyword),
                    parameterList:=parameterList),
                GetStatementList(statements),
                SyntaxFactory.EndRaiseEventStatement())
        End Function

        Public Overrides Function AsPublicInterfaceImplementation(declaration As SyntaxNode, interfaceTypeName As SyntaxNode, interfaceMemberName As String) As SyntaxNode
            Return Isolate(declaration, Function(decl) AsPublicInterfaceImplementationInternal(decl, interfaceTypeName, interfaceMemberName))
        End Function

        Private Function AsPublicInterfaceImplementationInternal(declaration As SyntaxNode, interfaceTypeName As SyntaxNode, interfaceMemberName As String) As SyntaxNode
            Dim type = DirectCast(interfaceTypeName, NameSyntax)

            declaration = WithBody(declaration, allowDefault:=True)
            declaration = WithAccessibility(declaration, Accessibility.Public)

            Dim memberName = If(interfaceMemberName IsNot Nothing, interfaceMemberName, GetInterfaceMemberName(declaration))
            declaration = WithName(declaration, memberName)
            declaration = WithImplementsClause(declaration, SyntaxFactory.ImplementsClause(SyntaxFactory.QualifiedName(type, SyntaxFactory.IdentifierName(memberName))))

            Return declaration
        End Function

        Public Overrides Function AsPrivateInterfaceImplementation(declaration As SyntaxNode, interfaceTypeName As SyntaxNode, interfaceMemberName As String) As SyntaxNode
            Return Isolate(declaration, Function(decl) AsPrivateInterfaceImplementationInternal(decl, interfaceTypeName, interfaceMemberName))
        End Function

        Private Function AsPrivateInterfaceImplementationInternal(declaration As SyntaxNode, interfaceTypeName As SyntaxNode, interfaceMemberName As String) As SyntaxNode
            Dim type = DirectCast(interfaceTypeName, NameSyntax)

            declaration = WithBody(declaration, allowDefault:=False)
            declaration = WithAccessibility(declaration, Accessibility.Private)

            Dim memberName = If(interfaceMemberName IsNot Nothing, interfaceMemberName, GetInterfaceMemberName(declaration))
            declaration = WithName(declaration, GetNameAsIdentifier(interfaceTypeName) & "_" & memberName)
            declaration = WithImplementsClause(declaration, SyntaxFactory.ImplementsClause(SyntaxFactory.QualifiedName(type, SyntaxFactory.IdentifierName(memberName))))

            Return declaration
        End Function

        Private Function GetInterfaceMemberName(declaration As SyntaxNode) As String
            Dim clause = GetImplementsClause(declaration)
            If clause IsNot Nothing Then
                Dim qname = clause.InterfaceMembers.FirstOrDefault(Function(n) n.Right IsNot Nothing)
                If qname IsNot Nothing Then
                    Return qname.Right.ToString()
                End If
            End If
            Return GetName(declaration)
        End Function

        Private Function GetImplementsClause(declaration As SyntaxNode) As ImplementsClauseSyntax
            Select Case declaration.Kind
                Case SyntaxKind.SubBlock,
                    SyntaxKind.FunctionBlock
                    Return DirectCast(declaration, MethodBlockSyntax).SubOrFunctionStatement.ImplementsClause
                Case SyntaxKind.SubStatement,
                    SyntaxKind.FunctionStatement
                    Return DirectCast(declaration, MethodStatementSyntax).ImplementsClause
                Case SyntaxKind.PropertyBlock
                    Return DirectCast(declaration, PropertyBlockSyntax).PropertyStatement.ImplementsClause
                Case SyntaxKind.PropertyStatement
                    Return DirectCast(declaration, PropertyStatementSyntax).ImplementsClause
                Case Else
                    Return Nothing
            End Select
        End Function

        Private Function WithImplementsClause(declaration As SyntaxNode, clause As ImplementsClauseSyntax) As SyntaxNode
            Select Case declaration.Kind
                Case SyntaxKind.SubBlock,
                    SyntaxKind.FunctionBlock
                    Dim mb = DirectCast(declaration, MethodBlockSyntax)
                    Return mb.WithSubOrFunctionStatement(mb.SubOrFunctionStatement.WithImplementsClause(clause))
                Case SyntaxKind.SubStatement,
                    SyntaxKind.FunctionStatement
                    Return DirectCast(declaration, MethodStatementSyntax).WithImplementsClause(clause)
                Case SyntaxKind.PropertyBlock
                    Dim pb = DirectCast(declaration, PropertyBlockSyntax)
                    Return pb.WithPropertyStatement(pb.PropertyStatement.WithImplementsClause(clause))
                Case SyntaxKind.PropertyStatement
                    Return DirectCast(declaration, PropertyStatementSyntax).WithImplementsClause(clause)
                Case Else
                    Return declaration
            End Select
        End Function

        Private Function GetNameAsIdentifier(type As SyntaxNode) As String
            Dim name = TryCast(type, IdentifierNameSyntax)
            If name IsNot Nothing Then
                Return name.Identifier.ValueText
            End If

            Dim gname = TryCast(type, GenericNameSyntax)
            If gname IsNot Nothing Then
                Return gname.Identifier.ValueText & "_" & gname.TypeArgumentList.Arguments.Select(Function(t) GetNameAsIdentifier(t)).Aggregate(Function(a, b) a & "_" & b)
            End If

            Dim qname = TryCast(type, QualifiedNameSyntax)
            If qname IsNot Nothing Then
                Return GetNameAsIdentifier(qname.Right)
            End If

            Return "[" & type.ToString() & "]"
        End Function

        Private Function WithBody(declaration As SyntaxNode, allowDefault As Boolean) As SyntaxNode

            declaration = Me.WithModifiersInternal(declaration, Me.GetModifiers(declaration) - DeclarationModifiers.Abstract)

            Dim method = TryCast(declaration, MethodStatementSyntax)
            If method IsNot Nothing Then
                Return SyntaxFactory.MethodBlock(
                    kind:=If(method.IsKind(SyntaxKind.FunctionStatement), SyntaxKind.FunctionBlock, SyntaxKind.SubBlock),
                    subOrFunctionStatement:=method,
                    endSubOrFunctionStatement:=If(method.IsKind(SyntaxKind.FunctionStatement), SyntaxFactory.EndFunctionStatement(), SyntaxFactory.EndSubStatement()))
            End If

            Dim prop = TryCast(declaration, PropertyStatementSyntax)
            If prop IsNot Nothing Then
                prop = prop.WithModifiers(WithIsDefault(prop.Modifiers, GetIsDefault(prop.Modifiers) And allowDefault, GetDeclarationKind(declaration)))

                Dim accessors = New List(Of AccessorBlockSyntax)
                accessors.Add(CreateGetAccessorBlock(Nothing))

                If (Not prop.Modifiers.Any(SyntaxKind.ReadOnlyKeyword)) Then
                    accessors.Add(CreateSetAccessorBlock(prop.AsClause.Type, Nothing))
                End If

                Return SyntaxFactory.PropertyBlock(
                    propertyStatement:=prop,
                    accessors:=SyntaxFactory.List(accessors),
                    endPropertyStatement:=SyntaxFactory.EndPropertyStatement())
            End If

            Return declaration
        End Function

        Private Function GetIsDefault(modifierList As SyntaxTokenList) As Boolean
            Dim access As Accessibility
            Dim modifiers As DeclarationModifiers
            Dim isDefault As Boolean

            Me.GetAccessibilityAndModifiers(modifierList, access, modifiers, isDefault)

            Return isDefault
        End Function

        Private Function WithIsDefault(modifierList As SyntaxTokenList, isDefault As Boolean, kind As DeclarationKind) As SyntaxTokenList
            Dim access As Accessibility
            Dim modifiers As DeclarationModifiers
            Dim currentIsDefault As Boolean

            Me.GetAccessibilityAndModifiers(modifierList, access, modifiers, currentIsDefault)

            If currentIsDefault <> isDefault Then
                Return GetModifierList(access, modifiers, kind, isDefault)
            Else
                Return modifierList
            End If
        End Function

        Public Overrides Function ConstructorDeclaration(
            Optional name As String = Nothing,
            Optional parameters As IEnumerable(Of SyntaxNode) = Nothing,
            Optional accessibility As Accessibility = Nothing,
            Optional modifiers As DeclarationModifiers = Nothing,
            Optional baseConstructorArguments As IEnumerable(Of SyntaxNode) = Nothing,
            Optional statements As IEnumerable(Of SyntaxNode) = Nothing) As SyntaxNode

            Dim stats = GetStatementList(statements)

            If (baseConstructorArguments IsNot Nothing) Then
                Dim baseCall = DirectCast(Me.ExpressionStatement(Me.InvocationExpression(Me.MemberAccessExpression(Me.BaseExpression(), SyntaxFactory.IdentifierName("New")), baseConstructorArguments)), StatementSyntax)
                stats = stats.Insert(0, baseCall)
            End If

            Return SyntaxFactory.ConstructorBlock(
                subNewStatement:=SyntaxFactory.SubNewStatement(
                    attributeLists:=Nothing,
                    modifiers:=GetModifierList(accessibility, modifiers And s_constructorModifiers, DeclarationKind.Constructor),
                    parameterList:=If(parameters IsNot Nothing, SyntaxFactory.ParameterList(SyntaxFactory.SeparatedList(parameters.Cast(Of ParameterSyntax)())), SyntaxFactory.ParameterList())),
                statements:=stats)
        End Function

        Public Overrides Function ClassDeclaration(
            name As String,
            Optional typeParameters As IEnumerable(Of String) = Nothing,
            Optional accessibility As Accessibility = Nothing,
            Optional modifiers As DeclarationModifiers = Nothing,
            Optional baseType As SyntaxNode = Nothing,
            Optional interfaceTypes As IEnumerable(Of SyntaxNode) = Nothing,
            Optional members As IEnumerable(Of SyntaxNode) = Nothing) As SyntaxNode

            Dim itypes = If(interfaceTypes IsNot Nothing, interfaceTypes.Cast(Of TypeSyntax), Nothing)
            If itypes IsNot Nothing AndAlso itypes.Count = 0 Then
                itypes = Nothing
            End If

            Return SyntaxFactory.ClassBlock(
                classStatement:=SyntaxFactory.ClassStatement(
                    attributeLists:=Nothing,
                    modifiers:=GetModifierList(accessibility, modifiers And s_classModifiers, DeclarationKind.Class),
                    identifier:=name.ToIdentifierToken(),
                    typeParameterList:=GetTypeParameters(typeParameters)),
                    [inherits]:=If(baseType IsNot Nothing, SyntaxFactory.SingletonList(SyntaxFactory.InheritsStatement(DirectCast(baseType, TypeSyntax))), Nothing),
                    [implements]:=If(itypes IsNot Nothing, SyntaxFactory.SingletonList(SyntaxFactory.ImplementsStatement(SyntaxFactory.SeparatedList(itypes))), Nothing),
                    members:=AsClassMembers(members))
        End Function

        Private Function AsClassMembers(nodes As IEnumerable(Of SyntaxNode)) As SyntaxList(Of StatementSyntax)
            If nodes IsNot Nothing Then
                Return SyntaxFactory.List(nodes.Select(AddressOf AsClassMember).Where(Function(n) n IsNot Nothing))
            Else
                Return Nothing
            End If
        End Function

        Private Function AsClassMember(node As SyntaxNode) As StatementSyntax
            Return TryCast(AsIsolatedDeclaration(node), StatementSyntax)
        End Function

        Public Overrides Function StructDeclaration(
            name As String,
            Optional typeParameters As IEnumerable(Of String) = Nothing,
            Optional accessibility As Accessibility = Nothing,
            Optional modifiers As DeclarationModifiers = Nothing,
            Optional interfaceTypes As IEnumerable(Of SyntaxNode) = Nothing,
            Optional members As IEnumerable(Of SyntaxNode) = Nothing) As SyntaxNode

            Dim itypes = If(interfaceTypes IsNot Nothing, interfaceTypes.Cast(Of TypeSyntax), Nothing)
            If itypes IsNot Nothing AndAlso itypes.Count = 0 Then
                itypes = Nothing
            End If

            Return SyntaxFactory.StructureBlock(
                structureStatement:=SyntaxFactory.StructureStatement(
                    attributeLists:=Nothing,
                    modifiers:=GetModifierList(accessibility, modifiers And s_structModifiers, DeclarationKind.Struct),
                    identifier:=name.ToIdentifierToken(),
                    typeParameterList:=GetTypeParameters(typeParameters)),
                [inherits]:=Nothing,
                [implements]:=If(itypes IsNot Nothing, SyntaxFactory.SingletonList(SyntaxFactory.ImplementsStatement(SyntaxFactory.SeparatedList(itypes))), Nothing),
                members:=If(members IsNot Nothing, SyntaxFactory.List(members.Cast(Of StatementSyntax)()), Nothing))
        End Function

        Private Function AsStructureMembers(nodes As IEnumerable(Of SyntaxNode)) As SyntaxList(Of StatementSyntax)
            If nodes IsNot Nothing Then
                Return SyntaxFactory.List(nodes.Select(AddressOf AsStructureMember).Where(Function(n) n IsNot Nothing))
            Else
                Return Nothing
            End If
        End Function

        Private Function AsStructureMember(node As SyntaxNode) As StatementSyntax
            Return TryCast(node, StatementSyntax)
        End Function

        Public Overrides Function InterfaceDeclaration(
            name As String,
            Optional typeParameters As IEnumerable(Of String) = Nothing,
            Optional accessibility As Accessibility = Nothing,
            Optional interfaceTypes As IEnumerable(Of SyntaxNode) = Nothing,
            Optional members As IEnumerable(Of SyntaxNode) = Nothing) As SyntaxNode

            Dim itypes = If(interfaceTypes IsNot Nothing, interfaceTypes.Cast(Of TypeSyntax), Nothing)
            If itypes IsNot Nothing AndAlso itypes.Count = 0 Then
                itypes = Nothing
            End If

            Return SyntaxFactory.InterfaceBlock(
                interfaceStatement:=SyntaxFactory.InterfaceStatement(
                    attributeLists:=Nothing,
                    modifiers:=GetModifierList(accessibility, DeclarationModifiers.None, DeclarationKind.Interface),
                    identifier:=name.ToIdentifierToken(),
                    typeParameterList:=GetTypeParameters(typeParameters)),
                [inherits]:=If(itypes IsNot Nothing, SyntaxFactory.SingletonList(SyntaxFactory.InheritsStatement(SyntaxFactory.SeparatedList(itypes))), Nothing),
                [implements]:=Nothing,
                members:=AsInterfaceMembers(members))
        End Function

        Private Function AsInterfaceMembers(nodes As IEnumerable(Of SyntaxNode)) As SyntaxList(Of StatementSyntax)
            If nodes IsNot Nothing Then
                Return SyntaxFactory.List(nodes.Select(AddressOf AsInterfaceMember).Where(Function(n) n IsNot Nothing))
            Else
                Return Nothing
            End If
        End Function

        Private Function AsInterfaceMember(node As SyntaxNode) As StatementSyntax
            Select Case node.Kind
                Case SyntaxKind.FunctionBlock,
                     SyntaxKind.SubBlock
                    Return AsInterfaceMember(DirectCast(node, MethodBlockSyntax).BlockStatement)
                Case SyntaxKind.FunctionStatement,
                     SyntaxKind.SubStatement
                    Return DirectCast(node, MethodStatementSyntax).WithModifiers(Nothing)
                Case SyntaxKind.PropertyBlock
                    Return AsInterfaceMember(DirectCast(node, PropertyBlockSyntax).PropertyStatement)
                Case SyntaxKind.PropertyStatement
                    Dim propertyStatement = DirectCast(node, PropertyStatementSyntax)
                    Dim mods = SyntaxFactory.TokenList(propertyStatement.Modifiers.Where(Function(tk) tk.IsKind(SyntaxKind.ReadOnlyKeyword) Or tk.IsKind(SyntaxKind.DefaultKeyword)))
                    Return propertyStatement.WithModifiers(mods)
                Case SyntaxKind.EventBlock
                    Return AsInterfaceMember(DirectCast(node, EventBlockSyntax).EventStatement)
                Case SyntaxKind.EventStatement
                    Return DirectCast(node, EventStatementSyntax).WithModifiers(Nothing).WithCustomKeyword(Nothing)
            End Select
            Return Nothing
        End Function

        Public Overrides Function EnumDeclaration(
            name As String,
            Optional accessibility As Accessibility = Nothing,
            Optional modifiers As DeclarationModifiers = Nothing,
            Optional members As IEnumerable(Of SyntaxNode) = Nothing) As SyntaxNode

            Return SyntaxFactory.EnumBlock(
                enumStatement:=SyntaxFactory.EnumStatement(
                    attributeLists:=Nothing,
                    modifiers:=GetModifierList(accessibility, modifiers And GetAllowedModifiers(SyntaxKind.EnumStatement), DeclarationKind.Enum),
                    identifier:=name.ToIdentifierToken(),
                    underlyingType:=Nothing),
                    members:=AsEnumMembers(members))
        End Function

        Public Overrides Function EnumMember(name As String, Optional expression As SyntaxNode = Nothing) As SyntaxNode
            Return SyntaxFactory.EnumMemberDeclaration(
                attributeLists:=Nothing,
                identifier:=name.ToIdentifierToken(),
                initializer:=If(expression IsNot Nothing, SyntaxFactory.EqualsValue(DirectCast(expression, ExpressionSyntax)), Nothing))
        End Function

        Private Function AsEnumMembers(nodes As IEnumerable(Of SyntaxNode)) As SyntaxList(Of StatementSyntax)
            If nodes IsNot Nothing Then
                Return SyntaxFactory.List(nodes.Select(AddressOf AsEnumMember).Where(Function(n) n IsNot Nothing))
            Else
                Return Nothing
            End If
        End Function

        Private Function AsEnumMember(node As SyntaxNode) As StatementSyntax
            Dim id = TryCast(node, IdentifierNameSyntax)
            If id IsNot Nothing Then
                Return DirectCast(EnumMember(id.Identifier.ValueText), EnumMemberDeclarationSyntax)
            End If

            Return TryCast(node, EnumMemberDeclarationSyntax)
        End Function

        Public Overrides Function DelegateDeclaration(
            name As String,
            Optional parameters As IEnumerable(Of SyntaxNode) = Nothing,
            Optional typeParameters As IEnumerable(Of String) = Nothing,
            Optional returnType As SyntaxNode = Nothing,
            Optional accessibility As Accessibility = Accessibility.NotApplicable,
            Optional modifiers As DeclarationModifiers = Nothing) As SyntaxNode

            Dim kind = If(returnType Is Nothing, SyntaxKind.DelegateSubStatement, SyntaxKind.DelegateFunctionStatement)

            Return SyntaxFactory.DelegateStatement(
                kind:=kind,
                attributeLists:=Nothing,
                modifiers:=GetModifierList(accessibility, modifiers And GetAllowedModifiers(kind), DeclarationKind.Delegate),
                subOrFunctionKeyword:=If(kind = SyntaxKind.DelegateSubStatement, SyntaxFactory.Token(SyntaxKind.SubKeyword), SyntaxFactory.Token(SyntaxKind.FunctionKeyword)),
                identifier:=name.ToIdentifierToken(),
                typeParameterList:=GetTypeParameters(typeParameters),
                parameterList:=GetParameterList(parameters),
                asClause:=If(kind = SyntaxKind.DelegateFunctionStatement, SyntaxFactory.SimpleAsClause(DirectCast(returnType, TypeSyntax)), Nothing))
        End Function

        Public Overrides Function CompilationUnit(declarations As IEnumerable(Of SyntaxNode)) As SyntaxNode
            Return SyntaxFactory.CompilationUnit().WithImports(AsImports(declarations)).WithMembers(AsNamespaceMembers(declarations))
        End Function

        Private Function AsImports(declarations As IEnumerable(Of SyntaxNode)) As SyntaxList(Of ImportsStatementSyntax)
            Return If(declarations Is Nothing, Nothing, SyntaxFactory.List(declarations.Select(AddressOf AsNamespaceImport).OfType(Of ImportsStatementSyntax)()))
        End Function

        Private Function AsNamespaceImport(node As SyntaxNode) As SyntaxNode
            Dim name = TryCast(node, NameSyntax)
            If name IsNot Nothing Then
                Return Me.NamespaceImportDeclaration(name)
            End If
            Return TryCast(node, ImportsStatementSyntax)
        End Function

        Private Function AsNamespaceMembers(declarations As IEnumerable(Of SyntaxNode)) As SyntaxList(Of StatementSyntax)
            Return If(declarations Is Nothing, Nothing, SyntaxFactory.List(declarations.OfType(Of StatementSyntax)().Where(Function(s) Not TypeOf s Is ImportsStatementSyntax)))
        End Function

        Public Overrides Function NamespaceImportDeclaration(name As SyntaxNode) As SyntaxNode
            Return SyntaxFactory.ImportsStatement(SyntaxFactory.SingletonSeparatedList(Of ImportsClauseSyntax)(SyntaxFactory.SimpleImportsClause(DirectCast(name, NameSyntax))))
        End Function

        Public Overrides Function NamespaceDeclaration(name As SyntaxNode, nestedDeclarations As IEnumerable(Of SyntaxNode)) As SyntaxNode
            Dim imps As IEnumerable(Of StatementSyntax) = AsImports(nestedDeclarations)
            Dim members As IEnumerable(Of StatementSyntax) = AsNamespaceMembers(nestedDeclarations)

            ' put imports at start
            Dim statements = imps.Concat(members)

            Return SyntaxFactory.NamespaceBlock(
                SyntaxFactory.NamespaceStatement(DirectCast(name, NameSyntax)),
                members:=SyntaxFactory.List(statements))
        End Function

        Public Overrides Function Attribute(name As SyntaxNode, Optional attributeArguments As IEnumerable(Of SyntaxNode) = Nothing) As SyntaxNode
            Dim attr = SyntaxFactory.Attribute(
                target:=Nothing,
                name:=DirectCast(name, TypeSyntax),
                argumentList:=AsArgumentList(attributeArguments))

            Return AsAttributeList(attr)
        End Function

        Private Function AsArgumentList(arguments As IEnumerable(Of SyntaxNode)) As ArgumentListSyntax
            If arguments IsNot Nothing Then
                Return SyntaxFactory.ArgumentList(SyntaxFactory.SeparatedList(arguments.Select(AddressOf AsArgument)))
            Else
                Return Nothing
            End If
        End Function

        Public Overrides Function AttributeArgument(name As String, expression As SyntaxNode) As SyntaxNode
            Return Argument(name, RefKind.None, expression)
        End Function

        Public Overrides Function ClearTrivia(Of TNode As SyntaxNode)(node As TNode) As TNode
            If node IsNot Nothing Then
                Return node.WithLeadingTrivia(SyntaxFactory.ElasticMarker).WithTrailingTrivia(SyntaxFactory.ElasticMarker)
            Else
                Return Nothing
            End If
        End Function

        Private Function AsAttributeLists(attributes As IEnumerable(Of SyntaxNode)) As SyntaxList(Of AttributeListSyntax)
            If attributes IsNot Nothing Then
                Return SyntaxFactory.List(attributes.Select(AddressOf AsAttributeList))
            Else
                Return Nothing
            End If
        End Function

        Private Function AsAttributeList(node As SyntaxNode) As AttributeListSyntax
            Dim attr = TryCast(node, AttributeSyntax)
            If attr IsNot Nothing Then
                Return SyntaxFactory.AttributeList(SyntaxFactory.SingletonSeparatedList(WithNoTarget(attr)))
            Else
                Return WithNoTargets(DirectCast(node, AttributeListSyntax))
            End If
        End Function

        Private Overloads Function WithNoTargets(attrs As AttributeListSyntax) As AttributeListSyntax
            If (attrs.Attributes.Any(Function(a) a.Target IsNot Nothing)) Then
                Return attrs.WithAttributes(SyntaxFactory.SeparatedList(attrs.Attributes.Select(AddressOf WithAssemblyTarget)))
            Else
                Return attrs
            End If
        End Function

        Private Overloads Function WithNoTarget(attr As AttributeSyntax) As AttributeSyntax
            Return attr.WithTarget(Nothing)
        End Function

        Public Overrides Function GetAttributes(declaration As SyntaxNode) As IReadOnlyList(Of SyntaxNode)
            Return Me.Flatten(GetAttributeLists(declaration))
        End Function

        Public Overrides Function InsertAttributes(declaration As SyntaxNode, index As Integer, attributes As IEnumerable(Of SyntaxNode)) As SyntaxNode
            Return Isolate(declaration, Function(d) InsertAttributesInternal(d, index, attributes))
        End Function

        Private Function InsertAttributesInternal(declaration As SyntaxNode, index As Integer, attributes As IEnumerable(Of SyntaxNode)) As SyntaxNode
            Dim newAttributes = AsAttributeLists(attributes)
            Dim existingAttributes = Me.GetAttributes(declaration)

            If index >= 0 AndAlso index < existingAttributes.Count Then
                Return Me.InsertNodesBefore(declaration, existingAttributes(index), newAttributes)
            ElseIf existingAttributes.Count > 0 Then
                Return Me.InsertNodesAfter(declaration, existingAttributes(existingAttributes.Count - 1), newAttributes)
            Else
                Dim lists = Me.GetAttributeLists(declaration)
                Return Me.WithAttributeLists(declaration, lists.AddRange(AsAttributeLists(attributes)))
            End If
        End Function

        Private Shared Function HasAssemblyTarget(attr As AttributeSyntax) As Boolean
            Return attr.Target IsNot Nothing AndAlso attr.Target.AttributeModifier.IsKind(SyntaxKind.AssemblyKeyword)
        End Function

        Private Overloads Function WithAssemblyTargets(attrs As AttributeListSyntax) As AttributeListSyntax
            If attrs.Attributes.Any(Function(a) Not HasAssemblyTarget(a)) Then
                Return attrs.WithAttributes(SyntaxFactory.SeparatedList(attrs.Attributes.Select(AddressOf WithAssemblyTarget)))
            Else
                Return attrs
            End If
        End Function

        Private Overloads Function WithAssemblyTarget(attr As AttributeSyntax) As AttributeSyntax
            If Not HasAssemblyTarget(attr) Then
                Return attr.WithTarget(SyntaxFactory.AttributeTarget(SyntaxFactory.Token(SyntaxKind.AssemblyKeyword)))
            Else
                Return attr
            End If
        End Function

        Public Overrides Function GetReturnAttributes(declaration As SyntaxNode) As IReadOnlyList(Of SyntaxNode)
            Return Me.Flatten(GetReturnAttributeLists(declaration))
        End Function

        Public Overrides Function InsertReturnAttributes(declaration As SyntaxNode, index As Integer, attributes As IEnumerable(Of SyntaxNode)) As SyntaxNode
            Select Case declaration.Kind
                Case SyntaxKind.FunctionBlock,
                     SyntaxKind.FunctionStatement,
                     SyntaxKind.DelegateFunctionStatement
                    Return Isolate(declaration, Function(d) InsertReturnAttributesInternal(d, index, attributes))
                Case Else
                    Return declaration
            End Select
        End Function

        Private Function InsertReturnAttributesInternal(declaration As SyntaxNode, index As Integer, attributes As IEnumerable(Of SyntaxNode)) As SyntaxNode
            Dim newAttributes = AsAttributeLists(attributes)
            Dim existingReturnAttributes = Me.GetReturnAttributes(declaration)

            If index >= 0 AndAlso index < existingReturnAttributes.Count Then
                Return Me.InsertNodesBefore(declaration, existingReturnAttributes(index), newAttributes)
            ElseIf existingReturnAttributes.Count > 0 Then
                Return Me.InsertNodesAfter(declaration, existingReturnAttributes(existingReturnAttributes.Count - 1), newAttributes)
            Else
                Dim lists = Me.GetReturnAttributeLists(declaration)
                Dim newLists = lists.AddRange(newAttributes)
                Return Me.WithReturnAttributeLists(declaration, newLists)
            End If
        End Function

        Private Function GetReturnAttributeLists(declaration As SyntaxNode) As SyntaxList(Of AttributeListSyntax)
            Dim asClause = GetAsClause(declaration)
            If asClause IsNot Nothing Then
                Select Case declaration.Kind()
                    Case SyntaxKind.FunctionBlock,
                    SyntaxKind.FunctionStatement,
                    SyntaxKind.DelegateFunctionStatement
                        Return asClause.Attributes
                End Select
            End If
            Return Nothing
        End Function

        Private Function WithReturnAttributeLists(declaration As SyntaxNode, lists As IEnumerable(Of AttributeListSyntax)) As SyntaxNode
            If declaration Is Nothing Then
                Return Nothing
            End If

            Select Case declaration.Kind()
                Case SyntaxKind.FunctionBlock
                    Dim fb = DirectCast(declaration, MethodBlockSyntax)
                    Dim asClause = DirectCast(WithReturnAttributeLists(GetAsClause(declaration), lists), SimpleAsClauseSyntax)
                    Return fb.WithSubOrFunctionStatement(fb.SubOrFunctionStatement.WithAsClause(asClause))
                Case SyntaxKind.FunctionStatement
                    Dim ms = DirectCast(declaration, MethodStatementSyntax)
                    Dim asClause = DirectCast(WithReturnAttributeLists(GetAsClause(declaration), lists), SimpleAsClauseSyntax)
                    Return ms.WithAsClause(asClause)
                Case SyntaxKind.DelegateFunctionStatement
                    Dim df = DirectCast(declaration, DelegateStatementSyntax)
                    Dim asClause = DirectCast(WithReturnAttributeLists(GetAsClause(declaration), lists), SimpleAsClauseSyntax)
                    Return df.WithAsClause(asClause)
                Case SyntaxKind.SimpleAsClause
                    Return DirectCast(declaration, SimpleAsClauseSyntax).WithAttributeLists(SyntaxFactory.List(lists))
                Case Else
                    Return Nothing
            End Select
        End Function

        Private Function GetAttributeLists(node As SyntaxNode) As SyntaxList(Of AttributeListSyntax)
            Select Case node.Kind
                Case SyntaxKind.CompilationUnit
                    Return SyntaxFactory.List(DirectCast(node, CompilationUnitSyntax).Attributes.SelectMany(Function(s) s.AttributeLists))
                Case SyntaxKind.ClassBlock
                    Return DirectCast(node, ClassBlockSyntax).BlockStatement.AttributeLists
                Case SyntaxKind.ClassStatement
                    Return DirectCast(node, ClassStatementSyntax).AttributeLists
                Case SyntaxKind.StructureBlock
                    Return DirectCast(node, StructureBlockSyntax).BlockStatement.AttributeLists
                Case SyntaxKind.StructureStatement
                    Return DirectCast(node, StructureStatementSyntax).AttributeLists
                Case SyntaxKind.InterfaceBlock
                    Return DirectCast(node, InterfaceBlockSyntax).BlockStatement.AttributeLists
                Case SyntaxKind.InterfaceStatement
                    Return DirectCast(node, InterfaceStatementSyntax).AttributeLists
                Case SyntaxKind.EnumBlock
                    Return DirectCast(node, EnumBlockSyntax).EnumStatement.AttributeLists
                Case SyntaxKind.EnumStatement
                    Return DirectCast(node, EnumStatementSyntax).AttributeLists
                Case SyntaxKind.EnumMemberDeclaration
                    Return DirectCast(node, EnumMemberDeclarationSyntax).AttributeLists
                Case SyntaxKind.DelegateFunctionStatement,
                     SyntaxKind.DelegateSubStatement
                    Return DirectCast(node, DelegateStatementSyntax).AttributeLists
                Case SyntaxKind.FieldDeclaration
                    Return DirectCast(node, FieldDeclarationSyntax).AttributeLists
                Case SyntaxKind.FunctionBlock,
                     SyntaxKind.SubBlock,
                     SyntaxKind.ConstructorBlock
                    Return DirectCast(node, MethodBlockSyntax).BlockStatement.AttributeLists
                Case SyntaxKind.FunctionStatement,
                     SyntaxKind.SubStatement
                    Return DirectCast(node, MethodStatementSyntax).AttributeLists
                Case SyntaxKind.ConstructorBlock
                    Return DirectCast(node, ConstructorBlockSyntax).BlockStatement.AttributeLists
                Case SyntaxKind.SubNewStatement
                    Return DirectCast(node, SubNewStatementSyntax).AttributeLists
                Case SyntaxKind.Parameter
                    Return DirectCast(node, ParameterSyntax).AttributeLists
                Case SyntaxKind.PropertyBlock
                    Return DirectCast(node, PropertyBlockSyntax).PropertyStatement.AttributeLists
                Case SyntaxKind.PropertyStatement
                    Return DirectCast(node, PropertyStatementSyntax).AttributeLists
                Case SyntaxKind.OperatorBlock
                    Return DirectCast(node, OperatorBlockSyntax).BlockStatement.AttributeLists
                Case SyntaxKind.OperatorStatement
                    Return DirectCast(node, OperatorStatementSyntax).AttributeLists
                Case SyntaxKind.EventBlock
                    Return DirectCast(node, EventBlockSyntax).EventStatement.AttributeLists
                Case SyntaxKind.EventStatement
                    Return DirectCast(node, EventStatementSyntax).AttributeLists
                Case SyntaxKind.GetAccessorBlock,
                    SyntaxKind.SetAccessorBlock,
                    SyntaxKind.AddHandlerAccessorBlock,
                    SyntaxKind.RemoveHandlerAccessorBlock,
                    SyntaxKind.RaiseEventAccessorBlock
                    Return DirectCast(node, AccessorBlockSyntax).AccessorStatement.AttributeLists
                Case SyntaxKind.GetAccessorStatement,
                    SyntaxKind.SetAccessorStatement,
                    SyntaxKind.AddHandlerAccessorStatement,
                    SyntaxKind.RemoveHandlerAccessorStatement,
                    SyntaxKind.RaiseEventAccessorStatement
                    Return DirectCast(node, AccessorStatementSyntax).AttributeLists
                Case Else
                    Return Nothing
            End Select
        End Function

        Private Function WithAttributeLists(node As SyntaxNode, lists As IEnumerable(Of AttributeListSyntax)) As SyntaxNode
            Dim arg = SyntaxFactory.List(lists)

            Select Case node.Kind
                Case SyntaxKind.CompilationUnit
                    'convert to assembly target 
                    arg = SyntaxFactory.List(lists.Select(Function(lst) Me.WithAssemblyTargets(lst)))
                    ' add as single attributes statement
                    Return DirectCast(node, CompilationUnitSyntax).WithAttributes(SyntaxFactory.SingletonList(SyntaxFactory.AttributesStatement(arg)))
                Case SyntaxKind.ClassBlock
                    Return DirectCast(node, ClassBlockSyntax).WithClassStatement(DirectCast(node, ClassBlockSyntax).ClassStatement.WithAttributeLists(arg))
                Case SyntaxKind.ClassStatement
                    Return DirectCast(node, ClassStatementSyntax).WithAttributeLists(arg)
                Case SyntaxKind.StructureBlock
                    Return DirectCast(node, StructureBlockSyntax).WithStructureStatement(DirectCast(node, StructureBlockSyntax).StructureStatement.WithAttributeLists(arg))
                Case SyntaxKind.StructureStatement
                    Return DirectCast(node, StructureStatementSyntax).WithAttributeLists(arg)
                Case SyntaxKind.InterfaceBlock
                    Return DirectCast(node, InterfaceBlockSyntax).WithInterfaceStatement(DirectCast(node, InterfaceBlockSyntax).InterfaceStatement.WithAttributeLists(arg))
                Case SyntaxKind.InterfaceStatement
                    Return DirectCast(node, InterfaceStatementSyntax).WithAttributeLists(arg)
                Case SyntaxKind.EnumBlock
                    Return DirectCast(node, EnumBlockSyntax).WithEnumStatement(DirectCast(node, EnumBlockSyntax).EnumStatement.WithAttributeLists(arg))
                Case SyntaxKind.EnumStatement
                    Return DirectCast(node, EnumStatementSyntax).WithAttributeLists(arg)
                Case SyntaxKind.EnumMemberDeclaration
                    Return DirectCast(node, EnumMemberDeclarationSyntax).WithAttributeLists(arg)
                Case SyntaxKind.DelegateFunctionStatement,
                     SyntaxKind.DelegateSubStatement
                    Return DirectCast(node, DelegateStatementSyntax).WithAttributeLists(arg)
                Case SyntaxKind.FieldDeclaration
                    Return DirectCast(node, FieldDeclarationSyntax).WithAttributeLists(arg)
                Case SyntaxKind.FunctionBlock,
                     SyntaxKind.SubBlock
                    Return DirectCast(node, MethodBlockSyntax).WithSubOrFunctionStatement(DirectCast(node, MethodBlockSyntax).SubOrFunctionStatement.WithAttributeLists(arg))
                Case SyntaxKind.FunctionStatement,
                     SyntaxKind.SubStatement
                    Return DirectCast(node, MethodStatementSyntax).WithAttributeLists(arg)
                Case SyntaxKind.ConstructorBlock
                    Return DirectCast(node, ConstructorBlockSyntax).WithSubNewStatement(DirectCast(node, ConstructorBlockSyntax).SubNewStatement.WithAttributeLists(arg))
                Case SyntaxKind.SubNewStatement
                    Return DirectCast(node, SubNewStatementSyntax).WithAttributeLists(arg)
                Case SyntaxKind.Parameter
                    Return DirectCast(node, ParameterSyntax).WithAttributeLists(arg)
                Case SyntaxKind.PropertyBlock
                    Return DirectCast(node, PropertyBlockSyntax).WithPropertyStatement(DirectCast(node, PropertyBlockSyntax).PropertyStatement.WithAttributeLists(arg))
                Case SyntaxKind.PropertyStatement
                    Return DirectCast(node, PropertyStatementSyntax).WithAttributeLists(arg)
                Case SyntaxKind.OperatorBlock
                    Return DirectCast(node, OperatorBlockSyntax).WithOperatorStatement(DirectCast(node, OperatorBlockSyntax).OperatorStatement.WithAttributeLists(arg))
                Case SyntaxKind.OperatorStatement
                    Return DirectCast(node, OperatorStatementSyntax).WithAttributeLists(arg)
                Case SyntaxKind.EventBlock
                    Return DirectCast(node, EventBlockSyntax).WithEventStatement(DirectCast(node, EventBlockSyntax).EventStatement.WithAttributeLists(arg))
                Case SyntaxKind.EventStatement
                    Return DirectCast(node, EventStatementSyntax).WithAttributeLists(arg)
                Case SyntaxKind.GetAccessorBlock,
                    SyntaxKind.SetAccessorBlock,
                    SyntaxKind.AddHandlerAccessorBlock,
                    SyntaxKind.RemoveHandlerAccessorBlock,
                    SyntaxKind.RaiseEventAccessorBlock
                    Return DirectCast(node, AccessorBlockSyntax).WithAccessorStatement(DirectCast(node, AccessorBlockSyntax).AccessorStatement.WithAttributeLists(arg))
                Case SyntaxKind.GetAccessorStatement,
                    SyntaxKind.SetAccessorStatement,
                    SyntaxKind.AddHandlerAccessorStatement,
                    SyntaxKind.RemoveHandlerAccessorStatement,
                    SyntaxKind.RaiseEventAccessorStatement
                    Return DirectCast(node, AccessorStatementSyntax).WithAttributeLists(arg)
                Case Else
                    Return node
            End Select
        End Function

        Public Overrides Function GetDeclarationKind(declaration As SyntaxNode) As DeclarationKind
            Select Case declaration.Kind
                Case SyntaxKind.CompilationUnit
                    Return DeclarationKind.CompilationUnit
                Case SyntaxKind.NamespaceBlock
                    Return DeclarationKind.Namespace
                Case SyntaxKind.ImportsStatement
                    Return DeclarationKind.NamespaceImport
                Case SyntaxKind.ClassBlock
                    Return DeclarationKind.Class
                Case SyntaxKind.StructureBlock
                    Return DeclarationKind.Struct
                Case SyntaxKind.InterfaceBlock
                    Return DeclarationKind.Interface
                Case SyntaxKind.EnumBlock
                    Return DeclarationKind.Enum
                Case SyntaxKind.EnumMemberDeclaration
                    Return DeclarationKind.EnumMember
                Case SyntaxKind.DelegateFunctionStatement,
                     SyntaxKind.DelegateSubStatement
                    Return DeclarationKind.Delegate
                Case SyntaxKind.FunctionBlock,
                     SyntaxKind.SubBlock
                    Return DeclarationKind.Method
                Case SyntaxKind.FunctionStatement
                    If Not IsChildOf(declaration, SyntaxKind.FunctionBlock) Then
                        Return DeclarationKind.Method
                    End If
                Case SyntaxKind.SubStatement
                    If Not IsChildOf(declaration, SyntaxKind.SubBlock) Then
                        Return DeclarationKind.Method
                    End If
                Case SyntaxKind.ConstructorBlock
                    Return DeclarationKind.Constructor
                Case SyntaxKind.PropertyBlock
                    If IsIndexer(declaration) Then
                        Return DeclarationKind.Indexer
                    Else
                        Return DeclarationKind.Property
                    End If
                Case SyntaxKind.PropertyStatement
                    If Not IsChildOf(declaration, SyntaxKind.PropertyBlock) Then
                        If IsIndexer(declaration) Then
                            Return DeclarationKind.Indexer
                        Else
                            Return DeclarationKind.Property
                        End If
                    End If
                Case SyntaxKind.OperatorBlock
                    Return DeclarationKind.Operator
                Case SyntaxKind.OperatorStatement
                    If Not IsChildOf(declaration, SyntaxKind.OperatorBlock) Then
                        Return DeclarationKind.Operator
                    End If
                Case SyntaxKind.EventBlock
                    Return DeclarationKind.CustomEvent
                Case SyntaxKind.EventStatement
                    If Not IsChildOf(declaration, SyntaxKind.EventBlock) Then
                        Return DeclarationKind.Event
                    End If
                Case SyntaxKind.Parameter
                    Return DeclarationKind.Parameter
                Case SyntaxKind.FieldDeclaration
                    If GetDeclarationCount(declaration) = 1 Then
                        Return DeclarationKind.Field
                    End If
                Case SyntaxKind.LocalDeclarationStatement
                    If GetDeclarationCount(declaration) = 1 Then
                        Return DeclarationKind.Variable
                    End If
                Case SyntaxKind.ModifiedIdentifier
                    If IsChildOf(declaration, SyntaxKind.VariableDeclarator) Then
                        If IsChildOf(declaration.Parent, SyntaxKind.FieldDeclaration) And GetDeclarationCount(declaration.Parent.Parent) > 1 Then
                            Return DeclarationKind.Field
                        ElseIf IsChildOf(declaration.Parent, SyntaxKind.LocalDeclarationStatement) And GetDeclarationCount(declaration.Parent.Parent) > 1 Then
                            Return DeclarationKind.Variable
                        End If
                    End If
                Case SyntaxKind.Attribute
                    Dim list = TryCast(declaration.Parent, AttributeListSyntax)
                    If list Is Nothing OrElse list.Attributes.Count > 1 Then
                        Return DeclarationKind.Attribute
                    End If
                Case SyntaxKind.AttributeList
                    Dim list = DirectCast(declaration, AttributeListSyntax)
                    If list.Attributes.Count = 1 Then
                        Return DeclarationKind.Attribute
                    End If
                Case SyntaxKind.GetAccessorBlock
                    Return DeclarationKind.GetAccessor
                Case SyntaxKind.SetAccessorBlock
                    Return DeclarationKind.SetAccessor
                Case SyntaxKind.AddHandlerAccessorBlock
                    Return DeclarationKind.AddAccessor
                Case SyntaxKind.RemoveHandlerAccessorBlock
                    Return DeclarationKind.RemoveAccessor
                Case SyntaxKind.RaiseEventAccessorBlock
                    Return DeclarationKind.RaiseAccessor
            End Select
            Return DeclarationKind.None
        End Function

        Private Function GetDeclarationCount(nodes As IReadOnlyList(Of SyntaxNode)) As Integer
            Dim count As Integer = 0
            For i = 0 To nodes.Count - 1
                count = count + GetDeclarationCount(nodes(i))
            Next
            Return count
        End Function

        Private Function GetDeclarationCount(node As SyntaxNode) As Integer
            Select Case node.Kind
                Case SyntaxKind.FieldDeclaration
                    Return GetDeclarationCount(DirectCast(node, FieldDeclarationSyntax).Declarators)
                Case SyntaxKind.LocalDeclarationStatement
                    Return GetDeclarationCount(DirectCast(node, LocalDeclarationStatementSyntax).Declarators)
                Case SyntaxKind.VariableDeclarator
                    Return DirectCast(node, VariableDeclaratorSyntax).Names.Count
                Case SyntaxKind.AttributesStatement
                    Return GetDeclarationCount(DirectCast(node, AttributesStatementSyntax).AttributeLists)
                Case SyntaxKind.AttributeList
                    Return DirectCast(node, AttributeListSyntax).Attributes.Count
                Case SyntaxKind.ImportsStatement
                    Return DirectCast(node, ImportsStatementSyntax).ImportsClauses.Count
            End Select
            Return 1
        End Function

        Private Shared Function IsChildOf(node As SyntaxNode, kind As SyntaxKind) As Boolean
            Return node.Parent IsNot Nothing AndAlso node.Parent.IsKind(kind)
        End Function

        Private Shared Function IsChildOfVariableDeclaration(node As SyntaxNode) As Boolean
            Return IsChildOf(node, SyntaxKind.FieldDeclaration) OrElse IsChildOf(node, SyntaxKind.LocalDeclarationStatement)
        End Function

        Private Function Isolate(declaration As SyntaxNode, editor As Func(Of SyntaxNode, SyntaxNode), Optional shouldPreserveTrivia As Boolean = True) As SyntaxNode
            Dim isolated = AsIsolatedDeclaration(declaration)

            Dim result As SyntaxNode = Nothing

            If shouldPreserveTrivia Then
                result = PreserveTrivia(isolated, editor)
            Else
                result = editor(isolated)
            End If

            Return result
        End Function

        Private Function GetFullDeclaration(declaration As SyntaxNode) As SyntaxNode
            Select Case declaration.Kind
                Case SyntaxKind.ModifiedIdentifier
                    If IsChildOf(declaration, SyntaxKind.VariableDeclarator) Then
                        Return GetFullDeclaration(declaration.Parent)
                    End If
                Case SyntaxKind.VariableDeclarator
                    If IsChildOfVariableDeclaration(declaration) Then
                        Return declaration.Parent
                    End If
                Case SyntaxKind.Attribute
                    If declaration.Parent IsNot Nothing Then
                        Return declaration.Parent
                    End If
                Case SyntaxKind.SimpleImportsClause,
                     SyntaxKind.XmlNamespaceImportsClause
                    If declaration.Parent IsNot Nothing Then
                        Return declaration.Parent
                    End If
            End Select
            Return declaration
        End Function

        Private Function AsIsolatedDeclaration(declaration As SyntaxNode) As SyntaxNode
            Select Case declaration.Kind
                Case SyntaxKind.ModifiedIdentifier
                    Dim full = GetFullDeclaration(declaration)
                    If full IsNot declaration Then
                        Return WithSingleVariable(full, DirectCast(declaration, ModifiedIdentifierSyntax))
                    End If
                Case SyntaxKind.Attribute
                    Dim list = TryCast(declaration.Parent, AttributeListSyntax)
                    If list IsNot Nothing Then
                        Return list.WithAttributes(SyntaxFactory.SingletonSeparatedList(DirectCast(declaration, AttributeSyntax)))
                    End If
                Case SyntaxKind.SimpleImportsClause,
                     SyntaxKind.XmlNamespaceImportsClause
                    Dim stmt = TryCast(declaration.Parent, ImportsStatementSyntax)
                    If stmt IsNot Nothing Then
                        Return stmt.WithImportsClauses(SyntaxFactory.SingletonSeparatedList(DirectCast(declaration, ImportsClauseSyntax)))
                    End If
            End Select
            Return declaration
        End Function

        Private Function WithSingleVariable(declaration As SyntaxNode, variable As ModifiedIdentifierSyntax) As SyntaxNode
            Select Case declaration.Kind
                Case SyntaxKind.FieldDeclaration
                    Dim fd = DirectCast(declaration, FieldDeclarationSyntax)
                    Return ReplaceWithTrivia(declaration, fd.Declarators(0), fd.Declarators(0).WithNames(SyntaxFactory.SingletonSeparatedList(variable)))
                Case SyntaxKind.LocalDeclarationStatement
                    Dim ld = DirectCast(declaration, LocalDeclarationStatementSyntax)
                    Return ReplaceWithTrivia(declaration, ld.Declarators(0), ld.Declarators(0).WithNames(SyntaxFactory.SingletonSeparatedList(variable)))
                Case SyntaxKind.VariableDeclarator
                    Dim vd = DirectCast(declaration, VariableDeclaratorSyntax)
                    Return vd.WithNames(SyntaxFactory.SingletonSeparatedList(variable))
                Case Else
                    Return declaration
            End Select
        End Function

        Private Shared Function IsIndexer(declaration As SyntaxNode) As Boolean
            Select Case declaration.Kind
                Case SyntaxKind.PropertyBlock
                    Dim p = DirectCast(declaration, PropertyBlockSyntax).PropertyStatement
                    Return p.ParameterList IsNot Nothing AndAlso p.ParameterList.Parameters.Count > 0 AndAlso p.Modifiers.Any(SyntaxKind.DefaultKeyword)
                Case SyntaxKind.PropertyStatement
                    If Not IsChildOf(declaration, SyntaxKind.PropertyBlock) Then
                        Dim p = DirectCast(declaration, PropertyStatementSyntax)
                        Return p.ParameterList IsNot Nothing AndAlso p.ParameterList.Parameters.Count > 0 AndAlso p.Modifiers.Any(SyntaxKind.DefaultKeyword)
                    End If
            End Select
            Return False
        End Function

        Public Overrides Function GetName(declaration As SyntaxNode) As String
            Select Case declaration.Kind
                Case SyntaxKind.ClassBlock
                    Return DirectCast(declaration, ClassBlockSyntax).BlockStatement.Identifier.ValueText
                Case SyntaxKind.StructureBlock
                    Return DirectCast(declaration, StructureBlockSyntax).BlockStatement.Identifier.ValueText
                Case SyntaxKind.InterfaceBlock
                    Return DirectCast(declaration, InterfaceBlockSyntax).BlockStatement.Identifier.ValueText
                Case SyntaxKind.EnumBlock
                    Return DirectCast(declaration, EnumBlockSyntax).EnumStatement.Identifier.ValueText
                Case SyntaxKind.EnumMemberDeclaration
                    Return DirectCast(declaration, EnumMemberDeclarationSyntax).Identifier.ValueText
                Case SyntaxKind.DelegateFunctionStatement,
                     SyntaxKind.DelegateSubStatement
                    Return DirectCast(declaration, DelegateStatementSyntax).Identifier.ValueText
                Case SyntaxKind.FunctionBlock,
                     SyntaxKind.SubBlock
                    Return DirectCast(declaration, MethodBlockSyntax).SubOrFunctionStatement.Identifier.ValueText
                Case SyntaxKind.FunctionStatement,
                     SyntaxKind.SubStatement
                    Return DirectCast(declaration, MethodStatementSyntax).Identifier.ValueText
                Case SyntaxKind.PropertyBlock
                    Return DirectCast(declaration, PropertyBlockSyntax).PropertyStatement.Identifier.ValueText
                Case SyntaxKind.PropertyStatement
                    Return DirectCast(declaration, PropertyStatementSyntax).Identifier.ValueText
                Case SyntaxKind.EventBlock
                    Return DirectCast(declaration, EventBlockSyntax).EventStatement.Identifier.ValueText
                Case SyntaxKind.EventStatement
                    Return DirectCast(declaration, EventStatementSyntax).Identifier.ValueText
                Case SyntaxKind.EventStatement
                    Return DirectCast(declaration, EventStatementSyntax).Identifier.ValueText
                Case SyntaxKind.Parameter
                    Return DirectCast(declaration, ParameterSyntax).Identifier.Identifier.ValueText
                Case SyntaxKind.NamespaceBlock
                    Return DirectCast(declaration, NamespaceBlockSyntax).NamespaceStatement.Name.ToString()

                Case SyntaxKind.FieldDeclaration
                    Dim fd = DirectCast(declaration, FieldDeclarationSyntax)
                    If GetDeclarationCount(fd) = 1 Then
                        Return fd.Declarators(0).Names(0).Identifier.ValueText
                    End If

                Case SyntaxKind.LocalDeclarationStatement
                    Dim ld = DirectCast(declaration, LocalDeclarationStatementSyntax)
                    If GetDeclarationCount(ld) = 1 Then
                        Return ld.Declarators(0).Names(0).Identifier.ValueText
                    End If

                Case SyntaxKind.VariableDeclarator
                    Dim vd = DirectCast(declaration, VariableDeclaratorSyntax)
                    If vd.Names.Count = 1 Then
                        Return vd.Names(0).Identifier.ValueText
                    End If

                Case SyntaxKind.ModifiedIdentifier
                    Return DirectCast(declaration, ModifiedIdentifierSyntax).Identifier.ValueText

                Case SyntaxKind.Attribute
                    Return DirectCast(declaration, AttributeSyntax).Name.ToString()

                Case SyntaxKind.AttributeList
                    Dim list = DirectCast(declaration, AttributeListSyntax)
                    If list.Attributes.Count = 1 Then
                        Return list.Attributes(0).Name.ToString()
                    End If

                Case SyntaxKind.ImportsStatement
                    Dim stmt = DirectCast(declaration, ImportsStatementSyntax)
                    If stmt.ImportsClauses.Count = 1 Then
                        Return GetName(stmt.ImportsClauses(0))
                    End If

                Case SyntaxKind.SimpleImportsClause
                    Return DirectCast(declaration, SimpleImportsClauseSyntax).Name.ToString()
            End Select
            Return String.Empty
        End Function

        Public Overrides Function WithName(declaration As SyntaxNode, name As String) As SyntaxNode
            Return Isolate(declaration, Function(d) WithNameInternal(d, name))
        End Function

        Private Function WithNameInternal(declaration As SyntaxNode, name As String) As SyntaxNode
            Dim id = name.ToIdentifierToken()

            Select Case declaration.Kind
                Case SyntaxKind.ClassBlock
                    Return ReplaceWithTrivia(declaration, DirectCast(declaration, ClassBlockSyntax).BlockStatement.Identifier, id)
                Case SyntaxKind.StructureBlock
                    Return ReplaceWithTrivia(declaration, DirectCast(declaration, StructureBlockSyntax).BlockStatement.Identifier, id)
                Case SyntaxKind.InterfaceBlock
                    Return ReplaceWithTrivia(declaration, DirectCast(declaration, InterfaceBlockSyntax).BlockStatement.Identifier, id)
                Case SyntaxKind.EnumBlock
                    Return ReplaceWithTrivia(declaration, DirectCast(declaration, EnumBlockSyntax).EnumStatement.Identifier, id)
                Case SyntaxKind.EnumMemberDeclaration
                    Return ReplaceWithTrivia(declaration, DirectCast(declaration, EnumMemberDeclarationSyntax).Identifier, id)
                Case SyntaxKind.DelegateFunctionStatement,
                     SyntaxKind.DelegateSubStatement
                    Return ReplaceWithTrivia(declaration, DirectCast(declaration, DelegateStatementSyntax).Identifier, id)
                Case SyntaxKind.FunctionBlock,
                     SyntaxKind.SubBlock
                    Return ReplaceWithTrivia(declaration, DirectCast(declaration, MethodBlockSyntax).SubOrFunctionStatement.Identifier, id)
                Case SyntaxKind.FunctionStatement,
                     SyntaxKind.SubStatement
                    Return ReplaceWithTrivia(declaration, DirectCast(declaration, MethodStatementSyntax).Identifier, id)
                Case SyntaxKind.PropertyBlock
                    Return ReplaceWithTrivia(declaration, DirectCast(declaration, PropertyBlockSyntax).PropertyStatement.Identifier, id)
                Case SyntaxKind.PropertyStatement
                    Return ReplaceWithTrivia(declaration, DirectCast(declaration, PropertyStatementSyntax).Identifier, id)
                Case SyntaxKind.EventBlock
                    Return ReplaceWithTrivia(declaration, DirectCast(declaration, EventBlockSyntax).EventStatement.Identifier, id)
                Case SyntaxKind.EventStatement
                    Return ReplaceWithTrivia(declaration, DirectCast(declaration, EventStatementSyntax).Identifier, id)
                Case SyntaxKind.EventStatement
                    Return ReplaceWithTrivia(declaration, DirectCast(declaration, EventStatementSyntax).Identifier, id)
                Case SyntaxKind.Parameter
                    Return ReplaceWithTrivia(declaration, DirectCast(declaration, ParameterSyntax).Identifier.Identifier, id)
                Case SyntaxKind.NamespaceBlock
                    Return ReplaceWithTrivia(declaration, DirectCast(declaration, NamespaceBlockSyntax).NamespaceStatement.Name, Me.DottedName(name))
                Case SyntaxKind.LocalDeclarationStatement
                    Dim ld = DirectCast(declaration, LocalDeclarationStatementSyntax)
                    If ld.Declarators.Count = 1 AndAlso ld.Declarators(0).Names.Count = 1 Then
                        Return ReplaceWithTrivia(declaration, ld.Declarators(0).Names(0).Identifier, id)
                    End If
                Case SyntaxKind.FieldDeclaration
                    Dim fd = DirectCast(declaration, FieldDeclarationSyntax)
                    If fd.Declarators.Count = 1 AndAlso fd.Declarators(0).Names.Count = 1 Then
                        Return ReplaceWithTrivia(declaration, fd.Declarators(0).Names(0).Identifier, id)
                    End If
                Case SyntaxKind.Attribute
                    Return ReplaceWithTrivia(declaration, DirectCast(declaration, AttributeSyntax).Name, Me.DottedName(name))
                Case SyntaxKind.AttributeList
                    Dim al = DirectCast(declaration, AttributeListSyntax)
                    If al.Attributes.Count = 1 Then
                        Return ReplaceWithTrivia(declaration, al.Attributes(0).Name, Me.DottedName(name))
                    End If
                Case SyntaxKind.ImportsStatement
                    Dim stmt = DirectCast(declaration, ImportsStatementSyntax)
                    If stmt.ImportsClauses.Count = 1 Then
                        Dim clause = stmt.ImportsClauses(0)
                        Select Case clause.Kind
                            Case SyntaxKind.SimpleImportsClause
                                Return ReplaceWithTrivia(declaration, DirectCast(clause, SimpleImportsClauseSyntax).Name, Me.DottedName(name))
                        End Select
                    End If
            End Select

            Return declaration
        End Function

        Public Overrides Function [GetType](declaration As SyntaxNode) As SyntaxNode
            Select Case declaration.Kind
                Case SyntaxKind.ModifiedIdentifier
                    Dim vd = TryCast(declaration.Parent, VariableDeclaratorSyntax)
                    If vd IsNot Nothing Then
                        Return [GetType](vd)
                    End If
                Case Else
                    Dim asClause = GetAsClause(declaration)
                    If asClause IsNot Nothing Then
                        Return asClause.Type
                    End If
            End Select
            Return Nothing
        End Function

        Public Overrides Function WithType(declaration As SyntaxNode, type As SyntaxNode) As SyntaxNode
            Return Isolate(declaration, Function(d) WithTypeInternal(d, type))
        End Function

        Private Function WithTypeInternal(declaration As SyntaxNode, type As SyntaxNode) As SyntaxNode

            If type Is Nothing Then
                declaration = AsSub(declaration)
            Else
                declaration = AsFunction(declaration)
            End If

            Dim asClause = GetAsClause(declaration)

            If asClause IsNot Nothing Then
                If type IsNot Nothing Then
                    Select Case asClause.Kind
                        Case SyntaxKind.SimpleAsClause
                            asClause = DirectCast(asClause, SimpleAsClauseSyntax).WithType(DirectCast(type, TypeSyntax))
                        Case SyntaxKind.AsNewClause
                            Dim asNew = DirectCast(asClause, AsNewClauseSyntax)
                            Select Case asNew.NewExpression.Kind
                                Case SyntaxKind.ObjectCreationExpression
                                    asClause = asNew.WithNewExpression(DirectCast(asNew.NewExpression, ObjectCreationExpressionSyntax).WithType(DirectCast(type, TypeSyntax)))
                                Case SyntaxKind.ArrayCreationExpression
                                    asClause = asNew.WithNewExpression(DirectCast(asNew.NewExpression, ArrayCreationExpressionSyntax).WithType(DirectCast(type, TypeSyntax)))
                            End Select
                    End Select
                Else
                    asClause = Nothing
                End If
            ElseIf type IsNot Nothing Then
                asClause = SyntaxFactory.SimpleAsClause(DirectCast(type, TypeSyntax))
            End If

            Return WithAsClause(declaration, asClause)
        End Function

        Private Function GetAsClause(declaration As SyntaxNode) As AsClauseSyntax
            Select Case declaration.Kind
                Case SyntaxKind.DelegateFunctionStatement
                    Return DirectCast(declaration, DelegateStatementSyntax).AsClause
                Case SyntaxKind.FunctionBlock
                    Return DirectCast(declaration, MethodBlockSyntax).SubOrFunctionStatement.AsClause
                Case SyntaxKind.FunctionStatement
                    Return DirectCast(declaration, MethodStatementSyntax).AsClause
                Case SyntaxKind.PropertyBlock
                    Return DirectCast(declaration, PropertyBlockSyntax).PropertyStatement.AsClause
                Case SyntaxKind.PropertyStatement
                    Return DirectCast(declaration, PropertyStatementSyntax).AsClause
                Case SyntaxKind.EventBlock
                    Return DirectCast(declaration, EventBlockSyntax).EventStatement.AsClause
                Case SyntaxKind.EventStatement
                    Return DirectCast(declaration, EventStatementSyntax).AsClause
                Case SyntaxKind.Parameter
                    Return DirectCast(declaration, ParameterSyntax).AsClause
                Case SyntaxKind.FieldDeclaration
                    Dim fd = DirectCast(declaration, FieldDeclarationSyntax)
                    If fd.Declarators.Count = 1 Then
                        Return fd.Declarators(0).AsClause
                    End If
                Case SyntaxKind.LocalDeclarationStatement
                    Dim ld = DirectCast(declaration, LocalDeclarationStatementSyntax)
                    If ld.Declarators.Count = 1 Then
                        Return ld.Declarators(0).AsClause
                    End If
                Case SyntaxKind.VariableDeclarator
                    Return DirectCast(declaration, VariableDeclaratorSyntax).AsClause
                Case SyntaxKind.ModifiedIdentifier
                    Dim vd = TryCast(declaration.Parent, VariableDeclaratorSyntax)
                    If vd IsNot Nothing Then
                        Return vd.AsClause
                    End If
            End Select
            Return Nothing
        End Function

        Private Function WithAsClause(declaration As SyntaxNode, asClause As AsClauseSyntax) As SyntaxNode
            Select Case declaration.Kind
                Case SyntaxKind.DelegateFunctionStatement
                    Return DirectCast(declaration, DelegateStatementSyntax).WithAsClause(DirectCast(asClause, SimpleAsClauseSyntax))
                Case SyntaxKind.FieldDeclaration
                    Dim fd = DirectCast(declaration, FieldDeclarationSyntax)
                    If fd.Declarators.Count = 1 Then
                        Return ReplaceWithTrivia(declaration, fd.Declarators(0), fd.Declarators(0).WithAsClause(asClause))
                    End If
                Case SyntaxKind.FunctionBlock
                    Return DirectCast(declaration, MethodBlockSyntax).WithSubOrFunctionStatement(DirectCast(declaration, MethodBlockSyntax).SubOrFunctionStatement.WithAsClause(DirectCast(asClause, SimpleAsClauseSyntax)))
                Case SyntaxKind.FunctionStatement
                    Return DirectCast(declaration, MethodStatementSyntax).WithAsClause(DirectCast(asClause, SimpleAsClauseSyntax))
                Case SyntaxKind.PropertyBlock
                    Return DirectCast(declaration, PropertyBlockSyntax).WithPropertyStatement(DirectCast(declaration, PropertyBlockSyntax).PropertyStatement.WithAsClause(asClause))
                Case SyntaxKind.PropertyStatement
                    Return DirectCast(declaration, PropertyStatementSyntax).WithAsClause(asClause)
                Case SyntaxKind.EventBlock
                    Return DirectCast(declaration, EventBlockSyntax).WithEventStatement(DirectCast(declaration, EventBlockSyntax).EventStatement.WithAsClause(DirectCast(asClause, SimpleAsClauseSyntax)))
                Case SyntaxKind.EventStatement
                    Return DirectCast(declaration, EventStatementSyntax).WithAsClause(DirectCast(asClause, SimpleAsClauseSyntax))
                Case SyntaxKind.Parameter
                    Return DirectCast(declaration, ParameterSyntax).WithAsClause(DirectCast(asClause, SimpleAsClauseSyntax))
                Case SyntaxKind.LocalDeclarationStatement
                    Dim ld = DirectCast(declaration, LocalDeclarationStatementSyntax)
                    If ld.Declarators.Count = 1 Then
                        Return ReplaceWithTrivia(declaration, ld.Declarators(0), ld.Declarators(0).WithAsClause(asClause))
                    End If
                Case SyntaxKind.VariableDeclarator
                    Return DirectCast(declaration, VariableDeclaratorSyntax).WithAsClause(asClause)
            End Select
            Return declaration
        End Function

        Private Function AsFunction(declaration As SyntaxNode) As SyntaxNode
            Return Isolate(declaration, AddressOf AsFunctionInternal)
        End Function

        Private Function AsFunctionInternal(declaration As SyntaxNode) As SyntaxNode
            Select Case declaration.Kind
                Case SyntaxKind.SubBlock
                    Dim sb = DirectCast(declaration, MethodBlockSyntax)
                    Return SyntaxFactory.MethodBlock(
                        SyntaxKind.FunctionBlock,
                        DirectCast(AsFunction(sb.BlockStatement), MethodStatementSyntax),
                        sb.Statements,
                        SyntaxFactory.EndBlockStatement(
                            SyntaxKind.EndFunctionStatement,
                            sb.EndBlockStatement.EndKeyword,
                            SyntaxFactory.Token(sb.EndBlockStatement.BlockKeyword.LeadingTrivia, SyntaxKind.FunctionKeyword, sb.EndBlockStatement.BlockKeyword.TrailingTrivia)
                            ))
                Case SyntaxKind.SubStatement
                    Dim ss = DirectCast(declaration, MethodStatementSyntax)
                    Return SyntaxFactory.MethodStatement(
                        SyntaxKind.FunctionStatement,
                        ss.AttributeLists,
                        ss.Modifiers,
                        SyntaxFactory.Token(ss.DeclarationKeyword.LeadingTrivia, SyntaxKind.FunctionKeyword, ss.DeclarationKeyword.TrailingTrivia),
                        ss.Identifier,
                        ss.TypeParameterList,
                        ss.ParameterList,
                        SyntaxFactory.SimpleAsClause(SyntaxFactory.IdentifierName("Object")),
                        ss.HandlesClause,
                        ss.ImplementsClause)
                Case SyntaxKind.DelegateSubStatement
                    Dim ds = DirectCast(declaration, DelegateStatementSyntax)
                    Return SyntaxFactory.DelegateStatement(
                        SyntaxKind.DelegateFunctionStatement,
                        ds.AttributeLists,
                        ds.Modifiers,
                        SyntaxFactory.Token(ds.DeclarationKeyword.LeadingTrivia, SyntaxKind.FunctionKeyword, ds.DeclarationKeyword.TrailingTrivia),
                        ds.Identifier,
                        ds.TypeParameterList,
                        ds.ParameterList,
                        SyntaxFactory.SimpleAsClause(SyntaxFactory.IdentifierName("Object")))
                Case SyntaxKind.MultiLineSubLambdaExpression
                    Dim ml = DirectCast(declaration, MultiLineLambdaExpressionSyntax)
                    Return SyntaxFactory.MultiLineLambdaExpression(
                        SyntaxKind.MultiLineFunctionLambdaExpression,
                        DirectCast(AsFunction(ml.SubOrFunctionHeader), LambdaHeaderSyntax),
                        ml.Statements,
                        SyntaxFactory.EndBlockStatement(
                            SyntaxKind.EndFunctionStatement,
                            ml.EndSubOrFunctionStatement.EndKeyword,
                            SyntaxFactory.Token(ml.EndSubOrFunctionStatement.BlockKeyword.LeadingTrivia, SyntaxKind.FunctionKeyword, ml.EndSubOrFunctionStatement.BlockKeyword.TrailingTrivia)
                            ))
                Case SyntaxKind.SingleLineSubLambdaExpression
                    Dim sl = DirectCast(declaration, SingleLineLambdaExpressionSyntax)
                    Return SyntaxFactory.SingleLineLambdaExpression(
                        SyntaxKind.SingleLineFunctionLambdaExpression,
                        DirectCast(AsFunction(sl.SubOrFunctionHeader), LambdaHeaderSyntax),
                        sl.Body)
                Case SyntaxKind.SubLambdaHeader
                    Dim lh = DirectCast(declaration, LambdaHeaderSyntax)
                    Return SyntaxFactory.LambdaHeader(
                        SyntaxKind.FunctionLambdaHeader,
                        lh.AttributeLists,
                        lh.Modifiers,
                        SyntaxFactory.Token(lh.DeclarationKeyword.LeadingTrivia, SyntaxKind.FunctionKeyword, lh.DeclarationKeyword.TrailingTrivia),
                        lh.ParameterList,
                        asClause:=Nothing)
                Case SyntaxKind.DeclareSubStatement
                    Dim ds = DirectCast(declaration, DeclareStatementSyntax)
                    Return SyntaxFactory.DeclareStatement(
                        SyntaxKind.DeclareFunctionStatement,
                        ds.AttributeLists,
                        ds.Modifiers,
                        ds.CharsetKeyword,
                        SyntaxFactory.Token(ds.DeclarationKeyword.LeadingTrivia, SyntaxKind.FunctionKeyword, ds.DeclarationKeyword.TrailingTrivia),
                        ds.Identifier,
                        ds.LibraryName,
                        ds.AliasName,
                        ds.ParameterList,
                        SyntaxFactory.SimpleAsClause(SyntaxFactory.IdentifierName("Object")))
                Case Else
                    Return declaration
            End Select
        End Function

        Private Function AsSub(declaration As SyntaxNode) As SyntaxNode
            Return Isolate(declaration, AddressOf AsSubInternal)
        End Function

        Private Function AsSubInternal(declaration As SyntaxNode) As SyntaxNode
            Select Case declaration.Kind
                Case SyntaxKind.FunctionBlock
                    Dim mb = DirectCast(declaration, MethodBlockSyntax)
                    Return SyntaxFactory.MethodBlock(
                        SyntaxKind.SubBlock,
                        DirectCast(AsSub(mb.BlockStatement), MethodStatementSyntax),
                        mb.Statements,
                        SyntaxFactory.EndBlockStatement(
                            SyntaxKind.EndSubStatement,
                            mb.EndBlockStatement.EndKeyword,
                            SyntaxFactory.Token(mb.EndBlockStatement.BlockKeyword.LeadingTrivia, SyntaxKind.SubKeyword, mb.EndBlockStatement.BlockKeyword.TrailingTrivia)
                            ))
                Case SyntaxKind.FunctionStatement
                    Dim ms = DirectCast(declaration, MethodStatementSyntax)
                    Return SyntaxFactory.MethodStatement(
                        SyntaxKind.SubStatement,
                        ms.AttributeLists,
                        ms.Modifiers,
                        SyntaxFactory.Token(ms.DeclarationKeyword.LeadingTrivia, SyntaxKind.SubKeyword, ms.DeclarationKeyword.TrailingTrivia),
                        ms.Identifier,
                        ms.TypeParameterList,
                        ms.ParameterList,
                        asClause:=Nothing,
                        handlesClause:=ms.HandlesClause,
                        implementsClause:=ms.ImplementsClause)
                Case SyntaxKind.DelegateFunctionStatement
                    Dim ds = DirectCast(declaration, DelegateStatementSyntax)
                    Return SyntaxFactory.DelegateStatement(
                        SyntaxKind.DelegateSubStatement,
                        ds.AttributeLists,
                        ds.Modifiers,
                        SyntaxFactory.Token(ds.DeclarationKeyword.LeadingTrivia, SyntaxKind.SubKeyword, ds.DeclarationKeyword.TrailingTrivia),
                        ds.Identifier,
                        ds.TypeParameterList,
                        ds.ParameterList,
                        asClause:=Nothing)
                Case SyntaxKind.MultiLineFunctionLambdaExpression
                    Dim ml = DirectCast(declaration, MultiLineLambdaExpressionSyntax)
                    Return SyntaxFactory.MultiLineLambdaExpression(
                        SyntaxKind.MultiLineSubLambdaExpression,
                        DirectCast(AsSub(ml.SubOrFunctionHeader), LambdaHeaderSyntax),
                        ml.Statements,
                        SyntaxFactory.EndBlockStatement(
                            SyntaxKind.EndSubStatement,
                            ml.EndSubOrFunctionStatement.EndKeyword,
                            SyntaxFactory.Token(ml.EndSubOrFunctionStatement.BlockKeyword.LeadingTrivia, SyntaxKind.SubKeyword, ml.EndSubOrFunctionStatement.BlockKeyword.TrailingTrivia)
                            ))
                Case SyntaxKind.SingleLineFunctionLambdaExpression
                    Dim sl = DirectCast(declaration, SingleLineLambdaExpressionSyntax)
                    Return SyntaxFactory.SingleLineLambdaExpression(
                        SyntaxKind.SingleLineSubLambdaExpression,
                        DirectCast(AsSub(sl.SubOrFunctionHeader), LambdaHeaderSyntax),
                        sl.Body)
                Case SyntaxKind.FunctionLambdaHeader
                    Dim lh = DirectCast(declaration, LambdaHeaderSyntax)
                    Return SyntaxFactory.LambdaHeader(
                        SyntaxKind.SubLambdaHeader,
                        lh.AttributeLists,
                        lh.Modifiers,
                        SyntaxFactory.Token(lh.DeclarationKeyword.LeadingTrivia, SyntaxKind.SubKeyword, lh.DeclarationKeyword.TrailingTrivia),
                        lh.ParameterList,
                        asClause:=Nothing)
                Case SyntaxKind.DeclareFunctionStatement
                    Dim ds = DirectCast(declaration, DeclareStatementSyntax)
                    Return SyntaxFactory.DeclareStatement(
                        SyntaxKind.DeclareSubStatement,
                        ds.AttributeLists,
                        ds.Modifiers,
                        ds.CharsetKeyword,
                        SyntaxFactory.Token(ds.DeclarationKeyword.LeadingTrivia, SyntaxKind.SubKeyword, ds.DeclarationKeyword.TrailingTrivia),
                        ds.Identifier,
                        ds.LibraryName,
                        ds.AliasName,
                        ds.ParameterList,
                        asClause:=Nothing)
                Case Else
                    Return declaration
            End Select
        End Function

        Public Overrides Function GetModifiers(declaration As SyntaxNode) As DeclarationModifiers
            Dim tokens = GetModifierTokens(declaration)
            Dim acc As Accessibility
            Dim mods As DeclarationModifiers
            Dim isDefault As Boolean
            GetAccessibilityAndModifiers(tokens, acc, mods, isDefault)
            Return mods
        End Function

        Public Overrides Function WithModifiers(declaration As SyntaxNode, modifiers As DeclarationModifiers) As SyntaxNode
            Return Isolate(declaration, Function(d) Me.WithModifiersInternal(d, modifiers))
        End Function

        Private Function WithModifiersInternal(declaration As SyntaxNode, modifiers As DeclarationModifiers) As SyntaxNode
            Dim tokens = GetModifierTokens(declaration)

            Dim acc As Accessibility
            Dim currentMods As DeclarationModifiers
            Dim isDefault As Boolean
            GetAccessibilityAndModifiers(tokens, acc, currentMods, isDefault)

            If (currentMods <> modifiers) Then
                Dim newTokens = GetModifierList(acc, modifiers And GetAllowedModifiers(declaration.Kind), GetDeclarationKind(declaration), isDefault)
                Return WithModifierTokens(declaration, Merge(tokens, newTokens))
            Else
                Return declaration
            End If
        End Function

        Private Function Merge(original As SyntaxTokenList, newList As SyntaxTokenList) As SyntaxTokenList
            '' return tokens from newList, but use original tokens if kind matches
            Return SyntaxFactory.TokenList(newList.Select(Function(token) If(original.Any(token.Kind), original.First(Function(tk) tk.IsKind(token.Kind)), token)))
        End Function

        Private Function GetModifierTokens(declaration As SyntaxNode) As SyntaxTokenList
            Select Case declaration.Kind
                Case SyntaxKind.ClassBlock
                    Return DirectCast(declaration, ClassBlockSyntax).BlockStatement.Modifiers
                Case SyntaxKind.ClassStatement
                    Return DirectCast(declaration, ClassStatementSyntax).Modifiers
                Case SyntaxKind.StructureBlock
                    Return DirectCast(declaration, StructureBlockSyntax).BlockStatement.Modifiers
                Case SyntaxKind.StructureStatement
                    Return DirectCast(declaration, StructureStatementSyntax).Modifiers
                Case SyntaxKind.InterfaceBlock
                    Return DirectCast(declaration, InterfaceBlockSyntax).BlockStatement.Modifiers
                Case SyntaxKind.InterfaceStatement
                    Return DirectCast(declaration, InterfaceStatementSyntax).Modifiers
                Case SyntaxKind.EnumBlock
                    Return DirectCast(declaration, EnumBlockSyntax).EnumStatement.Modifiers
                Case SyntaxKind.EnumStatement
                    Return DirectCast(declaration, EnumStatementSyntax).Modifiers
                Case SyntaxKind.DelegateFunctionStatement,
                     SyntaxKind.DelegateSubStatement
                    Return DirectCast(declaration, DelegateStatementSyntax).Modifiers
                Case SyntaxKind.FieldDeclaration
                    Return DirectCast(declaration, FieldDeclarationSyntax).Modifiers
                Case SyntaxKind.FunctionBlock,
                     SyntaxKind.SubBlock
                    Return DirectCast(declaration, MethodBlockSyntax).BlockStatement.Modifiers
                Case SyntaxKind.ConstructorBlock
                    Return DirectCast(declaration, ConstructorBlockSyntax).BlockStatement.Modifiers
                Case SyntaxKind.FunctionStatement,
                     SyntaxKind.SubStatement
                    Return DirectCast(declaration, MethodStatementSyntax).Modifiers
                Case SyntaxKind.SubNewStatement
                    Return DirectCast(declaration, SubNewStatementSyntax).Modifiers
                Case SyntaxKind.PropertyBlock
                    Return DirectCast(declaration, PropertyBlockSyntax).PropertyStatement.Modifiers
                Case SyntaxKind.PropertyStatement
                    Return DirectCast(declaration, PropertyStatementSyntax).Modifiers
                Case SyntaxKind.OperatorBlock
                    Return DirectCast(declaration, OperatorBlockSyntax).BlockStatement.Modifiers
                Case SyntaxKind.OperatorStatement
                    Return DirectCast(declaration, OperatorStatementSyntax).Modifiers
                Case SyntaxKind.EventBlock
                    Return DirectCast(declaration, EventBlockSyntax).EventStatement.Modifiers
                Case SyntaxKind.EventStatement
                    Return DirectCast(declaration, EventStatementSyntax).Modifiers
                Case SyntaxKind.ModifiedIdentifier
                    If IsChildOf(declaration, SyntaxKind.VariableDeclarator) Then
                        Return GetModifierTokens(declaration.Parent)
                    End If
                Case SyntaxKind.VariableDeclarator
                    If IsChildOfVariableDeclaration(declaration) Then
                        Return GetModifierTokens(declaration.Parent)
                    End If
                Case SyntaxKind.GetAccessorBlock,
                     SyntaxKind.SetAccessorBlock,
                     SyntaxKind.AddHandlerAccessorBlock,
                     SyntaxKind.RemoveHandlerAccessorBlock,
                    SyntaxKind.RaiseEventAccessorBlock
                    Return GetModifierTokens(DirectCast(declaration, AccessorBlockSyntax).AccessorStatement)
                Case SyntaxKind.GetAccessorStatement,
                     SyntaxKind.SetAccessorStatement,
                     SyntaxKind.AddHandlerAccessorStatement,
                     SyntaxKind.RemoveHandlerAccessorStatement,
                     SyntaxKind.RaiseEventAccessorStatement
                    Return DirectCast(declaration, AccessorStatementSyntax).Modifiers
                Case Else
                    Return Nothing
            End Select
        End Function

        Private Function WithModifierTokens(declaration As SyntaxNode, tokens As SyntaxTokenList) As SyntaxNode
            Select Case declaration.Kind
                Case SyntaxKind.ClassBlock
                    Return DirectCast(declaration, ClassBlockSyntax).WithClassStatement(DirectCast(declaration, ClassBlockSyntax).ClassStatement.WithModifiers(tokens))
                Case SyntaxKind.ClassStatement
                    Return DirectCast(declaration, ClassStatementSyntax).WithModifiers(tokens)
                Case SyntaxKind.StructureBlock
                    Return DirectCast(declaration, StructureBlockSyntax).WithStructureStatement(DirectCast(declaration, StructureBlockSyntax).StructureStatement.WithModifiers(tokens))
                Case SyntaxKind.StructureStatement
                    Return DirectCast(declaration, StructureStatementSyntax).WithModifiers(tokens)
                Case SyntaxKind.InterfaceBlock
                    Return DirectCast(declaration, InterfaceBlockSyntax).WithInterfaceStatement(DirectCast(declaration, InterfaceBlockSyntax).InterfaceStatement.WithModifiers(tokens))
                Case SyntaxKind.InterfaceStatement
                    Return DirectCast(declaration, InterfaceStatementSyntax).WithModifiers(tokens)
                Case SyntaxKind.EnumBlock
                    Return DirectCast(declaration, EnumBlockSyntax).WithEnumStatement(DirectCast(declaration, EnumBlockSyntax).EnumStatement.WithModifiers(tokens))
                Case SyntaxKind.EnumStatement
                    Return DirectCast(declaration, EnumStatementSyntax).WithModifiers(tokens)
                Case SyntaxKind.DelegateFunctionStatement,
                     SyntaxKind.DelegateSubStatement
                    Return DirectCast(declaration, DelegateStatementSyntax).WithModifiers(tokens)
                Case SyntaxKind.FieldDeclaration
                    Return DirectCast(declaration, FieldDeclarationSyntax).WithModifiers(tokens)
                Case SyntaxKind.FunctionBlock,
                     SyntaxKind.SubBlock
                    Return DirectCast(declaration, MethodBlockSyntax).WithSubOrFunctionStatement(DirectCast(declaration, MethodBlockSyntax).SubOrFunctionStatement.WithModifiers(tokens))
                Case SyntaxKind.ConstructorBlock
                    Return DirectCast(declaration, ConstructorBlockSyntax).WithSubNewStatement(DirectCast(declaration, ConstructorBlockSyntax).SubNewStatement.WithModifiers(tokens))
                Case SyntaxKind.FunctionStatement,
                     SyntaxKind.SubStatement
                    Return DirectCast(declaration, MethodStatementSyntax).WithModifiers(tokens)
                Case SyntaxKind.SubNewStatement
                    Return DirectCast(declaration, SubNewStatementSyntax).WithModifiers(tokens)
                Case SyntaxKind.PropertyBlock
                    Return DirectCast(declaration, PropertyBlockSyntax).WithPropertyStatement(DirectCast(declaration, PropertyBlockSyntax).PropertyStatement.WithModifiers(tokens))
                Case SyntaxKind.PropertyStatement
                    Return DirectCast(declaration, PropertyStatementSyntax).WithModifiers(tokens)
                Case SyntaxKind.OperatorBlock
                    Return DirectCast(declaration, OperatorBlockSyntax).WithOperatorStatement(DirectCast(declaration, OperatorBlockSyntax).OperatorStatement.WithModifiers(tokens))
                Case SyntaxKind.OperatorStatement
                    Return DirectCast(declaration, OperatorStatementSyntax).WithModifiers(tokens)
                Case SyntaxKind.EventBlock
                    Return DirectCast(declaration, EventBlockSyntax).WithEventStatement(DirectCast(declaration, EventBlockSyntax).EventStatement.WithModifiers(tokens))
                Case SyntaxKind.EventStatement
                    Return DirectCast(declaration, EventStatementSyntax).WithModifiers(tokens)
                Case SyntaxKind.GetAccessorBlock,
                     SyntaxKind.SetAccessorBlock,
                     SyntaxKind.AddHandlerAccessorBlock,
                     SyntaxKind.RemoveHandlerAccessorBlock,
                    SyntaxKind.RaiseEventAccessorBlock
                    Return DirectCast(declaration, AccessorBlockSyntax).WithAccessorStatement(
                        DirectCast(Me.WithModifierTokens(DirectCast(declaration, AccessorBlockSyntax).AccessorStatement, tokens), AccessorStatementSyntax))
                Case SyntaxKind.GetAccessorStatement,
                     SyntaxKind.SetAccessorStatement,
                     SyntaxKind.AddHandlerAccessorStatement,
                     SyntaxKind.RemoveHandlerAccessorStatement,
                     SyntaxKind.RaiseEventAccessorStatement
                    Return DirectCast(declaration, AccessorStatementSyntax).WithModifiers(tokens)
                Case Else
                    Return declaration
            End Select
        End Function

        Public Overrides Function GetAccessibility(declaration As SyntaxNode) As Accessibility
            Dim tokens = GetModifierTokens(declaration)
            Dim acc As Accessibility
            Dim mods As DeclarationModifiers
            Dim isDefault As Boolean
            GetAccessibilityAndModifiers(tokens, acc, mods, isDefault)
            Return acc
        End Function

        Public Overrides Function WithAccessibility(declaration As SyntaxNode, accessibility As Accessibility) As SyntaxNode
            Return Isolate(declaration, Function(d) Me.WithAccessibilityInternal(d, accessibility))
        End Function

        Private Function WithAccessibilityInternal(declaration As SyntaxNode, accessibility As Accessibility) As SyntaxNode
            If Not CanHaveAccessibility(declaration) Then
                Return declaration
            End If

            Dim tokens = GetModifierTokens(declaration)
            Dim currentAcc As Accessibility
            Dim mods As DeclarationModifiers
            Dim isDefault As Boolean
            GetAccessibilityAndModifiers(tokens, currentAcc, mods, isDefault)

            If currentAcc = accessibility Then
                Return declaration
            End If

            Dim newTokens = GetModifierList(accessibility, mods, GetDeclarationKind(declaration), isDefault)
            Return WithModifierTokens(declaration, Merge(tokens, newTokens))
        End Function

        Private Function CanHaveAccessibility(declaration As SyntaxNode) As Boolean
            Select Case declaration.Kind
                Case SyntaxKind.ClassBlock,
                    SyntaxKind.ClassStatement,
                    SyntaxKind.StructureBlock,
                    SyntaxKind.StructureStatement,
                    SyntaxKind.InterfaceBlock,
                    SyntaxKind.InterfaceStatement,
                    SyntaxKind.EnumBlock,
                    SyntaxKind.EnumStatement,
                    SyntaxKind.DelegateFunctionStatement,
                    SyntaxKind.DelegateSubStatement,
                    SyntaxKind.FieldDeclaration,
                    SyntaxKind.FunctionBlock,
                    SyntaxKind.SubBlock,
                    SyntaxKind.ConstructorBlock,
                    SyntaxKind.FunctionStatement,
                    SyntaxKind.SubStatement,
                    SyntaxKind.SubNewStatement,
                    SyntaxKind.PropertyBlock,
                    SyntaxKind.PropertyStatement,
                    SyntaxKind.OperatorBlock,
                    SyntaxKind.OperatorStatement,
                    SyntaxKind.EventBlock,
                    SyntaxKind.EventStatement,
                    SyntaxKind.GetAccessorBlock,
                    SyntaxKind.GetAccessorStatement,
                    SyntaxKind.SetAccessorBlock,
                    SyntaxKind.SetAccessorStatement,
                    SyntaxKind.AddHandlerAccessorBlock,
                    SyntaxKind.AddHandlerAccessorStatement,
                    SyntaxKind.RemoveHandlerAccessorBlock,
                    SyntaxKind.RemoveHandlerAccessorStatement,
                    SyntaxKind.RaiseEventAccessorBlock,
                    SyntaxKind.RaiseEventAccessorStatement
                    Return True
                Case Else
                    Return False
            End Select
        End Function

        Private Function GetModifierList(accessibility As Accessibility, modifiers As DeclarationModifiers, kind As DeclarationKind, Optional isDefault As Boolean = False) As SyntaxTokenList
            Dim _list = SyntaxFactory.TokenList()

            If isDefault Then
                _list = _list.Add(SyntaxFactory.Token(SyntaxKind.DefaultKeyword))
            End If

            Select Case (accessibility)
                Case Accessibility.Internal
                    _list = _list.Add(SyntaxFactory.Token(SyntaxKind.FriendKeyword))
                Case Accessibility.Public
                    _list = _list.Add(SyntaxFactory.Token(SyntaxKind.PublicKeyword))
                Case Accessibility.Private
                    _list = _list.Add(SyntaxFactory.Token(SyntaxKind.PrivateKeyword))
                Case Accessibility.Protected
                    _list = _list.Add(SyntaxFactory.Token(SyntaxKind.ProtectedKeyword))
                Case Accessibility.ProtectedOrInternal
                    _list = _list.Add(SyntaxFactory.Token(SyntaxKind.FriendKeyword)).Add(SyntaxFactory.Token(SyntaxKind.ProtectedKeyword))
                Case Accessibility.NotApplicable
                Case Else
                    Throw New NotSupportedException(String.Format("Accessibility '{0}' not supported.", accessibility))
            End Select

            If modifiers.IsAbstract Then
                If kind = DeclarationKind.Class Then
                    _list = _list.Add(SyntaxFactory.Token(SyntaxKind.MustInheritKeyword))
                Else
                    _list = _list.Add(SyntaxFactory.Token(SyntaxKind.MustOverrideKeyword))
                End If
            End If

            If modifiers.IsNew Then
                _list = _list.Add(SyntaxFactory.Token(SyntaxKind.ShadowsKeyword))
            End If

            If modifiers.IsSealed Then
                If kind = DeclarationKind.Class Then
                    _list = _list.Add(SyntaxFactory.Token(SyntaxKind.NotInheritableKeyword))
                Else
                    _list = _list.Add(SyntaxFactory.Token(SyntaxKind.NotOverridableKeyword))
                End If
            End If

            If modifiers.IsOverride Then
                _list = _list.Add(SyntaxFactory.Token(SyntaxKind.OverridesKeyword))
            End If

            If modifiers.IsVirtual Then
                _list = _list.Add(SyntaxFactory.Token(SyntaxKind.OverridableKeyword))
            End If

            If modifiers.IsStatic Then
                _list = _list.Add(SyntaxFactory.Token(SyntaxKind.SharedKeyword))
            End If

            If modifiers.IsAsync Then
                _list = _list.Add(SyntaxFactory.Token(SyntaxKind.AsyncKeyword))
            End If

            If modifiers.IsConst Then
                _list = _list.Add(SyntaxFactory.Token(SyntaxKind.ConstKeyword))
            End If

            If modifiers.IsReadOnly Then
                _list = _list.Add(SyntaxFactory.Token(SyntaxKind.ReadOnlyKeyword))
            End If

            If modifiers.IsWriteOnly Then
                _list = _list.Add(SyntaxFactory.Token(SyntaxKind.WriteOnlyKeyword))
            End If

            If modifiers.IsUnsafe Then
                Throw New NotSupportedException("Unsupported modifier")
                ''''_list = _list.Add(SyntaxFactory.Token(SyntaxKind.UnsafeKeyword))
            End If

            If modifiers.IsWithEvents Then
                _list = _list.Add(SyntaxFactory.Token(SyntaxKind.WithEventsKeyword))
            End If

            ' partial must be last
            If modifiers.IsPartial Then
                _list = _list.Add(SyntaxFactory.Token(SyntaxKind.PartialKeyword))
            End If

            If (kind = DeclarationKind.Field AndAlso _list.Count = 0) Then
                _list = _list.Add(SyntaxFactory.Token(SyntaxKind.DimKeyword))
            End If

            Return _list
        End Function

        Private Sub GetAccessibilityAndModifiers(modifierTokens As SyntaxTokenList, ByRef accessibility As Accessibility, ByRef modifiers As DeclarationModifiers, ByRef isDefault As Boolean)
            accessibility = Accessibility.NotApplicable
            modifiers = DeclarationModifiers.None
            isDefault = False

            For Each token In modifierTokens
                Select Case token.Kind
                    Case SyntaxKind.DefaultKeyword
                        isDefault = True
                    Case SyntaxKind.PublicKeyword
                        accessibility = Accessibility.Public
                    Case SyntaxKind.PrivateKeyword
                        accessibility = Accessibility.Private
                    Case SyntaxKind.FriendKeyword
                        If accessibility = Accessibility.Protected Then
                            accessibility = Accessibility.ProtectedOrFriend
                        Else
                            accessibility = Accessibility.Friend
                        End If
                    Case SyntaxKind.ProtectedKeyword
                        If accessibility = Accessibility.Friend Then
                            accessibility = Accessibility.ProtectedOrFriend
                        Else
                            accessibility = Accessibility.Protected
                        End If
                    Case SyntaxKind.MustInheritKeyword, SyntaxKind.MustOverrideKeyword
                        modifiers = modifiers Or DeclarationModifiers.Abstract
                    Case SyntaxKind.ShadowsKeyword
                        modifiers = modifiers Or DeclarationModifiers.[New]
                    Case SyntaxKind.OverridesKeyword
                        modifiers = modifiers Or DeclarationModifiers.Override
                    Case SyntaxKind.OverridableKeyword
                        modifiers = modifiers Or DeclarationModifiers.Virtual
                    Case SyntaxKind.SharedKeyword
                        modifiers = modifiers Or DeclarationModifiers.Static
                    Case SyntaxKind.AsyncKeyword
                        modifiers = modifiers Or DeclarationModifiers.Async
                    Case SyntaxKind.ConstKeyword
                        modifiers = modifiers Or DeclarationModifiers.Const
                    Case SyntaxKind.ReadOnlyKeyword
                        modifiers = modifiers Or DeclarationModifiers.ReadOnly
                    Case SyntaxKind.WriteOnlyKeyword
                        modifiers = modifiers Or DeclarationModifiers.WriteOnly
                    Case SyntaxKind.NotInheritableKeyword, SyntaxKind.NotOverridableKeyword
                        modifiers = modifiers Or DeclarationModifiers.Sealed
                    Case SyntaxKind.WithEventsKeyword
                        modifiers = modifiers Or DeclarationModifiers.WithEvents
                    Case SyntaxKind.PartialKeyword
                        modifiers = modifiers Or DeclarationModifiers.Partial
                End Select
            Next
        End Sub

        Private Function GetTypeParameters(typeParameterNames As IEnumerable(Of String)) As TypeParameterListSyntax
            If typeParameterNames Is Nothing Then
                Return Nothing
            End If

            Dim typeParameterList = SyntaxFactory.TypeParameterList(SyntaxFactory.SeparatedList(typeParameterNames.Select(Function(name) SyntaxFactory.TypeParameter(name))))

            If typeParameterList.Parameters.Count = 0 Then
                typeParameterList = Nothing
            End If

            Return typeParameterList
        End Function

        Public Overrides Function WithTypeParameters(declaration As SyntaxNode, typeParameterNames As IEnumerable(Of String)) As SyntaxNode
            Dim typeParameterList = GetTypeParameters(typeParameterNames)
            Return ReplaceTypeParameterList(declaration, Function(old) typeParameterList)
        End Function

        Private Function ReplaceTypeParameterList(declaration As SyntaxNode, replacer As Func(Of TypeParameterListSyntax, TypeParameterListSyntax)) As SyntaxNode
            Dim method = TryCast(declaration, MethodStatementSyntax)
            If method IsNot Nothing Then
                Return method.WithTypeParameterList(replacer(method.TypeParameterList))
            End If

            Dim methodBlock = TryCast(declaration, MethodBlockSyntax)
            If methodBlock IsNot Nothing Then
                Return methodBlock.WithSubOrFunctionStatement(methodBlock.SubOrFunctionStatement.WithTypeParameterList(replacer(methodBlock.SubOrFunctionStatement.TypeParameterList)))
            End If

            Dim classBlock = TryCast(declaration, ClassBlockSyntax)
            If classBlock IsNot Nothing Then
                Return classBlock.WithClassStatement(classBlock.ClassStatement.WithTypeParameterList(replacer(classBlock.ClassStatement.TypeParameterList)))
            End If

            Dim structureBlock = TryCast(declaration, StructureBlockSyntax)
            If structureBlock IsNot Nothing Then
                Return structureBlock.WithStructureStatement(structureBlock.StructureStatement.WithTypeParameterList(replacer(structureBlock.StructureStatement.TypeParameterList)))
            End If

            Dim interfaceBlock = TryCast(declaration, InterfaceBlockSyntax)
            If interfaceBlock IsNot Nothing Then
                Return interfaceBlock.WithInterfaceStatement(interfaceBlock.InterfaceStatement.WithTypeParameterList(replacer(interfaceBlock.InterfaceStatement.TypeParameterList)))
            End If

            Return declaration
        End Function

        Public Overrides Function WithTypeConstraint(declaration As SyntaxNode, typeParameterName As String, kinds As SpecialTypeConstraintKind, Optional types As IEnumerable(Of SyntaxNode) = Nothing) As SyntaxNode
            Dim constraints = SyntaxFactory.SeparatedList(Of ConstraintSyntax)

            If types IsNot Nothing Then
                constraints = constraints.AddRange(types.Select(Function(t) SyntaxFactory.TypeConstraint(DirectCast(t, TypeSyntax))))
            End If

            If (kinds And SpecialTypeConstraintKind.Constructor) <> 0 Then
                constraints = constraints.Add(SyntaxFactory.NewConstraint(SyntaxFactory.Token(SyntaxKind.NewKeyword)))
            End If

            Dim isReferenceType = (kinds And SpecialTypeConstraintKind.ReferenceType) <> 0
            Dim isValueType = (kinds And SpecialTypeConstraintKind.ValueType) <> 0

            If isReferenceType Then
                constraints = constraints.Insert(0, SyntaxFactory.ClassConstraint(SyntaxFactory.Token(SyntaxKind.ClassKeyword)))
            ElseIf isValueType Then
                constraints = constraints.Insert(0, SyntaxFactory.StructureConstraint(SyntaxFactory.Token(SyntaxKind.StructureKeyword)))
            End If

            Dim clause As TypeParameterConstraintClauseSyntax = Nothing

            If constraints.Count = 1 Then
                clause = SyntaxFactory.TypeParameterSingleConstraintClause(constraints(0))
            ElseIf constraints.Count > 1 Then
                clause = SyntaxFactory.TypeParameterMultipleConstraintClause(constraints)
            End If

            Return ReplaceTypeParameterList(declaration, Function(old) WithTypeParameterConstraints(old, typeParameterName, clause))
        End Function

        Private Function WithTypeParameterConstraints(typeParameterList As TypeParameterListSyntax, typeParameterName As String, clause As TypeParameterConstraintClauseSyntax) As TypeParameterListSyntax
            If typeParameterList IsNot Nothing Then
                Dim typeParameter = typeParameterList.Parameters.FirstOrDefault(Function(tp) tp.Identifier.ToString() = typeParameterName)
                If typeParameter IsNot Nothing Then
                    Return typeParameterList.WithParameters(typeParameterList.Parameters.Replace(typeParameter, typeParameter.WithTypeParameterConstraintClause(clause)))
                End If
            End If

            Return typeParameterList
        End Function

        Public Overrides Function GetParameters(declaration As SyntaxNode) As IReadOnlyList(Of SyntaxNode)
            Dim list = GetParameterList(declaration)
            If list IsNot Nothing Then
                Return list.Parameters
            Else
                Return SpecializedCollections.EmptyReadOnlyList(Of SyntaxNode)
            End If
        End Function

        Public Overrides Function InsertParameters(declaration As SyntaxNode, index As Integer, parameters As IEnumerable(Of SyntaxNode)) As SyntaxNode
            Dim currentList = GetParameterList(declaration)
            Dim newList = GetParameterList(parameters)
            If currentList IsNot Nothing Then
                Return WithParameterList(declaration, currentList.WithParameters(currentList.Parameters.InsertRange(index, newList.Parameters)))
            Else
                Return WithParameterList(declaration, newList)
            End If
        End Function

        Private Function GetParameterList(declaration As SyntaxNode) As ParameterListSyntax
            Select Case declaration.Kind
                Case SyntaxKind.SubBlock,
                    SyntaxKind.FunctionBlock
                    Return DirectCast(declaration, MethodBlockSyntax).BlockStatement.ParameterList
                Case SyntaxKind.ConstructorBlock
                    Return DirectCast(declaration, ConstructorBlockSyntax).BlockStatement.ParameterList
                Case SyntaxKind.OperatorBlock
                    Return DirectCast(declaration, OperatorBlockSyntax).BlockStatement.ParameterList
                Case SyntaxKind.SubStatement,
                    SyntaxKind.FunctionStatement
                    Return DirectCast(declaration, MethodStatementSyntax).ParameterList
                Case SyntaxKind.SubNewStatement
                    Return DirectCast(declaration, SubNewStatementSyntax).ParameterList
                Case SyntaxKind.OperatorStatement
                    Return DirectCast(declaration, OperatorStatementSyntax).ParameterList
                Case SyntaxKind.DeclareSubStatement,
                    SyntaxKind.DeclareFunctionStatement
                    Return DirectCast(declaration, DeclareStatementSyntax).ParameterList
                Case SyntaxKind.DelegateSubStatement,
                    SyntaxKind.DelegateFunctionStatement
                    Return DirectCast(declaration, DelegateStatementSyntax).ParameterList
                Case SyntaxKind.PropertyBlock
                    Return DirectCast(declaration, PropertyBlockSyntax).PropertyStatement.ParameterList
                Case SyntaxKind.PropertyStatement
                    Return DirectCast(declaration, PropertyStatementSyntax).ParameterList
                Case SyntaxKind.EventBlock
                    Return DirectCast(declaration, EventBlockSyntax).EventStatement.ParameterList
                Case SyntaxKind.EventStatement
                    Return DirectCast(declaration, EventStatementSyntax).ParameterList
                Case SyntaxKind.MultiLineFunctionLambdaExpression,
                     SyntaxKind.MultiLineSubLambdaExpression
                    Return DirectCast(declaration, MultiLineLambdaExpressionSyntax).SubOrFunctionHeader.ParameterList
                Case SyntaxKind.SingleLineFunctionLambdaExpression,
                     SyntaxKind.SingleLineSubLambdaExpression
                    Return DirectCast(declaration, SingleLineLambdaExpressionSyntax).SubOrFunctionHeader.ParameterList
                Case Else
                    Return Nothing
            End Select
        End Function

        Private Function WithParameterList(declaration As SyntaxNode, list As ParameterListSyntax) As SyntaxNode
            Select Case declaration.Kind
                Case SyntaxKind.DelegateFunctionStatement,
                    SyntaxKind.DelegateSubStatement
                    Return DirectCast(declaration, DelegateStatementSyntax).WithParameterList(list)
                Case SyntaxKind.SubBlock,
                    SyntaxKind.FunctionBlock
                    Return DirectCast(declaration, MethodBlockSyntax).WithBlockStatement(DirectCast(declaration, MethodBlockSyntax).BlockStatement.WithParameterList(list))
                Case SyntaxKind.ConstructorBlock
                    Return DirectCast(declaration, ConstructorBlockSyntax).WithBlockStatement(DirectCast(declaration, ConstructorBlockSyntax).BlockStatement.WithParameterList(list))
                Case SyntaxKind.OperatorBlock
                    Return DirectCast(declaration, OperatorBlockSyntax).WithBlockStatement(DirectCast(declaration, OperatorBlockSyntax).BlockStatement.WithParameterList(list))
                Case SyntaxKind.SubStatement,
                    SyntaxKind.FunctionStatement
                    Return DirectCast(declaration, MethodStatementSyntax).WithParameterList(list)
                Case SyntaxKind.SubNewStatement
                    Return DirectCast(declaration, SubNewStatementSyntax).WithParameterList(list)
                Case SyntaxKind.OperatorStatement
                    Return DirectCast(declaration, OperatorStatementSyntax).WithParameterList(list)
                Case SyntaxKind.DeclareSubStatement,
                    SyntaxKind.DeclareFunctionStatement
                    Return DirectCast(declaration, DeclareStatementSyntax).WithParameterList(list)
                Case SyntaxKind.DelegateSubStatement,
                    SyntaxKind.DelegateFunctionStatement
                    Return DirectCast(declaration, DelegateStatementSyntax).WithParameterList(list)
                Case SyntaxKind.PropertyBlock
                    If GetDeclarationKind(declaration) = DeclarationKind.Indexer Then
                        Return DirectCast(declaration, PropertyBlockSyntax).WithPropertyStatement(DirectCast(declaration, PropertyBlockSyntax).PropertyStatement.WithParameterList(list))
                    End If
                Case SyntaxKind.PropertyStatement
                    If GetDeclarationKind(declaration) = DeclarationKind.Indexer Then
                        Return DirectCast(declaration, PropertyStatementSyntax).WithParameterList(list)
                    End If
                Case SyntaxKind.EventBlock
                    Return DirectCast(declaration, EventBlockSyntax).WithEventStatement(DirectCast(declaration, EventBlockSyntax).EventStatement.WithParameterList(list))
                Case SyntaxKind.EventStatement
                    Return DirectCast(declaration, EventStatementSyntax).WithParameterList(list)
                Case SyntaxKind.MultiLineFunctionLambdaExpression,
                     SyntaxKind.MultiLineSubLambdaExpression
                    Return DirectCast(declaration, MultiLineLambdaExpressionSyntax).WithSubOrFunctionHeader(DirectCast(declaration, MultiLineLambdaExpressionSyntax).SubOrFunctionHeader.WithParameterList(list))
                Case SyntaxKind.SingleLineFunctionLambdaExpression,
                     SyntaxKind.SingleLineSubLambdaExpression
                    Return DirectCast(declaration, SingleLineLambdaExpressionSyntax).WithSubOrFunctionHeader(DirectCast(declaration, SingleLineLambdaExpressionSyntax).SubOrFunctionHeader.WithParameterList(list))
            End Select

            Return declaration
        End Function

        Public Overrides Function GetExpression(declaration As SyntaxNode) As SyntaxNode
            Select Case declaration.Kind
                Case SyntaxKind.SingleLineFunctionLambdaExpression,
                     SyntaxKind.SingleLineSubLambdaExpression
                    Return AsExpression(DirectCast(declaration, SingleLineLambdaExpressionSyntax).Body)
                Case Else
                    Dim ev = GetEqualsValue(declaration)
                    If ev IsNot Nothing Then
                        Return ev.Value
                    End If
            End Select
            Return Nothing
        End Function

        Private Function AsExpression(node As SyntaxNode) As ExpressionSyntax
            Dim es = TryCast(node, ExpressionStatementSyntax)
            If es IsNot Nothing Then
                Return es.Expression
            End If
            Return DirectCast(node, ExpressionSyntax)
        End Function

        Public Overrides Function WithExpression(declaration As SyntaxNode, expression As SyntaxNode) As SyntaxNode
            Return Isolate(declaration, Function(d) WithExpressionInternal(d, expression))
        End Function

        Private Function WithExpressionInternal(declaration As SyntaxNode, expression As SyntaxNode) As SyntaxNode
            Dim expr = DirectCast(expression, ExpressionSyntax)

            Select Case declaration.Kind
                Case SyntaxKind.SingleLineFunctionLambdaExpression
                    Dim sll = DirectCast(declaration, SingleLineLambdaExpressionSyntax)
                    If expression IsNot Nothing Then
                        Return sll.WithBody(expr)
                    Else
                        Return SyntaxFactory.MultiLineLambdaExpression(SyntaxKind.MultiLineFunctionLambdaExpression, sll.SubOrFunctionHeader, SyntaxFactory.EndFunctionStatement())
                    End If
                Case SyntaxKind.MultiLineFunctionLambdaExpression
                    Dim mll = DirectCast(declaration, MultiLineLambdaExpressionSyntax)
                    If expression IsNot Nothing Then
                        Return SyntaxFactory.SingleLineLambdaExpression(SyntaxKind.SingleLineFunctionLambdaExpression, mll.SubOrFunctionHeader, expr)
                    End If
                Case SyntaxKind.SingleLineSubLambdaExpression
                    Dim sll = DirectCast(declaration, SingleLineLambdaExpressionSyntax)
                    If expression IsNot Nothing Then
                        Return sll.WithBody(AsStatement(expr))
                    Else
                        Return SyntaxFactory.MultiLineLambdaExpression(SyntaxKind.MultiLineSubLambdaExpression, sll.SubOrFunctionHeader, SyntaxFactory.EndSubStatement())
                    End If
                Case SyntaxKind.MultiLineSubLambdaExpression
                    Dim mll = DirectCast(declaration, MultiLineLambdaExpressionSyntax)
                    If expression IsNot Nothing Then
                        Return SyntaxFactory.SingleLineLambdaExpression(SyntaxKind.SingleLineSubLambdaExpression, mll.SubOrFunctionHeader, AsStatement(expr))
                    End If
                Case Else
                    Dim currentEV = GetEqualsValue(declaration)
                    If currentEV IsNot Nothing Then
                        Return WithEqualsValue(declaration, currentEV.WithValue(expr))
                    Else
                        Return WithEqualsValue(declaration, SyntaxFactory.EqualsValue(expr))
                    End If
            End Select
            Return declaration
        End Function

        Private Function GetEqualsValue(declaration As SyntaxNode) As EqualsValueSyntax
            Select Case declaration.Kind
                Case SyntaxKind.Parameter
                    Return DirectCast(declaration, ParameterSyntax).Default
                Case SyntaxKind.LocalDeclarationStatement
                    Dim ld = DirectCast(declaration, LocalDeclarationStatementSyntax)
                    If ld.Declarators.Count = 1 Then
                        Return ld.Declarators(0).Initializer
                    End If
                Case SyntaxKind.FieldDeclaration
                    Dim fd = DirectCast(declaration, FieldDeclarationSyntax)
                    If fd.Declarators.Count = 1 Then
                        Return fd.Declarators(0).Initializer
                    End If
                Case SyntaxKind.VariableDeclarator
                    Return DirectCast(declaration, VariableDeclaratorSyntax).Initializer
            End Select
            Return Nothing
        End Function

        Private Function WithEqualsValue(declaration As SyntaxNode, ev As EqualsValueSyntax) As SyntaxNode
            Select Case declaration.Kind
                Case SyntaxKind.Parameter
                    Return DirectCast(declaration, ParameterSyntax).WithDefault(ev)
                Case SyntaxKind.LocalDeclarationStatement
                    Dim ld = DirectCast(declaration, LocalDeclarationStatementSyntax)
                    If ld.Declarators.Count = 1 Then
                        Return ReplaceWithTrivia(declaration, ld.Declarators(0), ld.Declarators(0).WithInitializer(ev))
                    End If
                Case SyntaxKind.FieldDeclaration
                    Dim fd = DirectCast(declaration, FieldDeclarationSyntax)
                    If fd.Declarators.Count = 1 Then
                        Return ReplaceWithTrivia(declaration, fd.Declarators(0), fd.Declarators(0).WithInitializer(ev))
                    End If
            End Select
            Return declaration
        End Function

        Public Overrides Function GetNamespaceImports(declaration As SyntaxNode) As IReadOnlyList(Of SyntaxNode)
            Return Me.Flatten(Me.GetUnflattenedNamespaceImports(declaration))
        End Function

        Private Function GetUnflattenedNamespaceImports(declaration As SyntaxNode) As IReadOnlyList(Of SyntaxNode)
            Select Case declaration.Kind
                Case SyntaxKind.CompilationUnit
                    Return DirectCast(declaration, CompilationUnitSyntax).Imports
                Case Else
                    Return SpecializedCollections.EmptyReadOnlyList(Of SyntaxNode)
            End Select
        End Function

        Public Overrides Function InsertNamespaceImports(declaration As SyntaxNode, index As Integer, [imports] As IEnumerable(Of SyntaxNode)) As SyntaxNode
            Return Isolate(declaration, Function(d) InsertNamespaceImportsInternal(d, index, [imports]))
        End Function

        Private Function InsertNamespaceImportsInternal(declaration As SyntaxNode, index As Integer, [imports] As IEnumerable(Of SyntaxNode)) As SyntaxNode
            Dim newImports = AsImports([imports])
            Dim existingImports = Me.GetNamespaceImports(declaration)

            If index >= 0 AndAlso index < existingImports.Count Then
                Return Me.InsertNodesBefore(declaration, existingImports(index), newImports)
            ElseIf existingImports.Count > 0 Then
                Return Me.InsertNodesAfter(declaration, existingImports(existingImports.Count - 1), newImports)
            Else
                Select Case declaration.Kind
                    Case SyntaxKind.CompilationUnit
                        Dim cu = DirectCast(declaration, CompilationUnitSyntax)
                        Return cu.WithImports(cu.Imports.AddRange(newImports))
                    Case Else
                        Return declaration
                End Select
            End If
        End Function

        Public Overrides Function GetMembers(declaration As SyntaxNode) As IReadOnlyList(Of SyntaxNode)
            Return Flatten(GetUnflattenedMembers(declaration))
        End Function

        Private Function GetUnflattenedMembers(declaration As SyntaxNode) As IReadOnlyList(Of SyntaxNode)
            Select Case declaration.Kind
                Case SyntaxKind.CompilationUnit
                    Return DirectCast(declaration, CompilationUnitSyntax).Members
                Case SyntaxKind.NamespaceBlock
                    Return DirectCast(declaration, NamespaceBlockSyntax).Members
                Case SyntaxKind.ClassBlock
                    Return DirectCast(declaration, ClassBlockSyntax).Members
                Case SyntaxKind.StructureBlock
                    Return DirectCast(declaration, StructureBlockSyntax).Members
                Case SyntaxKind.InterfaceBlock
                    Return DirectCast(declaration, InterfaceBlockSyntax).Members
                Case SyntaxKind.EnumBlock
                    Return DirectCast(declaration, EnumBlockSyntax).Members
                Case Else
                    Return SpecializedCollections.EmptyReadOnlyList(Of SyntaxNode)()
            End Select
        End Function

        Private Function AsMembersOf(declaration As SyntaxNode, members As IEnumerable(Of SyntaxNode)) As IEnumerable(Of StatementSyntax)
            Select Case declaration.Kind
                Case SyntaxKind.CompilationUnit
                    Return AsNamespaceMembers(members)
                Case SyntaxKind.NamespaceBlock
                    Return AsNamespaceMembers(members)
                Case SyntaxKind.ClassBlock
                    Return AsClassMembers(members)
                Case SyntaxKind.StructureBlock
                    Return AsClassMembers(members)
                Case SyntaxKind.InterfaceBlock
                    Return AsInterfaceMembers(members)
                Case SyntaxKind.EnumBlock
                    Return AsEnumMembers(members)
                Case Else
                    Return SpecializedCollections.EmptyEnumerable(Of StatementSyntax)
            End Select

        End Function

        Public Overrides Function InsertMembers(declaration As SyntaxNode, index As Integer, members As IEnumerable(Of SyntaxNode)) As SyntaxNode
            Return Isolate(declaration, Function(d) InsertMembersInternal(d, index, members))
        End Function

        Private Function InsertMembersInternal(declaration As SyntaxNode, index As Integer, members As IEnumerable(Of SyntaxNode)) As SyntaxNode
            Dim newMembers = Me.AsMembersOf(declaration, members)
            Dim existingMembers = Me.GetMembers(declaration)

            If index >= 0 AndAlso index < existingMembers.Count Then
                Return Me.InsertNodesBefore(declaration, existingMembers(index), members)
            ElseIf existingMembers.Count > 0 Then
                Return Me.InsertNodesAfter(declaration, existingMembers(existingMembers.Count - 1), members)
            End If

            Select Case declaration.Kind
                Case SyntaxKind.CompilationUnit
                    Dim cu = DirectCast(declaration, CompilationUnitSyntax)
                    Return cu.WithMembers(cu.Members.AddRange(newMembers))
                Case SyntaxKind.NamespaceBlock
                    Dim ns = DirectCast(declaration, NamespaceBlockSyntax)
                    Return ns.WithMembers(ns.Members.AddRange(newMembers))
                Case SyntaxKind.ClassBlock
                    Dim cb = DirectCast(declaration, ClassBlockSyntax)
                    Return cb.WithMembers(cb.Members.AddRange(newMembers))
                Case SyntaxKind.StructureBlock
                    Dim sb = DirectCast(declaration, StructureBlockSyntax)
                    Return sb.WithMembers(sb.Members.AddRange(newMembers))
                Case SyntaxKind.InterfaceBlock
                    Dim ib = DirectCast(declaration, InterfaceBlockSyntax)
                    Return ib.WithMembers(ib.Members.AddRange(newMembers))
                Case SyntaxKind.EnumBlock
                    Dim eb = DirectCast(declaration, EnumBlockSyntax)
                    Return eb.WithMembers(eb.Members.AddRange(newMembers))
                Case Else
                    Return declaration
            End Select
        End Function

        Public Overrides Function GetStatements(declaration As SyntaxNode) As IReadOnlyList(Of SyntaxNode)
            Select Case declaration.Kind
                Case SyntaxKind.FunctionBlock,
                     SyntaxKind.SubBlock,
                     SyntaxKind.ConstructorBlock,
                     SyntaxKind.OperatorBlock
                    Return DirectCast(declaration, MethodBlockBaseSyntax).Statements
                Case SyntaxKind.MultiLineFunctionLambdaExpression,
                     SyntaxKind.MultiLineSubLambdaExpression
                    Return DirectCast(declaration, MultiLineLambdaExpressionSyntax).Statements
                Case SyntaxKind.GetAccessorBlock,
                     SyntaxKind.SetAccessorBlock,
                     SyntaxKind.AddHandlerAccessorBlock,
                     SyntaxKind.RemoveHandlerAccessorBlock,
                     SyntaxKind.RaiseEventAccessorBlock
                    Return DirectCast(declaration, AccessorBlockSyntax).Statements
                Case Else
                    Return SpecializedCollections.EmptyReadOnlyList(Of SyntaxNode)
            End Select
        End Function

        Public Overrides Function WithStatements(declaration As SyntaxNode, statements As IEnumerable(Of SyntaxNode)) As SyntaxNode
            Return Isolate(declaration, Function(d) WithStatementsInternal(d, statements))
        End Function

        Private Function WithStatementsInternal(declaration As SyntaxNode, statements As IEnumerable(Of SyntaxNode)) As SyntaxNode
            Dim list = GetStatementList(statements)
            Select Case declaration.Kind
                Case SyntaxKind.FunctionBlock,
                     SyntaxKind.SubBlock
                    Return DirectCast(declaration, MethodBlockSyntax).WithStatements(list)
                Case SyntaxKind.ConstructorBlock
                    Return DirectCast(declaration, ConstructorBlockSyntax).WithStatements(list)
                Case SyntaxKind.OperatorBlock
                    Return DirectCast(declaration, OperatorBlockSyntax).WithStatements(list)
                Case SyntaxKind.MultiLineFunctionLambdaExpression,
                     SyntaxKind.MultiLineSubLambdaExpression
                    Return DirectCast(declaration, MultiLineLambdaExpressionSyntax).WithStatements(list)
                Case SyntaxKind.SingleLineFunctionLambdaExpression
                    Dim sll = DirectCast(declaration, SingleLineLambdaExpressionSyntax)
                    Return SyntaxFactory.MultiLineLambdaExpression(SyntaxKind.MultiLineFunctionLambdaExpression, sll.SubOrFunctionHeader, list, SyntaxFactory.EndFunctionStatement())
                Case SyntaxKind.SingleLineSubLambdaExpression
                    Dim sll = DirectCast(declaration, SingleLineLambdaExpressionSyntax)
                    Return SyntaxFactory.MultiLineLambdaExpression(SyntaxKind.MultiLineSubLambdaExpression, sll.SubOrFunctionHeader, list, SyntaxFactory.EndSubStatement())
                Case SyntaxKind.GetAccessorBlock,
                     SyntaxKind.SetAccessorBlock,
                     SyntaxKind.AddHandlerAccessorBlock,
                     SyntaxKind.RemoveHandlerAccessorBlock,
                     SyntaxKind.RaiseEventAccessorBlock
                    Return DirectCast(declaration, AccessorBlockSyntax).WithStatements(list)
                Case Else
                    Return declaration
            End Select
        End Function

        Public Overrides Function GetAccessors(declaration As SyntaxNode) As IReadOnlyList(Of SyntaxNode)
            Select Case declaration.Kind
                Case SyntaxKind.PropertyBlock
                    Return DirectCast(declaration, PropertyBlockSyntax).Accessors
                Case SyntaxKind.EventBlock
                    Return DirectCast(declaration, EventBlockSyntax).Accessors
                Case Else
                    Return SpecializedCollections.EmptyReadOnlyList(Of SyntaxNode)()
            End Select
        End Function

        Public Overrides Function InsertAccessors(declaration As SyntaxNode, index As Integer, accessors As IEnumerable(Of SyntaxNode)) As SyntaxNode

            Dim currentList = GetAccessorList(declaration)
            Dim newList = AsAccessorList(accessors, declaration.Kind)

            If Not currentList.IsEmpty Then
                Return WithAccessorList(declaration, currentList.InsertRange(index, newList))
            Else
                Return WithAccessorList(declaration, newList)
            End If
        End Function

        Private Function GetAccessorList(declaration As SyntaxNode) As SyntaxList(Of AccessorBlockSyntax)
            Select Case declaration.Kind
                Case SyntaxKind.PropertyBlock
                    Return DirectCast(declaration, PropertyBlockSyntax).Accessors
                Case SyntaxKind.EventBlock
                    Return DirectCast(declaration, EventBlockSyntax).Accessors
                Case Else
                    Return Nothing
            End Select
        End Function

        Private Function WithAccessorList(declaration As SyntaxNode, accessorList As SyntaxList(Of AccessorBlockSyntax)) As SyntaxNode
            Select Case declaration.Kind
                Case SyntaxKind.PropertyBlock
                    Return DirectCast(declaration, PropertyBlockSyntax).WithAccessors(accessorList)
                Case SyntaxKind.EventBlock
                    Return DirectCast(declaration, EventBlockSyntax).WithAccessors(accessorList)
                Case Else
                    Return declaration
            End Select
        End Function

        Private Function AsAccessorList(nodes As IEnumerable(Of SyntaxNode), parentKind As SyntaxKind) As SyntaxList(Of AccessorBlockSyntax)
            Return SyntaxFactory.List(nodes.Select(Function(n) AsAccessor(n, parentKind)).Where(Function(n) n IsNot Nothing))
        End Function

        Private Function AsAccessor(node As SyntaxNode, parentKind As SyntaxKind) As AccessorBlockSyntax
            Select Case parentKind
                Case SyntaxKind.PropertyBlock
                    Select Case node.Kind
                        Case SyntaxKind.GetAccessorBlock,
                             SyntaxKind.SetAccessorBlock
                            Return DirectCast(node, AccessorBlockSyntax)
                    End Select
                Case SyntaxKind.EventBlock
                    Select Case node.Kind
                        Case SyntaxKind.AddHandlerAccessorBlock,
                             SyntaxKind.RemoveHandlerAccessorBlock,
                             SyntaxKind.RaiseEventAccessorBlock
                            Return DirectCast(node, AccessorBlockSyntax)
                    End Select
            End Select

            Return Nothing
        End Function

        Private Function CanHaveAccessors(kind As SyntaxKind) As Boolean
            Select Case kind
                Case SyntaxKind.PropertyBlock,
                     SyntaxKind.EventBlock
                    Return True
                Case Else
                    Return False
            End Select
        End Function

        Public Overrides Function GetGetAccessorStatements(declaration As SyntaxNode) As IReadOnlyList(Of SyntaxNode)
            Return GetAccessorStatements(declaration, SyntaxKind.GetAccessorBlock)
        End Function

        Public Overrides Function WithGetAccessorStatements(declaration As SyntaxNode, statements As IEnumerable(Of SyntaxNode)) As SyntaxNode
            Return WithAccessorStatements(declaration, statements, SyntaxKind.GetAccessorBlock)
        End Function

        Public Overrides Function GetSetAccessorStatements(declaration As SyntaxNode) As IReadOnlyList(Of SyntaxNode)
            Return GetAccessorStatements(declaration, SyntaxKind.SetAccessorBlock)
        End Function

        Public Overrides Function WithSetAccessorStatements(declaration As SyntaxNode, statements As IEnumerable(Of SyntaxNode)) As SyntaxNode
            Return WithAccessorStatements(declaration, statements, SyntaxKind.SetAccessorBlock)
        End Function

        Private Function GetAccessorStatements(declaration As SyntaxNode, kind As SyntaxKind) As IReadOnlyList(Of SyntaxNode)
            Dim accessor = Me.GetAccessorBlock(declaration, kind)
            If accessor IsNot Nothing Then
                Return Me.GetStatements(accessor)
            Else
                Return SpecializedCollections.EmptyReadOnlyList(Of SyntaxNode)()
            End If
        End Function

        Private Function WithAccessorStatements(declaration As SyntaxNode, statements As IEnumerable(Of SyntaxNode), kind As SyntaxKind) As SyntaxNode
            Dim accessor = Me.GetAccessorBlock(declaration, kind)
            If accessor IsNot Nothing Then
                accessor = DirectCast(Me.WithStatements(accessor, statements), AccessorBlockSyntax)
                Return Me.WithAccessorBlock(declaration, kind, accessor)
            ElseIf Me.CanHaveAccessors(declaration.Kind) Then
                accessor = Me.AccessorBlock(kind, statements, Me.ClearTrivia(Me.GetType(declaration)))
                Return Me.WithAccessorBlock(declaration, kind, accessor)
            Else
                Return declaration
            End If
        End Function

        Private Function GetAccessorBlock(declaration As SyntaxNode, kind As SyntaxKind) As AccessorBlockSyntax
            Select Case declaration.Kind
                Case SyntaxKind.PropertyBlock
                    Return DirectCast(declaration, PropertyBlockSyntax).Accessors.FirstOrDefault(Function(a) a.IsKind(kind))
                Case SyntaxKind.EventBlock
                    Return DirectCast(declaration, EventBlockSyntax).Accessors.FirstOrDefault(Function(a) a.IsKind(kind))
                Case Else
                    Return Nothing
            End Select
        End Function

        Private Function WithAccessorBlock(declaration As SyntaxNode, kind As SyntaxKind, accessor As AccessorBlockSyntax) As SyntaxNode
            Dim currentAccessor = Me.GetAccessorBlock(declaration, kind)
            If currentAccessor IsNot Nothing Then
                Return Me.ReplaceNode(declaration, currentAccessor, accessor)
            ElseIf accessor IsNot Nothing Then

                Select Case declaration.Kind
                    Case SyntaxKind.PropertyBlock
                        Dim pb = DirectCast(declaration, PropertyBlockSyntax)
                        Return pb.WithAccessors(pb.Accessors.Add(accessor))
                    Case SyntaxKind.EventBlock
                        Dim eb = DirectCast(declaration, EventBlockSyntax)
                        Return eb.WithAccessors(eb.Accessors.Add(accessor))
                End Select
            End If
            Return declaration
        End Function

        Public Overrides Function EventDeclaration(name As String, type As SyntaxNode, Optional accessibility As Accessibility = Accessibility.NotApplicable, Optional modifiers As DeclarationModifiers = Nothing) As SyntaxNode
            Return SyntaxFactory.EventStatement(
                attributeLists:=Nothing,
                modifiers:=GetModifierList(accessibility, modifiers And GetAllowedModifiers(SyntaxKind.EventStatement), DeclarationKind.Event),
                customKeyword:=Nothing,
                eventKeyword:=SyntaxFactory.Token(SyntaxKind.EventKeyword),
                identifier:=name.ToIdentifierToken(),
                parameterList:=Nothing,
                asClause:=SyntaxFactory.SimpleAsClause(DirectCast(type, TypeSyntax)),
                implementsClause:=Nothing)
        End Function

        Public Overrides Function CustomEventDeclaration(
            name As String,
            type As SyntaxNode,
            Optional accessibility As Accessibility = Accessibility.NotApplicable,
            Optional modifiers As DeclarationModifiers = Nothing,
            Optional parameters As IEnumerable(Of SyntaxNode) = Nothing,
            Optional addAccessorStatements As IEnumerable(Of SyntaxNode) = Nothing,
            Optional removeAccessorStatements As IEnumerable(Of SyntaxNode) = Nothing) As SyntaxNode

            Dim accessors = New List(Of AccessorBlockSyntax)()
            Dim raiseAccessorStatements As IEnumerable(Of SyntaxNode) = Nothing

            If modifiers.IsAbstract Then
                addAccessorStatements = Nothing
                removeAccessorStatements = Nothing
                raiseAccessorStatements = Nothing
            Else
                If addAccessorStatements Is Nothing Then
                    addAccessorStatements = SpecializedCollections.EmptyEnumerable(Of SyntaxNode)()
                End If
                If removeAccessorStatements Is Nothing Then
                    removeAccessorStatements = SpecializedCollections.EmptyEnumerable(Of SyntaxNode)()
                End If
                If raiseAccessorStatements Is Nothing Then
                    raiseAccessorStatements = SpecializedCollections.EmptyEnumerable(Of SyntaxNode)()
                End If
            End If

            accessors.Add(CreateAddHandlerAccessorBlock(type, addAccessorStatements))
            accessors.Add(CreateRemoveHandlerAccessorBlock(type, removeAccessorStatements))
            accessors.Add(CreateRaiseEventAccessorBlock(parameters, raiseAccessorStatements))

            Dim evStatement = SyntaxFactory.EventStatement(
                attributeLists:=Nothing,
                modifiers:=GetModifierList(accessibility, modifiers And GetAllowedModifiers(SyntaxKind.EventStatement), DeclarationKind.Event),
                customKeyword:=SyntaxFactory.Token(SyntaxKind.CustomKeyword),
                eventKeyword:=SyntaxFactory.Token(SyntaxKind.EventKeyword),
                identifier:=name.ToIdentifierToken(),
                parameterList:=Nothing,
                asClause:=SyntaxFactory.SimpleAsClause(DirectCast(type, TypeSyntax)),
                implementsClause:=Nothing)

            Return SyntaxFactory.EventBlock(
                eventStatement:=evStatement,
                accessors:=SyntaxFactory.List(accessors),
                endEventStatement:=SyntaxFactory.EndEventStatement())
        End Function

        Public Overrides Function GetAttributeArguments(attributeDeclaration As SyntaxNode) As IReadOnlyList(Of SyntaxNode)
            Dim list = GetArgumentList(attributeDeclaration)
            If list IsNot Nothing Then
                Return list.Arguments
            Else
                Return SpecializedCollections.EmptyReadOnlyList(Of SyntaxNode)()
            End If
        End Function

        Public Overrides Function InsertAttributeArguments(attributeDeclaration As SyntaxNode, index As Integer, attributeArguments As IEnumerable(Of SyntaxNode)) As SyntaxNode
            Return Isolate(attributeDeclaration, Function(d) InsertAttributeArgumentsInternal(d, index, attributeArguments))
        End Function

        Private Function InsertAttributeArgumentsInternal(attributeDeclaration As SyntaxNode, index As Integer, attributeArguments As IEnumerable(Of SyntaxNode)) As SyntaxNode
            Dim list = GetArgumentList(attributeDeclaration)
            Dim newArguments = AsArgumentList(attributeArguments)

            If list Is Nothing Then
                list = newArguments
            Else
                list = list.WithArguments(list.Arguments.InsertRange(index, newArguments.Arguments))
            End If

            Return WithArgumentList(attributeDeclaration, list)
        End Function

        Private Function GetArgumentList(declaration As SyntaxNode) As ArgumentListSyntax
            Select Case declaration.Kind
                Case SyntaxKind.AttributeList
                    Dim al = DirectCast(declaration, AttributeListSyntax)
                    If al.Attributes.Count = 1 Then
                        Return al.Attributes(0).ArgumentList
                    End If
                Case SyntaxKind.Attribute
                    Return DirectCast(declaration, AttributeSyntax).ArgumentList
            End Select
            Return Nothing
        End Function

        Private Function WithArgumentList(declaration As SyntaxNode, argumentList As ArgumentListSyntax) As SyntaxNode
            Select Case declaration.Kind
                Case SyntaxKind.AttributeList
                    Dim al = DirectCast(declaration, AttributeListSyntax)
                    If al.Attributes.Count = 1 Then
                        Return ReplaceWithTrivia(declaration, al.Attributes(0), al.Attributes(0).WithArgumentList(argumentList))
                    End If
                Case SyntaxKind.Attribute
                    Return DirectCast(declaration, AttributeSyntax).WithArgumentList(argumentList)
            End Select
            Return declaration
        End Function

        Public Overrides Function GetBaseAndInterfaceTypes(declaration As SyntaxNode) As IReadOnlyList(Of SyntaxNode)
            Return Me.GetInherits(declaration).SelectMany(Function(ih) ih.Types).Concat(Me.GetImplements(declaration).SelectMany(Function(imp) imp.Types)).ToImmutableReadOnlyListOrEmpty()
        End Function

        Public Overrides Function AddBaseType(declaration As SyntaxNode, baseType As SyntaxNode) As SyntaxNode
            If declaration.IsKind(SyntaxKind.ClassBlock) Then
                Dim existingBaseType = Me.GetInherits(declaration).SelectMany(Function(inh) inh.Types).FirstOrDefault()
                If existingBaseType IsNot Nothing Then
                    Return declaration.ReplaceNode(existingBaseType, baseType.WithTriviaFrom(existingBaseType))
                Else
                    Return Me.WithInherits(declaration, SyntaxFactory.SingletonList(SyntaxFactory.InheritsStatement(DirectCast(baseType, TypeSyntax))))
                End If
            Else
                Return declaration
            End If
        End Function

        Public Overrides Function AddInterfaceType(declaration As SyntaxNode, interfaceType As SyntaxNode) As SyntaxNode
            If declaration.IsKind(SyntaxKind.InterfaceBlock) Then
                Dim inh = Me.GetInherits(declaration)
                Dim last = inh.SelectMany(Function(s) s.Types).LastOrDefault()
                If inh.Count = 1 AndAlso last IsNot Nothing Then
                    Dim inh0 = inh(0)
                    Dim newInh0 = PreserveTrivia(inh0.TrackNodes(last), Function(_inh0) InsertNodesAfter(_inh0, _inh0.GetCurrentNode(last), {interfaceType}))
                    Return ReplaceNode(declaration, inh0, newInh0)
                Else
                    Return Me.WithInherits(declaration, inh.Add(SyntaxFactory.InheritsStatement(DirectCast(interfaceType, TypeSyntax))))
                End If
            Else
                Dim imp = Me.GetImplements(declaration)
                Dim last = imp.SelectMany(Function(s) s.Types).LastOrDefault()
                If imp.Count = 1 AndAlso last IsNot Nothing Then
                    Dim imp0 = imp(0)
                    Dim newImp0 = PreserveTrivia(imp0.TrackNodes(last), Function(_imp0) InsertNodesAfter(_imp0, _imp0.GetCurrentNode(last), {interfaceType}))
                    Return ReplaceNode(declaration, imp0, newImp0)
                Else
                    Return Me.WithImplements(declaration, imp.Add(SyntaxFactory.ImplementsStatement(DirectCast(interfaceType, TypeSyntax))))
                End If
            End If
        End Function

        Private Function GetInherits(declaration As SyntaxNode) As SyntaxList(Of InheritsStatementSyntax)
            Select Case declaration.Kind
                Case SyntaxKind.ClassBlock
                    Return DirectCast(declaration, ClassBlockSyntax).Inherits
                Case SyntaxKind.InterfaceBlock
                    Return DirectCast(declaration, InterfaceBlockSyntax).Inherits
                Case Else
                    Return Nothing
            End Select
        End Function

        Private Function WithInherits(declaration As SyntaxNode, list As SyntaxList(Of InheritsStatementSyntax)) As SyntaxNode
            Select Case declaration.Kind
                Case SyntaxKind.ClassBlock
                    Return DirectCast(declaration, ClassBlockSyntax).WithInherits(list)
                Case SyntaxKind.InterfaceBlock
                    Return DirectCast(declaration, InterfaceBlockSyntax).WithInherits(list)
                Case Else
                    Return declaration
            End Select
        End Function

        Private Function GetImplements(declaration As SyntaxNode) As SyntaxList(Of ImplementsStatementSyntax)
            Select Case declaration.Kind
                Case SyntaxKind.ClassBlock
                    Return DirectCast(declaration, ClassBlockSyntax).Implements
                Case SyntaxKind.StructureBlock
                    Return DirectCast(declaration, StructureBlockSyntax).Implements
                Case Else
                    Return Nothing
            End Select
        End Function

        Private Function WithImplements(declaration As SyntaxNode, list As SyntaxList(Of ImplementsStatementSyntax)) As SyntaxNode
            Select Case declaration.Kind
                Case SyntaxKind.ClassBlock
                    Return DirectCast(declaration, ClassBlockSyntax).WithImplements(list)
                Case SyntaxKind.StructureBlock
                    Return DirectCast(declaration, StructureBlockSyntax).WithImplements(list)
                Case Else
                    Return declaration
            End Select
        End Function

#End Region

#Region "Remove, Replace, Insert"
        Public Overrides Function ReplaceNode(root As SyntaxNode, declaration As SyntaxNode, newDeclaration As SyntaxNode) As SyntaxNode
            If newDeclaration Is Nothing Then
                Return Me.RemoveNode(root, declaration)
            End If

            If root.Span.Contains(declaration.Span) Then
                Dim newFullDecl = Me.AsIsolatedDeclaration(newDeclaration)
                Dim fullDecl = Me.GetFullDeclaration(declaration)

                ' special handling for replacing at location of a sub-declaration
                If fullDecl IsNot declaration Then

                    ' try to replace inline if possible
                    If fullDecl.IsKind(newFullDecl.Kind) AndAlso GetDeclarationCount(newFullDecl) = 1 Then
                        Dim newSubDecl = Me.GetSubDeclarations(newFullDecl)(0)
                        If AreInlineReplaceableSubDeclarations(declaration, newSubDecl) Then
                            Return MyBase.ReplaceNode(root, declaration, newSubDecl)
                        End If
                    End If

                    ' otherwise replace by splitting full-declaration into two parts and inserting newDeclaration between them
                    Dim index = MyBase.IndexOf(Me.GetSubDeclarations(fullDecl), declaration)
                    Return Me.ReplaceSubDeclaration(root, fullDecl, index, newFullDecl)
                End If

                ' attempt normal replace
                Return MyBase.ReplaceNode(root, declaration, newFullDecl)
            Else
                Return MyBase.ReplaceNode(root, declaration, newDeclaration)
            End If
        End Function

        ' return true if one sub-declaration can be replaced in-line with another sub-declaration
        Private Function AreInlineReplaceableSubDeclarations(decl1 As SyntaxNode, decl2 As SyntaxNode) As Boolean
            Dim kind = decl1.Kind
            If Not decl2.IsKind(kind) Then
                Return False
            End If

            Select Case kind
                Case SyntaxKind.ModifiedIdentifier,
                     SyntaxKind.Attribute,
                     SyntaxKind.SimpleImportsClause,
                     SyntaxKind.XmlNamespaceImportsClause
                    Return AreSimilarExceptForSubDeclarations(decl1.Parent, decl2.Parent)
            End Select

            Return False
        End Function

        Private Function AreSimilarExceptForSubDeclarations(decl1 As SyntaxNode, decl2 As SyntaxNode) As Boolean
            If decl1 Is Nothing OrElse decl2 Is Nothing Then
                Return False
            End If

            Dim kind = decl1.Kind
            If Not decl2.IsKind(kind) Then
                Return False
            End If

            Select Case kind
                Case SyntaxKind.FieldDeclaration
                    Dim fd1 = DirectCast(decl1, FieldDeclarationSyntax)
                    Dim fd2 = DirectCast(decl2, FieldDeclarationSyntax)
                    Return SyntaxFactory.AreEquivalent(fd1.AttributeLists, fd2.AttributeLists) AndAlso SyntaxFactory.AreEquivalent(fd1.Modifiers, fd2.Modifiers)

                Case SyntaxKind.LocalDeclarationStatement
                    Dim ld1 = DirectCast(decl1, LocalDeclarationStatementSyntax)
                    Dim ld2 = DirectCast(decl2, LocalDeclarationStatementSyntax)
                    Return SyntaxFactory.AreEquivalent(ld1.Modifiers, ld2.Modifiers)

                Case SyntaxKind.VariableDeclarator
                    Dim vd1 = DirectCast(decl1, VariableDeclaratorSyntax)
                    Dim vd2 = DirectCast(decl2, VariableDeclaratorSyntax)
                    Return SyntaxFactory.AreEquivalent(vd1.AsClause, vd2.AsClause) AndAlso SyntaxFactory.AreEquivalent(vd2.Initializer, vd1.Initializer) AndAlso AreSimilarExceptForSubDeclarations(decl1.Parent, decl2.Parent)

                Case SyntaxKind.AttributeList,
                    SyntaxKind.ImportsStatement
                    Return True
            End Select

            Return False
        End Function

        Public Overrides Function InsertNodesBefore(root As SyntaxNode, declaration As SyntaxNode, newDeclarations As IEnumerable(Of SyntaxNode)) As SyntaxNode
            If root.Span.Contains(declaration.Span) Then
                Return Isolate(root.TrackNodes(declaration), Function(r) InsertDeclarationsBeforeInternal(r, r.GetCurrentNode(declaration), newDeclarations))
            Else
                Return MyBase.InsertNodesBefore(root, declaration, newDeclarations)
            End If
        End Function

        Private Function InsertDeclarationsBeforeInternal(root As SyntaxNode, declaration As SyntaxNode, newDeclarations As IEnumerable(Of SyntaxNode)) As SyntaxNode
            Dim fullDecl = Me.GetFullDeclaration(declaration)
            If fullDecl Is declaration OrElse GetDeclarationCount(fullDecl) = 1 Then
                Return MyBase.InsertNodesBefore(root, declaration, newDeclarations)
            End If

            Dim subDecls = Me.GetSubDeclarations(fullDecl)
            Dim count = subDecls.Count
            Dim index = MyBase.IndexOf(subDecls, declaration)

            ' insert New declaration between full declaration split into two
            If index > 0 Then
                Return ReplaceRange(root, fullDecl, SplitAndInsert(fullDecl, subDecls, index, newDeclarations))
            End If

            Return MyBase.InsertNodesBefore(root, fullDecl, newDeclarations)
        End Function

        Public Overrides Function InsertNodesAfter(root As SyntaxNode, declaration As SyntaxNode, newDeclarations As IEnumerable(Of SyntaxNode)) As SyntaxNode
            If root.Span.Contains(declaration.Span) Then
                Return Isolate(root.TrackNodes(declaration), Function(r) InsertNodesAfterInternal(r, r.GetCurrentNode(declaration), newDeclarations))
            Else
                Return MyBase.InsertNodesAfter(root, declaration, newDeclarations)
            End If
        End Function

        Private Function InsertNodesAfterInternal(root As SyntaxNode, declaration As SyntaxNode, newDeclarations As IEnumerable(Of SyntaxNode)) As SyntaxNode
            Dim fullDecl = Me.GetFullDeclaration(declaration)
            If fullDecl Is declaration OrElse GetDeclarationCount(fullDecl) = 1 Then
                Return MyBase.InsertNodesAfter(root, declaration, newDeclarations)
            End If

            Dim subDecls = Me.GetSubDeclarations(fullDecl)
            Dim count = subDecls.Count
            Dim index = MyBase.IndexOf(subDecls, declaration)

            ' insert New declaration between full declaration split into two
            If index >= 0 AndAlso index < count - 1 Then
                Return ReplaceRange(root, fullDecl, SplitAndInsert(fullDecl, subDecls, index + 1, newDeclarations))
            End If

            Return MyBase.InsertNodesAfter(root, fullDecl, newDeclarations)
        End Function

        Private Function SplitAndInsert(multiPartDeclaration As SyntaxNode, subDeclarations As IReadOnlyList(Of SyntaxNode), index As Integer, newDeclarations As IEnumerable(Of SyntaxNode)) As IEnumerable(Of SyntaxNode)
            Dim count = subDeclarations.Count
            Dim newNodes = New List(Of SyntaxNode)()
            newNodes.Add(Me.WithSubDeclarationsRemoved(multiPartDeclaration, index, count - index).WithTrailingTrivia(SyntaxFactory.ElasticSpace))
            newNodes.AddRange(newDeclarations)
            newNodes.Add(Me.WithSubDeclarationsRemoved(multiPartDeclaration, 0, index).WithLeadingTrivia(SyntaxFactory.ElasticSpace))
            Return newNodes
        End Function

        ' replaces sub-declaration by splitting multi-part declaration first
        Private Function ReplaceSubDeclaration(root As SyntaxNode, declaration As SyntaxNode, index As Integer, newDeclaration As SyntaxNode) As SyntaxNode
            Dim newNodes = New List(Of SyntaxNode)()
            Dim count = GetDeclarationCount(declaration)

            If index >= 0 AndAlso index < count Then
                If (index > 0) Then
                    ' make a single declaration with only the sub-declarations before the sub-declaration being replaced
                    newNodes.Add(Me.WithSubDeclarationsRemoved(declaration, index, count - index).WithTrailingTrivia(SyntaxFactory.ElasticSpace))
                End If

                newNodes.Add(newDeclaration)

                If (index < count - 1) Then
                    ' make a single declaration with only the sub-declarations after the sub-declaration being replaced
                    newNodes.Add(Me.WithSubDeclarationsRemoved(declaration, 0, index + 1).WithLeadingTrivia(SyntaxFactory.ElasticSpace))
                End If

                ' replace declaration with multiple declarations
                Return ReplaceRange(root, declaration, newNodes)
            Else
                Return root
            End If
        End Function


        Private Function WithSubDeclarationsRemoved(declaration As SyntaxNode, index As Integer, count As Integer) As SyntaxNode
            Return Me.RemoveNodes(declaration, Me.GetSubDeclarations(declaration).Skip(index).Take(count))
        End Function

        Private Function GetSubDeclarations(declaration As SyntaxNode) As IReadOnlyList(Of SyntaxNode)
            Select Case declaration.Kind
                Case SyntaxKind.FieldDeclaration
                    Return DirectCast(declaration, FieldDeclarationSyntax).Declarators.SelectMany(Function(d) d.Names).ToImmutableReadOnlyListOrEmpty()
                Case SyntaxKind.LocalDeclarationStatement
                    Return DirectCast(declaration, LocalDeclarationStatementSyntax).Declarators.SelectMany(Function(d) d.Names).ToImmutableReadOnlyListOrEmpty()
                Case SyntaxKind.AttributeList
                    Return DirectCast(declaration, AttributeListSyntax).Attributes
                Case SyntaxKind.ImportsStatement
                    Return DirectCast(declaration, ImportsStatementSyntax).ImportsClauses
                Case Else
                    Return SpecializedCollections.EmptyReadOnlyList(Of SyntaxNode)
            End Select
        End Function

        Private Function Flatten(members As IReadOnlyList(Of SyntaxNode)) As IReadOnlyList(Of SyntaxNode)
            If members.Count = 0 OrElse Not members.Any(Function(m) GetDeclarationCount(m) > 1) Then
                Return members
            End If

            Dim list = New List(Of SyntaxNode)
            Flatten(members, list)
            Return list.ToImmutableReadOnlyListOrEmpty()
        End Function

        Private Sub Flatten(members As IReadOnlyList(Of SyntaxNode), list As List(Of SyntaxNode))
            For Each m In members
                If GetDeclarationCount(m) > 1 Then
                    Select Case m.Kind
                        Case SyntaxKind.FieldDeclaration
                            Flatten(DirectCast(m, FieldDeclarationSyntax).Declarators, list)
                        Case SyntaxKind.LocalDeclarationStatement
                            Flatten(DirectCast(m, LocalDeclarationStatementSyntax).Declarators, list)
                        Case SyntaxKind.VariableDeclarator
                            Flatten(DirectCast(m, VariableDeclaratorSyntax).Names, list)
                        Case SyntaxKind.AttributesStatement
                            Flatten(DirectCast(m, AttributesStatementSyntax).AttributeLists, list)
                        Case SyntaxKind.AttributeList
                            Flatten(DirectCast(m, AttributeListSyntax).Attributes, list)
                        Case SyntaxKind.ImportsStatement
                            Flatten(DirectCast(m, ImportsStatementSyntax).ImportsClauses, list)
                        Case Else
                            list.Add(m)
                    End Select
                Else
                    list.Add(m)
                End If
            Next
        End Sub

        Public Overrides Function RemoveNode(root As SyntaxNode, declaration As SyntaxNode) As SyntaxNode
            Return RemoveNode(root, declaration, DefaultRemoveOptions)
        End Function

        Public Overrides Function RemoveNode(root As SyntaxNode, declaration As SyntaxNode, options As SyntaxRemoveOptions) As SyntaxNode
            If root.Span.Contains(declaration.Span) Then
                Return Isolate(root.TrackNodes(declaration), Function(r) Me.RemoveNodeInternal(r, r.GetCurrentNode(declaration), options))
            Else
                Return MyBase.RemoveNode(root, declaration, options)
            End If
        End Function

        Private Function RemoveNodeInternal(root As SyntaxNode, node As SyntaxNode, options As SyntaxRemoveOptions) As SyntaxNode

            ' special case handling for nodes that remove their parents too
            Select Case node.Kind
                Case SyntaxKind.ModifiedIdentifier
                    Dim vd = TryCast(node.Parent, VariableDeclaratorSyntax)
                    If vd IsNot Nothing AndAlso vd.Names.Count = 1 Then
                        ' remove entire variable declarator if only name
                        Return RemoveNodeInternal(root, vd, options)
                    End If
                Case SyntaxKind.VariableDeclarator
                    If IsChildOfVariableDeclaration(node) AndAlso GetDeclarationCount(node.Parent) = 1 Then
                        ' remove entire parent declaration if this is the only declarator
                        Return RemoveNodeInternal(root, node.Parent, options)
                    End If
                Case SyntaxKind.AttributeList
                    Dim attrList = DirectCast(node, AttributeListSyntax)
                    Dim attrStmt = TryCast(attrList.Parent, AttributesStatementSyntax)
                    If attrStmt IsNot Nothing AndAlso attrStmt.AttributeLists.Count = 1 Then
                        ' remove entire attribute statement if this is the only attribute list
                        Return RemoveNodeInternal(root, attrStmt, options)
                    End If
                Case SyntaxKind.Attribute
                    Dim attrList = TryCast(node.Parent, AttributeListSyntax)
                    If attrList IsNot Nothing AndAlso attrList.Attributes.Count = 1 Then
                        ' remove entire attribute list if this is the only attribute
                        Return RemoveNodeInternal(root, attrList, options)
                    End If
                Case SyntaxKind.SimpleArgument
                    If IsChildOf(node, SyntaxKind.ArgumentList) AndAlso IsChildOf(node.Parent, SyntaxKind.Attribute) Then
                        Dim argList = DirectCast(node.Parent, ArgumentListSyntax)
                        If argList.Arguments.Count = 1 Then
                            ' remove attribute's arg list if this is the only argument
                            Return RemoveNodeInternal(root, argList, options)
                        End If
                    End If
                Case SyntaxKind.SimpleImportsClause,
                     SyntaxKind.XmlNamespaceImportsClause
                    Dim imps = DirectCast(node.Parent, ImportsStatementSyntax)
                    If imps.ImportsClauses.Count = 1 Then
                        ' remove entire imports statement if this is the only clause
                        Return RemoveNodeInternal(root, node.Parent, options)
                    End If
                Case Else
                    Dim parent = node.Parent
                    If parent IsNot Nothing Then
                        Select Case parent.Kind
                            Case SyntaxKind.ImplementsStatement
                                Dim imp = DirectCast(parent, ImplementsStatementSyntax)
                                If imp.Types.Count = 1 Then
                                    Return RemoveNodeInternal(root, parent, options)
                                End If
                            Case SyntaxKind.InheritsStatement
                                Dim inh = DirectCast(parent, InheritsStatementSyntax)
                                If inh.Types.Count = 1 Then
                                    Return RemoveNodeInternal(root, parent, options)
                                End If
                        End Select
                    End If
            End Select

            ' do it the normal way
            Return root.RemoveNode(node, options)
        End Function
#End Region

    End Class
End Namespace
