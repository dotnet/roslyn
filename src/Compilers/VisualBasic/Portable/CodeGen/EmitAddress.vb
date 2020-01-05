' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Reflection.Metadata
Imports Microsoft.CodeAnalysis.CodeGen
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols

Namespace Microsoft.CodeAnalysis.VisualBasic.CodeGen

    Friend Partial Class CodeGenerator

        ' VB has additional, stronger than CLR requirements on whether a reference to an item
        ' can be taken. 
        '
        ' In particular in VB read-only requirements are recursive.
        ' For example struct fields of readonly local are also considered readonly.
        ' If one needs to take a reference of such local it is not enough to not write to the
        ' reference itself. We need to guarantee that through such reference 
        ' nested fields will not be changed too. Otherwise a clone must be created.
        '
        ' On the other hand CLR only cares about shallow readonlyness.
        '
        ' To have enough information in both cases we use 3-state value (unlike C# that just uses bool)
        ' That specifies the context under which a reference is taken.
        Private Enum AddressKind
            ' reference may be written to
            Writeable

            ' reference itself will not be written to, but may be used to modify fields.
            [ReadOnly]

            ' will not directly or indirectly assign to the reference or to fields.
            Immutable
        End Enum

        ''' <summary>
        ''' Emits address as in &amp; 
        ''' 
        ''' May introduce a temp which it will return. (otherwise returns null)
        ''' </summary>
        Private Function EmitAddress(expression As BoundExpression, addressKind As AddressKind) As LocalDefinition
            Dim kind As BoundKind = expression.Kind
            Dim tempOpt As LocalDefinition = Nothing

            Dim allowedToTakeReference As Boolean = AllowedToTakeRef(expression, addressKind)

            If Not allowedToTakeReference Then
                ' language says expression should not be mutated. Emit address of a clone.
                Return EmitAddressOfTempClone(expression)
            End If

            Select Case kind
                Case BoundKind.Local
                    Dim boundLocal = DirectCast(expression, BoundLocal)
                    If IsStackLocal(boundLocal.LocalSymbol) Then
                        Debug.Assert(boundLocal.LocalSymbol.IsByRef) ' only allow byref locals in this context
                        ' do nothing, it should be on the stack
                    Else
                        Dim local = GetLocal(boundLocal)
                        _builder.EmitLocalAddress(local) ' EmitLocalAddress knows about byref locals
                    End If

                Case BoundKind.Dup
                    Debug.Assert(DirectCast(expression, BoundDup).IsReference, "taking address of a stack value?")
                    _builder.EmitOpCode(ILOpCode.Dup)

                Case BoundKind.ReferenceAssignment
                    EmitReferenceAssignment(DirectCast(expression, BoundReferenceAssignment), used:=True, needReference:=True)

                Case BoundKind.ConditionalAccessReceiverPlaceholder
                    ' do nothing receiver ref must be already pushed
                    Debug.Assert(Not expression.Type.IsReferenceType)
                    Debug.Assert(Not expression.Type.IsValueType)

                Case BoundKind.ComplexConditionalAccessReceiver
                    EmitComplexConditionalAccessReceiverAddress(DirectCast(expression, BoundComplexConditionalAccessReceiver))

                Case BoundKind.Parameter
                    EmitParameterAddress(DirectCast(expression, BoundParameter))

                Case BoundKind.FieldAccess
                    tempOpt = EmitFieldAddress(DirectCast(expression, BoundFieldAccess), addressKind)

                Case BoundKind.ArrayAccess
                    EmitArrayElementAddress(DirectCast(expression, BoundArrayAccess), addressKind)

                Case BoundKind.MeReference,
                    BoundKind.MyClassReference

                    Debug.Assert(expression.Type.IsValueType, "only valuetypes may need a ref to Me/MyClass")
                    _builder.EmitOpCode(ILOpCode.Ldarg_0)

                Case BoundKind.ValueTypeMeReference
                    _builder.EmitOpCode(ILOpCode.Ldarg_0)

                Case BoundKind.MyBaseReference
                    Debug.Assert(False, "base is always a reference type, why one may need a reference to it?")

                Case BoundKind.Parenthesized
                    ' rewriter should take care of Parenthesized
                    '
                    ' we do not know how to emit address of a parenthesized without context.
                    ' when it is an argument like  goo((arg)), it must be cloned, 
                    ' in other cases like (receiver).goo() it might not need to be...
                    '
                    Debug.Assert(False, "we should not see parenthesized in EmitAddress.")

                Case BoundKind.Sequence
                    tempOpt = EmitSequenceAddress(DirectCast(expression, BoundSequence), addressKind)

                Case BoundKind.SequencePointExpression
                    EmitSequencePointExpressionAddress(DirectCast(expression, BoundSequencePointExpression), addressKind)

                Case BoundKind.PseudoVariable
                    EmitPseudoVariableAddress(DirectCast(expression, BoundPseudoVariable))

                Case BoundKind.Call
                    Dim [call] = DirectCast(expression, BoundCall)
                    Debug.Assert([call].Method.ReturnsByRef)
                    EmitCallExpression([call], UseKind.UsedAsAddress)

                Case Else
                    Throw ExceptionUtilities.UnexpectedValue(kind)

            End Select

            Return tempOpt
        End Function

        Private Sub EmitPseudoVariableAddress(expression As BoundPseudoVariable)
            EmitExpression(expression.EmitExpressions.GetAddress(expression), used:=True)
        End Sub

        ''' <summary>
        ''' Emits address of a temp.
        ''' Used in cases where taking address directly is not possible 
        ''' (typically because expression does not have a home)
        ''' 
        ''' Will introduce a temp which it will return.
        ''' </summary>
        Private Function EmitAddressOfTempClone(expression As BoundExpression) As LocalDefinition
            EmitExpression(expression, True)
            Dim value = AllocateTemp(expression.Type, expression.Syntax)
            _builder.EmitLocalStore(value)
            _builder.EmitLocalAddress(value)
            Return value
        End Function

        Private Function EmitSequenceAddress(sequence As BoundSequence, addressKind As AddressKind) As LocalDefinition
            Dim hasLocals As Boolean = Not sequence.Locals.IsEmpty
            If hasLocals Then
                _builder.OpenLocalScope()

                For Each local In sequence.Locals
                    Me.DefineLocal(local, sequence.Syntax)
                Next
            End If

            Me.EmitSideEffects(sequence.SideEffects)
            Debug.Assert(sequence.ValueOpt IsNot Nothing)

            Dim tempOpt = Me.EmitAddress(sequence.ValueOpt, addressKind)

            ' when a sequence Is happened to be a byref receiver
            ' we may need to extend the life time of the target until we are done accessing it
            ' {.v ; v = Goo(); v}.Bar()     // v should be released after Bar() Is over.
            Dim doNotRelease As LocalSymbol = Nothing
            If (tempOpt Is Nothing) Then
                Dim referencedLocal As BoundLocal = DigForLocal(sequence.ValueOpt)
                If (referencedLocal IsNot Nothing) Then
                    doNotRelease = referencedLocal.LocalSymbol
                End If
            End If

            If hasLocals Then
                _builder.CloseLocalScope()

                For Each local In sequence.Locals
                    If (local IsNot doNotRelease) Then
                        FreeLocal(local)
                    Else
                        tempOpt = GetLocal(doNotRelease)
                    End If
                Next
            End If

            Return tempOpt
        End Function

        Private Function DigForLocal(value As BoundExpression) As BoundLocal
            Select Case value.Kind
                Case BoundKind.Local
                    Dim local = DirectCast(value, BoundLocal)
                    If Not local.LocalSymbol.IsByRef Then
                        Return local
                    End If

                Case BoundKind.Sequence
                    Return DigForLocal((DirectCast(value, BoundSequence)).ValueOpt)

                Case BoundKind.FieldAccess
                    Dim fieldAccess = DirectCast(value, BoundFieldAccess)
                    If Not fieldAccess.FieldSymbol.IsShared Then
                        Return DigForLocal(fieldAccess.ReceiverOpt)
                    End If
            End Select

            Return Nothing
        End Function

        ''' <summary>
        ''' Checks if expression represents directly or indirectly a value with its own home.
        ''' In such cases it is possible to get a reference without loading into a temporary.
        ''' 
        ''' This is a CLR concept which is weaker than VB's IsLValue.
        ''' For example all locals are homed even if VB may consider some locals read-only.
        ''' </summary>
        Private Function HasHome(expression As BoundExpression) As Boolean
            Select Case expression.Kind
                Case BoundKind.Sequence
                    Dim boundSequenceValue = DirectCast(expression, BoundSequence).ValueOpt
                    Return boundSequenceValue IsNot Nothing AndAlso Me.HasHome(boundSequenceValue)

                Case BoundKind.FieldAccess
                    Return HasHome(DirectCast(expression, BoundFieldAccess))

                Case BoundKind.MeReference,
                        BoundKind.MyBaseReference,
                        BoundKind.ArrayAccess,
                        BoundKind.ReferenceAssignment,
                        BoundKind.Parameter
                    Return True

                Case BoundKind.Local
                    'Note: in a case if we have a reference in a stack local, we definitely can "obtain" a reference. 
                    '      Unlike a case where we have a value.
                    Dim local = DirectCast(expression, BoundLocal).LocalSymbol
                    Return Not IsStackLocal(local) OrElse local.IsByRef

                Case BoundKind.Call
                    Dim method = DirectCast(expression, BoundCall).Method
                    Return method.ReturnsByRef

                Case BoundKind.Dup
                    ' For a dupped local we assume that if the dup 
                    ' is created for byref local it does have home
                    Return DirectCast(expression, BoundDup).IsReference

                Case BoundKind.ValueTypeMeReference
                    Return True

            End Select

            Return False
        End Function

        ''' <summary>
        ''' Special HasHome for fields. Fields have homes when they are writable.
        ''' </summary>
        Private Function HasHome(fieldAccess As BoundFieldAccess) As Boolean
            Dim field = fieldAccess.FieldSymbol

            ' const fields are literal values with no homes
            If field.IsConst AndAlso Not field.IsConstButNotMetadataConstant Then
                Return False
            End If

            If Not field.IsReadOnly Then
                Return True
            End If

            ' while readonly fields have home it is not valid to refer to it when not constructing.
            If Not TypeSymbol.Equals(field.ContainingType, Me._method.ContainingType, TypeCompareKind.ConsiderEverything) Then
                Return False
            End If

            If field.IsShared Then
                Return Me._method.MethodKind = MethodKind.SharedConstructor
            Else
                Return Me._method.MethodKind = MethodKind.Constructor AndAlso
                    fieldAccess.ReceiverOpt.Kind = BoundKind.MeReference
            End If
        End Function

        ''' <summary>
        ''' Checks if it is allowed to take a writable reference to expression according to VB rules.
        ''' </summary>
        Private Function AllowedToTakeRef(expression As BoundExpression, addressKind As AddressKind) As Boolean

            If expression.Kind = BoundKind.ConditionalAccessReceiverPlaceholder OrElse
               expression.Kind = BoundKind.ComplexConditionalAccessReceiver Then
                Return addressKind = AddressKind.ReadOnly OrElse addressKind = AddressKind.Immutable
            End If

            ' taking immutable addresses is ok as long as expression has home
            If addressKind <> AddressKind.Immutable Then

                Select Case expression.Kind
                    Case BoundKind.Sequence
                        Dim boundSequenceValue = DirectCast(expression, BoundSequence).ValueOpt
                        Return boundSequenceValue IsNot Nothing AndAlso Me.AllowedToTakeRef(boundSequenceValue, addressKind)

                    Case BoundKind.FieldAccess
                        Return AllowedToTakeRef(DirectCast(expression, BoundFieldAccess), addressKind)

                    Case BoundKind.Local
                        ' VB has concept of readonly locals. Some constants look like locals too.
                        Return AllowedToTakeRef(DirectCast(expression, BoundLocal), addressKind)

                    Case BoundKind.Parameter
                        ' parameters can be readonly (parameters in query lambdas) 
                        ' we ignore their read-onlyness for Dev10 compatibility
                        Return True

                    Case BoundKind.PseudoVariable
                        Return True

                    Case BoundKind.Dup
                        ' If this is a Dup of ByRef local we can use the address directly 
                        Return DirectCast(expression, BoundDup).IsReference

                    Case BoundKind.MeReference, BoundKind.MyClassReference
                        ' cannot modify Me/MyClass wholesale, but can modify fields
                        ' from within structure methods.
                        Return addressKind <> CodeGenerator.AddressKind.Writeable

                End Select
            End If

            Return HasHome(expression)
        End Function

        ''' <summary>
        ''' Checks if it is allowed to take a writable reference to expression according to VB rules.
        ''' </summary>
        Private Function AllowedToTakeRef(boundLocal As BoundLocal, addressKind As AddressKind) As Boolean
            Debug.Assert(addressKind <> CodeGenerator.AddressKind.Immutable, "immutable address is always ok")

            ' TODO: The condition is applicable only when LocalSymbol.IsReadOnly
            ' cannot write to a readonly local unless explicitly marked as an LValue
            If addressKind = CodeGenerator.AddressKind.Writeable AndAlso
                boundLocal.LocalSymbol.IsReadOnly AndAlso
                Not boundLocal.IsLValue Then

                Return False
            End If

            ' cannot take address of homeless local
            If Not HasHome(boundLocal) Then
                Return False
            End If

            'TODO:  we have to do the following to separate real locals
            '       from constants. 
            '       ideally we should not see local constants at this point,
            '       they all should be either true locals (decimal, datetime) or BoundLiterals.
            If boundLocal.IsConstant Then
                Dim localConstType = boundLocal.Type
                If Not localConstType.IsDecimalType AndAlso
                    Not localConstType.IsDateTimeType Then

                    ' this is not a local. It is a named literal.
                    Return False
                End If
            End If

            Return True
        End Function

        ''' <summary>
        ''' Can take a reference.
        ''' </summary>
        Private Function AllowedToTakeRef(fieldAccess As BoundFieldAccess, addressKind As AddressKind) As Boolean
            ' taking immutable addresses is ok as long as expression has home
            If addressKind <> AddressKind.Immutable Then

                ' If this field itself does not have home it cannot be mutated
                If Not HasHome(fieldAccess) Then
                    Return False
                End If

                ' fields of readonly structs are considered recursively readonly in VB

                If fieldAccess.FieldSymbol.ContainingType.IsValueType Then
                    Dim fieldReceiver = fieldAccess.ReceiverOpt

                    ' even when writing to a field, receiver is accessed as readonly
                    If fieldReceiver IsNot Nothing AndAlso Not AllowedToTakeRef(fieldReceiver, CodeGenerator.AddressKind.ReadOnly) Then
                        If Not HasHome(fieldReceiver) Then
                            ' can mutate field since its parent has no home and we will be dealing with a copy
                            Return True
                        Else
                            ' this field access is readonly due to language reasons -
                            ' most likely topmost receiver is a readonly local or a runtime const
                            Return False
                        End If
                    End If
                End If
            End If

            Return HasHome(fieldAccess)
        End Function

        Private Sub EmitArrayElementAddress(arrayAccess As BoundArrayAccess, addressKind As AddressKind)
            EmitExpression(arrayAccess.Expression, True)
            EmitExpressions(arrayAccess.Indices, True)

            Dim elementType As TypeSymbol = arrayAccess.Type

            'arrays are covariant, but elements can be written to.
            'the flag tells that we do not intend to use the address for writing.
            If addressKind <> AddressKind.Writeable AndAlso elementType.IsTypeParameter() Then
                _builder.EmitOpCode(ILOpCode.Readonly)
            End If

            If DirectCast(arrayAccess.Expression.Type, ArrayTypeSymbol).IsSZArray Then
                _builder.EmitOpCode(ILOpCode.Ldelema)
                EmitSymbolToken(elementType, arrayAccess.Syntax)
            Else
                _builder.EmitArrayElementAddress(_module.Translate(DirectCast(arrayAccess.Expression.Type, ArrayTypeSymbol)), arrayAccess.Syntax, _diagnostics)
            End If
        End Sub

        Private Function EmitFieldAddress(fieldAccess As BoundFieldAccess, addressKind As AddressKind) As LocalDefinition
            Dim field = fieldAccess.FieldSymbol

            If fieldAccess.FieldSymbol.IsShared Then
                EmitStaticFieldAddress(field, fieldAccess.Syntax)
                Return Nothing
            Else
                Return EmitInstanceFieldAddress(fieldAccess, addressKind)
            End If
        End Function

        Private Sub EmitStaticFieldAddress(field As FieldSymbol, syntaxNode As SyntaxNode)
            _builder.EmitOpCode(ILOpCode.Ldsflda)
            EmitSymbolToken(field, syntaxNode)
        End Sub

        Private Sub EmitParameterAddress(parameter As BoundParameter)
            Dim slot As Integer = ParameterSlot(parameter)
            If Not parameter.ParameterSymbol.IsByRef Then
                _builder.EmitLoadArgumentAddrOpcode(slot)
            Else
                _builder.EmitLoadArgumentOpcode(slot)
            End If
        End Sub

        ''' <summary>
        ''' Emits receiver in a form that allows member accesses ( O or &amp; ). For verifiably
        ''' reference types it is the actual reference. For generic types it is a address of the
        ''' receiver with readonly intent. For the value types it is an address of the receiver.
        ''' 
        ''' isAccessConstrained indicates that receiver is a target of a constrained callvirt
        ''' in such case it is unnecessary to box a receiver that is typed to a type parameter
        ''' 
        ''' May introduce a temp which it will return. (otherwise returns null)
        ''' </summary>
        Private Function EmitReceiverRef(receiver As BoundExpression,
                                         isAccessConstrained As Boolean,
                                         addressKind As AddressKind) As LocalDefinition

            Dim receiverType = receiver.Type

            If IsVerifierReference(receiverType) Then
                EmitExpression(receiver, used:=True)
                Return Nothing
            End If

            If receiverType.TypeKind = TypeKind.TypeParameter Then
                '[Note: Constraints on a generic parameter only restrict the types that 
                'the generic parameter may be instantiated with. Verification (see Partition III) 
                'requires that a field, property or method that a generic parameter is known 
                'to provide through meeting a constraint, cannot be directly accessed/called 
                'via the generic parameter unless it is first boxed (see Partition III) or 
                'the callvirt instruction is prefixed with the constrained. prefix instruction 
                '(see Partition III). end note]
                If isAccessConstrained Then
                    Return EmitAddress(receiver, AddressKind.ReadOnly)
                Else
                    EmitExpression(receiver, used:=True)

                    ' conditional receivers are already boxed if needed when pushed
                    If receiver.Kind <> BoundKind.ConditionalAccessReceiverPlaceholder Then
                        EmitBox(receiverType, receiver.Syntax)
                    End If

                    Return Nothing
                End If
            End If

            Debug.Assert(IsVerifierValue(receiverType))
            Return EmitAddress(receiver, addressKind)
        End Function

        ''' <summary>
        ''' May introduce a temp which it will return. (otherwise returns null)
        ''' </summary>
        Private Function EmitInstanceFieldAddress(fieldAccess As BoundFieldAccess, addressKind As AddressKind) As LocalDefinition
            Dim field = fieldAccess.FieldSymbol

            ' writing to a field is considered reading a receiver, unless receiver is a struct and not "Me"
            If addressKind = AddressKind.Writeable AndAlso IsMeReceiver(fieldAccess.ReceiverOpt) Then
                addressKind = AddressKind.ReadOnly
            End If

            Dim tempOpt = EmitReceiverRef(fieldAccess.ReceiverOpt, isAccessConstrained:=False, addressKind:=addressKind)

            Debug.Assert(HasHome(fieldAccess), "taking a ref of homeless field")
            _builder.EmitOpCode(ILOpCode.Ldflda)

            EmitSymbolToken(field, fieldAccess.Syntax)

            Return tempOpt
        End Function

    End Class

End Namespace

