' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Immutable
Imports System.Runtime.CompilerServices
Imports Microsoft.CodeAnalysis.VisualBasic

Namespace Microsoft.CodeAnalysis.Semantics
    Partial Friend NotInheritable Class VisualBasicOperationFactory

        Private Shared ReadOnly s_cache As New ConditionalWeakTable(Of BoundNode, IOperation)

        Public Shared Function Create(boundNode As BoundNode) As IOperation
            If boundNode Is Nothing Then
                Return Nothing
            End If

            Return s_cache.GetValue(boundNode, Function(n) CreateInternal(n))
        End Function

        Private Shared Function CreateInternal(boundNode As BoundNode) As IOperation
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
                Case BoundKind.UserDefinedConversion
                    Return CreateBoundUserDefinedConversionOperation(DirectCast(boundNode, BoundUserDefinedConversion))
                Case BoundKind.TernaryConditionalExpression
                    Return CreateBoundTernaryConditionalExpressionOperation(DirectCast(boundNode, BoundTernaryConditionalExpression))
                Case BoundKind.TypeOf
                    Return CreateBoundTypeOfOperation(DirectCast(boundNode, BoundTypeOf))
                Case BoundKind.ObjectCreationExpression
                    Return CreateBoundObjectCreationExpressionOperation(DirectCast(boundNode, BoundObjectCreationExpression))
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
                Case BoundKind.TupleLiteral, BoundKind.ConvertedTupleLiteral
                    Return CreateBoundTupleExpressionOperation(DirectCast(boundNode, BoundTupleExpression))
                Case Else
                    Dim constantValue = ConvertToOptional(TryCast(boundNode, BoundExpression)?.ConstantValueOpt)
                    Return Operation.CreateOperationNone(boundNode.HasErrors, boundNode.Syntax, constantValue, Function() GetIOperationChildren(boundNode))
            End Select
        End Function

        Private Shared Function GetIOperationChildren(boundNode As BoundNode) As ImmutableArray(Of IOperation)
            Dim boundNodeWithChildren = DirectCast(boundNode, IBoundNodeWithIOperationChildren)
            If boundNodeWithChildren.Children.IsDefaultOrEmpty Then
                Return ImmutableArray(Of IOperation).Empty
            End If

            Dim builder = ArrayBuilder(Of IOperation).GetInstance(boundNodeWithChildren.Children.Length)
            For Each childNode In boundNodeWithChildren.Children
                Dim operation = Create(childNode)
                builder.Add(operation)
            Next

            Return builder.ToImmutableAndFree()
        End Function

        Private Shared Function CreateBoundAssignmentOperatorOperation(boundAssignmentOperator As BoundAssignmentOperator) As IOperation
            Dim kind = GetAssignmentKind(boundAssignmentOperator)
            If kind = OperationKind.CompoundAssignmentExpression Then
                Dim binaryOperationKind As BinaryOperationKind = DirectCast(Create(boundAssignmentOperator.Right), IBinaryOperatorExpression).BinaryOperationKind
                Dim target As Lazy(Of IOperation) = New Lazy(Of IOperation)(Function() Create(boundAssignmentOperator.Left))
                Dim value As Lazy(Of IOperation) = New Lazy(Of IOperation)(Function() DirectCast(Create(boundAssignmentOperator.Right), IBinaryOperatorExpression).RightOperand)
                Dim usesOperatorMethod As Boolean = DirectCast(Create(boundAssignmentOperator.Right), IBinaryOperatorExpression).UsesOperatorMethod
                Dim operatorMethod As IMethodSymbol = DirectCast(Create(boundAssignmentOperator.Right), IBinaryOperatorExpression).OperatorMethod
                Dim isInvalid As Boolean = boundAssignmentOperator.HasErrors
                Dim syntax As SyntaxNode = boundAssignmentOperator.Syntax
                Dim type As ITypeSymbol = boundAssignmentOperator.Type
                Dim constantValue As [Optional](Of Object) = ConvertToOptional(boundAssignmentOperator.ConstantValueOpt)
                Return New LazyCompoundAssignmentExpression(binaryOperationKind, target, value, usesOperatorMethod, operatorMethod, isInvalid, syntax, type, constantValue)
            Else
                Dim target As Lazy(Of IOperation) = New Lazy(Of IOperation)(Function() Create(boundAssignmentOperator.Left))
                Dim value As Lazy(Of IOperation) = New Lazy(Of IOperation)(Function() Create(boundAssignmentOperator.Right))
                Dim isInvalid As Boolean = boundAssignmentOperator.HasErrors
                Dim syntax As SyntaxNode = boundAssignmentOperator.Syntax
                Dim type As ITypeSymbol = boundAssignmentOperator.Type
                Dim constantValue As [Optional](Of Object) = ConvertToOptional(boundAssignmentOperator.ConstantValueOpt)
                Return New LazyAssignmentExpression(target, value, isInvalid, syntax, type, constantValue)
            End If
        End Function

        Private Shared Function CreateBoundMeReferenceOperation(boundMeReference As BoundMeReference) As IInstanceReferenceExpression
            Dim instanceReferenceKind As InstanceReferenceKind = If(boundMeReference.WasCompilerGenerated, InstanceReferenceKind.Implicit, InstanceReferenceKind.Explicit)
            Dim isInvalid As Boolean = boundMeReference.HasErrors
            Dim syntax As SyntaxNode = boundMeReference.Syntax
            Dim type As ITypeSymbol = boundMeReference.Type
            Dim constantValue As [Optional](Of Object) = ConvertToOptional(boundMeReference.ConstantValueOpt)
            Return New InstanceReferenceExpression(instanceReferenceKind, isInvalid, syntax, type, constantValue)
        End Function

        Private Shared Function CreateBoundMyBaseReferenceOperation(boundMyBaseReference As BoundMyBaseReference) As IInstanceReferenceExpression
            Dim instanceReferenceKind As InstanceReferenceKind = InstanceReferenceKind.BaseClass
            Dim isInvalid As Boolean = boundMyBaseReference.HasErrors
            Dim syntax As SyntaxNode = boundMyBaseReference.Syntax
            Dim type As ITypeSymbol = boundMyBaseReference.Type
            Dim constantValue As [Optional](Of Object) = ConvertToOptional(boundMyBaseReference.ConstantValueOpt)
            Return New InstanceReferenceExpression(instanceReferenceKind, isInvalid, syntax, type, constantValue)
        End Function

        Private Shared Function CreateBoundMyClassReferenceOperation(boundMyClassReference As BoundMyClassReference) As IInstanceReferenceExpression
            Dim instanceReferenceKind As InstanceReferenceKind = InstanceReferenceKind.ThisClass
            Dim isInvalid As Boolean = boundMyClassReference.HasErrors
            Dim syntax As SyntaxNode = boundMyClassReference.Syntax
            Dim type As ITypeSymbol = boundMyClassReference.Type
            Dim constantValue As [Optional](Of Object) = ConvertToOptional(boundMyClassReference.ConstantValueOpt)
            Return New InstanceReferenceExpression(instanceReferenceKind, isInvalid, syntax, type, constantValue)
        End Function

        Private Shared Function CreateBoundLiteralOperation(boundLiteral As BoundLiteral) As ILiteralExpression
            Dim text As String = boundLiteral.Syntax.ToString()
            Dim isInvalid As Boolean = boundLiteral.HasErrors
            Dim syntax As SyntaxNode = boundLiteral.Syntax
            Dim type As ITypeSymbol = boundLiteral.Type
            Dim constantValue As [Optional](Of Object) = ConvertToOptional(boundLiteral.ConstantValueOpt)
            Return New LiteralExpression(text, isInvalid, syntax, type, constantValue)
        End Function

        Private Shared Function CreateBoundAwaitOperatorOperation(boundAwaitOperator As BoundAwaitOperator) As IAwaitExpression
            Dim awaitedValue As Lazy(Of IOperation) = New Lazy(Of IOperation)(Function() Create(boundAwaitOperator.Operand))
            Dim isInvalid As Boolean = boundAwaitOperator.HasErrors
            Dim syntax As SyntaxNode = boundAwaitOperator.Syntax
            Dim type As ITypeSymbol = boundAwaitOperator.Type
            Dim constantValue As [Optional](Of Object) = ConvertToOptional(boundAwaitOperator.ConstantValueOpt)
            Return New LazyAwaitExpression(awaitedValue, isInvalid, syntax, type, constantValue)
        End Function

        Private Shared Function CreateBoundLambdaOperation(boundLambda As BoundLambda) As ILambdaExpression
            Dim signature As IMethodSymbol = boundLambda.LambdaSymbol
            Dim body As Lazy(Of IBlockStatement) = New Lazy(Of IBlockStatement)(Function() DirectCast(Create(boundLambda.Body), IBlockStatement))
            Dim isInvalid As Boolean = boundLambda.HasErrors
            Dim syntax As SyntaxNode = boundLambda.Syntax
            Dim type As ITypeSymbol = boundLambda.Type
            Dim constantValue As [Optional](Of Object) = ConvertToOptional(boundLambda.ConstantValueOpt)
            Return New LazyLambdaExpression(signature, body, isInvalid, syntax, type, constantValue)
        End Function

        Private Shared Function CreateBoundCallOperation(boundCall As BoundCall) As IInvocationExpression
            Dim targetMethod As IMethodSymbol = boundCall.Method
            Dim instance As Lazy(Of IOperation) = New Lazy(Of IOperation)(Function() If(boundCall.Method.IsShared, Nothing, Create(boundCall.ReceiverOpt)))

            Dim method As IMethodSymbol = boundCall.Method
            Dim receiver As IOperation = Create(boundCall.ReceiverOpt)
            Dim isVirtual As Boolean = method IsNot Nothing AndAlso instance IsNot Nothing AndAlso (method.IsVirtual OrElse method.IsAbstract OrElse method.IsOverride) AndAlso receiver.Kind <> BoundKind.MyBaseReference AndAlso receiver.Kind <> BoundKind.MyClassReference

            Dim argumentsInEvaluationOrder As Lazy(Of ImmutableArray(Of IArgument)) = New Lazy(Of ImmutableArray(Of IArgument))(Function() DeriveArguments(boundCall.Arguments, boundCall.Method.Parameters))
            Dim isInvalid As Boolean = boundCall.HasErrors
            Dim syntax As SyntaxNode = boundCall.Syntax
            Dim type As ITypeSymbol = boundCall.Type
            Dim constantValue As [Optional](Of Object) = ConvertToOptional(boundCall.ConstantValueOpt)
            Return New LazyInvocationExpression(targetMethod, instance, isVirtual, argumentsInEvaluationOrder, isInvalid, syntax, type, constantValue)
        End Function

        Private Shared Function CreateBoundOmittedArgumentOperation(boundOmittedArgument As BoundOmittedArgument) As IOmittedArgumentExpression
            Dim isInvalid As Boolean = boundOmittedArgument.HasErrors
            Dim syntax As SyntaxNode = boundOmittedArgument.Syntax
            Dim type As ITypeSymbol = boundOmittedArgument.Type
            Dim constantValue As [Optional](Of Object) = ConvertToOptional(boundOmittedArgument.ConstantValueOpt)
            Return New OmittedArgumentExpression(isInvalid, syntax, type, constantValue)
        End Function

        Private Shared Function CreateBoundParenthesizedOperation(boundParenthesized As BoundParenthesized) As IParenthesizedExpression
            Dim operand As Lazy(Of IOperation) = New Lazy(Of IOperation)(Function() Create(boundParenthesized.Expression))
            Dim isInvalid As Boolean = boundParenthesized.HasErrors
            Dim syntax As SyntaxNode = boundParenthesized.Syntax
            Dim type As ITypeSymbol = boundParenthesized.Type
            Dim constantValue As [Optional](Of Object) = ConvertToOptional(boundParenthesized.ConstantValueOpt)
            Return New LazyParenthesizedExpression(operand, isInvalid, syntax, type, constantValue)
        End Function

        Private Shared Function CreateBoundArrayAccessOperation(boundArrayAccess As BoundArrayAccess) As IArrayElementReferenceExpression
            Dim arrayReference As Lazy(Of IOperation) = New Lazy(Of IOperation)(Function() Create(boundArrayAccess.Expression))
            Dim indices As Lazy(Of ImmutableArray(Of IOperation)) = New Lazy(Of ImmutableArray(Of IOperation))(Function() boundArrayAccess.Indices.SelectAsArray(Function(n) DirectCast(Create(n), IOperation)))
            Dim isInvalid As Boolean = boundArrayAccess.HasErrors
            Dim syntax As SyntaxNode = boundArrayAccess.Syntax
            Dim type As ITypeSymbol = boundArrayAccess.Type
            Dim constantValue As [Optional](Of Object) = ConvertToOptional(boundArrayAccess.ConstantValueOpt)
            Return New LazyArrayElementReferenceExpression(arrayReference, indices, isInvalid, syntax, type, constantValue)
        End Function

        Private Shared Function CreateBoundUnaryOperatorOperation(boundUnaryOperator As BoundUnaryOperator) As IUnaryOperatorExpression
            Dim unaryOperationKind As UnaryOperationKind = Helper.DeriveUnaryOperationKind(boundUnaryOperator.OperatorKind, boundUnaryOperator.Operand)
            Dim operand As Lazy(Of IOperation) = New Lazy(Of IOperation)(Function() Create(boundUnaryOperator.Operand))
            Dim usesOperatorMethod As Boolean = False
            Dim operatorMethod As IMethodSymbol = Nothing
            Dim isInvalid As Boolean = boundUnaryOperator.HasErrors
            Dim syntax As SyntaxNode = boundUnaryOperator.Syntax
            Dim type As ITypeSymbol = boundUnaryOperator.Type
            Dim constantValue As [Optional](Of Object) = ConvertToOptional(boundUnaryOperator.ConstantValueOpt)
            Return New LazyUnaryOperatorExpression(unaryOperationKind, operand, usesOperatorMethod, operatorMethod, isInvalid, syntax, type, constantValue)
        End Function

        Private Shared Function CreateBoundUserDefinedUnaryOperatorOperation(boundUserDefinedUnaryOperator As BoundUserDefinedUnaryOperator) As IUnaryOperatorExpression
            Dim unaryOperationKind As UnaryOperationKind = Helper.DeriveUnaryOperationKind(boundUserDefinedUnaryOperator.OperatorKind)
            Dim operand As Lazy(Of IOperation) = New Lazy(Of IOperation)(Function()
                                                                             If boundUserDefinedUnaryOperator.UnderlyingExpression.Kind = BoundKind.Call Then
                                                                                 Return Create(boundUserDefinedUnaryOperator.Operand)
                                                                             Else
                                                                                 Return GetChildOfBadExpression(boundUserDefinedUnaryOperator.UnderlyingExpression, 0)
                                                                             End If
                                                                         End Function)
            Dim operatorMethod As IMethodSymbol = If(boundUserDefinedUnaryOperator.UnderlyingExpression.Kind = BoundKind.Call, boundUserDefinedUnaryOperator.Call.Method, Nothing)
            Dim usesOperatorMethod As Boolean = operatorMethod IsNot Nothing
            Dim isInvalid As Boolean = boundUserDefinedUnaryOperator.HasErrors
            Dim syntax As SyntaxNode = boundUserDefinedUnaryOperator.Syntax
            Dim type As ITypeSymbol = boundUserDefinedUnaryOperator.Type
            Dim constantValue As [Optional](Of Object) = ConvertToOptional(boundUserDefinedUnaryOperator.ConstantValueOpt)
            Return New LazyUnaryOperatorExpression(unaryOperationKind, operand, usesOperatorMethod, operatorMethod, isInvalid, syntax, type, constantValue)
        End Function

        Private Shared Function CreateBoundBinaryOperatorOperation(boundBinaryOperator As BoundBinaryOperator) As IBinaryOperatorExpression
            Dim binaryOperationKind As BinaryOperationKind = Helper.DeriveBinaryOperationKind(boundBinaryOperator.OperatorKind, boundBinaryOperator.Left)
            Dim leftOperand As Lazy(Of IOperation) = New Lazy(Of IOperation)(Function() Create(boundBinaryOperator.Left))
            Dim rightOperand As Lazy(Of IOperation) = New Lazy(Of IOperation)(Function() Create(boundBinaryOperator.Right))
            Dim usesOperatorMethod As Boolean = False
            Dim operatorMethod As IMethodSymbol = Nothing
            Dim isInvalid As Boolean = boundBinaryOperator.HasErrors
            Dim syntax As SyntaxNode = boundBinaryOperator.Syntax
            Dim type As ITypeSymbol = boundBinaryOperator.Type
            Dim constantValue As [Optional](Of Object) = ConvertToOptional(boundBinaryOperator.ConstantValueOpt)
            Return New LazyBinaryOperatorExpression(binaryOperationKind, leftOperand, rightOperand, usesOperatorMethod, operatorMethod, isInvalid, syntax, type, constantValue)
        End Function

        Private Shared Function CreateBoundUserDefinedBinaryOperatorOperation(boundUserDefinedBinaryOperator As BoundUserDefinedBinaryOperator) As IBinaryOperatorExpression
            Dim binaryOperationKind As BinaryOperationKind = Helper.DeriveBinaryOperationKind(boundUserDefinedBinaryOperator.OperatorKind)
            Dim leftOperand As Lazy(Of IOperation) = New Lazy(Of IOperation)(Function() GetUserDefinedBinaryOperatorChild(boundUserDefinedBinaryOperator, 0))
            Dim rightOperand As Lazy(Of IOperation) = New Lazy(Of IOperation)(Function() GetUserDefinedBinaryOperatorChild(boundUserDefinedBinaryOperator, 1))
            Dim operatorMethod As IMethodSymbol = If(boundUserDefinedBinaryOperator.UnderlyingExpression.Kind = BoundKind.Call, boundUserDefinedBinaryOperator.Call.Method, Nothing)
            Dim usesOperatorMethod As Boolean = operatorMethod IsNot Nothing
            Dim isInvalid As Boolean = boundUserDefinedBinaryOperator.HasErrors
            Dim syntax As SyntaxNode = boundUserDefinedBinaryOperator.Syntax
            Dim type As ITypeSymbol = boundUserDefinedBinaryOperator.Type
            Dim constantValue As [Optional](Of Object) = ConvertToOptional(boundUserDefinedBinaryOperator.ConstantValueOpt)
            Return New LazyBinaryOperatorExpression(binaryOperationKind, leftOperand, rightOperand, usesOperatorMethod, operatorMethod, isInvalid, syntax, type, constantValue)
        End Function

        Private Shared Function CreateBoundBinaryConditionalExpressionOperation(boundBinaryConditionalExpression As BoundBinaryConditionalExpression) As INullCoalescingExpression
            Dim primaryOperand As Lazy(Of IOperation) = New Lazy(Of IOperation)(Function() Create(boundBinaryConditionalExpression.TestExpression))
            Dim secondaryOperand As Lazy(Of IOperation) = New Lazy(Of IOperation)(Function() Create(boundBinaryConditionalExpression.ElseExpression))
            Dim isInvalid As Boolean = boundBinaryConditionalExpression.HasErrors
            Dim syntax As SyntaxNode = boundBinaryConditionalExpression.Syntax
            Dim type As ITypeSymbol = boundBinaryConditionalExpression.Type
            Dim constantValue As [Optional](Of Object) = ConvertToOptional(boundBinaryConditionalExpression.ConstantValueOpt)
            Return New LazyNullCoalescingExpression(primaryOperand, secondaryOperand, isInvalid, syntax, type, constantValue)
        End Function

        Private Shared Function CreateBoundUserDefinedShortCircuitingOperatorOperation(boundUserDefinedShortCircuitingOperator As BoundUserDefinedShortCircuitingOperator) As IBinaryOperatorExpression
            Dim binaryOperationKind As BinaryOperationKind = If((boundUserDefinedShortCircuitingOperator.BitwiseOperator.OperatorKind And BinaryOperatorKind.And) <> 0, BinaryOperationKind.OperatorMethodConditionalAnd, BinaryOperationKind.OperatorMethodConditionalOr)
            Dim leftOperand As Lazy(Of IOperation) = New Lazy(Of IOperation)(Function() Create(boundUserDefinedShortCircuitingOperator.LeftOperand))
            Dim rightOperand As Lazy(Of IOperation) = New Lazy(Of IOperation)(Function() Create(boundUserDefinedShortCircuitingOperator.BitwiseOperator.Right))
            Dim usesOperatorMethod As Boolean = True
            Dim operatorMethod As IMethodSymbol = boundUserDefinedShortCircuitingOperator.BitwiseOperator.Call.Method
            Dim isInvalid As Boolean = boundUserDefinedShortCircuitingOperator.HasErrors
            Dim syntax As SyntaxNode = boundUserDefinedShortCircuitingOperator.Syntax
            Dim type As ITypeSymbol = boundUserDefinedShortCircuitingOperator.Type
            Dim constantValue As [Optional](Of Object) = ConvertToOptional(boundUserDefinedShortCircuitingOperator.ConstantValueOpt)
            Return New LazyBinaryOperatorExpression(binaryOperationKind, leftOperand, rightOperand, usesOperatorMethod, operatorMethod, isInvalid, syntax, type, constantValue)
        End Function

        Private Shared Function CreateBoundBadExpressionOperation(boundBadExpression As BoundBadExpression) As IInvalidExpression
            Dim children As Lazy(Of ImmutableArray(Of IOperation)) = New Lazy(Of ImmutableArray(Of IOperation))(Function() boundBadExpression.ChildBoundNodes.SelectAsArray(Function(n) Create(n)))
            Dim isInvalid As Boolean = boundBadExpression.HasErrors
            Dim syntax As SyntaxNode = boundBadExpression.Syntax
            Dim type As ITypeSymbol = boundBadExpression.Type
            Dim constantValue As [Optional](Of Object) = ConvertToOptional(boundBadExpression.ConstantValueOpt)
            Return New LazyInvalidExpression(children, isInvalid, syntax, type, constantValue)
        End Function

        Private Shared Function CreateBoundTryCastOperation(boundTryCast As BoundTryCast) As IConversionExpression
            Dim operand As Lazy(Of IOperation) = New Lazy(Of IOperation)(Function() Create(boundTryCast.Operand))
            Dim conversionKind As ConversionKind = Semantics.ConversionKind.TryCast
            Dim isExplicit As Boolean = True
            Dim usesOperatorMethod As Boolean = False
            Dim operatorMethod As IMethodSymbol = Nothing
            Dim isInvalid As Boolean = boundTryCast.HasErrors
            Dim syntax As SyntaxNode = boundTryCast.Syntax
            Dim type As ITypeSymbol = boundTryCast.Type
            Dim constantValue As [Optional](Of Object) = ConvertToOptional(boundTryCast.ConstantValueOpt)
            Return New LazyConversionExpression(operand, conversionKind, isExplicit, usesOperatorMethod, operatorMethod, isInvalid, syntax, type, constantValue)
        End Function

        Private Shared Function CreateBoundDirectCastOperation(boundDirectCast As BoundDirectCast) As IConversionExpression
            Dim operand As Lazy(Of IOperation) = New Lazy(Of IOperation)(Function() Create(boundDirectCast.Operand))
            Dim conversionKind As ConversionKind = Semantics.ConversionKind.Cast
            Dim isExplicit As Boolean = True
            Dim usesOperatorMethod As Boolean = False
            Dim operatorMethod As IMethodSymbol = Nothing
            Dim isInvalid As Boolean = boundDirectCast.HasErrors
            Dim syntax As SyntaxNode = boundDirectCast.Syntax
            Dim type As ITypeSymbol = boundDirectCast.Type
            Dim constantValue As [Optional](Of Object) = ConvertToOptional(boundDirectCast.ConstantValueOpt)
            Return New LazyConversionExpression(operand, conversionKind, isExplicit, usesOperatorMethod, operatorMethod, isInvalid, syntax, type, constantValue)
        End Function

        Private Shared Function CreateBoundConversionOperation(boundConversion As BoundConversion) As IConversionExpression
            Dim operand As Lazy(Of IOperation) = New Lazy(Of IOperation)(Function() Create(boundConversion.Operand))
            Dim conversionKind As ConversionKind = Semantics.ConversionKind.Basic
            Dim isExplicit As Boolean = boundConversion.ExplicitCastInCode
            Dim usesOperatorMethod As Boolean = False
            Dim operatorMethod As IMethodSymbol = Nothing
            Dim isInvalid As Boolean = boundConversion.HasErrors
            Dim syntax As SyntaxNode = boundConversion.Syntax
            Dim type As ITypeSymbol = boundConversion.Type
            Dim constantValue As [Optional](Of Object) = ConvertToOptional(boundConversion.ConstantValueOpt)
            Return New LazyConversionExpression(operand, conversionKind, isExplicit, usesOperatorMethod, operatorMethod, isInvalid, syntax, type, constantValue)
        End Function

        Private Shared Function CreateBoundUserDefinedConversionOperation(boundUserDefinedConversion As BoundUserDefinedConversion) As IConversionExpression
            Dim operand As Lazy(Of IOperation) = New Lazy(Of IOperation)(Function() Create(boundUserDefinedConversion.Operand))
            Dim conversionKind As ConversionKind = Semantics.ConversionKind.OperatorMethod
            Dim isExplicit As Boolean = Not boundUserDefinedConversion.WasCompilerGenerated
            Dim usesOperatorMethod As Boolean = True
            Dim operatorMethod As IMethodSymbol = boundUserDefinedConversion.Call.Method
            Dim isInvalid As Boolean = boundUserDefinedConversion.HasErrors
            Dim syntax As SyntaxNode = boundUserDefinedConversion.Syntax
            Dim type As ITypeSymbol = boundUserDefinedConversion.Type
            Dim constantValue As [Optional](Of Object) = ConvertToOptional(boundUserDefinedConversion.ConstantValueOpt)
            Return New LazyConversionExpression(operand, conversionKind, isExplicit, usesOperatorMethod, operatorMethod, isInvalid, syntax, type, constantValue)
        End Function

        Private Shared Function CreateBoundTernaryConditionalExpressionOperation(boundTernaryConditionalExpression As BoundTernaryConditionalExpression) As IConditionalChoiceExpression
            Dim condition As Lazy(Of IOperation) = New Lazy(Of IOperation)(Function() Create(boundTernaryConditionalExpression.Condition))
            Dim ifTrueValue As Lazy(Of IOperation) = New Lazy(Of IOperation)(Function() Create(boundTernaryConditionalExpression.WhenTrue))
            Dim ifFalseValue As Lazy(Of IOperation) = New Lazy(Of IOperation)(Function() Create(boundTernaryConditionalExpression.WhenFalse))
            Dim isInvalid As Boolean = boundTernaryConditionalExpression.HasErrors
            Dim syntax As SyntaxNode = boundTernaryConditionalExpression.Syntax
            Dim type As ITypeSymbol = boundTernaryConditionalExpression.Type
            Dim constantValue As [Optional](Of Object) = ConvertToOptional(boundTernaryConditionalExpression.ConstantValueOpt)
            Return New LazyConditionalChoiceExpression(condition, ifTrueValue, ifFalseValue, isInvalid, syntax, type, constantValue)
        End Function

        Private Shared Function CreateBoundTypeOfOperation(boundTypeOf As BoundTypeOf) As IIsTypeExpression
            Dim operand As Lazy(Of IOperation) = New Lazy(Of IOperation)(Function() Create(boundTypeOf.Operand))
            Dim isType As ITypeSymbol = boundTypeOf.TargetType
            Dim isInvalid As Boolean = boundTypeOf.HasErrors
            Dim syntax As SyntaxNode = boundTypeOf.Syntax
            Dim type As ITypeSymbol = boundTypeOf.Type
            Dim constantValue As [Optional](Of Object) = ConvertToOptional(boundTypeOf.ConstantValueOpt)
            Return New LazyIsTypeExpression(operand, isType, isInvalid, syntax, type, constantValue)
        End Function

        Private Shared Function CreateBoundObjectCreationExpressionOperation(boundObjectCreationExpression As BoundObjectCreationExpression) As IObjectCreationExpression
            Dim constructor As IMethodSymbol = boundObjectCreationExpression.ConstructorOpt
            Dim memberInitializers As Lazy(Of ImmutableArray(Of IOperation)) = New Lazy(Of ImmutableArray(Of IOperation))(
                Function()
                    Return GetObjectCreationInitializers(boundObjectCreationExpression)
                End Function)

            Debug.Assert(boundObjectCreationExpression.ConstructorOpt IsNot Nothing OrElse boundObjectCreationExpression.Arguments.IsEmpty())
            Dim argumentsInEvaluationOrder As Lazy(Of ImmutableArray(Of IArgument)) = New Lazy(Of ImmutableArray(Of IArgument))(
                Function()
                    Return If(boundObjectCreationExpression.ConstructorOpt Is Nothing,
                        ImmutableArray(Of IArgument).Empty,
                        DeriveArguments(boundObjectCreationExpression.Arguments, boundObjectCreationExpression.ConstructorOpt.Parameters))
                End Function)

            Dim isInvalid As Boolean = boundObjectCreationExpression.HasErrors
            Dim syntax As SyntaxNode = boundObjectCreationExpression.Syntax
            Dim type As ITypeSymbol = boundObjectCreationExpression.Type
            Dim constantValue As [Optional](Of Object) = ConvertToOptional(boundObjectCreationExpression.ConstantValueOpt)
            Return New LazyObjectCreationExpression(constructor, memberInitializers, argumentsInEvaluationOrder, isInvalid, syntax, type, constantValue)
        End Function

        Private Shared Function CreateBoundNewTOperation(boundNewT As BoundNewT) As ITypeParameterObjectCreationExpression
            Dim isInvalid As Boolean = boundNewT.HasErrors
            Dim syntax As SyntaxNode = boundNewT.Syntax
            Dim type As ITypeSymbol = boundNewT.Type
            Dim constantValue As [Optional](Of Object) = ConvertToOptional(boundNewT.ConstantValueOpt)
            Return New TypeParameterObjectCreationExpression(isInvalid, syntax, type, constantValue)
        End Function

        Private Shared Function CreateBoundArrayCreationOperation(boundArrayCreation As BoundArrayCreation) As IArrayCreationExpression
            Dim elementType As ITypeSymbol = TryCast(boundArrayCreation.Type, IArrayTypeSymbol)?.ElementType
            Dim dimensionSizes As Lazy(Of ImmutableArray(Of IOperation)) = New Lazy(Of ImmutableArray(Of IOperation))(Function() boundArrayCreation.Bounds.SelectAsArray(Function(n) Create(n)))
            Dim initializer As Lazy(Of IArrayInitializer) = New Lazy(Of IArrayInitializer)(Function() DirectCast(Create(boundArrayCreation.InitializerOpt), IArrayInitializer))
            Dim isInvalid As Boolean = boundArrayCreation.HasErrors
            Dim syntax As SyntaxNode = boundArrayCreation.Syntax
            Dim type As ITypeSymbol = boundArrayCreation.Type
            Dim constantValue As [Optional](Of Object) = ConvertToOptional(boundArrayCreation.ConstantValueOpt)
            Return New LazyArrayCreationExpression(elementType, dimensionSizes, initializer, isInvalid, syntax, type, constantValue)
        End Function

        Private Shared Function CreateBoundArrayInitializationOperation(boundArrayInitialization As BoundArrayInitialization) As IArrayInitializer
            Dim elementValues As Lazy(Of ImmutableArray(Of IOperation)) = New Lazy(Of ImmutableArray(Of IOperation))(Function() boundArrayInitialization.Initializers.SelectAsArray(Function(n) Create(n)))
            Dim isInvalid As Boolean = boundArrayInitialization.HasErrors
            Dim syntax As SyntaxNode = boundArrayInitialization.Syntax
            Dim type As ITypeSymbol = boundArrayInitialization.Type
            Dim constantValue As [Optional](Of Object) = ConvertToOptional(boundArrayInitialization.ConstantValueOpt)
            Return New LazyArrayInitializer(elementValues, isInvalid, syntax, type, constantValue)
        End Function

        Private Shared Function CreateBoundPropertyAccessOperation(boundPropertyAccess As BoundPropertyAccess) As IPropertyReferenceExpression
            Dim instance As Lazy(Of IOperation) = New Lazy(Of IOperation)(
                Function()
                    If boundPropertyAccess.PropertySymbol.IsShared Then
                        Return Nothing
                    Else
                        Return Create(boundPropertyAccess.ReceiverOpt)
                    End If
                End Function)

            Dim [property] As IPropertySymbol = boundPropertyAccess.PropertySymbol
            Dim member As ISymbol = boundPropertyAccess.PropertySymbol
            Dim argumentsInEvaluationOrder As Lazy(Of ImmutableArray(Of IArgument)) = New Lazy(Of ImmutableArray(Of IArgument))(Function() DeriveArguments(boundPropertyAccess.Arguments, boundPropertyAccess.PropertySymbol.Parameters))
            Dim isInvalid As Boolean = boundPropertyAccess.HasErrors
            Dim syntax As SyntaxNode = boundPropertyAccess.Syntax
            Dim type As ITypeSymbol = boundPropertyAccess.Type
            Dim constantValue As [Optional](Of Object) = ConvertToOptional(boundPropertyAccess.ConstantValueOpt)
            Return If(boundPropertyAccess.Arguments.Length > 0,
                DirectCast(New LazyIndexedPropertyReferenceExpression([property], instance, member, argumentsInEvaluationOrder, isInvalid, syntax, type, constantValue), IPropertyReferenceExpression),
                New LazyPropertyReferenceExpression([property], instance, member, isInvalid, syntax, type, constantValue))
        End Function

        Private Shared Function CreateBoundEventAccessOperation(boundEventAccess As BoundEventAccess) As IEventReferenceExpression
            Dim instance As Lazy(Of IOperation) = New Lazy(Of IOperation)(
                Function()
                    If boundEventAccess.EventSymbol.IsShared Then
                        Return Nothing
                    Else
                        Return Create(boundEventAccess.ReceiverOpt)
                    End If
                End Function)

            Dim [event] As IEventSymbol = boundEventAccess.EventSymbol
            Dim member As ISymbol = boundEventAccess.EventSymbol
            Dim isInvalid As Boolean = boundEventAccess.HasErrors
            Dim syntax As SyntaxNode = boundEventAccess.Syntax
            Dim type As ITypeSymbol = boundEventAccess.Type
            Dim constantValue As [Optional](Of Object) = ConvertToOptional(boundEventAccess.ConstantValueOpt)
            Return New LazyEventReferenceExpression([event], instance, member, isInvalid, syntax, type, constantValue)
        End Function

        Private Shared Function CreateBoundFieldAccessOperation(boundFieldAccess As BoundFieldAccess) As IFieldReferenceExpression
            Dim field As IFieldSymbol = boundFieldAccess.FieldSymbol
            Dim instance As Lazy(Of IOperation) = New Lazy(Of IOperation)(
                Function()
                    If boundFieldAccess.FieldSymbol.IsShared Then
                        Return Nothing
                    Else
                        Return Create(boundFieldAccess.ReceiverOpt)
                    End If
                End Function)

            Dim member As ISymbol = boundFieldAccess.FieldSymbol
            Dim isInvalid As Boolean = boundFieldAccess.HasErrors
            Dim syntax As SyntaxNode = boundFieldAccess.Syntax
            Dim type As ITypeSymbol = boundFieldAccess.Type
            Dim constantValue As [Optional](Of Object) = ConvertToOptional(boundFieldAccess.ConstantValueOpt)
            Return New LazyFieldReferenceExpression(field, instance, member, isInvalid, syntax, type, constantValue)
        End Function

        Private Shared Function CreateBoundConditionalAccessOperation(boundConditionalAccess As BoundConditionalAccess) As IConditionalAccessExpression
            Dim conditionalValue As Lazy(Of IOperation) = New Lazy(Of IOperation)(Function() Create(boundConditionalAccess.AccessExpression))
            Dim conditionalInstance As Lazy(Of IOperation) = New Lazy(Of IOperation)(Function() Create(boundConditionalAccess.Receiver))
            Dim isInvalid As Boolean = boundConditionalAccess.HasErrors
            Dim syntax As SyntaxNode = boundConditionalAccess.Syntax
            Dim type As ITypeSymbol = boundConditionalAccess.Type
            Dim constantValue As [Optional](Of Object) = ConvertToOptional(boundConditionalAccess.ConstantValueOpt)
            Return New LazyConditionalAccessExpression(conditionalValue, conditionalInstance, isInvalid, syntax, type, constantValue)
        End Function

        Private Shared Function CreateBoundConditionalAccessReceiverPlaceholderOperation(boundConditionalAccessReceiverPlaceholder As BoundConditionalAccessReceiverPlaceholder) As IConditionalAccessInstanceExpression
            Dim isInvalid As Boolean = boundConditionalAccessReceiverPlaceholder.HasErrors
            Dim syntax As SyntaxNode = boundConditionalAccessReceiverPlaceholder.Syntax
            Dim type As ITypeSymbol = boundConditionalAccessReceiverPlaceholder.Type
            Dim constantValue As [Optional](Of Object) = ConvertToOptional(boundConditionalAccessReceiverPlaceholder.ConstantValueOpt)
            Return New ConditionalAccessInstanceExpression(isInvalid, syntax, type, constantValue)
        End Function

        Private Shared Function CreateBoundParameterOperation(boundParameter As BoundParameter) As IParameterReferenceExpression
            Dim parameter As IParameterSymbol = boundParameter.ParameterSymbol
            Dim isInvalid As Boolean = boundParameter.HasErrors
            Dim syntax As SyntaxNode = boundParameter.Syntax
            Dim type As ITypeSymbol = boundParameter.Type
            Dim constantValue As [Optional](Of Object) = ConvertToOptional(boundParameter.ConstantValueOpt)
            Return New ParameterReferenceExpression(parameter, isInvalid, syntax, type, constantValue)
        End Function

        Private Shared Function CreateBoundLocalOperation(boundLocal As BoundLocal) As ILocalReferenceExpression
            Dim local As ILocalSymbol = boundLocal.LocalSymbol
            Dim isInvalid As Boolean = boundLocal.HasErrors
            Dim syntax As SyntaxNode = boundLocal.Syntax
            Dim type As ITypeSymbol = boundLocal.Type
            Dim constantValue As [Optional](Of Object) = ConvertToOptional(boundLocal.ConstantValueOpt)
            Return New LocalReferenceExpression(local, isInvalid, syntax, type, constantValue)
        End Function

        Private Shared Function CreateBoundLateMemberAccessOperation(boundLateMemberAccess As BoundLateMemberAccess) As ILateBoundMemberReferenceExpression
            Dim instance As Lazy(Of IOperation) = New Lazy(Of IOperation)(Function() Create(boundLateMemberAccess.ReceiverOpt))
            Dim memberName As String = boundLateMemberAccess.NameOpt
            Dim isInvalid As Boolean = boundLateMemberAccess.HasErrors
            Dim syntax As SyntaxNode = boundLateMemberAccess.Syntax
            Dim type As ITypeSymbol = boundLateMemberAccess.Type
            Dim constantValue As [Optional](Of Object) = ConvertToOptional(boundLateMemberAccess.ConstantValueOpt)
            Return New LazyLateBoundMemberReferenceExpression(instance, memberName, isInvalid, syntax, type, constantValue)
        End Function

        Private Shared Function CreateBoundFieldInitializerOperation(boundFieldInitializer As BoundFieldInitializer) As IFieldInitializer
            Dim initializedFields As ImmutableArray(Of IFieldSymbol) = ImmutableArray(Of IFieldSymbol).CastUp(boundFieldInitializer.InitializedFields)
            Dim value As Lazy(Of IOperation) = New Lazy(Of IOperation)(Function() Create(boundFieldInitializer.InitialValue))
            Dim kind As OperationKind = OperationKind.FieldInitializerAtDeclaration
            Dim isInvalid As Boolean = boundFieldInitializer.HasErrors
            Dim syntax As SyntaxNode = boundFieldInitializer.Syntax
            Dim type As ITypeSymbol = Nothing
            Dim constantValue As [Optional](Of Object) = New [Optional](Of Object)()
            Return New LazyFieldInitializer(initializedFields, value, kind, isInvalid, syntax, type, constantValue)
        End Function

        Private Shared Function CreateBoundPropertyInitializerOperation(boundPropertyInitializer As BoundPropertyInitializer) As IPropertyInitializer
            Dim initializedProperty As IPropertySymbol = boundPropertyInitializer.InitializedProperties.FirstOrDefault()
            Dim value As Lazy(Of IOperation) = New Lazy(Of IOperation)(Function() Create(boundPropertyInitializer.InitialValue))
            Dim kind As OperationKind = OperationKind.PropertyInitializerAtDeclaration
            Dim isInvalid As Boolean = boundPropertyInitializer.HasErrors
            Dim syntax As SyntaxNode = boundPropertyInitializer.Syntax
            Dim type As ITypeSymbol = Nothing
            Dim constantValue As [Optional](Of Object) = New [Optional](Of Object)()
            Return New LazyPropertyInitializer(initializedProperty, value, kind, isInvalid, syntax, type, constantValue)
        End Function

        Private Shared Function CreateBoundParameterEqualsValueOperation(boundParameterEqualsValue As BoundParameterEqualsValue) As IParameterInitializer
            Dim parameter As IParameterSymbol = boundParameterEqualsValue.Parameter
            Dim value As Lazy(Of IOperation) = New Lazy(Of IOperation)(Function() Create(boundParameterEqualsValue.Value))
            Dim kind As OperationKind = OperationKind.ParameterInitializerAtDeclaration
            Dim isInvalid As Boolean = boundParameterEqualsValue.Value.HasErrors
            Dim syntax As SyntaxNode = boundParameterEqualsValue.Syntax
            Dim type As ITypeSymbol = Nothing
            Dim constantValue As [Optional](Of Object) = New [Optional](Of Object)()
            Return New LazyParameterInitializer(parameter, value, kind, isInvalid, syntax, type, constantValue)
        End Function

        Private Shared Function CreateBoundRValuePlaceholderOperation(boundRValuePlaceholder As BoundRValuePlaceholder) As IPlaceholderExpression
            Dim isInvalid As Boolean = boundRValuePlaceholder.HasErrors
            Dim syntax As SyntaxNode = boundRValuePlaceholder.Syntax
            Dim type As ITypeSymbol = boundRValuePlaceholder.Type
            Dim constantValue As [Optional](Of Object) = ConvertToOptional(boundRValuePlaceholder.ConstantValueOpt)
            Return New PlaceholderExpression(isInvalid, syntax, type, constantValue)
        End Function

        Private Shared Function CreateBoundIfStatementOperation(boundIfStatement As BoundIfStatement) As IIfStatement
            Dim condition As Lazy(Of IOperation) = New Lazy(Of IOperation)(Function() Create(boundIfStatement.Condition))
            Dim ifTrueStatement As Lazy(Of IOperation) = New Lazy(Of IOperation)(Function() Create(boundIfStatement.Consequence))
            Dim ifFalseStatement As Lazy(Of IOperation) = New Lazy(Of IOperation)(Function() Create(boundIfStatement.AlternativeOpt))
            Dim isInvalid As Boolean = boundIfStatement.HasErrors
            Dim syntax As SyntaxNode = boundIfStatement.Syntax
            Dim type As ITypeSymbol = Nothing
            Dim constantValue As [Optional](Of Object) = New [Optional](Of Object)()
            Return New LazyIfStatement(condition, ifTrueStatement, ifFalseStatement, isInvalid, syntax, type, constantValue)
        End Function

        Private Shared Function CreateBoundSelectStatementOperation(boundSelectStatement As BoundSelectStatement) As ISwitchStatement
            Dim value As Lazy(Of IOperation) = New Lazy(Of IOperation)(Function() Create(boundSelectStatement.ExpressionStatement.Expression))
            Dim cases As Lazy(Of ImmutableArray(Of ISwitchCase)) = New Lazy(Of ImmutableArray(Of ISwitchCase))(Function() GetSwitchStatementCases(boundSelectStatement))
            Dim isInvalid As Boolean = boundSelectStatement.HasErrors
            Dim syntax As SyntaxNode = boundSelectStatement.Syntax
            Dim type As ITypeSymbol = Nothing
            Dim constantValue As [Optional](Of Object) = New [Optional](Of Object)()
            Return New LazySwitchStatement(value, cases, isInvalid, syntax, type, constantValue)
        End Function

        Private Shared Function CreateBoundSimpleCaseClauseOperation(boundSimpleCaseClause As BoundSimpleCaseClause) As ISingleValueCaseClause
            Dim value As Lazy(Of IOperation) = New Lazy(Of IOperation)(Function() Create(GetSingleValueCaseClauseValue(boundSimpleCaseClause)))
            Dim equality As BinaryOperationKind = GetSingleValueCaseClauseEquality(boundSimpleCaseClause)
            Dim caseKind As CaseKind = CaseKind.SingleValue
            Dim isInvalid As Boolean = boundSimpleCaseClause.HasErrors
            Dim syntax As SyntaxNode = boundSimpleCaseClause.Syntax
            Dim type As ITypeSymbol = Nothing
            Dim constantValue As [Optional](Of Object) = New [Optional](Of Object)()
            Return New LazySingleValueCaseClause(value, equality, caseKind, isInvalid, syntax, type, constantValue)
        End Function

        Private Shared Function CreateBoundRangeCaseClauseOperation(boundRangeCaseClause As BoundRangeCaseClause) As IRangeCaseClause
            Dim minimumValue As Lazy(Of IOperation) = New Lazy(Of IOperation)(
                Function()
                    If boundRangeCaseClause.LowerBoundOpt IsNot Nothing Then
                        Return Create(boundRangeCaseClause.LowerBoundOpt)
                    End If

                    If boundRangeCaseClause.LowerBoundConditionOpt.Kind = BoundKind.BinaryOperator Then
                        Dim lowerBound As BoundBinaryOperator = DirectCast(boundRangeCaseClause.LowerBoundConditionOpt, BoundBinaryOperator)
                        If lowerBound.OperatorKind = BinaryOperatorKind.GreaterThanOrEqual Then
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
                        If upperBound.OperatorKind = BinaryOperatorKind.LessThanOrEqual Then
                            Return Create(upperBound.Right)
                        End If
                    End If

                    Return Nothing
                End Function)
            Dim caseKind As CaseKind = CaseKind.Range
            Dim isInvalid As Boolean = boundRangeCaseClause.HasErrors
            Dim syntax As SyntaxNode = boundRangeCaseClause.Syntax
            Dim type As ITypeSymbol = Nothing
            Dim constantValue As [Optional](Of Object) = New [Optional](Of Object)()
            Return New LazyRangeCaseClause(minimumValue, maximumValue, caseKind, isInvalid, syntax, type, constantValue)
        End Function

        Private Shared Function CreateBoundRelationalCaseClauseOperation(boundRelationalCaseClause As BoundRelationalCaseClause) As IRelationalCaseClause
            Dim valueExpression = GetRelationalCaseClauseValue(boundRelationalCaseClause)
            Dim value As Lazy(Of IOperation) = New Lazy(Of IOperation)(Function() Create(valueExpression))
            Dim relation As BinaryOperationKind = If(valueExpression IsNot Nothing, Helper.DeriveBinaryOperationKind(boundRelationalCaseClause.OperatorKind, valueExpression), BinaryOperationKind.Invalid)
            Dim caseKind As CaseKind = CaseKind.Relational
            Dim isInvalid As Boolean = boundRelationalCaseClause.HasErrors
            Dim syntax As SyntaxNode = boundRelationalCaseClause.Syntax
            Dim type As ITypeSymbol = Nothing
            Dim constantValue As [Optional](Of Object) = New [Optional](Of Object)()
            Return New LazyRelationalCaseClause(value, relation, caseKind, isInvalid, syntax, type, constantValue)
        End Function

        Private Shared Function CreateBoundDoLoopStatementOperation(boundDoLoopStatement As BoundDoLoopStatement) As IWhileUntilLoopStatement
            Dim isTopTest As Boolean = boundDoLoopStatement.ConditionIsTop
            Dim isWhile As Boolean = Not boundDoLoopStatement.ConditionIsUntil
            Dim condition As Lazy(Of IOperation) = New Lazy(Of IOperation)(Function() Create(boundDoLoopStatement.ConditionOpt))
            Dim loopKind As LoopKind = LoopKind.WhileUntil
            Dim body As Lazy(Of IOperation) = New Lazy(Of IOperation)(Function() Create(boundDoLoopStatement.Body))
            Dim isInvalid As Boolean = boundDoLoopStatement.HasErrors
            Dim syntax As SyntaxNode = boundDoLoopStatement.Syntax
            Dim type As ITypeSymbol = Nothing
            Dim constantValue As [Optional](Of Object) = New [Optional](Of Object)()
            Return New LazyWhileUntilLoopStatement(isTopTest, isWhile, condition, loopKind, body, isInvalid, syntax, type, constantValue)
        End Function

        Private Shared Function CreateBoundForToStatementOperation(boundForToStatement As BoundForToStatement) As IForLoopStatement
            Dim before As Lazy(Of ImmutableArray(Of IOperation)) = New Lazy(Of ImmutableArray(Of IOperation))(Function() GetForLoopStatementBefore(boundForToStatement))
            Dim atLoopBottom As Lazy(Of ImmutableArray(Of IOperation)) = New Lazy(Of ImmutableArray(Of IOperation))(Function() GetForLoopStatementAtLoopBottom(boundForToStatement))
            Dim locals As ImmutableArray(Of ILocalSymbol) = ImmutableArray(Of ILocalSymbol).Empty
            Dim condition As Lazy(Of IOperation) = New Lazy(Of IOperation)(Function() GetForWhileUntilLoopStatmentCondition(boundForToStatement))
            Dim loopKind As LoopKind = LoopKind.For
            Dim body As Lazy(Of IOperation) = New Lazy(Of IOperation)(Function() Create(boundForToStatement.Body))
            Dim isInvalid As Boolean = boundForToStatement.HasErrors
            Dim syntax As SyntaxNode = boundForToStatement.Syntax
            Dim type As ITypeSymbol = Nothing
            Dim constantValue As [Optional](Of Object) = New [Optional](Of Object)()
            Return New LazyForLoopStatement(before, atLoopBottom, locals, condition, loopKind, body, isInvalid, syntax, type, constantValue)
        End Function

        Private Shared Function CreateBoundForEachStatementOperation(boundForEachStatement As BoundForEachStatement) As IForEachLoopStatement
            Dim iterationVariable As ILocalSymbol = Nothing ' Manual
            Dim collection As Lazy(Of IOperation) = New Lazy(Of IOperation)(Function() Create(boundForEachStatement.Collection))
            Dim loopKind As LoopKind = LoopKind.ForEach
            Dim body As Lazy(Of IOperation) = New Lazy(Of IOperation)(Function() Create(boundForEachStatement.Body))
            Dim isInvalid As Boolean = boundForEachStatement.HasErrors
            Dim syntax As SyntaxNode = boundForEachStatement.Syntax
            Dim type As ITypeSymbol = Nothing
            Dim constantValue As [Optional](Of Object) = New [Optional](Of Object)()
            Return New LazyForEachLoopStatement(iterationVariable, collection, loopKind, body, isInvalid, syntax, type, constantValue)
        End Function

        Private Shared Function CreateBoundTryStatementOperation(boundTryStatement As BoundTryStatement) As ITryStatement
            Dim body As Lazy(Of IBlockStatement) = New Lazy(Of IBlockStatement)(Function() DirectCast(Create(boundTryStatement.TryBlock), IBlockStatement))
            Dim catches As Lazy(Of ImmutableArray(Of ICatchClause)) = New Lazy(Of ImmutableArray(Of ICatchClause))(Function() boundTryStatement.CatchBlocks.As(Of ICatchClause)())
            Dim finallyHandler As Lazy(Of IBlockStatement) = New Lazy(Of IBlockStatement)(Function() DirectCast(Create(boundTryStatement.FinallyBlockOpt), IBlockStatement))
            Dim isInvalid As Boolean = boundTryStatement.HasErrors
            Dim syntax As SyntaxNode = boundTryStatement.Syntax
            Dim type As ITypeSymbol = Nothing
            Dim constantValue As [Optional](Of Object) = New [Optional](Of Object)()
            Return New LazyTryStatement(body, catches, finallyHandler, isInvalid, syntax, type, constantValue)
        End Function

        Private Shared Function CreateBoundCatchBlockOperation(boundCatchBlock As BoundCatchBlock) As ICatchClause
            Dim handler As Lazy(Of IBlockStatement) = New Lazy(Of IBlockStatement)(Function() DirectCast(Create(boundCatchBlock.Body), IBlockStatement))
            Dim caughtType As ITypeSymbol = Nothing ' Manual
            Dim filter As Lazy(Of IOperation) = New Lazy(Of IOperation)(Function() Create(boundCatchBlock.ExceptionFilterOpt))
            Dim exceptionLocal As ILocalSymbol = boundCatchBlock.LocalOpt
            Dim isInvalid As Boolean = boundCatchBlock.HasErrors
            Dim syntax As SyntaxNode = boundCatchBlock.Syntax
            Dim type As ITypeSymbol = Nothing
            Dim constantValue As [Optional](Of Object) = New [Optional](Of Object)()
            Return New LazyCatchClause(handler, caughtType, filter, exceptionLocal, isInvalid, syntax, type, constantValue)
        End Function

        Private Shared Function CreateBoundBlockOperation(boundBlock As BoundBlock) As IBlockStatement
            Dim statements As Lazy(Of ImmutableArray(Of IOperation)) = New Lazy(Of ImmutableArray(Of IOperation))(Function() GetBlockStatementStatements(boundBlock))
            Dim locals As ImmutableArray(Of ILocalSymbol) = boundBlock.Locals.As(Of ILocalSymbol)()
            Dim isInvalid As Boolean = boundBlock.HasErrors
            Dim syntax As SyntaxNode = boundBlock.Syntax
            Dim type As ITypeSymbol = Nothing
            Dim constantValue As [Optional](Of Object) = New [Optional](Of Object)()
            Return New LazyBlockStatement(statements, locals, isInvalid, syntax, type, constantValue)
        End Function

        Private Shared Function CreateBoundBadStatementOperation(boundBadStatement As BoundBadStatement) As IInvalidStatement
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
            Dim isInvalid As Boolean = boundBadStatement.HasErrors
            Dim syntax As SyntaxNode = boundBadStatement.Syntax
            Dim type As ITypeSymbol = Nothing
            Dim constantValue As [Optional](Of Object) = New [Optional](Of Object)()
            Return New LazyInvalidStatement(children, isInvalid, syntax, type, constantValue)
        End Function

        Private Shared Function CreateBoundReturnStatementOperation(boundReturnStatement As BoundReturnStatement) As IReturnStatement
            Dim returnedValue As Lazy(Of IOperation) = New Lazy(Of IOperation)(Function() Create(boundReturnStatement.ExpressionOpt))
            Dim isInvalid As Boolean = boundReturnStatement.HasErrors
            Dim syntax As SyntaxNode = boundReturnStatement.Syntax
            Dim type As ITypeSymbol = Nothing
            Dim constantValue As [Optional](Of Object) = New [Optional](Of Object)()
            Return New LazyReturnStatement(returnedValue, isInvalid, syntax, type, constantValue)
        End Function

        Private Shared Function CreateBoundThrowStatementOperation(boundThrowStatement As BoundThrowStatement) As IThrowStatement
            Dim thrownObject As Lazy(Of IOperation) = New Lazy(Of IOperation)(Function() Create(boundThrowStatement.ExpressionOpt))
            Dim isInvalid As Boolean = boundThrowStatement.HasErrors
            Dim syntax As SyntaxNode = boundThrowStatement.Syntax
            Dim type As ITypeSymbol = Nothing
            Dim constantValue As [Optional](Of Object) = New [Optional](Of Object)()
            Return New LazyThrowStatement(thrownObject, isInvalid, syntax, type, constantValue)
        End Function

        Private Shared Function CreateBoundWhileStatementOperation(boundWhileStatement As BoundWhileStatement) As IWhileUntilLoopStatement
            Dim isTopTest As Boolean = True
            Dim isWhile As Boolean = True
            Dim condition As Lazy(Of IOperation) = New Lazy(Of IOperation)(Function() Create(boundWhileStatement.Condition))
            Dim loopKind As LoopKind = LoopKind.WhileUntil
            Dim body As Lazy(Of IOperation) = New Lazy(Of IOperation)(Function() Create(boundWhileStatement.Body))
            Dim isInvalid As Boolean = boundWhileStatement.HasErrors
            Dim syntax As SyntaxNode = boundWhileStatement.Syntax
            Dim type As ITypeSymbol = Nothing
            Dim constantValue As [Optional](Of Object) = New [Optional](Of Object)()
            Return New LazyWhileUntilLoopStatement(isTopTest, isWhile, condition, loopKind, body, isInvalid, syntax, type, constantValue)
        End Function

        Private Shared Function CreateBoundDimStatementOperation(boundDimStatement As BoundDimStatement) As IVariableDeclarationStatement
            Dim declarations As Lazy(Of ImmutableArray(Of IVariableDeclaration)) = New Lazy(Of ImmutableArray(Of IVariableDeclaration))(Function() GetVariableDeclarationStatementVariables(boundDimStatement))
            Dim isInvalid As Boolean = boundDimStatement.HasErrors
            Dim syntax As SyntaxNode = boundDimStatement.Syntax
            Dim type As ITypeSymbol = Nothing
            Dim constantValue As [Optional](Of Object) = New [Optional](Of Object)()
            Return New LazyVariableDeclarationStatement(declarations, isInvalid, syntax, type, constantValue)
        End Function

        Private Shared Function CreateBoundYieldStatementOperation(boundYieldStatement As BoundYieldStatement) As IReturnStatement
            Dim returnedValue As Lazy(Of IOperation) = New Lazy(Of IOperation)(Function() Create(boundYieldStatement.Expression))
            Dim isInvalid As Boolean = boundYieldStatement.HasErrors
            Dim syntax As SyntaxNode = boundYieldStatement.Syntax
            Dim type As ITypeSymbol = Nothing
            Dim constantValue As [Optional](Of Object) = New [Optional](Of Object)()
            Return New LazyReturnStatement(returnedValue, isInvalid, syntax, type, constantValue)
        End Function

        Private Shared Function CreateBoundLabelStatementOperation(boundLabelStatement As BoundLabelStatement) As ILabelStatement
            Dim label As ILabelSymbol = boundLabelStatement.Label
            Dim labeledStatement As Lazy(Of IOperation) = New Lazy(Of IOperation)(Function() Create(Nothing))
            Dim isInvalid As Boolean = boundLabelStatement.HasErrors
            Dim syntax As SyntaxNode = boundLabelStatement.Syntax
            Dim type As ITypeSymbol = Nothing
            Dim constantValue As [Optional](Of Object) = New [Optional](Of Object)()
            Return New LazyLabelStatement(label, labeledStatement, isInvalid, syntax, type, constantValue)
        End Function

        Private Shared Function CreateBoundGotoStatementOperation(boundGotoStatement As BoundGotoStatement) As IBranchStatement
            Dim target As ILabelSymbol = boundGotoStatement.Label
            Dim branchKind As BranchKind = BranchKind.GoTo
            Dim isInvalid As Boolean = boundGotoStatement.HasErrors
            Dim syntax As SyntaxNode = boundGotoStatement.Syntax
            Dim type As ITypeSymbol = Nothing
            Dim constantValue As [Optional](Of Object) = New [Optional](Of Object)()
            Return New BranchStatement(target, branchKind, isInvalid, syntax, type, constantValue)
        End Function

        Private Shared Function CreateBoundContinueStatementOperation(boundContinueStatement As BoundContinueStatement) As IBranchStatement
            Dim target As ILabelSymbol = boundContinueStatement.Label
            Dim branchKind As BranchKind = BranchKind.Continue
            Dim isInvalid As Boolean = boundContinueStatement.HasErrors
            Dim syntax As SyntaxNode = boundContinueStatement.Syntax
            Dim type As ITypeSymbol = Nothing
            Dim constantValue As [Optional](Of Object) = New [Optional](Of Object)()
            Return New BranchStatement(target, branchKind, isInvalid, syntax, type, constantValue)
        End Function

        Private Shared Function CreateBoundExitStatementOperation(boundExitStatement As BoundExitStatement) As IBranchStatement
            Dim target As ILabelSymbol = boundExitStatement.Label
            Dim branchKind As BranchKind = BranchKind.Break
            Dim isInvalid As Boolean = boundExitStatement.HasErrors
            Dim syntax As SyntaxNode = boundExitStatement.Syntax
            Dim type As ITypeSymbol = Nothing
            Dim constantValue As [Optional](Of Object) = New [Optional](Of Object)()
            Return New BranchStatement(target, branchKind, isInvalid, syntax, type, constantValue)
        End Function

        Private Shared Function CreateBoundSyncLockStatementOperation(boundSyncLockStatement As BoundSyncLockStatement) As ILockStatement
            Dim lockedObject As Lazy(Of IOperation) = New Lazy(Of IOperation)(Function() Create(boundSyncLockStatement.LockExpression))
            Dim body As Lazy(Of IOperation) = New Lazy(Of IOperation)(Function() Create(boundSyncLockStatement.Body))
            Dim isInvalid As Boolean = boundSyncLockStatement.HasErrors
            Dim syntax As SyntaxNode = boundSyncLockStatement.Syntax
            Dim type As ITypeSymbol = Nothing
            Dim constantValue As [Optional](Of Object) = New [Optional](Of Object)()
            Return New LazyLockStatement(lockedObject, body, isInvalid, syntax, type, constantValue)
        End Function

        Private Shared Function CreateBoundNoOpStatementOperation(boundNoOpStatement As BoundNoOpStatement) As IEmptyStatement
            Dim isInvalid As Boolean = boundNoOpStatement.HasErrors
            Dim syntax As SyntaxNode = boundNoOpStatement.Syntax
            Dim type As ITypeSymbol = Nothing
            Dim constantValue As [Optional](Of Object) = New [Optional](Of Object)()
            Return New EmptyStatement(isInvalid, syntax, type, constantValue)
        End Function

        Private Shared Function CreateBoundStopStatementOperation(boundStopStatement As BoundStopStatement) As IStopStatement
            Dim isInvalid As Boolean = boundStopStatement.HasErrors
            Dim syntax As SyntaxNode = boundStopStatement.Syntax
            Dim type As ITypeSymbol = Nothing
            Dim constantValue As [Optional](Of Object) = New [Optional](Of Object)()
            Return New StopStatement(isInvalid, syntax, type, constantValue)
        End Function

        Private Shared Function CreateBoundEndStatementOperation(boundEndStatement As BoundEndStatement) As IEndStatement
            Dim isInvalid As Boolean = boundEndStatement.HasErrors
            Dim syntax As SyntaxNode = boundEndStatement.Syntax
            Dim type As ITypeSymbol = Nothing
            Dim constantValue As [Optional](Of Object) = New [Optional](Of Object)()
            Return New EndStatement(isInvalid, syntax, type, constantValue)
        End Function

        Private Shared Function CreateBoundWithStatementOperation(boundWithStatement As BoundWithStatement) As IWithStatement
            Dim body As Lazy(Of IOperation) = New Lazy(Of IOperation)(Function() Create(boundWithStatement.Body))
            Dim value As Lazy(Of IOperation) = New Lazy(Of IOperation)(Function() Create(boundWithStatement.OriginalExpression))
            Dim isInvalid As Boolean = boundWithStatement.HasErrors
            Dim syntax As SyntaxNode = boundWithStatement.Syntax
            Dim type As ITypeSymbol = Nothing
            Dim constantValue As [Optional](Of Object) = New [Optional](Of Object)()
            Return New LazyWithStatement(body, value, isInvalid, syntax, type, constantValue)
        End Function

        Private Shared Function CreateBoundUsingStatementOperation(boundUsingStatement As BoundUsingStatement) As IUsingStatement
            Dim body As Lazy(Of IOperation) = New Lazy(Of IOperation)(Function() Create(boundUsingStatement.Body))
            Dim declaration As Lazy(Of IVariableDeclarationStatement) = New Lazy(Of IVariableDeclarationStatement)(Function() GetUsingStatementDeclaration(boundUsingStatement))
            Dim value As Lazy(Of IOperation) = New Lazy(Of IOperation)(Function() Create(boundUsingStatement.ResourceExpressionOpt))
            Dim isInvalid As Boolean = boundUsingStatement.HasErrors
            Dim syntax As SyntaxNode = boundUsingStatement.Syntax
            Dim type As ITypeSymbol = Nothing
            Dim constantValue As [Optional](Of Object) = New [Optional](Of Object)()
            Return New LazyUsingStatement(body, declaration, value, isInvalid, syntax, type, constantValue)
        End Function

        Private Shared Function CreateBoundExpressionStatementOperation(boundExpressionStatement As BoundExpressionStatement) As IExpressionStatement
            Dim expression As Lazy(Of IOperation) = New Lazy(Of IOperation)(Function() Create(boundExpressionStatement.Expression))
            Dim isInvalid As Boolean = boundExpressionStatement.HasErrors
            Dim syntax As SyntaxNode = boundExpressionStatement.Syntax
            Dim type As ITypeSymbol = Nothing
            Dim constantValue As [Optional](Of Object) = New [Optional](Of Object)()
            Return New LazyExpressionStatement(expression, isInvalid, syntax, type, constantValue)
        End Function

        Private Shared Function CreateBoundRaiseEventStatementOperation(boundRaiseEventStatement As BoundRaiseEventStatement) As IExpressionStatement
            Dim expression As Lazy(Of IOperation) = New Lazy(Of IOperation)(Function() Create(boundRaiseEventStatement.EventInvocation))
            Dim isInvalid As Boolean = boundRaiseEventStatement.HasErrors
            Dim syntax As SyntaxNode = boundRaiseEventStatement.Syntax
            Dim type As ITypeSymbol = Nothing
            Dim constantValue As [Optional](Of Object) = New [Optional](Of Object)()
            Return New LazyExpressionStatement(expression, isInvalid, syntax, type, constantValue)
        End Function

        Private Shared Function CreateBoundAddHandlerStatementOperation(boundAddHandlerStatement As BoundAddHandlerStatement) As IExpressionStatement
            Dim expression As Lazy(Of IOperation) = New Lazy(Of IOperation)(Function() GetAddHandlerStatementExpression(boundAddHandlerStatement))
            Dim isInvalid As Boolean = boundAddHandlerStatement.HasErrors
            Dim syntax As SyntaxNode = boundAddHandlerStatement.Syntax
            Dim type As ITypeSymbol = Nothing
            Dim constantValue As [Optional](Of Object) = New [Optional](Of Object)()
            Return New LazyExpressionStatement(expression, isInvalid, syntax, type, constantValue)
        End Function

        Private Shared Function CreateBoundRemoveHandlerStatementOperation(boundRemoveHandlerStatement As BoundRemoveHandlerStatement) As IExpressionStatement
            Dim expression As Lazy(Of IOperation) = New Lazy(Of IOperation)(Function() GetRemoveStatementExpression(boundRemoveHandlerStatement))
            Dim isInvalid As Boolean = boundRemoveHandlerStatement.HasErrors
            Dim syntax As SyntaxNode = boundRemoveHandlerStatement.Syntax
            Dim type As ITypeSymbol = Nothing
            Dim constantValue As [Optional](Of Object) = New [Optional](Of Object)()
            Return New LazyExpressionStatement(expression, isInvalid, syntax, type, constantValue)
        End Function

        Private Shared Function CreateBoundTupleExpressionOperation(boundTupleExpression As BoundTupleExpression) As ITupleExpression
            Dim elements As New Lazy(Of ImmutableArray(Of IOperation))(Function() GetTupleElements(boundTupleExpression))
            Dim isInvalid As Boolean = boundTupleExpression.HasErrors
            Dim syntax As SyntaxNode = boundTupleExpression.Syntax
            Dim type As ITypeSymbol = boundTupleExpression.Type
            Dim constantValue As [Optional](Of Object) = ConvertToOptional(boundTupleExpression.ConstantValueOpt)
            Return New LazyTupleExpression(elements, isInvalid, syntax, type, constantValue)
        End Function
    End Class
End Namespace


