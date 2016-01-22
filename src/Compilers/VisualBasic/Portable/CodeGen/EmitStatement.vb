' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Immutable
Imports System.Reflection.Metadata
Imports System.Runtime.InteropServices
Imports Microsoft.CodeAnalysis.CodeGen
Imports Microsoft.CodeAnalysis.Collections
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols

Namespace Microsoft.CodeAnalysis.VisualBasic.CodeGen

    Friend Partial Class CodeGenerator
        Private Sub EmitStatement(statement As BoundStatement)
            Select Case statement.Kind

                Case BoundKind.Block
                    EmitBlock(DirectCast(statement, BoundBlock))

                Case BoundKind.SequencePoint
                    EmitSequencePointStatement(DirectCast(statement, BoundSequencePoint))

                Case BoundKind.SequencePointWithSpan
                    EmitSequencePointStatement(DirectCast(statement, BoundSequencePointWithSpan))

                Case BoundKind.ExpressionStatement
                    EmitExpression((DirectCast(statement, BoundExpressionStatement)).Expression, False)

                Case BoundKind.NoOpStatement
                    EmitNoOpStatement(DirectCast(statement, BoundNoOpStatement))

                Case BoundKind.StatementList
                    Dim list = DirectCast(statement, BoundStatementList)
                    Dim n As Integer = list.Statements.Length
                    For i = 0 To n - 1
                        EmitStatement(list.Statements(i))
                    Next

                Case BoundKind.ReturnStatement
                    EmitReturnStatement(DirectCast(statement, BoundReturnStatement))

                Case BoundKind.ThrowStatement
                    EmitThrowStatement(DirectCast(statement, BoundThrowStatement))

                Case BoundKind.GotoStatement
                    EmitGotoStatement(DirectCast(statement, BoundGotoStatement))

                Case BoundKind.LabelStatement
                    EmitLabelStatement(DirectCast(statement, BoundLabelStatement))

                Case BoundKind.ConditionalGoto
                    EmitConditionalGoto(DirectCast(statement, BoundConditionalGoto))

                Case BoundKind.TryStatement
                    EmitTryStatement(DirectCast(statement, BoundTryStatement))

                Case BoundKind.SelectStatement
                    EmitSelectStatement(DirectCast(statement, BoundSelectStatement))

                Case BoundKind.UnstructuredExceptionOnErrorSwitch
                    EmitUnstructuredExceptionOnErrorSwitch(DirectCast(statement, BoundUnstructuredExceptionOnErrorSwitch))

                Case BoundKind.UnstructuredExceptionResumeSwitch
                    EmitUnstructuredExceptionResumeSwitch(DirectCast(statement, BoundUnstructuredExceptionResumeSwitch))

                Case BoundKind.StateMachineScope
                    EmitStateMachineScope(DirectCast(statement, BoundStateMachineScope))

                Case Else
                    Throw ExceptionUtilities.UnexpectedValue(statement.Kind)
            End Select

#If DEBUG Then
            If Me._stackLocals Is Nothing OrElse Not Me._stackLocals.Any Then
                _builder.AssertStackEmpty()
            End If
