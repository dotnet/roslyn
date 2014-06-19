// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using Microsoft.CodeAnalysis.CodeGen;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.CodeGen
{
    partial class CodeGenerator
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

                case BoundKind.IteratorScope:
                    EmitIteratorScope((BoundIteratorScope)statement);
                    break;

                case BoundKind.NoOpStatement:
                    EmitNoOpStatement((BoundNoOpStatement)statement);
                    break;

                default:
                    // Code gen should not be invoked if there are errors.
                    throw ExceptionUtilities.UnexpectedValue(statement.Kind);
            }

#if DEBUG
            if (stackLocals == null || stackLocals.Count == 0)
            {
                builder.AssertStackEmpty();
            }
#endif
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
                    if (this.noOptimizations)
                    {
                        builder.EmitOpCode(ILOpCode.Nop);
                    }
                    break;

                case NoOpStatementFlavor.AwaitYieldPoint:
                    Debug.Assert((this.asyncYieldPoints == null) == (this.asyncResumePoints == null));
                    if (this.asyncYieldPoints == null)
                    {
                        this.asyncYieldPoints = ArrayBuilder<int>.GetInstance();
                        this.asyncResumePoints = ArrayBuilder<int>.GetInstance();
                    }
                    Debug.Assert(this.asyncYieldPoints.Count == this.asyncResumePoints.Count);
                    this.asyncYieldPoints.Add(this.builder.AllocateILMarker());
                    break;

                case NoOpStatementFlavor.AwaitResumePoint:
                    Debug.Assert(this.asyncYieldPoints != null);
                    Debug.Assert(this.asyncYieldPoints != null);
                    this.asyncResumePoints.Add(this.builder.AllocateILMarker());
                    Debug.Assert(this.asyncYieldPoints.Count == this.asyncResumePoints.Count);
                    break;

                case NoOpStatementFlavor.AsyncMethodCatchHandler:
                    Debug.Assert(this.asyncCatchHandlerOffset < 0); // only one expected
                    this.asyncCatchHandlerOffset = this.builder.AllocateILMarker();
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
                if (((object)exprType != null) && (exprType.TypeKind == TypeKind.TypeParameter))
                {
                    this.EmitBox(exprType, expr.Syntax);
                }
            }

            builder.EmitThrow(isRethrow: expr == null);
        }

        // specifies whether emitted conditional expression was a constant true/false or not a constant
        private enum ConstResKind
        {
            ConstFalse,
            ConstTrue,
            NotAConst,
        }

        private void EmitConditionalGoto(BoundConditionalGoto boundConditionalGoto)
        {
            if (noOptimizations)
            {
                //TODO: what is the point of this? 
                //native compiler does intentional dead-store here. Does it still help debugging?
                var boolTemp = AllocateTemp(boundConditionalGoto.Condition.Type, boundConditionalGoto.Condition.Syntax);

                ConstResKind crk = EmitCondExpr(boundConditionalGoto.Condition, boundConditionalGoto.JumpIfTrue);
                builder.EmitLocalStore(boolTemp);

                switch (crk)
                {
                    case ConstResKind.ConstFalse:
                        break;
                    case ConstResKind.ConstTrue:
                        builder.EmitBranch(ILOpCode.Br, boundConditionalGoto.Label);
                        break;
                    case ConstResKind.NotAConst:
                        builder.EmitLocalLoad(boolTemp);
                        builder.EmitBranch(ILOpCode.Brtrue, boundConditionalGoto.Label);
                        break;
                    default:
                        Debug.Assert(false);
                        goto case ConstResKind.NotAConst;
                }

                this.FreeTemp(boolTemp);
            }
            else
            {
                object label = boundConditionalGoto.Label;
                Debug.Assert(label != null);
                EmitCondBranch(boundConditionalGoto.Condition, ref label, boundConditionalGoto.JumpIfTrue);
            }
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

        const int IL_OP_CODE_ROW_LENGTH = 4;

        private static readonly ILOpCode[] CondJumpOpCodes = new ILOpCode[]
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
        /// Produces opcode for a jump that corresponds to given opearation and sense.
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

            revOpCode = CondJumpOpCodes[revOpIdx];
            return CondJumpOpCodes[opIdx];
        }

        // generate a jump to dest if (condition == sense) is true
        private void EmitCondBranch(BoundExpression condition, ref object dest, bool sense)
        {

        oneMoreTime:

            ILOpCode ilcode;

            if (condition.ConstantValue != null)
            {
                bool taken = condition.ConstantValue.IsDefaultValue != sense;

                if (taken)
                {
                    dest = dest ?? new object();
                    builder.EmitBranch(ILOpCode.Br, dest);
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
                                    builder.MarkLabel(fallThrough);
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
                            builder.EmitBranch(ilcode, dest, revOpCode);
                            return;
                    }

                    // none of above. 
                    // then it is regular binary expression - Or, And, Xor ...
                    goto default;

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
                        // box the operand for isint if it is not a verifier reference
                        EmitBox(operand.Type, operand.Syntax);
                    }
                    builder.EmitOpCode(ILOpCode.Isinst);
                    EmitSymbolToken(isOp.TargetType.Type, isOp.TargetType.Syntax);
                    ilcode = sense ? ILOpCode.Brtrue : ILOpCode.Brfalse;
                    dest = dest ?? new object();
                    builder.EmitBranch(ilcode, dest);
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
                    builder.EmitBranch(ilcode, dest);
                    return;
            }
        }

        private void EmitSequenceCondBranch(BoundSequence sequence, ref object dest, bool sense)
        {
            var hasLocals = !sequence.Locals.IsEmpty;

            if (hasLocals)
            {
                builder.OpenLocalScope();

                foreach (var local in sequence.Locals)
                {
                    DefineLocal(local, sequence.Syntax);
                }
            }

            EmitSideEffects(sequence);
            EmitCondBranch(sequence.Value, ref dest, sense);

            if (hasLocals)
            {
                builder.CloseLocalScope();

                foreach (var local in sequence.Locals)
                {
                    FreeLocal(local);
                }
            }
        }

        private void EmitLabelStatement(BoundLabelStatement boundLabelStatement)
        {
            builder.MarkLabel(boundLabelStatement.Label);
        }

        private void EmitGotoStatement(BoundGotoStatement boundGotoStatement)
        {
            builder.EmitBranch(ILOpCode.Br, boundGotoStatement.Label);
        }

        // used by HandleReturn method which tries to inject 
        // indirect ret sequence as a last statement in the block
        // that is the last statement of the current method
        // NOTE: it is important that there is no code after this "ret"
        //       it is desirable, for debug purposes, that this ret is emitted inside top level { } 
        private bool IsLastBlockInMethod(BoundBlock block)
        {
            if (this.block == block)
            {
                return true;
            }

            //sometimes top level node is a statement list containing 
            //epilogue and then a block. If we are having that block, it will do.
            var list = this.block as BoundStatementList;
            if (list != null && list.Statements.LastOrDefault() == block)
            {
                return true;
            }

            return false;
        }

        private void EmitBlock(BoundBlock block)
        {
            var hasLocals = !block.LocalsOpt.IsDefaultOrEmpty;

            if (hasLocals)
            {
                builder.OpenLocalScope();

                foreach (var local in block.LocalsOpt)
                {
                    var declaringReferences = local.DeclaringSyntaxReferences;
                    DefineLocal(local, !declaringReferences.IsEmpty ? (CSharpSyntaxNode)declaringReferences[0].GetSyntax() : block.Syntax);
                }
            }

            foreach (var statement in block.Statements)
            {
                EmitStatement(statement);
            }

            if (this.indirectReturnState == IndirectReturnState.Needed &&
                IsLastBlockInMethod(block))
            {
                HandleReturn();
            }

            if (hasLocals)
            {
                foreach (var local in block.LocalsOpt)
                {
                    FreeLocal(local);
                }

                builder.CloseLocalScope();
            }
        }

        private void EmitIteratorScope(BoundIteratorScope scope)
        {
            builder.OpenIteratorScope();
            foreach (var field in scope.Fields)
            {
                int index = field.IteratorLocalIndex;
                Debug.Assert(index >= 1);
                builder.DefineIteratorLocal(index);
            }

            EmitStatement(scope.Statement);
            builder.CloseIteratorScope();
        }

        //There are two ways a value can be returned from a function:
        // - Using ret opcode
        // - Store return value if any to a predefined temp and jump to the epilogue block
        // Sometimes ret is not an option (try/catch etc.). We also do this when emit debuggable code
        // this function is a stub for the logic that decides that.
        private bool ShouldUseIndirectReturn()
        {
            return noOptimizations || builder.InExceptionHandler;
        }

        // compiler generated return mapped to a block is very likely the synthetic return
        // that was added at the end of the last block of a void method by analysis.
        // This is likely to be the last return in the method,
        // so if we have not yet emitted return sequence, it is convenient to do it right here (if we can).
        private bool CanHandleReturnLabel(BoundReturnStatement boundReturnStatement)
        {
            return boundReturnStatement.WasCompilerGenerated &&
                    (boundReturnStatement.Syntax.Kind == SyntaxKind.Block || (((object)this.method != null) && this.method.IsImplicitConstructor)) &&
                    !builder.InExceptionHandler;
        }

        private void EmitReturnStatement(BoundReturnStatement boundReturnStatement)
        {
            this.EmitExpression(boundReturnStatement.ExpressionOpt, true);

            if (ShouldUseIndirectReturn())
            {
                if (boundReturnStatement.ExpressionOpt != null)
                {
                    builder.EmitLocalStore(LazyReturnTemp);
                }

                if (this.indirectReturnState != IndirectReturnState.Emitted && CanHandleReturnLabel(boundReturnStatement))
                {
                    HandleReturn();
                }
                else
                {
                    builder.EmitBranch(ILOpCode.Br, ReturnLabel);

                    if (this.indirectReturnState == IndirectReturnState.NotNeeded)
                    {
                        this.indirectReturnState = IndirectReturnState.Needed;
                    }
                }
            }
            else
            {
                if (this.indirectReturnState == IndirectReturnState.Needed && CanHandleReturnLabel(boundReturnStatement))
                {
                    if (boundReturnStatement.ExpressionOpt != null)
                    {
                        builder.EmitLocalStore(LazyReturnTemp);
                    }

                    HandleReturn();
                }
                else
                {
                    builder.EmitRet(boundReturnStatement.ExpressionOpt == null);
                }
            }
        }

        private void EmitTryStatement(BoundTryStatement statement, bool emitCatchesOnly = false)
        {
            Debug.Assert(!statement.CatchBlocks.IsDefault);

            // Stack must be empty at beginning of try block.
            builder.AssertStackEmpty();

            // IL requires catches and finally block to be distinct try
            // blocks so if the source contained both a catch and
            // a finally, nested scopes are emitted.
            bool emitNestedScopes = (!emitCatchesOnly &&
                (statement.CatchBlocks.Length > 0) &&
                (statement.FinallyBlockOpt != null));

            builder.OpenLocalScope(ScopeType.TryCatchFinally);

            builder.OpenLocalScope(ScopeType.Try);
            // IL requires catches and finally block to be distinct try
            // blocks so if the source contained both a catch and
            // a finally, nested scopes are emitted.

            tryNestingLevel++;
            if (emitNestedScopes)
            {
                EmitTryStatement(statement, emitCatchesOnly: true);
            }
            else
            {
                EmitBlock(statement.TryBlock);
            }

            tryNestingLevel--;
            // Close the Try scope
            builder.CloseLocalScope();

            if (!emitNestedScopes)
            {
                foreach (var catchBlock in statement.CatchBlocks)
                {
                    EmitCatchBlock(catchBlock);
                }
            }

            if (!emitCatchesOnly && (statement.FinallyBlockOpt != null))
            {
                builder.OpenLocalScope(statement.PreferFaultHandler ? ScopeType.Fault : ScopeType.Finally);
                EmitBlock(statement.FinallyBlockOpt);

                // close Finally scope
                builder.CloseLocalScope();

                // close the whole try statement scope
                builder.CloseLocalScope();

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
                builder.CloseLocalScope();
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
        /// catch (ExceptionType ex) if (Condition)
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

            var exceptionType = ((object)catchBlock.ExceptionTypeOpt != null) ?
                this.module.Translate(catchBlock.ExceptionTypeOpt, catchBlock.Syntax, diagnostics) :
                this.module.GetSpecialType(SpecialType.System_Object, catchBlock.Syntax, diagnostics);

            builder.AdjustStack(1); // Account for exception on the stack.

            // Open appropriate exception handler scope. (Catch or Filter)
            // if it is a Filter, emit prologue that checks if the type on the stack
            // converts to what we want.
            if (catchBlock.ExceptionFilterOpt == null)
            {
                builder.OpenLocalScope(ScopeType.Catch, exceptionType);

                // Dev12 inserts the sequence point on catch clause without a filter, just before 
                // the exception object is assigned to the variable.
                // 
                // Also in Dev12 the exception variable scope span starts right after the stloc instruction and 
                // ends right before leave instruction. So when stopped at the sequence point Dev12 inserts,
                // the exception variable is not visible. 
                if (this.emitSequencePoints)
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
                builder.OpenLocalScope(ScopeType.Filter);

                // Filtering starts with simulating regular catch through a 
                // type check. If this is not our type then we are done.
                var typeCheckPassedLabel = new object();
                typeCheckFailedLabel = new object();

                builder.EmitOpCode(ILOpCode.Isinst);
                builder.EmitToken(exceptionType, catchBlock.Syntax, diagnostics);
                builder.EmitOpCode(ILOpCode.Dup);
                builder.EmitBranch(ILOpCode.Brtrue, typeCheckPassedLabel);
                builder.EmitOpCode(ILOpCode.Pop);
                builder.EmitIntConstant(0);
                builder.EmitBranch(ILOpCode.Br, typeCheckFailedLabel);

                builder.MarkLabel(typeCheckPassedLabel);
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
                // if exception's type is a generic type prameter.
                if (!exceptionSourceOpt.Type.IsVerifierReference())
                {
                    Debug.Assert(exceptionSourceOpt.Type.IsTypeParameter()); // only expecting type parameters
                    builder.EmitOpCode(ILOpCode.Unbox_any);
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
                            builder.EmitLocalStore(GetLocal(exceptionSourceLocal));
                        }

                        break;

                    case BoundKind.FieldAccess:
                        var left = (BoundFieldAccess)exceptionSource;
                        Debug.Assert(!left.FieldSymbol.IsStatic, "Not supported");
                        Debug.Assert(!left.ReceiverOpt.Type.IsTypeParameter());

                        // When assigning to a field
                        // we need to push param address below the exception
                        var temp = AllocateTemp(exceptionSource.Type, exceptionSource.Syntax);
                        builder.EmitLocalStore(temp);

                        EmitReceiverRef(left.ReceiverOpt);

                        builder.EmitLocalLoad(temp);
                        EmitFieldStore(left);
                        break;

                    default:
                        throw ExceptionUtilities.UnexpectedValue(exceptionSource.Kind);
                }
            }
            else
            {
                builder.EmitOpCode(ILOpCode.Pop);
            }

            // Emit the actual filter expression, if we have one, and normalize
            // results.
            if (catchBlock.ExceptionFilterOpt != null)
            {
                EmitCondExpr(catchBlock.ExceptionFilterOpt, true);
                // Normalize the return value because values other than 0 or 1
                // produce unspecified results.
                builder.EmitIntConstant(0);
                builder.EmitOpCode(ILOpCode.Cgt_un);
                builder.MarkLabel(typeCheckFailedLabel);

                // Now we are starting the actual handler
                builder.MarkFilterConditionEnd();

                // Pop the exception; it should have already been stored to the
                // variable by the filter.
                builder.EmitOpCode(ILOpCode.Pop);
            }

            EmitBlock(catchBlock.Body);

            builder.CloseLocalScope();
        }

        private void EmitSwitchStatement(BoundSwitchStatement switchStatement)
        {
            Debug.Assert(switchStatement.OuterLocals.IsEmpty);

            // Switch expression must have a valid switch governing type
            Debug.Assert((object)switchStatement.BoundExpression.Type != null);
            Debug.Assert(switchStatement.BoundExpression.Type.IsValidSwitchGoverningType());

            // We must have rewritten nullable switch expression into non-nullable constructs.
            Debug.Assert(!switchStatement.BoundExpression.Type.IsNullableType());

            BoundExpression expression = switchStatement.BoundExpression;
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
                    builder.EmitBranch(ILOpCode.Br, fallThroughLabel);
                }
                else
                {
                    EmitSwitchHeader(switchStatement, expression, switchCaseLabels, fallThroughLabel);
                }
            }

            EmitSwitchBody(switchStatement.InnerLocalsOpt, switchSections, breakLabel, switchStatement.Syntax);
        }

        private static KeyValuePair<ConstantValue, object>[] GetSwitchCaseLabels(ImmutableArray<BoundSwitchSection> sections, ref LabelSymbol fallThroughLabel)
        {
            var labelsBuilder = ArrayBuilder<KeyValuePair<ConstantValue, object>>.GetInstance();
            foreach (var section in sections)
            {
                foreach (BoundSwitchLabel boundLabel in section.BoundSwitchLabels)
                {
                    var label = (SourceLabelSymbol)boundLabel.Label;
                    if (label.IdentifierNodeOrToken.CSharpKind() == SyntaxKind.DefaultSwitchLabel)
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
            builder.EmitBranch(ILOpCode.Br, target);
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

            var exprType = expression.Type;
            LocalDefinition temp = null;

            // Emit switch jump table            
            if (expression.Type.SpecialType != SpecialType.System_String)
            {
                if (expression.Kind == BoundKind.Local && ((BoundLocal)expression).LocalSymbol.RefKind == RefKind.None)
                {
                    builder.EmitIntegerSwitchJumpTable(switchCaseLabels, fallThroughLabel, this.GetLocal((BoundLocal)expression), exprType.EnumUnderlyingType().PrimitiveTypeCode);
                }
                else if (expression.Kind == BoundKind.Parameter && ((BoundParameter)expression).ParameterSymbol.RefKind == RefKind.None)
                {
                    builder.EmitIntegerSwitchJumpTable(switchCaseLabels, fallThroughLabel, ParameterSlot((BoundParameter)expression), exprType.EnumUnderlyingType().PrimitiveTypeCode);
                }
                else
                {
                    EmitExpression(expression, true);
                    temp = AllocateTemp(exprType, expression.Syntax);
                    builder.EmitLocalStore(temp);

                    builder.EmitIntegerSwitchJumpTable(switchCaseLabels, fallThroughLabel, temp, exprType.EnumUnderlyingType().PrimitiveTypeCode);
                }
            }
            else
            {
                if (expression.Kind == BoundKind.Local && ((BoundLocal)expression).LocalSymbol.RefKind == RefKind.None)
                {
                    this.EmitStringSwitchJumpTable(switchStatement, switchCaseLabels, fallThroughLabel, this.GetLocal((BoundLocal)expression), expression.Syntax);
                }
                else
                {
                    EmitExpression(expression, true);
                    temp = AllocateTemp(exprType, expression.Syntax);
                    builder.EmitLocalStore(temp);

                    this.EmitStringSwitchJumpTable(switchStatement, switchCaseLabels, fallThroughLabel, temp, expression.Syntax);
                }
            }

            if (temp != null)
            {
                FreeTemp(temp);
            }
        }

        private void EmitStringSwitchJumpTable(
            BoundSwitchStatement switchStatement,
            KeyValuePair<ConstantValue, object>[] switchCaseLabels,
            LabelSymbol fallThroughLabel,
            LocalDefinition key,
            CSharpSyntaxNode syntaxNode)
        {
            LocalDefinition keyHash = null;

            // Condition is necessary, but not sufficient (e.g. might be missing a special or well-known member).
            if (SwitchStringJumpTableEmitter.ShouldGenerateHashTableSwitch(this.module, switchCaseLabels.Length))
            {
                Debug.Assert(this.module.SupportsPrivateImplClass);

                var privateImplClass = this.module.GetPrivateImplClass(syntaxNode, diagnostics);
                Microsoft.Cci.IReference stringHashMethodRef = privateImplClass.GetMethod(PrivateImplementationDetails.SynthesizedStringHashFunctionName);

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

                    builder.EmitLocalLoad(key);
                    builder.EmitOpCode(ILOpCode.Call, stackAdjustment: 0);
                    builder.EmitToken(stringHashMethodRef, syntaxNode, diagnostics);

                    var UInt32Type = module.Compilation.GetSpecialType(SpecialType.System_UInt32);
                    keyHash = AllocateTemp(UInt32Type, syntaxNode);

                    builder.EmitLocalStore(keyHash);
                }
            }

            Microsoft.Cci.IReference stringEqualityMethodRef = module.Translate(switchStatement.StringEquality, syntaxNode, diagnostics);

            Microsoft.Cci.IMethodReference stringLengthRef = null;
            var stringLengthMethod = module.Compilation.GetSpecialTypeMember(SpecialMember.System_String__Length) as MethodSymbol;
            if (stringLengthMethod != null && !stringLengthMethod.HasUseSiteError)
            {
                stringLengthRef = module.Translate(stringLengthMethod, syntaxNode, diagnostics);
            }

            SwitchStringJumpTableEmitter.EmitStringCompareAndBranch emitStringCondBranchDelegate =
                (keyArg, stringConstant, targetLabel) =>
                {
                    if (stringConstant == ConstantValue.Null)
                    {
                        // if (key == null)
                        //      goto targetLabel
                        builder.EmitLocalLoad(keyArg);
                        builder.EmitBranch(ILOpCode.Brfalse, targetLabel, ILOpCode.Brtrue);
                    }
                    else if (stringConstant.StringValue.Length == 0 && stringLengthRef != null)
                    {
                        // if (key != null && key.Length == 0)
                        //      goto targetLabel

                        object skipToNext = new object();
                        builder.EmitLocalLoad(keyArg);
                        builder.EmitBranch(ILOpCode.Brfalse, skipToNext, ILOpCode.Brtrue);

                        builder.EmitLocalLoad(keyArg);
                        // Stack: key --> length
                        builder.EmitOpCode(ILOpCode.Call, 0);
                        var diag = DiagnosticBag.GetInstance();
                        builder.EmitToken(stringLengthRef, null, diag);
                        Debug.Assert(diag.IsEmptyWithoutResolution);
                        diag.Free();

                        builder.EmitBranch(ILOpCode.Brfalse, targetLabel, ILOpCode.Brtrue);
                        builder.MarkLabel(skipToNext);
                    }
                    else
                    {
                        this.EmitStringCompareAndBranch(key, syntaxNode, stringConstant, targetLabel, stringEqualityMethodRef);
                    }
                };

            builder.EmitStringSwitchJumpTable(
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
        private void EmitStringCompareAndBranch(LocalDefinition key, SyntaxNode syntaxNode, ConstantValue stringConstant, object targetLabel, Microsoft.Cci.IReference stringEqualityMethodRef)
        {
            // Emit compare and branch:

            // if (key == stringConstant)
            //      goto targetLabel;

            Debug.Assert(stringEqualityMethodRef != null);

#if DEBUG
            var assertDiagnostics = DiagnosticBag.GetInstance();
            Debug.Assert(stringEqualityMethodRef == module.Translate((MethodSymbol)module.Compilation.GetSpecialTypeMember(SpecialMember.System_String__op_Equality), (CSharpSyntaxNode)syntaxNode, assertDiagnostics));
            assertDiagnostics.Free();
#endif

            // static bool String.Equals(string a, string b)
            // pop 2 (a, b)
            // push 1 (bool return value)

            // stackAdjustment = (pushCount - popCount) = -1

            builder.EmitLocalLoad(key);
            builder.EmitConstantValue(stringConstant);
            builder.EmitOpCode(ILOpCode.Call, stackAdjustment: -1);
            builder.EmitToken(stringEqualityMethodRef, syntaxNode, diagnostics);

            // Branch to targetLabel if String.Equals returned true.
            builder.EmitBranch(ILOpCode.Brtrue, targetLabel, ILOpCode.Brfalse);
        }

        private void EmitSwitchBody(
            ImmutableArray<LocalSymbol> localsOpt,
            ImmutableArray<BoundSwitchSection> switchSections,
            GeneratedLabelSymbol breakLabel,
            CSharpSyntaxNode syntaxNode)
        {
            var hasLocals = !localsOpt.IsEmpty;

            if (hasLocals)
            {
                builder.OpenLocalScope();

                foreach (var local in localsOpt)
                {
                    DefineLocal(local, syntaxNode);
                }
            }

            foreach (var section in switchSections)
            {
                EmitSwitchSection(section);
            }

            builder.MarkLabel(breakLabel);

            if (hasLocals)
            {
                builder.CloseLocalScope();
            }
        }

        private void EmitSwitchSection(BoundSwitchSection switchSection)
        {
            foreach (var boundSwitchLabel in switchSection.BoundSwitchLabels)
            {
                builder.MarkLabel(boundSwitchLabel.Label);
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
            return builder.LocalSlotManager.GetLocal(symbol);
        }

        private LocalDefinition DefineLocal(LocalSymbol local, CSharpSyntaxNode syntaxNode)
        {
            var transformFlags = default(ImmutableArray<TypedConstant>);
            bool hasDynamic = local.Type.ContainsDynamic();
            var isDynamicSourceLocal = hasDynamic && !local.IsCompilerGenerated;
            if (isDynamicSourceLocal)
            {
                NamedTypeSymbol booleanType = this.module.Compilation.GetSpecialType(SpecialType.System_Boolean);
                transformFlags = CSharpCompilation.DynamicTransformsEncoder.Encode(local.Type, booleanType, 0, RefKind.None);
            }

            if (local.IsConst)
            {
                Debug.Assert(local.HasConstantValue);
                MetadataConstant compileTimeValue = this.module.CreateConstant(local.Type, local.ConstantValue, syntaxNode, diagnostics);
                LocalConstantDefinition localConstantDef = new LocalConstantDefinition(local.Name, local.Locations.FirstOrDefault() ?? Location.None, compileTimeValue, isDynamicSourceLocal, transformFlags);
                builder.AddLocalConstantToScope(localConstantDef);
                return null;
            }
            else if (IsStackLocal(local))
            {
                return null;
            }
            else
            {
                var name = RequiresGeneratedName(local) ? GenerateName(local) : local.Name;
                LocalSlotConstraints constraints;
                Microsoft.Cci.ITypeReference translatedType;

                if (local.DeclarationKind == LocalDeclarationKind.Fixed && local.IsPinned) // Excludes pointer local and string local in fixed string case.
                {
                    Debug.Assert(local.RefKind == RefKind.None);
                    Debug.Assert(local.Type.IsPointerType());

                    constraints = LocalSlotConstraints.ByRef | LocalSlotConstraints.Pinned;
                    PointerTypeSymbol pointerType = (PointerTypeSymbol)local.Type;
                    TypeSymbol pointedAtType = pointerType.PointedAtType;

                    // We can't declare a reference to void, so if the pointed-at type is void, use native int
                    // (represented here by IntPtr) instead.
                    translatedType = pointedAtType.SpecialType == SpecialType.System_Void
                        ? this.module.GetSpecialType(SpecialType.System_IntPtr, syntaxNode, diagnostics)
                        : this.module.Translate(pointedAtType, syntaxNode, diagnostics);
                }
                else
                {
                    constraints = (local.IsPinned ? LocalSlotConstraints.Pinned : LocalSlotConstraints.None) |
                        (local.RefKind != RefKind.None ? LocalSlotConstraints.ByRef : LocalSlotConstraints.None);
                    translatedType = this.module.Translate(local.Type, syntaxNode, diagnostics);
                }

                // Even though we don't need the token immediately, we will need it later when signature for the local is emitted.
                // Also, requesting the token has side-effect of registering types used, which is critical for embedded types (NoPia, VBCore, etc).
                this.module.GetFakeSymbolTokenForIL(translatedType, syntaxNode, diagnostics);

                var localDef = builder.LocalSlotManager.DeclareLocal(
                        type: translatedType,
                        identity: local,
                        name: name,
                        isCompilerGenerated: local.TempKind != TempKind.None,
                        constraints: constraints,
                        isDynamic: isDynamicSourceLocal,
                        dynamicTransformFlags: transformFlags);

                // If named, add it to the scope
                if (name != null)
                {
                    //reference in the scope for debugging purpose
                    builder.AddLocalToScope(localDef);
                }

                return localDef;
            }
        }

        /// <summary>
        /// Temporaries spanning multiple statements are
        /// named in debug builds to ensure the associated
        /// slots are recognized and reused in EnC.
        /// </summary>
        private bool RequiresGeneratedName(LocalSymbol local)
        {
            int kind = (int)local.TempKind;

            // Locals should only have been named if they represent explicit local variables.
            Debug.Assert((kind < 0) || (local.Name == null));

            // Only generating names in debug builds.
            return (kind >= 0) && (this.debugInformationKind != DebugInformationKind.None);
        }

        /// <summary>
        /// Generate a unique name for the temporary that
        /// will be recognized by EnC.
        /// </summary>
        private string GenerateName(LocalSymbol local)
        {
            Debug.Assert(local.Name == null);
            Debug.Assert(RequiresGeneratedName(local));

            return GeneratedNames.MakeTemporaryName(local.TempKind, uniqueId++);
        }

        /// <summary>
        /// Releases a local.
        /// </summary>
        private void FreeLocal(LocalSymbol local)
        {
            //TODO: releasing named locals is NYI.
            if (local.Name == null &&
                !RequiresGeneratedName(local) &&
                !IsStackLocal(local))
            {
                builder.LocalSlotManager.FreeLocal(local);
            }
        }

        /// <summary>
        /// Allocates a temp without identity.
        /// </summary>
        private LocalDefinition AllocateTemp(TypeSymbol type, CSharpSyntaxNode syntaxNode)
        {
            return builder.LocalSlotManager.AllocateSlot(
                this.module.Translate(type, syntaxNode, diagnostics),
                LocalSlotConstraints.None);
        }

        /// <summary>
        /// Frees a temp.
        /// </summary>
        private void FreeTemp(LocalDefinition temp)
        {
            builder.LocalSlotManager.FreeSlot(temp);
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
        private class FinallyCloner : BoundTreeRewriter
        {
            private Dictionary<LabelSymbol, GeneratedLabelSymbol> labelClones;

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
                BoundExpression boundExpression = node.BoundExpression;
                ImmutableArray<BoundSwitchSection> switchSections = (ImmutableArray<BoundSwitchSection>)this.VisitList(node.SwitchSections);
                Debug.Assert(node.OuterLocals.IsEmpty);
                return node.Update(node.OuterLocals, boundExpression, node.ConstantTargetOpt, node.InnerLocalsOpt, switchSections, breakLabelClone, node.StringEquality);
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
                var labelClones = this.labelClones;
                if (labelClones == null)
                {
                    this.labelClones = labelClones = new Dictionary<LabelSymbol, GeneratedLabelSymbol>();
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
