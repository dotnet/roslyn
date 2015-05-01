' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Immutable
Imports System.Threading
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic

    Partial Friend Class VisualBasicTypeInferenceService
        Private Class TypeInferrer
            Private ReadOnly _semanticModel As SemanticModel
            Private ReadOnly _cancellationToken As CancellationToken

            Public Sub New(
                semanticModel As SemanticModel,
                cancellationToken As CancellationToken)

                _semanticModel = semanticModel
                _cancellationToken = cancellationToken
            End Sub

            Private ReadOnly Property Compilation As Compilation
                Get
                    Return Me._semanticModel.Compilation
                End Get
            End Property

            Public Function InferTypes(expression As ExpressionSyntax) As IEnumerable(Of ITypeSymbol)
                If expression Is Nothing Then
                    Return Nothing
                End If

                Return InferTypesWorker(expression)
            End Function

            Public Function InferTypes(position As Integer) As IEnumerable(Of ITypeSymbol)
                Return InferTypesWorker(position)
            End Function

            Private Shared Function IsUnusableType(otherSideType As ITypeSymbol) As Boolean
                If otherSideType Is Nothing Then
                    Return True
                End If

                Return otherSideType.IsErrorType() AndAlso
                    otherSideType.Name = String.Empty
            End Function

            Private Overloads Function GetTypes(expression As ExpressionSyntax, Optional objectAsDefault As Boolean = False) As IEnumerable(Of ITypeSymbol)
                If expression IsNot Nothing Then
                    Dim info = _semanticModel.GetTypeInfo(expression)
                    If info.Type IsNot Nothing AndAlso info.Type.TypeKind <> TypeKind.Error Then
                        Return SpecializedCollections.SingletonEnumerable(info.Type)
                    End If

                    If info.ConvertedType IsNot Nothing AndAlso info.ConvertedType.TypeKind <> TypeKind.Error Then
                        Return SpecializedCollections.SingletonEnumerable(info.ConvertedType)
                    End If

                    If expression.Kind = SyntaxKind.AddressOfExpression Then
                        Dim unaryExpression = DirectCast(expression, UnaryExpressionSyntax)
                        Dim symbol = _semanticModel.GetSymbolInfo(unaryExpression.Operand, _cancellationToken).GetAnySymbol()
                        Dim type = symbol.ConvertToType(Me.Compilation)
                        If type IsNot Nothing Then
                            Return SpecializedCollections.SingletonEnumerable(type)
                        End If
                    End If
                End If

                Return If(objectAsDefault, SpecializedCollections.SingletonEnumerable(Me.Compilation.ObjectType), SpecializedCollections.EmptyEnumerable(Of ITypeSymbol)())
            End Function

            Private Function InferTypesWorker(expression As ExpressionSyntax) As IEnumerable(Of ITypeSymbol)
                expression = expression.WalkUpParentheses()
                Dim parent = expression.Parent
                If TypeOf parent Is ConditionalAccessExpressionSyntax Then
                    parent = parent.Parent
                End If

                If TypeOf parent Is MemberAccessExpressionSyntax Then
                    Dim awaitExpression = parent.GetAncestor(Of AwaitExpressionSyntax)
                    Dim lambdaExpression = parent.GetAncestor(Of LambdaExpressionSyntax)
                    If Not awaitExpression?.Contains(lambdaExpression) AndAlso awaitExpression IsNot Nothing Then
                        parent = awaitExpression
                    End If
                End If

                Return parent.TypeSwitch(
                    Function(addRemoveHandlerStatement As AddRemoveHandlerStatementSyntax) InferTypeInAddRemoveHandlerStatementSyntax(addRemoveHandlerStatement, expression),
                    Function(argument As ArgumentSyntax) InferTypeInArgumentList(TryCast(argument.Parent, ArgumentListSyntax), argument),
                    Function(arrayCreationExpression As ArrayCreationExpressionSyntax) InferTypeInArrayCreationExpression(arrayCreationExpression),
                    Function(arrayRank As ArrayRankSpecifierSyntax) InferTypeInArrayRankSpecifier(),
                    Function(arrayType As ArrayTypeSyntax) InferTypeInArrayType(arrayType),
                    Function(asClause As AsClauseSyntax) InferTypeInAsClause(asClause, expression),
                    Function(assignmentStatement As AssignmentStatementSyntax) InferTypeInAssignmentStatement(assignmentStatement, expression),
                    Function(attribute As AttributeSyntax) InferTypeInAttribute(attribute),
                    Function(awaitExpression As AwaitExpressionSyntax) InferTypeInAwaitExpression(awaitExpression),
                    Function(binaryExpression As BinaryExpressionSyntax) InferTypeInBinaryExpression(binaryExpression, expression),
                    Function(castExpression As CastExpressionSyntax) InferTypeInCastExpression(castExpression, expression),
                    Function(catchFilterClause As CatchFilterClauseSyntax) InferTypeInCatchFilterClause(catchFilterClause),
                    Function(conditionalExpression As BinaryConditionalExpressionSyntax) InferTypeInBinaryConditionalExpression(conditionalExpression, expression),
                    Function(conditionalExpression As TernaryConditionalExpressionSyntax) InferTypeInTernaryConditionalExpression(conditionalExpression, expression),
                    Function(doStatement As DoStatementSyntax) InferTypeInDoStatement(),
                    Function(equalsValue As EqualsValueSyntax) InferTypeInEqualsValue(equalsValue),
                    Function(callStatement As CallStatementSyntax) InferTypeInCallStatement(),
                    Function(forEachStatement As ForEachStatementSyntax) InferTypeInForEachStatement(forEachStatement, expression),
                    Function(forStepClause As ForStepClauseSyntax) InferTypeInForStepClause(forStepClause),
                    Function(forStatement As ForStatementSyntax) InferTypeInForStatement(forStatement, expression),
                    Function(ifStatement As IfStatementSyntax) InferTypeInIfOrElseIfStatement(),
                    Function(ifStatement As ElseIfStatementSyntax) InferTypeInIfOrElseIfStatement(),
                    Function(namedFieldInitializer As NamedFieldInitializerSyntax) InferTypeInNamedFieldInitializer(namedFieldInitializer),
                    Function(singleLineLambdaExpression As SingleLineLambdaExpressionSyntax) InferTypeInLambda(singleLineLambdaExpression),
                    Function(parenthesizedLambda As MultiLineLambdaExpressionSyntax) InferTypeInLambda(parenthesizedLambda),
                    Function(prefixUnary As UnaryExpressionSyntax) InferTypeInUnaryExpression(prefixUnary),
                    Function(returnStatement As ReturnStatementSyntax) InferTypeForReturnStatement(returnStatement),
                    Function(switchStatement As SelectStatementSyntax) InferTypeInSelectStatement(switchStatement),
                    Function(throwStatement As ThrowStatementSyntax) InferTypeInThrowStatement(),
                    Function(typeOfExpression As TypeOfExpressionSyntax) InferTypeInTypeOfExpressionSyntax(typeOfExpression),
                    Function(usingStatement As UsingStatementSyntax) InferTypeInUsingStatement(usingStatement),
                    Function(whileStatement As WhileStatementSyntax) InferTypeInWhileStatement(),
                    Function(whileStatement As WhileOrUntilClauseSyntax) InferTypeInWhileOrUntilClause(),
                    Function(yieldStatement As YieldStatementSyntax) InferTypeInYieldStatement(yieldStatement),
                    Function(expressionStatement As ExpressionStatementSyntax) InferTypeInExpressionStatement(expressionStatement),
                    Function(x) SpecializedCollections.EmptyEnumerable(Of ITypeSymbol)())
            End Function

            Private Function InferTypeInTypeOfExpressionSyntax(typeOfExpression As TypeOfExpressionSyntax) As IEnumerable(Of ITypeSymbol)
                Dim expresionType = typeOfExpression.Type
                If expresionType Is Nothing Then
                    Return SpecializedCollections.EmptyEnumerable(Of ITypeSymbol)()
                End If

                Dim typeSymbol = _semanticModel.GetTypeInfo(expresionType).Type
                If TypeOf typeSymbol IsNot INamedTypeSymbol Then
                    Return SpecializedCollections.EmptyEnumerable(Of ITypeSymbol)()
                End If

                Return SpecializedCollections.SingletonEnumerable(typeSymbol)
            End Function

            Private Function InferTypeInAddRemoveHandlerStatementSyntax(addRemoveHandlerStatement As AddRemoveHandlerStatementSyntax,
                                                                        expression As ExpressionSyntax) As IEnumerable(Of ITypeSymbol)
                If expression Is addRemoveHandlerStatement.DelegateExpression Then
                    Return GetTypes(addRemoveHandlerStatement.EventExpression)
                End If

                Return SpecializedCollections.EmptyEnumerable(Of ITypeSymbol)()
            End Function

            Private Function InferTypesWorker(position As Integer) As IEnumerable(Of ITypeSymbol)
                Dim tree = TryCast(Me._semanticModel.SyntaxTree, SyntaxTree)
                Dim token = tree.FindTokenOnLeftOfPosition(position, _cancellationToken)
                token = token.GetPreviousTokenIfTouchingWord(position)

                Dim parent = token.Parent

                Return parent.TypeSwitch(
                    Function(nameColonEquals As NameColonEqualsSyntax) InferTypeInArgumentList(TryCast(nameColonEquals.Parent.Parent, ArgumentListSyntax), DirectCast(nameColonEquals.Parent, ArgumentSyntax)),
                    Function(argument As ArgumentSyntax) InferTypeInArgumentList(TryCast(argument.Parent, ArgumentListSyntax), previousToken:=token),
                    Function(argumentList As ArgumentListSyntax) InferTypeInArgumentList(argumentList, previousToken:=token),
                    Function(arrayCreationExpression As ArrayCreationExpressionSyntax) InferTypeInArrayCreationExpression(arrayCreationExpression),
                    Function(arrayRank As ArrayRankSpecifierSyntax) InferTypeInArrayRankSpecifier(),
                    Function(arrayType As ArrayTypeSyntax) InferTypeInArrayType(arrayType),
                    Function(asClause As AsClauseSyntax) InferTypeInAsClause(asClause, previousToken:=token),
                    Function(assignmentStatement As AssignmentStatementSyntax) InferTypeInAssignmentStatement(assignmentStatement, previousToken:=token),
                    Function(attribute As AttributeSyntax) InferTypeInAttribute(attribute),
                    Function(awaitExpression As AwaitExpressionSyntax) InferTypeInAwaitExpression(awaitExpression),
                    Function(binaryExpression As BinaryExpressionSyntax) InferTypeInBinaryExpression(binaryExpression, previousToken:=token),
                    Function(caseStatement As CaseStatementSyntax) InferTypeInCaseStatement(caseStatement),
                    Function(castExpression As CastExpressionSyntax) InferTypeInCastExpression(castExpression),
                    Function(catchFilterClause As CatchFilterClauseSyntax) InferTypeInCatchFilterClause(catchFilterClause, previousToken:=token),
                    Function(conditionalExpression As BinaryConditionalExpressionSyntax) InferTypeInBinaryConditionalExpression(conditionalExpression, previousToken:=token),
                    Function(conditionalExpression As TernaryConditionalExpressionSyntax) InferTypeInTernaryConditionalExpression(conditionalExpression, previousToken:=token),
                    Function(doStatement As DoStatementSyntax) InferTypeInDoStatement(token),
                    Function(equalsValue As EqualsValueSyntax) InferTypeInEqualsValue(equalsValue, token),
                    Function(callStatement As CallStatementSyntax) InferTypeInCallStatement(),
                    Function(forEachStatement As ForEachStatementSyntax) InferTypeInForEachStatement(forEachStatement, previousToken:=token),
                    Function(forStepClause As ForStepClauseSyntax) InferTypeInForStepClause(forStepClause, token),
                    Function(forStatement As ForStatementSyntax) InferTypeInForStatement(forStatement, previousToken:=token),
                    Function(ifStatement As IfStatementSyntax) InferTypeInIfOrElseIfStatement(token),
                    Function(namedFieldInitializer As NamedFieldInitializerSyntax) InferTypeInNamedFieldInitializer(namedFieldInitializer, token),
                    Function(singleLineLambdaExpression As SingleLineLambdaExpressionSyntax) InferTypeInLambda(singleLineLambdaExpression, token),
                    Function(parenthesizedLambda As MultiLineLambdaExpressionSyntax) InferTypeInLambda(parenthesizedLambda, token),
                    Function(prefixUnary As UnaryExpressionSyntax) InferTypeInUnaryExpression(prefixUnary, token),
                    Function(returnStatement As ReturnStatementSyntax) InferTypeForReturnStatement(returnStatement, token),
                    Function(switchStatement As SelectStatementSyntax) InferTypeInSelectStatement(switchStatement, token),
                    Function(throwStatement As ThrowStatementSyntax) InferTypeInThrowStatement(),
                    Function(usingStatement As UsingStatementSyntax) InferTypeInUsingStatement(usingStatement),
                    Function(whileStatement As WhileStatementSyntax) InferTypeInWhileStatement(),
                    Function(whileStatement As WhileOrUntilClauseSyntax) InferTypeInWhileOrUntilClause(),
                    Function(yieldStatement As YieldStatementSyntax) InferTypeInYieldStatement(yieldStatement, token),
                    Function(expressionStatement As ExpressionStatementSyntax) InferTypeInExpressionStatement(expressionStatement),
                    Function(parameterListSyntax As ParameterListSyntax) If(parameterListSyntax.Parent IsNot Nothing,
                                                                            InferTypeInLambda(TryCast(parameterListSyntax.Parent.Parent, LambdaExpressionSyntax)),
                                                                            SpecializedCollections.EmptyEnumerable(Of ITypeSymbol)()),
                    Function(x) SpecializedCollections.EmptyEnumerable(Of ITypeSymbol)())
            End Function

            Private Function InferTypeInArgumentList(argumentList As ArgumentListSyntax,
                                                 Optional argumentOpt As ArgumentSyntax = Nothing, Optional previousToken As SyntaxToken = Nothing) As IEnumerable(Of ITypeSymbol)
                If argumentList Is Nothing Then
                    Return SpecializedCollections.EmptyEnumerable(Of ITypeSymbol)()
                End If

                If argumentList.Parent IsNot Nothing Then
                    If argumentList.IsParentKind(SyntaxKind.InvocationExpression) Then
                        Dim invocation = TryCast(argumentList.Parent, InvocationExpressionSyntax)

                        Dim index As Integer = 0
                        If argumentOpt IsNot Nothing Then
                            index = invocation.ArgumentList.Arguments.IndexOf(argumentOpt)
                        Else
                            index = GetArgumentListIndex(argumentList, previousToken)
                        End If

                        Dim info = _semanticModel.GetSymbolInfo(invocation)
                        ' Check all the methods that have at least enough arguments to support being
                        ' called with argument at this position.  Note: if they're calling an extension
                        ' method then it will need one more argument in order for us to call it.
                        Dim symbols = info.GetBestOrAllSymbols()
                        If symbols.Any() Then
                            Return InferTypeInArgument(argumentOpt, index, symbols)
                        Else
                            ' It may be an array access
                            Dim targetExpression As ExpressionSyntax = Nothing
                            If invocation.Expression IsNot Nothing Then
                                targetExpression = invocation.Expression
                            ElseIf invocation.Parent.IsKind(SyntaxKind.ConditionalAccessExpression)
                                targetExpression = DirectCast(invocation.Parent, ConditionalAccessExpressionSyntax).Expression
                            End If

                            If targetExpression IsNot Nothing Then
                                Dim expressionType = _semanticModel.GetTypeInfo(targetExpression)
                                If TypeOf expressionType.Type Is IArrayTypeSymbol Then
                                    Return SpecializedCollections.SingletonEnumerable(Compilation.GetSpecialType(SpecialType.System_Int32))
                                End If
                            End If
                        End If
                    ElseIf argumentList.IsParentKind(SyntaxKind.ObjectCreationExpression) Then
                        ' new Outer(Foo());
                        '
                        ' new Outer(a: Foo());
                        '
                        ' etc.
                        Dim creation = TryCast(argumentList.Parent, ObjectCreationExpressionSyntax)
                        Dim info = _semanticModel.GetSymbolInfo(creation.Type)
                        Dim namedType = TryCast(info.Symbol, INamedTypeSymbol)
                        If namedType IsNot Nothing Then
                            If namedType.TypeKind = TypeKind.Delegate Then
                                Return SpecializedCollections.SingletonEnumerable(namedType)
                            Else
                                Dim index As Integer = 0
                                If argumentOpt IsNot Nothing Then
                                    index = creation.ArgumentList.Arguments.IndexOf(argumentOpt)
                                Else
                                    index = GetArgumentListIndex(argumentList, previousToken)
                                End If
                                Dim constructors = namedType.InstanceConstructors.Where(Function(m) m.Parameters.Length > index)
                                Return InferTypeInArgument(argumentOpt, index, constructors)
                            End If
                        End If
                    ElseIf argumentList.IsParentKind(SyntaxKind.Attribute) Then
                        ' Ex: <SomeAttribute(here)>

                        Dim attribute = TryCast(argumentList.Parent, AttributeSyntax)

                        If argumentOpt IsNot Nothing AndAlso argumentOpt.IsNamed Then
                            Return GetTypes(DirectCast(argumentOpt, SimpleArgumentSyntax).NameColonEquals.Name)
                        End If

                        Dim index As Integer = 0
                        If argumentOpt IsNot Nothing Then
                            index = attribute.ArgumentList.Arguments.IndexOf(argumentOpt)
                        Else
                            index = GetArgumentListIndex(argumentList, previousToken)
                        End If

                        Dim info = _semanticModel.GetSymbolInfo(attribute)
                        Dim symbols = info.GetBestOrAllSymbols()
                        If symbols.Any() Then
                            Dim methods = symbols.OfType(Of IMethodSymbol)()
                            Return InferTypeInArgument(argumentOpt, index, methods)
                        End If