#End If
        End Sub

        Private Function EmitStatementAndCountInstructions(statement As BoundStatement) As Integer
            Dim n = _builder.InstructionsEmitted
            EmitStatement(statement)
            Return _builder.InstructionsEmitted - n
        End Function

        Private Sub EmitNoOpStatement(statement As BoundNoOpStatement)
            Select Case statement.Flavor
                Case NoOpStatementFlavor.Default
                    If _ilEmitStyle = ILEmitStyle.Debug Then
                        _builder.EmitOpCode(ILOpCode.Nop)
                    End If

                Case NoOpStatementFlavor.AwaitYieldPoint
                    Debug.Assert((_asyncYieldPoints Is Nothing) = (_asyncResumePoints Is Nothing))
                    If _asyncYieldPoints Is Nothing Then
                        _asyncYieldPoints = ArrayBuilder(Of Integer).GetInstance
                        _asyncResumePoints = ArrayBuilder(Of Integer).GetInstance
                    End If
                    Debug.Assert(_asyncYieldPoints.Count = _asyncResumePoints.Count)
                    _asyncYieldPoints.Add(_builder.AllocateILMarker())

                Case NoOpStatementFlavor.AwaitResumePoint
                    Debug.Assert(_asyncYieldPoints IsNot Nothing)
                    Debug.Assert(_asyncResumePoints IsNot Nothing)
                    Debug.Assert((_asyncYieldPoints.Count - 1) = _asyncResumePoints.Count)
                    _asyncResumePoints.Add(_builder.AllocateILMarker())

                Case Else
                    Throw ExceptionUtilities.UnexpectedValue(statement.Flavor)
            End Select
        End Sub

        Private Sub EmitTryStatement(statement As BoundTryStatement, Optional emitCatchesOnly As Boolean = False)
            Debug.Assert(Not statement.CatchBlocks.IsDefault)

            ' Stack must be empty at beginning of try block.
            _builder.AssertStackEmpty()

            ' IL requires catches and finally block to be distinct try
            ' blocks so if the source contained both a catch and
            ' a finally, nested scopes are emitted.
            Dim emitNestedScopes As Boolean = (Not emitCatchesOnly AndAlso (statement.CatchBlocks.Length > 0) AndAlso (statement.FinallyBlockOpt IsNot Nothing))
            _builder.OpenLocalScope(ScopeType.TryCatchFinally)
            _builder.OpenLocalScope(ScopeType.Try)

            Me._tryNestingLevel += 1

            If emitNestedScopes Then
                EmitTryStatement(statement, emitCatchesOnly:=True)
            Else
                EmitBlock(statement.TryBlock)
            End If

            Debug.Assert(Me._tryNestingLevel > 0)
            Me._tryNestingLevel -= 1

            ' close Try scope
            _builder.CloseLocalScope()

            If Not emitNestedScopes Then
                For Each catchBlock In statement.CatchBlocks
                    EmitCatchBlock(catchBlock)
                Next
            End If

            If Not emitCatchesOnly AndAlso (statement.FinallyBlockOpt IsNot Nothing) Then
                _builder.OpenLocalScope(ScopeType.Finally)
                EmitBlock(statement.FinallyBlockOpt)
                _builder.CloseLocalScope()
            End If

            _builder.CloseLocalScope()

            If Not emitCatchesOnly AndAlso statement.ExitLabelOpt IsNot Nothing Then
                _builder.MarkLabel(statement.ExitLabelOpt)
            End If
        End Sub


        'The interesting part in the following method is the support for exception filters. 
        '=== Example:
        '
        'Try
        '   <SomeCode>
        'Catch ex as NullReferenceException When ex.Message isnot Nothing
        '   <Handler>
        'End Try
        '
        'gets emitted as something like ===>
        '
        'Try
        '   <SomeCode>
        'Filter 
        '    Condition    ' starts with exception on the stack
        '   Dim temp As NullReferenceException = TryCast(Pop, NullReferenceException)
        '         if  temp is Nothing
        '              Push 0
        '         Else
        '              ex = temp
        '              Push if ((ex.Message isnot Nothing), 1, 0)
        '         End If
        '    End Condition   ' leaves 1 or 0 on the stack
        '    Handler             ' gets called after finalization of nested exception frames if condition above produced 1
        '         <Handler>
        '    End Handler
        'End Try
        Private Sub EmitCatchBlock(catchBlock As BoundCatchBlock)
            Dim oldCatchBlock = _currentCatchBlock
            _currentCatchBlock = catchBlock

            Dim typeCheckFailedLabel As Object = Nothing
            Dim exceptionSource = catchBlock.ExceptionSourceOpt

            Dim exceptionType As Cci.ITypeReference

            If exceptionSource IsNot Nothing Then
                exceptionType = Me._module.Translate(exceptionSource.Type, exceptionSource.Syntax, _diagnostics)
            Else
                ' if type is not specified it is assumed to be System.Exception
                exceptionType = Me._module.Translate(Me._module.Compilation.GetWellKnownType(WellKnownType.System_Exception), catchBlock.Syntax, _diagnostics)
            End If

            ' exception on stack
            _builder.AdjustStack(1)

            If catchBlock.ExceptionFilterOpt IsNot Nothing AndAlso catchBlock.ExceptionFilterOpt.Kind = BoundKind.UnstructuredExceptionHandlingCatchFilter Then
                ' This is a special catch created for Unstructured Exception Handling
                Debug.Assert(catchBlock.LocalOpt Is Nothing)
                Debug.Assert(exceptionSource Is Nothing)

                '
                ' Generate the OnError filter.
                '
                ' The On Error filter catches an exception when a handler is active and the method
                ' isn't currently in the process of handling an earlier error.  We know the method
                ' is handling an earlier error when we have a valid Resume target.
                '
                ' The filter expression is the equivalent of:
                '
                '     Catch e When (TypeOf e Is Exception) And (ActiveHandler <> 0) And (ResumeTarget = 0)
                '
                Dim filter = DirectCast(catchBlock.ExceptionFilterOpt, BoundUnstructuredExceptionHandlingCatchFilter)

                _builder.OpenLocalScope(ScopeType.Filter)

                'Determine if the exception object is or inherits from System.Exception
                _builder.EmitOpCode(ILOpCode.Isinst)
                _builder.EmitToken(exceptionType, catchBlock.Syntax, _diagnostics)
                _builder.EmitOpCode(ILOpCode.Ldnull)
                _builder.EmitOpCode(ILOpCode.Cgt_un)

                ' Calculate ActiveHandler <> 0
                EmitLocalLoad(filter.ActiveHandlerLocal, used:=True)
                _builder.EmitIntConstant(0)
                _builder.EmitOpCode(ILOpCode.Cgt_un)

                ' AND the values together.
                _builder.EmitOpCode(ILOpCode.And)

                ' Calculate ResumeTarget = 0
                EmitLocalLoad(filter.ResumeTargetLocal, used:=True)
                _builder.EmitIntConstant(0)
                _builder.EmitOpCode(ILOpCode.Ceq)

                ' AND the values together.
                _builder.EmitOpCode(ILOpCode.And)

                ' Now we are starting the actual handler
                _builder.MarkFilterConditionEnd()

                _builder.EmitOpCode(ILOpCode.Castclass)
                _builder.EmitToken(exceptionType, catchBlock.Syntax, _diagnostics)

                If ShouldNoteProjectErrors() Then
                    EmitSetProjectError(catchBlock.Syntax, catchBlock.ErrorLineNumberOpt)
                Else
                    _builder.EmitOpCode(ILOpCode.Pop)
                End If
            Else
                ' open appropriate exception handler scope. (Catch or Filter)
                ' if it is a Filter, emit prologue that checks if the type on the stack
                ' converts to what we want.
                If catchBlock.ExceptionFilterOpt Is Nothing Then
                    _builder.OpenLocalScope(ScopeType.Catch, exceptionType)

                    If catchBlock.IsSynthesizedAsyncCatchAll Then
                        Debug.Assert(_asyncCatchHandlerOffset < 0)
                        _asyncCatchHandlerOffset = _builder.AllocateILMarker()
                    End If
                Else
                    _builder.OpenLocalScope(ScopeType.Filter)

                    ' Filtering starts with simulating regular Catch through an imperative type check
                    ' If this is not our type, then we are done
                    Dim typeCheckPassedLabel As New Object
                    typeCheckFailedLabel = New Object

                    _builder.EmitOpCode(ILOpCode.Isinst)
                    _builder.EmitToken(exceptionType, catchBlock.Syntax, _diagnostics)
                    _builder.EmitOpCode(ILOpCode.Dup)
                    _builder.EmitBranch(ILOpCode.Brtrue, typeCheckPassedLabel)
                    _builder.EmitOpCode(ILOpCode.Pop)
                    _builder.EmitIntConstant(0)
                    _builder.EmitBranch(ILOpCode.Br, typeCheckFailedLabel)

                    _builder.MarkLabel(typeCheckPassedLabel)
                End If

                ' define local if we have one
                Dim localOpt = catchBlock.LocalOpt
                If localOpt IsNot Nothing Then
                    ' TODO: this local can be released when we can release named locals.
                    Dim declNodes = localOpt.DeclaringSyntaxReferences
                    DefineLocal(localOpt, If(Not declNodes.IsEmpty, DirectCast(declNodes(0).GetSyntax(), VisualBasicSyntaxNode), catchBlock.Syntax))
                End If

                ' assign the exception variable if we have one
                If exceptionSource IsNot Nothing Then
                    If ShouldNoteProjectErrors() Then
                        _builder.EmitOpCode(ILOpCode.Dup)
                        EmitSetProjectError(catchBlock.Syntax, catchBlock.ErrorLineNumberOpt)
                    End If

                    ' here we have our exception on the stack in a form of a reference type (O)
                    ' it means that we have to "unbox" it before storing to the local 
                    ' if exception's type is a generic type parameter.
                    If exceptionSource.Type.IsTypeParameter Then
                        _builder.EmitOpCode(ILOpCode.Unbox_any)
                        EmitSymbolToken(exceptionSource.Type, exceptionSource.Syntax)
                    End If

                    ' TODO: parts of the following code is common with AssignmentExpression
                    '       the only major difference is that assignee is on the stack
                    '       consider factoring out common code.

                    While exceptionSource.Kind = BoundKind.Sequence
                        Dim seq = DirectCast(exceptionSource, BoundSequence)
                        EmitSideEffects(seq.SideEffects)
                        If seq.ValueOpt Is Nothing Then
                            Exit While
                        Else
                            exceptionSource = seq.ValueOpt
                        End If
                    End While

                    Select Case exceptionSource.Kind
                        Case BoundKind.Local
                            Debug.Assert(Not DirectCast(exceptionSource, BoundLocal).LocalSymbol.IsByRef)
                            _builder.EmitLocalStore(GetLocal(DirectCast(exceptionSource, BoundLocal)))

                        Case BoundKind.Parameter
                            Dim left = DirectCast(exceptionSource, BoundParameter)
                            ' When assigning to a byref param 
                            ' we need to push param address below the exception
                            If left.ParameterSymbol.IsByRef Then
                                Dim temp = AllocateTemp(exceptionSource.Type, exceptionSource.Syntax)
                                _builder.EmitLocalStore(temp)
                                _builder.EmitLoadArgumentOpcode(ParameterSlot(left))
                                _builder.EmitLocalLoad(temp)
                                FreeTemp(temp)
                            End If

                            EmitParameterStore(left)

                        Case BoundKind.FieldAccess
                            Dim left = DirectCast(exceptionSource, BoundFieldAccess)
                            If Not left.FieldSymbol.IsShared Then

                                Dim stateMachineField = TryCast(left.FieldSymbol, StateMachineFieldSymbol)
                                If (stateMachineField IsNot Nothing) AndAlso (stateMachineField.SlotIndex >= 0) Then
                                    DefineUserDefinedStateMachineHoistedLocal(stateMachineField)
                                End If

                                ' When assigning to a field
                                ' we need to push param address below the exception
                                Dim temp = AllocateTemp(exceptionSource.Type, exceptionSource.Syntax)
                                _builder.EmitLocalStore(temp)

                                Dim receiver = left.ReceiverOpt

                                ' EmitFieldReceiver will handle receivers with type parameter type,
                                ' but we do not know of a test case that will get here with receiver
                                ' of type T. The assert is here to catch such a case. If the assert
                                ' fails, remove the assert and add a corresponding test case.
                                Debug.Assert(receiver.Type.TypeKind <> TypeKind.TypeParameter)

                                Dim temp1 = EmitReceiverRef(receiver, isAccessConstrained:=False, addressKind:=AddressKind.[ReadOnly])
                                Debug.Assert(temp1 Is Nothing, "temp is unexpected when assigning to a field")

                                _builder.EmitLocalLoad(temp)
                            End If

                            EmitFieldStore(left)

                        Case Else
                            Throw ExceptionUtilities.UnexpectedValue(exceptionSource.Kind)
                    End Select
                Else
                    If ShouldNoteProjectErrors() Then
                        EmitSetProjectError(catchBlock.Syntax, catchBlock.ErrorLineNumberOpt)
                    Else
                        _builder.EmitOpCode(ILOpCode.Pop)
                    End If
                End If

                ' emit the actual filter expression, if we have one, 
                ' and normalize results
                If catchBlock.ExceptionFilterOpt IsNot Nothing Then
                    EmitCondExpr(catchBlock.ExceptionFilterOpt, True)
                    ' Normalize the return value because values other than 0 or 1 produce unspecified results.
                    _builder.EmitIntConstant(0)
                    _builder.EmitOpCode(ILOpCode.Cgt_un)
                    _builder.MarkLabel(typeCheckFailedLabel)

                    ' Now we are starting the actual handler
                    _builder.MarkFilterConditionEnd()

                    'pop the exception, it should have been already stored to the variable by the filter.
                    _builder.EmitOpCode(ILOpCode.Pop)
                End If
            End If

            ' emit actual handler body
            ' Note that it is a block so it will introduce its own scope for locals
            ' it should also have access to the exception local if we have declared one
            ' as that is scoped to the whole Catch/Filter
            EmitBlock(catchBlock.Body)

            ' if the end of handler is reachable we should clear project errors. 
            ' (if unreachable, this will not be emitted)
            If ShouldNoteProjectErrors() AndAlso
               (catchBlock.ExceptionFilterOpt Is Nothing OrElse catchBlock.ExceptionFilterOpt.Kind <> BoundKind.UnstructuredExceptionHandlingCatchFilter) Then
                EmitClearProjectError(catchBlock.Syntax)
            End If

            _builder.CloseLocalScope()

            _currentCatchBlock = oldCatchBlock
        End Sub

        ''' <summary>
        ''' Tells if we should emit [Set/Clear]ProjectErrors when entering/leaving handlers
        ''' </summary>
        Private Function ShouldNoteProjectErrors() As Boolean
            Return Not Me._module.SourceModule.ContainingSourceAssembly.IsVbRuntime
        End Function

        Private Sub EmitSetProjectError(syntaxNode As VisualBasicSyntaxNode, errorLineNumberOpt As BoundExpression)
            Dim setProjectErrorMethod As MethodSymbol

            If errorLineNumberOpt Is Nothing Then
                setProjectErrorMethod = DirectCast(Me._module.Compilation.GetWellKnownTypeMember(WellKnownMember.Microsoft_VisualBasic_CompilerServices_ProjectData__SetProjectError), MethodSymbol)
                ' consumes exception object from the stack
                _builder.EmitOpCode(ILOpCode.Call, -1)
            Else
                setProjectErrorMethod = DirectCast(Me._module.Compilation.GetWellKnownTypeMember(WellKnownMember.Microsoft_VisualBasic_CompilerServices_ProjectData__SetProjectError_Int32), MethodSymbol)
                EmitExpression(errorLineNumberOpt, used:=True)
                ' consumes exception object from the stack and the error line number
                _builder.EmitOpCode(ILOpCode.Call, -2)
            End If

            Me.EmitSymbolToken(setProjectErrorMethod, syntaxNode)
        End Sub

        Private Sub EmitClearProjectError(syntaxNode As VisualBasicSyntaxNode)
            Const clearProjectError As WellKnownMember = WellKnownMember.Microsoft_VisualBasic_CompilerServices_ProjectData__ClearProjectError
            Dim clearProjectErrorMethod = DirectCast(Me._module.Compilation.GetWellKnownTypeMember(clearProjectError), MethodSymbol)

            ' void static with no arguments
            _builder.EmitOpCode(ILOpCode.Call, 0)
            Me.EmitSymbolToken(clearProjectErrorMethod, syntaxNode)
        End Sub

        ' specifies whether emitted conditional expression was a constant true/false or not a constant
        Private Enum ConstResKind
            ConstFalse
            ConstTrue
            NotAConst
        End Enum

        Private Sub EmitConditionalGoto(boundConditionalGoto As BoundConditionalGoto)
            Dim label As Object = boundConditionalGoto.Label
            Debug.Assert(label IsNot Nothing)
            EmitCondBranch(boundConditionalGoto.Condition, label, boundConditionalGoto.JumpIfTrue)
        End Sub

        ' 3.17 The brfalse instruction transfers control to target if value (of type int32, int64, object reference, managed
        'pointer, unmanaged pointer or native int) is zero (false). If value is non-zero (true), execution continues at
        'the next instruction.
        Private Function CanPassToBrfalse(ts As TypeSymbol) As Boolean
            If ts.IsEnumType Then
                ' valid enums are all primitives
                Return True
            End If

            Dim tc = ts.PrimitiveTypeCode
            Select Case tc
                Case Cci.PrimitiveTypeCode.Float32, Cci.PrimitiveTypeCode.Float64
                    Return False

                Case Cci.PrimitiveTypeCode.NotPrimitive
                    ' if this is a generic type param, verifier will want us to box
                    ' EmitCondBranch knows that
                    Return ts.IsReferenceType

                Case Else
                    Debug.Assert(tc <> Cci.PrimitiveTypeCode.Invalid)
                    Debug.Assert(tc <> Cci.PrimitiveTypeCode.Void)
                    Return True
            End Select
        End Function

        Private Function TryReduce(condition As BoundBinaryOperator, ByRef sense As Boolean) As BoundExpression
            Dim opKind = condition.OperatorKind And BinaryOperatorKind.OpMask

            Debug.Assert(opKind = BinaryOperatorKind.Equals OrElse
                         opKind = BinaryOperatorKind.NotEquals OrElse
                         opKind = BinaryOperatorKind.Is OrElse
                         opKind = BinaryOperatorKind.IsNot)

            Dim nonConstOp As BoundExpression
            Dim constOp As ConstantValue = condition.Left.ConstantValueOpt

            If constOp IsNot Nothing Then
                nonConstOp = condition.Right
            Else
                constOp = condition.Right.ConstantValueOpt

                If constOp Is Nothing Then
                    Return Nothing
                End If

                nonConstOp = condition.Left
            End If

            Dim nonConstType = nonConstOp.Type

            Debug.Assert(nonConstType IsNot Nothing OrElse (nonConstOp.IsNothingLiteral() AndAlso (opKind = BinaryOperatorKind.Is OrElse opKind = BinaryOperatorKind.IsNot)))

            If nonConstType IsNot Nothing AndAlso Not CanPassToBrfalse(nonConstType) Then
                Return Nothing
            End If

            Dim isBool As Boolean = nonConstType IsNot Nothing AndAlso nonConstType.PrimitiveTypeCode = Microsoft.Cci.PrimitiveTypeCode.Boolean
            Dim isZero As Boolean = constOp.IsDefaultValue

            If Not isBool AndAlso Not isZero Then
                Return Nothing
            End If

            If isZero Then
                sense = Not sense
            End If

            If opKind = BinaryOperatorKind.NotEquals OrElse opKind = BinaryOperatorKind.IsNot Then
                sense = Not sense
            End If

            Return nonConstOp
        End Function

        Private Const s_IL_OP_CODE_ROW_LENGTH = 4

        '    //  <            <=               >                >=
        '    ILOpCode.Blt,    ILOpCode.Ble,    ILOpCode.Bgt,    ILOpCode.Bge,     // Signed
        '    ILOpCode.Bge,    ILOpCode.Bgt,    ILOpCode.Ble,    ILOpCode.Blt,     // Signed Invert
        '    ILOpCode.Blt_un, ILOpCode.Ble_un, ILOpCode.Bgt_un, ILOpCode.Bge_un,  // Unsigned
        '    ILOpCode.Bge_un, ILOpCode.Bgt_un, ILOpCode.Ble_un, ILOpCode.Blt_un,  // Unsigned Invert
        '    ILOpCode.Blt,    ILOpCode.Ble,    ILOpCode.Bgt,    ILOpCode.Bge,     // Float
        '    ILOpCode.Bge_un, ILOpCode.Bgt_un, ILOpCode.Ble_un, ILOpCode.Blt_un,  // Float Invert

        Private Shared ReadOnly s_condJumpOpCodes As ILOpCode() = New ILOpCode() {
            ILOpCode.Blt, ILOpCode.Ble, ILOpCode.Bgt, ILOpCode.Bge,
            ILOpCode.Bge, ILOpCode.Bgt, ILOpCode.Ble, ILOpCode.Blt,
            ILOpCode.Blt_un, ILOpCode.Ble_un, ILOpCode.Bgt_un, ILOpCode.Bge_un,
            ILOpCode.Bge_un, ILOpCode.Bgt_un, ILOpCode.Ble_un, ILOpCode.Blt_un,
            ILOpCode.Blt, ILOpCode.Ble, ILOpCode.Bgt, ILOpCode.Bge,
            ILOpCode.Bge_un, ILOpCode.Bgt_un, ILOpCode.Ble_un, ILOpCode.Blt_un}

        Private Function CodeForJump(expression As BoundBinaryOperator, sense As Boolean, <Out()> ByRef revOpCode As ILOpCode) As ILOpCode
            Dim opIdx As Integer
            Dim opKind = (expression.OperatorKind And BinaryOperatorKind.OpMask)
            Dim operandType = expression.Left.Type

            Debug.Assert(operandType IsNot Nothing OrElse (expression.Left.IsNothingLiteral() AndAlso (opKind = BinaryOperatorKind.Is OrElse opKind = BinaryOperatorKind.IsNot)))

            If operandType IsNot Nothing AndAlso operandType.IsBooleanType() Then
                ' Since VB True is -1 but is stored as 1 in IL, relational operations on Boolean must
                ' be reversed to yield the correct results. Note that = and <> do not need reversal.
                Select Case opKind
                    Case BinaryOperatorKind.LessThan
                        opKind = BinaryOperatorKind.GreaterThan
                    Case BinaryOperatorKind.LessThanOrEqual
                        opKind = BinaryOperatorKind.GreaterThanOrEqual
                    Case BinaryOperatorKind.GreaterThan
                        opKind = BinaryOperatorKind.LessThan
                    Case BinaryOperatorKind.GreaterThanOrEqual
                        opKind = BinaryOperatorKind.LessThanOrEqual
                End Select
            End If

            Select Case opKind
                Case BinaryOperatorKind.IsNot
                    ValidateReferenceEqualityOperands(expression)
                    Return If(sense, ILOpCode.Bne_un, ILOpCode.Beq)

                Case BinaryOperatorKind.Is
                    ValidateReferenceEqualityOperands(expression)
                    Return If(sense, ILOpCode.Beq, ILOpCode.Bne_un)

                Case BinaryOperatorKind.Equals
                    Return If(sense, ILOpCode.Beq, ILOpCode.Bne_un)

                Case BinaryOperatorKind.NotEquals
                    Return If(sense, ILOpCode.Bne_un, ILOpCode.Beq)

                Case BinaryOperatorKind.LessThan
                    opIdx = 0

                Case BinaryOperatorKind.LessThanOrEqual
                    opIdx = 1

                Case BinaryOperatorKind.GreaterThan
                    opIdx = 2

                Case BinaryOperatorKind.GreaterThanOrEqual
                    opIdx = 3

                Case Else
                    Throw ExceptionUtilities.UnexpectedValue(opKind)
            End Select

            If operandType IsNot Nothing Then
                If operandType.IsUnsignedIntegralType() Then
                    opIdx += 2 * s_IL_OP_CODE_ROW_LENGTH 'unsigned
                Else
                    If operandType.IsFloatingType() Then
                        opIdx += 4 * s_IL_OP_CODE_ROW_LENGTH 'float
                    End If
                End If
            End If

            Dim revOpIdx = opIdx

            If Not sense Then
                opIdx += s_IL_OP_CODE_ROW_LENGTH 'invert op
            Else
                revOpIdx += s_IL_OP_CODE_ROW_LENGTH 'invert orev
            End If

            revOpCode = s_condJumpOpCodes(revOpIdx)
            Return s_condJumpOpCodes(opIdx)
        End Function

        ' generate a jump to dest if (condition == sense) is true
        ' it is ok if lazyDest is Nothing
        ' if lazyDest is needed it will be initialized to a new object
        Private Sub EmitCondBranch(condition As BoundExpression, ByRef lazyDest As Object, sense As Boolean)
            _recursionDepth += 1

            If _recursionDepth > 1 Then
                StackGuard.EnsureSufficientExecutionStack(_recursionDepth)

                EmitCondBranchCore(condition, lazyDest, sense)
            Else
                EmitCondBranchCoreWithStackGuard(condition, lazyDest, sense)
            End If

            _recursionDepth -= 1
        End Sub

        Private Sub EmitCondBranchCoreWithStackGuard(condition As BoundExpression, ByRef lazyDest As Object, sense As Boolean)
            Debug.Assert(_recursionDepth = 1)

            Try
                EmitCondBranchCore(condition, lazyDest, sense)
                Debug.Assert(_recursionDepth = 1)

            Catch ex As Exception When StackGuard.IsInsufficientExecutionStackException(ex)
                _diagnostics.Add(ERRID.ERR_TooLongOrComplexExpression,
                                 BoundTreeVisitor.CancelledByStackGuardException.GetTooLongOrComplexExpressionErrorLocation(condition))
                Throw New EmitCancelledException()
            End Try
        End Sub

        Private Sub EmitCondBranchCore(condition As BoundExpression, ByRef lazyDest As Object, sense As Boolean)
oneMoreTime:
            Dim ilcode As ILOpCode
            Dim constExprValue = condition.ConstantValueOpt

            If constExprValue IsNot Nothing Then
                ' make sure that only the bool bits are set or it is a Nothing literal 
                ' or it is a string literal in which case it is equal to True
                Debug.Assert(constExprValue.Discriminator = ConstantValueTypeDiscriminator.Boolean OrElse
                             constExprValue.Discriminator = ConstantValueTypeDiscriminator.String OrElse
                             constExprValue.IsNothing)

                Dim taken As Boolean = constExprValue.IsDefaultValue <> sense
                If taken Then
                    lazyDest = If(lazyDest, New Object())
                    _builder.EmitBranch(ILOpCode.Br, lazyDest)
                Else
                    ' otherwise this branch will never be taken, so just fall through...
                End If

                Return
            End If

            Select Case condition.Kind
                Case BoundKind.BinaryOperator
                    Dim binOp = DirectCast(condition, BoundBinaryOperator)
                    Dim testBothArgs As Boolean = sense

                    Select Case binOp.OperatorKind And BinaryOperatorKind.OpMask
                        Case BinaryOperatorKind.OrElse
                            testBothArgs = Not testBothArgs
                            GoTo BinaryOperatorKindLogicalAnd

                        Case BinaryOperatorKind.AndAlso
BinaryOperatorKindLogicalAnd:
                            If testBothArgs Then
                                ' gotoif(a != sense) fallThrough
                                ' gotoif(b == sense) dest
                                ' fallThrough:
                                Dim lazyFallThrough = New Object
                                EmitCondBranch(binOp.Left, lazyFallThrough, Not sense)
                                EmitCondBranch(binOp.Right, lazyDest, sense)

                                If (lazyFallThrough IsNot Nothing) Then
                                    _builder.MarkLabel(lazyFallThrough)
                                End If
                            Else
                                EmitCondBranch(binOp.Left, lazyDest, sense)
                                condition = binOp.Right
                                GoTo oneMoreTime
                            End If
                            Return

                        Case BinaryOperatorKind.IsNot,
                             BinaryOperatorKind.Is

                            ValidateReferenceEqualityOperands(binOp)
                            GoTo BinaryOperatorKindEqualsNotEquals

                        Case BinaryOperatorKind.Equals,
                             BinaryOperatorKind.NotEquals
BinaryOperatorKindEqualsNotEquals:

                            Dim reduced = TryReduce(binOp, sense)
                            If reduced IsNot Nothing Then
                                condition = reduced
                                GoTo oneMoreTime
                            End If
                            GoTo BinaryOperatorKindLessThan

                        Case BinaryOperatorKind.LessThan,
                             BinaryOperatorKind.LessThanOrEqual,
                             BinaryOperatorKind.GreaterThan,
                             BinaryOperatorKind.GreaterThanOrEqual

BinaryOperatorKindLessThan:
                            EmitExpression(binOp.Left, True)
                            EmitExpression(binOp.Right, True)
                            Dim revOpCode As ILOpCode
                            ilcode = CodeForJump(binOp, sense, revOpCode)
                            lazyDest = If(lazyDest, New Object())
                            _builder.EmitBranch(ilcode, lazyDest, revOpCode)
                            Return
                    End Select

                    ' none of above. 
                    ' then it is regular binary expression - Or, And, Xor ...
                    GoTo OtherExpressions

                Case BoundKind.UnaryOperator
                    Dim unOp = DirectCast(condition, BoundUnaryOperator)
                    If (unOp.OperatorKind = UnaryOperatorKind.Not) Then
                        Debug.Assert(unOp.Type.IsBooleanType())
                        sense = Not sense
                        condition = unOp.Operand
                        GoTo oneMoreTime
                    Else
                        GoTo OtherExpressions
                    End If


                Case BoundKind.TypeOf

                    Dim typeOfExpression = DirectCast(condition, BoundTypeOf)

                    EmitTypeOfExpression(typeOfExpression, used:=True, optimize:=True)

                    If typeOfExpression.IsTypeOfIsNotExpression Then
                        sense = Not sense
                    End If

                    ilcode = If(sense, ILOpCode.Brtrue, ILOpCode.Brfalse)
                    lazyDest = If(lazyDest, New Object())
                    _builder.EmitBranch(ilcode, lazyDest)
                    Return

#If False Then
                Case BoundKind.AsOperator
                    Dim asOp = DirectCast(condition, BoundIsOperator)
                    EmitExpression(asOp.Operand, True)
                    _builder.EmitOpCode(ILOpCode.Isinst)
                    EmitSymbolToken(asOp.TargetType.Type)
                    ilcode = If(sense, ILOpCode.Brtrue, ILOpCode.Brfalse)
                    _builder.EmitBranch(ilcode, dest)
                    Return
#End If
                Case BoundKind.Sequence
                    Dim sequence = DirectCast(condition, BoundSequence)
                    EmitSequenceCondBranch(sequence, lazyDest, sense)
                    Return

                Case Else
OtherExpressions:
                    EmitExpression(condition, True)

                    Dim conditionType = condition.Type
                    If conditionType.IsReferenceType AndAlso Not IsVerifierReference(conditionType) Then
                        EmitBox(conditionType, condition.Syntax)
                    End If

                    ilcode = If(sense, ILOpCode.Brtrue, ILOpCode.Brfalse)
                    lazyDest = If(lazyDest, New Object())
                    _builder.EmitBranch(ilcode, lazyDest)
                    Return
            End Select
        End Sub

        <Conditional("DEBUG")>
        Private Sub ValidateReferenceEqualityOperands(binOp As BoundBinaryOperator)
            Debug.Assert(binOp.Left.IsNothingLiteral() OrElse binOp.Left.Type.SpecialType = SpecialType.System_Object OrElse binOp.WasCompilerGenerated)
            Debug.Assert(binOp.Right.IsNothingLiteral() OrElse binOp.Right.Type.SpecialType = SpecialType.System_Object OrElse binOp.WasCompilerGenerated)
        End Sub

        'TODO: is this to fold value? Same in C#?
        Private Sub EmitSequenceCondBranch(sequence As BoundSequence, ByRef lazyDest As Object, sense As Boolean)
            Dim hasLocals As Boolean = Not sequence.Locals.IsEmpty
            If hasLocals Then
                _builder.OpenLocalScope()

                For Each local In sequence.Locals
                    Me.DefineLocal(local, sequence.Syntax)
                Next
            End If

            Me.EmitSideEffects(sequence.SideEffects)
            Debug.Assert(sequence.ValueOpt IsNot Nothing)
            Me.EmitCondBranch(sequence.ValueOpt, lazyDest, sense)

            If hasLocals Then
                _builder.CloseLocalScope()

                For Each local In sequence.Locals
                    Me.FreeLocal(local)
                Next
            End If
        End Sub

        Private Sub EmitLabelStatement(boundLabelStatement As BoundLabelStatement)
            _builder.MarkLabel(boundLabelStatement.Label)
        End Sub

        Private Sub EmitGotoStatement(boundGotoStatement As BoundGotoStatement)
            ' if branch leaves current Catch block we need to emit ClearProjectError()
            If ShouldNoteProjectErrors() Then
                If _currentCatchBlock IsNot Nothing AndAlso
                    (_currentCatchBlock.ExceptionFilterOpt Is Nothing OrElse _currentCatchBlock.ExceptionFilterOpt.Kind <> BoundKind.UnstructuredExceptionHandlingCatchFilter) AndAlso
                    Not LabelFinder.NodeContainsLabel(_currentCatchBlock, boundGotoStatement.Label) Then

                    EmitClearProjectError(boundGotoStatement.Syntax)

                End If
            End If

            _builder.EmitBranch(ILOpCode.Br, boundGotoStatement.Label)
        End Sub

        ''' <summary>
        ''' tells if given node contains a label statement that defines given label symbol
        ''' </summary>
        Private Class LabelFinder
            Inherits StatementWalker

            Private ReadOnly _label As LabelSymbol
            Private _found As Boolean = False

            Private Sub New(label As LabelSymbol)
                Me._label = label
            End Sub

            Public Overrides Function Visit(node As BoundNode) As BoundNode
                If Not _found AndAlso TypeOf node IsNot BoundExpression Then
                    Return MyBase.Visit(node)
                End If

                Return Nothing
            End Function

            Public Overrides Function VisitLabelStatement(node As BoundLabelStatement) As BoundNode
                If node.Label Is Me._label Then
                    _found = True
                End If

                Return MyBase.VisitLabelStatement(node)
            End Function

            Public Shared Function NodeContainsLabel(node As BoundNode, label As LabelSymbol) As Boolean
                Dim finder = New LabelFinder(label)
                finder.Visit(node)

                Return finder._found
            End Function
        End Class

        Private Sub EmitReturnStatement(boundReturnStatement As BoundReturnStatement)
            Me.EmitExpression(boundReturnStatement.ExpressionOpt, True)
            _builder.EmitRet(boundReturnStatement.ExpressionOpt Is Nothing)
        End Sub

        Private Sub EmitThrowStatement(boundThrowStatement As BoundThrowStatement)
            Dim operand = boundThrowStatement.ExpressionOpt

            If operand IsNot Nothing Then
                EmitExpression(operand, used:=True)

                Dim operandType = operand.Type
                ' "Throw Nothing" is not supported by the language
                ' so operand.Type should always be set.
                Debug.Assert(operandType IsNot Nothing)
                If (operandType IsNot Nothing) AndAlso (operandType.TypeKind = TypeKind.TypeParameter) Then
                    EmitBox(operandType, operand.Syntax)
                End If
            End If

            _builder.EmitThrow(operand Is Nothing)
        End Sub

        Private Sub EmitSelectStatement(boundSelectStatement As BoundSelectStatement)
            Debug.Assert(boundSelectStatement.RecommendSwitchTable)

            Dim selectExpression = boundSelectStatement.ExpressionStatement.Expression
            Dim caseBlocks = boundSelectStatement.CaseBlocks
            Dim exitLabel = boundSelectStatement.ExitLabel
            Dim fallThroughLabel = exitLabel

            Debug.Assert(selectExpression.Type IsNot Nothing)
            Debug.Assert(caseBlocks.Any())

            ' Create labels for case blocks
            Dim caseBlockLabels As ImmutableArray(Of GeneratedLabelSymbol) = CreateCaseBlockLabels(caseBlocks)

            ' Create an array of key value pairs (key: case clause constant value, value: case block label)
            ' for emitting switch table based header.
            ' This function also ensures the correct fallThroughLabel is set, i.e. case else block label if one exists, otherwise exit label.
            Dim caseLabelsForEmit As KeyValuePair(Of ConstantValue, Object)() = GetCaseLabelsForEmitSwitchHeader(caseBlocks, caseBlockLabels, fallThroughLabel)

            ' Emit switch table header
            EmitSwitchTableHeader(selectExpression, caseLabelsForEmit, fallThroughLabel)

            ' Emit case blocks
            EmitCaseBlocks(caseBlocks, caseBlockLabels, exitLabel)

            ' Emit exit label
            _builder.MarkLabel(exitLabel)
        End Sub

        ' Create a label for each case block
        Private Function CreateCaseBlockLabels(caseBlocks As ImmutableArray(Of BoundCaseBlock)) As ImmutableArray(Of GeneratedLabelSymbol)
            Debug.Assert(Not caseBlocks.IsEmpty)

            Dim caseBlockLabels = ArrayBuilder(Of GeneratedLabelSymbol).GetInstance(caseBlocks.Length)
            Dim cur As Integer = 0

            For Each caseBlock In caseBlocks
                cur = cur + 1
                caseBlockLabels.Add(New GeneratedLabelSymbol("Case Block " + cur.ToString()))
            Next

            Return caseBlockLabels.ToImmutableAndFree()
        End Function

        ' Creates an array of key value pairs (key: case clause constant value, value: case block label)
        ' for emitting switch table based header.
        ' This function also ensures the correct fallThroughLabel is set, i.e. case else block label if one exists, otherwise exit label.
        Private Function GetCaseLabelsForEmitSwitchHeader(
            caseBlocks As ImmutableArray(Of BoundCaseBlock),
            caseBlockLabels As ImmutableArray(Of GeneratedLabelSymbol),
            ByRef fallThroughLabel As LabelSymbol
        ) As KeyValuePair(Of ConstantValue, Object)()

            Debug.Assert(Not caseBlocks.IsEmpty)
            Debug.Assert(Not caseBlockLabels.IsEmpty)
            Debug.Assert(caseBlocks.Length = caseBlockLabels.Length)

            Dim labelsBuilder = ArrayBuilder(Of KeyValuePair(Of ConstantValue, Object)).GetInstance()
            Dim constantsSet = New HashSet(Of ConstantValue)(New SwitchConstantValueHelper.SwitchLabelsComparer())

            Dim cur As Integer = 0

            For Each caseBlock In caseBlocks
                Dim caseBlockLabel = caseBlockLabels(cur)
                Dim caseClauses = caseBlock.CaseStatement.CaseClauses

                If caseClauses.Any() Then
                    For Each caseClause In caseClauses
                        Dim constant As ConstantValue

                        Select Case caseClause.Kind
                            Case BoundKind.SimpleCaseClause
                                Dim simpleCaseClause = DirectCast(caseClause, BoundSimpleCaseClause)

                                Debug.Assert(simpleCaseClause.ValueOpt IsNot Nothing)
                                Debug.Assert(simpleCaseClause.ConditionOpt Is Nothing)

                                constant = simpleCaseClause.ValueOpt.ConstantValueOpt

                            Case BoundKind.RelationalCaseClause
                                Dim relationalCaseClause = DirectCast(caseClause, BoundRelationalCaseClause)

                                Debug.Assert(relationalCaseClause.OperatorKind = BinaryOperatorKind.Equals)
                                Debug.Assert(relationalCaseClause.OperandOpt IsNot Nothing)
                                Debug.Assert(relationalCaseClause.ConditionOpt Is Nothing)

                                constant = relationalCaseClause.OperandOpt.ConstantValueOpt

                            Case BoundKind.RangeCaseClause
                                ' TODO: For now we use IF lists if we encounter
                                ' TODO: BoundRangeCaseClause, we should not reach here.
                                Throw ExceptionUtilities.UnexpectedValue(caseClause.Kind)

                            Case Else
                                Throw ExceptionUtilities.UnexpectedValue(caseClause.Kind)
                        End Select

                        Debug.Assert(constant IsNot Nothing)
                        Debug.Assert(SwitchConstantValueHelper.IsValidSwitchCaseLabelConstant(constant))

                        ' If we have duplicate case value constants, use the lexically first case constant.
                        If Not constantsSet.Contains(constant) Then
                            labelsBuilder.Add(New KeyValuePair(Of ConstantValue, Object)(constant, caseBlockLabel))
                            constantsSet.Add(constant)
                        End If
                    Next
                Else
                    Debug.Assert(caseBlock.Syntax.Kind = SyntaxKind.CaseElseBlock)

                    ' We have a case else block, update the fallThroughLabel to the corresponding caseBlockLabel
                    fallThroughLabel = caseBlockLabel
                End If

                cur = cur + 1
            Next

            Return labelsBuilder.ToArrayAndFree()
        End Function

        Private Sub EmitSwitchTableHeader(selectExpression As BoundExpression, caseLabels As KeyValuePair(Of ConstantValue, Object)(), fallThroughLabel As LabelSymbol)
            Debug.Assert(selectExpression.Type IsNot Nothing)
            Debug.Assert(caseLabels IsNot Nothing)

            If Not caseLabels.Any() Then
                ' No case labels, emit branch to fallThroughLabel
                _builder.EmitBranch(ILOpCode.Br, fallThroughLabel)
            Else

                Dim exprType = selectExpression.Type
                Dim temp As LocalDefinition = Nothing

                If exprType.SpecialType <> SpecialType.System_String Then
                    If selectExpression.Kind = BoundKind.Local AndAlso Not DirectCast(selectExpression, BoundLocal).LocalSymbol.IsByRef Then
                        _builder.EmitIntegerSwitchJumpTable(caseLabels, fallThroughLabel, GetLocal(DirectCast(selectExpression, BoundLocal)), keyTypeCode:=exprType.GetEnumUnderlyingTypeOrSelf.PrimitiveTypeCode)

                    ElseIf selectExpression.Kind = BoundKind.Parameter AndAlso Not DirectCast(selectExpression, BoundParameter).ParameterSymbol.IsByRef Then
                        _builder.EmitIntegerSwitchJumpTable(caseLabels, fallThroughLabel, ParameterSlot(DirectCast(selectExpression, BoundParameter)), keyTypeCode:=exprType.GetEnumUnderlyingTypeOrSelf.PrimitiveTypeCode)

                    Else
                        EmitExpression(selectExpression, True)
                        temp = AllocateTemp(exprType, selectExpression.Syntax)
                        _builder.EmitLocalStore(temp)

                        _builder.EmitIntegerSwitchJumpTable(caseLabels, fallThroughLabel, temp, keyTypeCode:=exprType.GetEnumUnderlyingTypeOrSelf.PrimitiveTypeCode)
                    End If
                Else
                    If selectExpression.Kind = BoundKind.Local AndAlso Not DirectCast(selectExpression, BoundLocal).LocalSymbol.IsByRef Then
                        EmitStringSwitchJumpTable(caseLabels, fallThroughLabel, GetLocal(DirectCast(selectExpression, BoundLocal)), selectExpression.Syntax)

                    Else
                        EmitExpression(selectExpression, True)
                        temp = AllocateTemp(exprType, selectExpression.Syntax)
                        _builder.EmitLocalStore(temp)

                        EmitStringSwitchJumpTable(caseLabels, fallThroughLabel, temp, selectExpression.Syntax)
                    End If
                End If

                If temp IsNot Nothing Then
                    FreeTemp(temp)
                End If
            End If
        End Sub

        Private Sub EmitStringSwitchJumpTable(caseLabels As KeyValuePair(Of ConstantValue, Object)(), fallThroughLabel As LabelSymbol, key As LocalDefinition, syntaxNode As VisualBasicSyntaxNode)
            Dim genHashTableSwitch As Boolean = SwitchStringJumpTableEmitter.ShouldGenerateHashTableSwitch(_module, caseLabels.Length)
            Dim keyHash As LocalDefinition = Nothing

            If genHashTableSwitch Then
                Debug.Assert(_module.SupportsPrivateImplClass)
                Dim privateImplClass = _module.GetPrivateImplClass(syntaxNode, _diagnostics)
                Dim stringHashMethodRef As Microsoft.Cci.IReference = privateImplClass.GetMethod(PrivateImplementationDetails.SynthesizedStringHashFunctionName)
                Debug.Assert(stringHashMethodRef IsNot Nothing)

                ' static uint ComputeStringHash(string s)
                ' pop 1 (s)
                ' push 1 (uint return value)
                ' stackAdjustment = (pushCount - popCount) = 0

                _builder.EmitLocalLoad(key)
                _builder.EmitOpCode(ILOpCode.[Call], stackAdjustment:=0)
                _builder.EmitToken(stringHashMethodRef, syntaxNode, _diagnostics)

                Dim UInt32Type = DirectCast(_module.GetSpecialType(SpecialType.System_UInt32, syntaxNode, _diagnostics), TypeSymbol)
                keyHash = AllocateTemp(UInt32Type, syntaxNode)

                _builder.EmitLocalStore(keyHash)
            End If

            ' Prefer embedded version of the member if present
            Dim embeddedOperatorsType As NamedTypeSymbol = Me._module.Compilation.GetWellKnownType(WellKnownType.Microsoft_VisualBasic_CompilerServices_EmbeddedOperators)
            Dim compareStringMember As WellKnownMember =
                If(embeddedOperatorsType.IsErrorType AndAlso TypeOf embeddedOperatorsType Is MissingMetadataTypeSymbol,
                   WellKnownMember.Microsoft_VisualBasic_CompilerServices_Operators__CompareStringStringStringBoolean,
                   WellKnownMember.Microsoft_VisualBasic_CompilerServices_EmbeddedOperators__CompareStringStringStringBoolean)

            Dim stringCompareMethod = DirectCast(Me._module.Compilation.GetWellKnownTypeMember(compareStringMember), MethodSymbol)
            Dim stringCompareMethodRef As Cci.IReference = Me._module.Translate(stringCompareMethod, needDeclaration:=False, syntaxNodeOpt:=syntaxNode, diagnostics:=_diagnostics)

            Dim compareDelegate As SwitchStringJumpTableEmitter.EmitStringCompareAndBranch =
                Sub(keyArg, stringConstant, targetLabel)
                    EmitStringCompareAndBranch(keyArg, syntaxNode, stringConstant, targetLabel, stringCompareMethodRef)
                End Sub

            _builder.EmitStringSwitchJumpTable(
                            caseLabels,
                            fallThroughLabel,
                            key,
                            keyHash,
                            compareDelegate,
                            AddressOf SynthesizedStringSwitchHashMethod.ComputeStringHash)

            If keyHash IsNot Nothing Then
                FreeTemp(keyHash)
            End If
        End Sub

        ''' <summary>
        ''' Delegate to emit string compare call and conditional branch based on the compare result.
        ''' </summary>
        ''' <param name="key">Key to compare</param>
        ''' <param name="syntaxNode">Node for diagnostics</param>
        ''' <param name="stringConstant">Case constant to compare the key against</param>
        ''' <param name="targetLabel">Target label to branch to if key = stringConstant</param>
        ''' <param name="stringCompareMethodRef">String equality method</param>
        Private Sub EmitStringCompareAndBranch(key As LocalOrParameter, syntaxNode As SyntaxNode, stringConstant As ConstantValue, targetLabel As Object, stringCompareMethodRef As Microsoft.Cci.IReference)
            ' Emit compare and branch:

            ' If key = stringConstant Then
            '      Goto targetLabel
            ' End If

            Debug.Assert(stringCompareMethodRef IsNot Nothing)

