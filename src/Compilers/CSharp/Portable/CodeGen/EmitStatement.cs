// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Reflection.Metadata;
using System.Runtime.CompilerServices;
using Microsoft.CodeAnalysis.CodeGen;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Symbols;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;
using static Microsoft.CodeAnalysis.CSharp.Binder;

namespace Microsoft.CodeAnalysis.CSharp.CodeGen
{
    internal partial class CodeGenerator
    {
        private void EmitStatement(BoundStatement statement)
        {
            switch (statement.Kind)
            {
                case BoundKind.Block:
                    EmitBlock((BoundBlock)statement);
                    break;

                case BoundKind.Scope:
                    EmitScope((BoundScope)statement);
                    break;

                case BoundKind.SequencePoint:
                    this.EmitSequencePointStatement((BoundSequencePoint)statement);
                    break;

                case BoundKind.SequencePointWithSpan:
                    this.EmitSequencePointStatement((BoundSequencePointWithSpan)statement);
                    break;

                case BoundKind.SavePreviousSequencePoint:
                    this.EmitSavePreviousSequencePoint((BoundSavePreviousSequencePoint)statement);
                    break;

                case BoundKind.RestorePreviousSequencePoint:
                    this.EmitRestorePreviousSequencePoint((BoundRestorePreviousSequencePoint)statement);
                    break;

                case BoundKind.StepThroughSequencePoint:
                    this.EmitStepThroughSequencePoint((BoundStepThroughSequencePoint)statement);
                    break;

                case BoundKind.ExpressionStatement:
                    EmitExpression(((BoundExpressionStatement)statement).Expression, false);
                    break;

                case BoundKind.StatementList:
                    EmitStatementList((BoundStatementList)statement);
                    break;

                case BoundKind.ReturnStatement:
                    EmitReturnStatement((BoundReturnStatement)statement);
                    break;

                case BoundKind.GotoStatement:
                    EmitGotoStatement((BoundGotoStatement)statement);
                    break;

                case BoundKind.LabelStatement:
                    EmitLabelStatement((BoundLabelStatement)statement);
                    break;

                case BoundKind.ConditionalGoto:
                    EmitConditionalGoto((BoundConditionalGoto)statement);
                    break;

                case BoundKind.ThrowStatement:
                    EmitThrowStatement((BoundThrowStatement)statement);
                    break;

                case BoundKind.TryStatement:
                    EmitTryStatement((BoundTryStatement)statement);
                    break;

                case BoundKind.SwitchDispatch:
                    EmitSwitchDispatch((BoundSwitchDispatch)statement);
                    break;

                case BoundKind.StateMachineScope:
                    EmitStateMachineScope((BoundStateMachineScope)statement);
                    break;

                case BoundKind.NoOpStatement:
                    EmitNoOpStatement((BoundNoOpStatement)statement);
                    break;

                default:
                    // Code gen should not be invoked if there are errors.
                    throw ExceptionUtilities.UnexpectedValue(statement.Kind);
            }

#if DEBUG
            if (_stackLocals == null || _stackLocals.Count == 0)
            {
                _builder.AssertStackEmpty();
            }
#endif

            ReleaseExpressionTemps();
        }

        private int EmitStatementAndCountInstructions(BoundStatement statement)
        {
            int n = _builder.InstructionsEmitted;
            this.EmitStatement(statement);
            return _builder.InstructionsEmitted - n;
        }

        private void EmitStatementList(BoundStatementList list)
        {
            for (int i = 0, n = list.Statements.Length; i < n; i++)
            {
                EmitStatement(list.Statements[i]);
            }
        }

        private void EmitNoOpStatement(BoundNoOpStatement statement)
        {
            switch (statement.Flavor)
            {
                case NoOpStatementFlavor.Default:
                    if (_ilEmitStyle == ILEmitStyle.Debug)
                    {
                        _builder.EmitOpCode(ILOpCode.Nop);
                    }
                    break;

                case NoOpStatementFlavor.AwaitYieldPoint:
                    Debug.Assert((_asyncYieldPoints == null) == (_asyncResumePoints == null));
                    if (_asyncYieldPoints == null)
                    {
                        _asyncYieldPoints = ArrayBuilder<int>.GetInstance();
                        _asyncResumePoints = ArrayBuilder<int>.GetInstance();
                    }
                    Debug.Assert(_asyncYieldPoints.Count == _asyncResumePoints.Count);
                    _asyncYieldPoints.Add(_builder.AllocateILMarker());
                    break;

                case NoOpStatementFlavor.AwaitResumePoint:
                    Debug.Assert(_asyncYieldPoints != null);
                    Debug.Assert(_asyncYieldPoints != null);
                    _asyncResumePoints.Add(_builder.AllocateILMarker());
                    Debug.Assert(_asyncYieldPoints.Count == _asyncResumePoints.Count);
                    break;

                default:
                    throw ExceptionUtilities.UnexpectedValue(statement.Flavor);
            }
        }

        private void EmitThrowStatement(BoundThrowStatement node)
        {
            EmitThrow(node.ExpressionOpt);
        }

        private void EmitThrow(BoundExpression thrown)
        {
            if (thrown != null)
            {
                this.EmitExpression(thrown, true);

                var exprType = thrown.Type;
                // Expression type will be null for "throw null;".
                if (exprType?.TypeKind == TypeKind.TypeParameter)
                {
                    this.EmitBox(exprType, thrown.Syntax);
                }
            }

            _builder.EmitThrow(isRethrow: thrown == null);
        }

        private void EmitConditionalGoto(BoundConditionalGoto boundConditionalGoto)
        {
            object label = boundConditionalGoto.Label;
            Debug.Assert(label != null);
            EmitCondBranch(boundConditionalGoto.Condition, ref label, boundConditionalGoto.JumpIfTrue);
        }

        // 3.17 The brfalse instruction transfers control to target if value (of type int32, int64, object reference, managed
        //pointer, unmanaged pointer or native int) is zero (false). If value is non-zero (true), execution continues at
        //the next instruction.

        private static bool CanPassToBrfalse(TypeSymbol ts)
        {
            if (ts.IsEnumType())
            {
                // valid enums are all primitives
                return true;
            }

            var tc = ts.PrimitiveTypeCode;
            switch (tc)
            {
                case Microsoft.Cci.PrimitiveTypeCode.Float32:
                case Microsoft.Cci.PrimitiveTypeCode.Float64:
                    return false;

                case Microsoft.Cci.PrimitiveTypeCode.NotPrimitive:
                    // if this is a generic type param, verifier will want us to box
                    // EmitCondBranch knows that
                    return ts.IsReferenceType;

                default:
                    Debug.Assert(tc != Microsoft.Cci.PrimitiveTypeCode.Invalid);
                    Debug.Assert(tc != Microsoft.Cci.PrimitiveTypeCode.Void);

                    return true;
            }
        }

        private static BoundExpression TryReduce(BoundBinaryOperator condition, ref bool sense)
        {
            var opKind = condition.OperatorKind.Operator();

            Debug.Assert(opKind == BinaryOperatorKind.Equal ||
                        opKind == BinaryOperatorKind.NotEqual);

            BoundExpression nonConstOp;
            BoundExpression constOp = (condition.Left.ConstantValueOpt != null) ? condition.Left : null;

            if (constOp != null)
            {
                nonConstOp = condition.Right;
            }
            else
            {
                constOp = (condition.Right.ConstantValueOpt != null) ? condition.Right : null;
                if (constOp == null)
                {
                    return null;
                }
                nonConstOp = condition.Left;
            }

            var nonConstType = nonConstOp.Type;
            if (!CanPassToBrfalse(nonConstType))
            {
                return null;
            }

            bool isBool = nonConstType.PrimitiveTypeCode == Microsoft.Cci.PrimitiveTypeCode.Boolean;
            bool isZero = constOp.ConstantValueOpt.IsDefaultValue;

            // bool is special, only it can be compared to true and false...
            if (!isBool && !isZero)
            {
                return null;
            }

            // if comparing to zero, flip the sense
            if (isZero)
            {
                sense = !sense;
            }

            // if comparing != flip the sense
            if (opKind == BinaryOperatorKind.NotEqual)
            {
                sense = !sense;
            }

            return nonConstOp;
        }

        private const int IL_OP_CODE_ROW_LENGTH = 4;

        private static readonly ILOpCode[] s_condJumpOpCodes = new ILOpCode[]
        {
            //  <            <=               >                >=
            ILOpCode.Blt,    ILOpCode.Ble,    ILOpCode.Bgt,    ILOpCode.Bge,     // Signed
            ILOpCode.Bge,    ILOpCode.Bgt,    ILOpCode.Ble,    ILOpCode.Blt,     // Signed Invert
            ILOpCode.Blt_un, ILOpCode.Ble_un, ILOpCode.Bgt_un, ILOpCode.Bge_un,  // Unsigned
            ILOpCode.Bge_un, ILOpCode.Bgt_un, ILOpCode.Ble_un, ILOpCode.Blt_un,  // Unsigned Invert
            ILOpCode.Blt,    ILOpCode.Ble,    ILOpCode.Bgt,    ILOpCode.Bge,     // Float
            ILOpCode.Bge_un, ILOpCode.Bgt_un, ILOpCode.Ble_un, ILOpCode.Blt_un,  // Float Invert
        };

