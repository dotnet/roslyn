﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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

            // reference itself will not be written to, but may be used for call, callvirt.
            // for all purposes it is the same as Writeable, except when fetching an address of an array element
            // where it results in a ".readonly" prefix to deal with array covariance.
            Constrained,

            // reference itself will not be written to, nor it will be used to modify fields.
            ReadOnly,
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
                    return EmitFieldAddress((BoundFieldAccess)expression, addressKind);

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
                    var methodRefKind = call.Method.RefKind;

                    if (methodRefKind == RefKind.Ref || 
                        (addressKind == AddressKind.ReadOnly && methodRefKind == RefKind.RefReadOnly))
                    {
                        EmitCallExpression(call, UseKind.UsedAsAddress);
                        break;
                    }

                    goto default;

                //PROTOTYPE(readonly-ref): the following is more efficient codegen
                //                         not enabling in this change since results in too much of test churn and is not required for the current change
                //                         should be enabled together with other similar "PROTOTYPE" in codegen.
                //case BoundKind.DefaultOperator:
                //    var type = expression.Type;

                //    var temp = this.AllocateTemp(type, expression.Syntax);
                //    _builder.EmitLocalAddress(temp);                  //  ldloca temp
                //    _builder.EmitOpCode(ILOpCode.Dup);                //  dup
                //    _builder.EmitOpCode(ILOpCode.Initobj);            //  initobj  <type>
                //    EmitSymbolToken(type, expression.Syntax);
                //    return temp;

                case BoundKind.ConditionalOperator:
                    var conditional = (BoundConditionalOperator)expression;
                    if (!conditional.IsByRef)
                    {
                        goto default;
                    }

                    EmitConditionalOperatorAddress(conditional, addressKind);
                    break;

                case BoundKind.AssignmentOperator:
                    var assignment = (BoundAssignmentOperator)expression;
                    if (assignment.RefKind == RefKind.None)
                    {
                        goto default;
                    }

                    throw ExceptionUtilities.UnexpectedValue(assignment.RefKind);

                default:
                    Debug.Assert(!HasHome(expression, addressKind != AddressKind.ReadOnly));
                    return EmitAddressOfTempClone(expression);
            }

            return null;
        }

        /// <summary>
        /// Emit code for a conditional (aka ternary) operator.
        /// </summary>
        /// <remarks>
        /// (b ? x : y) becomes
        ///     push b
        ///     if pop then goto CONSEQUENCE
        ///     push y
        ///     goto DONE
        ///   CONSEQUENCE:
        ///     push x
        ///   DONE:
        /// </remarks>
        private void EmitConditionalOperatorAddress(BoundConditionalOperator expr, AddressKind addressKind)
        {
            Debug.Assert(expr.ConstantValue == null, "Constant value should have been emitted directly");

            object consequenceLabel = new object();
            object doneLabel = new object();

            EmitCondBranch(expr.Condition, ref consequenceLabel, sense: true);
            var temp = EmitAddress(expr.Alternative, addressKind);
            Debug.Assert(temp == null);

            _builder.EmitBranch(ILOpCode.Br, doneLabel);

            // If we get to consequenceLabel, we should not have Alternative on stack, adjust for that.
            _builder.AdjustStack(-1);

            _builder.MarkLabel(consequenceLabel);
            EmitAddress(expr.Consequence, addressKind);

            _builder.MarkLabel(doneLabel);
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

            var receiverTemp = EmitAddress(expression.ReferenceTypeReceiver, AddressKind.ReadOnly);
            Debug.Assert(receiverTemp == null);
            _builder.EmitBranch(ILOpCode.Br, doneLabel);
            _builder.AdjustStack(-1);

            _builder.MarkLabel(whenValueTypeLabel);
            // we will not write through this receiver, but it could be a target of mutating calls
            EmitReceiverRef(expression.ValueTypeReceiver, AddressKind.Constrained);

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
        private bool HasHome(BoundExpression expression, bool needWriteable)
        {
            switch (expression.Kind)
            {
                case BoundKind.ArrayAccess:
                case BoundKind.ThisReference:
                case BoundKind.BaseReference:
                case BoundKind.PointerIndirectionOperator:
                case BoundKind.RefValueOperator:
                    return true;

                case BoundKind.Parameter:
                    return !needWriteable || 
                        ((BoundParameter)expression).ParameterSymbol.RefKind != RefKind.RefReadOnly;

                case BoundKind.Local:
                    // locals have home unless they are byval stack locals
                    var local = ((BoundLocal)expression).LocalSymbol;
                    return !IsStackLocal(local) || local.RefKind != RefKind.None;

                case BoundKind.Call:
                    var methodRefKind = ((BoundCall)expression).Method.RefKind;
                    return methodRefKind == RefKind.Ref ||
                           (!needWriteable && methodRefKind == RefKind.RefReadOnly);

                case BoundKind.Dup:
                    //PROTOTYPE(readonlyRefs): makes sure readonly variables are not duped and written to
                    return ((BoundDup)expression).RefKind != RefKind.None;

                case BoundKind.FieldAccess:
                    return HasHome((BoundFieldAccess)expression, needWriteable);

                case BoundKind.Sequence:
                    return HasHome(((BoundSequence)expression).Value, needWriteable);

                case BoundKind.AssignmentOperator:
                    return ((BoundAssignmentOperator)expression).RefKind != RefKind.None;

                case BoundKind.ComplexConditionalReceiver:
                    Debug.Assert(HasHome(((BoundComplexConditionalReceiver)expression).ValueTypeReceiver, needWriteable));
                    Debug.Assert(HasHome(((BoundComplexConditionalReceiver)expression).ReferenceTypeReceiver, needWriteable));
                    goto case BoundKind.ConditionalReceiver;

                case BoundKind.ConditionalReceiver:
                    //PROTOTYPE(readonlyRefs): are these always writeable? Test coverage?
                    return true;

                case BoundKind.ConditionalOperator:
                    //PROTOTYPE(readonlyRefs): Test coverage for in-place assignment/init.
                    return ((BoundConditionalOperator)expression).IsByRef;

                default:
                    return false;
            }
        }

        /// <summary>
        /// Special HasHome for fields. 
        /// Fields have readable homes when they are not constants.
        /// Fields have writeable homes unless they are readonly and used outside of the constructor.
        /// </summary>
        private bool HasHome(BoundFieldAccess fieldAccess, bool needWriteable)
        {
            FieldSymbol field = fieldAccess.FieldSymbol;

            // const fields are literal values with no homes. (ex: decimal.Zero)
            if (field.IsConst)
            {
                return false;
            }

            if (!needWriteable)
            {
                return true;
            }

            // Some field accesses must be values; values do not have homes.
            if (fieldAccess.IsByValue)
            {
                return false;
            }

            if (!field.IsReadOnly)
            {
                //PROTOTYPE(readonlyRefs): should we dig through struct receivers? 
                //   roField.a.b.c.d.Method() // roField is readonly, all structs
                //   is it cheaper to copy "d" than "roField", but getting rw ref of roField could upset verifier
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

            if (addressKind == AddressKind.Constrained)
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
        private LocalDefinition EmitFieldAddress(BoundFieldAccess fieldAccess, AddressKind addressKind)
        {
            FieldSymbol field = fieldAccess.FieldSymbol;

            if (!HasHome(fieldAccess, addressKind != AddressKind.ReadOnly))
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
                //NOTE: we are not propagating AddressKind here.
                //      the reason is that while Constrained permits calls, it does not permit 
                //      taking field addresses, so we have to turn Constrained into writeable.
                //      It is less error prone to just pass a bool "isReadonly" 
                return EmitInstanceFieldAddress(fieldAccess, isReadonly: addressKind == AddressKind.ReadOnly);
            }
        }

        private void EmitStaticFieldAddress(FieldSymbol field, SyntaxNode syntaxNode)
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
        /// Emits receiver in a form that allows member accesses ( O or &amp; ). 
        /// For verifier-reference types it is the actual reference. 
        /// For the value types it is an address of the receiver.
        /// For generic types it is either a boxed receiver or the address of the receiver with readonly intent. 
        /// 
        /// addressKind - kind of address that is needed in case if receiver is not a reference type.
        /// 
        /// May introduce a temp which it will return. (otherwise returns null)
        /// </summary>
        private LocalDefinition EmitReceiverRef(BoundExpression receiver, AddressKind addressKind)
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
                if (addressKind == AddressKind.Constrained)
                {
                    return EmitAddress(receiver, addressKind);
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
            return EmitAddress(receiver, addressKind);
        }

        /// <summary>
        /// May introduce a temp which it will return. (otherwise returns null)
        /// </summary>
        private LocalDefinition EmitInstanceFieldAddress(BoundFieldAccess fieldAccess, bool isReadonly)
        {
            var field = fieldAccess.FieldSymbol;

            var tempOpt = EmitReceiverRef(fieldAccess.ReceiverOpt, isReadonly? AddressKind.ReadOnly: AddressKind.Writeable);

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
