Imports System.Collections.Immutable
Imports Microsoft.CodeAnalysis.Semantics
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols

Namespace Microsoft.CodeAnalysis.VisualBasic

    Partial Class BoundStatement
        Implements Semantics.IStatement

        Private ReadOnly Property IKind As Semantics.OperationKind Implements Semantics.IOperation.Kind
            Get
                Return Me.StatementKind()
            End Get
        End Property

        Private ReadOnly Property ISyntax As SyntaxNode Implements Semantics.IOperation.Syntax
            Get
                Return Me.Syntax
            End Get
        End Property

        ' Protected MustOverride Function StatementKind() As Unified.StatementKind

        Protected Overridable Function StatementKind() As Semantics.OperationKind
            Return Semantics.OperationKind.None
        End Function
    End Class

    Partial Class BoundIfStatement
        Implements Semantics.IIf
        Implements Semantics.IIfClause

        Private ReadOnly Property IElse As Semantics.IStatement Implements Semantics.IIf.Else
            Get
                Return Me.AlternativeOpt
            End Get
        End Property

        Private ReadOnly Property IIfClauses As ImmutableArray(Of Semantics.IIfClause) Implements Semantics.IIf.IfClauses
            Get
                ' Apparently the VB bound trees do not preserve multi-clause if statements. This is disappointing.
                Return ImmutableArray.Create(Of Semantics.IIfClause)(Me)
            End Get
        End Property

        Private ReadOnly Property IBody As Semantics.IStatement Implements Semantics.IIfClause.Body
            Get
                Return Me.Consequence
            End Get
        End Property

        Private ReadOnly Property ICondition As Semantics.IExpression Implements Semantics.IIfClause.Condition
            Get
                Return Me.Condition
            End Get
        End Property

        Protected Overrides Function StatementKind() As Semantics.OperationKind
            Return Semantics.OperationKind.IfStatement
        End Function

    End Class

    Partial Class BoundSelectStatement
        Implements Semantics.ISwitch

        Private ReadOnly Property ICases As ImmutableArray(Of Semantics.ICase) Implements Semantics.ISwitch.Cases
            Get
                Return Me.CaseBlocks.As(Of Semantics.ICase)()
            End Get
        End Property

        Private ReadOnly Property IValue As Semantics.IExpression Implements Semantics.ISwitch.Value
            Get
                Return Me.ExpressionStatement.Expression
            End Get
        End Property

        Protected Overrides Function StatementKind() As Semantics.OperationKind
            Return Semantics.OperationKind.SwitchStatement
        End Function
    End Class

    Partial Class BoundCaseBlock
        Implements Semantics.ICase

        Private ReadOnly Property IBody As ImmutableArray(Of Semantics.IStatement) Implements Semantics.ICase.Body
            Get
                Return ImmutableArray.Create(Of Semantics.IStatement)(Me.Body)
            End Get
        End Property

        Private ReadOnly Property IClauses As ImmutableArray(Of Semantics.ICaseClause) Implements Semantics.ICase.Clauses
            Get
                Return Me.CaseStatement.CaseClauses.As(Of Semantics.ICaseClause)()
            End Get
        End Property

        Protected Overrides Function StatementKind() As Semantics.OperationKind
            Return Semantics.OperationKind.None
        End Function
    End Class

    Partial Class BoundCaseClause
        Implements Semantics.ICaseClause

        Protected MustOverride ReadOnly Property ICaseClass As Semantics.CaseKind Implements Semantics.ICaseClause.CaseClass
    End Class

    Partial Class BoundSimpleCaseClause
        Implements Semantics.ISingleValueCaseClause

        Private ReadOnly Property IEquality As Semantics.RelationalOperatorCode Implements Semantics.ISingleValueCaseClause.Equality
            Get
                Dim caseValue As BoundExpression = Me.ValueOpt
                If caseValue IsNot Nothing Then
                    Select Case caseValue.Type.SpecialType
                        Case SpecialType.System_Int32, SpecialType.System_Int64, SpecialType.System_UInt32, SpecialType.System_UInt64, SpecialType.System_UInt16, SpecialType.System_Int16, SpecialType.System_SByte, SpecialType.System_Byte, SpecialType.System_Char
                            Return Semantics.RelationalOperatorCode.IntegerEqual

                        Case SpecialType.System_Boolean
                            Return Semantics.RelationalOperatorCode.BooleanEqual

                        Case SpecialType.System_String
                            Return Semantics.RelationalOperatorCode.StringEqual
                    End Select

                    If caseValue.Type.TypeKind = TypeKind.Enum Then
                        Return Semantics.RelationalOperatorCode.EnumEqual
                    End If
                End If

                Return Semantics.RelationalOperatorCode.None
            End Get
        End Property

        Private ReadOnly Property IValue As Semantics.IExpression Implements Semantics.ISingleValueCaseClause.Value
            Get
                Return Me.ValueOpt
            End Get
        End Property

        Protected Overrides ReadOnly Property ICaseClass As Semantics.CaseKind
            Get
                Return If(Me.ValueOpt IsNot Nothing, Semantics.CaseKind.SingleValue, Semantics.CaseKind.Default)
            End Get
        End Property
    End Class

    Partial Class BoundRangeCaseClause
        Implements Semantics.IRangeCaseClause

        Private ReadOnly Property IMaximumValue As Semantics.IExpression Implements Semantics.IRangeCaseClause.MaximumValue
            Get
                Return Me.UpperBoundOpt
            End Get
        End Property

        Private ReadOnly Property IMinimumValue As Semantics.IExpression Implements Semantics.IRangeCaseClause.MinimumValue
            Get
                Return Me.LowerBoundOpt
            End Get
        End Property

        Protected Overrides ReadOnly Property ICaseClass As Semantics.CaseKind
            Get
                Return Semantics.CaseKind.Range
            End Get
        End Property
    End Class

    Partial Class BoundRelationalCaseClause
        Implements Semantics.IRelationalCaseClause

        Private ReadOnly Property Relation As Semantics.RelationalOperatorCode Implements Semantics.IRelationalCaseClause.Relation
            Get
                If Me.OperandOpt IsNot Nothing Then
                    Return DeriveRelationalOperatorCode(Me.OperatorKind, Me.OperandOpt)
                End If

                Return Semantics.RelationalOperatorCode.None
            End Get
        End Property

        Private ReadOnly Property Value As Semantics.IExpression Implements Semantics.IRelationalCaseClause.Value
            Get
                Return Me.OperandOpt
            End Get
        End Property

        Protected Overrides ReadOnly Property ICaseClass As Semantics.CaseKind
            Get
                Return Semantics.CaseKind.Relational
            End Get
        End Property
    End Class

    Partial Class BoundDoLoopStatement
        Implements Semantics.IWhileUntil

        Private ReadOnly Property ICondition As Semantics.IExpression Implements Semantics.IForWhileUntil.Condition
            Get
                Return Me.ConditionOpt
            End Get
        End Property

        Private ReadOnly Property IBody As Semantics.IStatement Implements Semantics.ILoop.Body
            Get
                Return Me.Body
            End Get
        End Property

        Private ReadOnly Property ILoopClass As Semantics.LoopKind Implements Semantics.ILoop.LoopClass
            Get
                Return Semantics.LoopKind.WhileUntil
            End Get
        End Property

        Private ReadOnly Property IIsTopTest As Boolean Implements Semantics.IWhileUntil.IsTopTest
            Get
                Return Me.ConditionIsTop
            End Get
        End Property

        Private ReadOnly Property IIsWhile As Boolean Implements Semantics.IWhileUntil.IsWhile
            Get
                Return Not Me.ConditionIsUntil
            End Get
        End Property

        Protected Overrides Function StatementKind() As Semantics.OperationKind
            Return Semantics.OperationKind.LoopStatement
        End Function

    End Class

    Partial Class BoundForToStatement
        Implements Semantics.IFor

        Private ReadOnly Property IAtLoopBottom As ImmutableArray(Of Semantics.IStatement) Implements Semantics.IFor.AtLoopBottom
            Get
                Dim statements As ArrayBuilder(Of Semantics.IStatement) = ArrayBuilder(Of Semantics.IStatement).GetInstance()
                Dim operators As BoundForToUserDefinedOperators = Me.OperatorsOpt
                If operators IsNot Nothing Then
                    ' Use the operator methods. Figure out the precise rules first.
                Else
                    Dim controlReference As Semantics.IReference = TryCast(Me.ControlVariable, Semantics.IReference)
                    If controlReference IsNot Nothing Then

                        ' ControlVariable += StepValue

                        Dim controlType As TypeSymbol = Me.ControlVariable.Type

                        Dim stepValue As BoundExpression = Me.StepValue
                        If stepValue Is Nothing Then
                            stepValue = New BoundLiteral(Nothing, Semantics.Expression.SynthesizeNumeric(controlType, 1), controlType)
                        End If

                        Dim stepOperand As Semantics.IExpression = If(stepValue.IsConstant, DirectCast(stepValue, Semantics.IExpression), New Temporary(Semantics.TemporaryKind.StepValue, Me, stepValue))
                        statements.Add(New Semantics.CompoundAssignment(controlReference, stepOperand, Semantics.Expression.DeriveAdditionCode(controlType), Nothing, stepValue.Syntax))
                    End If
                End If

                Return statements.ToImmutableAndFree()
            End Get
        End Property

        Private ReadOnly Property IBefore As ImmutableArray(Of Semantics.IStatement) Implements Semantics.IFor.Before
            Get
                Dim statements As ArrayBuilder(Of Semantics.IStatement) = ArrayBuilder(Of Semantics.IStatement).GetInstance()

                ' ControlVariable = InitialValue
                Dim controlReference As Semantics.IReference = TryCast(Me.ControlVariable, Semantics.IReference)
                If controlReference IsNot Nothing Then
                    statements.Add(New Semantics.Assignment(controlReference, Me.InitialValue, Me.InitialValue.Syntax))
                End If

                ' T0 = LimitValue
                If Not Me.LimitValue.IsConstant Then
                    statements.Add(New Semantics.Assignment(New Temporary(Semantics.TemporaryKind.LimitValue, Me, Me.LimitValue), Me.LimitValue, Me.LimitValue.Syntax))
                End If

                ' T1 = StepValue
                If Me.StepValue IsNot Nothing AndAlso Not Me.StepValue.IsConstant Then
                    statements.Add(New Semantics.Assignment(New Temporary(Semantics.TemporaryKind.StepValue, Me, Me.StepValue), Me.StepValue, Me.StepValue.Syntax))
                End If

                Return statements.ToImmutableAndFree()
            End Get
        End Property

        Private ReadOnly Property ILocals As ImmutableArray(Of ILocalSymbol) Implements Semantics.IFor.Locals
            Get
                Return ImmutableArray.Create(Of ILocalSymbol)()
            End Get
        End Property

        Private ReadOnly Property ICondition As Semantics.IExpression Implements Semantics.IForWhileUntil.Condition
            Get
                Dim limitValue As Semantics.IExpression = If(Me.LimitValue.IsConstant, DirectCast(Me.LimitValue, Semantics.IExpression), New Temporary(Semantics.TemporaryKind.LimitValue, Me, Me.LimitValue))
                Dim controlVariable As BoundExpression = Me.ControlVariable

                Dim booleanType As ITypeSymbol = Me.ControlVariable.Type.DeclaringCompilation.GetSpecialType(SpecialType.System_Boolean)

                Dim operators As BoundForToUserDefinedOperators = Me.OperatorsOpt
                If operators IsNot Nothing Then
                    ' Use the operator methods. Figure out the precise rules first.
                    Return Nothing
                Else
                    If Me.StepValue Is Nothing OrElse (Me.StepValue.IsConstant AndAlso Me.StepValue.ConstantValueOpt IsNot Nothing) Then
                        ' Either ControlVariable <= LimitValue or ControlVariable >= LimitValue, depending on whether the step value is negative.

                        Dim relationalCode As Semantics.RelationalOperatorCode = DeriveRelationalOperatorCode(If(Me.StepValue IsNot Nothing AndAlso Me.StepValue.ConstantValueOpt.IsNegativeNumeric, BinaryOperatorKind.GreaterThanOrEqual, BinaryOperatorKind.LessThanOrEqual), controlVariable)
                        Return New Semantics.Relational(relationalCode, controlVariable, limitValue, booleanType, Nothing, limitValue.Syntax)
                    Else
                        ' If(StepValue >= 0, ControlVariable <= LimitValue, ControlVariable >= LimitValue)

                        Dim stepValue As Semantics.IExpression = New Temporary(Semantics.TemporaryKind.StepValue, Me, Me.StepValue)
                        Dim stepRelationalCode As Semantics.RelationalOperatorCode = DeriveRelationalOperatorCode(BinaryOperatorKind.GreaterThanOrEqual, Me.StepValue)
                        Dim stepCondition As Semantics.IExpression = New Semantics.Relational(stepRelationalCode, stepValue, New BoundLiteral(Nothing, Semantics.Expression.SynthesizeNumeric(stepValue.ResultType, 0), Me.StepValue.Type), booleanType, Nothing, Me.StepValue.Syntax)

                        Dim positiveStepRelationalCode As Semantics.RelationalOperatorCode = DeriveRelationalOperatorCode(BinaryOperatorKind.LessThanOrEqual, controlVariable)
                        Dim positiveStepCondition As Semantics.IExpression = New Semantics.Relational(positiveStepRelationalCode, controlVariable, limitValue, booleanType, Nothing, limitValue.Syntax)

                        Dim negativeStepRelationalCode As Semantics.RelationalOperatorCode = DeriveRelationalOperatorCode(BinaryOperatorKind.GreaterThanOrEqual, controlVariable)
                        Dim negativeStepCondition As Semantics.IExpression = New Semantics.Relational(negativeStepRelationalCode, controlVariable, limitValue, booleanType, Nothing, limitValue.Syntax)

                        Return New Semantics.ConditionalChoice(stepCondition, positiveStepCondition, negativeStepCondition, booleanType, limitValue.Syntax)
                    End If
                End If
            End Get
        End Property

        Private ReadOnly Property IBody As Semantics.IStatement Implements Semantics.ILoop.Body
            Get
                Return Me.Body
            End Get
        End Property

        Private ReadOnly Property ILoopClass As Semantics.LoopKind Implements Semantics.ILoop.LoopClass
            Get
                Return Semantics.LoopKind.For
            End Get
        End Property

        Protected Overrides Function StatementKind() As Semantics.OperationKind
            Return Semantics.OperationKind.LoopStatement
        End Function

        Private Class Temporary
            Implements Semantics.ITemporaryReference

            Private _temporaryKind As Semantics.TemporaryKind
            Private _containingStatement As Semantics.IStatement
            Private _capturedValue As Semantics.IExpression

            Public Sub New(temporaryKind As Semantics.TemporaryKind, containingStatement As Semantics.IStatement, capturedValue As Semantics.IExpression)
                Me._temporaryKind = temporaryKind
                Me._containingStatement = containingStatement
                Me._capturedValue = capturedValue
            End Sub

            Public ReadOnly Property ConstantValue As Object Implements Semantics.IExpression.ConstantValue
                Get
                    Return Nothing
                End Get
            End Property

            Public ReadOnly Property Kind As Semantics.OperationKind Implements Semantics.IOperation.Kind
                Get
                    Return Semantics.OperationKind.TemporaryReference
                End Get
            End Property

            Public ReadOnly Property ResultType As ITypeSymbol Implements Semantics.IExpression.ResultType
                Get
                    Return Me._capturedValue.ResultType
                End Get
            End Property

            Public ReadOnly Property Syntax As SyntaxNode Implements Semantics.IExpression.Syntax
                Get
                    Return Me._capturedValue.Syntax
                End Get
            End Property

            Public ReadOnly Property ReferenceClass As Semantics.ReferenceKind Implements Semantics.IReference.ReferenceClass
                Get
                    Return Semantics.ReferenceKind.Temporary
                End Get
            End Property

            Public ReadOnly Property ContainingStatement As Semantics.IStatement Implements Semantics.ITemporaryReference.ContainingStatement
                Get
                    Return Me._containingStatement
                End Get
            End Property

            Public ReadOnly Property TemporaryKind As Semantics.TemporaryKind Implements Semantics.ITemporaryReference.TemporaryKind
                Get
                    Return Me._temporaryKind
                End Get
            End Property
        End Class
    End Class

    Partial Class BoundForEachStatement
        Implements Semantics.IForEach

        Private ReadOnly Property IterationVariable As ILocalSymbol Implements IForEach.IterationVariable
            Get
                Dim controlReference As Semantics.ILocalReference = TryCast(Me.ControlVariable, Semantics.ILocalReference)
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
        Implements Semantics.ITry

        Private ReadOnly Property IBody As Semantics.IBlock Implements Semantics.ITry.Body
            Get
                Return Me.TryBlock
            End Get
        End Property

        Private ReadOnly Property ICatches As ImmutableArray(Of Semantics.ICatch) Implements Semantics.ITry.Catches
            Get
                Return Me.CatchBlocks.As(Of Semantics.ICatch)()
            End Get
        End Property

        Private ReadOnly Property IFinallyHandler As Semantics.IBlock Implements Semantics.ITry.FinallyHandler
            Get
                Return Me.FinallyBlockOpt
            End Get
        End Property

        Protected Overrides Function StatementKind() As Semantics.OperationKind
            Return Semantics.OperationKind.TryStatement
        End Function
    End Class

    Partial Class BoundCatchBlock
        Implements Semantics.ICatch

        Private ReadOnly Property ICaughtType As ITypeSymbol Implements Semantics.ICatch.CaughtType
            Get
                If Me.ExceptionSourceOpt IsNot Nothing Then
                    Return Me.ExceptionSourceOpt.Type
                End If

                ' Ideally return System.Exception here is best, but without being able to get to a Compilation object, that's difficult.
                Return Nothing
            End Get
        End Property

        Private ReadOnly Property IFilter As Semantics.IExpression Implements Semantics.ICatch.Filter
            Get
                Return Me.ExceptionFilterOpt
            End Get
        End Property

        Private ReadOnly Property IHandler As Semantics.IBlock Implements Semantics.ICatch.Handler
            Get
                Return Me.Body
            End Get
        End Property

        Private ReadOnly Property ILocals As ILocalSymbol Implements Semantics.ICatch.ExceptionLocal
            Get
                Return Me.LocalOpt
            End Get
        End Property

        Private ReadOnly Property IKind As Semantics.OperationKind Implements Semantics.IOperation.Kind
            Get
                Return Semantics.OperationKind.CatchHandler
            End Get
        End Property

        Private ReadOnly Property ISyntax As SyntaxNode Implements Semantics.IOperation.Syntax
            Get
                Return Me.Syntax
            End Get
        End Property
    End Class

    Partial Class BoundBlock
        Implements Semantics.IBlock

        Private ReadOnly Property ILocals As ImmutableArray(Of ILocalSymbol) Implements Semantics.IBlock.Locals
            Get
                Return Me.Locals.As(Of ILocalSymbol)()
            End Get
        End Property

        Private ReadOnly Property IStatements As ImmutableArray(Of Semantics.IStatement) Implements Semantics.IBlock.Statements
            Get
                Return Me.Statements.As(Of Semantics.IStatement)()
            End Get
        End Property

        Protected Overrides Function StatementKind() As Semantics.OperationKind
            Return Semantics.OperationKind.BlockStatement
        End Function
    End Class

    Partial Class BoundBadStatement
        Protected Overrides Function StatementKind() As Semantics.OperationKind
            Return Semantics.OperationKind.None
        End Function
    End Class

    Partial Class BoundReturnStatement
        Implements Semantics.IReturn

        Private ReadOnly Property IReturned As Semantics.IExpression Implements Semantics.IReturn.Returned
            Get
                Return Me.ExpressionOpt
            End Get
        End Property

        Protected Overrides Function StatementKind() As Semantics.OperationKind
            Return Semantics.OperationKind.ReturnStatement
        End Function
    End Class

    Partial Class BoundThrowStatement
        Implements Semantics.IThrow

        Private ReadOnly Property IThrown As Semantics.IExpression Implements Semantics.IThrow.Thrown
            Get
                Return Me.ExpressionOpt
            End Get
        End Property

        Protected Overrides Function StatementKind() As Semantics.OperationKind
            Return Semantics.OperationKind.ThrowStatement
        End Function
    End Class

    Partial Class BoundWhileStatement
        Implements Semantics.IWhileUntil

        Private ReadOnly Property ICondition As Semantics.IExpression Implements Semantics.IForWhileUntil.Condition
            Get
                Return Me.Condition
            End Get
        End Property

        Private ReadOnly Property IBody As Semantics.IStatement Implements Semantics.ILoop.Body
            Get
                Return Me.Body
            End Get
        End Property

        Private ReadOnly Property ILoopClass As Semantics.LoopKind Implements Semantics.ILoop.LoopClass
            Get
                Return Semantics.LoopKind.WhileUntil
            End Get
        End Property

        Private ReadOnly Property IIsTopTest As Boolean Implements Semantics.IWhileUntil.IsTopTest
            Get
                Return True
            End Get
        End Property

        Private ReadOnly Property IIsWhile As Boolean Implements Semantics.IWhileUntil.IsWhile
            Get
                Return True
            End Get
        End Property

        Protected Overrides Function StatementKind() As Semantics.OperationKind
            Return Semantics.OperationKind.LoopStatement
        End Function
    End Class

    Partial Class BoundLocalDeclarationBase
        Implements Semantics.IVariable

        Protected MustOverride ReadOnly Property IInitialValue As Semantics.IExpression Implements Semantics.IVariable.InitialValue

        Protected MustOverride ReadOnly Property IVariable As ILocalSymbol Implements Semantics.IVariable.Variable
    End Class

    Partial Class BoundLocalDeclaration
        Implements Semantics.IVariableDeclaration

        Private ReadOnly Property IVariables As ImmutableArray(Of Semantics.IVariable) Implements Semantics.IVariableDeclaration.Variables
            Get
                Return ImmutableArray.Create(Of Semantics.IVariable)(Me)
            End Get
        End Property

        Protected Overrides ReadOnly Property IInitialValue As Semantics.IExpression
            Get
                Return Me.InitializerOpt
            End Get
        End Property

        Protected Overrides ReadOnly Property IVariable As ILocalSymbol
            Get
                Return Me.LocalSymbol
            End Get
        End Property

        Protected Overrides Function StatementKind() As Semantics.OperationKind
            Return Semantics.OperationKind.VariableDeclarationStatement
        End Function
    End Class

    Partial Class BoundAsNewLocalDeclarations
        Implements Semantics.IVariableDeclaration

        Private ReadOnly Property IVariables As ImmutableArray(Of Semantics.IVariable) Implements Semantics.IVariableDeclaration.Variables
            Get
                Return Me.LocalDeclarations.As(Of Semantics.IVariable)()
            End Get
        End Property

        Protected Overrides ReadOnly Property IInitialValue As Semantics.IExpression
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

        Protected Overrides Function StatementKind() As Semantics.OperationKind
            Return Semantics.OperationKind.VariableDeclarationStatement
        End Function
    End Class

    Partial Class BoundDimStatement
        Implements Semantics.IVariableDeclaration

        Private ReadOnly Property IVariables As ImmutableArray(Of Semantics.IVariable) Implements Semantics.IVariableDeclaration.Variables
            Get
                Return Me.LocalDeclarations.As(Of Semantics.IVariable)()
            End Get
        End Property

        Protected Overrides Function StatementKind() As Semantics.OperationKind
            Return Semantics.OperationKind.VariableDeclarationStatement
        End Function
    End Class

    Partial Class BoundYieldStatement
        Implements Semantics.IReturn
        Private ReadOnly Property IReturned As Semantics.IExpression Implements Semantics.IReturn.Returned
            Get
                Return Me.Expression
            End Get
        End Property

        Protected Overrides Function StatementKind() As Semantics.OperationKind
            Return Semantics.OperationKind.YieldReturnStatement
        End Function
    End Class

    Partial Class BoundLabelStatement
        Implements Semantics.ILabel

        Private ReadOnly Property ILabel As ILabelSymbol Implements Semantics.ILabel.Label
            Get
                Return Me.Label
            End Get
        End Property

        Protected Overrides Function StatementKind() As Semantics.OperationKind
            Return Semantics.OperationKind.LabelStatement
        End Function
    End Class

    Partial Class BoundGotoStatement
        Implements Semantics.IBranch

        Private ReadOnly Property ITarget As ILabelSymbol Implements Semantics.IBranch.Target
            Get
                Return Me.Label
            End Get
        End Property

        Protected Overrides Function StatementKind() As Semantics.OperationKind
            Return Semantics.OperationKind.GoToStatement
        End Function
    End Class

    Partial Class BoundContinueStatement
        Implements Semantics.IBranch

        Private ReadOnly Property ITarget As ILabelSymbol Implements Semantics.IBranch.Target
            Get
                Return Me.Label
            End Get
        End Property

        Protected Overrides Function StatementKind() As Semantics.OperationKind
            Return Semantics.OperationKind.ContinueStatement
        End Function
    End Class

    Partial Class BoundExitStatement
        Implements Semantics.IBranch

        Private ReadOnly Property ITarget As ILabelSymbol Implements Semantics.IBranch.Target
            Get
                Return Me.Label
            End Get
        End Property

        Protected Overrides Function StatementKind() As Semantics.OperationKind
            Return Semantics.OperationKind.BreakStatement
        End Function
    End Class

    Partial Class BoundSyncLockStatement
        Implements Semantics.ILock

        Private ReadOnly Property ILocked As Semantics.IExpression Implements Semantics.ILock.Locked
            Get
                Return Me.LockExpression
            End Get
        End Property

        Protected Overrides Function StatementKind() As Semantics.OperationKind
            Return Semantics.OperationKind.LockStatement
        End Function
    End Class

    Module Statement
        Friend ReadOnly EmptyStatementArray As ImmutableArray(Of Semantics.IStatement) = ImmutableArray.Create(Of Semantics.IStatement)()
    End Module

End Namespace