        /// <summary>
        /// Produces opcode for a jump that corresponds to given operation and sense.
        /// Also produces a reverse opcode - opcode for the same condition with inverted sense.
        /// </summary>
        private static ILOpCode CodeForJump(BoundBinaryOperator op, bool sense, out ILOpCode revOpCode)
        {
            int opIdx;

            switch (op.OperatorKind.Operator())
            {
                case BinaryOperatorKind.Equal:
                    revOpCode = !sense ? ILOpCode.Beq : ILOpCode.Bne_un;
                    return sense ? ILOpCode.Beq : ILOpCode.Bne_un;

                case BinaryOperatorKind.NotEqual:
                    revOpCode = !sense ? ILOpCode.Bne_un : ILOpCode.Beq;
                    return sense ? ILOpCode.Bne_un : ILOpCode.Beq;

                case BinaryOperatorKind.LessThan:
                    opIdx = 0;
                    break;

                case BinaryOperatorKind.LessThanOrEqual:
                    opIdx = 1;
                    break;

                case BinaryOperatorKind.GreaterThan:
                    opIdx = 2;
                    break;

                case BinaryOperatorKind.GreaterThanOrEqual:
                    opIdx = 3;
                    break;

                default:
                    throw ExceptionUtilities.UnexpectedValue(op.OperatorKind.Operator());
            }

            if (IsUnsignedBinaryOperator(op))
            {
                opIdx += 2 * IL_OP_CODE_ROW_LENGTH; //unsigned
            }
            else if (IsFloat(op.OperatorKind))
            {
                opIdx += 4 * IL_OP_CODE_ROW_LENGTH;  //float
            }

            int revOpIdx = opIdx;

            if (!sense)
            {
                opIdx += IL_OP_CODE_ROW_LENGTH; //invert op
            }
            else
            {
                revOpIdx += IL_OP_CODE_ROW_LENGTH; //invert rev
            }

            revOpCode = s_condJumpOpCodes[revOpIdx];
            return s_condJumpOpCodes[opIdx];
        }

        // generate a jump to dest if (condition == sense) is true
        private void EmitCondBranch(BoundExpression condition, ref object dest, bool sense)
        {
            _recursionDepth++;

            if (_recursionDepth > 1)
            {
                StackGuard.EnsureSufficientExecutionStack(_recursionDepth);

                EmitCondBranchCore(condition, ref dest, sense);
            }
            else
            {
                EmitCondBranchCoreWithStackGuard(condition, ref dest, sense);
            }

            _recursionDepth--;
        }

        private void EmitCondBranchCoreWithStackGuard(BoundExpression condition, ref object dest, bool sense)
        {
            Debug.Assert(_recursionDepth == 1);

            try
            {
                EmitCondBranchCore(condition, ref dest, sense);
                Debug.Assert(_recursionDepth == 1);
            }
            catch (InsufficientExecutionStackException)
            {
                _diagnostics.Add(ErrorCode.ERR_InsufficientStack,
                                 BoundTreeVisitor.CancelledByStackGuardException.GetTooLongOrComplexExpressionErrorLocation(condition));
                throw new EmitCancelledException();
            }
        }

        private void EmitCondBranchCore(BoundExpression condition, ref object dest, bool sense)
        {
oneMoreTime:

            ILOpCode ilcode;

            if (condition.ConstantValueOpt != null)
            {
                bool taken = condition.ConstantValueOpt.IsDefaultValue != sense;

                if (taken)
                {
                    dest = dest ?? new object();
                    _builder.EmitBranch(ILOpCode.Br, dest);
                }
                else
                {
                    // otherwise this branch will never be taken, so just fall through...
                }

                return;
            }

            switch (condition.Kind)
            {
                case BoundKind.BinaryOperator:

                    var binOp = (BoundBinaryOperator)condition;
                    Debug.Assert(binOp.ConstantValueOpt is null);

#nullable enable 
                    if (binOp.OperatorKind.OperatorWithLogical() is BinaryOperatorKind.LogicalOr or BinaryOperatorKind.LogicalAnd)
                    {
                        var stack = ArrayBuilder<(BoundExpression? condition, StrongBox<object?> destBox, bool sense)>.GetInstance();
                        var destBox = new StrongBox<object?>(dest);
                        stack.Push((binOp, destBox, sense));

                        do
                        {
                            (BoundExpression? condition, StrongBox<object?> destBox, bool sense) top = stack.Pop();

                            if (top.condition is null)
                            {
                                // This is a special entry to indicate that it is time to append the block
                                object? fallThrough = top.destBox.Value;
                                if (fallThrough != null)
                                {
                                    _builder.MarkLabel(fallThrough);
                                }
                            }
                            else if (top.condition.ConstantValueOpt is null &&
                                     top.condition is BoundBinaryOperator binary &&
                                     binary.OperatorKind.OperatorWithLogical() is BinaryOperatorKind.LogicalOr or BinaryOperatorKind.LogicalAnd)
                            {
                                if (binary.OperatorKind.OperatorWithLogical() is BinaryOperatorKind.LogicalOr ? !top.sense : top.sense)
                                {
                                    // gotoif(a != sense) fallThrough
                                    // gotoif(b == sense) dest
                                    // fallThrough:

                                    var fallThrough = new StrongBox<object?>();

                                    // Note, operations are pushed to the stack in opposite order
                                    stack.Push((null, fallThrough, true)); // This is a special entry to indicate that it is time to append the fallThrough block
                                    stack.Push((binary.Right, top.destBox, top.sense));
                                    stack.Push((binary.Left, fallThrough, !top.sense));
                                }
                                else
                                {
                                    // gotoif(a == sense) labDest
                                    // gotoif(b == sense) labDest

                                    // Note, operations are pushed to the stack in opposite order
                                    stack.Push((binary.Right, top.destBox, top.sense));
                                    stack.Push((binary.Left, top.destBox, top.sense));
                                }
                            }
                            else if (stack.Count == 0 && ReferenceEquals(destBox, top.destBox))
                            {
                                // Instead of recursion we can restart from the top with new condition
                                condition = top.condition;
                                sense = top.sense;
                                dest = destBox.Value;
                                stack.Free();
                                goto oneMoreTime;
                            }
                            else
                            {
                                EmitCondBranch(top.condition, ref top.destBox.Value, top.sense);
                            }
                        }
                        while (stack.Count != 0);

                        dest = destBox.Value;
                        stack.Free();
                        return;
                    }
#nullable disable

                    switch (binOp.OperatorKind.OperatorWithLogical())
                    {
                        case BinaryOperatorKind.LogicalOr:
                        case BinaryOperatorKind.LogicalAnd:
                            throw ExceptionUtilities.Unreachable();

                        case BinaryOperatorKind.Equal:
                        case BinaryOperatorKind.NotEqual:
                            var reduced = TryReduce(binOp, ref sense);
                            if (reduced != null)
                            {
                                condition = reduced;
                                goto oneMoreTime;
                            }
                            // Fall through
                            goto case BinaryOperatorKind.LessThan;

                        case BinaryOperatorKind.LessThan:
                        case BinaryOperatorKind.LessThanOrEqual:
                        case BinaryOperatorKind.GreaterThan:
                        case BinaryOperatorKind.GreaterThanOrEqual:
                            EmitExpression(binOp.Left, true);
                            EmitExpression(binOp.Right, true);
                            ILOpCode revOpCode;
                            ilcode = CodeForJump(binOp, sense, out revOpCode);
                            dest = dest ?? new object();
                            _builder.EmitBranch(ilcode, dest, revOpCode);
                            return;
                    }

                    // none of above.
                    // then it is regular binary expression - Or, And, Xor ...
                    goto default;

                case BoundKind.LoweredConditionalAccess:
                    {
                        var ca = (BoundLoweredConditionalAccess)condition;
                        var receiver = ca.Receiver;
                        var receiverType = receiver.Type;

                        // we need a copy if we deal with nonlocal value (to capture the value)
                        // or if we deal with stack local (reads are destructive)
                        var complexCase = !receiverType.IsReferenceType ||
                                          LocalRewriter.CanChangeValueBetweenReads(receiver, localsMayBeAssignedOrCaptured: false) ||
                                          (receiver.Kind == BoundKind.Local && IsStackLocal(((BoundLocal)receiver).LocalSymbol)) ||
                                          (ca.WhenNullOpt?.IsDefaultValue() == false);

                        if (complexCase)
                        {
                            goto default;
                        }

                        if (sense)
                        {
                            // gotoif(receiver != null) fallThrough
                            // gotoif(receiver.Access) dest
                            // fallThrough:

                            object fallThrough = null;

                            EmitCondBranch(receiver, ref fallThrough, sense: false);
                            // receiver is a reference type, and we only intend to read it
                            EmitReceiverRef(receiver, AddressKind.ReadOnly);
                            EmitCondBranch(ca.WhenNotNull, ref dest, sense: true);

                            if (fallThrough != null)
                            {
                                _builder.MarkLabel(fallThrough);
                            }
                        }
                        else
                        {
                            // gotoif(receiver == null) labDest
                            // gotoif(!receiver.Access) labDest
                            EmitCondBranch(receiver, ref dest, sense: false);
                            // receiver is a reference type, and we only intend to read it
                            EmitReceiverRef(receiver, AddressKind.ReadOnly);
                            condition = ca.WhenNotNull;
                            goto oneMoreTime;
                        }
                    }
                    return;

                case BoundKind.UnaryOperator:
                    var unOp = (BoundUnaryOperator)condition;
                    if (unOp.OperatorKind == UnaryOperatorKind.BoolLogicalNegation)
                    {
                        sense = !sense;
                        condition = unOp.Operand;
                        goto oneMoreTime;
                    }
                    goto default;

                case BoundKind.IsOperator:
                    var isOp = (BoundIsOperator)condition;
                    var operand = isOp.Operand;
                    EmitExpression(operand, true);
                    Debug.Assert((object)operand.Type != null);
                    if (!operand.Type.IsVerifierReference())
                    {
                        // box the operand for isinst if it is not a verifier reference
                        EmitBox(operand.Type, operand.Syntax);
                    }
                    _builder.EmitOpCode(ILOpCode.Isinst);
                    EmitSymbolToken(isOp.TargetType.Type, isOp.TargetType.Syntax);
                    ilcode = sense ? ILOpCode.Brtrue : ILOpCode.Brfalse;
                    dest = dest ?? new object();
                    _builder.EmitBranch(ilcode, dest);
                    return;

                case BoundKind.Sequence:
                    var seq = (BoundSequence)condition;
                    EmitSequenceCondBranch(seq, ref dest, sense);
                    return;

                default:
                    EmitExpression(condition, true);

                    var conditionType = condition.Type;
                    if (conditionType.IsReferenceType && !conditionType.IsVerifierReference())
                    {
                        EmitBox(conditionType, condition.Syntax);
                    }

                    ilcode = sense ? ILOpCode.Brtrue : ILOpCode.Brfalse;
                    dest = dest ?? new object();
                    _builder.EmitBranch(ilcode, dest);
                    return;
            }
        }

