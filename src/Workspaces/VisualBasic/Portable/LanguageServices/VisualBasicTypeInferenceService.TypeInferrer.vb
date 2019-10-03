' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Immutable
Imports System.Threading
Imports System.Threading.Tasks
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.LanguageServices
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic

    Partial Friend Class VisualBasicTypeInferenceService
        Private Class TypeInferrer
            Inherits AbstractTypeInferrer

            Public Sub New(semanticModel As SemanticModel, cancellationToken As CancellationToken)
                MyBase.New(semanticModel, cancellationToken)
            End Sub

            Protected Overrides Function IsUnusableType(otherSideType As ITypeSymbol) As Boolean
                Return otherSideType.IsErrorType() AndAlso
                    otherSideType.Name = String.Empty
            End Function

            Protected Overrides Function GetTypes_DoNotCallDirectly(node As SyntaxNode, objectAsDefault As Boolean) As IEnumerable(Of TypeInferenceInfo)
                If node IsNot Nothing Then
                    Dim info = SemanticModel.GetTypeInfo(node)
                    If info.Type IsNot Nothing AndAlso info.Type.TypeKind <> TypeKind.Error Then
                        Return CreateResult(info.Type)
                    End If

                    If info.ConvertedType IsNot Nothing AndAlso info.ConvertedType.TypeKind <> TypeKind.Error Then
                        Return CreateResult(info.ConvertedType)
                    End If

                    If node.Kind = SyntaxKind.AddressOfExpression Then
                        Dim unaryExpression = DirectCast(node, UnaryExpressionSyntax)
                        Dim symbol = SemanticModel.GetSymbolInfo(unaryExpression.Operand, CancellationToken).GetAnySymbol()
                        Dim type = symbol.ConvertToType(Me.Compilation)
                        If type IsNot Nothing Then
                            Return CreateResult(type)
                        End If
                    End If
                End If

                Return If(objectAsDefault, CreateResult(Me.Compilation.ObjectType), SpecializedCollections.EmptyEnumerable(Of TypeInferenceInfo)())
            End Function

            Protected Overrides Function InferTypesWorker_DoNotCallDirectly(node As SyntaxNode) As IEnumerable(Of TypeInferenceInfo)
                Dim expression = TryCast(node, ExpressionSyntax)
                If expression IsNot Nothing Then
                    expression = expression.WalkUpParentheses()
                    node = expression
                End If

                Dim parent = node.Parent

                Return parent.TypeSwitch(
                    Function(addRemoveHandlerStatement As AddRemoveHandlerStatementSyntax) InferTypeInAddRemoveHandlerStatementSyntax(addRemoveHandlerStatement, expression),
                    Function(argument As ArgumentSyntax) InferTypeInArgument(argument),
                    Function(arrayCreationExpression As ArrayCreationExpressionSyntax) InferTypeInArrayCreationExpression(arrayCreationExpression),
                    Function(arrayRank As ArrayRankSpecifierSyntax) InferTypeInArrayRankSpecifier(),
                    Function(arrayType As ArrayTypeSyntax) InferTypeInArrayType(arrayType),
                    Function(asClause As AsClauseSyntax) InferTypeInAsClause(asClause, expression),
                    Function(assignmentStatement As AssignmentStatementSyntax) InferTypeInAssignmentStatement(assignmentStatement, expression),
                    Function(attribute As AttributeSyntax) InferTypeInAttribute(attribute),
                    Function(awaitExpression As AwaitExpressionSyntax) InferTypeInAwaitExpression(awaitExpression),
                    Function(binaryExpression As BinaryExpressionSyntax) InferTypeInBinaryExpression(binaryExpression, expression),
                    Function(callStatement As CallStatementSyntax) InferTypeInCallStatement(),
                    Function(castExpression As CastExpressionSyntax) InferTypeInCastExpression(castExpression, expression),
                    Function(catchFilterClause As CatchFilterClauseSyntax) InferTypeInCatchFilterClause(catchFilterClause),
                    Function(collectionInitializer As CollectionInitializerSyntax) InferTypeInCollectionInitializerExpression(collectionInitializer, expression),
                    Function(conditionalAccessExpression As ConditionalAccessExpressionSyntax) InferTypeInConditionalAccessExpression(conditionalAccessExpression),
                    Function(conditionalExpression As BinaryConditionalExpressionSyntax) InferTypeInBinaryConditionalExpression(conditionalExpression, expression),
                    Function(conditionalExpression As TernaryConditionalExpressionSyntax) InferTypeInTernaryConditionalExpression(conditionalExpression, expression),
                    Function(doStatement As DoStatementSyntax) InferTypeInDoStatement(),
                    Function(equalsValue As EqualsValueSyntax) InferTypeInEqualsValue(equalsValue),
                    Function(expressionStatement As ExpressionStatementSyntax) InferTypeInExpressionStatement(expressionStatement),
                    Function(forEachStatement As ForEachStatementSyntax) InferTypeInForEachStatement(forEachStatement, expression),
                    Function(forStatement As ForStatementSyntax) InferTypeInForStatement(forStatement, expression),
                    Function(forStepClause As ForStepClauseSyntax) InferTypeInForStepClause(forStepClause),
                    Function(ifStatement As ElseIfStatementSyntax) InferTypeInIfOrElseIfStatement(),
                    Function(ifStatement As IfStatementSyntax) InferTypeInIfOrElseIfStatement(),
                    Function(memberAccessExpression As MemberAccessExpressionSyntax) InferTypeInMemberAccessExpression(memberAccessExpression, expression),
                    Function(namedFieldInitializer As NamedFieldInitializerSyntax) InferTypeInNamedFieldInitializer(namedFieldInitializer),
                    Function(parenthesizedLambda As MultiLineLambdaExpressionSyntax) InferTypeInLambda(parenthesizedLambda),
                    Function(prefixUnary As UnaryExpressionSyntax) InferTypeInUnaryExpression(prefixUnary),
                    Function(returnStatement As ReturnStatementSyntax) InferTypeForReturnStatement(returnStatement),
                    Function(singleLineLambdaExpression As SingleLineLambdaExpressionSyntax) InferTypeInLambda(singleLineLambdaExpression),
                    Function(switchStatement As SelectStatementSyntax) InferTypeInSelectStatement(switchStatement),
                    Function(throwStatement As ThrowStatementSyntax) InferTypeInThrowStatement(),
                    Function(typeOfExpression As TypeOfExpressionSyntax) InferTypeInTypeOfExpressionSyntax(typeOfExpression),
                    Function(usingStatement As UsingStatementSyntax) InferTypeInUsingStatement(usingStatement),
                    Function(whileStatement As WhileOrUntilClauseSyntax) InferTypeInWhileOrUntilClause(),
                    Function(whileStatement As WhileStatementSyntax) InferTypeInWhileStatement(),
                    Function(yieldStatement As YieldStatementSyntax) InferTypeInYieldStatement(yieldStatement),
                    Function(x) SpecializedCollections.EmptyEnumerable(Of TypeInferenceInfo)())
            End Function

            Private Function InferTypeInTypeOfExpressionSyntax(typeOfExpression As TypeOfExpressionSyntax) As IEnumerable(Of TypeInferenceInfo)
                Dim expressionType = typeOfExpression.Type
                If expressionType Is Nothing Then
                    Return SpecializedCollections.EmptyEnumerable(Of TypeInferenceInfo)()
                End If

                Dim typeSymbol = SemanticModel.GetTypeInfo(expressionType).Type
                If TypeOf typeSymbol IsNot INamedTypeSymbol Then
                    Return SpecializedCollections.EmptyEnumerable(Of TypeInferenceInfo)()
                End If

                Return CreateResult(typeSymbol)
            End Function

            Private Function InferTypeInAddRemoveHandlerStatementSyntax(addRemoveHandlerStatement As AddRemoveHandlerStatementSyntax,
                                                                        expression As ExpressionSyntax) As IEnumerable(Of TypeInferenceInfo)
                If expression Is addRemoveHandlerStatement.DelegateExpression Then
                    Return GetTypes(addRemoveHandlerStatement.EventExpression)
                End If

                Return SpecializedCollections.EmptyEnumerable(Of TypeInferenceInfo)()
            End Function

            Protected Overrides Function InferTypesWorker_DoNotCallDirectly(position As Integer) As IEnumerable(Of TypeInferenceInfo)
                Dim tree = Me.SemanticModel.SyntaxTree
                Dim token = tree.FindTokenOnLeftOfPosition(position, CancellationToken)
                token = token.GetPreviousTokenIfTouchingWord(position)

                Dim parent = token.Parent

                Return parent.TypeSwitch(
                    Function(argument As ArgumentSyntax) InferTypeInArgument(argument, previousToken:=token),
                    Function(argumentList As ArgumentListSyntax) InferTypeInArgumentList(argumentList, previousToken:=token),
                    Function(arrayCreationExpression As ArrayCreationExpressionSyntax) InferTypeInArrayCreationExpression(arrayCreationExpression),
                    Function(arrayRank As ArrayRankSpecifierSyntax) InferTypeInArrayRankSpecifier(),
                    Function(arrayType As ArrayTypeSyntax) InferTypeInArrayType(arrayType),
                    Function(asClause As AsClauseSyntax) InferTypeInAsClause(asClause, previousToken:=token),
                    Function(assignmentStatement As AssignmentStatementSyntax) InferTypeInAssignmentStatement(assignmentStatement, previousToken:=token),
                    Function(attribute As AttributeSyntax) InferTypeInAttribute(attribute),
                    Function(awaitExpression As AwaitExpressionSyntax) InferTypeInAwaitExpression(awaitExpression),
                    Function(binaryExpression As BinaryExpressionSyntax) InferTypeInBinaryExpression(binaryExpression, previousToken:=token),
                    Function(callStatement As CallStatementSyntax) InferTypeInCallStatement(),
                    Function(caseStatement As CaseStatementSyntax) InferTypeInCaseStatement(caseStatement),
                    Function(castExpression As CastExpressionSyntax) InferTypeInCastExpression(castExpression),
                    Function(catchFilterClause As CatchFilterClauseSyntax) InferTypeInCatchFilterClause(catchFilterClause, previousToken:=token),
                    Function(conditionalExpression As BinaryConditionalExpressionSyntax) InferTypeInBinaryConditionalExpression(conditionalExpression, previousToken:=token),
                    Function(conditionalExpression As TernaryConditionalExpressionSyntax) InferTypeInTernaryConditionalExpression(conditionalExpression, previousToken:=token),
                    Function(doStatement As DoStatementSyntax) InferTypeInDoStatement(token),
                    Function(equalsValue As EqualsValueSyntax) InferTypeInEqualsValue(equalsValue, token),
                    Function(expressionStatement As ExpressionStatementSyntax) InferTypeInExpressionStatement(expressionStatement),
                    Function(forEachStatement As ForEachStatementSyntax) InferTypeInForEachStatement(forEachStatement, previousToken:=token),
                    Function(forStatement As ForStatementSyntax) InferTypeInForStatement(forStatement, previousToken:=token),
                    Function(forStepClause As ForStepClauseSyntax) InferTypeInForStepClause(forStepClause, token),
                    Function(ifStatement As IfStatementSyntax) InferTypeInIfOrElseIfStatement(token),
                    Function(memberAccessExpression As MemberAccessExpressionSyntax) InferTypeInMemberAccessExpression(memberAccessExpression, previousToken:=token),
                    Function(nameColonEquals As NameColonEqualsSyntax) InferTypeInArgumentList(TryCast(nameColonEquals.Parent.Parent, ArgumentListSyntax), DirectCast(nameColonEquals.Parent, ArgumentSyntax)),
                    Function(namedFieldInitializer As NamedFieldInitializerSyntax) InferTypeInNamedFieldInitializer(namedFieldInitializer, token),
                    Function(objectCreation As ObjectCreationExpressionSyntax) InferTypes(objectCreation),
                    Function(parameterListSyntax As ParameterListSyntax) InferTypeInParameterList(parameterListSyntax),
                    Function(parenthesizedLambda As MultiLineLambdaExpressionSyntax) InferTypeInLambda(parenthesizedLambda, token),
                    Function(prefixUnary As UnaryExpressionSyntax) InferTypeInUnaryExpression(prefixUnary, token),
                    Function(returnStatement As ReturnStatementSyntax) InferTypeForReturnStatement(returnStatement, token),
                    Function(singleLineLambdaExpression As SingleLineLambdaExpressionSyntax) InferTypeInLambda(singleLineLambdaExpression, token),
                    Function(switchStatement As SelectStatementSyntax) InferTypeInSelectStatement(switchStatement, token),
                    Function(throwStatement As ThrowStatementSyntax) InferTypeInThrowStatement(),
                    Function(usingStatement As UsingStatementSyntax) InferTypeInUsingStatement(usingStatement),
                    Function(whileStatement As WhileOrUntilClauseSyntax) InferTypeInWhileOrUntilClause(),
                    Function(whileStatement As WhileStatementSyntax) InferTypeInWhileStatement(),
                    Function(yieldStatement As YieldStatementSyntax) InferTypeInYieldStatement(yieldStatement, token),
                    Function(x) SpecializedCollections.EmptyEnumerable(Of TypeInferenceInfo)())
            End Function

            Private Function InferTypeInParameterList(parameterList As ParameterListSyntax) As IEnumerable(Of TypeInferenceInfo)
                Return If(parameterList.Parent IsNot Nothing,
                    InferTypeInLambda(TryCast(parameterList.Parent.Parent, LambdaExpressionSyntax)),
                    SpecializedCollections.EmptyEnumerable(Of TypeInferenceInfo)())
            End Function

            Private Function InferTypeInArgument(argument As ArgumentSyntax,
                                                 Optional previousToken As SyntaxToken = Nothing) As IEnumerable(Of TypeInferenceInfo)
                If TypeOf argument.Parent Is ArgumentListSyntax Then
                    Return InferTypeInArgumentList(
                        DirectCast(argument.Parent, ArgumentListSyntax), argument, previousToken)
                End If

                If TypeOf argument.Parent Is TupleExpressionSyntax Then
                    Return InferTypeInTupleExpression(
                        DirectCast(argument.Parent, TupleExpressionSyntax),
                        DirectCast(argument, SimpleArgumentSyntax))
                End If

                Return SpecializedCollections.EmptyEnumerable(Of TypeInferenceInfo)
            End Function

            Private Function InferTypeInTupleExpression(tupleExpression As TupleExpressionSyntax,
                                                        argument As SimpleArgumentSyntax) As IEnumerable(Of TypeInferenceInfo)
                Dim index = tupleExpression.Arguments.IndexOf(argument)
                Dim parentTypes = InferTypes(tupleExpression)

                Return parentTypes.Select(Function(TypeInfo) TypeInfo.InferredType).
                                   OfType(Of INamedTypeSymbol)().
                                   Where(Function(namedType) namedType.IsTupleType AndAlso index < namedType.TupleElements.Length).
                                   Select(Function(tupleType) New TypeInferenceInfo(tupleType.TupleElements(index).Type))
            End Function

            Private Function InferTypeInArgumentList(argumentList As ArgumentListSyntax,
                                                     Optional argumentOpt As ArgumentSyntax = Nothing,
                                                     Optional previousToken As SyntaxToken = Nothing) As IEnumerable(Of TypeInferenceInfo)
                If argumentList Is Nothing Then
                    Return SpecializedCollections.EmptyEnumerable(Of TypeInferenceInfo)()
                End If

                If argumentList.Parent IsNot Nothing Then

                    If argumentList.IsParentKind(SyntaxKind.ArrayCreationExpression) Then
                        Return CreateResult(Compilation.GetSpecialType(SpecialType.System_Int32))
                    ElseIf argumentList.IsParentKind(SyntaxKind.InvocationExpression) Then
                        Dim invocation = TryCast(argumentList.Parent, InvocationExpressionSyntax)

                        Dim index As Integer = 0
                        If argumentOpt IsNot Nothing Then
                            index = invocation.ArgumentList.Arguments.IndexOf(argumentOpt)
                        Else
                            index = GetArgumentListIndex(argumentList, previousToken)
                        End If

                        If index < 0 Then
                            Return SpecializedCollections.EmptyEnumerable(Of TypeInferenceInfo)()
                        End If

                        Dim info = SemanticModel.GetSymbolInfo(invocation)
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
                            ElseIf invocation.Parent.IsKind(SyntaxKind.ConditionalAccessExpression) Then
                                targetExpression = DirectCast(invocation.Parent, ConditionalAccessExpressionSyntax).Expression
                            End If

                            If targetExpression IsNot Nothing Then
                                Dim expressionType = SemanticModel.GetTypeInfo(targetExpression)
                                If TypeOf expressionType.Type Is IArrayTypeSymbol Then
                                    Return CreateResult(Compilation.GetSpecialType(SpecialType.System_Int32))
                                End If
                            End If
                        End If
                    ElseIf argumentList.IsParentKind(SyntaxKind.ObjectCreationExpression) Then
                        ' new Outer(Goo());
                        '
                        ' new Outer(a: Goo());
                        '
                        ' etc.
                        Dim creation = TryCast(argumentList.Parent, ObjectCreationExpressionSyntax)
                        Dim info = SemanticModel.GetSymbolInfo(creation.Type)
                        Dim namedType = TryCast(info.Symbol, INamedTypeSymbol)
                        If namedType IsNot Nothing Then
                            If namedType.TypeKind = TypeKind.Delegate Then
                                Return CreateResult(namedType)
                            Else
                                Dim index As Integer = 0
                                If argumentOpt IsNot Nothing Then
                                    index = creation.ArgumentList.Arguments.IndexOf(argumentOpt)
                                Else
                                    index = GetArgumentListIndex(argumentList, previousToken)
                                End If

                                If index < 0 Then
                                    Return SpecializedCollections.EmptyEnumerable(Of TypeInferenceInfo)()
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

                        If index < 0 Then
                            Return SpecializedCollections.EmptyEnumerable(Of TypeInferenceInfo)()
                        End If

                        Dim info = SemanticModel.GetSymbolInfo(attribute)
                        Dim symbols = info.GetBestOrAllSymbols()
                        If symbols.Any() Then
                            Dim methods = symbols.OfType(Of IMethodSymbol)()
                            Return InferTypeInArgument(argumentOpt, index, methods)
                        End If

