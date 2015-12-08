' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Immutable
Imports Microsoft.CodeAnalysis.Semantics
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols

Namespace Microsoft.CodeAnalysis.VisualBasic

    Partial Class BoundStatement
        Implements IStatement

        Private ReadOnly Property IKind As OperationKind Implements IOperation.Kind
            Get
                Return Me.StatementKind()
            End Get
        End Property

        Private ReadOnly Property IIsInvalid As Boolean Implements IOperation.IsInvalid
            Get
                Return Me.HasErrors
            End Get
        End Property

        Private ReadOnly Property ISyntax As SyntaxNode Implements IOperation.Syntax
            Get
                Return Me.Syntax
            End Get
        End Property

        ' Protected MustOverride Function StatementKind() As OperationKind

        Protected Overridable Function StatementKind() As OperationKind
            Return OperationKind.None
        End Function
    End Class

    Partial Class BoundIfStatement
        Implements IIfStatement

        Private ReadOnly Property ICondition As IExpression Implements IIfStatement.Condition
            Get
                Return Me.Condition
            End Get
        End Property

        Private ReadOnly Property IIfTrue As IStatement Implements IIfStatement.IfTrue
            Get
                Return Me.Consequence
            End Get
        End Property

        Private ReadOnly Property IIfFalse As IStatement Implements IIfStatement.IfFalse
            Get
                Return Me.AlternativeOpt
            End Get
        End Property

        Protected Overrides Function StatementKind() As OperationKind
            Return OperationKind.IfStatement
        End Function

    End Class

    Partial Class BoundSelectStatement
        Implements ISwitchStatement

        Private ReadOnly Property ICases As ImmutableArray(Of ICase) Implements ISwitchStatement.Cases
            Get
                Return Me.CaseBlocks.As(Of ICase)()
            End Get
        End Property

        Private ReadOnly Property IValue As IExpression Implements ISwitchStatement.Value
            Get
                Return Me.ExpressionStatement.Expression
            End Get
        End Property

        Protected Overrides Function StatementKind() As OperationKind
            Return OperationKind.SwitchStatement
        End Function
    End Class

    Partial Class BoundCaseBlock
        Implements ICase

        Private ReadOnly Property IBody As ImmutableArray(Of IStatement) Implements ICase.Body
            Get
                Return ImmutableArray.Create(Of IStatement)(Me.Body)
            End Get
        End Property

        Private ReadOnly Property IClauses As ImmutableArray(Of ICaseClause) Implements ICase.Clauses
            Get
                If Me.CaseStatement.CaseClauses.IsEmpty Then
                    Return ImmutableArray.Create(CaseElseClause)
                End If

                Return Me.CaseStatement.CaseClauses.As(Of ICaseClause)()
            End Get
        End Property

        Protected Overrides Function StatementKind() As OperationKind
            Return OperationKind.None
        End Function

        Private Shared CaseElseClause As ICaseClause = New CaseElse

        Private Class CaseElse
            Implements ICaseClause

            Private ReadOnly Property ICaseClass As CaseKind Implements ICaseClause.CaseKind
                Get
                    Return CaseKind.Default
                End Get
            End Property
        End Class

    End Class

    Partial Class BoundCaseClause
        Implements ICaseClause

        Protected MustOverride ReadOnly Property ICaseClass As CaseKind Implements ICaseClause.CaseKind
    End Class

    Partial Class BoundSimpleCaseClause
        Implements ISingleValueCaseClause

        Private ReadOnly Property IEquality As BinaryOperationKind Implements ISingleValueCaseClause.Equality
            Get
                ' Can lifted operators appear here, and if so what is their correct treatment?
                Dim caseValue As BoundExpression = DirectCast(Me.IValue, BoundExpression)
                If caseValue IsNot Nothing Then
                    Select Case caseValue.Type.SpecialType
                        Case SpecialType.System_Int32, SpecialType.System_Int64, SpecialType.System_UInt32, SpecialType.System_UInt64, SpecialType.System_UInt16, SpecialType.System_Int16, SpecialType.System_SByte, SpecialType.System_Byte, SpecialType.System_Char
                            Return BinaryOperationKind.IntegerEquals

                        Case SpecialType.System_Boolean
                            Return BinaryOperationKind.BooleanEquals

                        Case SpecialType.System_String
                            Return BinaryOperationKind.StringEquals
                    End Select

                    If caseValue.Type.TypeKind = TypeKind.Enum Then
                        Return BinaryOperationKind.EnumEquals
                    End If
                End If

                Return BinaryOperationKind.None
            End Get
        End Property

        Private ReadOnly Property IValue As IExpression Implements ISingleValueCaseClause.Value
            Get
                If Me.ValueOpt IsNot Nothing Then
                    Return Me.ValueOpt
                End If

                If Me.ConditionOpt IsNot Nothing AndAlso Me.ConditionOpt.Kind = BoundKind.BinaryOperator Then
                    Dim value As BoundBinaryOperator = DirectCast(Me.ConditionOpt, BoundBinaryOperator)
                    If value.OperatorKind = BinaryOperatorKind.Equals Then
                        Return value.Right
                    End If
                End If

                Return Nothing
            End Get
        End Property

        Protected Overrides ReadOnly Property ICaseClass As CaseKind
            Get
                Return If(Me.IValue IsNot Nothing, CaseKind.SingleValue, CaseKind.Default)
            End Get
        End Property
    End Class

    Partial Class BoundRangeCaseClause
        Implements IRangeCaseClause

        Private ReadOnly Property IMaximumValue As IExpression Implements IRangeCaseClause.MaximumValue
            Get
                If Me.UpperBoundOpt IsNot Nothing Then
                    Return Me.UpperBoundOpt
                End If

                If Me.UpperBoundConditionOpt.Kind = BoundKind.BinaryOperator Then
                    Dim upperBound As BoundBinaryOperator = DirectCast(Me.UpperBoundConditionOpt, BoundBinaryOperator)
                    If upperBound.OperatorKind = BinaryOperatorKind.LessThanOrEqual Then
                        Return upperBound.Right
                    End If
                End If

                Return Nothing
            End Get
        End Property

        Private ReadOnly Property IMinimumValue As IExpression Implements IRangeCaseClause.MinimumValue
            Get
                If Me.LowerBoundOpt IsNot Nothing Then
                    Return Me.LowerBoundOpt
                End If

                If Me.LowerBoundConditionOpt.Kind = BoundKind.BinaryOperator Then
                    Dim lowerBound As BoundBinaryOperator = DirectCast(Me.LowerBoundConditionOpt, BoundBinaryOperator)
                    If lowerBound.OperatorKind = BinaryOperatorKind.GreaterThanOrEqual Then
                        Return lowerBound.Right
                    End If
                End If

                Return Nothing
            End Get
        End Property

        Protected Overrides ReadOnly Property ICaseClass As CaseKind
            Get
                Return CaseKind.Range
            End Get
        End Property
    End Class

    Partial Class BoundRelationalCaseClause
        Implements IRelationalCaseClause

        Private ReadOnly Property Relation As BinaryOperationKind Implements IRelationalCaseClause.Relation
            Get
                If Me.Value IsNot Nothing Then
                    Return DeriveBinaryOperationKind(Me.OperatorKind, DirectCast(Me.Value, BoundExpression))
                End If

                Return BinaryOperationKind.None
            End Get
        End Property

        Private ReadOnly Property Value As IExpression Implements IRelationalCaseClause.Value
            Get
                If Me.OperandOpt IsNot Nothing Then
                    Return Me.OperandOpt
                End If

                If Me.ConditionOpt IsNot Nothing AndAlso Me.ConditionOpt.Kind = BoundKind.BinaryOperator Then
                    Return DirectCast(Me.ConditionOpt, BoundBinaryOperator).Right
                End If

                Return Nothing
            End Get
        End Property

        Protected Overrides ReadOnly Property ICaseClass As CaseKind
            Get
                Return CaseKind.Relational
            End Get
        End Property
    End Class

    Partial Class BoundCaseStatement

        ' Cases are found by going through ISwitch, so the VB Case statement is orphaned.
        Protected Overrides Function StatementKind() As OperationKind
            Return OperationKind.None
        End Function
    End Class

    Partial Class BoundDoLoopStatement
        Implements IWhileUntilLoopStatement

        Private ReadOnly Property ICondition As IExpression Implements IForWhileUntilLoopStatement.Condition
            Get
                Return Me.ConditionOpt
            End Get
        End Property

        Private ReadOnly Property IBody As IStatement Implements ILoopStatement.Body
            Get
                Return Me.Body
            End Get
        End Property

        Private ReadOnly Property ILoopClass As LoopKind Implements ILoopStatement.LoopKind
            Get
                Return LoopKind.WhileUntil
            End Get
        End Property

        Private ReadOnly Property IIsTopTest As Boolean Implements IWhileUntilLoopStatement.IsTopTest
            Get
                Return Me.ConditionIsTop
            End Get
        End Property

        Private ReadOnly Property IIsWhile As Boolean Implements IWhileUntilLoopStatement.IsWhile
            Get
                Return Not Me.ConditionIsUntil
            End Get
        End Property

        Protected Overrides Function StatementKind() As OperationKind
            Return OperationKind.LoopStatement
        End Function

    End Class

    Partial Class BoundForToStatement
        Implements IForLoopStatement

        Private Shared LoopBottomMappings As New System.Runtime.CompilerServices.ConditionalWeakTable(Of BoundForToStatement, Object)

        Private ReadOnly Property IAtLoopBottom As ImmutableArray(Of IStatement) Implements IForLoopStatement.AtLoopBottom
            Get
                Dim result = LoopBottomMappings.GetValue(
                    Me,
                    Function(BoundFor)
                        Dim statements As ArrayBuilder(Of IStatement) = ArrayBuilder(Of IStatement).GetInstance()
                        Dim operators As BoundForToUserDefinedOperators = BoundFor.OperatorsOpt
                        If operators IsNot Nothing Then
                            ' Use the operator methods. Figure out the precise rules first.
                        Else
                            Dim controlReference As IReferenceExpression = TryCast(BoundFor.ControlVariable, IReferenceExpression)
                            If controlReference IsNot Nothing Then

                                ' ControlVariable += StepValue

                                Dim controlType As TypeSymbol = BoundFor.ControlVariable.Type

                                Dim stepValue As BoundExpression = BoundFor.StepValue
                                If stepValue Is Nothing Then
                                    stepValue = New BoundLiteral(Nothing, Semantics.Expression.SynthesizeNumeric(controlType, 1), controlType)
                                End If

                                Dim stepOperand As IExpression = If(stepValue.IsConstant, DirectCast(stepValue, IExpression), New Temporary(SyntheticLocalKind.ForLoopStepValue, BoundFor, stepValue))
                                statements.Add(New CompoundAssignment(controlReference, stepOperand, Semantics.Expression.DeriveAdditionKind(controlType), Nothing, stepValue.Syntax))
                            End If
                        End If

                        Return statements.ToImmutableAndFree()
                    End Function)

                Return DirectCast(result, ImmutableArray(Of IStatement))
            End Get
        End Property

        Private Shared LoopTopMappings As New System.Runtime.CompilerServices.ConditionalWeakTable(Of BoundForToStatement, Object)

        Private ReadOnly Property IBefore As ImmutableArray(Of IStatement) Implements IForLoopStatement.Before
            Get
                Dim result = LoopTopMappings.GetValue(
                    Me,
                    Function(BoundFor)
                        Dim statements As ArrayBuilder(Of IStatement) = ArrayBuilder(Of IStatement).GetInstance()

                        ' ControlVariable = InitialValue
                        Dim controlReference As IReferenceExpression = TryCast(BoundFor.ControlVariable, IReferenceExpression)
                        If controlReference IsNot Nothing Then
                            statements.Add(New Assignment(controlReference, BoundFor.InitialValue, BoundFor.InitialValue.Syntax))
                        End If

                        ' T0 = LimitValue
                        If Not Me.LimitValue.IsConstant Then
                            statements.Add(New Assignment(New Temporary(SyntheticLocalKind.ForLoopLimitValue, BoundFor, BoundFor.LimitValue), BoundFor.LimitValue, BoundFor.LimitValue.Syntax))
                        End If

                        ' T1 = StepValue
                        If BoundFor.StepValue IsNot Nothing AndAlso Not BoundFor.StepValue.IsConstant Then
                            statements.Add(New Assignment(New Temporary(SyntheticLocalKind.ForLoopStepValue, BoundFor, BoundFor.StepValue), BoundFor.StepValue, BoundFor.StepValue.Syntax))
                        End If

                        Return statements.ToImmutableAndFree()
                    End Function)

                Return DirectCast(result, ImmutableArray(Of IStatement))
            End Get
        End Property

        Private ReadOnly Property ILocals As ImmutableArray(Of ILocalSymbol) Implements IForLoopStatement.Locals
            Get
                Return ImmutableArray(Of ILocalSymbol).Empty
            End Get
        End Property

        Private Shared LoopConditionMappings As New System.Runtime.CompilerServices.ConditionalWeakTable(Of BoundForToStatement, IExpression)

        Private ReadOnly Property ICondition As IExpression Implements IForWhileUntilLoopStatement.Condition
            Get
                Return LoopConditionMappings.GetValue(
                    Me,
                    Function(BoundFor)
                        Dim limitValue As IExpression = If(BoundFor.LimitValue.IsConstant, DirectCast(BoundFor.LimitValue, IExpression), New Temporary(SyntheticLocalKind.ForLoopLimitValue, BoundFor, BoundFor.LimitValue))
                        Dim controlVariable As BoundExpression = BoundFor.ControlVariable

                        Dim booleanType As ITypeSymbol = controlVariable.ExpressionSymbol.DeclaringCompilation.GetSpecialType(SpecialType.System_Boolean)

                        Dim operators As BoundForToUserDefinedOperators = Me.OperatorsOpt
                        If operators IsNot Nothing Then
                            ' Use the operator methods. Figure out the precise rules first.
                            Return Nothing
                        Else
                            If BoundFor.StepValue Is Nothing OrElse (BoundFor.StepValue.IsConstant AndAlso BoundFor.StepValue.ConstantValueOpt IsNot Nothing) Then
                                ' Either ControlVariable <= LimitValue or ControlVariable >= LimitValue, depending on whether the step value is negative.

                                Dim relationalCode As BinaryOperationKind = DeriveBinaryOperationKind(If(BoundFor.StepValue IsNot Nothing AndAlso BoundFor.StepValue.ConstantValueOpt.IsNegativeNumeric, BinaryOperatorKind.GreaterThanOrEqual, BinaryOperatorKind.LessThanOrEqual), controlVariable)
                                Return New Binary(relationalCode, controlVariable, limitValue, booleanType, limitValue.Syntax)
                            Else
                                ' If(StepValue >= 0, ControlVariable <= LimitValue, ControlVariable >= LimitValue)

                                Dim stepValue As IExpression = New Temporary(SyntheticLocalKind.ForLoopStepValue, BoundFor, BoundFor.StepValue)
                                Dim stepRelationalCode As BinaryOperationKind = DeriveBinaryOperationKind(BinaryOperatorKind.GreaterThanOrEqual, BoundFor.StepValue)
                                Dim stepCondition As IExpression = New Binary(stepRelationalCode, stepValue, New BoundLiteral(Nothing, Semantics.Expression.SynthesizeNumeric(stepValue.ResultType, 0), BoundFor.StepValue.Type), booleanType, BoundFor.StepValue.Syntax)

                                Dim positiveStepRelationalCode As BinaryOperationKind = DeriveBinaryOperationKind(BinaryOperatorKind.LessThanOrEqual, controlVariable)
                                Dim positiveStepCondition As IExpression = New Binary(positiveStepRelationalCode, controlVariable, limitValue, booleanType, limitValue.Syntax)

                                Dim negativeStepRelationalCode As BinaryOperationKind = DeriveBinaryOperationKind(BinaryOperatorKind.GreaterThanOrEqual, controlVariable)
                                Dim negativeStepCondition As IExpression = New Binary(negativeStepRelationalCode, controlVariable, limitValue, booleanType, limitValue.Syntax)

                                Return New ConditionalChoice(stepCondition, positiveStepCondition, negativeStepCondition, booleanType, limitValue.Syntax)
                            End If
                        End If
                    End Function)
            End Get
        End Property

        Private ReadOnly Property IBody As IStatement Implements ILoopStatement.Body
            Get
                Return Me.Body
            End Get
        End Property

        Private ReadOnly Property ILoopClass As LoopKind Implements ILoopStatement.LoopKind
            Get
                Return LoopKind.For
            End Get
        End Property

        Protected Overrides Function StatementKind() As OperationKind
            Return OperationKind.LoopStatement
        End Function

        Private Class Temporary
            Implements ISyntheticLocalReferenceExpression

            Private _temporaryKind As SyntheticLocalKind
            Private _containingStatement As IStatement
            Private _capturedValue As IExpression

            Public Sub New(temporaryKind As SyntheticLocalKind, containingStatement As IStatement, capturedValue As IExpression)
                Me._temporaryKind = temporaryKind
                Me._containingStatement = containingStatement
                Me._capturedValue = capturedValue
            End Sub

            Public ReadOnly Property ConstantValue As Object Implements IExpression.ConstantValue
                Get
                    Return Nothing
                End Get
            End Property

            Public ReadOnly Property Kind As OperationKind Implements IOperation.Kind
                Get
                    Return OperationKind.SyntheticLocalReferenceExpression
                End Get
            End Property

            Public ReadOnly Property IsInvalid As Boolean Implements IExpression.IsInvalid
                Get
                    Return False
                End Get
            End Property

            Public ReadOnly Property ResultType As ITypeSymbol Implements IExpression.ResultType
                Get
                    Return Me._capturedValue.ResultType
                End Get
            End Property

            Public ReadOnly Property Syntax As SyntaxNode Implements IExpression.Syntax
                Get
                    Return Me._capturedValue.Syntax
                End Get
            End Property

            Public ReadOnly Property ContainingStatement As IStatement Implements ISyntheticLocalReferenceExpression.ContainingStatement
                Get
                    Return Me._containingStatement
                End Get
            End Property

            Public ReadOnly Property SyntheticLocalKind As SyntheticLocalKind Implements ISyntheticLocalReferenceExpression.SyntheticLocalKind
                Get
                    Return Me._temporaryKind
                End Get
            End Property
        End Class
    End Class

    Partial Class BoundForEachStatement
        Implements IForEachLoopStatement

        Private ReadOnly Property IterationVariable As ILocalSymbol Implements IForEachLoopStatement.IterationVariable
            Get
                Dim controlReference As ILocalReferenceExpression = TryCast(Me.ControlVariable, ILocalReferenceExpression)
                If controlReference IsNot Nothing Then
                    Return controlReference.Local
                End If

                Return Nothing
            End Get
        End Property

        Private ReadOnly Property LoopClass As LoopKind Implements ILoopStatement.LoopKind
            Get
                Return LoopKind.ForEach
            End Get
        End Property

        Private ReadOnly Property IForEach_Collection As IExpression Implements IForEachLoopStatement.Collection
            Get
                Return Me.Collection
            End Get
        End Property

        Private ReadOnly Property ILoop_Body As IStatement Implements ILoopStatement.Body
            Get
                Return Me.Body
            End Get
        End Property

        Protected Overrides Function StatementKind() As OperationKind
            Return OperationKind.LoopStatement
        End Function
    End Class

    Partial Class BoundTryStatement
        Implements ITryStatement

        Private ReadOnly Property IBody As IBlockStatement Implements ITryStatement.Body
            Get
                Return Me.TryBlock
            End Get
        End Property

        Private ReadOnly Property ICatches As ImmutableArray(Of ICatch) Implements ITryStatement.Catches
            Get
                Return Me.CatchBlocks.As(Of ICatch)()
            End Get
        End Property

        Private ReadOnly Property IFinallyHandler As IBlockStatement Implements ITryStatement.FinallyHandler
            Get
                Return Me.FinallyBlockOpt
            End Get
        End Property

        Protected Overrides Function StatementKind() As OperationKind
            Return OperationKind.TryStatement
        End Function
    End Class

    Partial Class BoundCatchBlock
        Implements ICatch

        Private ReadOnly Property ICaughtType As ITypeSymbol Implements ICatch.CaughtType
            Get
                If Me.ExceptionSourceOpt IsNot Nothing Then
                    Return Me.ExceptionSourceOpt.Type
                End If

                ' Ideally return System.Exception here is best, but without being able to get to a Compilation object, that's difficult.
                Return Nothing
            End Get
        End Property

        Private ReadOnly Property IFilter As IExpression Implements ICatch.Filter
            Get
                Return Me.ExceptionFilterOpt
            End Get
        End Property

        Private ReadOnly Property IHandler As IBlockStatement Implements ICatch.Handler
            Get
                Return Me.Body
            End Get
        End Property

        Private ReadOnly Property ILocals As ILocalSymbol Implements ICatch.ExceptionLocal
            Get
                Return Me.LocalOpt
            End Get
        End Property

        Private ReadOnly Property IKind As OperationKind Implements IOperation.Kind
            Get
                Return OperationKind.CatchHandler
            End Get
        End Property

        Private ReadOnly Property IIsInvalid As Boolean Implements IOperation.IsInvalid
            Get
                Return Me.HasErrors
            End Get
        End Property

        Private ReadOnly Property ISyntax As SyntaxNode Implements IOperation.Syntax
            Get
                Return Me.Syntax
            End Get
        End Property
    End Class

    Partial Class BoundBlock
        Implements IBlockStatement

        Private ReadOnly Property ILocals As ImmutableArray(Of ILocalSymbol) Implements IBlockStatement.Locals
            Get
                Return Me.Locals.As(Of ILocalSymbol)()
            End Get
        End Property

        Private ReadOnly Property IStatements As ImmutableArray(Of IStatement) Implements IBlockStatement.Statements
            Get
                Return Me.Statements.As(Of IStatement)()
            End Get
        End Property

        Protected Overrides Function StatementKind() As OperationKind
            Return OperationKind.BlockStatement
        End Function
    End Class

    Partial Class BoundBadStatement
        Protected Overrides Function StatementKind() As OperationKind
            Return OperationKind.InvalidStatement
        End Function
    End Class

    Partial Class BoundReturnStatement
        Implements IReturnStatement

        Private ReadOnly Property IReturned As IExpression Implements IReturnStatement.Returned
            Get
                Return Me.ExpressionOpt
            End Get
        End Property

        Protected Overrides Function StatementKind() As OperationKind
            Return OperationKind.ReturnStatement
        End Function
    End Class

    Partial Class BoundThrowStatement
        Implements IThrowStatement

        Private ReadOnly Property IThrown As IExpression Implements IThrowStatement.Thrown
            Get
                Return Me.ExpressionOpt
            End Get
        End Property

        Protected Overrides Function StatementKind() As OperationKind
            Return OperationKind.ThrowStatement
        End Function
    End Class

    Partial Class BoundWhileStatement
        Implements IWhileUntilLoopStatement

        Private ReadOnly Property ICondition As IExpression Implements IForWhileUntilLoopStatement.Condition
            Get
                Return Me.Condition
            End Get
        End Property

        Private ReadOnly Property IBody As IStatement Implements ILoopStatement.Body
            Get
                Return Me.Body
            End Get
        End Property

        Private ReadOnly Property ILoopClass As LoopKind Implements ILoopStatement.LoopKind
            Get
                Return LoopKind.WhileUntil
            End Get
        End Property

        Private ReadOnly Property IIsTopTest As Boolean Implements IWhileUntilLoopStatement.IsTopTest
            Get
                Return True
            End Get
        End Property

        Private ReadOnly Property IIsWhile As Boolean Implements IWhileUntilLoopStatement.IsWhile
            Get
                Return True
            End Get
        End Property

        Protected Overrides Function StatementKind() As OperationKind
            Return OperationKind.LoopStatement
        End Function
    End Class

    Partial Class BoundLocalDeclarationBase
        Implements IVariable

        Protected MustOverride ReadOnly Property IInitialValue As IExpression Implements IVariable.InitialValue

        Protected MustOverride ReadOnly Property IVariable As ILocalSymbol Implements IVariable.Variable
    End Class

    Partial Class BoundLocalDeclaration
        Implements IVariableDeclarationStatement

        Private ReadOnly Property IVariables As ImmutableArray(Of IVariable) Implements IVariableDeclarationStatement.Variables
            Get
                Return ImmutableArray.Create(Of IVariable)(Me)
            End Get
        End Property

        Protected Overrides ReadOnly Property IInitialValue As IExpression
            Get
                Return Me.InitializerOpt
            End Get
        End Property

        Protected Overrides ReadOnly Property IVariable As ILocalSymbol
            Get
                Return Me.LocalSymbol
            End Get
        End Property

        Protected Overrides Function StatementKind() As OperationKind
            Return OperationKind.VariableDeclarationStatement
        End Function
    End Class

    Partial Class BoundAsNewLocalDeclarations
        Implements IVariableDeclarationStatement

        Private ReadOnly Property IVariables As ImmutableArray(Of IVariable) Implements IVariableDeclarationStatement.Variables
            Get
                Return Me.LocalDeclarations.As(Of IVariable)()
            End Get
        End Property

        Protected Overrides ReadOnly Property IInitialValue As IExpression
            Get
                Return Me.Initializer
            End Get
        End Property

        Protected Overrides ReadOnly Property IVariable As ILocalSymbol
            Get
                ' ZZZ Get clear about what's happening in the VB bound trees. BoundAsNewLocalDeclarations has multiple symbols and
                ' inherits from BoundLocalDeclarationBase, which occurs multiply in BoundDimStatement.
                Dim local As BoundLocalDeclaration = Me.LocalDeclarations.FirstOrDefault()
                Return If(local IsNot Nothing, local.LocalSymbol, Nothing)
            End Get
        End Property

        Protected Overrides Function StatementKind() As OperationKind
            Return OperationKind.VariableDeclarationStatement
        End Function
    End Class

    Partial Class BoundDimStatement
        Implements IVariableDeclarationStatement

        Private ReadOnly Property IVariables As ImmutableArray(Of IVariable) Implements IVariableDeclarationStatement.Variables
            Get
                Return Me.LocalDeclarations.As(Of IVariable)()
            End Get
        End Property

        Protected Overrides Function StatementKind() As OperationKind
            Return OperationKind.VariableDeclarationStatement
        End Function
    End Class

    Partial Class BoundYieldStatement
        Implements IReturnStatement
        Private ReadOnly Property IReturned As IExpression Implements IReturnStatement.Returned
            Get
                Return Me.Expression
            End Get
        End Property

        Protected Overrides Function StatementKind() As OperationKind
            Return OperationKind.YieldReturnStatement
        End Function
    End Class

    Partial Class BoundLabelStatement
        Implements ILabelStatement

        Private ReadOnly Property ILabel As ILabelSymbol Implements ILabelStatement.Label
            Get
                Return Me.Label
            End Get
        End Property

        Protected Overrides Function StatementKind() As OperationKind
            Return OperationKind.LabelStatement
        End Function
    End Class

    Partial Class BoundGotoStatement
        Implements IBranchStatement

        Private ReadOnly Property ITarget As ILabelSymbol Implements IBranchStatement.Target
            Get
                Return Me.Label
            End Get
        End Property

        Protected Overrides Function StatementKind() As OperationKind
            Return OperationKind.GoToStatement
        End Function
    End Class

    Partial Class BoundContinueStatement
        Implements IBranchStatement

        Private ReadOnly Property ITarget As ILabelSymbol Implements IBranchStatement.Target
            Get
                Return Me.Label
            End Get
        End Property

        Protected Overrides Function StatementKind() As OperationKind
            Return OperationKind.ContinueStatement
        End Function
    End Class

    Partial Class BoundExitStatement
        Implements IBranchStatement

        Private ReadOnly Property ITarget As ILabelSymbol Implements IBranchStatement.Target
            Get
                Return Me.Label
            End Get
        End Property

        Protected Overrides Function StatementKind() As OperationKind
            Return OperationKind.BreakStatement
        End Function
    End Class

    Partial Class BoundSyncLockStatement
        Implements ILockStatement

        Private ReadOnly Property ILocked As IExpression Implements ILockStatement.Locked
            Get
                Return Me.LockExpression
            End Get
        End Property

        Private ReadOnly Property IBody As IStatement Implements ILockStatement.Body
            Get
                Return Me.Body
            End Get
        End Property

        Protected Overrides Function StatementKind() As OperationKind
            Return OperationKind.LockStatement
        End Function
    End Class

    Partial Class BoundNoOpStatement
        Protected Overrides Function StatementKind() As OperationKind
            Return OperationKind.EmptyStatement
        End Function
    End Class

    Partial Class BoundSequencePoint
        Protected Overrides Function StatementKind() As OperationKind
            Return OperationKind.None
        End Function
    End Class

    Partial Class BoundSequencePointWithSpan
        Protected Overrides Function StatementKind() As OperationKind
            Return OperationKind.None
        End Function
    End Class

    Partial Class BoundStateMachineScope
        Protected Overrides Function StatementKind() As OperationKind
            Return OperationKind.None
        End Function
    End Class

    Partial Class BoundStopStatement
        Protected Overrides Function StatementKind() As OperationKind
            Return OperationKind.StopStatement
        End Function
    End Class

    Partial Class BoundEndStatement
        Protected Overrides Function StatementKind() As OperationKind
            Return OperationKind.EndStatement
        End Function
    End Class

    Partial Class BoundWithStatement
        Implements IWithStatement

        Private ReadOnly Property IBody As IStatement Implements IWithStatement.Body
            Get
                Return Me.Body
            End Get
        End Property

        Private ReadOnly Property IValue As IExpression Implements IWithStatement.Value
            Get
                Return Me.OriginalExpression
            End Get
        End Property

        Protected Overrides Function StatementKind() As OperationKind
            Return OperationKind.WithStatement
        End Function
    End Class

    Partial Class BoundUsingStatement
        Implements IUsingWithExpressionStatement, IUsingWithDeclarationStatement

        Private ReadOnly Property IValue As IExpression Implements IUsingWithExpressionStatement.Value
            Get
                Return Me.ResourceExpressionOpt
            End Get
        End Property

        Private Shared VariablesMappings As New System.Runtime.CompilerServices.ConditionalWeakTable(Of BoundUsingStatement, Variables)

        Private ReadOnly Property IVariables As IVariableDeclarationStatement Implements IUsingWithDeclarationStatement.Variables
            Get
                Return VariablesMappings.GetValue(
                    Me,
                    Function(BoundUsing)
                        Return New Variables(BoundUsing.ResourceList.As(Of IVariable))
                    End Function)
            End Get
        End Property

        Private ReadOnly Property IBody As IStatement Implements IUsingStatement.Body
            Get
                Return Me.Body
            End Get
        End Property

        Protected Overrides Function StatementKind() As OperationKind
            Return If(Me._ResourceExpressionOpt Is Nothing, OperationKind.UsingWithDeclarationStatement, OperationKind.UsingWithExpressionStatement)
        End Function

        Private Class Variables
            Implements IVariableDeclarationStatement

            Private ReadOnly _variables As ImmutableArray(Of IVariable)

            Public Sub New(variables As ImmutableArray(Of IVariable))
                _variables = variables
            End Sub

            Public ReadOnly Property Kind As OperationKind Implements IOperation.Kind
                Get
                    Return OperationKind.VariableDeclarationStatement
                End Get
            End Property

            Public ReadOnly Property IsInvalid As Boolean Implements IOperation.IsInvalid
                Get
                    Return False
                End Get
            End Property

            Public ReadOnly Property Syntax As SyntaxNode Implements IOperation.Syntax
                Get
                    Return Nothing
                End Get
            End Property

            Private ReadOnly Property IVariableDeclaration_Variables As ImmutableArray(Of IVariable) Implements IVariableDeclarationStatement.Variables
                Get
                    Return _variables
                End Get
            End Property
        End Class
    End Class

    Partial Class BoundExpressionStatement
        Implements IExpressionStatement

        Private ReadOnly Property IExpression As IExpression Implements IExpressionStatement.Expression
            Get
                Return Me.Expression
            End Get
        End Property

        Protected Overrides Function StatementKind() As OperationKind
            Return OperationKind.ExpressionStatement
        End Function
    End Class
End Namespace