        private void EmitSequenceCondBranch(BoundSequence sequence, ref object dest, bool sense)
        {
            DefineLocals(sequence);
            EmitSideEffects(sequence);
            EmitCondBranch(sequence.Value, ref dest, sense);

            // sequence is used as a value, can release all locals
            FreeLocals(sequence);
        }

        private void EmitLabelStatement(BoundLabelStatement boundLabelStatement)
        {
            _builder.MarkLabel(boundLabelStatement.Label);
        }

        private void EmitGotoStatement(BoundGotoStatement boundGotoStatement)
        {
            _builder.EmitBranch(ILOpCode.Br, boundGotoStatement.Label);
        }

        // used by HandleReturn method which tries to inject
        // indirect ret sequence as a last statement in the block
        // that is the last statement of the current method
        // NOTE: it is important that there is no code after this "ret"
        //       it is desirable, for debug purposes, that this ret is emitted inside top level { }
        private bool IsLastBlockInMethod(BoundBlock block)
        {
            if (_boundBody == block)
            {
                return true;
            }

            //sometimes top level node is a statement list containing
            //epilogue and then a block. If we are having that block, it will do.
            var list = _boundBody as BoundStatementList;
            if (list != null && list.Statements.LastOrDefault() == block)
            {
                return true;
            }

            return false;
        }

        private void EmitBlock(BoundBlock block)
        {
            if (block.Instrumentation is not null)
            {
                EmitInstrumentedBlock(block.Instrumentation, block);
            }
            else
            {
                EmitUninstrumentedBlock(block);
            }
        }

        private void EmitInstrumentedBlock(BoundBlockInstrumentation instrumentation, BoundBlock block)
        {
            if (!instrumentation.Locals.IsEmpty)
            {
                _builder.OpenLocalScope();

                foreach (var local in instrumentation.Locals)
                {
                    DefineLocal(local, block.Syntax);
                }
            }

            if (instrumentation.Prologue != null)
            {
                if (_emitPdbSequencePoints)
                {
                    EmitHiddenSequencePoint();
                }

                EmitStatement(instrumentation.Prologue);
            }

            _builder.AssertStackEmpty();

            if (instrumentation.Epilogue != null)
            {
                // Check if we're emitting instrumentation try/finally in a catch filter, which produces invalid IL
                if (_inCatchFilterLevel > 0)
                {
                    // Try/finallys are not allowed in catch filters by spec, an error should be reported for this in initial binding.
                    Debug.Fail("Exception handling constructs should be blocked at the binding layer, not here at emit time");

                    // Report an error as this would produce invalid IL
                    _diagnostics.Add(ErrorCode.ERR_ModuleEmitFailure, block.Syntax.Location, ((Cci.INamedEntity)_module).Name, "Exception handling is not allowed in exception filters");
                }

                _builder.OpenLocalScope(ScopeType.TryCatchFinally);

                _builder.OpenLocalScope(ScopeType.Try);

                EmitUninstrumentedBlock(block);
                _builder.CloseLocalScope(); // try

                _builder.OpenLocalScope(ScopeType.Finally);

                if (_emitPdbSequencePoints)
                {
                    EmitHiddenSequencePoint();
                }

                EmitStatement(instrumentation.Epilogue);
                _builder.CloseLocalScope(); // finally

                _builder.CloseLocalScope(); // try-finally
            }
            else
            {
                EmitUninstrumentedBlock(block);
            }

            if (!instrumentation.Locals.IsEmpty)
            {
                foreach (var local in instrumentation.Locals)
                {
                    FreeLocal(local);
                }

                _builder.CloseLocalScope();
            }
        }

        private void EmitUninstrumentedBlock(BoundBlock block)
        {
            var hasLocals = !block.Locals.IsEmpty;

            if (hasLocals)
            {
                _builder.OpenLocalScope();

                foreach (var local in block.Locals)
                {
                    Debug.Assert(local.RefKind == RefKind.None || local.SynthesizedKind.IsLongLived(),
                        "A ref local ended up in a block and claims it is shortlived. That is dangerous. Are we sure it is short lived?");

                    var declaringReferences = local.DeclaringSyntaxReferences;
                    DefineLocal(local, !declaringReferences.IsEmpty ? (CSharpSyntaxNode)declaringReferences[0].GetSyntax() : block.Syntax);
                }
            }

            EmitStatements(block.Statements);

            if (_indirectReturnState == IndirectReturnState.Needed &&
                IsLastBlockInMethod(block))
            {
                if (block.Instrumentation != null)
                {
                    // jump out of try-finally
                    _builder.EmitBranch(ILOpCode.Br, s_returnLabel);
                }
                else
                {
                    HandleReturn();
                }
            }

            if (hasLocals)
            {
                foreach (var local in block.Locals)
                {
                    FreeLocal(local);
                }

                _builder.CloseLocalScope();
            }
        }

        private void EmitStatements(ImmutableArray<BoundStatement> statements)
        {
            foreach (var statement in statements)
            {
                EmitStatement(statement);
            }
        }

        private void EmitScope(BoundScope block)
        {
            Debug.Assert(!block.Locals.IsEmpty);

            _builder.OpenLocalScope();

            foreach (var local in block.Locals)
            {
                Debug.Assert(local.Name != null);
                Debug.Assert(local.SynthesizedKind == SynthesizedLocalKind.UserDefined &&
                    (local.ScopeDesignatorOpt?.Kind() == SyntaxKind.SwitchSection || local.ScopeDesignatorOpt?.Kind() == SyntaxKind.SwitchExpressionArm));
                if (!local.IsConst && !IsStackLocal(local))
                {
                    _builder.AddLocalToScope(_builder.LocalSlotManager.GetLocal(local));
                }
            }

            EmitStatements(block.Statements);

            _builder.CloseLocalScope();
        }

        private void EmitStateMachineScope(BoundStateMachineScope scope)
        {
            _builder.OpenLocalScope(ScopeType.StateMachineVariable);
            foreach (var field in scope.Fields)
            {
                if (field.SlotIndex >= 0)
                {
                    _builder.DefineUserDefinedStateMachineHoistedLocal(field.SlotIndex);
                }
            }

            EmitStatement(scope.Statement);
            _builder.CloseLocalScope();
        }

        // There are two ways a value can be returned from a function:
        // - Using ret opcode
        // - Store return value if any to a predefined temp and jump to the epilogue block
        // Sometimes ret is not an option (try/catch etc.). We also do this when emitting
        // debuggable code. This function is a stub for the logic that decides that.
        private bool ShouldUseIndirectReturn()
        {
            // If the method/lambda body is a block we define a sequence point for the closing brace of the body
            // and associate it with the ret instruction. If there is a return statement we need to store the value
            // to a long-lived synthesized local since a sequence point requires an empty evaluation stack.
            //
            // The emitted pattern is:
            //   <evaluate return statement expression>
            //   stloc $ReturnValue
            //   ldloc  $ReturnValue // sequence point
            //   ret
            //
            // Do not emit this pattern if the method doesn't include user code or doesn't have a block body.
            return _ilEmitStyle == ILEmitStyle.Debug && _method.GenerateDebugInfo && _methodBodySyntaxOpt?.IsKind(SyntaxKind.Block) == true ||
                   _builder.InExceptionHandler;
        }

        // Compiler generated return mapped to a block is very likely the synthetic return
        // that was added at the end of the last block of a void method by analysis.
        // This is likely to be the last return in the method, so if we have not yet
        // emitted return sequence, it is convenient to do it right here (if we can).
        private bool CanHandleReturnLabel(BoundReturnStatement boundReturnStatement)
        {
            return boundReturnStatement.WasCompilerGenerated &&
                    (boundReturnStatement.Syntax.IsKind(SyntaxKind.Block) || _method?.IsImplicitConstructor == true) &&
                    !_builder.InExceptionHandler;
        }

