' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Concurrent
Imports System.Collections.Immutable
Imports Microsoft.CodeAnalysis.PooledObjects
Imports Microsoft.CodeAnalysis.VisualBasic
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.Semantics
    Partial Friend NotInheritable Class VisualBasicOperationFactory

        Private ReadOnly _cache As ConcurrentDictionary(Of BoundNode, IOperation) =
            New ConcurrentDictionary(Of BoundNode, IOperation)(concurrencyLevel:=2, capacity:=10)

        Private ReadOnly _semanticModel As SemanticModel

        Public Sub New(semanticModel As SemanticModel)
            _semanticModel = semanticModel
        End Sub

        Public Function Create(boundNode As BoundNode) As IOperation
            If boundNode Is Nothing Then
                Return Nothing
            End If

            ' this should be removed once this issue is fixed
            ' https://github.com/dotnet/roslyn/issues/21186
            ' https://github.com/dotnet/roslyn/issues/21554
            If TypeOf boundNode Is BoundValuePlaceholderBase OrElse
               (TypeOf boundNode Is BoundParameter AndAlso boundNode.WasCompilerGenerated) Then
                ' since same bound node appears in multiple places in the tree
                ' we can't use bound node to operation map.
                ' for now, we will just create new operation and return cloned
                Return _semanticModel.CloneOperation(CreateInternal(boundNode))
            End If

            ' A BoundUserDefined conversion is always the operand of a BoundConversion, and is handled
            ' by the BoundConversion creation. We should never receive one in this top level create call.
            Debug.Assert(boundNode.Kind <> BoundKind.UserDefinedConversion)

            Return _cache.GetOrAdd(boundNode, Function(n) CreateInternal(n))
        End Function

        Private Function CreateInternal(boundNode As BoundNode) As IOperation
            Select Case boundNode.Kind
                Case BoundKind.AssignmentOperator
                    Return CreateBoundAssignmentOperatorOperation(DirectCast(boundNode, BoundAssignmentOperator))
                Case BoundKind.MeReference
                    Return CreateBoundMeReferenceOperation(DirectCast(boundNode, BoundMeReference))
                Case BoundKind.MyBaseReference
                    Return CreateBoundMyBaseReferenceOperation(DirectCast(boundNode, BoundMyBaseReference))
                Case BoundKind.MyClassReference
                    Return CreateBoundMyClassReferenceOperation(DirectCast(boundNode, BoundMyClassReference))
                Case BoundKind.Literal
                    Return CreateBoundLiteralOperation(DirectCast(boundNode, BoundLiteral))
                Case BoundKind.AwaitOperator
                    Return CreateBoundAwaitOperatorOperation(DirectCast(boundNode, BoundAwaitOperator))
                Case BoundKind.NameOfOperator
                    Return CreateBoundNameOfOperatorOperation(DirectCast(boundNode, BoundNameOfOperator))
                Case BoundKind.Lambda
                    Return CreateBoundLambdaOperation(DirectCast(boundNode, BoundLambda))
                Case BoundKind.Call
                    Return CreateBoundCallOperation(DirectCast(boundNode, BoundCall))
                Case BoundKind.OmittedArgument
                    Return CreateBoundOmittedArgumentOperation(DirectCast(boundNode, BoundOmittedArgument))
                Case BoundKind.Parenthesized
                    Return CreateBoundParenthesizedOperation(DirectCast(boundNode, BoundParenthesized))
                Case BoundKind.ArrayAccess
                    Return CreateBoundArrayAccessOperation(DirectCast(boundNode, BoundArrayAccess))
                Case BoundKind.UnaryOperator
                    Return CreateBoundUnaryOperatorOperation(DirectCast(boundNode, BoundUnaryOperator))
                Case BoundKind.UserDefinedUnaryOperator
                    Return CreateBoundUserDefinedUnaryOperatorOperation(DirectCast(boundNode, BoundUserDefinedUnaryOperator))
                Case BoundKind.BinaryOperator
                    Return CreateBoundBinaryOperatorOperation(DirectCast(boundNode, BoundBinaryOperator))
                Case BoundKind.UserDefinedBinaryOperator
                    Return CreateBoundUserDefinedBinaryOperatorOperation(DirectCast(boundNode, BoundUserDefinedBinaryOperator))
                Case BoundKind.BinaryConditionalExpression
                    Return CreateBoundBinaryConditionalExpressionOperation(DirectCast(boundNode, BoundBinaryConditionalExpression))
                Case BoundKind.UserDefinedShortCircuitingOperator
                    Return CreateBoundUserDefinedShortCircuitingOperatorOperation(DirectCast(boundNode, BoundUserDefinedShortCircuitingOperator))
                Case BoundKind.BadExpression
                    Return CreateBoundBadExpressionOperation(DirectCast(boundNode, BoundBadExpression))
                Case BoundKind.TryCast
                    Return CreateBoundTryCastOperation(DirectCast(boundNode, BoundTryCast))
                Case BoundKind.DirectCast
                    Return CreateBoundDirectCastOperation(DirectCast(boundNode, BoundDirectCast))
                Case BoundKind.Conversion
                    Return CreateBoundConversionOperation(DirectCast(boundNode, BoundConversion))
                Case BoundKind.DelegateCreationExpression
                    Return CreateBoundDelegateCreationExpressionOperation(DirectCast(boundNode, BoundDelegateCreationExpression))
                Case BoundKind.TernaryConditionalExpression
                    Return CreateBoundTernaryConditionalExpressionOperation(DirectCast(boundNode, BoundTernaryConditionalExpression))
                Case BoundKind.TypeOf
                    Return CreateBoundTypeOfOperation(DirectCast(boundNode, BoundTypeOf))
                Case BoundKind.GetType
                    Return CreateBoundGetTypeOperation(DirectCast(boundNode, BoundGetType))
                Case BoundKind.ObjectCreationExpression
                    Return CreateBoundObjectCreationExpressionOperation(DirectCast(boundNode, BoundObjectCreationExpression))
                Case BoundKind.ObjectInitializerExpression
                    Return CreateBoundObjectInitializerExpressionOperation(DirectCast(boundNode, BoundObjectInitializerExpression))
                Case BoundKind.CollectionInitializerExpression
                    Return CreateBoundCollectionInitializerExpressionOperation(DirectCast(boundNode, BoundCollectionInitializerExpression))
                Case BoundKind.NewT
                    Return CreateBoundNewTOperation(DirectCast(boundNode, BoundNewT))
                Case BoundKind.ArrayCreation
                    Return CreateBoundArrayCreationOperation(DirectCast(boundNode, BoundArrayCreation))
                Case BoundKind.ArrayInitialization
                    Return CreateBoundArrayInitializationOperation(DirectCast(boundNode, BoundArrayInitialization))
                Case BoundKind.PropertyAccess
                    Return CreateBoundPropertyAccessOperation(DirectCast(boundNode, BoundPropertyAccess))
                Case BoundKind.EventAccess
                    Return CreateBoundEventAccessOperation(DirectCast(boundNode, BoundEventAccess))
                Case BoundKind.FieldAccess
                    Return CreateBoundFieldAccessOperation(DirectCast(boundNode, BoundFieldAccess))
                Case BoundKind.ConditionalAccess
                    Return CreateBoundConditionalAccessOperation(DirectCast(boundNode, BoundConditionalAccess))
                Case BoundKind.ConditionalAccessReceiverPlaceholder
                    Return CreateBoundConditionalAccessReceiverPlaceholderOperation(DirectCast(boundNode, BoundConditionalAccessReceiverPlaceholder))
                Case BoundKind.Parameter
                    Return CreateBoundParameterOperation(DirectCast(boundNode, BoundParameter))
                Case BoundKind.Local
                    Return CreateBoundLocalOperation(DirectCast(boundNode, BoundLocal))
                Case BoundKind.LateInvocation
                    Return CreateBoundLateInvocationOperation(DirectCast(boundNode, BoundLateInvocation))
                Case BoundKind.LateMemberAccess
                    Return CreateBoundLateMemberAccessOperation(DirectCast(boundNode, BoundLateMemberAccess))
                Case BoundKind.FieldInitializer
                    Return CreateBoundFieldInitializerOperation(DirectCast(boundNode, BoundFieldInitializer))
                Case BoundKind.PropertyInitializer
                    Return CreateBoundPropertyInitializerOperation(DirectCast(boundNode, BoundPropertyInitializer))
                Case BoundKind.ParameterEqualsValue
                    Return CreateBoundParameterEqualsValueOperation(DirectCast(boundNode, BoundParameterEqualsValue))
                Case BoundKind.RValuePlaceholder
                    Return CreateBoundRValuePlaceholderOperation(DirectCast(boundNode, BoundRValuePlaceholder))
                Case BoundKind.IfStatement
                    Return CreateBoundIfStatementOperation(DirectCast(boundNode, BoundIfStatement))
                Case BoundKind.SelectStatement
                    Return CreateBoundSelectStatementOperation(DirectCast(boundNode, BoundSelectStatement))
                Case BoundKind.CaseBlock
                    Return CreateBoundCaseBlockOperation(DirectCast(boundNode, BoundCaseBlock))
                Case BoundKind.SimpleCaseClause
                    Return CreateBoundSimpleCaseClauseOperation(DirectCast(boundNode, BoundSimpleCaseClause))
                Case BoundKind.RangeCaseClause
                    Return CreateBoundRangeCaseClauseOperation(DirectCast(boundNode, BoundRangeCaseClause))
                Case BoundKind.RelationalCaseClause
                    Return CreateBoundRelationalCaseClauseOperation(DirectCast(boundNode, BoundRelationalCaseClause))
                Case BoundKind.DoLoopStatement
                    Return CreateBoundDoLoopStatementOperation(DirectCast(boundNode, BoundDoLoopStatement))
                Case BoundKind.ForToStatement
                    Return CreateBoundForToStatementOperation(DirectCast(boundNode, BoundForToStatement))
                Case BoundKind.ForEachStatement
                    Return CreateBoundForEachStatementOperation(DirectCast(boundNode, BoundForEachStatement))
                Case BoundKind.TryStatement
                    Return CreateBoundTryStatementOperation(DirectCast(boundNode, BoundTryStatement))
                Case BoundKind.CatchBlock
                    Return CreateBoundCatchBlockOperation(DirectCast(boundNode, BoundCatchBlock))
                Case BoundKind.Block
                    Return CreateBoundBlockOperation(DirectCast(boundNode, BoundBlock))
                Case BoundKind.BadStatement
                    Return CreateBoundBadStatementOperation(DirectCast(boundNode, BoundBadStatement))
                Case BoundKind.ReturnStatement
                    Return CreateBoundReturnStatementOperation(DirectCast(boundNode, BoundReturnStatement))
                Case BoundKind.ThrowStatement
                    Return CreateBoundThrowStatementOperation(DirectCast(boundNode, BoundThrowStatement))
                Case BoundKind.WhileStatement
                    Return CreateBoundWhileStatementOperation(DirectCast(boundNode, BoundWhileStatement))
                Case BoundKind.DimStatement
                    Return CreateBoundDimStatementOperation(DirectCast(boundNode, BoundDimStatement))
                Case BoundKind.YieldStatement
                    Return CreateBoundYieldStatementOperation(DirectCast(boundNode, BoundYieldStatement))
                Case BoundKind.LabelStatement
                    Return CreateBoundLabelStatementOperation(DirectCast(boundNode, BoundLabelStatement))
                Case BoundKind.GotoStatement
                    Return CreateBoundGotoStatementOperation(DirectCast(boundNode, BoundGotoStatement))
                Case BoundKind.ContinueStatement
                    Return CreateBoundContinueStatementOperation(DirectCast(boundNode, BoundContinueStatement))
                Case BoundKind.ExitStatement
                    Return CreateBoundExitStatementOperation(DirectCast(boundNode, BoundExitStatement))
                Case BoundKind.SyncLockStatement
                    Return CreateBoundSyncLockStatementOperation(DirectCast(boundNode, BoundSyncLockStatement))
                Case BoundKind.NoOpStatement
                    Return CreateBoundNoOpStatementOperation(DirectCast(boundNode, BoundNoOpStatement))
                Case BoundKind.StopStatement
                    Return CreateBoundStopStatementOperation(DirectCast(boundNode, BoundStopStatement))
                Case BoundKind.EndStatement
                    Return CreateBoundEndStatementOperation(DirectCast(boundNode, BoundEndStatement))
                Case BoundKind.WithStatement
                    Return CreateBoundWithStatementOperation(DirectCast(boundNode, BoundWithStatement))
                Case BoundKind.UsingStatement
                    Return CreateBoundUsingStatementOperation(DirectCast(boundNode, BoundUsingStatement))
                Case BoundKind.ExpressionStatement
                    Return CreateBoundExpressionStatementOperation(DirectCast(boundNode, BoundExpressionStatement))
                Case BoundKind.RaiseEventStatement
                    Return CreateBoundRaiseEventStatementOperation(DirectCast(boundNode, BoundRaiseEventStatement))
                Case BoundKind.AddHandlerStatement
                    Return CreateBoundAddHandlerStatementOperation(DirectCast(boundNode, BoundAddHandlerStatement))
                Case BoundKind.RemoveHandlerStatement
                    Return CreateBoundRemoveHandlerStatementOperation(DirectCast(boundNode, BoundRemoveHandlerStatement))
                Case BoundKind.TupleLiteral,
                     BoundKind.ConvertedTupleLiteral
                    Return CreateBoundTupleExpressionOperation(DirectCast(boundNode, BoundTupleExpression))
                Case BoundKind.InterpolatedStringExpression
                    Return CreateBoundInterpolatedStringExpressionOperation(DirectCast(boundNode, BoundInterpolatedStringExpression))
                Case BoundKind.Interpolation
                    Return CreateBoundInterpolationOperation(DirectCast(boundNode, BoundInterpolation))
                Case BoundKind.AnonymousTypeCreationExpression
                    Return CreateBoundAnonymousTypeCreationExpressionOperation(DirectCast(boundNode, BoundAnonymousTypeCreationExpression))
                Case BoundKind.AnonymousTypeFieldInitializer
                    Return Create(DirectCast(boundNode, BoundAnonymousTypeFieldInitializer).Value)
                Case BoundKind.AnonymousTypePropertyAccess
                    Return CreateBoundAnonymousTypePropertyAccessOperation(DirectCast(boundNode, BoundAnonymousTypePropertyAccess))
                Case BoundKind.QueryExpression
                    Return CreateBoundQueryExpressionOperation(DirectCast(boundNode, BoundQueryExpression))
                Case BoundKind.QueryClause
                    ' Query clause has no special representation in the IOperation tree
                    Return Create(DirectCast(boundNode, BoundQueryClause).UnderlyingExpression)
                Case BoundKind.QueryableSource
                    ' Queryable source has no special representation in the IOperation tree
                    Return Create(DirectCast(boundNode, BoundQueryableSource).Source)
                Case BoundKind.AggregateClause
                    ' Aggregate clause has no special representation in the IOperation tree
                    Return Create(DirectCast(boundNode, BoundAggregateClause).UnderlyingExpression)
                Case BoundKind.Ordering
                    ' Ordering clause has no special representation in the IOperation tree
                    Return Create(DirectCast(boundNode, BoundOrdering).UnderlyingExpression)
                Case BoundKind.GroupAggregation
                    ' Group aggregation has no special representation in the IOperation tree
                    Return Create(DirectCast(boundNode, BoundGroupAggregation).Group)
                Case BoundKind.QuerySource
                    ' Query source has no special representation in the IOperation tree
                    Return Create(DirectCast(boundNode, BoundQuerySource).Expression)
                Case BoundKind.ToQueryableCollectionConversion
                    ' Queryable collection conversion has no special representation in the IOperation tree
                    Return Create(DirectCast(boundNode, BoundToQueryableCollectionConversion).ConversionCall)
                Case BoundKind.QueryLambda
                    ' Query lambda must be lowered to the regular lambda form for the operation tree.
                    Dim rewrittenLambda As BoundNode = RewriteQueryLambda(DirectCast(boundNode, BoundQueryLambda))
                    Return Create(rewrittenLambda)
                Case BoundKind.RangeVariableAssignment
                    ' Range variable assignment has no special representation in the IOperation tree
                    Return Create(DirectCast(boundNode, BoundRangeVariableAssignment).Value)
                Case Else
                    Dim constantValue = ConvertToOptional(TryCast(boundNode, BoundExpression)?.ConstantValueOpt)
                    Dim isImplicit As Boolean = boundNode.WasCompilerGenerated
                    Return Operation.CreateOperationNone(_semanticModel, boundNode.Syntax, constantValue, Function() GetIOperationChildren(boundNode), isImplicit)
            End Select
        End Function

        Private Function GetIOperationChildren(boundNode As BoundNode) As ImmutableArray(Of IOperation)
            Dim boundNodeWithChildren = DirectCast(boundNode, IBoundNodeWithIOperationChildren)
            If boundNodeWithChildren.Children.IsDefaultOrEmpty Then
                Return ImmutableArray(Of IOperation).Empty
            End If

            Dim builder = ArrayBuilder(Of IOperation).GetInstance(boundNodeWithChildren.Children.Length)
            For Each childNode In boundNodeWithChildren.Children
                Dim operation = Create(childNode)
                If operation Is Nothing Then
                    Continue For
                End If

                builder.Add(operation)
            Next

            Return builder.ToImmutableAndFree()
        End Function

        Private Function CreateBoundAssignmentOperatorOperation(boundAssignmentOperator As BoundAssignmentOperator) As IOperation
            Dim kind = GetAssignmentKind(boundAssignmentOperator)
            Dim isImplicit As Boolean = boundAssignmentOperator.WasCompilerGenerated
            If kind = OperationKind.CompoundAssignmentExpression Then
                ' convert Right to IOperation temporarily. we do this to get right operand, operator method and etc
                Dim temporaryRight = DirectCast(Create(boundAssignmentOperator.Right), IBinaryOperatorExpression)

                Dim operatorKind As BinaryOperatorKind = temporaryRight.OperatorKind
                Dim target As Lazy(Of IOperation) = New Lazy(Of IOperation)(Function() Create(boundAssignmentOperator.Left))

                ' right now, parent of right operand is set to the temporary IOperation, reset the parent
                ' we basically need to do this since we skip BoundAssignmentOperator.Right from IOperation tree
                Dim rightOperand = Operation.ResetParentOperation(temporaryRight.RightOperand)
                Dim value As Lazy(Of IOperation) = New Lazy(Of IOperation)(Function() rightOperand)

                Dim usesOperatorMethod As Boolean = temporaryRight.UsesOperatorMethod
                Dim operatorMethod As IMethodSymbol = temporaryRight.OperatorMethod
                Dim syntax As SyntaxNode = boundAssignmentOperator.Syntax
                Dim type As ITypeSymbol = boundAssignmentOperator.Type
                Dim constantValue As [Optional](Of Object) = ConvertToOptional(boundAssignmentOperator.ConstantValueOpt)
                Dim isLifted As Boolean = boundAssignmentOperator.Type.IsNullableType()
                Dim isChecked As Boolean = temporaryRight.IsChecked
                Return New LazyCompoundAssignmentExpression(operatorKind, isLifted, isChecked, target, value, usesOperatorMethod, operatorMethod, _semanticModel, syntax, type, constantValue, isImplicit)
            Else
                Dim target As Lazy(Of IOperation) = New Lazy(Of IOperation)(Function() Create(boundAssignmentOperator.Left))
                Dim value As Lazy(Of IOperation) = New Lazy(Of IOperation)(Function() Create(boundAssignmentOperator.Right))
                Dim syntax As SyntaxNode = boundAssignmentOperator.Syntax
                Dim type As ITypeSymbol = boundAssignmentOperator.Type
                Dim constantValue As [Optional](Of Object) = ConvertToOptional(boundAssignmentOperator.ConstantValueOpt)
                Return New LazySimpleAssignmentExpression(target, value, _semanticModel, syntax, type, constantValue, isImplicit)
            End If
        End Function

        Private Function CreateBoundMeReferenceOperation(boundMeReference As BoundMeReference) As IInstanceReferenceExpression
            Dim syntax As SyntaxNode = boundMeReference.Syntax
            Dim type As ITypeSymbol = boundMeReference.Type
            Dim constantValue As [Optional](Of Object) = ConvertToOptional(boundMeReference.ConstantValueOpt)
            Dim isImplicit As Boolean = boundMeReference.WasCompilerGenerated
            Return New InstanceReferenceExpression(_semanticModel, syntax, type, constantValue, isImplicit)
        End Function

        Private Function CreateBoundMyBaseReferenceOperation(boundMyBaseReference As BoundMyBaseReference) As IInstanceReferenceExpression
            Dim syntax As SyntaxNode = boundMyBaseReference.Syntax
            Dim type As ITypeSymbol = boundMyBaseReference.Type
            Dim constantValue As [Optional](Of Object) = ConvertToOptional(boundMyBaseReference.ConstantValueOpt)
            Dim isImplicit As Boolean = boundMyBaseReference.WasCompilerGenerated
            Return New InstanceReferenceExpression(_semanticModel, syntax, type, constantValue, isImplicit)
        End Function

        Private Function CreateBoundMyClassReferenceOperation(boundMyClassReference As BoundMyClassReference) As IInstanceReferenceExpression
            Dim syntax As SyntaxNode = boundMyClassReference.Syntax
            Dim type As ITypeSymbol = boundMyClassReference.Type
            Dim constantValue As [Optional](Of Object) = ConvertToOptional(boundMyClassReference.ConstantValueOpt)
            Dim isImplicit As Boolean = boundMyClassReference.WasCompilerGenerated
            Return New InstanceReferenceExpression(_semanticModel, syntax, type, constantValue, isImplicit)
        End Function

        Private Function CreateBoundLiteralOperation(boundLiteral As BoundLiteral) As ILiteralExpression
            Dim syntax As SyntaxNode = boundLiteral.Syntax
            Dim type As ITypeSymbol = boundLiteral.Type
            Dim constantValue As [Optional](Of Object) = ConvertToOptional(boundLiteral.ConstantValueOpt)
            Dim isImplicit As Boolean = boundLiteral.WasCompilerGenerated
            Return New LiteralExpression(_semanticModel, syntax, type, constantValue, isImplicit)
        End Function

        Private Function CreateBoundAwaitOperatorOperation(boundAwaitOperator As BoundAwaitOperator) As IAwaitExpression
            Dim awaitedValue As Lazy(Of IOperation) = New Lazy(Of IOperation)(Function() Create(boundAwaitOperator.Operand))
            Dim syntax As SyntaxNode = boundAwaitOperator.Syntax
            Dim type As ITypeSymbol = boundAwaitOperator.Type
            Dim constantValue As [Optional](Of Object) = ConvertToOptional(boundAwaitOperator.ConstantValueOpt)
            Dim isImplicit As Boolean = boundAwaitOperator.WasCompilerGenerated
            Return New LazyAwaitExpression(awaitedValue, _semanticModel, syntax, type, constantValue, isImplicit)
        End Function

        Private Function CreateBoundNameOfOperatorOperation(boundNameOfOperator As BoundNameOfOperator) As INameOfExpression
            Dim argument As Lazy(Of IOperation) = New Lazy(Of IOperation)(Function() Create(boundNameOfOperator.Argument))
            Dim syntax As SyntaxNode = boundNameOfOperator.Syntax
            Dim type As ITypeSymbol = boundNameOfOperator.Type
            Dim constantValue As [Optional](Of Object) = ConvertToOptional(boundNameOfOperator.ConstantValueOpt)
            Dim isImplicit As Boolean = boundNameOfOperator.WasCompilerGenerated
            Return New LazyNameOfExpression(argument, _semanticModel, syntax, type, constantValue, isImplicit)
        End Function

        Private Function CreateBoundLambdaOperation(boundLambda As BoundLambda) As IAnonymousFunctionExpression
            Dim symbol As IMethodSymbol = boundLambda.LambdaSymbol
            Dim body As Lazy(Of IBlockStatement) = New Lazy(Of IBlockStatement)(Function() DirectCast(Create(boundLambda.Body), IBlockStatement))
            Dim syntax As SyntaxNode = boundLambda.Syntax
            Dim type As ITypeSymbol = boundLambda.Type
            Dim constantValue As [Optional](Of Object) = ConvertToOptional(boundLambda.ConstantValueOpt)
            Dim isImplicit As Boolean = boundLambda.WasCompilerGenerated
            Return New LazyAnonymousFunctionExpression(symbol, body, _semanticModel, syntax, type, constantValue, isImplicit)
        End Function

        Private Function CreateBoundCallOperation(boundCall As BoundCall) As IInvocationExpression
            Dim targetMethod As IMethodSymbol = boundCall.Method
            Dim receiver As IOperation = Create(boundCall.ReceiverOpt)

            Dim instance As Lazy(Of IOperation) = New Lazy(Of IOperation)(Function() If(targetMethod.IsShared, Nothing, receiver))
            Dim isVirtual As Boolean =
                targetMethod IsNot Nothing AndAlso
                instance IsNot Nothing AndAlso
                (targetMethod.IsVirtual OrElse targetMethod.IsAbstract OrElse targetMethod.IsOverride) AndAlso
                receiver.Kind <> BoundKind.MyBaseReference AndAlso
                receiver.Kind <> BoundKind.MyClassReference

            Dim argumentsInEvaluationOrder As Lazy(Of ImmutableArray(Of IArgument)) = New Lazy(Of ImmutableArray(Of IArgument))(
                Function()
                    Return DeriveArguments(boundCall.Arguments, boundCall.Method.Parameters)
                End Function)

            Dim syntax As SyntaxNode = boundCall.Syntax
            Dim type As ITypeSymbol = boundCall.Type
            Dim constantValue As [Optional](Of Object) = ConvertToOptional(boundCall.ConstantValueOpt)
            Dim isImplicit As Boolean = boundCall.WasCompilerGenerated
            Return New LazyInvocationExpression(targetMethod, instance, isVirtual, argumentsInEvaluationOrder, _semanticModel, syntax, type, constantValue, isImplicit)
        End Function

        Private Function CreateBoundOmittedArgumentOperation(boundOmittedArgument As BoundOmittedArgument) As IOmittedArgumentExpression
            Dim syntax As SyntaxNode = boundOmittedArgument.Syntax
            Dim type As ITypeSymbol = boundOmittedArgument.Type
            Dim constantValue As [Optional](Of Object) = ConvertToOptional(boundOmittedArgument.ConstantValueOpt)
            Dim isImplicit As Boolean = boundOmittedArgument.WasCompilerGenerated
            Return New OmittedArgumentExpression(_semanticModel, syntax, type, constantValue, isImplicit)
        End Function

        Private Function CreateBoundParenthesizedOperation(boundParenthesized As BoundParenthesized) As IParenthesizedExpression
            Dim operand As Lazy(Of IOperation) = New Lazy(Of IOperation)(Function() Create(boundParenthesized.Expression))
            Dim syntax As SyntaxNode = boundParenthesized.Syntax
            Dim type As ITypeSymbol = boundParenthesized.Type
            Dim constantValue As [Optional](Of Object) = ConvertToOptional(boundParenthesized.ConstantValueOpt)
            Dim isImplicit As Boolean = boundParenthesized.WasCompilerGenerated
            Return New LazyParenthesizedExpression(operand, _semanticModel, syntax, type, constantValue, isImplicit)
        End Function

        Private Function CreateBoundArrayAccessOperation(boundArrayAccess As BoundArrayAccess) As IArrayElementReferenceExpression
            Dim arrayReference As Lazy(Of IOperation) = New Lazy(Of IOperation)(Function() Create(boundArrayAccess.Expression))
            Dim indices As Lazy(Of ImmutableArray(Of IOperation)) = New Lazy(Of ImmutableArray(Of IOperation))(Function() boundArrayAccess.Indices.SelectAsArray(Function(n) DirectCast(Create(n), IOperation)))
            Dim syntax As SyntaxNode = boundArrayAccess.Syntax
            Dim type As ITypeSymbol = boundArrayAccess.Type
            Dim constantValue As [Optional](Of Object) = ConvertToOptional(boundArrayAccess.ConstantValueOpt)
            Dim isImplicit As Boolean = boundArrayAccess.WasCompilerGenerated
            Return New LazyArrayElementReferenceExpression(arrayReference, indices, _semanticModel, syntax, type, constantValue, isImplicit)
        End Function

        Private Function CreateBoundUnaryOperatorOperation(boundUnaryOperator As BoundUnaryOperator) As IUnaryOperatorExpression
            Dim operatorKind As UnaryOperatorKind = Helper.DeriveUnaryOperatorKind(boundUnaryOperator.OperatorKind)
            Dim operand As Lazy(Of IOperation) = New Lazy(Of IOperation)(Function() Create(boundUnaryOperator.Operand))
            Dim usesOperatorMethod As Boolean = False
            Dim operatorMethod As IMethodSymbol = Nothing
            Dim syntax As SyntaxNode = boundUnaryOperator.Syntax
            Dim type As ITypeSymbol = boundUnaryOperator.Type
            Dim constantValue As [Optional](Of Object) = ConvertToOptional(boundUnaryOperator.ConstantValueOpt)
            Dim isLifted As Boolean = (boundUnaryOperator.OperatorKind And VisualBasic.UnaryOperatorKind.Lifted) <> 0
            Dim isChecked As Boolean = boundUnaryOperator.Checked
            Dim isImplicit As Boolean = boundUnaryOperator.WasCompilerGenerated
            Return New LazyUnaryOperatorExpression(operatorKind, operand, isLifted, isChecked, usesOperatorMethod, operatorMethod, _semanticModel, syntax, type, constantValue, isImplicit)
        End Function

        Private Function CreateBoundUserDefinedUnaryOperatorOperation(boundUserDefinedUnaryOperator As BoundUserDefinedUnaryOperator) As IUnaryOperatorExpression
            Dim operatorKind As UnaryOperatorKind = Helper.DeriveUnaryOperatorKind(boundUserDefinedUnaryOperator.OperatorKind)
            Dim operand As Lazy(Of IOperation) = New Lazy(Of IOperation)(Function()
                                                                             If boundUserDefinedUnaryOperator.UnderlyingExpression.Kind = BoundKind.Call Then
                                                                                 Return Create(boundUserDefinedUnaryOperator.Operand)
                                                                             Else
                                                                                 Return GetChildOfBadExpression(boundUserDefinedUnaryOperator.UnderlyingExpression, 0)
                                                                             End If
                                                                         End Function)
            Dim operatorMethod As IMethodSymbol = If(boundUserDefinedUnaryOperator.UnderlyingExpression.Kind = BoundKind.Call, boundUserDefinedUnaryOperator.Call.Method, Nothing)
            Dim usesOperatorMethod As Boolean = operatorMethod IsNot Nothing
            Dim syntax As SyntaxNode = boundUserDefinedUnaryOperator.Syntax
            Dim type As ITypeSymbol = boundUserDefinedUnaryOperator.Type
            Dim constantValue As [Optional](Of Object) = ConvertToOptional(boundUserDefinedUnaryOperator.ConstantValueOpt)
            Dim isLifted As Boolean = (boundUserDefinedUnaryOperator.OperatorKind And VisualBasic.UnaryOperatorKind.Lifted) <> 0
            Dim isChecked As Boolean = False
            Dim isImplicit As Boolean = boundUserDefinedUnaryOperator.WasCompilerGenerated
            Return New LazyUnaryOperatorExpression(operatorKind, operand, isLifted, isChecked, usesOperatorMethod, operatorMethod, _semanticModel, syntax, type, constantValue, isImplicit)
        End Function

        Private Function CreateBoundBinaryOperatorOperation(boundBinaryOperator As BoundBinaryOperator) As IBinaryOperatorExpression
            Dim operatorKind As BinaryOperatorKind = Helper.DeriveBinaryOperatorKind(boundBinaryOperator.OperatorKind, boundBinaryOperator.Left)
            Dim leftOperand As Lazy(Of IOperation) = New Lazy(Of IOperation)(Function() Create(boundBinaryOperator.Left))
            Dim rightOperand As Lazy(Of IOperation) = New Lazy(Of IOperation)(Function() Create(boundBinaryOperator.Right))
            Dim usesOperatorMethod As Boolean = False
            Dim operatorMethod As IMethodSymbol = Nothing
            Dim syntax As SyntaxNode = boundBinaryOperator.Syntax
            Dim type As ITypeSymbol = boundBinaryOperator.Type
            Dim constantValue As [Optional](Of Object) = ConvertToOptional(boundBinaryOperator.ConstantValueOpt)
            Dim isLifted As Boolean = (boundBinaryOperator.OperatorKind And VisualBasic.BinaryOperatorKind.Lifted) <> 0
            Dim isChecked As Boolean = boundBinaryOperator.Checked
            Dim isCompareText As Boolean = (boundBinaryOperator.OperatorKind And VisualBasic.BinaryOperatorKind.CompareText) <> 0
            Dim isImplicit As Boolean = boundBinaryOperator.WasCompilerGenerated
            Return New LazyBinaryOperatorExpression(operatorKind, leftOperand, rightOperand, isLifted, isChecked, isCompareText, usesOperatorMethod, operatorMethod, _semanticModel, syntax, type, constantValue, isImplicit)
        End Function

        Private Function CreateBoundUserDefinedBinaryOperatorOperation(boundUserDefinedBinaryOperator As BoundUserDefinedBinaryOperator) As IBinaryOperatorExpression
            Dim operatorKind As BinaryOperatorKind = Helper.DeriveBinaryOperatorKind(boundUserDefinedBinaryOperator.OperatorKind, leftOpt:=Nothing)
            Dim leftOperand As Lazy(Of IOperation) = New Lazy(Of IOperation)(Function() GetUserDefinedBinaryOperatorChild(boundUserDefinedBinaryOperator, 0))
            Dim rightOperand As Lazy(Of IOperation) = New Lazy(Of IOperation)(Function() GetUserDefinedBinaryOperatorChild(boundUserDefinedBinaryOperator, 1))
            Dim operatorMethod As IMethodSymbol = If(boundUserDefinedBinaryOperator.UnderlyingExpression.Kind = BoundKind.Call, boundUserDefinedBinaryOperator.Call.Method, Nothing)
            Dim usesOperatorMethod As Boolean = operatorMethod IsNot Nothing
            Dim syntax As SyntaxNode = boundUserDefinedBinaryOperator.Syntax
            Dim type As ITypeSymbol = boundUserDefinedBinaryOperator.Type
            Dim constantValue As [Optional](Of Object) = ConvertToOptional(boundUserDefinedBinaryOperator.ConstantValueOpt)
            Dim isLifted As Boolean = (boundUserDefinedBinaryOperator.OperatorKind And VisualBasic.BinaryOperatorKind.Lifted) <> 0
            Dim isChecked As Boolean = boundUserDefinedBinaryOperator.Checked
            Dim isCompareText As Boolean = False
            Dim isImplicit As Boolean = boundUserDefinedBinaryOperator.WasCompilerGenerated
            Return New LazyBinaryOperatorExpression(operatorKind, leftOperand, rightOperand, isLifted, isChecked, isCompareText, usesOperatorMethod, operatorMethod, _semanticModel, syntax, type, constantValue, isImplicit)
        End Function

        Private Function CreateBoundBinaryConditionalExpressionOperation(boundBinaryConditionalExpression As BoundBinaryConditionalExpression) As ICoalesceExpression
            Dim expression As Lazy(Of IOperation) = New Lazy(Of IOperation)(Function() Create(boundBinaryConditionalExpression.TestExpression))
            Dim whenNull As Lazy(Of IOperation) = New Lazy(Of IOperation)(Function() Create(boundBinaryConditionalExpression.ElseExpression))
            Dim syntax As SyntaxNode = boundBinaryConditionalExpression.Syntax
            Dim type As ITypeSymbol = boundBinaryConditionalExpression.Type
            Dim constantValue As [Optional](Of Object) = ConvertToOptional(boundBinaryConditionalExpression.ConstantValueOpt)
            Dim isImplicit As Boolean = boundBinaryConditionalExpression.WasCompilerGenerated
            Return New LazyCoalesceExpression(expression, whenNull, _semanticModel, syntax, type, constantValue, isImplicit)
        End Function

        Private Function CreateBoundUserDefinedShortCircuitingOperatorOperation(boundUserDefinedShortCircuitingOperator As BoundUserDefinedShortCircuitingOperator) As IBinaryOperatorExpression
            Dim operatorKind As BinaryOperatorKind = If((boundUserDefinedShortCircuitingOperator.BitwiseOperator.OperatorKind And VisualBasic.BinaryOperatorKind.And) <> 0, BinaryOperatorKind.ConditionalAnd, BinaryOperatorKind.ConditionalOr)
            Dim leftOperand As Lazy(Of IOperation) = New Lazy(Of IOperation)(Function() Create(boundUserDefinedShortCircuitingOperator.LeftOperand))
            Dim rightOperand As Lazy(Of IOperation) = New Lazy(Of IOperation)(Function() Create(boundUserDefinedShortCircuitingOperator.BitwiseOperator.Right))
            Dim usesOperatorMethod As Boolean = True
            Dim operatorMethod As IMethodSymbol = boundUserDefinedShortCircuitingOperator.BitwiseOperator.Call.Method
            Dim syntax As SyntaxNode = boundUserDefinedShortCircuitingOperator.Syntax
            Dim type As ITypeSymbol = boundUserDefinedShortCircuitingOperator.Type
            Dim constantValue As [Optional](Of Object) = ConvertToOptional(boundUserDefinedShortCircuitingOperator.ConstantValueOpt)
            Dim isLifted As Boolean = (boundUserDefinedShortCircuitingOperator.BitwiseOperator.OperatorKind And VisualBasic.BinaryOperatorKind.Lifted) <> 0
            Dim isChecked As Boolean = False
            Dim isCompareText As Boolean = False
            Dim isImplicit As Boolean = boundUserDefinedShortCircuitingOperator.WasCompilerGenerated
            Return New LazyBinaryOperatorExpression(operatorKind, leftOperand, rightOperand, isLifted, isChecked, isCompareText, usesOperatorMethod, operatorMethod, _semanticModel, syntax, type, constantValue, isImplicit)
        End Function

        Private Function CreateBoundBadExpressionOperation(boundBadExpression As BoundBadExpression) As IInvalidExpression
            Dim children As Lazy(Of ImmutableArray(Of IOperation)) = New Lazy(Of ImmutableArray(Of IOperation))(Function() boundBadExpression.ChildBoundNodes.SelectAsArray(Function(n) Create(n)))
            Dim syntax As SyntaxNode = boundBadExpression.Syntax
            ' We match semantic model here: If the Then expression IsMissing, we have a null type, rather than the ErrorType Of the bound node.
            Dim type As ITypeSymbol = If(syntax.IsMissing, Nothing, boundBadExpression.Type)
            Dim constantValue As [Optional](Of Object) = ConvertToOptional(boundBadExpression.ConstantValueOpt)

            ' if child has syntax node point to same syntax node as bad expression, then this invalid expression Is implicit
            Dim isImplicit = boundBadExpression.WasCompilerGenerated OrElse boundBadExpression.ChildBoundNodes.Any(Function(e) e?.Syntax Is boundBadExpression.Syntax)
            Return New LazyInvalidExpression(children, _semanticModel, syntax, type, constantValue, isImplicit)
        End Function

        Private Function CreateBoundTryCastOperation(boundTryCast As BoundTryCast) As IOperation
            Dim syntax As SyntaxNode = boundTryCast.Syntax
            Dim type As ITypeSymbol = boundTryCast.Type
            Dim isImplicit As Boolean = boundTryCast.WasCompilerGenerated

            Dim constantValue As [Optional](Of Object) = ConvertToOptional(boundTryCast.ConstantValueOpt)
            Dim conversionInformation = CreateConversionOperation(boundTryCast.Operand, boundTryCast.ConversionKind, boundTryCast.Syntax)
            Dim methodSymbol As MethodSymbol = conversionInformation.methodSymbol
            Dim operand As Lazy(Of IOperation) = conversionInformation.operation
            Dim isAddressOfDelegateCreation As Boolean = conversionInformation.isAddressOfDelegateCreation

            If isAddressOfDelegateCreation OrElse
               ((boundTryCast.Operand.Kind = BoundKind.Lambda OrElse
                 boundTryCast.Operand.Kind = BoundKind.QueryLambda OrElse
                 boundTryCast.Operand.Kind = BoundKind.UnboundLambda) AndAlso
                boundTryCast.Type.IsDelegateType()) Then
                ' If this is a conversion from a lambda to a delegate type, or this is an AddressOf delegate creation
                ' as determined above, we return a DelegateCreationExpression, instead of returning a conversion expression
                Return New LazyDelegateCreationExpression(operand, _semanticModel, syntax, type, constantValue, isImplicit)
            End If

            Dim conversion As Conversion = New Conversion(New KeyValuePair(Of ConversionKind, MethodSymbol)(boundTryCast.ConversionKind, Nothing))
            Dim isExplicitCastInCode As Boolean = True
            Dim isTryCast As Boolean = True
            Dim isChecked As Boolean = False
            Return New LazyVisualBasicConversionExpression(operand, conversion, isExplicitCastInCode, isTryCast, isChecked, _semanticModel, syntax, type, constantValue, isImplicit)
        End Function

        Private Function CreateBoundDirectCastOperation(boundDirectCast As BoundDirectCast) As IOperation
            Dim syntax As SyntaxNode = boundDirectCast.Syntax
            Dim type As ITypeSymbol = boundDirectCast.Type
            Dim constantValue As [Optional](Of Object) = ConvertToOptional(boundDirectCast.ConstantValueOpt)
            Dim isImplicit As Boolean = boundDirectCast.WasCompilerGenerated

            Dim conversionInformation = CreateConversionOperation(boundDirectCast.Operand, boundDirectCast.ConversionKind, boundDirectCast.Syntax)
            Dim methodSymbol As MethodSymbol = conversionInformation.methodSymbol
            Dim operand As Lazy(Of IOperation) = conversionInformation.operation
            Dim isAddressOfDelegateCreation As Boolean = conversionInformation.isAddressOfDelegateCreation

            If isAddressOfDelegateCreation OrElse
               ((boundDirectCast.Operand.Kind = BoundKind.Lambda OrElse
                 boundDirectCast.Operand.Kind = BoundKind.QueryLambda OrElse
                 boundDirectCast.Operand.Kind = BoundKind.UnboundLambda) AndAlso
                boundDirectCast.Type.IsDelegateType()) Then
                ' If this is a conversion from a lambda to a delegate type, or this is an AddressOf delegate creation
                ' as determined above, we return a DelegateCreationExpression, instead of returning a conversion expression
                Return New LazyDelegateCreationExpression(operand, _semanticModel, syntax, type, constantValue, isImplicit)
            End If

            Dim conversion As Conversion = New Conversion(New KeyValuePair(Of ConversionKind, MethodSymbol)(boundDirectCast.ConversionKind, Nothing))
            Dim isExplicit As Boolean = True
            Dim isTryCast As Boolean = False
            Dim isChecked As Boolean = False
            Return New LazyVisualBasicConversionExpression(operand, conversion, isExplicit, isTryCast, isChecked, _semanticModel, syntax, type, constantValue, isImplicit)
        End Function

        Private Function CreateBoundConversionOperation(boundConversion As BoundConversion) As IOperation
            Dim syntax As SyntaxNode = boundConversion.Syntax

            If syntax.IsMissing Then
                ' If the underlying syntax IsMissing, then that means we're in case where the compiler generated a piece of syntax to fill in for
                ' an error, such as this case:
                '
                '  Dim i =
                '
                ' Semantic model has a special case here that we match: if the underlying syntax is missing, don't create a conversion expression,
                ' and instead directly return the operand, which will be a BoundBadExpression. When we generate a node for the BoundBadExpression,
                ' the resulting IOperation will also have a null Type.
                Debug.Assert(boundConversion.Operand.Kind = BoundKind.BadExpression)
                Return Create(boundConversion.Operand)
            End If

            Dim type As ITypeSymbol = boundConversion.Type
            Dim constantValue As [Optional](Of Object) = ConvertToOptional(boundConversion.ConstantValueOpt)
            Dim isImplicit As Boolean = boundConversion.WasCompilerGenerated

            Dim conversionInformation = CreateConversionOperation(boundConversion.Operand, boundConversion.ConversionKind, boundConversion.Syntax)
            Dim methodSymbol As MethodSymbol = conversionInformation.methodSymbol
            Dim operand As Lazy(Of IOperation) = conversionInformation.operation
            Dim isAddressOfDelegateCreation As Boolean = conversionInformation.isAddressOfDelegateCreation

            If isAddressOfDelegateCreation OrElse
               ((boundConversion.Operand.Kind = BoundKind.Lambda OrElse
                 boundConversion.Operand.Kind = BoundKind.QueryLambda OrElse
                 boundConversion.Operand.Kind = BoundKind.UnboundLambda) AndAlso
                boundConversion.Type.IsDelegateType()) Then
                ' If this is a conversion from a lambda to a delegate type, or this is an AddressOf delegate creation
                ' as determined above, we return a DelegateCreationExpression, instead of returning a conversion expression
                Return New LazyDelegateCreationExpression(operand, _semanticModel, syntax, type, constantValue, isImplicit)
            End If

            Dim conversion = New Conversion(New KeyValuePair(Of VisualBasic.ConversionKind, MethodSymbol)(boundConversion.ConversionKind, methodSymbol))
            Dim isExplicit As Boolean = boundConversion.ExplicitCastInCode
            Dim isTryCast As Boolean = False
            Dim isChecked As Boolean = False
            Return New LazyVisualBasicConversionExpression(operand, conversion, isExplicit, isTryCast, isChecked, _semanticModel, syntax, type, constantValue, isImplicit)
        End Function

        Private Function IsValidDelegateCreationSyntax(conversionKind As ConversionKind, conversionSyntax As SyntaxNode, operandSyntax As SyntaxNode) As Boolean
            ' An identity conversion is introduced by the compiler on top of New DelegateType(AddressOf Method). This node
            ' only exists for the convenience of SemanticModel, so we want to classify it as a Delegate Creation
            Dim validConversionKind = conversionKind = ConversionKind.Identity OrElse
                                      (conversionKind And (Not ConversionKind.DelegateRelaxationLevelMask)) = 0

            ' Any of the explicit cast types, as well as New DelegateType(AddressOf Method)
            Dim validConversionSyntax = conversionSyntax.Kind() = SyntaxKind.CTypeExpression OrElse
                                        conversionSyntax.Kind() = SyntaxKind.DirectCastExpression OrElse
                                        conversionSyntax.Kind() = SyntaxKind.TryCastExpression OrElse
                                        conversionSyntax.Kind() = SyntaxKind.ObjectCollectionInitializer

            Dim validOperandSyntax = operandSyntax.Kind() = SyntaxKind.AddressOfExpression

            Return validConversionKind AndAlso validConversionSyntax AndAlso validOperandSyntax
        End Function

        Private Function CreateConversionOperation(operand As BoundNode, conversionKind As ConversionKind, conversionSyntax As SyntaxNode) As (methodSymbol As MethodSymbol, operation As Lazy(Of IOperation), isAddressOfDelegateCreation As Boolean)
            If (conversionKind And VisualBasic.ConversionKind.UserDefined) = VisualBasic.ConversionKind.UserDefined Then
                Dim userDefinedConversion As BoundUserDefinedConversion = DirectCast(operand, BoundUserDefinedConversion)
                Return (userDefinedConversion.Call.Method, New Lazy(Of IOperation)(Function() Create(userDefinedConversion.Operand)), isAddressOfDelegateCreation:=False)
            ElseIf IsValidDelegateCreationSyntax(conversionKind, conversionSyntax, operand.Syntax) Then
                ' In this scenario, we're a delegate creation expression involving an AddressOf.
                Dim methodSymbol As MethodSymbol = Nothing
                Dim operandLazy As Lazy(Of IOperation)
                If operand.Kind = BoundKind.DelegateCreationExpression Then
                    operandLazy = New Lazy(Of IOperation)(Function() CreateBoundDelegateCreationExpressionChildOperation(DirectCast(operand, BoundDelegateCreationExpression)))
                Else
                    ' This is an error scenario in which we have a delegate conversion, but it failed to bind correctly, so the child here
                    ' is either a bad expression or a method group. Delegate to standard operation handling for this.
                    Debug.Assert(operand.Kind = BoundKind.MethodGroup OrElse
                                 operand.Kind = BoundKind.BadExpression OrElse
                                 operand.Kind = BoundKind.AddressOfOperator)
                    operandLazy = New Lazy(Of IOperation)(Function() Create(operand))
                End If
                Return (methodSymbol, operandLazy, isAddressOfDelegateCreation:=True)
            Else
                Dim methodSymbol As MethodSymbol = Nothing
                Return (methodSymbol, New Lazy(Of IOperation)(Function() Create(operand)), isAddressOfDelegateCreation:=False)
            End If
        End Function

        Private Function CreateBoundDelegateCreationExpressionOperation(boundDelegateCreationExpression As BoundDelegateCreationExpression) As IDelegateCreationExpression
            Dim syntax As SyntaxNode = boundDelegateCreationExpression.Syntax
            Dim type As ITypeSymbol = boundDelegateCreationExpression.Type
            Dim constantValue As [Optional](Of Object) = ConvertToOptional(boundDelegateCreationExpression.ConstantValueOpt)
            Dim isImplicit As Boolean = boundDelegateCreationExpression.WasCompilerGenerated

            Dim target As Lazy(Of IOperation) = New Lazy(Of IOperation)(Function() CreateBoundDelegateCreationExpressionChildOperation(boundDelegateCreationExpression))

            Return New LazyDelegateCreationExpression(target, _semanticModel, syntax, type, constantValue, isImplicit)
        End Function

        Private Function CreateBoundDelegateCreationExpressionChildOperation(boundDelegateCreationExpression As BoundDelegateCreationExpression) As IMethodReferenceExpression
            Dim method As IMethodSymbol = boundDelegateCreationExpression.Method
            Dim isVirtual As Boolean = method IsNot Nothing AndAlso
                                               (method.IsAbstract OrElse method.IsOverride OrElse method.IsVirtual) AndAlso
                                               Not boundDelegateCreationExpression.SuppressVirtualCalls
            Dim instance As Lazy(Of IOperation) =
                        New Lazy(Of IOperation)(Function() Create(If(boundDelegateCreationExpression.ReceiverOpt, boundDelegateCreationExpression.MethodGroupOpt?.ReceiverOpt)))
            Dim syntax As SyntaxNode = boundDelegateCreationExpression.Syntax
            Dim type As ITypeSymbol = boundDelegateCreationExpression.Type
            Dim constantValue As [Optional](Of Object) = ConvertToOptional(boundDelegateCreationExpression.ConstantValueOpt)
            Dim isImplicit As Boolean = boundDelegateCreationExpression.WasCompilerGenerated
            Return New LazyMethodReferenceExpression(method, isVirtual, instance, _semanticModel, syntax, type, constantValue, isImplicit)
        End Function

        Private Function CreateBoundTernaryConditionalExpressionOperation(boundTernaryConditionalExpression As BoundTernaryConditionalExpression) As IConditionalExpression
            Dim condition As Lazy(Of IOperation) = New Lazy(Of IOperation)(Function() Create(boundTernaryConditionalExpression.Condition))
            Dim whenTrue As Lazy(Of IOperation) = New Lazy(Of IOperation)(Function() Create(boundTernaryConditionalExpression.WhenTrue))
            Dim whenFalse As Lazy(Of IOperation) = New Lazy(Of IOperation)(Function() Create(boundTernaryConditionalExpression.WhenFalse))
            Dim syntax As SyntaxNode = boundTernaryConditionalExpression.Syntax
            Dim type As ITypeSymbol = boundTernaryConditionalExpression.Type
            Dim constantValue As [Optional](Of Object) = ConvertToOptional(boundTernaryConditionalExpression.ConstantValueOpt)
            Dim isImplicit As Boolean = boundTernaryConditionalExpression.WasCompilerGenerated
            Return New LazyConditionalExpression(condition, whenTrue, whenFalse, _semanticModel, syntax, type, constantValue, isImplicit)
        End Function

        Private Function CreateBoundTypeOfOperation(boundTypeOf As BoundTypeOf) As IIsTypeExpression
            Dim operand As Lazy(Of IOperation) = New Lazy(Of IOperation)(Function() Create(boundTypeOf.Operand))
            Dim isType As ITypeSymbol = boundTypeOf.TargetType
            Dim isNotTypeExpression As Boolean = boundTypeOf.IsTypeOfIsNotExpression
            Dim syntax As SyntaxNode = boundTypeOf.Syntax
            Dim type As ITypeSymbol = boundTypeOf.Type
            Dim constantValue As [Optional](Of Object) = ConvertToOptional(boundTypeOf.ConstantValueOpt)
            Dim isImplicit As Boolean = boundTypeOf.WasCompilerGenerated
            Return New LazyIsTypeExpression(operand, isType, isNotTypeExpression, _semanticModel, syntax, type, constantValue, isImplicit)
        End Function

        Private Function CreateBoundGetTypeOperation(boundGetType As BoundGetType) As ITypeOfExpression
            Dim typeOperand As ITypeSymbol = boundGetType.SourceType.Type
            Dim syntax As SyntaxNode = boundGetType.Syntax
            Dim type As ITypeSymbol = boundGetType.Type
            Dim constantValue As [Optional](Of Object) = ConvertToOptional(boundGetType.ConstantValueOpt)
            Dim isImplicit As Boolean = boundGetType.WasCompilerGenerated
            Return New TypeOfExpression(typeOperand, _semanticModel, syntax, type, constantValue, isImplicit)
        End Function

        Private Function CreateBoundLateInvocationOperation(boundLateInvocation As BoundLateInvocation) As IOperation
            Dim expression As Lazy(Of IOperation) = New Lazy(Of IOperation)(Function() Create(boundLateInvocation.Member))
            Dim arguments As Lazy(Of ImmutableArray(Of IOperation)) = New Lazy(Of ImmutableArray(Of IOperation))(
                Function()
                    Return If(boundLateInvocation.ArgumentsOpt.IsDefault,
                    ImmutableArray(Of IOperation).Empty,
                    boundLateInvocation.ArgumentsOpt.SelectAsArray(Function(n) Create(n)))
                End Function)
            Dim argumentNames As ImmutableArray(Of String) = boundLateInvocation.ArgumentNamesOpt
            Dim argumentRefKinds As ImmutableArray(Of RefKind) = Nothing
            Dim syntax As SyntaxNode = boundLateInvocation.Syntax
            Dim type As ITypeSymbol = boundLateInvocation.Type
            Dim constantValue As [Optional](Of Object) = ConvertToOptional(boundLateInvocation.ConstantValueOpt)
            Dim isImplicit As Boolean = boundLateInvocation.WasCompilerGenerated
            Return New LazyDynamicInvocationExpression(expression, arguments, argumentNames, argumentRefKinds, _semanticModel, syntax, type, constantValue, isImplicit)
        End Function

        Private Function CreateBoundObjectCreationExpressionOperation(boundObjectCreationExpression As BoundObjectCreationExpression) As IObjectCreationExpression
            Dim constructor As IMethodSymbol = boundObjectCreationExpression.ConstructorOpt
            Dim memberInitializers As Lazy(Of IObjectOrCollectionInitializerExpression) = New Lazy(Of IObjectOrCollectionInitializerExpression)(
                Function()
                    Return DirectCast(Create(boundObjectCreationExpression.InitializerOpt), IObjectOrCollectionInitializerExpression)
                End Function)

            Debug.Assert(boundObjectCreationExpression.ConstructorOpt IsNot Nothing OrElse boundObjectCreationExpression.Arguments.IsEmpty())
            Dim argumentsInEvaluationOrder As Lazy(Of ImmutableArray(Of IArgument)) = New Lazy(Of ImmutableArray(Of IArgument))(
                Function()
                    Return If(boundObjectCreationExpression.ConstructorOpt Is Nothing,
                        ImmutableArray(Of IArgument).Empty,
                        DeriveArguments(boundObjectCreationExpression.Arguments, boundObjectCreationExpression.ConstructorOpt.Parameters))
                End Function)

            Dim syntax As SyntaxNode = boundObjectCreationExpression.Syntax
            Dim type As ITypeSymbol = boundObjectCreationExpression.Type
            Dim constantValue As [Optional](Of Object) = ConvertToOptional(boundObjectCreationExpression.ConstantValueOpt)
            Dim isImplicit As Boolean = boundObjectCreationExpression.WasCompilerGenerated
            Return New LazyObjectCreationExpression(constructor, memberInitializers, argumentsInEvaluationOrder, _semanticModel, syntax, type, constantValue, isImplicit)
        End Function

        Private Function CreateBoundObjectInitializerExpressionOperation(boundObjectInitializerExpression As BoundObjectInitializerExpression) As IObjectOrCollectionInitializerExpression
            Dim initializers As Lazy(Of ImmutableArray(Of IOperation)) = New Lazy(Of ImmutableArray(Of IOperation))(Function() boundObjectInitializerExpression.Initializers.SelectAsArray(Function(n) Create(n)))
            Dim syntax As SyntaxNode = boundObjectInitializerExpression.Syntax
            Dim type As ITypeSymbol = boundObjectInitializerExpression.Type
            Dim constantValue As [Optional](Of Object) = ConvertToOptional(boundObjectInitializerExpression.ConstantValueOpt)
            Dim isImplicit As Boolean = boundObjectInitializerExpression.WasCompilerGenerated
            Return New LazyObjectOrCollectionInitializerExpression(initializers, _semanticModel, syntax, type, constantValue, isImplicit)
        End Function

        Private Function CreateBoundCollectionInitializerExpressionOperation(boundCollectionInitializerExpression As BoundCollectionInitializerExpression) As IObjectOrCollectionInitializerExpression
            Dim initializers As Lazy(Of ImmutableArray(Of IOperation)) = New Lazy(Of ImmutableArray(Of IOperation))(Function() boundCollectionInitializerExpression.Initializers.SelectAsArray(Function(n) CreateBoundCollectionElementInitializerOperation(n)))
            Dim syntax As SyntaxNode = boundCollectionInitializerExpression.Syntax
            Dim type As ITypeSymbol = boundCollectionInitializerExpression.Type
            Dim constantValue As [Optional](Of Object) = ConvertToOptional(boundCollectionInitializerExpression.ConstantValueOpt)
            Dim isImplicit As Boolean = boundCollectionInitializerExpression.WasCompilerGenerated
            Return New LazyObjectOrCollectionInitializerExpression(initializers, _semanticModel, syntax, type, constantValue, isImplicit)
        End Function

        Private Function CreateBoundCollectionElementInitializerOperation(boundExpression As BoundExpression) As IOperation
            If boundExpression.Kind <> BoundKind.Call Then
                ' Error case, not an Add method call for collection element initializer
                Return Create(boundExpression)
            End If
            Dim boundCall = DirectCast(boundExpression, BoundCall)
            Dim addMethod As IMethodSymbol = boundCall.Method
            Dim arguments As Lazy(Of ImmutableArray(Of IOperation)) = New Lazy(Of ImmutableArray(Of IOperation))(Function() boundCall.Arguments.SelectAsArray(Function(n) Create(n)))
            Dim isDynamic As Boolean = addMethod Is Nothing
            Dim syntax As SyntaxNode = boundExpression.Syntax
            Dim type As ITypeSymbol = boundExpression.Type
            Dim constantValue As [Optional](Of Object) = ConvertToOptional(boundExpression.ConstantValueOpt)
            Dim isImplicit As Boolean = boundExpression.WasCompilerGenerated
            Return New LazyCollectionElementInitializerExpression(addMethod, arguments, isDynamic, _semanticModel, syntax, type, constantValue, isImplicit)
        End Function

        Private Function CreateBoundNewTOperation(boundNewT As BoundNewT) As ITypeParameterObjectCreationExpression
            Dim initializer As Lazy(Of IObjectOrCollectionInitializerExpression) = New Lazy(Of IObjectOrCollectionInitializerExpression)(Function() DirectCast(Create(boundNewT.InitializerOpt), IObjectOrCollectionInitializerExpression))
            Dim syntax As SyntaxNode = boundNewT.Syntax
            Dim type As ITypeSymbol = boundNewT.Type
            Dim constantValue As [Optional](Of Object) = ConvertToOptional(boundNewT.ConstantValueOpt)
            Dim isImplicit As Boolean = boundNewT.WasCompilerGenerated
            Return New LazyTypeParameterObjectCreationExpression(initializer, _semanticModel, syntax, type, constantValue, isImplicit)
        End Function

        Private Function CreateBoundArrayCreationOperation(boundArrayCreation As BoundArrayCreation) As IArrayCreationExpression
            Dim elementType As ITypeSymbol = TryCast(boundArrayCreation.Type, IArrayTypeSymbol)?.ElementType
            Dim dimensionSizes As Lazy(Of ImmutableArray(Of IOperation)) = New Lazy(Of ImmutableArray(Of IOperation))(Function() boundArrayCreation.Bounds.SelectAsArray(Function(n) Create(n)))
            Dim initializer As Lazy(Of IArrayInitializer) = New Lazy(Of IArrayInitializer)(Function() DirectCast(Create(boundArrayCreation.InitializerOpt), IArrayInitializer))
            Dim syntax As SyntaxNode = boundArrayCreation.Syntax
            Dim type As ITypeSymbol = boundArrayCreation.Type
            Dim constantValue As [Optional](Of Object) = ConvertToOptional(boundArrayCreation.ConstantValueOpt)
            Dim isImplicit As Boolean = boundArrayCreation.WasCompilerGenerated
            Return New LazyArrayCreationExpression(elementType, dimensionSizes, initializer, _semanticModel, syntax, type, constantValue, isImplicit)
        End Function

        Private Function CreateBoundArrayInitializationOperation(boundArrayInitialization As BoundArrayInitialization) As IArrayInitializer
            Dim elementValues As Lazy(Of ImmutableArray(Of IOperation)) = New Lazy(Of ImmutableArray(Of IOperation))(Function() boundArrayInitialization.Initializers.SelectAsArray(Function(n) Create(n)))
            Dim syntax As SyntaxNode = boundArrayInitialization.Syntax
            Dim type As ITypeSymbol = boundArrayInitialization.Type
            Dim constantValue As [Optional](Of Object) = ConvertToOptional(boundArrayInitialization.ConstantValueOpt)
            Dim isImplicit As Boolean = boundArrayInitialization.WasCompilerGenerated
            Return New LazyArrayInitializer(elementValues, _semanticModel, syntax, type, constantValue, isImplicit)
        End Function

        Private Function CreateBoundPropertyAccessOperation(boundPropertyAccess As BoundPropertyAccess) As IPropertyReferenceExpression
            Dim instance As Lazy(Of IOperation) = New Lazy(Of IOperation)(
                Function()
                    If boundPropertyAccess.PropertySymbol.IsShared Then
                        Return Nothing
                    Else
                        Return Create(boundPropertyAccess.ReceiverOpt)
                    End If
                End Function)

            Dim [property] As IPropertySymbol = boundPropertyAccess.PropertySymbol
            Dim argumentsInEvaluationOrder As Lazy(Of ImmutableArray(Of IArgument)) = New Lazy(Of ImmutableArray(Of IArgument))(
                Function()
                    Return If(boundPropertyAccess.Arguments.Length = 0,
                        ImmutableArray(Of IArgument).Empty,
                        DeriveArguments(boundPropertyAccess.Arguments, boundPropertyAccess.PropertySymbol.Parameters))
                End Function)
            Dim syntax As SyntaxNode = boundPropertyAccess.Syntax
            Dim type As ITypeSymbol = boundPropertyAccess.Type
            Dim constantValue As [Optional](Of Object) = ConvertToOptional(boundPropertyAccess.ConstantValueOpt)
            Dim isImplicit As Boolean = boundPropertyAccess.WasCompilerGenerated
            Return New LazyPropertyReferenceExpression([property], instance, [property], argumentsInEvaluationOrder, _semanticModel, syntax, type, constantValue, isImplicit)
        End Function

        Private Function CreateBoundEventAccessOperation(boundEventAccess As BoundEventAccess) As IEventReferenceExpression
            Dim instance As Lazy(Of IOperation) = New Lazy(Of IOperation)(
                Function()
                    If boundEventAccess.EventSymbol.IsShared Then
                        Return Nothing
                    Else
                        Return Create(boundEventAccess.ReceiverOpt)
                    End If
                End Function)

            Dim [event] As IEventSymbol = boundEventAccess.EventSymbol
            Dim syntax As SyntaxNode = boundEventAccess.Syntax
            Dim type As ITypeSymbol = boundEventAccess.Type
            Dim constantValue As [Optional](Of Object) = ConvertToOptional(boundEventAccess.ConstantValueOpt)
            Dim isImplicit As Boolean = boundEventAccess.WasCompilerGenerated
            Return New LazyEventReferenceExpression([event], instance, [event], _semanticModel, syntax, type, constantValue, isImplicit)
        End Function

        Private Function CreateBoundFieldAccessOperation(boundFieldAccess As BoundFieldAccess) As IFieldReferenceExpression
            Dim field As IFieldSymbol = boundFieldAccess.FieldSymbol
            Dim isDeclaration As Boolean = False
            Dim instance As Lazy(Of IOperation) = New Lazy(Of IOperation)(
                Function()
                    If boundFieldAccess.FieldSymbol.IsShared Then
                        Return Nothing
                    Else
                        Return Create(boundFieldAccess.ReceiverOpt)
                    End If
                End Function)

            Dim member As ISymbol = boundFieldAccess.FieldSymbol
            Dim syntax As SyntaxNode = boundFieldAccess.Syntax
            Dim type As ITypeSymbol = boundFieldAccess.Type
            Dim constantValue As [Optional](Of Object) = ConvertToOptional(boundFieldAccess.ConstantValueOpt)
            Dim isImplicit As Boolean = boundFieldAccess.WasCompilerGenerated
            Return New LazyFieldReferenceExpression(field, isDeclaration, instance, member, _semanticModel, syntax, type, constantValue, isImplicit)
        End Function

        Private Function CreateBoundConditionalAccessOperation(boundConditionalAccess As BoundConditionalAccess) As IConditionalAccessExpression
            Dim whenNotNull As Lazy(Of IOperation) = New Lazy(Of IOperation)(Function() Create(boundConditionalAccess.AccessExpression))
            Dim expression As Lazy(Of IOperation) = New Lazy(Of IOperation)(Function() Create(boundConditionalAccess.Receiver))
            Dim syntax As SyntaxNode = boundConditionalAccess.Syntax
            Dim type As ITypeSymbol = boundConditionalAccess.Type
            Dim constantValue As [Optional](Of Object) = ConvertToOptional(boundConditionalAccess.ConstantValueOpt)
            Dim isImplicit As Boolean = boundConditionalAccess.WasCompilerGenerated
            Return New LazyConditionalAccessExpression(whenNotNull, expression, _semanticModel, syntax, type, constantValue, isImplicit)
        End Function

        Private Function CreateBoundConditionalAccessReceiverPlaceholderOperation(boundConditionalAccessReceiverPlaceholder As BoundConditionalAccessReceiverPlaceholder) As IConditionalAccessInstanceExpression
            Dim syntax As SyntaxNode = boundConditionalAccessReceiverPlaceholder.Syntax
            Dim type As ITypeSymbol = boundConditionalAccessReceiverPlaceholder.Type
            Dim constantValue As [Optional](Of Object) = ConvertToOptional(boundConditionalAccessReceiverPlaceholder.ConstantValueOpt)
            Dim isImplicit As Boolean = boundConditionalAccessReceiverPlaceholder.WasCompilerGenerated
            Return New ConditionalAccessInstanceExpression(_semanticModel, syntax, type, constantValue, isImplicit)
        End Function

        Private Function CreateBoundParameterOperation(boundParameter As BoundParameter) As IParameterReferenceExpression
            Dim parameter As IParameterSymbol = boundParameter.ParameterSymbol
            Dim syntax As SyntaxNode = boundParameter.Syntax
            Dim type As ITypeSymbol = boundParameter.Type
            Dim constantValue As [Optional](Of Object) = ConvertToOptional(boundParameter.ConstantValueOpt)
            Dim isImplicit As Boolean = boundParameter.WasCompilerGenerated
            Return New ParameterReferenceExpression(parameter, _semanticModel, syntax, type, constantValue, isImplicit)
        End Function

        Private Function CreateBoundLocalOperation(boundLocal As BoundLocal) As ILocalReferenceExpression
            Dim local As ILocalSymbol = boundLocal.LocalSymbol
            Dim isDeclaration As Boolean = False
            Dim syntax As SyntaxNode = boundLocal.Syntax
            Dim type As ITypeSymbol = boundLocal.Type
            Dim constantValue As [Optional](Of Object) = ConvertToOptional(boundLocal.ConstantValueOpt)
            Dim isImplicit As Boolean = boundLocal.WasCompilerGenerated
            Return New LocalReferenceExpression(local, isDeclaration, _semanticModel, syntax, type, constantValue, isImplicit)
        End Function

        Private Function CreateBoundLateMemberAccessOperation(boundLateMemberAccess As BoundLateMemberAccess) As IDynamicMemberReferenceExpression
            Dim instance As Lazy(Of IOperation) = New Lazy(Of IOperation)(Function() Create(boundLateMemberAccess.ReceiverOpt))
            Dim memberName As String = boundLateMemberAccess.NameOpt
            Dim typeArguments As ImmutableArray(Of ITypeSymbol) = ImmutableArray(Of ITypeSymbol).Empty
            If boundLateMemberAccess.TypeArgumentsOpt IsNot Nothing Then
                typeArguments = ImmutableArray(Of ITypeSymbol).CastUp(boundLateMemberAccess.TypeArgumentsOpt.Arguments)
            End If
            Dim containingType As ITypeSymbol = Nothing
            ' If there's nothing being late-bound against, something is very wrong
            Debug.Assert(boundLateMemberAccess.ReceiverOpt IsNot Nothing OrElse boundLateMemberAccess.ContainerTypeOpt IsNot Nothing)
            ' Only set containing type if the container is set to something, and either there is no reciever, or the receiver's type
            ' does not match the type of the containing type.
            If (boundLateMemberAccess.ContainerTypeOpt IsNot Nothing AndAlso
                (boundLateMemberAccess.ReceiverOpt Is Nothing OrElse
                 boundLateMemberAccess.ContainerTypeOpt <> boundLateMemberAccess.ReceiverOpt.Type)) Then
                containingType = boundLateMemberAccess.ContainerTypeOpt
            End If
            Dim syntax As SyntaxNode = boundLateMemberAccess.Syntax
            Dim type As ITypeSymbol = boundLateMemberAccess.Type
            Dim constantValue As [Optional](Of Object) = ConvertToOptional(boundLateMemberAccess.ConstantValueOpt)
            Dim isImplicit As Boolean = boundLateMemberAccess.WasCompilerGenerated
            Return New LazyDynamicMemberReferenceExpression(instance, memberName, typeArguments, containingType, _semanticModel, syntax, type, constantValue, isImplicit)
        End Function

        Private Function CreateBoundFieldInitializerOperation(boundFieldInitializer As BoundFieldInitializer) As IFieldInitializer
            Dim initializedFields As ImmutableArray(Of IFieldSymbol) = ImmutableArray(Of IFieldSymbol).CastUp(boundFieldInitializer.InitializedFields)
            Dim value As Lazy(Of IOperation) = New Lazy(Of IOperation)(Function() Create(boundFieldInitializer.InitialValue))
            Dim kind As OperationKind = OperationKind.FieldInitializer
            Dim syntax As SyntaxNode = boundFieldInitializer.Syntax
            Dim type As ITypeSymbol = Nothing
            Dim constantValue As [Optional](Of Object) = New [Optional](Of Object)()
            Dim isImplicit As Boolean = boundFieldInitializer.WasCompilerGenerated
            Return New LazyFieldInitializer(initializedFields, value, kind, _semanticModel, syntax, type, constantValue, isImplicit)
        End Function

        Private Function CreateBoundPropertyInitializerOperation(boundPropertyInitializer As BoundPropertyInitializer) As IPropertyInitializer
            Dim initializedProperty As IPropertySymbol = boundPropertyInitializer.InitializedProperties.FirstOrDefault()
            Dim value As Lazy(Of IOperation) = New Lazy(Of IOperation)(Function() Create(boundPropertyInitializer.InitialValue))
            Dim kind As OperationKind = OperationKind.PropertyInitializer
            Dim syntax As SyntaxNode = boundPropertyInitializer.Syntax
            Dim type As ITypeSymbol = Nothing
            Dim constantValue As [Optional](Of Object) = New [Optional](Of Object)()
            Dim isImplicit As Boolean = boundPropertyInitializer.WasCompilerGenerated
            Return New LazyPropertyInitializer(initializedProperty, value, kind, _semanticModel, syntax, type, constantValue, isImplicit)
        End Function

        Private Function CreateBoundParameterEqualsValueOperation(boundParameterEqualsValue As BoundParameterEqualsValue) As IParameterInitializer
            Dim parameter As IParameterSymbol = boundParameterEqualsValue.Parameter
            Dim value As Lazy(Of IOperation) = New Lazy(Of IOperation)(Function() Create(boundParameterEqualsValue.Value))
            Dim kind As OperationKind = OperationKind.ParameterInitializer
            Dim syntax As SyntaxNode = boundParameterEqualsValue.Syntax
            Dim type As ITypeSymbol = Nothing
            Dim constantValue As [Optional](Of Object) = New [Optional](Of Object)()
            Dim isImplicit As Boolean = boundParameterEqualsValue.WasCompilerGenerated
            Return New LazyParameterInitializer(parameter, value, kind, _semanticModel, syntax, type, constantValue, isImplicit)
        End Function

        Private Function CreateBoundRValuePlaceholderOperation(boundRValuePlaceholder As BoundRValuePlaceholder) As IPlaceholderExpression
            Dim syntax As SyntaxNode = boundRValuePlaceholder.Syntax
            Dim type As ITypeSymbol = boundRValuePlaceholder.Type
            Dim constantValue As [Optional](Of Object) = ConvertToOptional(boundRValuePlaceholder.ConstantValueOpt)
            Dim isImplicit As Boolean = boundRValuePlaceholder.WasCompilerGenerated
            Return New PlaceholderExpression(_semanticModel, syntax, type, constantValue, isImplicit)
        End Function

        Private Function CreateBoundIfStatementOperation(boundIfStatement As BoundIfStatement) As IIfStatement
            Dim condition As Lazy(Of IOperation) = New Lazy(Of IOperation)(Function() Create(boundIfStatement.Condition))
            Dim ifTrueStatement As Lazy(Of IOperation) = New Lazy(Of IOperation)(Function() Create(boundIfStatement.Consequence))
            Dim ifFalseStatement As Lazy(Of IOperation) = New Lazy(Of IOperation)(Function() Create(boundIfStatement.AlternativeOpt))
            Dim syntax As SyntaxNode = boundIfStatement.Syntax
            Dim type As ITypeSymbol = Nothing
            Dim constantValue As [Optional](Of Object) = New [Optional](Of Object)()
            Dim isImplicit As Boolean = boundIfStatement.WasCompilerGenerated
            Return New LazyIfStatement(condition, ifTrueStatement, ifFalseStatement, _semanticModel, syntax, type, constantValue, isImplicit)
        End Function

        Private Function CreateBoundSelectStatementOperation(boundSelectStatement As BoundSelectStatement) As ISwitchStatement
            Dim value As Lazy(Of IOperation) = New Lazy(Of IOperation)(Function() Create(boundSelectStatement.ExpressionStatement.Expression))
            Dim cases As Lazy(Of ImmutableArray(Of ISwitchCase)) = New Lazy(Of ImmutableArray(Of ISwitchCase))(Function() boundSelectStatement.CaseBlocks.SelectAsArray(Function(n) DirectCast(Create(n), ISwitchCase)))
            Dim syntax As SyntaxNode = boundSelectStatement.Syntax
            Dim type As ITypeSymbol = Nothing
            Dim constantValue As [Optional](Of Object) = New [Optional](Of Object)()
            Dim isImplicit As Boolean = boundSelectStatement.WasCompilerGenerated
            Return New LazySwitchStatement(value, cases, _semanticModel, syntax, type, constantValue, isImplicit)
        End Function

        Private Function CreateBoundCaseBlockOperation(boundCaseBlock As BoundCaseBlock) As ISwitchCase
            Dim clauses As Lazy(Of ImmutableArray(Of ICaseClause)) = New Lazy(Of ImmutableArray(Of ICaseClause))(
                Function()
                    ' `CaseElseClauseSyntax` is bound to `BoundCaseStatement` with an empty list of case clauses,
                    ' so we explicitly create an IOperation node for Case-Else clause to differentiate it from Case clause.
                    Dim caseStatement = boundCaseBlock.CaseStatement
                    If caseStatement.CaseClauses.IsEmpty AndAlso caseStatement.Syntax.Kind() = SyntaxKind.CaseElseStatement Then
                        Return ImmutableArray.Create(Of ICaseClause)(
                            New DefaultCaseClause(
                                _semanticModel,
                                caseStatement.Syntax,
                                type:=Nothing,
                                constantValue:=Nothing,
                                isImplicit:=boundCaseBlock.WasCompilerGenerated))
                    Else
                        Return caseStatement.CaseClauses.SelectAsArray(Function(n) DirectCast(Create(n), ICaseClause))
                    End If
                End Function)
            Dim body As Lazy(Of ImmutableArray(Of IOperation)) = New Lazy(Of ImmutableArray(Of IOperation))(Function() ImmutableArray.Create(Create(boundCaseBlock.Body)))
            Dim syntax As SyntaxNode = boundCaseBlock.Syntax
            Dim type As ITypeSymbol = Nothing
            Dim constantValue As [Optional](Of Object) = New [Optional](Of Object)()
            Dim isImplicit As Boolean = boundCaseBlock.WasCompilerGenerated
            Return New LazySwitchCase(clauses, body, _semanticModel, syntax, type, constantValue, isImplicit)
        End Function

        Private Function CreateBoundSimpleCaseClauseOperation(boundSimpleCaseClause As BoundSimpleCaseClause) As ISingleValueCaseClause
            Dim clauseValue = GetSingleValueCaseClauseValue(boundSimpleCaseClause)
            Dim value As Lazy(Of IOperation) = New Lazy(Of IOperation)(Function() Create(clauseValue))
            Dim syntax As SyntaxNode = boundSimpleCaseClause.Syntax
            Dim type As ITypeSymbol = Nothing
            Dim constantValue As [Optional](Of Object) = New [Optional](Of Object)()
            Dim isImplicit As Boolean = boundSimpleCaseClause.WasCompilerGenerated
            Return New LazySingleValueCaseClause(value, _semanticModel, syntax, type, constantValue, isImplicit)
        End Function

        Private Function CreateBoundRangeCaseClauseOperation(boundRangeCaseClause As BoundRangeCaseClause) As IRangeCaseClause
            Dim minimumValue As Lazy(Of IOperation) = New Lazy(Of IOperation)(
                Function()
                    If boundRangeCaseClause.LowerBoundOpt IsNot Nothing Then
                        Return Create(boundRangeCaseClause.LowerBoundOpt)
                    End If

                    If boundRangeCaseClause.LowerBoundConditionOpt.Kind = BoundKind.BinaryOperator Then
                        Dim lowerBound As BoundBinaryOperator = DirectCast(boundRangeCaseClause.LowerBoundConditionOpt, BoundBinaryOperator)
                        If lowerBound.OperatorKind = VisualBasic.BinaryOperatorKind.GreaterThanOrEqual Then
                            Return Create(lowerBound.Right)
                        End If
                    End If

                    Return Nothing
                End Function)
            Dim maximumValue As Lazy(Of IOperation) = New Lazy(Of IOperation)(
                Function()
                    If boundRangeCaseClause.UpperBoundOpt IsNot Nothing Then
                        Return Create(boundRangeCaseClause.UpperBoundOpt)
                    End If

                    If boundRangeCaseClause.UpperBoundConditionOpt.Kind = BoundKind.BinaryOperator Then
                        Dim upperBound As BoundBinaryOperator = DirectCast(boundRangeCaseClause.UpperBoundConditionOpt, BoundBinaryOperator)
                        If upperBound.OperatorKind = VisualBasic.BinaryOperatorKind.LessThanOrEqual Then
                            Return Create(upperBound.Right)
                        End If
                    End If

                    Return Nothing
                End Function)
            Dim syntax As SyntaxNode = boundRangeCaseClause.Syntax
            Dim type As ITypeSymbol = Nothing
            Dim constantValue As [Optional](Of Object) = New [Optional](Of Object)()
            Dim isImplicit As Boolean = boundRangeCaseClause.WasCompilerGenerated
            Return New LazyRangeCaseClause(minimumValue, maximumValue, _semanticModel, syntax, type, constantValue, isImplicit)
        End Function

        Private Function CreateBoundRelationalCaseClauseOperation(boundRelationalCaseClause As BoundRelationalCaseClause) As IRelationalCaseClause
            Dim valueExpression = GetRelationalCaseClauseValue(boundRelationalCaseClause)
            Dim value As Lazy(Of IOperation) = New Lazy(Of IOperation)(Function() Create(valueExpression))
            Dim relation As BinaryOperatorKind = If(valueExpression IsNot Nothing, Helper.DeriveBinaryOperatorKind(boundRelationalCaseClause.OperatorKind, leftOpt:=Nothing), BinaryOperatorKind.Invalid)
            Dim syntax As SyntaxNode = boundRelationalCaseClause.Syntax
            Dim type As ITypeSymbol = Nothing
            Dim constantValue As [Optional](Of Object) = New [Optional](Of Object)()
            Dim isImplicit As Boolean = boundRelationalCaseClause.WasCompilerGenerated
            Return New LazyRelationalCaseClause(value, relation, _semanticModel, syntax, type, constantValue, isImplicit)
        End Function

        Private Function CreateBoundDoLoopStatementOperation(boundDoLoopStatement As BoundDoLoopStatement) As IDoLoopStatement
            Dim doLoopKind As DoLoopKind = GetDoLoopKind(boundDoLoopStatement)
            Dim condition As Lazy(Of IOperation) = New Lazy(Of IOperation)(Function() Create(boundDoLoopStatement.ConditionOpt))
            Dim body As Lazy(Of IOperation) = New Lazy(Of IOperation)(Function() Create(boundDoLoopStatement.Body))
            Dim ignoredConditionOpt As Lazy(Of IOperation) = New Lazy(Of IOperation)(Function()
                                                                                         If doLoopKind = DoLoopKind.Invalid Then
                                                                                             Debug.Assert(boundDoLoopStatement.TopConditionOpt IsNot Nothing)
                                                                                             Debug.Assert(boundDoLoopStatement.BottomConditionOpt IsNot Nothing)
                                                                                             Debug.Assert(boundDoLoopStatement.ConditionOpt Is boundDoLoopStatement.TopConditionOpt)
                                                                                             Return Create(boundDoLoopStatement.BottomConditionOpt)
                                                                                         Else
                                                                                             Debug.Assert(boundDoLoopStatement.TopConditionOpt Is Nothing OrElse boundDoLoopStatement.BottomConditionOpt Is Nothing)
                                                                                             Return Nothing
                                                                                         End If
                                                                                     End Function)
            Dim locals As ImmutableArray(Of ILocalSymbol) = ImmutableArray(Of ILocalSymbol).Empty
            Dim syntax As SyntaxNode = boundDoLoopStatement.Syntax
            Dim type As ITypeSymbol = Nothing
            Dim constantValue As [Optional](Of Object) = New [Optional](Of Object)()
            Dim isImplicit As Boolean = boundDoLoopStatement.WasCompilerGenerated
            Return New LazyDoLoopStatement(doLoopKind, condition, body, ignoredConditionOpt, locals, _semanticModel, syntax, type, constantValue, isImplicit)
        End Function

        Private Shared Function GetDoLoopKind(boundDoLoopStatement As BoundDoLoopStatement) As DoLoopKind
            If boundDoLoopStatement.TopConditionOpt IsNot Nothing AndAlso boundDoLoopStatement.BottomConditionOpt IsNot Nothing Then
                Return DoLoopKind.Invalid
            End If

            If boundDoLoopStatement.ConditionIsTop Then
                If boundDoLoopStatement.ConditionIsUntil Then
                    Return DoLoopKind.DoUntilTopLoop
                Else
                    Return DoLoopKind.DoWhileTopLoop
                End If
            Else
                If boundDoLoopStatement.ConditionIsUntil Then
                    Return DoLoopKind.DoUntilBottomLoop
                Else
                    Return DoLoopKind.DoWhileBottomLoop
                End If
            End If
        End Function

        Private Function CreateBoundForToStatementOperation(boundForToStatement As BoundForToStatement) As IForToLoopStatement
            Dim locals As ImmutableArray(Of ILocalSymbol) = If(boundForToStatement.DeclaredOrInferredLocalOpt IsNot Nothing,
                ImmutableArray.Create(Of ILocalSymbol)(boundForToStatement.DeclaredOrInferredLocalOpt),
                ImmutableArray(Of ILocalSymbol).Empty)
            Dim loopControlVariable As Lazy(Of IOperation) = New Lazy(Of IOperation)(Function() Create(boundForToStatement.ControlVariable))
            Dim initialValue As Lazy(Of IOperation) = New Lazy(Of IOperation)(Function() Create(boundForToStatement.InitialValue))
            Dim limitValue As Lazy(Of IOperation) = New Lazy(Of IOperation)(Function() Create(boundForToStatement.LimitValue))
            Dim stepValue As Lazy(Of IOperation) = New Lazy(Of IOperation)(Function() Create(boundForToStatement.StepValue))
            Dim body As Lazy(Of IOperation) = New Lazy(Of IOperation)(Function() Create(boundForToStatement.Body))
            Dim nextVariables As Lazy(Of ImmutableArray(Of IOperation)) = New Lazy(Of ImmutableArray(Of IOperation))(
                Function()
                    Return If(boundForToStatement.NextVariablesOpt.IsDefault,
                        ImmutableArray(Of IOperation).Empty,
                        boundForToStatement.NextVariablesOpt.SelectAsArray(Function(n) Create(n)))
                End Function)
            Dim syntax As SyntaxNode = boundForToStatement.Syntax
            Dim type As ITypeSymbol = Nothing
            Dim constantValue As [Optional](Of Object) = New [Optional](Of Object)()
            Dim isImplicit As Boolean = boundForToStatement.WasCompilerGenerated
            Return New LazyForToLoopStatement(locals, loopControlVariable, initialValue, limitValue, stepValue, body, nextVariables, _semanticModel, syntax, type, constantValue, isImplicit)
        End Function

        Private Function CreateBoundForEachStatementOperation(boundForEachStatement As BoundForEachStatement) As IForEachLoopStatement
            Dim locals As ImmutableArray(Of ILocalSymbol) = If(boundForEachStatement.DeclaredOrInferredLocalOpt IsNot Nothing,
                ImmutableArray.Create(Of ILocalSymbol)(boundForEachStatement.DeclaredOrInferredLocalOpt),
                ImmutableArray(Of ILocalSymbol).Empty)
            Dim loopControlVariable As Lazy(Of IOperation) = New Lazy(Of IOperation)(Function() Create(boundForEachStatement.ControlVariable))
            Dim collection As Lazy(Of IOperation) = New Lazy(Of IOperation)(Function() Create(boundForEachStatement.Collection))
            Dim body As Lazy(Of IOperation) = New Lazy(Of IOperation)(Function() Create(boundForEachStatement.Body))
            Dim nextVariables As Lazy(Of ImmutableArray(Of IOperation)) = New Lazy(Of ImmutableArray(Of IOperation))(
                Function()
                    Return If(boundForEachStatement.NextVariablesOpt.IsDefault,
                        ImmutableArray(Of IOperation).Empty,
                        boundForEachStatement.NextVariablesOpt.SelectAsArray(Function(n) Create(n)))
                End Function)
            Dim syntax As SyntaxNode = boundForEachStatement.Syntax
            Dim type As ITypeSymbol = Nothing
            Dim constantValue As [Optional](Of Object) = New [Optional](Of Object)()
            Dim isImplicit As Boolean = boundForEachStatement.WasCompilerGenerated
            Return New LazyForEachLoopStatement(locals, loopControlVariable, collection, nextVariables, body, _semanticModel, syntax, type, constantValue, isImplicit)
        End Function

        Private Function CreateBoundTryStatementOperation(boundTryStatement As BoundTryStatement) As ITryStatement
            Dim body As Lazy(Of IBlockStatement) = New Lazy(Of IBlockStatement)(Function() DirectCast(Create(boundTryStatement.TryBlock), IBlockStatement))
            Dim catches As Lazy(Of ImmutableArray(Of ICatchClause)) = New Lazy(Of ImmutableArray(Of ICatchClause))(Function() boundTryStatement.CatchBlocks.SelectAsArray(Function(n) DirectCast(Create(n), ICatchClause)))
            Dim finallyHandler As Lazy(Of IBlockStatement) = New Lazy(Of IBlockStatement)(Function() DirectCast(Create(boundTryStatement.FinallyBlockOpt), IBlockStatement))
            Dim syntax As SyntaxNode = boundTryStatement.Syntax
            Dim type As ITypeSymbol = Nothing
            Dim constantValue As [Optional](Of Object) = New [Optional](Of Object)()
            Dim isImplicit As Boolean = boundTryStatement.WasCompilerGenerated
            Return New LazyTryStatement(body, catches, finallyHandler, _semanticModel, syntax, type, constantValue, isImplicit)
        End Function

        Private Function CreateBoundCatchBlockOperation(boundCatchBlock As BoundCatchBlock) As ICatchClause
            Dim handler As Lazy(Of IBlockStatement) = New Lazy(Of IBlockStatement)(Function() DirectCast(Create(boundCatchBlock.Body), IBlockStatement))
            Dim caughtType As ITypeSymbol = boundCatchBlock.ExceptionSourceOpt?.Type
            Dim filter As Lazy(Of IOperation) = New Lazy(Of IOperation)(Function() Create(boundCatchBlock.ExceptionFilterOpt))
            Dim exceptionLocal As ILocalSymbol = boundCatchBlock.LocalOpt
            Dim syntax As SyntaxNode = boundCatchBlock.Syntax
            Dim type As ITypeSymbol = Nothing
            Dim constantValue As [Optional](Of Object) = New [Optional](Of Object)()
            Dim isImplicit As Boolean = boundCatchBlock.WasCompilerGenerated
            Return New LazyCatchClause(handler, caughtType, filter, exceptionLocal, _semanticModel, syntax, type, constantValue, isImplicit)
        End Function

        Private Function CreateBoundBlockOperation(boundBlock As BoundBlock) As IBlockStatement
            Dim statements As Lazy(Of ImmutableArray(Of IOperation)) = New Lazy(Of ImmutableArray(Of IOperation))(
                Function()
                    ' We should not be filtering OperationKind.None statements.
                    ' https://github.com/dotnet/roslyn/issues/21776
                    Return boundBlock.Statements.Select(Function(n) (s:=Create(n), bound:=n)).Where(
                        Function(tuple)
                            Return tuple.s.Kind <> OperationKind.None OrElse tuple.bound.Kind = BoundKind.TryStatement OrElse
                                tuple.bound.Kind = BoundKind.WithStatement OrElse tuple.bound.Kind = BoundKind.StopStatement OrElse
                                tuple.bound.Kind = BoundKind.EndStatement
                        End Function).Select(Function(tuple) tuple.s).ToImmutableArray()
                End Function)
            Dim locals As ImmutableArray(Of ILocalSymbol) = boundBlock.Locals.As(Of ILocalSymbol)()
            Dim syntax As SyntaxNode = boundBlock.Syntax
            Dim type As ITypeSymbol = Nothing
            Dim constantValue As [Optional](Of Object) = New [Optional](Of Object)()
            Dim isImplicit As Boolean = boundBlock.WasCompilerGenerated
            Return New LazyBlockStatement(statements, locals, _semanticModel, syntax, type, constantValue, isImplicit)
        End Function

        Private Function CreateBoundBadStatementOperation(boundBadStatement As BoundBadStatement) As IInvalidStatement
            Dim children As Lazy(Of ImmutableArray(Of IOperation)) = New Lazy(Of ImmutableArray(Of IOperation))(
                Function()
                    Dim builder As ArrayBuilder(Of IOperation) = ArrayBuilder(Of IOperation).GetInstance(boundBadStatement.ChildBoundNodes.Length)
                    For Each childNode In boundBadStatement.ChildBoundNodes
                        Dim operation = Create(childNode)
                        If operation IsNot Nothing Then
                            builder.Add(operation)
                        End If
                    Next

                    Return builder.ToImmutableAndFree()
                End Function)
            Dim syntax As SyntaxNode = boundBadStatement.Syntax
            Dim type As ITypeSymbol = Nothing
            Dim constantValue As [Optional](Of Object) = New [Optional](Of Object)()

            ' if child has syntax node point to same syntax node as bad statement, then this invalid statement is implicit
            Dim isImplicit = boundBadStatement.WasCompilerGenerated OrElse boundBadStatement.ChildBoundNodes.Any(Function(e) e?.Syntax Is boundBadStatement.Syntax)
            Return New LazyInvalidStatement(children, _semanticModel, syntax, type, constantValue, isImplicit)
        End Function

        Private Function CreateBoundReturnStatementOperation(boundReturnStatement As BoundReturnStatement) As IReturnStatement
            Dim returnedValue As Lazy(Of IOperation) = New Lazy(Of IOperation)(Function() Create(boundReturnStatement.ExpressionOpt))
            Dim syntax As SyntaxNode = boundReturnStatement.Syntax
            Dim type As ITypeSymbol = Nothing
            Dim constantValue As [Optional](Of Object) = New [Optional](Of Object)()
            Dim isImplicit As Boolean = boundReturnStatement.WasCompilerGenerated
            Return New LazyReturnStatement(OperationKind.ReturnStatement, returnedValue, _semanticModel, syntax, type, constantValue, isImplicit)
        End Function

        Private Function CreateBoundThrowStatementOperation(boundThrowStatement As BoundThrowStatement) As IExpressionStatement
            Dim thrownObject As Lazy(Of IOperation) = New Lazy(Of IOperation)(Function() Create(boundThrowStatement.ExpressionOpt))
            Dim syntax As SyntaxNode = boundThrowStatement.Syntax
            Dim expressionType As ITypeSymbol = boundThrowStatement.ExpressionOpt?.Type
            Dim statementType As ITypeSymbol = Nothing
            Dim constantValue As [Optional](Of Object) = New [Optional](Of Object)()
            Dim isImplicit As Boolean = boundThrowStatement.WasCompilerGenerated
            Dim throwExpression As IOperation = New LazyThrowExpression(thrownObject, _semanticModel, syntax, expressionType, constantValue, isImplicit)
            Return New ExpressionStatement(throwExpression, _semanticModel, syntax, statementType, constantValue, isImplicit)
        End Function

        Private Function CreateBoundWhileStatementOperation(boundWhileStatement As BoundWhileStatement) As IWhileLoopStatement
            Dim condition As Lazy(Of IOperation) = New Lazy(Of IOperation)(Function() Create(boundWhileStatement.Condition))
            Dim body As Lazy(Of IOperation) = New Lazy(Of IOperation)(Function() Create(boundWhileStatement.Body))
            Dim locals As ImmutableArray(Of ILocalSymbol) = ImmutableArray(Of ILocalSymbol).Empty
            Dim syntax As SyntaxNode = boundWhileStatement.Syntax
            Dim type As ITypeSymbol = Nothing
            Dim constantValue As [Optional](Of Object) = New [Optional](Of Object)()
            Dim isImplicit As Boolean = boundWhileStatement.WasCompilerGenerated
            Return New LazyWhileLoopStatement(condition, body, locals, _semanticModel, syntax, type, constantValue, isImplicit)
        End Function

        Private Function CreateBoundDimStatementOperation(boundDimStatement As BoundDimStatement) As IVariableDeclarationStatement
            Dim declarations As Lazy(Of ImmutableArray(Of IVariableDeclaration)) = New Lazy(Of ImmutableArray(Of IVariableDeclaration))(Function() GetVariableDeclarationStatementVariables(boundDimStatement))
            Dim syntax As SyntaxNode = boundDimStatement.Syntax
            Dim type As ITypeSymbol = Nothing
            Dim constantValue As [Optional](Of Object) = New [Optional](Of Object)()
            Dim isImplicit As Boolean = boundDimStatement.WasCompilerGenerated
            Return New LazyVariableDeclarationStatement(declarations, _semanticModel, syntax, type, constantValue, isImplicit)
        End Function

        Private Function CreateBoundYieldStatementOperation(boundYieldStatement As BoundYieldStatement) As IReturnStatement
            Dim returnedValue As Lazy(Of IOperation) = New Lazy(Of IOperation)(Function() Create(boundYieldStatement.Expression))
            Dim syntax As SyntaxNode = boundYieldStatement.Syntax
            Dim type As ITypeSymbol = Nothing
            Dim constantValue As [Optional](Of Object) = New [Optional](Of Object)()
            Dim isImplicit As Boolean = boundYieldStatement.WasCompilerGenerated
            Return New LazyReturnStatement(OperationKind.YieldReturnStatement, returnedValue, _semanticModel, syntax, type, constantValue, isImplicit)
        End Function

        Private Function CreateBoundLabelStatementOperation(boundLabelStatement As BoundLabelStatement) As ILabeledStatement
            Dim label As ILabelSymbol = boundLabelStatement.Label
            Dim statement As Lazy(Of IOperation) = New Lazy(Of IOperation)(Function() Nothing)
            Dim syntax As SyntaxNode = boundLabelStatement.Syntax
            Dim type As ITypeSymbol = Nothing
            Dim constantValue As [Optional](Of Object) = New [Optional](Of Object)()
            Dim isImplicit As Boolean = boundLabelStatement.WasCompilerGenerated
            Return New LazyLabeledStatement(label, statement, _semanticModel, syntax, type, constantValue, isImplicit)
        End Function

        Private Function CreateBoundGotoStatementOperation(boundGotoStatement As BoundGotoStatement) As IBranchStatement
            Dim target As ILabelSymbol = boundGotoStatement.Label
            Dim branchKind As BranchKind = BranchKind.GoTo
            Dim syntax As SyntaxNode = boundGotoStatement.Syntax
            Dim type As ITypeSymbol = Nothing
            Dim constantValue As [Optional](Of Object) = New [Optional](Of Object)()
            Dim isImplicit As Boolean = boundGotoStatement.WasCompilerGenerated
            Return New BranchStatement(target, branchKind, _semanticModel, syntax, type, constantValue, isImplicit)
        End Function

        Private Function CreateBoundContinueStatementOperation(boundContinueStatement As BoundContinueStatement) As IBranchStatement
            Dim target As ILabelSymbol = boundContinueStatement.Label
            Dim branchKind As BranchKind = BranchKind.Continue
            Dim syntax As SyntaxNode = boundContinueStatement.Syntax
            Dim type As ITypeSymbol = Nothing
            Dim constantValue As [Optional](Of Object) = New [Optional](Of Object)()
            Dim isImplicit As Boolean = boundContinueStatement.WasCompilerGenerated
            Return New BranchStatement(target, branchKind, _semanticModel, syntax, type, constantValue, isImplicit)
        End Function

        Private Function CreateBoundExitStatementOperation(boundExitStatement As BoundExitStatement) As IBranchStatement
            Dim target As ILabelSymbol = boundExitStatement.Label
            Dim branchKind As BranchKind = BranchKind.Break
            Dim syntax As SyntaxNode = boundExitStatement.Syntax
            Dim type As ITypeSymbol = Nothing
            Dim constantValue As [Optional](Of Object) = New [Optional](Of Object)()
            Dim isImplicit As Boolean = boundExitStatement.WasCompilerGenerated
            Return New BranchStatement(target, branchKind, _semanticModel, syntax, type, constantValue, isImplicit)
        End Function

        Private Function CreateBoundSyncLockStatementOperation(boundSyncLockStatement As BoundSyncLockStatement) As ILockStatement
            Dim expression As Lazy(Of IOperation) = New Lazy(Of IOperation)(Function() Create(boundSyncLockStatement.LockExpression))
            Dim body As Lazy(Of IOperation) = New Lazy(Of IOperation)(Function() Create(boundSyncLockStatement.Body))
            Dim syntax As SyntaxNode = boundSyncLockStatement.Syntax
            Dim type As ITypeSymbol = Nothing
            Dim constantValue As [Optional](Of Object) = New [Optional](Of Object)()
            Dim isImplicit As Boolean = boundSyncLockStatement.WasCompilerGenerated
            Return New LazyLockStatement(expression, body, _semanticModel, syntax, type, constantValue, isImplicit)
        End Function

        Private Function CreateBoundNoOpStatementOperation(boundNoOpStatement As BoundNoOpStatement) As IEmptyStatement
            Dim syntax As SyntaxNode = boundNoOpStatement.Syntax
            Dim type As ITypeSymbol = Nothing
            Dim constantValue As [Optional](Of Object) = New [Optional](Of Object)()
            Dim isImplicit As Boolean = boundNoOpStatement.WasCompilerGenerated
            Return New EmptyStatement(_semanticModel, syntax, type, constantValue, isImplicit)
        End Function

        Private Function CreateBoundStopStatementOperation(boundStopStatement As BoundStopStatement) As IStopStatement
            Dim syntax As SyntaxNode = boundStopStatement.Syntax
            Dim type As ITypeSymbol = Nothing
            Dim constantValue As [Optional](Of Object) = New [Optional](Of Object)()
            Dim isImplicit As Boolean = boundStopStatement.WasCompilerGenerated
            Return New StopStatement(_semanticModel, syntax, type, constantValue, isImplicit)
        End Function

        Private Function CreateBoundEndStatementOperation(boundEndStatement As BoundEndStatement) As IEndStatement
            Dim syntax As SyntaxNode = boundEndStatement.Syntax
            Dim type As ITypeSymbol = Nothing
            Dim constantValue As [Optional](Of Object) = New [Optional](Of Object)()
            Dim isImplicit As Boolean = boundEndStatement.WasCompilerGenerated
            Return New EndStatement(_semanticModel, syntax, type, constantValue, isImplicit)
        End Function

        Private Function CreateBoundWithStatementOperation(boundWithStatement As BoundWithStatement) As IWithStatement
            Dim body As Lazy(Of IOperation) = New Lazy(Of IOperation)(Function() Create(boundWithStatement.Body))
            Dim value As Lazy(Of IOperation) = New Lazy(Of IOperation)(Function() Create(boundWithStatement.OriginalExpression))
            Dim syntax As SyntaxNode = boundWithStatement.Syntax
            Dim type As ITypeSymbol = Nothing
            Dim constantValue As [Optional](Of Object) = New [Optional](Of Object)()
            Dim isImplicit As Boolean = boundWithStatement.WasCompilerGenerated
            Return New LazyWithStatement(body, value, _semanticModel, syntax, type, constantValue, isImplicit)
        End Function

        Private Function CreateBoundUsingStatementOperation(boundUsingStatement As BoundUsingStatement) As IUsingStatement
            Dim body As Lazy(Of IOperation) = New Lazy(Of IOperation)(Function() Create(boundUsingStatement.Body))
            Dim declaration As Lazy(Of IVariableDeclarationStatement) = New Lazy(Of IVariableDeclarationStatement)(
                Function()
                    Return GetUsingStatementDeclaration(boundUsingStatement.ResourceList, DirectCast(boundUsingStatement.Syntax, UsingBlockSyntax).UsingStatement)
                End Function)
            Dim value As Lazy(Of IOperation) = New Lazy(Of IOperation)(Function() Create(boundUsingStatement.ResourceExpressionOpt))
            Dim syntax As SyntaxNode = boundUsingStatement.Syntax
            Dim type As ITypeSymbol = Nothing
            Dim constantValue As [Optional](Of Object) = New [Optional](Of Object)()
            Dim isImplicit As Boolean = boundUsingStatement.WasCompilerGenerated
            Return New LazyUsingStatement(body, declaration, value, _semanticModel, syntax, type, constantValue, isImplicit)
        End Function

        Private Function CreateBoundExpressionStatementOperation(boundExpressionStatement As BoundExpressionStatement) As IExpressionStatement
            Dim expression As Lazy(Of IOperation) = New Lazy(Of IOperation)(Function() Create(boundExpressionStatement.Expression))
            Dim syntax As SyntaxNode = boundExpressionStatement.Syntax
            Dim type As ITypeSymbol = Nothing
            Dim constantValue As [Optional](Of Object) = New [Optional](Of Object)()
            Dim isImplicit As Boolean = boundExpressionStatement.WasCompilerGenerated
            Return New LazyExpressionStatement(expression, _semanticModel, syntax, type, constantValue, isImplicit)
        End Function

        Private Function CreateBoundRaiseEventStatementOperation(boundRaiseEventStatement As BoundRaiseEventStatement) As IExpressionStatement
            Dim expression As Lazy(Of IOperation) = New Lazy(Of IOperation)(Function() Create(boundRaiseEventStatement.EventInvocation))
            Dim syntax As SyntaxNode = boundRaiseEventStatement.Syntax
            Dim type As ITypeSymbol = Nothing
            Dim constantValue As [Optional](Of Object) = New [Optional](Of Object)()
            Dim isImplicit As Boolean = boundRaiseEventStatement.WasCompilerGenerated
            Return New LazyExpressionStatement(expression, _semanticModel, syntax, type, constantValue, isImplicit)
        End Function

        Private Function CreateBoundAddHandlerStatementOperation(boundAddHandlerStatement As BoundAddHandlerStatement) As IExpressionStatement
            Dim expression As Lazy(Of IOperation) = New Lazy(Of IOperation)(Function() GetAddRemoveHandlerStatementExpression(boundAddHandlerStatement))
            Dim syntax As SyntaxNode = boundAddHandlerStatement.Syntax
            Dim type As ITypeSymbol = Nothing
            Dim constantValue As [Optional](Of Object) = New [Optional](Of Object)()
            Dim isImplicit As Boolean = boundAddHandlerStatement.WasCompilerGenerated
            Return New LazyExpressionStatement(expression, _semanticModel, syntax, type, constantValue, isImplicit)
        End Function

        Private Function CreateBoundRemoveHandlerStatementOperation(boundRemoveHandlerStatement As BoundRemoveHandlerStatement) As IExpressionStatement
            Dim expression As Lazy(Of IOperation) = New Lazy(Of IOperation)(Function() GetAddRemoveHandlerStatementExpression(boundRemoveHandlerStatement))
            Dim syntax As SyntaxNode = boundRemoveHandlerStatement.Syntax
            Dim type As ITypeSymbol = Nothing
            Dim constantValue As [Optional](Of Object) = New [Optional](Of Object)()
            Dim isImplicit As Boolean = boundRemoveHandlerStatement.WasCompilerGenerated
            Return New LazyExpressionStatement(expression, _semanticModel, syntax, type, constantValue, isImplicit)
        End Function

        Private Function CreateBoundTupleExpressionOperation(boundTupleExpression As BoundTupleExpression) As ITupleExpression
            Dim elements As New Lazy(Of ImmutableArray(Of IOperation))(Function() boundTupleExpression.Arguments.SelectAsArray(Function(element) Create(element)))
            Dim syntax As SyntaxNode = boundTupleExpression.Syntax
            Dim type As ITypeSymbol = boundTupleExpression.Type
            Dim constantValue As [Optional](Of Object) = Nothing
            Dim isImplicit As Boolean = boundTupleExpression.WasCompilerGenerated
            Return New LazyTupleExpression(elements, _semanticModel, syntax, type, constantValue, isImplicit)
        End Function

        Private Function CreateBoundInterpolatedStringExpressionOperation(boundInterpolatedString As BoundInterpolatedStringExpression) As IInterpolatedStringExpression
            Dim parts As New Lazy(Of ImmutableArray(Of IInterpolatedStringContent))(
                Function()
                    Return boundInterpolatedString.Contents.SelectAsArray(Function(interpolatedStringContent) CreateBoundInterpolatedStringContentOperation(interpolatedStringContent))
                End Function)

            Dim syntax As SyntaxNode = boundInterpolatedString.Syntax
            Dim type As ITypeSymbol = boundInterpolatedString.Type
            Dim constantValue As [Optional](Of Object) = ConvertToOptional(boundInterpolatedString.ConstantValueOpt)
            Dim isImplicit As Boolean = boundInterpolatedString.WasCompilerGenerated
            Return New LazyInterpolatedStringExpression(parts, _semanticModel, syntax, type, constantValue, isImplicit)
        End Function

        Private Function CreateBoundInterpolatedStringContentOperation(boundNode As BoundNode) As IInterpolatedStringContent
            If boundNode.Kind = BoundKind.Interpolation Then
                Return DirectCast(Create(boundNode), IInterpolatedStringContent)
            Else
                Return CreateBoundInterpolatedStringTextOperation(boundNode)
            End If
        End Function

        Private Function CreateBoundInterpolationOperation(boundInterpolation As BoundInterpolation) As IInterpolation
            Dim expression As Lazy(Of IOperation) = New Lazy(Of IOperation)(Function() Create(boundInterpolation.Expression))
            Dim alignment As Lazy(Of IOperation) = New Lazy(Of IOperation)(Function() Create(boundInterpolation.AlignmentOpt))
            Dim format As Lazy(Of IOperation) = New Lazy(Of IOperation)(Function() Create(boundInterpolation.FormatStringOpt))
            Dim syntax As SyntaxNode = boundInterpolation.Syntax
            Dim type As ITypeSymbol = Nothing
            Dim constantValue As [Optional](Of Object) = Nothing
            Dim isImplicit As Boolean = boundInterpolation.WasCompilerGenerated
            Return New LazyInterpolation(expression, alignment, format, _semanticModel, syntax, type, constantValue, isImplicit)
        End Function

        Private Function CreateBoundInterpolatedStringTextOperation(boundNode As BoundNode) As IInterpolatedStringText
            Dim text As Lazy(Of IOperation) = New Lazy(Of IOperation)(Function() Create(boundNode))
            Dim syntax As SyntaxNode = boundNode.Syntax
            Dim type As ITypeSymbol = Nothing
            Dim constantValue As [Optional](Of Object) = Nothing
            Dim isImplicit As Boolean = boundNode.WasCompilerGenerated
            Return New LazyInterpolatedStringText(text, _semanticModel, syntax, type, constantValue, isImplicit)
        End Function

        Private Function CreateBoundAnonymousTypeCreationExpressionOperation(boundAnonymousTypeCreationExpression As BoundAnonymousTypeCreationExpression) As IAnonymousObjectCreationExpression
            Dim initializers As Lazy(Of ImmutableArray(Of IOperation)) = New Lazy(Of ImmutableArray(Of IOperation))(
                Function()
                    Return GetAnonymousTypeCreationInitializers(boundAnonymousTypeCreationExpression)
                End Function)

            Dim syntax As SyntaxNode = boundAnonymousTypeCreationExpression.Syntax
            Dim type As ITypeSymbol = boundAnonymousTypeCreationExpression.Type
            Dim constantValue As [Optional](Of Object) = ConvertToOptional(boundAnonymousTypeCreationExpression.ConstantValueOpt)
            Dim isImplicit As Boolean = boundAnonymousTypeCreationExpression.WasCompilerGenerated
            Return New LazyAnonymousObjectCreationExpression(initializers, _semanticModel, syntax, type, constantValue, isImplicit)
        End Function

        Private Function CreateBoundAnonymousTypePropertyAccessOperation(boundAnonymousTypePropertyAccess As BoundAnonymousTypePropertyAccess) As IPropertyReferenceExpression
            Dim instance As Lazy(Of IOperation) = New Lazy(Of IOperation)(Function() Nothing)
            Dim [property] As IPropertySymbol = DirectCast(boundAnonymousTypePropertyAccess.ExpressionSymbol, IPropertySymbol)
            Dim argumentsInEvaluationOrder As Lazy(Of ImmutableArray(Of IArgument)) = New Lazy(Of ImmutableArray(Of IArgument))(Function() ImmutableArray(Of IArgument).Empty)
            Dim syntax As SyntaxNode = boundAnonymousTypePropertyAccess.Syntax
            Dim type As ITypeSymbol = boundAnonymousTypePropertyAccess.Type
            Dim constantValue As [Optional](Of Object) = ConvertToOptional(boundAnonymousTypePropertyAccess.ConstantValueOpt)
            Dim isImplicit As Boolean = boundAnonymousTypePropertyAccess.WasCompilerGenerated
            Return New LazyPropertyReferenceExpression([property], instance, [property], argumentsInEvaluationOrder, _semanticModel, syntax, type, constantValue, isImplicit)
        End Function

        Private Function CreateBoundQueryExpressionOperation(boundQueryExpression As BoundQueryExpression) As IOperation
            Dim expression As Lazy(Of IOperation) = New Lazy(Of IOperation)(Function() Create(boundQueryExpression.LastOperator))
            Dim syntax As SyntaxNode = boundQueryExpression.Syntax
            Dim type As ITypeSymbol = boundQueryExpression.Type
            Dim constantValue As [Optional](Of Object) = ConvertToOptional(boundQueryExpression.ConstantValueOpt)
            Dim isImplicit As Boolean = boundQueryExpression.WasCompilerGenerated
            Return New LazyTranslatedQueryExpression(expression, _semanticModel, syntax, type, constantValue, isImplicit)
        End Function
    End Class
End Namespace


