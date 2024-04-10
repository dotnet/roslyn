' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports System.Diagnostics
Imports System.Runtime.InteropServices
Imports Microsoft.CodeAnalysis.PooledObjects
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports TypeKind = Microsoft.CodeAnalysis.TypeKind

Namespace Microsoft.CodeAnalysis.VisualBasic
    Partial Friend NotInheritable Class LocalRewriter

        ''' <summary>
        ''' Rewrites ForTo loop.
        ''' </summary>
        Public Overrides Function VisitForToStatement(node As BoundForToStatement) As BoundNode
            Dim rewrittenControlVariable = VisitExpressionNode(node.ControlVariable)

            ' loop with object control variable is a special kind of loop 
            ' its logic is governed by runtime helpers.
            Dim isObjectLoop = rewrittenControlVariable.Type.IsObjectType
            Dim rewrittenInitialValue = VisitExpressionNode(node.InitialValue)
            Dim rewrittenLimit As BoundExpression = VisitExpressionNode(node.LimitValue)
            Dim rewrittenStep = VisitExpressionNode(node.StepValue)

            If Not isObjectLoop Then
                Return FinishNonObjectForLoop(node,
                                            rewrittenControlVariable,
                                            rewrittenInitialValue,
                                            rewrittenLimit,
                                            rewrittenStep)
            Else
                Debug.Assert(node.OperatorsOpt Is Nothing)
                Return FinishObjectForLoop(node,
                                            rewrittenControlVariable,
                                            rewrittenInitialValue,
                                            rewrittenLimit,
                                            rewrittenStep)
            End If
        End Function

        Private Function FinishNonObjectForLoop(forStatement As BoundForToStatement,
                                                rewrittenControlVariable As BoundExpression,
                                                rewrittenInitialValue As BoundExpression,
                                                rewrittenLimit As BoundExpression,
                                                rewrittenStep As BoundExpression) As BoundBlock

            Dim blockSyntax = DirectCast(forStatement.Syntax, ForBlockSyntax)

            Dim generateUnstructuredExceptionHandlingResumeCode As Boolean = ShouldGenerateUnstructuredExceptionHandlingResumeCode(forStatement)

            Dim loopResumeTarget As ImmutableArray(Of BoundStatement) = Nothing

            If generateUnstructuredExceptionHandlingResumeCode Then
                loopResumeTarget = RegisterUnstructuredExceptionHandlingResumeTarget(blockSyntax, canThrow:=True)
            End If

            Dim cacheAssignments = ArrayBuilder(Of BoundExpression).GetInstance()

            ' need to do this early while constant values are still constants 
            ' (may become sequences that capture initialization later)
            Dim unconditionalEntry As Boolean = WillDoAtLeastOneIteration(rewrittenInitialValue, rewrittenLimit, rewrittenStep)

            ' For loop must ensure that loop range and step do not change while iterating so it needs to 
            ' cache non-constant values.
            ' We do not need to do this for object loops though. Object loop helpers do caching internally.

            ' NOTE the order of the following initializations is important!!!!
            ' Values are evaluated before hoisting and it must be done in the order of declaration (value, limit, step).

            Dim locals = ArrayBuilder(Of LocalSymbol).GetInstance()

            rewrittenInitialValue = CacheToLocalIfNotConst(_currentMethodOrLambda, rewrittenInitialValue, locals, cacheAssignments, SynthesizedLocalKind.ForInitialValue, blockSyntax)
            rewrittenLimit = CacheToLocalIfNotConst(_currentMethodOrLambda, rewrittenLimit, locals, cacheAssignments, SynthesizedLocalKind.ForLimit, blockSyntax)
            rewrittenStep = CacheToLocalIfNotConst(_currentMethodOrLambda, rewrittenStep, locals, cacheAssignments, SynthesizedLocalKind.ForStep, blockSyntax)

            Dim positiveFlag As SynthesizedLocal = Nothing

            If forStatement.OperatorsOpt IsNot Nothing Then
                ' calculate and cache result of a positive check := step >= (step - step).
                AddPlaceholderReplacement(forStatement.OperatorsOpt.LeftOperandPlaceholder, rewrittenStep)
                AddPlaceholderReplacement(forStatement.OperatorsOpt.RightOperandPlaceholder, rewrittenStep)

                Dim subtraction = VisitExpressionNode(forStatement.OperatorsOpt.Subtraction)

                UpdatePlaceholderReplacement(forStatement.OperatorsOpt.RightOperandPlaceholder, subtraction)

                Dim greaterThanOrEqual = VisitExpressionNode(forStatement.OperatorsOpt.GreaterThanOrEqual)

                positiveFlag = New SynthesizedLocal(_currentMethodOrLambda, greaterThanOrEqual.Type, SynthesizedLocalKind.ForDirection, blockSyntax)
                locals.Add(positiveFlag)

                cacheAssignments.Add(New BoundAssignmentOperator(forStatement.OperatorsOpt.Syntax,
                                                                 New BoundLocal(forStatement.OperatorsOpt.Syntax,
                                                                                positiveFlag,
                                                                                positiveFlag.Type),
                                                                 greaterThanOrEqual,
                                                                 suppressObjectClone:=True, type:=positiveFlag.Type))

                RemovePlaceholderReplacement(forStatement.OperatorsOpt.LeftOperandPlaceholder)
                RemovePlaceholderReplacement(forStatement.OperatorsOpt.RightOperandPlaceholder)

            ElseIf rewrittenStep.ConstantValueOpt Is Nothing AndAlso
                Not rewrittenStep.Type.GetEnumUnderlyingTypeOrSelf.IsSignedIntegralType AndAlso
                Not rewrittenStep.Type.GetEnumUnderlyingTypeOrSelf.IsUnsignedIntegralType Then

                Dim stepValue As BoundExpression = rewrittenStep
                Dim stepHasValue As BoundExpression = Nothing

                If stepValue.Type.IsNullableType Then
                    stepHasValue = NullableHasValue(stepValue)
                    stepValue = NullableValueOrDefault(stepValue)
                End If

                If stepValue.Type.GetEnumUnderlyingTypeOrSelf.IsNumericType Then

                    ' this one is tricky.
                    ' step value is not used directly in the loop condition
                    ' however its value determines the iteration direction
                    ' isUp = IsTrue(step >= step - step)
                    ' which implies that "step = null" ==> "isUp = false"

                    Dim literalUnderlyingType As TypeSymbol = stepValue.Type.GetEnumUnderlyingTypeOrSelf
                    Dim literal As BoundExpression = New BoundLiteral(rewrittenStep.Syntax, ConstantValue.Default(literalUnderlyingType.SpecialType), stepValue.Type)

                    ' Rewrite decimal literal if needed 
                    If literalUnderlyingType.IsDecimalType Then
                        literal = RewriteDecimalConstant(literal, literal.ConstantValueOpt, Me._topMethod, Me._diagnostics)
                    End If

                    Dim isUp As BoundExpression = TransformRewrittenBinaryOperator(
                                                        New BoundBinaryOperator(rewrittenStep.Syntax,
                                                                                BinaryOperatorKind.GreaterThanOrEqual,
                                                                                stepValue,
                                                                                literal,
                                                                                checked:=False,
                                                                                type:=GetSpecialType(SpecialType.System_Boolean)))

                    If stepHasValue IsNot Nothing Then
                        ' null step is considered "not isUp"
                        isUp = MakeBooleanBinaryExpression(isUp.Syntax,
                                                            BinaryOperatorKind.AndAlso,
                                                            stepHasValue,
                                                            isUp)
                    End If

                    positiveFlag = New SynthesizedLocal(_currentMethodOrLambda, isUp.Type, SynthesizedLocalKind.ForDirection, blockSyntax)
                    locals.Add(positiveFlag)

                    cacheAssignments.Add(New BoundAssignmentOperator(isUp.Syntax,
                                                                     New BoundLocal(isUp.Syntax,
                                                                                    positiveFlag,
                                                                                    positiveFlag.Type),
                                                                     isUp,
                                                                     suppressObjectClone:=True, type:=positiveFlag.Type))
                Else
                    ' not numeric control variables are special and should not be handled here.
                    Throw ExceptionUtilities.Unreachable
                End If
            End If

            If cacheAssignments.Count > 0 Then
                rewrittenInitialValue = New BoundSequence(rewrittenInitialValue.Syntax,
                                                          ImmutableArray(Of LocalSymbol).Empty,
                                                          cacheAssignments.ToImmutable(),
                                                          rewrittenInitialValue,
                                                          rewrittenInitialValue.Type)
            End If

            cacheAssignments.Free()

            Dim rewrittenInitializer As BoundStatement = New BoundExpressionStatement(
                rewrittenInitialValue.Syntax,
                New BoundAssignmentOperator(
                    rewrittenInitialValue.Syntax,
                    rewrittenControlVariable,
                    rewrittenInitialValue,
                    suppressObjectClone:=True,
                    type:=rewrittenInitialValue.Type
                )
            )

            If Not loopResumeTarget.IsDefaultOrEmpty Then
                rewrittenInitializer = New BoundStatementList(rewrittenInitializer.Syntax, loopResumeTarget.Add(rewrittenInitializer))
            End If

            Dim instrument As Boolean = Me.Instrument(forStatement)

            If instrument Then
                ' first sequence point to highlight the for statement
                rewrittenInitializer = _instrumenterOpt.InstrumentForLoopInitialization(forStatement, rewrittenInitializer)
            End If

            Dim rewrittenBody = DirectCast(Visit(forStatement.Body), BoundStatement)

            Dim rewrittenIncrement As BoundStatement = RewriteForLoopIncrement(
                                                rewrittenControlVariable,
                                                rewrittenStep,
                                                forStatement.Checked,
                                                forStatement.OperatorsOpt)

            If generateUnstructuredExceptionHandlingResumeCode Then
                rewrittenIncrement = RegisterUnstructuredExceptionHandlingResumeTarget(blockSyntax, rewrittenIncrement, canThrow:=True)
            End If

            If instrument Then
                rewrittenIncrement = _instrumenterOpt.InstrumentForLoopIncrement(forStatement, rewrittenIncrement)
            End If

            Dim rewrittenCondition = RewriteForLoopCondition(rewrittenControlVariable, rewrittenLimit, rewrittenStep,
                                                             forStatement.OperatorsOpt, positiveFlag)

            Dim startLabel = GenerateLabel("start")
            Dim ifConditionGotoStart As BoundStatement = New BoundConditionalGoto(
                blockSyntax,
                rewrittenCondition,
                True,
                startLabel)

            ' For i as Integer = 3 To 6 step 2
            '    body
            ' Next
            '
            ' becomes
            '
            ' {
            '     ' NOTE: control variable life time is as in Dev10, Dev11 may change this!!!!
            '     dim i as Integer = 3  '<-- all iterations share same variable (important for closures)
            '     dim temp1, temp2 ...  ' temps if needed for hoisting of Limit/Step/Direction
            '
            '     goto postIncrement;
            '   start:
            '     body      
            '   continue:
            '     i += 2
            '   postIncrement:
            '     if i <= 6 goto start

            '   exit:
            ' }

            'optimization for a case where limit and initial value are constant and the first 
            'iteration is definite so we can simply drop through without initial branch

            Dim postIncrementLabel As GeneratedLabelSymbol = Nothing
            Dim gotoPostIncrement As BoundStatement = Nothing

            If Not unconditionalEntry Then
                'mark the initial jump as hidden.
                'We do not want to associate it with statement before.
                'This jump may be a target of another jump (for example if loops are nested) and that will make 
                'impression of the previous statement being re-executed
                postIncrementLabel = New GeneratedLabelSymbol("PostIncrement")
                Dim postIncrement As New BoundLabelStatement(blockSyntax, postIncrementLabel)

                gotoPostIncrement = New BoundGotoStatement(blockSyntax, postIncrementLabel, Nothing)

                If instrument Then
                    gotoPostIncrement = SyntheticBoundNodeFactory.HiddenSequencePoint(gotoPostIncrement)
                End If
            End If

            Dim statements = ArrayBuilder(Of BoundStatement).GetInstance()

            statements.Add(rewrittenInitializer)

            If gotoPostIncrement IsNot Nothing Then
                statements.Add(gotoPostIncrement)
            End If

            statements.Add(New BoundLabelStatement(blockSyntax, startLabel))
            statements.Add(rewrittenBody)

            statements.Add(New BoundLabelStatement(blockSyntax, forStatement.ContinueLabel))
            statements.Add(rewrittenIncrement)

            If postIncrementLabel IsNot Nothing Then
                Dim label As BoundStatement = New BoundLabelStatement(blockSyntax, postIncrementLabel)
                If instrument Then
                    gotoPostIncrement = SyntheticBoundNodeFactory.HiddenSequencePoint(label)
                End If
                statements.Add(label)
            End If

            If instrument Then
                ifConditionGotoStart = SyntheticBoundNodeFactory.HiddenSequencePoint(ifConditionGotoStart)
            End If
            statements.Add(ifConditionGotoStart)

            statements.Add(New BoundLabelStatement(blockSyntax, forStatement.ExitLabel))

            Dim localSymbol = forStatement.DeclaredOrInferredLocalOpt
            If localSymbol IsNot Nothing Then
                locals.Add(localSymbol)
            End If

            Return New BoundBlock(
                blockSyntax,
                Nothing,
                locals.ToImmutableAndFree(),
                statements.ToImmutableAndFree
            )
        End Function

        Private Shared Function WillDoAtLeastOneIteration(rewrittenInitialValue As BoundExpression,
                                                          rewrittenLimit As BoundExpression,
                                                          rewrittenStep As BoundExpression) As Boolean

            Dim initialConst = rewrittenInitialValue.ConstantValueOpt
            If initialConst Is Nothing Then
                Return False
            End If

            Dim limitConst = rewrittenLimit.ConstantValueOpt
            If limitConst Is Nothing Then
                Return False
            End If

            Dim isSteppingDown As Boolean

            Dim stepConst = rewrittenStep.ConstantValueOpt
            If stepConst Is Nothing Then
                If rewrittenStep.Type.GetEnumUnderlyingTypeOrSelf.IsUnsignedIntegralType Then
                    isSteppingDown = False
                Else
                    Return False
                End If
            Else
                isSteppingDown = stepConst.IsNegativeNumeric
            End If

            ' handle unsigned integrals
            If initialConst.IsUnsigned Then
                Dim initialValue As ULong = initialConst.UInt64Value
                Dim limitValue As ULong = limitConst.UInt64Value

                Return If(isSteppingDown,
                          initialValue >= limitValue,
                          initialValue <= limitValue)

            End If

            ' handle remaining (signed) integrals
            If initialConst.IsIntegral Then
                Dim initialValue As Long = initialConst.Int64Value
                Dim limitValue As Long = limitConst.Int64Value

                Return If(isSteppingDown,
                          initialValue >= limitValue,
                          initialValue <= limitValue)

            End If

            ' handle decimals
            If initialConst.IsDecimal Then
                Dim initialValue As Decimal = initialConst.DecimalValue
                Dim limitValue As Decimal = limitConst.DecimalValue

                Return If(isSteppingDown,
                          initialValue >= limitValue,
                          initialValue <= limitValue)

            End If

            ' the rest should be floats
            Debug.Assert(initialConst.IsFloating)
            If True Then
                Dim initialValue As Double = initialConst.DoubleValue
                Dim limitValue As Double = limitConst.DoubleValue

                Return If(isSteppingDown,
                          initialValue >= limitValue,
                          initialValue <= limitValue)

            End If

            Throw ExceptionUtilities.Unreachable
        End Function

        Private Function FinishObjectForLoop(forStatement As BoundForToStatement,
                                             rewrittenControlVariable As BoundExpression,
                                             rewrittenInitialValue As BoundExpression,
                                             rewrittenLimit As BoundExpression,
                                             rewrittenStep As BoundExpression) As BoundBlock

            Dim locals = ArrayBuilder(Of LocalSymbol).GetInstance()
            Dim blockSyntax = DirectCast(forStatement.Syntax, ForBlockSyntax)
            Dim generateUnstructuredExceptionHandlingResumeCode As Boolean = ShouldGenerateUnstructuredExceptionHandlingResumeCode(forStatement)

            ' For i as Object = 3 To 6 step 2
            '    body
            ' Next
            '
            ' becomes ==>
            '
            ' {
            '   Dim loopObj        ' mysterious object that holds the loop state
            '
            '   ' helper does internal initialization and tells if we need to do any iterations
            '   if Not ObjectFlowControl.ForLoopControl.ForLoopInitObj(ctrl, init, limit, step, ref loopObj, ref ctrl) 
            '                               goto exit:
            '   start:
            '       body
            '
            '   continue:
            '       ' helper updates loop state and tels if we need to do another iteration.
            '       if ObjectFlowControl.ForLoopControl.ForNextCheckObj(ctrl, loopObj, ref ctrl) 
            '                               GoTo start
            ' }
            ' exit:

            Debug.Assert(Compilation.GetSpecialType(SpecialType.System_Object) Is rewrittenControlVariable.Type)
            Dim objType = rewrittenControlVariable.Type
            Dim loopObjLocal = New SynthesizedLocal(Me._currentMethodOrLambda, objType, SynthesizedLocalKind.ForInitialValue, blockSyntax)
            locals.Add(loopObjLocal)

            Dim loopObj = New BoundLocal(blockSyntax, loopObjLocal, isLValue:=True, type:=loopObjLocal.Type)

            ' Create loop initialization and entrance criteria -
            '     if Not ObjectFlowControl.ForLoopControl.ForLoopInitObj(ctrl, init, limit, step, ref loopObj, ref ctrl) 
            '                               goto exit:
            Dim rewrittenInitCondition As BoundExpression
            Dim arguments = ImmutableArray.Create(
                rewrittenControlVariable.MakeRValue(),
                rewrittenInitialValue,
                rewrittenLimit,
                rewrittenStep,
                loopObj,
                rewrittenControlVariable)

            Dim ForLoopInitObj As MethodSymbol = Nothing
            If TryGetWellknownMember(ForLoopInitObj, WellKnownMember.Microsoft_VisualBasic_CompilerServices_ObjectFlowControl_ForLoopControl__ForLoopInitObj, blockSyntax) Then
                ' ForLoopInitObj(ctrl, init, limit, step, ref loopObj, ref ctrl)
                rewrittenInitCondition = New BoundCall(
                    rewrittenLimit.Syntax,
                    ForLoopInitObj,
                    Nothing,
                    Nothing,
                    arguments,
                    Nothing,
                    Compilation.GetSpecialType(SpecialType.System_Boolean),
                    suppressObjectClone:=True)
            Else
                rewrittenInitCondition = New BoundBadExpression(rewrittenLimit.Syntax, LookupResultKind.NotReferencable, ImmutableArray(Of Symbol).Empty,
                                                                arguments, Compilation.GetSpecialType(SpecialType.System_Boolean), hasErrors:=True)
            End If

            ' EnC: We need to insert a hidden sequence point to handle function remapping in case 
            ' the containing method is edited while the invoked method is being executed.
            Debug.Assert(rewrittenInitCondition IsNot Nothing)
            Dim instrument = Me.Instrument(forStatement)

            If instrument Then
                rewrittenInitCondition = _instrumenterOpt.InstrumentObjectForLoopInitCondition(forStatement, rewrittenInitCondition, _currentMethodOrLambda)
            End If

            Dim ifNotInitObjExit As BoundStatement = New BoundConditionalGoto(
                blockSyntax,
                rewrittenInitCondition,
                False,
                forStatement.ExitLabel)

            If generateUnstructuredExceptionHandlingResumeCode Then
                ifNotInitObjExit = RegisterUnstructuredExceptionHandlingResumeTarget(blockSyntax, ifNotInitObjExit, canThrow:=True)
            End If

            If instrument Then
                ' first sequence point to highlight the for statement
                ifNotInitObjExit = _instrumenterOpt.InstrumentForLoopInitialization(forStatement, ifNotInitObjExit)
            End If

            '### body

            Dim rewrittenBody = DirectCast(Visit(forStatement.Body), BoundStatement)

            ' Create loop condition (ifConditionGotoStart) - 
            '    if ObjectFlowControl.ForLoopControl.ForNextCheckObj(ctrl, loopObj, ref ctrl) 
            '                                               GoTo start
            Dim rewrittenLoopCondition As BoundExpression

            arguments = ImmutableArray.Create(
                rewrittenControlVariable.MakeRValue(),
                loopObj.MakeRValue,
                rewrittenControlVariable)

            Dim ForNextCheckObj As MethodSymbol = Nothing
            If TryGetWellknownMember(ForNextCheckObj, WellKnownMember.Microsoft_VisualBasic_CompilerServices_ObjectFlowControl_ForLoopControl__ForNextCheckObj, blockSyntax) Then
                ' ForNextCheckObj(ctrl, loopObj, ref ctrl) 
                rewrittenLoopCondition = New BoundCall(
                    rewrittenLimit.Syntax,
                    ForNextCheckObj,
                    Nothing,
                    Nothing,
                    arguments,
                    Nothing,
                    Compilation.GetSpecialType(SpecialType.System_Boolean),
                    suppressObjectClone:=True)
            Else
                rewrittenLoopCondition = New BoundBadExpression(rewrittenLimit.Syntax, LookupResultKind.NotReferencable, ImmutableArray(Of Symbol).Empty,
                                                                arguments, Compilation.GetSpecialType(SpecialType.System_Boolean), hasErrors:=True)
            End If

            Dim startLabel = GenerateLabel("start")

            ' EnC: We need to insert a hidden sequence point to handle function remapping in case 
            ' the containing method is edited while the invoked function is being executed.
            Debug.Assert(rewrittenLoopCondition IsNot Nothing)
            If instrument Then
                rewrittenLoopCondition = _instrumenterOpt.InstrumentObjectForLoopCondition(forStatement, rewrittenLoopCondition, _currentMethodOrLambda)
            End If

            Dim ifConditionGotoStart As BoundStatement = New BoundConditionalGoto(
                blockSyntax,
                rewrittenLoopCondition,
                True,
                startLabel)

            If generateUnstructuredExceptionHandlingResumeCode Then
                ifConditionGotoStart = RegisterUnstructuredExceptionHandlingResumeTarget(blockSyntax, ifConditionGotoStart, canThrow:=True)
            End If

            If instrument Then
                ifConditionGotoStart = _instrumenterOpt.InstrumentForLoopIncrement(forStatement, ifConditionGotoStart)
            End If

            Dim label As BoundStatement = New BoundLabelStatement(blockSyntax, forStatement.ContinueLabel)

            If instrument Then
                label = SyntheticBoundNodeFactory.HiddenSequencePoint(label)
                ifConditionGotoStart = SyntheticBoundNodeFactory.HiddenSequencePoint(ifConditionGotoStart)
            End If

            'Build the rewritten loop
            Dim statements = ImmutableArray.Create(
                ifNotInitObjExit,
                New BoundLabelStatement(blockSyntax, startLabel),
                rewrittenBody,
                label,
                ifConditionGotoStart,
                New BoundLabelStatement(blockSyntax, forStatement.ExitLabel))

            Dim localSymbol = forStatement.DeclaredOrInferredLocalOpt
            If localSymbol IsNot Nothing Then
                locals.Add(localSymbol)
            End If

            Return New BoundBlock(
                blockSyntax,
                Nothing,
                locals.ToImmutableAndFree(),
                statements)
        End Function

        Private Function RewriteForLoopIncrement(controlVariable As BoundExpression,
                       stepValue As BoundExpression,
                       isChecked As Boolean,
                       operatorsOpt As BoundForToUserDefinedOperators) As BoundStatement

            Debug.Assert(controlVariable.IsLValue)

            Dim newValue As BoundExpression

            If operatorsOpt Is Nothing Then
                Dim controlVariableUnwrapped = controlVariable

                ' if control variable happen to be nullable 
                ' unlift the increment by applying Add to the unwrapped step
                ' and controlVariable.
                ' since limit and control var are locals, GetValueOrDefault
                ' should be cheap enough to not bother with hoisting values into locals.

                Dim hasValues As BoundExpression = Nothing

                If controlVariable.Type.IsNullableType Then
                    hasValues = MakeBooleanBinaryExpression(controlVariable.Syntax,
                                                            BinaryOperatorKind.And,
                                                            NullableHasValue(stepValue),
                                                            NullableHasValue(controlVariable))

                    controlVariableUnwrapped = NullableValueOrDefault(controlVariable)
                    stepValue = NullableValueOrDefault(stepValue)
                End If

                newValue = TransformRewrittenBinaryOperator(
                                New BoundBinaryOperator(stepValue.Syntax,
                                                        BinaryOperatorKind.Add,
                                                        controlVariableUnwrapped.MakeRValue(),
                                                        stepValue,
                                                        isChecked,
                                                        controlVariableUnwrapped.Type))

                If controlVariable.Type.IsNullableType Then
                    newValue = MakeTernaryConditionalExpression(newValue.Syntax,
                                                                hasValues,
                                                                WrapInNullable(newValue, controlVariable.Type),
                                                                NullableNull(controlVariable.Syntax, controlVariable.Type))
                End If

            Else
                ' Generate: controlVariable + stepValue
                AddPlaceholderReplacement(operatorsOpt.LeftOperandPlaceholder, controlVariable.MakeRValue())
                AddPlaceholderReplacement(operatorsOpt.RightOperandPlaceholder, stepValue)

                newValue = VisitExpressionNode(operatorsOpt.Addition)

                RemovePlaceholderReplacement(operatorsOpt.LeftOperandPlaceholder)
                RemovePlaceholderReplacement(operatorsOpt.RightOperandPlaceholder)
            End If

            Return New BoundExpressionStatement(
                stepValue.Syntax,
                New BoundAssignmentOperator(
                    stepValue.Syntax,
                    controlVariable,
                    newValue,
                    suppressObjectClone:=True,
                    type:=controlVariable.Type
                )
            )

        End Function

        ''' <summary>
        ''' Negates the value if step is negative
        ''' </summary>
        Private Function NegateIfStepNegative(value As BoundExpression, [step] As BoundExpression) As BoundExpression
            Dim int32Type = [step].Type.ContainingAssembly.GetPrimitiveType(Microsoft.Cci.PrimitiveTypeCode.Int32)

            Dim bits As Integer = [step].Type.GetEnumUnderlyingTypeOrSelf.SpecialType.VBForToShiftBits()

            Dim shiftConst = New BoundLiteral(value.Syntax, ConstantValue.Create(bits), int32Type)
            Dim shiftedStep = TransformRewrittenBinaryOperator(
                                New BoundBinaryOperator(value.Syntax, BinaryOperatorKind.RightShift, [step], shiftConst, False, [step].Type))

            Return TransformRewrittenBinaryOperator(New BoundBinaryOperator(value.Syntax, BinaryOperatorKind.Xor, shiftedStep, value, False, value.Type))
        End Function

        ''' <summary>
        ''' Given the control variable, limit and step, produce the loop condition.
        ''' The principle is simple - 
        '''       if step is negative (stepping "Up") then it is "control >= limit"
        '''       otherwise it is "control &lt;= limit"
        ''' 
        ''' It gets more complicated when step is not a constant or not a numeric or 
        ''' involves overloaded comparison/IsTrue operators
        ''' </summary>

        Private Function RewriteForLoopCondition(controlVariable As BoundExpression,
                                        limit As BoundExpression,
                                        stepValue As BoundExpression,
                                        operatorsOpt As BoundForToUserDefinedOperators,
                                        positiveFlag As SynthesizedLocal) As BoundExpression
            Debug.Assert(operatorsOpt Is Nothing OrElse positiveFlag IsNot Nothing)

            If operatorsOpt IsNot Nothing Then

                ' Generate If(positiveFlag, controlVariable <= limit, controlVariable >= limit)
                AddPlaceholderReplacement(operatorsOpt.LeftOperandPlaceholder, controlVariable.MakeRValue())
                AddPlaceholderReplacement(operatorsOpt.RightOperandPlaceholder, limit)

                Dim result = MakeTernaryConditionalExpression(operatorsOpt.Syntax,
                                                                   New BoundLocal(operatorsOpt.Syntax,
                                                                                  positiveFlag,
                                                                                  isLValue:=False,
                                                                                  type:=positiveFlag.Type),
                                                                   VisitExpressionNode(operatorsOpt.LessThanOrEqual),
                                                                   VisitExpressionNode(operatorsOpt.GreaterThanOrEqual))

                RemovePlaceholderReplacement(operatorsOpt.LeftOperandPlaceholder)
                RemovePlaceholderReplacement(operatorsOpt.RightOperandPlaceholder)

                Return result
            End If

            Dim booleanType = GetSpecialType(SpecialType.System_Boolean)

            ' unsigned step is always Up
            If stepValue.Type.GetEnumUnderlyingTypeOrSelf.IsUnsignedIntegralType Then
                Return TransformRewrittenBinaryOperator(
                            New BoundBinaryOperator(limit.Syntax,
                                                    BinaryOperatorKind.LessThanOrEqual,
                                                    controlVariable.MakeRValue(),
                                                    limit,
                                                    checked:=False,
                                                    type:=booleanType))
            End If

            'Up/Down for numeric constants is also simple 
            Dim constStep = stepValue.ConstantValueOpt
            If constStep IsNot Nothing Then
                Dim comparisonOperator As BinaryOperatorKind
                If constStep.IsNegativeNumeric Then
                    comparisonOperator = BinaryOperatorKind.GreaterThanOrEqual
                ElseIf constStep.IsNumeric Then
                    comparisonOperator = BinaryOperatorKind.LessThanOrEqual
                Else
                    ' it is a constant, but not numeric. Can this happen?
                    Throw ExceptionUtilities.UnexpectedValue(constStep)
                End If

                Return TransformRewrittenBinaryOperator(
                            New BoundBinaryOperator(limit.Syntax,
                                                    comparisonOperator,
                                                    controlVariable.MakeRValue(),
                                                    limit,
                                                    checked:=False,
                                                    type:=booleanType))
            End If

            ' for signed integral steps not known at compile time
            ' we do    " (val Xor (step >> 31)) <= (limit Xor (step >> 31)) "
            ' where 31 is actually the size-1
            ' TODO: we could hoist (step >> 31) into a temp. Dev10 does not do this. 
            '       Perhaps not worth it since non-const steps are uncommon
            If stepValue.Type.GetEnumUnderlyingTypeOrSelf.IsSignedIntegralType Then
                Return TransformRewrittenBinaryOperator(
                            New BoundBinaryOperator(stepValue.Syntax,
                                                    BinaryOperatorKind.LessThanOrEqual,
                                                    NegateIfStepNegative(controlVariable.MakeRValue(), stepValue),
                                                    NegateIfStepNegative(limit, stepValue),
                                                    checked:=False,
                                                    type:=booleanType))
            End If

            Dim condition As BoundExpression
            Dim ctrlLimitHasValue As BoundExpression = Nothing

            If controlVariable.Type.IsNullableType Then

                ' if either limit or control variable is null, we exit the loop
                ctrlLimitHasValue = MakeBooleanBinaryExpression(controlVariable.Syntax,
                                                                BinaryOperatorKind.And,
                                                                NullableHasValue(limit),
                                                                NullableHasValue(controlVariable))
                controlVariable = NullableValueOrDefault(controlVariable)
                limit = NullableValueOrDefault(limit)
            End If

            If positiveFlag IsNot Nothing Then

                'If (stepValue >= 0.0, ctrl <= limit, ctrl >= limit)
                Dim lte = TransformRewrittenBinaryOperator(
                                New BoundBinaryOperator(limit.Syntax,
                                                        BinaryOperatorKind.LessThanOrEqual,
                                                        controlVariable.MakeRValue(),
                                                        limit,
                                                        checked:=False,
                                                        type:=booleanType))

                Dim gte = TransformRewrittenBinaryOperator(
                                New BoundBinaryOperator(limit.Syntax,
                                                        BinaryOperatorKind.GreaterThanOrEqual,
                                                        controlVariable.MakeRValue(),
                                                        limit,
                                                        checked:=False,
                                                        type:=booleanType))

                Dim isUp As BoundExpression = New BoundLocal(limit.Syntax,
                                                             positiveFlag,
                                                             isLValue:=False,
                                                             type:=positiveFlag.Type)

                condition = MakeTernaryConditionalExpression(limit.Syntax,
                                                                isUp,
                                                                lte,
                                                                gte)
            Else
                ' not numeric control variables are special and should not be handled here.
                Throw ExceptionUtilities.Unreachable
            End If

            If ctrlLimitHasValue IsNot Nothing Then
                ' check for has values before checking condition
                condition = MakeBooleanBinaryExpression(condition.Syntax,
                                                        BinaryOperatorKind.AndAlso,
                                                        ctrlLimitHasValue,
                                                        condition)
            End If

            Return condition
        End Function
    End Class
End Namespace