        private void EmitReturnStatement(BoundReturnStatement boundReturnStatement)
        {
            var expressionOpt = boundReturnStatement.ExpressionOpt;
            if (boundReturnStatement.RefKind == RefKind.None)
            {
                this.EmitExpression(expressionOpt, true);
            }
            else
            {
                // NOTE: passing "ReadOnlyStrict" here.
                //       we should never return an address of a copy
                var unexpectedTemp = this.EmitAddress(expressionOpt, this._method.RefKind == RefKind.RefReadOnly ? AddressKind.ReadOnlyStrict : AddressKind.Writeable);
                Debug.Assert(unexpectedTemp == null, "ref-returning a temp?");
            }

            if (ShouldUseIndirectReturn())
            {
                if (expressionOpt != null)
                {
                    _builder.EmitLocalStore(LazyReturnTemp);
                }

                if (_indirectReturnState != IndirectReturnState.Emitted && CanHandleReturnLabel(boundReturnStatement))
                {
                    HandleReturn();
                }
                else
                {
                    _builder.EmitBranch(ILOpCode.Br, s_returnLabel);

                    if (_indirectReturnState == IndirectReturnState.NotNeeded)
                    {
                        _indirectReturnState = IndirectReturnState.Needed;
                    }
                }
            }
            else
            {
                if (_indirectReturnState == IndirectReturnState.Needed && CanHandleReturnLabel(boundReturnStatement))
                {
                    if (expressionOpt != null)
                    {
                        _builder.EmitLocalStore(LazyReturnTemp);
                    }

                    HandleReturn();
                }
                else
                {
                    if (expressionOpt != null)
                    {
                        // Ensure the return type has been translated. (Necessary
                        // for cases of untranslated anonymous types.)
                        _module.Translate(expressionOpt.Type, boundReturnStatement.Syntax, _diagnostics.DiagnosticBag);
                    }
                    _builder.EmitRet(expressionOpt == null);
                }
            }
        }

        private void EmitTryStatement(BoundTryStatement statement, bool emitCatchesOnly = false)
        {
            if (_inCatchFilterLevel > 0)
            {
                // Try/finallys are not allowed in catch filters by spec, an error should be reported for this in initial binding.
                Debug.Fail("Exception handling constructs should be blocked at the binding layer, not here at emit time");

                // Report an error as this would produce invalid IL
                _diagnostics.Add(ErrorCode.ERR_ModuleEmitFailure, statement.Syntax.Location, ((Cci.INamedEntity)_module).Name, "Exception handling is not allowed in exception filters");
            }

            Debug.Assert(!statement.CatchBlocks.IsDefault);

            // Stack must be empty at beginning of try block.
            _builder.AssertStackEmpty();

            // IL requires catches and finally block to be distinct try
            // blocks so if the source contained both a catch and
            // a finally, nested scopes are emitted.
            bool emitNestedScopes = (!emitCatchesOnly &&
                (statement.CatchBlocks.Length > 0) &&
                (statement.FinallyBlockOpt != null));

            _builder.OpenLocalScope(ScopeType.TryCatchFinally);

            _builder.OpenLocalScope(ScopeType.Try);
            // IL requires catches and finally block to be distinct try
            // blocks so if the source contained both a catch and
            // a finally, nested scopes are emitted.

            _tryNestingLevel++;
            if (emitNestedScopes)
            {
                EmitTryStatement(statement, emitCatchesOnly: true);
            }
            else
            {
                EmitBlock(statement.TryBlock);
            }

            _tryNestingLevel--;
            // Close the Try scope
            _builder.CloseLocalScope();

            if (!emitNestedScopes)
            {
                foreach (var catchBlock in statement.CatchBlocks)
                {
                    EmitCatchBlock(catchBlock);
                }
            }

            if (!emitCatchesOnly && (statement.FinallyBlockOpt != null))
            {
                _builder.OpenLocalScope(statement.PreferFaultHandler ? ScopeType.Fault : ScopeType.Finally);
                EmitBlock(statement.FinallyBlockOpt);

                // close Finally scope
                _builder.CloseLocalScope();

                // close the whole try statement scope
                _builder.CloseLocalScope();

                // in a case where we emit surrogate Finally using Fault, we emit code like this
                //
                // try{
                //      . . .
                // } fault {
                //      finallyBlock;
                // }
                // finallyBlock;
                //
                // This is where the second copy of finallyBlock is emitted.
                if (statement.PreferFaultHandler)
                {
                    var finallyClone = FinallyCloner.MakeFinallyClone(statement);
                    EmitBlock(finallyClone);
                }
            }
            else
            {
                // close the whole try statement scope
                _builder.CloseLocalScope();
            }
        }

        /// <remarks>
        /// The interesting part in the following method is the support for exception filters.
        /// === Example:
        ///
        /// try
        /// {
        ///    TryBlock
        /// }
        /// catch (ExceptionType ex) when (Condition)
        /// {
        ///    Handler
        /// }
        ///
        /// gets emitted as something like ===>
        ///
        /// Try
        ///     TryBlock
        /// Filter
        ///     var tmp = Pop() as {ExceptionType}
        ///     if (tmp == null)
        ///     {
        ///         Push 0
        ///     }
        ///     else
        ///     {
        ///         ex = tmp
        ///         Push Condition ? 1 : 0
        ///     }
        /// End Filter // leaves 1 or 0 on the stack
        /// Catch      // gets called after finalization of nested exception frames if condition above produced 1
        ///     Pop    // CLR pushes the exception object again
        ///     variable ex can be used here
        ///     Handler
        /// EndCatch
        ///
        /// When evaluating `Condition` requires additional statements be executed first, those
        /// statements are stored in `catchBlock.ExceptionFilterPrologueOpt` and emitted before the condition.
        /// </remarks>
        private void EmitCatchBlock(BoundCatchBlock catchBlock)
        {
            object typeCheckFailedLabel = null;
#if DEBUG
            int currentCatchFilterLevel = _inCatchFilterLevel;
#endif

            _builder.AdjustStack(1); // Account for exception on the stack.

            // Open appropriate exception handler scope. (Catch or Filter)
            // if it is a Filter, emit prologue that checks if the type on the stack
            // converts to what we want.
            if (catchBlock.ExceptionFilterOpt == null)
            {
                var exceptionType = ((object)catchBlock.ExceptionTypeOpt != null) ?
                    _module.Translate(catchBlock.ExceptionTypeOpt, catchBlock.Syntax, _diagnostics.DiagnosticBag) :
                    _module.GetSpecialType(SpecialType.System_Object, catchBlock.Syntax, _diagnostics.DiagnosticBag);

                _builder.OpenLocalScope(ScopeType.Catch, exceptionType);

                RecordAsyncCatchHandlerOffset(catchBlock);

                // Dev12 inserts the sequence point on catch clause without a filter, just before
                // the exception object is assigned to the variable.
                //
                // Also in Dev12 the exception variable scope span starts right after the stloc instruction and
                // ends right before leave instruction. So when stopped at the sequence point Dev12 inserts,
                // the exception variable is not visible.
                if (_emitPdbSequencePoints)
                {
                    var syntax = catchBlock.Syntax as CatchClauseSyntax;
                    if (syntax != null)
                    {
                        TextSpan spSpan;
                        var declaration = syntax.Declaration;

                        if (declaration == null)
                        {
                            spSpan = syntax.CatchKeyword.Span;
                        }
                        else
                        {
                            spSpan = TextSpan.FromBounds(syntax.SpanStart, syntax.Declaration.Span.End);
                        }

                        this.EmitSequencePoint(catchBlock.SyntaxTree, spSpan);
                    }
                }
            }
            else
            {
                _builder.OpenLocalScope(ScopeType.Filter);
                _inCatchFilterLevel++;

                RecordAsyncCatchHandlerOffset(catchBlock);

                // Filtering starts with simulating regular catch through a
                // type check. If this is not our type then we are done.
                var typeCheckPassedLabel = new object();
                typeCheckFailedLabel = new object();

                if ((object)catchBlock.ExceptionTypeOpt != null)
                {
                    var exceptionType = _module.Translate(catchBlock.ExceptionTypeOpt, catchBlock.Syntax, _diagnostics.DiagnosticBag);

                    _builder.EmitOpCode(ILOpCode.Isinst);
                    _builder.EmitToken(exceptionType, catchBlock.Syntax);
                    _builder.EmitOpCode(ILOpCode.Dup);
                    _builder.EmitBranch(ILOpCode.Brtrue, typeCheckPassedLabel);
                    _builder.EmitOpCode(ILOpCode.Pop);
                    _builder.EmitIntConstant(0);
                    _builder.EmitBranch(ILOpCode.Br, typeCheckFailedLabel);
                }
                else
                {
                    // no formal exception type means we always pass the check
                }

                _builder.MarkLabel(typeCheckPassedLabel);
            }

            foreach (var local in catchBlock.Locals)
            {
                var declaringReferences = local.DeclaringSyntaxReferences;
                var localSyntax = !declaringReferences.IsEmpty ? (CSharpSyntaxNode)declaringReferences[0].GetSyntax() : catchBlock.Syntax;
                DefineLocal(local, localSyntax);
            }

            var exceptionSourceOpt = catchBlock.ExceptionSourceOpt;
            if (exceptionSourceOpt != null)
            {
                // here we have our exception on the stack in a form of a reference type (O)
                // it means that we have to "unbox" it before storing to the local
                // if exception's type is a generic type parameter.
                if (!exceptionSourceOpt.Type.IsVerifierReference())
                {
                    Debug.Assert(exceptionSourceOpt.Type.IsTypeParameter()); // only expecting type parameters
                    _builder.EmitOpCode(ILOpCode.Unbox_any);
                    EmitSymbolToken(exceptionSourceOpt.Type, exceptionSourceOpt.Syntax);
                }

                BoundExpression exceptionSource = exceptionSourceOpt;
                while (exceptionSource.Kind == BoundKind.Sequence)
                {
                    var seq = (BoundSequence)exceptionSource;
                    Debug.Assert(seq.Locals.IsDefaultOrEmpty);
                    EmitSideEffects(seq);
                    exceptionSource = seq.Value;
                }

                switch (exceptionSource.Kind)
                {
                    case BoundKind.Local:
                        var exceptionSourceLocal = (BoundLocal)exceptionSource;
                        Debug.Assert(exceptionSourceLocal.LocalSymbol.RefKind == RefKind.None);
                        if (!IsStackLocal(exceptionSourceLocal.LocalSymbol))
                        {
                            _builder.EmitLocalStore(GetLocal(exceptionSourceLocal));
                        }

                        break;

                    case BoundKind.FieldAccess:
                        var left = (BoundFieldAccess)exceptionSource;
                        Debug.Assert(!left.FieldSymbol.IsStatic, "Not supported");
                        Debug.Assert(!left.ReceiverOpt.Type.IsTypeParameter());
                        Debug.Assert(left.FieldSymbol.RefKind == RefKind.None);

                        var stateMachineField = left.FieldSymbol as StateMachineFieldSymbol;
                        if (((object)stateMachineField != null) && (stateMachineField.SlotIndex >= 0))
                        {
                            _builder.DefineUserDefinedStateMachineHoistedLocal(stateMachineField.SlotIndex);
                        }

                        // When assigning to a field
                        // we need to push param address below the exception
                        var temp = AllocateTemp(exceptionSource.Type, exceptionSource.Syntax);
                        _builder.EmitLocalStore(temp);

                        var receiverTemp = EmitReceiverRef(left.ReceiverOpt, AddressKind.Writeable);
                        Debug.Assert(receiverTemp == null);

                        _builder.EmitLocalLoad(temp);
                        FreeTemp(temp);

                        EmitFieldStore(left, refAssign: false);
                        break;

                    default:
                        throw ExceptionUtilities.UnexpectedValue(exceptionSource.Kind);
                }
            }
            else
            {
                _builder.EmitOpCode(ILOpCode.Pop);
            }

            if (catchBlock.ExceptionFilterPrologueOpt != null)
            {
                Debug.Assert(_builder.IsStackEmpty);
                EmitStatements(catchBlock.ExceptionFilterPrologueOpt.Statements);
            }

            // Emit the actual filter expression, if we have one, and normalize
            // results.
            if (catchBlock.ExceptionFilterOpt != null)
            {
                EmitCondExpr(catchBlock.ExceptionFilterOpt, true);
                // Normalize the return value because values other than 0 or 1
                // produce unspecified results.
                _builder.EmitIntConstant(0);
                _builder.EmitOpCode(ILOpCode.Cgt_un);
                _builder.MarkLabel(typeCheckFailedLabel);

                // Now we are starting the actual handler
                _builder.MarkFilterConditionEnd();
                _inCatchFilterLevel--;

                // Pop the exception; it should have already been stored to the
                // variable by the filter.
                _builder.EmitOpCode(ILOpCode.Pop);
            }

#if DEBUG
            Debug.Assert(currentCatchFilterLevel == _inCatchFilterLevel);
#endif
            EmitBlock(catchBlock.Body);

            _builder.CloseLocalScope();
        }