#If False Then
            ElseIf argument.Parent.IsParentKind(SyntaxKind.ElementAccessExpression) Then
                ' Outer[Foo()];
                '
                ' Outer[a: Foo()];
                '
                ' etc.
                Dim elementAccess = TryCast(argument.Parent.Parent, ElementAccessExpressionSyntax)
                Dim info = Me.Binding.GetSemanticInfo(elementAccess.Expression)
                If TypeOf info.Type Is ArrayTypeSymbol Then
                    Return Me.Compilation.GetSpecialType(SpecialType.System_Int32)
                Else
                    Dim [type] = TryCast(info.Type, NamedTypeSymbol)
                    Dim index = elementAccess.ArgumentList.Arguments.IndexOf(argument)
                    Dim indexer = [type].GetMembers().OfType().Where(Function(p) p.IsIndexer).Where(Function(p) p.Parameters.Count > index).FirstOrDefault()
                    If indexer IsNot Nothing Then
                        Return InferTypeInArgument(argument, index, SpecializedCollections.SingletonEnumerable(indexer.Parameters))
                    End If
                End If
#End If
                    End If
                End If

                Return SpecializedCollections.EmptyEnumerable(Of ITypeSymbol)()
            End Function

            Private Function InferTypeInArgument(argument As ArgumentSyntax, index As Integer, symbols As IEnumerable(Of ISymbol)) As IEnumerable(Of ITypeSymbol)
                Dim methods = symbols.OfType(Of IMethodSymbol)()
                If methods.Any() Then
                    Dim parameters = methods.Select(Function(m) m.Parameters)
                    Return InferTypeInArgument(argument, index, parameters)
                End If

                Dim properties = symbols.OfType(Of IPropertySymbol)()
                If properties.Any() Then
                    Dim parameters = properties.Select(Function(p) p.Parameters)
                    Return InferTypeInArgument(argument, index, parameters)
                End If

                Return SpecializedCollections.EmptyEnumerable(Of ITypeSymbol)()
            End Function

            Private Function InferTypeInArgument(
                argument As ArgumentSyntax,
                index As Integer,
                parameterizedSymbols As IEnumerable(Of ImmutableArray(Of IParameterSymbol))) As IEnumerable(Of ITypeSymbol)

                Dim simpleArgument = TryCast(argument, SimpleArgumentSyntax)

                If simpleArgument IsNot Nothing AndAlso simpleArgument.IsNamed Then
                    Dim parameters = parameterizedSymbols _
                                        .SelectMany(Function(m) m) _
                                        .Where(Function(p) p.Name = simpleArgument.NameColonEquals.Name.Identifier.ValueText)

                    Return parameters.Select(Function(p) p.Type)
                Else
                    ' Otherwise, just take the first overload and pick what type this parameter is
                    ' based on index.
                    Return parameterizedSymbols.Where(Function(a) index < a.Length).Select(Function(a) a(index).Type)
                End If

                Return SpecializedCollections.EmptyEnumerable(Of ITypeSymbol)()
            End Function

            Private Function InferTypeInArrayCreationExpression(arrayCreationExpression As ArrayCreationExpressionSyntax) As IEnumerable(Of ITypeSymbol)
                Dim outerTypes = InferTypes(arrayCreationExpression)
                Return outerTypes.OfType(Of IArrayTypeSymbol).Select(Function(a) a.ElementType)
            End Function

            Private Function InferTypeInArrayRankSpecifier() As IEnumerable(Of INamedTypeSymbol)
                Return SpecializedCollections.SingletonEnumerable(Me.Compilation.GetSpecialType(SpecialType.System_Int32))
            End Function

            Private Function InferTypeInArrayType(arrayType As ArrayTypeSyntax) As IEnumerable(Of ITypeSymbol)
                ' Bind the array type, then unwrap whatever we get back based on the number of rank
                ' specifiers we see.
                Dim currentTypes = InferTypes(arrayType)
                Dim i = 0
                While i < arrayType.RankSpecifiers.Count
                    currentTypes = currentTypes.OfType(Of IArrayTypeSymbol)().Select(Function(c) c.ElementType)

                    i = i + 1
                End While

                Return currentTypes
            End Function

            Private Function InferTypeInAsClause(asClause As AsClauseSyntax,
                                                 Optional expressionOpt As ExpressionSyntax = Nothing,
                                                 Optional previousToken As SyntaxToken = Nothing) As IEnumerable(Of ITypeSymbol)
                If previousToken <> Nothing AndAlso previousToken.Kind <> SyntaxKind.AsKeyword Then
                    Return SpecializedCollections.EmptyEnumerable(Of ITypeSymbol)()
                End If

                If asClause.IsParentKind(SyntaxKind.CatchStatement) Then
                    If expressionOpt Is asClause.Type OrElse previousToken.Kind = SyntaxKind.AsKeyword Then
                        Return SpecializedCollections.SingletonEnumerable(Me.Compilation.ExceptionType)
                    End If
                End If

                Return SpecializedCollections.EmptyEnumerable(Of ITypeSymbol)()
            End Function

            Private Function InferTypeInAssignmentStatement(assignmentStatement As AssignmentStatementSyntax,
                                                            Optional expressionOpt As ExpressionSyntax = Nothing,
                                                            Optional previousToken As SyntaxToken = Nothing) As IEnumerable(Of ITypeSymbol)

                If assignmentStatement.IsKind(SyntaxKind.LeftShiftAssignmentStatement) OrElse
                    assignmentStatement.IsKind(SyntaxKind.RightShiftAssignmentStatement) Then
                    Return SpecializedCollections.SingletonEnumerable(Me.Compilation.GetSpecialType(SpecialType.System_Int32))
                End If

                If expressionOpt Is assignmentStatement.Right OrElse previousToken = assignmentStatement.OperatorToken Then
                    Return GetTypes(assignmentStatement.Left)
                End If

                If expressionOpt Is assignmentStatement.Left Then
                    Return GetTypes(assignmentStatement.Right)
                End If

                Return SpecializedCollections.EmptyEnumerable(Of ITypeSymbol)()
            End Function

            Private Function InferTypeInAttribute(attribute As AttributeSyntax) As IEnumerable(Of ITypeSymbol)
                Return SpecializedCollections.SingletonEnumerable(Me.Compilation.AttributeType)
            End Function

            Private Function InferTypeInAwaitExpression(awaitExpression As AwaitExpressionSyntax) As IEnumerable(Of ITypeSymbol)
                ' await <expression>

                Dim types = InferTypes(awaitExpression)

                Dim task = Me.Compilation.GetTypeByMetadataName("System.Threading.Tasks.Task")
                Dim taskOfT = Me.Compilation.GetTypeByMetadataName("System.Threading.Tasks.Task`1")

                If task Is Nothing OrElse taskOfT Is Nothing Then
                    Return SpecializedCollections.EmptyEnumerable(Of ITypeSymbol)()
                End If

                If Not types.Any() Then
                    Return SpecializedCollections.SingletonEnumerable(task)
                End If

                Return types.Select(Function(t) If(t.SpecialType = SpecialType.System_Void, task, taskOfT.Construct(t)))
            End Function

            Private Function InferTypeInBinaryConditionalExpression(conditional As BinaryConditionalExpressionSyntax,
                                                                    Optional expressionOpt As ExpressionSyntax = Nothing,
                                                                    Optional previousToken As SyntaxToken = Nothing) As IEnumerable(Of ITypeSymbol)
                If previousToken <> Nothing AndAlso previousToken <> conditional.OpenParenToken AndAlso previousToken <> conditional.CommaToken Then
                    Return SpecializedCollections.EmptyEnumerable(Of ITypeSymbol)()
                End If

                If conditional.FirstExpression Is expressionOpt OrElse previousToken = conditional.OpenParenToken Then
                    Dim rightTypes = GetTypes(conditional.SecondExpression, objectAsDefault:=True)
                    ' value type : If (Foo(), 0)
                    ' otherwise : If (Foo(), "")
                    Return rightTypes.Select(Function(t) If(t.IsValueType, Me.Compilation.GetSpecialType(SpecialType.System_Nullable_T).Construct(t), t))

                Else
                    Dim leftTypes = GetTypes(conditional.FirstExpression)
                    Return leftTypes.Select(Function(t)
                                                If t.OriginalDefinition.SpecialType = SpecialType.System_Nullable_T Then
                                                    Return DirectCast(t, INamedTypeSymbol).TypeArguments(0)
                                                Else
                                                    Return t
                                                End If
                                            End Function)
                End If
            End Function

            Private Function InferTypeInBinaryExpression(binop As BinaryExpressionSyntax,
                                                         Optional expression As ExpressionSyntax = Nothing,
                                                         Optional previousToken As SyntaxToken = Nothing) As IEnumerable(Of ITypeSymbol)
                ' If we got a token, it must be the operator in the binary expression
                Contract.ThrowIfTrue(previousToken <> Nothing AndAlso binop.OperatorToken <> previousToken)

                Dim rightSide = previousToken <> Nothing OrElse expression Is binop.Right

                Select Case binop.OperatorToken.Kind
                    Case SyntaxKind.LessThanLessThanToken,
                        SyntaxKind.GreaterThanGreaterThanToken,
                        SyntaxKind.LessThanLessThanEqualsToken,
                        SyntaxKind.GreaterThanGreaterThanEqualsToken
                        If rightSide Then
                            Return SpecializedCollections.SingletonEnumerable(Me.Compilation.GetSpecialType(SpecialType.System_Int32))
                        End If
                End Select

                ' Try to figure out what's on the other size of the binop.  If we can, then just that
                ' type.  This is often a reasonable heuristics to use for most operators.  NOTE(cyrusn):
                ' we could try to bind the token to see what overloaded operators it corresponds to.
                ' But the gain is pretty marginal IMO.
                Dim otherSide = If(rightSide, binop.Left, binop.Right)
                Dim otherSideTypes = GetTypes(otherSide)
                If otherSideTypes.Any(Function(t) t.SpecialType <> SpecialType.System_Object AndAlso Not t.IsErrorType()) Then
                    Return otherSideTypes
                End If

                Select Case binop.OperatorToken.Kind
                    Case SyntaxKind.CaretToken,
                        SyntaxKind.AmpersandToken,
                        SyntaxKind.LessThanToken,
                        SyntaxKind.LessThanEqualsToken,
                        SyntaxKind.GreaterThanToken,
                        SyntaxKind.GreaterThanEqualsToken,
                        SyntaxKind.PlusToken,
                        SyntaxKind.MinusToken,
                        SyntaxKind.AsteriskToken,
                        SyntaxKind.SlashToken,
                        SyntaxKind.CaretEqualsToken,
                        SyntaxKind.PlusEqualsToken,
                        SyntaxKind.MinusEqualsToken,
                        SyntaxKind.AsteriskEqualsToken,
                        SyntaxKind.SlashEqualsToken,
                        SyntaxKind.LessThanLessThanToken,
                        SyntaxKind.GreaterThanGreaterThanToken,
                        SyntaxKind.LessThanLessThanEqualsToken,
                        SyntaxKind.GreaterThanGreaterThanEqualsToken
                        Return SpecializedCollections.SingletonEnumerable(Me.Compilation.GetSpecialType(SpecialType.System_Int32))

                    Case SyntaxKind.AndKeyword,
                        SyntaxKind.AndAlsoKeyword,
                        SyntaxKind.OrKeyword,
                        SyntaxKind.OrElseKeyword
                        Return SpecializedCollections.SingletonEnumerable(Me.Compilation.GetSpecialType(SpecialType.System_Boolean))
                End Select

                Return SpecializedCollections.EmptyEnumerable(Of ITypeSymbol)()
            End Function

            Private Function InferTypeInCastExpression(castExpression As CastExpressionSyntax,
                                                       Optional expressionOpt As ExpressionSyntax = Nothing) As IEnumerable(Of ITypeSymbol)
                If castExpression.Expression Is expressionOpt Then
                    Return SpecializedCollections.EmptyEnumerable(Of ITypeSymbol)()
                End If

                Return GetTypes(castExpression.Type)
            End Function

            Private Function InferTypeInCatchFilterClause(catchFilterClause As CatchFilterClauseSyntax, Optional previousToken As SyntaxToken = Nothing) As IEnumerable(Of ITypeSymbol)
                If previousToken <> Nothing AndAlso previousToken <> catchFilterClause.WhenKeyword Then
                    Return SpecializedCollections.EmptyEnumerable(Of ITypeSymbol)()
                End If

                Return SpecializedCollections.SingletonEnumerable(Me.Compilation.GetSpecialType(SpecialType.System_Boolean))
            End Function

            Private Function InferTypeInDoStatement(Optional previousToken As SyntaxToken = Nothing) As IEnumerable(Of INamedTypeSymbol)
                Return SpecializedCollections.SingletonEnumerable(Me.Compilation.GetSpecialType(SpecialType.System_Boolean))
            End Function

            Private Function InferTypeInEqualsValue(equalsValue As EqualsValueSyntax, Optional previousToken As SyntaxToken = Nothing) As IEnumerable(Of ITypeSymbol)
                If equalsValue.IsParentKind(SyntaxKind.VariableDeclarator) Then
                    Dim variableDeclarator = DirectCast(equalsValue.Parent, VariableDeclaratorSyntax)
                    If variableDeclarator.AsClause Is Nothing AndAlso variableDeclarator.IsParentKind(SyntaxKind.UsingStatement) Then
                        Return SpecializedCollections.SingletonEnumerable(Me.Compilation.GetSpecialType(SpecialType.System_IDisposable))
                    End If

                    If variableDeclarator.Names.Count >= 1 Then
                        Dim name = variableDeclarator.Names(0)
                        Dim symbol = _semanticModel.GetDeclaredSymbol(name, _cancellationToken)

                        If symbol IsNot Nothing Then
                            Select Case symbol.Kind
                                Case SymbolKind.Field
                                    Return SpecializedCollections.SingletonEnumerable(DirectCast(symbol, IFieldSymbol).Type)
                                Case SymbolKind.Local
                                    Return SpecializedCollections.SingletonEnumerable(DirectCast(symbol, ILocalSymbol).Type)
                            End Select
                        End If
                    End If

                    If TypeOf variableDeclarator.AsClause Is SimpleAsClauseSyntax Then
                        Dim asClause = DirectCast(variableDeclarator.AsClause, SimpleAsClauseSyntax)
                        Return GetTypes(asClause.Type)
                    End If
                ElseIf equalsValue.IsParentKind(SyntaxKind.PropertyStatement) Then
                    Dim propertySyntax = CType(equalsValue.Parent, PropertyStatementSyntax)
                    Dim propertySymbol = _semanticModel.GetDeclaredSymbol(propertySyntax)
                    If propertySymbol Is Nothing Then
                        Return SpecializedCollections.EmptyEnumerable(Of ITypeSymbol)()
                    End If
                    Return If(propertySymbol.Type IsNot Nothing,
                        SpecializedCollections.SingletonEnumerable(propertySymbol.Type),
                        SpecializedCollections.EmptyEnumerable(Of ITypeSymbol)())
                End If

                Return SpecializedCollections.EmptyEnumerable(Of ITypeSymbol)()
            End Function

            Private Function InferTypeInExpressionStatement(expressionStatement As ExpressionStatementSyntax) As IEnumerable(Of INamedTypeSymbol)
                If expressionStatement.Expression.IsKind(SyntaxKind.InvocationExpression) Then
                    Return InferTypeInCallStatement()
                End If
                Return SpecializedCollections.EmptyEnumerable(Of INamedTypeSymbol)()
            End Function

            Private Function InferTypeInCallStatement() As IEnumerable(Of INamedTypeSymbol)
                Return SpecializedCollections.SingletonEnumerable(Me.Compilation.GetSpecialType(SpecialType.System_Void))
            End Function

            Private Function InferTypeInForEachStatement(forEachStatement As ForEachStatementSyntax,
                                                         Optional expressionOpt As ExpressionSyntax = Nothing,
                                                         Optional previousToken As SyntaxToken = Nothing) As IEnumerable(Of ITypeSymbol)
                If expressionOpt Is forEachStatement.Expression OrElse previousToken = forEachStatement.InKeyword Then
                    If TypeOf forEachStatement.ControlVariable Is VariableDeclaratorSyntax Then
                        Dim declarator = DirectCast(forEachStatement.ControlVariable, VariableDeclaratorSyntax)
                        If TypeOf declarator.AsClause Is SimpleAsClauseSyntax Then
                            Dim variableTypes = GetTypes(DirectCast(declarator.AsClause, SimpleAsClauseSyntax).Type, objectAsDefault:=True)

                            Dim type = Me.Compilation.GetSpecialType(SpecialType.System_Collections_Generic_IEnumerable_T)
                            Return variableTypes.Select(Function(t) type.Construct(t))
                        End If
                    ElseIf TypeOf forEachStatement.ControlVariable Is SimpleNameSyntax Then
                        Dim type = Me.Compilation.GetSpecialType(SpecialType.System_Collections_Generic_IEnumerable_T)
                        Return SpecializedCollections.SingletonEnumerable(type.Construct(Compilation.GetSpecialType(SpecialType.System_Object)))
                    End If
                End If

                Return SpecializedCollections.EmptyEnumerable(Of ITypeSymbol)()
            End Function

            Private Function InferTypeInForStatement(forStatement As ForStatementSyntax,
                                                     Optional expressionOpt As ExpressionSyntax = Nothing,
                                                     Optional previousToken As SyntaxToken = Nothing) As IEnumerable(Of ITypeSymbol)
                If (expressionOpt IsNot Nothing AndAlso expressionOpt IsNot forStatement.ControlVariable) OrElse
                    previousToken = forStatement.ToKeyword OrElse
                    previousToken = forStatement.EqualsToken Then
                    If TypeOf forStatement.ControlVariable Is VariableDeclaratorSyntax Then
                        Dim declarator = DirectCast(forStatement.ControlVariable, VariableDeclaratorSyntax)
                        If TypeOf declarator.AsClause Is SimpleAsClauseSyntax Then
                            Return GetTypes(DirectCast(declarator.AsClause, SimpleAsClauseSyntax).Type, objectAsDefault:=True)
                        End If
                    End If
                End If

                Return SpecializedCollections.EmptyEnumerable(Of ITypeSymbol)()
            End Function

            Private Function InferTypeInForStepClause(forStepClause As ForStepClauseSyntax, Optional previousToken As SyntaxToken = Nothing) As IEnumerable(Of ITypeSymbol)
                ' TODO(cyrusn): Potentially infer a different type based on the type of the variable
                ' being foreached over.
                Return SpecializedCollections.SingletonEnumerable(Me.Compilation.GetSpecialType(SpecialType.System_Int32))
            End Function

            Private Function InferTypeInIfOrElseIfStatement(Optional previousToken As SyntaxToken = Nothing) As IEnumerable(Of INamedTypeSymbol)
                Return SpecializedCollections.SingletonEnumerable(Me.Compilation.GetSpecialType(SpecialType.System_Boolean))
            End Function

            Private Function InferTypeInLambda(lambda As ExpressionSyntax, Optional previousToken As SyntaxToken = Nothing) As IEnumerable(Of ITypeSymbol)
                If lambda Is Nothing Then
                    Return SpecializedCollections.EmptyEnumerable(Of ITypeSymbol)()
                End If

                ' Func<int,string> = i => Foo();
                Dim lambdaTypes = GetTypes(lambda).Where(Function(t) Not IsUnusableType(t))
                If lambdaTypes.IsEmpty() Then
                    lambdaTypes = InferTypes(lambda)
                End If

                Return lambdaTypes.Where(Function(t) t.TypeKind = TypeKind.Delegate).SelectMany(Function(t) t.GetMembers(WellKnownMemberNames.DelegateInvokeName).OfType(Of IMethodSymbol)().Select(Function(m) m.ReturnType))

                Return SpecializedCollections.EmptyEnumerable(Of ITypeSymbol)()
            End Function

            Private Function InferTypeForReturnStatement(returnStatement As ReturnStatementSyntax, Optional previousToken As SyntaxToken = Nothing) As IEnumerable(Of ITypeSymbol)
                ' If we're position based, we must have gotten the Return token
                If previousToken <> Nothing AndAlso previousToken.Kind <> SyntaxKind.ReturnKeyword Then
                    Return SpecializedCollections.EmptyEnumerable(Of ITypeSymbol)()
                End If

                ' If we're in a lambda, then use the return tpe of the lambda to figure out what to
                ' infer.  i.e.   Func<int,string> f = i => { return Foo(); }
                Dim lambda = returnStatement.GetAncestorsOrThis(Of ExpressionSyntax)().FirstOrDefault(
                    Function(e) TypeOf e Is MultiLineLambdaExpressionSyntax OrElse
                        TypeOf e Is SingleLineLambdaExpressionSyntax)
                If lambda IsNot Nothing Then
                    Return InferTypeInLambda(lambda)
                End If

                If returnStatement.GetAncestorOrThis(Of MethodBlockBaseSyntax) Is Nothing Then
                    Return SpecializedCollections.EmptyEnumerable(Of ITypeSymbol)
                End If

                Dim memberSymbol = GetDeclaredMemberSymbolFromOriginalSemanticModel(_semanticModel, returnStatement.GetAncestor(Of MethodBlockBaseSyntax).BlockStatement)

                Dim memberMethod = TryCast(memberSymbol, IMethodSymbol)
                If memberMethod IsNot Nothing Then

                    If memberMethod.IsAsync Then
                        Dim typeArguments = memberMethod.ReturnType.GetTypeArguments()
                        Dim taskOfT = Me.Compilation.TaskOfTType()

                        Return If(
                            taskOfT IsNot Nothing AndAlso memberMethod.ReturnType.OriginalDefinition Is taskOfT AndAlso typeArguments.Any(),
                            SpecializedCollections.SingletonEnumerable(typeArguments.First()),
                            SpecializedCollections.EmptyEnumerable(Of ITypeSymbol))
                    Else
                        Return SpecializedCollections.SingletonEnumerable(memberMethod.ReturnType)
                    End If
                End If

                Return SpecializedCollections.EmptyEnumerable(Of ITypeSymbol)()
            End Function

            Private Function InferTypeInYieldStatement(yieldStatement As YieldStatementSyntax, Optional previoustoken As SyntaxToken = Nothing) As IEnumerable(Of ITypeSymbol)
                ' If we're position based, we must be after the Yield token
                If previoustoken <> Nothing AndAlso Not previoustoken.IsKind(SyntaxKind.YieldKeyword) Then
                    Return SpecializedCollections.EmptyEnumerable(Of ITypeSymbol)
                End If

                If yieldStatement.GetAncestorOrThis(Of MethodBlockBaseSyntax) Is Nothing Then
                    Return SpecializedCollections.EmptyEnumerable(Of ITypeSymbol)
                End If

                Dim memberSymbol = GetDeclaredMemberSymbolFromOriginalSemanticModel(_semanticModel, yieldStatement.GetAncestor(Of MethodBlockBaseSyntax).BlockStatement)

                Dim memberType = memberSymbol.TypeSwitch(
                    Function(method As IMethodSymbol) method.ReturnType,
                    Function([property] As IPropertySymbol) [property].Type)

                If TypeOf memberType Is INamedTypeSymbol Then
                    If memberType.OriginalDefinition.SpecialType = SpecialType.System_Collections_Generic_IEnumerable_T OrElse
                       memberType.OriginalDefinition.SpecialType = SpecialType.System_Collections_Generic_IEnumerator_T Then

                        Return SpecializedCollections.SingletonEnumerable(DirectCast(memberType, INamedTypeSymbol).TypeArguments(0))
                    End If
                End If

                Return SpecializedCollections.EmptyEnumerable(Of ITypeSymbol)
            End Function

            Private Function GetDeclaredMemberSymbolFromOriginalSemanticModel(currentSemanticModel As SemanticModel, declarationInCurrentTree As DeclarationStatementSyntax) As ISymbol
                Dim originalSemanticModel = currentSemanticModel.GetOriginalSemanticModel()
                Dim declaration As DeclarationStatementSyntax

                If currentSemanticModel.IsSpeculativeSemanticModel Then
                    Dim tokenInOriginalTree = originalSemanticModel.SyntaxTree.GetRoot(_cancellationToken).FindToken(currentSemanticModel.OriginalPositionForSpeculation)
                    declaration = tokenInOriginalTree.GetAncestor(Of DeclarationStatementSyntax)
                Else
                    declaration = declarationInCurrentTree
                End If

                Return originalSemanticModel.GetDeclaredSymbol(declaration, _cancellationToken)
            End Function

            Private Function InferTypeInSelectStatement(switchStatementSyntax As SelectStatementSyntax, Optional previousToken As SyntaxToken = Nothing) As IEnumerable(Of ITypeSymbol)
                ' Use the first case label to determine the return type.
                If TypeOf switchStatementSyntax.Parent Is SelectBlockSyntax Then
                    Dim firstCase = DirectCast(switchStatementSyntax.Parent, SelectBlockSyntax).CaseBlocks.SelectMany(Function(c) c.CaseStatement.Cases).OfType(Of SimpleCaseClauseSyntax).FirstOrDefault()
                    If firstCase IsNot Nothing Then
                        Return GetTypes(firstCase.Value)
                    End If
                End If

                Return SpecializedCollections.SingletonEnumerable(Me.Compilation.GetSpecialType(SpecialType.System_Int32))
            End Function

            Private Function InferTypeInTernaryConditionalExpression(conditional As TernaryConditionalExpressionSyntax,
                                                                     Optional expressionOpt As ExpressionSyntax = Nothing,
                                                                     Optional previousToken As SyntaxToken = Nothing) As IEnumerable(Of ITypeSymbol)
                If previousToken <> Nothing AndAlso previousToken.Kind <> SyntaxKind.OpenParenToken AndAlso previousToken.Kind <> SyntaxKind.CommaToken Then
                    Return SpecializedCollections.EmptyEnumerable(Of ITypeSymbol)()
                ElseIf previousToken = conditional.OpenParenToken Then
                    Return SpecializedCollections.SingletonEnumerable(Me.Compilation.GetSpecialType(SpecialType.System_Boolean))
                ElseIf previousToken = conditional.FirstCommaToken Then
                    Return GetTypes(conditional.WhenTrue)
                ElseIf previousToken = conditional.SecondCommaToken Then
                    Return GetTypes(conditional.WhenFalse)
                End If

                If conditional.Condition Is expressionOpt Then
                    Return SpecializedCollections.SingletonEnumerable(Me.Compilation.GetSpecialType(SpecialType.System_Boolean))
                Else
                    Return If(conditional.WhenTrue Is expressionOpt, GetTypes(conditional.WhenFalse), GetTypes(conditional.WhenTrue))
                End If
            End Function

            Private Function InferTypeInThrowStatement(Optional previousToken As SyntaxToken = Nothing) As IEnumerable(Of ITypeSymbol)
                ' If we're not the Throw token, there's nothing to to
                If previousToken <> Nothing AndAlso previousToken.Kind <> SyntaxKind.ThrowKeyword Then
                    Return SpecializedCollections.EmptyEnumerable(Of ITypeSymbol)()
                End If

                Return SpecializedCollections.SingletonEnumerable(Me.Compilation.ExceptionType)
            End Function

            Private Function InferTypeInUnaryExpression(unaryExpressionSyntax As UnaryExpressionSyntax, Optional previousToken As SyntaxToken = Nothing) As IEnumerable(Of ITypeSymbol)
                Select Case unaryExpressionSyntax.Kind
                    Case SyntaxKind.UnaryPlusExpression, SyntaxKind.UnaryMinusExpression
                        Return SpecializedCollections.SingletonEnumerable(Me.Compilation.GetSpecialType(SpecialType.System_Int32))
                    Case SyntaxKind.NotExpression
                        Dim types = InferTypes(unaryExpressionSyntax)
                        If types.Any(Function(t) t.IsNumericType) Then
                            Return types.Where(Function(t) t.IsNumericType)
                        End If

                        Return SpecializedCollections.SingletonEnumerable(Me.Compilation.GetSpecialType(SpecialType.System_Boolean))
                    Case SyntaxKind.AddressOfExpression
                        Return InferTypes(unaryExpressionSyntax)
                End Select

                Return SpecializedCollections.EmptyEnumerable(Of ITypeSymbol)()
            End Function

            Private Function InferTypeInUsingStatement(usingStatement As UsingStatementSyntax, Optional previousToken As SyntaxToken = Nothing) As IEnumerable(Of ITypeSymbol)
                Return SpecializedCollections.SingletonEnumerable(Me.Compilation.GetSpecialType(SpecialType.System_IDisposable))
            End Function

            Private Function InferTypeInVariableDeclarator(expression As ExpressionSyntax,
                                                           variableDeclarator As VariableDeclaratorSyntax,
                                                           Optional previousToken As SyntaxToken = Nothing) As IEnumerable(Of ITypeSymbol)
                If variableDeclarator.AsClause IsNot Nothing Then
                    Return SpecializedCollections.EmptyEnumerable(Of ITypeSymbol)()
                End If

                Return GetTypes(variableDeclarator.AsClause.Type)
            End Function

            Private Function InferTypeInWhileStatement(Optional previousToken As SyntaxToken = Nothing) As IEnumerable(Of ITypeSymbol)
                Return SpecializedCollections.SingletonEnumerable(Me.Compilation.GetSpecialType(SpecialType.System_Boolean))
            End Function

            Private Function InferTypeInWhileOrUntilClause(Optional previousToken As SyntaxToken = Nothing) As IEnumerable(Of ITypeSymbol)
                Return SpecializedCollections.SingletonEnumerable(Me.Compilation.GetSpecialType(SpecialType.System_Boolean))
            End Function

            Private Function InferTypeInNamedFieldInitializer(initializer As NamedFieldInitializerSyntax, Optional previousToken As SyntaxToken = Nothing) As IEnumerable(Of ITypeSymbol)
                Dim right = _semanticModel.GetTypeInfo(initializer.Name).Type
                If right IsNot Nothing AndAlso TypeOf right IsNot IErrorTypeSymbol Then
                    Return SpecializedCollections.SingletonEnumerable(right)
                End If

                Return SpecializedCollections.SingletonEnumerable(_semanticModel.GetTypeInfo(initializer.Expression).Type)
            End Function

            Public Function InferTypeInCaseStatement(caseStatement As CaseStatementSyntax) As IEnumerable(Of ITypeSymbol)
                Dim selectBlock = caseStatement.GetAncestor(Of SelectBlockSyntax)()
                If selectBlock IsNot Nothing Then
                    Return GetTypes(selectBlock.SelectStatement.Expression)
                End If

                Return SpecializedCollections.EmptyEnumerable(Of ITypeSymbol)()
            End Function

            Private Function GetCollectionElementType(namedType As INamedTypeSymbol, parameterIndex As Integer, parameterCount As Integer) As IEnumerable(Of ITypeSymbol)
                If namedType IsNot Nothing Then
