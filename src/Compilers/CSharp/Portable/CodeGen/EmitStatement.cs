// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using Microsoft.CodeAnalysis.CodeGen;
using Microsoft.CodeAnalysis.Collections;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Symbols;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

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

                case BoundKind.SequencePoint:
                    this.EmitSequencePointStatement((BoundSequencePoint)statement);
                    break;

                case BoundKind.SequencePointWithSpan:
                    this.EmitSequencePointStatement((BoundSequencePointWithSpan)statement);
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

                case BoundKind.SwitchStatement:
                    EmitSwitchStatement((BoundSwitchStatement)statement);
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
            BoundExpression expr = node.ExpressionOpt;
            if (expr != null)
            {
                this.EmitExpression(expr, true);

                var exprType = expr.Type;
                // Expression type will be null for "throw null;".
                if (exprType?.TypeKind == TypeKind.TypeParameter)
                {
                    this.EmitBox(exprType, expr.Syntax);
                }
            }

            _builder.EmitThrow(isRethrow: expr == null);
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
            BoundExpression constOp = (condition.Left.ConstantValue != null) ? condition.Left : null;

            if (constOp != null)
            {
                nonConstOp = condition.Right;
            }
            else
            {
                constOp = (condition.Right.ConstantValue != null) ? condition.Right : null;
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
            bool isZero = constOp.ConstantValue.IsDefaultValue;

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
            catch (Exception ex) when (StackGuard.IsInsufficientExecutionStackException(ex))
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

            if (condition.ConstantValue != null)
            {
                bool taken = condition.ConstantValue.IsDefaultValue != sense;

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
                    bool testBothArgs = sense;

                    switch (binOp.OperatorKind.OperatorWithLogical())
                    {
                        case BinaryOperatorKind.LogicalOr:
                            testBothArgs = !testBothArgs;
                            // Fall through
                            goto case BinaryOperatorKind.LogicalAnd;

                        case BinaryOperatorKind.LogicalAnd:
                            if (testBothArgs)
                            {
                                // gotoif(a != sense) fallThrough
                                // gotoif(b == sense) dest
                                // fallThrough:

                                object fallThrough = null;

                                EmitCondBranch(binOp.Left, ref fallThrough, !sense);
                                EmitCondBranch(binOp.Right, ref dest, sense);

                                if (fallThrough != null)
                                {
                                    _builder.MarkLabel(fallThrough);
                                }
                            }
                            else
                            {
                                // gotoif(a == sense) labDest
                                // gotoif(b == sense) labDest

                                EmitCondBranch(binOp.Left, ref dest, sense);
                                condition = binOp.Right;
                                goto oneMoreTime;
                            }
                            return;

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
                            EmitReceiverRef(receiver, isAccessConstrained: false);
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
                            EmitReceiverRef(receiver, isAccessConstrained: false);
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
            FreeLocals(sequence, doNotRelease: null);
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
            var hasLocals = !block.Locals.IsEmpty;

            if (hasLocals)
            {
                _builder.OpenLocalScope();

                foreach (var local in block.Locals)
                {
                    var declaringReferences = local.DeclaringSyntaxReferences;
                    DefineLocal(local, !declaringReferences.IsEmpty ? (CSharpSyntaxNode)declaringReferences[0].GetSyntax() : block.Syntax);
                }
            }

            foreach (var statement in block.Statements)
            {
                EmitStatement(statement);
            }

            if (_indirectReturnState == IndirectReturnState.Needed &&
                IsLastBlockInMethod(block))
            {
                HandleReturn();
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

        private void EmitStateMachineScope(BoundStateMachineScope scope)
        {
            _builder.OpenStateMachineScope();
            foreach (var field in scope.Fields)
            {
                _builder.DefineUserDefinedStateMachineHoistedLocal(field.SlotIndex);
            }

            EmitStatement(scope.Statement);
            _builder.CloseStateMachineScope();
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

            this.EmitExpression(expressionOpt, used: true);

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
                        var returnType = expressionOpt.Type;
                        var byRefType = returnType as ByRefReturnErrorTypeSymbol;
                        if ((object)byRefType != null)
                        {
                            returnType = byRefType.ReferencedType;
                        }
                        _module.Translate(returnType, boundReturnStatement.Syntax, _diagnostics);
                    }
                    _builder.EmitRet(expressionOpt == null);
                }
            }
        }

        private void EmitTryStatement(BoundTryStatement statement, bool emitCatchesOnly = false)
        {
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
        /// </remarks>
        private void EmitCatchBlock(BoundCatchBlock catchBlock)
        {
            object typeCheckFailedLabel = null;


            _builder.AdjustStack(1); // Account for exception on the stack.

            // Open appropriate exception handler scope. (Catch or Filter)
            // if it is a Filter, emit prologue that checks if the type on the stack
            // converts to what we want.
            if (catchBlock.ExceptionFilterOpt == null)
            {
                var exceptionType = ((object)catchBlock.ExceptionTypeOpt != null) ?
                    _module.Translate(catchBlock.ExceptionTypeOpt, catchBlock.Syntax, _diagnostics) :
                    _module.GetSpecialType(SpecialType.System_Object, catchBlock.Syntax, _diagnostics);

                _builder.OpenLocalScope(ScopeType.Catch, exceptionType);

                if (catchBlock.IsSynthesizedAsyncCatchAll)
                {
                    Debug.Assert(_asyncCatchHandlerOffset < 0); // only one expected
                    _asyncCatchHandlerOffset = _builder.AllocateILMarker();
                }

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

                // Filtering starts with simulating regular catch through a 
                // type check. If this is not our type then we are done.
                var typeCheckPassedLabel = new object();
                typeCheckFailedLabel = new object();

                if ((object)catchBlock.ExceptionTypeOpt != null)
                {
                    var exceptionType = _module.Translate(catchBlock.ExceptionTypeOpt, catchBlock.Syntax, _diagnostics);

                    _builder.EmitOpCode(ILOpCode.Isinst);
                    _builder.EmitToken(exceptionType, catchBlock.Syntax, _diagnostics);
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

            if ((object)catchBlock.LocalOpt != null)
            {
                var declaringReferences = catchBlock.LocalOpt.DeclaringSyntaxReferences;
                var localSyntax = !declaringReferences.IsEmpty ? (CSharpSyntaxNode)declaringReferences[0].GetSyntax() : catchBlock.Syntax;
                DefineLocal(catchBlock.LocalOpt, localSyntax);
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

                        var stateMachineField = left.FieldSymbol as StateMachineFieldSymbol;
                        if (((object)stateMachineField != null) && (stateMachineField.SlotIndex >= 0))
                        {
                            _builder.DefineUserDefinedStateMachineHoistedLocal(stateMachineField.SlotIndex);
                        }

                        // When assigning to a field
                        // we need to push param address below the exception
                        var temp = AllocateTemp(exceptionSource.Type, exceptionSource.Syntax);
                        _builder.EmitLocalStore(temp);

                        var receiverTemp = EmitReceiverRef(left.ReceiverOpt);
                        Debug.Assert(receiverTemp == null);

                        _builder.EmitLocalLoad(temp);
                        FreeTemp(temp);

                        EmitFieldStore(left);
                        break;

                    default:
                        throw ExceptionUtilities.UnexpectedValue(exceptionSource.Kind);
                }
            }
            else
            {
                _builder.EmitOpCode(ILOpCode.Pop);
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

                // Pop the exception; it should have already been stored to the
                // variable by the filter.
                _builder.EmitOpCode(ILOpCode.Pop);
            }

            EmitBlock(catchBlock.Body);

            _builder.CloseLocalScope();
        }

        private void EmitSwitchStatement(BoundSwitchStatement switchStatement)
        {
            // Switch expression must have a valid switch governing type
            Debug.Assert((object)switchStatement.Expression.Type != null);
            Debug.Assert(switchStatement.Expression.Type.IsValidSwitchGoverningType());

            // We must have rewritten nullable switch expression into non-nullable constructs.
            Debug.Assert(!switchStatement.Expression.Type.IsNullableType());

            BoundExpression expression = switchStatement.Expression;
            ImmutableArray<BoundSwitchSection> switchSections = switchStatement.SwitchSections;
            GeneratedLabelSymbol breakLabel = switchStatement.BreakLabel;
            LabelSymbol constantTargetOpt = switchStatement.ConstantTargetOpt;

            if ((object)constantTargetOpt != null)
            {
                EmitConstantSwitchHeader(expression, constantTargetOpt);
            }
            else
            {
                // ConstantTargetOpt should be set to breakLabel for empty switch statement
                Debug.Assert(switchStatement.SwitchSections.Any());

                // Get switch case labels (indexed by their constant value) for emitting switch header and jump table
                LabelSymbol fallThroughLabel = breakLabel;
                KeyValuePair<ConstantValue, object>[] switchCaseLabels = GetSwitchCaseLabels(switchSections, ref fallThroughLabel);

                // CONSIDER: EmitSwitchHeader may modify the switchCaseLabels array by sorting it.
                // CONSIDER: Currently, only purpose of creating this switchCaseLabels array is for Emitting the switch header.
                // CONSIDER: If this requirement changes, we may want to pass in ArrayBuilder<KeyValuePair<ConstantValue, object>> instead.

                if (switchCaseLabels.Length == 0)
                {
                    // no case labels
                    EmitExpression(expression, used: false);
                    _builder.EmitBranch(ILOpCode.Br, fallThroughLabel);
                }
                else
                {
                    EmitSwitchHeader(switchStatement, expression, switchCaseLabels, fallThroughLabel);
                }
            }

            EmitSwitchBody(switchStatement.InnerLocals, switchSections, breakLabel, switchStatement.Syntax);
        }

        private static KeyValuePair<ConstantValue, object>[] GetSwitchCaseLabels(ImmutableArray<BoundSwitchSection> sections, ref LabelSymbol fallThroughLabel)
        {
            var labelsBuilder = ArrayBuilder<KeyValuePair<ConstantValue, object>>.GetInstance();
            foreach (var section in sections)
            {
                foreach (BoundSwitchLabel boundLabel in section.SwitchLabels)
                {
                    var label = (SourceLabelSymbol)boundLabel.Label;
                    if (label.IdentifierNodeOrToken.Kind() == SyntaxKind.DefaultSwitchLabel)
                    {
                        fallThroughLabel = label;
                    }
                    else
                    {
                        Debug.Assert(label.SwitchCaseLabelConstant != null
                            && SwitchConstantValueHelper.IsValidSwitchCaseLabelConstant(label.SwitchCaseLabelConstant));

                        labelsBuilder.Add(new KeyValuePair<ConstantValue, object>(label.SwitchCaseLabelConstant, label));
                    }
                }
            }

            return labelsBuilder.ToArrayAndFree();
        }

        private void EmitConstantSwitchHeader(BoundExpression expression, LabelSymbol target)
        {
            EmitExpression(expression, false);
            _builder.EmitBranch(ILOpCode.Br, target);
        }

        private void EmitSwitchHeader(
            BoundSwitchStatement switchStatement,
            BoundExpression expression,
            KeyValuePair<ConstantValue, object>[] switchCaseLabels,
            LabelSymbol fallThroughLabel)
        {
            Debug.Assert(expression.ConstantValue == null);
            Debug.Assert((object)expression.Type != null &&
                expression.Type.IsValidSwitchGoverningType());
            Debug.Assert(switchCaseLabels.Length > 0);

            Debug.Assert(switchCaseLabels != null);

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

            // Emit switch jump table            
            if (expression.Type.SpecialType != SpecialType.System_String)
            {
                _builder.EmitIntegerSwitchJumpTable(switchCaseLabels, fallThroughLabel, key, expression.Type.EnumUnderlyingType().PrimitiveTypeCode);
            }
            else
            {
                this.EmitStringSwitchJumpTable(switchStatement, switchCaseLabels, fallThroughLabel, key, expression.Syntax);
            }

            if (temp != null)
            {
                FreeTemp(temp);
            }

            if (sequence != null)
            {
                FreeLocals(sequence, doNotRelease: null);
            }
        }

        private void EmitStringSwitchJumpTable(
            BoundSwitchStatement switchStatement,
            KeyValuePair<ConstantValue, object>[] switchCaseLabels,
            LabelSymbol fallThroughLabel,
            LocalOrParameter key,
            CSharpSyntaxNode syntaxNode)
        {
            LocalDefinition keyHash = null;

            // Condition is necessary, but not sufficient (e.g. might be missing a special or well-known member).
            if (SwitchStringJumpTableEmitter.ShouldGenerateHashTableSwitch(_module, switchCaseLabels.Length))
            {
                Debug.Assert(_module.SupportsPrivateImplClass);

                var privateImplClass = _module.GetPrivateImplClass(syntaxNode, _diagnostics);
                Cci.IReference stringHashMethodRef = privateImplClass.GetMethod(PrivateImplementationDetails.SynthesizedStringHashFunctionName);

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
                    _builder.EmitToken(stringHashMethodRef, syntaxNode, _diagnostics);

                    var UInt32Type = _module.Compilation.GetSpecialType(SpecialType.System_UInt32);
                    keyHash = AllocateTemp(UInt32Type, syntaxNode);

                    _builder.EmitLocalStore(keyHash);
                }
            }

            Cci.IReference stringEqualityMethodRef = _module.Translate(switchStatement.StringEquality, syntaxNode, _diagnostics);

            Cci.IMethodReference stringLengthRef = null;
            var stringLengthMethod = _module.Compilation.GetSpecialTypeMember(SpecialMember.System_String__Length) as MethodSymbol;
            if (stringLengthMethod != null && !stringLengthMethod.HasUseSiteError)
            {
                stringLengthRef = _module.Translate(stringLengthMethod, syntaxNode, _diagnostics);
            }

            SwitchStringJumpTableEmitter.EmitStringCompareAndBranch emitStringCondBranchDelegate =
                (keyArg, stringConstant, targetLabel) =>
                {
                    if (stringConstant == ConstantValue.Null)
                    {
                        // if (key == null)
                        //      goto targetLabel
                        _builder.EmitLoad(keyArg);
                        _builder.EmitBranch(ILOpCode.Brfalse, targetLabel, ILOpCode.Brtrue);
                    }
                    else if (stringConstant.StringValue.Length == 0 && stringLengthRef != null)
                    {
                        // if (key != null && key.Length == 0)
                        //      goto targetLabel

                        object skipToNext = new object();
                        _builder.EmitLoad(keyArg);
                        _builder.EmitBranch(ILOpCode.Brfalse, skipToNext, ILOpCode.Brtrue);

                        _builder.EmitLoad(keyArg);
                        // Stack: key --> length
                        _builder.EmitOpCode(ILOpCode.Call, 0);
                        var diag = DiagnosticBag.GetInstance();
                        _builder.EmitToken(stringLengthRef, null, diag);
                        Debug.Assert(diag.IsEmptyWithoutResolution);
                        diag.Free();

                        _builder.EmitBranch(ILOpCode.Brfalse, targetLabel, ILOpCode.Brtrue);
                        _builder.MarkLabel(skipToNext);
                    }
                    else
                    {
                        this.EmitStringCompareAndBranch(key, syntaxNode, stringConstant, targetLabel, stringEqualityMethodRef);
                    }
                };

            _builder.EmitStringSwitchJumpTable(
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
            _builder.EmitConstantValue(stringConstant);
            _builder.EmitOpCode(ILOpCode.Call, stackAdjustment: -1);
            _builder.EmitToken(stringEqualityMethodRef, syntaxNode, _diagnostics);

            // Branch to targetLabel if String.Equals returned true.
            _builder.EmitBranch(ILOpCode.Brtrue, targetLabel, ILOpCode.Brfalse);
        }

        private void EmitSwitchBody(
            ImmutableArray<LocalSymbol> locals,
            ImmutableArray<BoundSwitchSection> switchSections,
            GeneratedLabelSymbol breakLabel,
            CSharpSyntaxNode syntaxNode)
        {
            var hasLocals = !locals.IsEmpty;

            if (hasLocals)
            {
                _builder.OpenLocalScope();

                foreach (var local in locals)
                {
                    DefineLocal(local, syntaxNode);
                }
            }

            foreach (var section in switchSections)
            {
                EmitSwitchSection(section);
            }

            _builder.MarkLabel(breakLabel);

            if (hasLocals)
            {
                _builder.CloseLocalScope();
            }
        }

        private void EmitSwitchSection(BoundSwitchSection switchSection)
        {
            foreach (var boundSwitchLabel in switchSection.SwitchLabels)
            {
                _builder.MarkLabel(boundSwitchLabel.Label);
            }

            foreach (var statement in switchSection.Statements)
            {
                EmitStatement(statement);
            }
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

        private LocalDefinition DefineLocal(LocalSymbol local, CSharpSyntaxNode syntaxNode)
        {
            var transformFlags = default(ImmutableArray<TypedConstant>);
            bool hasDynamic = local.Type.ContainsDynamic();
            var isDynamicSourceLocal = hasDynamic && !local.IsCompilerGenerated;
            if (isDynamicSourceLocal)
            {
                NamedTypeSymbol booleanType = _module.Compilation.GetSpecialType(SpecialType.System_Boolean);
                transformFlags = CSharpCompilation.DynamicTransformsEncoder.Encode(local.Type, booleanType, 0, RefKind.None);
            }

            if (local.IsConst)
            {
                Debug.Assert(local.HasConstantValue);
                MetadataConstant compileTimeValue = _module.CreateConstant(local.Type, local.ConstantValue, syntaxNode, _diagnostics);
                LocalConstantDefinition localConstantDef = new LocalConstantDefinition(local.Name, local.Locations.FirstOrDefault() ?? Location.None, compileTimeValue, isDynamicSourceLocal, transformFlags);
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
                Debug.Assert(local.Type.IsPointerType());

                constraints = LocalSlotConstraints.ByRef | LocalSlotConstraints.Pinned;
                PointerTypeSymbol pointerType = (PointerTypeSymbol)local.Type;
                TypeSymbol pointedAtType = pointerType.PointedAtType;

                // We can't declare a reference to void, so if the pointed-at type is void, use native int
                // (represented here by IntPtr) instead.
                translatedType = pointedAtType.SpecialType == SpecialType.System_Void
                    ? _module.GetSpecialType(SpecialType.System_IntPtr, syntaxNode, _diagnostics)
                    : _module.Translate(pointedAtType, syntaxNode, _diagnostics);
            }
            else
            {
                constraints = (local.IsPinned ? LocalSlotConstraints.Pinned : LocalSlotConstraints.None) |
                    (local.RefKind != RefKind.None ? LocalSlotConstraints.ByRef : LocalSlotConstraints.None);
                translatedType = _module.Translate(local.Type, syntaxNode, _diagnostics);
            }

            // Even though we don't need the token immediately, we will need it later when signature for the local is emitted.
            // Also, requesting the token has side-effect of registering types used, which is critical for embedded types (NoPia, VBCore, etc).
            _module.GetFakeSymbolTokenForIL(translatedType, syntaxNode, _diagnostics);

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
                isDynamic: isDynamicSourceLocal,
                dynamicTransformFlags: transformFlags,
                isSlotReusable: local.SynthesizedKind.IsSlotReusable(_ilEmitStyle != ILEmitStyle.Release));

            // If named, add it to the local debug scope.
            if (localDef.Name != null)
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

            if (!localKind.IsLongLived())
            {
                return null;
            }

            if (_ilEmitStyle == ILEmitStyle.Debug)
            {
                var syntax = local.GetDeclaratorSyntax();
                int syntaxOffset = _method.CalculateLocalSyntaxOffset(syntax.SpanStart, syntax.SyntaxTree);

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
        private LocalDefinition AllocateTemp(TypeSymbol type, SyntaxNode syntaxNode)
        {
            return _builder.LocalSlotManager.AllocateSlot(
                _module.Translate(type, syntaxNode, _diagnostics),
                LocalSlotConstraints.None);
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

            public override BoundNode VisitSwitchStatement(BoundSwitchStatement node)
            {
                var breakLabelClone = GetLabelClone(node.BreakLabel);

                // expressions do not contain labels or branches
                BoundExpression boundExpression = node.Expression;
                ImmutableArray<BoundSwitchSection> switchSections = (ImmutableArray<BoundSwitchSection>)this.VisitList(node.SwitchSections);
                return node.Update(boundExpression, node.ConstantTargetOpt, node.InnerLocals, node.InnerLocalFunctions, switchSections, breakLabelClone, node.StringEquality);
            }

            public override BoundNode VisitSwitchLabel(BoundSwitchLabel node)
            {
                var labelClone = GetLabelClone(node.Label);

                // expressions do not contain labels or branches
                BoundExpression expressionOpt = node.ExpressionOpt;
                return node.Update(labelClone, expressionOpt);
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