        private void RecordAsyncCatchHandlerOffset(BoundCatchBlock catchBlock)
        {
            if (catchBlock.IsSynthesizedAsyncCatchAll)
            {
                Debug.Assert(_asyncCatchHandlerOffset < 0); // only one expected
                _asyncCatchHandlerOffset = _builder.AllocateILMarker();
            }
        }

        private void EmitSwitchDispatch(BoundSwitchDispatch dispatch)
        {
            // Switch expression must have a valid switch governing type
            Debug.Assert((object)dispatch.Expression.Type != null);
            Debug.Assert(dispatch.Expression.Type.IsValidV6SwitchGoverningType() || dispatch.Expression.Type.IsSpanOrReadOnlySpanChar());

            // We must have rewritten nullable switch expression into non-nullable constructs.
            Debug.Assert(!dispatch.Expression.Type.IsNullableType());

            // This must be used only for nontrivial dispatches.
            Debug.Assert(dispatch.Cases.Any());

            EmitSwitchHeader(
                dispatch.Expression,
                dispatch.Cases.Select(p => new KeyValuePair<ConstantValue, object>(p.value, p.label)).ToArray(),
                dispatch.DefaultLabel,
                dispatch.LengthBasedStringSwitchDataOpt);
        }

        private void EmitSwitchHeader(
            BoundExpression expression,
            KeyValuePair<ConstantValue, object>[] switchCaseLabels,
            LabelSymbol fallThroughLabel,
            LengthBasedStringSwitchData lengthBasedSwitchStringJumpTableOpt)
        {
            Debug.Assert(expression.ConstantValueOpt == null);
            Debug.Assert((object)expression.Type != null &&
                (expression.Type.IsValidV6SwitchGoverningType() || expression.Type.IsSpanOrReadOnlySpanChar()));
            Debug.Assert(switchCaseLabels.Length > 0);

            Debug.Assert(switchCaseLabels != null || lengthBasedSwitchStringJumpTableOpt != null);
            LocalDefinition temp = null;
            LocalOrParameter key;
            BoundSequence sequence = null;

            if (expression.Kind == BoundKind.Sequence)
            {
                sequence = (BoundSequence)expression;
                DefineLocals(sequence);
                EmitSideEffects(sequence);
                expression = sequence.Value;
            }

            if (expression.Kind == BoundKind.SequencePointExpression)
            {
                var sequencePointExpression = (BoundSequencePointExpression)expression;
                EmitSequencePoint(sequencePointExpression);
                expression = sequencePointExpression.Expression;
            }

            switch (expression.Kind)
            {
                case BoundKind.Local:
                    var local = ((BoundLocal)expression).LocalSymbol;
                    if (local.RefKind == RefKind.None && !IsStackLocal(local))
                    {
                        key = this.GetLocal(local);
                        break;
                    }
                    goto default;

                case BoundKind.Parameter:
                    var parameter = (BoundParameter)expression;
                    if (parameter.ParameterSymbol.RefKind == RefKind.None)
                    {
                        key = ParameterSlot(parameter);
                        break;
                    }
                    goto default;

                default:
                    EmitExpression(expression, true);
                    temp = AllocateTemp(expression.Type, expression.Syntax);
                    _builder.EmitLocalStore(temp);
                    key = temp;
                    break;
            }

            Debug.Assert(lengthBasedSwitchStringJumpTableOpt is null ||
                expression.Type.SpecialType == SpecialType.System_String || expression.Type.IsSpanOrReadOnlySpanChar());

            // Emit switch jump table
            if (expression.Type.SpecialType == SpecialType.System_String || expression.Type.IsSpanOrReadOnlySpanChar())
            {
                if (lengthBasedSwitchStringJumpTableOpt is null)
                {
                    this.EmitStringSwitchJumpTable(switchCaseLabels, fallThroughLabel, key, expression.Syntax, expression.Type);
                }
                else
                {
                    this.EmitLengthBasedStringSwitchJumpTable(lengthBasedSwitchStringJumpTableOpt, fallThroughLabel, key, expression.Syntax, expression.Type);
                }
            }
            else
            {
                _builder.EmitIntegerSwitchJumpTable(switchCaseLabels, fallThroughLabel, key, expression.Type.EnumUnderlyingTypeOrSelf().PrimitiveTypeCode, expression.Syntax);
            }

            if (temp != null)
            {
                FreeTemp(temp);
            }

            if (sequence != null)
            {
                // sequence was used as a value, can release all its locals.
                FreeLocals(sequence);
            }
        }

#nullable enable
        private void EmitLengthBasedStringSwitchJumpTable(
            LengthBasedStringSwitchData lengthBasedSwitchData,
            LabelSymbol fallThroughLabel,
            LocalOrParameter keyTemp,
            SyntaxNode syntaxNode,
            TypeSymbol keyType)
        {
            // For the LengthJumpTable, emit:
            //   if (keyTemp is null)
            //     goto nullCaseLabel; OR goto fallThroughLabel;
            //
            //   var lengthTmp = keyTemp.Length;
            //   switch dispatch on lengthTemp using fallThroughLabel and cases:
            //     lengthConstant -> corresponding label (may be the label to a CharJumpTable, or to a StringJumpTable in 1-length scenario, or in 0-length scenario, a final case label)
            //
            //   var charTemp;
            //
            // For each CharJumpTable, emit:
            //   label for CharJumpTable:
            //   charTemp = keyTemp[selectedCharPosition];
            //   switch dispatch on charTemp using fallThroughLabel and cases:
            //     charConstant -> corresponding label (may be the label for a StringJumpTable or, in 1-length scenario, a final case label)
            //
            // For each StringJumpTable label, emit:
            //   label for StringJumpTable:
            //   switch dispatch on keyTemp using fallThroughLabel and cases:
            //     stringConstant -> corresponding label

            bool isSpan = keyType.IsSpanChar();
            bool isReadOnlySpan = keyType.IsReadOnlySpanChar();
            bool isSpanOrReadOnlySpan = isSpan || isReadOnlySpan;
            var indexerRef = GetIndexerRef(syntaxNode, keyType, isReadOnlySpan, isSpanOrReadOnlySpan);
            var lengthMethodRef = GetLengthMethodRef(syntaxNode, keyType, isReadOnlySpan, isSpanOrReadOnlySpan);
            Debug.Assert(indexerRef is not null);
            Debug.Assert(lengthMethodRef is not null);

            emitLengthDispatch(lengthBasedSwitchData, keyTemp, fallThroughLabel, syntaxNode);
            emitCharDispatches(lengthBasedSwitchData, keyTemp, fallThroughLabel, syntaxNode);
            emitFinalDispatches(lengthBasedSwitchData, keyTemp, keyType, fallThroughLabel, syntaxNode);

            return;

            void emitLengthDispatch(LengthBasedStringSwitchData lengthBasedSwitchInfo, LocalOrParameter keyTemp, LabelSymbol fallThroughLabel, SyntaxNode syntaxNode)
            {
                if (!isSpanOrReadOnlySpan)
                {
                    // if (keyTemp is null)
                    //   goto nullCaseLabel; OR goto fallThroughLabel;
                    _builder.EmitLoad(keyTemp);
                    _builder.EmitBranch(ILOpCode.Brfalse, lengthBasedSwitchInfo.LengthBasedJumpTable.NullCaseLabel ?? fallThroughLabel, ILOpCode.Brtrue);
                }

                // var stringLength = keyTemp.Length;
                var int32Type = Binder.GetSpecialType(_module.Compilation, SpecialType.System_Int32, syntaxNode, _diagnostics);
                var stringLength = AllocateTemp(int32Type, syntaxNode);
                if (isSpanOrReadOnlySpan)
                {
                    _builder.EmitLoadAddress(keyTemp);
                }
                else
                {
                    _builder.EmitLoad(keyTemp);
                }
                _builder.EmitOpCode(ILOpCode.Call, stackAdjustment: 0);
                emitMethodRef(lengthMethodRef);
                _builder.EmitLocalStore(stringLength);

                // switch dispatch on lengthTemp using fallThroughLabel and cases:
                //   lengthConstant -> corresponding label
                _builder.EmitIntegerSwitchJumpTable(
                    lengthBasedSwitchInfo.LengthBasedJumpTable.LengthCaseLabels.Select(p => new KeyValuePair<ConstantValue, object>(ConstantValue.Create(p.value), p.label)).ToArray(),
                    fallThroughLabel,
                    stringLength,
                    int32Type.PrimitiveTypeCode,
                    syntaxNode);

                FreeTemp(stringLength);
            }

            void emitCharDispatches(LengthBasedStringSwitchData lengthBasedSwitchInfo, LocalOrParameter keyTemp, LabelSymbol fallThroughLabel, SyntaxNode syntaxNode)
            {
                var charType = Binder.GetSpecialType(_module.Compilation, SpecialType.System_Char, syntaxNode, _diagnostics);
                var charTemp = AllocateTemp(charType, syntaxNode);

                foreach (var charJumpTable in lengthBasedSwitchInfo.CharBasedJumpTables)
                {
                    // label for CharJumpTable:
                    _builder.MarkLabel(charJumpTable.Label);

                    //   charTemp = keyTemp[selectedCharPosition];
                    if (isSpanOrReadOnlySpan)
                    {
                        _builder.EmitLoadAddress(keyTemp);
                    }
                    else
                    {
                        _builder.EmitLoad(keyTemp);
                    }
                    _builder.EmitIntConstant(charJumpTable.SelectedCharPosition);
                    _builder.EmitOpCode(ILOpCode.Call, stackAdjustment: -1);
                    emitMethodRef(indexerRef);
                    if (isSpanOrReadOnlySpan)
                    {
                        _builder.EmitOpCode(ILOpCode.Ldind_u2);
                    }
                    _builder.EmitLocalStore(charTemp);

                    // switch dispatch on charTemp using fallThroughLabel and cases:
                    //   charConstant -> corresponding label
                    _builder.EmitIntegerSwitchJumpTable(
                        charJumpTable.CharCaseLabels.Select(p => new KeyValuePair<ConstantValue, object>(ConstantValue.Create(p.value), p.label)).ToArray(),
                        fallThroughLabel,
                        charTemp,
                        charType.PrimitiveTypeCode,
                        syntaxNode);
                }

                FreeTemp(charTemp);
            }

            void emitFinalDispatches(LengthBasedStringSwitchData lengthBasedSwitchInfo, LocalOrParameter keyTemp, TypeSymbol keyType, LabelSymbol fallThroughLabel, SyntaxNode syntaxNode)
            {
                foreach (var stringJumpTable in lengthBasedSwitchInfo.StringBasedJumpTables)
                {
                    // label for StringJumpTable:
                    _builder.MarkLabel(stringJumpTable.Label);

                    // switch dispatch on keyTemp using fallThroughLabel and cases:
                    //   stringConstant -> corresponding label
                    EmitStringSwitchJumpTable(
                        stringJumpTable.StringCaseLabels.Select(p => new KeyValuePair<ConstantValue, object>(ConstantValue.Create(p.value), p.label)).ToArray(),
                        fallThroughLabel, keyTemp, syntaxNode, keyType);
                }
            }

            void emitMethodRef(Microsoft.Cci.IMethodReference lengthMethodRef)
            {
                var diag = DiagnosticBag.GetInstance();
                _builder.EmitToken(lengthMethodRef, syntaxNode: null);
                Debug.Assert(diag.IsEmptyWithoutResolution);
                diag.Free();
            }
        }
#nullable disable

