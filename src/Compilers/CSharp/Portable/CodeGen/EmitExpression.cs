// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Reflection.Metadata;
using System.Runtime.CompilerServices;
using Microsoft.CodeAnalysis.CodeGen;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.CodeAnalysis.PooledObjects;
using Roslyn.Utilities;

using static System.Linq.ImmutableArrayExtensions;

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

            var constantValue = expression.ConstantValueOpt;
            if (constantValue != null)
            {
                if (!used)
                {
                    // unused constants have no side-effects.
                    return;
                }

                if ((object)expression.Type == null ||
                    (expression.Type.SpecialType != SpecialType.System_Decimal &&
                     !expression.Type.IsNullableType()))
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
            catch (InsufficientExecutionStackException)
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

                case BoundKind.ConvertedStackAllocExpression:
                    EmitConvertedStackAllocExpression((BoundConvertedStackAllocExpression)expression, used);
                    break;

                case BoundKind.ReadOnlySpanFromArray:
                    EmitReadOnlySpanFromArrayExpression((BoundReadOnlySpanFromArray)expression, used);
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

                case BoundKind.PassByCopy:
                    EmitExpression(((BoundPassByCopy)expression).Expression, used);
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

                case BoundKind.RefArrayAccess:
                    EmitArrayElementRefLoad((BoundRefArrayAccess)expression, used);
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
                    EmitIsExpression((BoundIsOperator)expression, used, omitBooleanConversion: false);
                    break;

                case BoundKind.AsOperator:
                    EmitAsExpression((BoundAsOperator)expression, used);
                    break;

                case BoundKind.DefaultExpression:
                    EmitDefaultExpression((BoundDefaultExpression)expression, used);
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

                case BoundKind.ModuleVersionId:
                    Debug.Assert(used);
                    EmitModuleVersionIdLoad((BoundModuleVersionId)expression);
                    break;

                case BoundKind.ModuleVersionIdString:
                    Debug.Assert(used);
                    EmitModuleVersionIdStringLoad();
                    break;

                case BoundKind.ThrowIfModuleCancellationRequested:
                    Debug.Assert(!used);
                    EmitThrowIfModuleCancellationRequested(expression.Syntax);
                    break;

                case BoundKind.ModuleCancellationTokenExpression:
                    Debug.Assert(used);
                    EmitModuleCancellationTokenLoad(expression.Syntax);
                    break;

                case BoundKind.InstrumentationPayloadRoot:
                    Debug.Assert(used);
                    EmitInstrumentationPayloadRootLoad((BoundInstrumentationPayloadRoot)expression);
                    break;

                case BoundKind.MethodDefIndex:
                    Debug.Assert(used);
                    EmitMethodDefIndexExpression((BoundMethodDefIndex)expression);
                    break;

                case BoundKind.MaximumMethodDefIndex:
                    Debug.Assert(used);
                    EmitMaximumMethodDefIndexExpression((BoundMaximumMethodDefIndex)expression);
                    break;

                case BoundKind.SourceDocumentIndex:
                    Debug.Assert(used);
                    EmitSourceDocumentIndex((BoundSourceDocumentIndex)expression);
                    break;

                case BoundKind.LocalId:
                    Debug.Assert(used);
                    EmitLocalIdExpression((BoundLocalId)expression);
                    break;

                case BoundKind.ParameterId:
                    Debug.Assert(used);
                    EmitParameterIdExpression((BoundParameterId)expression);
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

                case BoundKind.FunctionPointerInvocation:
                    EmitCalli((BoundFunctionPointerInvocation)expression, used ? UseKind.UsedAsValue : UseKind.Unused);
                    break;

                case BoundKind.FunctionPointerLoad:
                    EmitLoadFunction((BoundFunctionPointerLoad)expression, used);
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
            this.EmitThrow(node.Expression);

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

            if (used)
            {
                _builder.AdjustStack(-1);
            }

            _builder.MarkLabel(whenValueTypeLabel);
            EmitExpression(expression.ValueTypeReceiver, used);

            _builder.MarkLabel(doneLabel);
        }

        private void EmitLoweredConditionalAccessExpression(BoundLoweredConditionalAccess expression, bool used)
        {
            var receiver = expression.Receiver;

            var receiverType = receiver.Type;
            LocalDefinition receiverTemp = null;
            Debug.Assert(!receiverType.IsValueType ||
                (receiverType.IsNullableType() && expression.HasValueMethodOpt != null), "conditional receiver cannot be a struct");

            var receiverConstant = receiver.ConstantValueOpt;
            if (receiverConstant?.IsNull == false)
            {
                // const but not null, must be a reference type
                Debug.Assert(receiverType.IsVerifierReference());
                // receiver is a reference type, so addresskind does not matter, but we do not intend to write.
                receiverTemp = EmitReceiverRef(receiver, AddressKind.ReadOnly);
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

            var notConstrained = !receiverType.IsReferenceType && !receiverType.IsValueType;

            // we need a copy if we deal with nonlocal value (to capture the value)
            // or if we have a ref-constrained T (to do box just once) 
            // or if we deal with stack local (reads are destructive)
            // or if we have default(T) (to do box just once)
            var nullCheckOnCopy = (expression.ForceCopyOfNullableValueType && notConstrained &&
                                   ((TypeParameterSymbol)receiverType).EffectiveInterfacesNoUseSiteDiagnostics.IsEmpty) || // This could be a nullable value type, which must be copied in order to not mutate the original value
                                   LocalRewriter.CanChangeValueBetweenReads(receiver, localsMayBeAssignedOrCaptured: false) ||
                                   (receiverType.IsReferenceType && receiverType.TypeKind == TypeKind.TypeParameter) ||
                                   (receiver.Kind == BoundKind.Local && IsStackLocal(((BoundLocal)receiver).LocalSymbol)) ||
                                   (notConstrained && IsConditionalConstrainedCallThatMustUseTempForReferenceTypeReceiverWalker.Analyze(expression));

            // ===== RECEIVER
            if (nullCheckOnCopy)
            {
                if (notConstrained)
                {
                    // if T happens to be a value type, it could be a target of mutating calls.
                    receiverTemp = EmitReceiverRef(receiver, AddressKind.Constrained);

                    if (receiverTemp is null)
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
                        EmitBox(receiverType, receiver.Syntax);

                        // here we have loaded a ref to a temp and its boxed value { &T, O }
                    }
                    else
                    {
                        // We are calling the expression on a copy of the target anyway, 
                        // so even if T is a struct, we don't need to make sure we call the expression on the original target.

                        // We currently have an address on the stack. Duplicate it, and load the value of the address.
                        _builder.EmitOpCode(ILOpCode.Dup);
                        EmitLoadIndirect(receiverType, receiver.Syntax);
                        EmitBox(receiverType, receiver.Syntax);
                    }
                }
                else
                {
                    // this does not need to be writeable
                    // we may call "HasValue" on this, but it is not mutating 
                    var addressKind = AddressKind.ReadOnly;

                    receiverTemp = EmitReceiverRef(receiver, addressKind);
                    _builder.EmitOpCode(ILOpCode.Dup);
                    // here we have loaded two copies of a reference   { O, O }  or  {&nub, &nub}
                }
            }
            else
            {
                // this does not need to be writeable.
                // we may call "HasValue" on this, but it is not mutating
                // besides, since we are not making a copy, the receiver is not a field, 
                // so it cannot be readonly, in verifier sense, anyways.
                receiverTemp = EmitReceiverRef(receiver, AddressKind.ReadOnly);
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
                // receiver may be used as target of a struct call (if T happens to be a struct)
                receiverTemp = EmitReceiverRef(receiver, AddressKind.Constrained);
                Debug.Assert(receiverTemp == null || receiver.IsDefaultValue());
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

        /// <summary>
        /// We must use a temp when there is a chance that evaluation of the call arguments
        /// could actually modify value of the reference type receiver. The call must use
        /// the original (unmodified) receiver.
        /// </summary>
        private sealed class IsConditionalConstrainedCallThatMustUseTempForReferenceTypeReceiverWalker : BoundTreeWalkerWithStackGuardWithoutRecursionOnTheLeftOfBinaryOperator
        {
            private readonly BoundLoweredConditionalAccess _conditionalAccess;
            private bool? _result;

            private IsConditionalConstrainedCallThatMustUseTempForReferenceTypeReceiverWalker(BoundLoweredConditionalAccess conditionalAccess)
            {
                _conditionalAccess = conditionalAccess;
            }

            public static bool Analyze(BoundLoweredConditionalAccess conditionalAccess)
            {
                var walker = new IsConditionalConstrainedCallThatMustUseTempForReferenceTypeReceiverWalker(conditionalAccess);
                walker.Visit(conditionalAccess.WhenNotNull);
                Debug.Assert(walker._result.HasValue);
                return walker._result.GetValueOrDefault();
            }

            public override BoundNode Visit(BoundNode node)
            {
                if (_result.HasValue)
                {
                    return null;
                }

                return base.Visit(node);
            }

            protected override void VisitReceiver(BoundCall node)
            {
                if (node.ReceiverOpt is BoundConditionalReceiver { Id: var id } && id == _conditionalAccess.Id)
                {
                    Debug.Assert(!_result.HasValue);
                    _result = !IsSafeToDereferenceReceiverRefAfterEvaluatingArguments(node.Arguments);
                }
            }

            public override BoundNode VisitConditionalReceiver(BoundConditionalReceiver node)
            {
                if (node.Id == _conditionalAccess.Id)
                {
                    Debug.Assert(!_result.HasValue);
                    _result = false;
                    return null;
                }

                return base.VisitConditionalReceiver(node);
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
            switch (refKind)
            {
                case RefKind.None:
                    EmitExpression(argument, true);
                    break;

                default:
                    Debug.Assert(refKind is RefKind.In or RefKind.Ref or RefKind.Out or RefKindExtensions.StrictIn);
                    var temp = EmitAddress(argument, GetArgumentAddressKind(refKind));
                    if (temp != null)
                    {
                        // interestingly enough "ref dynamic" sometimes is passed via a clone
                        // receiver of a ref field can be cloned too
                        Debug.Assert(refKind is RefKind.In || argument.Type.IsDynamic() || argument is BoundFieldAccess { FieldSymbol.RefKind: not RefKind.None }, "passing args byref should not clone them into temps");
                        AddExpressionTemp(temp);
                    }

                    break;
            }
        }

        internal static AddressKind GetArgumentAddressKind(RefKind refKind)
        {
            switch (refKind)
            {
                case RefKind.None:
                    throw ExceptionUtilities.UnexpectedValue(refKind);

                case RefKind.In:
                    return AddressKind.ReadOnly;

                default:
                    Debug.Assert(refKind is RefKind.Ref or RefKind.Out or RefKindExtensions.StrictIn);
                    // NOTE: returning "ReadOnlyStrict" here. 
                    //       we should not get an address of a copy if at all possible
                    return refKind == RefKindExtensions.StrictIn ? AddressKind.ReadOnlyStrict : AddressKind.Writeable;
            }
        }

        private void EmitAddressOfExpression(BoundAddressOfOperator expression, bool used)
        {
            // NOTE: passing "ReadOnlyStrict" here. 
            //       we should not get an address of a copy if at all possible
            var temp = EmitAddress(expression.Operand, AddressKind.ReadOnlyStrict);
            Debug.Assert(temp == null, "If the operand is addressable, then a temp shouldn't be required.");

            if (used && !expression.IsManaged)
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
            if (!expression.RefersToLocation)
            {
                EmitLoadIndirect(expression.Type, expression.Syntax);
            }

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
            EmitExpression(expression.EmitExpressions.GetValue(expression, _diagnostics.DiagnosticBag), used);
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
            FreeLocals(sequence);
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

        private void FreeLocals(BoundSequence sequence)
        {
            if (sequence.Locals.IsEmpty)
            {
                return;
            }

            _builder.CloseLocalScope();

            foreach (var local in sequence.Locals)
            {
                FreeLocal(local);
            }
        }

        /// <summary>
        /// Defines sequence locals and record them so that they could be retained for the duration of the encompassing expression
        /// Use this when taking a reference of the sequence, which can indirectly refer to any of its locals.
        /// </summary>
        private void DefineAndRecordLocals(BoundSequence sequence)
        {
            if (sequence.Locals.IsEmpty)
            {
                return;
            }

            _builder.OpenLocalScope();

            foreach (var local in sequence.Locals)
            {
                var seqLocal = DefineLocal(local, sequence.Syntax);
                AddExpressionTemp(seqLocal);
            }
        }

        /// <summary>
        /// Closes the visibility/debug scopes for the sequence locals, but keep the local slots from reuse
        /// for the duration of the encompassing expression.
        /// Use this paired with DefineAndRecordLocals when taking a reference of the sequence, which can indirectly refer to any of its locals.
        /// </summary>
        private void CloseScopeAndKeepLocals(BoundSequence sequence)
        {
            if (sequence.Locals.IsEmpty)
            {
                return;
            }

            _builder.CloseLocalScope();
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

        private void EmitArguments(ImmutableArray<BoundExpression> arguments, ImmutableArray<ParameterSymbol> parameters, ImmutableArray<RefKind> argRefKindsOpt)
        {
            // We might have an extra argument for the __arglist() of a varargs method.
            Debug.Assert(arguments.Length == parameters.Length ||
                (arguments.Length == parameters.Length + 1 && arguments is [.., BoundArgListOperator]), "argument count must match parameter count");
            Debug.Assert(parameters.All(p => p.RefKind == RefKind.None) || !argRefKindsOpt.IsDefault, "there are nontrivial parameters, so we must have argRefKinds");
            // We might have a missing ref kind for the __arglist() of a varargs method.
            Debug.Assert(argRefKindsOpt.IsDefault || argRefKindsOpt.Length == arguments.Length ||
                (argRefKindsOpt.Length == arguments.Length - 1 && arguments is [.., BoundArgListOperator]), "if we have argRefKinds, we should have one for each argument");

            for (int i = 0; i < arguments.Length; i++)
            {
                RefKind argRefKind = GetArgumentRefKind(arguments, parameters, argRefKindsOpt, i);
                EmitArgument(arguments[i], argRefKind);
            }
        }

        /// <summary>
        /// Computes the desired refkind of the argument.
        /// Considers all the cases - where ref kinds are explicit, omitted, vararg cases.
        /// </summary>
        internal static RefKind GetArgumentRefKind(ImmutableArray<BoundExpression> arguments, ImmutableArray<ParameterSymbol> parameters, ImmutableArray<RefKind> argRefKindsOpt, int i)
        {
            RefKind argRefKind;
            if (i < parameters.Length)
            {
                if (!argRefKindsOpt.IsDefault && i < argRefKindsOpt.Length)
                {
                    // if we have an explicit refKind for the given argument, use that
                    argRefKind = argRefKindsOpt[i];

                    Debug.Assert(argRefKind == parameters[i].RefKind ||
                            parameters[i].RefKind switch
                            {
                                RefKind.In => argRefKind == RefKindExtensions.StrictIn,
                                RefKind.RefReadOnlyParameter => argRefKind is RefKind.In or RefKindExtensions.StrictIn,
                                _ => false,
                            },
                            "in Emit the argument RefKind must be compatible with the corresponding parameter");
                }
                else
                {
                    Debug.Assert(parameters[i].RefKind != RefKind.RefReadOnlyParameter,
                        "LocalRewriter.GetEffectiveArgumentRefKinds should ensure 'ref readonly' parameters get an entry in 'argRefKindsOpt'.");

                    // otherwise fallback to the refKind of the parameter
                    argRefKind = parameters[i].RefKind switch
                    {
                        RefKind.RefReadOnlyParameter => RefKind.In, // should not happen, asserted above
                        var refKind => refKind
                    };
                }
            }
            else
            {
                // vararg case
                Debug.Assert(arguments[i].Kind == BoundKind.ArgListOperator);
                argRefKind = RefKind.None;
            }

            return argRefKind;
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
                    case Microsoft.Cci.PrimitiveTypeCode.FunctionPointer:
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
                _builder.EmitArrayElementLoad(_module.Translate((ArrayTypeSymbol)arrayAccess.Expression.Type), arrayAccess.Expression.Syntax);
            }

            EmitPopIfUnused(used);
        }

        private void EmitArrayElementRefLoad(BoundRefArrayAccess refArrayAccess, bool used)
        {
            if (used)
            {
                throw ExceptionUtilities.Unreachable();
            }

            EmitArrayElementAddress(refArrayAccess.ArrayAccess, AddressKind.Writeable);
            _builder.EmitOpCode(ILOpCode.Pop);
        }

        private void EmitFieldLoad(BoundFieldAccess fieldAccess, bool used)
        {
            var field = fieldAccess.FieldSymbol;

            if (!used)
            {
                // fetching unused captured frame is a no-op (like reading "this")
                if (field.IsCapturedFrame)
                {
                    return;
                }

                // Accessing a volatile field is sideeffecting because it establishes an acquire fence.
                // Otherwise, accessing an unused instance field on a struct is a noop. Just emit an unused receiver.
                if (!field.IsVolatile && !field.IsStatic && fieldAccess.ReceiverOpt.Type.IsVerifierValue() && field.RefKind == RefKind.None)
                {
                    EmitExpression(fieldAccess.ReceiverOpt, used: false);
                    return;
                }
            }

            Debug.Assert(!field.IsConst || field.ContainingType.SpecialType == SpecialType.System_Decimal,
                "rewriter should lower constant fields into constant expressions");

            EmitFieldLoadNoIndirection(fieldAccess, used);

            if (field.RefKind != RefKind.None)
            {
                EmitLoadIndirect(field.Type, fieldAccess.Syntax);
            }

            EmitPopIfUnused(used);
        }

        private void EmitFieldLoadNoIndirection(BoundFieldAccess fieldAccess, bool used)
        {
            var field = fieldAccess.FieldSymbol;

            // static field access is sideeffecting since it guarantees that ..ctor has run.
            // we emit static accesses even if unused.
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
                TypeSymbol fieldType = field.Type;
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
        }

        private LocalDefinition EmitFieldLoadReceiver(BoundExpression receiver)
        {
            // ldfld can work with structs directly or with their addresses
            // accessing via address is typically same or cheaper, but not for homeless values, obviously
            // there are also cases where we must emit receiver as a reference
            if (FieldLoadMustUseRef(receiver) || FieldLoadPrefersRef(receiver))
            {
                return EmitFieldLoadReceiverAddress(receiver) ? null : EmitReceiverRef(receiver, AddressKind.ReadOnly);
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

        // ldfld can work with structs directly or with their addresses.
        // In some cases it results in same native code emitted, but in some cases JIT pushes values for real
        // resulting in much worse code (on x64 in particular).
        // So, we will always prefer references here except when receiver is a struct, non-ref local, or parameter. 
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
            if (!HasHome(receiver, AddressKind.ReadOnly))
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
                    var field = fieldAccess.FieldSymbol;

                    if (field.IsStatic || field.RefKind != RefKind.None)
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
            bool isRefLocal = local.LocalSymbol.RefKind != RefKind.None;
            if (IsStackLocal(local.LocalSymbol))
            {
                // local must be already on the stack
                EmitPopIfUnused(used || isRefLocal);
            }
            else
            {
                if (used || isRefLocal)
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

            if (isRefLocal)
            {
                EmitLoadIndirect(local.LocalSymbol.Type, local.Syntax);
                EmitPopIfUnused(used);
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

        private void EmitLoadIndirect(TypeSymbol type, SyntaxNode syntaxNode)
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
                case Microsoft.Cci.PrimitiveTypeCode.FunctionPointer:
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

            var constVal = receiver.ConstantValueOpt;
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
                    // NOTE: there are cases involving ProxyAttribute
                    // where newobj may produce null
                    return true;

                case BoundKind.Conversion:
                    var conversion = (BoundConversion)receiver;

                    switch (conversion.ConversionKind)
                    {
                        case ConversionKind.Boxing:
                            // NOTE: boxing can produce null for Nullable, but any call through that
                            // will result in null reference exceptions anyways.
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
                    // NOTE: these actually can be null if called from a different language
                    // however, we assume it is responsibility of the caller to nullcheck "this"
                    // if we already have access to "this", we must be in a member and should 
                    // not redo the check
                    return true;

                case BoundKind.FieldAccess:
                    // same reason as for "ThisReference"
                    return ((BoundFieldAccess)receiver).FieldSymbol.IsCapturedFrame;

                case BoundKind.Local:
                    // same reason as for "ThisReference"
                    return ((BoundLocal)receiver).LocalSymbol.SynthesizedKind == SynthesizedLocalKind.FrameCache;

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
            if (call.Method.IsDefaultValueTypeConstructor())
            {
                EmitDefaultValueTypeConstructorCallExpression(call);
            }
            else if (!call.Method.RequiresInstanceReceiver)
            {
                EmitStaticCallExpression(call, useKind);
            }
            else
            {
                EmitInstanceCallExpression(call, useKind);
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private void EmitDefaultValueTypeConstructorCallExpression(BoundCall call)
        {
            var method = call.Method;
            var receiver = call.ReceiverOpt;

            // Calls to the default struct constructor are emitted as initobj, rather than call.
            // NOTE: constructor invocations are represented as BoundObjectCreationExpressions,
            // rather than BoundCalls.  This is why we can be confident that if we see a call to a
            // constructor, it has this very specific form.
            Debug.Assert(method.IsImplicitlyDeclared);
            Debug.Assert(TypeSymbol.Equals(method.ContainingType, receiver.Type, TypeCompareKind.ConsiderEverything2));
            Debug.Assert(receiver.Kind == BoundKind.ThisReference);

            LocalDefinition tempOpt = EmitReceiverRef(receiver, AddressKind.Writeable);
            _builder.EmitOpCode(ILOpCode.Initobj);    //  initobj  <MyStruct>
            EmitSymbolToken(method.ContainingType, call.Syntax);
            FreeOptTemp(tempOpt);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private void EmitStaticCallExpression(BoundCall call, UseKind useKind)
        {
            var method = call.Method;
            var receiver = call.ReceiverOpt;
            var arguments = call.Arguments;

            Debug.Assert(method.IsStatic);

            var countBefore = _builder.LocalSlotManager.StartScopeOfTrackingAddressedLocals();

            EmitArguments(arguments, method.Parameters, call.ArgumentRefKindsOpt);

            _builder.LocalSlotManager.EndScopeOfTrackingAddressedLocals(countBefore,
                MightEscapeTemporaryRefs(call, used: useKind != UseKind.Unused));

            int stackBehavior = GetCallStackBehavior(method, arguments);

            if (method.IsAbstract || method.IsVirtual)
            {
                if (receiver is not BoundTypeExpression { Type: { TypeKind: TypeKind.TypeParameter } })
                {
                    throw ExceptionUtilities.Unreachable();
                }

                _builder.EmitOpCode(ILOpCode.Constrained);
                EmitSymbolToken(receiver.Type, receiver.Syntax);
            }

            _builder.EmitOpCode(ILOpCode.Call, stackBehavior);

            EmitSymbolToken(method, call.Syntax,
                            method.IsVararg ? (BoundArgListOperator)arguments[arguments.Length - 1] : null);

            EmitCallCleanup(call.Syntax, useKind, method);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private void EmitInstanceCallExpression(BoundCall call, UseKind useKind)
        {
            CallKind callKind;
            AddressKind? addressKind;
            bool box;
            LocalDefinition tempOpt;

            var countBefore = _builder.LocalSlotManager.StartScopeOfTrackingAddressedLocals();

            if (receiverIsInstanceCall(call, out BoundCall nested))
            {
                var calls = ArrayBuilder<BoundCall>.GetInstance();

                calls.Push(call);

                call = nested;
                while (receiverIsInstanceCall(call, out nested))
                {
                    calls.Push(call);
                    call = nested;
                }

                callKind = determineEmitReceiverStrategy(call, out addressKind, out box);
                emitReceiver(call, callKind, addressKind, box, out tempOpt);

                while (calls.Count != 0)
                {
                    var parentCall = calls.Pop();
                    CallKind parentCallKind = determineEmitReceiverStrategy(parentCall, out addressKind, out box);

                    var parentCallReceiverType = call.Type;
                    UseKind receiverUseKind;
                    if (addressKind is null)
                    {
                        receiverUseKind = UseKind.UsedAsValue;
                    }
                    else if (BoxNonVerifierReferenceReceiver(parentCallReceiverType, addressKind.GetValueOrDefault()))
                    {
                        Debug.Assert(!box);
                        // This code path is covered by IL comparison in Microsoft.CodeAnalysis.CSharp.UnitTests.BreakingChanges.NestedCollectionInitializerOnGenericProperty​ unit-test

                        // EmitReceiverRef pushes boxed value rather than an address in this case 
                        receiverUseKind = UseKind.UsedAsValue;
                        box = true;

                        // not subject to emitGenericReceiverCloneIfNecessary effect
                        Debug.Assert(addressKind.GetValueOrDefault() != AddressKind.Constrained);
                        Debug.Assert(!parentCallReceiverType.IsVerifierValue());
                        Debug.Assert(parentCallKind != CallKind.ConstrainedCallVirt);
                    }
                    else
                    {
                        Debug.Assert(!box);
                        Debug.Assert(!parentCallReceiverType.IsVerifierReference());

                        var methodRefKind = call.Method.RefKind;
                        if (UseCallResultAsAddress(call, addressKind.GetValueOrDefault()))
                        {
                            // This code path is covered by IL comparison in
                            // - Microsoft.CodeAnalysis.CSharp.UnitTests.RefReturnTests.RefReturnConditionalAccess01​, and
                            // - Microsoft.CodeAnalysis.CSharp.UnitTests.CodeGenRefReadOnlyReturnTests.RefReadOnlyMethod_PassThrough_ChainNoCopying
                            // unit tests
                            receiverUseKind = UseKind.UsedAsAddress;
                        }
                        else
                        {
                            // This code path is covered by IL comparison in Microsoft.CodeAnalysis.CSharp.UnitTests.CodeGen.CodeGenShortCircuitOperatorTests.TestConditionalMemberAccessUnused2a unit-test

                            // EmitAddress pushes a reference to a temp with a value in this case
                            receiverUseKind = UseKind.UsedAsValue;
                        }
                    }

                    emitArgumentsAndCallEpilogue(call, callKind, receiverUseKind);

                    _builder.LocalSlotManager.EndScopeOfTrackingAddressedLocals(countBefore, MightEscapeTemporaryRefs(call, used: true));

                    countBefore = _builder.LocalSlotManager.StartScopeOfTrackingAddressedLocals();

                    FreeOptTemp(tempOpt);
                    tempOpt = null;

                    nested = call;
                    call = parentCall;
                    callKind = parentCallKind;

                    if (box)
                    {
                        Debug.Assert(receiverUseKind == UseKind.UsedAsValue);

                        // This code path is covered by IL comparison in
                        // - Microsoft.CodeAnalysis.CSharp.UnitTests.CodeGen.CodeGenTests.BoxingReceiver, and
                        // - Microsoft.CodeAnalysis.CSharp.UnitTests.BreakingChanges.NestedCollectionInitializerOnGenericProperty​
                        // unit-tests
                        EmitBox(parentCallReceiverType, nested.Syntax);
                    }
                    else if (addressKind is null)
                    {
                        Debug.Assert(receiverUseKind == UseKind.UsedAsValue);
                        // This code path is covered by IL comparison in Microsoft.CodeAnalysis.CSharp.UnitTests.CodeGen.CodeGenShortCircuitOperatorTests.TestConditionalMemberAccessUnused2a unit-test
                    }
                    else
                    {
                        Debug.Assert(!parentCallReceiverType.IsVerifierReference());

                        if (receiverUseKind != UseKind.UsedAsAddress)
                        {
                            Debug.Assert(receiverUseKind == UseKind.UsedAsValue);
                            Debug.Assert(!HasHome(nested, addressKind.GetValueOrDefault()));

                            // This code path is covered by IL comparison in Microsoft.CodeAnalysis.CSharp.UnitTests.CodeGen.CodeGenShortCircuitOperatorTests.TestConditionalMemberAccessUnused2a unit-test

                            // EmitAddress pushes a reference to a temp with a value in this case
                            tempOpt = this.AllocateTemp(parentCallReceiverType, nested.Syntax);
                            _builder.EmitLocalStore(tempOpt);
                            _builder.EmitLocalAddress(tempOpt);
                        }
                        else
                        {
                            // This code path is covered at least by IL comparison in
                            // - Microsoft.CodeAnalysis.CSharp.UnitTests.RefReturnTests.RefReturnConditionalAccess01​, and
                            // - Microsoft.CodeAnalysis.CSharp.UnitTests.CodeGenRefReadOnlyReturnTests.RefReadOnlyMethod_PassThrough_ChainNoCopying
                            // unit tests
                        }

                        // Effect of this call is covered by IL comparison in Microsoft.CodeAnalysis.CSharp.UnitTests.CodeGen.CodeGenCallTests.ChainedCalls unit-test
                        emitGenericReceiverCloneIfNecessary(call, callKind, ref tempOpt);
                    }
                }

                calls.Free();
            }
            else
            {
                callKind = determineEmitReceiverStrategy(call, out addressKind, out box);
                emitReceiver(call, callKind, addressKind, box, out tempOpt);
            }

            emitArgumentsAndCallEpilogue(call, callKind, useKind);

            _builder.LocalSlotManager.EndScopeOfTrackingAddressedLocals(countBefore, MightEscapeTemporaryRefs(call, used: useKind != UseKind.Unused));

            FreeOptTemp(tempOpt);

            return;

            [MethodImpl(MethodImplOptions.NoInlining)]
            CallKind determineEmitReceiverStrategy(BoundCall call, out AddressKind? addressKind, out bool box)
            {
                var method = call.Method;
                var receiver = call.ReceiverOpt;
                Debug.Assert(!method.IsStatic && !method.IsDefaultValueTypeConstructor() && method.RequiresInstanceReceiver);

                CallKind callKind;

                var receiverType = receiver.Type;
                box = false;

                if (receiverType.IsVerifierReference())
                {
                    addressKind = null;

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
                    if (methodContainingType.IsVerifierValue())
                    {
                        // if method is defined in the struct itself it is assumed to be mutating, unless 
                        // it is a member of a readonly struct and is not a constructor
                        addressKind = IsReadOnlyCall(method, methodContainingType) ?
                                                                        AddressKind.ReadOnly :
                                                                        AddressKind.Writeable;
                        if (MayUseCallForStructMethod(method))
                        {
                            // NOTE: this should be either a method which overrides some abstract method or 
                            //       does not override anything (with few exceptions, see MayUseCallForStructMethod); 
                            //       otherwise we should not use direct 'call' and must use constrained call;

                            // calling a method defined in a value type
                            Debug.Assert(TypeSymbol.Equals(receiverType, methodContainingType, TypeCompareKind.ObliviousNullableModifierMatchesAny));
                            callKind = CallKind.Call;
                        }
                        else
                        {
                            callKind = CallKind.ConstrainedCallVirt;
                        }
                    }
                    else
                    {
                        // calling a method defined in a base class or interface.

                        // When calling a method that is virtual in metadata on a struct receiver, 
                        // we use a constrained virtual call. If possible, it will skip boxing.
                        if (method.IsMetadataVirtual())
                        {
                            addressKind = AddressKind.Writeable;
                            callKind = CallKind.ConstrainedCallVirt;
                        }
                        else
                        {
                            addressKind = null;
                            box = true;
                            callKind = CallKind.Call;
                        }
                    }
                }
                else
                {
                    // receiver is generic and method must come from the base or an interface or a generic constraint
                    // if the receiver is actually a value type it would need to be boxed.
                    // let .constrained sort this out. 
                    callKind = receiverType.IsReferenceType &&
                               (!IsRef(receiver) ||
                                (!ReceiverIsKnownToReferToTempIfReferenceType(receiver) && !IsSafeToDereferenceReceiverRefAfterEvaluatingArguments(call.Arguments))) ?
                                CallKind.CallVirt :
                                CallKind.ConstrainedCallVirt;

                    addressKind = (callKind == CallKind.ConstrainedCallVirt) ? AddressKind.Constrained : AddressKind.Writeable;
                }

                Debug.Assert((callKind != CallKind.ConstrainedCallVirt) || (addressKind.GetValueOrDefault() == AddressKind.Constrained) || receiverType.IsVerifierValue());

                return callKind;
            }

            [MethodImpl(MethodImplOptions.NoInlining)]
            void emitReceiver(BoundCall call, CallKind callKind, AddressKind? addressKind, bool box, out LocalDefinition tempOpt)
            {
                var receiver = call.ReceiverOpt;
                var receiverType = receiver.Type;
                tempOpt = null;

                if (addressKind is null)
                {
                    EmitExpression(receiver, used: true);

                    if (box)
                    {
                        EmitBox(receiverType, receiver.Syntax);
                    }
                }
                else
                {
                    Debug.Assert(!box);
                    Debug.Assert(!receiverType.IsVerifierReference());
                    tempOpt = EmitReceiverRef(receiver, addressKind.GetValueOrDefault());

                    emitGenericReceiverCloneIfNecessary(call, callKind, ref tempOpt);
                }
            }

            [MethodImpl(MethodImplOptions.NoInlining)]
            void emitArgumentsAndCallEpilogue(BoundCall call, CallKind callKind, UseKind useKind)
            {
                var method = call.Method;
                var receiver = call.ReceiverOpt;

                // When emitting a callvirt to a virtual method we always emit the method info of the
                // method that first declared the virtual method, not the method info of an
                // overriding method. It would be a subtle breaking change to change that rule;
                // see bug 6156 for details.

                MethodSymbol actualMethodTargetedByTheCall = method;
                if (method.IsOverride && callKind != CallKind.Call)
                {
                    actualMethodTargetedByTheCall = method.GetConstructedLeastOverriddenMethod(_method.ContainingType, requireSameReturnType: true);
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

                var arguments = call.Arguments;
                EmitArguments(arguments, method.Parameters, call.ArgumentRefKindsOpt);
                int stackBehavior = GetCallStackBehavior(method, arguments);
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
                                actualMethodTargetedByTheCall.IsVararg ? (BoundArgListOperator)arguments[arguments.Length - 1] : null);

                EmitCallCleanup(call.Syntax, useKind, method);
            }

            [MethodImpl(MethodImplOptions.NoInlining)]
            void emitGenericReceiverCloneIfNecessary(BoundCall call, CallKind callKind, ref LocalDefinition tempOpt)
            {
                var receiver = call.ReceiverOpt;
                var receiverType = receiver.Type;

                if (callKind == CallKind.ConstrainedCallVirt && tempOpt is null && !receiverType.IsValueType &&
                    !ReceiverIsKnownToReferToTempIfReferenceType(receiver) &&
                    !IsSafeToDereferenceReceiverRefAfterEvaluatingArguments(call.Arguments))
                {
                    // A case where T is actually a class must be handled specially.
                    // Taking a reference to a class instance is fragile because the value behind the 
                    // reference might change while arguments are evaluated. However, the call should be
                    // performed on the instance that is behind reference at the time we push the
                    // reference to the stack. So, for a class we need to emit a reference to a temporary
                    // location, rather than to the original location

                    // Struct values are never nulls.
                    // We will emit a check for such case, but the check is really a JIT-time 
                    // constant since JIT will know if T is a struct or not.

                    // if ((object)default(T) == null) 
                    // {
                    //     temp = receiverRef
                    //     receiverRef = ref temp
                    // }

                    object whenNotNullLabel = null;

                    if (!receiverType.IsReferenceType)
                    {
                        // if ((object)default(T) == null) 
                        EmitDefaultValue(receiverType, true, receiver.Syntax);
                        EmitBox(receiverType, receiver.Syntax);
                        whenNotNullLabel = new object();
                        _builder.EmitBranch(ILOpCode.Brtrue, whenNotNullLabel);
                    }

                    //     temp = receiverRef
                    //     receiverRef = ref temp
                    EmitLoadIndirect(receiverType, receiver.Syntax);
                    tempOpt = AllocateTemp(receiverType, receiver.Syntax);
                    _builder.EmitLocalStore(tempOpt);
                    _builder.EmitLocalAddress(tempOpt);

                    if (whenNotNullLabel is not null)
                    {
                        _builder.MarkLabel(whenNotNullLabel);
                    }
                }
            }

            static bool receiverIsInstanceCall(BoundCall call, out BoundCall nested)
            {
                if (call.ReceiverOpt is BoundCall { Method: { RequiresInstanceReceiver: true } method } receiver && !method.IsDefaultValueTypeConstructor())
                {
                    nested = receiver;
                    return true;
                }

                nested = null;
                return false;
            }
        }

        internal static bool IsPossibleReferenceTypeReceiverOfConstrainedCall(BoundExpression receiver)
        {
            var receiverType = receiver.Type;

            if (receiverType.IsVerifierReference() || receiverType.IsVerifierValue())
            {
                return false;
            }

            return !receiverType.IsValueType;
        }

        internal static bool ReceiverIsKnownToReferToTempIfReferenceType(BoundExpression receiver)
        {
            while (receiver is BoundSequence sequence)
            {
                receiver = sequence.Value;
            }

            if (receiver is
                    BoundLocal { LocalSymbol.IsKnownToReferToTempIfReferenceType: true } or
                    BoundComplexConditionalReceiver or
                    BoundConditionalReceiver { Type: { IsReferenceType: false, IsValueType: false } })
            {
                return true;
            }

            return false;
        }

        internal static bool IsSafeToDereferenceReceiverRefAfterEvaluatingArguments(ImmutableArray<BoundExpression> arguments)
        {
            return arguments.All(isSafeToDereferenceReceiverRefAfterEvaluatingArgument);

            static bool isSafeToDereferenceReceiverRefAfterEvaluatingArgument(BoundExpression expression)
            {
                var current = expression;
                while (true)
                {
                    if (current.ConstantValueOpt != null)
                    {
                        return true;
                    }

                    switch (current.Kind)
                    {
                        default:
                            return false;
                        case BoundKind.TypeExpression:
                        case BoundKind.Parameter:
                        case BoundKind.Local:
                        case BoundKind.ThisReference:
                            return true;
                        case BoundKind.FieldAccess:
                            {
                                var field = (BoundFieldAccess)current;
                                current = field.ReceiverOpt;
                                if (current is null)
                                {
                                    return true;
                                }

                                break;
                            }
                        case BoundKind.PassByCopy:
                            current = ((BoundPassByCopy)current).Expression;
                            break;
                        case BoundKind.BinaryOperator:
                            {
                                BoundBinaryOperator b = (BoundBinaryOperator)current;
                                Debug.Assert(!b.OperatorKind.IsUserDefined());

                                if (b.OperatorKind.IsUserDefined() || !isSafeToDereferenceReceiverRefAfterEvaluatingArgument(b.Right))
                                {
                                    return false;
                                }

                                current = b.Left;
                                break;
                            }
                        case BoundKind.Conversion:
                            {
                                BoundConversion conv = (BoundConversion)current;
                                Debug.Assert(!conv.ConversionKind.IsUserDefinedConversion());
                                Debug.Assert(!conv.ConversionKind.IsUnionConversion());

                                if (conv.ConversionKind.IsUserDefinedConversion() || conv.ConversionKind.IsUnionConversion())
                                {
                                    return false;
                                }

                                current = conv.Operand;
                                break;
                            }
                    }
                }
            }
        }

        private bool IsReadOnlyCall(MethodSymbol method, NamedTypeSymbol methodContainingType)
        {
            Debug.Assert(methodContainingType.IsVerifierValue(), "only struct calls can be readonly");

            if (method.IsEffectivelyReadOnly && method.MethodKind != MethodKind.Constructor)
            {
                return true;
            }

            if (methodContainingType.IsNullableType())
            {
                var originalMethod = method.OriginalDefinition;

                if ((object)originalMethod == this._module.Compilation.GetSpecialTypeMember(SpecialMember.System_Nullable_T_GetValueOrDefault) ||
                    (object)originalMethod == this._module.Compilation.GetSpecialTypeMember(SpecialMember.System_Nullable_T_get_Value) ||
                    (object)originalMethod == this._module.Compilation.GetSpecialTypeMember(SpecialMember.System_Nullable_T_get_HasValue))
                {
                    return true;
                }
            }

            return false;
        }

        // returns true when receiver is already a ref.
        // in such cases calling through a ref could be preferred over 
        // calling through indirectly loaded value.
        internal static bool IsRef(BoundExpression receiver)
        {
            switch (receiver.Kind)
            {
                case BoundKind.Local:
                    return ((BoundLocal)receiver).LocalSymbol.RefKind != RefKind.None;

                case BoundKind.Parameter:
                    return ((BoundParameter)receiver).ParameterSymbol.RefKind != RefKind.None;

                case BoundKind.Call:
                    return ((BoundCall)receiver).Method.RefKind != RefKind.None;

                case BoundKind.FunctionPointerInvocation:
                    return ((BoundFunctionPointerInvocation)receiver).FunctionPointer.Signature.RefKind != RefKind.None;

                case BoundKind.Dup:
                    return ((BoundDup)receiver).RefKind != RefKind.None;

                case BoundKind.Sequence:
                    return IsRef(((BoundSequence)receiver).Value);
            }

            return false;
        }

        private static int GetCallStackBehavior(MethodSymbol method, ImmutableArray<BoundExpression> arguments)
        {
            int stack = 0;

            if (!method.ReturnsVoid)
            {
                // The call puts the return value on the stack.
                stack += 1;
            }

            if (method.RequiresInstanceReceiver)
            {
                // The call pops the receiver off the stack.
                stack -= 1;
            }

            if (method.IsVararg)
            {
                // The call pops all the arguments, fixed and variadic.
                int fixedArgCount = arguments.Length - 1;
                int varArgCount = ((BoundArgListOperator)arguments[fixedArgCount]).Arguments.Length;
                stack -= fixedArgCount;
                stack -= varArgCount;
            }
            else
            {
                // The call pops all the arguments.
                stack -= arguments.Length;
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

            if (!method.IsMetadataVirtual() || method.IsStatic)
            {
                return true;
            }

            var overriddenMethod = method.OverriddenMethod;
            if ((object)overriddenMethod == null || overriddenMethod.IsAbstract)
            {
                return true;
            }

            var containingType = method.ContainingType;
            // Overrides in structs of some special types can be called directly.
            // We can assume that these special types will not be removing overrides.
            // This pattern can probably be applied to all special types,
            // but that would introduce a silent change every time a new special type is added,
            // so we constrain the check to a fixed range of types
            return containingType.SpecialType.CanOptimizeBehavior();
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
                _builder.EmitArrayCreation(_module.Translate(arrayType), expression.Syntax);
            }

            if (expression.InitializerOpt != null)
            {
                EmitArrayInitializers(arrayType, expression.InitializerOpt);
            }

            // newarr has side-effects (negative bounds etc) so always emitted.
            EmitPopIfUnused(used);
        }

        private void EmitConvertedStackAllocExpression(BoundConvertedStackAllocExpression expression, bool used)
        {
            var initializer = expression.InitializerOpt;
            if (used)
            {
                EmitStackAlloc(expression.Type, initializer, expression.Count);
            }
            else
            {
                // the only sideeffect of a localloc is a nondeterministic and generally fatal StackOverflow.
                // we can ignore that if the actual result is unused
                EmitExpression(expression.Count, used: false);

                if (initializer != null)
                {
                    // If not used, just emit initializer elements to preserve possible sideeffects
                    foreach (var init in initializer.Initializers)
                    {
                        EmitExpression(init, used: false);
                    }
                }
            }
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
                // check if need to construct at all
                if (!used && ConstructorNotSideEffecting(constructor))
                {
                    // ctor has no side-effects, so we will just evaluate the arguments
                    foreach (var arg in expression.Arguments)
                    {
                        EmitExpression(arg, used: false);
                    }

                    return;
                }

                // ReadOnlySpan may just refer to the blob, if possible.
                if (TryEmitOptimizedReadonlySpan(expression, used, inPlaceTarget: null, out _))
                {
                    return;
                }

                // none of the above cases, so just create an instance

                var countBefore = _builder.LocalSlotManager.StartScopeOfTrackingAddressedLocals();

                EmitArguments(expression.Arguments, constructor.Parameters, expression.ArgumentRefKindsOpt);

                _builder.LocalSlotManager.EndScopeOfTrackingAddressedLocals(countBefore,
                    MightEscapeTemporaryRefs(expression, used));

                var stackAdjustment = GetObjCreationStackBehavior(expression);
                _builder.EmitOpCode(ILOpCode.Newobj, stackAdjustment);

                // for variadic ctors emit expanded ctor token
                EmitSymbolToken(constructor, expression.Syntax,
                                constructor.IsVararg ? (BoundArgListOperator)expression.Arguments[expression.Arguments.Length - 1] : null);

                EmitPopIfUnused(used);
            }
        }

        private bool TryEmitOptimizedReadonlySpan(BoundObjectCreationExpression expression, bool used, BoundExpression inPlaceTarget, out bool avoidInPlace)
        {
            int argumentsLength = expression.Arguments.Length;
            avoidInPlace = false;
            return ((argumentsLength == 1 &&
                     expression.Constructor.OriginalDefinition == (object)this._module.Compilation.GetWellKnownTypeMember(WellKnownMember.System_ReadOnlySpan_T__ctor_Array)) ||
                    (argumentsLength == 3 &&
                     expression.Constructor.OriginalDefinition == (object)this._module.Compilation.GetWellKnownTypeMember(WellKnownMember.System_ReadOnlySpan_T__ctor_Array_Start_Length))) &&
                   TryEmitOptimizedReadonlySpanCreation((NamedTypeSymbol)expression.Type, expression.Arguments[0], used, inPlaceTarget, out avoidInPlace,
                           start: argumentsLength == 3 ? expression.Arguments[1] : null,
                           length: argumentsLength == 3 ? expression.Arguments[2] : null);
        }

        /// <summary>
        /// Recognizes constructors known to not have side-effects (which means they can be skipped unless the constructed object is used)
        /// </summary>
        private bool ConstructorNotSideEffecting(MethodSymbol constructor)
        {
            var originalDef = constructor.OriginalDefinition;
            var compilation = _module.Compilation;

            if (originalDef == compilation.GetSpecialTypeMember(SpecialMember.System_Nullable_T__ctor))
            {
                return true;
            }

            if (originalDef.ContainingType.Name == NamedTypeSymbol.ValueTupleTypeName &&
                    (originalDef == compilation.GetWellKnownTypeMember(WellKnownMember.System_ValueTuple_T2__ctor) ||
                    originalDef == compilation.GetWellKnownTypeMember(WellKnownMember.System_ValueTuple_T3__ctor) ||
                    originalDef == compilation.GetWellKnownTypeMember(WellKnownMember.System_ValueTuple_T4__ctor) ||
                    originalDef == compilation.GetWellKnownTypeMember(WellKnownMember.System_ValueTuple_T5__ctor) ||
                    originalDef == compilation.GetWellKnownTypeMember(WellKnownMember.System_ValueTuple_T6__ctor) ||
                    originalDef == compilation.GetWellKnownTypeMember(WellKnownMember.System_ValueTuple_T7__ctor) ||
                    originalDef == compilation.GetWellKnownTypeMember(WellKnownMember.System_ValueTuple_TRest__ctor) ||
                    originalDef == compilation.GetWellKnownTypeMember(WellKnownMember.System_ValueTuple_T1__ctor)))
            {
                return true;
            }

            return false;
        }

        private void EmitAssignmentExpression(BoundAssignmentOperator assignmentOperator, UseKind useKind)
        {
            if (TryEmitAssignmentInPlace(assignmentOperator, useKind != UseKind.Unused))
            {
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

        // sometimes it is possible and advantageous to get an address of the LHS and 
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
            // If the left hand is itself a ref, then we can't use in-place assignment
            // because we need to spill the creation.
            if (assignmentOperator.IsRef)
            {
                return false;
            }

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
                if (rightType.IsReferenceType || (right.ConstantValueOpt != null && rightType.SpecialType != SpecialType.System_Decimal))
                {
                    return false;
                }
            }

            if (right.IsDefaultValue())
            {
                InPlaceInit(left, used);
                return true;
            }

            if (right is BoundObjectCreationExpression objCreation)
            {
                // If we are creating a Span<T> from a stackalloc, which is a particular pattern of code
                // produced by lowering, we must use the constructor in its standard form because the stack
                // is required to contain nothing more than stackalloc's argument.
                if (objCreation.Arguments.Length > 0 && objCreation.Arguments[0].Kind == BoundKind.ConvertedStackAllocExpression)
                {
                    return false;
                }

                // It is desirable to do in-place ctor call if possible.
                // we could do newobj/stloc, but in-place call 
                // produces the same or better code in current JITs 
                if (PartialCtorResultCannotEscape(left))
                {
                    var ctor = objCreation.Constructor;

                    // ctor can possibly see its own assignments indirectly if there are ref parameters or __arglist
                    if (System.Linq.ImmutableArrayExtensions.All(ctor.Parameters, p => p.RefKind == RefKind.None) &&
                        !ctor.IsVararg &&
                        TryInPlaceCtorCall(left, objCreation, used))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private bool SafeToGetWriteableReference(BoundExpression left)
        {
            if (!HasHome(left, AddressKind.Writeable))
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

            _builder.EmitOpCode(ILOpCode.Initobj);    //  initobj  <MyStruct>
            EmitSymbolToken(target.Type, target.Syntax);

            if (used)
            {
                Debug.Assert(TargetIsNotOnHeap(target), "cannot read-back the target since it could have been modified");
                EmitExpression(target, used);
            }
        }

        private bool TryInPlaceCtorCall(BoundExpression target, BoundObjectCreationExpression objCreation, bool used)
        {
            Debug.Assert(TargetIsNotOnHeap(target), "in-place construction target should not be on heap");

            // ReadOnlySpan may just refer to the blob, if possible.
            if (TryEmitOptimizedReadonlySpan(objCreation, used, target, out bool avoidInPlace))
            {
                return true;
            }

            if (avoidInPlace)
            {
                // We can use an ROS wrapper around a blob if we don't initialize in-place.
                return false;
            }

            var temp = EmitAddress(target, AddressKind.Writeable);
            Debug.Assert(temp == null, "in-place ctor target should not create temps");

            var constructor = objCreation.Constructor;

            var countBefore = _builder.LocalSlotManager.StartScopeOfTrackingAddressedLocals();

            EmitArguments(objCreation.Arguments, constructor.Parameters, objCreation.ArgumentRefKindsOpt);

            _builder.LocalSlotManager.EndScopeOfTrackingAddressedLocals(countBefore,
                MightEscapeTemporaryRefs(objCreation, used));

            // -2 to adjust for consumed target address and not produced value.
            var stackAdjustment = GetObjCreationStackBehavior(objCreation) - 2;
            _builder.EmitOpCode(ILOpCode.Call, stackAdjustment);
            // for variadic ctors emit expanded ctor token
            EmitSymbolToken(constructor, objCreation.Syntax,
                            constructor.IsVararg ? (BoundArgListOperator)objCreation.Arguments[objCreation.Arguments.Length - 1] : null);

            if (used)
            {
                EmitExpression(target, used: true);
            }

            return true;
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
                        if (left.FieldSymbol.RefKind != RefKind.None &&
                            !assignmentOperator.IsRef)
                        {
                            EmitFieldLoadNoIndirection(left, used: true);
                            lhsUsesStack = true;
                        }
                        else if (!left.FieldSymbol.IsStatic)
                        {
                            var temp = EmitReceiverRef(left.ReceiverOpt, AddressKind.Writeable);
                            Debug.Assert(temp == null, "temp is unexpected when assigning to a field");
                            lhsUsesStack = true;
                        }
                    }
                    break;

                case BoundKind.Parameter:
                    {
                        var left = (BoundParameter)assignmentTarget;
                        if (left.ParameterSymbol.RefKind != RefKind.None &&
                            !assignmentOperator.IsRef)
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

                        if (left.LocalSymbol.RefKind != RefKind.None && !assignmentOperator.IsRef)
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

                case BoundKind.ConditionalOperator:
                    {
                        var left = (BoundConditionalOperator)assignmentTarget;
                        Debug.Assert(left.IsRef);

                        var temp = EmitAddress(left, AddressKind.Writeable);
                        Debug.Assert(temp == null, "taking ref of this should not create a temp");

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

                        // NOTE: not releasing sequence locals right away. 
                        // Since sequence is used as a variable, we will keep the locals for the extent of the containing expression
                        DefineAndRecordLocals(sequence);
                        EmitSideEffects(sequence);
                        lhsUsesStack = EmitAssignmentPreamble(assignmentOperator.Update(sequence.Value, assignmentOperator.Right, assignmentOperator.IsRef, assignmentOperator.Type));
                        CloseScopeAndKeepLocals(sequence);
                    }
                    break;

                case BoundKind.Call:
                    {
                        var left = (BoundCall)assignmentTarget;

                        Debug.Assert(left.Method.RefKind != RefKind.None);
                        EmitCallExpression(left, UseKind.UsedAsAddress);

                        lhsUsesStack = true;
                    }
                    break;

                case BoundKind.FunctionPointerInvocation:
                    {
                        var left = (BoundFunctionPointerInvocation)assignmentTarget;

                        Debug.Assert(left.FunctionPointer.Signature.RefKind != RefKind.None);
                        EmitCalli(left, UseKind.UsedAsAddress);

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

                case BoundKind.ModuleVersionId:
                case BoundKind.InstrumentationPayloadRoot:
                    break;

                case BoundKind.AssignmentOperator:
                    var assignment = (BoundAssignmentOperator)assignmentTarget;
                    if (!assignment.IsRef)
                    {
                        goto default;
                    }
                    EmitAssignmentExpression(assignment, UseKind.UsedAsAddress);
                    break;

                default:
                    throw ExceptionUtilities.UnexpectedValue(assignmentTarget.Kind);
            }

            return lhsUsesStack;
        }

        private void EmitAssignmentValue(BoundAssignmentOperator assignmentOperator)
        {
            if (!assignmentOperator.IsRef)
            {
                EmitExpression(assignmentOperator.Right, used: true);
            }
            else
            {
                int exprTempsBefore = _expressionTemps?.Count ?? 0;
                BoundExpression lhs = assignmentOperator.Left;

                // NOTE: passing "ReadOnlyStrict" here. 
                //       we should not get an address of a copy if at all possible
                LocalDefinition temp = EmitAddress(assignmentOperator.Right, lhs.GetRefKind() is RefKind.RefReadOnly or RefKindExtensions.StrictIn or RefKind.RefReadOnlyParameter ? AddressKind.ReadOnlyStrict : AddressKind.Writeable);

                // Generally taking a ref for the purpose of ref assignment should not be done on homeless values
                // however, there are very rare cases when we need to get a ref off a temp in synthetic code.
                // Retain those temps for the extent of the encompassing expression.
                AddExpressionTemp(temp);

                var exprTempsAfter = _expressionTemps?.Count ?? 0;

                // are we, by the way, ref-assigning to something that lives longer than encompassing expression?
                Debug.Assert(lhs.Kind != BoundKind.Parameter || exprTempsAfter <= exprTempsBefore);

                if (lhs.Kind == BoundKind.Local && ((BoundLocal)lhs).LocalSymbol.SynthesizedKind.IsLongLived())
                {
                    // This situation is extremely rare. We are assigning a ref to a local with unknown lifetime
                    // while computing that ref required expression temps.
                    //
                    // We cannot reuse any of those temps and must leak them from the retained set.
                    // Any of them could be directly or indirectly referred by the LHS after the assignment.
                    // and we do not know the scope of the LHS - could be the whole method.
                    if (exprTempsAfter > exprTempsBefore)
                    {
                        _expressionTemps.Count = exprTempsBefore;
                    }
                }
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
                    // If we have something like:
                    //
                    // ref int t1 = (ref int t2 = ref M().s); 
                    //
                    // or the even more odd:
                    //
                    // int t1 = (ref int t2 = ref M().s);
                    //
                    // We need to figure out which of the situations above we are in, and ensure that the
                    // correct kind of temporary is created here. And also that either its value or its
                    // indirected value is read out after the store, in EmitAssignmentPostfix, below.

                    temp = AllocateTemp(
                        assignmentOperator.Left.Type,
                        assignmentOperator.Left.Syntax,
                        assignmentOperator.IsRef ? LocalSlotConstraints.ByRef : LocalSlotConstraints.None);
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
                    EmitFieldStore((BoundFieldAccess)expression, assignment.IsRef);
                    break;

                case BoundKind.Local:
                    // If we are doing a 'normal' local assignment like 'int t = 10;', or
                    // if we are initializing a temporary like 'ref int t = ref M().s;' then
                    // we just emit a local store. If we are doing an assignment through
                    // a ref local temporary then we assume that the instruction to load
                    // the address is already on the stack, and we must indirect through it.

                    // See the comments in EmitAssignmentExpression above for details.
                    BoundLocal local = (BoundLocal)expression;
                    if (local.LocalSymbol.RefKind != RefKind.None && !assignment.IsRef)
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
                    EmitParameterStore((BoundParameter)expression, assignment.IsRef);
                    break;

                case BoundKind.Dup:
                    Debug.Assert(((BoundDup)expression).RefKind != RefKind.None);
                    EmitIndirectStore(expression.Type, expression.Syntax);
                    break;

                case BoundKind.ConditionalOperator:
                    Debug.Assert(((BoundConditionalOperator)expression).IsRef);
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
                        EmitStore(assignment.Update(sequence.Value, assignment.Right, assignment.IsRef, assignment.Type));
                    }
                    break;

                case BoundKind.Call:
                    Debug.Assert(((BoundCall)expression).Method.RefKind != RefKind.None);
                    EmitIndirectStore(expression.Type, expression.Syntax);
                    break;

                case BoundKind.FunctionPointerInvocation:
                    Debug.Assert(((BoundFunctionPointerInvocation)expression).FunctionPointer.Signature.RefKind != RefKind.None);
                    EmitIndirectStore(expression.Type, expression.Syntax);
                    break;

                case BoundKind.ModuleVersionId:
                    EmitModuleVersionIdStore((BoundModuleVersionId)expression);
                    break;

                case BoundKind.InstrumentationPayloadRoot:
                    EmitInstrumentationPayloadRootStore((BoundInstrumentationPayloadRoot)expression);
                    break;

                case BoundKind.AssignmentOperator:
                    var nested = (BoundAssignmentOperator)expression;
                    if (!nested.IsRef)
                    {
                        goto default;
                    }
                    EmitIndirectStore(nested.Type, expression.Syntax);
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
                if (useKind == UseKind.UsedAsAddress)
                {
                    _builder.EmitLocalAddress(temp);
                }
                else
                {
                    _builder.EmitLocalLoad(temp);
                }
                FreeTemp(temp);
            }

            if (useKind == UseKind.UsedAsValue && assignment.IsRef)
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

        private void EmitArrayElementStore(ArrayTypeSymbol arrayType, SyntaxNode syntaxNode)
        {
            if (arrayType.IsSZArray)
            {
                EmitVectorElementStore(arrayType, syntaxNode);
            }
            else
            {
                _builder.EmitArrayElementStore(_module.Translate(arrayType), syntaxNode);
            }
        }

        /// <summary>
        /// Emit an element store instruction for a single dimensional array.
        /// </summary>
        private void EmitVectorElementStore(ArrayTypeSymbol arrayType, SyntaxNode syntaxNode)
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
                case Microsoft.Cci.PrimitiveTypeCode.FunctionPointer:
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

        private void EmitFieldStore(BoundFieldAccess fieldAccess, bool refAssign)
        {
            var field = fieldAccess.FieldSymbol;

            if (field.IsVolatile)
            {
                _builder.EmitOpCode(ILOpCode.Volatile);
            }

            if (field.RefKind != RefKind.None && !refAssign)
            {
                //NOTE: we should have the actual field already loaded, 
                //now need to do a store to where it points to
                EmitIndirectStore(field.Type, fieldAccess.Syntax);
            }
            else
            {
                _builder.EmitOpCode(field.IsStatic ? ILOpCode.Stsfld : ILOpCode.Stfld);
                EmitSymbolToken(field, fieldAccess.Syntax);
            }
        }

        private void EmitParameterStore(BoundParameter parameter, bool refAssign)
        {
            if (parameter.ParameterSymbol.RefKind != RefKind.None && !refAssign)
            {
                //NOTE: we should have the actual parameter already loaded, 
                //now need to do a store to where it points to
                EmitIndirectStore(parameter.ParameterSymbol.Type, parameter.Syntax);
            }
            else
            {
                int slot = ParameterSlot(parameter);
                _builder.EmitStoreArgumentOpcode(slot);
            }
        }

        private void EmitIndirectStore(TypeSymbol type, SyntaxNode syntaxNode)
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
                case Microsoft.Cci.PrimitiveTypeCode.FunctionPointer:
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

        private void EmitIsExpression(BoundIsOperator isOp, bool used, bool omitBooleanConversion)
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

                if (!omitBooleanConversion)
                {
                    _builder.EmitOpCode(ILOpCode.Ldnull);
                    _builder.EmitOpCode(ILOpCode.Cgt_un);
                }
            }
        }

        private void EmitAsExpression(BoundAsOperator asOp, bool used)
        {
            Debug.Assert(asOp.OperandPlaceholder is null);
            Debug.Assert(asOp.OperandConversion is null);

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

        private void EmitDefaultValue(TypeSymbol type, bool used, SyntaxNode syntaxNode)
        {
            if (used)
            {
                // default type parameter values must be emitted as 'initobj' regardless of constraints
                if (!type.IsTypeParameter() && type.SpecialType != SpecialType.System_Decimal)
                {
                    var constantValue = type.GetDefaultValue();
                    if (constantValue != null)
                    {
                        _builder.EmitConstantValue(constantValue, syntaxNode);
                        return;
                    }
                }

                if (type.IsPointerOrFunctionPointer() || type.SpecialType == SpecialType.System_UIntPtr)
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
                    EmitInitObj(type, true, syntaxNode);
                }
            }
        }

        private void EmitDefaultExpression(BoundDefaultExpression expression, bool used)
        {
            Debug.Assert(expression.Type.SpecialType == SpecialType.System_Decimal ||
                expression.Type.GetDefaultValue() == null, "constant should be set on this expression");

            // Default value for the given default expression is not a constant
            // Expression must be of type parameter type or a non-primitive value type
            // Emit an initobj instruction for these cases
            EmitDefaultValue(expression.Type, used, expression.Syntax);
        }

        private void EmitConstantExpression(TypeSymbol type, ConstantValue constantValue, bool used, SyntaxNode syntaxNode)
        {
            // unused constant has no side-effects
            if (!used)
            {
                return;
            }

            // Null type parameter values must be emitted as 'initobj' rather than 'ldnull'.
            if (type is { TypeKind: TypeKind.TypeParameter } && constantValue.IsNull)
            {
                EmitInitObj(type, used, syntaxNode);
            }
            else
            {
                _builder.EmitConstantValue(constantValue, syntaxNode);
            }
        }

        private void EmitInitObj(TypeSymbol type, bool used, SyntaxNode syntaxNode)
        {
            if (used)
            {
                var temp = this.AllocateTemp(type, syntaxNode);
                _builder.EmitLocalAddress(temp);                  //  ldloca temp
                _builder.EmitOpCode(ILOpCode.Initobj);            //  initobj  <MyStruct>
                EmitSymbolToken(type, syntaxNode);
                _builder.EmitLocalLoad(temp);                     //  ldloc temp
                FreeTemp(temp);
            }
        }

        private void EmitGetTypeFromHandle(BoundTypeOf boundTypeOf)
        {
            _builder.EmitOpCode(ILOpCode.Call, stackAdjustment: 0); //argument off, return value on
            var getTypeMethod = boundTypeOf.GetTypeFromHandle;
            Debug.Assert((object)getTypeMethod != null); // Should have been checked during binding
            EmitSymbolToken(getTypeMethod, boundTypeOf.Syntax, null);
        }

        private void EmitTypeOfExpression(BoundTypeOfOperator boundTypeOfOperator)
        {
            TypeSymbol type = boundTypeOfOperator.SourceType.Type;
            _builder.EmitOpCode(ILOpCode.Ldtoken);
            EmitSymbolToken(type, boundTypeOfOperator.SourceType.Syntax);
            EmitGetTypeFromHandle(boundTypeOfOperator);
        }

        private void EmitSizeOfExpression(BoundSizeOfOperator boundSizeOfOperator)
        {
            TypeSymbol type = boundSizeOfOperator.SourceType.Type;
            _builder.EmitOpCode(ILOpCode.Sizeof);
            EmitSymbolToken(type, boundSizeOfOperator.SourceType.Syntax);
        }

        private void EmitMethodDefIndexExpression(BoundMethodDefIndex node)
        {
            Debug.Assert(node.Method.IsDefinition);
            Debug.Assert(node.Type.SpecialType == SpecialType.System_Int32);
            _builder.EmitOpCode(ILOpCode.Ldtoken);

            // For partial methods, we emit pseudo token based on the symbol for the partial
            // definition part as opposed to the symbol for the partial implementation part.
            // We will need to resolve the symbol associated with each pseudo token in order
            // to compute the real method definition tokens later. For partial methods, this
            // resolution can only succeed if the associated symbol is the symbol for the
            // partial definition and not the symbol for the partial implementation (see
            // MethodSymbol.ResolvedMethodImpl()).
            var symbol = node.Method.PartialDefinitionPart ?? node.Method;

            EmitSymbolToken(symbol, node.Syntax, null, encodeAsRawDefinitionToken: true);
        }

        private void EmitLocalIdExpression(BoundLocalId node)
        {
            Debug.Assert(node.Type.SpecialType == SpecialType.System_Int32);

            if (node.HoistedField is null)
            {
                _builder.EmitIntConstant(GetLocal(node.Local).SlotIndex);
            }
            else
            {
                EmitHoistedVariableId(node.HoistedField, node.Syntax);
            }
        }

        private void EmitParameterIdExpression(BoundParameterId node)
        {
            Debug.Assert(node.Type.SpecialType == SpecialType.System_Int32);

            if (node.HoistedField is null)
            {
                _builder.EmitIntConstant(node.Parameter.Ordinal);
            }
            else
            {
                EmitHoistedVariableId(node.HoistedField, node.Syntax);
            }
        }

        private void EmitHoistedVariableId(FieldSymbol field, SyntaxNode syntax)
        {
            Debug.Assert(field.IsDefinition);
            var fieldRef = _module.Translate(field, syntax, _diagnostics.DiagnosticBag, needDeclaration: true);

            _builder.EmitOpCode(ILOpCode.Ldtoken);
            _builder.EmitToken(fieldRef, syntax, Cci.MetadataWriter.RawTokenEncoding.LiftedVariableId);
        }

        private void EmitMaximumMethodDefIndexExpression(BoundMaximumMethodDefIndex node)
        {
            Debug.Assert(node.Type.SpecialType == SpecialType.System_Int32);
            _builder.EmitOpCode(ILOpCode.Ldtoken);
            _builder.EmitGreatestMethodToken();
        }

        private void EmitModuleVersionIdLoad(BoundModuleVersionId node)
        {
            _builder.EmitOpCode(ILOpCode.Ldsfld);
            EmitModuleVersionIdToken(node);
        }

        private void EmitModuleVersionIdStore(BoundModuleVersionId node)
        {
            _builder.EmitOpCode(ILOpCode.Stsfld);
            EmitModuleVersionIdToken(node);
        }

        private void EmitModuleVersionIdToken(BoundModuleVersionId node)
        {
            _builder.EmitToken(
                _module.GetModuleVersionId(_module.Translate(node.Type, node.Syntax, _diagnostics.DiagnosticBag), node.Syntax, _diagnostics.DiagnosticBag),
                node.Syntax);
        }

        private void EmitThrowIfModuleCancellationRequested(SyntaxNode syntax)
        {
            var cancellationTokenType = _module.CommonCompilation.CommonGetWellKnownType(WellKnownType.System_Threading_CancellationToken);

            _builder.EmitOpCode(ILOpCode.Ldsflda);
            _builder.EmitToken(
                _module.GetModuleCancellationToken(_module.Translate(cancellationTokenType, syntax, _diagnostics.DiagnosticBag), syntax, _diagnostics.DiagnosticBag),
                syntax);

            var throwMethod = (MethodSymbol)_module.Compilation.GetWellKnownTypeMember(WellKnownMember.System_Threading_CancellationToken__ThrowIfCancellationRequested);

            // BoundThrowIfModuleCancellationRequested should not be created if the method doesn't exist.
            Debug.Assert(throwMethod != null);

            _builder.EmitOpCode(ILOpCode.Call, -1);
            _builder.EmitToken(
                _module.Translate(throwMethod, syntax, _diagnostics.DiagnosticBag),
                syntax);
        }

        private void EmitModuleCancellationTokenLoad(SyntaxNode syntax)
        {
            var cancellationTokenType = _module.CommonCompilation.CommonGetWellKnownType(WellKnownType.System_Threading_CancellationToken);

            _builder.EmitOpCode(ILOpCode.Ldsfld);
            _builder.EmitToken(
                _module.GetModuleCancellationToken(_module.Translate(cancellationTokenType, syntax, _diagnostics.DiagnosticBag), syntax, _diagnostics.DiagnosticBag),
                syntax);
        }

        private void EmitModuleVersionIdStringLoad()
        {
            _builder.EmitOpCode(ILOpCode.Ldstr);
            _builder.EmitModuleVersionIdStringToken();
        }

        private void EmitInstrumentationPayloadRootLoad(BoundInstrumentationPayloadRoot node)
        {
            _builder.EmitOpCode(ILOpCode.Ldsfld);
            EmitInstrumentationPayloadRootToken(node);
        }

        private void EmitInstrumentationPayloadRootStore(BoundInstrumentationPayloadRoot node)
        {
            _builder.EmitOpCode(ILOpCode.Stsfld);
            EmitInstrumentationPayloadRootToken(node);
        }

        private void EmitInstrumentationPayloadRootToken(BoundInstrumentationPayloadRoot node)
        {
            _builder.EmitToken(_module.GetInstrumentationPayloadRoot(node.AnalysisKind, _module.Translate(node.Type, node.Syntax, _diagnostics.DiagnosticBag), node.Syntax, _diagnostics.DiagnosticBag), node.Syntax);
        }

        private void EmitSourceDocumentIndex(BoundSourceDocumentIndex node)
        {
            Debug.Assert(node.Type.SpecialType == SpecialType.System_Int32);
            _builder.EmitOpCode(ILOpCode.Ldtoken);
            _builder.EmitSourceDocumentIndexToken(node.Document);
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
            if (!TypeSymbol.Equals(node.Type, getMethod.ReturnType, TypeCompareKind.ConsiderEverything2))
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
            if (!TypeSymbol.Equals(node.Type, getField.ReturnType, TypeCompareKind.ConsiderEverything2))
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
            Debug.Assert(expr.ConstantValueOpt == null, "Constant value should have been emitted directly");

            // Generate branchless IL for (b ? 1 : 0).
            if (used && _ilEmitStyle != ILEmitStyle.Debug &&
                (IsNumeric(expr.Type) || expr.Type.PrimitiveTypeCode == Cci.PrimitiveTypeCode.Boolean) &&
                expr.Consequence.ConstantValueOpt?.IsIntegralValueZeroOrOne(out bool isConsequenceOne) == true &&
                expr.Alternative.ConstantValueOpt?.IsIntegralValueZeroOrOne(out bool isAlternativeOne) == true &&
                isConsequenceOne != isAlternativeOne &&
                TryEmitComparison(expr.Condition, sense: isConsequenceOne))
            {
                var toType = expr.Type.PrimitiveTypeCode;
                if (toType != Cci.PrimitiveTypeCode.Boolean)
                {
                    _builder.EmitNumericConversion(Cci.PrimitiveTypeCode.Int32, toType, @checked: false);
                }
                return;
            }

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
                else if (expr.Type.IsInterfaceType() && !TypeSymbol.Equals(expr.Type, mergeTypeOfAlternative, TypeCompareKind.ConsiderEverything2))
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
                else if (expr.Type.IsInterfaceType() && !TypeSymbol.Equals(expr.Type, mergeTypeOfConsequence, TypeCompareKind.ConsiderEverything2))
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
            Debug.Assert(expr.LeftConversion is null, "coalesce with nontrivial left conversions are lowered into conditional.");
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
                else if (expr.Type.IsInterfaceType() && !TypeSymbol.Equals(expr.Type, mergeTypeOfLeftValue, TypeCompareKind.ConsiderEverything2))
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
        // types from the types of operands when performing stack merges in coalesce/conditional.
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
            if (!(expr.Type.IsInterfaceType() || expr.Type.IsDelegateType()))
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
                    Debug.Assert(conversionKind != ConversionKind.NullLiteral && conversionKind != ConversionKind.DefaultLiteral);

                    if (conversionKind.IsImplicitConversion() &&
                        conversionKind != ConversionKind.MethodGroup &&
                        conversionKind != ConversionKind.NullLiteral &&
                        conversionKind != ConversionKind.DefaultLiteral)
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
            if (TypeSymbol.Equals(to, from, TypeCompareKind.ConsiderEverything2))
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

            return (to.IsDelegateType() && !TypeSymbol.Equals(to, from, TypeCompareKind.ConsiderEverything2)) ||
                   (to.IsInterfaceType() && from.IsInterfaceType() && !from.InterfacesAndTheirBaseInterfacesNoUseSiteDiagnostics.ContainsKey((NamedTypeSymbol)to));
        }

        private void EmitStaticCast(TypeSymbol to, SyntaxNode syntax)
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

        private void EmitBox(TypeSymbol type, SyntaxNode syntaxNode)
        {
            Debug.Assert(!type.IsRefLikeType);

            _builder.EmitOpCode(ILOpCode.Box);
            EmitSymbolToken(type, syntaxNode);
        }

        private void EmitCalli(BoundFunctionPointerInvocation ptrInvocation, UseKind useKind)
        {
            EmitExpression(ptrInvocation.InvokedExpression, used: true);
            LocalDefinition temp = null;
            // The function pointer token must be the last thing on the stack before the
            // calli invocation, but we need to preserve left-to-right semantics of the
            // actual code. If there are arguments, therefore, we evaluate the code that
            // produces the function pointer token, store it in a local, evaluate the
            // arguments, then load that token again.
            if (ptrInvocation.Arguments.Length > 0)
            {
                temp = AllocateTemp(ptrInvocation.InvokedExpression.Type, ptrInvocation.Syntax);
                _builder.EmitLocalStore(temp);
            }

            FunctionPointerMethodSymbol method = ptrInvocation.FunctionPointer.Signature;

            var countBefore = _builder.LocalSlotManager.StartScopeOfTrackingAddressedLocals();

            EmitArguments(ptrInvocation.Arguments, method.Parameters, ptrInvocation.ArgumentRefKindsOpt);

            _builder.LocalSlotManager.EndScopeOfTrackingAddressedLocals(countBefore,
                MightEscapeTemporaryRefs(ptrInvocation, used: useKind != UseKind.Unused));

            var stackBehavior = GetCallStackBehavior(ptrInvocation.FunctionPointer.Signature, ptrInvocation.Arguments);

            if (temp is object)
            {
                _builder.EmitLocalLoad(temp);
                FreeTemp(temp);
            }

            _builder.EmitOpCode(ILOpCode.Calli, stackBehavior);
            EmitSignatureToken(ptrInvocation.FunctionPointer, ptrInvocation.Syntax);
            EmitCallCleanup(ptrInvocation.Syntax, useKind, method);
        }

        private void EmitCallCleanup(SyntaxNode syntax, UseKind useKind, MethodSymbol method)
        {
            if (!method.ReturnsVoid)
            {
                EmitPopIfUnused(useKind != UseKind.Unused);
            }
            else if (_ilEmitStyle == ILEmitStyle.Debug)
            {
                // The only void methods with usable return values are constructors and the only
                // time we see them here, the return should be unused.
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
                EmitLoadIndirect(method.ReturnType, syntax);
            }
            else if (useKind == UseKind.UsedAsAddress)
            {
                Debug.Assert(method.RefKind != RefKind.None);
            }
        }

        private void EmitLoadFunction(BoundFunctionPointerLoad load, bool used)
        {
            Debug.Assert(load.Type is { TypeKind: TypeKind.FunctionPointer });

            if (used)
            {
                if ((load.TargetMethod.IsAbstract || load.TargetMethod.IsVirtual) && load.TargetMethod.IsStatic)
                {
                    if (load.ConstrainedToTypeOpt is not { TypeKind: TypeKind.TypeParameter })
                    {
                        throw ExceptionUtilities.Unreachable();
                    }

                    _builder.EmitOpCode(ILOpCode.Constrained);
                    EmitSymbolToken(load.ConstrainedToTypeOpt, load.Syntax);
                }

                _builder.EmitOpCode(ILOpCode.Ldftn);
                EmitSymbolToken(load.TargetMethod, load.Syntax, optArgList: null);
            }
        }
    }
}
