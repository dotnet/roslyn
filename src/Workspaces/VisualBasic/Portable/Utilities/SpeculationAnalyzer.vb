' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Generic
Imports System.Collections.Immutable
Imports System.Diagnostics
Imports System.Linq
Imports System.Threading
Imports Microsoft.CodeAnalysis.VisualBasic.Extensions
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Microsoft.CodeAnalysis.Shared.Extensions

Namespace Microsoft.CodeAnalysis.VisualBasic.Utilities
    ''' <summary>
    ''' Helper class to analyze the semantic effects of a speculated syntax node replacement on the parenting nodes.
    ''' Given an expression node from a syntax tree and a new expression from a different syntax tree,
    ''' it replaces the expression with the new expression to create a speculated syntax tree.
    ''' It uses the original tree's semantic model to create a speculative semantic model and verifies that
    ''' the syntax replacement doesn't break the semantics of any parenting nodes of the original expression.
    ''' </summary>
    Friend Class SpeculationAnalyzer
        Inherits AbstractSpeculationAnalyzer(Of SyntaxNode, ExpressionSyntax, TypeSyntax, AttributeSyntax,
                                             ArgumentSyntax, ForEachStatementSyntax, ThrowStatementSyntax, SemanticModel)

        ''' <summary>
        ''' Creates a semantic analyzer for speculative syntax replacement.
        ''' </summary>
        ''' <param name="expression">Original expression to be replaced.</param>
        ''' <param name="newExpression">New expression to replace the original expression.</param>
        ''' <param name="semanticModel">Semantic model of <paramref name="expression"/> node's syntax tree.</param>
        ''' <param name="cancellationToken">Cancellation token.</param>
        ''' <param name="skipVerificationForReplacedNode">
        ''' True if semantic analysis should be skipped for the replaced node and performed starting from parent of the original and replaced nodes.
        ''' This could be the case when custom verifications are required to be done by the caller or
        ''' semantics of the replaced expression are different from the original expression.
        ''' </param>
        ''' <param name="failOnOverloadResolutionFailuresInOriginalCode">
        ''' True if semantic analysis should fail when any of the invocation expression ancestors of <paramref name="expression"/> in original code has overload resolution failures.
        ''' </param>        
        Public Sub New(expression As ExpressionSyntax, newExpression As ExpressionSyntax, semanticModel As SemanticModel, cancellationToken As CancellationToken, Optional skipVerificationForReplacedNode As Boolean = False, Optional failOnOverloadResolutionFailuresInOriginalCode As Boolean = False)
            MyBase.New(expression, newExpression, semanticModel, cancellationToken, skipVerificationForReplacedNode, failOnOverloadResolutionFailuresInOriginalCode)
        End Sub

        Protected Overrides Function GetSemanticRootForSpeculation(expression As ExpressionSyntax) As SyntaxNode
            Debug.Assert(expression IsNot Nothing)

            Dim parentNodeToSpeculate = expression _
                .AncestorsAndSelf(ascendOutOfTrivia:=False) _
                .Where(Function(node) CanSpeculateOnNode(node)) _
                .LastOrDefault()

            If parentNodeToSpeculate Is Nothing Then
                parentNodeToSpeculate = expression
            End If

            Return parentNodeToSpeculate
        End Function

        Public Shared Function CanSpeculateOnNode(node As SyntaxNode) As Boolean
            Return TypeOf node Is ExecutableStatementSyntax OrElse
                TypeOf node Is TypeSyntax OrElse
                node.Kind = SyntaxKind.Attribute OrElse
                node.Kind = SyntaxKind.EqualsValue OrElse
                node.Kind = SyntaxKind.AsNewClause OrElse
                node.Kind = SyntaxKind.RangeArgument
        End Function

        Protected Overrides Function GetSemanticRootOfReplacedExpression(semanticRootOfOriginalExpr As SyntaxNode, annotatedReplacedExpression As ExpressionSyntax) As SyntaxNode
            Dim originalExpression = Me.OriginalExpression

            ' Speculation is not supported for AsNewClauseSyntax nodes.
            ' Generate an EqualsValueSyntax node with the inner NewExpression of the AsNewClauseSyntax node for speculation.
            If semanticRootOfOriginalExpr.Kind = SyntaxKind.AsNewClause Then
                ' Because the original expression will change identity in the newly generated EqualsValueSyntax node,
                ' we annotate it here to allow us to get back to it after replace.
                Dim originalExprAnnotation = New SyntaxAnnotation()
                Dim annotatedOriginalExpression = originalExpression.WithAdditionalAnnotations(originalExprAnnotation)
                semanticRootOfOriginalExpr = semanticRootOfOriginalExpr.ReplaceNode(originalExpression, annotatedOriginalExpression)

                Dim asNewClauseNode = DirectCast(semanticRootOfOriginalExpr, AsNewClauseSyntax)
                semanticRootOfOriginalExpr = SyntaxFactory.EqualsValue(asNewClauseNode.NewExpression)
                semanticRootOfOriginalExpr = asNewClauseNode.CopyAnnotationsTo(semanticRootOfOriginalExpr)
                originalExpression = DirectCast(semanticRootOfOriginalExpr.GetAnnotatedNodesAndTokens(originalExprAnnotation).Single().AsNode(), ExpressionSyntax)
            End If

            Return semanticRootOfOriginalExpr.ReplaceNode(originalExpression, annotatedReplacedExpression)
        End Function

        Protected Overrides Sub ValidateSpeculativeSemanticModel(speculativeSemanticModel As SemanticModel, nodeToSpeculate As SyntaxNode)
            Debug.Assert(speculativeSemanticModel IsNot Nothing OrElse
                        TypeOf nodeToSpeculate Is ExpressionSyntax OrElse
                        Me.SemanticRootOfOriginalExpression.GetAncestors().Any(Function(node) node.IsKind(SyntaxKind.IncompleteMember)),
                        "SemanticModel.TryGetSpeculativeSemanticModel() API returned false.")
        End Sub

        Protected Overrides Function CreateSpeculativeSemanticModel(originalNode As SyntaxNode, nodeToSpeculate As SyntaxNode, semanticModel As SemanticModel) As SemanticModel
            Return CreateSpeculativeSemanticModelForNode(originalNode, nodeToSpeculate, semanticModel)
        End Function

        Public Shared Function CreateSpeculativeSemanticModelForNode(originalNode As SyntaxNode, nodeToSpeculate As SyntaxNode, semanticModel As SemanticModel) As SemanticModel
            Dim position = originalNode.SpanStart
            Dim isInNamespaceOrTypeContext = SyntaxFacts.IsInNamespaceOrTypeContext(TryCast(originalNode, ExpressionSyntax))
            Return CreateSpeculativeSemanticModelForNode(nodeToSpeculate, semanticModel, position, isInNamespaceOrTypeContext)
        End Function

        Public Shared Function CreateSpeculativeSemanticModelForNode(nodeToSpeculate As SyntaxNode, semanticModel As SemanticModel, position As Integer, isInNamespaceOrTypeContext As Boolean) As SemanticModel
            If semanticModel.IsSpeculativeSemanticModel Then
                ' Chaining speculative model Not supported, speculate off the original model.
                Debug.Assert(semanticModel.ParentModel IsNot Nothing)
                Debug.Assert(Not semanticModel.ParentModel.IsSpeculativeSemanticModel)
                position = semanticModel.OriginalPositionForSpeculation
                semanticModel = semanticModel.ParentModel
            End If

            Dim speculativeModel As SemanticModel = Nothing
            Dim statementNode = TryCast(nodeToSpeculate, ExecutableStatementSyntax)
            If statementNode IsNot Nothing Then
                semanticModel.TryGetSpeculativeSemanticModel(position, statementNode, speculativeModel)
                Return speculativeModel
            End If

            Dim type = TryCast(nodeToSpeculate, TypeSyntax)
            If type IsNot Nothing Then
                Dim bindingOption = If(isInNamespaceOrTypeContext,
                                       SpeculativeBindingOption.BindAsTypeOrNamespace,
                                       SpeculativeBindingOption.BindAsExpression)
                semanticModel.TryGetSpeculativeSemanticModel(position, type, speculativeModel, bindingOption)
                Return speculativeModel
            End If

            Select Case nodeToSpeculate.Kind
                Case SyntaxKind.Attribute
                    semanticModel.TryGetSpeculativeSemanticModel(position, DirectCast(nodeToSpeculate, AttributeSyntax), speculativeModel)
                    Return speculativeModel

                Case SyntaxKind.EqualsValue
                    semanticModel.TryGetSpeculativeSemanticModel(position, DirectCast(nodeToSpeculate, EqualsValueSyntax), speculativeModel)
                    Return speculativeModel

                Case SyntaxKind.RangeArgument
                    semanticModel.TryGetSpeculativeSemanticModel(position, DirectCast(nodeToSpeculate, RangeArgumentSyntax), speculativeModel)
                    Return speculativeModel
            End Select

            ' CONSIDER: Do we care about this case?
            Debug.Assert(TypeOf nodeToSpeculate Is ExpressionSyntax)
            Return Nothing
        End Function