#If False Then
            Dim addMethods = Me.Binding.Lookup(leftType, "Add").OfType()
            Dim method = addMethods.Where(Function(m) Not m.IsStatic).Where(Function(m) m.Arity = 0).Where(Function(m) m.Parameters.Count = parameterCount).FirstOrDefault()
            If method IsNot Nothing Then
                Return method.Parameters(parameterIndex).Type
            End If
#End If
                End If

                Return SpecializedCollections.EmptyEnumerable(Of ITypeSymbol)()
            End Function

            Private Function GetArgumentListIndex(argumentList As ArgumentListSyntax, previousToken As SyntaxToken) As Integer
                If previousToken = argumentList.OpenParenToken Then
                    Return 0
                End If

                Dim index = argumentList.Arguments.GetWithSeparators().IndexOf(previousToken)
                Return (index + 1) \ 2
            End Function

#If False Then
    Private Function InferTypeInInitializerExpression(expression As ExpressionSyntax, initializer As InitializerExpressionSyntax) As TypeSymbol
        If initializer.IsParentKind(SyntaxKind.ArrayCreationExpression) Then
            ' new int[] { Foo() }
            Dim arrayCreation = DirectCast(initializer.Parent, ArrayCreationExpressionSyntax)
            Dim [type] = [GetType](arrayCreation)
            If TypeOf [type] Is ArrayTypeSymbol Then
                Return (DirectCast([type], ArrayTypeSymbol)).ElementType
            End If
        ElseIf initializer.IsParentKind(SyntaxKind.ObjectCreationExpression) Then
            ' new List<T> { Foo() }
            Dim objectCreation = DirectCast(initializer.Parent, ObjectCreationExpressionSyntax)
            Dim [type] = TryCast([GetType](objectCreation), NamedTypeSymbol)
            Return GetCollectionElementType([type], parameterIndex:=0, parameterCount:=1)
        ElseIf initializer.IsParentKind(SyntaxKind.InitializerExpression) AndAlso initializer.Parent.IsParentKind(SyntaxKind.ObjectCreationExpression) Then
            ' new Dictionary<K,V> { { Foo(), .. } }
            Dim objectCreation = DirectCast(initializer.Parent.Parent, ObjectCreationExpressionSyntax)
            Dim [type] = TryCast([GetType](objectCreation), NamedTypeSymbol)
            Return GetCollectionElementType([type], parameterIndex:=initializer.Expressions.IndexOf(expression), parameterCount:=initializer.Expressions.Count)
        ElseIf initializer.IsParentKind(SyntaxKind.AssignExpression) Then
            ' new Foo { a = { Foo() } }
            Dim assignExpression = DirectCast(initializer.Parent, BinaryExpressionSyntax)
            Dim [type] = TryCast([GetType](assignExpression.Left), NamedTypeSymbol)
            Return GetCollectionElementType([type], parameterIndex:=initializer.Expressions.IndexOf(expression), parameterCount:=initializer.Expressions.Count)
        End If
        Return Me.Compilation.ObjectType
    End Function
#End If
        End Class
    End Class
End Namespace