#If False Then
            ElseIf argument.Parent.IsParentKind(SyntaxKind.ElementAccessExpression) Then
                ' Outer[Goo()];
                '
                ' Outer[a: Goo()];
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

                Return SpecializedCollections.EmptyEnumerable(Of TypeInferenceInfo)()
            End Function

            Private Function InferTypeInArgument(argument As ArgumentSyntax, index As Integer, symbols As IEnumerable(Of ISymbol)) As IEnumerable(Of TypeInferenceInfo)
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

                Return SpecializedCollections.EmptyEnumerable(Of TypeInferenceInfo)()
            End Function

            Private Function InferTypeInArgument(
                argument As ArgumentSyntax,
                index As Integer,
                parameterizedSymbols As IEnumerable(Of ImmutableArray(Of IParameterSymbol))) As IEnumerable(Of TypeInferenceInfo)

                Dim simpleArgument = TryCast(argument, SimpleArgumentSyntax)

                If simpleArgument IsNot Nothing AndAlso simpleArgument.IsNamed Then
                    Dim parameters = parameterizedSymbols _
                                        .SelectMany(Function(m) m) _
                                        .Where(Function(p) p.Name = simpleArgument.NameColonEquals.Name.Identifier.ValueText)

                    Return parameters.Select(Function(p) New TypeInferenceInfo(p.Type, p.IsParams))
                Else
                    ' Otherwise, just take the first overload and pick what type this parameter is
                    ' based on index.
                    Return parameterizedSymbols.Where(Function(a) index < a.Length).Select(Function(a) New TypeInferenceInfo(a(index).Type, a(index).IsParams))
                End If

                Return SpecializedCollections.EmptyEnumerable(Of TypeInferenceInfo)()
            End Function

            Private Function InferTypeInArrayCreationExpression(arrayCreationExpression As ArrayCreationExpressionSyntax) As IEnumerable(Of TypeInferenceInfo)
                Dim outerTypes = InferTypes(arrayCreationExpression)
                Return outerTypes.Where(Function(c) TypeOf c.InferredType Is IArrayTypeSymbol) _
                        .Select(Function(c) New TypeInferenceInfo(DirectCast(c.InferredType, IArrayTypeSymbol).ElementType))
            End Function

            Private Function InferTypeInArrayRankSpecifier() As IEnumerable(Of TypeInferenceInfo)
                Return CreateResult(Me.Compilation.GetSpecialType(SpecialType.System_Int32))
            End Function

            Private Function InferTypeInArrayType(arrayType As ArrayTypeSyntax) As IEnumerable(Of TypeInferenceInfo)
                ' Bind the array type, then unwrap whatever we get back based on the number of rank
                ' specifiers we see.
                Dim currentTypes = InferTypes(arrayType)
                Dim i = 0
                While i < arrayType.RankSpecifiers.Count
                    currentTypes = currentTypes.WhereAsArray(Function(c) TypeOf c.InferredType Is IArrayTypeSymbol).
                                                SelectAsArray(Function(c) New TypeInferenceInfo(DirectCast(c.InferredType, IArrayTypeSymbol).ElementType))

                    i = i + 1
                End While

                Return currentTypes
            End Function

            Private Function InferTypeInAsClause(asClause As AsClauseSyntax,
                                                 Optional expressionOpt As ExpressionSyntax = Nothing,
                                                 Optional previousToken As SyntaxToken = Nothing) As IEnumerable(Of TypeInferenceInfo)
                If previousToken <> Nothing AndAlso previousToken.Kind <> SyntaxKind.AsKeyword Then
                    Return SpecializedCollections.EmptyEnumerable(Of TypeInferenceInfo)()
                End If

                If asClause.IsParentKind(SyntaxKind.CatchStatement) Then
                    If expressionOpt Is asClause.Type OrElse previousToken.Kind = SyntaxKind.AsKeyword Then
                        Return CreateResult(Me.Compilation.ExceptionType)
                    End If
                End If

                Return SpecializedCollections.EmptyEnumerable(Of TypeInferenceInfo)()
            End Function

            Private Function InferTypeInAssignmentStatement(assignmentStatement As AssignmentStatementSyntax,
                                                            Optional expressionOpt As ExpressionSyntax = Nothing,
                                                            Optional previousToken As SyntaxToken = Nothing) As IEnumerable(Of TypeInferenceInfo)

                If assignmentStatement.IsKind(SyntaxKind.LeftShiftAssignmentStatement) OrElse
                    assignmentStatement.IsKind(SyntaxKind.RightShiftAssignmentStatement) Then
                    Return CreateResult(Me.Compilation.GetSpecialType(SpecialType.System_Int32))
                End If

                If expressionOpt Is assignmentStatement.Right OrElse previousToken = assignmentStatement.OperatorToken Then
                    Return GetTypes(assignmentStatement.Left)
                End If

                If expressionOpt Is assignmentStatement.Left Then
                    Return GetTypes(assignmentStatement.Right)
                End If

                Return SpecializedCollections.EmptyEnumerable(Of TypeInferenceInfo)()
            End Function

            Private Function InferTypeInAttribute(attribute As AttributeSyntax) As IEnumerable(Of TypeInferenceInfo)
                Return CreateResult(Me.Compilation.AttributeType)
            End Function

            Private Function InferTypeInAwaitExpression(awaitExpression As AwaitExpressionSyntax) As IEnumerable(Of TypeInferenceInfo)
                ' await <expression>

                Dim types = InferTypes(awaitExpression, filterUnusable:=False)

                Dim task = Me.Compilation.GetTypeByMetadataName(GetType(Task).FullName)
                Dim taskOfT = Me.Compilation.GetTypeByMetadataName(GetType(Task(Of)).FullName)

                If task Is Nothing OrElse taskOfT Is Nothing Then
                    Return SpecializedCollections.EmptyEnumerable(Of TypeInferenceInfo)()
                End If

                If Not types.Any() Then
                    Return CreateResult(task)
                End If

                Return types.Select(Function(t) New TypeInferenceInfo(If(t.InferredType.SpecialType = SpecialType.System_Void, task, taskOfT.Construct(t.InferredType))))
            End Function

            Private Function InferTypeInConditionalAccessExpression(conditional As ConditionalAccessExpressionSyntax) As IEnumerable(Of TypeInferenceInfo)
                Return InferTypes(conditional)
            End Function

            Private Function InferTypeInBinaryConditionalExpression(conditional As BinaryConditionalExpressionSyntax,
                                                                    Optional expressionOpt As ExpressionSyntax = Nothing,
                                                                    Optional previousToken As SyntaxToken = Nothing) As IEnumerable(Of TypeInferenceInfo)
                If previousToken <> Nothing AndAlso previousToken <> conditional.OpenParenToken AndAlso previousToken <> conditional.CommaToken Then
                    Return SpecializedCollections.EmptyEnumerable(Of TypeInferenceInfo)()
                End If

                If conditional.FirstExpression Is expressionOpt OrElse previousToken = conditional.OpenParenToken Then
                    Dim rightTypes = GetTypes(conditional.SecondExpression, objectAsDefault:=True)
                    ' value type : If (Goo(), 0)
                    ' otherwise : If (Goo(), "")
                    Return rightTypes.Select(Function(t) If(t.InferredType.IsValueType,
                                                 New TypeInferenceInfo(Me.Compilation.GetSpecialType(SpecialType.System_Nullable_T).Construct(t.InferredType)),
                                                 t))

                Else
                    Dim leftTypes = GetTypes(conditional.FirstExpression)
                    Return leftTypes.Select(Function(t)
                                                If t.InferredType.OriginalDefinition.SpecialType = SpecialType.System_Nullable_T Then
                                                    Return New TypeInferenceInfo(DirectCast(t.InferredType, INamedTypeSymbol).TypeArguments(0))
                                                Else
                                                    Return t
                                                End If
                                            End Function)
                End If
            End Function

            Private Function InferTypeInBinaryExpression(binop As BinaryExpressionSyntax,
                                                         Optional expression As ExpressionSyntax = Nothing,
                                                         Optional previousToken As SyntaxToken = Nothing) As IEnumerable(Of TypeInferenceInfo)
                ' If we got a token, it must be the operator in the binary expression
                Contract.ThrowIfTrue(previousToken <> Nothing AndAlso binop.OperatorToken <> previousToken)

                Dim rightSide = previousToken <> Nothing OrElse expression Is binop.Right

                Select Case binop.OperatorToken.Kind
                    Case SyntaxKind.LessThanLessThanToken,
                        SyntaxKind.GreaterThanGreaterThanToken,
                        SyntaxKind.LessThanLessThanEqualsToken,
                        SyntaxKind.GreaterThanGreaterThanEqualsToken
                        If rightSide Then
                            Return CreateResult(Me.Compilation.GetSpecialType(SpecialType.System_Int32))
                        End If
                End Select

                ' Try to figure out what's on the other size of the binop.  If we can, then just that
                ' type.  This is often a reasonable heuristics to use for most operators.  NOTE(cyrusn):
                ' we could try to bind the token to see what overloaded operators it corresponds to.
                ' But the gain is pretty marginal IMO.
                Dim otherSide = If(rightSide, binop.Left, binop.Right)
                Dim otherSideTypes = GetTypes(otherSide)
                If otherSideTypes.Any(Function(t) t.InferredType.SpecialType <> SpecialType.System_Object AndAlso Not t.InferredType.IsErrorType()) Then
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
                        Return CreateResult(Me.Compilation.GetSpecialType(SpecialType.System_Int32))

                    Case SyntaxKind.AndKeyword,
                        SyntaxKind.AndAlsoKeyword,
                        SyntaxKind.OrKeyword,
                        SyntaxKind.OrElseKeyword
                        Return CreateResult(SpecialType.System_Boolean)
                End Select

                Return SpecializedCollections.EmptyEnumerable(Of TypeInferenceInfo)()
            End Function

            Private Function InferTypeInCastExpression(castExpression As CastExpressionSyntax,
                                                       Optional expressionOpt As ExpressionSyntax = Nothing) As IEnumerable(Of TypeInferenceInfo)
                If castExpression.Expression Is expressionOpt Then
                    Return SpecializedCollections.EmptyEnumerable(Of TypeInferenceInfo)()
                End If

                Return GetTypes(castExpression.Type)
            End Function

            Private Function InferTypeInCatchFilterClause(catchFilterClause As CatchFilterClauseSyntax, Optional previousToken As SyntaxToken = Nothing) As IEnumerable(Of TypeInferenceInfo)
                If previousToken <> Nothing AndAlso previousToken <> catchFilterClause.WhenKeyword Then
                    Return SpecializedCollections.EmptyEnumerable(Of TypeInferenceInfo)()
                End If

                Return CreateResult(SpecialType.System_Boolean)
            End Function

            Private Function InferTypeInDoStatement(Optional previousToken As SyntaxToken = Nothing) As IEnumerable(Of TypeInferenceInfo)
                Return CreateResult(SpecialType.System_Boolean)
            End Function

            Private Function InferTypeInEqualsValue(equalsValue As EqualsValueSyntax, Optional previousToken As SyntaxToken = Nothing) As IEnumerable(Of TypeInferenceInfo)
                If equalsValue.IsParentKind(SyntaxKind.VariableDeclarator) Then
                    Dim variableDeclarator = DirectCast(equalsValue.Parent, VariableDeclaratorSyntax)
                    If variableDeclarator.AsClause Is Nothing AndAlso variableDeclarator.IsParentKind(SyntaxKind.UsingStatement) Then
                        Return CreateResult(SpecialType.System_IDisposable)
                    End If

                    If variableDeclarator.Names.Count >= 1 Then
                        Dim name = variableDeclarator.Names(0)
                        Dim symbol = SemanticModel.GetDeclaredSymbol(name, CancellationToken)

                        If symbol IsNot Nothing Then
                            Select Case symbol.Kind
                                Case SymbolKind.Field
                                    Return CreateResult(DirectCast(symbol, IFieldSymbol).Type)
                                Case SymbolKind.Local
                                    Return CreateResult(DirectCast(symbol, ILocalSymbol).Type)
                            End Select
                        End If
                    End If

                    If TypeOf variableDeclarator.AsClause Is SimpleAsClauseSyntax Then
                        Dim asClause = DirectCast(variableDeclarator.AsClause, SimpleAsClauseSyntax)
                        Return GetTypes(asClause.Type)
                    End If
                ElseIf equalsValue.IsParentKind(SyntaxKind.PropertyStatement) Then
                    Dim propertySyntax = CType(equalsValue.Parent, PropertyStatementSyntax)
                    Dim propertySymbol = SemanticModel.GetDeclaredSymbol(propertySyntax)
                    If propertySymbol Is Nothing Then
                        Return SpecializedCollections.EmptyEnumerable(Of TypeInferenceInfo)()
                    End If

                    Return CreateResult(propertySymbol.Type)
                End If

                Return SpecializedCollections.EmptyEnumerable(Of TypeInferenceInfo)()
            End Function

            Private Function InferTypeInExpressionStatement(expressionStatement As ExpressionStatementSyntax) As IEnumerable(Of TypeInferenceInfo)
                If expressionStatement.Expression.IsKind(SyntaxKind.InvocationExpression) Then
                    Return InferTypeInCallStatement()
                End If
                Return SpecializedCollections.EmptyEnumerable(Of TypeInferenceInfo)()
            End Function

            Private Function InferTypeInCallStatement() As IEnumerable(Of TypeInferenceInfo)
                Return CreateResult(SpecialType.System_Void)
            End Function

            Private Function InferTypeInForEachStatement(forEachStatement As ForEachStatementSyntax,
                                                         Optional expressionOpt As ExpressionSyntax = Nothing,
                                                         Optional previousToken As SyntaxToken = Nothing) As IEnumerable(Of TypeInferenceInfo)
                If expressionOpt Is forEachStatement.Expression OrElse previousToken = forEachStatement.InKeyword Then
                    If TypeOf forEachStatement.ControlVariable Is VariableDeclaratorSyntax Then
                        Dim declarator = DirectCast(forEachStatement.ControlVariable, VariableDeclaratorSyntax)
                        If TypeOf declarator.AsClause Is SimpleAsClauseSyntax Then
                            Dim variableTypes = GetTypes(DirectCast(declarator.AsClause, SimpleAsClauseSyntax).Type, objectAsDefault:=True)

                            Dim type = Me.Compilation.GetSpecialType(SpecialType.System_Collections_Generic_IEnumerable_T)
                            Return variableTypes.Select(Function(t) New TypeInferenceInfo(type.Construct(t.InferredType)))
                        End If
                    ElseIf TypeOf forEachStatement.ControlVariable Is SimpleNameSyntax Then
                        Dim type = Me.Compilation.GetSpecialType(SpecialType.System_Collections_Generic_IEnumerable_T)
                        Return CreateResult(type.Construct(Compilation.GetSpecialType(SpecialType.System_Object)))
                    End If
                End If

                Return SpecializedCollections.EmptyEnumerable(Of TypeInferenceInfo)()
            End Function

            Private Function InferTypeInForStatement(forStatement As ForStatementSyntax,
                                                     Optional expressionOpt As ExpressionSyntax = Nothing,
                                                     Optional previousToken As SyntaxToken = Nothing) As IEnumerable(Of TypeInferenceInfo)
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

                Return SpecializedCollections.EmptyEnumerable(Of TypeInferenceInfo)()
            End Function

            Private Function InferTypeInForStepClause(forStepClause As ForStepClauseSyntax, Optional previousToken As SyntaxToken = Nothing) As IEnumerable(Of TypeInferenceInfo)
                ' TODO(cyrusn): Potentially infer a different type based on the type of the variable
                ' being foreach-ed over.
                Return CreateResult(Me.Compilation.GetSpecialType(SpecialType.System_Int32))
            End Function

            Private Function InferTypeInIfOrElseIfStatement(Optional previousToken As SyntaxToken = Nothing) As IEnumerable(Of TypeInferenceInfo)
                Return CreateResult(SpecialType.System_Boolean)
            End Function

            Private Function InferTypeInLambda(lambda As ExpressionSyntax, Optional previousToken As SyntaxToken = Nothing) As IEnumerable(Of TypeInferenceInfo)
                If lambda Is Nothing Then
                    Return SpecializedCollections.EmptyEnumerable(Of TypeInferenceInfo)()
                End If

                ' Func<int,string> = i => Goo();
                Dim lambdaTypes = GetTypes(lambda).Where(IsUsableTypeFunc)
                If lambdaTypes.IsEmpty() Then
                    lambdaTypes = InferTypes(lambda)
                End If

                Return lambdaTypes.Where(Function(t) t.InferredType.TypeKind = TypeKind.Delegate).SelectMany(Function(t) t.InferredType.GetMembers(WellKnownMemberNames.DelegateInvokeName).OfType(Of IMethodSymbol)().Select(Function(m) New TypeInferenceInfo(m.ReturnType)))

                Return SpecializedCollections.EmptyEnumerable(Of TypeInferenceInfo)()
            End Function

            Private Function InferTypeForReturnStatement(returnStatement As ReturnStatementSyntax, Optional previousToken As SyntaxToken = Nothing) As IEnumerable(Of TypeInferenceInfo)
                ' If we're position based, we must have gotten the Return token
                If previousToken <> Nothing AndAlso previousToken.Kind <> SyntaxKind.ReturnKeyword Then
                    Return SpecializedCollections.EmptyEnumerable(Of TypeInferenceInfo)()
                End If

                ' If we're in a lambda, then use the return type of the lambda to figure out what to
                ' infer.  i.e.   Func<int,string> f = i => { return Goo(); }
                Dim lambda = returnStatement.GetAncestorsOrThis(Of ExpressionSyntax)().FirstOrDefault(
                    Function(e) TypeOf e Is MultiLineLambdaExpressionSyntax OrElse
                        TypeOf e Is SingleLineLambdaExpressionSyntax)
                If lambda IsNot Nothing Then
                    Return InferTypeInLambda(lambda)
                End If

                If returnStatement.GetAncestorOrThis(Of MethodBlockBaseSyntax) Is Nothing Then
                    Return SpecializedCollections.EmptyEnumerable(Of TypeInferenceInfo)
                End If

                Dim memberSymbol = GetDeclaredMemberSymbolFromOriginalSemanticModel(SemanticModel, returnStatement.GetAncestor(Of MethodBlockBaseSyntax).BlockStatement)

                Dim memberMethod = TryCast(memberSymbol, IMethodSymbol)
                If memberMethod IsNot Nothing Then

                    If memberMethod.IsAsync Then
                        Dim typeArguments = memberMethod.ReturnType.GetTypeArguments()
                        Dim taskOfT = Me.Compilation.TaskOfTType()

                        Return If(
                            taskOfT IsNot Nothing AndAlso Equals(memberMethod.ReturnType.OriginalDefinition, taskOfT) AndAlso typeArguments.Any(),
                            CreateResult(typeArguments.First()),
                            SpecializedCollections.EmptyEnumerable(Of TypeInferenceInfo))
                    Else
                        Return CreateResult(memberMethod.ReturnType)
                    End If
                End If

                Return SpecializedCollections.EmptyEnumerable(Of TypeInferenceInfo)()
            End Function

            Private Function InferTypeInYieldStatement(yieldStatement As YieldStatementSyntax, Optional previoustoken As SyntaxToken = Nothing) As IEnumerable(Of TypeInferenceInfo)
                ' If we're position based, we must be after the Yield token
                If previoustoken <> Nothing AndAlso Not previoustoken.IsKind(SyntaxKind.YieldKeyword) Then
                    Return SpecializedCollections.EmptyEnumerable(Of TypeInferenceInfo)
                End If

                If yieldStatement.GetAncestorOrThis(Of MethodBlockBaseSyntax) Is Nothing Then
                    Return SpecializedCollections.EmptyEnumerable(Of TypeInferenceInfo)
                End If

                Dim memberSymbol = GetDeclaredMemberSymbolFromOriginalSemanticModel(SemanticModel, yieldStatement.GetAncestor(Of MethodBlockBaseSyntax).BlockStatement)

                Dim memberType = If(TryCast(memberSymbol, IMethodSymbol)?.ReturnType,
                                    TryCast(memberSymbol, IPropertySymbol)?.Type)

                If TypeOf memberType Is INamedTypeSymbol Then
                    If memberType.OriginalDefinition.SpecialType = SpecialType.System_Collections_Generic_IEnumerable_T OrElse
                       memberType.OriginalDefinition.SpecialType = SpecialType.System_Collections_Generic_IEnumerator_T Then

                        Return CreateResult(DirectCast(memberType, INamedTypeSymbol).TypeArguments(0))
                    End If
                End If

                Return SpecializedCollections.EmptyEnumerable(Of TypeInferenceInfo)
            End Function

            Private Function GetDeclaredMemberSymbolFromOriginalSemanticModel(currentSemanticModel As SemanticModel, declarationInCurrentTree As DeclarationStatementSyntax) As ISymbol
                Dim originalSemanticModel = currentSemanticModel.GetOriginalSemanticModel()
                Dim declaration As DeclarationStatementSyntax

                If currentSemanticModel.IsSpeculativeSemanticModel Then
                    Dim tokenInOriginalTree = originalSemanticModel.SyntaxTree.GetRoot(CancellationToken).FindToken(currentSemanticModel.OriginalPositionForSpeculation)
                    declaration = tokenInOriginalTree.GetAncestor(Of DeclarationStatementSyntax)
                Else
                    declaration = declarationInCurrentTree
                End If

                Return originalSemanticModel.GetDeclaredSymbol(declaration, CancellationToken)
            End Function

            Private Function InferTypeInSelectStatement(switchStatementSyntax As SelectStatementSyntax, Optional previousToken As SyntaxToken = Nothing) As IEnumerable(Of TypeInferenceInfo)
                ' Use the first case label to determine the return type.
                If TypeOf switchStatementSyntax.Parent Is SelectBlockSyntax Then
                    Dim firstCase = DirectCast(switchStatementSyntax.Parent, SelectBlockSyntax).CaseBlocks.SelectMany(Function(c) c.CaseStatement.Cases).OfType(Of SimpleCaseClauseSyntax).FirstOrDefault()
                    If firstCase IsNot Nothing Then
                        Return GetTypes(firstCase.Value)
                    End If
                End If

                Return CreateResult(Me.Compilation.GetSpecialType(SpecialType.System_Int32))
            End Function

            Private Function InferTypeInTernaryConditionalExpression(conditional As TernaryConditionalExpressionSyntax,
                                                                     Optional expressionOpt As ExpressionSyntax = Nothing,
                                                                     Optional previousToken As SyntaxToken = Nothing) As IEnumerable(Of TypeInferenceInfo)
                If previousToken <> Nothing AndAlso previousToken.Kind <> SyntaxKind.OpenParenToken AndAlso previousToken.Kind <> SyntaxKind.CommaToken Then
                    Return SpecializedCollections.EmptyEnumerable(Of TypeInferenceInfo)()
                ElseIf previousToken = conditional.OpenParenToken Then
                    Return CreateResult(SpecialType.System_Boolean)
                ElseIf previousToken = conditional.FirstCommaToken Then
                    Return GetTypes(conditional.WhenTrue)
                ElseIf previousToken = conditional.SecondCommaToken Then
                    Return GetTypes(conditional.WhenFalse)
                End If

                If conditional.Condition Is expressionOpt Then
                    Return CreateResult(SpecialType.System_Boolean)
                Else
                    Return If(conditional.WhenTrue Is expressionOpt, GetTypes(conditional.WhenFalse), GetTypes(conditional.WhenTrue))
                End If
            End Function

            Private Function InferTypeInThrowStatement(Optional previousToken As SyntaxToken = Nothing) As IEnumerable(Of TypeInferenceInfo)
                ' If we're not the Throw token, there's nothing to do
                If previousToken <> Nothing AndAlso previousToken.Kind <> SyntaxKind.ThrowKeyword Then
                    Return SpecializedCollections.EmptyEnumerable(Of TypeInferenceInfo)()
                End If

                Return CreateResult(Me.Compilation.ExceptionType)
            End Function

            Private Function InferTypeInUnaryExpression(unaryExpressionSyntax As UnaryExpressionSyntax, Optional previousToken As SyntaxToken = Nothing) As IEnumerable(Of TypeInferenceInfo)
                Select Case unaryExpressionSyntax.Kind
                    Case SyntaxKind.UnaryPlusExpression, SyntaxKind.UnaryMinusExpression
                        Return CreateResult(Me.Compilation.GetSpecialType(SpecialType.System_Int32))
                    Case SyntaxKind.NotExpression
                        Dim types = InferTypes(unaryExpressionSyntax)
                        If types.Any(Function(t) t.InferredType.IsNumericType) Then
                            Return types.Where(Function(t) t.InferredType.IsNumericType)
                        End If

                        Return CreateResult(SpecialType.System_Boolean)
                    Case SyntaxKind.AddressOfExpression
                        Return InferTypes(unaryExpressionSyntax)
                End Select

                Return SpecializedCollections.EmptyEnumerable(Of TypeInferenceInfo)()
            End Function

            Private Function InferTypeInUsingStatement(usingStatement As UsingStatementSyntax, Optional previousToken As SyntaxToken = Nothing) As IEnumerable(Of TypeInferenceInfo)
                Return CreateResult(SpecialType.System_IDisposable)
            End Function

            Private Function InferTypeInVariableDeclarator(expression As ExpressionSyntax,
                                                           variableDeclarator As VariableDeclaratorSyntax,
                                                           Optional previousToken As SyntaxToken = Nothing) As IEnumerable(Of TypeInferenceInfo)
                If variableDeclarator.AsClause IsNot Nothing Then
                    Return SpecializedCollections.EmptyEnumerable(Of TypeInferenceInfo)()
                End If

                Return GetTypes(variableDeclarator.AsClause.Type)
            End Function

            Private Function InferTypeInWhileStatement(Optional previousToken As SyntaxToken = Nothing) As IEnumerable(Of TypeInferenceInfo)
                Return CreateResult(SpecialType.System_Boolean)
            End Function

            Private Function InferTypeInWhileOrUntilClause(Optional previousToken As SyntaxToken = Nothing) As IEnumerable(Of TypeInferenceInfo)
                Return CreateResult(SpecialType.System_Boolean)
            End Function

            Private Function InferTypeInMemberAccessExpression(
                    memberAccessExpression As MemberAccessExpressionSyntax,
                    Optional expressionOpt As ExpressionSyntax = Nothing,
                    Optional previousToken As SyntaxToken? = Nothing) As IEnumerable(Of TypeInferenceInfo)

                ' We need to be on the right of the dot to infer an appropriate type for
                ' the member access expression.  i.e. if we have "Goo.Bar" then we can 
                ' def infer what the type of 'Bar' should be (it's whatever type we infer
                ' for 'Goo.Bar' itself.  However, if we're on 'Goo' then we can't figure
                ' out anything about its type.
                If previousToken <> Nothing Then
                    If previousToken.Value <> memberAccessExpression.OperatorToken Then
                        Return SpecializedCollections.EmptyEnumerable(Of TypeInferenceInfo)
                    End If

                    Return InferTypes(memberAccessExpression)
                Else
                    ' If we're on the left side of a dot, it's possible in a few cases
                    ' to figure out what type we should be.  Specifically, if we have
                    '
                    '      await goo.ConfigureAwait()
                    '
                    ' then we can figure out what 'goo' should be based on teh await
                    ' context.
                    If expressionOpt Is memberAccessExpression.Expression Then
                        Return InferTypeForExpressionOfMemberAccessExpression(memberAccessExpression)
                    End If

                    Return InferTypes(memberAccessExpression)
                End If
            End Function

            Private Function InferTypeForExpressionOfMemberAccessExpression(memberAccessExpression As MemberAccessExpressionSyntax) As IEnumerable(Of TypeInferenceInfo)
                Dim name = memberAccessExpression.Name.Identifier.Value

                If name.Equals(NameOf(Task(Of Integer).ConfigureAwait)) AndAlso
                   memberAccessExpression.IsParentKind(SyntaxKind.InvocationExpression) AndAlso
                   memberAccessExpression.Parent.IsParentKind(SyntaxKind.AwaitExpression) Then
                    Return InferTypes(DirectCast(memberAccessExpression.Parent, ExpressionSyntax))
                ElseIf name.Equals(NameOf(Task(Of Integer).ContinueWith)) Then
                    ' goo.ContinueWith(...)
                    ' We want to infer Task<T>.  For now, we'll just do Task<object>,
                    ' in the future it would be nice to figure out the actual result
                    ' type based on the argument to ContinueWith.
                    Dim taskOfT = Me.Compilation.TaskOfTType()
                    If taskOfT IsNot Nothing Then
                        Return CreateResult(taskOfT.Construct(Me.Compilation.ObjectType))
                    End If
                ElseIf name.Equals(NameOf(Enumerable.Select)) OrElse
                       name.Equals(NameOf(Enumerable.Where)) Then

                    Dim ienumerableType = Me.Compilation.IEnumerableOfTType()

                    ' goo.Select
                    ' We want to infer IEnumerable<T>.  We can try to figure out what 
                    ' T if we get a delegate as the first argument to Select/Where.
                    If ienumerableType IsNot Nothing AndAlso memberAccessExpression.IsParentKind(SyntaxKind.InvocationExpression) Then
                        Dim invocation = DirectCast(memberAccessExpression.Parent, InvocationExpressionSyntax)
                        If invocation.ArgumentList IsNot Nothing AndAlso invocation.ArgumentList.Arguments.Count > 0 AndAlso
                           TypeOf invocation.ArgumentList.Arguments(0) Is SimpleArgumentSyntax Then
                            Dim argumentExpression = DirectCast(invocation.ArgumentList.Arguments(0), SimpleArgumentSyntax).Expression
                            Dim argumentTypes = GetTypes(argumentExpression)
                            Dim delegateType = argumentTypes.FirstOrDefault().InferredType.GetDelegateType(Me.Compilation)
                            Dim typeArg = If(delegateType?.TypeArguments.Length > 0,
                                delegateType.TypeArguments(0),
                                Me.Compilation.ObjectType)

                            If delegateType Is Nothing OrElse IsUnusableType(typeArg) Then
                                If TypeOf argumentExpression Is LambdaExpressionSyntax Then
                                    typeArg = If(InferTypeForFirstParameterOfLambda(DirectCast(argumentExpression, LambdaExpressionSyntax)),
                                    Me.Compilation.ObjectType)
                                End If
                            End If

                            Return CreateResult(ienumerableType.Construct(typeArg))
                        End If
                    End If
                End If

                Return SpecializedCollections.EmptyEnumerable(Of TypeInferenceInfo)()
            End Function

            Private Function InferTypeForFirstParameterOfLambda(
                    lambda As LambdaExpressionSyntax) As ITypeSymbol
                If lambda.SubOrFunctionHeader.ParameterList.Parameters.Count > 0 Then
                    Dim parameter = lambda.SubOrFunctionHeader.ParameterList.Parameters(0)
                    Dim parameterName = parameter.Identifier.Identifier.ValueText

                    If TypeOf lambda Is SingleLineLambdaExpressionSyntax Then
                        Dim singleLine = DirectCast(lambda, SingleLineLambdaExpressionSyntax)
                        Return InferTypeForFirstParameterOfLambda(parameterName, singleLine.Body)
                    ElseIf TypeOf lambda Is MultiLineLambdaExpressionSyntax Then
                        Dim multiLine = DirectCast(lambda, MultiLineLambdaExpressionSyntax)
                        For Each statement In multiLine.Statements
                            Dim type = InferTypeForFirstParameterOfLambda(parameterName, statement)
                            If type IsNot Nothing Then
                                Return type
                            End If
                        Next
                    End If
                End If

                Return Nothing
            End Function

            Private Function InferTypeForFirstParameterOfLambda(
                    parameterName As String, node As SyntaxNode) As ITypeSymbol
                If node.IsKind(SyntaxKind.IdentifierName) Then
                    Dim identifier = DirectCast(node, IdentifierNameSyntax)
                    If CaseInsensitiveComparison.Equals(parameterName, identifier.Identifier.ValueText) AndAlso
                       SemanticModel.GetSymbolInfo(identifier.Identifier).Symbol?.Kind = SymbolKind.Parameter Then
                        Return InferTypes(identifier).FirstOrDefault().InferredType
                    End If
                Else
                    For Each child In node.ChildNodesAndTokens()
                        If child.IsNode Then
                            Dim type = InferTypeForFirstParameterOfLambda(parameterName, child.AsNode)
                            If type IsNot Nothing Then
                                Return type
                            End If
                        End If
                    Next
                End If

                Return Nothing
            End Function

            Private Function InferTypeInNamedFieldInitializer(initializer As NamedFieldInitializerSyntax, Optional previousToken As SyntaxToken = Nothing) As IEnumerable(Of TypeInferenceInfo)
                Dim right = SemanticModel.GetTypeInfo(initializer.Name).Type
                If right IsNot Nothing AndAlso TypeOf right IsNot IErrorTypeSymbol Then
                    Return CreateResult(right)
                End If

                Return CreateResult(SemanticModel.GetTypeInfo(initializer.Expression).Type)
            End Function

            Public Function InferTypeInCaseStatement(caseStatement As CaseStatementSyntax) As IEnumerable(Of TypeInferenceInfo)
                Dim selectBlock = caseStatement.GetAncestor(Of SelectBlockSyntax)()
                If selectBlock IsNot Nothing Then
                    Return GetTypes(selectBlock.SelectStatement.Expression)
                End If

                Return SpecializedCollections.EmptyEnumerable(Of TypeInferenceInfo)()
            End Function

            Private Function GetArgumentListIndex(argumentList As ArgumentListSyntax, previousToken As SyntaxToken) As Integer
                If previousToken = argumentList.OpenParenToken Then
                    Return 0
                End If

                Dim index = argumentList.Arguments.GetWithSeparators().IndexOf(previousToken)
                Return If(index >= 0, (index + 1) \ 2, -1)
            End Function

            Private Function InferTypeInCollectionInitializerExpression(
                collectionInitializer As CollectionInitializerSyntax,
                Optional expression As ExpressionSyntax = Nothing,
                Optional previousToken As SyntaxToken? = Nothing) As IEnumerable(Of TypeInferenceInfo)

                ' New List(Of T) From { x }
                If expression IsNot Nothing Then
                    Dim expressionAddMethodSymbols = SemanticModel.GetCollectionInitializerSymbolInfo(expression).GetAllSymbols()
                    Dim expressionAddMethodParameterTypes = expressionAddMethodSymbols _
                        .Where(Function(a) DirectCast(a, IMethodSymbol).Parameters.Length = 1) _
                        .Select(Function(a) New TypeInferenceInfo(DirectCast(a, IMethodSymbol).Parameters(0).Type))

                    If expressionAddMethodParameterTypes.Any() Then
                        Return expressionAddMethodParameterTypes
                    End If
                End If

                ' New Dictionary<K,V> From { { x, ... } }
                Dim parameterIndex = If(previousToken.HasValue,
                        collectionInitializer.Initializers.GetSeparators().ToList().IndexOf(previousToken.Value) + 1,
                        collectionInitializer.Initializers.IndexOf(expression))

                Dim initializerAddMethodSymbols = SemanticModel.GetCollectionInitializerSymbolInfo(collectionInitializer).GetAllSymbols()
                Dim initializerAddMethodParameterTypes = initializerAddMethodSymbols _
                    .Where(Function(a) DirectCast(a, IMethodSymbol).Parameters.Length = collectionInitializer.Initializers.Count) _
                    .Select(Function(a) DirectCast(a, IMethodSymbol).Parameters.ElementAtOrDefault(parameterIndex)?.Type) _
                    .WhereNotNull() _
                    .Select(Function(a) New TypeInferenceInfo(a))


                If initializerAddMethodParameterTypes.Any() Then
                    Return initializerAddMethodParameterTypes
                End If

                Return SpecializedCollections.EmptyEnumerable(Of TypeInferenceInfo)
            End Function
        End Class
    End Class
End Namespace
