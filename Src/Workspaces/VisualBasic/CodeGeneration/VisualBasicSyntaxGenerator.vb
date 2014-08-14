' Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Linq
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.CodeGeneration
Imports Microsoft.CodeAnalysis.Simplification
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Microsoft.CodeAnalysis.Host
Imports Microsoft.CodeAnalysis.Host.Mef

Namespace Microsoft.CodeAnalysis.VisualBasic.CodeGeneration
    <ExportLanguageService(GetType(SyntaxGenerator), LanguageNames.VisualBasic)>
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
                Return SyntaxFactory.NamedArgument(name.ToIdentifierName, DirectCast(expression, ExpressionSyntax))
            End If
        End Function

        Public Overrides Function AsExpression(expression As SyntaxNode, type As SyntaxNode) As SyntaxNode
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

            Dim ifPart = SyntaxFactory.IfPart(
                    SyntaxFactory.IfStatement(SyntaxKind.IfStatement, Nothing, SyntaxFactory.Token(SyntaxKind.IfKeyword), DirectCast(condition, ExpressionSyntax), SyntaxFactory.Token(SyntaxKind.ThenKeyword)),
                    GetStatements(trueStatements))

            If falseStatements Is Nothing Then
                Return SyntaxFactory.MultiLineIfBlock(ifPart, Nothing, Nothing)
            End If

            ' convert nested if-blocks into else-if parts
            Dim statements = falseStatements.ToList()
            If (statements.Count = 1 AndAlso TypeOf statements(0) Is MultiLineIfBlockSyntax) Then
                Dim mifBlock = DirectCast(statements(0), MultiLineIfBlockSyntax)

                ' insert block's if-part onto head of elseIf-parts
                Dim elseIfParts = mifBlock.ElseIfParts.Insert(0,
                    SyntaxFactory.IfPart(
                        SyntaxFactory.IfStatement(SyntaxKind.ElseIfStatement, Nothing, SyntaxFactory.Token(SyntaxKind.ElseIfKeyword), mifBlock.IfPart.Begin.Condition, SyntaxFactory.Token(SyntaxKind.ThenKeyword)),
                        mifBlock.IfPart.Statements))

                Return SyntaxFactory.MultiLineIfBlock(ifPart, elseIfParts, mifBlock.ElsePart)
            End If

            Return SyntaxFactory.MultiLineIfBlock(ifPart, Nothing, SyntaxFactory.ElsePart(GetStatements(statements)))
        End Function

        Private Function GetStatements(nodes As IEnumerable(Of SyntaxNode)) As SyntaxList(Of StatementSyntax)
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

        Public Overrides Function IsExpression(expression As SyntaxNode, type As SyntaxNode) As SyntaxNode
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
                GetStatements(statements))
        End Function

        Public Overloads Overrides Function UsingStatement(expression As SyntaxNode, statements As IEnumerable(Of SyntaxNode)) As SyntaxNode
            Return SyntaxFactory.UsingBlock(
                SyntaxFactory.UsingStatement(
                    expression:=DirectCast(expression, ExpressionSyntax),
                    variables:=Nothing),
                GetStatements(statements))
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
                GetStatements(statements))
        End Function

        Public Overrides Function DefaultSwitchSection(statements As IEnumerable(Of SyntaxNode)) As SyntaxNode
            Return SyntaxFactory.CaseBlock(
                SyntaxFactory.CaseStatement(SyntaxFactory.ElseCaseClause()),
                GetStatements(statements))
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
                SyntaxFactory.FunctionLambdaHeader().WithParameterList(SyntaxFactory.ParameterList(SyntaxFactory.SeparatedList(parameters.Cast(Of ParameterSyntax)()))),
                DirectCast(expression, ExpressionSyntax))
        End Function

        Public Overrides Function VoidReturningLambdaExpression(lambdaParameters As IEnumerable(Of SyntaxNode), expression As SyntaxNode) As SyntaxNode
            Return SyntaxFactory.SingleLineSubLambdaExpression(
                    SyntaxFactory.SubLambdaHeader().WithParameterList(SyntaxFactory.ParameterList(SyntaxFactory.SeparatedList(lambdaParameters.Cast(Of ParameterSyntax)()))),
                    AsStatement(expression))
        End Function

        Public Overloads Overrides Function ValueReturningLambdaExpression(lambdaParameters As IEnumerable(Of SyntaxNode), statements As IEnumerable(Of SyntaxNode)) As SyntaxNode
            Return SyntaxFactory.MultiLineFunctionLambdaExpression(
                SyntaxFactory.FunctionLambdaHeader().WithParameterList(SyntaxFactory.ParameterList(SyntaxFactory.SeparatedList(lambdaParameters.Cast(Of ParameterSyntax)()))),
                GetStatements(statements),
                SyntaxFactory.EndFunctionStatement())
        End Function

        Public Overrides Function VoidReturningLambdaExpression(lambdaParameters As IEnumerable(Of SyntaxNode), statements As IEnumerable(Of SyntaxNode)) As SyntaxNode
            Return SyntaxFactory.MultiLineSubLambdaExpression(
                        SyntaxFactory.SubLambdaHeader().WithParameterList(SyntaxFactory.ParameterList(SyntaxFactory.SeparatedList(lambdaParameters.Cast(Of ParameterSyntax)()))),
                        GetStatements(statements),
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

        Public Overrides Function FieldDeclaration(name As String, type As SyntaxNode, Optional accessibility As Accessibility = Nothing, Optional modifiers As SymbolModifiers = Nothing, Optional initializer As SyntaxNode = Nothing) As SyntaxNode
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
            Optional modifiers As SymbolModifiers = Nothing,
            Optional statements As IEnumerable(Of SyntaxNode) = Nothing) As SyntaxNode

            Dim statement = SyntaxFactory.MethodStatement(
                kind:=If(returnType Is Nothing, SyntaxKind.SubStatement, SyntaxKind.FunctionStatement),
                attributeLists:=Nothing,
                modifiers:=GetModifierList(accessibility, modifiers And methodModifiers),
                keyword:=If(returnType Is Nothing, SyntaxFactory.Token(SyntaxKind.SubKeyword), SyntaxFactory.Token(SyntaxKind.FunctionKeyword)),
                identifier:=SyntaxFactory.Identifier(identifier),
                typeParameterList:=GetTypeParameters(typeParameters),
                parameterList:=GetParameters(parameters),
                asClause:=If(returnType IsNot Nothing, SyntaxFactory.SimpleAsClause(DirectCast(returnType, TypeSyntax)), Nothing),
                handlesClause:=Nothing,
                implementsClause:=Nothing)

            If modifiers.IsAbstract Then
                Return statement
            Else
                Return SyntaxFactory.MethodBlock(
                    kind:=If(returnType Is Nothing, SyntaxKind.SubBlock, SyntaxKind.FunctionBlock),
                    begin:=statement,
                    statements:=GetStatements(statements),
                    [end]:=If(returnType Is Nothing, SyntaxFactory.EndSubStatement(), SyntaxFactory.EndFunctionStatement()))
            End If
        End Function

        Private Function GetParameters(parameters As IEnumerable(Of SyntaxNode)) As ParameterListSyntax
            Return If(parameters IsNot Nothing, SyntaxFactory.ParameterList(SyntaxFactory.SeparatedList(parameters.Cast(Of ParameterSyntax)())), SyntaxFactory.ParameterList())
        End Function

        Public Overrides Function ParameterDeclaration(name As String, Optional type As SyntaxNode = Nothing, Optional initializer As SyntaxNode = Nothing, Optional refKind As RefKind = Nothing) As SyntaxNode
            Return SyntaxFactory.Parameter(
                attributeLists:=Nothing,
                modifiers:=GetParameterModifiers(refKind, initializer),
                identifier:=SyntaxFactory.ModifiedIdentifier(name),
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
            Optional modifiers As SymbolModifiers = Nothing,
            Optional getterStatements As IEnumerable(Of SyntaxNode) = Nothing,
            Optional setterStatements As IEnumerable(Of SyntaxNode) = Nothing) As SyntaxNode

            Dim asClause = SyntaxFactory.SimpleAsClause(DirectCast(type, TypeSyntax))
            Dim statement = SyntaxFactory.PropertyStatement(
                attributeLists:=Nothing,
                modifiers:=GetModifierList(accessibility, modifiers And propertyModifiers),
                identifier:=SyntaxFactory.Identifier(identifier),
                parameterList:=Nothing,
                asClause:=asClause,
                initializer:=Nothing,
                implementsClause:=Nothing)

            If modifiers.IsAbstract Then
                Return statement
            Else
                Dim accessors = New List(Of AccessorBlockSyntax)

                accessors.Add(GetAccessorBlock(getterStatements))

                If Not modifiers.IsReadOnly Then
                    accessors.Add(SetAccessorBlock(setterStatements, type))
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
            Optional modifiers As SymbolModifiers = Nothing,
            Optional getterStatements As IEnumerable(Of SyntaxNode) = Nothing,
            Optional setterStatements As IEnumerable(Of SyntaxNode) = Nothing) As SyntaxNode

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

                accessors.Add(GetAccessorBlock(getterStatements))

                If Not modifiers.IsReadOnly Then
                    accessors.Add(SetAccessorBlock(setterStatements, type))
                End If

                Return SyntaxFactory.PropertyBlock(
                    propertyStatement:=statement,
                    accessors:=SyntaxFactory.List(accessors),
                    endPropertyStatement:=SyntaxFactory.EndPropertyStatement())
            End If
        End Function

        Private Function GetAccessorBlock(statements As IEnumerable(Of SyntaxNode)) As AccessorBlockSyntax
            Return SyntaxFactory.AccessorBlock(
                SyntaxKind.PropertyGetBlock,
                SyntaxFactory.AccessorStatement(SyntaxKind.GetAccessorStatement, SyntaxFactory.Token(SyntaxKind.GetKeyword)),
                GetStatements(statements),
                SyntaxFactory.EndBlockStatement(SyntaxKind.EndGetStatement, SyntaxFactory.Token(SyntaxKind.GetKeyword)))
        End Function

        Private Function SetAccessorBlock(statements As IEnumerable(Of SyntaxNode), type As SyntaxNode) As AccessorBlockSyntax
            Dim asClause = SyntaxFactory.SimpleAsClause(DirectCast(type, TypeSyntax))

            Dim setParameter = SyntaxFactory.Parameter(
                        attributeLists:=Nothing,
                        modifiers:=Nothing,
                        identifier:=SyntaxFactory.ModifiedIdentifier("value"),
                        asClause:=asClause,
                        [default]:=Nothing)

            Return SyntaxFactory.AccessorBlock(
                SyntaxKind.PropertySetBlock,
                SyntaxFactory.AccessorStatement(
                    kind:=SyntaxKind.SetAccessorStatement,
                    attributeLists:=Nothing,
                    modifiers:=Nothing,
                    keyword:=SyntaxFactory.Token(SyntaxKind.SetKeyword),
                    parameterList:=SyntaxFactory.ParameterList(SyntaxFactory.SingletonSeparatedList(setParameter))),
                GetStatements(statements),
                SyntaxFactory.EndBlockStatement(SyntaxKind.EndSetStatement, SyntaxFactory.Token(SyntaxKind.SetKeyword)))
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
                    prop.PropertyStatement.WithIdentifier(SyntaxFactory.Identifier(GetNameAsIdentifier(typeName) & "_" & GetNameAsIdentifier(memberName))))

                Return prop.WithPropertyStatement(
                    prop.PropertyStatement.WithImplementsClause(
                        SyntaxFactory.ImplementsClause(SyntaxFactory.QualifiedName(type, interfaceMemberName))))
            End If

            Return declaration
        End Function

        Private Function GetNameAsIdentifier(type As SyntaxNode) As String
            Dim name = TryCast(type, IdentifierNameSyntax)
            If name IsNot Nothing Then
                Return name.Identifier.ToString()
            End If

            Dim gname = TryCast(type, GenericNameSyntax)
            If gname IsNot Nothing Then
                Return gname.Identifier.ToString() & "_" & gname.TypeArgumentList.Arguments.Select(Function(t) GetNameAsIdentifier(t)).Aggregate(Function(a, b) a & "_" & b)
            End If

            Dim qname = TryCast(type, QualifiedNameSyntax)
            If qname IsNot Nothing Then
                Return GetNameAsIdentifier(qname.Right)
            End If

            Return "[" & type.ToString() & "]"
        End Function

        Private Function AsImplementation(declaration As SyntaxNode, requiredAccess As Accessibility, allowDefault As Boolean) As SyntaxNode

            Dim access As Accessibility
            Dim modifiers As SymbolModifiers
            Dim isDefault As Boolean

            Dim method = TryCast(declaration, MethodStatementSyntax)
            If method IsNot Nothing Then
                Me.GetAccessibilityAndModifiers(method.Modifiers, access, modifiers, isDefault)
                If modifiers.IsAbstract OrElse access <> requiredAccess Then
                    method = method.WithModifiers(GetModifierList(requiredAccess, modifiers - SymbolModifiers.Abstract, False))
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
                    prop = prop.WithModifiers(GetModifierList(requiredAccess, modifiers - SymbolModifiers.Abstract, isDefault And allowDefault))
                End If

                Dim accessors = New List(Of AccessorBlockSyntax)
                accessors.Add(GetAccessorBlock(Nothing))

                If (Not prop.Modifiers.Any(SyntaxKind.ReadOnlyKeyword)) Then
                    accessors.Add(SetAccessorBlock(Nothing, prop.AsClause.Type))
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
            Optional modifiers As SymbolModifiers = Nothing,
            Optional baseConstructorArguments As IEnumerable(Of SyntaxNode) = Nothing,
            Optional statements As IEnumerable(Of SyntaxNode) = Nothing) As SyntaxNode

            Dim stats = GetStatements(statements)

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
            Optional modifiers As SymbolModifiers = Nothing,
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
                    modifiers:=GetModifierList(accessibility, modifiers And typeModifiers),
                    identifier:=SyntaxFactory.Identifier(name),
                    typeParameterList:=GetTypeParameters(typeParameters)),
                    [inherits]:=If(baseType IsNot Nothing, SyntaxFactory.SingletonList(SyntaxFactory.InheritsStatement(DirectCast(baseType, TypeSyntax))), Nothing),
                    [implements]:=If(itypes IsNot Nothing, SyntaxFactory.SingletonList(SyntaxFactory.ImplementsStatement(SyntaxFactory.SeparatedList(itypes))), Nothing),
                    members:=If(members IsNot Nothing, SyntaxFactory.List(members.Cast(Of StatementSyntax)()), Nothing))
        End Function

        Public Overrides Function StructDeclaration(
            name As String,
            Optional typeParameters As IEnumerable(Of String) = Nothing,
            Optional accessibility As Accessibility = Nothing,
            Optional modifiers As SymbolModifiers = Nothing,
            Optional interfaceTypes As IEnumerable(Of SyntaxNode) = Nothing,
            Optional members As IEnumerable(Of SyntaxNode) = Nothing) As SyntaxNode

            Dim itypes = If(interfaceTypes IsNot Nothing, interfaceTypes.Cast(Of TypeSyntax), Nothing)
            If itypes IsNot Nothing AndAlso itypes.Count = 0 Then
                itypes = Nothing
            End If

            Return SyntaxFactory.StructureBlock(
                begin:=SyntaxFactory.StructureStatement(
                    attributeLists:=Nothing,
                    modifiers:=GetModifierList(accessibility, modifiers And typeModifiers),
                    identifier:=SyntaxFactory.Identifier(name),
                    typeParameterList:=GetTypeParameters(typeParameters)),
                [inherits]:=Nothing,
                [implements]:=If(itypes IsNot Nothing, SyntaxFactory.SingletonList(SyntaxFactory.ImplementsStatement(SyntaxFactory.SeparatedList(itypes))), Nothing),
                members:=If(members IsNot Nothing, SyntaxFactory.List(members.Cast(Of StatementSyntax)()), Nothing))
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
                    modifiers:=GetModifierList(accessibility, SymbolModifiers.None),
                    identifier:=SyntaxFactory.Identifier(name),
                    typeParameterList:=GetTypeParameters(typeParameters)),
                [inherits]:=If(itypes IsNot Nothing, SyntaxFactory.SingletonList(SyntaxFactory.InheritsStatement(SyntaxFactory.SeparatedList(itypes))), Nothing),
                [implements]:=Nothing,
                members:=If(members IsNot Nothing, SyntaxFactory.List(members.Select(AddressOf AsInterfaceMember)), Nothing))
        End Function

        Private Function AsInterfaceMember(node As SyntaxNode) As StatementSyntax
            Dim methodBlock = TryCast(node, MethodBlockSyntax)
            If methodBlock IsNot Nothing Then
                node = methodBlock.Begin
            End If

            Dim methodStatement = TryCast(node, MethodStatementSyntax)
            If methodStatement IsNot Nothing Then
                Return methodStatement.WithModifiers(Nothing)
            End If

            Dim propertyBlock = TryCast(node, PropertyBlockSyntax)
            If propertyBlock IsNot Nothing Then
                node = propertyBlock.PropertyStatement
            End If

            Dim propertyStatement = TryCast(node, PropertyStatementSyntax)
            If propertyStatement IsNot Nothing Then
                Dim mods = SyntaxFactory.TokenList(propertyBlock.PropertyStatement.Modifiers.Where(Function(tk) tk.IsKind(SyntaxKind.ReadOnlyKeyword) Or tk.IsKind(SyntaxKind.DefaultKeyword)))
                Return propertyStatement.WithModifiers(mods)
            End If

            Throw New ArgumentException("Declaration is not a valid interface member.")
        End Function

        Public Overrides Function EnumDeclaration(
            identifier As String,
            Optional accessibility As Accessibility = Nothing,
            Optional members As IEnumerable(Of SyntaxNode) = Nothing) As SyntaxNode

            Return SyntaxFactory.EnumBlock(
                enumStatement:=SyntaxFactory.EnumStatement(
                    attributeLists:=Nothing,
                    modifiers:=GetModifierList(accessibility, SymbolModifiers.None),
                    identifier:=SyntaxFactory.Identifier(identifier),
                    underlyingType:=Nothing),
                    members:=If(members IsNot Nothing, SyntaxFactory.List(members.Select(AddressOf AsEnumMember)), Nothing))
        End Function

        Public Overrides Function EnumMember(identifier As String, Optional expression As SyntaxNode = Nothing) As SyntaxNode
            Return SyntaxFactory.EnumMemberDeclaration(
                attributeLists:=Nothing,
                identifier:=SyntaxFactory.Identifier(identifier),
                initializer:=If(expression IsNot Nothing, SyntaxFactory.EqualsValue(DirectCast(expression, ExpressionSyntax)), Nothing))
        End Function

        Private Function AsEnumMember(node As SyntaxNode) As StatementSyntax
            Dim id = TryCast(node, IdentifierNameSyntax)
            If id IsNot Nothing Then
                Return DirectCast(EnumMember(id.Identifier.ToString()), EnumMemberDeclarationSyntax)
            End If

            Return DirectCast(node, EnumMemberDeclarationSyntax)
        End Function

        Public Overrides Function CompilationUnit(Optional declarations As IEnumerable(Of SyntaxNode) = Nothing) As SyntaxNode
            If declarations IsNot Nothing Then
                Dim [imports] = declarations.OfType(Of ImportsStatementSyntax)()
                Dim members = declarations.OfType(Of StatementSyntax).Where(Function(s) Not TypeOf s Is ImportsStatementSyntax)
                Return SyntaxFactory.CompilationUnit().WithImports(SyntaxFactory.List([imports])).WithMembers(SyntaxFactory.List(members))
            Else
                Return SyntaxFactory.CompilationUnit()
            End If
        End Function

        Public Overrides Function NamespaceImportDeclaration(name As SyntaxNode) As SyntaxNode
            Return SyntaxFactory.ImportsStatement(SyntaxFactory.SingletonSeparatedList(Of ImportsClauseSyntax)(SyntaxFactory.MembersImportsClause(DirectCast(name, NameSyntax))))
        End Function

        Public Overrides Function NamespaceDeclaration(name As SyntaxNode, nestedDeclarations As IEnumerable(Of SyntaxNode)) As SyntaxNode
            ' put imports at start
            Dim imps = nestedDeclarations.Where(Function(nd) TypeOf nd Is ImportsStatementSyntax).Concat(
                            nestedDeclarations.Where(Function(nd) TypeOf nd IsNot ImportsStatementSyntax))

            Return SyntaxFactory.NamespaceBlock(
                SyntaxFactory.NamespaceStatement(DirectCast(name, NameSyntax)),
                members:=If(nestedDeclarations IsNot Nothing, SyntaxFactory.List(imps.Cast(Of StatementSyntax)), Nothing))
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

        Public Overrides Function AddAttributes(declaration As SyntaxNode, attributes As IEnumerable(Of SyntaxNode)) As SyntaxNode
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

            Throw New ArgumentException("declaration")
        End Function

        Private Overloads Function WithAssemblyTargets(attrs As AttributeListSyntax) As AttributeListSyntax
            Return attrs.WithAttributes(SyntaxFactory.SeparatedList(attrs.Attributes.Select(AddressOf WithAssemblyTarget)))
        End Function

        Private Overloads Function WithAssemblyTarget(attr As AttributeSyntax) As AttributeSyntax
            Return attr.WithTarget(SyntaxFactory.AttributeTarget(SyntaxFactory.Token(SyntaxKind.AssemblyKeyword)))
        End Function

        Public Overrides Function AddReturnAttributes(methodDeclaration As SyntaxNode, attributes As IEnumerable(Of SyntaxNode)) As SyntaxNode
            Dim lists = GetAttributeLists(attributes)

            Dim methodBlock = TryCast(methodDeclaration, MethodBlockSyntax)
            If (methodBlock IsNot Nothing) Then
                Return methodBlock.WithBegin(methodBlock.Begin.WithAsClause(methodBlock.Begin.AsClause.WithAttributeLists(methodBlock.Begin.AttributeLists.AddRange(lists))))
            End If

            Dim method = TryCast(methodDeclaration, MethodStatementSyntax)
            If method IsNot Nothing Then
                Return method.WithAsClause(method.AsClause.WithAttributeLists(method.AttributeLists.AddRange(lists)))
            End If

            Throw New ArgumentException("methodDeclaration")
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
                Return SyntaxFactory.AttributeList(SyntaxFactory.SingletonSeparatedList(attr))
            Else
                Return DirectCast(node, AttributeListSyntax)
            End If
        End Function

        Private Function GetModifierList(accessibility As Accessibility, modifiers As SymbolModifiers, Optional isDefault As Boolean = False, Optional isField As Boolean = False) As SyntaxTokenList
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

        Private Sub GetAccessibilityAndModifiers(modifierTokens As SyntaxTokenList, ByRef accessibility As Accessibility, ByRef modifiers As SymbolModifiers, ByRef isDefault As Boolean)
            accessibility = Accessibility.NotApplicable
            modifiers = SymbolModifiers.None
            isDefault = False

            For Each token In modifierTokens
                Select Case token.VisualBasicKind
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
                        modifiers = modifiers Or SymbolModifiers.Abstract
                    Case SyntaxKind.ShadowsKeyword
                        modifiers = modifiers Or SymbolModifiers.[New]
                    Case SyntaxKind.OverridesKeyword
                        modifiers = modifiers Or SymbolModifiers.Override
                    Case SyntaxKind.OverridableKeyword
                        modifiers = modifiers Or SymbolModifiers.Virtual
                    Case SyntaxKind.SharedKeyword
                        modifiers = modifiers Or SymbolModifiers.Static
                    Case SyntaxKind.AsyncKeyword
                        modifiers = modifiers Or SymbolModifiers.Async
                    Case SyntaxKind.ConstKeyword
                        modifiers = modifiers Or SymbolModifiers.Const
                    Case SyntaxKind.ReadOnlyKeyword
                        modifiers = modifiers Or SymbolModifiers.ReadOnly
                    Case SyntaxKind.NotInheritableKeyword
                        modifiers = modifiers Or SymbolModifiers.Sealed
                    Case SyntaxKind.WithEventsKeyword
                        modifiers = modifiers Or SymbolModifiers.WithEvents
                    Case SyntaxKind.PartialKeyword
                        modifiers = modifiers Or SymbolModifiers.Partial
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
                Return SyntaxFactory.MemberAccessExpression(name.VisualBasicKind(), sma.Expression, sma.OperatorToken, DirectCast(WithTypeArguments(sma.Name, typeArguments), SimpleNameSyntax))
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
                SyntaxFactory.TryPart(GetStatements(tryStatements)),
                If(catchClauses IsNot Nothing, SyntaxFactory.List(catchClauses.Cast(Of CatchPartSyntax)()), Nothing),
                If(finallyStatements IsNot Nothing, SyntaxFactory.FinallyPart(GetStatements(finallyStatements)), Nothing))
        End Function

        Public Overrides Function CatchClause(type As SyntaxNode, identifier As String, statements As IEnumerable(Of SyntaxNode)) As SyntaxNode
            Return SyntaxFactory.CatchPart(
                SyntaxFactory.CatchStatement(
                    SyntaxFactory.IdentifierName(identifier),
                    SyntaxFactory.SimpleAsClause(DirectCast(type, TypeSyntax)),
                    whenClause:=Nothing),
                GetStatements(statements))
        End Function

        Public Overrides Function WhileStatement(condition As SyntaxNode, statements As IEnumerable(Of SyntaxNode)) As SyntaxNode
            Return SyntaxFactory.WhileBlock(
                SyntaxFactory.WhileStatement(DirectCast(condition, ExpressionSyntax)),
                GetStatements(statements))
        End Function
    End Class
End Namespace