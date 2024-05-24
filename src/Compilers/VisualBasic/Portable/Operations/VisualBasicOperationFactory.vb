' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Concurrent
Imports System.Collections.Immutable
Imports Microsoft.CodeAnalysis.PooledObjects
Imports Microsoft.CodeAnalysis.VisualBasic
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports ReferenceEqualityComparer = Roslyn.Utilities.ReferenceEqualityComparer

Namespace Microsoft.CodeAnalysis.Operations
    Partial Friend NotInheritable Class VisualBasicOperationFactory

        Private _lazyPlaceholderToParentMap As ConcurrentDictionary(Of BoundValuePlaceholderBase, BoundNode) = Nothing

        Private ReadOnly _semanticModel As SemanticModel

        Public Sub New(semanticModel As SemanticModel)
            _semanticModel = semanticModel
        End Sub

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

            ' A BoundUserDefined conversion is always the operand of a BoundConversion, and is handled
            ' by the BoundConversion creation. We should never receive one in this top level create call.
            Debug.Assert(boundNode.Kind <> BoundKind.UserDefinedConversion)

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
                Case BoundKind.TypeArguments
                    Return CreateBoundTypeArgumentsOperation(DirectCast(boundNode, BoundTypeArguments))
                Case BoundKind.Attribute
                    Return CreateBoundAttributeOperation(DirectCast(boundNode, BoundAttribute))

                Case BoundKind.AddressOfOperator,
                     BoundKind.ArrayLiteral,
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

                    Dim constantValue As ConstantValue = Nothing
                    Dim type As TypeSymbol = Nothing
                    Dim expression = TryCast(boundNode, BoundExpression)
                    If expression IsNot Nothing Then
                        constantValue = expression.ConstantValueOpt
                        type = expression.Type
                    End If
                    Dim isImplicit As Boolean = boundNode.WasCompilerGenerated
                    Dim children As ImmutableArray(Of IOperation) = GetIOperationChildren(boundNode)
                    Return New NoneOperation(children, _semanticModel, boundNode.Syntax, type, constantValue, isImplicit)

                Case Else
                    ' If you're hitting this because the IOperation test hook has failed, see
                    ' <roslyn-root>/docs/Compilers/IOperation Test Hook.md for instructions on how to fix.
                    Throw ExceptionUtilities.UnexpectedValue(boundNode.Kind)
            End Select
        End Function

        Public Function CreateFromArray(Of TBoundNode As BoundNode, TOperation As {Class, IOperation})(nodeArray As ImmutableArray(Of TBoundNode)) As ImmutableArray(Of TOperation)
            Dim builder = ArrayBuilder(Of TOperation).GetInstance(nodeArray.Length)
            For Each node In nodeArray
                builder.AddIfNotNull(DirectCast(Create(node), TOperation))
            Next

            Return builder.ToImmutableAndFree()
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
                Dim constantValue As ConstantValue = boundAssignmentOperator.ConstantValueOpt
                Dim isImplicit As Boolean = boundAssignmentOperator.WasCompilerGenerated
                Dim children As ImmutableArray(Of IOperation) = GetIOperationChildren(boundAssignmentOperator)
                Return New NoneOperation(children, _semanticModel, boundAssignmentOperator.Syntax, type:=Nothing, constantValue, isImplicit)
            ElseIf boundAssignmentOperator.LeftOnTheRightOpt IsNot Nothing Then
                Return CreateCompoundAssignment(boundAssignmentOperator)
            Else
                Dim target As IOperation = Create(boundAssignmentOperator.Left)
                Dim value As IOperation = Create(boundAssignmentOperator.Right)
                Dim isImplicit As Boolean = boundAssignmentOperator.WasCompilerGenerated
                Dim isRef As Boolean = False
                Dim syntax As SyntaxNode = boundAssignmentOperator.Syntax
                Dim type As ITypeSymbol = boundAssignmentOperator.Type
                Dim constantValue As ConstantValue = boundAssignmentOperator.ConstantValueOpt
                Return New SimpleAssignmentOperation(isRef, target, value, _semanticModel, syntax, type, constantValue, isImplicit)
            End If
        End Function

        Private Function CreateBoundMeReferenceOperation(boundMeReference As BoundMeReference) As IInstanceReferenceOperation
            Dim referenceKind As InstanceReferenceKind = InstanceReferenceKind.ContainingTypeInstance
            Dim syntax As SyntaxNode = boundMeReference.Syntax
            Dim type As ITypeSymbol = boundMeReference.Type
            Dim isImplicit As Boolean = boundMeReference.WasCompilerGenerated
            Return New InstanceReferenceOperation(referenceKind, _semanticModel, syntax, type, isImplicit)
        End Function

        Private Function CreateBoundMyBaseReferenceOperation(boundMyBaseReference As BoundMyBaseReference) As IInstanceReferenceOperation
            Dim referenceKind As InstanceReferenceKind = InstanceReferenceKind.ContainingTypeInstance
            Dim syntax As SyntaxNode = boundMyBaseReference.Syntax
            Dim type As ITypeSymbol = boundMyBaseReference.Type
            Dim isImplicit As Boolean = boundMyBaseReference.WasCompilerGenerated
            Return New InstanceReferenceOperation(referenceKind, _semanticModel, syntax, type, isImplicit)
        End Function

        Private Function CreateBoundMyClassReferenceOperation(boundMyClassReference As BoundMyClassReference) As IInstanceReferenceOperation
            Dim referenceKind As InstanceReferenceKind = InstanceReferenceKind.ContainingTypeInstance
            Dim syntax As SyntaxNode = boundMyClassReference.Syntax
            Dim type As ITypeSymbol = boundMyClassReference.Type
            Dim isImplicit As Boolean = boundMyClassReference.WasCompilerGenerated
            Return New InstanceReferenceOperation(referenceKind, _semanticModel, syntax, type, isImplicit)
        End Function

        Friend Function CreateBoundLiteralOperation(boundLiteral As BoundLiteral, Optional implicit As Boolean = False) As ILiteralOperation
            Dim syntax As SyntaxNode = boundLiteral.Syntax
            Dim type As ITypeSymbol = boundLiteral.Type
            Dim constantValue As ConstantValue = boundLiteral.ConstantValueOpt
            Dim isImplicit As Boolean = boundLiteral.WasCompilerGenerated OrElse implicit
            Return New LiteralOperation(_semanticModel, syntax, type, constantValue, isImplicit)
        End Function

        Private Function CreateBoundAwaitOperatorOperation(boundAwaitOperator As BoundAwaitOperator) As IAwaitOperation
            Dim awaitedValue As IOperation = Create(boundAwaitOperator.Operand)
            Dim syntax As SyntaxNode = boundAwaitOperator.Syntax
            Dim type As ITypeSymbol = boundAwaitOperator.Type
            Dim isImplicit As Boolean = boundAwaitOperator.WasCompilerGenerated
            Return New AwaitOperation(awaitedValue, _semanticModel, syntax, type, isImplicit)
        End Function

        Private Function CreateBoundNameOfOperatorOperation(boundNameOfOperator As BoundNameOfOperator) As INameOfOperation
            Dim argument As IOperation = Create(boundNameOfOperator.Argument)
            Dim syntax As SyntaxNode = boundNameOfOperator.Syntax
            Dim type As ITypeSymbol = boundNameOfOperator.Type
            Dim constantValue As ConstantValue = boundNameOfOperator.ConstantValueOpt
            Dim isImplicit As Boolean = boundNameOfOperator.WasCompilerGenerated
            Return New NameOfOperation(argument, _semanticModel, syntax, type, constantValue, isImplicit)
        End Function

        Private Function CreateBoundLambdaOperation(boundLambda As BoundLambda) As IAnonymousFunctionOperation
            Dim symbol As IMethodSymbol = boundLambda.LambdaSymbol
            Dim body As IBlockOperation = DirectCast(Create(boundLambda.Body), IBlockOperation)
            Dim syntax As SyntaxNode = boundLambda.Syntax
            Dim isImplicit As Boolean = boundLambda.WasCompilerGenerated
            Return New AnonymousFunctionOperation(symbol, body, _semanticModel, syntax, isImplicit)
        End Function

        Private Function CreateBoundCallOperation(boundCall As BoundCall) As IInvocationOperation
            Dim targetMethod As IMethodSymbol = boundCall.Method

            Dim isVirtual As Boolean =
                   targetMethod IsNot Nothing AndAlso
                   (targetMethod.IsVirtual OrElse targetMethod.IsAbstract OrElse targetMethod.IsOverride) AndAlso
                   If(boundCall.ReceiverOpt?.Kind <> BoundKind.MyBaseReference, False) AndAlso
                   If(boundCall.ReceiverOpt?.Kind <> BoundKind.MyClassReference, False)

            Dim boundReceiver As BoundExpression = If(boundCall.ReceiverOpt, boundCall.MethodGroupOpt?.ReceiverOpt)
            Dim receiver as IOperation = CreateReceiverOperation(boundReceiver, targetMethod)
            Dim arguments As ImmutableArray(Of IArgumentOperation) = DeriveArguments(boundCall)

            Dim syntax As SyntaxNode = boundCall.Syntax
            Dim type As ITypeSymbol = boundCall.Type
            Dim isImplicit As Boolean = boundCall.WasCompilerGenerated
            Return New InvocationOperation(targetMethod, constrainedToType:=Nothing, receiver, isVirtual, arguments, _semanticModel, syntax, type, isImplicit)
        End Function

        Private Function CreateBoundOmittedArgumentOperation(boundOmittedArgument As BoundOmittedArgument) As IOmittedArgumentOperation
            Dim syntax As SyntaxNode = boundOmittedArgument.Syntax
            Dim type As ITypeSymbol = boundOmittedArgument.Type
            Dim isImplicit As Boolean = boundOmittedArgument.WasCompilerGenerated
            Return New OmittedArgumentOperation(_semanticModel, syntax, type, isImplicit)
        End Function

        Private Function CreateBoundParenthesizedOperation(boundParenthesized As BoundParenthesized) As IParenthesizedOperation
            Dim operand As IOperation = Create(boundParenthesized.Expression)
            Dim syntax As SyntaxNode = boundParenthesized.Syntax
            Dim type As ITypeSymbol = boundParenthesized.Type
            Dim constantValue As ConstantValue = boundParenthesized.ConstantValueOpt
            Dim isImplicit As Boolean = boundParenthesized.WasCompilerGenerated
            Return New ParenthesizedOperation(operand, _semanticModel, syntax, type, constantValue, isImplicit)
        End Function

        Private Function CreateBoundArrayAccessOperation(boundArrayAccess As BoundArrayAccess) As IArrayElementReferenceOperation
            Dim arrayReference as IOperation = Create(boundArrayAccess.Expression)
            Dim indices = CreateFromArray(Of BoundExpression, IOperation)(boundArrayAccess.Indices)
            Dim syntax As SyntaxNode = boundArrayAccess.Syntax
            Dim type As ITypeSymbol = boundArrayAccess.Type
            Dim isImplicit As Boolean = boundArrayAccess.WasCompilerGenerated
            Return New ArrayElementReferenceOperation(arrayReference, indices, _semanticModel, syntax, type, isImplicit)
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
            Dim operand As IOperation = CreateBoundUnaryOperatorChild(boundUnaryOperator)
            Dim operatorKind As UnaryOperatorKind = Helper.DeriveUnaryOperatorKind(boundUnaryOperator.OperatorKind)
            Dim operatorMethod As IMethodSymbol = Nothing
            Dim syntax As SyntaxNode = boundUnaryOperator.Syntax
            Dim type As ITypeSymbol = boundUnaryOperator.Type
            Dim constantValue As ConstantValue = boundUnaryOperator.ConstantValueOpt
            Dim isLifted As Boolean = (boundUnaryOperator.OperatorKind And VisualBasic.UnaryOperatorKind.Lifted) <> 0
            Dim isChecked As Boolean = boundUnaryOperator.Checked
            Dim isImplicit As Boolean = boundUnaryOperator.WasCompilerGenerated
            Return New UnaryOperation(operatorKind, operand, isLifted, isChecked, operatorMethod, constrainedToType:=Nothing, _semanticModel, syntax, type, constantValue, isImplicit)
        End Function

        Private Function CreateBoundUserDefinedUnaryOperatorOperation(boundUserDefinedUnaryOperator As BoundUserDefinedUnaryOperator) As IUnaryOperation
            Dim operand As IOperation = CreateBoundUnaryOperatorChild(boundUserDefinedUnaryOperator)
            Dim operatorKind As UnaryOperatorKind = Helper.DeriveUnaryOperatorKind(boundUserDefinedUnaryOperator.OperatorKind)
            Dim operatorMethod As IMethodSymbol = TryGetOperatorMethod(boundUserDefinedUnaryOperator)
            Dim syntax As SyntaxNode = boundUserDefinedUnaryOperator.Syntax
            Dim type As ITypeSymbol = boundUserDefinedUnaryOperator.Type
            Dim constantValue As ConstantValue = boundUserDefinedUnaryOperator.ConstantValueOpt
            Dim isLifted As Boolean = (boundUserDefinedUnaryOperator.OperatorKind And VisualBasic.UnaryOperatorKind.Lifted) <> 0
            Dim isChecked As Boolean = False
            Dim isImplicit As Boolean = boundUserDefinedUnaryOperator.WasCompilerGenerated
            Return New UnaryOperation(operatorKind, operand, isLifted, isChecked, operatorMethod, constrainedToType:=Nothing, _semanticModel, syntax, type, constantValue, isImplicit)
        End Function

        Private Shared Function TryGetOperatorMethod(boundUserDefinedUnaryOperator As BoundUserDefinedUnaryOperator) As MethodSymbol
            Return If(boundUserDefinedUnaryOperator.UnderlyingExpression.Kind = BoundKind.Call, boundUserDefinedUnaryOperator.Call.Method, Nothing)
        End Function

        Friend Function CreateBoundBinaryOperatorChild(binaryOperator As BoundExpression, isLeft As Boolean) As IOperation
            Select Case binaryOperator.Kind
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

        Private Function CreateBoundBinaryOperatorOperation(boundBinaryOperator As BoundBinaryOperator) As IOperation
            ' Binary operators can be nested _many_ levels deep, and cause a stack overflow if we manually recurse.
            ' To solve this, we use a manual stack for the left side.
            Dim stack As ArrayBuilder(Of BoundBinaryOperator) = ArrayBuilder(Of BoundBinaryOperator).GetInstance()
            Dim currentBinary As BoundBinaryOperator = boundBinaryOperator

            Do
                stack.Push(currentBinary)
                currentBinary = TryCast(currentBinary.Left, BoundBinaryOperator)
            Loop While currentBinary IsNot Nothing

            Debug.Assert(stack.Count > 0)
            Dim left As IOperation = Nothing

            While stack.TryPop(currentBinary)
                left = If(left, Create(currentBinary.Left))
                Dim right As IOperation = Create(currentBinary.Right)

                Dim binaryOperatorInfo = GetBinaryOperatorInfo(currentBinary)
                Dim syntax As SyntaxNode = currentBinary.Syntax
                Dim type As ITypeSymbol = currentBinary.Type
                Dim constantValue As ConstantValue = currentBinary.ConstantValueOpt
                Dim isImplicit As Boolean = currentBinary.WasCompilerGenerated

                left = New BinaryOperation(binaryOperatorInfo.OperatorKind, left, right, binaryOperatorInfo.IsLifted,
                                           binaryOperatorInfo.IsChecked, binaryOperatorInfo.IsCompareText, binaryOperatorInfo.OperatorMethod, constrainedToType:=Nothing,
                                           unaryOperatorMethod:=Nothing, _semanticModel, syntax, type, constantValue, isImplicit)
            End While

            Debug.Assert(left IsNot Nothing AndAlso stack.Count = 0)
            stack.Free()
            Return left
        End Function

        Private Function CreateBoundUserDefinedBinaryOperatorOperation(boundUserDefinedBinaryOperator As BoundUserDefinedBinaryOperator) As IBinaryOperation
            Dim left As IOperation = CreateBoundBinaryOperatorChild(boundUserDefinedBinaryOperator, isLeft:=True)
            Dim right As IOperation = CreateBoundBinaryOperatorChild(boundUserDefinedBinaryOperator, isLeft:=False)
            Dim binaryOperatorInfo = GetUserDefinedBinaryOperatorInfo(boundUserDefinedBinaryOperator)
            Dim syntax As SyntaxNode = boundUserDefinedBinaryOperator.Syntax
            Dim type As ITypeSymbol = boundUserDefinedBinaryOperator.Type
            Dim constantValue As ConstantValue = boundUserDefinedBinaryOperator.ConstantValueOpt
            Dim isImplicit As Boolean = boundUserDefinedBinaryOperator.WasCompilerGenerated
            Return New BinaryOperation(binaryOperatorInfo.OperatorKind, left, right, binaryOperatorInfo.IsLifted,
                                       binaryOperatorInfo.IsChecked, binaryOperatorInfo.IsCompareText, binaryOperatorInfo.OperatorMethod, constrainedToType:=Nothing,
                                       unaryOperatorMethod:=Nothing, _semanticModel, syntax, type, constantValue, isImplicit)
        End Function

        Private Function CreateBoundBinaryConditionalExpressionOperation(boundBinaryConditionalExpression As BoundBinaryConditionalExpression) As ICoalesceOperation
            Dim value As IOperation = Create(boundBinaryConditionalExpression.TestExpression)
            Dim whenFalse As IOperation = Create(boundBinaryConditionalExpression.ElseExpression)
            Dim syntax As SyntaxNode = boundBinaryConditionalExpression.Syntax
            Dim type As ITypeSymbol = boundBinaryConditionalExpression.Type
            Dim constantValue As ConstantValue = boundBinaryConditionalExpression.ConstantValueOpt
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

            Return New CoalesceOperation(value, whenFalse, valueConversion, _semanticModel, syntax, type, constantValue, isImplicit)
        End Function

        Private Function CreateBoundUserDefinedShortCircuitingOperatorOperation(boundUserDefinedShortCircuitingOperator As BoundUserDefinedShortCircuitingOperator) As IBinaryOperation
            Dim left As IOperation = CreateBoundBinaryOperatorChild(boundUserDefinedShortCircuitingOperator, isLeft:=True)
            Dim right As IOperation = CreateBoundBinaryOperatorChild(boundUserDefinedShortCircuitingOperator, isLeft:=False)
            Dim bitwiseOperator As BoundUserDefinedBinaryOperator = boundUserDefinedShortCircuitingOperator.BitwiseOperator
            Dim binaryOperatorInfo As BinaryOperatorInfo = GetUserDefinedBinaryOperatorInfo(bitwiseOperator)
            Dim operatorKind As BinaryOperatorKind = If(binaryOperatorInfo.OperatorKind = BinaryOperatorKind.And, BinaryOperatorKind.ConditionalAnd, BinaryOperatorKind.ConditionalOr)

            Dim syntax As SyntaxNode = boundUserDefinedShortCircuitingOperator.Syntax
            Dim type As ITypeSymbol = boundUserDefinedShortCircuitingOperator.Type
            Dim constantValue As ConstantValue = boundUserDefinedShortCircuitingOperator.ConstantValueOpt
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

            Return New BinaryOperation(operatorKind, left, right, binaryOperatorInfo.IsLifted, isChecked, isCompareText,
                                       binaryOperatorInfo.OperatorMethod, constrainedToType:=Nothing, unaryOperatorMethod, _semanticModel, syntax, type, constantValue, isImplicit)
        End Function

        Private Function CreateBoundBadExpressionOperation(boundBadExpression As BoundBadExpression) As IInvalidOperation
            Dim syntax As SyntaxNode = boundBadExpression.Syntax
            ' We match semantic model here: If the Then expression IsMissing, we have a null type, rather than the ErrorType Of the bound node.
            Dim type As ITypeSymbol = If(syntax.IsMissing, Nothing, boundBadExpression.Type)
            Dim constantValue As ConstantValue = boundBadExpression.ConstantValueOpt

            ' if child has syntax node point to same syntax node as bad expression, then this invalid expression Is implicit
            Dim isImplicit = boundBadExpression.WasCompilerGenerated OrElse boundBadExpression.ChildBoundNodes.Any(Function(e) e?.Syntax Is boundBadExpression.Syntax)
            Dim children = CreateFromArray(Of BoundExpression, IOperation)(boundBadExpression.ChildBoundNodes)
            Return New InvalidOperation(children, _semanticModel, syntax, type, constantValue, isImplicit)
        End Function

        Private Function CreateBoundTypeArgumentsOperation(boundTypeArguments As BoundTypeArguments) As IInvalidOperation
            ' This can occur in scenarios involving latebound member accesses in Strict mode, such as
            ' element.UnresolvedMember(Of String)
            ' The BadExpression has 2 children in this case: the receiver, and the type arguments.
            ' Just create an invalid operation to represent the node, as it won't ever be surfaced in good code.

            Dim syntax As SyntaxNode = boundTypeArguments.Syntax
            ' Match GetTypeInfo behavior for the syntax
            Dim type As ITypeSymbol = Nothing
            Dim constantValue As ConstantValue = boundTypeArguments.ConstantValueOpt
            Dim isImplicit As Boolean = boundTypeArguments.WasCompilerGenerated
            Dim children As ImmutableArray(Of IOperation) = ImmutableArray(Of IOperation).Empty

            Return New InvalidOperation(children, _semanticModel, syntax, type, constantValue, isImplicit)
        End Function

        Private Function CreateBoundAttributeOperation(boundAttribute As BoundAttribute) As IAttributeOperation
            Dim isAttributeImplicit = boundAttribute.WasCompilerGenerated
            If boundAttribute.Constructor Is Nothing OrElse boundAttribute.ConstructorArguments.Length <> boundAttribute.Constructor.ParameterCount Then
                Dim invalidOperation = OperationFactory.CreateInvalidOperation(_semanticModel, boundAttribute.Syntax, GetIOperationChildren(boundAttribute), isImplicit:=True)
                Return New AttributeOperation(invalidOperation, _semanticModel, boundAttribute.Syntax, isAttributeImplicit)
            End If

            Dim initializer As ObjectOrCollectionInitializerOperation = Nothing
            If Not boundAttribute.NamedArguments.IsEmpty Then
                Dim namedArguments = CreateFromArray(Of BoundExpression, IOperation)(boundAttribute.NamedArguments)
                initializer = New ObjectOrCollectionInitializerOperation(namedArguments, _semanticModel, boundAttribute.Syntax, boundAttribute.Type, isImplicit:=True)
            End If

            Dim objectCreationOperation = New ObjectCreationOperation(boundAttribute.Constructor, initializer, DeriveArguments(boundAttribute), _semanticModel, boundAttribute.Syntax, boundAttribute.Type, boundAttribute.ConstantValueOpt, isImplicit:=True)
            Return New AttributeOperation(objectCreationOperation, _semanticModel, boundAttribute.Syntax, isAttributeImplicit)
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
            Dim constantValue As ConstantValue = boundConversionOrCast.ConstantValueOpt
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
                Return New DelegateCreationOperation(conversionInfo.Operation, _semanticModel, syntax, type, isImplicit)
            Else
                Return New ConversionOperation(conversionInfo.Operation, conversion, isTryCast, isChecked, _semanticModel, syntax, type, constantValue, isImplicit)
            End If
        End Function

        Private Function CreateBoundDelegateCreationExpressionOperation(boundDelegateCreationExpression As BoundDelegateCreationExpression) As IDelegateCreationOperation
            Dim target As IOperation = CreateBoundDelegateCreationExpressionChildOperation(boundDelegateCreationExpression)
            Dim syntax As SyntaxNode = boundDelegateCreationExpression.Syntax
            Dim type As ITypeSymbol = boundDelegateCreationExpression.Type

            ' The operand for this is going to be using the same syntax node as this, and since that node can be Explicit, this node cannot be.
            Dim isImplicit As Boolean = True

            Return New DelegateCreationOperation(target, _semanticModel, syntax, type, isImplicit)
        End Function

        Friend Function CreateBoundDelegateCreationExpressionChildOperation(boundDelegateCreationExpression As BoundDelegateCreationExpression) As IMethodReferenceOperation
            Dim method As IMethodSymbol = boundDelegateCreationExpression.Method
            Dim isVirtual As Boolean = method IsNot Nothing AndAlso
                                               (method.IsAbstract OrElse method.IsOverride OrElse method.IsVirtual) AndAlso
                                               Not boundDelegateCreationExpression.SuppressVirtualCalls

            Dim receiverOpt As IOperation = CreateReceiverOperation(
                If(boundDelegateCreationExpression.ReceiverOpt, boundDelegateCreationExpression.MethodGroupOpt?.ReceiverOpt),
                method)

            ' The compiler creates a BoundDelegateCreationExpression node for the AddressOf expression, and that's the node we want to use for the operand
            ' of the IDelegateCreationExpression parent
            Dim syntax As SyntaxNode = boundDelegateCreationExpression.Syntax
            Dim type As ITypeSymbol = Nothing
            Dim isImplicit As Boolean = boundDelegateCreationExpression.WasCompilerGenerated
            Return New MethodReferenceOperation(method, constrainedToType:=Nothing, isVirtual, receiverOpt, _semanticModel, syntax, type, isImplicit)
        End Function

        Private Function CreateBoundTernaryConditionalExpressionOperation(boundTernaryConditionalExpression As BoundTernaryConditionalExpression) As IConditionalOperation
            Dim condition As IOperation = Create(boundTernaryConditionalExpression.Condition)
            Dim whenTrue As IOperation = Create(boundTernaryConditionalExpression.WhenTrue)
            Dim whenFalse As IOperation = Create(boundTernaryConditionalExpression.WhenFalse)
            Dim syntax As SyntaxNode = boundTernaryConditionalExpression.Syntax
            Dim type As ITypeSymbol = boundTernaryConditionalExpression.Type
            Dim constantValue As ConstantValue = boundTernaryConditionalExpression.ConstantValueOpt
            Dim isImplicit As Boolean = boundTernaryConditionalExpression.WasCompilerGenerated
            Dim isRef As Boolean = False
            Return New ConditionalOperation(condition, whenTrue, whenFalse, isRef, _semanticModel, syntax, type, constantValue, isImplicit)
        End Function

        Private Function CreateBoundTypeOfOperation(boundTypeOf As BoundTypeOf) As IIsTypeOperation
            Dim valueOperand = Create(boundTypeOf.Operand)
            Dim typeOperand As ITypeSymbol = boundTypeOf.TargetType
            Dim isNegated As Boolean = boundTypeOf.IsTypeOfIsNotExpression
            Dim syntax As SyntaxNode = boundTypeOf.Syntax
            Dim type As ITypeSymbol = boundTypeOf.Type
            Dim isImplicit As Boolean = boundTypeOf.WasCompilerGenerated
            Return New IsTypeOperation(valueOperand, typeOperand, isNegated, _semanticModel, syntax, type, isImplicit)
        End Function

        Private Function CreateBoundGetTypeOperation(boundGetType As BoundGetType) As ITypeOfOperation
            Dim typeOperand As ITypeSymbol = boundGetType.SourceType.Type
            Dim syntax As SyntaxNode = boundGetType.Syntax
            Dim type As ITypeSymbol = boundGetType.Type
            Dim isImplicit As Boolean = boundGetType.WasCompilerGenerated
            Return New TypeOfOperation(typeOperand, _semanticModel, syntax, type, isImplicit)
        End Function

        Private Function CreateBoundLateInvocationOperation(boundLateInvocation As BoundLateInvocation) As IOperation
            Dim operation As IOperation = Create(boundLateInvocation.Member)
            Dim arguments As ImmutableArray(Of IOperation) = CreateFromArray(Of BoundExpression, IOperation)(boundLateInvocation.ArgumentsOpt)
            Dim argumentNames As ImmutableArray(Of String) = boundLateInvocation.ArgumentNamesOpt
            Dim argumentRefKinds As ImmutableArray(Of RefKind) = Nothing
            Dim syntax As SyntaxNode = boundLateInvocation.Syntax
            Dim type As ITypeSymbol = boundLateInvocation.Type
            Dim isImplicit As Boolean = boundLateInvocation.WasCompilerGenerated
            Return New DynamicInvocationOperation(operation, arguments, argumentNames, argumentRefKinds, _semanticModel, syntax, type, isImplicit)
        End Function

        Private Function CreateBoundObjectCreationExpressionOperation(boundObjectCreationExpression As BoundObjectCreationExpression) As IObjectCreationOperation
            Debug.Assert(boundObjectCreationExpression.ConstructorOpt IsNot Nothing OrElse boundObjectCreationExpression.Arguments.IsEmpty())
            Dim constructor As IMethodSymbol = boundObjectCreationExpression.ConstructorOpt
            Dim initializer As IObjectOrCollectionInitializerOperation = DirectCast(Create(boundObjectCreationExpression.InitializerOpt), IObjectOrCollectionInitializerOperation)
            Dim arguments as ImmutableArray(Of IArgumentOperation) = DeriveArguments(boundObjectCreationExpression)

            Dim syntax As SyntaxNode = boundObjectCreationExpression.Syntax
            Dim type As ITypeSymbol = boundObjectCreationExpression.Type
            Dim constantValue As ConstantValue = boundObjectCreationExpression.ConstantValueOpt
            Dim isImplicit As Boolean = boundObjectCreationExpression.WasCompilerGenerated
            Return New ObjectCreationOperation(constructor, initializer, arguments, _semanticModel, syntax, type, constantValue, isImplicit)
        End Function

        Private Function CreateBoundObjectInitializerExpressionOperation(boundObjectInitializerExpression As BoundObjectInitializerExpression) As IObjectOrCollectionInitializerOperation
            Dim initializers As ImmutableArray(Of IOperation) = CreateFromArray(Of BoundExpression, IOperation)(boundObjectInitializerExpression.Initializers)
            Dim syntax As SyntaxNode = boundObjectInitializerExpression.Syntax
            Dim type As ITypeSymbol = boundObjectInitializerExpression.Type
            Dim isImplicit As Boolean = boundObjectInitializerExpression.WasCompilerGenerated
            Return New ObjectOrCollectionInitializerOperation(initializers, _semanticModel, syntax, type, isImplicit)
        End Function

        Private Function CreateBoundCollectionInitializerExpressionOperation(boundCollectionInitializerExpression As BoundCollectionInitializerExpression) As IObjectOrCollectionInitializerOperation
            Dim initializers As ImmutableArray(Of IOperation) = CreateFromArray(Of BoundExpression, IOperation)(boundCollectionInitializerExpression.Initializers)
            Dim syntax As SyntaxNode = boundCollectionInitializerExpression.Syntax
            Dim type As ITypeSymbol = boundCollectionInitializerExpression.Type
            Dim isImplicit As Boolean = boundCollectionInitializerExpression.WasCompilerGenerated
            Return New ObjectOrCollectionInitializerOperation(initializers, _semanticModel, syntax, type, isImplicit)
        End Function

        Private Function CreateBoundNewTOperation(boundNewT As BoundNewT) As ITypeParameterObjectCreationOperation
            Dim initializer As IObjectOrCollectionInitializerOperation = DirectCast(Create(boundNewT.InitializerOpt), IObjectOrCollectionInitializerOperation)
            Dim syntax As SyntaxNode = boundNewT.Syntax
            Dim type As ITypeSymbol = boundNewT.Type
            Dim isImplicit As Boolean = boundNewT.WasCompilerGenerated
            Return New TypeParameterObjectCreationOperation(initializer, _semanticModel, syntax, type, isImplicit)
        End Function

        Private Function CreateNoPiaObjectCreationExpressionOperation(creation As BoundNoPiaObjectCreationExpression) As INoPiaObjectCreationOperation
            Dim initializer As IObjectOrCollectionInitializerOperation = DirectCast(Create(creation.InitializerOpt), IObjectOrCollectionInitializerOperation)
            Dim syntax As SyntaxNode = creation.Syntax
            Dim type As ITypeSymbol = creation.Type
            Dim isImplicit As Boolean = creation.WasCompilerGenerated
            Return New NoPiaObjectCreationOperation(initializer, _semanticModel, syntax, type, isImplicit)
        End Function

        Private Function CreateBoundArrayCreationOperation(boundArrayCreation As BoundArrayCreation) As IArrayCreationOperation
            Dim dimensionSizes As ImmutableArray(Of IOperation) = CreateFromArray(Of BoundExpression, IOperation)(boundArrayCreation.Bounds)
            Dim initializer As IArrayInitializerOperation = DirectCast(Create(boundArrayCreation.InitializerOpt), IArrayInitializerOperation)
            Dim syntax As SyntaxNode = boundArrayCreation.Syntax
            Dim type As ITypeSymbol = boundArrayCreation.Type
            Dim isImplicit As Boolean = boundArrayCreation.WasCompilerGenerated
            Return New ArrayCreationOperation(dimensionSizes, initializer, _semanticModel, syntax, type, isImplicit)
        End Function

        Private Function CreateBoundArrayInitializationOperation(boundArrayInitialization As BoundArrayInitialization) As IArrayInitializerOperation
            Dim elementValues As ImmutableArray(Of IOperation) = CreateFromArray(Of BoundExpression, IOperation)(boundArrayInitialization.Initializers)
            Dim syntax As SyntaxNode = boundArrayInitialization.Syntax
            Dim isImplicit As Boolean = boundArrayInitialization.WasCompilerGenerated
            Return New ArrayInitializerOperation(elementValues, _semanticModel, syntax, isImplicit)
        End Function

        Private Function CreateBoundPropertyAccessOperation(boundPropertyAccess As BoundPropertyAccess) As IPropertyReferenceOperation
            Dim [property] As IPropertySymbol = boundPropertyAccess.PropertySymbol
            Dim instance As IOperation = CreateReceiverOperation(
                If(boundPropertyAccess.ReceiverOpt, boundPropertyAccess.PropertyGroupOpt?.ReceiverOpt),
                [property])
            Dim arguments as ImmutableArray(Of IArgumentOperation) = DeriveArguments(boundPropertyAccess)

            Dim syntax As SyntaxNode = boundPropertyAccess.Syntax
            Dim type As ITypeSymbol = boundPropertyAccess.Type
            Dim isImplicit As Boolean = boundPropertyAccess.WasCompilerGenerated
            Return New PropertyReferenceOperation([property], constrainedToType:=Nothing, arguments, instance, _semanticModel, syntax, type, isImplicit)
        End Function

        Private Function CreateBoundWithLValueExpressionPlaceholder(boundWithLValueExpressionPlaceholder As BoundWithLValueExpressionPlaceholder) As IInstanceReferenceOperation
            Dim referenceKind As InstanceReferenceKind = InstanceReferenceKind.ImplicitReceiver
            Dim syntax As SyntaxNode = boundWithLValueExpressionPlaceholder.Syntax
            Dim type As ITypeSymbol = boundWithLValueExpressionPlaceholder.Type
            Dim isImplicit As Boolean = boundWithLValueExpressionPlaceholder.WasCompilerGenerated
            Return New InstanceReferenceOperation(referenceKind, _semanticModel, syntax, type, isImplicit)
        End Function

        Private Function CreateBoundWithRValueExpressionPlaceholder(boundWithRValueExpressionPlaceholder As BoundWithRValueExpressionPlaceholder) As IInstanceReferenceOperation
            Dim referenceKind As InstanceReferenceKind = InstanceReferenceKind.ImplicitReceiver
            Dim syntax As SyntaxNode = boundWithRValueExpressionPlaceholder.Syntax
            Dim type As ITypeSymbol = boundWithRValueExpressionPlaceholder.Type
            Dim isImplicit As Boolean = boundWithRValueExpressionPlaceholder.WasCompilerGenerated
            Return New InstanceReferenceOperation(referenceKind, _semanticModel, syntax, type, isImplicit)
        End Function

        Private Function CreateBoundEventAccessOperation(boundEventAccess As BoundEventAccess) As IEventReferenceOperation
            Dim [event] As IEventSymbol = boundEventAccess.EventSymbol
            Dim instance As IOperation = CreateReceiverOperation(boundEventAccess.ReceiverOpt, [event])

            Dim syntax As SyntaxNode = boundEventAccess.Syntax
            Dim type As ITypeSymbol = boundEventAccess.Type
            Dim isImplicit As Boolean = boundEventAccess.WasCompilerGenerated
            Return New EventReferenceOperation([event], constrainedToType:=Nothing, instance, _semanticModel, syntax, type, isImplicit)
        End Function

        Private Function CreateBoundFieldAccessOperation(boundFieldAccess As BoundFieldAccess) As IFieldReferenceOperation
            Dim field As IFieldSymbol = boundFieldAccess.FieldSymbol
            Dim isDeclaration As Boolean = False
            Dim instance As IOperation = CreateReceiverOperation(boundFieldAccess.ReceiverOpt, field)

            Dim syntax As SyntaxNode = boundFieldAccess.Syntax
            Dim type As ITypeSymbol = boundFieldAccess.Type
            Dim constantValue As ConstantValue = boundFieldAccess.ConstantValueOpt
            Dim isImplicit As Boolean = boundFieldAccess.WasCompilerGenerated
            Return New FieldReferenceOperation(field, isDeclaration, instance, _semanticModel, syntax, type, constantValue, isImplicit)
        End Function

        Private Function CreateBoundConditionalAccessOperation(boundConditionalAccess As BoundConditionalAccess) As IConditionalAccessOperation
            RecordParent(boundConditionalAccess.Placeholder, boundConditionalAccess)
            Dim operation As IOperation = Create(boundConditionalAccess.Receiver)
            Dim whenNotNull As IOperation = Create(boundConditionalAccess.AccessExpression)
            Dim syntax As SyntaxNode = boundConditionalAccess.Syntax
            Dim type As ITypeSymbol = boundConditionalAccess.Type
            Dim isImplicit As Boolean = boundConditionalAccess.WasCompilerGenerated
            Return New ConditionalAccessOperation(operation, whenNotNull, _semanticModel, syntax, type, isImplicit)
        End Function

        Private Function CreateBoundConditionalAccessReceiverPlaceholderOperation(boundConditionalAccessReceiverPlaceholder As BoundConditionalAccessReceiverPlaceholder) As IConditionalAccessInstanceOperation
            Dim syntax As SyntaxNode = boundConditionalAccessReceiverPlaceholder.Syntax
            Dim type As ITypeSymbol = boundConditionalAccessReceiverPlaceholder.Type
            Dim isImplicit As Boolean = boundConditionalAccessReceiverPlaceholder.WasCompilerGenerated
            Return New ConditionalAccessInstanceOperation(_semanticModel, syntax, type, isImplicit)
        End Function

        Private Function CreateBoundParameterOperation(boundParameter As BoundParameter) As IParameterReferenceOperation
            Dim parameter As IParameterSymbol = boundParameter.ParameterSymbol
            Dim syntax As SyntaxNode = boundParameter.Syntax
            Dim type As ITypeSymbol = boundParameter.Type
            Dim isImplicit As Boolean = boundParameter.WasCompilerGenerated
            Return New ParameterReferenceOperation(parameter, _semanticModel, syntax, type, isImplicit)
        End Function

        Private Function CreateBoundLocalOperation(boundLocal As BoundLocal) As IOperation
            Dim local As ILocalSymbol = boundLocal.LocalSymbol
            Dim isDeclaration As Boolean = False
            Dim syntax As SyntaxNode = boundLocal.Syntax
            Dim type As ITypeSymbol = boundLocal.Type
            Dim constantValue As ConstantValue = boundLocal.ConstantValueOpt
            Dim isImplicit As Boolean = boundLocal.WasCompilerGenerated
            Return New LocalReferenceOperation(local, isDeclaration, _semanticModel, syntax, type, constantValue, isImplicit)
        End Function

        Private Function CreateBoundLateMemberAccessOperation(boundLateMemberAccess As BoundLateMemberAccess) As IDynamicMemberReferenceOperation
            Debug.Assert(boundLateMemberAccess.ReceiverOpt Is Nothing OrElse boundLateMemberAccess.ReceiverOpt.Kind <> BoundKind.TypeExpression)

            Dim instance As IOperation = Create(boundLateMemberAccess.ReceiverOpt)
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
            Dim isImplicit As Boolean = boundLateMemberAccess.WasCompilerGenerated
            Return New DynamicMemberReferenceOperation(instance, memberName, typeArguments, containingType, _semanticModel, syntax, type, isImplicit)
        End Function

        Private Function CreateBoundFieldInitializerOperation(boundFieldInitializer As BoundFieldInitializer) As IFieldInitializerOperation
            Dim initializedFields As ImmutableArray(Of IFieldSymbol) = boundFieldInitializer.InitializedFields.As(Of IFieldSymbol)
            Dim value As IOperation = Create(boundFieldInitializer.InitialValue)
            Dim syntax As SyntaxNode = boundFieldInitializer.Syntax
            Dim isImplicit As Boolean = boundFieldInitializer.WasCompilerGenerated
            Return New FieldInitializerOperation(initializedFields, ImmutableArray(Of ILocalSymbol).Empty, value, _semanticModel, syntax, isImplicit)
        End Function

        Private Function CreateBoundPropertyInitializerOperation(boundPropertyInitializer As BoundPropertyInitializer) As IPropertyInitializerOperation
            Dim initializedProperties As ImmutableArray(Of IPropertySymbol) = boundPropertyInitializer.InitializedProperties.As(Of IPropertySymbol)
            Dim value As IOperation = Create(boundPropertyInitializer.InitialValue)
            Dim syntax As SyntaxNode = boundPropertyInitializer.Syntax
            Dim isImplicit As Boolean = boundPropertyInitializer.WasCompilerGenerated
            Return New PropertyInitializerOperation(initializedProperties, ImmutableArray(Of ILocalSymbol).Empty, value, _semanticModel, syntax, isImplicit)
        End Function

        Private Function CreateBoundParameterEqualsValueOperation(boundParameterEqualsValue As BoundParameterEqualsValue) As IParameterInitializerOperation
            Dim parameter As IParameterSymbol = boundParameterEqualsValue.Parameter
            Dim value As IOperation = Create(boundParameterEqualsValue.Value)
            Dim syntax As SyntaxNode = boundParameterEqualsValue.Syntax
            Dim isImplicit As Boolean = boundParameterEqualsValue.WasCompilerGenerated
            Return New ParameterInitializerOperation(parameter, ImmutableArray(Of ILocalSymbol).Empty, value, _semanticModel, syntax, isImplicit)
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
                        Return New ConditionalAccessInstanceOperation(_semanticModel, syntax, type, isImplicit)

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

            Return New PlaceholderOperation(placeholderKind, _semanticModel, syntax, type, isImplicit)
        End Function

        Private Function CreateBoundIfStatementOperation(boundIfStatement As BoundIfStatement) As IConditionalOperation
            Dim condition as IOperation = Create(boundIfStatement.Condition)
            Dim whenTrue as IOperation = Create(boundIfStatement.Consequence)
            Dim whenFalse as IOperation = Create(boundIfStatement.AlternativeOpt)
            Dim syntax As SyntaxNode = boundIfStatement.Syntax
            Dim type As ITypeSymbol = Nothing
            Dim constantValue As ConstantValue = Nothing
            Dim isImplicit As Boolean = boundIfStatement.WasCompilerGenerated
            Dim isRef As Boolean = False
            Return New ConditionalOperation(condition, whenTrue, whenFalse, isRef, _semanticModel, syntax, type, constantValue, isImplicit)
        End Function

        Private Function CreateBoundSelectStatementOperation(boundSelectStatement As BoundSelectStatement) As ISwitchOperation
            RecordParent(boundSelectStatement.ExprPlaceholderOpt, boundSelectStatement)
            Dim value As IOperation = Create(boundSelectStatement.ExpressionStatement.Expression)
            Dim cases As ImmutableArray(Of ISwitchCaseOperation) = CreateFromArray(Of BoundCaseBlock, ISwitchCaseOperation)(boundSelectStatement.CaseBlocks)
            Dim exitLabel As ILabelSymbol = boundSelectStatement.ExitLabel
            Dim syntax As SyntaxNode = boundSelectStatement.Syntax
            Dim isImplicit As Boolean = boundSelectStatement.WasCompilerGenerated
            Return New SwitchOperation(ImmutableArray(Of ILocalSymbol).Empty, value, cases, exitLabel, _semanticModel, syntax, isImplicit)
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
                                isImplicit:=boundCaseBlock.WasCompilerGenerated))
            Else
                Return caseStatement.CaseClauses.SelectAsArray(Function(n) DirectCast(Create(n), ICaseClauseOperation))
            End If
        End Function

        Friend Function CreateBoundCaseBlockBody(boundCaseBlock As BoundCaseBlock) As ImmutableArray(Of IOperation)
            Return ImmutableArray.Create(Create(boundCaseBlock.Body))
        End Function

        Friend Function CreateBoundCaseBlockCondition(boundCaseBlock As BoundCaseBlock) As IOperation
            Return Create(boundCaseBlock.CaseStatement.ConditionOpt)
        End Function

        Private Function CreateBoundCaseBlockOperation(boundCaseBlock As BoundCaseBlock) As ISwitchCaseOperation
            Dim clauses As ImmutableArray(Of ICaseClauseOperation) = CreateBoundCaseBlockClauses(boundCaseBlock)
            Dim body As ImmutableArray(Of IOperation) = ImmutableArray.Create(Create(boundCaseBlock.Body))
            Dim condition As IOperation = CreateBoundCaseBlockCondition(boundCaseBlock)
            Dim syntax As SyntaxNode = boundCaseBlock.Syntax
            Dim isImplicit As Boolean = boundCaseBlock.WasCompilerGenerated

            Return New SwitchCaseOperation(clauses, body, ImmutableArray(Of ILocalSymbol).Empty, condition, _semanticModel, syntax, isImplicit)
        End Function

        Private Function CreateBoundSimpleCaseClauseOperation(boundSimpleCaseClause As BoundSimpleCaseClause) As ISingleValueCaseClauseOperation
            Dim clauseValue As IOperation = Create(GetSingleValueCaseClauseValue(boundSimpleCaseClause))
            Dim syntax As SyntaxNode = boundSimpleCaseClause.Syntax
            Dim isImplicit As Boolean = boundSimpleCaseClause.WasCompilerGenerated
            Return New SingleValueCaseClauseOperation(clauseValue, label:=Nothing, _semanticModel, syntax, isImplicit)
        End Function

        Private Function CreateBoundRangeCaseClauseOperation(boundRangeCaseClause As BoundRangeCaseClause) As IRangeCaseClauseOperation
            Dim minimumValue As IOperation = Create(GetCaseClauseValue(boundRangeCaseClause.LowerBoundOpt, boundRangeCaseClause.LowerBoundConditionOpt))
            Dim maximumValue As IOperation = Create(GetCaseClauseValue(boundRangeCaseClause.UpperBoundOpt, boundRangeCaseClause.UpperBoundConditionOpt))
            Debug.Assert(minimumValue IsNot Nothing AndAlso maximumValue IsNot Nothing)
            Dim syntax As SyntaxNode = boundRangeCaseClause.Syntax
            Dim isImplicit As Boolean = boundRangeCaseClause.WasCompilerGenerated
            Return New RangeCaseClauseOperation(minimumValue, maximumValue, label:=Nothing, _semanticModel, syntax, isImplicit)
        End Function

        Private Function CreateBoundRelationalCaseClauseOperation(boundRelationalCaseClause As BoundRelationalCaseClause) As IRelationalCaseClauseOperation
            Dim valueExpression As IOperation = Create(GetSingleValueCaseClauseValue(boundRelationalCaseClause))
            Dim relation As BinaryOperatorKind = If(valueExpression IsNot Nothing, Helper.DeriveBinaryOperatorKind(boundRelationalCaseClause.OperatorKind, leftOpt:=Nothing), BinaryOperatorKind.None)
            Dim syntax As SyntaxNode = boundRelationalCaseClause.Syntax
            Dim isImplicit As Boolean = boundRelationalCaseClause.WasCompilerGenerated
            Return New RelationalCaseClauseOperation(valueExpression, relation, label:=Nothing, _semanticModel, syntax, isImplicit)
        End Function

        Private Function CreateBoundDoLoopStatementOperation(boundDoLoopStatement As BoundDoLoopStatement) As IWhileLoopOperation
            Dim condition As IOperation = Create(boundDoLoopStatement.ConditionOpt)
            Dim body As IOperation = Create(boundDoLoopStatement.Body)
            Dim ignoredCondition As IOperation = If(boundDoLoopStatement.TopConditionOpt IsNot Nothing AndAlso boundDoLoopStatement.BottomConditionOpt IsNot Nothing,
                Create(boundDoLoopStatement.BottomConditionOpt), Nothing)
            Dim locals As ImmutableArray(Of ILocalSymbol) = ImmutableArray(Of ILocalSymbol).Empty
            Dim continueLabel As ILabelSymbol = boundDoLoopStatement.ContinueLabel
            Dim exitLabel As ILabelSymbol = boundDoLoopStatement.ExitLabel
            Dim conditionIsTop As Boolean = boundDoLoopStatement.ConditionIsTop
            Dim conditionIsUntil As Boolean = boundDoLoopStatement.ConditionIsUntil
            Dim syntax As SyntaxNode = boundDoLoopStatement.Syntax
            Dim isImplicit As Boolean = boundDoLoopStatement.WasCompilerGenerated
            Return New WhileLoopOperation(condition, conditionIsTop, conditionIsUntil, ignoredCondition, body, locals, continueLabel, exitLabel, _semanticModel, syntax, isImplicit)
        End Function

        Private Function CreateBoundForToStatementOperation(boundForToStatement As BoundForToStatement) As IForToLoopOperation
            Dim loopControlVariable As IOperation = CreateBoundControlVariableOperation(boundForToStatement)
            Dim initialValue As IOperation = Create(boundForToStatement.InitialValue)
            Dim limitValue As IOperation = Create(boundForToStatement.LimitValue)
            Dim stepValue As IOperation = Create(boundForToStatement.StepValue)
            Dim body As IOperation = Create(boundForToStatement.Body)
            Dim nextVariables As ImmutableArray(Of IOperation) = If(boundForToStatement.NextVariablesOpt.IsDefault,
                ImmutableArray(Of IOperation).Empty,
                CreateFromArray(Of BoundExpression, IOperation)(boundForToStatement.NextVariablesOpt))
            Dim locals As ImmutableArray(Of ILocalSymbol) = If(boundForToStatement.DeclaredOrInferredLocalOpt IsNot Nothing,
                ImmutableArray.Create(Of ILocalSymbol)(boundForToStatement.DeclaredOrInferredLocalOpt),
                ImmutableArray(Of ILocalSymbol).Empty)
            Dim continueLabel As ILabelSymbol = boundForToStatement.ContinueLabel
            Dim exitLabel As ILabelSymbol = boundForToStatement.ExitLabel
            Dim syntax As SyntaxNode = boundForToStatement.Syntax
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
                userDefinedInfo = New ForToLoopOperationUserDefinedInfo(DirectCast(Operation.SetParentOperation(Create(operatorsOpt.Addition), Nothing), IBinaryOperation),
                                                                        DirectCast(Operation.SetParentOperation(Create(operatorsOpt.Subtraction), Nothing), IBinaryOperation),
                                                                        Operation.SetParentOperation(Create(operatorsOpt.LessThanOrEqual), Nothing),
                                                                        Operation.SetParentOperation(Create(operatorsOpt.GreaterThanOrEqual), Nothing))
            End If

            Return New ForToLoopOperation(loopControlVariable, initialValue, limitValue, stepValue, boundForToStatement.Checked, nextVariables, (loopObj, userDefinedInfo),
                                          body, locals, continueLabel, exitLabel, _semanticModel, syntax, isImplicit)
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

            Return New ForEachLoopOperationInfo(statementInfo.ElementType,
                                                     statementInfo.GetEnumeratorMethod,
                                                     statementInfo.CurrentProperty,
                                                     statementInfo.MoveNextMethod,
                                                     isAsynchronous:=False,
                                                     inlineArrayConversion:=Nothing,
                                                     collectionIsInlineArrayValue:=False,
                                                     boundForEachStatement.EnumeratorInfo.NeedToDispose,
                                                     knownToImplementIDisposable:=boundForEachStatement.EnumeratorInfo.NeedToDispose AndAlso
                                                                                  boundForEachStatement.EnumeratorInfo.IsOrInheritsFromOrImplementsIDisposable,
                                                     patternDisposeMethod:=Nothing,
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
            Dim info As ForEachLoopOperationInfo = GetForEachLoopOperationInfo(boundForEachStatement)
            Dim controlVariable As IOperation = CreateBoundControlVariableOperation(boundForEachStatement)
            Dim collection As IOperation = Create(boundForEachStatement.Collection)
            Dim nextVariables = If(boundForEachStatement.NextVariablesOpt.IsDefault,
                ImmutableArray(Of IOperation).Empty,
                CreateFromArray(Of BoundExpression, IOperation)(boundForEachStatement.NextVariablesOpt))
            Dim body As IOperation = Create(boundForEachStatement.Body)
            Dim locals As ImmutableArray(Of ILocalSymbol) = If(boundForEachStatement.DeclaredOrInferredLocalOpt IsNot Nothing,
                ImmutableArray.Create(Of ILocalSymbol)(boundForEachStatement.DeclaredOrInferredLocalOpt),
                ImmutableArray(Of ILocalSymbol).Empty)
            Dim continueLabel As ILabelSymbol = boundForEachStatement.ContinueLabel
            Dim exitLabel As ILabelSymbol = boundForEachStatement.ExitLabel
            Dim syntax As SyntaxNode = boundForEachStatement.Syntax
            Dim isImplicit As Boolean = boundForEachStatement.WasCompilerGenerated
            Return New ForEachLoopOperation(controlVariable, collection, nextVariables, info, isAsynchronous:=False, body, locals, continueLabel, exitLabel, _semanticModel, syntax, isImplicit)
        End Function

        Friend Function CreateBoundControlVariableOperation(boundForStatement As BoundForStatement) As IOperation
            Dim localOpt As LocalSymbol = boundForStatement.DeclaredOrInferredLocalOpt
            Dim controlVariable As BoundExpression = boundForStatement.ControlVariable
            Return If(localOpt IsNot Nothing,
                New VariableDeclaratorOperation(localOpt, initializer:=Nothing, ignoredArguments:=ImmutableArray(Of IOperation).Empty, semanticModel:=_semanticModel, syntax:=controlVariable.Syntax, isImplicit:=boundForStatement.WasCompilerGenerated),
                Create(controlVariable))
        End Function

        Private Function CreateBoundTryStatementOperation(boundTryStatement As BoundTryStatement) As ITryOperation
            Dim body As IBlockOperation = DirectCast(Create(boundTryStatement.TryBlock), IBlockOperation)
            Dim catches As ImmutableArray(Of ICatchClauseOperation) = CreateFromArray(Of BoundCatchBlock, ICatchClauseOperation)(boundTryStatement.CatchBlocks)
            Dim [finally] As IBlockOperation = DirectCast(Create(boundTryStatement.FinallyBlockOpt), IBlockOperation)
            Dim exitLabel As ILabelSymbol = boundTryStatement.ExitLabelOpt
            Dim syntax As SyntaxNode = boundTryStatement.Syntax
            Dim isImplicit As Boolean = boundTryStatement.WasCompilerGenerated
            Return New TryOperation(body, catches, [finally], exitLabel, _semanticModel, syntax, isImplicit)
        End Function

        Friend Function CreateBoundCatchBlockExceptionDeclarationOrExpression(boundCatchBlock As BoundCatchBlock) As IOperation
            If boundCatchBlock.LocalOpt IsNot Nothing AndAlso
                        If(boundCatchBlock.ExceptionSourceOpt?.Kind = BoundKind.Local, False) AndAlso
                        boundCatchBlock.LocalOpt Is DirectCast(boundCatchBlock.ExceptionSourceOpt, BoundLocal).LocalSymbol Then
                Return New VariableDeclaratorOperation(boundCatchBlock.LocalOpt, initializer:=Nothing, ignoredArguments:=ImmutableArray(Of IOperation).Empty, semanticModel:=_semanticModel, syntax:=boundCatchBlock.ExceptionSourceOpt.Syntax, isImplicit:=False)
            Else
                Return Create(boundCatchBlock.ExceptionSourceOpt)
            End If
        End Function

        Private Function CreateBoundCatchBlockOperation(boundCatchBlock As BoundCatchBlock) As ICatchClauseOperation
            Dim exceptionDeclarationOrExpression as IOperation = CreateBoundCatchBlockExceptionDeclarationOrExpression(boundCatchBlock)
            Dim filter As IOperation = Create(boundCatchBlock.ExceptionFilterOpt)
            Dim handler As IBlockOperation = DirectCast(Create(boundCatchBlock.Body), IBlockOperation)
            Dim exceptionType As ITypeSymbol = If(boundCatchBlock.ExceptionSourceOpt?.Type, DirectCast(_semanticModel.Compilation, VisualBasicCompilation).GetWellKnownType(WellKnownType.System_Exception))
            Dim locals As ImmutableArray(Of ILocalSymbol) = If(boundCatchBlock.LocalOpt IsNot Nothing,
                ImmutableArray.Create(Of ILocalSymbol)(boundCatchBlock.LocalOpt),
                ImmutableArray(Of ILocalSymbol).Empty)
            Dim syntax As SyntaxNode = boundCatchBlock.Syntax
            Dim isImplicit As Boolean = boundCatchBlock.WasCompilerGenerated
            Return New CatchClauseOperation(exceptionDeclarationOrExpression, exceptionType, locals, filter, handler, _semanticModel, syntax, isImplicit)
        End Function

        Private Function CreateBoundBlockOperation(boundBlock As BoundBlock) As IBlockOperation
            Dim operations As ImmutableArray(Of IOperation) = CreateFromArray(Of BoundStatement, IOperation)(boundBlock.Statements)
            Dim locals As ImmutableArray(Of ILocalSymbol) = boundBlock.Locals.As(Of ILocalSymbol)()
            Dim syntax As SyntaxNode = boundBlock.Syntax
            Dim isImplicit As Boolean = boundBlock.WasCompilerGenerated
            Return New BlockOperation(operations, locals, _semanticModel, syntax, isImplicit)
        End Function

        Private Function CreateBoundBadStatementOperation(boundBadStatement As BoundBadStatement) As IInvalidOperation
            Dim syntax As SyntaxNode = boundBadStatement.Syntax

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

            Dim children = CreateFromArray(Of BoundNode, IOperation)(boundBadStatement.ChildBoundNodes)

            Return New InvalidOperation(children, _semanticModel, syntax, type:=Nothing, constantValue:=Nothing, isImplicit)
        End Function

        Private Function CreateBoundReturnStatementOperation(boundReturnStatement As BoundReturnStatement) As IReturnOperation
            Dim returnedValue As IOperation = Create(boundReturnStatement.ExpressionOpt)
            Dim syntax As SyntaxNode = boundReturnStatement.Syntax
            Dim isImplicit As Boolean = boundReturnStatement.WasCompilerGenerated OrElse IsEndSubOrFunctionStatement(syntax)
            Return New ReturnOperation(returnedValue, OperationKind.Return, _semanticModel, syntax, isImplicit)
        End Function

        Private Shared Function IsEndSubOrFunctionStatement(syntax As SyntaxNode) As Boolean
            Return TryCast(syntax.Parent, MethodBlockBaseSyntax)?.EndBlockStatement Is syntax OrElse
                   TryCast(syntax.Parent, MultiLineLambdaExpressionSyntax)?.EndSubOrFunctionStatement Is syntax
        End Function

        Private Function CreateBoundThrowStatementOperation(boundThrowStatement As BoundThrowStatement) As IThrowOperation
            Dim thrownObject As IOperation = Create(boundThrowStatement.ExpressionOpt)
            Dim syntax As SyntaxNode = boundThrowStatement.Syntax
            Dim expressionType As ITypeSymbol = Nothing
            Dim isImplicit As Boolean = boundThrowStatement.WasCompilerGenerated
            Return New ThrowOperation(thrownObject, _semanticModel, syntax, expressionType, isImplicit)
        End Function

        Private Function CreateBoundWhileStatementOperation(boundWhileStatement As BoundWhileStatement) As IWhileLoopOperation
            Dim condition As IOperation = Create(boundWhileStatement.Condition)
            Dim body As IOperation = Create(boundWhileStatement.Body)
            Dim ignoredCondition As IOperation = Nothing
            Dim locals As ImmutableArray(Of ILocalSymbol) = ImmutableArray(Of ILocalSymbol).Empty
            Dim continueLabel As ILabelSymbol = boundWhileStatement.ContinueLabel
            Dim exitLabel As ILabelSymbol = boundWhileStatement.ExitLabel
            Dim conditionIsTop As Boolean = True
            Dim conditionIsUntil As Boolean = False
            Dim syntax As SyntaxNode = boundWhileStatement.Syntax
            Dim isImplicit As Boolean = boundWhileStatement.WasCompilerGenerated
            Return New WhileLoopOperation(condition, conditionIsTop, conditionIsUntil, ignoredCondition, body, locals, continueLabel, exitLabel, _semanticModel, syntax, isImplicit)
        End Function

        Private Function CreateBoundDimStatementOperation(boundDimStatement As BoundDimStatement) As IVariableDeclarationGroupOperation
            Dim declarations As ImmutableArray(Of IVariableDeclarationOperation) = GetVariableDeclarationStatementVariables(boundDimStatement.LocalDeclarations)
            Dim syntax As SyntaxNode = boundDimStatement.Syntax
            Dim isImplicit As Boolean = boundDimStatement.WasCompilerGenerated
            Return New VariableDeclarationGroupOperation(declarations, _semanticModel, syntax, isImplicit)
        End Function

        Private Function CreateBoundLocalDeclarationOperation(boundLocalDeclaration As BoundLocalDeclaration) As IVariableDeclarationGroupOperation
            Dim declarations As ImmutableArray(Of IVariableDeclarationOperation) =
                GetVariableDeclarationStatementVariables(ImmutableArray.Create(Of BoundLocalDeclarationBase)(boundLocalDeclaration))
            Dim syntax As SyntaxNode = boundLocalDeclaration.Syntax
            Debug.Assert(boundLocalDeclaration.WasCompilerGenerated)
            Dim isImplicit As Boolean = True
            Return New VariableDeclarationGroupOperation(declarations, _semanticModel, syntax, isImplicit)
        End Function

        Private Function CreateBoundYieldStatementOperation(boundYieldStatement As BoundYieldStatement) As IReturnOperation
            Dim returnedValue As IOperation = Create(boundYieldStatement.Expression)
            Dim syntax As SyntaxNode = boundYieldStatement.Syntax
            Dim isImplicit As Boolean = boundYieldStatement.WasCompilerGenerated
            Return New ReturnOperation(returnedValue, OperationKind.YieldReturn, _semanticModel, syntax, isImplicit)
        End Function

        Private Function CreateBoundLabelStatementOperation(boundLabelStatement As BoundLabelStatement) As ILabeledOperation
            Dim label As ILabelSymbol = boundLabelStatement.Label
            Dim statement As IOperation = Nothing
            Dim syntax As SyntaxNode = boundLabelStatement.Syntax
            Dim isImplicit As Boolean = boundLabelStatement.WasCompilerGenerated OrElse IsEndSubOrFunctionStatement(syntax)
            Return New LabeledOperation(label, statement, _semanticModel, syntax, isImplicit)
        End Function

        Private Function CreateBoundGotoStatementOperation(boundGotoStatement As BoundGotoStatement) As IBranchOperation
            Dim target As ILabelSymbol = boundGotoStatement.Label
            Dim branchKind As BranchKind = BranchKind.GoTo
            Dim syntax As SyntaxNode = boundGotoStatement.Syntax
            Dim isImplicit As Boolean = boundGotoStatement.WasCompilerGenerated
            Return New BranchOperation(target, branchKind, _semanticModel, syntax, isImplicit)
        End Function

        Private Function CreateBoundContinueStatementOperation(boundContinueStatement As BoundContinueStatement) As IBranchOperation
            Dim target As ILabelSymbol = boundContinueStatement.Label
            Dim branchKind As BranchKind = BranchKind.Continue
            Dim syntax As SyntaxNode = boundContinueStatement.Syntax
            Dim isImplicit As Boolean = boundContinueStatement.WasCompilerGenerated
            Return New BranchOperation(target, branchKind, _semanticModel, syntax, isImplicit)
        End Function

        Private Function CreateBoundExitStatementOperation(boundExitStatement As BoundExitStatement) As IBranchOperation
            Dim target As ILabelSymbol = boundExitStatement.Label
            Dim branchKind As BranchKind = BranchKind.Break
            Dim syntax As SyntaxNode = boundExitStatement.Syntax
            Dim isImplicit As Boolean = boundExitStatement.WasCompilerGenerated
            Return New BranchOperation(target, branchKind, _semanticModel, syntax, isImplicit)
        End Function

        Private Function CreateBoundSyncLockStatementOperation(boundSyncLockStatement As BoundSyncLockStatement) As ILockOperation
            Dim legacyMode = _semanticModel.Compilation.CommonGetWellKnownTypeMember(WellKnownMember.System_Threading_Monitor__Enter2) Is Nothing
            Dim lockTakenSymbol As ILocalSymbol =
                If(legacyMode, Nothing,
                               New SynthesizedLocal(DirectCast(_semanticModel.GetEnclosingSymbol(boundSyncLockStatement.Syntax.SpanStart), Symbol),
                                                    DirectCast(_semanticModel.Compilation.GetSpecialType(SpecialType.System_Boolean), TypeSymbol),
                                                    SynthesizedLocalKind.LockTaken,
                                                    syntaxOpt:=boundSyncLockStatement.LockExpression.Syntax))
            Dim lockedValue as IOperation = Create(boundSyncLockStatement.LockExpression)
            Dim body as IOperation = Create(boundSyncLockStatement.Body)
            Dim syntax As SyntaxNode = boundSyncLockStatement.Syntax
            Dim isImplicit As Boolean = boundSyncLockStatement.WasCompilerGenerated
            Return New LockOperation(lockedValue, body, lockTakenSymbol, _semanticModel, syntax, isImplicit)
        End Function

        Private Function CreateBoundNoOpStatementOperation(boundNoOpStatement As BoundNoOpStatement) As IEmptyOperation
            Dim syntax As SyntaxNode = boundNoOpStatement.Syntax
            Dim type As ITypeSymbol = Nothing
            Dim constantValue As ConstantValue = Nothing
            Dim isImplicit As Boolean = boundNoOpStatement.WasCompilerGenerated
            Return New EmptyOperation(_semanticModel, syntax, isImplicit)
        End Function

        Private Function CreateBoundStopStatementOperation(boundStopStatement As BoundStopStatement) As IStopOperation
            Dim syntax As SyntaxNode = boundStopStatement.Syntax
            Dim isImplicit As Boolean = boundStopStatement.WasCompilerGenerated
            Return New StopOperation(_semanticModel, syntax, isImplicit)
        End Function

        Private Function CreateBoundEndStatementOperation(boundEndStatement As BoundEndStatement) As IEndOperation
            Dim syntax As SyntaxNode = boundEndStatement.Syntax
            Dim isImplicit As Boolean = boundEndStatement.WasCompilerGenerated
            Return New EndOperation(_semanticModel, syntax, isImplicit)
        End Function

        Private Function CreateBoundWithStatementOperation(boundWithStatement As BoundWithStatement) As IWithStatementOperation
            Dim value As IOperation = Create(boundWithStatement.OriginalExpression)
            Dim body As IOperation = Create(boundWithStatement.Body)
            Dim syntax As SyntaxNode = boundWithStatement.Syntax
            Dim isImplicit As Boolean = boundWithStatement.WasCompilerGenerated
            Return New WithStatementOperation(body, value, _semanticModel, syntax, isImplicit)
        End Function

        Friend Function CreateBoundUsingStatementResources(boundUsingStatement As BoundUsingStatement) As IOperation
            If Not boundUsingStatement.ResourceList.IsDefault Then
                Return GetUsingStatementDeclaration(boundUsingStatement.ResourceList, DirectCast(boundUsingStatement.Syntax, UsingBlockSyntax).UsingStatement)
            Else
                Return Create(boundUsingStatement.ResourceExpressionOpt)
            End If
        End Function

        Private Function CreateBoundUsingStatementOperation(boundUsingStatement As BoundUsingStatement) As IUsingOperation
            Dim resources As IOperation = CreateBoundUsingStatementResources(boundUsingStatement)
            Dim body As IOperation = Create(boundUsingStatement.Body)
            Dim locals As ImmutableArray(Of ILocalSymbol) = ImmutableArray(Of ILocalSymbol).CastUp(boundUsingStatement.Locals)
            Dim syntax As SyntaxNode = boundUsingStatement.Syntax
            Dim isImplicit As Boolean = boundUsingStatement.WasCompilerGenerated
            Return New UsingOperation(resources, body, locals, isAsynchronous:=False, disposeInfo:=Nothing, _semanticModel, syntax, isImplicit)
        End Function

        Private Function CreateBoundExpressionStatementOperation(boundExpressionStatement As BoundExpressionStatement) As IExpressionStatementOperation
            Dim expression As IOperation = Create(boundExpressionStatement.Expression)
            Dim syntax As SyntaxNode = boundExpressionStatement.Syntax
            Dim isImplicit As Boolean = boundExpressionStatement.WasCompilerGenerated
            Return New ExpressionStatementOperation(expression, _semanticModel, syntax, isImplicit)
        End Function

        Friend Function CreateBoundRaiseEventStatementEventReference(boundRaiseEventStatement As BoundRaiseEventStatement) As IEventReferenceOperation
            Dim eventInvocation = DirectCast(boundRaiseEventStatement.EventInvocation, BoundCall)
            Dim receiverOpt = eventInvocation.ReceiverOpt
            Dim eventReferenceSyntax = If(receiverOpt?.Syntax,
                                          If(TryCast(boundRaiseEventStatement.Syntax, RaiseEventStatementSyntax)?.Name,
                                             boundRaiseEventStatement.Syntax))
            Dim eventReferenceType As ITypeSymbol = boundRaiseEventStatement.EventSymbol.Type
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

            Dim instance = CreateReceiverOperation(receiverOpt, boundRaiseEventStatement.EventSymbol)
            Return New EventReferenceOperation(boundRaiseEventStatement.EventSymbol, constrainedToType:=Nothing,
                                               instance,
                                               _semanticModel,
                                               eventReferenceSyntax,
                                               eventReferenceType,
                                               eventReferenceIsImplicit)
        End Function

        Private Function CreateBoundRaiseEventStatementOperation(boundRaiseEventStatement As BoundRaiseEventStatement) As IOperation
            Dim syntax As SyntaxNode = boundRaiseEventStatement.Syntax
            Dim isImplicit As Boolean = boundRaiseEventStatement.WasCompilerGenerated

            Dim eventSymbol = boundRaiseEventStatement.EventSymbol
            Dim eventInvocation = TryCast(boundRaiseEventStatement.EventInvocation, BoundCall)

            ' Return an invalid statement for invalid raise event statement
            If eventInvocation Is Nothing OrElse (eventInvocation.ReceiverOpt Is Nothing AndAlso Not eventSymbol.IsShared) Then
                Debug.Assert(boundRaiseEventStatement.HasErrors)
                Dim children = CreateFromArray(Of BoundNode, IOperation)(DirectCast(boundRaiseEventStatement, IBoundInvalidNode).InvalidNodeChildren)
                Return New InvalidOperation(children, _semanticModel, syntax, type:=Nothing, constantValue:=Nothing, isImplicit)
            End If

            Dim eventReference = CreateBoundRaiseEventStatementEventReference(boundRaiseEventStatement)
            Dim arguments = DeriveArguments(boundRaiseEventStatement)

            Return New RaiseEventOperation(eventReference, arguments, _semanticModel, syntax, isImplicit)
        End Function

        Private Function CreateBoundAddHandlerStatementOperation(boundAddHandlerStatement As BoundAddHandlerStatement) As IExpressionStatementOperation
            Dim expression As IOperation = GetAddRemoveHandlerStatementExpression(boundAddHandlerStatement)
            Dim syntax As SyntaxNode = boundAddHandlerStatement.Syntax
            Dim isImplicit As Boolean = boundAddHandlerStatement.WasCompilerGenerated
            Return New ExpressionStatementOperation(expression, _semanticModel, syntax, isImplicit)
        End Function

        Private Function CreateBoundRemoveHandlerStatementOperation(boundRemoveHandlerStatement As BoundRemoveHandlerStatement) As IExpressionStatementOperation
            Dim expression As IOperation = GetAddRemoveHandlerStatementExpression(boundRemoveHandlerStatement)
            Dim syntax As SyntaxNode = boundRemoveHandlerStatement.Syntax
            Dim type As ITypeSymbol = Nothing
            Dim constantValue As ConstantValue = Nothing
            Dim isImplicit As Boolean = boundRemoveHandlerStatement.WasCompilerGenerated
            Return New ExpressionStatementOperation(expression, _semanticModel, syntax, isImplicit)
        End Function

        Private Function CreateBoundTupleLiteralOperation(boundTupleLiteral As BoundTupleLiteral) As ITupleOperation
            Return CreateTupleOperation(boundTupleLiteral, boundTupleLiteral.Type)
        End Function

        Private Function CreateBoundConvertedTupleLiteralOperation(boundConvertedTupleLiteral As BoundConvertedTupleLiteral) As ITupleOperation
            Return CreateTupleOperation(boundConvertedTupleLiteral, boundConvertedTupleLiteral.NaturalTypeOpt)
        End Function

        Private Function CreateTupleOperation(boundTupleExpression As BoundTupleExpression, naturalType As ITypeSymbol) As ITupleOperation
            Dim elements As ImmutableArray(Of IOperation) = CreateFromArray(Of BoundExpression, IOperation)(boundTupleExpression.Arguments)
            Dim syntax As SyntaxNode = boundTupleExpression.Syntax
            Dim type As ITypeSymbol = boundTupleExpression.Type
            Dim isImplicit As Boolean = boundTupleExpression.WasCompilerGenerated
            Return New TupleOperation(elements, naturalType, _semanticModel, syntax, type, isImplicit)
        End Function

        Private Function CreateBoundInterpolatedStringExpressionOperation(boundInterpolatedString As BoundInterpolatedStringExpression) As IInterpolatedStringOperation
            Dim parts As ImmutableArray(Of IInterpolatedStringContentOperation) = CreateBoundInterpolatedStringContentOperation(boundInterpolatedString.Contents)
            Dim syntax As SyntaxNode = boundInterpolatedString.Syntax
            Dim type As ITypeSymbol = boundInterpolatedString.Type
            Dim constantValue As ConstantValue = boundInterpolatedString.ConstantValueOpt
            Dim isImplicit As Boolean = boundInterpolatedString.WasCompilerGenerated
            Return New InterpolatedStringOperation(parts, _semanticModel, syntax, type, constantValue, isImplicit)
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
            Dim expression As IOperation = Create(boundInterpolation.Expression)
            Dim alignment As IOperation = Create(boundInterpolation.AlignmentOpt)
            Dim formatString As IOperation = Create(boundInterpolation.FormatStringOpt)
            Dim syntax As SyntaxNode = boundInterpolation.Syntax
            Dim isImplicit As Boolean = boundInterpolation.WasCompilerGenerated
            Return New InterpolationOperation(expression, alignment, formatString, _semanticModel, syntax, isImplicit)
        End Function

        Private Function CreateBoundInterpolatedStringTextOperation(boundLiteral As BoundLiteral) As IInterpolatedStringTextOperation
            Dim text As IOperation = CreateBoundLiteralOperation(boundLiteral, implicit:=True)
            Dim syntax As SyntaxNode = boundLiteral.Syntax
            Dim isImplicit As Boolean = boundLiteral.WasCompilerGenerated
            Return New InterpolatedStringTextOperation(text, _semanticModel, syntax, isImplicit)
        End Function

        Private Function CreateBoundAnonymousTypeCreationExpressionOperation(boundAnonymousTypeCreationExpression As BoundAnonymousTypeCreationExpression) As IAnonymousObjectCreationOperation
            Dim initializers As ImmutableArray(Of IOperation) = GetAnonymousTypeCreationInitializers(boundAnonymousTypeCreationExpression)
            Dim syntax As SyntaxNode = boundAnonymousTypeCreationExpression.Syntax
            Dim type As ITypeSymbol = boundAnonymousTypeCreationExpression.Type
            Dim isImplicit As Boolean = boundAnonymousTypeCreationExpression.WasCompilerGenerated
            Return New AnonymousObjectCreationOperation(initializers, _semanticModel, syntax, type, isImplicit)
        End Function

        Private Function CreateBoundAnonymousTypePropertyAccessOperation(boundAnonymousTypePropertyAccess As BoundAnonymousTypePropertyAccess) As IPropertyReferenceOperation
            Dim [property] As IPropertySymbol = DirectCast(boundAnonymousTypePropertyAccess.ExpressionSymbol, IPropertySymbol)
            Dim instance As IOperation = CreateAnonymousTypePropertyAccessImplicitReceiverOperation([property], boundAnonymousTypePropertyAccess.Syntax.FirstAncestorOrSelf(Of AnonymousObjectCreationExpressionSyntax))
            Dim arguments = ImmutableArray(Of IArgumentOperation).Empty
            Dim syntax As SyntaxNode = boundAnonymousTypePropertyAccess.Syntax
            Dim type As ITypeSymbol = boundAnonymousTypePropertyAccess.Type
            Dim isImplicit As Boolean = boundAnonymousTypePropertyAccess.WasCompilerGenerated
            Return New PropertyReferenceOperation([property], constrainedToType:=Nothing, arguments, instance, _semanticModel, syntax, type, isImplicit)
        End Function

        Private Function CreateAnonymousTypePropertyAccessImplicitReceiverOperation(propertySym As IPropertySymbol, syntax As SyntaxNode) As InstanceReferenceOperation
            Debug.Assert(propertySym IsNot Nothing)
            Debug.Assert(syntax IsNot Nothing)
            Return New InstanceReferenceOperation(
                InstanceReferenceKind.ImplicitReceiver,
                _semanticModel,
                syntax,
                propertySym.ContainingType,
                isImplicit:=True)
        End Function

        Private Function CreateBoundQueryExpressionOperation(boundQueryExpression As BoundQueryExpression) As IOperation
            Dim operation As IOperation = Create(boundQueryExpression.LastOperator)
            Dim syntax As SyntaxNode = boundQueryExpression.Syntax
            Dim type As ITypeSymbol = boundQueryExpression.Type
            Dim isImplicit As Boolean = boundQueryExpression.WasCompilerGenerated
            Return New TranslatedQueryOperation(operation, _semanticModel, syntax, type, isImplicit)
        End Function

        Private Function CreateBoundAggregateClauseOperation(boundAggregateClause As BoundAggregateClause) As IOperation
            If boundAggregateClause.CapturedGroupOpt Is Nothing Then
                ' This Aggregate clause has no special representation in the IOperation tree
                Return Create(boundAggregateClause.UnderlyingExpression)
            End If

            Debug.Assert(boundAggregateClause.GroupPlaceholderOpt IsNot Nothing)
            RecordParent(boundAggregateClause.GroupPlaceholderOpt, boundAggregateClause)

            Dim group As IOperation = Create(boundAggregateClause.CapturedGroupOpt)
            Dim aggregation As IOperation = Create(boundAggregateClause.UnderlyingExpression)
            Dim syntax As SyntaxNode = boundAggregateClause.Syntax
            Dim type As ITypeSymbol = boundAggregateClause.Type
            Dim isImplicit As Boolean = boundAggregateClause.WasCompilerGenerated
            Return New AggregateQueryOperation(group, aggregation, _semanticModel, syntax, type, isImplicit)
        End Function

        Private Function CreateBoundNullableIsTrueOperator(boundNullableIsTrueOperator As BoundNullableIsTrueOperator) As IOperation
            Dim syntax As SyntaxNode = boundNullableIsTrueOperator.Syntax
            Dim type As ITypeSymbol = boundNullableIsTrueOperator.Type
            Dim constantValue As ConstantValue = boundNullableIsTrueOperator.ConstantValueOpt
            Dim isImplicit As Boolean = boundNullableIsTrueOperator.WasCompilerGenerated

            Debug.Assert(boundNullableIsTrueOperator.Operand.Type.IsNullableOfBoolean() AndAlso boundNullableIsTrueOperator.Type.IsBooleanType())

            Dim method = DirectCast(DirectCast(_semanticModel.Compilation, VisualBasicCompilation).
                                        GetSpecialTypeMember(SpecialMember.System_Nullable_T_GetValueOrDefault), MethodSymbol)

            If method IsNot Nothing Then
                Dim receiver as IOperation = CreateReceiverOperation(boundNullableIsTrueOperator.Operand, method)
                Return New InvocationOperation(method.AsMember(DirectCast(boundNullableIsTrueOperator.Operand.Type, NamedTypeSymbol)), constrainedToType:=Nothing,
                                                              receiver,
                                                              isVirtual:=False,
                                                              arguments:=ImmutableArray(Of IArgumentOperation).Empty,
                                                              _semanticModel,
                                                              syntax,
                                                              boundNullableIsTrueOperator.Type,
                                                              isImplicit)
            Else
                Dim children = CreateFromArray(Of BoundNode, IOperation)(DirectCast(boundNullableIsTrueOperator, IBoundInvalidNode).InvalidNodeChildren)
                Return New InvalidOperation(children, _semanticModel, syntax, type, constantValue, isImplicit)
            End If
        End Function

        Private Function CreateBoundReDimOperation(boundRedimStatement As BoundRedimStatement) As IReDimOperation
            Dim clauses As ImmutableArray(Of IReDimClauseOperation) = CreateFromArray(Of BoundRedimClause, IReDimClauseOperation)(boundRedimStatement.Clauses)
            Dim preserve As Boolean = boundRedimStatement.Syntax.Kind = SyntaxKind.ReDimPreserveStatement
#If DEBUG Then
            For Each clause In boundRedimStatement.Clauses
                Debug.Assert(preserve = clause.Preserve)
            Next
#End If
            Dim syntax As SyntaxNode = boundRedimStatement.Syntax
            Dim isImplicit As Boolean = boundRedimStatement.WasCompilerGenerated
            Return New ReDimOperation(clauses, preserve, _semanticModel, syntax, isImplicit)
        End Function

        Private Function CreateBoundReDimClauseOperation(boundRedimClause As BoundRedimClause) As IReDimClauseOperation
            Dim operand As IOperation = Create(boundRedimClause.Operand)
            Dim dimensionSizes As ImmutableArray(Of IOperation) = CreateFromArray(Of BoundExpression, IOperation)(boundRedimClause.Indices)
            Dim syntax As SyntaxNode = boundRedimClause.Syntax
            Dim isImplicit As Boolean = boundRedimClause.WasCompilerGenerated
            Return New ReDimClauseOperation(operand, dimensionSizes, _semanticModel, syntax, isImplicit)
        End Function
    End Class
End Namespace

