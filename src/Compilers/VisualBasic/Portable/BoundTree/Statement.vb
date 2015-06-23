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
        Implements IIf
        Implements IIfClause

        Private ReadOnly Property IElse As IStatement Implements IIf.Else
            Get
                Return Me.AlternativeOpt
            End Get
        End Property

        Private ReadOnly Property IIfClauses As ImmutableArray(Of IIfClause) Implements IIf.IfClauses
            Get
                ' Apparently the VB bound trees do not preserve multi-clause if statements. This is disappointing.
                Return ImmutableArray.Create(Of IIfClause)(Me)
            End Get
        End Property

        Private ReadOnly Property IBody As IStatement Implements IIfClause.Body
            Get
                Return Me.Consequence
            End Get
        End Property

        Private ReadOnly Property ICondition As IExpression Implements IIfClause.Condition
            Get
                Return Me.Condition
            End Get
        End Property

        Protected Overrides Function StatementKind() As OperationKind
            Return OperationKind.IfStatement
        End Function

    End Class

    Partial Class BoundSelectStatement
        Implements ISwitch

        Private ReadOnly Property ICases As ImmutableArray(Of ICase) Implements ISwitch.Cases
            Get
                Return Me.CaseBlocks.As(Of ICase)()
            End Get
        End Property

        Private ReadOnly Property IValue As IExpression Implements ISwitch.Value
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
                Return Me.CaseStatement.CaseClauses.As(Of ICaseClause)()
            End Get
        End Property

        Protected Overrides Function StatementKind() As OperationKind
            Return OperationKind.None
        End Function
    End Class

    Partial Class BoundCaseClause
        Implements ICaseClause

        Protected MustOverride ReadOnly Property ICaseClass As CaseKind Implements ICaseClause.CaseClass
    End Class

    Partial Class BoundSimpleCaseClause
        Implements ISingleValueCaseClause

        Private ReadOnly Property IEquality As RelationalOperatorCode Implements ISingleValueCaseClause.Equality
            Get
                Dim caseValue As BoundExpression = Me.ValueOpt
                If caseValue IsNot Nothing Then
                    Select Case caseValue.Type.SpecialType
                        Case SpecialType.System_Int32, SpecialType.System_Int64, SpecialType.System_UInt32, SpecialType.System_UInt64, SpecialType.System_UInt16, SpecialType.System_Int16, SpecialType.System_SByte, SpecialType.System_Byte, SpecialType.System_Char
                            Return RelationalOperatorCode.IntegerEqual

                        Case SpecialType.System_Boolean
                            Return RelationalOperatorCode.BooleanEqual

                        Case SpecialType.System_String
                            Return RelationalOperatorCode.StringEqual
                    End Select

                    If caseValue.Type.TypeKind = TypeKind.Enum Then
                        Return RelationalOperatorCode.EnumEqual
                    End If
                End If

                Return RelationalOperatorCode.None
            End Get
        End Property

        Private ReadOnly Property IValue As IExpression Implements ISingleValueCaseClause.Value
            Get
                Return Me.ValueOpt
            End Get
        End Property

        Protected Overrides ReadOnly Property ICaseClass As CaseKind
            Get
                Return If(Me.ValueOpt IsNot Nothing, CaseKind.SingleValue, CaseKind.Default)
            End Get
        End Property
    End Class

    Partial Class BoundRangeCaseClause
        Implements IRangeCaseClause

        Private ReadOnly Property IMaximumValue As IExpression Implements IRangeCaseClause.MaximumValue
            Get
                Return Me.UpperBoundOpt
            End Get
        End Property

        Private ReadOnly Property IMinimumValue As IExpression Implements IRangeCaseClause.MinimumValue
            Get
                Return Me.LowerBoundOpt
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

        Private ReadOnly Property Relation As RelationalOperatorCode Implements IRelationalCaseClause.Relation
            Get
                If Me.OperandOpt IsNot Nothing Then
                    Return DeriveRelationalOperatorCode(Me.OperatorKind, Me.OperandOpt)
                End If

                Return RelationalOperatorCode.None
            End Get
        End Property

        Private ReadOnly Property Value As IExpression Implements IRelationalCaseClause.Value
            Get
                Return Me.OperandOpt
            End Get
        End Property

        Protected Overrides ReadOnly Property ICaseClass As CaseKind
            Get
                Return CaseKind.Relational
            End Get
        End Property
    End Class

    Partial Class BoundDoLoopStatement
        Implements IWhileUntil

        Private ReadOnly Property ICondition As IExpression Implements IForWhileUntil.Condition
            Get
                Return Me.ConditionOpt
            End Get
        End Property

        Private ReadOnly Property IBody As IStatement Implements ILoop.Body
            Get
                Return Me.Body
            End Get
        End Property

        Private ReadOnly Property ILoopClass As LoopKind Implements ILoop.LoopClass
            Get
                Return LoopKind.WhileUntil
            End Get
        End Property

        Private ReadOnly Property IIsTopTest As Boolean Implements IWhileUntil.IsTopTest
            Get
                Return Me.ConditionIsTop
            End Get
        End Property

        Private ReadOnly Property IIsWhile As Boolean Implements IWhileUntil.IsWhile
            Get
                Return Not Me.ConditionIsUntil
            End Get
        End Property

        Protected Overrides Function StatementKind() As OperationKind
            Return OperationKind.LoopStatement
        End Function

    End Class

    Partial Class BoundForToStatement
        Implements IFor

        Private Shared LoopBottomMappings As New System.Runtime.CompilerServices.ConditionalWeakTable(Of BoundForToStatement, Object)

        Private ReadOnly Property IAtLoopBottom As ImmutableArray(Of IStatement) Implements IFor.AtLoopBottom
            Get
                Dim result = LoopBottomMappings.GetValue(
                    Me,
                    Function(BoundFor)
                        Dim statements As ArrayBuilder(Of IStatement) = ArrayBuilder(Of IStatement).GetInstance()
                        Dim operators As BoundForToUserDefinedOperators = BoundFor.OperatorsOpt
                        If operators IsNot Nothing Then
                            ' Use the operator methods. Figure out the precise rules first.
                        Else
                            Dim controlReference As IReference = TryCast(BoundFor.ControlVariable, IReference)
                            If controlReference IsNot Nothing Then

                                ' ControlVariable += StepValue

                                Dim controlType As TypeSymbol = BoundFor.ControlVariable.Type

                                Dim stepValue As BoundExpression = BoundFor.StepValue
                                If stepValue Is Nothing Then
                                    stepValue = New BoundLiteral(Nothing, Semantics.Expression.SynthesizeNumeric(controlType, 1), controlType)
                                End If

                                Dim stepOperand As IExpression = If(stepValue.IsConstant, DirectCast(stepValue, IExpression), New Temporary(TemporaryKind.StepValue, BoundFor, stepValue))
                                statements.Add(New CompoundAssignment(controlReference, stepOperand, Semantics.Expression.DeriveAdditionCode(controlType), Nothing, stepValue.Syntax))
                            End If
                        End If

                        Return statements.ToImmutableAndFree()
                    End Function)

                Return DirectCast(result, ImmutableArray(Of IStatement))
            End Get
        End Property

        Private Shared LoopTopMappings As New System.Runtime.CompilerServices.ConditionalWeakTable(Of BoundForToStatement, Object)

        Private ReadOnly Property IBefore As ImmutableArray(Of IStatement) Implements IFor.Before
            Get
                Dim result = LoopTopMappings.GetValue(
                    Me,
                    Function(BoundFor)
                        Dim statements As ArrayBuilder(Of IStatement) = ArrayBuilder(Of IStatement).GetInstance()

                        ' ControlVariable = InitialValue
                        Dim controlReference As IReference = TryCast(BoundFor.ControlVariable, IReference)
                        If controlReference IsNot Nothing Then
                            statements.Add(New Assignment(controlReference, BoundFor.InitialValue, BoundFor.InitialValue.Syntax))
                        End If

                        ' T0 = LimitValue
                        If Not Me.LimitValue.IsConstant Then
                            statements.Add(New Assignment(New Temporary(TemporaryKind.LimitValue, BoundFor, BoundFor.LimitValue), BoundFor.LimitValue, BoundFor.LimitValue.Syntax))
                        End If

                        ' T1 = StepValue
                        If BoundFor.StepValue IsNot Nothing AndAlso Not BoundFor.StepValue.IsConstant Then
                            statements.Add(New Assignment(New Temporary(TemporaryKind.StepValue, BoundFor, BoundFor.StepValue), BoundFor.StepValue, BoundFor.StepValue.Syntax))
                        End If

                        Return statements.ToImmutableAndFree()
                    End Function)

                Return DirectCast(result, ImmutableArray(Of IStatement))
            End Get
        End Property

        Private ReadOnly Property ILocals As ImmutableArray(Of ILocalSymbol) Implements IFor.Locals
            Get
                Return ImmutableArray(Of ILocalSymbol).Empty
            End Get
        End Property

        Private Shared LoopConditionMappings As New System.Runtime.CompilerServices.ConditionalWeakTable(Of BoundForToStatement, IExpression)

        Private ReadOnly Property ICondition As IExpression Implements IForWhileUntil.Condition
            Get
                Return LoopConditionMappings.GetValue(
                    Me,
                    Function(BoundFor)
                        Dim limitValue As IExpression = If(BoundFor.LimitValue.IsConstant, DirectCast(BoundFor.LimitValue, IExpression), New Temporary(TemporaryKind.LimitValue, BoundFor, BoundFor.LimitValue))
                        Dim controlVariable As BoundExpression = BoundFor.ControlVariable

                        Dim booleanType As ITypeSymbol = BoundFor.ControlVariable.Type.DeclaringCompilation.GetSpecialType(SpecialType.System_Boolean)

                        Dim operators As BoundForToUserDefinedOperators = Me.OperatorsOpt
                        If operators IsNot Nothing Then
                            ' Use the operator methods. Figure out the precise rules first.
                            Return Nothing
                        Else
                            If BoundFor.StepValue Is Nothing OrElse (BoundFor.StepValue.IsConstant AndAlso BoundFor.StepValue.ConstantValueOpt IsNot Nothing) Then
                                ' Either ControlVariable <= LimitValue or ControlVariable >= LimitValue, depending on whether the step value is negative.

                                Dim relationalCode As RelationalOperatorCode = DeriveRelationalOperatorCode(If(BoundFor.StepValue IsNot Nothing AndAlso BoundFor.StepValue.ConstantValueOpt.IsNegativeNumeric, BinaryOperatorKind.GreaterThanOrEqual, BinaryOperatorKind.LessThanOrEqual), controlVariable)
                                Return New Relational(relationalCode, controlVariable, limitValue, booleanType, Nothing, limitValue.Syntax)
                            Else
                                ' If(StepValue >= 0, ControlVariable <= LimitValue, ControlVariable >= LimitValue)

                                Dim stepValue As IExpression = New Temporary(TemporaryKind.StepValue, BoundFor, BoundFor.StepValue)
                                Dim stepRelationalCode As RelationalOperatorCode = DeriveRelationalOperatorCode(BinaryOperatorKind.GreaterThanOrEqual, BoundFor.StepValue)
                                Dim stepCondition As IExpression = New Relational(stepRelationalCode, stepValue, New BoundLiteral(Nothing, Semantics.Expression.SynthesizeNumeric(stepValue.ResultType, 0), BoundFor.StepValue.Type), booleanType, Nothing, BoundFor.StepValue.Syntax)

                                Dim positiveStepRelationalCode As RelationalOperatorCode = DeriveRelationalOperatorCode(BinaryOperatorKind.LessThanOrEqual, controlVariable)
                                Dim positiveStepCondition As IExpression = New Relational(positiveStepRelationalCode, controlVariable, limitValue, booleanType, Nothing, limitValue.Syntax)

                                Dim negativeStepRelationalCode As RelationalOperatorCode = DeriveRelationalOperatorCode(BinaryOperatorKind.GreaterThanOrEqual, controlVariable)
                                Dim negativeStepCondition As IExpression = New Relational(negativeStepRelationalCode, controlVariable, limitValue, booleanType, Nothing, limitValue.Syntax)

                                Return New ConditionalChoice(stepCondition, positiveStepCondition, negativeStepCondition, booleanType, limitValue.Syntax)
                            End If
                        End If
                    End Function)
            End Get
        End Property

        Private ReadOnly Property IBody As IStatement Implements ILoop.Body
            Get
                Return Me.Body
            End Get
        End Property

        Private ReadOnly Property ILoopClass As LoopKind Implements ILoop.LoopClass
            Get
                Return LoopKind.For
            End Get
        End Property

        Protected Overrides Function StatementKind() As OperationKind
            Return OperationKind.LoopStatement
        End Function

        Private Class Temporary
            Implements ITemporaryReference

            Private _temporaryKind As TemporaryKind
            Private _containingStatement As IStatement
            Private _capturedValue As IExpression

            Public Sub New(temporaryKind As TemporaryKind, containingStatement As IStatement, capturedValue As IExpression)
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
                    Return OperationKind.TemporaryReference
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

            Public ReadOnly Property ReferenceClass As ReferenceKind Implements IReference.ReferenceClass
                Get
                    Return ReferenceKind.Temporary
                End Get
            End Property

            Public ReadOnly Property ContainingStatement As IStatement Implements ITemporaryReference.ContainingStatement
                Get
                    Return Me._containingStatement
                End Get
            End Property

            Public ReadOnly Property TemporaryKind As TemporaryKind Implements ITemporaryReference.TemporaryKind
                Get
                    Return Me._temporaryKind
                End Get
            End Property
        End Class
    End Class

    Partial Class BoundForEachStatement
        Implements IForEach

        Private ReadOnly Property IterationVariable As ILocalSymbol Implements IForEach.IterationVariable
            Get
                Dim controlReference As ILocalReference = TryCast(Me.ControlVariable, ILocalReference)
                If controlReference IsNot Nothing Then
                    Return controlReference.Local
                End If

                Return Nothing
            End Get
        End Property

        Private ReadOnly Property LoopClass As LoopKind Implements ILoop.LoopClass
            Get
                Return LoopKind.ForEach
            End Get
        End Property

        Private ReadOnly Property IForEach_Collection As IExpression Implements IForEach.Collection
            Get
                Return Me.Collection
            End Get
        End Property

        Private ReadOnly Property ILoop_Body As IStatement Implements ILoop.Body
            Get
                Return Me.Body
            End Get
        End Property
    End Class

    Partial Class BoundTryStatement
        Implements ITry

        Private ReadOnly Property IBody As IBlock Implements ITry.Body
            Get
                Return Me.TryBlock
            End Get
        End Property

        Private ReadOnly Property ICatches As ImmutableArray(Of ICatch) Implements ITry.Catches
            Get
                Return Me.CatchBlocks.As(Of ICatch)()
            End Get
        End Property

        Private ReadOnly Property IFinallyHandler As IBlock Implements ITry.FinallyHandler
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

        Private ReadOnly Property IHandler As IBlock Implements ICatch.Handler
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

        Private ReadOnly Property ISyntax As SyntaxNode Implements IOperation.Syntax
            Get
                Return Me.Syntax
            End Get
        End Property
    End Class

    Partial Class BoundBlock
        Implements IBlock

        Private ReadOnly Property ILocals As ImmutableArray(Of ILocalSymbol) Implements IBlock.Locals
            Get
                Return Me.Locals.As(Of ILocalSymbol)()
            End Get
        End Property

        Private ReadOnly Property IStatements As ImmutableArray(Of IStatement) Implements IBlock.Statements
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
            Return OperationKind.None
        End Function
    End Class

    Partial Class BoundReturnStatement
        Implements IReturn

        Private ReadOnly Property IReturned As IExpression Implements IReturn.Returned
            Get
                Return Me.ExpressionOpt
            End Get
        End Property

        Protected Overrides Function StatementKind() As OperationKind
            Return OperationKind.ReturnStatement
        End Function
    End Class

    Partial Class BoundThrowStatement
        Implements IThrow

        Private ReadOnly Property IThrown As IExpression Implements IThrow.Thrown
            Get
                Return Me.ExpressionOpt
            End Get
        End Property

        Protected Overrides Function StatementKind() As OperationKind
            Return OperationKind.ThrowStatement
        End Function
    End Class

    Partial Class BoundWhileStatement
        Implements IWhileUntil

        Private ReadOnly Property ICondition As IExpression Implements IForWhileUntil.Condition
            Get
                Return Me.Condition
            End Get
        End Property

        Private ReadOnly Property IBody As IStatement Implements ILoop.Body
            Get
                Return Me.Body
            End Get
        End Property

        Private ReadOnly Property ILoopClass As LoopKind Implements ILoop.LoopClass
            Get
                Return LoopKind.WhileUntil
            End Get
        End Property

        Private ReadOnly Property IIsTopTest As Boolean Implements IWhileUntil.IsTopTest
            Get
                Return True
            End Get
        End Property

        Private ReadOnly Property IIsWhile As Boolean Implements IWhileUntil.IsWhile
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
        Implements IVariableDeclaration

        Private ReadOnly Property IVariables As ImmutableArray(Of IVariable) Implements IVariableDeclaration.Variables
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
        Implements IVariableDeclaration

        Private ReadOnly Property IVariables As ImmutableArray(Of IVariable) Implements IVariableDeclaration.Variables
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
        Implements IVariableDeclaration

        Private ReadOnly Property IVariables As ImmutableArray(Of IVariable) Implements IVariableDeclaration.Variables
            Get
                Return Me.LocalDeclarations.As(Of IVariable)()
            End Get
        End Property

        Protected Overrides Function StatementKind() As OperationKind
            Return OperationKind.VariableDeclarationStatement
        End Function
    End Class

    Partial Class BoundYieldStatement
        Implements IReturn
        Private ReadOnly Property IReturned As IExpression Implements IReturn.Returned
            Get
                Return Me.Expression
            End Get
        End Property

        Protected Overrides Function StatementKind() As OperationKind
            Return OperationKind.YieldReturnStatement
        End Function
    End Class

    Partial Class BoundLabelStatement
        Implements ILabel

        Private ReadOnly Property ILabel As ILabelSymbol Implements ILabel.Label
            Get
                Return Me.Label
            End Get
        End Property

        Protected Overrides Function StatementKind() As OperationKind
            Return OperationKind.LabelStatement
        End Function
    End Class

    Partial Class BoundGotoStatement
        Implements IBranch

        Private ReadOnly Property ITarget As ILabelSymbol Implements IBranch.Target
            Get
                Return Me.Label
            End Get
        End Property

        Protected Overrides Function StatementKind() As OperationKind
            Return OperationKind.GoToStatement
        End Function
    End Class

    Partial Class BoundContinueStatement
        Implements IBranch

        Private ReadOnly Property ITarget As ILabelSymbol Implements IBranch.Target
            Get
                Return Me.Label
            End Get
        End Property

        Protected Overrides Function StatementKind() As OperationKind
            Return OperationKind.ContinueStatement
        End Function
    End Class

    Partial Class BoundExitStatement
        Implements IBranch

        Private ReadOnly Property ITarget As ILabelSymbol Implements IBranch.Target
            Get
                Return Me.Label
            End Get
        End Property

        Protected Overrides Function StatementKind() As OperationKind
            Return OperationKind.BreakStatement
        End Function
    End Class

    Partial Class BoundSyncLockStatement
        Implements ILock

        Private ReadOnly Property ILocked As IExpression Implements ILock.Locked
            Get
                Return Me.LockExpression
            End Get
        End Property

        Protected Overrides Function StatementKind() As OperationKind
            Return OperationKind.LockStatement
        End Function
    End Class

End Namespace
