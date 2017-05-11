' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Immutable
Imports System.Runtime.CompilerServices
Imports Microsoft.CodeAnalysis.VisualBasic

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
                        Dim leftOperand = GetUserDefinedBinaryOperatorChild(rightOperatorBinary, 0)
                        If leftOperand Is value.LeftOnTheRightOpt Then
                            Return OperationKind.CompoundAssignmentExpression
                        End If
                End Select
            End If

            Return OperationKind.AssignmentExpression
        End Function

        Private Shared Function GetUserDefinedBinaryOperatorChild([operator] As BoundUserDefinedBinaryOperator, index As Integer) As IOperation
            If [operator].UnderlyingExpression.Kind = BoundKind.Call Then
                If index = 0 Then
                    Return Create([operator].Left)
                ElseIf index = 1 Then
                    Return Create([operator].Right)
                Else
                    Throw ExceptionUtilities.UnexpectedValue(index)
                End If
            Else
                Return GetChildOfBadExpression([operator].UnderlyingExpression, index)
            End If
        End Function

        Friend Shared Function DeriveArguments(boundArguments As ImmutableArray(Of BoundExpression), parameters As ImmutableArray(Of VisualBasic.Symbols.ParameterSymbol)) As ImmutableArray(Of IArgument)
            Dim argumentsLength As Integer = boundArguments.Length
            Debug.Assert(argumentsLength = parameters.Length)

            Dim arguments As ArrayBuilder(Of IArgument) = ArrayBuilder(Of IArgument).GetInstance(argumentsLength)
            For index As Integer = 0 To argumentsLength - 1 Step 1
                arguments.Add(DeriveArgument(index, boundArguments(index), parameters))
            Next

            Return arguments.ToImmutableAndFree()
        End Function

        Private Shared ReadOnly s_argumentMappings As New System.Runtime.CompilerServices.ConditionalWeakTable(Of BoundExpression, IArgument)

        Private Shared Function DeriveArgument(index As Integer, argument As BoundExpression, parameters As ImmutableArray(Of VisualBasic.Symbols.ParameterSymbol)) As IArgument
            Select Case argument.Kind
                Case BoundKind.ByRefArgumentWithCopyBack
                    Return s_argumentMappings.GetValue(
                        argument,
                        Function(argumentValue)
                            Dim byRefArgument = DirectCast(argumentValue, BoundByRefArgumentWithCopyBack)
                            Dim parameter = parameters(index)
                            Dim value = Create(byRefArgument.OriginalArgument)
                            Return New Argument(
                                ArgumentKind.Explicit,
                                parameter,
                                value,
                                Create(byRefArgument.InConversion),
                                Create(byRefArgument.OutConversion),
                                parameter Is Nothing OrElse value.IsInvalid,
                                value.Syntax,
                                type:=Nothing,
                                constantValue:=Nothing)
                        End Function)
                Case Else
                    Return s_argumentMappings.GetValue(
                        argument,
                        Function(argumentValue)
                            Dim lastParameterIndex = parameters.Length - 1
                            If index = lastParameterIndex AndAlso ParameterIsParamArray(parameters(lastParameterIndex)) Then
                                ' TODO: figure out if this is true:
                                '       a compiler generated argument for a ParamArray parameter is created iff 
                                '       a list of arguments (including 0 argument) is provided for ParamArray parameter in source
                                '       https://github.com/dotnet/roslyn/issues/18550
                                Dim kind = If(argumentValue.WasCompilerGenerated AndAlso argumentValue.Kind = BoundKind.ArrayCreation, ArgumentKind.ParamArray, ArgumentKind.Explicit)
                                Dim parameter = parameters(lastParameterIndex)
                                Dim value = Create(argumentValue)

                                Return New Argument(
                                    kind,
                                    parameter,
                                    value,
                                    inConversion:=Nothing,
                                    outConversion:=Nothing,
                                    isInvalid:=parameter Is Nothing OrElse value.IsInvalid,
                                    syntax:=value.Syntax,
                                    type:=Nothing,
                                    constantValue:=Nothing)
                            Else
                                ' TODO: figure our if this is true:
                                '       a compiler generated argument for an Optional parameter is created iff
                                '       the argument is omitted from the source
                                '       https://github.com/dotnet/roslyn/issues/18550
                                Dim kind = If(argumentValue.WasCompilerGenerated, ArgumentKind.DefaultValue, ArgumentKind.Explicit)
                                Dim parameter = parameters(index)
                                Dim value = Create(argumentValue)

                                Return New Argument(
                                    kind,
                                    parameter,
                                    value,
                                    inConversion:=Nothing,
                                    outConversion:=Nothing,
                                    isInvalid:=parameter Is Nothing OrElse value.IsInvalid,
                                    syntax:=value.Syntax,
                                    type:=Nothing,
                                    constantValue:=Nothing)
                            End If
                        End Function)
            End Select
        End Function

        Private Shared Function ParameterIsParamArray(parameter As VisualBasic.Symbols.ParameterSymbol) As Boolean
            Return If(parameter.IsParamArray AndAlso parameter.Type.Kind = SymbolKind.ArrayType, DirectCast(parameter.Type, VisualBasic.Symbols.ArrayTypeSymbol).IsSZArray, False)
        End Function

        Private Shared Function GetChildOfBadExpression(parent As BoundNode, index As Integer) As IOperation
            Dim badParent As BoundBadExpression = TryCast(parent, BoundBadExpression)
            If badParent?.ChildBoundNodes.Length > index Then
                Dim child As IOperation = Create(badParent.ChildBoundNodes(index))
                If child IsNot Nothing Then
                    Return child
                End If
            End If

            Return OperationFactory.CreateInvalidExpression(parent.Syntax, ImmutableArray(Of IOperation).Empty)
        End Function

        Private Shared ReadOnly s_memberInitializersMappings As New ConditionalWeakTable(Of BoundObjectCreationExpression, Object)

        Private Shared Function GetObjectCreationMemberInitializers(expression As BoundObjectCreationExpression) As ImmutableArray(Of ISymbolInitializer)
            Dim initializer = s_memberInitializersMappings.GetValue(expression,
                Function(objectCreationStatement)
                    Dim objectInitializerExpression As BoundObjectInitializerExpressionBase = objectCreationStatement.InitializerOpt
                    If objectInitializerExpression IsNot Nothing Then
                        Dim builder = ArrayBuilder(Of ISymbolInitializer).GetInstance(objectInitializerExpression.Initializers.Length)
                        For Each memberAssignment In objectInitializerExpression.Initializers
                            Dim assignment = TryCast(memberAssignment, BoundAssignmentOperator)
                            Dim left = assignment?.Left
                            If left Is Nothing Then
                                Continue For
                            End If

                            Select Case left.Kind
                                Case BoundKind.FieldAccess
                                    Dim field = DirectCast(left, BoundFieldAccess).FieldSymbol
                                    Dim value = Create(assignment.Right)
                                    builder.Add(New FieldInitializer(
                                        ImmutableArray.Create(Of IFieldSymbol)(field),
                                        value,
                                        OperationKind.FieldInitializerInCreation,
                                        field Is Nothing OrElse value.IsInvalid,
                                        assignment.Syntax,
                                        type:=Nothing,
                                        constantValue:=Nothing))
                                Case BoundKind.PropertyAccess
                                    Dim [property] = DirectCast(left, BoundPropertyAccess).PropertySymbol
                                    Dim value = Create(assignment.Right)
                                    builder.Add(New PropertyInitializer(
                                        [property],
                                        value,
                                        OperationKind.PropertyInitializerInCreation,
                                        [property] Is Nothing OrElse [property].SetMethod Is Nothing OrElse value.IsInvalid,
                                        assignment.Syntax,
                                        type:=Nothing,
                                        constantValue:=Nothing))
                            End Select
                        Next
                        Return builder.ToImmutableAndFree()
                    End If

                    Return ImmutableArray(Of ISymbolInitializer).Empty
                End Function)

            Return DirectCast(initializer, ImmutableArray(Of ISymbolInitializer))
        End Function

        Private Shared ReadOnly s_caseBlocksMappings As New ConditionalWeakTable(Of BoundSelectStatement, Object)

        Private Shared Function GetSwitchStatementCases(statement As BoundSelectStatement) As ImmutableArray(Of ISwitchCase)
            Dim cases = s_caseBlocksMappings.GetValue(statement,
                Function(boundSelect)
                    Return boundSelect.CaseBlocks.SelectAsArray(
                        Function(boundCaseBlock)
                            ' `CaseElseClauseSyntax` is bound to `BoundCaseStatement` with an empty list of case clauses, 
                            ' so we explicitly create an IOperation node for Case-Else clause to differentiate it from Case clause.
                            Dim clauses As ImmutableArray(Of ICaseClause)
                            Dim caseStatement = boundCaseBlock.CaseStatement
                            If caseStatement.CaseClauses.IsEmpty AndAlso caseStatement.Syntax.Kind() = SyntaxKind.CaseElseStatement Then
                                clauses = ImmutableArray.Create(Of ICaseClause)(
                                                                            New SingleValueCaseClause(
                                                                                value:=Nothing,
                                                                                equality:=BinaryOperationKind.None,
                                                                                caseKind:=CaseKind.Default,
                                                                                isInvalid:=caseStatement.HasErrors,
                                                                                syntax:=caseStatement.Syntax,
                                                                                type:=Nothing,
                                                                                constantValue:=Nothing))
                            Else
                                clauses = caseStatement.CaseClauses.SelectAsArray(Function(n) DirectCast(Create(n), ICaseClause))
                            End If

                            Dim body = ImmutableArray.Create(Create(boundCaseBlock.Body))
                            Dim isInvalid = boundCaseBlock.HasErrors
                            Dim syntax = boundCaseBlock.Syntax
                            Return DirectCast(New SwitchCase(clauses, body, isInvalid, syntax, type:=Nothing, constantValue:=Nothing), ISwitchCase)
                        End Function)
                End Function)
            Return DirectCast(cases, ImmutableArray(Of ISwitchCase))
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

        Private Shared ReadOnly s_loopTopMappings As New ConditionalWeakTable(Of BoundForToStatement, Object)

        Private Shared Function GetForLoopStatementBefore(boundForToStatement As BoundForToStatement) As ImmutableArray(Of IOperation)
            Dim result = s_loopTopMappings.GetValue(
                    boundForToStatement,
                    Function(boundFor)
                        Dim statements As ArrayBuilder(Of IOperation) = ArrayBuilder(Of IOperation).GetInstance()

                        ' ControlVariable = InitialValue
                        Dim controlReference As IOperation = Create(boundFor.ControlVariable)
                        If controlReference IsNot Nothing Then
                            statements.Add(OperationFactory.CreateAssignmentExpressionStatement(controlReference, Create(boundFor.InitialValue), boundFor.InitialValue.Syntax))
                        End If

                        ' T0 = LimitValue
                        If Not boundFor.LimitValue.IsConstant Then
                            Dim value = Create(boundFor.LimitValue)
                            statements.Add(
                                OperationFactory.CreateAssignmentExpressionStatement(
                                    New SyntheticLocalReferenceExpression(
                                            SyntheticLocalKind.ForLoopLimitValue,
                                            Create(boundFor),
                                            isInvalid:=False,
                                            syntax:=value.Syntax,
                                            type:=value.Type,
                                            constantValue:=Nothing), value, value.Syntax))
                        End If

                        ' T1 = StepValue
                        If boundFor.StepValue IsNot Nothing AndAlso Not boundFor.StepValue.IsConstant Then
                            Dim value = Create(boundFor.StepValue)
                            statements.Add(
                                OperationFactory.CreateAssignmentExpressionStatement(
                                    New SyntheticLocalReferenceExpression(
                                        SyntheticLocalKind.ForLoopStepValue,
                                        Create(boundFor),
                                        isInvalid:=False,
                                        syntax:=value.Syntax,
                                        type:=value.Type,
                                        constantValue:=Nothing), value, value.Syntax))
                        End If

                        Return statements.ToImmutableAndFree()
                    End Function)

            Return DirectCast(result, ImmutableArray(Of IOperation))
        End Function

        Private Shared ReadOnly s_loopBottomMappings As New ConditionalWeakTable(Of BoundForToStatement, Object)

        Private Shared Function GetForLoopStatementAtLoopBottom(boundForToStatement As BoundForToStatement) As ImmutableArray(Of IOperation)
            Dim result = s_loopBottomMappings.GetValue(
                    boundForToStatement,
                    Function(boundFor)
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
                                            isInvalid:=False,
                                            syntax:=value.Syntax,
                                            type:=value.Type,
                                            constantValue:=Nothing))
                                statements.Add(OperationFactory.CreateCompoundAssignmentExpressionStatement(controlReference, stepOperand, Semantics.Expression.DeriveAdditionKind(controlType), Nothing, stepValue.Syntax))
                            End If
                        End If

                        Return statements.ToImmutableAndFree()
                    End Function)

            Return DirectCast(result, ImmutableArray(Of IOperation))
        End Function

        Private Shared ReadOnly s_loopConditionMappings As New System.Runtime.CompilerServices.ConditionalWeakTable(Of BoundForToStatement, IOperation)

        Private Shared Function GetForWhileUntilLoopStatmentCondition(boundForToStatement As BoundForToStatement) As IOperation
            Return s_loopConditionMappings.GetValue(
                boundForToStatement,
                Function(boundFor)
                    Dim operationValue = Create(boundFor.LimitValue)
                    Dim limitValue As IOperation =
                        If(boundFor.LimitValue.IsConstant,
                            operationValue,
                            New SyntheticLocalReferenceExpression(
                                SyntheticLocalKind.ForLoopLimitValue,
                                Create(boundFor),
                                isInvalid:=False,
                                syntax:=operationValue.Syntax,
                                type:=operationValue.Type,
                                constantValue:=Nothing))

                    Dim controlVariable As BoundExpression = boundFor.ControlVariable

                    ' controlVariable can be a BoundBadExpression in case of error
                    Dim booleanType As ITypeSymbol = controlVariable.ExpressionSymbol?.DeclaringCompilation.GetSpecialType(SpecialType.System_Boolean)

                    Dim operators As BoundForToUserDefinedOperators = boundForToStatement.OperatorsOpt
                    If operators IsNot Nothing Then
                        ' Use the operator methods. Figure out the precise rules first.
                        Return Nothing
                    Else
                        If boundFor.StepValue Is Nothing OrElse (boundFor.StepValue.IsConstant AndAlso boundFor.StepValue.ConstantValueOpt IsNot Nothing) Then
                            ' Either ControlVariable <= LimitValue or ControlVariable >= LimitValue, depending on whether the step value is negative.

                            Dim relationalCode As BinaryOperationKind = Helper.DeriveBinaryOperationKind(If(boundFor.StepValue IsNot Nothing AndAlso boundFor.StepValue.ConstantValueOpt.IsNegativeNumeric, BinaryOperatorKind.GreaterThanOrEqual, BinaryOperatorKind.LessThanOrEqual), controlVariable)
                            Return OperationFactory.CreateBinaryOperatorExpression(relationalCode, Create(controlVariable), limitValue, booleanType, limitValue.Syntax)
                        Else
                            ' If(StepValue >= 0, ControlVariable <= LimitValue, ControlVariable >= LimitValue)

                            Dim value = Create(boundFor.StepValue)
                            Dim stepValue As IOperation = New SyntheticLocalReferenceExpression(
                                SyntheticLocalKind.ForLoopStepValue,
                                Create(boundFor),
                                isInvalid:=False,
                                syntax:=value.Syntax,
                                type:=value.Type,
                                constantValue:=Nothing)

                            Dim stepRelationalCode As BinaryOperationKind = Helper.DeriveBinaryOperationKind(BinaryOperatorKind.GreaterThanOrEqual, boundFor.StepValue)
                            Dim stepCondition As IOperation = OperationFactory.CreateBinaryOperatorExpression(stepRelationalCode,
                                 stepValue,
                                 OperationFactory.CreateLiteralExpression(Semantics.Expression.SynthesizeNumeric(stepValue.Type, 0), boundFor.StepValue.Type, boundFor.StepValue.Syntax),
                                 booleanType,
                                 boundFor.StepValue.Syntax)

                            Dim positiveStepRelationalCode As BinaryOperationKind = Helper.DeriveBinaryOperationKind(BinaryOperatorKind.LessThanOrEqual, controlVariable)
                            Dim positiveStepCondition As IOperation = OperationFactory.CreateBinaryOperatorExpression(positiveStepRelationalCode, Create(controlVariable), limitValue, booleanType, limitValue.Syntax)

                            Dim negativeStepRelationalCode As BinaryOperationKind = Helper.DeriveBinaryOperationKind(BinaryOperatorKind.GreaterThanOrEqual, controlVariable)
                            Dim negativeStepCondition As IOperation = OperationFactory.CreateBinaryOperatorExpression(negativeStepRelationalCode, Create(controlVariable), limitValue, booleanType, limitValue.Syntax)

                            Return OperationFactory.CreateConditionalChoiceExpression(stepCondition, positiveStepCondition, negativeStepCondition, booleanType, limitValue.Syntax)
                        End If
                    End If
                End Function)
        End Function

        Private Shared ReadOnly s_blockStatementsMappings As New System.Runtime.CompilerServices.ConditionalWeakTable(Of BoundBlock, Object)

        Private Shared Function GetBlockStatementStatements(block As BoundBlock) As ImmutableArray(Of IOperation)
            ' This is to filter out operations of kind None.
            Dim statements = s_blockStatementsMappings.GetValue(
                block,
                Function(boundBlock)
                    Return boundBlock.Statements.Select(Function(n) Create(n)).Where(Function(s) s.Kind <> OperationKind.None).ToImmutableArray()
                End Function)
            Return DirectCast(statements, ImmutableArray(Of IOperation))
        End Function

        Private Shared ReadOnly s_variablesMappings As New ConditionalWeakTable(Of BoundDimStatement, Object)

        Private Shared Function GetVariableDeclarationStatementVariables(statement As BoundDimStatement) As ImmutableArray(Of IVariableDeclaration)
            Dim variables = s_variablesMappings.GetValue(
                statement,
                Function(dimStatement)
                    Dim builder = ArrayBuilder(Of IVariableDeclaration).GetInstance()
                    For Each base In dimStatement.LocalDeclarations
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
                                                               )
            Return DirectCast(variables, ImmutableArray(Of IVariableDeclaration))
        End Function

        Private Shared ReadOnly s_variablesDeclMappings As New ConditionalWeakTable(Of BoundUsingStatement, VariableDeclarationStatement)

        Private Shared Function GetUsingStatementDeclaration(boundUsingStatement As BoundUsingStatement) As IVariableDeclarationStatement
            Return s_variablesDeclMappings.GetValue(
                    boundUsingStatement,
                    Function(boundUsing)
                        Return New VariableDeclarationStatement(
                            boundUsing.ResourceList.SelectAsArray(Function(n) DirectCast(Create(n), IVariableDeclaration)),
                            isInvalid:=False,
                            syntax:=Nothing,
                            type:=Nothing,
                            constantValue:=Nothing)
                    End Function)
        End Function

        Private Shared ReadOnly s_expressionsMappings As New ConditionalWeakTable(Of BoundAddRemoveHandlerStatement, IEventAssignmentExpression)


        Private Shared Function GetAddHandlerStatementExpression(handlerStatement As BoundAddHandlerStatement) As IOperation
            Return s_expressionsMappings.GetValue(
                handlerStatement,
                Function(statement)
                    Dim eventAccess As BoundEventAccess = TryCast(statement.EventAccess, BoundEventAccess)
                    Dim [event] As IEventSymbol = eventAccess?.EventSymbol
                    Dim instance = If([event] Is Nothing OrElse [event].IsStatic, Nothing, If(eventAccess IsNot Nothing, Create(eventAccess.ReceiverOpt), Nothing))

                    Return New EventAssignmentExpression(
                        [event], instance, Create(statement.Handler), adds:=True, isInvalid:=statement.HasErrors, syntax:=statement.Syntax, type:=Nothing, constantValue:=Nothing)
                End Function)
        End Function


        Private Shared Function GetRemoveStatementExpression(handlerStatement As BoundRemoveHandlerStatement) As IOperation
            Return s_expressionsMappings.GetValue(
                handlerStatement,
                Function(statement)
                    Dim eventAccess As BoundEventAccess = TryCast(statement.EventAccess, BoundEventAccess)
                    Dim [event] As IEventSymbol = eventAccess?.EventSymbol
                    Dim instance = If([event] Is Nothing OrElse [event].IsStatic, Nothing, If(eventAccess IsNot Nothing, Create(eventAccess.ReceiverOpt), Nothing))

                    Return New EventAssignmentExpression(
                        [event], instance, Create(statement.Handler), adds:=False, isInvalid:=statement.HasErrors, syntax:=statement.Syntax, type:=Nothing, constantValue:=Nothing)

                End Function)
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
                Select Case operand.Type.SpecialType
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
                Select Case left.Type.SpecialType

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

                If left.Type.TypeKind = TypeKind.Enum Then
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