' Copyright (c) Microsoft.  All Rights Reserved.  Licensedf under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Immutable
Imports Microsoft.CodeAnalysis.Semantics
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols

Namespace Microsoft.CodeAnalysis.VisualBasic

    Partial Friend Class BoundStatement
        Implements IOperation

        Private ReadOnly Property IOperation_Kind As OperationKind Implements IOperation.Kind
            Get
                Return Me.StatementKind()
            End Get
        End Property

        Private ReadOnly Property IOperation_IsInvalid As Boolean Implements IOperation.IsInvalid
            Get
                Return Me.HasErrors
            End Get
        End Property

        Private ReadOnly Property IOperation_Syntax As SyntaxNode Implements IOperation.Syntax
            Get
                Return Me.Syntax
            End Get
        End Property

        Private ReadOnly Property IOperation_Type As ITypeSymbol Implements IOperation.Type
            Get
                Return Nothing
            End Get
        End Property

        Private ReadOnly Property IOperation_ConstantValue As [Optional](Of Object) Implements IOperation.ConstantValue
            Get
                Return New [Optional](Of Object)()
            End Get
        End Property

        Protected MustOverride Function StatementKind() As OperationKind

        Public MustOverride Overloads Sub Accept(visitor As OperationVisitor) Implements IOperation.Accept

        Public MustOverride Overloads Function Accept(Of TArgument, TResult)(visitor As OperationVisitor(Of TArgument, TResult), argument As TArgument) As TResult Implements IOperation.Accept
    End Class

    Friend Partial Class BoundIfStatement
        Implements IIfStatement

        Private ReadOnly Property IOperation_Condition As IOperation Implements IIfStatement.Condition
            Get
                Return Me.Condition
            End Get
        End Property

        Private ReadOnly Property IIfStatement_IfTrueStatement As IOperation Implements IIfStatement.IfTrueStatement
            Get
                Return Me.Consequence
            End Get
        End Property

        Private ReadOnly Property IIfStatement_IfFalseStatement As IOperation Implements IIfStatement.IfFalseStatement
            Get
                Return Me.AlternativeOpt
            End Get
        End Property

        Protected Overrides Function StatementKind() As OperationKind
            Return OperationKind.IfStatement
        End Function

        Public Overrides Sub Accept(visitor As OperationVisitor)
            visitor.VisitIfStatement(Me)
        End Sub

        Public Overrides Function Accept(Of TArgument, TResult)(visitor As OperationVisitor(Of TArgument, TResult), argument As TArgument) As TResult
            Return visitor.VisitIfStatement(Me, argument)
        End Function
    End Class

    Friend Partial Class BoundSelectStatement
        Implements ISwitchStatement

        Private Shared ReadOnly s_caseBlocksMappings As New System.Runtime.CompilerServices.ConditionalWeakTable(Of BoundSelectStatement, Object)

        Private ReadOnly Property ISwitchStatement_Cases As ImmutableArray(Of ISwitchCase) Implements ISwitchStatement.Cases
            Get
                Dim cases = s_caseBlocksMappings.GetValue(Me, Function(boundSelect)
                                                                  Return boundSelect.CaseBlocks.SelectAsArray(Function(boundCaseBlock)
                                                                                                                  Return DirectCast(New CaseBlock(boundCaseBlock), ISwitchCase)
                                                                                                              End Function)
                                                              End Function)
                Return DirectCast(cases, ImmutableArray(Of ISwitchCase))
            End Get
        End Property

        Private ReadOnly Property ISwitchStatement_Value As IOperation Implements ISwitchStatement.Value
            Get
                Return Me.ExpressionStatement.Expression
            End Get
        End Property

        Protected Overrides Function StatementKind() As OperationKind
            Return OperationKind.SwitchStatement
        End Function

        Public Overrides Sub Accept(visitor As OperationVisitor)
            visitor.VisitSwitchStatement(Me)
        End Sub

        Public Overrides Function Accept(Of TArgument, TResult)(visitor As OperationVisitor(Of TArgument, TResult), argument As TArgument) As TResult
            Return visitor.VisitSwitchStatement(Me, argument)
        End Function

        Private NotInheritable Class CaseBlock
            Implements ISwitchCase

            Private ReadOnly _clauses As ImmutableArray(Of ICaseClause)
            Private ReadOnly _body As ImmutableArray(Of IOperation)
            Private ReadOnly _isInvalid As Boolean
            Private ReadOnly _syntax As SyntaxNode

            Public Sub New(boundCaseBlock As BoundCaseBlock)
                ' `CaseElseClauseSyntax` is bound to `BoundCaseStatement` with an empty list of case clauses, 
                ' so we explicitly create an IOperation node for Case-Else clause to differentiate it from Case clause.
                Dim caseStatement = boundCaseBlock.CaseStatement
                If caseStatement.CaseClauses.IsEmpty AndAlso caseStatement.Syntax.Kind() = SyntaxKind.CaseElseStatement Then
                    _clauses = ImmutableArray.Create(Of ICaseClause)(New CaseElse(caseStatement))
                Else
                    _clauses = caseStatement.CaseClauses.As(Of ICaseClause)()
                End If

                _body = ImmutableArray.Create(Of IOperation)(boundCaseBlock.Body)
                _isInvalid = boundCaseBlock.HasErrors
                _syntax = boundCaseBlock.Syntax
            End Sub

            Public Sub Accept(visitor As OperationVisitor) Implements IOperation.Accept
                visitor.VisitSwitchCase(Me)
            End Sub

            Public Function Accept(Of TArgument, TResult)(visitor As OperationVisitor(Of TArgument, TResult), argument As TArgument) As TResult Implements IOperation.Accept
                Return visitor.VisitSwitchCase(Me, argument)
            End Function

            Public ReadOnly Property Body As ImmutableArray(Of IOperation) Implements ISwitchCase.Body
                Get
                    Return _body
                End Get
            End Property

            Public ReadOnly Property Clauses As ImmutableArray(Of ICaseClause) Implements ISwitchCase.Clauses
                Get
                    Return _clauses
                End Get
            End Property

            Public ReadOnly Property IsInvalid As Boolean Implements IOperation.IsInvalid
                Get
                    Return _isInvalid
                End Get
            End Property

            Public ReadOnly Property Kind As OperationKind Implements IOperation.Kind
                Get
                    Return OperationKind.SwitchCase
                End Get
            End Property

            Public ReadOnly Property Syntax As SyntaxNode Implements IOperation.Syntax
                Get
                    Return _syntax
                End Get
            End Property

            Private ReadOnly Property IOperation_Type As ITypeSymbol Implements IOperation.Type
                Get
                    Return Nothing
                End Get
            End Property

            Private ReadOnly Property IOperation_ConstantValue As [Optional](Of Object) Implements IOperation.ConstantValue
                Get
                    Return New [Optional](Of Object)()
                End Get
            End Property

            Private NotInheritable Class CaseElse
                Implements ISingleValueCaseClause

                Private ReadOnly _boundCaseStatement As BoundCaseStatement

                Public Sub New(boundCaseStatement As BoundCaseStatement)
                    _boundCaseStatement = boundCaseStatement
                End Sub

                Public Sub Accept(visitor As OperationVisitor) Implements IOperation.Accept
                    visitor.VisitSingleValueCaseClause(Me)
                End Sub

                Public Function Accept(Of TArgument, TResult)(visitor As OperationVisitor(Of TArgument, TResult), argument As TArgument) As TResult Implements IOperation.Accept
                    Return visitor.VisitSingleValueCaseClause(Me, argument)
                End Function

                Public ReadOnly Property Equality As BinaryOperationKind Implements ISingleValueCaseClause.Equality
                    Get
                        Return BinaryOperationKind.None
                    End Get
                End Property

                Public ReadOnly Property Value As IOperation Implements ISingleValueCaseClause.Value
                    Get
                        Return Nothing
                    End Get
                End Property

                Public ReadOnly Property IsInvalid As Boolean Implements IOperation.IsInvalid
                    Get
                        Return _boundCaseStatement.HasErrors
                    End Get
                End Property

                Public ReadOnly Property Kind As OperationKind Implements IOperation.Kind
                    Get
                        Return OperationKind.SingleValueCaseClause
                    End Get
                End Property

                Public ReadOnly Property Syntax As SyntaxNode Implements IOperation.Syntax
                    Get
                        Return _boundCaseStatement.Syntax
                    End Get
                End Property

                Private ReadOnly Property ICaseClause_CaseKind As CaseKind Implements ICaseClause.CaseKind
                    Get
                        Return CaseKind.Default
                    End Get
                End Property

                Private ReadOnly Property IOperation_Type As ITypeSymbol Implements IOperation.Type
                    Get
                        Return Nothing
                    End Get
                End Property

                Private ReadOnly Property IOperation_ConstantValue As [Optional](Of Object) Implements IOperation.ConstantValue
                    Get
                        Return New [Optional](Of Object)()
                    End Get
                End Property
            End Class

        End Class
    End Class

    Friend Partial Class BoundCaseBlock
        Protected Overrides Function StatementKind() As OperationKind
            Return OperationKind.None
        End Function

        Public Overrides Sub Accept(visitor As OperationVisitor)
            visitor.VisitNoneOperation(Me)
        End Sub

        Public Overrides Function Accept(Of TArgument, TResult)(visitor As OperationVisitor(Of TArgument, TResult), argument As TArgument) As TResult
            Return visitor.VisitNoneOperation(Me, argument)
        End Function
    End Class

    Friend Partial Class BoundCaseClause
        Implements ICaseClause

        Private ReadOnly Property IOperation_IsInvalid As Boolean Implements IOperation.IsInvalid
            Get
                Return Me.HasErrors
            End Get
        End Property

        Private ReadOnly Property IOperation_Syntax As SyntaxNode Implements IOperation.Syntax
            Get
                Return Me.Syntax
            End Get
        End Property

        Protected MustOverride ReadOnly Property IOperation_Kind As OperationKind Implements IOperation.Kind

        Protected MustOverride ReadOnly Property ICaseClause_CaseKind As CaseKind Implements ICaseClause.CaseKind

        Public MustOverride Overloads Sub Accept(visitor As OperationVisitor) Implements IOperation.Accept

        Public MustOverride Overloads Function Accept(Of TArgument, TResult)(visitor As OperationVisitor(Of TArgument, TResult), argument As TArgument) As TResult Implements IOperation.Accept

        Private ReadOnly Property IType As ITypeSymbol Implements IOperation.Type
            Get
                Return Nothing
            End Get
        End Property

        Private ReadOnly Property IOperation_ConstantValue As [Optional](Of Object) Implements IOperation.ConstantValue
            Get
                Return New [Optional](Of Object)()
            End Get
        End Property
    End Class

    Friend Partial Class BoundSimpleCaseClause
        Implements ISingleValueCaseClause

        Private ReadOnly Property ISingleValueCaseClause_Equality As BinaryOperationKind Implements ISingleValueCaseClause.Equality
            Get
                ' Can lifted operators appear here, and if so what is their correct treatment?
                Dim caseValue As BoundExpression = DirectCast(Me.ISingleValueCaseClause_Value, BoundExpression)
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

                Return BinaryOperationKind.Invalid
            End Get
        End Property

        Private ReadOnly Property ISingleValueCaseClause_Value As IOperation Implements ISingleValueCaseClause.Value
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

        Protected Overrides ReadOnly Property IOperation_Kind As OperationKind
            Get
                Return OperationKind.SingleValueCaseClause
            End Get
        End Property

        Protected Overrides ReadOnly Property ICaseClause_CaseKind As CaseKind
            Get
                Return CaseKind.SingleValue
            End Get
        End Property

        Public Overrides Sub Accept(visitor As OperationVisitor)
            visitor.VisitSingleValueCaseClause(Me)
        End Sub

        Public Overrides Function Accept(Of TArgument, TResult)(visitor As OperationVisitor(Of TArgument, TResult), argument As TArgument) As TResult
            Return visitor.VisitSingleValueCaseClause(Me, argument)
        End Function
    End Class

    Friend Partial Class BoundRangeCaseClause
        Implements IRangeCaseClause

        Private ReadOnly Property IRangeCaseClause_MaximumValue As IOperation Implements IRangeCaseClause.MaximumValue
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

        Private ReadOnly Property IRangeCaseClause_MinimumValue As IOperation Implements IRangeCaseClause.MinimumValue
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

        Protected Overrides ReadOnly Property IOperation_Kind As OperationKind
            Get
                Return OperationKind.RangeCaseClause
            End Get
        End Property

        Protected Overrides ReadOnly Property ICaseClause_CaseKind As CaseKind
            Get
                Return CaseKind.Range
            End Get
        End Property

        Public Overrides Sub Accept(visitor As OperationVisitor)
            visitor.VisitRangeCaseClause(Me)
        End Sub

        Public Overrides Function Accept(Of TArgument, TResult)(visitor As OperationVisitor(Of TArgument, TResult), argument As TArgument) As TResult
            Return visitor.VisitRangeCaseClause(Me, argument)
        End Function
    End Class

    Friend Partial Class BoundRelationalCaseClause
        Implements IRelationalCaseClause

        Private ReadOnly Property IRelationalCaseClause_Relation As BinaryOperationKind Implements IRelationalCaseClause.Relation
            Get
                If Me.IRelationalCaseClause_Value IsNot Nothing Then
                    Return DeriveBinaryOperationKind(Me.OperatorKind, DirectCast(Me.IRelationalCaseClause_Value, BoundExpression))
                End If

                Return BinaryOperationKind.Invalid

            End Get
        End Property

        Private ReadOnly Property IRelationalCaseClause_Value As IOperation Implements IRelationalCaseClause.Value
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

        Protected Overrides ReadOnly Property IOperation_Kind As OperationKind
            Get
                Return OperationKind.RelationalCaseClause
            End Get
        End Property

        Protected Overrides ReadOnly Property ICaseClause_CaseKind As CaseKind
            Get
                Return CaseKind.Relational
            End Get
        End Property

        Public Overrides Sub Accept(visitor As OperationVisitor)
            visitor.VisitRelationalCaseClause(Me)
        End Sub

        Public Overrides Function Accept(Of TArgument, TResult)(visitor As OperationVisitor(Of TArgument, TResult), argument As TArgument) As TResult
            Return visitor.VisitRelationalCaseClause(Me, argument)
        End Function
    End Class

    Friend Partial Class BoundCaseStatement

        ' Cases are found by going through ISwitch, so the VB Case statement is orphaned.
        Protected Overrides Function StatementKind() As OperationKind
            Return OperationKind.None
        End Function

        Public Overrides Sub Accept(visitor As OperationVisitor)
            visitor.VisitNoneOperation(Me)
        End Sub

        Public Overrides Function Accept(Of TArgument, TResult)(visitor As OperationVisitor(Of TArgument, TResult), argument As TArgument) As TResult
            Return visitor.VisitNoneOperation(Me, argument)
        End Function
    End Class

    Friend Partial Class BoundDoLoopStatement
        Implements IWhileUntilLoopStatement

        Private ReadOnly Property IForWhileUntilLoopStatement_Condition As IOperation Implements IForWhileUntilLoopStatement.Condition
            Get
                Return Me.ConditionOpt
            End Get
        End Property

        Private ReadOnly Property ILoopStatement_Body As IOperation Implements ILoopStatement.Body
            Get
                Return Me.Body
            End Get
        End Property

        Private ReadOnly Property ILoopStatement_LoopKind As LoopKind Implements ILoopStatement.LoopKind
            Get
                Return LoopKind.WhileUntil
            End Get
        End Property

        Private ReadOnly Property IWhileUntilLoopStatement_IsTopTest As Boolean Implements IWhileUntilLoopStatement.IsTopTest
            Get
                Return Me.ConditionIsTop
            End Get
        End Property

        Private ReadOnly Property IWhileUntilLoopStatement_IsWhile As Boolean Implements IWhileUntilLoopStatement.IsWhile
            Get
                Return Not Me.ConditionIsUntil
            End Get
        End Property

        Protected Overrides Function StatementKind() As OperationKind
            Return OperationKind.LoopStatement
        End Function

        Public Overrides Sub Accept(visitor As OperationVisitor)
            visitor.VisitWhileUntilLoopStatement(Me)
        End Sub

        Public Overrides Function Accept(Of TArgument, TResult)(visitor As OperationVisitor(Of TArgument, TResult), argument As TArgument) As TResult
            Return visitor.VisitWhileUntilLoopStatement(Me, argument)
        End Function
    End Class

    Friend Partial Class BoundForToStatement
        Implements IForLoopStatement

        Private Shared ReadOnly s_loopBottomMappings As New System.Runtime.CompilerServices.ConditionalWeakTable(Of BoundForToStatement, Object)

        Private ReadOnly Property IForLoopStatement_AtLoopBottom As ImmutableArray(Of IOperation) Implements IForLoopStatement.AtLoopBottom
            Get
                Dim result = s_loopBottomMappings.GetValue(
                    Me,
                    Function(BoundFor)
                        Dim statements As ArrayBuilder(Of IOperation) = ArrayBuilder(Of IOperation).GetInstance()
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

                                Dim stepOperand As IOperation = If(stepValue.IsConstant, DirectCast(stepValue, IOperation), New Temporary(SyntheticLocalKind.ForLoopStepValue, BoundFor, stepValue))
                                statements.Add(New CompoundAssignment(controlReference, stepOperand, Semantics.Expression.DeriveAdditionKind(controlType), Nothing, stepValue.Syntax))
                            End If
                        End If

                        Return statements.ToImmutableAndFree()
                    End Function)

                Return DirectCast(result, ImmutableArray(Of IOperation))
            End Get
        End Property

        Private Shared ReadOnly s_loopTopMappings As New System.Runtime.CompilerServices.ConditionalWeakTable(Of BoundForToStatement, Object)

        Private ReadOnly Property IForLoopStatement_Before As ImmutableArray(Of IOperation) Implements IForLoopStatement.Before
            Get
                Dim result = s_loopTopMappings.GetValue(
                    Me,
                    Function(BoundFor)
                        Dim statements As ArrayBuilder(Of IOperation) = ArrayBuilder(Of IOperation).GetInstance()

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

                Return DirectCast(result, ImmutableArray(Of IOperation))
            End Get
        End Property

        Private ReadOnly Property IForLoopStatement_Locals As ImmutableArray(Of ILocalSymbol) Implements IForLoopStatement.Locals
            Get
                Return ImmutableArray(Of ILocalSymbol).Empty
            End Get
        End Property

        Private Shared ReadOnly s_loopConditionMappings As New System.Runtime.CompilerServices.ConditionalWeakTable(Of BoundForToStatement, IOperation)

        Private ReadOnly Property IForWhileUntilLoopStatment_Condition As IOperation Implements IForWhileUntilLoopStatement.Condition
            Get
                Return s_loopConditionMappings.GetValue(
                    Me,
                    Function(BoundFor)
                        Dim limitValue As IOperation = If(BoundFor.LimitValue.IsConstant, DirectCast(BoundFor.LimitValue, IOperation), New Temporary(SyntheticLocalKind.ForLoopLimitValue, BoundFor, BoundFor.LimitValue))
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

                                Dim stepValue As IOperation = New Temporary(SyntheticLocalKind.ForLoopStepValue, BoundFor, BoundFor.StepValue)
                                Dim stepRelationalCode As BinaryOperationKind = DeriveBinaryOperationKind(BinaryOperatorKind.GreaterThanOrEqual, BoundFor.StepValue)
                                Dim stepCondition As IOperation = New Binary(stepRelationalCode, stepValue, New BoundLiteral(BoundFor.StepValue.Syntax, Semantics.Expression.SynthesizeNumeric(stepValue.Type, 0), BoundFor.StepValue.Type), booleanType, BoundFor.StepValue.Syntax)

                                Dim positiveStepRelationalCode As BinaryOperationKind = DeriveBinaryOperationKind(BinaryOperatorKind.LessThanOrEqual, controlVariable)
                                Dim positiveStepCondition As IOperation = New Binary(positiveStepRelationalCode, controlVariable, limitValue, booleanType, limitValue.Syntax)

                                Dim negativeStepRelationalCode As BinaryOperationKind = DeriveBinaryOperationKind(BinaryOperatorKind.GreaterThanOrEqual, controlVariable)
                                Dim negativeStepCondition As IOperation = New Binary(negativeStepRelationalCode, controlVariable, limitValue, booleanType, limitValue.Syntax)

                                Return New ConditionalChoice(stepCondition, positiveStepCondition, negativeStepCondition, booleanType, limitValue.Syntax)
                            End If
                        End If
                    End Function)
            End Get
        End Property

        Private ReadOnly Property ILoopStatement_Body As IOperation Implements ILoopStatement.Body
            Get
                Return Me.Body
            End Get
        End Property

        Private ReadOnly Property ILoopStatement_LoopKind As LoopKind Implements ILoopStatement.LoopKind
            Get
                Return LoopKind.For
            End Get
        End Property

        Protected Overrides Function StatementKind() As OperationKind
            Return OperationKind.LoopStatement
        End Function

        Public Overrides Sub Accept(visitor As OperationVisitor)
            visitor.VisitForLoopStatement(Me)
        End Sub

        Public Overrides Function Accept(Of TArgument, TResult)(visitor As OperationVisitor(Of TArgument, TResult), argument As TArgument) As TResult
            Return visitor.VisitForLoopStatement(Me, argument)
        End Function

        Private NotInheritable Class Temporary
            Implements ISyntheticLocalReferenceExpression

            Private _temporaryKind As SyntheticLocalKind
            Private _containingStatement As IOperation
            Private _capturedValue As IOperation

            Public Sub New(temporaryKind As SyntheticLocalKind, containingStatement As IOperation, capturedValue As IOperation)
                Me._temporaryKind = temporaryKind
                Me._containingStatement = containingStatement
                Me._capturedValue = capturedValue
            End Sub

            Public Sub Accept(visitor As OperationVisitor) Implements IOperation.Accept
                visitor.VisitSyntheticLocalReferenceExpression(Me)
            End Sub

            Public Function Accept(Of TArgument, TResult)(visitor As OperationVisitor(Of TArgument, TResult), argument As TArgument) As TResult Implements IOperation.Accept
                Return visitor.VisitSyntheticLocalReferenceExpression(Me, argument)
            End Function

            Public ReadOnly Property ConstantValue As [Optional](Of Object) Implements IOperation.ConstantValue
                Get
                    Return New [Optional](Of Object)()
                End Get
            End Property

            Public ReadOnly Property Kind As OperationKind Implements IOperation.Kind
                Get
                    Return OperationKind.SyntheticLocalReferenceExpression
                End Get
            End Property

            Public ReadOnly Property IsInvalid As Boolean Implements IOperation.IsInvalid
                Get
                    Return False
                End Get
            End Property

            Public ReadOnly Property Type As ITypeSymbol Implements IOperation.Type
                Get
                    Return Me._capturedValue.Type
                End Get
            End Property

            Public ReadOnly Property Syntax As SyntaxNode Implements IOperation.Syntax
                Get
                    Return Me._capturedValue.Syntax
                End Get
            End Property

            Public ReadOnly Property ContainingStatement As IOperation Implements ISyntheticLocalReferenceExpression.ContainingStatement
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

    Friend Partial Class BoundForEachStatement
        Implements IForEachLoopStatement

        Private ReadOnly Property IForEachLoopStatement_IterationVariable As ILocalSymbol Implements IForEachLoopStatement.IterationVariable
            Get
                Dim controlReference As ILocalReferenceExpression = TryCast(Me.ControlVariable, ILocalReferenceExpression)
                If controlReference IsNot Nothing Then
                    Return controlReference.Local
                End If

                Return Nothing
            End Get
        End Property

        Private ReadOnly Property ILoopStatement_LoopKind As LoopKind Implements ILoopStatement.LoopKind
            Get
                Return LoopKind.ForEach
            End Get
        End Property

        Private ReadOnly Property IForEachLoopStatement_Collection As IOperation Implements IForEachLoopStatement.Collection
            Get
                Return Me.Collection
            End Get
        End Property

        Private ReadOnly Property ILoopStatement_Body As IOperation Implements ILoopStatement.Body
            Get
                Return Me.Body
            End Get
        End Property

        Protected Overrides Function StatementKind() As OperationKind
            Return OperationKind.LoopStatement
        End Function

        Public Overrides Sub Accept(visitor As OperationVisitor)
            visitor.VisitForEachLoopStatement(Me)
        End Sub

        Public Overrides Function Accept(Of TArgument, TResult)(visitor As OperationVisitor(Of TArgument, TResult), argument As TArgument) As TResult
            Return visitor.VisitForEachLoopStatement(Me, argument)
        End Function
    End Class

    Friend Partial Class BoundTryStatement
        Implements ITryStatement

        Private ReadOnly Property ITryStatement_Body As IBlockStatement Implements ITryStatement.Body
            Get
                Return Me.TryBlock
            End Get
        End Property

        Private ReadOnly Property ITryStatement_Catches As ImmutableArray(Of ICatchClause) Implements ITryStatement.Catches
            Get
                Return Me.CatchBlocks.As(Of ICatchClause)()
            End Get
        End Property

        Private ReadOnly Property ITryStatement_FinallyHandler As IBlockStatement Implements ITryStatement.FinallyHandler
            Get
                Return Me.FinallyBlockOpt
            End Get
        End Property

        Protected Overrides Function StatementKind() As OperationKind
            Return OperationKind.TryStatement
        End Function

        Public Overrides Sub Accept(visitor As OperationVisitor)
            visitor.VisitTryStatement(Me)
        End Sub

        Public Overrides Function Accept(Of TArgument, TResult)(visitor As OperationVisitor(Of TArgument, TResult), argument As TArgument) As TResult
            Return visitor.VisitTryStatement(Me, argument)
        End Function
    End Class

    Partial Friend Class BoundCatchBlock
        Implements ICatchClause

        Private ReadOnly Property ICatchClause_CaughtType As ITypeSymbol Implements ICatchClause.CaughtType
            Get
                If Me.ExceptionSourceOpt IsNot Nothing Then
                    Return Me.ExceptionSourceOpt.Type
                End If

                ' Ideally return System.Exception here is best, but without being able to get to a Compilation object, that's difficult.
                Return Nothing
            End Get
        End Property

        Private ReadOnly Property ICatchClause_Filter As IOperation Implements ICatchClause.Filter
            Get
                Return Me.ExceptionFilterOpt
            End Get
        End Property

        Private ReadOnly Property ICatchClause_Handler As IBlockStatement Implements ICatchClause.Handler
            Get
                Return Me.Body
            End Get
        End Property

        Private ReadOnly Property ICatchClause_ExceptionLocal As ILocalSymbol Implements ICatchClause.ExceptionLocal
            Get
                Return Me.LocalOpt
            End Get
        End Property

        Private ReadOnly Property IOperation_Kind As OperationKind Implements IOperation.Kind
            Get
                Return OperationKind.CatchClause
            End Get
        End Property

        Private ReadOnly Property IOperation_IsInvalid As Boolean Implements IOperation.IsInvalid
            Get
                Return Me.HasErrors
            End Get
        End Property

        Private ReadOnly Property IOperation_Syntax As SyntaxNode Implements IOperation.Syntax
            Get
                Return Me.Syntax
            End Get
        End Property

        Private ReadOnly Property IOperation_Type As ITypeSymbol Implements IOperation.Type
            Get
                Return Nothing
            End Get
        End Property

        Private ReadOnly Property IOperation_ConstantValue As [Optional](Of Object) Implements IOperation.ConstantValue
            Get
                Return New [Optional](Of Object)()
            End Get
        End Property

        Public Overloads Sub Accept(visitor As OperationVisitor) Implements IOperation.Accept
            visitor.VisitCatch(Me)
        End Sub

        Public Overloads Function Accept(Of TArgument, TResult)(visitor As OperationVisitor(Of TArgument, TResult), argument As TArgument) As TResult Implements IOperation.Accept
            Return visitor.VisitCatch(Me, argument)
        End Function
    End Class

    Friend Partial Class BoundBlock
        Implements IBlockStatement

        Private Shared ReadOnly s_blockStatementsMappings As New System.Runtime.CompilerServices.ConditionalWeakTable(Of BoundBlock, Object)

        Private ReadOnly Property IBlockStatement_Locals As ImmutableArray(Of ILocalSymbol) Implements IBlockStatement.Locals
            Get
                Return Me.Locals.As(Of ILocalSymbol)()
            End Get
        End Property

        Private ReadOnly Property IBlockStatement_Statements As ImmutableArray(Of IOperation) Implements IBlockStatement.Statements
            Get
                ' This is to filter out operations of kind None.
                Dim statements = s_blockStatementsMappings.GetValue(Me, Function(boundBlock)
                                                                            Return boundBlock.Statements.As(Of IOperation).WhereAsArray(Function(statement)
                                                                                                                                            Return statement.Kind <> OperationKind.None
                                                                                                                                        End Function)
                                                                        End Function)
                Return DirectCast(statements, ImmutableArray(Of IOperation))
            End Get
        End Property

        Protected Overrides Function StatementKind() As OperationKind
            Return OperationKind.BlockStatement
        End Function

        Public Overrides Sub Accept(visitor As OperationVisitor)
            visitor.VisitBlockStatement(Me)
        End Sub

        Public Overrides Function Accept(Of TArgument, TResult)(visitor As OperationVisitor(Of TArgument, TResult), argument As TArgument) As TResult
            Return visitor.VisitBlockStatement(Me, argument)
        End Function
    End Class

    Partial Friend Class BoundBadStatement
        Implements IInvalidStatement

        Protected Overrides Function StatementKind() As OperationKind
            Return OperationKind.InvalidStatement
        End Function

        Public Overrides Sub Accept(visitor As OperationVisitor)
            visitor.VisitInvalidStatement(Me)
        End Sub

        Public Overrides Function Accept(Of TArgument, TResult)(visitor As OperationVisitor(Of TArgument, TResult), argument As TArgument) As TResult
            Return visitor.VisitInvalidStatement(Me, argument)
        End Function
    End Class

    Friend Partial Class BoundReturnStatement
        Implements IReturnStatement

        Private ReadOnly Property IReturnStatement_ReturnedValue As IOperation Implements IReturnStatement.ReturnedValue
            Get
                Return Me.ExpressionOpt
            End Get
        End Property

        Protected Overrides Function StatementKind() As OperationKind
            Return OperationKind.ReturnStatement
        End Function

        Public Overrides Sub Accept(visitor As OperationVisitor)
            visitor.VisitReturnStatement(Me)
        End Sub

        Public Overrides Function Accept(Of TArgument, TResult)(visitor As OperationVisitor(Of TArgument, TResult), argument As TArgument) As TResult
            Return visitor.VisitReturnStatement(Me, argument)
        End Function
    End Class

    Friend Partial Class BoundThrowStatement
        Implements IThrowStatement

        Private ReadOnly Property IThrowStatement_ThrownObject As IOperation Implements IThrowStatement.ThrownObject
            Get
                Return Me.ExpressionOpt
            End Get
        End Property

        Protected Overrides Function StatementKind() As OperationKind
            Return OperationKind.ThrowStatement
        End Function

        Public Overrides Sub Accept(visitor As OperationVisitor)
            visitor.VisitThrowStatement(Me)
        End Sub

        Public Overrides Function Accept(Of TArgument, TResult)(visitor As OperationVisitor(Of TArgument, TResult), argument As TArgument) As TResult
            Return visitor.VisitThrowStatement(Me, argument)
        End Function
    End Class

    Friend Partial Class BoundWhileStatement
        Implements IWhileUntilLoopStatement

        Private ReadOnly Property IForWhileUntilLoopStatement_Condition As IOperation Implements IForWhileUntilLoopStatement.Condition
            Get
                Return Me.Condition
            End Get
        End Property

        Private ReadOnly Property ILoopStatement_Body As IOperation Implements ILoopStatement.Body
            Get
                Return Me.Body
            End Get
        End Property

        Private ReadOnly Property ILoopStatement_LoopKind As LoopKind Implements ILoopStatement.LoopKind
            Get
                Return LoopKind.WhileUntil
            End Get
        End Property

        Private ReadOnly Property IWhileUntilLoopStatement_IsTopTest As Boolean Implements IWhileUntilLoopStatement.IsTopTest
            Get
                Return True
            End Get
        End Property

        Private ReadOnly Property IWhileUntilLoopStatement_IsWhile As Boolean Implements IWhileUntilLoopStatement.IsWhile
            Get
                Return True
            End Get
        End Property

        Protected Overrides Function StatementKind() As OperationKind
            Return OperationKind.LoopStatement
        End Function

        Public Overrides Sub Accept(visitor As OperationVisitor)
            visitor.VisitWhileUntilLoopStatement(Me)
        End Sub

        Public Overrides Function Accept(Of TArgument, TResult)(visitor As OperationVisitor(Of TArgument, TResult), argument As TArgument) As TResult
            Return visitor.VisitWhileUntilLoopStatement(Me, argument)
        End Function
    End Class

    Friend Partial Class BoundDimStatement
        Implements IVariableDeclarationStatement

        Private Shared ReadOnly s_variablesMappings As New System.Runtime.CompilerServices.ConditionalWeakTable(Of BoundDimStatement, Object)

        Private ReadOnly Property IVariableDeclarationStatement_Variables As ImmutableArray(Of IVariableDeclaration) Implements IVariableDeclarationStatement.Variables
            Get
                Dim variables = s_variablesMappings.GetValue(Me, Function(dimStatement)
                                                                     Dim builder = ArrayBuilder(Of IVariableDeclaration).GetInstance()
                                                                     For Each base In dimStatement.LocalDeclarations
                                                                         If base.Kind = BoundKind.LocalDeclaration Then
                                                                             Dim declaration = DirectCast(base, BoundLocalDeclaration)
                                                                             builder.Add(New VariableDeclaration(declaration.LocalSymbol, declaration.InitializerOpt, declaration.Syntax))
                                                                         ElseIf base.Kind = BoundKind.AsNewLocalDeclarations Then
                                                                             Dim asNewDeclarations = DirectCast(base, BoundAsNewLocalDeclarations)
                                                                             For Each asNewDeclaration In asNewDeclarations.LocalDeclarations
                                                                                 builder.Add(New VariableDeclaration(asNewDeclaration.LocalSymbol, asNewDeclarations.Initializer, asNewDeclaration.Syntax))
                                                                             Next
                                                                         End If
                                                                     Next
                                                                     Return builder.ToImmutableAndFree()
                                                                 End Function
                                                               )
                Return DirectCast(variables, ImmutableArray(Of IVariableDeclaration))
            End Get
        End Property

        Protected Overrides Function StatementKind() As OperationKind
            Return OperationKind.VariableDeclarationStatement
        End Function

        Public Overrides Sub Accept(visitor As OperationVisitor)
            visitor.VisitVariableDeclarationStatement(Me)
        End Sub

        Public Overrides Function Accept(Of TArgument, TResult)(visitor As OperationVisitor(Of TArgument, TResult), argument As TArgument) As TResult
            Return visitor.VisitVariableDeclarationStatement(Me, argument)
        End Function
    End Class

    Friend Partial Class BoundYieldStatement
        Implements IReturnStatement
        Private ReadOnly Property IReturnStatement_ReturnedValue As IOperation Implements IReturnStatement.ReturnedValue
            Get
                Return Me.Expression
            End Get
        End Property

        Protected Overrides Function StatementKind() As OperationKind
            Return OperationKind.YieldReturnStatement
        End Function

        Public Overrides Sub Accept(visitor As OperationVisitor)
            visitor.VisitReturnStatement(Me)
        End Sub

        Public Overrides Function Accept(Of TArgument, TResult)(visitor As OperationVisitor(Of TArgument, TResult), argument As TArgument) As TResult
            Return visitor.VisitReturnStatement(Me, argument)
        End Function
    End Class

    Friend Partial Class BoundLabelStatement
        Implements ILabelStatement

        Private ReadOnly Property ILabelStatement_Label As ILabelSymbol Implements ILabelStatement.Label
            Get
                Return Me.Label
            End Get
        End Property

        Private ReadOnly Property ILabelStatement_LabeledStatement As IOperation Implements ILabelStatement.LabeledStatement
            Get
                ' The VB bound trees do not encode the statement to which the label is attached.
                Return Nothing
            End Get
        End Property

        Protected Overrides Function StatementKind() As OperationKind
            Return OperationKind.LabelStatement
        End Function

        Public Overrides Sub Accept(visitor As OperationVisitor)
            visitor.VisitLabelStatement(Me)
        End Sub

        Public Overrides Function Accept(Of TArgument, TResult)(visitor As OperationVisitor(Of TArgument, TResult), argument As TArgument) As TResult
            Return visitor.VisitLabelStatement(Me, argument)
        End Function
    End Class

    Friend Partial Class BoundGotoStatement
        Implements IBranchStatement

        Private ReadOnly Property IBranchStatement_Target As ILabelSymbol Implements IBranchStatement.Target
            Get
                Return Me.Label
            End Get
        End Property

        Private ReadOnly Property IBranchStatement_BranchKind As BranchKind Implements IBranchStatement.BranchKind
            Get
                Return BranchKind.GoTo
            End Get
        End Property

        Protected Overrides Function StatementKind() As OperationKind
            Return OperationKind.BranchStatement
        End Function

        Public Overrides Sub Accept(visitor As OperationVisitor)
            visitor.VisitBranchStatement(Me)
        End Sub

        Public Overrides Function Accept(Of TArgument, TResult)(visitor As OperationVisitor(Of TArgument, TResult), argument As TArgument) As TResult
            Return visitor.VisitBranchStatement(Me, argument)
        End Function
    End Class

    Friend Partial Class BoundContinueStatement
        Implements IBranchStatement

        Private ReadOnly Property IBranchStatement_Target As ILabelSymbol Implements IBranchStatement.Target
            Get
                Return Me.Label
            End Get
        End Property

        Private ReadOnly Property IBranchStatement_BranchKind As BranchKind Implements IBranchStatement.BranchKind
            Get
                Return BranchKind.Continue
            End Get
        End Property

        Protected Overrides Function StatementKind() As OperationKind
            Return OperationKind.BranchStatement
        End Function

        Public Overrides Sub Accept(visitor As OperationVisitor)
            visitor.VisitBranchStatement(Me)
        End Sub

        Public Overrides Function Accept(Of TArgument, TResult)(visitor As OperationVisitor(Of TArgument, TResult), argument As TArgument) As TResult
            Return visitor.VisitBranchStatement(Me, argument)
        End Function
    End Class

    Friend Partial Class BoundExitStatement
        Implements IBranchStatement

        Private ReadOnly Property IBranchStatement_Target As ILabelSymbol Implements IBranchStatement.Target
            Get
                Return Me.Label
            End Get
        End Property

        Private ReadOnly Property IBranchStatement_BranchKind As BranchKind Implements IBranchStatement.BranchKind
            Get
                Return BranchKind.Break
            End Get
        End Property

        Protected Overrides Function StatementKind() As OperationKind
            Return OperationKind.BranchStatement
        End Function

        Public Overrides Sub Accept(visitor As OperationVisitor)
            visitor.VisitBranchStatement(Me)
        End Sub

        Public Overrides Function Accept(Of TArgument, TResult)(visitor As OperationVisitor(Of TArgument, TResult), argument As TArgument) As TResult
            Return visitor.VisitBranchStatement(Me, argument)
        End Function
    End Class

    Friend Partial Class BoundSyncLockStatement
        Implements ILockStatement

        Private ReadOnly Property ILockStatement_LockedObject As IOperation Implements ILockStatement.LockedObject
            Get
                Return Me.LockExpression
            End Get
        End Property

        Private ReadOnly Property ILockStatement_Body As IOperation Implements ILockStatement.Body
            Get
                Return Me.Body
            End Get
        End Property

        Protected Overrides Function StatementKind() As OperationKind
            Return OperationKind.LockStatement
        End Function

        Public Overrides Sub Accept(visitor As OperationVisitor)
            visitor.VisitLockStatement(Me)
        End Sub

        Public Overrides Function Accept(Of TArgument, TResult)(visitor As OperationVisitor(Of TArgument, TResult), argument As TArgument) As TResult
            Return visitor.VisitLockStatement(Me, argument)
        End Function
    End Class

    Partial Friend Class BoundNoOpStatement
        Implements IEmptyStatement

        Protected Overrides Function StatementKind() As OperationKind
            Return OperationKind.EmptyStatement
        End Function

        Public Overrides Sub Accept(visitor As OperationVisitor)
            visitor.VisitEmptyStatement(Me)
        End Sub

        Public Overrides Function Accept(Of TArgument, TResult)(visitor As OperationVisitor(Of TArgument, TResult), argument As TArgument) As TResult
            Return visitor.VisitEmptyStatement(Me, argument)
        End Function
    End Class

    Friend Partial Class BoundSequencePoint
        Protected Overrides Function StatementKind() As OperationKind
            Return OperationKind.None
        End Function

        Public Overrides Sub Accept(visitor As OperationVisitor)
            visitor.VisitNoneOperation(Me)
        End Sub

        Public Overrides Function Accept(Of TArgument, TResult)(visitor As OperationVisitor(Of TArgument, TResult), argument As TArgument) As TResult
            Return visitor.VisitNoneOperation(Me, argument)
        End Function
    End Class

    Friend Partial Class BoundSequencePointWithSpan
        Protected Overrides Function StatementKind() As OperationKind
            Return OperationKind.None
        End Function

        Public Overrides Sub Accept(visitor As OperationVisitor)
            visitor.VisitNoneOperation(Me)
        End Sub

        Public Overrides Function Accept(Of TArgument, TResult)(visitor As OperationVisitor(Of TArgument, TResult), argument As TArgument) As TResult
            Return visitor.VisitNoneOperation(Me, argument)
        End Function
    End Class

    Friend Partial Class BoundStateMachineScope
        Protected Overrides Function StatementKind() As OperationKind
            Return OperationKind.None
        End Function

        Public Overrides Sub Accept(visitor As OperationVisitor)
            visitor.VisitNoneOperation(Me)
        End Sub

        Public Overrides Function Accept(Of TArgument, TResult)(visitor As OperationVisitor(Of TArgument, TResult), argument As TArgument) As TResult
            Return visitor.VisitNoneOperation(Me, argument)
        End Function
    End Class

    Partial Friend Class BoundStopStatement
        Implements IStopStatement

        Protected Overrides Function StatementKind() As OperationKind
            Return OperationKind.StopStatement
        End Function

        Public Overrides Sub Accept(visitor As OperationVisitor)
            visitor.VisitStopStatement(Me)
        End Sub

        Public Overrides Function Accept(Of TArgument, TResult)(visitor As OperationVisitor(Of TArgument, TResult), argument As TArgument) As TResult
            Return visitor.VisitStopStatement(Me, argument)
        End Function
    End Class

    Partial Friend Class BoundEndStatement
        Implements IEndStatement

        Protected Overrides Function StatementKind() As OperationKind
            Return OperationKind.EndStatement
        End Function

        Public Overrides Sub Accept(visitor As OperationVisitor)
            visitor.VisitEndStatement(Me)
        End Sub

        Public Overrides Function Accept(Of TArgument, TResult)(visitor As OperationVisitor(Of TArgument, TResult), argument As TArgument) As TResult
            Return visitor.VisitEndStatement(Me, argument)
        End Function
    End Class

    Friend Partial Class BoundWithStatement
        Implements IWithStatement

        Private ReadOnly Property IWithStatement_Body As IOperation Implements IWithStatement.Body
            Get
                Return Me.Body
            End Get
        End Property

        Private ReadOnly Property IWithStatement_Value As IOperation Implements IWithStatement.Value
            Get
                Return Me.OriginalExpression
            End Get
        End Property

        Protected Overrides Function StatementKind() As OperationKind
            Return OperationKind.WithStatement
        End Function

        Public Overrides Sub Accept(visitor As OperationVisitor)
            visitor.VisitWithStatement(Me)
        End Sub

        Public Overrides Function Accept(Of TArgument, TResult)(visitor As OperationVisitor(Of TArgument, TResult), argument As TArgument) As TResult
            Return visitor.VisitWithStatement(Me, argument)
        End Function
    End Class

    Friend Partial Class BoundUsingStatement
        Implements IUsingStatement

        Private ReadOnly Property IUsingStatement_Value As IOperation Implements IUsingStatement.Value
            Get
                Return Me.ResourceExpressionOpt
            End Get
        End Property

        Private Shared ReadOnly s_variablesMappings As New System.Runtime.CompilerServices.ConditionalWeakTable(Of BoundUsingStatement, Variables)

        Private ReadOnly Property IUsingStatement_Declaration As IVariableDeclarationStatement Implements IUsingStatement.Declaration
            Get
                Return s_variablesMappings.GetValue(
                    Me,
                    Function(BoundUsing)
                        Return New Variables(BoundUsing.ResourceList.As(Of IVariableDeclaration))
                    End Function)
            End Get
        End Property

        Private ReadOnly Property IUsingStatement_Body As IOperation Implements IUsingStatement.Body
            Get
                Return Me.Body
            End Get
        End Property

        Protected Overrides Function StatementKind() As OperationKind
            Return OperationKind.UsingStatement
        End Function

        Public Overrides Sub Accept(visitor As OperationVisitor)
            visitor.VisitUsingStatement(Me)
        End Sub

        Public Overrides Function Accept(Of TArgument, TResult)(visitor As OperationVisitor(Of TArgument, TResult), argument As TArgument) As TResult
            Return visitor.VisitUsingStatement(Me, argument)
        End Function

        Private NotInheritable Class Variables
            Implements IVariableDeclarationStatement

            Private ReadOnly _variables As ImmutableArray(Of IVariableDeclaration)

            Public Sub New(variables As ImmutableArray(Of IVariableDeclaration))
                _variables = variables
            End Sub

            Public Sub Accept(visitor As OperationVisitor) Implements IOperation.Accept
                visitor.VisitVariableDeclarationStatement(Me)
            End Sub

            Public Function Accept(Of TArgument, TResult)(visitor As OperationVisitor(Of TArgument, TResult), argument As TArgument) As TResult Implements IOperation.Accept
                Return visitor.VisitVariableDeclarationStatement(Me, argument)
            End Function

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

            Private ReadOnly Property IVariableDeclarationStatement_Variables As ImmutableArray(Of IVariableDeclaration) Implements IVariableDeclarationStatement.Variables
                Get
                    Return _variables
                End Get
            End Property

            Private ReadOnly Property IOperation_Type As ITypeSymbol Implements IOperation.Type
                Get
                    Return Nothing
                End Get
            End Property

            Private ReadOnly Property IOperation_ConstantValue As [Optional](Of Object) Implements IOperation.ConstantValue
                Get
                    Return New [Optional](Of Object)()
                End Get
            End Property
        End Class
    End Class

    Friend Partial Class BoundExpressionStatement
        Implements IExpressionStatement

        Private ReadOnly Property IExpressionStatement_Expression As IOperation Implements IExpressionStatement.Expression
            Get
                Return Me.Expression
            End Get
        End Property

        Protected Overrides Function StatementKind() As OperationKind
            Return OperationKind.ExpressionStatement
        End Function

        Public Overrides Sub Accept(visitor As OperationVisitor)
            visitor.VisitExpressionStatement(Me)
        End Sub

        Public Overrides Function Accept(Of TArgument, TResult)(visitor As OperationVisitor(Of TArgument, TResult), argument As TArgument) As TResult
            Return visitor.VisitExpressionStatement(Me, argument)
        End Function
    End Class

    Partial Friend Class BoundAddRemoveHandlerStatement
        Implements IExpressionStatement

        Protected Shared ReadOnly s_expressionsMappings As New System.Runtime.CompilerServices.ConditionalWeakTable(Of BoundAddRemoveHandlerStatement, IEventAssignmentExpression)

        Protected Overrides Function StatementKind() As OperationKind
            Return OperationKind.ExpressionStatement
        End Function

        Protected MustOverride ReadOnly Property IExpressionStatement_Expression As IOperation Implements IExpressionStatement.Expression

        Public Overrides Sub Accept(visitor As OperationVisitor)
            visitor.VisitExpressionStatement(Me)
        End Sub

        Public Overrides Function Accept(Of TArgument, TResult)(visitor As OperationVisitor(Of TArgument, TResult), argument As TArgument) As TResult
            Return visitor.VisitExpressionStatement(Me, argument)
        End Function

        Protected NotInheritable Class EventAssignmentExpression
            Implements IEventAssignmentExpression

            Private ReadOnly _statement As BoundAddRemoveHandlerStatement
            Private ReadOnly _adds As Boolean

            Public Sub New(statement As BoundAddRemoveHandlerStatement, adds As Boolean)
                _statement = statement
                _adds = adds
            End Sub

            Public Sub Accept(visitor As OperationVisitor) Implements IOperation.Accept
                visitor.VisitEventAssignmentExpression(Me)
            End Sub

            Public Function Accept(Of TArgument, TResult)(visitor As OperationVisitor(Of TArgument, TResult), argument As TArgument) As TResult Implements IOperation.Accept
                Return visitor.VisitEventAssignmentExpression(Me, argument)
            End Function

            Public ReadOnly Property Adds As Boolean Implements IEventAssignmentExpression.Adds
                Get
                    Return _adds
                End Get
            End Property

            Public ReadOnly Property ConstantValue As [Optional](Of Object) Implements IOperation.ConstantValue
                Get
                    Return New [Optional](Of Object)()
                End Get
            End Property

            Public ReadOnly Property [Event] As IEventSymbol Implements IEventAssignmentExpression.Event
                Get
                    Dim eventAccess As BoundEventAccess = TryCast(_statement.EventAccess, BoundEventAccess)
                    If eventAccess IsNot Nothing Then
                        Return eventAccess.EventSymbol
                    End If

                    Return Nothing
                End Get
            End Property

            Public ReadOnly Property EventInstance As IOperation Implements IEventAssignmentExpression.EventInstance
                Get
                    If [Event].IsStatic Then
                        Return Nothing
                    End If

                    Dim eventAccess As BoundEventAccess = TryCast(_statement.EventAccess, BoundEventAccess)
                    If eventAccess IsNot Nothing Then
                        Return eventAccess.ReceiverOpt
                    End If

                    Return Nothing
                End Get
            End Property

            Public ReadOnly Property HandlerValue As IOperation Implements IEventAssignmentExpression.HandlerValue
                Get
                    Return _statement.Handler
                End Get
            End Property

            Public ReadOnly Property IsInvalid As Boolean Implements IOperation.IsInvalid
                Get
                    Return _statement.HasErrors
                End Get
            End Property

            Public ReadOnly Property Kind As OperationKind Implements IOperation.Kind
                Get
                    Return OperationKind.EventAssignmentExpression
                End Get
            End Property

            Public ReadOnly Property Type As ITypeSymbol Implements IOperation.Type
                Get
                    Return Nothing
                End Get
            End Property

            Public ReadOnly Property Syntax As SyntaxNode Implements IOperation.Syntax
                Get
                    Return _statement.Syntax
                End Get
            End Property
        End Class
    End Class

    Partial Friend Class BoundAddHandlerStatement

        Protected Overrides ReadOnly Property IExpressionStatement_Expression As IOperation
            Get
                Return s_expressionsMappings.GetValue(Me, Function(statement)
                                                              Return New EventAssignmentExpression(statement, True)
                                                          End Function)
            End Get
        End Property
    End Class

    Partial Friend Class BoundRemoveHandlerStatement

        Protected Overrides ReadOnly Property IExpressionStatement_Expression As IOperation
            Get
                Return s_expressionsMappings.GetValue(Me, Function(statement)
                                                              Return New EventAssignmentExpression(statement, False)
                                                          End Function)
            End Get
        End Property
    End Class

    Partial Friend Class BoundRedimStatement
        Protected Overrides Function StatementKind() As OperationKind
            Return OperationKind.None
        End Function

        Public Overrides Sub Accept(visitor As OperationVisitor)
            visitor.VisitNoneOperation(Me)
        End Sub

        Public Overrides Function Accept(Of TArgument, TResult)(visitor As OperationVisitor(Of TArgument, TResult), argument As TArgument) As TResult
            Return visitor.VisitNoneOperation(Me, argument)
        End Function
    End Class

    Partial Friend Class BoundRedimClause
        Protected Overrides Function StatementKind() As OperationKind
            Return OperationKind.None
        End Function

        Public Overrides Sub Accept(visitor As OperationVisitor)
            visitor.VisitNoneOperation(Me)
        End Sub

        Public Overrides Function Accept(Of TArgument, TResult)(visitor As OperationVisitor(Of TArgument, TResult), argument As TArgument) As TResult
            Return visitor.VisitNoneOperation(Me, argument)
        End Function
    End Class

    Partial Friend Class BoundEraseStatement
        Protected Overrides Function StatementKind() As OperationKind
            Return OperationKind.None
        End Function

        Public Overrides Sub Accept(visitor As OperationVisitor)
            visitor.VisitNoneOperation(Me)
        End Sub

        Public Overrides Function Accept(Of TArgument, TResult)(visitor As OperationVisitor(Of TArgument, TResult), argument As TArgument) As TResult
            Return visitor.VisitNoneOperation(Me, argument)
        End Function
    End Class

    Partial Friend Class BoundLocalDeclaration
        Protected Overrides Function StatementKind() As OperationKind
            Return OperationKind.None
        End Function

        Public Overrides Sub Accept(visitor As OperationVisitor)
            visitor.VisitNoneOperation(Me)
        End Sub

        Public Overrides Function Accept(Of TArgument, TResult)(visitor As OperationVisitor(Of TArgument, TResult), argument As TArgument) As TResult
            Return visitor.VisitNoneOperation(Me, argument)
        End Function
    End Class

    Partial Friend Class BoundAsNewLocalDeclarations
        Protected Overrides Function StatementKind() As OperationKind
            Return OperationKind.None
        End Function

        Public Overrides Sub Accept(visitor As OperationVisitor)
            visitor.VisitNoneOperation(Me)
        End Sub

        Public Overrides Function Accept(Of TArgument, TResult)(visitor As OperationVisitor(Of TArgument, TResult), argument As TArgument) As TResult
            Return visitor.VisitNoneOperation(Me, argument)
        End Function
    End Class

    Partial Friend Class BoundInitializer
        Protected Overrides Function StatementKind() As OperationKind
            Return OperationKind.None
        End Function

        Public Overrides Sub Accept(visitor As OperationVisitor)
            visitor.VisitNoneOperation(Me)
        End Sub

        Public Overrides Function Accept(Of TArgument, TResult)(visitor As OperationVisitor(Of TArgument, TResult), argument As TArgument) As TResult
            Return visitor.VisitNoneOperation(Me, argument)
        End Function
    End Class

    Partial Friend Class BoundConditionalGoto
        Protected Overrides Function StatementKind() As OperationKind
            Return OperationKind.None
        End Function

        Public Overrides Sub Accept(visitor As OperationVisitor)
            visitor.VisitNoneOperation(Me)
        End Sub

        Public Overrides Function Accept(Of TArgument, TResult)(visitor As OperationVisitor(Of TArgument, TResult), argument As TArgument) As TResult
            Return visitor.VisitNoneOperation(Me, argument)
        End Function
    End Class

    Partial Friend Class BoundStatementList
        Protected Overrides Function StatementKind() As OperationKind
            Return OperationKind.None
        End Function

        Public Overrides Sub Accept(visitor As OperationVisitor)
            visitor.VisitNoneOperation(Me)
        End Sub

        Public Overrides Function Accept(Of TArgument, TResult)(visitor As OperationVisitor(Of TArgument, TResult), argument As TArgument) As TResult
            Return visitor.VisitNoneOperation(Me, argument)
        End Function
    End Class

    Partial Friend Class BoundRaiseEventStatement
        Implements IExpressionStatement

        Public ReadOnly Property Expression As IOperation Implements IExpressionStatement.Expression
            Get
                Return Me.EventInvocation
            End Get
        End Property

        Protected Overrides Function StatementKind() As OperationKind
            Return OperationKind.ExpressionStatement
        End Function

        Public Overrides Sub Accept(visitor As OperationVisitor)
            visitor.VisitExpressionStatement(Me)
        End Sub

        Public Overrides Function Accept(Of TArgument, TResult)(visitor As OperationVisitor(Of TArgument, TResult), argument As TArgument) As TResult
            Return visitor.VisitExpressionStatement(Me, argument)
        End Function
    End Class

    Friend Partial Class BoundResumeStatement
        Protected Overrides Function StatementKind() As OperationKind
            Return OperationKind.None
        End Function

        Public Overrides Sub Accept(visitor As OperationVisitor)
            visitor.VisitNoneOperation(Me)
        End Sub

        Public Overrides Function Accept(Of TArgument, TResult)(visitor As OperationVisitor(Of TArgument, TResult), argument As TArgument) As TResult
            Return visitor.VisitNoneOperation(Me, argument)
        End Function
    End Class

    Friend Partial Class BoundOnErrorStatement
        Protected Overrides Function StatementKind() As OperationKind
            Return OperationKind.None
        End Function

        Public Overrides Sub Accept(visitor As OperationVisitor)
            visitor.VisitNoneOperation(Me)
        End Sub

        Public Overrides Function Accept(Of TArgument, TResult)(visitor As OperationVisitor(Of TArgument, TResult), argument As TArgument) As TResult
            Return visitor.VisitNoneOperation(Me, argument)
        End Function
    End Class

    Friend Partial Class BoundUnstructuredExceptionHandlingStatement
        Protected Overrides Function StatementKind() As OperationKind
            Return OperationKind.None
        End Function

        Public Overrides Sub Accept(visitor As OperationVisitor)
            visitor.VisitNoneOperation(Me)
        End Sub

        Public Overrides Function Accept(Of TArgument, TResult)(visitor As OperationVisitor(Of TArgument, TResult), argument As TArgument) As TResult
            Return visitor.VisitNoneOperation(Me, argument)
        End Function
    End Class

    Friend Partial Class BoundUnstructuredExceptionOnErrorSwitch
        Protected Overrides Function StatementKind() As OperationKind
            Return OperationKind.None
        End Function

        Public Overrides Sub Accept(visitor As OperationVisitor)
            visitor.VisitNoneOperation(Me)
        End Sub

        Public Overrides Function Accept(Of TArgument, TResult)(visitor As OperationVisitor(Of TArgument, TResult), argument As TArgument) As TResult
            Return visitor.VisitNoneOperation(Me, argument)
        End Function
    End Class

    Friend Partial Class BoundUnstructuredExceptionResumeSwitch
        Protected Overrides Function StatementKind() As OperationKind
            Return OperationKind.None
        End Function

        Public Overrides Sub Accept(visitor As OperationVisitor)
            visitor.VisitNoneOperation(Me)
        End Sub

        Public Overrides Function Accept(Of TArgument, TResult)(visitor As OperationVisitor(Of TArgument, TResult), argument As TArgument) As TResult
            Return visitor.VisitNoneOperation(Me, argument)
        End Function
    End Class
End Namespace
