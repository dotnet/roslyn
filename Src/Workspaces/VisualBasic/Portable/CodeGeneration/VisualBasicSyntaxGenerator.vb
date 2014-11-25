' Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Linq
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.CodeGeneration
Imports Microsoft.CodeAnalysis.Simplification
Imports Microsoft.CodeAnalysis.VisualBasic
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Microsoft.CodeAnalysis.Host
Imports Microsoft.CodeAnalysis.Host.Mef
Imports System.Composition

Namespace Microsoft.CodeAnalysis.VisualBasic.CodeGeneration
    <ExportLanguageService(GetType(SyntaxGenerator), LanguageNames.VisualBasic), [Shared]>
    Friend Class VisualBasicSyntaxGenerator
        Inherits SyntaxGenerator

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
            Return SyntaxFactory.DirectCastExpression(DirectCast(expression, ExpressionSyntax), DirectCast(type, TypeSyntax))
        End Function

        Public Overrides Function ConvertExpression(type As SyntaxNode, expression As SyntaxNode) As SyntaxNode
            Return SyntaxFactory.CTypeExpression(DirectCast(expression, ExpressionSyntax), DirectCast(type, TypeSyntax))
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
            Return SyntaxFactory.CaseBlock(
                SyntaxFactory.CaseStatement(SyntaxFactory.ElseCaseClause()),
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

        Private Function AsReadOnlyList(Of T)(sequence As IEnumerable(Of T)) As IReadOnlyList(Of T)
            Dim list = TryCast(sequence, IReadOnlyList(Of T))

            If list Is Nothing Then
                list = sequence.ToImmutableReadOnlyListOrEmpty()
            End If

            Return list
        End Function

        Private Shared fieldModifiers As DeclarationModifiers = DeclarationModifiers.Const Or DeclarationModifiers.[New] Or DeclarationModifiers.ReadOnly Or DeclarationModifiers.Static
        Private Shared methodModifiers As DeclarationModifiers = DeclarationModifiers.Abstract Or DeclarationModifiers.Async Or DeclarationModifiers.[New] Or DeclarationModifiers.Override Or DeclarationModifiers.Partial Or DeclarationModifiers.Sealed Or DeclarationModifiers.Static Or DeclarationModifiers.Virtual
        Private Shared constructorModifers As DeclarationModifiers = DeclarationModifiers.Static
        Private Shared propertyModifiers As DeclarationModifiers = DeclarationModifiers.Abstract Or DeclarationModifiers.[New] Or DeclarationModifiers.Override Or DeclarationModifiers.ReadOnly Or DeclarationModifiers.Sealed Or DeclarationModifiers.Static Or DeclarationModifiers.Virtual
        Private Shared indexerModifiers As DeclarationModifiers = DeclarationModifiers.Abstract Or DeclarationModifiers.[New] Or DeclarationModifiers.Override Or DeclarationModifiers.ReadOnly Or DeclarationModifiers.Sealed Or DeclarationModifiers.Static Or DeclarationModifiers.Virtual
        Private Shared classModifiers As DeclarationModifiers = DeclarationModifiers.Abstract Or DeclarationModifiers.[New] Or DeclarationModifiers.Partial Or DeclarationModifiers.Sealed Or DeclarationModifiers.Static
        Private Shared structModifiers As DeclarationModifiers = DeclarationModifiers.[New] Or DeclarationModifiers.Partial
        Private Shared interfaceModifiers As DeclarationModifiers = DeclarationModifiers.[New] Or DeclarationModifiers.Partial

        Private Function GetAllowedModifiers(kind As SyntaxKind) As DeclarationModifiers
            Select Case kind
                Case SyntaxKind.ClassBlock, SyntaxKind.ClassStatement
                    Return classModifiers

                Case SyntaxKind.EnumBlock, SyntaxKind.EnumStatement
                    Return DeclarationModifiers.[New]

                Case SyntaxKind.DelegateFunctionStatement, SyntaxKind.DelegateSubStatement
                    Return DeclarationModifiers.[New]

                Case SyntaxKind.InterfaceBlock, SyntaxKind.InterfaceStatement
                    Return interfaceModifiers

                Case SyntaxKind.StructureBlock, SyntaxKind.StructureStatement
                    Return structModifiers

                Case SyntaxKind.FunctionBlock,
                     SyntaxKind.FunctionStatement,
                     SyntaxKind.SubBlock,
                     SyntaxKind.SubStatement,
                     SyntaxKind.OperatorBlock,
                     SyntaxKind.OperatorStatement
                    Return methodModifiers

                Case SyntaxKind.ConstructorBlock,
                     SyntaxKind.SubNewStatement
                    Return constructorModifers

                Case SyntaxKind.FieldDeclaration
                    Return fieldModifiers

                Case SyntaxKind.PropertyBlock,
                     SyntaxKind.PropertyStatement
                    Return propertyModifiers

                Case SyntaxKind.EventBlock,
                     SyntaxKind.EventStatement
                    Return propertyModifiers

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
                modifiers:=GetModifierList(accessibility, modifiers And fieldModifiers, isField:=True),
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
                modifiers:=GetModifierList(accessibility, modifiers And methodModifiers),
                keyword:=If(returnType Is Nothing, SyntaxFactory.Token(SyntaxKind.SubKeyword), SyntaxFactory.Token(SyntaxKind.FunctionKeyword)),
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
                    begin:=statement,
                    statements:=GetStatementList(statements),
                    [end]:=If(returnType Is Nothing, SyntaxFactory.EndSubStatement(), SyntaxFactory.EndFunctionStatement()))
            End If
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
                modifiers:=GetModifierList(accessibility, modifiers And propertyModifiers),
                identifier:=identifier.ToIdentifierToken(),
                parameterList:=Nothing,
                asClause:=asClause,
                initializer:=Nothing,
                implementsClause:=Nothing)

            If modifiers.IsAbstract Then
                Return statement
            Else
                Dim accessors = New List(Of AccessorBlockSyntax)

                accessors.Add(CreateGetAccessorBlock(getAccessorStatements))

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
                modifiers:=GetModifierList(accessibility, modifiers And indexerModifiers, isDefault:=True),
                identifier:=SyntaxFactory.Identifier("Item"),
                parameterList:=SyntaxFactory.ParameterList(SyntaxFactory.SeparatedList(parameters.Cast(Of ParameterSyntax))),
                asClause:=asClause,
                initializer:=Nothing,
                implementsClause:=Nothing)

            If modifiers.IsAbstract Then
                Return statement
            Else
                Dim accessors = New List(Of AccessorBlockSyntax)

                accessors.Add(CreateGetAccessorBlock(getAccessorStatements))

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
                    keyword:=SyntaxFactory.Token(SyntaxKind.SetKeyword),
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
                    keyword:=SyntaxFactory.Token(SyntaxKind.AddHandlerKeyword),
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
                    keyword:=SyntaxFactory.Token(SyntaxKind.RemoveHandlerKeyword),
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
                    keyword:=SyntaxFactory.Token(SyntaxKind.RaiseEventKeyword),
                    parameterList:=parameterList),
                GetStatementList(statements),
                SyntaxFactory.EndRaiseEventStatement())
        End Function

        Public Overrides Function AsPublicInterfaceImplementation(declaration As SyntaxNode, typeName As SyntaxNode) As SyntaxNode
            Dim type = DirectCast(typeName, NameSyntax)

            declaration = AsImplementation(declaration, Accessibility.Public, allowDefault:=True)

            Dim method = TryCast(declaration, MethodBlockSyntax)
            If method IsNot Nothing Then
                Return method.WithBegin(
                    method.Begin.WithImplementsClause(
                        SyntaxFactory.ImplementsClause(SyntaxFactory.QualifiedName(type, SyntaxFactory.IdentifierName(method.Begin.Identifier)))))
            End If

            Dim prop = TryCast(declaration, PropertyBlockSyntax)
            If prop IsNot Nothing Then
                Return prop.WithPropertyStatement(
                    prop.PropertyStatement.WithImplementsClause(
                        SyntaxFactory.ImplementsClause(SyntaxFactory.QualifiedName(type, SyntaxFactory.IdentifierName(prop.PropertyStatement.Identifier)))))
            End If

            Return declaration
        End Function

        Public Overrides Function AsPrivateInterfaceImplementation(declaration As SyntaxNode, typeName As SyntaxNode) As SyntaxNode
            Dim type = DirectCast(typeName, NameSyntax)

            ' convert declaration statements to blocks
            declaration = AsImplementation(declaration, Accessibility.Private, allowDefault:=False)

            Dim method = TryCast(declaration, MethodBlockSyntax)
            If method IsNot Nothing Then
                Dim interfaceMemberName = SyntaxFactory.IdentifierName(method.Begin.Identifier)

                ' original method's name is used for interace member's name
                Dim memberName = SyntaxFactory.IdentifierName(method.Begin.Identifier)

                ' change actual method name to hide it
                method = method.WithBegin(
                    method.Begin.WithIdentifier(SyntaxFactory.Identifier(GetNameAsIdentifier(typeName) & "_" & GetNameAsIdentifier(memberName))))

                ' add implements clause
                Return method.WithBegin(
                    method.Begin.WithImplementsClause(
                        SyntaxFactory.ImplementsClause(SyntaxFactory.QualifiedName(type, interfaceMemberName))))
            End If

            Dim prop = TryCast(declaration, PropertyBlockSyntax)
            If prop IsNot Nothing Then
                Dim interfaceMemberName = SyntaxFactory.IdentifierName(prop.PropertyStatement.Identifier)

                ' original property's name is used as interface member's name
                Dim memberName = SyntaxFactory.IdentifierName(prop.PropertyStatement.Identifier)

                ' change actual property name to hide it
                prop = prop.WithPropertyStatement(
                    prop.PropertyStatement.WithIdentifier((GetNameAsIdentifier(typeName) & "_" & GetNameAsIdentifier(memberName)).ToIdentifierToken()))

                Return prop.WithPropertyStatement(
                    prop.PropertyStatement.WithImplementsClause(
                        SyntaxFactory.ImplementsClause(SyntaxFactory.QualifiedName(type, interfaceMemberName))))
            End If

            Return declaration
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

        Private Function AsImplementation(declaration As SyntaxNode, requiredAccess As Accessibility, allowDefault As Boolean) As SyntaxNode

            Dim access As Accessibility
            Dim modifiers As DeclarationModifiers
            Dim isDefault As Boolean

            Dim method = TryCast(declaration, MethodStatementSyntax)
            If method IsNot Nothing Then
                Me.GetAccessibilityAndModifiers(method.Modifiers, access, modifiers, isDefault)
                If modifiers.IsAbstract OrElse access <> requiredAccess Then
                    method = method.WithModifiers(GetModifierList(requiredAccess, modifiers - DeclarationModifiers.Abstract, False))
                End If

                Return SyntaxFactory.MethodBlock(
                    kind:=If(method.IsKind(SyntaxKind.FunctionStatement), SyntaxKind.FunctionBlock, SyntaxKind.SubBlock),
                    begin:=method,
                    [end]:=If(method.IsKind(SyntaxKind.FunctionStatement), SyntaxFactory.EndFunctionStatement(), SyntaxFactory.EndSubStatement()))
            End If

            Dim prop = TryCast(declaration, PropertyStatementSyntax)
            If prop IsNot Nothing Then
                Me.GetAccessibilityAndModifiers(prop.Modifiers, access, modifiers, isDefault)
                If modifiers.IsAbstract OrElse access <> requiredAccess Then
                    prop = prop.WithModifiers(GetModifierList(requiredAccess, modifiers - DeclarationModifiers.Abstract, isDefault And allowDefault))
                End If

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
                begin:=SyntaxFactory.SubNewStatement(
                    attributeLists:=Nothing,
                    modifiers:=GetModifierList(accessibility, modifiers And constructorModifers),
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
                begin:=SyntaxFactory.ClassStatement(
                    attributeLists:=Nothing,
                    modifiers:=GetModifierList(accessibility, modifiers And classModifiers),
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
            Return TryCast(node, StatementSyntax)
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
                begin:=SyntaxFactory.StructureStatement(
                    attributeLists:=Nothing,
                    modifiers:=GetModifierList(accessibility, modifiers And structModifiers),
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
                begin:=SyntaxFactory.InterfaceStatement(
                    attributeLists:=Nothing,
                    modifiers:=GetModifierList(accessibility, DeclarationModifiers.None),
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
            Select Case node.VBKind
                Case SyntaxKind.FunctionBlock,
                     SyntaxKind.SubBlock
                    Return AsInterfaceMember(DirectCast(node, MethodBlockSyntax).Begin)
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
                    modifiers:=GetModifierList(accessibility, modifiers),
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
                modifiers:=GetModifierList(accessibility, modifiers, kind),
                keyword:=If(kind = SyntaxKind.DelegateSubStatement, SyntaxFactory.Token(SyntaxKind.SubKeyword), SyntaxFactory.Token(SyntaxKind.FunctionKeyword)),
                identifier:=name.ToIdentifierToken(),
                typeParameterList:=GetTypeParameters(typeParameters),
                parameterList:=GetParameterList(parameters),
                asClause:=If(kind = SyntaxKind.DelegateFunctionStatement, SyntaxFactory.SimpleAsClause(DirectCast(returnType, TypeSyntax)), Nothing))
        End Function

        Public Overrides Function CompilationUnit(Optional declarations As IEnumerable(Of SyntaxNode) = Nothing) As SyntaxNode
            Return SyntaxFactory.CompilationUnit().WithImports(GetImports(declarations)).WithMembers(GetNamespaceMembers(declarations))
        End Function

        Private Function GetImports(declarations As IEnumerable(Of SyntaxNode)) As SyntaxList(Of ImportsStatementSyntax)
            Return If(declarations Is Nothing, Nothing, SyntaxFactory.List(declarations.OfType(Of ImportsStatementSyntax)()))
        End Function

        Private Function GetNamespaceMembers(declarations As IEnumerable(Of SyntaxNode)) As SyntaxList(Of StatementSyntax)
            Return If(declarations Is Nothing, Nothing, SyntaxFactory.List(declarations.OfType(Of StatementSyntax)().Where(Function(s) Not TypeOf s Is ImportsStatementSyntax)))
        End Function

        Public Overrides Function NamespaceImportDeclaration(name As SyntaxNode) As SyntaxNode
            Return SyntaxFactory.ImportsStatement(SyntaxFactory.SingletonSeparatedList(Of ImportsClauseSyntax)(SyntaxFactory.SimpleImportsClause(DirectCast(name, NameSyntax))))
        End Function

        Public Overrides Function NamespaceDeclaration(name As SyntaxNode, nestedDeclarations As IEnumerable(Of SyntaxNode)) As SyntaxNode
            Dim imps As IEnumerable(Of StatementSyntax) = GetImports(nestedDeclarations)
            Dim members As IEnumerable(Of StatementSyntax) = GetNamespaceMembers(nestedDeclarations)

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
                argumentList:=If(attributeArguments IsNot Nothing, SyntaxFactory.ArgumentList(SyntaxFactory.SeparatedList(attributeArguments.Select(AddressOf AsArgument))), Nothing))

            Return AsAttributeList(attr)
        End Function

        Public Overrides Function AttributeArgument(name As String, expression As SyntaxNode) As SyntaxNode
            Return Argument(name, RefKind.None, expression)
        End Function

        Private Shared Function ClearTrivia(Of TNode As SyntaxNode)(nodes As IEnumerable(Of TNode)) As IEnumerable(Of TNode)
            Return If(nodes IsNot Nothing, nodes.Select(Function(n) ClearTrivia(n)), Nothing)
        End Function

        Private Shared Function ClearTrivia(Of TNode As SyntaxNode)(node As TNode) As TNode
            Return node.WithLeadingTrivia(SyntaxFactory.ElasticMarker).WithTrailingTrivia(SyntaxFactory.ElasticMarker)
        End Function

        Private Function GetAttributeLists(attributes As IEnumerable(Of SyntaxNode)) As SyntaxList(Of AttributeListSyntax)
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

        Public Overrides Function GetAttributes(declaration As SyntaxNode) As IEnumerable(Of SyntaxNode)
            Return GetAttributeLists(declaration)
        End Function

        Public Overrides Function RemoveAttributes(declaration As SyntaxNode) As SyntaxNode
            Return PreserveTrivia(declaration, Function(d) RemoveReturnAttributes(WithAttributeLists(d, Nothing)))
        End Function

        Public Overrides Function AddAttributes(declaration As SyntaxNode, attributes As IEnumerable(Of SyntaxNode)) As SyntaxNode
            Return PreserveTrivia(declaration, Function(d) AddAttributesInternal(d, ClearTrivia(attributes)))
        End Function

        Private Function AddAttributesInternal(declaration As SyntaxNode, attributes As IEnumerable(Of SyntaxNode)) As SyntaxNode
            Dim lists = GetAttributeLists(attributes)

            Dim compUnit = TryCast(declaration, CompilationUnitSyntax)
            If compUnit IsNot Nothing Then
                Dim attributesWithAssemblyTarget = lists.Select(AddressOf WithAssemblyTargets)
                Return compUnit.WithAttributes(compUnit.Attributes.Add(SyntaxFactory.AttributesStatement(SyntaxFactory.List(attributesWithAssemblyTarget))))
            End If

            Dim parameter = TryCast(declaration, ParameterSyntax)
            If parameter IsNot Nothing Then
                Return parameter.AddAttributeLists(lists.ToArray())
            End If

            Dim statement = TryCast(declaration, StatementSyntax)
            If statement IsNot Nothing Then
                Return statement.AddAttributeLists(lists.ToArray())
            End If

            Return declaration
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

        Public Overrides Function GetReturnAttributes(declaration As SyntaxNode) As IEnumerable(Of SyntaxNode)
            Select Case declaration.VBKind()
                Case SyntaxKind.FunctionBlock
                    Return GetReturnAttributes(DirectCast(declaration, MethodBlockSyntax).Begin)
                Case SyntaxKind.FunctionStatement
                    Return DirectCast(declaration, MethodStatementSyntax).AsClause.AttributeLists
                Case SyntaxKind.DelegateFunctionStatement
                    Return DirectCast(declaration, DelegateStatementSyntax).AsClause.AttributeLists
                Case Else
                    Return SpecializedCollections.EmptyEnumerable(Of SyntaxNode)
            End Select
        End Function

        Public Overrides Function WithReturnAttributes(declaration As SyntaxNode, attributes As IEnumerable(Of SyntaxNode)) As SyntaxNode
            Dim lists = GetAttributeLists(attributes)

            Select Case declaration.VBKind()
                Case SyntaxKind.FunctionBlock
                    Dim block = DirectCast(declaration, MethodBlockSyntax)
                    Return block.Begin.WithAsClause(block.Begin.AsClause.WithAttributeLists(lists))
                Case SyntaxKind.FunctionStatement
                    Dim stmt = DirectCast(declaration, MethodStatementSyntax)
                    Return stmt.WithAsClause(stmt.AsClause.WithAttributeLists(lists))
                Case SyntaxKind.DelegateFunctionStatement
                    Dim fn = DirectCast(declaration, DelegateStatementSyntax)
                    Return fn.WithAsClause(fn.AsClause.WithAttributeLists(lists))
                Case Else
                    Return declaration
            End Select
        End Function

        Private Function RemoveReturnAttributes(declaration As SyntaxNode) As SyntaxNode
            Dim asClause = TryCast(GetAsClause(declaration), SimpleAsClauseSyntax)
            If asClause IsNot Nothing Then
                Dim newAsClause = asClause.WithAttributeLists(Nothing)
                Return declaration.ReplaceNode(asClause, newAsClause)
            Else
                Return declaration
            End If
        End Function

        Public Overrides Function AddReturnAttributes(methodDeclaration As SyntaxNode, attributes As IEnumerable(Of SyntaxNode)) As SyntaxNode
            Dim lists = GetAttributeLists(ClearTrivia(attributes))

            Dim methodBlock = TryCast(methodDeclaration, MethodBlockSyntax)
            If (methodBlock IsNot Nothing) Then
                Return methodBlock.WithBegin(methodBlock.Begin.WithAsClause(methodBlock.Begin.AsClause.WithAttributeLists(methodBlock.Begin.AttributeLists.AddRange(lists))))
            End If

            Dim method = TryCast(methodDeclaration, MethodStatementSyntax)
            If method IsNot Nothing Then
                Return method.WithAsClause(method.AsClause.WithAttributeLists(method.AttributeLists.AddRange(lists)))
            End If

            Return methodDeclaration
        End Function

        Private Function GetAttributeLists(node As SyntaxNode) As IEnumerable(Of AttributeListSyntax)
            Select Case node.VBKind
                Case SyntaxKind.CompilationUnit
                    Return DirectCast(node, CompilationUnitSyntax).Attributes.SelectMany(Function(s) s.AttributeLists)
                Case SyntaxKind.ClassBlock
                    Return DirectCast(node, ClassBlockSyntax).Begin.AttributeLists
                Case SyntaxKind.ClassStatement
                    Return DirectCast(node, ClassStatementSyntax).AttributeLists
                Case SyntaxKind.StructureBlock
                    Return DirectCast(node, StructureBlockSyntax).Begin.AttributeLists
                Case SyntaxKind.StructureStatement
                    Return DirectCast(node, StructureStatementSyntax).AttributeLists
                Case SyntaxKind.InterfaceBlock
                    Return DirectCast(node, InterfaceBlockSyntax).Begin.AttributeLists
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
                    Return DirectCast(node, MethodBlockSyntax).Begin.AttributeLists
                Case SyntaxKind.FunctionStatement,
                     SyntaxKind.SubStatement,
                     SyntaxKind.SubNewStatement
                    Return DirectCast(node, MethodStatementSyntax).AttributeLists
                Case SyntaxKind.PropertyBlock
                    Return DirectCast(node, PropertyBlockSyntax).PropertyStatement.AttributeLists
                Case SyntaxKind.PropertyStatement
                    Return DirectCast(node, PropertyStatementSyntax).AttributeLists
                Case SyntaxKind.OperatorBlock
                    Return DirectCast(node, OperatorBlockSyntax).Begin.AttributeLists
                Case SyntaxKind.OperatorStatement
                    Return DirectCast(node, OperatorStatementSyntax).AttributeLists
                Case SyntaxKind.EventBlock
                    Return DirectCast(node, EventBlockSyntax).EventStatement.AttributeLists
                Case SyntaxKind.EventStatement
                    Return DirectCast(node, EventStatementSyntax).AttributeLists
                Case Else
                    Return SpecializedCollections.EmptyEnumerable(Of AttributeListSyntax)()
            End Select
        End Function

        Private Function WithAttributeLists(node As SyntaxNode, lists As IEnumerable(Of AttributeListSyntax)) As SyntaxNode
            Dim arg = SyntaxFactory.List(lists)

            Select Case node.VBKind
                Case SyntaxKind.CompilationUnit
                    ' convert to assembly target
                    arg = SyntaxFactory.List(lists.Select(Function(lst) Me.WithAssemblyTargets(lst)))
                    Return DirectCast(node, CompilationUnitSyntax).WithAttributes(SyntaxFactory.SingletonList(SyntaxFactory.AttributesStatement(arg)))
                Case SyntaxKind.ClassBlock
                    Return DirectCast(node, ClassBlockSyntax).WithBegin(DirectCast(node, ClassBlockSyntax).Begin.WithAttributeLists(arg))
                Case SyntaxKind.ClassStatement
                    Return DirectCast(node, ClassStatementSyntax).WithAttributeLists(arg)
                Case SyntaxKind.StructureBlock
                    Return DirectCast(node, StructureBlockSyntax).WithBegin(DirectCast(node, StructureBlockSyntax).Begin.WithAttributeLists(arg))
                Case SyntaxKind.StructureStatement
                    Return DirectCast(node, StructureStatementSyntax).WithAttributeLists(arg)
                Case SyntaxKind.InterfaceBlock
                    Return DirectCast(node, InterfaceBlockSyntax).WithBegin(DirectCast(node, InterfaceBlockSyntax).Begin.WithAttributeLists(arg))
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
                     SyntaxKind.SubBlock,
                     SyntaxKind.ConstructorBlock
                    Return DirectCast(node, MethodBlockSyntax).WithBegin(DirectCast(node, MethodBlockSyntax).Begin.WithAttributeLists(arg))
                Case SyntaxKind.FunctionStatement,
                     SyntaxKind.SubStatement,
                     SyntaxKind.SubNewStatement
                    Return DirectCast(node, MethodStatementSyntax).WithAttributeLists(arg)
                Case SyntaxKind.PropertyBlock
                    Return DirectCast(node, PropertyBlockSyntax).WithPropertyStatement(DirectCast(node, PropertyBlockSyntax).PropertyStatement.WithAttributeLists(arg))
                Case SyntaxKind.PropertyStatement
                    Return DirectCast(node, PropertyStatementSyntax).WithAttributeLists(arg)
                Case SyntaxKind.OperatorBlock
                    Return DirectCast(node, OperatorBlockSyntax).WithBegin(DirectCast(node, OperatorBlockSyntax).Begin.WithAttributeLists(arg))
                Case SyntaxKind.OperatorStatement
                    Return DirectCast(node, OperatorStatementSyntax).WithAttributeLists(arg)
                Case SyntaxKind.EventBlock
                    Return DirectCast(node, EventBlockSyntax).WithEventStatement(DirectCast(node, EventBlockSyntax).EventStatement.WithAttributeLists(arg))
                Case SyntaxKind.EventStatement
                    Return DirectCast(node, EventStatementSyntax).WithAttributeLists(arg)
                Case Else
                    Return node
            End Select
        End Function

        Public Overrides Function GetDeclarationKind(declaration As SyntaxNode) As DeclarationKind
            Select Case declaration.VBKind
                Case SyntaxKind.CompilationUnit
                    Return DeclarationKind.CompilationUnit
                Case SyntaxKind.NamespaceBlock
                    Return DeclarationKind.Namespace
                Case SyntaxKind.ImportsStatement
                    Return DeclarationKind.NamespaceImport
                Case SyntaxKind.Attribute,
                     SyntaxKind.AttributeList,
                     SyntaxKind.AttributesStatement
                    Return DeclarationKind.Attribute
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
                Case SyntaxKind.FieldDeclaration
                    Return DeclarationKind.Field
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
                Case SyntaxKind.LocalDeclarationStatement
                    Return DeclarationKind.LocalVariable
            End Select
            Return DeclarationKind.None
        End Function

        Private Shared Function IsChildOf(node As SyntaxNode, kind As SyntaxKind) As Boolean
            Return node.Parent IsNot Nothing AndAlso node.Parent.IsKind(kind)
        End Function

        Private Shared Function IsIndexer(declaration As SyntaxNode) As Boolean
            Select Case declaration.VBKind
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
            Select Case declaration.VBKind
                Case SyntaxKind.ClassBlock
                    Return DirectCast(declaration, ClassBlockSyntax).Begin.Identifier.ValueText
                Case SyntaxKind.StructureBlock
                    Return DirectCast(declaration, StructureBlockSyntax).Begin.Identifier.ValueText
                Case SyntaxKind.InterfaceBlock
                    Return DirectCast(declaration, InterfaceBlockSyntax).Begin.Identifier.ValueText
                Case SyntaxKind.EnumBlock
                    Return DirectCast(declaration, EnumBlockSyntax).EnumStatement.Identifier.ValueText
                Case SyntaxKind.EnumMemberDeclaration
                    Return DirectCast(declaration, EnumMemberDeclarationSyntax).Identifier.ValueText
                Case SyntaxKind.DelegateFunctionStatement,
                     SyntaxKind.DelegateSubStatement
                    Return DirectCast(declaration, DelegateStatementSyntax).Identifier.ValueText
                Case SyntaxKind.FieldDeclaration
                    Return DirectCast(declaration, FieldDeclarationSyntax).Declarators(0).Names(0).Identifier.ValueText
                Case SyntaxKind.FunctionBlock,
                     SyntaxKind.SubBlock
                    Return DirectCast(declaration, MethodBlockSyntax).Begin.Identifier.ValueText
                Case SyntaxKind.FunctionStatement,
                     SyntaxKind.SubStatement
                    Return DirectCast(declaration, MethodStatementSyntax).Identifier.ValueText
                Case SyntaxKind.PropertyBlock
                    If GetDeclarationKind(declaration) = DeclarationKind.Property Then
                        Return DirectCast(declaration, PropertyBlockSyntax).PropertyStatement.Identifier.ValueText
                    End If
                Case SyntaxKind.PropertyStatement
                    If GetDeclarationKind(declaration) = DeclarationKind.Property Then
                        Return DirectCast(declaration, PropertyStatementSyntax).Identifier.ValueText
                    End If
                Case SyntaxKind.EventBlock
                    Return DirectCast(declaration, EventBlockSyntax).EventStatement.Identifier.ValueText
                Case SyntaxKind.EventStatement
                    Return DirectCast(declaration, EventStatementSyntax).Identifier.ValueText
                Case SyntaxKind.EventStatement
                    Return DirectCast(declaration, EventStatementSyntax).Identifier.ValueText
                Case SyntaxKind.Parameter
                    Return DirectCast(declaration, ParameterSyntax).Identifier.Identifier.ValueText
                Case SyntaxKind.LocalDeclarationStatement
                    Return DirectCast(declaration, LocalDeclarationStatementSyntax).Declarators(0).Names(0).Identifier.ValueText
                Case SyntaxKind.NamespaceBlock
                    Return DirectCast(declaration, NamespaceBlockSyntax).NamespaceStatement.Name.ToString()
                Case SyntaxKind.Attribute
                    Return DirectCast(declaration, AttributeSyntax).Name.ToString()
                Case SyntaxKind.AttributeList
                    Return DirectCast(declaration, AttributeListSyntax).Attributes(0).Name.ToString()
                Case SyntaxKind.ImportsStatement
                    Dim clause = DirectCast(declaration, ImportsStatementSyntax).ImportsClauses(0)
                    Select Case clause.VBKind
                        Case SyntaxKind.SimpleImportsClause
                            Return DirectCast(clause, SimpleImportsClauseSyntax).Name.ToString()
                    End Select
            End Select
            Return String.Empty
        End Function

        Public Overrides Function WithName(declaration As SyntaxNode, name As String) As SyntaxNode
            Dim id = name.ToIdentifierToken()

            Select Case declaration.VBKind
                Case SyntaxKind.ClassBlock
                    Return ReplaceWithTrivia(declaration, DirectCast(declaration, ClassBlockSyntax).Begin.Identifier, id)
                Case SyntaxKind.StructureBlock
                    Return ReplaceWithTrivia(declaration, DirectCast(declaration, StructureBlockSyntax).Begin.Identifier, id)
                Case SyntaxKind.InterfaceBlock
                    Return ReplaceWithTrivia(declaration, DirectCast(declaration, InterfaceBlockSyntax).Begin.Identifier, id)
                Case SyntaxKind.EnumBlock
                    Return ReplaceWithTrivia(declaration, DirectCast(declaration, EnumBlockSyntax).EnumStatement.Identifier, id)
                Case SyntaxKind.EnumMemberDeclaration
                    Return ReplaceWithTrivia(declaration, DirectCast(declaration, EnumMemberDeclarationSyntax).Identifier, id)
                Case SyntaxKind.DelegateFunctionStatement,
                     SyntaxKind.DelegateSubStatement
                    Return ReplaceWithTrivia(declaration, DirectCast(declaration, DelegateStatementSyntax).Identifier, id)
                Case SyntaxKind.FieldDeclaration
                    Return ReplaceWithTrivia(declaration, DirectCast(declaration, FieldDeclarationSyntax).Declarators(0).Names(0).Identifier, id)
                Case SyntaxKind.FunctionBlock,
                     SyntaxKind.SubBlock
                    Return ReplaceWithTrivia(declaration, DirectCast(declaration, MethodBlockSyntax).Begin.Identifier, id)
                Case SyntaxKind.FunctionStatement,
                     SyntaxKind.SubStatement
                    Return ReplaceWithTrivia(declaration, DirectCast(declaration, MethodStatementSyntax).Identifier, id)
                Case SyntaxKind.PropertyBlock
                    If GetDeclarationKind(declaration) = DeclarationKind.Property Then
                        Return ReplaceWithTrivia(declaration, DirectCast(declaration, PropertyBlockSyntax).PropertyStatement.Identifier, id)
                    End If
                Case SyntaxKind.PropertyStatement
                    If GetDeclarationKind(declaration) = DeclarationKind.Property Then
                        Return ReplaceWithTrivia(declaration, DirectCast(declaration, PropertyStatementSyntax).Identifier, id)
                    End If
                Case SyntaxKind.EventBlock
                    Return ReplaceWithTrivia(declaration, DirectCast(declaration, EventBlockSyntax).EventStatement.Identifier, id)
                Case SyntaxKind.EventStatement
                    Return ReplaceWithTrivia(declaration, DirectCast(declaration, EventStatementSyntax).Identifier, id)
                Case SyntaxKind.EventStatement
                    Return ReplaceWithTrivia(declaration, DirectCast(declaration, EventStatementSyntax).Identifier, id)
                Case SyntaxKind.Parameter
                    Return ReplaceWithTrivia(declaration, DirectCast(declaration, ParameterSyntax).Identifier.Identifier, id)
                Case SyntaxKind.LocalDeclarationStatement
                    Return ReplaceWithTrivia(declaration, DirectCast(declaration, LocalDeclarationStatementSyntax).Declarators(0).Names(0).Identifier, id)
                Case SyntaxKind.NamespaceBlock
                    Return ReplaceWithTrivia(declaration, DirectCast(declaration, NamespaceBlockSyntax).NamespaceStatement.Name, Me.DottedName(name))
                Case SyntaxKind.Attribute
                    Return ReplaceWithTrivia(declaration, DirectCast(declaration, AttributeSyntax).Name, Me.DottedName(name))
                Case SyntaxKind.AttributeList
                    Return ReplaceWithTrivia(declaration, DirectCast(declaration, AttributeListSyntax).Attributes(0).Name, Me.DottedName(name))
                Case SyntaxKind.ImportsStatement
                    Dim clause = DirectCast(declaration, ImportsStatementSyntax).ImportsClauses(0)
                    Select Case clause.VBKind
                        Case SyntaxKind.SimpleImportsClause
                            Return ReplaceWithTrivia(declaration, DirectCast(clause, SimpleImportsClauseSyntax).Name, Me.DottedName(name))
                    End Select
            End Select

            Return declaration
        End Function

        Public Overrides Function [GetType](declaration As SyntaxNode) As SyntaxNode
            Dim asClause = GetAsClause(declaration)
            If asClause IsNot Nothing Then
                Return asClause.Type
            Else
                Return Nothing
            End If
        End Function

        Public Overrides Function WithType(declaration As SyntaxNode, type As SyntaxNode) As SyntaxNode
            Return PreserveTrivia(declaration, Function(d) WithTypeInternal(d, type))
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
                    Select Case asClause.VBKind
                        Case SyntaxKind.SimpleAsClause
                            asClause = DirectCast(asClause, SimpleAsClauseSyntax).WithType(DirectCast(type, TypeSyntax))
                        Case SyntaxKind.AsNewClause
                            Dim asNew = DirectCast(asClause, AsNewClauseSyntax)
                            Select Case asNew.NewExpression.VBKind
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
            Select Case declaration.VBKind
                Case SyntaxKind.DelegateFunctionStatement
                    Return DirectCast(declaration, DelegateStatementSyntax).AsClause
                Case SyntaxKind.FieldDeclaration
                    Return DirectCast(declaration, FieldDeclarationSyntax).Declarators(0).AsClause
                Case SyntaxKind.FunctionBlock
                    Return DirectCast(declaration, MethodBlockSyntax).Begin.AsClause
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
                Case SyntaxKind.LocalDeclarationStatement
                    Return DirectCast(declaration, LocalDeclarationStatementSyntax).Declarators(0).AsClause
                Case Else
                    Return Nothing
            End Select
        End Function

        Private Function WithAsClause(declaration As SyntaxNode, asClause As AsClauseSyntax) As SyntaxNode
            Select Case declaration.VBKind
                Case SyntaxKind.DelegateFunctionStatement
                    Return DirectCast(declaration, DelegateStatementSyntax).WithAsClause(DirectCast(asClause, SimpleAsClauseSyntax))
                Case SyntaxKind.FieldDeclaration
                    Dim d = DirectCast(declaration, FieldDeclarationSyntax).Declarators(0)
                    Return declaration.ReplaceNode(d, d.WithAsClause(asClause))
                Case SyntaxKind.FunctionBlock
                    Return DirectCast(declaration, MethodBlockSyntax).WithBegin(DirectCast(declaration, MethodBlockSyntax).Begin.WithAsClause(DirectCast(asClause, SimpleAsClauseSyntax)))
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
                    Dim ld = DirectCast(declaration, LocalDeclarationStatementSyntax).Declarators(0)
                    Return declaration.ReplaceNode(ld, ld.WithAsClause(asClause))
                Case Else
                    Return declaration
            End Select
        End Function

        Private Function AsFunction(declaration As SyntaxNode) As SyntaxNode
            Return PreserveTrivia(declaration, AddressOf AsFunctionInternal)
        End Function

        Private Function AsFunctionInternal(declaration As SyntaxNode) As SyntaxNode
            Select Case declaration.VBKind
                Case SyntaxKind.SubBlock
                    Dim sb = DirectCast(declaration, MethodBlockSyntax)
                    Return SyntaxFactory.MethodBlock(
                        SyntaxKind.FunctionBlock,
                        DirectCast(AsFunction(sb.Begin), MethodStatementSyntax),
                        sb.Statements,
                        SyntaxFactory.EndBlockStatement(
                            SyntaxKind.EndFunctionStatement,
                            sb.End.EndKeyword,
                            SyntaxFactory.Token(sb.End.BlockKeyword.LeadingTrivia, SyntaxKind.FunctionKeyword, sb.End.BlockKeyword.TrailingTrivia)
                            ))
                Case SyntaxKind.SubStatement
                    Dim ss = DirectCast(declaration, MethodStatementSyntax)
                    Return SyntaxFactory.MethodStatement(
                        SyntaxKind.FunctionStatement,
                        ss.AttributeLists,
                        ss.Modifiers,
                        SyntaxFactory.Token(ss.Keyword.LeadingTrivia, SyntaxKind.FunctionKeyword, ss.Keyword.TrailingTrivia),
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
                        SyntaxFactory.Token(ds.Keyword.LeadingTrivia, SyntaxKind.FunctionKeyword, ds.Keyword.TrailingTrivia),
                        ds.Identifier,
                        ds.TypeParameterList,
                        ds.ParameterList,
                        SyntaxFactory.SimpleAsClause(SyntaxFactory.IdentifierName("Object")))
                Case SyntaxKind.MultiLineSubLambdaExpression
                    Dim ml = DirectCast(declaration, MultiLineLambdaExpressionSyntax)
                    Return SyntaxFactory.MultiLineLambdaExpression(
                        SyntaxKind.MultiLineFunctionLambdaExpression,
                        DirectCast(AsFunction(ml.Begin), LambdaHeaderSyntax),
                        ml.Statements,
                        SyntaxFactory.EndBlockStatement(
                            SyntaxKind.EndFunctionStatement,
                            ml.End.EndKeyword,
                            SyntaxFactory.Token(ml.End.BlockKeyword.LeadingTrivia, SyntaxKind.FunctionKeyword, ml.End.BlockKeyword.TrailingTrivia)
                            ))
                Case SyntaxKind.SingleLineSubLambdaExpression
                    Dim sl = DirectCast(declaration, SingleLineLambdaExpressionSyntax)
                    Return SyntaxFactory.SingleLineLambdaExpression(
                        SyntaxKind.SingleLineFunctionLambdaExpression,
                        DirectCast(AsFunction(sl.Begin), LambdaHeaderSyntax),
                        sl.Body)
                Case SyntaxKind.SubLambdaHeader
                    Dim lh = DirectCast(declaration, LambdaHeaderSyntax)
                    Return SyntaxFactory.LambdaHeader(
                        SyntaxKind.FunctionLambdaHeader,
                        lh.AttributeLists,
                        lh.Modifiers,
                        SyntaxFactory.Token(lh.Keyword.LeadingTrivia, SyntaxKind.FunctionKeyword, lh.Keyword.TrailingTrivia),
                        lh.ParameterList,
                        asClause:=Nothing)
                Case SyntaxKind.DeclareSubStatement
                    Dim ds = DirectCast(declaration, DeclareStatementSyntax)
                    Return SyntaxFactory.DeclareStatement(
                        SyntaxKind.DeclareFunctionStatement,
                        ds.AttributeLists,
                        ds.Modifiers,
                        ds.CharsetKeyword,
                        SyntaxFactory.Token(ds.Keyword.LeadingTrivia, SyntaxKind.FunctionKeyword, ds.Keyword.TrailingTrivia),
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
            Return PreserveTrivia(declaration, AddressOf AsSubInternal)
        End Function

        Private Function AsSubInternal(declaration As SyntaxNode) As SyntaxNode
            Select Case declaration.VBKind
                Case SyntaxKind.FunctionBlock
                    Dim mb = DirectCast(declaration, MethodBlockSyntax)
                    Return SyntaxFactory.MethodBlock(
                        SyntaxKind.SubBlock,
                        DirectCast(AsSub(mb.Begin), MethodStatementSyntax),
                        mb.Statements,
                        SyntaxFactory.EndBlockStatement(
                            SyntaxKind.EndSubStatement,
                            mb.End.EndKeyword,
                            SyntaxFactory.Token(mb.End.BlockKeyword.LeadingTrivia, SyntaxKind.SubKeyword, mb.End.BlockKeyword.TrailingTrivia)
                            ))
                Case SyntaxKind.FunctionStatement
                    Dim ms = DirectCast(declaration, MethodStatementSyntax)
                    Return SyntaxFactory.MethodStatement(
                        SyntaxKind.SubStatement,
                        ms.AttributeLists,
                        ms.Modifiers,
                        SyntaxFactory.Token(ms.Keyword.LeadingTrivia, SyntaxKind.SubKeyword, ms.Keyword.TrailingTrivia),
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
                        SyntaxFactory.Token(ds.Keyword.LeadingTrivia, SyntaxKind.SubKeyword, ds.Keyword.TrailingTrivia),
                        ds.Identifier,
                        ds.TypeParameterList,
                        ds.ParameterList,
                        asClause:=Nothing)
                Case SyntaxKind.MultiLineFunctionLambdaExpression
                    Dim ml = DirectCast(declaration, MultiLineLambdaExpressionSyntax)
                    Return SyntaxFactory.MultiLineLambdaExpression(
                        SyntaxKind.MultiLineSubLambdaExpression,
                        DirectCast(AsSub(ml.Begin), LambdaHeaderSyntax),
                        ml.Statements,
                        SyntaxFactory.EndBlockStatement(
                            SyntaxKind.EndSubStatement,
                            ml.End.EndKeyword,
                            SyntaxFactory.Token(ml.End.BlockKeyword.LeadingTrivia, SyntaxKind.SubKeyword, ml.End.BlockKeyword.TrailingTrivia)
                            ))
                Case SyntaxKind.SingleLineFunctionLambdaExpression
                    Dim sl = DirectCast(declaration, SingleLineLambdaExpressionSyntax)
                    Return SyntaxFactory.SingleLineLambdaExpression(
                        SyntaxKind.SingleLineSubLambdaExpression,
                        DirectCast(AsSub(sl.Begin), LambdaHeaderSyntax),
                        sl.Body)
                Case SyntaxKind.FunctionLambdaHeader
                    Dim lh = DirectCast(declaration, LambdaHeaderSyntax)
                    Return SyntaxFactory.LambdaHeader(
                        SyntaxKind.SubLambdaHeader,
                        lh.AttributeLists,
                        lh.Modifiers,
                        SyntaxFactory.Token(lh.Keyword.LeadingTrivia, SyntaxKind.SubKeyword, lh.Keyword.TrailingTrivia),
                        lh.ParameterList,
                        asClause:=Nothing)
                Case SyntaxKind.DeclareFunctionStatement
                    Dim ds = DirectCast(declaration, DeclareStatementSyntax)
                    Return SyntaxFactory.DeclareStatement(
                        SyntaxKind.DeclareSubStatement,
                        ds.AttributeLists,
                        ds.Modifiers,
                        ds.CharsetKeyword,
                        SyntaxFactory.Token(ds.Keyword.LeadingTrivia, SyntaxKind.SubKeyword, ds.Keyword.TrailingTrivia),
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
            Return PreserveTrivia(declaration, Function(d) Me.WithModifiersInternal(d, modifiers))
        End Function

        Private Function WithModifiersInternal(declaration As SyntaxNode, modifiers As DeclarationModifiers) As SyntaxNode
            Dim tokens = GetModifierTokens(declaration)

            Dim acc As Accessibility
            Dim currentMods As DeclarationModifiers
            Dim isDefault As Boolean
            GetAccessibilityAndModifiers(tokens, acc, currentMods, isDefault)

            If (currentMods <> modifiers) Then
                Dim newTokens = GetModifierList(acc, modifiers, declaration.VBKind, isDefault)
                Return WithModifierTokens(declaration, Merge(tokens, newTokens))
            Else
                Return declaration
            End If
        End Function

        Private Function Merge(original As SyntaxTokenList, newList As SyntaxTokenList) As SyntaxTokenList
            '' return tokens from newList, but use original tokens if kind matches
            Return SyntaxFactory.TokenList(newList.Select(Function(token) If(original.Any(token.VBKind), original.First(Function(tk) tk.IsKind(token.VBKind)), token)))
        End Function

        Private Function GetModifierTokens(declaration As SyntaxNode) As SyntaxTokenList
            Select Case declaration.VBKind
                Case SyntaxKind.ClassBlock
                    Return DirectCast(declaration, ClassBlockSyntax).Begin.Modifiers
                Case SyntaxKind.ClassStatement
                    Return DirectCast(declaration, ClassStatementSyntax).Modifiers
                Case SyntaxKind.StructureBlock
                    Return DirectCast(declaration, StructureBlockSyntax).Begin.Modifiers
                Case SyntaxKind.StructureStatement
                    Return DirectCast(declaration, StructureStatementSyntax).Modifiers
                Case SyntaxKind.InterfaceBlock
                    Return DirectCast(declaration, InterfaceBlockSyntax).Begin.Modifiers
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
                    Return DirectCast(declaration, MethodBlockSyntax).Begin.Modifiers
                Case SyntaxKind.ConstructorBlock
                    Return DirectCast(declaration, ConstructorBlockSyntax).Begin.Modifiers
                Case SyntaxKind.FunctionStatement,
                     SyntaxKind.SubStatement,
                     SyntaxKind.SubNewStatement
                    Return DirectCast(declaration, MethodStatementSyntax).Modifiers
                Case SyntaxKind.PropertyBlock
                    Return DirectCast(declaration, PropertyBlockSyntax).PropertyStatement.Modifiers
                Case SyntaxKind.PropertyStatement
                    Return DirectCast(declaration, PropertyStatementSyntax).Modifiers
                Case SyntaxKind.OperatorBlock
                    Return DirectCast(declaration, OperatorBlockSyntax).Begin.Modifiers
                Case SyntaxKind.OperatorStatement
                    Return DirectCast(declaration, OperatorStatementSyntax).Modifiers
                Case SyntaxKind.EventBlock
                    Return DirectCast(declaration, EventBlockSyntax).EventStatement.Modifiers
                Case SyntaxKind.EventStatement
                    Return DirectCast(declaration, EventStatementSyntax).Modifiers
                Case Else
                    Return Nothing
            End Select
        End Function

        Private Function WithModifierTokens(declaration As SyntaxNode, tokens As SyntaxTokenList) As SyntaxNode
            Select Case declaration.VBKind
                Case SyntaxKind.ClassBlock
                    Return DirectCast(declaration, ClassBlockSyntax).WithBegin(DirectCast(declaration, ClassBlockSyntax).Begin.WithModifiers(tokens))
                Case SyntaxKind.ClassStatement
                    Return DirectCast(declaration, ClassStatementSyntax).WithModifiers(tokens)
                Case SyntaxKind.StructureBlock
                    Return DirectCast(declaration, StructureBlockSyntax).WithBegin(DirectCast(declaration, StructureBlockSyntax).Begin.WithModifiers(tokens))
                Case SyntaxKind.StructureStatement
                    Return DirectCast(declaration, StructureStatementSyntax).WithModifiers(tokens)
                Case SyntaxKind.InterfaceBlock
                    Return DirectCast(declaration, InterfaceBlockSyntax).WithBegin(DirectCast(declaration, InterfaceBlockSyntax).Begin.WithModifiers(tokens))
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
                    Return DirectCast(declaration, MethodBlockSyntax).WithBegin(DirectCast(declaration, MethodBlockSyntax).Begin.WithModifiers(tokens))
                Case SyntaxKind.ConstructorBlock
                    Return DirectCast(declaration, ConstructorBlockSyntax).WithBegin(DirectCast(declaration, ConstructorBlockSyntax).Begin.WithModifiers(tokens))
                Case SyntaxKind.FunctionStatement,
                     SyntaxKind.SubStatement
                    Return DirectCast(declaration, MethodStatementSyntax).WithModifiers(tokens)
                Case SyntaxKind.PropertyBlock
                    Return DirectCast(declaration, PropertyBlockSyntax).WithPropertyStatement(DirectCast(declaration, PropertyBlockSyntax).PropertyStatement.WithModifiers(tokens))
                Case SyntaxKind.PropertyStatement
                    Return DirectCast(declaration, PropertyStatementSyntax).WithModifiers(tokens)
                Case SyntaxKind.OperatorBlock
                    Return DirectCast(declaration, OperatorBlockSyntax).WithBegin(DirectCast(declaration, OperatorBlockSyntax).Begin.WithModifiers(tokens))
                Case SyntaxKind.OperatorStatement
                    Return DirectCast(declaration, OperatorStatementSyntax).WithModifiers(tokens)
                Case SyntaxKind.EventBlock
                    Return DirectCast(declaration, EventBlockSyntax).WithEventStatement(DirectCast(declaration, EventBlockSyntax).EventStatement.WithModifiers(tokens))
                Case SyntaxKind.EventStatement
                    Return DirectCast(declaration, EventStatementSyntax).WithModifiers(tokens)
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
            Return PreserveTrivia(declaration, Function(d) Me.WithAccessibilityInternal(d, accessibility))
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

            Dim newTokens = GetModifierList(accessibility, mods, declaration.VBKind, isDefault)
            Return WithModifierTokens(declaration, Merge(tokens, newTokens))
        End Function

        Private Function CanHaveAccessibility(declaration As SyntaxNode) As Boolean
            Select Case declaration.VBKind
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
                    SyntaxKind.EventStatement
                    Return True
                Case Else
                    Return False
            End Select
        End Function

        Private Function GetModifierList(accessibility As Accessibility, modifiers As DeclarationModifiers, kind As SyntaxKind, Optional isDefault As Boolean = False) As SyntaxTokenList
            modifiers = modifiers And GetAllowedModifiers(kind)
            Return GetModifierList(accessibility, modifiers, isDefault, kind = SyntaxKind.FieldDeclaration)
        End Function

        Private Function GetModifierList(accessibility As Accessibility, modifiers As DeclarationModifiers, Optional isDefault As Boolean = False, Optional isField As Boolean = False) As SyntaxTokenList
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
                _list = _list.Add(SyntaxFactory.Token(SyntaxKind.MustInheritKeyword))
            End If

            If modifiers.IsNew Then
                _list = _list.Add(SyntaxFactory.Token(SyntaxKind.ShadowsKeyword))
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

            If modifiers.IsSealed Then
                _list = _list.Add(SyntaxFactory.Token(SyntaxKind.NotInheritableKeyword))
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

            If (isField AndAlso _list.Count = 0) Then
                _list = _list.Add(SyntaxFactory.Token(SyntaxKind.DimKeyword))
            End If

            Return _list
        End Function

        Private Sub GetAccessibilityAndModifiers(modifierTokens As SyntaxTokenList, ByRef accessibility As Accessibility, ByRef modifiers As DeclarationModifiers, ByRef isDefault As Boolean)
            accessibility = Accessibility.NotApplicable
            modifiers = DeclarationModifiers.None
            isDefault = False

            For Each token In modifierTokens
                Select Case token.VBKind
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
                    Case SyntaxKind.MustInheritKeyword
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
                    Case SyntaxKind.NotInheritableKeyword
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
                Return methodBlock.WithBegin(methodBlock.Begin.WithTypeParameterList(replacer(methodBlock.Begin.TypeParameterList)))
            End If

            Dim classBlock = TryCast(declaration, ClassBlockSyntax)
            If classBlock IsNot Nothing Then
                Return classBlock.WithBegin(classBlock.Begin.WithTypeParameterList(replacer(classBlock.Begin.TypeParameterList)))
            End If

            Dim structureBlock = TryCast(declaration, StructureBlockSyntax)
            If structureBlock IsNot Nothing Then
                Return structureBlock.WithBegin(structureBlock.Begin.WithTypeParameterList(replacer(structureBlock.Begin.TypeParameterList)))
            End If

            Dim interfaceBlock = TryCast(declaration, InterfaceBlockSyntax)
            If interfaceBlock IsNot Nothing Then
                Return interfaceBlock.WithBegin(interfaceBlock.Begin.WithTypeParameterList(replacer(interfaceBlock.Begin.TypeParameterList)))
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
                Return SyntaxFactory.MemberAccessExpression(name.VBKind(), sma.Expression, sma.OperatorToken, DirectCast(WithTypeArguments(sma.Name, typeArguments), SimpleNameSyntax))
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

        Public Overrides Function GetParameters(declaration As SyntaxNode) As IReadOnlyList(Of SyntaxNode)
            Dim list = GetParameterList(declaration)
            If list IsNot Nothing Then
                Return list.Parameters
            Else
                Return SpecializedCollections.EmptyReadOnlyList(Of SyntaxNode)
            End If
        End Function

        Public Overrides Function WithParameters(declaration As SyntaxNode, parameters As IEnumerable(Of SyntaxNode)) As SyntaxNode
            Dim currentList = GetParameterList(declaration)
            Dim newList = GetParameterList(parameters)
            If currentList IsNot Nothing Then
                Return WithParameterList(declaration, currentList.WithParameters(newList.Parameters))
            Else
                Return WithParameterList(declaration, newList)
            End If
        End Function

        Private Function GetParameterList(declaration As SyntaxNode) As ParameterListSyntax
            Select Case declaration.VBKind
                Case SyntaxKind.SubBlock,
                    SyntaxKind.FunctionBlock
                    Return DirectCast(declaration, MethodBlockSyntax).Begin.ParameterList
                Case SyntaxKind.ConstructorBlock
                    Return DirectCast(declaration, ConstructorBlockSyntax).Begin.ParameterList
                Case SyntaxKind.OperatorBlock
                    Return DirectCast(declaration, OperatorBlockSyntax).Begin.ParameterList
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
                    Return DirectCast(declaration, MultiLineLambdaExpressionSyntax).Begin.ParameterList
                Case SyntaxKind.SingleLineFunctionLambdaExpression,
                     SyntaxKind.SingleLineSubLambdaExpression
                    Return DirectCast(declaration, SingleLineLambdaExpressionSyntax).Begin.ParameterList
                Case Else
                    Return Nothing
            End Select
        End Function

        Private Function WithParameterList(declaration As SyntaxNode, list As ParameterListSyntax) As SyntaxNode
            Select Case declaration.VBKind
                Case SyntaxKind.DelegateFunctionStatement,
                    SyntaxKind.DelegateSubStatement
                    Return DirectCast(declaration, DelegateStatementSyntax).WithParameterList(list)
                Case SyntaxKind.SubBlock,
                    SyntaxKind.FunctionBlock
                    Return DirectCast(declaration, MethodBlockSyntax).WithBegin(DirectCast(declaration, MethodBlockSyntax).Begin.WithParameterList(list))
                Case SyntaxKind.ConstructorBlock
                    Return DirectCast(declaration, ConstructorBlockSyntax).WithBegin(DirectCast(declaration, ConstructorBlockSyntax).Begin.WithParameterList(list))
                Case SyntaxKind.OperatorBlock
                    Return DirectCast(declaration, OperatorBlockSyntax).WithBegin(DirectCast(declaration, OperatorBlockSyntax).Begin.WithParameterList(list))
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
                    Return DirectCast(declaration, MultiLineLambdaExpressionSyntax).WithBegin(DirectCast(declaration, MultiLineLambdaExpressionSyntax).Begin.WithParameterList(list))
                Case SyntaxKind.SingleLineFunctionLambdaExpression,
                     SyntaxKind.SingleLineSubLambdaExpression
                    Return DirectCast(declaration, SingleLineLambdaExpressionSyntax).WithBegin(DirectCast(declaration, SingleLineLambdaExpressionSyntax).Begin.WithParameterList(list))
            End Select

            Return declaration
        End Function

        Public Overrides Function GetInitializer(declaration As SyntaxNode) As SyntaxNode
            Dim ev = GetEqualsValue(declaration)
            If ev IsNot Nothing Then
                Return ev.Value
            Else
                Return Nothing
            End If
        End Function

        Public Overrides Function WithInitializer(declaration As SyntaxNode, initializer As SyntaxNode) As SyntaxNode
            Return PreserveTrivia(declaration, Function(d) WithInitializerInternal(d, initializer))
        End Function

        Private Function WithInitializerInternal(declaration As SyntaxNode, initializer As SyntaxNode) As SyntaxNode
            Dim currentEV = GetEqualsValue(declaration)
            If currentEV IsNot Nothing Then
                Return WithEqualsValue(declaration, currentEV.WithValue(DirectCast(initializer, ExpressionSyntax)))
            Else
                Return WithEqualsValue(declaration, SyntaxFactory.EqualsValue(DirectCast(initializer, ExpressionSyntax)))
            End If
        End Function

        Private Function GetEqualsValue(declaration As SyntaxNode) As EqualsValueSyntax
            Select Case declaration.VBKind
                Case SyntaxKind.Parameter
                    Return DirectCast(declaration, ParameterSyntax).Default
                Case SyntaxKind.LocalDeclarationStatement
                    Dim d = DirectCast(declaration, LocalDeclarationStatementSyntax).Declarators(0)
                    Return d.Initializer
                Case SyntaxKind.FieldDeclaration
                    Dim d2 = DirectCast(declaration, FieldDeclarationSyntax).Declarators(0)
                    Return d2.Initializer
                Case Else
                    Return Nothing
            End Select
        End Function

        Private Function WithEqualsValue(declaration As SyntaxNode, ev As EqualsValueSyntax) As SyntaxNode
            Select Case declaration.VBKind
                Case SyntaxKind.Parameter
                    Return DirectCast(declaration, ParameterSyntax).WithDefault(ev)
                Case SyntaxKind.LocalDeclarationStatement
                    Dim d = DirectCast(declaration, LocalDeclarationStatementSyntax).Declarators(0)
                    Return declaration.ReplaceNode(d, d.WithInitializer(ev))
                Case SyntaxKind.FieldDeclaration
                    Dim d2 = DirectCast(declaration, FieldDeclarationSyntax).Declarators(0)
                    Return declaration.ReplaceNode(d2, d2.WithInitializer(ev))
                Case Else
                    Return declaration
            End Select
        End Function

        Public Overrides Function GetMembers(declaration As SyntaxNode) As IReadOnlyList(Of SyntaxNode)
            Select Case declaration.VBKind
                Case SyntaxKind.CompilationUnit
                    Dim cu = DirectCast(declaration, CompilationUnitSyntax)
                    Return cu.Imports.Cast(Of SyntaxNode).Concat(cu.Members.Cast(Of SyntaxNode)).ToImmutableReadOnlyListOrEmpty()
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

        Public Overrides Function WithMembers(declaration As SyntaxNode, declarations As IEnumerable(Of SyntaxNode)) As SyntaxNode
            Return PreserveTrivia(declaration, Function(d) WithMembersInternal(d, declarations))
        End Function

        Private Function WithMembersInternal(declaration As SyntaxNode, declarations As IEnumerable(Of SyntaxNode)) As SyntaxNode
            Select Case declaration.VBKind
                Case SyntaxKind.CompilationUnit
                    Return DirectCast(declaration, CompilationUnitSyntax).WithImports(GetImports(declarations)).WithMembers(GetNamespaceMembers(declarations))
                Case SyntaxKind.NamespaceBlock
                    Return DirectCast(declaration, NamespaceBlockSyntax).WithMembers(GetNamespaceMembers(declarations))
                Case SyntaxKind.ClassBlock
                    Return DirectCast(declaration, ClassBlockSyntax).WithMembers(AsClassMembers(declarations))
                Case SyntaxKind.StructureBlock
                    Return DirectCast(declaration, StructureBlockSyntax).WithMembers(AsStructureMembers(declarations))
                Case SyntaxKind.InterfaceBlock
                    Return DirectCast(declaration, InterfaceBlockSyntax).WithMembers(AsInterfaceMembers(declarations))
                Case SyntaxKind.EnumBlock
                    Return DirectCast(declaration, EnumBlockSyntax).WithMembers(AsEnumMembers(declarations))
                Case Else
                    Return declaration
            End Select
        End Function

        Public Overrides Function AddMembers(declaration As SyntaxNode, declarations As IEnumerable(Of SyntaxNode)) As SyntaxNode
            Return PreserveTrivia(declaration, Function(d) AddMembersInternal(d, declarations))
        End Function

        Private Function AddMembersInternal(declaration As SyntaxNode, declarations As IEnumerable(Of SyntaxNode)) As SyntaxNode
            Select Case declaration.VBKind
                Case SyntaxKind.CompilationUnit
                    Dim cu = DirectCast(declaration, CompilationUnitSyntax)
                    Return cu.WithImports(cu.Imports.AddRange(GetImports(declarations))).WithMembers(cu.Members.AddRange(GetNamespaceMembers(declarations)))
                Case SyntaxKind.NamespaceBlock
                    Dim ns = DirectCast(declaration, NamespaceBlockSyntax)
                    Return ns.WithMembers(ns.Members.AddRange(GetNamespaceMembers(declarations)))
                Case SyntaxKind.ClassBlock
                    Dim cb = DirectCast(declaration, ClassBlockSyntax)
                    Return cb.WithMembers(cb.Members.AddRange(AsClassMembers(declarations)))
                Case SyntaxKind.StructureBlock
                    Dim sb = DirectCast(declaration, StructureBlockSyntax)
                    Return sb.WithMembers(sb.Members.AddRange(AsStructureMembers(declarations)))
                Case SyntaxKind.InterfaceBlock
                    Dim ib = DirectCast(declaration, InterfaceBlockSyntax)
                    Return ib.WithMembers(ib.Members.AddRange(AsInterfaceMembers(declarations)))
                Case SyntaxKind.EnumBlock
                    Dim eb = DirectCast(declaration, EnumBlockSyntax)
                    Return eb.WithMembers(eb.Members.AddRange(AsEnumMembers(declarations)))
                Case Else
                    Return declaration
            End Select
        End Function

        Public Overrides Function GetStatements(declaration As SyntaxNode) As IReadOnlyList(Of SyntaxNode)
            Select Case declaration.VBKind
                Case SyntaxKind.FunctionBlock,
                     SyntaxKind.SubBlock,
                     SyntaxKind.ConstructorBlock,
                     SyntaxKind.OperatorBlock
                    Return DirectCast(declaration, MethodBlockBaseSyntax).Statements
                Case SyntaxKind.MultiLineFunctionLambdaExpression,
                     SyntaxKind.MultiLineSubLambdaExpression
                    Return DirectCast(declaration, MultiLineLambdaExpressionSyntax).Statements
                Case Else
                    Return SpecializedCollections.EmptyReadOnlyList(Of SyntaxNode)
            End Select
        End Function

        Public Overrides Function WithStatements(declaration As SyntaxNode, statements As IEnumerable(Of SyntaxNode)) As SyntaxNode
            Return PreserveTrivia(declaration, Function(d) WithStatementsInternal(d, statements))
        End Function

        Private Function WithStatementsInternal(declaration As SyntaxNode, statements As IEnumerable(Of SyntaxNode)) As SyntaxNode
            Dim list = GetStatementList(statements)
            Select Case declaration.VBKind
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
                Case Else
                    Return declaration
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
            Select Case declaration.VBKind
                Case SyntaxKind.PropertyBlock
                    Dim accessor = DirectCast(declaration, PropertyBlockSyntax).Accessors.FirstOrDefault(Function(a) a.IsKind(kind))
                    If accessor IsNot Nothing Then
                        Return accessor.Statements
                    End If
                Case SyntaxKind.EventBlock
                    Dim accessor = DirectCast(declaration, EventBlockSyntax).Accessors.FirstOrDefault(Function(a) a.IsKind(kind))
                    If accessor IsNot Nothing Then
                        Return accessor.Statements
                    End If
            End Select
            Return SpecializedCollections.EmptyReadOnlyList(Of SyntaxNode)()
        End Function

        Private Function WithAccessorStatements(declaration As SyntaxNode, statements As IEnumerable(Of SyntaxNode), kind As SyntaxKind) As SyntaxNode
            Select Case declaration.VBKind
                Case SyntaxKind.PropertyBlock
                    Dim pb = DirectCast(declaration, PropertyBlockSyntax)
                    Dim accessor = AccessorBlock(kind, statements, pb.PropertyStatement.AsClause?.Type)
                    Return pb.WithAccessors(SyntaxFactory.List(WithAccessorBlock(pb.Accessors, accessor)))
                Case SyntaxKind.PropertyBlock
                    Dim eb = DirectCast(declaration, EventBlockSyntax)
                    Dim accessor = AccessorBlock(kind, statements, eb.EventStatement.AsClause?.Type)
                    Return eb.WithAccessors(SyntaxFactory.List(WithAccessorBlock(eb.Accessors, accessor)))
                Case Else
                    Return declaration
            End Select
        End Function

        Private Function WithAccessorBlock(accessors As SyntaxList(Of AccessorBlockSyntax), accessor As AccessorBlockSyntax) As SyntaxList(Of AccessorBlockSyntax)
            Dim currentAccessor = accessors.FirstOrDefault(Function(a) a.IsKind(accessor.VBKind))
            If currentAccessor IsNot Nothing Then
                Return accessors.Replace(currentAccessor, currentAccessor.WithStatements(accessor.Statements))
            Else
                Return accessors.Add(accessor)
            End If
        End Function

        Public Overrides Function EventDeclaration(name As String, type As SyntaxNode, Optional accessibility As Accessibility = Accessibility.NotApplicable, Optional modifiers As DeclarationModifiers = Nothing) As SyntaxNode
            Return SyntaxFactory.EventStatement(
                attributeLists:=Nothing,
                modifiers:=GetModifierList(accessibility, modifiers, SyntaxKind.EventStatement),
                customKeyword:=Nothing,
                keyword:=SyntaxFactory.Token(SyntaxKind.EventKeyword),
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
                modifiers:=GetModifierList(accessibility, modifiers, SyntaxKind.EventStatement),
                customKeyword:=SyntaxFactory.Token(SyntaxKind.CustomKeyword),
                keyword:=SyntaxFactory.Token(SyntaxKind.EventKeyword),
                identifier:=name.ToIdentifierToken(),
                parameterList:=Nothing,
                asClause:=SyntaxFactory.SimpleAsClause(DirectCast(type, TypeSyntax)),
                implementsClause:=Nothing)

            Return SyntaxFactory.EventBlock(
                eventStatement:=evStatement,
                accessors:=SyntaxFactory.List(accessors),
                endEventStatement:=SyntaxFactory.EndEventStatement())
        End Function
    End Class
End Namespace