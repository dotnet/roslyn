' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Concurrent
Imports System.Collections.Immutable
Imports Microsoft.CodeAnalysis.PooledObjects
Imports Microsoft.CodeAnalysis.VisualBasic
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.Operations
    Partial Friend NotInheritable Class VisualBasicOperationFactory

        Private ReadOnly _nodeMap As ConcurrentDictionary(Of BoundNode, IOperation) =
            New ConcurrentDictionary(Of BoundNode, IOperation)(concurrencyLevel:=2, capacity:=10)

        Private _lazyPlaceholderToParentMap As ConcurrentDictionary(Of BoundValuePlaceholderBase, BoundNode) = Nothing

        Private ReadOnly _cachedCreateInternal As Func(Of BoundNode, IOperation)

        Private ReadOnly _semanticModel As SemanticModel

        Public Sub New(semanticModel As SemanticModel)
            _semanticModel = semanticModel
            _cachedCreateInternal = AddressOf CreateInternal
        End Sub

        Private Function Clone() As VisualBasicOperationFactory
            Dim factory As New VisualBasicOperationFactory(_semanticModel)
            factory._lazyPlaceholderToParentMap = _lazyPlaceholderToParentMap
            Return factory
        End Function

        ''' <summary>
        ''' Returns <code>Nothing</code> if parent is not known.
        ''' </summary>
        Private Function TryGetParent(placeholder As BoundValuePlaceholderBase) As BoundNode
            Dim knownParent As BoundNode = Nothing

            If _lazyPlaceholderToParentMap IsNot Nothing AndAlso
               _lazyPlaceholderToParentMap.TryGetValue(placeholder, knownParent) Then
                Return knownParent
            End If

            Return Nothing
        End Function

        Private Sub RecordParent(placeholderOpt As BoundValuePlaceholderBase, parent As BoundNode)
            Debug.Assert(parent IsNot Nothing)

            If placeholderOpt Is Nothing Then
                Return
            End If

            If _lazyPlaceholderToParentMap Is Nothing Then
                Threading.Interlocked.CompareExchange(_lazyPlaceholderToParentMap,
                                                      New ConcurrentDictionary(Of BoundValuePlaceholderBase, BoundNode)(concurrencyLevel:=2, capacity:=10, comparer:=ReferenceEqualityComparer.Instance),
                                                      Nothing)
            End If

            Dim knownParent = _lazyPlaceholderToParentMap.GetOrAdd(placeholderOpt, parent)
            Debug.Assert(knownParent Is parent)
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
                Return OperationCloner.CloneOperation(CreateInternal(boundNode))
            End If

            ' A BoundUserDefined conversion is always the operand of a BoundConversion, and is handled
            ' by the BoundConversion creation. We should never receive one in this top level create call.
            Debug.Assert(boundNode.Kind <> BoundKind.UserDefinedConversion)

            Return _nodeMap.GetOrAdd(boundNode, _cachedCreateInternal)
        End Function

        Public Function CreateFromArray(Of TBoundNode As BoundNode, TOperation As {Class, IOperation})(nodeArray As ImmutableArray(Of TBoundNode)) As ImmutableArray(Of TOperation)
            Dim builder = ArrayBuilder(Of TOperation).GetInstance(nodeArray.Length)
            For Each node In nodeArray
                builder.AddIfNotNull(DirectCast(Create(node), TOperation))
            Next

            Return builder.ToImmutableAndFree()
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
                Case BoundKind.NoPiaObjectCreationExpression
                    Return CreateNoPiaObjectCreationExpressionOperation(DirectCast(boundNode, BoundNoPiaObjectCreationExpression))
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
                Case BoundKind.LocalDeclaration
                    Return CreateBoundLocalDeclarationOperation(DirectCast(boundNode, BoundLocalDeclaration))
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
                Case BoundKind.TupleLiteral
                    Return CreateBoundTupleLiteralOperation(DirectCast(boundNode, BoundTupleLiteral))
                Case BoundKind.ConvertedTupleLiteral
                    Return CreateBoundConvertedTupleLiteralOperation(DirectCast(boundNode, BoundConvertedTupleLiteral))
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
                Case BoundKind.WithLValueExpressionPlaceholder
                    Return CreateBoundWithLValueExpressionPlaceholder(DirectCast(boundNode, BoundWithLValueExpressionPlaceholder))
                Case BoundKind.WithRValueExpressionPlaceholder
                    Return CreateBoundWithRValueExpressionPlaceholder(DirectCast(boundNode, BoundWithRValueExpressionPlaceholder))
                Case BoundKind.QueryExpression
                    Return CreateBoundQueryExpressionOperation(DirectCast(boundNode, BoundQueryExpression))
                Case BoundKind.LValueToRValueWrapper
                    Return CreateBoundLValueToRValueWrapper(DirectCast(boundNode, BoundLValueToRValueWrapper))
                Case BoundKind.QueryClause
                    ' Query clause has no special representation in the IOperation tree
                    Return Create(DirectCast(boundNode, BoundQueryClause).UnderlyingExpression)
                Case BoundKind.QueryableSource
                    ' Queryable source has no special representation in the IOperation tree
                    Return Create(DirectCast(boundNode, BoundQueryableSource).Source)
                Case BoundKind.AggregateClause
                    Return CreateBoundAggregateClauseOperation(DirectCast(boundNode, BoundAggregateClause))
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
                Case BoundKind.BadVariable
                    Return Create(DirectCast(boundNode, BoundBadVariable).Expression)
                Case BoundKind.NullableIsTrueOperator
                    Return CreateBoundNullableIsTrueOperator(DirectCast(boundNode, BoundNullableIsTrueOperator))
                Case BoundKind.RedimStatement
                    Return CreateBoundReDimOperation(DirectCast(boundNode, BoundRedimStatement))
                Case BoundKind.RedimClause
                    Return CreateBoundReDimClauseOperation(DirectCast(boundNode, BoundRedimClause))

                Case BoundKind.AddressOfOperator,
                     BoundKind.ArrayLiteral,
                     BoundKind.Attribute,
                     BoundKind.ByRefArgumentWithCopyBack,
                     BoundKind.CompoundAssignmentTargetPlaceholder,
                     BoundKind.EraseStatement,
                     BoundKind.Label,
                     BoundKind.LateAddressOfOperator,
                     BoundKind.MethodGroup,
                     BoundKind.MidResult,
                     BoundKind.NamespaceExpression,
                     BoundKind.OnErrorStatement,
                     BoundKind.PropertyGroup,
                     BoundKind.RangeVariable,
                     BoundKind.ResumeStatement,
                     BoundKind.TypeAsValueExpression,
                     BoundKind.TypeExpression,
                     BoundKind.TypeOrValueExpression,
                     BoundKind.XmlCData,
                     BoundKind.XmlComment,
                     BoundKind.XmlDocument,
                     BoundKind.XmlElement,
                     BoundKind.XmlEmbeddedExpression,
                     BoundKind.XmlMemberAccess,
                     BoundKind.XmlNamespace,
                     BoundKind.XmlProcessingInstruction,
                     BoundKind.UnboundLambda,
                     BoundKind.UnstructuredExceptionHandlingStatement

                    Dim constantValue = ConvertToOptional(TryCast(boundNode, BoundExpression)?.ConstantValueOpt)
                    Dim isImplicit As Boolean = boundNode.WasCompilerGenerated
                    Return New VisualBasicLazyNoneOperation(Me, boundNode, _semanticModel, boundNode.Syntax, constantValue, isImplicit)

                Case Else
                    Throw ExceptionUtilities.UnexpectedValue(boundNode.Kind)
            End Select
        End Function

        Friend Function GetIOperationChildren(boundNode As BoundNode) As ImmutableArray(Of IOperation)
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
            If IsMidStatement(boundAssignmentOperator.Right) Then
                ' We don't support mid statements currently. Return a none operation for them
                ' https://github.com/dotnet/roslyn/issues/23109
                Dim constantValue As [Optional](Of Object) = ConvertToOptional(boundAssignmentOperator.ConstantValueOpt)
                Dim isImplicit As Boolean = boundAssignmentOperator.WasCompilerGenerated
                Return New VisualBasicLazyNoneOperation(Me, boundAssignmentOperator, _semanticModel, boundAssignmentOperator.Syntax, constantValue, isImplicit)
            ElseIf boundAssignmentOperator.LeftOnTheRightOpt IsNot Nothing Then
                Return CreateCompoundAssignment(boundAssignmentOperator)
            Else
                Dim isImplicit As Boolean = boundAssignmentOperator.WasCompilerGenerated
                Dim isRef As Boolean = False
                Dim syntax As SyntaxNode = boundAssignmentOperator.Syntax
                Dim type As ITypeSymbol = boundAssignmentOperator.Type
                Dim constantValue As [Optional](Of Object) = ConvertToOptional(boundAssignmentOperator.ConstantValueOpt)
                Return New VisualBasicLazySimpleAssignmentOperation(Me, boundAssignmentOperator, isRef, _semanticModel, syntax, type, constantValue, isImplicit)
            End If
        End Function

        Private Function CreateBoundMeReferenceOperation(boundMeReference As BoundMeReference) As IInstanceReferenceOperation
            Dim referenceKind As InstanceReferenceKind = InstanceReferenceKind.ContainingTypeInstance
            Dim syntax As SyntaxNode = boundMeReference.Syntax
            Dim type As ITypeSymbol = boundMeReference.Type
            Dim constantValue As [Optional](Of Object) = ConvertToOptional(boundMeReference.ConstantValueOpt)
            Dim isImplicit As Boolean = boundMeReference.WasCompilerGenerated
            Return New InstanceReferenceOperation(referenceKind, _semanticModel, syntax, type, constantValue, isImplicit)
        End Function

        Private Function CreateBoundMyBaseReferenceOperation(boundMyBaseReference As BoundMyBaseReference) As IInstanceReferenceOperation
            Dim referenceKind As InstanceReferenceKind = InstanceReferenceKind.ContainingTypeInstance
            Dim syntax As SyntaxNode = boundMyBaseReference.Syntax
            Dim type As ITypeSymbol = boundMyBaseReference.Type
            Dim constantValue As [Optional](Of Object) = ConvertToOptional(boundMyBaseReference.ConstantValueOpt)
            Dim isImplicit As Boolean = boundMyBaseReference.WasCompilerGenerated
            Return New InstanceReferenceOperation(referenceKind, _semanticModel, syntax, type, constantValue, isImplicit)
        End Function

        Private Function CreateBoundMyClassReferenceOperation(boundMyClassReference As BoundMyClassReference) As IInstanceReferenceOperation
            Dim referenceKind As InstanceReferenceKind = InstanceReferenceKind.ContainingTypeInstance
            Dim syntax As SyntaxNode = boundMyClassReference.Syntax
            Dim type As ITypeSymbol = boundMyClassReference.Type
            Dim constantValue As [Optional](Of Object) = ConvertToOptional(boundMyClassReference.ConstantValueOpt)
            Dim isImplicit As Boolean = boundMyClassReference.WasCompilerGenerated
            Return New InstanceReferenceOperation(referenceKind, _semanticModel, syntax, type, constantValue, isImplicit)
        End Function

        Friend Function CreateBoundLiteralOperation(boundLiteral As BoundLiteral, Optional implicit As Boolean = False) As ILiteralOperation
            Dim syntax As SyntaxNode = boundLiteral.Syntax
            Dim type As ITypeSymbol = boundLiteral.Type
            Dim constantValue As [Optional](Of Object) = ConvertToOptional(boundLiteral.ConstantValueOpt)
            Dim isImplicit As Boolean = boundLiteral.WasCompilerGenerated OrElse implicit
            Return New LiteralOperation(_semanticModel, syntax, type, constantValue, isImplicit)
        End Function

        Private Function CreateBoundAwaitOperatorOperation(boundAwaitOperator As BoundAwaitOperator) As IAwaitOperation
            Dim awaitedValue As BoundNode = boundAwaitOperator.Operand
            Dim syntax As SyntaxNode = boundAwaitOperator.Syntax
            Dim type As ITypeSymbol = boundAwaitOperator.Type
            Dim constantValue As [Optional](Of Object) = ConvertToOptional(boundAwaitOperator.ConstantValueOpt)
            Dim isImplicit As Boolean = boundAwaitOperator.WasCompilerGenerated
            Return New VisualBasicLazyAwaitOperation(Me, awaitedValue, _semanticModel, syntax, type, constantValue, isImplicit)
        End Function

        Private Function CreateBoundNameOfOperatorOperation(boundNameOfOperator As BoundNameOfOperator) As INameOfOperation
            Dim argument As BoundNode = boundNameOfOperator.Argument
            Dim syntax As SyntaxNode = boundNameOfOperator.Syntax
            Dim type As ITypeSymbol = boundNameOfOperator.Type
            Dim constantValue As [Optional](Of Object) = ConvertToOptional(boundNameOfOperator.ConstantValueOpt)
            Dim isImplicit As Boolean = boundNameOfOperator.WasCompilerGenerated
            Return New VisualBasicLazyNameOfOperation(Me, argument, _semanticModel, syntax, type, constantValue, isImplicit)
        End Function

        Private Function CreateBoundLambdaOperation(boundLambda As BoundLambda) As IAnonymousFunctionOperation
            Dim symbol As IMethodSymbol = boundLambda.LambdaSymbol
            Dim body As BoundNode = boundLambda.Body
            Dim syntax As SyntaxNode = boundLambda.Syntax
            Dim type As ITypeSymbol = boundLambda.Type
            Dim constantValue As [Optional](Of Object) = ConvertToOptional(boundLambda.ConstantValueOpt)
            Dim isImplicit As Boolean = boundLambda.WasCompilerGenerated
            Return New VisualBasicLazyAnonymousFunctionOperation(Me, body, symbol, _semanticModel, syntax, type, constantValue, isImplicit)
        End Function

        Private Function CreateBoundCallOperation(boundCall As BoundCall) As IInvocationOperation
            Dim targetMethod As IMethodSymbol = boundCall.Method

            Dim isVirtual As Boolean =
                   targetMethod IsNot Nothing AndAlso
                   (targetMethod.IsVirtual OrElse targetMethod.IsAbstract OrElse targetMethod.IsOverride) AndAlso
                   If(boundCall.ReceiverOpt?.Kind <> BoundKind.MyBaseReference, False) AndAlso
                   If(boundCall.ReceiverOpt?.Kind <> BoundKind.MyClassReference, False)

            Dim syntax As SyntaxNode = boundCall.Syntax
            Dim type As ITypeSymbol = boundCall.Type
            Dim constantValue As [Optional](Of Object) = ConvertToOptional(boundCall.ConstantValueOpt)
            Dim isImplicit As Boolean = boundCall.WasCompilerGenerated
            Return New VisualBasicLazyInvocationOperation(Me, boundCall, targetMethod, isVirtual, _semanticModel, syntax, type, constantValue, isImplicit)
        End Function

        Private Function CreateBoundOmittedArgumentOperation(boundOmittedArgument As BoundOmittedArgument) As IOmittedArgumentOperation
            Dim syntax As SyntaxNode = boundOmittedArgument.Syntax
            Dim type As ITypeSymbol = boundOmittedArgument.Type
            Dim constantValue As [Optional](Of Object) = ConvertToOptional(boundOmittedArgument.ConstantValueOpt)
            Dim isImplicit As Boolean = boundOmittedArgument.WasCompilerGenerated
            Return New OmittedArgumentOperation(_semanticModel, syntax, type, constantValue, isImplicit)
        End Function

        Private Function CreateBoundParenthesizedOperation(boundParenthesized As BoundParenthesized) As IParenthesizedOperation
            Dim operand As BoundNode = boundParenthesized.Expression
            Dim syntax As SyntaxNode = boundParenthesized.Syntax
            Dim type As ITypeSymbol = boundParenthesized.Type
            Dim constantValue As [Optional](Of Object) = ConvertToOptional(boundParenthesized.ConstantValueOpt)
            Dim isImplicit As Boolean = boundParenthesized.WasCompilerGenerated
            Return New VisualBasicLazyParenthesizedOperation(Me, operand, _semanticModel, syntax, type, constantValue, isImplicit)
        End Function

        Private Function CreateBoundArrayAccessOperation(boundArrayAccess As BoundArrayAccess) As IArrayElementReferenceOperation
            Dim syntax As SyntaxNode = boundArrayAccess.Syntax
            Dim type As ITypeSymbol = boundArrayAccess.Type
            Dim constantValue As [Optional](Of Object) = ConvertToOptional(boundArrayAccess.ConstantValueOpt)
            Dim isImplicit As Boolean = boundArrayAccess.WasCompilerGenerated
            Return New VisualBasicLazyArrayElementReferenceOperation(Me, boundArrayAccess, _semanticModel, syntax, type, constantValue, isImplicit)
        End Function

        Friend Function CreateBoundUnaryOperatorChild(boundOperator As BoundExpression) As IOperation
            Select Case boundOperator.Kind
                Case BoundKind.UnaryOperator
                    Return Create(DirectCast(boundOperator, BoundUnaryOperator).Operand)
                Case BoundKind.UserDefinedUnaryOperator
                    Dim userDefined = DirectCast(boundOperator, BoundUserDefinedUnaryOperator)
                    If userDefined.UnderlyingExpression.Kind = BoundKind.Call Then
                        Return Create(userDefined.Operand)
                    Else
                        Return GetChildOfBadExpression(userDefined.UnderlyingExpression, 0)
                    End If
                Case Else
                    Throw ExceptionUtilities.UnexpectedValue(boundOperator.Kind)
            End Select
        End Function

        Private Function CreateBoundUnaryOperatorOperation(boundUnaryOperator As BoundUnaryOperator) As IUnaryOperation
            Dim operatorKind As UnaryOperatorKind = Helper.DeriveUnaryOperatorKind(boundUnaryOperator.OperatorKind)
            Dim operatorMethod As IMethodSymbol = Nothing
            Dim syntax As SyntaxNode = boundUnaryOperator.Syntax
            Dim type As ITypeSymbol = boundUnaryOperator.Type
            Dim constantValue As [Optional](Of Object) = ConvertToOptional(boundUnaryOperator.ConstantValueOpt)
            Dim isLifted As Boolean = (boundUnaryOperator.OperatorKind And VisualBasic.UnaryOperatorKind.Lifted) <> 0
            Dim isChecked As Boolean = boundUnaryOperator.Checked
            Dim isImplicit As Boolean = boundUnaryOperator.WasCompilerGenerated
            Return New VisualBasicLazyUnaryOperation(Me, boundUnaryOperator, operatorKind, isLifted, isChecked, operatorMethod, _semanticModel, syntax, type, constantValue, isImplicit)
        End Function

        Private Function CreateBoundUserDefinedUnaryOperatorOperation(boundUserDefinedUnaryOperator As BoundUserDefinedUnaryOperator) As IUnaryOperation
            Dim operatorKind As UnaryOperatorKind = Helper.DeriveUnaryOperatorKind(boundUserDefinedUnaryOperator.OperatorKind)
            Dim operatorMethod As IMethodSymbol = TryGetOperatorMethod(boundUserDefinedUnaryOperator)
            Dim syntax As SyntaxNode = boundUserDefinedUnaryOperator.Syntax
            Dim type As ITypeSymbol = boundUserDefinedUnaryOperator.Type
            Dim constantValue As [Optional](Of Object) = ConvertToOptional(boundUserDefinedUnaryOperator.ConstantValueOpt)
            Dim isLifted As Boolean = (boundUserDefinedUnaryOperator.OperatorKind And VisualBasic.UnaryOperatorKind.Lifted) <> 0
            Dim isChecked As Boolean = False
            Dim isImplicit As Boolean = boundUserDefinedUnaryOperator.WasCompilerGenerated
            Return New VisualBasicLazyUnaryOperation(Me, boundUserDefinedUnaryOperator, operatorKind, isLifted, isChecked, operatorMethod, _semanticModel, syntax, type, constantValue, isImplicit)
        End Function

        Private Shared Function TryGetOperatorMethod(boundUserDefinedUnaryOperator As BoundUserDefinedUnaryOperator) As MethodSymbol
            Return If(boundUserDefinedUnaryOperator.UnderlyingExpression.Kind = BoundKind.Call, boundUserDefinedUnaryOperator.Call.Method, Nothing)
        End Function

        Friend Function CreateBoundBinaryOperatorChild(binaryOperator As BoundExpression, isLeft As Boolean) As IOperation
            Select Case binaryOperator.Kind
                Case BoundKind.BinaryOperator
                    Dim boundBinaryOperator = DirectCast(binaryOperator, BoundBinaryOperator)
                    Dim binaryOperatorInfo = GetBinaryOperatorInfo(boundBinaryOperator)
                    Return Create(If(isLeft, binaryOperatorInfo.LeftOperand, binaryOperatorInfo.RightOperand))
                Case BoundKind.UserDefinedBinaryOperator
                    Dim boundUserDefined = DirectCast(binaryOperator, BoundUserDefinedBinaryOperator)
                    Dim binaryOperatorInfo As BinaryOperatorInfo = GetUserDefinedBinaryOperatorInfo(boundUserDefined)
                    Return GetUserDefinedBinaryOperatorChild(boundUserDefined, If(isLeft, binaryOperatorInfo.LeftOperand, binaryOperatorInfo.RightOperand))
                Case BoundKind.UserDefinedShortCircuitingOperator
                    Dim boundShortCircuiting = DirectCast(binaryOperator, BoundUserDefinedShortCircuitingOperator)
                    Dim binaryOperatorInfo As BinaryOperatorInfo = GetUserDefinedBinaryOperatorInfo(boundShortCircuiting.BitwiseOperator)
                    If isLeft Then
                        Return If(boundShortCircuiting.LeftOperand IsNot Nothing,
                                  Create(boundShortCircuiting.LeftOperand), ' Possibly dropping conversions https://github.com/dotnet/roslyn/issues/23236
                                  GetUserDefinedBinaryOperatorChild(boundShortCircuiting.BitwiseOperator, binaryOperatorInfo.LeftOperand))
                    Else
                        Return GetUserDefinedBinaryOperatorChild(boundShortCircuiting.BitwiseOperator, binaryOperatorInfo.RightOperand)
                    End If
                Case Else
                    Throw ExceptionUtilities.UnexpectedValue(binaryOperator.Kind)
            End Select
        End Function

        Private Function CreateBoundBinaryOperatorOperation(boundBinaryOperator As BoundBinaryOperator) As IBinaryOperation
            Dim binaryOperatorInfo = GetBinaryOperatorInfo(boundBinaryOperator)
            Dim syntax As SyntaxNode = boundBinaryOperator.Syntax
            Dim type As ITypeSymbol = boundBinaryOperator.Type
            Dim constantValue As [Optional](Of Object) = ConvertToOptional(boundBinaryOperator.ConstantValueOpt)
            Dim isImplicit As Boolean = boundBinaryOperator.WasCompilerGenerated
            Return New VisualBasicLazyBinaryOperation(Me, boundBinaryOperator, binaryOperatorInfo.OperatorKind, binaryOperatorInfo.IsLifted,
                                                      binaryOperatorInfo.IsChecked, binaryOperatorInfo.IsCompareText, binaryOperatorInfo.OperatorMethod,
                                                      unaryOperatorMethod:=Nothing, _semanticModel, syntax, type, constantValue, isImplicit)
        End Function

        Private Function CreateBoundUserDefinedBinaryOperatorOperation(boundUserDefinedBinaryOperator As BoundUserDefinedBinaryOperator) As IBinaryOperation
            Dim binaryOperatorInfo = GetUserDefinedBinaryOperatorInfo(boundUserDefinedBinaryOperator)
            Dim syntax As SyntaxNode = boundUserDefinedBinaryOperator.Syntax
            Dim type As ITypeSymbol = boundUserDefinedBinaryOperator.Type
            Dim constantValue As [Optional](Of Object) = ConvertToOptional(boundUserDefinedBinaryOperator.ConstantValueOpt)
            Dim isImplicit As Boolean = boundUserDefinedBinaryOperator.WasCompilerGenerated
            Return New VisualBasicLazyBinaryOperation(Me, boundUserDefinedBinaryOperator, binaryOperatorInfo.OperatorKind, binaryOperatorInfo.IsLifted,
                                                      binaryOperatorInfo.IsChecked, binaryOperatorInfo.IsCompareText, binaryOperatorInfo.OperatorMethod,
                                                      unaryOperatorMethod:=Nothing, _semanticModel, syntax, type, constantValue, isImplicit)
        End Function

        Private Function CreateBoundBinaryConditionalExpressionOperation(boundBinaryConditionalExpression As BoundBinaryConditionalExpression) As ICoalesceOperation
            Dim syntax As SyntaxNode = boundBinaryConditionalExpression.Syntax
            Dim type As ITypeSymbol = boundBinaryConditionalExpression.Type
            Dim constantValue As [Optional](Of Object) = ConvertToOptional(boundBinaryConditionalExpression.ConstantValueOpt)
            Dim isImplicit As Boolean = boundBinaryConditionalExpression.WasCompilerGenerated

            Dim valueConversion = New Conversion(Conversions.Identity)

            If Not TypeSymbol.Equals(boundBinaryConditionalExpression.Type, boundBinaryConditionalExpression.TestExpression.Type, TypeCompareKind.ConsiderEverything) Then
                Dim convertedTestExpression As BoundExpression = boundBinaryConditionalExpression.ConvertedTestExpression
                If convertedTestExpression IsNot Nothing Then
                    If convertedTestExpression.Kind = BoundKind.Conversion Then
                        valueConversion = CreateConversion(convertedTestExpression)
                    Else
                        Debug.Assert(convertedTestExpression.Kind = BoundKind.BadExpression)
                        valueConversion = New Conversion()
                    End If
                End If
            End If

            Return New VisualBasicLazyCoalesceOperation(Me, boundBinaryConditionalExpression, valueConversion, _semanticModel, syntax, type, constantValue, isImplicit)
        End Function

        Private Function CreateBoundUserDefinedShortCircuitingOperatorOperation(boundUserDefinedShortCircuitingOperator As BoundUserDefinedShortCircuitingOperator) As IBinaryOperation
            Dim bitwiseOperator As BoundUserDefinedBinaryOperator = boundUserDefinedShortCircuitingOperator.BitwiseOperator
            Dim binaryOperatorInfo As BinaryOperatorInfo = GetUserDefinedBinaryOperatorInfo(bitwiseOperator)
            Dim operatorKind As BinaryOperatorKind = If(binaryOperatorInfo.OperatorKind = BinaryOperatorKind.And, BinaryOperatorKind.ConditionalAnd, BinaryOperatorKind.ConditionalOr)

            Dim syntax As SyntaxNode = boundUserDefinedShortCircuitingOperator.Syntax
            Dim type As ITypeSymbol = boundUserDefinedShortCircuitingOperator.Type
            Dim constantValue As [Optional](Of Object) = ConvertToOptional(boundUserDefinedShortCircuitingOperator.ConstantValueOpt)
            Dim isChecked As Boolean = False
            Dim isCompareText As Boolean = False
            Dim isImplicit As Boolean = boundUserDefinedShortCircuitingOperator.WasCompilerGenerated

            Dim unaryOperatorMethod As IMethodSymbol = Nothing
            Dim leftTest As BoundExpression = boundUserDefinedShortCircuitingOperator.LeftTest
            If leftTest IsNot Nothing Then
                unaryOperatorMethod = TryGetOperatorMethod(DirectCast(If(leftTest.Kind = BoundKind.UserDefinedUnaryOperator,
                                                                         leftTest,
                                                                         DirectCast(leftTest, BoundNullableIsTrueOperator).Operand),
                                                                      BoundUserDefinedUnaryOperator))
            End If

            Return New VisualBasicLazyBinaryOperation(Me, boundUserDefinedShortCircuitingOperator, operatorKind, binaryOperatorInfo.IsLifted, isChecked, isCompareText,
                                                      binaryOperatorInfo.OperatorMethod, unaryOperatorMethod, _semanticModel, syntax, type, constantValue, isImplicit)
        End Function

        Private Function CreateBoundBadExpressionOperation(boundBadExpression As BoundBadExpression) As IInvalidOperation
            Dim syntax As SyntaxNode = boundBadExpression.Syntax
            ' We match semantic model here: If the Then expression IsMissing, we have a null type, rather than the ErrorType Of the bound node.
            Dim type As ITypeSymbol = If(syntax.IsMissing, Nothing, boundBadExpression.Type)
            Dim constantValue As [Optional](Of Object) = ConvertToOptional(boundBadExpression.ConstantValueOpt)

            ' if child has syntax node point to same syntax node as bad expression, then this invalid expression Is implicit
            Dim isImplicit = boundBadExpression.WasCompilerGenerated OrElse boundBadExpression.ChildBoundNodes.Any(Function(e) e?.Syntax Is boundBadExpression.Syntax)
            Return New VisualBasicLazyInvalidOperation(Me, boundBadExpression, _semanticModel, syntax, type, constantValue, isImplicit)
        End Function

        Private Function CreateBoundTryCastOperation(boundTryCast As BoundTryCast) As IOperation
            Return CreateBoundConversionOrCastOperation(boundTryCast, isTryCast:=True)
        End Function

        Private Function CreateBoundDirectCastOperation(boundDirectCast As BoundDirectCast) As IOperation
            Return CreateBoundConversionOrCastOperation(boundDirectCast, isTryCast:=False)
        End Function

        Private Function CreateBoundConversionOperation(boundConversion As BoundConversion) As IOperation
            Dim syntax As SyntaxNode = boundConversion.Syntax

            If syntax.IsMissing AndAlso boundConversion.Operand.Kind = BoundKind.BadExpression Then
                ' If the underlying syntax IsMissing, then that means we're in case where the compiler generated a piece of syntax to fill in for
                ' an error, such as this case:
                '
                '  Dim i =
                '
                ' Semantic model has a special case here that we match: if the underlying syntax is missing, don't create a conversion expression,
                ' and instead directly return the operand, which will be a BoundBadExpression. When we generate a node for the BoundBadExpression,
                ' the resulting IOperation will also have a null Type.
                Return Create(boundConversion.Operand)
            End If

            Return CreateBoundConversionOrCastOperation(boundConversion, isTryCast:=False)
        End Function

        Private Function CreateBoundConversionOrCastOperation(boundConversionOrCast As BoundConversionOrCast, isTryCast As Boolean) As IOperation
            Dim isChecked As Boolean = False
            Dim type As ITypeSymbol = boundConversionOrCast.Type
            Dim constantValue As [Optional](Of Object) = ConvertToOptional(boundConversionOrCast.ConstantValueOpt)
            Dim syntax As SyntaxNode = boundConversionOrCast.Syntax
            Dim isImplicit As Boolean = boundConversionOrCast.WasCompilerGenerated OrElse Not boundConversionOrCast.ExplicitCastInCode

            Dim boundOperand = GetConversionOperand(boundConversionOrCast)
            If boundOperand.Syntax Is boundConversionOrCast.Syntax Then
                If boundOperand.Kind = BoundKind.ConvertedTupleLiteral AndAlso TypeSymbol.Equals(boundOperand.Type, boundConversionOrCast.Type, TypeCompareKind.ConsiderEverything) Then
                    ' Erase this conversion, this is an artificial conversion added on top of BoundConvertedTupleLiteral
                    ' in Binder.ReclassifyTupleLiteral
                    Return Create(boundOperand)
                Else
                    ' Make this conversion implicit
                    isImplicit = True
                End If
            End If

            Dim conversionInfo = GetConversionInfo(boundConversionOrCast)
            Dim conversion As Conversion = conversionInfo.Conversion

            If conversionInfo.IsDelegateCreation Then
                Return New DelegateCreationOperation(conversionInfo.Operation, _semanticModel, syntax, type, constantValue, isImplicit)
            Else
                Return New ConversionOperation(conversionInfo.Operation, conversion, isTryCast, isChecked, _semanticModel, syntax, type, constantValue, isImplicit)
            End If
        End Function

        Private Function CreateBoundDelegateCreationExpressionOperation(boundDelegateCreationExpression As BoundDelegateCreationExpression) As IDelegateCreationOperation
            Dim syntax As SyntaxNode = boundDelegateCreationExpression.Syntax
            Dim type As ITypeSymbol = boundDelegateCreationExpression.Type
            Dim constantValue As [Optional](Of Object) = ConvertToOptional(boundDelegateCreationExpression.ConstantValueOpt)

            ' The operand for this is going to be using the same syntax node as this, and since that node can be Explicit, this node cannot be.
            Dim isImplicit As Boolean = True

            Return New VisualBasicLazyDelegateCreationOperation(Me, boundDelegateCreationExpression, _semanticModel, syntax, type, constantValue, isImplicit)
        End Function

        Friend Function CreateBoundDelegateCreationExpressionChildOperation(boundDelegateCreationExpression As BoundDelegateCreationExpression) As IMethodReferenceOperation
            Dim method As IMethodSymbol = boundDelegateCreationExpression.Method
            Dim isVirtual As Boolean = method IsNot Nothing AndAlso
                                               (method.IsAbstract OrElse method.IsOverride OrElse method.IsVirtual) AndAlso
                                               Not boundDelegateCreationExpression.SuppressVirtualCalls

            Dim receiverOpt As BoundExpression = If(boundDelegateCreationExpression.ReceiverOpt, boundDelegateCreationExpression.MethodGroupOpt?.ReceiverOpt)

            ' The compiler creates a BoundDelegateCreationExpression node for the AddressOf expression, and that's the node we want to use for the operand
            ' of the IDelegateCreationExpression parent
            Dim syntax As SyntaxNode = boundDelegateCreationExpression.Syntax
            Dim type As ITypeSymbol = Nothing
            Dim constantValue As [Optional](Of Object) = ConvertToOptional(boundDelegateCreationExpression.ConstantValueOpt)
            Dim isImplicit As Boolean = boundDelegateCreationExpression.WasCompilerGenerated
            Return New VisualBasicLazyMethodReferenceOperation(Me, receiverOpt, method, isVirtual, _semanticModel, syntax, type, constantValue, isImplicit)
        End Function

        Private Function CreateBoundTernaryConditionalExpressionOperation(boundTernaryConditionalExpression As BoundTernaryConditionalExpression) As IConditionalOperation
            Dim syntax As SyntaxNode = boundTernaryConditionalExpression.Syntax
            Dim type As ITypeSymbol = boundTernaryConditionalExpression.Type
            Dim constantValue As [Optional](Of Object) = ConvertToOptional(boundTernaryConditionalExpression.ConstantValueOpt)
            Dim isImplicit As Boolean = boundTernaryConditionalExpression.WasCompilerGenerated
            Dim isRef As Boolean = False
            Return New VisualBasicLazyConditionalOperation(Me, boundTernaryConditionalExpression, isRef, _semanticModel, syntax, type, constantValue, isImplicit)
        End Function

        Private Function CreateBoundTypeOfOperation(boundTypeOf As BoundTypeOf) As IIsTypeOperation
            Dim valueOperand = boundTypeOf.Operand
            Dim typeOperand As ITypeSymbol = boundTypeOf.TargetType
            Dim isNegated As Boolean = boundTypeOf.IsTypeOfIsNotExpression
            Dim syntax As SyntaxNode = boundTypeOf.Syntax
            Dim type As ITypeSymbol = boundTypeOf.Type
            Dim constantValue As [Optional](Of Object) = ConvertToOptional(boundTypeOf.ConstantValueOpt)
            Dim isImplicit As Boolean = boundTypeOf.WasCompilerGenerated
            Return New VisualBasicLazyIsTypeOperation(Me, valueOperand, typeOperand, isNegated, _semanticModel, syntax, type, constantValue, isImplicit)
        End Function

        Private Function CreateBoundGetTypeOperation(boundGetType As BoundGetType) As ITypeOfOperation
            Dim typeOperand As ITypeSymbol = boundGetType.SourceType.Type
            Dim syntax As SyntaxNode = boundGetType.Syntax
            Dim type As ITypeSymbol = boundGetType.Type
            Dim constantValue As [Optional](Of Object) = ConvertToOptional(boundGetType.ConstantValueOpt)
            Dim isImplicit As Boolean = boundGetType.WasCompilerGenerated
            Return New TypeOfOperation(typeOperand, _semanticModel, syntax, type, constantValue, isImplicit)
        End Function

        Private Function CreateBoundLateInvocationOperation(boundLateInvocation As BoundLateInvocation) As IOperation
            Dim argumentNames As ImmutableArray(Of String) = boundLateInvocation.ArgumentNamesOpt
            Dim argumentRefKinds As ImmutableArray(Of RefKind) = Nothing
            Dim syntax As SyntaxNode = boundLateInvocation.Syntax
            Dim type As ITypeSymbol = boundLateInvocation.Type
            Dim constantValue As [Optional](Of Object) = ConvertToOptional(boundLateInvocation.ConstantValueOpt)
            Dim isImplicit As Boolean = boundLateInvocation.WasCompilerGenerated
            Return New VisualBasicLazyDynamicInvocationOperation(Me, boundLateInvocation, argumentNames, argumentRefKinds, _semanticModel, syntax, type, constantValue, isImplicit)
        End Function

        Private Function CreateBoundObjectCreationExpressionOperation(boundObjectCreationExpression As BoundObjectCreationExpression) As IObjectCreationOperation
            Debug.Assert(boundObjectCreationExpression.ConstructorOpt IsNot Nothing OrElse boundObjectCreationExpression.Arguments.IsEmpty())
            Dim constructor As IMethodSymbol = boundObjectCreationExpression.ConstructorOpt

            Dim syntax As SyntaxNode = boundObjectCreationExpression.Syntax
            Dim type As ITypeSymbol = boundObjectCreationExpression.Type
            Dim constantValue As [Optional](Of Object) = ConvertToOptional(boundObjectCreationExpression.ConstantValueOpt)
            Dim isImplicit As Boolean = boundObjectCreationExpression.WasCompilerGenerated
            Return New VisualBasicLazyObjectCreationOperation(Me, boundObjectCreationExpression, constructor, _semanticModel, syntax, type, constantValue, isImplicit)
        End Function

        Private Function CreateBoundObjectInitializerExpressionOperation(boundObjectInitializerExpression As BoundObjectInitializerExpression) As IObjectOrCollectionInitializerOperation
            Dim syntax As SyntaxNode = boundObjectInitializerExpression.Syntax
            Dim type As ITypeSymbol = boundObjectInitializerExpression.Type
            Dim constantValue As [Optional](Of Object) = ConvertToOptional(boundObjectInitializerExpression.ConstantValueOpt)
            Dim isImplicit As Boolean = boundObjectInitializerExpression.WasCompilerGenerated
            Return New VisualBasicLazyObjectOrCollectionInitializerOperation(Me, boundObjectInitializerExpression, _semanticModel, syntax, type, constantValue, isImplicit)
        End Function

        Private Function CreateBoundCollectionInitializerExpressionOperation(boundCollectionInitializerExpression As BoundCollectionInitializerExpression) As IObjectOrCollectionInitializerOperation
            Dim syntax As SyntaxNode = boundCollectionInitializerExpression.Syntax
            Dim type As ITypeSymbol = boundCollectionInitializerExpression.Type
            Dim constantValue As [Optional](Of Object) = ConvertToOptional(boundCollectionInitializerExpression.ConstantValueOpt)
            Dim isImplicit As Boolean = boundCollectionInitializerExpression.WasCompilerGenerated
            Return New VisualBasicLazyObjectOrCollectionInitializerOperation(Me, boundCollectionInitializerExpression, _semanticModel, syntax, type, constantValue, isImplicit)
        End Function

        Private Function CreateBoundNewTOperation(boundNewT As BoundNewT) As ITypeParameterObjectCreationOperation
            Dim initializer As BoundNode = boundNewT.InitializerOpt
            Dim syntax As SyntaxNode = boundNewT.Syntax
            Dim type As ITypeSymbol = boundNewT.Type
            Dim constantValue As [Optional](Of Object) = ConvertToOptional(boundNewT.ConstantValueOpt)
            Dim isImplicit As Boolean = boundNewT.WasCompilerGenerated
            Return New VisualBasicLazyTypeParameterObjectCreationOperation(Me, initializer, _semanticModel, syntax, type, constantValue, isImplicit)
        End Function

        Private Function CreateNoPiaObjectCreationExpressionOperation(creation As BoundNoPiaObjectCreationExpression) As INoPiaObjectCreationOperation
            Dim initializer As BoundNode = creation.InitializerOpt
            Dim syntax As SyntaxNode = creation.Syntax
            Dim type As ITypeSymbol = creation.Type
            Dim constantValue As [Optional](Of Object) = ConvertToOptional(creation.ConstantValueOpt)
            Dim isImplicit As Boolean = creation.WasCompilerGenerated
            Return New VisualBasicLazyNoPiaObjectCreationOperation(Me, initializer, _semanticModel, syntax, type, constantValue, isImplicit)
        End Function

        Private Function CreateBoundArrayCreationOperation(boundArrayCreation As BoundArrayCreation) As IArrayCreationOperation
            Dim syntax As SyntaxNode = boundArrayCreation.Syntax
            Dim type As ITypeSymbol = boundArrayCreation.Type
            Dim constantValue As [Optional](Of Object) = ConvertToOptional(boundArrayCreation.ConstantValueOpt)
            Dim isImplicit As Boolean = boundArrayCreation.WasCompilerGenerated
            Return New VisualBasicLazyArrayCreationOperation(Me, boundArrayCreation, _semanticModel, syntax, type, constantValue, isImplicit)
        End Function

        Private Function CreateBoundArrayInitializationOperation(boundArrayInitialization As BoundArrayInitialization) As IArrayInitializerOperation
            Dim syntax As SyntaxNode = boundArrayInitialization.Syntax
            Dim constantValue As [Optional](Of Object) = ConvertToOptional(boundArrayInitialization.ConstantValueOpt)
            Dim isImplicit As Boolean = boundArrayInitialization.WasCompilerGenerated
            Return New VisualBasicLazyArrayInitializerOperation(Me, boundArrayInitialization, _semanticModel, syntax, constantValue, isImplicit)
        End Function

        Private Function CreateBoundPropertyAccessOperation(boundPropertyAccess As BoundPropertyAccess) As IPropertyReferenceOperation
            Dim [property] As IPropertySymbol = boundPropertyAccess.PropertySymbol

            Dim syntax As SyntaxNode = boundPropertyAccess.Syntax
            Dim type As ITypeSymbol = boundPropertyAccess.Type
            Dim constantValue As [Optional](Of Object) = ConvertToOptional(boundPropertyAccess.ConstantValueOpt)
            Dim isImplicit As Boolean = boundPropertyAccess.WasCompilerGenerated
            Return New VisualBasicLazyPropertyReferenceOperation(Me, boundPropertyAccess, [property], _semanticModel, syntax, type, constantValue, isImplicit)
        End Function

        Private Function CreateBoundWithLValueExpressionPlaceholder(boundWithLValueExpressionPlaceholder As BoundWithLValueExpressionPlaceholder) As IInstanceReferenceOperation
            Dim referenceKind As InstanceReferenceKind = InstanceReferenceKind.ImplicitReceiver
            Dim syntax As SyntaxNode = boundWithLValueExpressionPlaceholder.Syntax
            Dim type As ITypeSymbol = boundWithLValueExpressionPlaceholder.Type
            Dim constantValue As [Optional](Of Object) = ConvertToOptional(boundWithLValueExpressionPlaceholder.ConstantValueOpt)
            Dim isImplicit As Boolean = boundWithLValueExpressionPlaceholder.WasCompilerGenerated
            Return New InstanceReferenceOperation(referenceKind, _semanticModel, syntax, type, constantValue, isImplicit)
        End Function

        Private Function CreateBoundWithRValueExpressionPlaceholder(boundWithRValueExpressionPlaceholder As BoundWithRValueExpressionPlaceholder) As IInstanceReferenceOperation
            Dim referenceKind As InstanceReferenceKind = InstanceReferenceKind.ImplicitReceiver
            Dim syntax As SyntaxNode = boundWithRValueExpressionPlaceholder.Syntax
            Dim type As ITypeSymbol = boundWithRValueExpressionPlaceholder.Type
            Dim constantValue As [Optional](Of Object) = ConvertToOptional(boundWithRValueExpressionPlaceholder.ConstantValueOpt)
            Dim isImplicit As Boolean = boundWithRValueExpressionPlaceholder.WasCompilerGenerated
            Return New InstanceReferenceOperation(referenceKind, _semanticModel, syntax, type, constantValue, isImplicit)
        End Function

        Private Function CreateBoundEventAccessOperation(boundEventAccess As BoundEventAccess) As IEventReferenceOperation
            Dim [event] As IEventSymbol = boundEventAccess.EventSymbol
            Dim instance As BoundNode = boundEventAccess.ReceiverOpt

            Dim syntax As SyntaxNode = boundEventAccess.Syntax
            Dim type As ITypeSymbol = boundEventAccess.Type
            Dim constantValue As [Optional](Of Object) = ConvertToOptional(boundEventAccess.ConstantValueOpt)
            Dim isImplicit As Boolean = boundEventAccess.WasCompilerGenerated
            Return New VisualBasicLazyEventReferenceOperation(Me, instance, [event], _semanticModel, syntax, type, constantValue, isImplicit)
        End Function

        Private Function CreateBoundFieldAccessOperation(boundFieldAccess As BoundFieldAccess) As IFieldReferenceOperation
            Dim field As IFieldSymbol = boundFieldAccess.FieldSymbol
            Dim isDeclaration As Boolean = False
            Dim instance As BoundNode = boundFieldAccess.ReceiverOpt

            Dim syntax As SyntaxNode = boundFieldAccess.Syntax
            Dim type As ITypeSymbol = boundFieldAccess.Type
            Dim constantValue As [Optional](Of Object) = ConvertToOptional(boundFieldAccess.ConstantValueOpt)
            Dim isImplicit As Boolean = boundFieldAccess.WasCompilerGenerated
            Return New VisualBasicLazyFieldReferenceOperation(Me, instance, field, isDeclaration, _semanticModel, syntax, type, constantValue, isImplicit)
        End Function

        Private Function CreateBoundConditionalAccessOperation(boundConditionalAccess As BoundConditionalAccess) As IConditionalAccessOperation
            RecordParent(boundConditionalAccess.Placeholder, boundConditionalAccess)
            Dim syntax As SyntaxNode = boundConditionalAccess.Syntax
            Dim type As ITypeSymbol = boundConditionalAccess.Type
            Dim constantValue As [Optional](Of Object) = ConvertToOptional(boundConditionalAccess.ConstantValueOpt)
            Dim isImplicit As Boolean = boundConditionalAccess.WasCompilerGenerated
            Return New VisualBasicLazyConditionalAccessOperation(Me, boundConditionalAccess, _semanticModel, syntax, type, constantValue, isImplicit)
        End Function

        Private Function CreateBoundConditionalAccessReceiverPlaceholderOperation(boundConditionalAccessReceiverPlaceholder As BoundConditionalAccessReceiverPlaceholder) As IConditionalAccessInstanceOperation
            Dim syntax As SyntaxNode = boundConditionalAccessReceiverPlaceholder.Syntax
            Dim type As ITypeSymbol = boundConditionalAccessReceiverPlaceholder.Type
            Dim constantValue As [Optional](Of Object) = ConvertToOptional(boundConditionalAccessReceiverPlaceholder.ConstantValueOpt)
            Dim isImplicit As Boolean = boundConditionalAccessReceiverPlaceholder.WasCompilerGenerated
            Return New ConditionalAccessInstanceOperation(_semanticModel, syntax, type, constantValue, isImplicit)
        End Function

        Private Function CreateBoundParameterOperation(boundParameter As BoundParameter) As IParameterReferenceOperation
            Dim parameter As IParameterSymbol = boundParameter.ParameterSymbol
            Dim syntax As SyntaxNode = boundParameter.Syntax
            Dim type As ITypeSymbol = boundParameter.Type
            Dim constantValue As [Optional](Of Object) = ConvertToOptional(boundParameter.ConstantValueOpt)
            Dim isImplicit As Boolean = boundParameter.WasCompilerGenerated
            Return New ParameterReferenceOperation(parameter, _semanticModel, syntax, type, constantValue, isImplicit)
        End Function

        Private Function CreateBoundLocalOperation(boundLocal As BoundLocal) As IOperation
            Dim local As ILocalSymbol = boundLocal.LocalSymbol
            Dim isDeclaration As Boolean = False
            Dim syntax As SyntaxNode = boundLocal.Syntax
            Dim type As ITypeSymbol = boundLocal.Type
            Dim constantValue As [Optional](Of Object) = ConvertToOptional(boundLocal.ConstantValueOpt)
            Dim isImplicit As Boolean = boundLocal.WasCompilerGenerated
            Return New LocalReferenceOperation(local, isDeclaration, _semanticModel, syntax, type, constantValue, isImplicit)
        End Function

        Private Function CreateBoundLateMemberAccessOperation(boundLateMemberAccess As BoundLateMemberAccess) As IDynamicMemberReferenceOperation
            Debug.Assert(boundLateMemberAccess.ReceiverOpt Is Nothing OrElse boundLateMemberAccess.ReceiverOpt.Kind <> BoundKind.TypeExpression)

            Dim instance As BoundNode = boundLateMemberAccess.ReceiverOpt
            Dim memberName As String = boundLateMemberAccess.NameOpt
            Dim typeArguments As ImmutableArray(Of ITypeSymbol) = ImmutableArray(Of ITypeSymbol).Empty
            If boundLateMemberAccess.TypeArgumentsOpt IsNot Nothing Then
                typeArguments = ImmutableArray(Of ITypeSymbol).CastUp(boundLateMemberAccess.TypeArgumentsOpt.Arguments)
            End If
            Dim containingType As ITypeSymbol = Nothing
            ' If there's nothing being late-bound against, something is very wrong
            Debug.Assert(boundLateMemberAccess.ReceiverOpt IsNot Nothing OrElse boundLateMemberAccess.ContainerTypeOpt IsNot Nothing)
            ' Only set containing type if the container is set to something, and either there is no receiver, or the receiver's type
            ' does not match the type of the containing type.
            If (boundLateMemberAccess.ContainerTypeOpt IsNot Nothing AndAlso
                (boundLateMemberAccess.ReceiverOpt Is Nothing OrElse
                 Not TypeSymbol.Equals(boundLateMemberAccess.ContainerTypeOpt, boundLateMemberAccess.ReceiverOpt.Type, TypeCompareKind.ConsiderEverything))) Then
                containingType = boundLateMemberAccess.ContainerTypeOpt
            End If
            Dim syntax As SyntaxNode = boundLateMemberAccess.Syntax
            Dim type As ITypeSymbol = boundLateMemberAccess.Type
            Dim constantValue As [Optional](Of Object) = ConvertToOptional(boundLateMemberAccess.ConstantValueOpt)
            Dim isImplicit As Boolean = boundLateMemberAccess.WasCompilerGenerated
            Return New VisualBasicLazyDynamicMemberReferenceOperation(Me, instance, memberName, typeArguments, containingType, _semanticModel, syntax, type, constantValue, isImplicit)
        End Function

        Private Function CreateBoundFieldInitializerOperation(boundFieldInitializer As BoundFieldInitializer) As IFieldInitializerOperation
            Dim initializedFields As ImmutableArray(Of IFieldSymbol) = boundFieldInitializer.InitializedFields.As(Of IFieldSymbol)
            Dim value As BoundNode = boundFieldInitializer.InitialValue
            Dim kind As OperationKind = OperationKind.FieldInitializer
            Dim syntax As SyntaxNode = boundFieldInitializer.Syntax
            Dim type As ITypeSymbol = Nothing
            Dim constantValue As [Optional](Of Object) = New [Optional](Of Object)()
            Dim isImplicit As Boolean = boundFieldInitializer.WasCompilerGenerated
            Return New VisualBasicLazyFieldInitializerOperation(Me, value, ImmutableArray(Of ILocalSymbol).Empty, initializedFields, kind, _semanticModel, syntax, type, constantValue, isImplicit)
        End Function

        Private Function CreateBoundPropertyInitializerOperation(boundPropertyInitializer As BoundPropertyInitializer) As IPropertyInitializerOperation
            Dim initializedProperties As ImmutableArray(Of IPropertySymbol) = boundPropertyInitializer.InitializedProperties.As(Of IPropertySymbol)
            Dim value = boundPropertyInitializer.InitialValue
            Dim kind As OperationKind = OperationKind.PropertyInitializer
            Dim syntax As SyntaxNode = boundPropertyInitializer.Syntax
            Dim type As ITypeSymbol = Nothing
            Dim constantValue As [Optional](Of Object) = New [Optional](Of Object)()
            Dim isImplicit As Boolean = boundPropertyInitializer.WasCompilerGenerated
            Return New VisualBasicLazyPropertyInitializerOperation(Me, value, ImmutableArray(Of ILocalSymbol).Empty, initializedProperties, kind, _semanticModel, syntax, type, constantValue, isImplicit)
        End Function

        Private Function CreateBoundParameterEqualsValueOperation(boundParameterEqualsValue As BoundParameterEqualsValue) As IParameterInitializerOperation
            Dim parameter As IParameterSymbol = boundParameterEqualsValue.Parameter
            Dim value = boundParameterEqualsValue.Value
            Dim kind As OperationKind = OperationKind.ParameterInitializer
            Dim syntax As SyntaxNode = boundParameterEqualsValue.Syntax
            Dim type As ITypeSymbol = Nothing
            Dim constantValue As [Optional](Of Object) = New [Optional](Of Object)()
            Dim isImplicit As Boolean = boundParameterEqualsValue.WasCompilerGenerated
            Return New VisualBasicLazyParameterInitializerOperation(Me, value, ImmutableArray(Of ILocalSymbol).Empty, parameter, kind, _semanticModel, syntax, type, constantValue, isImplicit)
        End Function

        Private Function CreateBoundLValueToRValueWrapper(boundNode As BoundLValueToRValueWrapper) As IOperation
            ' This is created in cases like collection initializers, where the implicit receiver is wrapped
            ' by this node. The node itself doesn't have anything interesting we want to expose, so we
            ' just pass through.
            Return Create(boundNode.UnderlyingLValue)
        End Function

        Private Function CreateBoundRValuePlaceholderOperation(boundRValuePlaceholder As BoundRValuePlaceholder) As IOperation
            Dim syntax As SyntaxNode = boundRValuePlaceholder.Syntax
            Dim type As ITypeSymbol = boundRValuePlaceholder.Type
            Dim constantValue As [Optional](Of Object) = ConvertToOptional(boundRValuePlaceholder.ConstantValueOpt)
            Dim isImplicit As Boolean = boundRValuePlaceholder.WasCompilerGenerated

            Dim knownParent As BoundNode = TryGetParent(boundRValuePlaceholder)
            Dim placeholderKind As PlaceholderKind = PlaceholderKind.Unspecified

            If knownParent IsNot Nothing Then
                Select Case knownParent.Kind
                    Case BoundKind.ConditionalAccess
                        ' BoundConditionalAccessReceiver isn't actually used until local rewriting, until then that node will be a
                        ' BoundRValuePlaceholder with a syntax node of the entire conditional access. So we dig through the syntax
                        ' to get the expression being conditionally accessed, and return an IConditionalAccessInstanceOperation
                        ' instead of a PlaceholderOperation
                        syntax = If(TryCast(syntax, ConditionalAccessExpressionSyntax)?.Expression, syntax)
                        Return New ConditionalAccessInstanceOperation(_semanticModel, syntax, type, constantValue, isImplicit)

                    Case BoundKind.SelectStatement
                        placeholderKind = PlaceholderKind.SwitchOperationExpression

                    Case BoundKind.ForToStatement
                        Dim operators As BoundForToUserDefinedOperators = DirectCast(knownParent, BoundForToStatement).OperatorsOpt
                        If boundRValuePlaceholder Is operators.LeftOperandPlaceholder Then
                            placeholderKind = PlaceholderKind.ForToLoopBinaryOperatorLeftOperand
                        Else
                            Debug.Assert(boundRValuePlaceholder Is operators.RightOperandPlaceholder)
                            placeholderKind = PlaceholderKind.ForToLoopBinaryOperatorRightOperand
                        End If

                    Case BoundKind.AggregateClause
                        placeholderKind = PlaceholderKind.AggregationGroup

                End Select
            End If

            Return New PlaceholderOperation(placeholderKind, _semanticModel, syntax, type, constantValue, isImplicit)
        End Function

        Private Function CreateBoundIfStatementOperation(boundIfStatement As BoundIfStatement) As IConditionalOperation
            Dim syntax As SyntaxNode = boundIfStatement.Syntax
            Dim type As ITypeSymbol = Nothing
            Dim constantValue As [Optional](Of Object) = New [Optional](Of Object)()
            Dim isImplicit As Boolean = boundIfStatement.WasCompilerGenerated
            Dim isRef As Boolean = False
            Return New VisualBasicLazyConditionalOperation(Me, boundIfStatement, isRef, _semanticModel, syntax, type, constantValue, isImplicit)
        End Function

        Private Function CreateBoundSelectStatementOperation(boundSelectStatement As BoundSelectStatement) As ISwitchOperation
            RecordParent(boundSelectStatement.ExprPlaceholderOpt, boundSelectStatement)
            Dim exitLabel As ILabelSymbol = boundSelectStatement.ExitLabel
            Dim syntax As SyntaxNode = boundSelectStatement.Syntax
            Dim type As ITypeSymbol = Nothing
            Dim constantValue As [Optional](Of Object) = New [Optional](Of Object)()
            Dim isImplicit As Boolean = boundSelectStatement.WasCompilerGenerated
            Return New VisualBasicLazySwitchOperation(Me, boundSelectStatement, ImmutableArray(Of ILocalSymbol).Empty, exitLabel, _semanticModel, syntax, type, constantValue, isImplicit)
        End Function

        Friend Function CreateBoundCaseBlockClauses(boundCaseBlock As BoundCaseBlock) As ImmutableArray(Of ICaseClauseOperation)
            ' `CaseElseClauseSyntax` is bound to `BoundCaseStatement` with an empty list of case clauses,
            ' so we explicitly create an IOperation node for Case-Else clause to differentiate it from Case clause.
            Dim caseStatement = boundCaseBlock.CaseStatement
            If caseStatement.CaseClauses.IsEmpty AndAlso caseStatement.Syntax.Kind() = SyntaxKind.CaseElseStatement Then
                Return ImmutableArray.Create(Of ICaseClauseOperation)(
                            New DefaultCaseClauseOperation(
                                label:=Nothing,
                                _semanticModel,
                                caseStatement.Syntax,
                                type:=Nothing,
                                constantValue:=Nothing,
                                isImplicit:=boundCaseBlock.WasCompilerGenerated))
            Else
                Return caseStatement.CaseClauses.SelectAsArray(Function(n) DirectCast(Create(n), ICaseClauseOperation))
            End If
        End Function

        Friend Function CreateBoundCaseBlockBody(boundCaseBlock As BoundCaseBlock) As ImmutableArray(Of IOperation)
            Return ImmutableArray.Create(Create(boundCaseBlock.Body))
        End Function

        Friend Function CreateBoundCaseBlockCondition(boundCaseBlock As BoundCaseBlock) As IOperation
            Return Clone().Create(boundCaseBlock.CaseStatement.ConditionOpt)
        End Function

        Private Function CreateBoundCaseBlockOperation(boundCaseBlock As BoundCaseBlock) As ISwitchCaseOperation
            Dim syntax As SyntaxNode = boundCaseBlock.Syntax
            Dim type As ITypeSymbol = Nothing
            Dim constantValue As [Optional](Of Object) = New [Optional](Of Object)()
            Dim isImplicit As Boolean = boundCaseBlock.WasCompilerGenerated

            ' Some bound nodes used by the boundCaseBlock.CaseStatement.CaseClauses are also going to be used in boundCaseBlock.CaseStatement.ConditionOpt.
            ' If we simply create another tree based on boundCaseBlock.CaseStatement.ConditionOpt, due to the caching we will end up with the same
            ' IOperation nodes in two trees, and two parents will compete for assigning itself as the parent - trouble. To avoid that, we simply use
            ' a new factory to create IOperation tree for the condition. Note that the condition tree is internal and regular consumers cannot get to
            ' the nodes it contains. At the moment, it is used only by CFG builder. The builder, rewrites all nodes anyway, it is producing a "forest"
            ' of different trees. So, there is really no chance of some external consumer getting confused by multiple explicit nodes tied to the same
            ' syntax.

            Return New VisualBasicLazySwitchCaseOperation(Me, boundCaseBlock, ImmutableArray(Of ILocalSymbol).Empty, _semanticModel, syntax, type, constantValue, isImplicit)
        End Function

        Private Function CreateBoundSimpleCaseClauseOperation(boundSimpleCaseClause As BoundSimpleCaseClause) As ISingleValueCaseClauseOperation
            Dim clauseValue = GetSingleValueCaseClauseValue(boundSimpleCaseClause)
            Dim syntax As SyntaxNode = boundSimpleCaseClause.Syntax
            Dim type As ITypeSymbol = Nothing
            Dim constantValue As [Optional](Of Object) = New [Optional](Of Object)()
            Dim isImplicit As Boolean = boundSimpleCaseClause.WasCompilerGenerated
            Return New VisualBasicLazySingleValueCaseClauseOperation(Me, clauseValue, label:=Nothing, _semanticModel, syntax, type, constantValue, isImplicit)
        End Function

        Private Function CreateBoundRangeCaseClauseOperation(boundRangeCaseClause As BoundRangeCaseClause) As IRangeCaseClauseOperation
            Dim syntax As SyntaxNode = boundRangeCaseClause.Syntax
            Dim type As ITypeSymbol = Nothing
            Dim constantValue As [Optional](Of Object) = New [Optional](Of Object)()
            Dim isImplicit As Boolean = boundRangeCaseClause.WasCompilerGenerated
            Return New VisualBasicLazyRangeCaseClauseOperation(Me, boundRangeCaseClause, _semanticModel, syntax, type, constantValue, isImplicit)
        End Function

        Private Function CreateBoundRelationalCaseClauseOperation(boundRelationalCaseClause As BoundRelationalCaseClause) As IRelationalCaseClauseOperation
            Dim valueExpression = GetSingleValueCaseClauseValue(boundRelationalCaseClause)
            Dim relation As BinaryOperatorKind = If(valueExpression IsNot Nothing, Helper.DeriveBinaryOperatorKind(boundRelationalCaseClause.OperatorKind, leftOpt:=Nothing), BinaryOperatorKind.None)
            Dim syntax As SyntaxNode = boundRelationalCaseClause.Syntax
            Dim type As ITypeSymbol = Nothing
            Dim constantValue As [Optional](Of Object) = New [Optional](Of Object)()
            Dim isImplicit As Boolean = boundRelationalCaseClause.WasCompilerGenerated
            Return New VisualBasicLazyRelationalCaseClauseOperation(Me, valueExpression, relation, _semanticModel, syntax, type, constantValue, isImplicit)
        End Function

        Private Function CreateBoundDoLoopStatementOperation(boundDoLoopStatement As BoundDoLoopStatement) As IWhileLoopOperation
            Dim locals As ImmutableArray(Of ILocalSymbol) = ImmutableArray(Of ILocalSymbol).Empty
            Dim continueLabel As ILabelSymbol = boundDoLoopStatement.ContinueLabel
            Dim exitLabel As ILabelSymbol = boundDoLoopStatement.ExitLabel
            Dim conditionIsTop As Boolean = boundDoLoopStatement.ConditionIsTop
            Dim conditionIsUntil As Boolean = boundDoLoopStatement.ConditionIsUntil
            Dim syntax As SyntaxNode = boundDoLoopStatement.Syntax
            Dim type As ITypeSymbol = Nothing
            Dim constantValue As [Optional](Of Object) = New [Optional](Of Object)()
            Dim isImplicit As Boolean = boundDoLoopStatement.WasCompilerGenerated
            Return New VisualBasicLazyWhileLoopOperation(Me, boundDoLoopStatement, locals, continueLabel, exitLabel, conditionIsTop, conditionIsUntil, _semanticModel, syntax, type, constantValue, isImplicit)
        End Function

        Private Function CreateBoundForToStatementOperation(boundForToStatement As BoundForToStatement) As IForToLoopOperation
            Dim locals As ImmutableArray(Of ILocalSymbol) = If(boundForToStatement.DeclaredOrInferredLocalOpt IsNot Nothing,
                ImmutableArray.Create(Of ILocalSymbol)(boundForToStatement.DeclaredOrInferredLocalOpt),
                ImmutableArray(Of ILocalSymbol).Empty)
            Dim continueLabel As ILabelSymbol = boundForToStatement.ContinueLabel
            Dim exitLabel As ILabelSymbol = boundForToStatement.ExitLabel
            Dim syntax As SyntaxNode = boundForToStatement.Syntax
            Dim type As ITypeSymbol = Nothing
            Dim constantValue As [Optional](Of Object) = New [Optional](Of Object)()
            Dim isImplicit As Boolean = boundForToStatement.WasCompilerGenerated
            Dim loopObj = If(boundForToStatement.ControlVariable.Type.IsObjectType,
                             New SynthesizedLocal(DirectCast(_semanticModel.GetEnclosingSymbol(boundForToStatement.Syntax.SpanStart), Symbol), boundForToStatement.ControlVariable.Type,
                                                  SynthesizedLocalKind.ForInitialValue, boundForToStatement.Syntax),
                             Nothing)

            Dim userDefinedInfo As ForToLoopOperationUserDefinedInfo = Nothing
            Dim operatorsOpt As BoundForToUserDefinedOperators = boundForToStatement.OperatorsOpt
            If operatorsOpt IsNot Nothing Then
                RecordParent(operatorsOpt.LeftOperandPlaceholder, boundForToStatement)
                RecordParent(operatorsOpt.RightOperandPlaceholder, boundForToStatement)
                userDefinedInfo = New ForToLoopOperationUserDefinedInfo(New Lazy(Of IBinaryOperation)(Function() DirectCast(Operation.SetParentOperation(Create(operatorsOpt.Addition), Nothing),
                                                                                                                            IBinaryOperation)),
                                                                        New Lazy(Of IBinaryOperation)(Function() DirectCast(Operation.SetParentOperation(Create(operatorsOpt.Subtraction), Nothing),
                                                                                                                            IBinaryOperation)),
                                                                        New Lazy(Of IOperation)(Function() Operation.SetParentOperation(Create(operatorsOpt.LessThanOrEqual), Nothing)),
                                                                        New Lazy(Of IOperation)(Function() Operation.SetParentOperation(Create(operatorsOpt.GreaterThanOrEqual), Nothing)))
            End If

            Return New VisualBasicLazyForToLoopOperation(Me, boundForToStatement, locals, boundForToStatement.Checked, (loopObj, userDefinedInfo), continueLabel, exitLabel,
                                                         _semanticModel, syntax, type, constantValue, isImplicit)
        End Function

        Friend Function GetForEachLoopOperationInfo(boundForEachStatement As BoundForEachStatement) As ForEachLoopOperationInfo
            Dim getEnumeratorArguments As ImmutableArray(Of BoundExpression) = Nothing
            Dim getEnumeratorDefaultArguments As BitVector = BitVector.Null
            Dim moveNextArguments As ImmutableArray(Of BoundExpression) = Nothing
            Dim moveNextDefaultArguments As BitVector = BitVector.Null
            Dim currentArguments As ImmutableArray(Of BoundExpression) = Nothing
            Dim currentDefaultArguments As BitVector = BitVector.Null
            Dim statementInfo As ForEachStatementInfo = MemberSemanticModel.GetForEachStatementInfo(boundForEachStatement,
                                                                                                    DirectCast(_semanticModel.Compilation, VisualBasicCompilation),
                                                                                                    getEnumeratorArguments, getEnumeratorDefaultArguments,
                                                                                                    moveNextArguments, moveNextDefaultArguments,
                                                                                                    currentArguments, currentDefaultArguments)
            Dim useSiteDiagnostics As HashSet(Of DiagnosticInfo) = Nothing
            Return New ForEachLoopOperationInfo(statementInfo.ElementType,
                                                     statementInfo.GetEnumeratorMethod,
                                                     statementInfo.CurrentProperty,
                                                     statementInfo.MoveNextMethod,
                                                     boundForEachStatement.EnumeratorInfo.NeedToDispose,
                                                     knownToImplementIDisposable:=boundForEachStatement.EnumeratorInfo.NeedToDispose AndAlso
                                                                                  boundForEachStatement.EnumeratorInfo.IsOrInheritsFromOrImplementsIDisposable,
                                                     statementInfo.CurrentConversion,
                                                     statementInfo.ElementConversion,
                                                     If(getEnumeratorArguments.IsDefaultOrEmpty, Nothing,
                                                        Operation.SetParentOperation(
                                                                           DeriveArguments(getEnumeratorArguments,
                                                                                           DirectCast(statementInfo.GetEnumeratorMethod, MethodSymbol).Parameters,
                                                                                           getEnumeratorDefaultArguments),
                                                                           Nothing)),
                                                     If(moveNextArguments.IsDefaultOrEmpty, Nothing,
                                                        Operation.SetParentOperation(
                                                                           DeriveArguments(moveNextArguments,
                                                                                           DirectCast(statementInfo.MoveNextMethod, MethodSymbol).Parameters,
                                                                                           moveNextDefaultArguments),
                                                                           Nothing)),
                                                     If(currentArguments.IsDefaultOrEmpty, Nothing,
                                                        Operation.SetParentOperation(
                                                                           DeriveArguments(currentArguments,
                                                                                           DirectCast(statementInfo.CurrentProperty, PropertySymbol).Parameters,
                                                                                           currentDefaultArguments),
                                                                           Nothing)))
        End Function

        Private Function CreateBoundForEachStatementOperation(boundForEachStatement As BoundForEachStatement) As IForEachLoopOperation
            Dim locals As ImmutableArray(Of ILocalSymbol) = If(boundForEachStatement.DeclaredOrInferredLocalOpt IsNot Nothing,
                ImmutableArray.Create(Of ILocalSymbol)(boundForEachStatement.DeclaredOrInferredLocalOpt),
                ImmutableArray(Of ILocalSymbol).Empty)
            Dim continueLabel As ILabelSymbol = boundForEachStatement.ContinueLabel
            Dim exitLabel As ILabelSymbol = boundForEachStatement.ExitLabel
            Dim syntax As SyntaxNode = boundForEachStatement.Syntax
            Dim type As ITypeSymbol = Nothing
            Dim constantValue As [Optional](Of Object) = New [Optional](Of Object)()
            Dim isImplicit As Boolean = boundForEachStatement.WasCompilerGenerated
            Return New VisualBasicLazyForEachLoopOperation(Me, boundForEachStatement, locals, continueLabel, exitLabel, _semanticModel, syntax, type, constantValue, isImplicit)
        End Function

        Friend Function CreateBoundControlVariableOperation(boundForStatement As BoundForStatement) As IOperation
            Dim localOpt As LocalSymbol = boundForStatement.DeclaredOrInferredLocalOpt
            Dim controlVariable As BoundExpression = boundForStatement.ControlVariable
            Return If(localOpt IsNot Nothing,
                New VariableDeclaratorOperation(localOpt, initializer:=Nothing, ignoredArguments:=ImmutableArray(Of IOperation).Empty, semanticModel:=_semanticModel, syntax:=controlVariable.Syntax, type:=Nothing, constantValue:=Nothing, isImplicit:=boundForStatement.WasCompilerGenerated),
                Create(controlVariable))
        End Function

        Private Function CreateBoundTryStatementOperation(boundTryStatement As BoundTryStatement) As ITryOperation
            Dim exitLabel As ILabelSymbol = boundTryStatement.ExitLabelOpt
            Dim syntax As SyntaxNode = boundTryStatement.Syntax
            Dim type As ITypeSymbol = Nothing
            Dim constantValue As [Optional](Of Object) = New [Optional](Of Object)()
            Dim isImplicit As Boolean = boundTryStatement.WasCompilerGenerated
            Return New VisualBasicLazyTryOperation(Me, boundTryStatement, exitLabel, _semanticModel, syntax, type, constantValue, isImplicit)
        End Function

        Friend Function CreateBoundCatchBlockExceptionDeclarationOrExpression(boundCatchBlock As BoundCatchBlock) As IOperation
            If boundCatchBlock.LocalOpt IsNot Nothing AndAlso
                        boundCatchBlock.ExceptionSourceOpt?.Kind = BoundKind.Local AndAlso
                        boundCatchBlock.LocalOpt Is DirectCast(boundCatchBlock.ExceptionSourceOpt, BoundLocal).LocalSymbol Then
                Return New VariableDeclaratorOperation(boundCatchBlock.LocalOpt, initializer:=Nothing, ignoredArguments:=ImmutableArray(Of IOperation).Empty, semanticModel:=_semanticModel, syntax:=boundCatchBlock.ExceptionSourceOpt.Syntax, type:=Nothing, constantValue:=Nothing, isImplicit:=False)
            Else
                Return Create(boundCatchBlock.ExceptionSourceOpt)
            End If
        End Function

        Private Function CreateBoundCatchBlockOperation(boundCatchBlock As BoundCatchBlock) As ICatchClauseOperation
            Dim exceptionType As ITypeSymbol = If(boundCatchBlock.ExceptionSourceOpt?.Type, DirectCast(_semanticModel.Compilation, VisualBasicCompilation).GetWellKnownType(WellKnownType.System_Exception))
            Dim locals As ImmutableArray(Of ILocalSymbol) = If(boundCatchBlock.LocalOpt IsNot Nothing,
                ImmutableArray.Create(Of ILocalSymbol)(boundCatchBlock.LocalOpt),
                ImmutableArray(Of ILocalSymbol).Empty)
            Dim syntax As SyntaxNode = boundCatchBlock.Syntax
            Dim type As ITypeSymbol = Nothing
            Dim constantValue As [Optional](Of Object) = New [Optional](Of Object)()
            Dim isImplicit As Boolean = boundCatchBlock.WasCompilerGenerated
            Return New VisualBasicLazyCatchClauseOperation(Me, boundCatchBlock, exceptionType, locals, _semanticModel, syntax, type, constantValue, isImplicit)
        End Function

        Private Function CreateBoundBlockOperation(boundBlock As BoundBlock) As IBlockOperation
            Dim locals As ImmutableArray(Of ILocalSymbol) = boundBlock.Locals.As(Of ILocalSymbol)()
            Dim syntax As SyntaxNode = boundBlock.Syntax
            Dim type As ITypeSymbol = Nothing
            Dim constantValue As [Optional](Of Object) = New [Optional](Of Object)()
            Dim isImplicit As Boolean = boundBlock.WasCompilerGenerated
            Return New VisualBasicLazyBlockOperation(Me, boundBlock, locals, _semanticModel, syntax, type, constantValue, isImplicit)
        End Function

        Private Function CreateBoundBadStatementOperation(boundBadStatement As BoundBadStatement) As IInvalidOperation
            Dim syntax As SyntaxNode = boundBadStatement.Syntax
            Dim type As ITypeSymbol = Nothing
            Dim constantValue As [Optional](Of Object) = New [Optional](Of Object)()

            ' if child has syntax node point to same syntax node as bad statement, then this invalid statement is implicit
            Dim isImplicit = boundBadStatement.WasCompilerGenerated
            If Not isImplicit Then
                For Each child In boundBadStatement.ChildBoundNodes
                    If child?.Syntax Is boundBadStatement.Syntax Then
                        isImplicit = True
                        Exit For
                    End If
                Next
            End If
            Return New VisualBasicLazyInvalidOperation(Me, boundBadStatement, _semanticModel, syntax, type, constantValue, isImplicit)
        End Function

        Private Function CreateBoundReturnStatementOperation(boundReturnStatement As BoundReturnStatement) As IReturnOperation
            Dim returnedValue As BoundNode = boundReturnStatement.ExpressionOpt
            Dim syntax As SyntaxNode = boundReturnStatement.Syntax
            Dim type As ITypeSymbol = Nothing
            Dim constantValue As [Optional](Of Object) = New [Optional](Of Object)()
            Dim isImplicit As Boolean = boundReturnStatement.WasCompilerGenerated OrElse IsEndSubOrFunctionStatement(syntax)
            Return New VisualBasicLazyReturnOperation(Me, returnedValue, OperationKind.Return, _semanticModel, syntax, type, constantValue, isImplicit)
        End Function

        Private Shared Function IsEndSubOrFunctionStatement(syntax As SyntaxNode) As Boolean
            Return TryCast(syntax.Parent, MethodBlockBaseSyntax)?.EndBlockStatement Is syntax OrElse
                   TryCast(syntax.Parent, MultiLineLambdaExpressionSyntax)?.EndSubOrFunctionStatement Is syntax
        End Function

        Private Function CreateBoundThrowStatementOperation(boundThrowStatement As BoundThrowStatement) As IThrowOperation
            Dim thrownObject As BoundNode = boundThrowStatement.ExpressionOpt
            Dim syntax As SyntaxNode = boundThrowStatement.Syntax
            Dim expressionType As ITypeSymbol = Nothing
            Dim constantValue As [Optional](Of Object) = New [Optional](Of Object)()
            Dim isImplicit As Boolean = boundThrowStatement.WasCompilerGenerated
            Return New VisualBasicLazyThrowOperation(Me, thrownObject, _semanticModel, syntax, expressionType, constantValue, isImplicit)
        End Function

        Private Function CreateBoundWhileStatementOperation(boundWhileStatement As BoundWhileStatement) As IWhileLoopOperation
            Dim locals As ImmutableArray(Of ILocalSymbol) = ImmutableArray(Of ILocalSymbol).Empty
            Dim continueLabel As ILabelSymbol = boundWhileStatement.ContinueLabel
            Dim exitLabel As ILabelSymbol = boundWhileStatement.ExitLabel
            Dim conditionIsTop As Boolean = True
            Dim conditionIsUntil As Boolean = False
            Dim syntax As SyntaxNode = boundWhileStatement.Syntax
            Dim type As ITypeSymbol = Nothing
            Dim constantValue As [Optional](Of Object) = New [Optional](Of Object)()
            Dim isImplicit As Boolean = boundWhileStatement.WasCompilerGenerated
            Return New VisualBasicLazyWhileLoopOperation(Me, boundWhileStatement, locals, continueLabel, exitLabel, conditionIsTop, conditionIsUntil, _semanticModel, syntax, type, constantValue, isImplicit)
        End Function

        Private Function CreateBoundDimStatementOperation(boundDimStatement As BoundDimStatement) As IVariableDeclarationGroupOperation
            Dim syntax As SyntaxNode = boundDimStatement.Syntax
            Dim type As ITypeSymbol = Nothing
            Dim constantValue As [Optional](Of Object) = New [Optional](Of Object)()
            Dim isImplicit As Boolean = boundDimStatement.WasCompilerGenerated
            Return New VisualBasicLazyVariableDeclarationGroupOperation(Me, boundDimStatement, _semanticModel, syntax, type, constantValue, isImplicit)
        End Function

        Private Function CreateBoundLocalDeclarationOperation(boundLocalDeclaration As BoundLocalDeclaration) As IVariableDeclarationGroupOperation
            Dim syntax As SyntaxNode = boundLocalDeclaration.Syntax
            Dim type As ITypeSymbol = Nothing
            Dim constantValue As [Optional](Of Object) = New [Optional](Of Object)()
            Debug.Assert(boundLocalDeclaration.WasCompilerGenerated)
            Dim isImplicit As Boolean = True
            Return New VisualBasicLazyVariableDeclarationGroupOperation(Me, boundLocalDeclaration, _semanticModel, syntax, type, constantValue, isImplicit)
        End Function

        Private Function CreateBoundYieldStatementOperation(boundYieldStatement As BoundYieldStatement) As IReturnOperation
            Dim returnedValue As BoundNode = boundYieldStatement.Expression
            Dim syntax As SyntaxNode = boundYieldStatement.Syntax
            Dim type As ITypeSymbol = Nothing
            Dim constantValue As [Optional](Of Object) = New [Optional](Of Object)()
            Dim isImplicit As Boolean = boundYieldStatement.WasCompilerGenerated
            Return New VisualBasicLazyReturnOperation(Me, returnedValue, OperationKind.YieldReturn, _semanticModel, syntax, type, constantValue, isImplicit)
        End Function

        Private Function CreateBoundLabelStatementOperation(boundLabelStatement As BoundLabelStatement) As ILabeledOperation
            Dim label As ILabelSymbol = boundLabelStatement.Label
            Dim statement As BoundNode = Nothing
            Dim syntax As SyntaxNode = boundLabelStatement.Syntax
            Dim type As ITypeSymbol = Nothing
            Dim constantValue As [Optional](Of Object) = New [Optional](Of Object)()
            Dim isImplicit As Boolean = boundLabelStatement.WasCompilerGenerated OrElse IsEndSubOrFunctionStatement(syntax)
            Return New VisualBasicLazyLabeledOperation(Me, statement, label, _semanticModel, syntax, type, constantValue, isImplicit)
        End Function

        Private Function CreateBoundGotoStatementOperation(boundGotoStatement As BoundGotoStatement) As IBranchOperation
            Dim target As ILabelSymbol = boundGotoStatement.Label
            Dim branchKind As BranchKind = BranchKind.GoTo
            Dim syntax As SyntaxNode = boundGotoStatement.Syntax
            Dim type As ITypeSymbol = Nothing
            Dim constantValue As [Optional](Of Object) = New [Optional](Of Object)()
            Dim isImplicit As Boolean = boundGotoStatement.WasCompilerGenerated
            Return New BranchOperation(target, branchKind, _semanticModel, syntax, type, constantValue, isImplicit)
        End Function

        Private Function CreateBoundContinueStatementOperation(boundContinueStatement As BoundContinueStatement) As IBranchOperation
            Dim target As ILabelSymbol = boundContinueStatement.Label
            Dim branchKind As BranchKind = BranchKind.Continue
            Dim syntax As SyntaxNode = boundContinueStatement.Syntax
            Dim type As ITypeSymbol = Nothing
            Dim constantValue As [Optional](Of Object) = New [Optional](Of Object)()
            Dim isImplicit As Boolean = boundContinueStatement.WasCompilerGenerated
            Return New BranchOperation(target, branchKind, _semanticModel, syntax, type, constantValue, isImplicit)
        End Function

        Private Function CreateBoundExitStatementOperation(boundExitStatement As BoundExitStatement) As IBranchOperation
            Dim target As ILabelSymbol = boundExitStatement.Label
            Dim branchKind As BranchKind = BranchKind.Break
            Dim syntax As SyntaxNode = boundExitStatement.Syntax
            Dim type As ITypeSymbol = Nothing
            Dim constantValue As [Optional](Of Object) = New [Optional](Of Object)()
            Dim isImplicit As Boolean = boundExitStatement.WasCompilerGenerated
            Return New BranchOperation(target, branchKind, _semanticModel, syntax, type, constantValue, isImplicit)
        End Function

        Private Function CreateBoundSyncLockStatementOperation(boundSyncLockStatement As BoundSyncLockStatement) As ILockOperation
            Dim legacyMode = _semanticModel.Compilation.CommonGetWellKnownTypeMember(WellKnownMember.System_Threading_Monitor__Enter2) Is Nothing
            Dim lockTakenSymbol As ILocalSymbol =
                If(legacyMode, Nothing,
                               New SynthesizedLocal(DirectCast(_semanticModel.GetEnclosingSymbol(boundSyncLockStatement.Syntax.SpanStart), Symbol),
                                                    DirectCast(_semanticModel.Compilation.GetSpecialType(SpecialType.System_Boolean), TypeSymbol),
                                                    SynthesizedLocalKind.LockTaken,
                                                    syntaxOpt:=boundSyncLockStatement.LockExpression.Syntax))
            Dim syntax As SyntaxNode = boundSyncLockStatement.Syntax
            Dim type As ITypeSymbol = Nothing
            Dim constantValue As [Optional](Of Object) = New [Optional](Of Object)()
            Dim isImplicit As Boolean = boundSyncLockStatement.WasCompilerGenerated
            Return New VisualBasicLazyLockOperation(Me, boundSyncLockStatement, lockTakenSymbol, _semanticModel, syntax, type, constantValue, isImplicit)
        End Function

        Private Function CreateBoundNoOpStatementOperation(boundNoOpStatement As BoundNoOpStatement) As IEmptyOperation
            Dim syntax As SyntaxNode = boundNoOpStatement.Syntax
            Dim type As ITypeSymbol = Nothing
            Dim constantValue As [Optional](Of Object) = New [Optional](Of Object)()
            Dim isImplicit As Boolean = boundNoOpStatement.WasCompilerGenerated
            Return New EmptyOperation(_semanticModel, syntax, type, constantValue, isImplicit)
        End Function

        Private Function CreateBoundStopStatementOperation(boundStopStatement As BoundStopStatement) As IStopOperation
            Dim syntax As SyntaxNode = boundStopStatement.Syntax
            Dim type As ITypeSymbol = Nothing
            Dim constantValue As [Optional](Of Object) = New [Optional](Of Object)()
            Dim isImplicit As Boolean = boundStopStatement.WasCompilerGenerated
            Return New StopOperation(_semanticModel, syntax, type, constantValue, isImplicit)
        End Function

        Private Function CreateBoundEndStatementOperation(boundEndStatement As BoundEndStatement) As IEndOperation
            Dim syntax As SyntaxNode = boundEndStatement.Syntax
            Dim type As ITypeSymbol = Nothing
            Dim constantValue As [Optional](Of Object) = New [Optional](Of Object)()
            Dim isImplicit As Boolean = boundEndStatement.WasCompilerGenerated
            Return New EndOperation(_semanticModel, syntax, type, constantValue, isImplicit)
        End Function

        Private Function CreateBoundWithStatementOperation(boundWithStatement As BoundWithStatement) As IWithOperation
            Dim syntax As SyntaxNode = boundWithStatement.Syntax
            Dim type As ITypeSymbol = Nothing
            Dim constantValue As [Optional](Of Object) = New [Optional](Of Object)()
            Dim isImplicit As Boolean = boundWithStatement.WasCompilerGenerated
            Return New VisualBasicLazyWithOperation(Me, boundWithStatement, _semanticModel, syntax, type, constantValue, isImplicit)
        End Function

        Friend Function CreateBoundUsingStatementResources(boundUsingStatement As BoundUsingStatement) As IOperation
            If Not boundUsingStatement.ResourceList.IsDefault Then
                Return GetUsingStatementDeclaration(boundUsingStatement.ResourceList, DirectCast(boundUsingStatement.Syntax, UsingBlockSyntax).UsingStatement)
            Else
                Return Create(boundUsingStatement.ResourceExpressionOpt)
            End If
        End Function

        Private Function CreateBoundUsingStatementOperation(boundUsingStatement As BoundUsingStatement) As IUsingOperation
            Dim locals As ImmutableArray(Of ILocalSymbol) = ImmutableArray(Of ILocalSymbol).CastUp(boundUsingStatement.Locals)
            Dim syntax As SyntaxNode = boundUsingStatement.Syntax
            Dim type As ITypeSymbol = Nothing
            Dim constantValue As [Optional](Of Object) = New [Optional](Of Object)()
            Dim isImplicit As Boolean = boundUsingStatement.WasCompilerGenerated
            Return New VisualBasicLazyUsingOperation(Me, boundUsingStatement, locals, _semanticModel, syntax, type, constantValue, isImplicit)
        End Function

        Private Function CreateBoundExpressionStatementOperation(boundExpressionStatement As BoundExpressionStatement) As IExpressionStatementOperation
            Dim expression As BoundNode = boundExpressionStatement.Expression
            Dim syntax As SyntaxNode = boundExpressionStatement.Syntax
            Dim type As ITypeSymbol = Nothing
            Dim constantValue As [Optional](Of Object) = New [Optional](Of Object)()
            Dim isImplicit As Boolean = boundExpressionStatement.WasCompilerGenerated
            Return New VisualBasicLazyExpressionStatementOperation(Me, expression, _semanticModel, syntax, type, constantValue, isImplicit)
        End Function

        Friend Function CreateBoundRaiseEventStatementEventReference(boundRaiseEventStatement As BoundRaiseEventStatement) As IEventReferenceOperation
            Dim eventInvocation = DirectCast(boundRaiseEventStatement.EventInvocation, BoundCall)
            Dim receiverOpt = eventInvocation.ReceiverOpt
            Dim eventReferenceSyntax = If(receiverOpt?.Syntax,
                                          If(TryCast(boundRaiseEventStatement.Syntax, RaiseEventStatementSyntax)?.Name,
                                             boundRaiseEventStatement.Syntax))
            Dim eventReferenceType As ITypeSymbol = boundRaiseEventStatement.EventSymbol.Type
            Dim eventReferenceConstantValue As [Optional](Of Object) = ConvertToOptional(receiverOpt?.ConstantValueOpt)
            ' EventReference in a raise event statement is never implicit. However, the way it is implemented, we don't get
            ' a "BoundEventAccess" for either field backed event or custom event, and the bound nodes we get are marked as
            ' generated by compiler. As a result, we have to explicitly set IsImplicit to false.
            Dim eventReferenceIsImplicit As Boolean = False

            If receiverOpt?.Kind = BoundKind.FieldAccess Then
                ' For raising a field backed event, we will only get a field access node in bound tree.
                Dim eventFieldAccess = DirectCast(receiverOpt, BoundFieldAccess)
                Debug.Assert(eventFieldAccess.FieldSymbol.AssociatedSymbol = boundRaiseEventStatement.EventSymbol)

                receiverOpt = eventFieldAccess.ReceiverOpt
            End If

            Return New VisualBasicLazyEventReferenceOperation(Me,
                                                              receiverOpt,
                                                              boundRaiseEventStatement.EventSymbol,
                                                              _semanticModel,
                                                              eventReferenceSyntax,
                                                              eventReferenceType,
                                                              eventReferenceConstantValue,
                                                              eventReferenceIsImplicit)
        End Function

        Private Function CreateBoundRaiseEventStatementOperation(boundRaiseEventStatement As BoundRaiseEventStatement) As IOperation
            Dim syntax As SyntaxNode = boundRaiseEventStatement.Syntax
            Dim type As ITypeSymbol = Nothing
            Dim constantValue As [Optional](Of Object) = New [Optional](Of Object)()
            Dim isImplicit As Boolean = boundRaiseEventStatement.WasCompilerGenerated

            Dim eventSymbol = boundRaiseEventStatement.EventSymbol
            Dim eventInvocation = TryCast(boundRaiseEventStatement.EventInvocation, BoundCall)

            ' Return an invalid statement for invalid raise event statement
            If eventInvocation Is Nothing OrElse (eventInvocation.ReceiverOpt Is Nothing AndAlso Not eventSymbol.IsShared) Then
                Debug.Assert(boundRaiseEventStatement.HasErrors)
                Return New VisualBasicLazyInvalidOperation(Me, boundRaiseEventStatement, _semanticModel, syntax, type, constantValue, isImplicit)
            End If

            Return New VisualBasicLazyRaiseEventOperation(Me, boundRaiseEventStatement, _semanticModel, syntax, type, constantValue, isImplicit)
        End Function

        Private Function CreateBoundAddHandlerStatementOperation(boundAddHandlerStatement As BoundAddHandlerStatement) As IExpressionStatementOperation
            Dim expression As IOperation = GetAddRemoveHandlerStatementExpression(boundAddHandlerStatement)
            Dim syntax As SyntaxNode = boundAddHandlerStatement.Syntax
            Dim type As ITypeSymbol = Nothing
            Dim constantValue As [Optional](Of Object) = New [Optional](Of Object)()
            Dim isImplicit As Boolean = boundAddHandlerStatement.WasCompilerGenerated
            Return New ExpressionStatementOperation(expression, _semanticModel, syntax, type, constantValue, isImplicit)
        End Function

        Private Function CreateBoundRemoveHandlerStatementOperation(boundRemoveHandlerStatement As BoundRemoveHandlerStatement) As IExpressionStatementOperation
            Dim expression As IOperation = GetAddRemoveHandlerStatementExpression(boundRemoveHandlerStatement)
            Dim syntax As SyntaxNode = boundRemoveHandlerStatement.Syntax
            Dim type As ITypeSymbol = Nothing
            Dim constantValue As [Optional](Of Object) = New [Optional](Of Object)()
            Dim isImplicit As Boolean = boundRemoveHandlerStatement.WasCompilerGenerated
            Return New ExpressionStatementOperation(expression, _semanticModel, syntax, type, constantValue, isImplicit)
        End Function

        Private Function CreateBoundTupleLiteralOperation(boundTupleLiteral As BoundTupleLiteral) As ITupleOperation
            Return CreateTupleOperation(boundTupleLiteral, boundTupleLiteral.Type)
        End Function

        Private Function CreateBoundConvertedTupleLiteralOperation(boundConvertedTupleLiteral As BoundConvertedTupleLiteral) As ITupleOperation
            Return CreateTupleOperation(boundConvertedTupleLiteral, boundConvertedTupleLiteral.NaturalTypeOpt)
        End Function

        Private Function CreateTupleOperation(boundTupleExpression As BoundTupleExpression, naturalType As ITypeSymbol) As ITupleOperation
            Dim syntax As SyntaxNode = boundTupleExpression.Syntax
            Dim type As ITypeSymbol = boundTupleExpression.Type
            Dim constantValue As [Optional](Of Object) = Nothing
            Dim isImplicit As Boolean = boundTupleExpression.WasCompilerGenerated
            Return New VisualBasicLazyTupleOperation(Me, boundTupleExpression, _semanticModel, syntax, type, naturalType, constantValue, isImplicit)
        End Function

        Private Function CreateBoundInterpolatedStringExpressionOperation(boundInterpolatedString As BoundInterpolatedStringExpression) As IInterpolatedStringOperation
            Dim syntax As SyntaxNode = boundInterpolatedString.Syntax
            Dim type As ITypeSymbol = boundInterpolatedString.Type
            Dim constantValue As [Optional](Of Object) = ConvertToOptional(boundInterpolatedString.ConstantValueOpt)
            Dim isImplicit As Boolean = boundInterpolatedString.WasCompilerGenerated
            Return New VisualBasicLazyInterpolatedStringOperation(Me, boundInterpolatedString, _semanticModel, syntax, type, constantValue, isImplicit)
        End Function

        Friend Function CreateBoundInterpolatedStringContentOperation(parts As ImmutableArray(Of BoundNode)) As ImmutableArray(Of IInterpolatedStringContentOperation)
            Dim builder = ArrayBuilder(Of IInterpolatedStringContentOperation).GetInstance(parts.Length)
            For Each part In parts
                If part.Kind = BoundKind.Interpolation Then
                    builder.Add(DirectCast(Create(part), IInterpolatedStringContentOperation))
                Else
                    builder.Add(CreateBoundInterpolatedStringTextOperation(DirectCast(part, BoundLiteral)))
                End If
            Next
            Return builder.ToImmutableAndFree()
        End Function

        Private Function CreateBoundInterpolationOperation(boundInterpolation As BoundInterpolation) As IInterpolationOperation
            Dim syntax As SyntaxNode = boundInterpolation.Syntax
            Dim type As ITypeSymbol = Nothing
            Dim constantValue As [Optional](Of Object) = Nothing
            Dim isImplicit As Boolean = boundInterpolation.WasCompilerGenerated
            Return New VisualBasicLazyInterpolationOperation(Me, boundInterpolation, _semanticModel, syntax, type, constantValue, isImplicit)
        End Function

        Private Function CreateBoundInterpolatedStringTextOperation(boundLiteral As BoundLiteral) As IInterpolatedStringTextOperation
            Dim syntax As SyntaxNode = boundLiteral.Syntax
            Dim type As ITypeSymbol = Nothing
            Dim constantValue As [Optional](Of Object) = Nothing
            Dim isImplicit As Boolean = boundLiteral.WasCompilerGenerated
            Return New VisualBasicLazyInterpolatedStringTextOperation(Me, boundLiteral, _semanticModel, syntax, type, constantValue, isImplicit)
        End Function

        Private Function CreateBoundAnonymousTypeCreationExpressionOperation(boundAnonymousTypeCreationExpression As BoundAnonymousTypeCreationExpression) As IAnonymousObjectCreationOperation
            Dim syntax As SyntaxNode = boundAnonymousTypeCreationExpression.Syntax
            Dim type As ITypeSymbol = boundAnonymousTypeCreationExpression.Type
            Dim constantValue As [Optional](Of Object) = ConvertToOptional(boundAnonymousTypeCreationExpression.ConstantValueOpt)
            Dim isImplicit As Boolean = boundAnonymousTypeCreationExpression.WasCompilerGenerated
            Return New VisualBasicLazyAnonymousObjectCreationOperation(Me, boundAnonymousTypeCreationExpression, _semanticModel, syntax, type, constantValue, isImplicit)
        End Function

        Private Function CreateBoundAnonymousTypePropertyAccessOperation(boundAnonymousTypePropertyAccess As BoundAnonymousTypePropertyAccess) As IPropertyReferenceOperation
            Dim [property] As IPropertySymbol = DirectCast(boundAnonymousTypePropertyAccess.ExpressionSymbol, IPropertySymbol)
            Dim instance As IOperation = CreateAnonymousTypePropertyAccessImplicitReceiverOperation([property], boundAnonymousTypePropertyAccess.Syntax.FirstAncestorOrSelf(Of AnonymousObjectCreationExpressionSyntax))
            Dim arguments = ImmutableArray(Of IArgumentOperation).Empty
            Dim syntax As SyntaxNode = boundAnonymousTypePropertyAccess.Syntax
            Dim type As ITypeSymbol = boundAnonymousTypePropertyAccess.Type
            Dim constantValue As [Optional](Of Object) = ConvertToOptional(boundAnonymousTypePropertyAccess.ConstantValueOpt)
            Dim isImplicit As Boolean = boundAnonymousTypePropertyAccess.WasCompilerGenerated
            Return New PropertyReferenceOperation([property], arguments, instance, _semanticModel, syntax, type, constantValue, isImplicit)
        End Function

        Private Function CreateAnonymousTypePropertyAccessImplicitReceiverOperation(propertySym As IPropertySymbol, syntax As SyntaxNode) As InstanceReferenceOperation
            Debug.Assert(propertySym IsNot Nothing)
            Debug.Assert(syntax IsNot Nothing)
            Return New InstanceReferenceOperation(
                InstanceReferenceKind.ImplicitReceiver,
                _semanticModel,
                syntax,
                propertySym.ContainingType,
                constantValue:=Nothing,
                isImplicit:=True)
        End Function

        Private Function CreateBoundQueryExpressionOperation(boundQueryExpression As BoundQueryExpression) As IOperation
            Dim expression As BoundNode = boundQueryExpression.LastOperator
            Dim syntax As SyntaxNode = boundQueryExpression.Syntax
            Dim type As ITypeSymbol = boundQueryExpression.Type
            Dim constantValue As [Optional](Of Object) = ConvertToOptional(boundQueryExpression.ConstantValueOpt)
            Dim isImplicit As Boolean = boundQueryExpression.WasCompilerGenerated
            Return New VisualBasicLazyTranslatedQueryOperation(Me, expression, _semanticModel, syntax, type, constantValue, isImplicit)
        End Function

        Private Function CreateBoundAggregateClauseOperation(boundAggregateClause As BoundAggregateClause) As IOperation
            If boundAggregateClause.CapturedGroupOpt Is Nothing Then
                ' This Aggregate clause has no special representation in the IOperation tree
                Return Create(boundAggregateClause.UnderlyingExpression)
            End If

            Debug.Assert(boundAggregateClause.GroupPlaceholderOpt IsNot Nothing)
            RecordParent(boundAggregateClause.GroupPlaceholderOpt, boundAggregateClause)

            Dim syntax As SyntaxNode = boundAggregateClause.Syntax
            Dim type As ITypeSymbol = boundAggregateClause.Type
            Dim constantValue As [Optional](Of Object) = ConvertToOptional(boundAggregateClause.ConstantValueOpt)
            Dim isImplicit As Boolean = boundAggregateClause.WasCompilerGenerated
            Return New VisualBasicLazyAggregateQueryOperation(Me, boundAggregateClause, _semanticModel, syntax, type, constantValue, isImplicit)
        End Function

        Private Function CreateBoundNullableIsTrueOperator(boundNullableIsTrueOperator As BoundNullableIsTrueOperator) As IOperation
            Dim syntax As SyntaxNode = boundNullableIsTrueOperator.Syntax
            Dim type As ITypeSymbol = boundNullableIsTrueOperator.Type
            Dim constantValue As [Optional](Of Object) = ConvertToOptional(boundNullableIsTrueOperator.ConstantValueOpt)
            Dim isImplicit As Boolean = boundNullableIsTrueOperator.WasCompilerGenerated

            Debug.Assert(boundNullableIsTrueOperator.Operand.Type.IsNullableOfBoolean() AndAlso boundNullableIsTrueOperator.Type.IsBooleanType())

            Dim method = DirectCast(DirectCast(_semanticModel.Compilation, VisualBasicCompilation).
                                        GetSpecialTypeMember(SpecialMember.System_Nullable_T_GetValueOrDefault), MethodSymbol)

            If method IsNot Nothing Then
                Return New VisualBasicLazyInvocationOperation(Me,
                                                              boundNullableIsTrueOperator,
                                                              method.AsMember(DirectCast(boundNullableIsTrueOperator.Operand.Type, NamedTypeSymbol)),
                                                              isVirtual:=False,
                                                              semanticModel:=_semanticModel,
                                                              syntax,
                                                              boundNullableIsTrueOperator.Type,
                                                              constantValue,
                                                              isImplicit)
            Else
                Return New VisualBasicLazyInvalidOperation(Me, boundNullableIsTrueOperator, _semanticModel, syntax, type, constantValue, isImplicit)
            End If
        End Function

        Private Function CreateBoundReDimOperation(boundRedimStatement As BoundRedimStatement) As IReDimOperation
            Dim preserve As Boolean = boundRedimStatement.Syntax.Kind = SyntaxKind.ReDimPreserveStatement
#If DEBUG Then
            For Each clause In boundRedimStatement.Clauses
                Debug.Assert(preserve = clause.Preserve)
            Next
#End If
            Dim syntax As SyntaxNode = boundRedimStatement.Syntax
            Dim type As ITypeSymbol = Nothing
            Dim constantValue As [Optional](Of Object) = Nothing
            Dim isImplicit As Boolean = boundRedimStatement.WasCompilerGenerated
            Return New VisualBasicLazyReDimOperation(Me, boundRedimStatement, preserve, _semanticModel, syntax, type, constantValue, isImplicit)
        End Function

        Private Function CreateBoundReDimClauseOperation(boundRedimClause As BoundRedimClause) As IReDimClauseOperation
            Dim syntax As SyntaxNode = boundRedimClause.Syntax
            Dim type As ITypeSymbol = Nothing
            Dim constantValue As [Optional](Of Object) = Nothing
            Dim isImplicit As Boolean = boundRedimClause.WasCompilerGenerated
            Return New VisualBasicLazyReDimClauseOperation(Me, boundRedimClause, _semanticModel, syntax, type, constantValue, isImplicit)
        End Function
    End Class
End Namespace


