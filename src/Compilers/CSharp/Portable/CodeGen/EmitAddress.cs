// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Reflection.Metadata;
using Microsoft.CodeAnalysis.CodeGen;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.CodeGen
{
    internal partial class CodeGenerator
    {
        private enum AddressKind
        {
            // reference may be written to
            Writeable,

            // reference itself will not be written to, but may be used to modify fields.
            ReadOnly
        }

        /// <summary>
        /// Emits address as in &amp; 
        /// 
        /// May introduce a temp which it will return. (otherwise returns null)
        /// </summary>
        private LocalDefinition EmitAddress(BoundExpression expression, AddressKind addressKind)
        {
            switch (expression.Kind)
            {
                case BoundKind.RefValueOperator:
                    EmitRefValueAddress((BoundRefValueOperator)expression);
                    break;

                case BoundKind.Local:
                    EmitLocalAddress((BoundLocal)expression);
                    break;

                case BoundKind.Dup:
                    Debug.Assert(((BoundDup)expression).RefKind != RefKind.None, "taking address of a stack value?");
                    _builder.EmitOpCode(ILOpCode.Dup);
                    break;

                case BoundKind.ConditionalReceiver:
                    // do nothing receiver ref must be already pushed
                    Debug.Assert(!expression.Type.IsReferenceType);
                    Debug.Assert(!expression.Type.IsValueType || expression.Type.IsNullableType());
                    break;

                case BoundKind.ComplexConditionalReceiver:
                    EmitComplexConditionalReceiverAddress((BoundComplexConditionalReceiver)expression);
                    break;

                case BoundKind.Parameter:
                    EmitParameterAddress((BoundParameter)expression);
                    break;

                case BoundKind.FieldAccess:
                    return EmitFieldAddress((BoundFieldAccess)expression);

                case BoundKind.ArrayAccess:
                    //arrays are covariant, but elements can be written to.
                    //the flag tells that we do not intend to use the address for writing.
                    EmitArrayElementAddress((BoundArrayAccess)expression, addressKind);
                    break;

                case BoundKind.ThisReference:
                    Debug.Assert(expression.Type.IsValueType, "only value types may need a ref to this");
                    _builder.EmitOpCode(ILOpCode.Ldarg_0);
                    break;

                case BoundKind.PreviousSubmissionReference:
                    // script references are lowered to a this reference and a field access
                    throw ExceptionUtilities.UnexpectedValue(expression.Kind);

                case BoundKind.BaseReference:
                    Debug.Assert(false, "base is always a reference type, why one may need a reference to it?");
                    break;

                case BoundKind.Sequence:
                    return EmitSequenceAddress((BoundSequence)expression, addressKind);

                case BoundKind.PointerIndirectionOperator:
                    // The address of a dereferenced address is that address.
                    BoundExpression operand = ((BoundPointerIndirectionOperator)expression).Operand;
                    Debug.Assert(operand.Type.IsPointerType());
                    EmitExpression(operand, used: true);
                    break;

                case BoundKind.PseudoVariable:
                    EmitPseudoVariableAddress((BoundPseudoVariable)expression);
                    break;

                case BoundKind.Call:
                    var call = (BoundCall)expression;
                    if (call.Method.RefKind == RefKind.None)
                    {
                        goto default;
                    }

                    EmitCallExpression(call, UseKind.UsedAsAddress);
                    break;

                case BoundKind.AssignmentOperator:
                    var assignment = (BoundAssignmentOperator)expression;
                    if (assignment.RefKind == RefKind.None)
                    {
                        goto default;
                    }

                    EmitAssignmentExpression(assignment, UseKind.UsedAsAddress);
                    break;

                default:
                    Debug.Assert(!HasHome(expression));
                    return EmitAddressOfTempClone(expression);
            }

            return null;
        }

        private void EmitComplexConditionalReceiverAddress(BoundComplexConditionalReceiver expression)
        {
            Debug.Assert(!expression.Type.IsReferenceType);
            Debug.Assert(!expression.Type.IsValueType);

            var receiverType = expression.Type;

            var whenValueTypeLabel = new Object();
            var doneLabel = new Object();

            EmitInitObj(receiverType, true, expression.Syntax);
            EmitBox(receiverType, expression.Syntax);
            _builder.EmitBranch(ILOpCode.Brtrue, whenValueTypeLabel);

            var receiverTemp = EmitAddress(expression.ReferenceTypeReceiver, addressKind: AddressKind.ReadOnly);
            Debug.Assert(receiverTemp == null);
            _builder.EmitBranch(ILOpCode.Br, doneLabel);
            _builder.AdjustStack(-1);

            _builder.MarkLabel(whenValueTypeLabel);
            EmitReceiverRef(expression.ValueTypeReceiver, isAccessConstrained: true);

            _builder.MarkLabel(doneLabel);
        }

        private void EmitLocalAddress(BoundLocal localAccess)
        {
            var local = localAccess.LocalSymbol;

            if (IsStackLocal(local))
            {
                if (local.RefKind != RefKind.None)
                {
                    // do nothing, ref should be on the stack
                }
                else
                {
                    // cannot get address of a stack value. 
                    // Something is wrong with optimizer
                    throw ExceptionUtilities.UnexpectedValue(local.RefKind);
                }
            }
            else
            {
                _builder.EmitLocalAddress(GetLocal(localAccess));
            }
        }

        private void EmitPseudoVariableAddress(BoundPseudoVariable expression)
        {
            EmitExpression(expression.EmitExpressions.GetAddress(expression), used: true);
        }

        private void EmitRefValueAddress(BoundRefValueOperator refValue)
        {
            // push typed reference
            // refanyval type -- pops typed reference, pushes address of variable
            EmitExpression(refValue.Operand, true);
            _builder.EmitOpCode(ILOpCode.Refanyval);
            EmitSymbolToken(refValue.Type, refValue.Syntax);
        }

        /// <summary>
        /// Emits address of a temp.
        /// Used in cases where taking address directly is not possible 
        /// (typically because expression does not have a home)
        /// 
        /// Introduce a temp which it will return.
        /// </summary>
        private LocalDefinition EmitAddressOfTempClone(BoundExpression expression)
        {
            EmitExpression(expression, true);
            var value = this.AllocateTemp(expression.Type, expression.Syntax);
            _builder.EmitLocalStore(value);
            _builder.EmitLocalAddress(value);

            return value;
        }

        /// <summary>
        /// May introduce a temp which it will return. (otherwise returns null)
        /// </summary>
        private LocalDefinition EmitSequenceAddress(BoundSequence sequence, AddressKind addressKind)
        {
            var hasLocals = !sequence.Locals.IsEmpty;

            if (hasLocals)
            {
                _builder.OpenLocalScope();

                foreach (var local in sequence.Locals)
                {
                    DefineLocal(local, sequence.Syntax);
                }
            }

            EmitSideEffects(sequence);
            var tempOpt = EmitAddress(sequence.Value, addressKind);

            // when a sequence is happened to be a byref receiver
            // we may need to extend the life time of the target until we are done accessing it
            // {.v ; v = Foo(); v}.Bar()     // v should be released only after Bar() is done.
            LocalSymbol doNotRelease = null;
            if (tempOpt == null)
            {
                doNotRelease = DigForValueLocal(sequence);
                if (doNotRelease != null)
                {
                    tempOpt = GetLocal(doNotRelease);
                }
            }

            FreeLocals(sequence, doNotRelease);
            return tempOpt;
        }

        // if sequence value is a local scoped to the sequence, return that local
        private LocalSymbol DigForValueLocal(BoundSequence topSequence)
        {
            return DigForValueLocal(topSequence, topSequence.Value);
        }

        private LocalSymbol DigForValueLocal(BoundSequence topSequence, BoundExpression value)
        {
            switch (value.Kind)
            {
                case BoundKind.Local:
                    var local = (BoundLocal)value;
                    var symbol = local.LocalSymbol;
                    if (topSequence.Locals.Contains(symbol))
                    {
                        return symbol;
                    }
                    break;

                case BoundKind.Sequence:
                    return DigForValueLocal(topSequence, ((BoundSequence)value).Value);

                case BoundKind.FieldAccess:
                    var fieldAccess = (BoundFieldAccess)value;
                    if (!fieldAccess.FieldSymbol.IsStatic)
                    {
                        var receiver = fieldAccess.ReceiverOpt;
                        if (!receiver.Type.IsReferenceType)
                        {
                            return DigForValueLocal(topSequence, receiver);
                        }
                    }
                    break;
            }

            return null;
        }


        /// <summary>
        /// Checks if expression directly or indirectly represents a value with its own home. In
        /// such cases it is possible to get a reference without loading into a temporary.
        /// </summary>
        private bool HasHome(BoundExpression expression)
        {
            switch (expression.Kind)
            {
                case BoundKind.Parameter:
                case BoundKind.ArrayAccess:
                case BoundKind.ThisReference:
                case BoundKind.BaseReference:
                case BoundKind.PointerIndirectionOperator:
                case BoundKind.RefValueOperator:
                    return true;

                case BoundKind.Local:
                    // locals have home unless they are byval stack locals
                    var local = ((BoundLocal)expression).LocalSymbol;
                    return !IsStackLocal(local) || local.RefKind != RefKind.None;

                case BoundKind.Call:
                    var method = ((BoundCall)expression).Method;
                    return method.RefKind != RefKind.None;

                case BoundKind.Dup:
                    return ((BoundDup)expression).RefKind != RefKind.None;

                case BoundKind.FieldAccess:
                    return HasHome((BoundFieldAccess)expression);

                case BoundKind.Sequence:
                    return HasHome(((BoundSequence)expression).Value);

                case BoundKind.AssignmentOperator:
                    return ((BoundAssignmentOperator)expression).RefKind != RefKind.None;

                case BoundKind.ComplexConditionalReceiver:
                    Debug.Assert(HasHome(((BoundComplexConditionalReceiver)expression).ValueTypeReceiver));
                    Debug.Assert(HasHome(((BoundComplexConditionalReceiver)expression).ReferenceTypeReceiver));
                    goto case BoundKind.ConditionalReceiver;

                case BoundKind.ConditionalReceiver:
                    return true;

                default:
                    return false;
            }
        }

        /// <summary>
        /// Special HasHome for fields. Fields have homes when they are writable.
        /// </summary>
        private bool HasHome(BoundFieldAccess fieldAccess)
        {
            // Some field accesses must be values; values do not have homes.
            if (fieldAccess.IsByValue)
            {
                return false;
            }

            FieldSymbol field = fieldAccess.FieldSymbol;

            // const fields are literal values with no homes
            if (field.IsConst)
            {
                return false;
            }

            if (!field.IsReadOnly)
            {
                return true;
            }

            // while readonly fields have home it is not valid to refer to it when not constructing.
            if (field.ContainingType != _method.ContainingType)
            {
                return false;
            }

            if (field.IsStatic)
            {
                return _method.MethodKind == MethodKind.StaticConstructor;
            }
            else
            {
                return _method.MethodKind == MethodKind.Constructor &&
                    fieldAccess.ReceiverOpt.Kind == BoundKind.ThisReference;
            }
        }

        private void EmitArrayIndices(ImmutableArray<BoundExpression> indices)
        {
            for (int i = 0; i < indices.Length; ++i)
            {
                BoundExpression index = indices[i];
                EmitExpression(index, used: true);
                TreatLongsAsNative(index.Type.PrimitiveTypeCode);
            }
        }

        private void EmitArrayElementAddress(BoundArrayAccess arrayAccess, AddressKind addressKind)
        {
            EmitExpression(arrayAccess.Expression, used: true);
            EmitArrayIndices(arrayAccess.Indices);

            if (addressKind == AddressKind.ReadOnly)
            {
                Debug.Assert(arrayAccess.Type.TypeKind == TypeKind.TypeParameter,
                    ".readonly is only needed when element type is a type param");

                _builder.EmitOpCode(ILOpCode.Readonly);
            }

            if (((ArrayTypeSymbol)arrayAccess.Expression.Type).IsSZArray)
            {
                _builder.EmitOpCode(ILOpCode.Ldelema);
                var elementType = arrayAccess.Type;
                EmitSymbolToken(elementType, arrayAccess.Syntax);
            }
            else
            {
                _builder.EmitArrayElementAddress(Emit.PEModuleBuilder.Translate((ArrayTypeSymbol)arrayAccess.Expression.Type),
                                                arrayAccess.Syntax, _diagnostics);
            }
        }

        /// <summary>
        /// May introduce a temp which it will return. (otherwise returns null)
        /// </summary>
        private LocalDefinition EmitFieldAddress(BoundFieldAccess fieldAccess)
        {
            FieldSymbol field = fieldAccess.FieldSymbol;

            if (!HasHome(fieldAccess))
            {
                // accessing a field that is not writable (const or readonly)
                return EmitAddressOfTempClone(fieldAccess);
            }
            else if (fieldAccess.FieldSymbol.IsStatic)
            {
                EmitStaticFieldAddress(field, fieldAccess.Syntax);
                return null;
            }
            else
            {
                return EmitInstanceFieldAddress(fieldAccess);
            }
        }

        private void EmitStaticFieldAddress(FieldSymbol field, CSharpSyntaxNode syntaxNode)
        {
            _builder.EmitOpCode(ILOpCode.Ldsflda);
            EmitSymbolToken(field, syntaxNode);
        }

        private void EmitParameterAddress(BoundParameter parameter)
        {
            int slot = ParameterSlot(parameter);
            if (parameter.ParameterSymbol.RefKind == RefKind.None)
            {
                _builder.EmitLoadArgumentAddrOpcode(slot);
            }
            else
            {
                _builder.EmitLoadArgumentOpcode(slot);
            }
        }

        /// <summary>
        /// Emits receiver in a form that allows member accesses ( O or &amp; ). For verifiably
        /// reference types it is the actual reference. For generic types it is a address of the
        /// receiver with readonly intent. For the value types it is an address of the receiver.
        /// 
        /// isAccessConstrained indicates that receiver is a target of a constrained callvirt
        /// in such case it is unnecessary to box a receiver that is typed to a type parameter
        /// 
        /// May introduce a temp which it will return. (otherwise returns null)
        /// </summary>
        private LocalDefinition EmitReceiverRef(BoundExpression receiver, bool isAccessConstrained = false)
        {
            var receiverType = receiver.Type;
            if (receiverType.IsVerifierReference())
            {
                EmitExpression(receiver, used: true);
                return null;
            }

            if (receiverType.TypeKind == TypeKind.TypeParameter)
            {
                //[Note: Constraints on a generic parameter only restrict the types that 
                //the generic parameter may be instantiated with. Verification (see Partition III) 
                //requires that a field, property or method that a generic parameter is known 
                //to provide through meeting a constraint, cannot be directly accessed/called 
                //via the generic parameter unless it is first boxed (see Partition III) or 
                //the callvirt instruction is prefixed with the constrained. prefix instruction 
                //(see Partition III). end note]
                if (isAccessConstrained)
                {
                    return EmitAddress(receiver, AddressKind.ReadOnly);
                }
                else
                {
                    EmitExpression(receiver, used: true);
                    // conditional receivers are already boxed if needed when pushed
                    if (receiver.Kind != BoundKind.ConditionalReceiver)
                    {
                        EmitBox(receiver.Type, receiver.Syntax);
                    }
                    return null;
                }
            }

            Debug.Assert(receiverType.IsVerifierValue());
            return EmitAddress(receiver, AddressKind.Writeable);
        }

        /// <summary>
        /// May introduce a temp which it will return. (otherwise returns null)
        /// </summary>
        private LocalDefinition EmitInstanceFieldAddress(BoundFieldAccess fieldAccess)
        {
            var field = fieldAccess.FieldSymbol;

            var tempOpt = EmitReceiverRef(fieldAccess.ReceiverOpt);

            _builder.EmitOpCode(ILOpCode.Ldflda);
            EmitSymbolToken(field, fieldAccess.Syntax);

            // when loading an address of a fixed field, we actually 
            // want to load the address of its "FixedElementField" instead.
            // Both the buffer backing struct and its only field should be at the same location,
            // so we could in theory just use address of the struct, but in some contexts that causes 
            // PEVerify errors because the struct has unexpected type. (Ex: struct& when int& is expected)
            if (field.IsFixed)
            {
                var fixedImpl = field.FixedImplementationType(_module);
                var fixedElementField = fixedImpl.FixedElementField;

                // if we get a mildly corrupted FixedImplementationType which does
                // not happen to have fixedElementField
                // we just leave address of the whole struct.
                //
                // That seems an adequate fallback because:
                // 1) it should happen only in impossibly rare cases involving malformed types
                // 2) the address of the struct is same as that of the buffer, just type is wrong.
                //    and that only matters to the verifier and we are in unsafe context anyways.
                if ((object)fixedElementField != null)
                {
                    _builder.EmitOpCode(ILOpCode.Ldflda);
                    EmitSymbolToken(fixedElementField, fieldAccess.Syntax);
                }
            }

            return tempOpt;
        }
    }
}