#Region "Semantic comparison helpers"

        Private Function QuerySymbolsAreCompatible(originalNode As CollectionRangeVariableSyntax, newNode As CollectionRangeVariableSyntax) As Boolean
            Debug.Assert(originalNode IsNot Nothing)
            Debug.Assert(Me.SemanticRootOfOriginalExpression.DescendantNodesAndSelf().Contains(originalNode))
            Debug.Assert(newNode IsNot Nothing)
            Debug.Assert(Me.SemanticRootOfReplacedExpression.DescendantNodesAndSelf().Contains(newNode))

            Dim originalSymbolInfo = Me.OriginalSemanticModel.GetCollectionRangeVariableSymbolInfo(originalNode)
            Dim newSymbolInfo = Me.SpeculativeSemanticModel.GetCollectionRangeVariableSymbolInfo(newNode)
            Return SymbolInfosAreCompatible(originalSymbolInfo, newSymbolInfo)
        End Function

        Private Function QuerySymbolsAreCompatible(originalNode As AggregateClauseSyntax, newNode As AggregateClauseSyntax) As Boolean
            Debug.Assert(originalNode IsNot Nothing)
            Debug.Assert(Me.SemanticRootOfOriginalExpression.DescendantNodesAndSelf().Contains(originalNode))
            Debug.Assert(newNode IsNot Nothing)
            Debug.Assert(Me.SemanticRootOfReplacedExpression.DescendantNodesAndSelf().Contains(newNode))

            Dim originalSymbolInfo = Me.OriginalSemanticModel.GetAggregateClauseSymbolInfo(originalNode)
            Dim newSymbolInfo = Me.SpeculativeSemanticModel.GetAggregateClauseSymbolInfo(newNode)
            Return SymbolInfosAreCompatible(originalSymbolInfo, newSymbolInfo)
        End Function

        Private Function QuerySymbolsAreCompatible(originalNode As ExpressionRangeVariableSyntax, newNode As ExpressionRangeVariableSyntax) As Boolean
            Debug.Assert(originalNode IsNot Nothing)
            Debug.Assert(Me.SemanticRootOfOriginalExpression.DescendantNodesAndSelf().Contains(originalNode))
            Debug.Assert(newNode IsNot Nothing)
            Debug.Assert(Me.SemanticRootOfReplacedExpression.DescendantNodesAndSelf().Contains(newNode))

            Dim originalSymbolInfo = Me.OriginalSemanticModel.GetSymbolInfo(originalNode)
            Dim newSymbolInfo = Me.SpeculativeSemanticModel.GetSymbolInfo(newNode)
            Return SymbolInfosAreCompatible(originalSymbolInfo, newSymbolInfo)
        End Function

        Private Function QuerySymbolsAreCompatible(originalNode As OrderingSyntax, newNode As OrderingSyntax) As Boolean
            Debug.Assert(originalNode IsNot Nothing)
            Debug.Assert(Me.SemanticRootOfOriginalExpression.DescendantNodesAndSelf().Contains(originalNode))
            Debug.Assert(newNode IsNot Nothing)
            Debug.Assert(Me.SemanticRootOfReplacedExpression.DescendantNodesAndSelf().Contains(newNode))

            Dim originalSymbolInfo = Me.OriginalSemanticModel.GetSymbolInfo(originalNode)
            Dim newSymbolInfo = Me.SpeculativeSemanticModel.GetSymbolInfo(newNode)
            Return SymbolInfosAreCompatible(originalSymbolInfo, newSymbolInfo)
        End Function

        Private Function QuerySymbolsAreCompatible(originalNode As QueryClauseSyntax, newNode As QueryClauseSyntax) As Boolean
            Debug.Assert(originalNode IsNot Nothing)
            Debug.Assert(Me.SemanticRootOfOriginalExpression.DescendantNodesAndSelf().Contains(originalNode))
            Debug.Assert(newNode IsNot Nothing)
            Debug.Assert(Me.SemanticRootOfReplacedExpression.DescendantNodesAndSelf().Contains(newNode))

            Dim originalSymbolInfo = Me.OriginalSemanticModel.GetSymbolInfo(originalNode)
            Dim newSymbolInfo = Me.SpeculativeSemanticModel.GetSymbolInfo(newNode)
            Return SymbolInfosAreCompatible(originalSymbolInfo, newSymbolInfo)
        End Function

        Private Overloads Function SymbolInfosAreCompatible(originalSymbolInfo As CollectionRangeVariableSymbolInfo, newSymbolInfo As CollectionRangeVariableSymbolInfo) As Boolean
            Return SymbolInfosAreCompatible(originalSymbolInfo.ToQueryableCollectionConversion, newSymbolInfo.ToQueryableCollectionConversion) AndAlso
                SymbolInfosAreCompatible(originalSymbolInfo.AsClauseConversion, newSymbolInfo.AsClauseConversion) AndAlso
                SymbolInfosAreCompatible(originalSymbolInfo.SelectMany, newSymbolInfo.SelectMany)
        End Function

        Private Overloads Function SymbolInfosAreCompatible(originalSymbolInfo As AggregateClauseSymbolInfo, newSymbolInfo As AggregateClauseSymbolInfo) As Boolean
            Return SymbolInfosAreCompatible(originalSymbolInfo.Select1, newSymbolInfo.Select1) AndAlso
                SymbolInfosAreCompatible(originalSymbolInfo.Select2, newSymbolInfo.Select2)
        End Function