        private void EmitStringSwitchJumpTable(
            KeyValuePair<ConstantValue, object>[] switchCaseLabels,
            LabelSymbol fallThroughLabel,
            LocalOrParameter key,
            SyntaxNode syntaxNode,
            TypeSymbol keyType)
        {
            var isSpan = keyType.IsSpanChar();
            var isReadOnlySpan = keyType.IsReadOnlySpanChar();
            var isSpanOrReadOnlySpan = isSpan || isReadOnlySpan;

            LocalDefinition keyHash = null;

            // Condition is necessary, but not sufficient (e.g. might be missing a special or well-known member).
            if (SwitchStringJumpTableEmitter.ShouldGenerateHashTableSwitch(switchCaseLabels.Length))
            {
                var privateImplClass = _module.GetPrivateImplClass(syntaxNode, _diagnostics.DiagnosticBag).PrivateImplementationDetails;
                Cci.IReference stringHashMethodRef = privateImplClass.GetMethod(
                    isSpanOrReadOnlySpan
                        ? isReadOnlySpan
                            ? PrivateImplementationDetails.SynthesizedReadOnlySpanHashFunctionName
                            : PrivateImplementationDetails.SynthesizedSpanHashFunctionName
                        : PrivateImplementationDetails.SynthesizedStringHashFunctionName);

                // Heuristics and well-known member availability determine the existence
                // of this helper.  Rather than reproduce that (language-specific) logic here,
                // we simply check for the information we really want - whether the helper is
                // available.
                if (stringHashMethodRef != null)
                {
                    // static uint ComputeStringHash(string s)
                    // pop 1 (s)
                    // push 1 (uint return value)
                    // stackAdjustment = (pushCount - popCount) = 0

                    _builder.EmitLoad(key);
                    _builder.EmitOpCode(ILOpCode.Call, stackAdjustment: 0);
                    _builder.EmitToken(stringHashMethodRef, syntaxNode);

                    var UInt32Type = Binder.GetSpecialType(_module.Compilation, SpecialType.System_UInt32, syntaxNode, _diagnostics);
                    keyHash = AllocateTemp(UInt32Type, syntaxNode);

                    _builder.EmitLocalStore(keyHash);
                }
            }

            Cci.IMethodReference stringEqualityMethodRef = null;

            Cci.IMethodReference sequenceEqualsMethodRef = null;
            Cci.IMethodReference asSpanMethodRef = null;

            if (isSpanOrReadOnlySpan)
            {
                // Binder.ConvertPatternExpression() has checked for these well-known members.
                var sequenceEqualsTMethod = (MethodSymbol)Binder.GetWellKnownTypeMember(_module.Compilation,
                    (isReadOnlySpan
                    ? WellKnownMember.System_MemoryExtensions__SequenceEqual_ReadOnlySpan_T
                    : WellKnownMember.System_MemoryExtensions__SequenceEqual_Span_T),
                    _diagnostics, syntax: syntaxNode);
                Debug.Assert(sequenceEqualsTMethod != null && !sequenceEqualsTMethod.HasUseSiteError);
                var sequenceEqualsCharMethod = sequenceEqualsTMethod.Construct(Binder.GetSpecialType(_module.Compilation, SpecialType.System_Char, syntaxNode, _diagnostics));
                sequenceEqualsMethodRef = _module.Translate(sequenceEqualsCharMethod, null, _diagnostics.DiagnosticBag);

                var asSpanMethod = (MethodSymbol)Binder.GetWellKnownTypeMember(_module.Compilation, WellKnownMember.System_MemoryExtensions__AsSpan_String, _diagnostics, syntax: syntaxNode);
                Debug.Assert(asSpanMethod != null && !asSpanMethod.HasUseSiteError);
                asSpanMethodRef = _module.Translate(asSpanMethod, null, _diagnostics.DiagnosticBag);
            }
            else
            {
                var stringEqualityMethod = _module.Compilation.GetSpecialTypeMember(SpecialMember.System_String__op_Equality) as MethodSymbol;
                Debug.Assert(stringEqualityMethod != null && !stringEqualityMethod.HasUseSiteError);
                stringEqualityMethodRef = _module.Translate(stringEqualityMethod, syntaxNode, _diagnostics.DiagnosticBag);
            }

            Microsoft.Cci.IMethodReference lengthMethodRef = GetLengthMethodRef(syntaxNode, keyType, isReadOnlySpan, isSpanOrReadOnlySpan);

            SwitchStringJumpTableEmitter.EmitStringCompareAndBranch emitStringCondBranchDelegate =
                (keyArg, stringConstant, targetLabel) =>
                {
                    if (stringConstant == ConstantValue.Null)
                    {
                        Debug.Assert(!isSpanOrReadOnlySpan);

                        // if (key == null)
                        //      goto targetLabel
                        _builder.EmitLoad(keyArg);
                        _builder.EmitBranch(ILOpCode.Brfalse, targetLabel, ILOpCode.Brtrue);
                    }
                    else if (stringConstant.StringValue.Length == 0 && lengthMethodRef != null)
                    {
                        // if (key != null && key.Length == 0)
                        //      goto targetLabel

                        object skipToNext = new object();
                        if (isSpanOrReadOnlySpan)
                        {
                            // The caller ensures that the key is not byref, and is not a stack local
                            _builder.EmitLoadAddress(keyArg);
                        }
                        else
                        {
                            _builder.EmitLoad(keyArg);
                            _builder.EmitBranch(ILOpCode.Brfalse, skipToNext, ILOpCode.Brtrue);

                            _builder.EmitLoad(keyArg);
                        }

                        // Stack: key --> length
                        _builder.EmitOpCode(ILOpCode.Call, 0);
                        var diag = DiagnosticBag.GetInstance();
                        _builder.EmitToken(lengthMethodRef, null);
                        Debug.Assert(diag.IsEmptyWithoutResolution);
                        diag.Free();

                        _builder.EmitBranch(ILOpCode.Brfalse, targetLabel, ILOpCode.Brtrue);
                        _builder.MarkLabel(skipToNext);
                    }
                    else
                    {
                        if (isSpanOrReadOnlySpan)
                        {
                            this.EmitCharCompareAndBranch(key, syntaxNode, stringConstant, targetLabel, sequenceEqualsMethodRef, asSpanMethodRef);
                        }
                        else
                        {
                            this.EmitStringCompareAndBranch(key, syntaxNode, stringConstant, targetLabel, stringEqualityMethodRef);
                        }
                    }
                };

            _builder.EmitStringSwitchJumpTable(
                syntaxNode,
                caseLabels: switchCaseLabels,
                fallThroughLabel: fallThroughLabel,
                key: key,
                keyHash: keyHash,
                emitStringCondBranchDelegate: emitStringCondBranchDelegate,
                computeStringHashcodeDelegate: SynthesizedStringSwitchHashMethod.ComputeStringHash);

            if (keyHash != null)
            {
                FreeTemp(keyHash);
            }
        }

#nullable enable
        private Cci.IMethodReference? GetLengthMethodRef(SyntaxNode syntaxNode, TypeSymbol keyType, bool isReadOnlySpan, bool isSpanOrReadOnlySpan)
        {
            if (isSpanOrReadOnlySpan)
            {
                var spanTLengthMethod = (MethodSymbol)Binder.GetWellKnownTypeMember(_module.Compilation,
                    (isReadOnlySpan ? WellKnownMember.System_ReadOnlySpan_T__get_Length : WellKnownMember.System_Span_T__get_Length),
                    _diagnostics, syntax: syntaxNode);

                Debug.Assert(spanTLengthMethod != null && !spanTLengthMethod.HasUseSiteError);
                var spanCharLengthMethod = spanTLengthMethod.AsMember((NamedTypeSymbol)keyType);
                return _module.Translate(spanCharLengthMethod, syntaxNode, _diagnostics.DiagnosticBag);
            }
            else
            {
                var stringLengthMethod = _module.Compilation.GetSpecialTypeMember(SpecialMember.System_String__Length) as MethodSymbol;
                if (stringLengthMethod != null && !stringLengthMethod.HasUseSiteError)
                {
                    return _module.Translate(stringLengthMethod, syntaxNode, _diagnostics.DiagnosticBag);
                }
            }

            return null;
        }

