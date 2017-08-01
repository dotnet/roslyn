﻿' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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

            Return OperationFactory.CreateInvalidExpression([operator].UnderlyingExpression.Syntax, ImmutableArray(Of IOperation).Empty)
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
                        value.Syntax,
                        type:=Nothing,
                        constantValue:=Nothing)
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
                            syntax:=value.Syntax,
                            type:=Nothing,
                            constantValue:=Nothing)
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
                            syntax:=value.Syntax,
                            type:=Nothing,
                            constantValue:=Nothing)
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

            Return OperationFactory.CreateInvalidExpression(parent.Syntax, ImmutableArray(Of IOperation).Empty)
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
                Dim assignment = New SimpleAssignmentExpression(target, value, syntax, type, constantValue)
                builder.Add(assignment)
            Next i

            Return builder.ToImmutableAndFree()
        End Function

        Private Function GetSwitchStatementCases(statement As BoundSelectStatement) As ImmutableArray(Of ISwitchCase)
            Return statement.CaseBlocks.SelectAsArray(
                Function(boundCaseBlock)
                    ' `CaseElseClauseSyntax` is bound to `BoundCaseStatement` with an empty list of case clauses, 
                    ' so we explicitly create an IOperation node for Case-Else clause to differentiate it from Case clause.
                    Dim clauses As ImmutableArray(Of ICaseClause)
                    Dim caseStatement = boundCaseBlock.CaseStatement
                    If caseStatement.CaseClauses.IsEmpty AndAlso caseStatement.Syntax.Kind() = SyntaxKind.CaseElseStatement Then
                        clauses = ImmutableArray.Create(Of ICaseClause)(
                                                                    New DefaultCaseClause(
                                                                        syntax:=caseStatement.Syntax,
                                                                        type:=Nothing,
                                                                        constantValue:=Nothing))
                    Else
                        clauses = caseStatement.CaseClauses.SelectAsArray(Function(n) DirectCast(Create(n), ICaseClause))
                    End If

                    Dim body = ImmutableArray.Create(Create(boundCaseBlock.Body))
                    Dim syntax = boundCaseBlock.Syntax
                    Return DirectCast(New SwitchCase(clauses, body, syntax, type:=Nothing, constantValue:=Nothing), ISwitchCase)
                End Function)
        End Function

        Private Shared Function GetSingleValueCaseClauseValue(clause As BoundSimpleCaseClause) As BoundExpression
            If clause.ValueOpt IsNot Nothing Then
                Return clause.ValueOpt
            End If

            If clause.ConditionOpt IsNot Nothing AndAlso clause.ConditionOpt.Kind = BoundKind.BinaryOperator Then
                Dim value As BoundBinaryOperator = DirectCast(clause.ConditionOpt, BoundBinaryOperator)
                If value.OperatorKind = BinaryOperatorKind.Equals Then
                    Return value.Right
                End If
            End If

            Return Nothing
        End Function

        Private Shared Function GetSingleValueCaseClauseEquality(clause As BoundSimpleCaseClause) As BinaryOperationKind
            ' Can lifted operators appear here, and if so what is their correct treatment?
            Dim caseValue As BoundExpression = GetSingleValueCaseClauseValue(clause)
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

        Private Function GetForLoopStatementBefore(boundFor As BoundForToStatement) As ImmutableArray(Of IOperation)
            Dim statements As ArrayBuilder(Of IOperation) = ArrayBuilder(Of IOperation).GetInstance()

            ' ControlVariable = InitialValue
            Dim controlReference As IOperation = Create(boundFor.ControlVariable)
            If controlReference IsNot Nothing Then
                statements.Add(OperationFactory.CreateSimpleAssignmentExpressionStatement(controlReference, Create(boundFor.InitialValue), boundFor.InitialValue.Syntax))
            End If

            ' T0 = LimitValue
            If Not boundFor.LimitValue.IsConstant Then
                Dim value = Create(boundFor.LimitValue)
                statements.Add(
                                OperationFactory.CreateSimpleAssignmentExpressionStatement(
                                    New SyntheticLocalReferenceExpression(
                                            SyntheticLocalKind.ForLoopLimitValue,
                                            Create(boundFor),
                                            syntax:=value.Syntax,
                                            type:=value.Type,
                                            constantValue:=Nothing), value, value.Syntax))
            End If

            ' T1 = StepValue
            If boundFor.StepValue IsNot Nothing AndAlso Not boundFor.StepValue.IsConstant Then
                Dim value = Create(boundFor.StepValue)
                statements.Add(
                                OperationFactory.CreateSimpleAssignmentExpressionStatement(
                                    New SyntheticLocalReferenceExpression(
                                        SyntheticLocalKind.ForLoopStepValue,
                                        Create(boundFor),
                                        syntax:=value.Syntax,
                                        type:=value.Type,
                                        constantValue:=Nothing), value, value.Syntax))
            End If

            Return statements.ToImmutableAndFree()
        End Function

        Private Function GetForLoopStatementAtLoopBottom(boundFor As BoundForToStatement) As ImmutableArray(Of IOperation)
            Dim statements As ArrayBuilder(Of IOperation) = ArrayBuilder(Of IOperation).GetInstance()
            Dim operators As BoundForToUserDefinedOperators = boundFor.OperatorsOpt
            If operators IsNot Nothing Then
                ' Use the operator methods. Figure out the precise rules first.
            Else
                Dim controlReference As IOperation = Create(boundFor.ControlVariable)
                If controlReference IsNot Nothing Then

                    ' ControlVariable += StepValue

                    Dim controlType As VisualBasic.Symbols.TypeSymbol = boundFor.ControlVariable.Type

                    Dim stepValue As BoundExpression = If(boundFor.StepValue, New BoundLiteral(Nothing, Semantics.Expression.SynthesizeNumeric(controlType, 1), controlType))

                    Dim value = Create(stepValue)
                    Dim stepOperand As IOperation =
                                    If(stepValue.IsConstant,
                                        value,
                                        New SyntheticLocalReferenceExpression(
                                            SyntheticLocalKind.ForLoopStepValue,
                                            Create(boundFor),
                                            syntax:=value.Syntax,
                                            type:=value.Type,
                                            constantValue:=Nothing))
                    statements.Add(OperationFactory.CreateCompoundAssignmentExpressionStatement(
                        controlReference, stepOperand,
                        Expression.DeriveAdditionKind(controlType.GetNullableUnderlyingTypeOrSelf()), controlType.IsNullableType(),
                        Nothing, stepValue.Syntax))
                End If
            End If

            Return statements.ToImmutableAndFree()
        End Function

        Private Function GetForWhileUntilLoopStatmentCondition(boundFor As BoundForToStatement) As IOperation
            Dim operationValue = Create(boundFor.LimitValue)
            Dim limitValue As IOperation =
                        If(boundFor.LimitValue.IsConstant,
                            operationValue,
                            New SyntheticLocalReferenceExpression(
                                SyntheticLocalKind.ForLoopLimitValue,
                                Create(boundFor),
                                syntax:=operationValue.Syntax,
                                type:=operationValue.Type,
                                constantValue:=Nothing))

            Dim controlVariable As BoundExpression = boundFor.ControlVariable

            ' controlVariable can be a BoundBadExpression in case of error
            Dim booleanType As ITypeSymbol = controlVariable.ExpressionSymbol?.DeclaringCompilation.GetSpecialType(SpecialType.System_Boolean)

            Dim operators As BoundForToUserDefinedOperators = boundFor.OperatorsOpt
            If operators IsNot Nothing Then
                ' Use the operator methods. Figure out the precise rules first.
                Return Nothing
            Else
                ' We are comparing the control variable against the limit value.  Using
                ' either the default stepping constant, or a user supplied constant.
                ' This will be a lifted comparison if either the control variable or
                ' limit value is nullable itself.
                Dim isLifted = controlVariable.Type.IsNullableType() OrElse
                               boundFor.LimitValue.Type.IsNullableType()

                If boundFor.StepValue Is Nothing OrElse (boundFor.StepValue.IsConstant AndAlso boundFor.StepValue.ConstantValueOpt IsNot Nothing) Then
                    ' Either ControlVariable <= LimitValue or ControlVariable >= LimitValue, depending on whether the step value is negative.

                    Dim relationalCode As BinaryOperationKind = Helper.DeriveBinaryOperationKind(If(boundFor.StepValue IsNot Nothing AndAlso boundFor.StepValue.ConstantValueOpt.IsNegativeNumeric, BinaryOperatorKind.GreaterThanOrEqual, BinaryOperatorKind.LessThanOrEqual), controlVariable)
                    Return OperationFactory.CreateBinaryOperatorExpression(
                        relationalCode, Create(controlVariable), limitValue, booleanType, limitValue.Syntax, isLifted)
                Else
                    ' If(StepValue >= 0, ControlVariable <= LimitValue, ControlVariable >= LimitValue)

                    Dim value = Create(boundFor.StepValue)
                    Dim stepValue As IOperation = New SyntheticLocalReferenceExpression(
                                SyntheticLocalKind.ForLoopStepValue,
                                Create(boundFor),
                                syntax:=value.Syntax,
                                type:=value.Type,
                                constantValue:=Nothing)

                    Dim stepRelationalCode As BinaryOperationKind = Helper.DeriveBinaryOperationKind(BinaryOperatorKind.GreaterThanOrEqual, boundFor.StepValue)
                    Dim stepConditionIsLifted = boundFor.StepValue.Type.IsNullableType()
                    Dim stepCondition As IOperation = OperationFactory.CreateBinaryOperatorExpression(stepRelationalCode,
                                 stepValue,
                                 OperationFactory.CreateLiteralExpression(Semantics.Expression.SynthesizeNumeric(stepValue.Type, 0), boundFor.StepValue.Type, boundFor.StepValue.Syntax),
                                 booleanType,
                                 boundFor.StepValue.Syntax,
                                 stepConditionIsLifted)

                    Dim positiveStepRelationalCode As BinaryOperationKind = Helper.DeriveBinaryOperationKind(BinaryOperatorKind.LessThanOrEqual, controlVariable)
                    Dim positiveStepCondition As IOperation = OperationFactory.CreateBinaryOperatorExpression(positiveStepRelationalCode, Create(controlVariable), limitValue, booleanType, limitValue.Syntax, isLifted)

                    Dim negativeStepRelationalCode As BinaryOperationKind = Helper.DeriveBinaryOperationKind(BinaryOperatorKind.GreaterThanOrEqual, controlVariable)
                    Dim negativeStepCondition As IOperation = OperationFactory.CreateBinaryOperatorExpression(negativeStepRelationalCode, Create(controlVariable), limitValue, booleanType, limitValue.Syntax, isLifted)

                    Return OperationFactory.CreateConditionalChoiceExpression(stepCondition, positiveStepCondition, negativeStepCondition, booleanType, limitValue.Syntax)
                End If
            End If
        End Function

        Private Function GetVariableDeclarationStatementVariables(statement As BoundDimStatement) As ImmutableArray(Of IVariableDeclaration)
            Dim builder = ArrayBuilder(Of IVariableDeclaration).GetInstance()
            For Each base In statement.LocalDeclarations
                If base.Kind = BoundKind.LocalDeclaration Then
                    Dim declaration = DirectCast(base, BoundLocalDeclaration)
                    builder.Add(OperationFactory.CreateVariableDeclaration(declaration.LocalSymbol, Create(declaration.InitializerOpt), declaration.Syntax))
                ElseIf base.Kind = BoundKind.AsNewLocalDeclarations Then
                    Dim asNewDeclarations = DirectCast(base, BoundAsNewLocalDeclarations)
                    Dim localSymbols = asNewDeclarations.LocalDeclarations.SelectAsArray(Of ILocalSymbol)(Function(declaration) declaration.LocalSymbol)
                    builder.Add(OperationFactory.CreateVariableDeclaration(localSymbols, Create(asNewDeclarations.Initializer), asNewDeclarations.Syntax))
                End If
            Next

            Return builder.ToImmutableAndFree()
        End Function

        Private Function GetUsingStatementDeclaration(boundUsing As BoundUsingStatement) As IVariableDeclarationStatement
            If boundUsing.ResourceList.IsDefault Then
                Return Nothing
            End If
            Dim declaration = boundUsing.ResourceList.Select(Function(n) Create(n)).OfType(Of IVariableDeclaration).ToImmutableArray()
            Dim syntax = DirectCast(boundUsing.Syntax, UsingBlockSyntax).UsingStatement
            Return New VariableDeclarationStatement(
                            declaration,
                            syntax:=syntax,
                            type:=Nothing,
                            constantValue:=Nothing)
        End Function

        Private Function GetAddHandlerStatementExpression(statement As BoundAddHandlerStatement) As IOperation
            Dim eventAccess As BoundEventAccess = TryCast(statement.EventAccess, BoundEventAccess)
            Dim [event] As IEventSymbol = eventAccess?.EventSymbol
            Dim instance = If([event] Is Nothing OrElse [event].IsStatic, Nothing, If(eventAccess IsNot Nothing, Create(eventAccess.ReceiverOpt), Nothing))

            Return New EventAssignmentExpression(
                        [event], instance, Create(statement.Handler), adds:=True, syntax:=statement.Syntax, type:=Nothing, constantValue:=Nothing)
        End Function

        Private Function GetRemoveStatementExpression(statement As BoundRemoveHandlerStatement) As IOperation
            Dim eventAccess As BoundEventAccess = TryCast(statement.EventAccess, BoundEventAccess)
            Dim [event] As IEventSymbol = eventAccess?.EventSymbol
            Dim instance = If([event] Is Nothing OrElse [event].IsStatic, Nothing, If(eventAccess IsNot Nothing, Create(eventAccess.ReceiverOpt), Nothing))

            Return New EventAssignmentExpression(
                [event], instance, Create(statement.Handler), adds:=False, syntax:=statement.Syntax, type:=Nothing, constantValue:=Nothing)
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
            Friend Shared Function DeriveUnaryOperationKind(operatorKind As UnaryOperatorKind) As UnaryOperationKind
                Select Case operatorKind And UnaryOperatorKind.OpMask
                    Case UnaryOperatorKind.Plus
                        Return UnaryOperationKind.OperatorMethodPlus
                    Case UnaryOperatorKind.Minus
                        Return UnaryOperationKind.OperatorMethodMinus
                    Case UnaryOperatorKind.Not
                        Return UnaryOperationKind.OperatorMethodBitwiseNegation
                    Case UnaryOperatorKind.IsTrue
                        Return UnaryOperationKind.OperatorMethodTrue
                    Case UnaryOperatorKind.IsFalse
                        Return UnaryOperationKind.OperatorMethodFalse
                    Case Else
                        Return UnaryOperationKind.Invalid
                End Select
            End Function

            Friend Shared Function DeriveUnaryOperationKind(operatorKind As UnaryOperatorKind, operand As BoundExpression) As UnaryOperationKind
                Dim type = operand.Type.GetNullableUnderlyingTypeOrSelf()
                Select Case type.SpecialType
                    Case SpecialType.System_Byte, SpecialType.System_Int16, SpecialType.System_Int32, SpecialType.System_Int64, SpecialType.System_SByte, SpecialType.System_UInt16, SpecialType.System_UInt32, SpecialType.System_UInt64
                        Select Case operatorKind And UnaryOperatorKind.OpMask
                            Case UnaryOperatorKind.Plus
                                Return UnaryOperationKind.IntegerPlus
                            Case UnaryOperatorKind.Minus
                                Return UnaryOperationKind.IntegerMinus
                            Case UnaryOperatorKind.Not
                                Return UnaryOperationKind.IntegerBitwiseNegation
                        End Select
                    Case SpecialType.System_Single, SpecialType.System_Double
                        Select Case operatorKind And UnaryOperatorKind.OpMask
                            Case UnaryOperatorKind.Plus
                                Return UnaryOperationKind.FloatingPlus
                            Case UnaryOperatorKind.Minus
                                Return UnaryOperationKind.FloatingMinus
                        End Select
                    Case SpecialType.System_Decimal
                        Select Case operatorKind And UnaryOperatorKind.OpMask
                            Case UnaryOperatorKind.Plus
                                Return UnaryOperationKind.DecimalPlus
                            Case UnaryOperatorKind.Minus
                                Return UnaryOperationKind.DecimalMinus
                        End Select
                    Case SpecialType.System_Boolean
                        Select Case operatorKind And UnaryOperatorKind.OpMask
                            Case UnaryOperatorKind.Not
                                Return UnaryOperationKind.BooleanBitwiseNegation
                        End Select
                    Case SpecialType.System_Object
                        Select Case operatorKind And UnaryOperatorKind.OpMask
                            Case UnaryOperatorKind.Plus
                                Return UnaryOperationKind.ObjectPlus
                            Case UnaryOperatorKind.Minus
                                Return UnaryOperationKind.ObjectMinus
                            Case UnaryOperatorKind.Not
                                Return UnaryOperationKind.ObjectNot
                        End Select
                End Select

                Return UnaryOperationKind.Invalid
            End Function

            Friend Shared Function DeriveBinaryOperationKind(operatorKind As BinaryOperatorKind) As BinaryOperationKind
                Select Case operatorKind And BinaryOperatorKind.OpMask
                    Case BinaryOperatorKind.Add
                        Return BinaryOperationKind.OperatorMethodAdd
                    Case BinaryOperatorKind.Subtract
                        Return BinaryOperationKind.OperatorMethodSubtract
                    Case BinaryOperatorKind.Multiply
                        Return BinaryOperationKind.OperatorMethodMultiply
                    Case BinaryOperatorKind.Divide
                        Return BinaryOperationKind.OperatorMethodDivide
                    Case BinaryOperatorKind.IntegerDivide
                        Return BinaryOperationKind.OperatorMethodIntegerDivide
                    Case BinaryOperatorKind.Modulo
                        Return BinaryOperationKind.OperatorMethodRemainder
                    Case BinaryOperatorKind.And
                        Return BinaryOperationKind.OperatorMethodAnd
                    Case BinaryOperatorKind.Or
                        Return BinaryOperationKind.OperatorMethodOr
                    Case BinaryOperatorKind.Xor
                        Return BinaryOperationKind.OperatorMethodExclusiveOr
                    Case BinaryOperatorKind.AndAlso
                        Return BinaryOperationKind.OperatorMethodConditionalAnd
                    Case BinaryOperatorKind.OrElse
                        Return BinaryOperationKind.OperatorMethodConditionalOr
                    Case BinaryOperatorKind.LeftShift
                        Return BinaryOperationKind.OperatorMethodLeftShift
                    Case BinaryOperatorKind.RightShift
                        Return BinaryOperationKind.OperatorMethodRightShift
                    Case BinaryOperatorKind.LessThan
                        Return BinaryOperationKind.OperatorMethodLessThan
                    Case BinaryOperatorKind.LessThanOrEqual
                        Return BinaryOperationKind.OperatorMethodLessThanOrEqual
                    Case BinaryOperatorKind.Equals
                        Return BinaryOperationKind.OperatorMethodEquals
                    Case BinaryOperatorKind.NotEquals
                        Return BinaryOperationKind.OperatorMethodNotEquals
                    Case BinaryOperatorKind.GreaterThanOrEqual
                        Return BinaryOperationKind.OperatorMethodGreaterThanOrEqual
                    Case BinaryOperatorKind.GreaterThan
                        Return BinaryOperationKind.OperatorMethodGreaterThan
                    Case BinaryOperatorKind.Power
                        Return BinaryOperationKind.OperatorMethodPower
                    Case Else
                        Return BinaryOperationKind.Invalid
                End Select
            End Function

            Friend Shared Function DeriveBinaryOperationKind(operatorKind As BinaryOperatorKind, left As BoundExpression) As BinaryOperationKind
                Dim type = left.Type.GetNullableUnderlyingTypeOrSelf()
                Select Case type.SpecialType

                    Case SpecialType.System_SByte, SpecialType.System_Int16, SpecialType.System_Int32, SpecialType.System_Int64
                        Select Case operatorKind And BinaryOperatorKind.OpMask
                            Case BinaryOperatorKind.Add
                                Return BinaryOperationKind.IntegerAdd
                            Case BinaryOperatorKind.Subtract
                                Return BinaryOperationKind.IntegerSubtract
                            Case BinaryOperatorKind.Multiply
                                Return BinaryOperationKind.IntegerMultiply
                            Case BinaryOperatorKind.IntegerDivide
                                Return BinaryOperationKind.IntegerDivide
                            Case BinaryOperatorKind.Modulo
                                Return BinaryOperationKind.IntegerRemainder
                            Case BinaryOperatorKind.And
                                Return BinaryOperationKind.IntegerAnd
                            Case BinaryOperatorKind.Or
                                Return BinaryOperationKind.IntegerOr
                            Case BinaryOperatorKind.Xor
                                Return BinaryOperationKind.IntegerExclusiveOr
                            Case BinaryOperatorKind.LeftShift
                                Return BinaryOperationKind.IntegerLeftShift
                            Case BinaryOperatorKind.RightShift
                                Return BinaryOperationKind.IntegerRightShift
                            Case BinaryOperatorKind.LessThan
                                Return BinaryOperationKind.IntegerLessThan
                            Case BinaryOperatorKind.LessThanOrEqual
                                Return BinaryOperationKind.IntegerLessThanOrEqual
                            Case BinaryOperatorKind.Equals
                                Return BinaryOperationKind.IntegerEquals
                            Case BinaryOperatorKind.NotEquals
                                Return BinaryOperationKind.IntegerNotEquals
                            Case BinaryOperatorKind.GreaterThanOrEqual
                                Return BinaryOperationKind.IntegerGreaterThanOrEqual
                            Case BinaryOperatorKind.GreaterThan
                                Return BinaryOperationKind.IntegerGreaterThan
                        End Select
                    Case SpecialType.System_Byte, SpecialType.System_UInt16, SpecialType.System_UInt32, SpecialType.System_UInt64, SpecialType.System_Char
                        Select Case operatorKind And BinaryOperatorKind.OpMask
                            Case BinaryOperatorKind.Add
                                Return BinaryOperationKind.UnsignedAdd
                            Case BinaryOperatorKind.Subtract
                                Return BinaryOperationKind.UnsignedSubtract
                            Case BinaryOperatorKind.Multiply
                                Return BinaryOperationKind.UnsignedMultiply
                            Case BinaryOperatorKind.IntegerDivide
                                Return BinaryOperationKind.UnsignedDivide
                            Case BinaryOperatorKind.Modulo
                                Return BinaryOperationKind.UnsignedRemainder
                            Case BinaryOperatorKind.And
                                Return BinaryOperationKind.UnsignedAnd
                            Case BinaryOperatorKind.Or
                                Return BinaryOperationKind.UnsignedOr
                            Case BinaryOperatorKind.Xor
                                Return BinaryOperationKind.UnsignedExclusiveOr
                            Case BinaryOperatorKind.LeftShift
                                Return BinaryOperationKind.UnsignedLeftShift
                            Case BinaryOperatorKind.RightShift
                                Return BinaryOperationKind.UnsignedRightShift
                            Case BinaryOperatorKind.LessThan
                                Return BinaryOperationKind.UnsignedLessThan
                            Case BinaryOperatorKind.LessThanOrEqual
                                Return BinaryOperationKind.UnsignedLessThanOrEqual
                            Case BinaryOperatorKind.Equals
                                Return BinaryOperationKind.IntegerEquals
                            Case BinaryOperatorKind.NotEquals
                                Return BinaryOperationKind.IntegerNotEquals
                            Case BinaryOperatorKind.GreaterThanOrEqual
                                Return BinaryOperationKind.UnsignedGreaterThanOrEqual
                            Case BinaryOperatorKind.GreaterThan
                                Return BinaryOperationKind.UnsignedGreaterThan
                        End Select
                    Case SpecialType.System_Single, SpecialType.System_Double
                        Select Case operatorKind And BinaryOperatorKind.OpMask
                            Case BinaryOperatorKind.Add
                                Return BinaryOperationKind.FloatingAdd
                            Case BinaryOperatorKind.Subtract
                                Return BinaryOperationKind.FloatingSubtract
                            Case BinaryOperatorKind.Multiply
                                Return BinaryOperationKind.FloatingMultiply
                            Case BinaryOperatorKind.Divide
                                Return BinaryOperationKind.FloatingDivide
                            Case BinaryOperatorKind.Modulo
                                Return BinaryOperationKind.FloatingRemainder
                            Case BinaryOperatorKind.Power
                                Return BinaryOperationKind.FloatingPower
                            Case BinaryOperatorKind.LessThan
                                Return BinaryOperationKind.FloatingLessThan
                            Case BinaryOperatorKind.LessThanOrEqual
                                Return BinaryOperationKind.FloatingLessThanOrEqual
                            Case BinaryOperatorKind.Equals
                                Return BinaryOperationKind.FloatingEquals
                            Case BinaryOperatorKind.NotEquals
                                Return BinaryOperationKind.FloatingNotEquals
                            Case BinaryOperatorKind.GreaterThanOrEqual
                                Return BinaryOperationKind.FloatingGreaterThanOrEqual
                            Case BinaryOperatorKind.GreaterThan
                                Return BinaryOperationKind.FloatingGreaterThan
                        End Select
                    Case SpecialType.System_Decimal
                        Select Case operatorKind And BinaryOperatorKind.OpMask
                            Case BinaryOperatorKind.Add
                                Return BinaryOperationKind.DecimalAdd
                            Case BinaryOperatorKind.Subtract
                                Return BinaryOperationKind.DecimalSubtract
                            Case BinaryOperatorKind.Multiply
                                Return BinaryOperationKind.DecimalMultiply
                            Case BinaryOperatorKind.Divide
                                Return BinaryOperationKind.DecimalDivide
                            Case BinaryOperatorKind.LessThan
                                Return BinaryOperationKind.DecimalLessThan
                            Case BinaryOperatorKind.LessThanOrEqual
                                Return BinaryOperationKind.DecimalLessThanOrEqual
                            Case BinaryOperatorKind.Equals
                                Return BinaryOperationKind.DecimalEquals
                            Case BinaryOperatorKind.NotEquals
                                Return BinaryOperationKind.DecimalNotEquals
                            Case BinaryOperatorKind.GreaterThanOrEqual
                                Return BinaryOperationKind.DecimalGreaterThanOrEqual
                            Case BinaryOperatorKind.GreaterThan
                                Return BinaryOperationKind.DecimalGreaterThan
                        End Select
                    Case SpecialType.System_Boolean
                        Select Case operatorKind And BinaryOperatorKind.OpMask
                            Case BinaryOperatorKind.And
                                Return BinaryOperationKind.BooleanAnd
                            Case BinaryOperatorKind.Or
                                Return BinaryOperationKind.BooleanOr
                            Case BinaryOperatorKind.Xor
                                Return BinaryOperationKind.BooleanExclusiveOr
                            Case BinaryOperatorKind.AndAlso
                                Return BinaryOperationKind.BooleanConditionalAnd
                            Case BinaryOperatorKind.OrElse
                                Return BinaryOperationKind.BooleanConditionalOr
                            Case BinaryOperatorKind.Equals
                                Return BinaryOperationKind.BooleanEquals
                            Case BinaryOperatorKind.NotEquals
                                Return BinaryOperationKind.BooleanNotEquals
                        End Select
                    Case SpecialType.System_String
                        Select Case operatorKind And BinaryOperatorKind.OpMask
                            Case BinaryOperatorKind.Concatenate
                                Return BinaryOperationKind.StringConcatenate
                            Case BinaryOperatorKind.Equals
                                Return BinaryOperationKind.StringEquals
                            Case BinaryOperatorKind.NotEquals
                                Return BinaryOperationKind.StringNotEquals
                            Case BinaryOperatorKind.Like
                                Return BinaryOperationKind.StringLike
                        End Select
                    Case SpecialType.System_Object
                        Select Case operatorKind And BinaryOperatorKind.OpMask
                            Case BinaryOperatorKind.Add
                                Return BinaryOperationKind.ObjectAdd
                            Case BinaryOperatorKind.Subtract
                                Return BinaryOperationKind.ObjectSubtract
                            Case BinaryOperatorKind.Multiply
                                Return BinaryOperationKind.ObjectMultiply
                            Case BinaryOperatorKind.Power
                                Return BinaryOperationKind.ObjectPower
                            Case BinaryOperatorKind.IntegerDivide
                                Return BinaryOperationKind.ObjectIntegerDivide
                            Case BinaryOperatorKind.Divide
                                Return BinaryOperationKind.ObjectDivide
                            Case BinaryOperatorKind.Modulo
                                Return BinaryOperationKind.ObjectRemainder
                            Case BinaryOperatorKind.Concatenate
                                Return BinaryOperationKind.ObjectConcatenate
                            Case BinaryOperatorKind.And
                                Return BinaryOperationKind.ObjectAnd
                            Case BinaryOperatorKind.Or
                                Return BinaryOperationKind.ObjectOr
                            Case BinaryOperatorKind.Xor
                                Return BinaryOperationKind.ObjectExclusiveOr
                            Case BinaryOperatorKind.AndAlso
                                Return BinaryOperationKind.ObjectConditionalAnd
                            Case BinaryOperatorKind.OrElse
                                Return BinaryOperationKind.ObjectConditionalOr
                            Case BinaryOperatorKind.LeftShift
                                Return BinaryOperationKind.ObjectLeftShift
                            Case BinaryOperatorKind.RightShift
                                Return BinaryOperationKind.ObjectRightShift
                            Case BinaryOperatorKind.LessThan
                                Return BinaryOperationKind.ObjectLessThan
                            Case BinaryOperatorKind.LessThanOrEqual
                                Return BinaryOperationKind.ObjectLessThanOrEqual
                            Case BinaryOperatorKind.Equals
                                Return BinaryOperationKind.ObjectVBEquals
                            Case BinaryOperatorKind.Is
                                Return BinaryOperationKind.ObjectEquals
                            Case BinaryOperatorKind.IsNot
                                Return BinaryOperationKind.ObjectNotEquals
                            Case BinaryOperatorKind.NotEquals
                                Return BinaryOperationKind.ObjectVBNotEquals
                            Case BinaryOperatorKind.Like
                                Return BinaryOperationKind.ObjectLike
                            Case BinaryOperatorKind.GreaterThanOrEqual
                                Return BinaryOperationKind.ObjectGreaterThanOrEqual
                            Case BinaryOperatorKind.GreaterThan
                                Return BinaryOperationKind.ObjectGreaterThan
                        End Select
                End Select

                If type.TypeKind = TypeKind.Enum Then
                    Select Case operatorKind And BinaryOperatorKind.OpMask
                        Case BinaryOperatorKind.Add
                            Return BinaryOperationKind.EnumAdd
                        Case BinaryOperatorKind.Subtract
                            Return BinaryOperationKind.EnumSubtract
                        Case BinaryOperatorKind.And
                            Return BinaryOperationKind.EnumAnd
                        Case BinaryOperatorKind.Or
                            Return BinaryOperationKind.EnumOr
                        Case BinaryOperatorKind.Xor
                            Return BinaryOperationKind.EnumExclusiveOr
                        Case BinaryOperatorKind.LessThan
                            Return BinaryOperationKind.EnumLessThan
                        Case BinaryOperatorKind.LessThanOrEqual
                            Return BinaryOperationKind.EnumLessThanOrEqual
                        Case BinaryOperatorKind.Equals
                            Return BinaryOperationKind.EnumEquals
                        Case BinaryOperatorKind.NotEquals
                            Return BinaryOperationKind.EnumNotEquals
                        Case BinaryOperatorKind.GreaterThanOrEqual
                            Return BinaryOperationKind.EnumGreaterThanOrEqual
                        Case BinaryOperatorKind.GreaterThan
                            Return BinaryOperationKind.EnumGreaterThan
                    End Select
                End If

                Return BinaryOperationKind.Invalid
            End Function
        End Class
    End Class
End Namespace
