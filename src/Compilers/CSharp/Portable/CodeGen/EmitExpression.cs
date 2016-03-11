// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Reflection.Metadata;
using Microsoft.CodeAnalysis.CodeGen;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.CodeGen
{
    internal partial class CodeGenerator
    {
        private int _recursionDepth;

        private class EmitCancelledException : Exception
        { }

        private enum UseKind
        {
            Unused,
            UsedAsValue,
            UsedAsAddress
        }

        private void EmitExpression(BoundExpression expression, bool used)
        {
            if (expression == null)
            {
                return;
            }

            var constantValue = expression.ConstantValue;
            if (constantValue != null)
            {
                if (!used)
                {
                    // unused constants have no side-effects.
                    return;
                }

                if ((object)expression.Type == null || expression.Type.SpecialType != SpecialType.System_Decimal)
                {
                    EmitConstantExpression(expression.Type, constantValue, used, expression.Syntax);
                    return;
                }
            }

            _recursionDepth++;

            if (_recursionDepth > 1)
            {
                StackGuard.EnsureSufficientExecutionStack(_recursionDepth);

                EmitExpressionCore(expression, used);
            }
            else
            {
                EmitExpressionCoreWithStackGuard(expression, used);
            }

            _recursionDepth--;
        }

        private void EmitExpressionCoreWithStackGuard(BoundExpression expression, bool used)
        {
            Debug.Assert(_recursionDepth == 1);

            try
            {
                EmitExpressionCore(expression, used);
                Debug.Assert(_recursionDepth == 1);
            }
            catch (Exception ex) when (StackGuard.IsInsufficientExecutionStackException(ex))
            {
                _diagnostics.Add(ErrorCode.ERR_InsufficientStack,
                                 BoundTreeVisitor.CancelledByStackGuardException.GetTooLongOrComplexExpressionErrorLocation(expression));
                throw new EmitCancelledException();
            }
        }

        private void EmitExpressionCore(BoundExpression expression, bool used)
        {
            switch (expression.Kind)
            {
                case BoundKind.AssignmentOperator:
                    EmitAssignmentExpression((BoundAssignmentOperator)expression, used ? UseKind.UsedAsValue : UseKind.Unused);
                    break;

                case BoundKind.Call:
                    EmitCallExpression((BoundCall)expression, used ? UseKind.UsedAsValue : UseKind.Unused);
                    break;

                case BoundKind.ObjectCreationExpression:
                    EmitObjectCreationExpression((BoundObjectCreationExpression)expression, used);
                    break;

                case BoundKind.DelegateCreationExpression:
                    EmitDelegateCreationExpression((BoundDelegateCreationExpression)expression, used);
                    break;

                case BoundKind.ArrayCreation:
                    EmitArrayCreationExpression((BoundArrayCreation)expression, used);
                    break;

                case BoundKind.StackAllocArrayCreation:
                    EmitStackAllocArrayCreationExpression((BoundStackAllocArrayCreation)expression, used);
                    break;

                case BoundKind.Conversion:
                    EmitConversionExpression((BoundConversion)expression, used);
                    break;

                case BoundKind.Local:
                    EmitLocalLoad((BoundLocal)expression, used);
                    break;

                case BoundKind.Dup:
                    EmitDupExpression((BoundDup)expression, used);
                    break;

                case BoundKind.Parameter:
                    if (used)  // unused parameter has no side-effects
                    {
                        EmitParameterLoad((BoundParameter)expression);
                    }
                    break;

                case BoundKind.FieldAccess:
                    EmitFieldLoad((BoundFieldAccess)expression, used);
                    break;

                case BoundKind.ArrayAccess:
                    EmitArrayElementLoad((BoundArrayAccess)expression, used);
                    break;

                case BoundKind.ArrayLength:
                    EmitArrayLength((BoundArrayLength)expression, used);
                    break;

                case BoundKind.ThisReference:
                    if (used) // unused this has no side-effects
                    {
                        EmitThisReferenceExpression((BoundThisReference)expression);
                    }
                    break;

                case BoundKind.PreviousSubmissionReference:
                    // Script references are lowered to a this reference and a field access.
                    throw ExceptionUtilities.UnexpectedValue(expression.Kind);

                case BoundKind.BaseReference:
                    if (used) // unused base has no side-effects
                    {
                        var thisType = _method.ContainingType;
                        _builder.EmitOpCode(ILOpCode.Ldarg_0);
                        if (thisType.IsValueType)
                        {
                            EmitLoadIndirect(thisType, expression.Syntax);
                            EmitBox(thisType, expression.Syntax);
                        }
                    }
                    break;

                case BoundKind.Sequence:
                    EmitSequenceExpression((BoundSequence)expression, used);
                    break;

                case BoundKind.SequencePointExpression:
                    EmitSequencePointExpression((BoundSequencePointExpression)expression, used);
                    break;

                case BoundKind.UnaryOperator:
                    EmitUnaryOperatorExpression((BoundUnaryOperator)expression, used);
                    break;

                case BoundKind.BinaryOperator:
                    EmitBinaryOperatorExpression((BoundBinaryOperator)expression, used);
                    break;

                case BoundKind.NullCoalescingOperator:
                    EmitNullCoalescingOperator((BoundNullCoalescingOperator)expression, used);
                    break;

                case BoundKind.IsOperator:
                    EmitIsExpression((BoundIsOperator)expression, used);
                    break;

                case BoundKind.AsOperator:
                    EmitAsExpression((BoundAsOperator)expression, used);
                    break;

                case BoundKind.DefaultOperator:
                    EmitDefaultExpression((BoundDefaultOperator)expression, used);
                    break;

                case BoundKind.TypeOfOperator:
                    if (used) // unused typeof has no side-effects
                    {
                        EmitTypeOfExpression((BoundTypeOfOperator)expression);
                    }
                    break;

                case BoundKind.SizeOfOperator:
                    if (used) // unused sizeof has no side-effects
                    {
                        EmitSizeOfExpression((BoundSizeOfOperator)expression);
                    }
                    break;

                case BoundKind.MethodToken:
                    if (used)
                    {
                        EmitMethodTokenExpression((BoundMethodToken)expression);
                    }
                    break;

                case BoundKind.MethodInfo:
                    if (used)
                    {
                        EmitMethodInfoExpression((BoundMethodInfo)expression);
                    }
                    break;

                case BoundKind.FieldInfo:
                    if (used)
                    {
                        EmitFieldInfoExpression((BoundFieldInfo)expression);
                    }
                    break;

                case BoundKind.ConditionalOperator:
                    EmitConditionalOperator((BoundConditionalOperator)expression, used);
                    break;

                case BoundKind.AddressOfOperator:
                    EmitAddressOfExpression((BoundAddressOfOperator)expression, used);
                    break;

                case BoundKind.PointerIndirectionOperator:
                    EmitPointerIndirectionOperator((BoundPointerIndirectionOperator)expression, used);
                    break;

                case BoundKind.ArgList:
                    EmitArgList(used);
                    break;

                case BoundKind.ArgListOperator:
                    Debug.Assert(used);
                    EmitArgListOperator((BoundArgListOperator)expression);
                    break;

                case BoundKind.RefTypeOperator:
                    EmitRefTypeOperator((BoundRefTypeOperator)expression, used);
                    break;

                case BoundKind.MakeRefOperator:
                    EmitMakeRefOperator((BoundMakeRefOperator)expression, used);
                    break;

                case BoundKind.RefValueOperator:
                    EmitRefValueOperator((BoundRefValueOperator)expression, used);
                    break;

                case BoundKind.LoweredConditionalAccess:
                    EmitLoweredConditionalAccessExpression((BoundLoweredConditionalAccess)expression, used);
                    break;

                case BoundKind.ConditionalReceiver:
                    EmitConditionalReceiver((BoundConditionalReceiver)expression, used);
                    break;

                case BoundKind.ComplexConditionalReceiver:
                    EmitComplexConditionalReceiver((BoundComplexConditionalReceiver)expression, used);
                    break;

                case BoundKind.PseudoVariable:
                    EmitPseudoVariableValue((BoundPseudoVariable)expression, used);
                    break;

                case BoundKind.ThrowExpression:
                    EmitThrowExpression((BoundThrowExpression)expression, used);
                    break;

                default:
                    // Code gen should not be invoked if there are errors.
                    Debug.Assert(expression.Kind != BoundKind.BadExpression);

                    // node should have been lowered:
                    throw ExceptionUtilities.UnexpectedValue(expression.Kind);
            }
        }

        private void EmitThrowExpression(BoundThrowExpression node, bool used)
        {
            this.EmitExpression(node.Expression, true);
            var thrownType = node.Expression.Type;
            if (thrownType?.TypeKind == TypeKind.TypeParameter)
            {
                this.EmitBox(thrownType, node.Expression.Syntax);
            }

            _builder.EmitThrow(isRethrow: false);

            // to satisfy invariants, we push a default value to pretend to adjust the stack height
            EmitDefaultValue(node.Type, used, node.Syntax);
        }

        private void EmitComplexConditionalReceiver(BoundComplexConditionalReceiver expression, bool used)
        {
            Debug.Assert(!expression.Type.IsReferenceType);
            Debug.Assert(!expression.Type.IsValueType);

            var receiverType = expression.Type;

            var whenValueTypeLabel = new object();
            var doneLabel = new object();

            EmitInitObj(receiverType, true, expression.Syntax);
            EmitBox(receiverType, expression.Syntax);
            _builder.EmitBranch(ILOpCode.Brtrue, whenValueTypeLabel);

            EmitExpression(expression.ReferenceTypeReceiver, used);
            _builder.EmitBranch(ILOpCode.Br, doneLabel);
            _builder.AdjustStack(-1);

            _builder.MarkLabel(whenValueTypeLabel);
            EmitExpression(expression.ValueTypeReceiver, used);

            _builder.MarkLabel(doneLabel);
        }

        private void EmitLoweredConditionalAccessExpression(BoundLoweredConditionalAccess expression, bool used)
        {
            var receiver = expression.Receiver;

            if (receiver.IsDefaultValue())
            {
                EmitDefaultValue(expression.Type, used, expression.Syntax);
                return;
            }

            var receiverType = receiver.Type;
            LocalDefinition receiverTemp = null;
            Debug.Assert(!receiverType.IsValueType ||
                (receiverType.IsNullableType() && expression.HasValueMethodOpt != null), "conditional receiver cannot be a struct");

            var receiverConstant = receiver.ConstantValue;
            if (receiverConstant != null)
            {
                // const but not default
                receiverTemp = EmitReceiverRef(receiver, isAccessConstrained: !receiverType.IsReferenceType);
                EmitExpression(expression.WhenNotNull, used);
                if (receiverTemp != null)
                {
                    FreeTemp(receiverTemp);
                }
                return;
            }

            // labels
            object whenNotNullLabel = new object();
            object doneLabel = new object();
            LocalDefinition cloneTemp = null;

            // we need a copy if we deal with nonlocal value (to capture the value)
            // or if we have a ref-constrained T (to do box just once) 
            // or if we deal with stack local (reads are destructive)
            var nullCheckOnCopy = LocalRewriter.CanChangeValueBetweenReads(receiver, localsMayBeAssignedOrCaptured: false) ||
                                   (receiverType.IsReferenceType && receiverType.TypeKind == TypeKind.TypeParameter) ||
                                   (receiver.Kind == BoundKind.Local && IsStackLocal(((BoundLocal)receiver).LocalSymbol));

            var unconstrainedReceiver = !receiverType.IsReferenceType && !receiverType.IsValueType;


            // ===== RECEIVER
            if (nullCheckOnCopy)
            {
                receiverTemp = EmitReceiverRef(receiver, isAccessConstrained: unconstrainedReceiver);
                if (unconstrainedReceiver)
                {
                    // unconstrained case needs to handle case where T is actually a struct.
                    // such values are never nulls
                    // we will emit a check for such case, but the check is really a JIT-time 
                    // constant since JIT will know if T is a struct or not.

                    // if ((object)default(T) != null) 
                    // {
                    //     goto whenNotNull
                    // }
                    // else
                    // {
                    //     temp = receiverRef
                    //     receiverRef = ref temp
                    // }
                    EmitDefaultValue(receiverType, true, receiver.Syntax);
                    EmitBox(receiverType, receiver.Syntax);
                    _builder.EmitBranch(ILOpCode.Brtrue, whenNotNullLabel);
                    EmitLoadIndirect(receiverType, receiver.Syntax);

                    cloneTemp = AllocateTemp(receiverType, receiver.Syntax);
                    _builder.EmitLocalStore(cloneTemp);
                    _builder.EmitLocalAddress(cloneTemp);
                    _builder.EmitLocalLoad(cloneTemp);
                    EmitBox(receiver.Type, receiver.Syntax);

                    // here we have loaded a ref to a temp and its boxed value { &T, O }
                }
                else
                {
                    _builder.EmitOpCode(ILOpCode.Dup);
                    // here we have loaded two copies of a reference   { O, O }  or  {&nub, &nub}
                }
            }
            else
            {
                receiverTemp = EmitReceiverRef(receiver, isAccessConstrained: false);
                // here we have loaded just { O } or  {&nub}
                // we have the most trivial case where we can just reload receiver when needed again
            }

            // ===== CONDITION

            var hasValueOpt = expression.HasValueMethodOpt;
            if (hasValueOpt != null)
            {
                Debug.Assert(receiver.Type.IsNullableType());
                _builder.EmitOpCode(ILOpCode.Call, stackAdjustment: 0);
                EmitSymbolToken(hasValueOpt, expression.Syntax, null);
            }

            _builder.EmitBranch(ILOpCode.Brtrue, whenNotNullLabel);

            // no longer need the temp if we are not holding a copy
            if (receiverTemp != null && !nullCheckOnCopy)
            {
                FreeTemp(receiverTemp);
                receiverTemp = null;
            }

            // ===== WHEN NULL
            if (nullCheckOnCopy)
            {
                _builder.EmitOpCode(ILOpCode.Pop);
            }

            var whenNull = expression.WhenNullOpt;
            if (whenNull == null)
            {
                EmitDefaultValue(expression.Type, used, expression.Syntax);
            }
            else
            {
                EmitExpression(whenNull, used);
            }

            _builder.EmitBranch(ILOpCode.Br, doneLabel);


            // ===== WHEN NOT NULL 
            if (nullCheckOnCopy)
            {
                // notNull branch pops copy of receiver off the stack when nullCheckOnCopy
                // however on the isNull branch we still have the stack as it was and need 
                // to adjust stack depth correspondingly.
                _builder.AdjustStack(+1);
            }

            if (used)
            {
                // notNull branch pushes default on the stack when used
                // however on the isNull branch we still have the stack as it was and need 
                // to adjust stack depth correspondingly.
                _builder.AdjustStack(-1);
            }

            _builder.MarkLabel(whenNotNullLabel);

            if (!nullCheckOnCopy)
            {
                Debug.Assert(receiverTemp == null);
                receiverTemp = EmitReceiverRef(receiver, isAccessConstrained: unconstrainedReceiver);
                Debug.Assert(receiverTemp == null);
            }

            EmitExpression(expression.WhenNotNull, used);

            // ===== DONE
            _builder.MarkLabel(doneLabel);

            if (cloneTemp != null)
            {
                FreeTemp(cloneTemp);
            }

            if (receiverTemp != null)
            {
                FreeTemp(receiverTemp);
            }
        }

        private void EmitConditionalReceiver(BoundConditionalReceiver expression, bool used)
        {
            Debug.Assert(!expression.Type.IsValueType);

            if (!expression.Type.IsReferenceType)
            {
                EmitLoadIndirect(expression.Type, expression.Syntax);
            }

            EmitPopIfUnused(used);
        }

        private void EmitRefValueOperator(BoundRefValueOperator expression, bool used)
        {
            EmitRefValueAddress(expression);
            EmitLoadIndirect(expression.Type, expression.Syntax);
            EmitPopIfUnused(used);
        }

        private void EmitMakeRefOperator(BoundMakeRefOperator expression, bool used)
        {
            // push address of variable
            // mkrefany [Type] -- takes address off stack, puts TypedReference on stack

            var temp = EmitAddress(expression.Operand, AddressKind.Writeable);
            Debug.Assert(temp == null, "makeref should not create temps");

            _builder.EmitOpCode(ILOpCode.Mkrefany);
            EmitSymbolToken(expression.Operand.Type, expression.Operand.Syntax);
            EmitPopIfUnused(used);
        }

        private void EmitRefTypeOperator(BoundRefTypeOperator expression, bool used)
        {
            // push TypedReference
            // refanytype -- takes TypedReference off stack, puts token on stack
            // call GetTypeFromHandle -- takes token off stack, puts Type on stack

            EmitExpression(expression.Operand, true);
            _builder.EmitOpCode(ILOpCode.Refanytype);
            _builder.EmitOpCode(ILOpCode.Call, stackAdjustment: 0);
            var getTypeMethod = expression.GetTypeFromHandle;
            Debug.Assert((object)getTypeMethod != null);
            EmitSymbolToken(getTypeMethod, expression.Syntax, null);
            EmitPopIfUnused(used);
        }

        private void EmitArgList(bool used)
        {
            _builder.EmitOpCode(ILOpCode.Arglist);
            EmitPopIfUnused(used);
        }

        private void EmitArgListOperator(BoundArgListOperator expression)
        {
            for (int i = 0; i < expression.Arguments.Length; i++)
            {
                BoundExpression argument = expression.Arguments[i];
                RefKind refKind = expression.ArgumentRefKindsOpt.IsDefaultOrEmpty ? RefKind.None : expression.ArgumentRefKindsOpt[i];
                EmitArgument(argument, refKind);
            }
        }

        private void EmitArgument(BoundExpression argument, RefKind refKind)
        {
            if (refKind == RefKind.None)
            {
                EmitExpression(argument, true);
            }
            else
            {
                var temp = EmitAddress(argument, AddressKind.Writeable);
                Debug.Assert(temp == null, "passing args byref should not clone them into temps");
            }
        }

        private void EmitAddressOfExpression(BoundAddressOfOperator expression, bool used)
        {
            var temp = EmitAddress(expression.Operand, AddressKind.Writeable);
            Debug.Assert(temp == null, "If the operand is addressable, then a temp shouldn't be required.");
            if (used && !expression.IsFixedStatementAddressOf)
            {
                // When computing an address to be used to initialize a fixed-statement variable, we have to be careful
                // not to convert the managed reference to an unmanaged pointer before storing it.  Otherwise the GC might
                // come along and move memory around, invalidating the pointer before it is pinned by being stored in
                // the fixed variable.  But elsewhere in the code we do use a conv.u instruction to convert the managed
                // reference to the underlying type for unmanaged pointers, which is the type "unsigned int" (see CLI
                // standard, Partition I section 12.1.1.1).
                _builder.EmitOpCode(ILOpCode.Conv_u);
            }

            EmitPopIfUnused(used);
        }

        private void EmitPointerIndirectionOperator(BoundPointerIndirectionOperator expression, bool used)
        {
            EmitExpression(expression.Operand, used: true);
            EmitLoadIndirect(expression.Type, expression.Syntax);
            EmitPopIfUnused(used);
        }

        private void EmitDupExpression(BoundDup expression, bool used)
        {
            if (expression.RefKind == RefKind.None)
            {
                // unused dup is noop
                if (used)
                {
                    _builder.EmitOpCode(ILOpCode.Dup);
                }
            }
            else
            {
                _builder.EmitOpCode(ILOpCode.Dup);

                // must read in case if it is a null ref
                EmitLoadIndirect(expression.Type, expression.Syntax);
                EmitPopIfUnused(used);
            }
        }

        private void EmitDelegateCreationExpression(BoundDelegateCreationExpression expression, bool used)
        {
            var mg = expression.Argument as BoundMethodGroup;
            var receiver = mg != null ? mg.ReceiverOpt : expression.Argument;
            var meth = expression.MethodOpt ?? receiver.Type.DelegateInvokeMethod();
            Debug.Assert((object)meth != null);
            EmitDelegateCreation(expression, receiver, expression.IsExtensionMethod, meth, expression.Type, used);
        }

        private void EmitThisReferenceExpression(BoundThisReference thisRef)
        {
            var thisType = thisRef.Type;
            Debug.Assert(thisType.TypeKind != TypeKind.TypeParameter);

            _builder.EmitOpCode(ILOpCode.Ldarg_0);
            if (thisType.IsValueType)
            {
                EmitLoadIndirect(thisType, thisRef.Syntax);
            }
        }

        private void EmitPseudoVariableValue(BoundPseudoVariable expression, bool used)
        {
            EmitExpression(expression.EmitExpressions.GetValue(expression, _diagnostics), used);
        }

        private void EmitSequencePointExpression(BoundSequencePointExpression node, bool used)
        {
            EmitSequencePoint(node);

            // used is true to ensure that something is emitted
            EmitExpression(node.Expression, used: true);
            EmitPopIfUnused(used);
        }

        private void EmitSequencePoint(BoundSequencePointExpression node)
        {
            var syntax = node.Syntax;
            if (_emitPdbSequencePoints)
            {
                if (syntax == null)
                {
                    EmitHiddenSequencePoint();
                }
                else
                {
                    EmitSequencePoint(syntax);
                }
            }
        }

        private void EmitSequenceExpression(BoundSequence sequence, bool used)
        {
            DefineLocals(sequence);
            EmitSideEffects(sequence);

            // CONSIDER:    LocalRewriter.RewriteNestedObjectOrCollectionInitializerExpression may create a bound sequence with an unused BoundTypeExpression as the value,
            // CONSIDER:    which must be ignored by codegen. See comments in RewriteNestedObjectOrCollectionInitializerExpression for details and an example.
            // CONSIDER:    We may want to instead consider making the Value field of BoundSequence node optional to allow a sequence with
            // CONSIDER:    only side effects and no value. Note that VB's BoundSequence node has an optional value field.
            // CONSIDER:    This will allow us to remove the below check before emitting the value.

            Debug.Assert(sequence.Value.Kind != BoundKind.TypeExpression || !used);
            if (sequence.Value.Kind != BoundKind.TypeExpression)
            {
                EmitExpression(sequence.Value, used);
            }

            // sequence is used as a value, can release all locals
            FreeLocals(sequence, doNotRelease: null);
        }

        private void DefineLocals(BoundSequence sequence)
        {
            if (sequence.Locals.IsEmpty)
            {
                return;
            }

            _builder.OpenLocalScope();

            foreach (var local in sequence.Locals)
            {
                DefineLocal(local, sequence.Syntax);
            }
        }

        private void FreeLocals(BoundSequence sequence, LocalSymbol doNotRelease)
        {
            if (sequence.Locals.IsEmpty)
            {
                return;
            }

            _builder.CloseLocalScope();

            foreach (var local in sequence.Locals)
            {
                if ((object)local != doNotRelease)
                {
                    FreeLocal(local);
                }
            }
        }

        private void EmitSideEffects(BoundSequence sequence)
        {
            var sideEffects = sequence.SideEffects;
            if (!sideEffects.IsDefaultOrEmpty)
            {
                foreach (var se in sideEffects)
                {
                    EmitExpression(se, false);
                }
            }
        }

        private void EmitArguments(ImmutableArray<BoundExpression> arguments, ImmutableArray<ParameterSymbol> parameters)
        {
            // We might have an extra argument for the __arglist() of a varargs method.
            Debug.Assert(arguments.Length == parameters.Length || arguments.Length == parameters.Length + 1, "argument count must match parameter count");
            for (int i = 0; i < arguments.Length; i++)
            {
                BoundExpression argument = arguments[i];
                RefKind refKind = (i == parameters.Length) ? RefKind.None : parameters[i].RefKind;
                EmitArgument(argument, refKind);
            }
        }

        private void EmitArrayElementLoad(BoundArrayAccess arrayAccess, bool used)
        {
            EmitExpression(arrayAccess.Expression, used: true);
            EmitArrayIndices(arrayAccess.Indices);

            if (((ArrayTypeSymbol)arrayAccess.Expression.Type).IsSZArray)
            {
                var elementType = arrayAccess.Type;
                if (elementType.IsEnumType())
                {
                    //underlying primitives do not need type tokens.
                    elementType = ((NamedTypeSymbol)elementType).EnumUnderlyingType;
                }

                switch (elementType.PrimitiveTypeCode)
                {
                    case Microsoft.Cci.PrimitiveTypeCode.Int8:
                        _builder.EmitOpCode(ILOpCode.Ldelem_i1);
                        break;

                    case Microsoft.Cci.PrimitiveTypeCode.Boolean:
                    case Microsoft.Cci.PrimitiveTypeCode.UInt8:
                        _builder.EmitOpCode(ILOpCode.Ldelem_u1);
                        break;

                    case Microsoft.Cci.PrimitiveTypeCode.Int16:
                        _builder.EmitOpCode(ILOpCode.Ldelem_i2);
                        break;

                    case Microsoft.Cci.PrimitiveTypeCode.Char:
                    case Microsoft.Cci.PrimitiveTypeCode.UInt16:
                        _builder.EmitOpCode(ILOpCode.Ldelem_u2);
                        break;

                    case Microsoft.Cci.PrimitiveTypeCode.Int32:
                        _builder.EmitOpCode(ILOpCode.Ldelem_i4);
                        break;

                    case Microsoft.Cci.PrimitiveTypeCode.UInt32:
                        _builder.EmitOpCode(ILOpCode.Ldelem_u4);
                        break;

                    case Microsoft.Cci.PrimitiveTypeCode.Int64:
                    case Microsoft.Cci.PrimitiveTypeCode.UInt64:
                        _builder.EmitOpCode(ILOpCode.Ldelem_i8);
                        break;

                    case Microsoft.Cci.PrimitiveTypeCode.IntPtr:
                    case Microsoft.Cci.PrimitiveTypeCode.UIntPtr:
                    case Microsoft.Cci.PrimitiveTypeCode.Pointer:
                        _builder.EmitOpCode(ILOpCode.Ldelem_i);
                        break;

                    case Microsoft.Cci.PrimitiveTypeCode.Float32:
                        _builder.EmitOpCode(ILOpCode.Ldelem_r4);
                        break;

                    case Microsoft.Cci.PrimitiveTypeCode.Float64:
                        _builder.EmitOpCode(ILOpCode.Ldelem_r8);
                        break;

                    default:
                        if (elementType.IsVerifierReference())
                        {
                            _builder.EmitOpCode(ILOpCode.Ldelem_ref);
                        }
                        else
                        {
                            if (used)
                            {
                                _builder.EmitOpCode(ILOpCode.Ldelem);
                            }
                            else
                            {
                                // no need to read whole element of nontrivial type/size here
                                // just take a reference to an element for array access side-effects 
                                if (elementType.TypeKind == TypeKind.TypeParameter)
                                {
                                    _builder.EmitOpCode(ILOpCode.Readonly);
                                }

                                _builder.EmitOpCode(ILOpCode.Ldelema);
                            }

                            EmitSymbolToken(elementType, arrayAccess.Syntax);
                        }
                        break;
                }
            }
            else
            {
                _builder.EmitArrayElementLoad(Emit.PEModuleBuilder.Translate((ArrayTypeSymbol)arrayAccess.Expression.Type), arrayAccess.Expression.Syntax, _diagnostics);
            }

            EmitPopIfUnused(used);
        }

        private void EmitFieldLoad(BoundFieldAccess fieldAccess, bool used)
        {
            var field = fieldAccess.FieldSymbol;

            //TODO: For static field access this may require ..ctor to run. Is this a side-effect?
            // Accessing unused instance field on a struct is a noop. Just emit the receiver.
            if (!used && !field.IsVolatile && !field.IsStatic && fieldAccess.ReceiverOpt.Type.IsVerifierValue())
            {
                EmitExpression(fieldAccess.ReceiverOpt, used: false);
                return;
            }

            Debug.Assert(!field.IsConst || field.ContainingType.SpecialType == SpecialType.System_Decimal,
                "rewriter should lower constant fields into constant expressions");

            if (field.IsStatic)
            {
                if (field.IsVolatile)
                {
                    _builder.EmitOpCode(ILOpCode.Volatile);
                }
                _builder.EmitOpCode(ILOpCode.Ldsfld);
                EmitSymbolToken(field, fieldAccess.Syntax);
            }
            else
            {
                var receiver = fieldAccess.ReceiverOpt;
                var fieldType = field.Type;
                if (fieldType.IsValueType && (object)fieldType == (object)receiver.Type)
                {
                    //Handle emitting a field of a self-containing struct (only possible in mscorlib)
                    //since "val.field" is the same as val, we only need to emit val.
                    EmitExpression(receiver, used);
                }
                else
                {
                    var temp = EmitFieldLoadReceiver(receiver);
                    if (temp != null)
                    {
                        Debug.Assert(FieldLoadMustUseRef(receiver), "only clr-ambiguous structs use temps here");
                        FreeTemp(temp);
                    }

                    if (field.IsVolatile)
                    {
                        _builder.EmitOpCode(ILOpCode.Volatile);
                    }

                    _builder.EmitOpCode(ILOpCode.Ldfld);
                    EmitSymbolToken(field, fieldAccess.Syntax);
                }
            }
            EmitPopIfUnused(used);
        }

        private LocalDefinition EmitFieldLoadReceiver(BoundExpression receiver)
        {
            // ldfld can work with structs directly or with their addresses
            // accessing via address is typically same or cheaper, but not for homeless values, obviously
            // there are also cases where we must emit receiver as a reference
            if (FieldLoadMustUseRef(receiver) || FieldLoadPrefersRef(receiver))
            {
                return EmitFieldLoadReceiverAddress(receiver) ? null : EmitReceiverRef(receiver);
            }

            EmitExpression(receiver, true);
            return null;
        }

        // In special case of loading the sequence of field accesses we can perform all the 
        // necessary field loads using the following IL: 
        //
        //      <expr>.a.b...y.z
        //          |
        //          V
        //      Unbox -or- Load.Ref (<expr>)
        //      Ldflda a
        //      Ldflda b
        //      ...
        //      Ldflda y
        //      Ldfld z
        //
        // Returns 'true' if the receiver was actually emitted this way
        private bool EmitFieldLoadReceiverAddress(BoundExpression receiver)
        {
            if (receiver == null || !receiver.Type.IsValueType)
            {
                return false;
            }
            else if (receiver.Kind == BoundKind.Conversion)
            {
                var conversion = (BoundConversion)receiver;
                if (conversion.ConversionKind == ConversionKind.Unboxing)
                {
                    EmitExpression(conversion.Operand, true);
                    _builder.EmitOpCode(ILOpCode.Unbox);
                    EmitSymbolToken(receiver.Type, receiver.Syntax);
                    return true;
                }
            }
            else if (receiver.Kind == BoundKind.FieldAccess)
            {
                var fieldAccess = (BoundFieldAccess)receiver;
                var field = fieldAccess.FieldSymbol;

                if (!field.IsStatic && EmitFieldLoadReceiverAddress(fieldAccess.ReceiverOpt))
                {
                    Debug.Assert(!field.IsVolatile, "volatile valuetype fields are unexpected");

                    _builder.EmitOpCode(ILOpCode.Ldflda);
                    EmitSymbolToken(field, fieldAccess.Syntax);
                    return true;
                }
            }

            return false;
        }

        // ldfld can work with structs directly or with their addresses
        // In some cases it results in same native code emitted, but in some cases JIT pushes values for real
        // resulting in much worse code (on x64 in particular).
        // So, we will always prefer references here except when receiver is a struct non-ref local or parameter. 
        private bool FieldLoadPrefersRef(BoundExpression receiver)
        {
            // only fields of structs can be accessed via value
            if (!receiver.Type.IsVerifierValue())
            {
                return true;
            }

            // can unbox directly into a ref.
            if (receiver.Kind == BoundKind.Conversion && ((BoundConversion)receiver).ConversionKind == ConversionKind.Unboxing)
            {
                return true;
            }

            // can we take address at all?
            if (!HasHome(receiver))
            {
                return false;
            }

            switch (receiver.Kind)
            {
                case BoundKind.Parameter:
                    // prefer ldarg over ldarga
                    return ((BoundParameter)receiver).ParameterSymbol.RefKind != RefKind.None;

                case BoundKind.Local:
                    // prefer ldloc over ldloca
                    return ((BoundLocal)receiver).LocalSymbol.RefKind != RefKind.None;

                case BoundKind.Sequence:
                    return FieldLoadPrefersRef(((BoundSequence)receiver).Value);

                case BoundKind.FieldAccess:
                    var fieldAccess = (BoundFieldAccess)receiver;
                    if (fieldAccess.FieldSymbol.IsStatic)
                    {
                        return true;
                    }

                    if (DiagnosticsPass.IsNonAgileFieldAccess(fieldAccess, _module.Compilation))
                    {
                        return false;
                    }

                    return FieldLoadPrefersRef(fieldAccess.ReceiverOpt);
            }

            return true;
        }

        internal static bool FieldLoadMustUseRef(BoundExpression expr)
        {
            var type = expr.Type;

            // type parameter values must be boxed to get access to fields
            if (type.IsTypeParameter())
            {
                return true;
            }

            // From   Dev12/symbol.cpp
            //  
            //  // Used by ILGEN to determine if the type of this AggregateSymbol is one that the CLR
            //  // will consider ambiguous to an unmanaged pointer when it is on the stack (see VSW #396011)
            //  bool AggregateSymbol::IsCLRAmbigStruct()
            //      . . .
            switch (type.SpecialType)
            {
                // case PT_BYTE:
                case SpecialType.System_Byte:
                // case PT_SHORT:
                case SpecialType.System_Int16:
                // case PT_INT:
                case SpecialType.System_Int32:
                // case PT_LONG:
                case SpecialType.System_Int64:
                // case PT_CHAR:
                case SpecialType.System_Char:
                // case PT_BOOL:
                case SpecialType.System_Boolean:
                // case PT_SBYTE:
                case SpecialType.System_SByte:
                // case PT_USHORT:
                case SpecialType.System_UInt16:
                // case PT_UINT:
                case SpecialType.System_UInt32:
                // case PT_ULONG:
                case SpecialType.System_UInt64:
                // case PT_INTPTR:
                case SpecialType.System_IntPtr:
                // case PT_UINTPTR:
                case SpecialType.System_UIntPtr:
                // case PT_FLOAT:
                case SpecialType.System_Single:
                // case PT_DOUBLE:
                case SpecialType.System_Double:
                // case PT_TYPEHANDLE:
                case SpecialType.System_RuntimeTypeHandle:
                // case PT_FIELDHANDLE:
                case SpecialType.System_RuntimeFieldHandle:
                // case PT_METHODHANDLE:
                case SpecialType.System_RuntimeMethodHandle:
                //case PT_ARGUMENTHANDLE:
                case SpecialType.System_RuntimeArgumentHandle:
                    return true;
            }

            // this is for value__
            // I do not know how to hit this, since value__ is not bindable in C#, but Dev12 has code to handle this
            return type.IsEnumType();
        }


        private static int ParameterSlot(BoundParameter parameter)
        {
            var sym = parameter.ParameterSymbol;
            int slot = sym.Ordinal;
            if (!sym.ContainingSymbol.IsStatic)
            {
                slot++;  // skip "this"
            }
            return slot;
        }

        private void EmitLocalLoad(BoundLocal local, bool used)
        {
            if (IsStackLocal(local.LocalSymbol))
            {
                // local must be already on the stack
                EmitPopIfUnused(used);
            }
            else
            {
                if (used)
                {
                    LocalDefinition definition = GetLocal(local);
                    _builder.EmitLocalLoad(definition);
                }
                else
                {
                    // do nothing. Unused local load has no side-effects.
                    return;
                }
            }

            if (used && local.LocalSymbol.RefKind != RefKind.None)
            {
                EmitLoadIndirect(local.LocalSymbol.Type, local.Syntax);
            }
        }

        private void EmitParameterLoad(BoundParameter parameter)
        {
            int slot = ParameterSlot(parameter);
            _builder.EmitLoadArgumentOpcode(slot);

            if (parameter.ParameterSymbol.RefKind != RefKind.None)
            {
                var parameterType = parameter.ParameterSymbol.Type;
                EmitLoadIndirect(parameterType, parameter.Syntax);
            }
        }

        private void EmitLoadIndirect(TypeSymbol type, CSharpSyntaxNode syntaxNode)
        {
            if (type.IsEnumType())
            {
                //underlying primitives do not need type tokens.
                type = ((NamedTypeSymbol)type).EnumUnderlyingType;
            }

            switch (type.PrimitiveTypeCode)
            {
                case Microsoft.Cci.PrimitiveTypeCode.Int8:
                    _builder.EmitOpCode(ILOpCode.Ldind_i1);
                    break;

                case Microsoft.Cci.PrimitiveTypeCode.Boolean:
                case Microsoft.Cci.PrimitiveTypeCode.UInt8:
                    _builder.EmitOpCode(ILOpCode.Ldind_u1);
                    break;

                case Microsoft.Cci.PrimitiveTypeCode.Int16:
                    _builder.EmitOpCode(ILOpCode.Ldind_i2);
                    break;

                case Microsoft.Cci.PrimitiveTypeCode.Char:
                case Microsoft.Cci.PrimitiveTypeCode.UInt16:
                    _builder.EmitOpCode(ILOpCode.Ldind_u2);
                    break;

                case Microsoft.Cci.PrimitiveTypeCode.Int32:
                    _builder.EmitOpCode(ILOpCode.Ldind_i4);
                    break;

                case Microsoft.Cci.PrimitiveTypeCode.UInt32:
                    _builder.EmitOpCode(ILOpCode.Ldind_u4);
                    break;

                case Microsoft.Cci.PrimitiveTypeCode.Int64:
                case Microsoft.Cci.PrimitiveTypeCode.UInt64:
                    _builder.EmitOpCode(ILOpCode.Ldind_i8);
                    break;

                case Microsoft.Cci.PrimitiveTypeCode.IntPtr:
                case Microsoft.Cci.PrimitiveTypeCode.UIntPtr:
                case Microsoft.Cci.PrimitiveTypeCode.Pointer:
                    _builder.EmitOpCode(ILOpCode.Ldind_i);
                    break;

                case Microsoft.Cci.PrimitiveTypeCode.Float32:
                    _builder.EmitOpCode(ILOpCode.Ldind_r4);
                    break;

                case Microsoft.Cci.PrimitiveTypeCode.Float64:
                    _builder.EmitOpCode(ILOpCode.Ldind_r8);
                    break;

                default:
                    if (type.IsVerifierReference())
                    {
                        _builder.EmitOpCode(ILOpCode.Ldind_ref);
                    }
                    else
                    {
                        _builder.EmitOpCode(ILOpCode.Ldobj);
                        EmitSymbolToken(type, syntaxNode);
                    }
                    break;
            }
        }

        /// <summary>
        /// Used to decide if we need to emit call or callvirt.
        /// It basically checks if the receiver expression cannot be null, but it is not 100% precise. 
        /// There are cases where it really can be null, but we do not care.
        /// </summary>
        private bool CanUseCallOnRefTypeReceiver(BoundExpression receiver)
        {
            // It seems none of the ways that could produce a receiver typed as a type param 
            // can guarantee that it is not null.
            if (receiver.Type.IsTypeParameter())
            {
                return false;
            }

            Debug.Assert(receiver.Type.IsVerifierReference(), "this is not a reference");
            Debug.Assert(receiver.Kind != BoundKind.BaseReference, "base should always use call");

            var constVal = receiver.ConstantValue;
            if (constVal != null)
            {
                // only when this is a constant Null, we need a callvirt
                return !constVal.IsNull;
            }

            switch (receiver.Kind)
            {
                case BoundKind.ArrayCreation:
                    return true;

                case BoundKind.ObjectCreationExpression:
                    //NOTE: there are cases involving ProxyAttribute
                    //where newobj may produce null
                    return true;

                case BoundKind.Conversion:
                    var conversion = (BoundConversion)receiver;

                    switch (conversion.ConversionKind)
                    {
                        case ConversionKind.Boxing:
                            //NOTE: boxing can produce null for Nullable, but any call through that
                            //will result in null reference exceptions anyways.
                            return true;

                        case ConversionKind.MethodGroup:
                        case ConversionKind.AnonymousFunction:
                            return true;

                        case ConversionKind.ExplicitReference:
                        case ConversionKind.ImplicitReference:
                            return CanUseCallOnRefTypeReceiver(conversion.Operand);
                    }
                    break;

                case BoundKind.ThisReference:
                    //NOTE: these actually can be null if called from a different language
                    //if that has already happen, we will just propagate the behavior.
                    return true;

                case BoundKind.DelegateCreationExpression:
                    return true;

                case BoundKind.Sequence:
                    var seqValue = ((BoundSequence)(receiver)).Value;
                    return CanUseCallOnRefTypeReceiver(seqValue);

                case BoundKind.AssignmentOperator:
                    var rhs = ((BoundAssignmentOperator)receiver).Right;
                    return CanUseCallOnRefTypeReceiver(rhs);

                case BoundKind.TypeOfOperator:
                    return true;

                case BoundKind.FieldAccess:
                    return ((BoundFieldAccess)receiver).FieldSymbol.IsCapturedFrame;

                case BoundKind.ConditionalReceiver:
                    return true;

                    //TODO: there could be more cases where we can be sure that receiver is not a null.
            }

            return false;
        }

        /// <summary>
        /// checks if receiver is effectively ldarg.0
        /// </summary>
        private bool IsThisReceiver(BoundExpression receiver)
        {
            switch (receiver.Kind)
            {
                case BoundKind.ThisReference:
                    return true;

                case BoundKind.Sequence:
                    var seqValue = ((BoundSequence)(receiver)).Value;
                    return IsThisReceiver(seqValue);
            }

            return false;
        }

        private enum CallKind
        {
            Call,
            CallVirt,
            ConstrainedCallVirt,
        }

        private void EmitCallExpression(BoundCall call, UseKind useKind)
        {
            var method = call.Method;
            var receiver = call.ReceiverOpt;
            LocalDefinition tempOpt = null;

            // Calls to the default struct constructor are emitted as initobj, rather than call.
            // NOTE: constructor invocations are represented as BoundObjectCreationExpressions,
            // rather than BoundCalls.  This is why we can be confident that if we see a call to a
            // constructor, it has this very specific form.
            if (method.IsDefaultValueTypeConstructor())
            {
                Debug.Assert(method.IsImplicitlyDeclared);
                Debug.Assert(method.ContainingType == receiver.Type);
                Debug.Assert(receiver.Kind == BoundKind.ThisReference);

                tempOpt = EmitReceiverRef(receiver);
                _builder.EmitOpCode(ILOpCode.Initobj);    //  initobj  <MyStruct>
                EmitSymbolToken(method.ContainingType, call.Syntax);
                FreeOptTemp(tempOpt);

                return;
            }

            var arguments = call.Arguments;

            CallKind callKind;

            if (method.IsStatic)
            {
                callKind = CallKind.Call;
            }
            else
            {
                var receiverType = receiver.Type;

                if (receiverType.IsVerifierReference())
                {
                    tempOpt = EmitReceiverRef(receiver, isAccessConstrained: false);

                    // In some cases CanUseCallOnRefTypeReceiver returns true which means that 
                    // null check is unnecessary and we can use "call"
                    if (receiver.SuppressVirtualCalls ||
                        (!method.IsMetadataVirtual() && CanUseCallOnRefTypeReceiver(receiver)))
                    {
                        callKind = CallKind.Call;
                    }
                    else
                    {
                        callKind = CallKind.CallVirt;
                    }
                }
                else if (receiverType.IsVerifierValue())
                {
                    NamedTypeSymbol methodContainingType = method.ContainingType;
                    if (methodContainingType.IsVerifierValue() && MayUseCallForStructMethod(method))
                    {
                        // NOTE: this should be either a method which overrides some abstract method or 
                        //       does not override anything (with few exceptions, see MayUseCallForStructMethod); 
                        //       otherwise we should not use direct 'call' and must use constrained call;

                        // calling a method defined in a value type
                        Debug.Assert(receiverType == methodContainingType);
                        tempOpt = EmitReceiverRef(receiver);
                        callKind = CallKind.Call;
                    }
                    else
                    {
                        if (method.IsMetadataVirtual())
                        {
                            // When calling a method that is virtual in metadata on a struct receiver, 
                            // we use a constrained virtual call. If possible, it will skip boxing.
                            tempOpt = EmitReceiverRef(receiver, isAccessConstrained: true);
                            callKind = CallKind.ConstrainedCallVirt;
                        }
                        else
                        {
                            // calling a method defined in a base class.
                            EmitExpression(receiver, used: true);
                            EmitBox(receiverType, receiver.Syntax);
                            callKind = CallKind.Call;
                        }
                    }
                }
                else
                {
                    // receiver is generic and method must come from the base or an interface or a generic constraint
                    // if the receiver is actually a value type it would need to be boxed.
                    // let .constrained sort this out. 
                    callKind = receiverType.IsReferenceType && !IsRef(receiver) ?
                                CallKind.CallVirt :
                                CallKind.ConstrainedCallVirt;

                    tempOpt = EmitReceiverRef(receiver, isAccessConstrained: callKind == CallKind.ConstrainedCallVirt);
                }
            }

            // When emitting a callvirt to a virtual method we always emit the method info of the
            // method that first declared the virtual method, not the method info of an
            // overriding method. It would be a subtle breaking change to change that rule;
            // see bug 6156 for details.

            MethodSymbol actualMethodTargetedByTheCall = method;
            if (method.IsOverride && callKind != CallKind.Call)
            {
                actualMethodTargetedByTheCall = method.GetConstructedLeastOverriddenMethod(_method.ContainingType);
            }

            if (callKind == CallKind.ConstrainedCallVirt && actualMethodTargetedByTheCall.ContainingType.IsValueType)
            {
                // special case for overridden methods like ToString(...) called on
                // value types: if the original method used in emit cannot use callvirt in this
                // case, change it to Call.
                callKind = CallKind.Call;
            }

            // Devirtualizing of calls to effectively sealed methods.
            if (callKind == CallKind.CallVirt)
            {
                // NOTE: we check that we call method in same module just to be sure
                // that it cannot be recompiled as not final and make our call not verifiable. 
                // such change by adversarial user would arguably be a compat break, but better be safe...
                // In reality we would typically have one method calling another method in the same class (one GetEnumerator calling another).
                // Other scenarios are uncommon since base class cannot be sealed and 
                // referring to a derived type in a different module is not an easy thing to do.
                if (IsThisReceiver(receiver) && actualMethodTargetedByTheCall.ContainingType.IsSealed &&
                        (object)actualMethodTargetedByTheCall.ContainingModule == (object)_method.ContainingModule)
                {
                    // special case for target is in a sealed class and "this" receiver.
                    Debug.Assert(receiver.Type.IsVerifierReference());
                    callKind = CallKind.Call;
                }

                // NOTE: we do not check that we call method in same module.
                // Because of the "GetOriginalConstructedOverriddenMethod" above, the actual target
                // can only be final when it is "newslot virtual final".
                // In such case Dev11 emits "call" and we will just replicate the behavior. (see DevDiv: 546853 )
                else if (actualMethodTargetedByTheCall.IsMetadataFinal && CanUseCallOnRefTypeReceiver(receiver))
                {
                    // special case for calling 'final' virtual method on reference receiver
                    Debug.Assert(receiver.Type.IsVerifierReference());
                    callKind = CallKind.Call;
                }
            }

            EmitArguments(arguments, method.Parameters);
            int stackBehavior = GetCallStackBehavior(call);
            switch (callKind)
            {
                case CallKind.Call:
                    _builder.EmitOpCode(ILOpCode.Call, stackBehavior);
                    break;

                case CallKind.CallVirt:
                    _builder.EmitOpCode(ILOpCode.Callvirt, stackBehavior);
                    break;

                case CallKind.ConstrainedCallVirt:
                    _builder.EmitOpCode(ILOpCode.Constrained);
                    EmitSymbolToken(receiver.Type, receiver.Syntax);
                    _builder.EmitOpCode(ILOpCode.Callvirt, stackBehavior);
                    break;
            }

            EmitSymbolToken(actualMethodTargetedByTheCall, call.Syntax,
                            actualMethodTargetedByTheCall.IsVararg ? (BoundArgListOperator)call.Arguments[call.Arguments.Length - 1] : null);

            if (!method.ReturnsVoid)
            {
                EmitPopIfUnused(useKind != UseKind.Unused);
            }
            else if (_ilEmitStyle == ILEmitStyle.Debug)
            {
                // The only void methods with usable return values are constructors and we represent those
                // as BoundObjectCreationExpressions, not BoundCalls.
                Debug.Assert(useKind == UseKind.Unused, "Using the return value of a void method.");
                Debug.Assert(_method.GenerateDebugInfo, "Implied by this.emitSequencePoints");

                // DevDiv #15135.  When a method like System.Diagnostics.Debugger.Break() is called, the
                // debugger sees an event indicating that a user break (vs a breakpoint) has occurred.
                // When this happens, it uses ICorDebugILFrame.GetIP(out uint, out CorDebugMappingResult)
                // to determine the current instruction pointer.  This method returns the instruction
                // *after* the call.  The source location is then given by the last sequence point before
                // or on this instruction.  As a result, if the instruction after the call has its own
                // sequence point, then that sequence point will be used to determine the source location
                // and the debugging experience will be disrupted.  The easiest way to ensure that the next
                // instruction does not have a sequence point is to insert a nop.  Obviously, we only do this
                // if debugging is enabled and optimization is disabled.

                // From ILGENREC::genCall:
                //   We want to generate a NOP after CALL opcodes that end a statement so the debugger
                //   has better stepping behavior

                // CONSIDER: In the native compiler, there's an additional restriction on when this nop is
                // inserted.  It is quite complicated, but it basically seems to say that, if we thought
                // we could omit the temp-and-copy for a struct construction and it turned out that we
                // couldn't (perhaps because the assigned local was captured by a lambda), and if we're
                // not using the result of the constructor call (how can this even happen?), then we don't
                // want to insert the nop.  Since the consequence of not implementing this complicated logic
                // is an extra nop in debug code, this is likely not a priority.

                // CONSIDER: The native compiler also checks !(tree->flags & EXF_NODEBUGINFO).  We don't have
                // this mutable bit on our bound nodes, so we can't exactly match the behavior.  We might be
                // able to approximate the native behavior by inspecting call.WasCompilerGenerated, but it is
                // not in a reliable state after lowering.

                _builder.EmitOpCode(ILOpCode.Nop);
            }

            if (useKind == UseKind.UsedAsValue && method.RefKind != RefKind.None)
            {
                EmitLoadIndirect(method.ReturnType, call.Syntax);
            }
            else if (useKind == UseKind.UsedAsAddress)
            {
                Debug.Assert(method.RefKind != RefKind.None);
            }

            FreeOptTemp(tempOpt);
        }

        // returns true when receiver is already a ref.
        // in such cases calling through a ref could be preferred over 
        // calling through indirectly loaded value.
        private bool IsRef(BoundExpression receiver)
        {
            switch (receiver.Kind)
            {
                case BoundKind.Local:
                    return ((BoundLocal)receiver).LocalSymbol.RefKind != RefKind.None;

                case BoundKind.Parameter:
                    return ((BoundParameter)receiver).ParameterSymbol.RefKind != RefKind.None;

                case BoundKind.Call:
                    return ((BoundCall)receiver).Method.RefKind != RefKind.None;

                case BoundKind.Dup:
                    return ((BoundDup)receiver).RefKind != RefKind.None;

                case BoundKind.Sequence:
                    return IsRef(((BoundSequence)receiver).Value);
            }

            return false;
        }

        private static int GetCallStackBehavior(BoundCall call)
        {
            int stack = 0;

            if (!call.Method.ReturnsVoid)
            {
                // The call puts the return value on the stack.
                stack += 1;
            }

            if (!call.Method.IsStatic)
            {
                // The call pops the receiver off the stack.
                stack -= 1;
            }

            if (call.Method.IsVararg)
            {
                // The call pops all the arguments, fixed and variadic.
                int fixedArgCount = call.Arguments.Length - 1;
                int varArgCount = ((BoundArgListOperator)call.Arguments[fixedArgCount]).Arguments.Length;
                stack -= fixedArgCount;
                stack -= varArgCount;
            }
            else
            {
                // The call pops all the arguments.
                stack -= call.Arguments.Length;
            }

            return stack;
        }

        private static int GetObjCreationStackBehavior(BoundObjectCreationExpression objCreation)
        {
            int stack = 0;

            // Constructor puts the return value on the stack.
            stack += 1;

            if (objCreation.Constructor.IsVararg)
            {
                // Constructor pops all the arguments, fixed and variadic.
                int fixedArgCount = objCreation.Arguments.Length - 1;
                int varArgCount = ((BoundArgListOperator)objCreation.Arguments[fixedArgCount]).Arguments.Length;
                stack -= fixedArgCount;
                stack -= varArgCount;
            }
            else
            {
                // Constructor pops all the arguments.
                stack -= objCreation.Arguments.Length;
            }

            return stack;
        }

        /// <summary>
        /// Used to decide if we need to emit 'call' or 'callvirt' for structure method.
        /// It basically checks if the method overrides any other and method's defining type
        /// is not a 'special' or 'special-by-ref' type. 
        /// </summary>
        internal static bool MayUseCallForStructMethod(MethodSymbol method)
        {
            Debug.Assert(method.ContainingType.IsVerifierValue(), "this is not a value type");

            if (!method.IsMetadataVirtual())
            {
                return true;
            }

            var overriddenMethod = method.OverriddenMethod;
            if ((object)overriddenMethod == null || overriddenMethod.IsAbstract)
            {
                return true;
            }

            var containingType = method.ContainingType;
            // overrides in structs that are special types can be caled directly.
            // we can assume that special types will not be removing oiverrides
            return containingType.SpecialType != SpecialType.None;
        }

        /// <summary>
        /// When array operation get long or ulong arguments the args should be 
        /// cast to native int.
        /// Note that the cast is always checked.
        /// </summary>
        private void TreatLongsAsNative(Microsoft.Cci.PrimitiveTypeCode tc)
        {
            if (tc == Microsoft.Cci.PrimitiveTypeCode.Int64)
            {
                _builder.EmitOpCode(ILOpCode.Conv_ovf_i);
            }
            else if (tc == Microsoft.Cci.PrimitiveTypeCode.UInt64)
            {
                _builder.EmitOpCode(ILOpCode.Conv_ovf_i_un);
            }
        }

        private void EmitArrayLength(BoundArrayLength expression, bool used)
        {
            // The binder recognizes Array.Length and Array.LongLength and creates BoundArrayLength for them.
            // 
            // ArrayLength can be either 
            //      int32 for Array.Length
            //      int64 for Array.LongLength
            //      UIntPtr for synthetic code that needs just check if length != 0 - 
            //                  this is used in "fixed(int* ptr = arr)"
            Debug.Assert(expression.Type.SpecialType == SpecialType.System_Int32 ||
                expression.Type.SpecialType == SpecialType.System_Int64 ||
                expression.Type.SpecialType == SpecialType.System_UIntPtr);

            // ldlen will null-check the expression so it must be "used"
            EmitExpression(expression.Expression, used: true);
            _builder.EmitOpCode(ILOpCode.Ldlen);

            var typeTo = expression.Type.PrimitiveTypeCode;

            // NOTE: ldlen returns native uint, but newarr takes native int, so the length value is always 
            //       a positive native int. We can treat it as either signed or unsigned.
            //       We will use whatever typeTo says so we do not need to convert because of sign.
            var typeFrom = typeTo.IsUnsigned() ? Microsoft.Cci.PrimitiveTypeCode.UIntPtr : Microsoft.Cci.PrimitiveTypeCode.IntPtr;

            // NOTE: In Dev10 C# this cast is unchecked.
            // That seems to be wrong since that would cause silent truncation on 64bit platform if that implements large arrays. 
            // 
            // Emitting checked conversion however results in redundant overflow checks on 64bit and also inhibits range check hoisting in loops.
            // Therefore we will emit unchecked conversion here as C# compiler always did.
            _builder.EmitNumericConversion(typeFrom, typeTo, @checked: false);

            EmitPopIfUnused(used);
        }

        private void EmitArrayCreationExpression(BoundArrayCreation expression, bool used)
        {
            var arrayType = (ArrayTypeSymbol)expression.Type;

            EmitArrayIndices(expression.Bounds);

            if (arrayType.IsSZArray)
            {
                _builder.EmitOpCode(ILOpCode.Newarr);
                EmitSymbolToken(arrayType.ElementType, expression.Syntax);
            }
            else
            {
                _builder.EmitArrayCreation(Emit.PEModuleBuilder.Translate(arrayType), expression.Syntax, _diagnostics);
            }

            if (expression.InitializerOpt != null)
            {
                EmitArrayInitializers(arrayType, expression.InitializerOpt);
            }

            // newarr has side-effects (negative bounds etc) so always emitted.
            EmitPopIfUnused(used);
        }

        private void EmitStackAllocArrayCreationExpression(BoundStackAllocArrayCreation expression, bool used)
        {
            EmitExpression(expression.Count, used: true);
            _builder.EmitOpCode(ILOpCode.Localloc);
            EmitPopIfUnused(used); //localalloc could overflow the stack, so don't omit, even if used.
        }

        private void EmitObjectCreationExpression(BoundObjectCreationExpression expression, bool used)
        {
            MethodSymbol constructor = expression.Constructor;
            if (constructor.IsDefaultValueTypeConstructor())
            {
                EmitInitObj(expression.Type, used, expression.Syntax);
            }
            else
            {
                if (!used &&
                    expression.Constructor.OriginalDefinition == _module.Compilation.GetSpecialTypeMember(SpecialMember.System_Nullable_T__ctor))
                {
                    // creating nullable has no side-effects, so we will just evaluate the arg
                    EmitExpression(expression.Arguments[0], used: false);
                }
                else
                {
                    EmitArguments(expression.Arguments, constructor.Parameters);

                    var stackAdjustment = GetObjCreationStackBehavior(expression);
                    _builder.EmitOpCode(ILOpCode.Newobj, stackAdjustment);

                    // for variadic ctors emit expanded ctor token
                    EmitSymbolToken(constructor, expression.Syntax,
                                    constructor.IsVararg ? (BoundArgListOperator)expression.Arguments[expression.Arguments.Length - 1] : null);

                    EmitPopIfUnused(used);
                }
            }
        }

        private void EmitAssignmentExpression(BoundAssignmentOperator assignmentOperator, UseKind useKind)
        {
            if (TryEmitAssignmentInPlace(assignmentOperator, useKind != UseKind.Unused))
            {
                Debug.Assert(assignmentOperator.RefKind == RefKind.None);
                return;
            }

            // Assignment expression codegen has the following parts:
            //
            // * PreRHS: We need to emit instructions before the load of the right hand side if:
            //   - If the left hand side is a ref local or ref formal parameter and the right hand 
            //     side is a value then we must put the ref on the stack early so that we can store 
            //     indirectly into it.
            //   - If the left hand side is an array slot then we must evaluate the array and indices
            //     before we evaluate the right hand side. We ensure that the array and indices are 
            //     on the stack when the store is executed.
            //   - Similarly, if the left hand side is a non-static field then its receiver must be
            //     evaluated before the right hand side.
            //
            // * RHS: There are three possible ways to do an assignment with respect to "refness", 
            //   and all are found in the lowering of:
            //
            //   N().s += 10;
            //
            //   That expression is realized as 
            //
            //   ref int addr = ref N().s;   // Assign a ref on the right hand side to the left hand side.
            //   int sum = addr + 10;        // No refs at all; assign directly to sum.
            //   addr = sum;                 // Assigns indirectly through the address.
            //
            //   - If we are in the first case then assignmentOperator.RefKind is Ref and the left hand side is a 
            //     ref local temporary. We simply assign the ref on the RHS to the storage on the LHS with no indirection.
            //
            //   - If we are in the second case then nothing is ref; we have a value on one side an a local on the other.
            //     Again, there is no indirection.
            // 
            //   - If we are in the third case then we have a ref on the left and a value on the right. We must compute the
            //     value of the right hand side and then store it into the left hand side.
            //
            // * Duplication: The result of an assignment operation is the value that was assigned. It is possible that 
            //   later codegen is expecting this value to be on the stack when we're done here. This is controlled by
            //   the "used" formal parameter. There are two possible cases:
            //   - If the preamble put stuff on the stack for the usage of the store, then we must not put an extra copy
            //     of the right hand side value on the stack; that will be between the value and the stuff needed to 
            //     do the storage. In that case we put the right hand side value in a temporary and restore it later.
            //   - Otherwise we can just do a dup instruction; there's nothing before the dup on the stack that we'll need.
            // 
            // * Storage: Either direct or indirect, depending. See the RHS section above for details.
            // 
            // * Post-storage: If we stashed away the duplicated value in the temporary, we need to restore it back to the stack.

            bool lhsUsesStack = EmitAssignmentPreamble(assignmentOperator);
            EmitAssignmentValue(assignmentOperator);
            LocalDefinition temp = EmitAssignmentDuplication(assignmentOperator, useKind, lhsUsesStack);
            EmitStore(assignmentOperator);
            EmitAssignmentPostfix(assignmentOperator, temp, useKind);
        }

        // sometimes it is possible and advantageous to get an address of the lHS and 
        // perform assignment as an in-place initialization via initobj or constructor invocation.
        //
        // 1) initobj 
        //    is used when assigning default value to T that is not a verifier reference.
        //
        // 2) in-place ctor call 
        //    is used when assigning a freshly created struct. "x = new S(arg)" can be
        //    replaced by x.S(arg) as long as partial assignment cannot be observed -
        //    i.e. target must not be on the heap and we should not be in a try block.
        private bool TryEmitAssignmentInPlace(BoundAssignmentOperator assignmentOperator, bool used)
        {
            var left = assignmentOperator.Left;

            // if result is used, and lives on heap, we must keep RHS value on the stack.
            // otherwise we can try conjuring up the RHS value directly where it belongs.
            if (used && !TargetIsNotOnHeap(left))
            {
                return false;
            }

            if (!SafeToGetWriteableReference(left))
            {
                // cannot take a ref
                return false;
            }

            var right = assignmentOperator.Right;
            var rightType = right.Type;

            // in-place is not advantageous for reference types or constants
            if (!rightType.IsTypeParameter())
            {
                if (rightType.IsReferenceType || (right.ConstantValue != null && rightType.SpecialType != SpecialType.System_Decimal))
                {
                    return false;
                }
            }

            if (right.IsDefaultValue())
            {
                InPlaceInit(left, used);
                return true;
            }

            if (right.Kind == BoundKind.ObjectCreationExpression)
            {
                // It is desirable to do in-place ctor call if possible.
                // we could do newobj/stloc, but in-place call 
                // produces same or better code in current JITs 
                if (PartialCtorResultCannotEscape(left))
                {
                    var objCreation = (BoundObjectCreationExpression)right;
                    InPlaceCtorCall(left, objCreation, used);
                    return true;
                }
            }

            return false;
        }

        private bool SafeToGetWriteableReference(BoundExpression left)
        {
            if (!HasHome(left))
            {
                return false;
            }

            // because of array covariance, taking a reference to an element of 
            // generic array may fail even though assignment "arr[i] = default(T)" would always succeed.
            if (left.Kind == BoundKind.ArrayAccess && left.Type.TypeKind == TypeKind.TypeParameter && !left.Type.IsValueType)
            {
                return false;
            }

            if (left.Kind == BoundKind.FieldAccess)
            {
                var fieldAccess = (BoundFieldAccess)left;
                if (fieldAccess.FieldSymbol.IsVolatile ||
                    DiagnosticsPass.IsNonAgileFieldAccess(fieldAccess, _module.Compilation))
                {
                    return false;
                }
            }

            return true;
        }

        private void InPlaceInit(BoundExpression target, bool used)
        {
            var temp = EmitAddress(target, AddressKind.Writeable);
            Debug.Assert(temp == null, "in-place init target should not create temps");

            _builder.EmitOpCode(ILOpCode.Initobj);    //  intitobj  <MyStruct>
            EmitSymbolToken(target.Type, target.Syntax);

            if (used)
            {
                Debug.Assert(TargetIsNotOnHeap(target), "cannot read-back the target since it could have been modified");
                EmitExpression(target, used);
            }
        }

        private void InPlaceCtorCall(BoundExpression target, BoundObjectCreationExpression objCreation, bool used)
        {
            var temp = EmitAddress(target, AddressKind.Writeable);
            Debug.Assert(temp == null, "in-place ctor target should not create temps");

            var constructor = objCreation.Constructor;
            EmitArguments(objCreation.Arguments, constructor.Parameters);
            // -2 to adjust for consumed target address and not produced value.
            var stackAdjustment = GetObjCreationStackBehavior(objCreation) - 2;
            _builder.EmitOpCode(ILOpCode.Call, stackAdjustment);
            // for variadic ctors emit expanded ctor token
            EmitSymbolToken(constructor, objCreation.Syntax,
                            constructor.IsVararg ? (BoundArgListOperator)objCreation.Arguments[objCreation.Arguments.Length - 1] : null);

            if (used)
            {
                Debug.Assert(TargetIsNotOnHeap(target), "cannot read-back the target since it could have been modified");
                EmitExpression(target, used: true);
            }
        }

        // partial ctor results are not observable when target is not on the heap.
        // we also must not be in a try, otherwise if ctor throws
        // partially assigned value may be observed in the handler.
        private bool PartialCtorResultCannotEscape(BoundExpression left)
        {
            if (TargetIsNotOnHeap(left))
            {
                if (_tryNestingLevel != 0)
                {
                    var local = left as BoundLocal;
                    if (local != null && !_builder.PossiblyDefinedOutsideOfTry(GetLocal(local)))
                    {
                        // local defined inside immediate Try - cannot escape
                        return true;
                    }

                    // local defined outside of immediate try or it is a parameter - can escape
                    return false;
                }

                // we are not in a try - locals, parameters cannot escape
                return true;
            }

            // left is a reference, partial initializations can escape.
            return false;
        }

        // returns True when assignment target is definitely not on the heap
        private static bool TargetIsNotOnHeap(BoundExpression left)
        {
            switch (left.Kind)
            {
                case BoundKind.Parameter:
                    return ((BoundParameter)left).ParameterSymbol.RefKind == RefKind.None;

                case BoundKind.Local:
                    // NOTE: stack locals are either homeless or refs, no need to special case them
                    //       they will never be assigned in-place.
                    return ((BoundLocal)left).LocalSymbol.RefKind == RefKind.None;
            }

            return false;
        }


        private bool EmitAssignmentPreamble(BoundAssignmentOperator assignmentOperator)
        {
            var assignmentTarget = assignmentOperator.Left;
            bool lhsUsesStack = false;

            switch (assignmentTarget.Kind)
            {
                case BoundKind.RefValueOperator:
                    EmitRefValueAddress((BoundRefValueOperator)assignmentTarget);
                    break;

                case BoundKind.FieldAccess:
                    {
                        var left = (BoundFieldAccess)assignmentTarget;
                        if (!left.FieldSymbol.IsStatic)
                        {
                            var temp = EmitReceiverRef(left.ReceiverOpt);
                            Debug.Assert(temp == null, "temp is unexpected when assigning to a field");
                            lhsUsesStack = true;
                        }
                    }
                    break;

                case BoundKind.Parameter:
                    {
                        var left = (BoundParameter)assignmentTarget;
                        if (left.ParameterSymbol.RefKind != RefKind.None)
                        {
                            _builder.EmitLoadArgumentOpcode(ParameterSlot(left));
                            lhsUsesStack = true;
                        }
                    }
                    break;

                case BoundKind.Local:
                    {
                        var left = (BoundLocal)assignmentTarget;

                        // Again, consider our earlier case:
                        //
                        // ref int addr = ref N().s;
                        // int sum = addr + 10; 
                        // addr = sum;
                        //
                        // There are three different ways we could be assigning to a local.
                        //
                        // In the first case, we want to simply call N(), take the address
                        // of s, and then store that address in addr.
                        //
                        // In the second case again we simply want to compute the sum and
                        // store the result in sum.
                        //
                        // In the third case however we want to first load the contents of
                        // addr -- the address of field s -- then put the sum on the stack,
                        // and then do an indirect store. In that case we need to have the
                        // contents of addr on the stack.

                        if (left.LocalSymbol.RefKind != RefKind.None && assignmentOperator.RefKind == RefKind.None)
                        {
                            if (!IsStackLocal(left.LocalSymbol))
                            {
                                LocalDefinition localDefinition = GetLocal(left);
                                _builder.EmitLocalLoad(localDefinition);
                            }
                            else
                            {
                                // this is a case of indirect assignment to a stack temp.
                                // currently byref temp can only be a stack local in scenarios where 
                                // there is only one assignment and it is the last one. 
                                // I do not yet know how to support cases where we assign more than once. 
                                // That where Dup of LHS would be needed, but as a general scenario 
                                // it is not always possible to handle. Fortunately all the cases where we
                                // indirectly assign to a byref temp come from rewriter and all
                                // they all are write-once cases.
                                //
                                // For now analyzer asserts that indirect writes are final reads of 
                                // a ref local. And we never need a dup here.

                                // builder.EmitOpCode(ILOpCode.Dup);
                            }

                            lhsUsesStack = true;
                        }
                    }
                    break;

                case BoundKind.ArrayAccess:
                    {
                        var left = (BoundArrayAccess)assignmentTarget;
                        EmitExpression(left.Expression, used: true);
                        EmitArrayIndices(left.Indices);
                        lhsUsesStack = true;
                    }
                    break;

                case BoundKind.ThisReference:
                    {
                        var left = (BoundThisReference)assignmentTarget;

                        var temp = EmitAddress(left, AddressKind.Writeable);
                        Debug.Assert(temp == null, "taking ref of this should not create a temp");

                        lhsUsesStack = true;
                    }
                    break;

                case BoundKind.Dup:
                    {
                        var left = (BoundDup)assignmentTarget;

                        var temp = EmitAddress(left, AddressKind.Writeable);
                        Debug.Assert(temp == null, "taking ref of Dup should not create a temp");

                        lhsUsesStack = true;
                    }
                    break;

                case BoundKind.PointerIndirectionOperator:
                    {
                        var left = (BoundPointerIndirectionOperator)assignmentTarget;

                        EmitExpression(left.Operand, used: true);

                        lhsUsesStack = true;
                    }
                    break;

                case BoundKind.Sequence:
                    {
                        var sequence = (BoundSequence)assignmentTarget;

                        DefineLocals(sequence);
                        EmitSideEffects(sequence);

                        lhsUsesStack = EmitAssignmentPreamble(assignmentOperator.Update(sequence.Value, assignmentOperator.Right, assignmentOperator.RefKind, assignmentOperator.Type));

                        // doNotRelease will be released in EmitStore after we are done with the whole assignment.
                        var doNotRelease = DigForValueLocal(sequence);
                        FreeLocals(sequence, doNotRelease);
                    }
                    break;

                case BoundKind.Call:
                    {
                        var left = (BoundCall)assignmentOperator.Left;

                        Debug.Assert(left.Method.RefKind != RefKind.None);
                        EmitCallExpression(left, UseKind.UsedAsAddress);

                        lhsUsesStack = true;
                    }
                    break;

                case BoundKind.AssignmentOperator:
                    {
                        var left = (BoundAssignmentOperator)assignmentOperator.Left;

                        Debug.Assert(left.RefKind != RefKind.None);
                        EmitAssignmentExpression(left, UseKind.UsedAsAddress);

                        lhsUsesStack = true;
                    }
                    break;

                case BoundKind.PropertyAccess:
                case BoundKind.IndexerAccess:
                // Property access should have been rewritten.
                case BoundKind.PreviousSubmissionReference:
                    // Script references are lowered to a this reference and a field access.
                    throw ExceptionUtilities.UnexpectedValue(assignmentTarget.Kind);

                case BoundKind.PseudoVariable:
                    EmitPseudoVariableAddress((BoundPseudoVariable)assignmentTarget);
                    lhsUsesStack = true;
                    break;
            }

            return lhsUsesStack;
        }

        private void EmitAssignmentValue(BoundAssignmentOperator assignmentOperator)
        {
            if (assignmentOperator.RefKind == RefKind.None)
            {
                EmitExpression(assignmentOperator.Right, used: true);
            }
            else
            {
                // LEAKING A TEMP IS OK HERE 
                // generally taking a ref for the purpose of ref assignment should not be done on homeless values
                // however, there are very rare cases when we need to get a ref off a copy in synthetic code and we have to leak those.
                // fortunately these are very short-lived temps that should not cause value sharing.
                var temp = EmitAddress(assignmentOperator.Right, AddressKind.Writeable);
#if DEBUG
                Debug.Assert(temp == null || ((SynthesizedLocal)assignmentOperator.Left.ExpressionSymbol).SynthesizedKind == SynthesizedLocalKind.LoweringTemp);
#endif
            }
        }

        private LocalDefinition EmitAssignmentDuplication(BoundAssignmentOperator assignmentOperator, UseKind useKind, bool lhsUsesStack)
        {
            LocalDefinition temp = null;
            if (useKind != UseKind.Unused)
            {
                _builder.EmitOpCode(ILOpCode.Dup);

                if (lhsUsesStack)
                {
                    // Today we sometimes have a case where we assign a ref directly to a temporary of ref type:
                    //
                    // ref int addr = ref N().y;  <-- copies the address by value; no indirection
                    // int sum = addr + 10;
                    // addr = sum;
                    //
                    // In "Redhawk" we can write this sort of code directly as well. However, we should
                    // never have a case where the value of the assignment is "used", either in our own
                    // lowering passes or in Redhawk. We never have something like:
                    //
                    // ref int t1 = (ref int t2 = ref M().s); 
                    //
                    // or the even more odd:
                    //
                    // int t1 = (ref int t2 = ref M().s);
                    //
                    // Therefore we don't have to worry about what if the temporary value we are stashing
                    // away is of ref type.
                    //
                    // If we ever do implement this sort of feature then we will need to figure out which
                    // of the situations above we are in, and ensure that the correct kind of temporary
                    // is created here. And also that either its value or its indirected value is read out
                    // after the store, in EmitAssignmentPostfix, below.

                    Debug.Assert(assignmentOperator.RefKind == RefKind.None);

                    temp = AllocateTemp(assignmentOperator.Left.Type, assignmentOperator.Left.Syntax);
                    _builder.EmitLocalStore(temp);
                }
            }
            return temp;
        }

        private void EmitStore(BoundAssignmentOperator assignment)
        {
            BoundExpression expression = assignment.Left;
            switch (expression.Kind)
            {
                case BoundKind.FieldAccess:
                    EmitFieldStore((BoundFieldAccess)expression);
                    break;

                case BoundKind.Local:
                    // If we are doing a 'normal' local assignment like 'int t = 10;', or
                    // if we are initializing a temporary like 'ref int t = ref M().s;' then
                    // we just emit a local store. If we are doing an assignment through
                    // a ref local temporary then we assume that the instruction to load
                    // the address is already on the stack, and we must indirect through it.

                    // See the comments in EmitAssignmentExpression above for details.
                    BoundLocal local = (BoundLocal)expression;
                    if (local.LocalSymbol.RefKind != RefKind.None && assignment.RefKind == RefKind.None)
                    {
                        EmitIndirectStore(local.LocalSymbol.Type, local.Syntax);
                    }
                    else
                    {
                        if (IsStackLocal(local.LocalSymbol))
                        {
                            // assign to stack var == leave original value on stack
                            break;
                        }
                        else
                        {
                            _builder.EmitLocalStore(GetLocal(local));
                        }
                    }
                    break;

                case BoundKind.ArrayAccess:
                    var array = ((BoundArrayAccess)expression).Expression;
                    var arrayType = (ArrayTypeSymbol)array.Type;
                    EmitArrayElementStore(arrayType, expression.Syntax);
                    break;

                case BoundKind.ThisReference:
                    EmitThisStore((BoundThisReference)expression);
                    break;

                case BoundKind.Parameter:
                    EmitParameterStore((BoundParameter)expression);
                    break;

                case BoundKind.Dup:
                    Debug.Assert(((BoundDup)expression).RefKind != RefKind.None);
                    EmitIndirectStore(expression.Type, expression.Syntax);
                    break;

                case BoundKind.RefValueOperator:
                case BoundKind.PointerIndirectionOperator:
                case BoundKind.PseudoVariable:
                    EmitIndirectStore(expression.Type, expression.Syntax);
                    break;

                case BoundKind.Sequence:
                    {
                        var sequence = (BoundSequence)expression;
                        EmitStore(assignment.Update(sequence.Value, assignment.Right, assignment.RefKind, assignment.Type));

                        var notReleased = DigForValueLocal(sequence);
                        if (notReleased != null)
                        {
                            FreeLocal(notReleased);
                        }
                    }
                    break;

                case BoundKind.Call:
                    Debug.Assert(((BoundCall)expression).Method.RefKind != RefKind.None);
                    EmitIndirectStore(expression.Type, expression.Syntax);
                    break;

                case BoundKind.AssignmentOperator:
                    Debug.Assert(((BoundAssignmentOperator)expression).RefKind != RefKind.None);
                    EmitIndirectStore(expression.Type, expression.Syntax);
                    break;

                case BoundKind.PreviousSubmissionReference:
                // Script references are lowered to a this reference and a field access.
                default:
                    throw ExceptionUtilities.UnexpectedValue(expression.Kind);
            }
        }

        private void EmitAssignmentPostfix(BoundAssignmentOperator assignment, LocalDefinition temp, UseKind useKind)
        {
            if (temp != null)
            {
                _builder.EmitLocalLoad(temp);
                FreeTemp(temp);
            }

            if (useKind == UseKind.UsedAsValue && assignment.RefKind != RefKind.None)
            {
                EmitLoadIndirect(assignment.Type, assignment.Syntax);
            }
        }

        private void EmitThisStore(BoundThisReference thisRef)
        {
            Debug.Assert(thisRef.Type.IsValueType);

            _builder.EmitOpCode(ILOpCode.Stobj);
            EmitSymbolToken(thisRef.Type, thisRef.Syntax);
        }

        private void EmitArrayElementStore(ArrayTypeSymbol arrayType, CSharpSyntaxNode syntaxNode)
        {
            if (arrayType.IsSZArray)
            {
                EmitVectorElementStore(arrayType, syntaxNode);
            }
            else
            {
                _builder.EmitArrayElementStore(Emit.PEModuleBuilder.Translate(arrayType), syntaxNode, _diagnostics);
            }
        }

        /// <summary>
        /// Emit an element store instruction for a single dimensional array.
        /// </summary>
        private void EmitVectorElementStore(ArrayTypeSymbol arrayType, CSharpSyntaxNode syntaxNode)
        {
            var elementType = arrayType.ElementType;

            if (elementType.IsEnumType())
            {
                //underlying primitives do not need type tokens.
                elementType = ((NamedTypeSymbol)elementType).EnumUnderlyingType;
            }

            switch (elementType.PrimitiveTypeCode)
            {
                case Microsoft.Cci.PrimitiveTypeCode.Boolean:
                case Microsoft.Cci.PrimitiveTypeCode.Int8:
                case Microsoft.Cci.PrimitiveTypeCode.UInt8:
                    _builder.EmitOpCode(ILOpCode.Stelem_i1);
                    break;

                case Microsoft.Cci.PrimitiveTypeCode.Char:
                case Microsoft.Cci.PrimitiveTypeCode.Int16:
                case Microsoft.Cci.PrimitiveTypeCode.UInt16:
                    _builder.EmitOpCode(ILOpCode.Stelem_i2);
                    break;

                case Microsoft.Cci.PrimitiveTypeCode.Int32:
                case Microsoft.Cci.PrimitiveTypeCode.UInt32:
                    _builder.EmitOpCode(ILOpCode.Stelem_i4);
                    break;

                case Microsoft.Cci.PrimitiveTypeCode.Int64:
                case Microsoft.Cci.PrimitiveTypeCode.UInt64:
                    _builder.EmitOpCode(ILOpCode.Stelem_i8);
                    break;

                case Microsoft.Cci.PrimitiveTypeCode.IntPtr:
                case Microsoft.Cci.PrimitiveTypeCode.UIntPtr:
                case Microsoft.Cci.PrimitiveTypeCode.Pointer:
                    _builder.EmitOpCode(ILOpCode.Stelem_i);
                    break;

                case Microsoft.Cci.PrimitiveTypeCode.Float32:
                    _builder.EmitOpCode(ILOpCode.Stelem_r4);
                    break;

                case Microsoft.Cci.PrimitiveTypeCode.Float64:
                    _builder.EmitOpCode(ILOpCode.Stelem_r8);
                    break;

                default:
                    if (elementType.IsVerifierReference())
                    {
                        _builder.EmitOpCode(ILOpCode.Stelem_ref);
                    }
                    else
                    {
                        _builder.EmitOpCode(ILOpCode.Stelem);
                        EmitSymbolToken(elementType, syntaxNode);
                    }
                    break;
            }
        }

        private void EmitFieldStore(BoundFieldAccess fieldAccess)
        {
            var field = fieldAccess.FieldSymbol;

            if (field.IsVolatile)
            {
                _builder.EmitOpCode(ILOpCode.Volatile);
            }

            _builder.EmitOpCode(field.IsStatic ? ILOpCode.Stsfld : ILOpCode.Stfld);
            EmitSymbolToken(field, fieldAccess.Syntax);
        }

        private void EmitParameterStore(BoundParameter parameter)
        {
            int slot = ParameterSlot(parameter);

            if (parameter.ParameterSymbol.RefKind == RefKind.None)
            {
                _builder.EmitStoreArgumentOpcode(slot);
            }
            else
            {
                //NOTE: we should have the actual parameter already loaded, 
                //now need to do a store to where it points to
                EmitIndirectStore(parameter.ParameterSymbol.Type, parameter.Syntax);
            }
        }

        private void EmitIndirectStore(TypeSymbol type, CSharpSyntaxNode syntaxNode)
        {
            if (type.IsEnumType())
            {
                //underlying primitives do not need type tokens.
                type = ((NamedTypeSymbol)type).EnumUnderlyingType;
            }

            switch (type.PrimitiveTypeCode)
            {
                case Microsoft.Cci.PrimitiveTypeCode.Boolean:
                case Microsoft.Cci.PrimitiveTypeCode.Int8:
                case Microsoft.Cci.PrimitiveTypeCode.UInt8:
                    _builder.EmitOpCode(ILOpCode.Stind_i1);
                    break;

                case Microsoft.Cci.PrimitiveTypeCode.Char:
                case Microsoft.Cci.PrimitiveTypeCode.Int16:
                case Microsoft.Cci.PrimitiveTypeCode.UInt16:
                    _builder.EmitOpCode(ILOpCode.Stind_i2);
                    break;

                case Microsoft.Cci.PrimitiveTypeCode.Int32:
                case Microsoft.Cci.PrimitiveTypeCode.UInt32:
                    _builder.EmitOpCode(ILOpCode.Stind_i4);
                    break;

                case Microsoft.Cci.PrimitiveTypeCode.Int64:
                case Microsoft.Cci.PrimitiveTypeCode.UInt64:
                    _builder.EmitOpCode(ILOpCode.Stind_i8);
                    break;

                case Microsoft.Cci.PrimitiveTypeCode.IntPtr:
                case Microsoft.Cci.PrimitiveTypeCode.UIntPtr:
                case Microsoft.Cci.PrimitiveTypeCode.Pointer:
                    _builder.EmitOpCode(ILOpCode.Stind_i);
                    break;

                case Microsoft.Cci.PrimitiveTypeCode.Float32:
                    _builder.EmitOpCode(ILOpCode.Stind_r4);
                    break;

                case Microsoft.Cci.PrimitiveTypeCode.Float64:
                    _builder.EmitOpCode(ILOpCode.Stind_r8);
                    break;

                default:
                    if (type.IsVerifierReference())
                    {
                        _builder.EmitOpCode(ILOpCode.Stind_ref);
                    }
                    else
                    {
                        _builder.EmitOpCode(ILOpCode.Stobj);
                        EmitSymbolToken(type, syntaxNode);
                    }
                    break;
            }
        }

        private void EmitPopIfUnused(bool used)
        {
            if (!used)
            {
                _builder.EmitOpCode(ILOpCode.Pop);
            }
        }

        private void EmitIsExpression(BoundIsOperator isOp, bool used)
        {
            var operand = isOp.Operand;
            EmitExpression(operand, used);
            if (used)
            {
                Debug.Assert((object)operand.Type != null);
                if (!operand.Type.IsVerifierReference())
                {
                    // box the operand for isinst if it is not a verifier reference
                    EmitBox(operand.Type, operand.Syntax);
                }
                _builder.EmitOpCode(ILOpCode.Isinst);
                EmitSymbolToken(isOp.TargetType.Type, isOp.Syntax);
                _builder.EmitOpCode(ILOpCode.Ldnull);
                _builder.EmitOpCode(ILOpCode.Cgt_un);
            }
        }

        private void EmitAsExpression(BoundAsOperator asOp, bool used)
        {
            Debug.Assert(!asOp.Conversion.Kind.IsImplicitConversion());

            var operand = asOp.Operand;
            EmitExpression(operand, used);

            if (used)
            {
                var operandType = operand.Type;
                var targetType = asOp.Type;
                Debug.Assert((object)targetType != null);
                if ((object)operandType != null && !operandType.IsVerifierReference())
                {
                    // box the operand for isinst if it is not a verifier reference
                    EmitBox(operandType, operand.Syntax);
                }
                _builder.EmitOpCode(ILOpCode.Isinst);
                EmitSymbolToken(targetType, asOp.Syntax);
                if (!targetType.IsVerifierReference())
                {
                    // We need to unbox if the target type is not a reference type
                    _builder.EmitOpCode(ILOpCode.Unbox_any);
                    EmitSymbolToken(targetType, asOp.Syntax);
                }
            }
        }

        private void EmitDefaultValue(TypeSymbol type, bool used, CSharpSyntaxNode syntaxNode)
        {
            if (used)
            {
                // default type parameter values must be emitted as 'initobj' regardless of constraints
                if (!type.IsTypeParameter())
                {
                    var constantValue = type.GetDefaultValue();
                    if (constantValue != null)
                    {
                        _builder.EmitConstantValue(constantValue);
                        return;
                    }
                }

                EmitInitObj(type, true, syntaxNode);
            }
        }

        private void EmitDefaultExpression(BoundDefaultOperator expression, bool used)
        {
            Debug.Assert(expression.Type.SpecialType == SpecialType.System_Decimal ||
                expression.Type.GetDefaultValue() == null, "constant should be set on this expression");

            // Default value for the given default expression is not a constant
            // Expression must be of type parameter type or a non-primitive value type
            // Emit an initobj instruction for these cases
            EmitInitObj(expression.Type, used, expression.Syntax);
        }

        private void EmitConstantExpression(TypeSymbol type, ConstantValue constantValue, bool used, CSharpSyntaxNode syntaxNode)
        {
            if (used)  // unused constant has no side-effects
            {
                // Null type parameter values must be emitted as 'initobj' rather than 'ldnull'.
                if (((object)type != null) && (type.TypeKind == TypeKind.TypeParameter) && constantValue.IsNull)
                {
                    EmitInitObj(type, used, syntaxNode);
                }
                else
                {
                    _builder.EmitConstantValue(constantValue);
                }
            }
        }

        private void EmitInitObj(TypeSymbol type, bool used, CSharpSyntaxNode syntaxNode)
        {
            if (used)
            {
                if (type.IsPointerType() || type.SpecialType == SpecialType.System_UIntPtr)
                {
                    // default(whatever*) and default(UIntPtr) can be emitted as:
                    _builder.EmitOpCode(ILOpCode.Ldc_i4_0);
                    _builder.EmitOpCode(ILOpCode.Conv_u);
                }
                else if (type.SpecialType == SpecialType.System_IntPtr)
                {
                    _builder.EmitOpCode(ILOpCode.Ldc_i4_0);
                    _builder.EmitOpCode(ILOpCode.Conv_i);
                }
                else
                {
                    var temp = this.AllocateTemp(type, syntaxNode);
                    _builder.EmitLocalAddress(temp);                  //  ldloca temp
                    _builder.EmitOpCode(ILOpCode.Initobj);            //  intitobj  <MyStruct>
                    EmitSymbolToken(type, syntaxNode);
                    _builder.EmitLocalLoad(temp);                     //  ldloc temp
                    FreeTemp(temp);
                }
            }
        }

        private void EmitTypeOfExpression(BoundTypeOfOperator boundTypeOfOperator)
        {
            TypeSymbol type = boundTypeOfOperator.SourceType.Type;
            _builder.EmitOpCode(ILOpCode.Ldtoken);
            EmitSymbolToken(type, boundTypeOfOperator.SourceType.Syntax);
            _builder.EmitOpCode(ILOpCode.Call, stackAdjustment: 0); //argument off, return value on
            var getTypeMethod = boundTypeOfOperator.GetTypeFromHandle;
            Debug.Assert((object)getTypeMethod != null); // Should have been checked during binding
            EmitSymbolToken(getTypeMethod, boundTypeOfOperator.Syntax, null);
        }

        private void EmitSizeOfExpression(BoundSizeOfOperator boundSizeOfOperator)
        {
            TypeSymbol type = boundSizeOfOperator.SourceType.Type;
            _builder.EmitOpCode(ILOpCode.Sizeof);
            EmitSymbolToken(type, boundSizeOfOperator.SourceType.Syntax);
        }

        private void EmitMethodTokenExpression(BoundMethodToken node)
        {
            _builder.EmitOpCode(ILOpCode.Ldc_i4);
            EmitSymbolToken(node.Method, node.Syntax, null);
        }

        private void EmitMethodInfoExpression(BoundMethodInfo node)
        {
            _builder.EmitOpCode(ILOpCode.Ldtoken);
            EmitSymbolToken(node.Method, node.Syntax, null);

            MethodSymbol getMethod = node.GetMethodFromHandle;
            Debug.Assert((object)getMethod != null);

            if (getMethod.ParameterCount == 1)
            {
                _builder.EmitOpCode(ILOpCode.Call, stackAdjustment: 0); //argument off, return value on
            }
            else
            {
                Debug.Assert(getMethod.ParameterCount == 2);
                _builder.EmitOpCode(ILOpCode.Ldtoken);
                EmitSymbolToken(node.Method.ContainingType, node.Syntax);
                _builder.EmitOpCode(ILOpCode.Call, stackAdjustment: -1); //2 arguments off, return value on
            }

            EmitSymbolToken(getMethod, node.Syntax, null);
            if (node.Type != getMethod.ReturnType)
            {
                _builder.EmitOpCode(ILOpCode.Castclass);
                EmitSymbolToken(node.Type, node.Syntax);
            }
        }

        private void EmitFieldInfoExpression(BoundFieldInfo node)
        {
            _builder.EmitOpCode(ILOpCode.Ldtoken);
            EmitSymbolToken(node.Field, node.Syntax);
            MethodSymbol getField = node.GetFieldFromHandle;
            Debug.Assert((object)getField != null);

            if (getField.ParameterCount == 1)
            {
                _builder.EmitOpCode(ILOpCode.Call, stackAdjustment: 0); //argument off, return value on
            }
            else
            {
                Debug.Assert(getField.ParameterCount == 2);
                _builder.EmitOpCode(ILOpCode.Ldtoken);
                EmitSymbolToken(node.Field.ContainingType, node.Syntax);
                _builder.EmitOpCode(ILOpCode.Call, stackAdjustment: -1); //2 arguments off, return value on
            }

            EmitSymbolToken(getField, node.Syntax, null);
            if (node.Type != getField.ReturnType)
            {
                _builder.EmitOpCode(ILOpCode.Castclass);
                EmitSymbolToken(node.Type, node.Syntax);
            }
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
        private void EmitConditionalOperator(BoundConditionalOperator expr, bool used)
        {
            Debug.Assert(expr.ConstantValue == null, "Constant value should have been emitted directly");

            object consequenceLabel = new object();
            object doneLabel = new object();

            EmitCondBranch(expr.Condition, ref consequenceLabel, sense: true);
            EmitExpression(expr.Alternative, used);

            //
            // III.1.8.1.3 Merging stack states
            // . . . 
            // Let T be the type from the slot on the newly computed state and S
            // be the type from the corresponding slot on the previously stored state. The merged type, U, shall
            // be computed as follows (recall that S := T is the compatibility function defined
            // in §III.1.8.1.2.2):
            // 1. if S := T then U=S
            // 2. Otherwise, if T := S then U=T
            // 3. Otherwise, if S and T are both object types, then let V be the closest common supertype of S and T then U=V.
            // 4. Otherwise, the merge shall fail.
            //
            // When the target merge type is an interface that one or more classes implement, we emit static casts
            // from any class to the target interface.
            // You may think that it's possible to elide one of the static casts and have the CLR recognize
            // that merging a class and interface should succeed if the class implements the interface. Unfortunately,
            // it seems that either PEVerify or the runtime/JIT verifier will complain at you if you try to remove
            // either of the casts.
            //
            var mergeTypeOfAlternative = StackMergeType(expr.Alternative);
            if (used)
            {
                if (IsVarianceCast(expr.Type, mergeTypeOfAlternative))
                {
                    EmitStaticCast(expr.Type, expr.Syntax);
                    mergeTypeOfAlternative = expr.Type;
                }
                else if (expr.Type.IsInterfaceType() && expr.Type != mergeTypeOfAlternative)
                {
                    EmitStaticCast(expr.Type, expr.Syntax);
                }
            }

            _builder.EmitBranch(ILOpCode.Br, doneLabel);
            if (used)
            {
                // If we get to consequenceLabel, we should not have Alternative on stack, adjust for that.
                _builder.AdjustStack(-1);
            }

            _builder.MarkLabel(consequenceLabel);
            EmitExpression(expr.Consequence, used);

            if (used)
            {
                var mergeTypeOfConsequence = StackMergeType(expr.Consequence);
                if (IsVarianceCast(expr.Type, mergeTypeOfConsequence))
                {
                    EmitStaticCast(expr.Type, expr.Syntax);
                    mergeTypeOfConsequence = expr.Type;
                }
                else if (expr.Type.IsInterfaceType() && expr.Type != mergeTypeOfConsequence)
                {
                    EmitStaticCast(expr.Type, expr.Syntax);
                }
            }

            _builder.MarkLabel(doneLabel);
        }

        /// <summary>
        /// Emit code for a null-coalescing operator.
        /// </summary>
        /// <remarks>
        /// x ?? y becomes
        ///   push x
        ///   dup x
        ///   if pop != null goto LEFT_NOT_NULL
        ///     pop 
        ///     push y
        ///   LEFT_NOT_NULL:
        /// </remarks>
        private void EmitNullCoalescingOperator(BoundNullCoalescingOperator expr, bool used)
        {
            Debug.Assert(expr.LeftConversion.IsIdentity, "coalesce with nontrivial left conversions are lowered into ternary.");
            Debug.Assert(expr.Type.IsReferenceType);

            EmitExpression(expr.LeftOperand, used: true);

            // See the notes about verification type merges in EmitConditionalOperator
            var mergeTypeOfLeftValue = StackMergeType(expr.LeftOperand);
            if (used)
            {
                if (IsVarianceCast(expr.Type, mergeTypeOfLeftValue))
                {
                    EmitStaticCast(expr.Type, expr.Syntax);
                    mergeTypeOfLeftValue = expr.Type;
                }
                else if (expr.Type.IsInterfaceType() && expr.Type != mergeTypeOfLeftValue)
                {
                    EmitStaticCast(expr.Type, expr.Syntax);
                }

                _builder.EmitOpCode(ILOpCode.Dup);
            }

            if (expr.Type.IsTypeParameter())
            {
                EmitBox(expr.Type, expr.LeftOperand.Syntax);
            }

            object ifLeftNotNullLabel = new object();
            _builder.EmitBranch(ILOpCode.Brtrue, ifLeftNotNullLabel);

            if (used)
            {
                _builder.EmitOpCode(ILOpCode.Pop);
            }

            EmitExpression(expr.RightOperand, used);
            if (used)
            {
                var mergeTypeOfRightValue = StackMergeType(expr.RightOperand);
                if (IsVarianceCast(expr.Type, mergeTypeOfRightValue))
                {
                    EmitStaticCast(expr.Type, expr.Syntax);
                    mergeTypeOfRightValue = expr.Type;
                }
            }

            _builder.MarkLabel(ifLeftNotNullLabel);
        }

        // Implicit casts are not emitted. As a result verifier may operate on a different 
        // types from the types of operands when performing stack merges in coalesce/ternary.
        // Such differences are in general irrelevant since merging rules work the same way
        // for base and derived types.
        //
        // Situation becomes more complicated with delegates, arrays and interfaces since they 
        // allow implicit casts from types that do not derive from them. In such cases
        // we may need to introduce static casts in the code to prod the verifier to the 
        // right direction
        //
        // This helper returns actual type of array|interface|delegate expression ignoring implicit 
        // casts. This would be the effective stack merge type in the verifier.
        // 
        // NOTE: In cases where stack merge type cannot be determined, we just return null.
        //       We still must assume that it can be an array, delegate or interface though.
        private TypeSymbol StackMergeType(BoundExpression expr)
        {
            // these cases are not interesting. Merge type is the same or derived. No difference.
            if (!(expr.Type.IsArray() || expr.Type.IsInterfaceType() || expr.Type.IsDelegateType()))
            {
                return expr.Type;
            }

            // Dig through casts. We only need to check for expressions that -
            // 1) implicit casts
            // 2) transparently return operands, so we need to dig deeper
            // 3) stack values
            switch (expr.Kind)
            {
                case BoundKind.Conversion:
                    var conversion = (BoundConversion)expr;
                    var conversionKind = conversion.ConversionKind;
                    if (conversionKind.IsImplicitConversion() &&
                        conversionKind != ConversionKind.MethodGroup &&
                        conversionKind != ConversionKind.NullLiteral)
                    {
                        return StackMergeType(conversion.Operand);
                    }
                    break;

                case BoundKind.AssignmentOperator:
                    var assignment = (BoundAssignmentOperator)expr;
                    return StackMergeType(assignment.Right);

                case BoundKind.Sequence:
                    var sequence = (BoundSequence)expr;
                    return StackMergeType(sequence.Value);

                case BoundKind.Local:
                    var local = (BoundLocal)expr;
                    if (this.IsStackLocal(local.LocalSymbol))
                    {
                        // stack value, we cannot be sure what it is
                        return null;
                    }
                    break;

                case BoundKind.Dup:
                    // stack value, we cannot be sure what it is
                    return null;
            }

            return expr.Type;
        }

        // Although III.1.8.1.3 seems to imply that verifier understands variance casts.
        // It appears that verifier/JIT gets easily confused. 
        // So to not rely on whether that should work or not we will flag potentially 
        // "complicated" casts and make them static casts to ensure we are all on 
        // the same page with what type should be tracked.
        private static bool IsVarianceCast(TypeSymbol to, TypeSymbol from)
        {
            if (to == from)
            {
                return false;
            }

            if ((object)from == null)
            {
                // from unknown type - this could be a variance conversion.
                return true;
            }

            // while technically variance casts, array conversions do not seem to be a problem
            // unless the element types are converted via variance.
            if (to.IsArray())
            {
                return IsVarianceCast(((ArrayTypeSymbol)to).ElementType, ((ArrayTypeSymbol)from).ElementType);
            }

            return (to.IsDelegateType() && to != from) ||
                   (to.IsInterfaceType() && from.IsInterfaceType() && !from.InterfacesAndTheirBaseInterfacesNoUseSiteDiagnostics.Contains((NamedTypeSymbol)to));
        }

        private void EmitStaticCast(TypeSymbol to, CSharpSyntaxNode syntax)
        {
            Debug.Assert(to.IsVerifierReference());

            // From ILGENREC::GenQMark
            // See VSWhidbey Bugs #49619 and 108643. If the destination type is an interface we need
            // to force a static cast to be generated for any cast result expressions. The static cast
            // should be done before the unifying jump so the code is verifiable and to allow the JIT to
            // optimize it away. NOTE: Since there is no staticcast instruction, we implement static cast
            // with a stloc / ldloc to a temporary.
            // Bug: VSWhidbey/49619
            // Bug: VSWhidbey/108643
            // Bug: Devdiv/42645

            var temp = AllocateTemp(to, syntax);
            _builder.EmitLocalStore(temp);
            _builder.EmitLocalLoad(temp);
            FreeTemp(temp);
        }

        private void EmitBox(TypeSymbol type, CSharpSyntaxNode syntaxNode)
        {
            _builder.EmitOpCode(ILOpCode.Box);
            EmitSymbolToken(type, syntaxNode);
        }
    }
}
