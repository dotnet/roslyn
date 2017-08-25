' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Immutable
Imports Microsoft.CodeAnalysis.PooledObjects
Imports Microsoft.CodeAnalysis.VisualBasic
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.Semantics
    Partial Friend NotInheritable Class VisualBasicOperationFactory
        Private Shared Function ConvertToOptional(value As ConstantValue) As [Optional](Of Object)
            Return If(value Is Nothing, New [Optional](Of Object)(), New [Optional](Of Object)(value.Value))
        End Function

        Private Shared Function GetAssignmentKind(value As BoundAssignmentOperator) As OperationKind
            If value.LeftOnTheRightOpt IsNot Nothing Then
                Select Case value.Right.Kind
                    Case BoundKind.BinaryOperator
                        Dim rightBinary As BoundBinaryOperator = DirectCast(value.Right, BoundBinaryOperator)
                        If rightBinary.Left Is value.LeftOnTheRightOpt Then
                            Return OperationKind.CompoundAssignmentExpression
                        End If
                    Case BoundKind.UserDefinedBinaryOperator
                        Dim rightOperatorBinary As BoundUserDefinedBinaryOperator = DirectCast(value.Right, BoundUserDefinedBinaryOperator)

                        ' It is not permissible to access the Left property of a BoundUserDefinedBinaryOperator unconditionally,
                        ' because that property can throw an exception if the operator expression is semantically invalid.
                        ' get it through helper method
                        Dim leftOperand = GetUserDefinedBinaryOperatorChildBoundNode(rightOperatorBinary, 0)
                        If leftOperand Is value.LeftOnTheRightOpt Then
                            Return OperationKind.CompoundAssignmentExpression
                        End If
                End Select
            End If

            Return OperationKind.SimpleAssignmentExpression
        End Function

        Private Function GetUserDefinedBinaryOperatorChild([operator] As BoundUserDefinedBinaryOperator, index As Integer) As IOperation
            Dim child = Create(GetUserDefinedBinaryOperatorChildBoundNode([operator], index))
            If child IsNot Nothing Then
                Return child
            End If
            Dim isImplicit As Boolean = [operator].WasCompilerGenerated
            Return OperationFactory.CreateInvalidExpression(_semanticModel, [operator].UnderlyingExpression.Syntax, ImmutableArray(Of IOperation).Empty, isImplicit)
        End Function

        Private Shared Function GetUserDefinedBinaryOperatorChildBoundNode([operator] As BoundUserDefinedBinaryOperator, index As Integer) As BoundNode
            If [operator].UnderlyingExpression.Kind = BoundKind.Call Then
                If index = 0 Then
                    Return [operator].Left
                ElseIf index = 1 Then
                    Return [operator].Right
                Else
                    Throw ExceptionUtilities.UnexpectedValue(index)
                End If
            End If

            Return GetChildOfBadExpressionBoundNode([operator].UnderlyingExpression, index)
        End Function

        Friend Function DeriveArguments(boundArguments As ImmutableArray(Of BoundExpression), parameters As ImmutableArray(Of VisualBasic.Symbols.ParameterSymbol)) As ImmutableArray(Of IArgument)
            Dim argumentsLength As Integer = boundArguments.Length
            Debug.Assert(argumentsLength = parameters.Length)

            Dim arguments As ArrayBuilder(Of IArgument) = ArrayBuilder(Of IArgument).GetInstance(argumentsLength)
            For index As Integer = 0 To argumentsLength - 1 Step 1
                arguments.Add(DeriveArgument(index, boundArguments(index), parameters))
            Next

            Return arguments.ToImmutableAndFree()
        End Function

        Private Function DeriveArgument(index As Integer, argument As BoundExpression, parameters As ImmutableArray(Of VisualBasic.Symbols.ParameterSymbol)) As IArgument
            Dim isImplicit As Boolean = argument.WasCompilerGenerated
            Select Case argument.Kind
                Case BoundKind.ByRefArgumentWithCopyBack
                    Dim byRefArgument = DirectCast(argument, BoundByRefArgumentWithCopyBack)
                    Dim parameter = parameters(index)
                    Dim value = Create(byRefArgument.OriginalArgument)
                    Return New Argument(
                        ArgumentKind.Explicit,
                        parameter,
                        value,
                        Create(byRefArgument.InConversion),
                        Create(byRefArgument.OutConversion),
                        _semanticModel,
                        value.Syntax,
                        type:=Nothing,
                        constantValue:=Nothing,
                        isImplicit:=isImplicit)
                Case Else
                    Dim lastParameterIndex = parameters.Length - 1
                    If index = lastParameterIndex AndAlso ParameterIsParamArray(parameters(lastParameterIndex)) Then
                        ' TODO: figure out if this is true:
                        '       a compiler generated argument for a ParamArray parameter is created iff
                        '       a list of arguments (including 0 argument) is provided for ParamArray parameter in source
                        '       https://github.com/dotnet/roslyn/issues/18550
                        Dim kind = If(argument.WasCompilerGenerated AndAlso argument.Kind = BoundKind.ArrayCreation, ArgumentKind.ParamArray, ArgumentKind.Explicit)
                        Dim parameter = parameters(lastParameterIndex)
                        Dim value = Create(argument)

                        Return New Argument(
                            kind,
                            parameter,
                            value,
                            inConversion:=Nothing,
                            outConversion:=Nothing,
                            semanticModel:=_semanticModel,
                            syntax:=value.Syntax,
                            type:=Nothing,
                            constantValue:=Nothing,
                            isImplicit:=isImplicit)
                    Else
                        ' TODO: figure our if this is true:
                        '       a compiler generated argument for an Optional parameter is created iff
                        '       the argument is omitted from the source
                        '       https://github.com/dotnet/roslyn/issues/18550
                        Dim kind = If(argument.WasCompilerGenerated, ArgumentKind.DefaultValue, ArgumentKind.Explicit)
                        Dim parameter = parameters(index)
                        Dim value = Create(argument)

                        Return New Argument(
                            kind,
                            parameter,
                            value,
                            inConversion:=Nothing,
                            outConversion:=Nothing,
                            semanticModel:=_semanticModel,
                            syntax:=value.Syntax,
                            type:=Nothing,
                            constantValue:=Nothing,
                            isImplicit:=isImplicit)
                    End If
            End Select
        End Function

        Private Shared Function ParameterIsParamArray(parameter As VisualBasic.Symbols.ParameterSymbol) As Boolean
            Return If(parameter.IsParamArray AndAlso parameter.Type.Kind = SymbolKind.ArrayType, DirectCast(parameter.Type, VisualBasic.Symbols.ArrayTypeSymbol).IsSZArray, False)
        End Function

        Private Function GetChildOfBadExpression(parent As BoundNode, index As Integer) As IOperation
            Dim child = Create(GetChildOfBadExpressionBoundNode(parent, index))
            If child IsNot Nothing Then
                Return child
            End If
            Dim isImplicit As Boolean = parent.WasCompilerGenerated
            Return OperationFactory.CreateInvalidExpression(_semanticModel, parent.Syntax, ImmutableArray(Of IOperation).Empty, isImplicit)
        End Function

        Private Shared Function GetChildOfBadExpressionBoundNode(parent As BoundNode, index As Integer) As BoundNode
            Dim badParent As BoundBadExpression = TryCast(parent, BoundBadExpression)
            If badParent?.ChildBoundNodes.Length > index Then
                Dim child As BoundNode = badParent.ChildBoundNodes(index)
                If child IsNot Nothing Then
                    Return child
                End If
            End If

            Return Nothing
        End Function

        Private Function GetObjectCreationInitializers(expression As BoundObjectCreationExpression) As ImmutableArray(Of IOperation)
            Return If(expression.InitializerOpt IsNot Nothing, expression.InitializerOpt.Initializers.SelectAsArray(Function(n) Create(n)), ImmutableArray(Of IOperation).Empty)
        End Function

        Private Function GetAnonymousTypeCreationInitializers(expression As BoundAnonymousTypeCreationExpression) As ImmutableArray(Of IOperation)
            Debug.Assert(expression.Arguments.Length >= expression.Declarations.Length)

            Dim builder = ArrayBuilder(Of IOperation).GetInstance(expression.Arguments.Length)
            For i As Integer = 0 To expression.Arguments.Length - 1
                Dim value As IOperation = Create(expression.Arguments(i))
                If i >= expression.Declarations.Length Then
                    builder.Add(value)
                    Continue For
                End If

                Dim target As IOperation = Create(expression.Declarations(i))
                Dim syntax As SyntaxNode = If(value.Syntax?.Parent, expression.Syntax)
                Dim type As ITypeSymbol = target.Type
                Dim constantValue As [Optional](Of Object) = value.ConstantValue
                Dim assignment = New SimpleAssignmentExpression(target, value, _semanticModel, syntax, type, constantValue, isImplicit:=value.IsImplicit)
                builder.Add(assignment)
            Next i

            Return builder.ToImmutableAndFree()
        End Function

        Private Function GetSwitchStatementCases(caseBlocks As ImmutableArray(Of BoundCaseBlock)) As ImmutableArray(Of ISwitchCase)
            Return caseBlocks.SelectAsArray(
                Function(boundCaseBlock)
                    ' `CaseElseClauseSyntax` is bound to `BoundCaseStatement` with an empty list of case clauses,
                    ' so we explicitly create an IOperation node for Case-Else clause to differentiate it from Case clause.
                    Dim clauses As ImmutableArray(Of ICaseClause)
                    Dim caseStatement = boundCaseBlock.CaseStatement
                    Dim isImplicit As Boolean = boundCaseBlock.WasCompilerGenerated
                    If caseStatement.CaseClauses.IsEmpty AndAlso caseStatement.Syntax.Kind() = SyntaxKind.CaseElseStatement Then
                        clauses = ImmutableArray.Create(Of ICaseClause)(
                                                                    New DefaultCaseClause(
                                                                        _semanticModel,
                                                                        caseStatement.Syntax,
                                                                        type:=Nothing,
                                                                        constantValue:=Nothing,
                                                                        isImplicit:=isImplicit))
                    Else
                        clauses = caseStatement.CaseClauses.SelectAsArray(Function(n) DirectCast(Create(n), ICaseClause))
                    End If

                    Dim body = ImmutableArray.Create(Create(boundCaseBlock.Body))
                    Dim syntax = boundCaseBlock.Syntax
                    Return DirectCast(New SwitchCase(clauses, body, _semanticModel, syntax, type:=Nothing, constantValue:=Nothing, isImplicit:=isImplicit), ISwitchCase)
                End Function)
        End Function

        Private Shared Function GetSingleValueCaseClauseValue(clause As BoundSimpleCaseClause) As BoundExpression
            If clause.ValueOpt IsNot Nothing Then
                Return clause.ValueOpt
            End If

            If clause.ConditionOpt IsNot Nothing AndAlso clause.ConditionOpt.Kind = BoundKind.BinaryOperator Then
                Dim value As BoundBinaryOperator = DirectCast(clause.ConditionOpt, BoundBinaryOperator)
                If value.OperatorKind = VisualBasic.BinaryOperatorKind.Equals Then
                    Return value.Right
                End If
            End If

            Return Nothing
        End Function

        Private Shared Function GetRelationalCaseClauseValue(clause As BoundRelationalCaseClause) As BoundExpression
            If clause.OperandOpt IsNot Nothing Then
                Return clause.OperandOpt
            End If

            If clause.ConditionOpt?.Kind = BoundKind.BinaryOperator Then
                Return DirectCast(clause.ConditionOpt, BoundBinaryOperator).Right
            End If

            Return Nothing
        End Function

        Private Function GetForLoopStatementBefore(
            controlVariable As BoundExpression,
            initialValue As BoundExpression,
            limitValue As IOperation,
            stepValue As IOperation) As ImmutableArray(Of IOperation)
            Dim statements As ArrayBuilder(Of IOperation) = ArrayBuilder(Of IOperation).GetInstance()

            ' ControlVariable = InitialValue
            If controlVariable IsNot Nothing Then
                statements.Add(OperationFactory.CreateSimpleAssignmentExpressionStatement(Create(controlVariable), Create(initialValue), _semanticModel, initialValue.Syntax, isImplicit:=controlVariable.WasCompilerGenerated))
            End If

            ' T0 = LimitValue
            If Not limitValue.ConstantValue.HasValue Then
                statements.Add(
                                OperationFactory.CreateSimpleAssignmentExpressionStatement(
                                    New SyntheticLocalReferenceExpression(
                                            SyntheticLocalKind.ForLoopLimitValue,
                                            _semanticModel,
                                            limitValue.Syntax,
                                            limitValue.Type,
                                            constantValue:=Nothing,
                                            isImplicit:=limitValue.IsImplicit), limitValue, _semanticModel, limitValue.Syntax, limitValue.IsImplicit))
            End If

            ' T1 = StepValue
            If stepValue IsNot Nothing AndAlso Not stepValue.ConstantValue.HasValue Then
                statements.Add(
                                OperationFactory.CreateSimpleAssignmentExpressionStatement(
                                    New SyntheticLocalReferenceExpression(
                                        SyntheticLocalKind.ForLoopStepValue,
                                        _semanticModel,
                                        stepValue.Syntax,
                                        stepValue.Type,
                                        constantValue:=Nothing,
                                        isImplicit:=stepValue.IsImplicit), stepValue, _semanticModel, stepValue.Syntax, stepValue.IsImplicit))
            End If

            Return statements.ToImmutableAndFree()
        End Function

        Private Function GetForLoopStatementAtLoopBottom(
            controlVariable As IOperation,
            stepValue As BoundExpression,
            operatorsOpt As BoundForToUserDefinedOperators) As ImmutableArray(Of IOperation)
            Dim statements As ArrayBuilder(Of IOperation) = ArrayBuilder(Of IOperation).GetInstance()
            If operatorsOpt IsNot Nothing Then
                ' Use the operator methods. Figure out the precise rules first.
            Else
                If controlVariable IsNot Nothing Then
                    ' ControlVariable += StepValue
                    Dim controlType = DirectCast(controlVariable.Type, VisualBasic.Symbols.TypeSymbol)
                    Dim stepValueExpression As BoundExpression = If(stepValue, New BoundLiteral(Nothing, Semantics.Expression.SynthesizeNumeric(controlType, 1), controlType))

                    Dim value = Create(stepValueExpression)
                    Dim stepOperand As IOperation =
                                    If(stepValueExpression.IsConstant,
                                        value,
                                        New SyntheticLocalReferenceExpression(
                                            SyntheticLocalKind.ForLoopStepValue,
                                            _semanticModel,
                                            value.Syntax,
                                            value.Type,
                                            constantValue:=Nothing,
                                            isImplicit:=value.IsImplicit))
                    statements.Add(OperationFactory.CreateCompoundAssignmentExpressionStatement(
                        controlVariable, stepOperand,
                        BinaryOperatorKind.Add, controlType.IsNullableType(), False,
                        Nothing, _semanticModel, stepValueExpression.Syntax, value.IsImplicit))
                End If
            End If

            Return statements.ToImmutableAndFree()
        End Function

        Private Function GetForWhileUntilLoopStatementCondition(
            controlVariable As BoundExpression,
            limitValue As BoundExpression,
            stepValue As BoundExpression,
            operatorsOpt As BoundForToUserDefinedOperators) As IOperation

            Dim limitValueOperation = Create(limitValue)
            Dim limitValueReference As IOperation =
                        If(limitValue.IsConstant,
                           limitValueOperation,
                           New SyntheticLocalReferenceExpression(
                               SyntheticLocalKind.ForLoopLimitValue,
                               _semanticModel,
                               limitValueOperation.Syntax,
                               limitValueOperation.Type,
                               constantValue:=Nothing,
                               isImplicit:=limitValueOperation.IsImplicit))

            ' controlVariable can be a BoundBadExpression in case of error
            Dim booleanType As ITypeSymbol = controlVariable.ExpressionSymbol?.DeclaringCompilation.GetSpecialType(SpecialType.System_Boolean)

            If operatorsOpt IsNot Nothing Then
                ' Use the operator methods. Figure out the precise rules first.
                Return Nothing
            Else
                ' We are comparing the control variable against the limit value.  Using
                ' either the default stepping constant, or a user supplied constant.
                ' This will be a lifted comparison if either the control variable or
                ' limit value is nullable itself.
                Dim isLifted = controlVariable.Type.IsNullableType() OrElse
                               limitValue.Type.IsNullableType()

                If stepValue Is Nothing OrElse (stepValue.IsConstant AndAlso stepValue.ConstantValueOpt IsNot Nothing) Then
                    ' Either ControlVariable <= LimitValue or ControlVariable >= LimitValue, depending on whether the step value is negative.

                    Dim relationalCode As BinaryOperatorKind =
                        If(stepValue IsNot Nothing AndAlso stepValue.ConstantValueOpt.IsNegativeNumeric, BinaryOperatorKind.GreaterThanOrEqual, BinaryOperatorKind.LessThanOrEqual)
                    Return OperationFactory.CreateBinaryOperatorExpression(
                        relationalCode, _semanticModel.CloneOperation(Create(controlVariable)), limitValueReference, booleanType, _semanticModel, limitValueReference.Syntax, isLifted, isChecked:=False, isCompareText:=False, isImplicit:=limitValueReference.IsImplicit)
                Else
                    ' If(StepValue >= 0, ControlVariable <= LimitValue, ControlVariable >= LimitValue)
                    Dim value = Create(stepValue)
                    Dim stepValueReference As IOperation = New SyntheticLocalReferenceExpression(
                                SyntheticLocalKind.ForLoopStepValue,
                                _semanticModel,
                                value.Syntax,
                                value.Type,
                                constantValue:=Nothing,
                                isImplicit:=value.IsImplicit)

                    Dim stepRelationalCode As BinaryOperatorKind = BinaryOperatorKind.GreaterThanOrEqual
                    Dim stepConditionIsLifted = stepValue.Type.IsNullableType()
                    Dim stepCondition As IOperation = OperationFactory.CreateBinaryOperatorExpression(stepRelationalCode,
                                 stepValueReference,
                                 OperationFactory.CreateLiteralExpression(Semantics.Expression.SynthesizeNumeric(stepValueReference.Type, 0), stepValue.Type, _semanticModel, stepValue.Syntax, stepValueReference.IsImplicit),
                                 booleanType,
                                 _semanticModel,
                                 stepValue.Syntax,
                                 stepConditionIsLifted,
                                 isChecked:=False,
                                 isCompareText:=False,
                                 isImplicit:=stepValue.WasCompilerGenerated)

                    Dim positiveStepRelationalCode As BinaryOperatorKind = BinaryOperatorKind.LessThanOrEqual
                    Dim positiveStepCondition As IOperation = OperationFactory.CreateBinaryOperatorExpression(positiveStepRelationalCode, _semanticModel.CloneOperation(Create(controlVariable)), limitValueReference, booleanType, _semanticModel, limitValueReference.Syntax, isLifted, isChecked:=False, isCompareText:=False, isImplicit:=limitValueReference.IsImplicit)

                    Dim negativeStepRelationalCode As BinaryOperatorKind = BinaryOperatorKind.GreaterThanOrEqual
                    Dim negativeStepCondition As IOperation = OperationFactory.CreateBinaryOperatorExpression(negativeStepRelationalCode, _semanticModel.CloneOperation(Create(controlVariable)), _semanticModel.CloneOperation(limitValueReference), booleanType, _semanticModel, limitValueReference.Syntax, isLifted, isChecked:=False, isCompareText:=False, isImplicit:=limitValueReference.IsImplicit)

                    Return OperationFactory.CreateConditionalExpression(stepCondition, positiveStepCondition, negativeStepCondition, booleanType, _semanticModel, limitValueReference.Syntax, limitValueReference.IsImplicit)
                End If
            End If
        End Function

        Private Function GetVariableDeclarationStatementVariables(statement As BoundDimStatement) As ImmutableArray(Of IVariableDeclaration)
            Dim builder = ArrayBuilder(Of IVariableDeclaration).GetInstance()
            For Each base In statement.LocalDeclarations
                If base.Kind = BoundKind.LocalDeclaration Then
                    Dim declaration = DirectCast(base, BoundLocalDeclaration)
                    builder.Add(OperationFactory.CreateVariableDeclaration(declaration.LocalSymbol, Create(declaration.InitializerOpt), _semanticModel, declaration.Syntax))
                ElseIf base.Kind = BoundKind.AsNewLocalDeclarations Then
                    Dim asNewDeclarations = DirectCast(base, BoundAsNewLocalDeclarations)
                    Dim localSymbols = asNewDeclarations.LocalDeclarations.SelectAsArray(Of ILocalSymbol)(Function(declaration) declaration.LocalSymbol)
                    builder.Add(OperationFactory.CreateVariableDeclaration(localSymbols, Create(asNewDeclarations.Initializer), _semanticModel, asNewDeclarations.Syntax))
                End If
            Next

            Return builder.ToImmutableAndFree()
        End Function

        Private Function GetUsingStatementDeclaration(resourceList As ImmutableArray(Of BoundLocalDeclarationBase), syntax As SyntaxNode) As IVariableDeclarationStatement
            If resourceList.IsDefault Then
                Return Nothing
            End If
            Dim declaration = resourceList.Select(Function(n) Create(n)).OfType(Of IVariableDeclaration).ToImmutableArray()
            Return New VariableDeclarationStatement(
                            declaration,
                            _semanticModel,
                            syntax,
                            type:=Nothing,
                            constantValue:=Nothing,
                            isImplicit:=False) ' Declaration is always explicit
        End Function

        Private Function GetAddRemoveHandlerStatementExpression(statement As BoundAddRemoveHandlerStatement) As IOperation
            Dim eventAccess As BoundEventAccess = TryCast(statement.EventAccess, BoundEventAccess)
            Dim eventReference = If(eventAccess Is Nothing, Nothing, CreateBoundEventAccessOperation(eventAccess))
            Dim adds = statement.Kind = BoundKind.AddHandlerStatement
            Return New EventAssignmentExpression(
                eventReference, Create(statement.Handler), adds:=adds, semanticModel:=_semanticModel, syntax:=statement.Syntax, type:=Nothing, constantValue:=Nothing, isImplicit:=statement.WasCompilerGenerated)
        End Function

        Private Shared Function GetConversionKind(kind As VisualBasic.ConversionKind) As Semantics.ConversionKind
            Dim operationKind = Semantics.ConversionKind.Invalid

            If kind.HasFlag(VisualBasic.ConversionKind.UserDefined) Then
                operationKind = Semantics.ConversionKind.OperatorMethod
            ElseIf Conversions.IsIdentityConversion(kind) OrElse
                   kind.HasFlag(VisualBasic.ConversionKind.Reference) OrElse
                   kind.HasFlag(VisualBasic.ConversionKind.TypeParameter) OrElse
                   kind.HasFlag(VisualBasic.ConversionKind.Array) OrElse
                   kind.HasFlag(VisualBasic.ConversionKind.Value) Then
                operationKind = Semantics.ConversionKind.Cast
            ElseIf Conversions.NoConversion(kind) Then
                operationKind = Semantics.ConversionKind.Invalid
            ElseIf kind.HasFlag(VisualBasic.ConversionKind.InterpolatedString) Then
                operationKind = Semantics.ConversionKind.InterpolatedString
            Else
                operationKind = Semantics.ConversionKind.Basic
            End If

            Return operationKind
        End Function

        Friend Class Helper
            Friend Shared Function DeriveUnaryOperatorKind(operatorKind As VisualBasic.UnaryOperatorKind) As UnaryOperatorKind
                Select Case operatorKind And VisualBasic.UnaryOperatorKind.OpMask
                    Case VisualBasic.UnaryOperatorKind.Plus
                        Return UnaryOperatorKind.Plus
                    Case VisualBasic.UnaryOperatorKind.Minus
                        Return UnaryOperatorKind.Minus
                    Case VisualBasic.UnaryOperatorKind.Not
                        Return UnaryOperatorKind.Not
                    Case VisualBasic.UnaryOperatorKind.IsTrue
                        Return UnaryOperatorKind.True
                    Case VisualBasic.UnaryOperatorKind.IsFalse
                        Return UnaryOperatorKind.False
                    Case Else
                        Return UnaryOperatorKind.Invalid
                End Select
            End Function

            Friend Shared Function DeriveBinaryOperatorKind(operatorKind As VisualBasic.BinaryOperatorKind, leftOpt As BoundExpression) As BinaryOperatorKind
                Select Case operatorKind And VisualBasic.BinaryOperatorKind.OpMask
                    Case VisualBasic.BinaryOperatorKind.Add
                        Return BinaryOperatorKind.Add
                    Case VisualBasic.BinaryOperatorKind.Subtract
                        Return BinaryOperatorKind.Subtract
                    Case VisualBasic.BinaryOperatorKind.Multiply
                        Return BinaryOperatorKind.Multiply
                    Case VisualBasic.BinaryOperatorKind.Divide
                        Return BinaryOperatorKind.Divide
                    Case VisualBasic.BinaryOperatorKind.IntegerDivide
                        Return BinaryOperatorKind.IntegerDivide
                    Case VisualBasic.BinaryOperatorKind.Modulo
                        Return BinaryOperatorKind.Remainder
                    Case VisualBasic.BinaryOperatorKind.And
                        Return BinaryOperatorKind.And
                    Case VisualBasic.BinaryOperatorKind.Or
                        Return BinaryOperatorKind.Or
                    Case VisualBasic.BinaryOperatorKind.Xor
                        Return BinaryOperatorKind.ExclusiveOr
                    Case VisualBasic.BinaryOperatorKind.AndAlso
                        Return BinaryOperatorKind.ConditionalAnd
                    Case VisualBasic.BinaryOperatorKind.OrElse
                        Return BinaryOperatorKind.ConditionalOr
                    Case VisualBasic.BinaryOperatorKind.LeftShift
                        Return BinaryOperatorKind.LeftShift
                    Case VisualBasic.BinaryOperatorKind.RightShift
                        Return BinaryOperatorKind.RightShift
                    Case VisualBasic.BinaryOperatorKind.LessThan
                        Return BinaryOperatorKind.LessThan
                    Case VisualBasic.BinaryOperatorKind.LessThanOrEqual
                        Return BinaryOperatorKind.LessThanOrEqual
                    Case VisualBasic.BinaryOperatorKind.Equals
                        Return If(leftOpt?.Type?.SpecialType = SpecialType.System_Object, BinaryOperatorKind.ObjectValueEquals, BinaryOperatorKind.Equals)
                    Case VisualBasic.BinaryOperatorKind.NotEquals
                        Return If(leftOpt?.Type?.SpecialType = SpecialType.System_Object, BinaryOperatorKind.ObjectValueNotEquals, BinaryOperatorKind.NotEquals)
                    Case VisualBasic.BinaryOperatorKind.Is
                        Return BinaryOperatorKind.Equals
                    Case VisualBasic.BinaryOperatorKind.IsNot
                        Return BinaryOperatorKind.NotEquals
                    Case VisualBasic.BinaryOperatorKind.GreaterThanOrEqual
                        Return BinaryOperatorKind.GreaterThanOrEqual
                    Case VisualBasic.BinaryOperatorKind.GreaterThan
                        Return BinaryOperatorKind.GreaterThan
                    Case VisualBasic.BinaryOperatorKind.Power
                        Return BinaryOperatorKind.Power
                    Case VisualBasic.BinaryOperatorKind.Like
                        Return BinaryOperatorKind.Like
                    Case VisualBasic.BinaryOperatorKind.Concatenate
                        Return BinaryOperatorKind.Concatenate
                    Case Else
                        Return BinaryOperatorKind.Invalid
                End Select
            End Function
        End Class
    End Class
End Namespace