#End Region

        ''' <summary>
        ''' Determines whether performing the syntax replacement in one of the sibling nodes of the given lambda expressions will change the lambda binding semantics.
        ''' This is done by first determining the lambda parameters whose type differs in the replaced lambda node.
        ''' For each of these parameters, we find the descendant identifier name nodes in the lambda body and check if semantics of any of the parenting nodes of these
        ''' identifier nodes have changed in the replaced lambda.
        ''' </summary>
        Public Function ReplacementChangesSemanticsOfUnchangedLambda(originalLambda As ExpressionSyntax, replacedLambda As ExpressionSyntax) As Boolean
            originalLambda = originalLambda.WalkDownParentheses()
            replacedLambda = replacedLambda.WalkDownParentheses()

            Dim originalLambdaBody As SyntaxNode, replacedLambdaBody As SyntaxNode
            Dim originalParams As SeparatedSyntaxList(Of ParameterSyntax), replacedParams As SeparatedSyntaxList(Of ParameterSyntax)

            Select Case originalLambda.Kind
                Case SyntaxKind.SingleLineFunctionLambdaExpression, SyntaxKind.SingleLineSubLambdaExpression
                    Dim originalSingleLineLambda = DirectCast(originalLambda, SingleLineLambdaExpressionSyntax)
                    Dim replacedSingleLineLambda = DirectCast(replacedLambda, SingleLineLambdaExpressionSyntax)

                    originalParams = originalSingleLineLambda.SubOrFunctionHeader.ParameterList.Parameters
                    replacedParams = replacedSingleLineLambda.SubOrFunctionHeader.ParameterList.Parameters
                    originalLambdaBody = originalSingleLineLambda.Body
                    replacedLambdaBody = replacedSingleLineLambda.Body

                Case SyntaxKind.MultiLineFunctionLambdaExpression, SyntaxKind.MultiLineSubLambdaExpression
                    Dim originalMultiLineLambda = DirectCast(originalLambda, MultiLineLambdaExpressionSyntax)
                    Dim replacedMultiLineLambda = DirectCast(replacedLambda, MultiLineLambdaExpressionSyntax)

                    originalParams = originalMultiLineLambda.SubOrFunctionHeader.ParameterList.Parameters
                    replacedParams = replacedMultiLineLambda.SubOrFunctionHeader.ParameterList.Parameters
                    originalLambdaBody = originalMultiLineLambda
                    replacedLambdaBody = replacedMultiLineLambda

                Case Else

                    Throw ExceptionUtilities.UnexpectedValue(originalLambda.Kind)
            End Select

            Debug.Assert(originalParams.Count = replacedParams.Count)

            If Not originalParams.Any() Then
                Return False
            End If

            Dim paramNames = New List(Of String)()
            For i As Integer = 0 To originalParams.Count - 1
                Dim originalParam = originalParams(i)
                Dim replacedParam = replacedParams(i)
                If Not HaveSameParameterType(originalParam, replacedParam) Then
                    paramNames.Add(originalParam.Identifier.Identifier.ValueText)
                End If
            Next

            If Not paramNames.Any() Then
                Return False
            End If

            Dim originalIdentifierNodes = originalLambdaBody _
                                          .DescendantNodes() _
                                          .OfType(Of IdentifierNameSyntax)() _
                                          .Where(Function(node) paramNames.Contains(node.Identifier.ValueText))
            If Not originalIdentifierNodes.Any() Then
                Return False
            End If

            Dim replacedIdentifierNodes = replacedLambdaBody _
                                          .DescendantNodes() _
                                          .OfType(Of IdentifierNameSyntax)() _
                                          .Where(Function(node) paramNames.Contains(node.Identifier.ValueText))
            Return ReplacementChangesSemanticsForNodes(originalIdentifierNodes, replacedIdentifierNodes, originalLambdaBody, replacedLambdaBody)
        End Function

        Private Function HaveSameParameterType(originalParam As ParameterSyntax, replacedParam As ParameterSyntax) As Boolean
            Dim originalParamType = Me.OriginalSemanticModel.GetDeclaredSymbol(originalParam).Type
            Dim replacedParamType = Me.SpeculativeSemanticModel.GetDeclaredSymbol(replacedParam).Type
            Return Equals(originalParamType, replacedParamType)
        End Function

        Private Function ReplacementChangesSemanticsForNodes(
            originalIdentifierNodes As IEnumerable(Of IdentifierNameSyntax),
            replacedIdentifierNodes As IEnumerable(Of IdentifierNameSyntax),
            originalRoot As SyntaxNode,
            replacedRoot As SyntaxNode) As Boolean

            Debug.Assert(originalIdentifierNodes.Any())
            Debug.Assert(originalIdentifierNodes.Count() = replacedIdentifierNodes.Count())

            Dim originalChildNodeEnum = originalIdentifierNodes.GetEnumerator()
            Dim replacedChildNodeEnum = replacedIdentifierNodes.GetEnumerator()

            While originalChildNodeEnum.MoveNext()
                replacedChildNodeEnum.MoveNext()
                If ReplacementChangesSemantics(originalChildNodeEnum.Current, replacedChildNodeEnum.Current, originalRoot, skipVerificationForCurrentNode:=True) Then
                    Return True
                End If
            End While

            Return False
        End Function

        Protected Overrides Function ReplacementChangesSemanticsForNodeLanguageSpecific(currentOriginalNode As SyntaxNode, currentReplacedNode As SyntaxNode, previousOriginalNode As SyntaxNode, previousReplacedNode As SyntaxNode) As Boolean
            Debug.Assert(previousOriginalNode Is Nothing OrElse previousOriginalNode.Parent Is currentOriginalNode)
            Debug.Assert(previousReplacedNode Is Nothing OrElse previousReplacedNode.Parent Is currentReplacedNode)

            If TypeOf currentOriginalNode Is BinaryExpressionSyntax Then
                ' If replacing the node will result in a broken binary expression, we won't remove it.
                Dim originalExpression = DirectCast(currentOriginalNode, BinaryExpressionSyntax)
                Dim newExpression = DirectCast(currentReplacedNode, BinaryExpressionSyntax)
                If ReplacementBreaksBinaryExpression(originalExpression, newExpression) Then
                    Return True
                End If

                Return Not ImplicitConversionsAreCompatible(originalExpression, newExpression)
            ElseIf TypeOf currentOriginalNode Is AssignmentStatementSyntax Then
                Dim originalAssignmentStatement = DirectCast(currentOriginalNode, AssignmentStatementSyntax)

                If SyntaxFacts.IsAssignmentStatementOperatorToken(originalAssignmentStatement.OperatorToken.Kind()) Then
                    Dim newAssignmentStatement = DirectCast(currentReplacedNode, AssignmentStatementSyntax)

                    If ReplacementBreaksCompoundAssignment(originalAssignmentStatement.Left, originalAssignmentStatement.Right, newAssignmentStatement.Left, newAssignmentStatement.Right) Then
                        Return True
                    End If
                End If
            ElseIf currentOriginalNode.Kind = SyntaxKind.ConditionalAccessExpression Then
                Dim originalExpression = DirectCast(currentOriginalNode, ConditionalAccessExpressionSyntax)
                Dim newExpression = DirectCast(currentReplacedNode, ConditionalAccessExpressionSyntax)
                Return ReplacementBreaksConditionalAccessExpression(originalExpression, newExpression)
            ElseIf currentOriginalNode.Kind = SyntaxKind.VariableDeclarator Then
                ' Heuristic: If replacing the node will result in changing the type of a local variable
                ' that is type-inferred, we won't remove it. It's possible to do this analysis, but it's
                ' very expensive and the benefit to the user is small.

                Dim originalDeclarator = DirectCast(currentOriginalNode, VariableDeclaratorSyntax)
                Dim newDeclarator = DirectCast(currentReplacedNode, VariableDeclaratorSyntax)
                If originalDeclarator.IsTypeInferred(Me.OriginalSemanticModel) AndAlso Not ConvertedTypesAreCompatible(originalDeclarator.Initializer.Value, newDeclarator.Initializer.Value) Then
                    Return True
                End If

                Return False
            ElseIf currentOriginalNode.Kind = SyntaxKind.CollectionInitializer Then
                Return _
                    previousOriginalNode IsNot Nothing AndAlso
                    ReplacementBreaksCollectionInitializerAddMethod(DirectCast(previousOriginalNode, ExpressionSyntax), DirectCast(previousReplacedNode, ExpressionSyntax))
            ElseIf currentOriginalNode.Kind = SyntaxKind.Interpolation Then
                Dim orignalInterpolation = DirectCast(currentOriginalNode, InterpolationSyntax)
                Dim newInterpolation = DirectCast(currentReplacedNode, InterpolationSyntax)

                Return ReplacementBreaksInterpolation(orignalInterpolation, newInterpolation)
            Else
                Dim originalCollectionRangeVariableSyntax = TryCast(currentOriginalNode, CollectionRangeVariableSyntax)
                If originalCollectionRangeVariableSyntax IsNot Nothing Then
                    Dim newCollectionRangeVariableSyntax = DirectCast(currentReplacedNode, CollectionRangeVariableSyntax)
                    Return Not QuerySymbolsAreCompatible(originalCollectionRangeVariableSyntax, newCollectionRangeVariableSyntax)
                End If

                Dim originalAggregateSyntax = TryCast(currentOriginalNode, AggregateClauseSyntax)
                If originalAggregateSyntax IsNot Nothing Then
                    Dim newAggregateSyntax = DirectCast(currentReplacedNode, AggregateClauseSyntax)
                    Return Not QuerySymbolsAreCompatible(originalAggregateSyntax, newAggregateSyntax)
                End If

                Dim originalExprRangeVariableSyntax = TryCast(currentOriginalNode, ExpressionRangeVariableSyntax)
                If originalExprRangeVariableSyntax IsNot Nothing Then
                    Dim newExprRangeVariableSyntax = DirectCast(currentReplacedNode, ExpressionRangeVariableSyntax)
                    Return Not QuerySymbolsAreCompatible(originalExprRangeVariableSyntax, newExprRangeVariableSyntax)
                End If

                Dim originalFunctionAggregationSyntax = TryCast(currentOriginalNode, FunctionAggregationSyntax)
                If originalFunctionAggregationSyntax IsNot Nothing Then
                    Dim newFunctionAggregationSyntax = DirectCast(currentReplacedNode, FunctionAggregationSyntax)
                    Return Not SymbolsAreCompatible(originalFunctionAggregationSyntax, newFunctionAggregationSyntax)
                End If

                Dim originalOrderingSyntax = TryCast(currentOriginalNode, OrderingSyntax)
                If originalOrderingSyntax IsNot Nothing Then
                    Dim newOrderingSyntax = DirectCast(currentReplacedNode, OrderingSyntax)
                    Return Not QuerySymbolsAreCompatible(originalOrderingSyntax, newOrderingSyntax)
                End If

                Dim originalQueryClauseSyntax = TryCast(currentOriginalNode, QueryClauseSyntax)
                If originalQueryClauseSyntax IsNot Nothing Then
                    Dim newQueryClauseSyntax = DirectCast(currentReplacedNode, QueryClauseSyntax)
                    Return Not QuerySymbolsAreCompatible(originalQueryClauseSyntax, newQueryClauseSyntax)
                End If
            End If

            Return False
        End Function

        Private Function ReplacementBreaksCollectionInitializerAddMethod(originalInitializer As ExpressionSyntax, newInitializer As ExpressionSyntax) As Boolean
            Dim originalSymbol = Me.OriginalSemanticModel.GetCollectionInitializerSymbolInfo(originalInitializer, CancellationToken).Symbol
            Dim newSymbol = Me.SpeculativeSemanticModel.GetCollectionInitializerSymbolInfo(newInitializer, CancellationToken).Symbol
            Return Not SymbolsAreCompatible(originalSymbol, newSymbol)
        End Function

        Protected Overrides Function IsForEachTypeInferred(forEachStatement As ForEachStatementSyntax, semanticModel As SemanticModel) As Boolean
            Dim forEachControlVariable = TryCast(forEachStatement.ControlVariable, VariableDeclaratorSyntax)
            Return forEachControlVariable IsNot Nothing AndAlso forEachControlVariable.IsTypeInferred(Me.OriginalSemanticModel)
        End Function

        Protected Overrides Function IsInvocableExpression(node As SyntaxNode) As Boolean
            If node.IsKind(SyntaxKind.InvocationExpression) OrElse node.IsKind(SyntaxKind.ObjectCreationExpression) Then
                Return True
            End If

            If node.IsKind(SyntaxKind.SimpleMemberAccessExpression) AndAlso
                Not node.IsParentKind(SyntaxKind.InvocationExpression) AndAlso
                Not node.IsParentKind(SyntaxKind.ObjectCreationExpression) Then
                Return True
            End If

            Return False
        End Function

        Protected Overrides Function GetReceiver(expression As ExpressionSyntax) As ExpressionSyntax
            expression = expression.WalkDownParentheses()

            Select Case expression.Kind
                Case SyntaxKind.SimpleMemberAccessExpression
                    Return DirectCast(expression, MemberAccessExpressionSyntax).Expression.WalkDownParentheses()

                Case SyntaxKind.InvocationExpression
                    Dim result = DirectCast(expression, InvocationExpressionSyntax).Expression.WalkDownParentheses()
                    If result.IsKind(SyntaxKind.SimpleMemberAccessExpression) Then
                        Return GetReceiver(result)
                    End If

                    Return result

                Case Else
                    Return Nothing
            End Select
        End Function

        Protected Overrides Function IsNamedArgument(argument As ArgumentSyntax) As Boolean
            Return argument.IsNamed
        End Function

        Protected Overrides Function GetNamedArgumentIdentifierValueText(argument As ArgumentSyntax) As String
            Return DirectCast(argument, SimpleArgumentSyntax).NameColonEquals.Name.Identifier.ValueText
        End Function

        Protected Overrides Function GetArguments(expression As ExpressionSyntax) As ImmutableArray(Of ArgumentSyntax)
            Dim argumentList = GetArgumentList(expression)
            Return If(argumentList IsNot Nothing,
                      argumentList.Arguments.AsImmutable(),
                      ImmutableArray.Create(Of ArgumentSyntax)())
        End Function

        Private Shared Function GetArgumentList(expression As ExpressionSyntax) As ArgumentListSyntax
            expression = expression.WalkDownParentheses()

            Select Case expression.Kind
                Case SyntaxKind.InvocationExpression
                    Return DirectCast(expression, InvocationExpressionSyntax).ArgumentList
                Case SyntaxKind.ObjectCreationExpression
                    Return DirectCast(expression, ObjectCreationExpressionSyntax).ArgumentList
                Case Else
                    Return Nothing
            End Select
        End Function

        Private Function ReplacementBreaksBinaryExpression(binaryExpression As BinaryExpressionSyntax, newBinaryExpression As BinaryExpressionSyntax) As Boolean
            Dim operatorTokenKind = binaryExpression.OperatorToken.Kind
            If SyntaxFacts.IsAssignmentStatementOperatorToken(operatorTokenKind) AndAlso
                operatorTokenKind <> SyntaxKind.LessThanLessThanEqualsToken AndAlso
                operatorTokenKind <> SyntaxKind.GreaterThanGreaterThanEqualsToken AndAlso
                ReplacementBreaksCompoundAssignment(binaryExpression.Left, binaryExpression.Right, newBinaryExpression.Left, newBinaryExpression.Right) Then
                Return True
            End If

            Return Not SymbolsAreCompatible(binaryExpression, newBinaryExpression) OrElse
                Not TypesAreCompatible(binaryExpression, newBinaryExpression)
        End Function

        Private Function ReplacementBreaksConditionalAccessExpression(conditionalAccessExpression As ConditionalAccessExpressionSyntax, newConditionalAccessExpression As ConditionalAccessExpressionSyntax) As Boolean
            Return _
                Not SymbolsAreCompatible(conditionalAccessExpression, newConditionalAccessExpression) OrElse
                Not TypesAreCompatible(conditionalAccessExpression, newConditionalAccessExpression) OrElse
                Not SymbolsAreCompatible(conditionalAccessExpression.WhenNotNull, newConditionalAccessExpression.WhenNotNull) OrElse
                Not TypesAreCompatible(conditionalAccessExpression.WhenNotNull, newConditionalAccessExpression.WhenNotNull)
        End Function

        Private Function ReplacementBreaksInterpolation(interpolation As InterpolationSyntax, newInterpolation As InterpolationSyntax) As Boolean
            Return Not TypesAreCompatible(interpolation.Expression, newInterpolation.Expression)
        End Function

        Protected Overrides Function GetForEachStatementExpression(forEachStatement As ForEachStatementSyntax) As ExpressionSyntax
            Return forEachStatement.Expression
        End Function

        Protected Overrides Function GetThrowStatementExpression(throwStatement As ThrowStatementSyntax) As ExpressionSyntax
            Return throwStatement.Expression
        End Function

        Protected Overrides Function IsInNamespaceOrTypeContext(node As ExpressionSyntax) As Boolean
            Return SyntaxFacts.IsInNamespaceOrTypeContext(node)
        End Function

        Protected Overrides Function IsParenthesizedExpression(node As SyntaxNode) As Boolean
            Return node.IsKind(SyntaxKind.ParenthesizedExpression)
        End Function

        Protected Overrides Function ConversionsAreCompatible(originalModel As SemanticModel, originalExpression As ExpressionSyntax, newModel As SemanticModel, newExpression As ExpressionSyntax) As Boolean
            Return ConversionsAreCompatible(originalModel.GetConversion(originalExpression), newModel.GetConversion(newExpression))
        End Function

        Protected Overrides Function ConversionsAreCompatible(originalExpression As ExpressionSyntax, originalTargetType As ITypeSymbol, newExpression As ExpressionSyntax, newTargetType As ITypeSymbol) As Boolean
            Dim originalConversion = Me.OriginalSemanticModel.ClassifyConversion(originalExpression, originalTargetType)
            Dim newConversion = Me.SpeculativeSemanticModel.ClassifyConversion(newExpression, newTargetType)

            ' When Option Strict is not Off and the new expression has a constant value, it's possible that
            ' there Is a hidden narrowing conversion that will be missed. In that case, use the
            ' conversion between the type of the new expression and the new target type.

            If Me.OriginalSemanticModel.OptionStrict() <> OptionStrict.Off AndAlso
               Me.SpeculativeSemanticModel.GetConstantValue(newExpression).HasValue Then

                Dim newExpressionType = Me.SpeculativeSemanticModel.GetTypeInfo(newExpression).Type
                newConversion = Me.OriginalSemanticModel.Compilation.ClassifyConversion(newExpressionType, newTargetType)
            End If

            Return ConversionsAreCompatible(originalConversion, newConversion)
        End Function

        Private Overloads Function ConversionsAreCompatible(originalConversion As Conversion, newConversion As Conversion) As Boolean
            If originalConversion.Exists <> newConversion.Exists OrElse
                ((Not originalConversion.IsNarrowing) AndAlso newConversion.IsNarrowing) Then
                Return False
            End If

            Dim originalIsUserDefined = originalConversion.IsUserDefined
            Dim newIsUserDefined = newConversion.IsUserDefined

            If (originalIsUserDefined <> newIsUserDefined) Then
                Return False
            End If

            If (originalIsUserDefined OrElse originalConversion.MethodSymbol IsNot Nothing OrElse newConversion.MethodSymbol IsNot Nothing) Then
                Return SymbolsAreCompatible(originalConversion.MethodSymbol, newConversion.MethodSymbol)
            End If

            Return True
        End Function

        Protected Overrides Function ForEachConversionsAreCompatible(originalModel As SemanticModel, originalForEach As ForEachStatementSyntax, newModel As SemanticModel, newForEach As ForEachStatementSyntax) As Boolean
            Dim originalInfo = originalModel.GetForEachStatementInfo(originalForEach)
            Dim newInfo = newModel.GetForEachStatementInfo(newForEach)
            Return ConversionsAreCompatible(originalInfo.CurrentConversion, newInfo.CurrentConversion) AndAlso ConversionsAreCompatible(originalInfo.ElementConversion, newInfo.ElementConversion)
        End Function

        Protected Overrides Sub GetForEachSymbols(model As SemanticModel, forEach As ForEachStatementSyntax, ByRef getEnumeratorMethod As IMethodSymbol, ByRef elementType As ITypeSymbol)
            Dim info = model.GetForEachStatementInfo(forEach)
            getEnumeratorMethod = info.GetEnumeratorMethod
            elementType = info.ElementType
        End Sub

        Protected Overrides Function IsReferenceConversion(compilation As Compilation, sourceType As ITypeSymbol, targetType As ITypeSymbol) As Boolean
            Return compilation.ClassifyConversion(sourceType, targetType).IsReference
        End Function
    End Class
End Namespace
