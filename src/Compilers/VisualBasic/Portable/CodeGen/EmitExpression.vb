' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Immutable
Imports System.Reflection.Metadata
Imports Microsoft.CodeAnalysis.CodeGen
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols

Namespace Microsoft.CodeAnalysis.VisualBasic.CodeGen

    Partial Friend Class CodeGenerator
        Private _recursionDepth As Integer

        Private Class EmitCancelledException
            Inherits Exception
        End Class

        Private Enum UseKind
            Unused
            UsedAsValue
            UsedAsAddress
        End Enum

        Private Sub EmitExpression(expression As BoundExpression, used As Boolean)
            If expression Is Nothing Then
                Return
            End If

            Dim constantValue = expression.ConstantValueOpt
            If constantValue IsNot Nothing Then
                If Not used Then
                    ' unused constants have no side-effects.
                    Return
                End If
                If constantValue.IsDecimal OrElse constantValue.IsDateTime Then
                    ' Decimal/DateTime literal fields like Decimal.One should be emitted as fields.
                    Debug.Assert(expression.Kind = BoundKind.FieldAccess)
                Else
                    EmitConstantExpression(expression.Type, constantValue, used, expression.Syntax)
                    Return
                End If
            End If

            _recursionDepth += 1

            If _recursionDepth > 1 Then
                StackGuard.EnsureSufficientExecutionStack(_recursionDepth)

                EmitExpressionCore(expression, used)
            Else
                EmitExpressionCoreWithStackGuard(expression, used)
            End If

            _recursionDepth -= 1
        End Sub

        Private Sub EmitExpressionCoreWithStackGuard(expression As BoundExpression, used As Boolean)
            Debug.Assert(_recursionDepth = 1)

            Try
                EmitExpressionCore(expression, used)
                Debug.Assert(_recursionDepth = 1)

            Catch ex As InsufficientExecutionStackException
                _diagnostics.Add(ERRID.ERR_TooLongOrComplexExpression,
                                 BoundTreeVisitor.CancelledByStackGuardException.GetTooLongOrComplexExpressionErrorLocation(expression))
                Throw New EmitCancelledException()
            End Try
        End Sub

        Private Sub EmitExpressionCore(expression As BoundExpression, used As Boolean)

            Select Case expression.Kind
                Case BoundKind.AssignmentOperator
                    EmitAssignmentExpression(DirectCast(expression, BoundAssignmentOperator), used)

                Case BoundKind.Call
                    EmitCallExpression(DirectCast(expression, BoundCall), If(used, UseKind.UsedAsValue, UseKind.Unused))

                Case BoundKind.TernaryConditionalExpression
                    EmitTernaryConditionalExpression(DirectCast(expression, BoundTernaryConditionalExpression), used)

                Case BoundKind.BinaryConditionalExpression
                    EmitBinaryConditionalExpression(DirectCast(expression, BoundBinaryConditionalExpression), used)

                Case BoundKind.ObjectCreationExpression
                    EmitObjectCreationExpression(DirectCast(expression, BoundObjectCreationExpression), used)

                Case BoundKind.ArrayCreation
                    EmitArrayCreationExpression(DirectCast(expression, BoundArrayCreation), used)

                Case BoundKind.ArrayLength
                    EmitArrayLengthExpression(DirectCast(expression, BoundArrayLength), used)

                Case BoundKind.Conversion
                    EmitConversionExpression(DirectCast(expression, BoundConversion), used)

                Case BoundKind.DirectCast
                    EmitDirectCastExpression(DirectCast(expression, BoundDirectCast), used)

                Case BoundKind.TryCast
                    EmitTryCastExpression(DirectCast(expression, BoundTryCast), used)

                Case BoundKind.TypeOf
                    EmitTypeOfExpression(DirectCast(expression, BoundTypeOf), used)

                Case BoundKind.Local
                    EmitLocalLoad(DirectCast(expression, BoundLocal), used)

                Case BoundKind.Parameter
                    If used Then ' unused parameter has no side-effects
                        EmitParameterLoad(DirectCast(expression, BoundParameter))
                    End If

                Case BoundKind.Dup
                    EmitDupExpression(DirectCast(expression, BoundDup), used)

                Case BoundKind.FieldAccess
                    EmitFieldLoad(DirectCast(expression, BoundFieldAccess), used)

                Case BoundKind.ArrayAccess
                    EmitArrayElementLoad(DirectCast(expression, BoundArrayAccess), used)

                Case BoundKind.MeReference, BoundKind.MyClassReference
                    If used Then ' unused Me/MyClass has no side-effects
                        EmitMeOrMyClassReferenceExpression(expression)
                    End If

                Case BoundKind.MyBaseReference
                    If used Then ' unused base has no side-effects
                        _builder.EmitOpCode(ILOpCode.Ldarg_0)
                    End If

                Case BoundKind.Sequence
                    EmitSequenceExpression(DirectCast(expression, BoundSequence), used)

                Case BoundKind.SequencePointExpression
                    EmitSequencePointExpression(DirectCast(expression, BoundSequencePointExpression), used)

                Case BoundKind.UnaryOperator
                    EmitUnaryOperatorExpression(DirectCast(expression, BoundUnaryOperator), used)

                Case BoundKind.BinaryOperator
                    EmitBinaryOperatorExpression(DirectCast(expression, BoundBinaryOperator), used)

                Case BoundKind.DelegateCreationExpression
                    EmitDelegateCreationExpression(DirectCast(expression, BoundDelegateCreationExpression), used)

                Case BoundKind.GetType
                    EmitGetType(DirectCast(expression, BoundGetType), used)

                Case BoundKind.FieldInfo
                    EmitFieldInfoExpression(DirectCast(expression, BoundFieldInfo), used)

                Case BoundKind.MethodInfo
                    EmitMethodInfoExpression(DirectCast(expression, BoundMethodInfo), used)

                Case BoundKind.ReferenceAssignment
                    EmitReferenceAssignment(DirectCast(expression, BoundReferenceAssignment), used)

                Case BoundKind.ValueTypeMeReference
                    ' We want to restrict the usage of BoundValueTypeMeReference to the very minimum possible,
                    ' which is to be able to pass Me reference of value type as ByRef argument in compiler
                    ' generated code, this is why we specifically prohibit emitting this as value
                    Throw ExceptionUtilities.UnexpectedValue(expression.Kind)

                Case BoundKind.LoweredConditionalAccess
                    EmitConditionalAccess(DirectCast(expression, BoundLoweredConditionalAccess), used)

                Case BoundKind.ConditionalAccessReceiverPlaceholder
                    EmitConditionalAccessReceiverPlaceholder(DirectCast(expression, BoundConditionalAccessReceiverPlaceholder), used)

                Case BoundKind.ComplexConditionalAccessReceiver
                    EmitComplexConditionalAccessReceiver(DirectCast(expression, BoundComplexConditionalAccessReceiver), used)

                Case BoundKind.PseudoVariable
                    EmitPseudoVariableValue(DirectCast(expression, BoundPseudoVariable), used)

                Case BoundKind.ModuleVersionId
                    Debug.Assert(used)
                    EmitModuleVersionIdLoad(DirectCast(expression, BoundModuleVersionId))

                Case BoundKind.ModuleVersionIdString
                    Debug.Assert(used)
                    EmitModuleVersionIdStringLoad(DirectCast(expression, BoundModuleVersionIdString))

                Case BoundKind.InstrumentationPayloadRoot
                    Debug.Assert(used)
                    EmitInstrumentationPayloadRootLoad(DirectCast(expression, BoundInstrumentationPayloadRoot))

                Case BoundKind.MethodDefIndex
                    Debug.Assert(used)
                    EmitMethodDefIndexExpression(DirectCast(expression, BoundMethodDefIndex))

                Case BoundKind.MaximumMethodDefIndex
                    Debug.Assert(used)
                    EmitMaximumMethodDefIndexExpression(DirectCast(expression, BoundMaximumMethodDefIndex))

                Case BoundKind.SourceDocumentIndex
                    Debug.Assert(used)
                    EmitSourceDocumentIndex(DirectCast(expression, BoundSourceDocumentIndex))

                Case Else
                    ' Code gen should not be invoked if there are errors.
                    ' Debug.Assert(expression.Kind <> BoundKind.BadExpression AndAlso expression.Kind <> BoundKind.Parenthesized)
                    Throw ExceptionUtilities.UnexpectedValue(expression.Kind)
                    Return
            End Select
        End Sub

        Private Sub EmitConditionalAccessReceiverPlaceholder(expression As BoundConditionalAccessReceiverPlaceholder, used As Boolean)
            Debug.Assert(Not expression.Type.IsValueType)

            If used AndAlso Not expression.Type.IsReferenceType Then
                EmitLoadIndirect(expression.Type, expression.Syntax)
            End If

            EmitPopIfUnused(used)
        End Sub

        Private Sub EmitComplexConditionalAccessReceiver(expression As BoundComplexConditionalAccessReceiver, used As Boolean)
            Debug.Assert(Not expression.Type.IsReferenceType)
            Debug.Assert(Not expression.Type.IsValueType)

            Dim receiverType = expression.Type

            Dim whenValueTypeLabel As New Object()
            Dim doneLabel As New Object()

            EmitInitObj(receiverType, True, expression.Syntax)
            EmitBox(receiverType, expression.Syntax)
            _builder.EmitBranch(ILOpCode.Brtrue, whenValueTypeLabel)

            EmitExpression(expression.ReferenceTypeReceiver, used)
            _builder.EmitBranch(ILOpCode.Br, doneLabel)
            _builder.AdjustStack(-1)

            _builder.MarkLabel(whenValueTypeLabel)
            EmitExpression(expression.ValueTypeReceiver, used)

            _builder.MarkLabel(doneLabel)
        End Sub

        Private Sub EmitConditionalAccess(conditional As BoundLoweredConditionalAccess, used As Boolean)

            Debug.Assert(conditional.WhenNullOpt IsNot Nothing OrElse Not used)

            If conditional.ReceiverOrCondition.Type.IsBooleanType() Then
                ' This is a trivial case 
                Debug.Assert(Not conditional.CaptureReceiver)
                Debug.Assert(conditional.PlaceholderId = 0)

                Dim doneLabel = New Object()

                Dim consequenceLabel = New Object()

                EmitCondBranch(conditional.ReceiverOrCondition, consequenceLabel, sense:=True)

                If conditional.WhenNullOpt IsNot Nothing Then
                    EmitExpression(conditional.WhenNullOpt, used)
                Else
                    Debug.Assert(Not used)
                End If

                _builder.EmitBranch(ILOpCode.Br, doneLabel)
                If used Then
                    ' If we get to consequenceLabel, we should not have WhenFalse on stack, adjust for that.
                    _builder.AdjustStack(-1)
                End If

                _builder.MarkLabel(consequenceLabel)
                EmitExpression(conditional.WhenNotNull, used)

                _builder.MarkLabel(doneLabel)
            Else
                Debug.Assert(Not conditional.ReceiverOrCondition.Type.IsValueType)

                Dim receiverTemp As LocalDefinition = Nothing
                Dim temp As LocalDefinition = Nothing

                ' labels
                Dim whenNotNullLabel As New Object()
                Dim doneLabel As New Object()

                ' we need a copy if we deal with nonlocal value (to capture the value)
                ' Or if we have a ref-constrained T (to do box just once)
                Dim receiver As BoundExpression = conditional.ReceiverOrCondition
                Dim receiverType As TypeSymbol = receiver.Type
                Dim nullCheckOnCopy = conditional.CaptureReceiver OrElse (receiverType.IsReferenceType AndAlso receiverType.TypeKind = TypeKind.TypeParameter)

                If nullCheckOnCopy Then
                    receiverTemp = EmitReceiverRef(receiver, isAccessConstrained:=Not receiverType.IsReferenceType, addressKind:=AddressKind.ReadOnly)

                    If Not receiverType.IsReferenceType Then
                        If receiverTemp Is Nothing Then
                            ' unconstrained case needs to handle case where T Is actually a struct.
                            ' such values are never nulls
                            ' we will emit a check for such case, but the check Is really a JIT-time 
                            ' constant since JIT will know if T Is a struct Or Not.
                            '
                            ' if ((object)default(T) != null) 
                            ' {
                            '     goto whenNotNull
                            ' }
                            ' else
                            ' {
                            '     temp = receiverRef
                            '     receiverRef = ref temp
                            ' }
                            EmitInitObj(receiverType, True, receiver.Syntax)
                            EmitBox(receiverType, receiver.Syntax)
                            _builder.EmitBranch(ILOpCode.Brtrue, whenNotNullLabel)
                            EmitLoadIndirect(receiverType, receiver.Syntax)

                            temp = AllocateTemp(receiverType, receiver.Syntax)
                            _builder.EmitLocalStore(temp)
                            _builder.EmitLocalAddress(temp)
                            _builder.EmitLocalLoad(temp)
                            EmitBox(receiverType, receiver.Syntax)

                            ' here we have loaded a ref to a temp And its boxed value { &T, O }
                        Else
                            ' we are calling the expression on a copy of the target anyway, 
                            ' so even if T is a struct, we don't need to make sure we call the expression on the original target.

                            _builder.EmitLocalLoad(receiverTemp)
                            EmitBox(receiverType, receiver.Syntax)
                        End If
                    Else
                        _builder.EmitOpCode(ILOpCode.Dup)
                        ' here we have loaded two copies of a reference   { O, O }
                    End If
                Else
                    EmitExpression(receiver, True)
                    If Not receiverType.IsReferenceType Then
                        EmitBox(receiverType, receiver.Syntax)
                    End If

                    ' here we have loaded just { O }
                    ' we have the most trivial case where we can just reload O when needed
                End If

                _builder.EmitBranch(ILOpCode.Brtrue, whenNotNullLabel)

                If nullCheckOnCopy Then
                    _builder.EmitOpCode(ILOpCode.Pop)
                End If

                If conditional.WhenNullOpt IsNot Nothing Then
                    EmitExpression(conditional.WhenNullOpt, used)
                Else
                    Debug.Assert(Not used)
                End If

                _builder.EmitBranch(ILOpCode.Br, doneLabel)

                If used Then
                    ' If we get to whenNotNullLabel, we should not have WhenNullOpt on stack, adjust for that.
                    _builder.AdjustStack(-1)
                End If

                If nullCheckOnCopy Then
                    ' whenNull branch pops copy of the receiver off the stack when nullCheckOnCopy
                    ' however on this branch we still have the stack as it was and need 
                    ' to adjust stack depth accordingly.
                    _builder.AdjustStack(+1)
                End If

                _builder.MarkLabel(whenNotNullLabel)

                If Not nullCheckOnCopy Then
                    Debug.Assert(receiverTemp Is Nothing)
                    receiverTemp = EmitReceiverRef(receiver, isAccessConstrained:=Not receiverType.IsReferenceType, addressKind:=AddressKind.ReadOnly)
                    Debug.Assert(receiverTemp Is Nothing OrElse receiver.IsDefaultValue())
                End If

                EmitExpression(conditional.WhenNotNull, used)
                _builder.MarkLabel(doneLabel)

                If temp IsNot Nothing Then
                    FreeTemp(temp)
                End If

                If receiverTemp IsNot Nothing Then
                    FreeTemp(receiverTemp)
                End If
            End If
        End Sub

        Private Sub EmitComplexConditionalAccessReceiverAddress(expression As BoundComplexConditionalAccessReceiver)
            Debug.Assert(Not expression.Type.IsReferenceType)
            Debug.Assert(Not expression.Type.IsValueType)

            Dim receiverType = expression.Type

            Dim whenValueTypeLabel As New Object()
            Dim doneLabel As New Object()

            EmitInitObj(receiverType, True, expression.Syntax)
            EmitBox(receiverType, expression.Syntax)
            _builder.EmitBranch(ILOpCode.Brtrue, whenValueTypeLabel)

            Dim receiverTemp = EmitAddress(expression.ReferenceTypeReceiver, addressKind:=AddressKind.ReadOnly)
            Debug.Assert(receiverTemp Is Nothing)
            _builder.EmitBranch(ILOpCode.Br, doneLabel)
            _builder.AdjustStack(-1)

            _builder.MarkLabel(whenValueTypeLabel)
            EmitReceiverRef(expression.ValueTypeReceiver, isAccessConstrained:=True, addressKind:=AddressKind.ReadOnly)

            _builder.MarkLabel(doneLabel)
        End Sub

        Private Sub EmitDelegateCreationExpression(expression As BoundDelegateCreationExpression, used As Boolean)
            Dim invoke = DirectCast(expression.Method, MethodSymbol)
            EmitDelegateCreation(expression.ReceiverOpt, invoke, expression.Type, used, expression.Syntax)
        End Sub

        Private Sub EmitLocalLoad(local As BoundLocal, used As Boolean)
            If IsStackLocal(local.LocalSymbol) Then
                ' local should be already on the stack
                EmitPopIfUnused(used)
            Else
                If used Then ' unused local has no side-effects
                    _builder.EmitLocalLoad(GetLocal(local))
                Else
                    ' do nothing. Unused local load has no side-effects.                    
                    Return
                End If
            End If

            If used AndAlso local.LocalSymbol.IsByRef Then
                EmitLoadIndirect(local.Type, local.Syntax)
            End If
        End Sub

        Private Sub EmitDelegateCreation(receiver As BoundExpression, method As MethodSymbol, delegateType As TypeSymbol, used As Boolean, syntaxNode As SyntaxNode)
            Dim isStatic = receiver Is Nothing OrElse method.IsShared
            If Not used Then
                If Not isStatic Then
                    EmitExpression(receiver, False)
                End If

                Return
            End If

            If isStatic Then
                _builder.EmitNullConstant()
            Else
                EmitExpression(receiver, True)
                If Not IsVerifierReference(receiver.Type) Then
                    EmitBox(receiver.Type, receiver.Syntax)
                End If
            End If

            ' Metadata Spec (II.14.6):
            '   Delegates shall be declared sealed.
            '   The Invoke method shall be virtual.
            ' Dev11 VB uses ldvirtftn for delegate methods, we emit ldftn to be consistent with C#.
            If method.IsMetadataVirtual AndAlso Not method.ContainingType.IsDelegateType() AndAlso Not receiver.SuppressVirtualCalls Then
                _builder.EmitOpCode(ILOpCode.Dup)
                _builder.EmitOpCode(ILOpCode.Ldvirtftn)
            Else
                _builder.EmitOpCode(ILOpCode.Ldftn)
            End If

            Dim targetMethod = If(method.CallsiteReducedFromMethod, method)
            If Not isStatic AndAlso targetMethod.ContainingType.IsNullableType Then
                Debug.Assert(targetMethod.IsOverrides, "Nullable cannot be truly boxed therefore delegates of methods that do not override cannot be created")
                targetMethod = method.OverriddenMethod
            End If

            EmitSymbolToken(targetMethod, syntaxNode)

            ' TODO: check the ctor signature, and recover more gracefully from failure to
            ' find a single constructor with the correct signature
            Dim ctor = DirectCast(delegateType.GetMembers(WellKnownMemberNames.InstanceConstructorName).Single(), MethodSymbol)
            _builder.EmitOpCode(ILOpCode.Newobj, -1)
            EmitSymbolToken(ctor, syntaxNode)
        End Sub

        Private Sub EmitMeOrMyClassReferenceExpression(thisRef As BoundExpression)
            Debug.Assert(thisRef.Kind = BoundKind.MeReference OrElse thisRef.Kind = BoundKind.MyClassReference)

            Dim thisType = thisRef.Type
            Debug.Assert(thisType.TypeKind <> TypeKind.TypeParameter)

            _builder.EmitOpCode(ILOpCode.Ldarg_0)
            If thisType.IsValueType Then
                _builder.EmitOpCode(ILOpCode.Ldobj)
                EmitSymbolToken(thisRef.Type, thisRef.Syntax)
            End If

        End Sub

        Private Sub EmitPseudoVariableValue(expression As BoundPseudoVariable, used As Boolean)
            EmitExpression(expression.EmitExpressions.GetValue(expression, _diagnostics), used)
        End Sub

        Private Sub EmitSequenceExpression(sequence As BoundSequence, used As Boolean)
            Dim hasLocals As Boolean = Not sequence.Locals.IsEmpty
            If hasLocals Then
                _builder.OpenLocalScope()

                For Each local In sequence.Locals
                    Me.DefineLocal(local, sequence.Syntax)
                Next
            End If

            Me.EmitSideEffects(sequence.SideEffects)
            Debug.Assert(sequence.ValueOpt IsNot Nothing OrElse sequence.Type.SpecialType = SpecialType.System_Void)
            Me.EmitExpression(sequence.ValueOpt, used)

            If hasLocals Then
                _builder.CloseLocalScope()

                For Each local In sequence.Locals
                    Me.FreeLocal(local)
                Next
            End If
        End Sub

        Private Sub EmitSideEffects(sideEffects As ImmutableArray(Of BoundExpression))
            If Not sideEffects.IsDefaultOrEmpty Then
                For Each se In sideEffects
                    EmitExpression(se, False)
                Next
            End If
        End Sub

        Private Sub EmitExpressions(expressions As ImmutableArray(Of BoundExpression), used As Boolean)
            Dim i As Integer = 0
            While i < expressions.Length
                Dim expression = expressions(i)
                EmitExpression(expression, used)
                i = i + 1
            End While
        End Sub

        Private Sub EmitArguments(arguments As ImmutableArray(Of BoundExpression), parameters As ImmutableArray(Of ParameterSymbol))
            Debug.Assert(arguments.Length = parameters.Length)
            For i = 0 To arguments.Length - 1
                Dim argument = arguments(i)
                Dim parameter = parameters(i)
                If parameter.IsByRef Then
                    Dim temp = EmitAddress(argument, AddressKind.Writeable)
                    Debug.Assert(temp Is Nothing, "passing args byref should not clone them into temps. That should be done in rewriter.")
                Else
                    EmitExpression(argument, True)
                End If
            Next
        End Sub

        Private Sub EmitArrayElementLoad(arrayAccess As BoundArrayAccess, used As Boolean)
            EmitExpression(arrayAccess.Expression, True)
            EmitExpressions(arrayAccess.Indices, True)

            If DirectCast(arrayAccess.Expression.Type, ArrayTypeSymbol).IsSZArray Then
                Dim elementType = arrayAccess.Type
                If elementType.IsEnumType() Then
                    elementType = (DirectCast(elementType, NamedTypeSymbol)).EnumUnderlyingType
                End If
                Select Case elementType.PrimitiveTypeCode
                    Case Microsoft.Cci.PrimitiveTypeCode.Int8
                        _builder.EmitOpCode(ILOpCode.Ldelem_i1)

                    Case Microsoft.Cci.PrimitiveTypeCode.Boolean,
                         Microsoft.Cci.PrimitiveTypeCode.UInt8
                        _builder.EmitOpCode(ILOpCode.Ldelem_u1)

                    Case Microsoft.Cci.PrimitiveTypeCode.Int16
                        _builder.EmitOpCode(ILOpCode.Ldelem_i2)

                    Case Microsoft.Cci.PrimitiveTypeCode.Char,
                         Microsoft.Cci.PrimitiveTypeCode.UInt16
                        _builder.EmitOpCode(ILOpCode.Ldelem_u2)

                    Case Microsoft.Cci.PrimitiveTypeCode.Int32
                        _builder.EmitOpCode(ILOpCode.Ldelem_i4)

                    Case Microsoft.Cci.PrimitiveTypeCode.UInt32
                        _builder.EmitOpCode(ILOpCode.Ldelem_u4)

                    Case Microsoft.Cci.PrimitiveTypeCode.Int64,
                        Microsoft.Cci.PrimitiveTypeCode.UInt64
                        _builder.EmitOpCode(ILOpCode.Ldelem_i8)

                    Case Microsoft.Cci.PrimitiveTypeCode.IntPtr,
                        Microsoft.Cci.PrimitiveTypeCode.UIntPtr,
                        Microsoft.Cci.PrimitiveTypeCode.Pointer
                        _builder.EmitOpCode(ILOpCode.Ldelem_i)

                    Case Microsoft.Cci.PrimitiveTypeCode.Float32
                        _builder.EmitOpCode(ILOpCode.Ldelem_r4)

                    Case Microsoft.Cci.PrimitiveTypeCode.Float64
                        _builder.EmitOpCode(ILOpCode.Ldelem_r8)

                    Case Else
                        If IsVerifierReference(elementType) Then
                            _builder.EmitOpCode(ILOpCode.Ldelem_ref)
                        Else
                            If used Then
                                _builder.EmitOpCode(ILOpCode.Ldelem)
                            Else
                                ' no need to read whole element of nontrivial type/size here
                                ' just take a reference to an element for array access side-effects 
                                If elementType.TypeKind = TypeKind.TypeParameter Then
                                    _builder.EmitOpCode(ILOpCode.Readonly)
                                End If

                                _builder.EmitOpCode(ILOpCode.Ldelema)
                            End If

                            EmitSymbolToken(elementType, arrayAccess.Expression.Syntax)
                        End If
                End Select
            Else
                _builder.EmitArrayElementLoad(_module.Translate(DirectCast(arrayAccess.Expression.Type, ArrayTypeSymbol)), arrayAccess.Expression.Syntax, _diagnostics)
            End If

            EmitPopIfUnused(used)
        End Sub

        Private Sub EmitFieldLoad(fieldAccess As BoundFieldAccess, used As Boolean)
            Dim field = fieldAccess.FieldSymbol

            'TODO: For static field access this may require ..ctor to run. Is this a side-effect?
            ' Accessing unused instance field on a struct is a noop. Just emit the receiver.
            If Not used AndAlso Not field.IsShared AndAlso fieldAccess.ReceiverOpt.Type.IsVerifierValue() Then
                EmitExpression(fieldAccess.ReceiverOpt, used:=False)
                Return
            End If

            Dim specType = field.Type.SpecialType
            If field.IsConst AndAlso specType <> SpecialType.System_Decimal AndAlso specType <> SpecialType.System_DateTime Then
                ' constant fields are not really fields and should not get here
                Throw ExceptionUtilities.Unreachable
            Else
                If field.IsShared Then
                    EmitStaticFieldLoad(field, used, fieldAccess.Syntax)
                Else
                    EmitInstanceFieldLoad(fieldAccess, used)
                End If
            End If
        End Sub

        Private Sub EmitDupExpression(dupExpression As BoundDup, used As Boolean)
            If Not dupExpression.IsReference Then
                ' unused dup is noop
                If used Then
                    _builder.EmitOpCode(ILOpCode.Dup)
                End If

            Else
                _builder.EmitOpCode(ILOpCode.Dup)

                ' must read in case if it is a null ref
                EmitLoadIndirect(dupExpression.Type, dupExpression.Syntax)
                EmitPopIfUnused(used)
            End If
        End Sub

        Private Sub EmitStaticFieldLoad(field As FieldSymbol, used As Boolean, syntaxNode As SyntaxNode)
            'TODO: this may require ..ctor to run. Is this a side-effect?
            _builder.EmitOpCode(ILOpCode.Ldsfld)
            EmitSymbolToken(field, syntaxNode)
            EmitPopIfUnused(used)
        End Sub

        Private Sub EmitInstanceFieldLoad(fieldAccess As BoundFieldAccess, used As Boolean)
            'TODO: access to a field on this/base has no side-effects.

            Dim field As FieldSymbol = fieldAccess.FieldSymbol
            Dim receiver = fieldAccess.ReceiverOpt

            'ldfld can work with structs directly (taking address is optional)
            'taking address is typically cheaper, but not for homeless exprs
            'those would need to be loaded anyways.
            If FieldLoadMustUseRef(receiver) OrElse FieldLoadPrefersRef(receiver) Then
                If Not EmitFieldLoadReceiverAddress(receiver) Then
                    ' Since we are simply loading the field value, the receiver reference is not going to be mutated.
                    Dim temp = EmitReceiverRef(receiver, isAccessConstrained:=False, addressKind:=AddressKind.Immutable)
                    Debug.Assert(temp Is Nothing OrElse receiver.Type.IsEnumType, "temp is unexpected, just reading a field")
                End If
            Else
                EmitExpression(receiver, True)
            End If

            _builder.EmitOpCode(ILOpCode.Ldfld)
            EmitSymbolToken(field, fieldAccess.Syntax)
            EmitPopIfUnused(used)
        End Sub

        ' In special case of loading the sequence of field accesses we can perform all the 
        ' necessary field loads using the following IL: 
        '
        '      <expr>.a.b...y.z
        '          |
        '          V
        '      Unbox -or- Load.Ref (<expr>)
        '      Ldflda a
        '      Ldflda b
        '      ...
        '      Ldflda y
        '      Ldfld z
        '
        ' Returns 'true' if the receiver was actually emitted this way
        Private Function EmitFieldLoadReceiverAddress(receiver As BoundExpression) As Boolean
            If receiver Is Nothing OrElse receiver.Type.IsReferenceType Then
                Return False

            ElseIf receiver.Kind = BoundKind.DirectCast AndAlso IsUnboxingDirectCast(DirectCast(receiver, BoundDirectCast)) Then
                EmitExpression(DirectCast(receiver, BoundDirectCast).Operand, True)
                Me._builder.EmitOpCode(ILOpCode.Unbox)
                EmitSymbolToken(receiver.Type, receiver.Syntax)
                Return True

            ElseIf receiver.Kind = BoundKind.FieldAccess Then
                Dim fieldAccess = DirectCast(receiver, BoundFieldAccess)
                Dim field As FieldSymbol = fieldAccess.FieldSymbol

                If Not field.IsShared AndAlso EmitFieldLoadReceiverAddress(fieldAccess.ReceiverOpt) Then
                    Me._builder.EmitOpCode(ILOpCode.Ldflda)
                    EmitSymbolToken(field, fieldAccess.Syntax)
                    Return True
                End If
            End If

            Return False
        End Function

        ' ldfld can work with structs directly or with their addresses
        ' In some cases it results in same native code emitted, but in some cases JIT pushes values for real
        ' resulting in much worse code (on x64 in particular).
        ' So, we will always prefer references here except when receiver is a struct non-ref local or parameter. 
        Private Function FieldLoadPrefersRef(receiver As BoundExpression) As Boolean
            ' only fields of structs can be accessed via value
            If Not receiver.Type.IsVerifierValue() Then
                Return True
            End If

            ' can unbox directly into a ref.
            If receiver.Kind = BoundKind.DirectCast AndAlso IsUnboxingDirectCast(DirectCast(receiver, BoundDirectCast)) Then
                Return True
            End If

            ' can we take address at all?
            If Not HasHome(receiver) Then
                Return False
            End If

            Select Case receiver.Kind
                Case BoundKind.Parameter
                    ' prefer ldarg over ldarga
                    Return DirectCast(receiver, BoundParameter).ParameterSymbol.IsByRef

                Case BoundKind.Local
                    ' prefer ldloc over ldloca
                    Return DirectCast(receiver, BoundLocal).LocalSymbol.IsByRef

                Case BoundKind.Sequence
                    Return FieldLoadPrefersRef(DirectCast(receiver, BoundSequence).ValueOpt)

                Case BoundKind.FieldAccess
                    Dim fieldAccess = DirectCast(receiver, BoundFieldAccess)
                    Return fieldAccess.FieldSymbol.IsShared OrElse FieldLoadPrefersRef(fieldAccess.ReceiverOpt)
            End Select

            Return True
        End Function

        Friend Shared Function FieldLoadMustUseRef(expr As BoundExpression) As Boolean
            ' NOTE: we emit a ref when receiver is an Enum.
            '       that is needed only when accessing value__ of the enum and that cannot be done off the enum value.
            If expr.Type.IsEnumType Then
                Return True
            End If

            ' type parameter values must be boxed to get access to fields
            Return expr.Type.IsTypeParameter()
        End Function

        Private Function ParameterSlot(parameter As BoundParameter) As Integer
            Dim sym = parameter.ParameterSymbol
            Dim slot As Integer = sym.Ordinal
            If Not sym.ContainingSymbol.IsShared Then
                slot = slot + 1 ' skip this
            End If
            Return slot
        End Function

        Private Sub EmitParameterLoad(parameter As BoundParameter)
            Dim slot As Integer = ParameterSlot(parameter)
            _builder.EmitLoadArgumentOpcode(slot)

            If parameter.ParameterSymbol.IsByRef Then
                Dim parameterType = parameter.ParameterSymbol.Type
                EmitLoadIndirect(parameterType, parameter.Syntax)
            End If
        End Sub

        Private Sub EmitLoadIndirect(type As TypeSymbol, syntaxNode As SyntaxNode)
            If type.IsEnumType() Then
                'underlying primitives do not need type tokens.
                type = (DirectCast(type, NamedTypeSymbol)).EnumUnderlyingType
            End If

            Select Case type.PrimitiveTypeCode
                Case Microsoft.Cci.PrimitiveTypeCode.Int8
                    _builder.EmitOpCode(ILOpCode.Ldind_i1)

                Case Microsoft.Cci.PrimitiveTypeCode.Boolean,
                     Microsoft.Cci.PrimitiveTypeCode.UInt8
                    _builder.EmitOpCode(ILOpCode.Ldind_u1)

                Case Microsoft.Cci.PrimitiveTypeCode.Int16
                    _builder.EmitOpCode(ILOpCode.Ldind_i2)

                Case Microsoft.Cci.PrimitiveTypeCode.Char,
                    Microsoft.Cci.PrimitiveTypeCode.UInt16
                    _builder.EmitOpCode(ILOpCode.Ldind_u2)

                Case Microsoft.Cci.PrimitiveTypeCode.Int32
                    _builder.EmitOpCode(ILOpCode.Ldind_i4)

                Case Microsoft.Cci.PrimitiveTypeCode.UInt32
                    _builder.EmitOpCode(ILOpCode.Ldind_u4)

                Case Microsoft.Cci.PrimitiveTypeCode.Int64,
                    Microsoft.Cci.PrimitiveTypeCode.UInt64
                    _builder.EmitOpCode(ILOpCode.Ldind_i8)

                Case Microsoft.Cci.PrimitiveTypeCode.IntPtr,
                    Microsoft.Cci.PrimitiveTypeCode.UIntPtr,
                    Microsoft.Cci.PrimitiveTypeCode.Pointer
                    _builder.EmitOpCode(ILOpCode.Ldind_i)

                Case Microsoft.Cci.PrimitiveTypeCode.Float32
                    _builder.EmitOpCode(ILOpCode.Ldind_r4)

                Case Microsoft.Cci.PrimitiveTypeCode.Float64
                    _builder.EmitOpCode(ILOpCode.Ldind_r8)

                Case Else
                    If IsVerifierReference(type) Then
                        _builder.EmitOpCode(ILOpCode.Ldind_ref)
                    Else
                        _builder.EmitOpCode(ILOpCode.Ldobj)
                        EmitSymbolToken(type, syntaxNode)
                    End If

            End Select
        End Sub

        ''' <summary>
        ''' Used to decide if we need to emit call or callvirt.
        ''' It basically checks if the receiver expression cannot be null, but it is not 100% precise. 
        ''' There are cases where it really can be null, but we do not care.
        ''' </summary>
        Private Function CanUseCallOnRefTypeReceiver(receiver As BoundExpression) As Boolean
            ' It seems none of the ways that could produce a receiver typed as a type param 
            ' can guarantee that it is not null.
            If receiver.Type.IsTypeParameter() Then
                Return False
            End If

            Debug.Assert(IsVerifierReference(receiver.Type), "this is not a reference")
            Debug.Assert(receiver.Kind <> BoundKind.MyBaseReference, "MyBase should always use call")
            Debug.Assert(receiver.Kind <> BoundKind.MyClassReference, "MyClass should always use call")

            Dim constVal = receiver.ConstantValueOpt
            If constVal IsNot Nothing Then
                ' only when this is a constant Nothing, we need a callvirt
                Return Not constVal.IsNothing
            End If

            Select Case receiver.Kind
                Case BoundKind.ArrayCreation
                    Return True

                Case BoundKind.ObjectCreationExpression
                    'NOTE: there are cases involving ProxyAttribute
                    'where newobj may produce null
                    Return True

                Case BoundKind.DirectCast
                    Dim convOperand = DirectCast(receiver, BoundDirectCast).Operand

                    If Not IsVerifierReference(convOperand.Type) Then
                        'this is boxing
                        'NOTE: it can produce null for Nullable, but any call through that
                        'will result in null reference exceptions anyways.
                        Return True
                    Else
                        Return CanUseCallOnRefTypeReceiver(convOperand)
                    End If

                Case BoundKind.MeReference, BoundKind.MyBaseReference, BoundKind.MyClassReference
                    'NOTE: these actually can be null if called from a different language
                    'if that has already happen, we will just propagate the behavior.
                    Return True

                Case BoundKind.DelegateCreationExpression, BoundKind.AddressOfOperator
                    Return True

                Case BoundKind.Sequence
                    Dim seqValue = DirectCast(receiver, BoundSequence).ValueOpt
                    Return seqValue IsNot Nothing AndAlso CanUseCallOnRefTypeReceiver(seqValue)

                Case BoundKind.AssignmentOperator
                    Dim rhs = DirectCast(receiver, BoundAssignmentOperator).Right
                    Return CanUseCallOnRefTypeReceiver(rhs)

                Case BoundKind.GetType
                    Return True

                Case BoundKind.FieldAccess
                    Return DirectCast(receiver, BoundFieldAccess).FieldSymbol.IsCapturedFrame

                Case BoundKind.ConditionalAccessReceiverPlaceholder,
                     BoundKind.ComplexConditionalAccessReceiver
                    Return True

                    'TODO: there must be more non-null cases.
            End Select

            Return False
        End Function

        ''' <summary>
        ''' checks if receiver is effectively ldarg.0
        ''' </summary>
        Private Function IsMeReceiver(receiver As BoundExpression) As Boolean
            Select Case receiver.Kind
                Case BoundKind.MeReference,
                    BoundKind.MyClassReference
                    Return True

                Case BoundKind.Sequence
                    Dim seqValue = DirectCast(receiver, BoundSequence).ValueOpt
                    Return IsMeReceiver(seqValue)
            End Select

            Return False
        End Function

        Private Enum CallKind
            [Call]
            CallVirt
            ConstrainedCallVirt
        End Enum

        Private Sub EmitCallExpression([call] As BoundCall, useKind As UseKind)
            Dim method = [call].Method
            Dim receiver = [call].ReceiverOpt

            Debug.Assert([call].MethodGroupOpt Is Nothing)
            Debug.Assert(Not Me._module.AllowOmissionOfConditionalCalls OrElse Not method.CallsAreOmitted([call].Syntax, [call].SyntaxTree))

            ' is this a call to a default struct constructor?
            ' this happens in struct non-parameterless constructors calling
            ' Me.New()
            If method.IsDefaultValueTypeConstructor() Then
                EmitInitObjOnTarget(receiver)
                Return
            End If

            Dim arguments = [call].Arguments
            Dim stackBehavior = (If(method.IsSub, 0, 1)) - arguments.Length

            Dim callKind As CallKind
            Dim tempOpt As LocalDefinition = Nothing

            If method.IsShared Then
                callKind = CallKind.Call
            Else
                stackBehavior = stackBehavior - 1
                Dim receiverType = receiver.Type
                If IsVerifierReference(receiverType) Then
                    tempOpt = EmitReceiverRef(receiver, isAccessConstrained:=False, addressKind:=AddressKind.ReadOnly)
                    'TODO: it is ok to use Call with final methods.
                    '      Dev10 does not do this, but perhaps we should.

                    ' Call/Callvirt is decided in the following order:
                    ' 1) MyBase/MyClass calls must use "call"
                    ' 2) virtual methods must use "callvirt"
                    ' 3) nonvirtual methods use "callvirt" too for the null check semantics.
                    '    3.a In some cases CanUseCallOnRefTypeReceiver returns true which means that 
                    '        null check is unnecessary and we can use "call"
                    If receiver.SuppressVirtualCalls OrElse (Not method.IsMetadataVirtual AndAlso CanUseCallOnRefTypeReceiver(receiver)) Then
                        callKind = CallKind.Call
                    Else
                        callKind = CallKind.CallVirt
                    End If

                ElseIf IsVerifierValue(receiverType) Then

                    Dim methodContainingType = method.ContainingType
                    If IsVerifierValue(methodContainingType) AndAlso MayUseCallForStructMethod(method) Then

                        ' NOTE: this should be either a method which overrides some abstract method or 
                        '       does not override anything (with few exceptions, see MayUseCallForStructMethod); 
                        '       otherwise we should not use direct 'call' and must use constrained call;

                        ' calling a method defined in a value type considered a "write" to the target unless the target is "Me"
                        Debug.Assert(TypeSymbol.Equals(receiverType, method.ContainingType, TypeCompareKind.ConsiderEverything))
                        tempOpt = EmitReceiverRef(
                            receiver,
                            isAccessConstrained:=False, addressKind:=If(IsMeReceiver(receiver),
                                                                         AddressKind.ReadOnly,
                                                                         AddressKind.Writeable))

                        callKind = CallKind.Call

                    Else
                        If method.IsMetadataVirtual Then
                            ' When calling a method that is virtual in metadata on a struct receiver, 
                            ' we use a constrained virtual call. If possible, it will skip boxing.
                            tempOpt = EmitReceiverRef(receiver, isAccessConstrained:=True, addressKind:=AddressKind.ReadOnly)
                            callKind = CallKind.ConstrainedCallVirt

                            ' In case we plan to use 'callvirt' we need to be sure the method we call 
                            ' is not the one from the structure, so we take the one we override
                            If IsVerifierValue(methodContainingType) Then

                                ' NOTE: the most overridden method needs to be used to match Dev10 behavior
                                While method.OverriddenMethod IsNot Nothing
                                    method = method.OverriddenMethod
                                End While
                            End If

                        Else
                            ' calling a method defined in a base class.
                            EmitExpression(receiver, True)
                            EmitBox(receiverType, receiver.Syntax)
                            callKind = CallKind.Call
                        End If

                    End If

                Else
                    ' receiver is generic and method must come from the base or an interface or a generic constraint
                    ' if the receiver is actually a value type it would need to be boxed.
                    ' let .constrained sort this out. 

                    Debug.Assert(Not receiverType.IsReferenceType OrElse receiver.Kind <> BoundKind.ComplexConditionalAccessReceiver)
                    callKind = If(receiverType.IsReferenceType AndAlso
                                   (receiver.Kind = BoundKind.ConditionalAccessReceiverPlaceholder OrElse Not AllowedToTakeRef(receiver, AddressKind.ReadOnly)),
                                    CallKind.CallVirt,
                                    CallKind.ConstrainedCallVirt)

                    tempOpt = EmitReceiverRef(receiver, isAccessConstrained:=callKind = CallKind.ConstrainedCallVirt, addressKind:=AddressKind.ReadOnly)
                End If

            End If

            ' Devirtualizing of calls to effectively sealed methods.
            If callKind = CallKind.CallVirt AndAlso
                method.ContainingModule Is Me._method.ContainingModule Then

                ' NOTE: we check that we call method in same module just to be sure
                ' that it cannot be recompiled as not final and make our call not verifiable. 
                ' such change by adversarial user would arguably be a compat break, but better be safe...
                ' In reality we would typically have one method calling another method in the same class (one GetEnumerator calling another).
                ' Other scenarios are uncommon since base class cannot be sealed and 
                ' referring to a derived type in a different module is not an easy thing to do.
                If IsMeReceiver(receiver) AndAlso method.ContainingType.IsNotInheritable Then
                    ' special case for target is in a sealed class and "this" receiver.
                    Debug.Assert(receiver.Type.IsVerifierReference())
                    callKind = CallKind.Call

                ElseIf method.IsMetadataFinal AndAlso CanUseCallOnRefTypeReceiver(receiver) Then
                    ' special case for calling 'final' virtual method on reference receiver
                    Debug.Assert(receiver.Type.IsVerifierReference())
                    callKind = CallKind.Call
                End If
            End If

            EmitArguments(arguments, method.Parameters)
            Select Case callKind
                Case CallKind.Call
                    _builder.EmitOpCode(ILOpCode.Call, stackBehavior)

                Case CallKind.CallVirt
                    _builder.EmitOpCode(ILOpCode.Callvirt, stackBehavior)

                Case CallKind.ConstrainedCallVirt
                    _builder.EmitOpCode(ILOpCode.Constrained)
                    EmitSymbolToken(receiver.Type, receiver.Syntax)
                    _builder.EmitOpCode(ILOpCode.Callvirt, stackBehavior)

            End Select

            EmitSymbolToken(method, [call].Syntax)
            If Not method.IsSub Then
                EmitPopIfUnused(useKind <> UseKind.Unused)
            ElseIf _ilEmitStyle = ILEmitStyle.Debug Then
                Debug.Assert(useKind = UseKind.Unused, "Using the return value of a void method.")
                Debug.Assert(_method.GenerateDebugInfo, "Implied by emitSequencePoints")

                ' DevDiv #15135.  When a method like System.Diagnostics.Debugger.Break() is called, the
                ' debugger sees an event indicating that a user break (vs a breakpoint) has occurred.
                ' When this happens, it uses ICorDebugILFrame.GetIP(out uint, out CorDebugMappingResult)
                ' to determine the current instruction pointer.  This method returns the instruction
                ' *after* the call.  The source location is then given by the last sequence point before
                ' or on this instruction.  As a result, if the instruction after the call has its own
                ' sequence point, then that sequence point will be used to determine the source location
                ' and the debugging experience will be disrupted.  The easiest way to ensure that the next
                ' instruction does not have a sequence point is to insert a nop.  Obviously, we only do this
                ' if debugging is enabled and optimization is disabled.

                ' From CodeGenerator::GenerateCall:
                '   The IP always points to the location where execution will return
                '   So we generate a NOP here so that the IP is still associated with
                '   the call statement and the user can double click in the callstack
                '   window and take him to the calling line.

                ' CONSIDER: The native compiler does not appear to consider whether we are optimizing or emitting debug info.

                ' CONSIDER: The native compiler also checks !(tree->flags & EXF_NODEBUGINFO).  We don't have
                ' this mutable bit on our bound nodes, so we can't exactly match the behavior.  We might be
                ' able to approximate the native behavior by inspecting call.WasCompilerGenerated, but it is
                ' Not in a reliable state after lowering.

                _builder.EmitOpCode(ILOpCode.Nop)
            End If

            If useKind = UseKind.UsedAsValue AndAlso method.ReturnsByRef Then
                EmitLoadIndirect(method.ReturnType, [call].Syntax)
            ElseIf useKind = UseKind.UsedAsAddress Then
                Debug.Assert(method.ReturnsByRef)
            End If

            FreeOptTemp(tempOpt)

            ' Dev10 #850039 Check if we must disable inlining and optimization for the enclosing proc.
            If Me._checkCallsForUnsafeJITOptimization AndAlso method.IsDefinition Then
                Dim disableJITOptimization As Boolean = False

                If method.ContainingSymbol Is Me._module.Compilation.GetWellKnownType(WellKnownType.Microsoft_VisualBasic_ErrObject) Then
                    If String.Equals(method.Name, "Raise", StringComparison.Ordinal) Then
                        disableJITOptimization = True
                    End If
                ElseIf method.ContainingSymbol Is Me._module.Compilation.GetWellKnownType(WellKnownType.Microsoft_VisualBasic_CompilerServices_ProjectData) Then
                    If String.Equals(method.Name, "EndApp", StringComparison.Ordinal) Then
                        disableJITOptimization = True
                    End If
                ElseIf method.ContainingSymbol Is Me._module.Compilation.GetWellKnownType(WellKnownType.Microsoft_VisualBasic_ApplicationServices_ApplicationBase) Then
                    If String.Equals(method.Name, "Info", StringComparison.Ordinal) Then
                        disableJITOptimization = True
                    End If
                ElseIf method.ContainingSymbol Is Me._module.Compilation.GetWellKnownType(WellKnownType.Microsoft_VisualBasic_ApplicationServices_WindowsFormsApplicationBase) Then
                    If String.Equals(method.Name, "Run", StringComparison.Ordinal) Then
                        disableJITOptimization = True
                    End If
                ElseIf method.ContainingSymbol Is Me._module.Compilation.GetWellKnownType(WellKnownType.Microsoft_VisualBasic_FileSystem) Then
                    Select Case method.Name
                        Case "Dir", "EOF", "FileAttr", "FileClose", "FileCopy", "FileGet", "FileGetObject", "FileOpen", "FilePut",
                             "FilePutObject", "FileWidth", "FreeFile", "Input", "InputString", "Kill", "LineInput", "Loc", "Lock",
                             "LOF", "Print", "PrintLine", "Rename", "Reset", "Seek", "SetAttr", "Unlock", "Write", "WriteLine"
                            disableJITOptimization = True
                    End Select
                End If

                If disableJITOptimization Then
                    Me._checkCallsForUnsafeJITOptimization = False
                    Me._module.SetDisableJITOptimization(Me._method)
                End If
            End If
        End Sub

        ''' <summary>
        ''' Used to decide if we need to emit 'call' or 'callvirt' for structure method.
        ''' It basically checks if the method overrides any other and method's defining type
        ''' is not a 'special' or 'special-by-ref' type. 
        ''' </summary>
        Private Function MayUseCallForStructMethod(method As MethodSymbol) As Boolean
            Debug.Assert(IsVerifierValue(method.ContainingType), "this is not a value type")

            If Not method.IsMetadataVirtual Then
                Return True
            End If

            Dim overriddenMethod = method.OverriddenMethod
            If overriddenMethod Is Nothing OrElse overriddenMethod.IsMustOverride Then
                Return True
            End If

            Dim containingType = method.ContainingType
            ' NOTE: current implementation of IsIntrinsicType treats DateTime as an 
            '       intrinsic type which differs from C# version
            ' NOTE: VB Dev10 uses 'constrained'/'callvirt' for calls to methods of restricted types 
            '       (those with IsRestrictedType = True); Roslyn uses 'call' to match C# behavior
            Return containingType.IsIntrinsicType OrElse containingType.IsRestrictedType
        End Function

        Private Sub EmitTypeOfExpression(expression As BoundTypeOf, used As Boolean, Optional optimize As Boolean = False)
            Dim operand = expression.Operand

            Debug.Assert(operand.Type.IsReferenceType AndAlso Not operand.Type.IsTypeParameter(), "operand.Type.IsReferenceType")

            EmitExpression(operand, True)

            If used Then

                Dim typeFrom = operand.Type
                Dim typeTo = expression.TargetType

                _builder.EmitOpCode(ILOpCode.Isinst)
                EmitSymbolToken(typeTo, expression.Syntax)

                ' If this expression is the condition of an If we can save these instructions
                ' and let the parent condition emit branches instead
                If Not optimize Then
                    _builder.EmitOpCode(ILOpCode.Ldnull)

                    If expression.IsTypeOfIsNotExpression Then
                        _builder.EmitOpCode(ILOpCode.Ceq)
                    Else
                        _builder.EmitOpCode(ILOpCode.Cgt_un)
                    End If
                End If
            End If

            EmitPopIfUnused(used)

        End Sub

        ''' <summary>
        ''' Emit code for a ternary conditional operator.
        ''' </summary>
        ''' <remarks>
        ''' if (b, x, y) becomes
        '''     push b
        '''     if pop then goto CONSEQUENCE
        '''     push y
        '''     goto DONE
        '''   CONSEQUENCE:
        '''     push x
        '''   DONE:
        ''' </remarks>
        Private Sub EmitTernaryConditionalExpression(expr As BoundTernaryConditionalExpression, used As Boolean)
            Debug.Assert(expr.ConstantValueOpt Is Nothing, "Constant value should have been emitted directly")

            Dim consequenceLabel = New Object()
            Dim doneLabel = New Object()

            EmitCondBranch(expr.Condition, consequenceLabel, sense:=True)
            EmitExpression(expr.WhenFalse, used)

            '
            ' III.1.8.1.3 Merging stack states
            ' . . . 
            ' Let T be the type from the slot on the newly computed state and S
            ' be the type from the corresponding slot on the previously stored state. The merged type, U, shall
            ' be computed as follows (recall that S := T is the compatibility function defined
            ' in §III.1.8.1.2.2):
            ' 1. if S := T then U=S
            ' 2. Otherwise, if T := S then U=T
            ' 3. Otherwise, if S and T are both object types, then let V be the closest common supertype of S and T then U=V.
            ' 4. Otherwise, the merge shall fail.
            '
            ' The issue is that, if the types don't match exactly but share an interface,
            ' there's no guarantee that the runtime will be able to find the interface.
            ' However, if you convert one of the types to the interface type, then it will
            ' work for both expression, and either (1) or (2) will succeed.
            ' This explains both (a) why this only applies to interfaces and (b) why it's
            ' okay to only explicitly convert one branch.
            '
            Dim mergeTypeOfAlternative As TypeSymbol = StackMergeType(expr.WhenFalse)
            If (used) Then
                If (IsVarianceCast(expr.Type, mergeTypeOfAlternative)) Then
                    EmitStaticCast(expr.Type, expr.Syntax)
                    mergeTypeOfAlternative = expr.Type
                End If
            End If

            _builder.EmitBranch(ILOpCode.Br, doneLabel)
            If (used) Then
                ' If we get to consequenceLabel, we should not have WhenFalse on stack, adjust for that.
                _builder.AdjustStack(-1)
            End If

            _builder.MarkLabel(consequenceLabel)
            EmitExpression(expr.WhenTrue, used)

            If (used) Then
                Dim mergeTypeOfConsequence As TypeSymbol = StackMergeType(expr.WhenTrue)
                If (IsVarianceCast(expr.Type, mergeTypeOfConsequence)) Then
                    EmitStaticCast(expr.Type, expr.Syntax)
                    mergeTypeOfConsequence = expr.Type

                ElseIf (expr.Type.IsInterfaceType() AndAlso Not TypeSymbol.Equals(expr.Type, mergeTypeOfAlternative, TypeCompareKind.ConsiderEverything) AndAlso Not TypeSymbol.Equals(expr.Type, mergeTypeOfConsequence, TypeCompareKind.ConsiderEverything)) Then
                    EmitStaticCast(expr.Type, expr.Syntax)
                End If
            End If

            _builder.MarkLabel(doneLabel)
        End Sub

        ''' <summary>
        ''' Emit code for a null-coalescing operator.
        ''' </summary>
        ''' <remarks>
        ''' if(x, y) becomes
        '''   push x
        '''   dup x
        '''   if pop isnot null goto LEFT_NOT_NULL
        '''     pop
        '''     push y
        '''   LEFT_NOT_NULL:
        ''' </remarks>
        Private Sub EmitBinaryConditionalExpression(expr As BoundBinaryConditionalExpression, used As Boolean)
            Debug.Assert(expr.ConvertedTestExpression Is Nothing, "coalesce with nontrivial test conversions are lowered into ternary.")
            Debug.Assert(TypeSymbol.Equals(expr.Type, expr.ElseExpression.Type, TypeCompareKind.ConsiderEverything))
            Debug.Assert(Not expr.Type.IsValueType)

            EmitExpression(expr.TestExpression, used:=True)

            ' See the notes about verification type merges in EmitConditionalOperator
            Dim mergeTypeOfLeftValue As TypeSymbol = StackMergeType(expr.TestExpression)
            If (used) Then
                If (IsVarianceCast(expr.Type, mergeTypeOfLeftValue)) Then
                    EmitStaticCast(expr.Type, expr.Syntax)
                    mergeTypeOfLeftValue = expr.Type
                End If

                _builder.EmitOpCode(ILOpCode.Dup)
            End If

            If (expr.Type.IsTypeParameter()) Then
                EmitBox(expr.Type, expr.TestExpression.Syntax)
            End If

            Dim ifLeftNotNullLabel = New Object()
            _builder.EmitBranch(ILOpCode.Brtrue, ifLeftNotNullLabel)

            If (used) Then
                _builder.EmitOpCode(ILOpCode.Pop)
            End If

            EmitExpression(expr.ElseExpression, used)
            If (used) Then
                Dim mergeTypeOfRightValue As TypeSymbol = StackMergeType(expr.ElseExpression)
                If (IsVarianceCast(expr.Type, mergeTypeOfRightValue)) Then
                    EmitStaticCast(expr.Type, expr.Syntax)
                    mergeTypeOfRightValue = expr.Type
                ElseIf (expr.Type.IsInterfaceType() AndAlso Not TypeSymbol.Equals(expr.Type, mergeTypeOfLeftValue, TypeCompareKind.ConsiderEverything) AndAlso Not TypeSymbol.Equals(expr.Type, mergeTypeOfRightValue, TypeCompareKind.ConsiderEverything)) Then
                    EmitStaticCast(expr.Type, expr.Syntax)
                End If
            End If

            _builder.MarkLabel(ifLeftNotNullLabel)
        End Sub

        ' Implicit casts are not emitted. As a result verifier may operate on a different 
        ' types from the types of operands when performing stack merges in coalesce/ternary.
        ' Such differences are in general irrelevant since merging rules work the same way
        ' for base and derived types.
        '
        ' Situation becomes more complicated with delegates, arrays and interfaces since they 
        ' allow implicit casts from types that do not derive from them. In such cases
        ' we may need to introduce static casts in the code to prod the verifier to the 
        ' right direction
        '
        ' This helper returns actual type of array|interface|delegate expression ignoring implicit 
        ' casts. This would be the effective stack merge type in the verifier.
        ' 
        ' NOTE: In cases where stack merge type cannot be determined, we just return null.
        '       We still must assume that it can be an array, delegate or interface though.
        Private Function StackMergeType(expr As BoundExpression) As TypeSymbol
            ' these cases are not interesting. Merge type is the same or derived. No difference.
            If (Not (expr.Type.IsArrayType OrElse expr.Type.IsInterfaceType OrElse expr.Type.IsDelegateType)) Then
                Return expr.Type
            End If

            ' Dig through casts. We only need to check for expressions that -
            ' 1) are implicit casts
            ' 2) may transparently return operands, so we need to dig deeper
            ' 3) stack values
            Select Case (expr.Kind)
                Case BoundKind.DirectCast
                    Dim conversion = DirectCast(expr, BoundDirectCast)
                    Dim conversionKind = conversion.ConversionKind
                    If (Conversions.IsWideningConversion(conversionKind)) Then
                        Return StackMergeType(conversion.Operand)
                    End If

                Case BoundKind.TryCast
                    Dim conversion = DirectCast(expr, BoundTryCast)
                    Dim conversionKind = conversion.ConversionKind
                    If (Conversions.IsWideningConversion(conversionKind)) Then
                        Return StackMergeType(conversion.Operand)
                    End If

                'Case BoundKind.Conversion
                '    Dim conversion = DirectCast(expr, BoundConversion)
                '    Dim conversionKind = conversion.ConversionKind
                '    If (Conversions.IsWideningConversion(conversionKind)) Then
                '        Return StackMergeType(conversion.Operand)
                '    End If

                Case BoundKind.AssignmentOperator
                    Dim assignment = DirectCast(expr, BoundAssignmentOperator)
                    Return StackMergeType(assignment.Right)

                Case BoundKind.Sequence
                    Dim sequence = DirectCast(expr, BoundSequence)
                    Return StackMergeType(sequence.ValueOpt)

                Case BoundKind.Local
                    Dim local = DirectCast(expr, BoundLocal)
                    If (Me.IsStackLocal(local.LocalSymbol)) Then
                        ' stack value, we cannot be sure what it is
                        Return Nothing
                    End If

                Case BoundKind.Dup
                    ' stack value, we cannot be sure what it is
                    Return Nothing
            End Select

            Return expr.Type
        End Function

        ' Although III.1.8.1.3 seems to imply that verifier understands variance casts.
        ' It appears that verifier/JIT gets easily confused. 
        ' So to not rely on whether that should work or not we will flag potentially 
        ' "complicated" casts and make them static casts to ensure we are all on 
        ' the same page with what type should be tracked.
        Private Shared Function IsVarianceCast(toType As TypeSymbol, fromType As TypeSymbol) As Boolean
            If (TypeSymbol.Equals(toType, fromType, TypeCompareKind.ConsiderEverything)) Then
                Return False
            End If

            If (fromType Is Nothing) Then
                ' from unknown type - this could be a variance conversion.
                Return True
            End If

            ' while technically variance casts, array conversions do not seem to be a problem
            ' unless the element types are converted via variance.
            If (toType.IsArrayType) Then
                Return IsVarianceCast(DirectCast(toType, ArrayTypeSymbol).ElementType, DirectCast(fromType, ArrayTypeSymbol).ElementType)
            End If

            Return (toType.IsDelegateType() AndAlso Not TypeSymbol.Equals(toType, fromType, TypeCompareKind.ConsiderEverything)) OrElse
                   (toType.IsInterfaceType() AndAlso fromType.IsInterfaceType() AndAlso
                    Not fromType.InterfacesAndTheirBaseInterfacesNoUseSiteDiagnostics.ContainsKey(DirectCast(toType, NamedTypeSymbol)))

        End Function

        Private Sub EmitStaticCast(toType As TypeSymbol, syntax As SyntaxNode)
            Debug.Assert(toType.IsVerifierReference())

            ' From ILGENREC::GenQMark
            ' See VSWhidbey Bugs #49619 and 108643. If the destination type is an interface we need
            ' to force a static cast to be generated for any cast result expressions. The static cast
            ' should be done before the unifying jump so the code is verifiable and to allow the JIT to
            ' optimize it away. NOTE: Since there is no staticcast instruction, we implement static cast
            ' with a stloc / ldloc to a temporary.
            ' Bug: VSWhidbey/49619
            ' Bug: VSWhidbey/108643
            ' Bug: DevDivBugs/42645

            Dim temp = AllocateTemp(toType, syntax)
            _builder.EmitLocalStore(temp)
            _builder.EmitLocalLoad(temp)
            FreeTemp(temp)
        End Sub

        Private Sub EmitArrayCreationExpression(expression As BoundArrayCreation, used As Boolean)
            Dim arrayType = DirectCast(expression.Type, ArrayTypeSymbol)

            EmitExpressions(expression.Bounds, True)

            If arrayType.IsSZArray Then
                _builder.EmitOpCode(ILOpCode.Newarr)
                EmitSymbolToken(arrayType.ElementType, expression.Syntax)
            Else
                _builder.EmitArrayCreation(_module.Translate(arrayType), expression.Syntax, _diagnostics)
            End If

            If expression.InitializerOpt IsNot Nothing Then
                EmitArrayInitializers(arrayType, expression.InitializerOpt)
            End If

            EmitPopIfUnused(used)
        End Sub

        Private Sub EmitArrayLengthExpression(expression As BoundArrayLength, used As Boolean)
            Debug.Assert(expression.Type.SpecialType = SpecialType.System_Int32 OrElse expression.Type.SpecialType = SpecialType.System_Int64 OrElse expression.Type.SpecialType = SpecialType.System_UIntPtr)
            EmitExpression(expression.Expression, used:=True)

            _builder.EmitOpCode(ILOpCode.Ldlen)

            Dim typeTo = expression.Type.PrimitiveTypeCode
            ' NOTE: ldlen returns native uint, but newarr takes native int, so the length value is always 
            '       a positive native int. We can treat it as either signed or unsigned.
            '       We will use whatever typeTo says so we do not need to convert because of sign.
            Dim typeFrom = If(typeTo.IsUnsigned(), Microsoft.Cci.PrimitiveTypeCode.UIntPtr, Microsoft.Cci.PrimitiveTypeCode.IntPtr)

            ' NOTE In Dev10 VB this cast Is checked.
            '
            ' Emitting checked conversion however results in redundant overflow checks on 64bit And also inhibits range check hoisting in loops.
            ' Therefore we will emit unchecked conversion here as C# compiler always did.
            _builder.EmitNumericConversion(typeFrom, typeTo, checked:=False)
            EmitPopIfUnused(used)
        End Sub

        Private Sub EmitObjectCreationExpression(expression As BoundObjectCreationExpression, used As Boolean)
            ' if there is no constructor or it is synthesized, we do not call it, call initobj instead
            If expression.IsDefaultValue Then
                EmitInitObj(expression.Type, used, expression.Syntax)
            Else
                Dim constructor As MethodSymbol = expression.ConstructorOpt
                EmitNewObj(expression.ConstructorOpt, expression.Arguments, used, expression.Syntax)
            End If
        End Sub

        Private Sub EmitInitObj(type As TypeSymbol, used As Boolean, syntaxNode As SyntaxNode)
            If (used) Then
                Dim temp = Me.AllocateTemp(type, syntaxNode)
                _builder.EmitLocalAddress(temp)                  '  ldloca temp
                _builder.EmitOpCode(ILOpCode.Initobj)            '  initobj  <MyStruct>
                EmitSymbolToken(type, syntaxNode)
                _builder.EmitLocalLoad(temp)                     '  ldloc temp
                FreeTemp(temp)
            End If
        End Sub

        Private Sub EmitNewObj(constructor As MethodSymbol,
                                arguments As ImmutableArray(Of BoundExpression),
                                used As Boolean,
                                syntaxNode As SyntaxNode)

            Debug.Assert(Not constructor.IsDefaultValueTypeConstructor(),
                         "do not call synthesized struct constructors, they do not exist")

            EmitArguments(arguments, constructor.Parameters)
            _builder.EmitOpCode(ILOpCode.Newobj, ILOpCode.Newobj.StackPushCount() - arguments.Length)
            EmitSymbolToken(constructor, syntaxNode)
            EmitPopIfUnused(used)
        End Sub

        Private Sub EmitLoadDefaultValueOfTypeParameter(type As TypeSymbol, used As Boolean, syntaxNode As SyntaxNode)
            Debug.Assert(type.IsTypeParameter)
            EmitLoadDefaultValueOfTypeFromNothingLiteral(type, used, syntaxNode)
        End Sub

        Private Sub EmitLoadDefaultValueOfTypeFromNothingLiteral(type As TypeSymbol, used As Boolean, syntaxNode As SyntaxNode)
            EmitInitObj(type, used, syntaxNode)
        End Sub

        Private Sub EmitStructConstructorCallOnTarget(constructor As MethodSymbol,
                                                      arguments As ImmutableArray(Of BoundExpression),
                                                      target As BoundExpression,
                                                      syntaxNode As VisualBasicSyntaxNode)

            Debug.Assert(target.IsLValue OrElse target.Kind = BoundKind.MeReference OrElse
                         (target.Kind = BoundKind.Local AndAlso DirectCast(target, BoundLocal).LocalSymbol.IsReadOnly))

            If target.Kind = BoundKind.Local AndAlso IsStackLocal(DirectCast(target, BoundLocal).LocalSymbol) Then
                ' A newobj for a struct will create a new object on the stack for stack locals
                EmitNewObj(constructor, arguments, True, syntaxNode)
                Return
            End If

            ' NOTE!!!: We are misusing isReadOnly here!!!
            '
            ' We are creating a fully modifiable reference to a struct in order to initialize it.
            ' In fact we are going to pass the reference as a byref arg to a constructor 
            ' (which can do whatever it wants with it - pass it byref to somebody else, etc...)
            '
            ' We are still going to say the reference is immutable. Since we are initializing, there is nothing to mutate.
            '
            ' Also note that we will not produce controlled mutability pointers here too.
            ' since the target is definitely a struct, it cannot be accessed covariantly.
            '
            EmitAddress(target, addressKind:=AddressKind.Immutable)
            ' otherwise instead of 'Initobj' the constructor should be called 

            ' NOTE: we don't call initobj before calling constructor following Dev10 implementation. This
            '       may cause errors in some scenarios in case constructor does not initialize all the fields
            ' TODO: revise?

            '  emit constructor call
            Dim stackBehavior = -constructor.ParameterCount - 1

            EmitArguments(arguments, constructor.Parameters)
            _builder.EmitOpCode(ILOpCode.Call, stackBehavior)
            EmitSymbolToken(constructor, syntaxNode)
        End Sub

        Private Sub EmitInitObjOnTarget(target As BoundExpression)
            ' NOTE!!!: We are misusing isReadOnly here!!!
            '
            ' We are creating a fully modifiable reference to a struct in order to initialize it.
            ' In fact we are going to wipe the target
            '
            ' We are still going to say the reference is immutable. Since we are initializing, 
            ' there is nothing to mutate.

            ' If we have to call an initobj a stack local, we still have to create a temp
            If target.Kind = BoundKind.Local AndAlso IsStackLocal(DirectCast(target, BoundLocal).LocalSymbol) Then
                EmitInitObj(target.Type, True, target.Syntax)
                Return
            End If

            ' Stack local should be on stack
            Debug.Assert(target.Kind <> BoundKind.Local OrElse Not IsStackLocal(DirectCast(target, BoundLocal).LocalSymbol))

            EmitAddress(target, addressKind:=AddressKind.Immutable)
            _builder.EmitOpCode(ILOpCode.Initobj)
            EmitSymbolToken(target.Type, target.Syntax)
        End Sub

        Private Sub EmitConstantExpression(type As TypeSymbol, constantValue As ConstantValue, used As Boolean, syntaxNode As SyntaxNode)
            ' unused constant has no side-effects
            If used Then
                ' Null type parameter values must be emitted as 'initobj' rather than 'ldnull'.
                If ((type IsNot Nothing) AndAlso (type.TypeKind = TypeKind.TypeParameter) AndAlso constantValue.IsNull) Then
                    EmitInitObj(type, used, syntaxNode)
                Else
                    _builder.EmitConstantValue(constantValue)
                End If
            End If
        End Sub

        Private Sub EmitConstantExpression(expression As BoundExpression)
            _builder.EmitConstantValue(expression.ConstantValueOpt)
        End Sub

        Private Sub EmitAssignmentExpression(assignmentOperator As BoundAssignmentOperator, used As Boolean)
            If Me.TryEmitAssignmentInPlace(assignmentOperator, used) Then
                Return
            End If

            ' Assignment expression codegen has the following parts:
            '
            ' * PreRHS: We need to emit instructions before the load of the right hand side if:
            '   - If the left hand side is a ref local or ref formal parameter and the right hand 
            '     side is a value then we must put the ref on the stack early so that we can store 
            '     indirectly into it.
            '   - If the left hand side is an array slot then we must evaluate the array and indices
            '     before we evaluate the right hand side. We ensure that the array and indices are 
            '     on the stack when the store is executed.
            '   - Similarly, if the left hand side is a non-static field then its receiver must be
            '     evaluated before the right hand side.
            '
            ' * RHS: There are three possible ways to do an assignment with respect to "refness", 
            '   and all are found in the lowering of:
            '
            '   N().s += 10;
            '
            '   That expression is realized as 
            '
            '   ref int addr = ref N().s;   ' Assign a ref on the right hand side to the left hand side.
            '   int sum = addr + 10;        ' No refs at all; assign directly to sum.
            '   addr = sum;                 ' Assigns indirectly through the address.
            '
            '   - If we are in the first case then assignmentOperator.RefKind is Ref and the left hand side is a 
            '     ref local temporary. We simply assign the ref on the RHS to the storage on the LHS with no indirection.
            '
            '   - If we are in the second case then nothing is ref; we have a value on one side an a local on the other.
            '     Again, there is no indirection.
            ' 
            '   - If we are in the third case then we have a ref on the left and a value on the right. We must compute the
            '     value of the right hand side and then store it into the left hand side.
            '
            ' * Duplication: The result of an assignment operation is the value that was assigned. It is possible that 
            '   later codegen is expecting this value to be on the stack when we're done here. This is controlled by
            '   the "used" formal parameter. There are two possible cases:
            '   - If the preamble put stuff on the stack for the usage of the store, then we must not put an extra copy
            '     of the right hand side value on the stack; that will be between the value and the stuff needed to 
            '     do the storage. In that case we put the right hand side value in a temporary and restore it later.
            '   - Otherwise we can just do a dup instruction; there's nothing before the dup on the stack that we'll need.
            ' 
            ' * Storage: Either direct or indirect, depending. See the RHS section above for details.
            ' 
            ' * Post-storage: If we stashed away the duplicated value in the temporary, we need to restore it back to the stack.

            Dim lhsUsesStack As Boolean = Me.EmitAssignmentPreamble(assignmentOperator.Left)
            Me.EmitExpression(assignmentOperator.Right, used:=True)
            Dim temp As LocalDefinition = Me.EmitAssignmentDuplication(assignmentOperator, used, lhsUsesStack)
            Me.EmitStore(assignmentOperator.Left)
            Me.EmitAssignmentPostfix(temp)
        End Sub

        ' sometimes it is possible and advantageous to get an address of the lHS and 
        ' perform assignment as an in-place initialization via initobj or constructor invocation.
        '
        ' 1) initobj 
        '    is used when assigning default value to T that is not a verifier reference.
        '
        ' 2) inplace ctor call 
        '    is used when assigning a freshly created struct. "x = new S(arg)" can be
        '    replaced by x.S(arg) as long as partial assignment cannot be observed -
        '    i.e. target must not be on the heap and we should not be in a try block.
        Private Function TryEmitAssignmentInPlace(assignmentOperator As BoundAssignmentOperator, used As Boolean) As Boolean
            Dim left As BoundExpression = assignmentOperator.Left

            ' if result is used, and lives on heap, we must keep RHS value on the stack.
            ' otherwise we can try conjuring up the RHS value directly where it belongs.
            If used AndAlso Not Me.TargetIsNotOnHeap(left) Then
                Return False
            End If

            If Not SafeToGetWriteableReference(left) Then
                ' cannot take a ref
                Return False
            End If

            Dim right As BoundExpression = assignmentOperator.Right
            Dim rightType = right.Type

            If Not rightType.IsTypeParameter Then
                If rightType.IsReferenceType OrElse (right.ConstantValueOpt IsNot Nothing AndAlso rightType.SpecialType <> SpecialType.System_Decimal) Then
                    ' in-place is not advantageous for reference types or constants
                    Return False
                End If
            End If

            If right.IsDefaultValue() Then
                Me.InPlaceInit(left, used)
                Return True
            Else
                If right.Kind = BoundKind.ObjectCreationExpression Then
                    ' It is desirable to do in-place ctor call if possible.
                    ' we could do newobj/stloc, but inplace call 
                    ' produces same or better code in current JITs 
                    If Me.PartialCtorResultCannotEscape(left) Then
                        Dim objCreation As BoundObjectCreationExpression = DirectCast(right, BoundObjectCreationExpression)
                        Me.InPlaceCtorCall(left, objCreation, used)
                        Return True
                    End If
                End If
            End If

            Return False
        End Function

        ' because of array covariance, taking a reference to an element of 
        ' generic array may fail even though assignment "arr(i) = Nothing" would always succeed.
        Private Function SafeToGetWriteableReference(left As BoundExpression) As Boolean
            Return AllowedToTakeRef(left, AddressKind.Writeable) AndAlso Not (left.Kind = BoundKind.ArrayAccess AndAlso left.Type.TypeKind = TypeKind.TypeParameter)
        End Function


        Private Sub InPlaceInit(target As BoundExpression, used As Boolean)
            Dim temp = Me.EmitAddress(target, AddressKind.Writeable)
            Debug.Assert(temp Is Nothing, "temp is not expected when in-place assigning")

            Me._builder.EmitOpCode(ILOpCode.Initobj)    '  initobj  <MyStruct>
            Me.EmitSymbolToken(target.Type, target.Syntax)

            If used Then
                Debug.Assert(Me.TargetIsNotOnHeap(target), "cannot read-back the target since it could have been modified")
                Me.EmitExpression(target, used = True)
            End If
        End Sub

        Private Sub InPlaceCtorCall(target As BoundExpression, objCreation As BoundObjectCreationExpression, used As Boolean)
            Dim temp = Me.EmitAddress(target, AddressKind.Writeable)
            Debug.Assert(temp Is Nothing, "temp is not expected when in-place assigning")

            Dim constructor As MethodSymbol = objCreation.ConstructorOpt
            Me.EmitArguments(objCreation.Arguments, constructor.Parameters)

            ' +1 to adjust for consumed target address
            Dim stackAdjustment As Integer = constructor.ParameterCount + 1
            Me._builder.EmitOpCode(ILOpCode.[Call], -stackAdjustment)
            Me.EmitSymbolToken(constructor, objCreation.Syntax)

            If used Then
                Debug.Assert(Me.TargetIsNotOnHeap(target), "cannot read-back the target since it could have been modified")
                Me.EmitExpression(target, used = True)
            End If
        End Sub

        ' partial ctor results are not observable when target is not on the heap.
        ' we also must not be in a try, otherwise if ctor throws
        ' partially assigned value may be observed in the handler.
        Private Function PartialCtorResultCannotEscape(left As BoundExpression) As Boolean
            Return Me._tryNestingLevel = 0 AndAlso Me.TargetIsNotOnHeap(left)
        End Function

        Private Function TargetIsNotOnHeap(left As BoundExpression) As Boolean
            Select Case left.Kind
                Case BoundKind.Local
                    Return Not DirectCast(left, BoundLocal).LocalSymbol.IsByRef

                Case BoundKind.Parameter
                    Return Not DirectCast(left, BoundParameter).ParameterSymbol.IsByRef

                Case BoundKind.ReferenceAssignment
                    Return False
            End Select
            Return False
        End Function

        Private Function EmitAssignmentPreamble(assignmentTarget As BoundExpression) As Boolean
            Dim lhsUsesStack = False

            Select Case assignmentTarget.Kind
                Case BoundKind.Local
                    Dim boundLocal = DirectCast(assignmentTarget, BoundLocal)
                    If boundLocal.LocalSymbol.IsByRef Then
                        If IsStackLocal(boundLocal.LocalSymbol) Then
                            ' the address is supposed to already be on stack 
                        Else
                            _builder.EmitLocalLoad(GetLocal(boundLocal))
                        End If
                        lhsUsesStack = True
                    End If

                Case BoundKind.ReferenceAssignment
                    EmitReferenceAssignment(DirectCast(assignmentTarget, BoundReferenceAssignment), used:=True, needReference:=True)
                    lhsUsesStack = True

                Case BoundKind.FieldAccess
                    Dim left = DirectCast(assignmentTarget, BoundFieldAccess)
                    If Not left.FieldSymbol.IsShared Then
                        ' we will not write to the receiver, but will write into its field.
                        Dim temp = EmitReceiverRef(left.ReceiverOpt, isAccessConstrained:=False, addressKind:=AddressKind.ReadOnly)
                        Debug.Assert(temp Is Nothing, "temp is unexpected when writing to a field")

                        lhsUsesStack = True
                    End If

                Case BoundKind.Parameter
                    Dim left = DirectCast(assignmentTarget, BoundParameter)
                    If left.ParameterSymbol.IsByRef Then
                        _builder.EmitLoadArgumentOpcode(ParameterSlot(left))
                        lhsUsesStack = True
                    End If

                Case BoundKind.ArrayAccess
                    Dim left = DirectCast(assignmentTarget, BoundArrayAccess)
                    EmitExpression(left.Expression, True)
                    EmitExpressions(left.Indices, True)
                    lhsUsesStack = True

                Case BoundKind.MeReference
                    Dim left = DirectCast(assignmentTarget, BoundMeReference)

                    ' why do we even need to handle this case? VB doesn't allow assigning to 'Me'.
                    Debug.Assert(False)

                    Dim temp = EmitAddress(left, addressKind:=AddressKind.Writeable)
                    Debug.Assert(temp Is Nothing, "taking ref of Me should not create a temp")
                    lhsUsesStack = True

                Case BoundKind.PseudoVariable
                    EmitPseudoVariableAddress(DirectCast(assignmentTarget, BoundPseudoVariable))
                    lhsUsesStack = True

                Case BoundKind.Sequence
                    Dim sequence = DirectCast(assignmentTarget, BoundSequence)

                    If Not sequence.Locals.IsEmpty Then
                        _builder.OpenLocalScope()

                        For Each local In sequence.Locals
                            Me.DefineLocal(local, sequence.Syntax)
                        Next
                    End If

                    Me.EmitSideEffects(sequence.SideEffects)
                    lhsUsesStack = EmitAssignmentPreamble(sequence.ValueOpt)

                Case BoundKind.Call
                    Dim left = DirectCast(assignmentTarget, BoundCall)
                    Debug.Assert(left.Method.ReturnsByRef)
                    EmitCallExpression(left, UseKind.UsedAsAddress)
                    lhsUsesStack = True

                Case BoundKind.ModuleVersionId, BoundKind.InstrumentationPayloadRoot

                Case Else
                    Throw ExceptionUtilities.UnexpectedValue(assignmentTarget.Kind)

            End Select

            Return lhsUsesStack
        End Function

        Private Function EmitAssignmentDuplication(assignmentOperator As BoundAssignmentOperator, used As Boolean, lhsUsesStack As Boolean) As LocalDefinition
            Dim temp As LocalDefinition = Nothing
            If used Then
                Me._builder.EmitOpCode(ILOpCode.Dup)
                If lhsUsesStack Then
                    temp = Me.AllocateTemp(assignmentOperator.Left.Type, assignmentOperator.Left.Syntax)
                    Me._builder.EmitLocalStore(temp)
                End If
            End If
            Return temp
        End Function

        Private Sub EmitAssignmentPostfix(temp As LocalDefinition)
            If temp IsNot Nothing Then
                Me._builder.EmitLocalLoad(temp)
                Me.FreeTemp(temp)
            End If
        End Sub

        Private Sub EmitReferenceAssignment(capture As BoundReferenceAssignment, used As Boolean, Optional needReference As Boolean = False)
            Debug.Assert(Not needReference OrElse used)

            Dim temp = EmitAddress(capture.LValue, addressKind:=AddressKind.Writeable)

            Debug.Assert(temp Is Nothing, "reference assignment should not clone the referent")

            If used Then
                _builder.EmitOpCode(ILOpCode.Dup)
            End If

            Dim boundLocal As BoundLocal = capture.ByRefLocal
            Debug.Assert(boundLocal.LocalSymbol.IsByRef)

            If IsStackLocal(boundLocal.LocalSymbol) Then
                ' just leave the address on stack
            Else
                Dim local = GetLocal(boundLocal)
                _builder.EmitLocalStore(local)
            End If

            If used AndAlso Not needReference Then
                EmitLoadIndirect(capture.Type, capture.Syntax)
            End If
        End Sub

        Private Sub EmitStore(expression As BoundExpression)
            Select Case expression.Kind
                Case BoundKind.FieldAccess
                    EmitFieldStore(DirectCast(expression, BoundFieldAccess))

                Case BoundKind.Local
                    Dim boundLocal = DirectCast(expression, BoundLocal)

                    If boundLocal.LocalSymbol.IsByRef Then
                        EmitStoreIndirect(boundLocal.LocalSymbol.Type, expression.Syntax)
                    ElseIf IsStackLocal(boundLocal.LocalSymbol) Then
                        ' just leave original value on stack
                    Else
                        Dim local = GetLocal(boundLocal)
                        _builder.EmitLocalStore(local)
                    End If

                Case BoundKind.ReferenceAssignment,
                     BoundKind.PseudoVariable
                    EmitStoreIndirect(expression.Type, expression.Syntax)

                Case BoundKind.ArrayAccess
                    Dim array = DirectCast(expression, BoundArrayAccess).Expression
                    Dim arrayType = DirectCast(array.Type, ArrayTypeSymbol)
                    EmitArrayElementStore(arrayType, expression.Syntax)

                Case BoundKind.MeReference
                    EmitMeStore(DirectCast(expression, BoundMeReference))

                Case BoundKind.Parameter
                    EmitParameterStore(DirectCast(expression, BoundParameter))

                Case BoundKind.Sequence
                    Dim sequence = DirectCast(expression, BoundSequence)
                    EmitStore(sequence.ValueOpt)

                    If Not sequence.Locals.IsEmpty Then
                        _builder.CloseLocalScope()

                        For Each local In sequence.Locals
                            Me.FreeLocal(local)
                        Next
                    End If

                Case BoundKind.Call
                    Debug.Assert(DirectCast(expression, BoundCall).Method.ReturnsByRef)
                    EmitStoreIndirect(expression.Type, expression.Syntax)

                Case BoundKind.ModuleVersionId
                    EmitModuleVersionIdStore(DirectCast(expression, BoundModuleVersionId))

                Case BoundKind.InstrumentationPayloadRoot
                    EmitInstrumentationPayloadRootStore(DirectCast(expression, BoundInstrumentationPayloadRoot))

                Case Else
                    Throw ExceptionUtilities.UnexpectedValue(expression.Kind)
            End Select
        End Sub

        Private Sub EmitMeStore(thisRef As BoundMeReference)
            Debug.Assert(thisRef.Type.IsValueType)

            _builder.EmitOpCode(ILOpCode.Stobj)
            EmitSymbolToken(thisRef.Type, thisRef.Syntax)
        End Sub

        Private Sub EmitArrayElementStore(arrayType As ArrayTypeSymbol, syntaxNode As SyntaxNode)
            If arrayType.IsSZArray Then
                EmitVectorElementStore(arrayType, syntaxNode)
            Else
                _builder.EmitArrayElementStore(_module.Translate(arrayType), syntaxNode, _diagnostics)
            End If
        End Sub

        ''' <summary>
        ''' Emit an element store instruction for a single dimensional array.
        ''' </summary>
        Private Sub EmitVectorElementStore(arrayType As ArrayTypeSymbol, syntaxNode As SyntaxNode)
            Dim elementType = arrayType.ElementType

            If elementType.IsEnumType() Then
                'underlying primitives do not need type tokens.
                elementType = (DirectCast(elementType, NamedTypeSymbol)).EnumUnderlyingType
            End If

            Select Case elementType.PrimitiveTypeCode
                Case Microsoft.Cci.PrimitiveTypeCode.Boolean,
                    Microsoft.Cci.PrimitiveTypeCode.Int8,
                    Microsoft.Cci.PrimitiveTypeCode.UInt8
                    _builder.EmitOpCode(ILOpCode.Stelem_i1)

                Case Microsoft.Cci.PrimitiveTypeCode.Char,
                    Microsoft.Cci.PrimitiveTypeCode.Int16,
                    Microsoft.Cci.PrimitiveTypeCode.UInt16
                    _builder.EmitOpCode(ILOpCode.Stelem_i2)

                Case Microsoft.Cci.PrimitiveTypeCode.Int32,
                    Microsoft.Cci.PrimitiveTypeCode.UInt32
                    _builder.EmitOpCode(ILOpCode.Stelem_i4)

                Case Microsoft.Cci.PrimitiveTypeCode.Int64,
                    Microsoft.Cci.PrimitiveTypeCode.UInt64
                    _builder.EmitOpCode(ILOpCode.Stelem_i8)

                Case Microsoft.Cci.PrimitiveTypeCode.IntPtr,
                    Microsoft.Cci.PrimitiveTypeCode.UIntPtr,
                    Microsoft.Cci.PrimitiveTypeCode.Pointer
                    _builder.EmitOpCode(ILOpCode.Stelem_i)

                Case Microsoft.Cci.PrimitiveTypeCode.Float32
                    _builder.EmitOpCode(ILOpCode.Stelem_r4)

                Case Microsoft.Cci.PrimitiveTypeCode.Float64
                    _builder.EmitOpCode(ILOpCode.Stelem_r8)

                Case Else
                    If IsVerifierReference(elementType) Then
                        _builder.EmitOpCode(ILOpCode.Stelem_ref)
                    Else
                        _builder.EmitOpCode(ILOpCode.Stelem)
                        EmitSymbolToken(elementType, syntaxNode)
                    End If

            End Select
        End Sub

        Private Sub EmitFieldStore(fieldAccess As BoundFieldAccess)
            Dim field As FieldSymbol = fieldAccess.FieldSymbol

            If field.IsShared Then
                _builder.EmitOpCode(ILOpCode.Stsfld)
            Else
                _builder.EmitOpCode(ILOpCode.Stfld)
            End If

            EmitSymbolToken(field, fieldAccess.Syntax)
        End Sub

        Private Sub EmitParameterStore(parameter As BoundParameter)

            If Not parameter.ParameterSymbol.IsByRef Then
                Dim slot As Integer = ParameterSlot(parameter)
                _builder.EmitStoreArgumentOpcode(slot)
            Else
                'NOTE: we should have the actual parameter already loaded,
                'now need to do a store to where it points to
                EmitStoreIndirect(parameter.ParameterSymbol.Type, parameter.Syntax)
            End If
        End Sub

        Private Sub EmitStoreIndirect(type As TypeSymbol, syntaxNode As SyntaxNode)
            If type.IsEnumType() Then
                type = (DirectCast(type, NamedTypeSymbol)).EnumUnderlyingType
            End If

            Select Case type.PrimitiveTypeCode
                Case Microsoft.Cci.PrimitiveTypeCode.Boolean,
                    Microsoft.Cci.PrimitiveTypeCode.Int8,
                    Microsoft.Cci.PrimitiveTypeCode.UInt8
                    _builder.EmitOpCode(ILOpCode.Stind_i1)

                Case Microsoft.Cci.PrimitiveTypeCode.Char,
                    Microsoft.Cci.PrimitiveTypeCode.Int16,
                    Microsoft.Cci.PrimitiveTypeCode.UInt16
                    _builder.EmitOpCode(ILOpCode.Stind_i2)

                Case Microsoft.Cci.PrimitiveTypeCode.Int32,
                    Microsoft.Cci.PrimitiveTypeCode.UInt32
                    _builder.EmitOpCode(ILOpCode.Stind_i4)

                Case Microsoft.Cci.PrimitiveTypeCode.Int64,
                    Microsoft.Cci.PrimitiveTypeCode.UInt64
                    _builder.EmitOpCode(ILOpCode.Stind_i8)

                Case Microsoft.Cci.PrimitiveTypeCode.IntPtr,
                    Microsoft.Cci.PrimitiveTypeCode.UIntPtr,
                    Microsoft.Cci.PrimitiveTypeCode.Pointer
                    _builder.EmitOpCode(ILOpCode.Stind_i)

                Case Microsoft.Cci.PrimitiveTypeCode.Float32
                    _builder.EmitOpCode(ILOpCode.Stind_r4)

                Case Microsoft.Cci.PrimitiveTypeCode.Float64
                    _builder.EmitOpCode(ILOpCode.Stind_r8)

                Case Else
                    If IsVerifierReference(type) Then
                        _builder.EmitOpCode(ILOpCode.Stind_ref)
                    Else
                        _builder.EmitOpCode(ILOpCode.Stobj)
                        EmitSymbolToken(type, syntaxNode)
                    End If

            End Select
        End Sub

        Private Sub EmitPopIfUnused(used As Boolean)
            If Not used Then
                _builder.EmitOpCode(ILOpCode.Pop)
            End If
        End Sub

        Private Sub EmitGetType(boundTypeOfOperator As BoundGetType, used As Boolean)
            ' an unused GetType can have side effects because it can e.g. throw a TypeLoadException

            Dim type As TypeSymbol = boundTypeOfOperator.SourceType.Type

            _builder.EmitOpCode(ILOpCode.Ldtoken)
            EmitSymbolToken(type, boundTypeOfOperator.SourceType.Syntax)

            _builder.EmitOpCode(ILOpCode.Call, stackAdjustment:=0) 'argument off, return value on
            Dim getTypeMethod = DirectCast(Me._module.Compilation.GetWellKnownTypeMember(WellKnownMember.System_Type__GetTypeFromHandle), MethodSymbol)
            Debug.Assert(getTypeMethod IsNot Nothing) ' Should have been checked during binding
            EmitSymbolToken(getTypeMethod, boundTypeOfOperator.Syntax)
            EmitPopIfUnused(used)
        End Sub

        Private Sub EmitFieldInfoExpression(node As BoundFieldInfo, used As Boolean)
            _builder.EmitOpCode(ILOpCode.Ldtoken)
            EmitSymbolToken(node.Field, node.Syntax)
            Dim getField As MethodSymbol
            If Not node.Field.ContainingType.IsGenericType Then
                Debug.Assert(Not node.Field.ContainingType.IsAnonymousType) ' NO anonymous types field access expected

                _builder.EmitOpCode(ILOpCode.Call, stackAdjustment:=0) ' argument off, return value on
                getField = DirectCast(Me._module.Compilation.GetWellKnownTypeMember(WellKnownMember.System_Reflection_FieldInfo__GetFieldFromHandle), MethodSymbol)
            Else
                _builder.EmitOpCode(ILOpCode.Ldtoken)
                EmitSymbolToken(node.Field.ContainingType, node.Syntax)
                _builder.EmitOpCode(ILOpCode.Call, stackAdjustment:=-1) ' 2 arguments off, return value on
                getField = DirectCast(Me._module.Compilation.GetWellKnownTypeMember(WellKnownMember.System_Reflection_FieldInfo__GetFieldFromHandle2), MethodSymbol)
            End If

            Debug.Assert(getField IsNot Nothing)
            EmitSymbolToken(getField, node.Syntax)
            If Not TypeSymbol.Equals(node.Type, getField.ReturnType, TypeCompareKind.ConsiderEverything) Then
                _builder.EmitOpCode(ILOpCode.Castclass)
                EmitSymbolToken(node.Type, node.Syntax)
            End If

            EmitPopIfUnused(used)
        End Sub

        Private Sub EmitMethodInfoExpression(node As BoundMethodInfo, used As Boolean)
            _builder.EmitOpCode(ILOpCode.Ldtoken)
            EmitSymbolToken(node.Method, node.Syntax)
            Dim getMethod As MethodSymbol
            If Not node.Method.ContainingType.IsGenericType AndAlso Not node.Method.ContainingType.IsAnonymousType Then ' anonymous types are generic under the hood.
                _builder.EmitOpCode(ILOpCode.Call, stackAdjustment:=0) ' argument off, return value on
                getMethod = DirectCast(Me._module.Compilation.GetWellKnownTypeMember(WellKnownMember.System_Reflection_MethodBase__GetMethodFromHandle), MethodSymbol)
            Else
                _builder.EmitOpCode(ILOpCode.Ldtoken)
                EmitSymbolToken(node.Method.ContainingType, node.Syntax)
                _builder.EmitOpCode(ILOpCode.Call, stackAdjustment:=-1) ' 2 arguments off, return value on
                getMethod = DirectCast(Me._module.Compilation.GetWellKnownTypeMember(WellKnownMember.System_Reflection_MethodBase__GetMethodFromHandle2), MethodSymbol)
            End If

            Debug.Assert(getMethod IsNot Nothing)
            EmitSymbolToken(getMethod, node.Syntax)
            If Not TypeSymbol.Equals(node.Type, getMethod.ReturnType, TypeCompareKind.ConsiderEverything) Then
                _builder.EmitOpCode(ILOpCode.Castclass)
                EmitSymbolToken(node.Type, node.Syntax)
            End If

            EmitPopIfUnused(used)
        End Sub

        Private Sub EmitBox(type As TypeSymbol, syntaxNode As SyntaxNode)
            _builder.EmitOpCode(ILOpCode.Box)
            EmitSymbolToken(type, syntaxNode)
        End Sub

        Private Sub EmitUnboxAny(type As TypeSymbol, syntaxNode As SyntaxNode)
            _builder.EmitOpCode(ILOpCode.Unbox_any)
            EmitSymbolToken(type, syntaxNode)
        End Sub

        Private Sub EmitMethodDefIndexExpression(node As BoundMethodDefIndex)
            Debug.Assert(node.Method.IsDefinition)
            Debug.Assert(node.Type.SpecialType = SpecialType.System_Int32)
            _builder.EmitOpCode(ILOpCode.Ldtoken)

            ' For partial methods, we emit pseudo token based on the symbol for the partial
            ' definition part as opposed to the symbol for the partial implementation part.
            ' We will need to resolve the symbol associated with each pseudo token in order
            ' to compute the real method definition tokens later. For partial methods, this
            ' resolution can only succeed if the associated symbol is the symbol for the
            ' partial definition and not the symbol for the partial implementation (see
            ' MethodSymbol.ResolvedMethodImpl()).
            Dim symbol = If(node.Method.PartialDefinitionPart, node.Method)

            EmitSymbolToken(symbol, node.Syntax, encodeAsRawDefinitionToken:=True)
        End Sub

        Private Sub EmitMaximumMethodDefIndexExpression(node As BoundMaximumMethodDefIndex)
            Debug.Assert(node.Type.SpecialType = SpecialType.System_Int32)
            _builder.EmitOpCode(ILOpCode.Ldtoken)
            _builder.EmitGreatestMethodToken()
        End Sub

        Private Sub EmitModuleVersionIdLoad(node As BoundModuleVersionId)
            _builder.EmitOpCode(ILOpCode.Ldsfld)
            EmitModuleVersionIdToken(node)
        End Sub

        Private Sub EmitModuleVersionIdStore(node As BoundModuleVersionId)
            _builder.EmitOpCode(ILOpCode.Stsfld)
            EmitModuleVersionIdToken(node)
        End Sub

        Private Sub EmitModuleVersionIdToken(node As BoundModuleVersionId)
            _builder.EmitToken(_module.GetModuleVersionId(_module.Translate(node.Type, node.Syntax, _diagnostics), node.Syntax, _diagnostics), node.Syntax, _diagnostics)
        End Sub

        Private Sub EmitModuleVersionIdStringLoad(node As BoundModuleVersionIdString)
            _builder.EmitOpCode(ILOpCode.Ldstr)
            _builder.EmitModuleVersionIdStringToken()
        End Sub

        Private Sub EmitInstrumentationPayloadRootLoad(node As BoundInstrumentationPayloadRoot)
            _builder.EmitOpCode(ILOpCode.Ldsfld)
            EmitInstrumentationPayloadRootToken(node)
        End Sub

        Private Sub EmitInstrumentationPayloadRootStore(node As BoundInstrumentationPayloadRoot)
            _builder.EmitOpCode(ILOpCode.Stsfld)
            EmitInstrumentationPayloadRootToken(node)
        End Sub

        Private Sub EmitInstrumentationPayloadRootToken(node As BoundInstrumentationPayloadRoot)
            _builder.EmitToken(_module.GetInstrumentationPayloadRoot(node.AnalysisKind, _module.Translate(node.Type, node.Syntax, _diagnostics), node.Syntax, _diagnostics), node.Syntax, _diagnostics)
        End Sub

        Private Sub EmitSourceDocumentIndex(node As BoundSourceDocumentIndex)
            Debug.Assert(node.Type.SpecialType = SpecialType.System_Int32)
            _builder.EmitOpCode(ILOpCode.Ldtoken)
            _builder.EmitSourceDocumentIndexToken(node.Document)
        End Sub

    End Class

End Namespace