        private Microsoft.Cci.IMethodReference? GetIndexerRef(SyntaxNode syntaxNode, TypeSymbol keyType, bool isReadOnlySpan, bool isSpanOrReadOnlySpan)
        {
            if (isSpanOrReadOnlySpan)
            {
                var spanTIndexerMethod = (MethodSymbol)Binder.GetWellKnownTypeMember(_module.Compilation,
                    (isReadOnlySpan ? WellKnownMember.System_ReadOnlySpan_T__get_Item : WellKnownMember.System_Span_T__get_Item),
                    _diagnostics, syntax: syntaxNode);

                if (spanTIndexerMethod != null && !spanTIndexerMethod.HasUseSiteError)
                {
                    var spanCharLengthMethod = spanTIndexerMethod.AsMember((NamedTypeSymbol)keyType);
                    return _module.Translate(spanCharLengthMethod, null, _diagnostics.DiagnosticBag);
                }
            }
            else
            {
                var stringCharsIndexer = _module.Compilation.GetSpecialTypeMember(SpecialMember.System_String__Chars) as MethodSymbol;
                if (stringCharsIndexer != null && !stringCharsIndexer.HasUseSiteError)
                {
                    return _module.Translate(stringCharsIndexer, syntaxNode, _diagnostics.DiagnosticBag);
                }
            }

            return null;
        }
#nullable disable

        /// <summary>
        /// Delegate to emit string compare call and conditional branch based on the compare result.
        /// </summary>
        /// <param name="key">Key to compare</param>
        /// <param name="syntaxNode">Node for diagnostics.</param>
        /// <param name="stringConstant">Case constant to compare the key against</param>
        /// <param name="targetLabel">Target label to branch to if key = stringConstant</param>
        /// <param name="stringEqualityMethodRef">String equality method</param>
        private void EmitStringCompareAndBranch(LocalOrParameter key, SyntaxNode syntaxNode, ConstantValue stringConstant, object targetLabel, Microsoft.Cci.IReference stringEqualityMethodRef)
        {
            // Emit compare and branch:

            // if (key == stringConstant)
            //      goto targetLabel;

            Debug.Assert(stringEqualityMethodRef != null);

#if DEBUG
            var assertDiagnostics = DiagnosticBag.GetInstance();
            Debug.Assert(stringEqualityMethodRef == _module.Translate((MethodSymbol)_module.Compilation.GetSpecialTypeMember(SpecialMember.System_String__op_Equality), (CSharpSyntaxNode)syntaxNode, assertDiagnostics));
            assertDiagnostics.Free();
#endif

            // static bool String.Equals(string a, string b)
            // pop 2 (a, b)
            // push 1 (bool return value)

            // stackAdjustment = (pushCount - popCount) = -1

            _builder.EmitLoad(key);
            _builder.EmitConstantValue(stringConstant, syntaxNode);
            _builder.EmitOpCode(ILOpCode.Call, stackAdjustment: -1);
            _builder.EmitToken(stringEqualityMethodRef, syntaxNode);

            // Branch to targetLabel if String.Equals returned true.
            _builder.EmitBranch(ILOpCode.Brtrue, targetLabel, ILOpCode.Brfalse);
        }

        /// <summary>
        /// Delegate to emit ReadOnlySpanChar compare with string and conditional branch based on the compare result.
        /// </summary>
        /// <param name="key">Key to compare</param>
        /// <param name="syntaxNode">Node for diagnostics.</param>
        /// <param name="stringConstant">Case constant to compare the key against</param>
        /// <param name="targetLabel">Target label to branch to if key = stringConstant</param>
        /// <param name="sequenceEqualsRef">String equality method</param>
        private void EmitCharCompareAndBranch(LocalOrParameter key, SyntaxNode syntaxNode, ConstantValue stringConstant, object targetLabel, Cci.IReference sequenceEqualsRef, Cci.IReference asSpanRef)
        {
            // Emit compare and branch:

            // if (key.SequenceEqual(stringConstant.AsSpan()))
            //      goto targetLabel;

            Debug.Assert(sequenceEqualsRef != null);
            Debug.Assert(asSpanRef != null);

            _builder.EmitLoad(key);
            _builder.EmitConstantValue(stringConstant, syntaxNode);
            _builder.EmitOpCode(ILOpCode.Call, stackAdjustment: 0);
            _builder.EmitToken(asSpanRef, syntaxNode);
            _builder.EmitOpCode(ILOpCode.Call, stackAdjustment: -1);
            _builder.EmitToken(sequenceEqualsRef, syntaxNode);

            // Branch to targetLabel if SequenceEquals returned true.
            _builder.EmitBranch(ILOpCode.Brtrue, targetLabel, ILOpCode.Brfalse);
        }

        /// <summary>
        /// Gets already declared and initialized local.
        /// </summary>
        private LocalDefinition GetLocal(BoundLocal localExpression)
        {
            var symbol = localExpression.LocalSymbol;
            return GetLocal(symbol);
        }

        private LocalDefinition GetLocal(LocalSymbol symbol)
        {
            return _builder.LocalSlotManager.GetLocal(symbol);
        }

