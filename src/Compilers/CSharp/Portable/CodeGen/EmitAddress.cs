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

            // reference itself will not be written to, but may be used for call, callvirt.
            // for all purposes it is the same as Writeable, except when fetching an address of an array element
            // where it results in a ".readonly" prefix to deal with array covariance.
            Constrained,

            // reference itself will not be written to, nor it will be used to modify fields.
            ReadOnly,

            // same as ReadOnly, but we are not supposed to get a reference to a clone
            // regardless of compat settings.
            ReadOnlyStrict,
        }

        private static bool IsReadOnly(AddressKind addressKind) => addressKind >= AddressKind.ReadOnly;

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
                    return EmitLocalAddress((BoundLocal)expression, addressKind);

                case BoundKind.Dup:
                    Debug.Assert(((BoundDup)expression).RefKind != RefKind.None, "taking address of a stack value?");
                    return EmitDupAddress((BoundDup)expression, addressKind);

                case BoundKind.ConditionalReceiver:
                    // do nothing receiver ref must be already pushed
                    Debug.Assert(!expression.Type.IsReferenceType);
                    Debug.Assert(!expression.Type.IsValueType || expression.Type.IsNullableType());
                    break;

                case BoundKind.ComplexConditionalReceiver:
                    EmitComplexConditionalReceiverAddress((BoundComplexConditionalReceiver)expression);
                    break;

                case BoundKind.Parameter:
                    return EmitParameterAddress((BoundParameter)expression, addressKind);

                case BoundKind.FieldAccess:
                    return EmitFieldAddress((BoundFieldAccess)expression, addressKind);

                case BoundKind.ArrayAccess:
                    if (!HasHome(expression, addressKind))
                    {
                        goto default;
                    }

                    EmitArrayElementAddress((BoundArrayAccess)expression, addressKind);
                    break;

                case BoundKind.ThisReference:
                    Debug.Assert(expression.Type.IsValueType || IsReadOnly(addressKind), "'this' is readonly in classes");

                    if (expression.Type.IsValueType)
                    {
                        _builder.EmitLoadArgumentOpcode(0);
                    }
                    else
                    {
                        _builder.EmitLoadArgumentAddrOpcode(0);
                    }

                    break;

                case BoundKind.PreviousSubmissionReference:
                    // script references are lowered to a this reference and a field access
                    throw ExceptionUtilities.UnexpectedValue(expression.Kind);

                case BoundKind.BaseReference:
                    Debug.Assert(false, "base is always a reference type, why one may need a reference to it?");
                    break;

                case BoundKind.PassByCopy:
                    return EmitPassByCopyAddress((BoundPassByCopy)expression, addressKind);

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
                        (IsReadOnly(addressKind) && methodRefKind == RefKind.RefReadOnly))
                    {
                        EmitCallExpression(call, UseKind.UsedAsAddress);
                        break;
                    }

                    goto default;

                case BoundKind.DefaultExpression:
                    var type = expression.Type;

                    var temp = this.AllocateTemp(type, expression.Syntax);
                    _builder.EmitLocalAddress(temp);                  //  ldloca temp
                    _builder.EmitOpCode(ILOpCode.Dup);                //  dup
                    _builder.EmitOpCode(ILOpCode.Initobj);            //  initobj  <type>
                    EmitSymbolToken(type, expression.Syntax);
                    return temp;

                case BoundKind.ConditionalOperator:
                    if (!HasHome(expression, addressKind))
                    {
                        goto default;
                    }

                    EmitConditionalOperatorAddress((BoundConditionalOperator)expression, addressKind);
                    break;

                case BoundKind.AssignmentOperator:
                    var assignment = (BoundAssignmentOperator)expression;
                    if (!assignment.IsRef)
                    {
                        goto default;
                    }

                    throw ExceptionUtilities.UnexpectedValue(assignment.IsRef);

                case BoundKind.ThrowExpression:
                    // emit value or address is the same here.
                    EmitExpression(expression, used: true);
                    return null;

                default:
                    Debug.Assert(!HasHome(expression, addressKind));
                    return EmitAddressOfTempClone(expression);
            }

            return null;
        }

        private LocalDefinition EmitPassByCopyAddress(BoundPassByCopy passByCopyExpr, AddressKind addressKind)
        {
            // Normally we can just defer PassByCopy to the `default`,
            // but in some cases the value inside is already a temp that is local to that node.
            // In such case we can skip extra store/reload
            if (passByCopyExpr.Expression is BoundSequence sequence)
            {
                if (DigForValueLocal(sequence, sequence.Value) != null)
                {
                    return EmitSequenceAddress(sequence, addressKind);
                }
            }

            return EmitAddressOfTempClone(passByCopyExpr);
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
            AddExpressionTemp(EmitAddress(expr.Alternative, addressKind));

            _builder.EmitBranch(ILOpCode.Br, doneLabel);

            // If we get to consequenceLabel, we should not have Alternative on stack, adjust for that.
            _builder.AdjustStack(-1);

            _builder.MarkLabel(consequenceLabel);
            AddExpressionTemp(EmitAddress(expr.Consequence, addressKind));

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

        /// <summary>
        /// May introduce a temp which it will return. (otherwise returns null)
        /// </summary>
        private LocalDefinition EmitLocalAddress(BoundLocal localAccess, AddressKind addressKind)
        {
            var local = localAccess.LocalSymbol;

            if (!HasHome(localAccess, addressKind))
            {
                return EmitAddressOfTempClone(localAccess);
            }

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

            return null;
        }

        /// <summary>
        /// May introduce a temp which it will return. (otherwise returns null)
        /// </summary>
        private LocalDefinition EmitDupAddress(BoundDup dup, AddressKind addressKind)
        {
            if (!HasHome(dup, addressKind))
            {
                return EmitAddressOfTempClone(dup);
            }

            _builder.EmitOpCode(ILOpCode.Dup);
            return null;
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
            DefineAndRecordLocals(sequence);
            EmitSideEffects(sequence);
            var result =  EmitAddress(sequence.Value, addressKind);
            CloseScopeAndKeepLocals(sequence);

            return result;
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
        private bool HasHome(BoundExpression expression, AddressKind addressKind)
        {
            switch (expression.Kind)
            {
                case BoundKind.ArrayAccess:
                    if (addressKind == AddressKind.ReadOnly && 
                        !expression.Type.IsValueType &&
                        EnablePEVerifyCompat())
                    {
                        // due to array covariance getting a reference may throw ArrayTypeMismatch when element is not a struct, 
                        // passing "readonly." prefix would prevent that, but it is unverifiable, so will make a copy in compat case
                        return false;
                    }

                    return true;

                case BoundKind.PointerIndirectionOperator:
                case BoundKind.RefValueOperator:
                    return true;

                case BoundKind.ThisReference:
                    var type = expression.Type;
                    if (type.IsReferenceType)
                    {
                        Debug.Assert(IsReadOnly(addressKind), "`this` is readonly in classes");
                        return true;
                    }

                    if (!IsReadOnly(addressKind) && type.IsReadOnly)
                    {
                        return _method.MethodKind == MethodKind.Constructor;
                    }

                    return true;

                case BoundKind.ThrowExpression:
                    // vacuously this is true, we can take address of throw without temps
                    return true;

                case BoundKind.Parameter:
                    return IsReadOnly(addressKind) || 
                        ((BoundParameter)expression).ParameterSymbol.RefKind != RefKind.In;

                case BoundKind.Local:
                    // locals have home unless they are byval stack locals or ref-readonly
                    // locals in a mutating call
                    var local = ((BoundLocal)expression).LocalSymbol;
                    return !((IsStackLocal(local) && local.RefKind == RefKind.None) || 
                        (!IsReadOnly(addressKind) && local.RefKind == RefKind.RefReadOnly));

                case BoundKind.Call:
                    var methodRefKind = ((BoundCall)expression).Method.RefKind;
                    return methodRefKind == RefKind.Ref ||
                           (IsReadOnly(addressKind) && methodRefKind == RefKind.RefReadOnly);

                case BoundKind.Dup:
                    //NB: Dup represents locals that do not need IL slot
                    var dupRefKind = ((BoundDup)expression).RefKind;
                    return dupRefKind == RefKind.Ref ||
                        (IsReadOnly(addressKind) && dupRefKind == RefKind.RefReadOnly);

                case BoundKind.FieldAccess:
                    return HasHome((BoundFieldAccess)expression, addressKind);

                case BoundKind.Sequence:
                    return HasHome(((BoundSequence)expression).Value, addressKind);

                case BoundKind.AssignmentOperator:
                    return ((BoundAssignmentOperator)expression).IsRef;

                case BoundKind.ComplexConditionalReceiver:
                    Debug.Assert(HasHome(((BoundComplexConditionalReceiver)expression).ValueTypeReceiver, addressKind));
                    Debug.Assert(HasHome(((BoundComplexConditionalReceiver)expression).ReferenceTypeReceiver, addressKind));
                    goto case BoundKind.ConditionalReceiver;

                case BoundKind.ConditionalReceiver:
                    //ConditionalReceiver is a noop from Emit point of view. - it represents something that has already been pushed. 
                    //We should never need a temp for it. 
                    return true;

                case BoundKind.ConditionalOperator:
                    var ternary = (BoundConditionalOperator)expression;
                    
                    // only ref ternary may be referenced as a variable
                    if (!ternary.IsRef)
                    {
                        return false;
                    }
 
                    // branch that has no home will need a temporary
                    // if both have no home, just say whole expression has no home 
                    // so we could just use one temp for the whole thing
                    return HasHome(ternary.Consequence, addressKind) && HasHome(ternary.Alternative, addressKind);

                default:
                    return false;
            }
        }

        /// <summary>
        /// Special HasHome for fields. 
        /// Fields have readable homes when they are not constants.
        /// Fields have writeable homes unless they are readonly and used outside of the constructor.
        /// </summary>
        private bool HasHome(BoundFieldAccess fieldAccess, AddressKind addressKind)
        {
            FieldSymbol field = fieldAccess.FieldSymbol;

            // const fields are literal values with no homes. (ex: decimal.Zero)
            if (field.IsConst)
            {
                return false;
            }

            // in readonly situations where ref to a copy is not allowed, consider fields as addressable
            if (addressKind == AddressKind.ReadOnlyStrict)
            {
                return true;
            }

            // ReadOnly references can always be taken unless we are in peverify compat mode
            if (addressKind == AddressKind.ReadOnly && !EnablePEVerifyCompat())
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
                // in a case if we have a writeable struct field with a receiver that only has a readable home we would need to pass it via a temp.
                // it would be advantageous to make a temp for the field, not for the the outer struct, since the field is smaller and we can get to is by fetching references.
                // NOTE: this would not be profitable if we have to satisfy verifier, since for verifiability 
                //       we would not be able to dig for the inner field using references and the outer struct will have to be copied to a temp anyways.
                if (!EnablePEVerifyCompat())
                {
                    Debug.Assert(!IsReadOnly(addressKind));

                    var receiver = fieldAccess.ReceiverOpt;
                    if (receiver?.Type.IsValueType == true)
                    {
                        // Check receiver:
                        // has writeable home -> return true - the whole chain has writeable home (also a more common case)
                        // has readable home -> return false - we need to copy the field
                        // otherwise         -> return true  - the copy will be made at higher level so the leaf field can have writeable home

                        return HasHome(receiver, addressKind) ||  
                               !HasHome(receiver, AddressKind.ReadOnly);
                    }
                }

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

            if (ShouldEmitReadOnlyPrefix(arrayAccess, addressKind))
            {
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

        private bool ShouldEmitReadOnlyPrefix(BoundArrayAccess arrayAccess, AddressKind addressKind)
        {
            if (addressKind == AddressKind.Constrained)
            {
                Debug.Assert(arrayAccess.Type.TypeKind == TypeKind.TypeParameter, "constrained call should only be used with type parameter types");
                return true;
            }

            if (!IsReadOnly(addressKind))
            {
                return false;
            }

            // no benefits to value types
            return !arrayAccess.Type.IsValueType;
        }

        /// <summary>
        /// May introduce a temp which it will return. (otherwise returns null)
        /// </summary>
        private LocalDefinition EmitFieldAddress(BoundFieldAccess fieldAccess, AddressKind addressKind)
        {
            FieldSymbol field = fieldAccess.FieldSymbol;

            if (!HasHome(fieldAccess, addressKind))
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
                return EmitInstanceFieldAddress(fieldAccess, addressKind);
            }
        }

        private void EmitStaticFieldAddress(FieldSymbol field, SyntaxNode syntaxNode)
        {
            _builder.EmitOpCode(ILOpCode.Ldsflda);
            EmitSymbolToken(field, syntaxNode);
        }

        private LocalDefinition EmitParameterAddress(BoundParameter parameter, AddressKind addressKind)
        {
            ParameterSymbol parameterSymbol = parameter.ParameterSymbol;

            if (!HasHome(parameter, addressKind))
            {
                // accessing a parameter that is not writable
                return EmitAddressOfTempClone(parameter);
            }

            int slot = ParameterSlot(parameter);
            if (parameterSymbol.RefKind == RefKind.None)
            {
                _builder.EmitLoadArgumentAddrOpcode(slot);
            }
            else
            {
                _builder.EmitLoadArgumentOpcode(slot);
            }

            return null;
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
        private LocalDefinition EmitInstanceFieldAddress(BoundFieldAccess fieldAccess, AddressKind addressKind)
        {
            var field = fieldAccess.FieldSymbol;

            //NOTE: we are not propagating AddressKind.Constrained here.
            //      the reason is that while Constrained permits calls, it does not permit 
            //      taking field addresses, so we have to turn Constrained into writeable.
            var tempOpt = EmitReceiverRef(fieldAccess.ReceiverOpt, addressKind == AddressKind.Constrained ? AddressKind.Writeable : addressKind);

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