#If DEBUG Then
            Dim assertDiagnostics = DiagnosticBag.GetInstance()
            Debug.Assert(stringCompareMethodRef Is Me._module.Translate(DirectCast(
                                                        If(TypeOf Me._module.Compilation.GetWellKnownType(WellKnownType.Microsoft_VisualBasic_CompilerServices_EmbeddedOperators) Is MissingMetadataTypeSymbol,
                                                           Me._module.Compilation.GetWellKnownTypeMember(WellKnownMember.Microsoft_VisualBasic_CompilerServices_Operators__CompareStringStringStringBoolean),
                                                           Me._module.Compilation.GetWellKnownTypeMember(WellKnownMember.Microsoft_VisualBasic_CompilerServices_EmbeddedOperators__CompareStringStringStringBoolean)), MethodSymbol), needDeclaration:=False,
                                                           syntaxNodeOpt:=DirectCast(syntaxNode, VisualBasicSyntaxNode), diagnostics:=assertDiagnostics))
            assertDiagnostics.Free()
#End If

            ' Public Shared Function CompareString(Left As String, Right As String, TextCompare As Boolean) As Integer
            ' pop 3 (Left, Right, TextCompare)
            ' push 1 (Integer return value)

            ' stackAdjustment = (pushCount - popCount) = -2

            ' NOTE: We generate string switch table only for Option Compare Binary, i.e. TextCompare = False

            _builder.EmitLoad(key)
            _builder.EmitConstantValue(stringConstant)
            _builder.EmitConstantValue(ConstantValue.False)
            _builder.EmitOpCode(ILOpCode.Call, stackAdjustment:=-2)
            _builder.EmitToken(stringCompareMethodRef, syntaxNode, _diagnostics)

            ' CompareString returns 0 if Left and Right strings are equal.
            ' Branch to targetLabel if CompareString returned 0.
            _builder.EmitBranch(ILOpCode.Brfalse, targetLabel, ILOpCode.Brtrue)
        End Sub

        Private Sub EmitCaseBlocks(caseBlocks As ImmutableArray(Of BoundCaseBlock), caseBlockLabels As ImmutableArray(Of GeneratedLabelSymbol), exitLabel As LabelSymbol)
            Debug.Assert(Not caseBlocks.IsEmpty)
            Debug.Assert(Not caseBlockLabels.IsEmpty)
            Debug.Assert(caseBlocks.Length = caseBlockLabels.Length)

            Dim cur As Integer = 0
            For Each caseBlock In caseBlocks
                ' Emit case block label
                _builder.MarkLabel(caseBlockLabels(cur))
                cur = cur + 1

                ' Emit case statement sequence point

                Dim caseStatement = caseBlock.CaseStatement
                If Not caseStatement.WasCompilerGenerated Then
                    Debug.Assert(caseStatement.Syntax IsNot Nothing)

                    If _emitPdbSequencePoints Then
                        EmitSequencePoint(caseStatement.Syntax)
                    End If

                    If _ilEmitStyle = ILEmitStyle.Debug Then
                        ' Emit nop for the case statement otherwise the above sequence point
                        ' will get associated with the first statement in subsequent case block.
                        ' This matches the native compiler codegen.
                        _builder.EmitOpCode(ILOpCode.Nop)
                    End If
                End If

                ' Emit case block body
                EmitBlock(caseBlock.Body)

                ' Emit a branch to exit label
                _builder.EmitBranch(ILOpCode.Br, exitLabel)
            Next
        End Sub

        Private Sub EmitBlock(scope As BoundBlock)
            Dim hasLocals As Boolean = Not scope.Locals.IsEmpty
            If hasLocals Then
                _builder.OpenLocalScope()

                For Each local In scope.Locals
                    Dim declNodes = local.DeclaringSyntaxReferences
                    Me.DefineLocal(local, If(declNodes.IsEmpty, scope.Syntax, declNodes(0).GetVisualBasicSyntax()))
                Next
            End If

            For Each statement In scope.Statements
                EmitStatement(statement)
            Next

            If hasLocals Then
                _builder.CloseLocalScope()
                'TODO: can we free any locals here? Perhaps nameless temps?
            End If
        End Sub

        Private Function DefineLocal(local As LocalSymbol, syntaxNode As VisualBasicSyntaxNode) As LocalDefinition
            Dim specType = local.Type.SpecialType

            ' We're treating constants of type Decimal and DateTime as local here to not create a new instance for each time
            ' the value is accessed. This means there will be one local in the scope for this constant.
            ' This has the side effect that this constant will later on appear in the PDB file as a common local and one is able
            ' to modify the value in the debugger (which is a regression from Dev10).
            ' To fix this while keeping the behavior of having just one local for the const, one would need to preserve the 
            ' information that this local is a ConstantButNotMetadataConstant (update ScopeManager.DeclareLocal & LocalDefinition)
            ' and modify PEWriter.Initialize to DefineLocalConstant instead of DefineLocalVariable if the local is 
            ' ConstantButNotMetadataConstant.
            ' See bug #11047

            If local.HasConstantValue Then
                Dim compileTimeValue As MetadataConstant = _module.CreateConstant(local.Type, local.ConstantValue, syntaxNode, _diagnostics)
                Dim localConstantDef = New LocalConstantDefinition(local.Name, If(local.Locations.FirstOrDefault(), Location.None), compileTimeValue)
                ' Reference in the scope for debugging purpose
                _builder.AddLocalConstantToScope(localConstantDef)
                Return Nothing
            End If

            If Me.IsStackLocal(local) Then
                Return Nothing
            End If

            Dim translatedType = _module.Translate(local.Type, syntaxNodeOpt:=syntaxNode, diagnostics:=_diagnostics)
            ' Even though we don't need the token immediately, we will need it later when signature for the local is emitted.
            ' Also, requesting the token has side-effect of registering types used, which is critical for embedded types (NoPia, VBCore, etc).
            _module.GetFakeSymbolTokenForIL(translatedType, syntaxNode, _diagnostics)

            Dim constraints = If(local.IsByRef, LocalSlotConstraints.ByRef, LocalSlotConstraints.None) Or
                If(local.IsPinned, LocalSlotConstraints.Pinned, LocalSlotConstraints.None)

            Dim localId As LocalDebugId = Nothing
            Dim name As String = GetLocalDebugName(local, localId)

            Dim synthesizedKind = local.SynthesizedKind
            Dim localDef = _builder.LocalSlotManager.DeclareLocal(
                type:=translatedType,
                symbol:=local,
                name:=name,
                kind:=synthesizedKind,
                id:=localId,
                pdbAttributes:=synthesizedKind.PdbAttributes(),
                constraints:=constraints,
                isDynamic:=False,
                dynamicTransformFlags:=Nothing,
                isSlotReusable:=synthesizedKind.IsSlotReusable(_ilEmitStyle <> ILEmitStyle.Release))

            ' If named, add it to the local debug scope.
            If localDef.Name IsNot Nothing Then
                _builder.AddLocalToScope(localDef)
            End If

            Return localDef
        End Function

        ''' <summary>
        ''' Gets the name And id of the local that are going to be generated into the debug metadata.
        ''' </summary>
        Private Function GetLocalDebugName(local As LocalSymbol, <Out> ByRef localId As LocalDebugId) As String
            localId = LocalDebugId.None

            If local.IsImportedFromMetadata Then
                Return local.Name
            End If

            ' We include function value locals in async and iterator methods so that appropriate
            ' errors can be reported when users attempt to refer to them.  However, there's no
            ' reason to actually emit them into the resulting MoveNext method, because they will
            ' never be accessed.  Unfortunately, for implementation-specific reasons, dropping them
            ' would be non-trivial.  Instead, we drop their names so that they do not appear while
            ' debugging (esp in the Locals window).
            If local.DeclarationKind = LocalDeclarationKind.FunctionValue AndAlso
                TypeOf _method Is SynthesizedStateMachineMethod Then
                Return Nothing
            End If

            Dim localKind = local.SynthesizedKind

            ' only user-defined locals should be named during lowering:
            Debug.Assert((local.Name Is Nothing) = (localKind <> SynthesizedLocalKind.UserDefined))

            If Not localKind.IsLongLived() Then
                Return Nothing
            End If

            If _ilEmitStyle = ILEmitStyle.Debug Then
                Dim syntax = local.GetDeclaratorSyntax()
                Dim syntaxOffset = _method.CalculateLocalSyntaxOffset(syntax.SpanStart, syntax.SyntaxTree)

                Dim ordinal = _synthesizedLocalOrdinals.AssignLocalOrdinal(localKind, syntaxOffset)

                ' user-defined locals should have 0 ordinal
                Debug.Assert(ordinal = 0 OrElse localKind <> SynthesizedLocalKind.UserDefined)

                localId = New LocalDebugId(syntaxOffset, ordinal)
            End If

            If local.Name IsNot Nothing Then
                Return local.Name
            End If

            Return GeneratedNames.MakeSynthesizedLocalName(localKind, _uniqueNameId)
        End Function

        Private Function IsSlotReusable(local As LocalSymbol) As Boolean
            Return local.SynthesizedKind.IsSlotReusable(_ilEmitStyle <> ILEmitStyle.Release)
        End Function

        Private Sub FreeLocal(local As LocalSymbol)
            'TODO: releasing locals with name NYI.
            'NOTE: VB considers named local's extent to be whole method 
            '      so releasing them may just not be possible.
            If local.Name Is Nothing AndAlso IsSlotReusable(local) AndAlso Not IsStackLocal(local) Then
                _builder.LocalSlotManager.FreeLocal(local)
            End If
        End Sub

        ''' <summary>
        ''' Gets already declared and initialized local.
        ''' </summary>
        Private Function GetLocal(localExpression As BoundLocal) As LocalDefinition
            Dim symbol = localExpression.LocalSymbol
            Return GetLocal(symbol)
        End Function

        Private Function GetLocal(symbol As LocalSymbol) As LocalDefinition
            Return _builder.LocalSlotManager.GetLocal(symbol)
        End Function

        ''' <summary>
        ''' Allocates a temp without identity.
        ''' </summary>
        Private Function AllocateTemp(type As TypeSymbol, syntaxNode As VisualBasicSyntaxNode) As LocalDefinition
            Return _builder.LocalSlotManager.AllocateSlot(
                Me._module.Translate(type, syntaxNodeOpt:=syntaxNode, diagnostics:=_diagnostics),
                LocalSlotConstraints.None)
        End Function

        ''' <summary>
        ''' Frees a temp without identity.
        ''' </summary>
        Private Sub FreeTemp(temp As LocalDefinition)
            _builder.LocalSlotManager.FreeSlot(temp)
        End Sub

        ''' <summary>
        ''' Frees an optional temp.
        ''' </summary>
        Private Sub FreeOptTemp(temp As LocalDefinition)
            If temp IsNot Nothing Then
                FreeTemp(temp)
            End If
        End Sub

        Private Sub EmitUnstructuredExceptionOnErrorSwitch(node As BoundUnstructuredExceptionOnErrorSwitch)
            EmitExpression(node.Value, used:=True)
            EmitSwitch(node.Jumps)
        End Sub

        Private Sub EmitSwitch(jumps As ImmutableArray(Of BoundGotoStatement))
            Dim labels(jumps.Length - 1) As Object

            For i As Integer = 0 To jumps.Length - 1
                labels(i) = jumps(i).Label
            Next

            _builder.EmitSwitch(labels)
        End Sub

        Private Sub EmitStateMachineScope(scope As BoundStateMachineScope)
            _builder.OpenLocalScope()

            'VB EE uses name mangling to match up original locals and the fields where they are hoisted
            'The scoping information is passed by recording PDB scopes of "fake" locals named the same 
            'as the fields. These locals are not emitted to IL.

            '         vb\language\debugger\procedurecontext.cpp
            '  813                  // Since state machines lift (almost) all locals of a method, the lifted fields should
            '  814                  // only be shown in the debugger when the original local variable was in scope.  So
            '  815                  // we first check if there's a local by the given name and attempt to remove it from 
            '  816                  // m_localVariableMap.  If it was present, we decode the original local's name, otherwise
            '  817                  // we skip loading this lifted field since it is out of scope.

            For Each field In scope.Fields
                DefineUserDefinedStateMachineHoistedLocal(DirectCast(field, StateMachineFieldSymbol))
            Next

            EmitStatement(scope.Statement)

            _builder.CloseLocalScope()
        End Sub

        Private Sub DefineUserDefinedStateMachineHoistedLocal(field As StateMachineFieldSymbol)
            Debug.Assert(field.SlotIndex >= 0)
            Dim fakePdbOnlyLocal = New LocalDefinition(
                        symbolOpt:=Nothing,
                        nameOpt:=field.Name,
                        type:=Nothing,
                        slot:=field.SlotIndex,
                        synthesizedKind:=SynthesizedLocalKind.EmitterTemp,
                        id:=Nothing,
                        pdbAttributes:=Cci.PdbWriter.DefaultLocalAttributesValue,
                        constraints:=LocalSlotConstraints.None,
                        isDynamic:=False,
                        dynamicTransformFlags:=Nothing)
            _builder.AddLocalToScope(fakePdbOnlyLocal)
        End Sub

        Private Sub EmitUnstructuredExceptionResumeSwitch(node As BoundUnstructuredExceptionResumeSwitch)
            ' Resume statements will branch here.  Just load the resume local and
            ' branch to the switch table
            EmitLabelStatement(node.ResumeLabel)
            EmitExpression(node.ResumeTargetTemporary, used:=True)

            Dim switchLabel As New Object()
            _builder.EmitBranch(ILOpCode.Br_s, switchLabel)

            _builder.AdjustStack(-1)

            ' Resume Next statements will branch here.  Increment the resume local and
            ' fall through to the switch table
            EmitLabelStatement(node.ResumeNextLabel)
            EmitExpression(node.ResumeTargetTemporary, used:=True)
            _builder.EmitIntConstant(1)
            _builder.EmitOpCode(ILOpCode.Add)

            ' now start generating the resume switch table
            _builder.MarkLabel(switchLabel)

            ' but first clear the resume local
            _builder.EmitIntConstant(0)
            _builder.EmitLocalStore(GetLocal(node.ResumeTargetTemporary))

            EmitSwitch(node.Jumps)
        End Sub

    End Class

End Namespace