        private LocalDefinition DefineLocal(LocalSymbol local, SyntaxNode syntaxNode)
        {
            var dynamicTransformFlags = !local.IsCompilerGenerated && local.Type.ContainsDynamic() ?
                CSharpCompilation.DynamicTransformsEncoder.Encode(local.Type, RefKind.None, 0) :
                ImmutableArray<bool>.Empty;
            var tupleElementNames = !local.IsCompilerGenerated && local.Type.ContainsTupleNames() ?
                CSharpCompilation.TupleNamesEncoder.Encode(local.Type) :
                ImmutableArray<string>.Empty;

            if (local.IsConst)
            {
                Debug.Assert(local.HasConstantValue);
                MetadataConstant compileTimeValue = _module.CreateConstant(local.Type, local.ConstantValue, syntaxNode, _diagnostics.DiagnosticBag);
                LocalConstantDefinition localConstantDef = new LocalConstantDefinition(
                    local.Name,
                    local.GetFirstLocationOrNone(),
                    compileTimeValue,
                    dynamicTransformFlags: dynamicTransformFlags,
                    tupleElementNames: tupleElementNames);
                _builder.AddLocalConstantToScope(localConstantDef);
                return null;
            }

            if (IsStackLocal(local))
            {
                return null;
            }

            LocalSlotConstraints constraints;
            Cci.ITypeReference translatedType;

            if (local.DeclarationKind == LocalDeclarationKind.FixedVariable && local.IsPinned) // Excludes pointer local and string local in fixed string case.
            {
                Debug.Assert(local.RefKind == RefKind.None);
                Debug.Assert(local.TypeWithAnnotations.Type.IsPointerType());

                constraints = LocalSlotConstraints.ByRef | LocalSlotConstraints.Pinned;
                PointerTypeSymbol pointerType = (PointerTypeSymbol)local.Type;
                TypeSymbol pointedAtType = pointerType.PointedAtType;

                // We can't declare a reference to void, so if the pointed-at type is void, use native int
                // (represented here by IntPtr) instead.
                translatedType = pointedAtType.IsVoidType()
                    ? _module.GetSpecialType(SpecialType.System_IntPtr, syntaxNode, _diagnostics.DiagnosticBag)
                    : _module.Translate(pointedAtType, syntaxNode, _diagnostics.DiagnosticBag);
            }
            else
            {
                constraints = (local.IsPinned ? LocalSlotConstraints.Pinned : LocalSlotConstraints.None) |
                    (local.RefKind != RefKind.None ? LocalSlotConstraints.ByRef : LocalSlotConstraints.None);
                translatedType = _module.Translate(local.Type, syntaxNode, _diagnostics.DiagnosticBag);
            }

            // Even though we don't need the token immediately, we will need it later when signature for the local is emitted.
            // Also, requesting the token has side-effect of registering types used, which is critical for embedded types (NoPia, VBCore, etc).
            _module.GetFakeSymbolTokenForIL(translatedType, syntaxNode, _diagnostics.DiagnosticBag);

            LocalDebugId localId;
            var name = GetLocalDebugName(local, out localId);

            var localDef = _builder.LocalSlotManager.DeclareLocal(
                type: translatedType,
                symbol: local,
                name: name,
                kind: local.SynthesizedKind,
                id: localId,
                pdbAttributes: local.SynthesizedKind.PdbAttributes(),
                constraints: constraints,
                dynamicTransformFlags: dynamicTransformFlags,
                tupleElementNames: tupleElementNames,
                isSlotReusable: local.SynthesizedKind.IsSlotReusable(_ilEmitStyle != ILEmitStyle.Release));

            // If named, add it to the local debug scope.
            if (localDef.Name != null &&
                !(local.SynthesizedKind == SynthesizedLocalKind.UserDefined &&
                // Visibility scope of such locals is represented by BoundScope node.
                (local.ScopeDesignatorOpt?.Kind() is SyntaxKind.SwitchSection or SyntaxKind.SwitchExpressionArm)))
            {
                _builder.AddLocalToScope(localDef);
            }

            return localDef;
        }

        /// <summary>
        /// Gets the name and id of the local that are going to be generated into the debug metadata.
        /// </summary>
        private string GetLocalDebugName(ILocalSymbolInternal local, out LocalDebugId localId)
        {
            localId = LocalDebugId.None;

            if (local.IsImportedFromMetadata)
            {
                return local.Name;
            }

            var localKind = local.SynthesizedKind;

            // only user-defined locals should be named during lowering:
            Debug.Assert((local.Name == null) == (localKind != SynthesizedLocalKind.UserDefined));

            // Generating debug names for instrumentation payloads should be allowed, as described in https://github.com/dotnet/roslyn/issues/11024.
            // For now, skip naming locals generated by instrumentation as they might not have a local syntax offset.
            // Locals generated by instrumentation might exist in methods which do not contain a body (auto property initializers).
            if (!localKind.IsLongLived() || localKind == SynthesizedLocalKind.InstrumentationPayload)
            {
                return null;
            }

            if (_ilEmitStyle == ILEmitStyle.Debug)
            {
                var syntax = local.GetDeclaratorSyntax();
                int syntaxOffset = _method.CalculateLocalSyntaxOffset(LambdaUtilities.GetDeclaratorPosition(syntax), syntax.SyntaxTree);
                int ordinal = _synthesizedLocalOrdinals.AssignLocalOrdinal(localKind, syntaxOffset);

                // user-defined locals should have 0 ordinal:
                Debug.Assert(ordinal == 0 || localKind != SynthesizedLocalKind.UserDefined);

                localId = new LocalDebugId(syntaxOffset, ordinal);
            }

            return local.Name ?? GeneratedNames.MakeSynthesizedLocalName(localKind, ref _uniqueNameId);
        }

        private bool IsSlotReusable(LocalSymbol local)
        {
            return local.SynthesizedKind.IsSlotReusable(_ilEmitStyle != ILEmitStyle.Release);
        }

        /// <summary>
        /// Releases a local.
        /// </summary>
        private void FreeLocal(LocalSymbol local)
        {
            // TODO: releasing named locals is NYI.
            if (local.Name == null && IsSlotReusable(local) && !IsStackLocal(local))
            {
                _builder.LocalSlotManager.FreeLocal(local);
            }
        }

        /// <summary>
        /// Allocates a temp without identity.
        /// </summary>
        private LocalDefinition AllocateTemp(TypeSymbol type, SyntaxNode syntaxNode, LocalSlotConstraints slotConstraints = LocalSlotConstraints.None)
        {
            return _builder.LocalSlotManager.AllocateSlot(
                _module.Translate(type, syntaxNode, _diagnostics.DiagnosticBag),
                slotConstraints);
        }

        /// <summary>
        /// Frees a temp.
        /// </summary>
        private void FreeTemp(LocalDefinition temp)
        {
            _builder.LocalSlotManager.FreeSlot(temp);
        }

        /// <summary>
        /// Frees an optional temp.
        /// </summary>
        private void FreeOptTemp(LocalDefinition temp)
        {
            if (temp != null)
            {
                FreeTemp(temp);
            }
        }

        /// <summary>
        /// Clones all labels used in a finally block.
        /// This allows creating an emittable clone of finally.
        /// It is safe to do because no branches can go in or out of the finally handler.
        /// </summary>
        private class FinallyCloner : BoundTreeRewriterWithStackGuardWithoutRecursionOnTheLeftOfBinaryOperator
        {
            private Dictionary<LabelSymbol, GeneratedLabelSymbol> _labelClones;

            private FinallyCloner() { }

            /// <summary>
            /// The argument is BoundTryStatement (and not a BoundBlock) specifically
            /// to support only Finally blocks where it is guaranteed to not have incoming or leaving branches.
            /// </summary>
            public static BoundBlock MakeFinallyClone(BoundTryStatement node)
            {
                var cloner = new FinallyCloner();
                return (BoundBlock)cloner.Visit(node.FinallyBlockOpt);
            }

            public override BoundNode VisitLabelStatement(BoundLabelStatement node)
            {
                return node.Update(GetLabelClone(node.Label));
            }

            public override BoundNode VisitGotoStatement(BoundGotoStatement node)
            {
                var labelClone = GetLabelClone(node.Label);

                // expressions do not contain labels or branches
                BoundExpression caseExpressionOpt = node.CaseExpressionOpt;
                // expressions do not contain labels or branches
                BoundLabel labelExpressionOpt = node.LabelExpressionOpt;

                return node.Update(labelClone, caseExpressionOpt, labelExpressionOpt);
            }

            public override BoundNode VisitConditionalGoto(BoundConditionalGoto node)
            {
                var labelClone = GetLabelClone(node.Label);

                // expressions do not contain labels or branches
                BoundExpression condition = node.Condition;

                return node.Update(condition, node.JumpIfTrue, labelClone);
            }

            public override BoundNode VisitSwitchDispatch(BoundSwitchDispatch node)
            {
                // expressions do not contain labels or branches
                BoundExpression expression = node.Expression;

                var defaultClone = GetLabelClone(node.DefaultLabel);
                var casesBuilder = ArrayBuilder<(ConstantValue, LabelSymbol)>.GetInstance();
                foreach (var (value, label) in node.Cases)
                {
                    casesBuilder.Add((value, GetLabelClone(label)));
                }

                var lengthBasedSwitchData = node.LengthBasedStringSwitchDataOpt;
                if (lengthBasedSwitchData is not null)
                {
                    // We don't currently produce switch dispatches inside `fault` handler
                    throw ExceptionUtilities.Unreachable();
                }

                return node.Update(expression, casesBuilder.ToImmutableAndFree(), defaultClone, lengthBasedSwitchData);
            }

            public override BoundNode VisitExpressionStatement(BoundExpressionStatement node)
            {
                // expressions do not contain labels or branches
                return node;
            }

            private GeneratedLabelSymbol GetLabelClone(LabelSymbol label)
            {
                var labelClones = _labelClones;
                if (labelClones == null)
                {
                    _labelClones = labelClones = new Dictionary<LabelSymbol, GeneratedLabelSymbol>();
                }

                GeneratedLabelSymbol clone;
                if (!labelClones.TryGetValue(label, out clone))
                {
                    clone = new GeneratedLabelSymbol("cloned_" + label.Name);
                    labelClones.Add(label, clone);
                }

                return clone;
            }
        }
    }
}
