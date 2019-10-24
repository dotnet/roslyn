// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

#nullable enable

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Reflection.Metadata;
using Roslyn.Utilities;
using static System.Linq.ImmutableArrayExtensions;

namespace Microsoft.CodeAnalysis.CodeGen
{
    internal partial class ILBuilder
    {
        internal void AdjustStack(int stackAdjustment)
        {
            _emitState.AdjustStack(stackAdjustment);
        }

        internal bool IsStackEmpty
        {
            get { return _emitState.CurStack == 0; }
        }

        internal void EmitOpCode(ILOpCode code)
        {
            this.EmitOpCode(code, code.NetStackBehavior());
        }

        internal void EmitOpCode(ILOpCode code, int stackAdjustment)
        {
            Debug.Assert(!code.IsControlTransfer(),
                "Control transferring opcodes should not be emitted directly. Use special methods such as EmitRet().");

            WriteOpCode(this.GetCurrentWriter(), code);

            _emitState.AdjustStack(stackAdjustment);
            _emitState.InstructionAdded();
        }

        internal void EmitToken(string value)
        {
            uint token = module?.GetFakeStringTokenForIL(value) ?? 0xFFFF;
            this.GetCurrentWriter().WriteUInt32(token);
        }

        internal void EmitToken(Cci.IReference value, SyntaxNode syntaxNode, DiagnosticBag diagnostics, bool encodeAsRawToken = false)
        {
            uint token = module?.GetFakeSymbolTokenForIL(value, syntaxNode, diagnostics) ?? 0xFFFF;
            // Setting the high bit indicates that the token value is to be interpreted literally rather than as a handle.
            if (encodeAsRawToken)
            {
                token |= Cci.MetadataWriter.LiteralMethodDefinitionToken;
            }
            this.GetCurrentWriter().WriteUInt32(token);
        }

        internal void EmitGreatestMethodToken()
        {
            // A magic value indicates that the token value is to be the literal value of the greatest method definition token.
            this.GetCurrentWriter().WriteUInt32(Cci.MetadataWriter.LiteralGreatestMethodDefinitionToken);
        }

        internal void EmitModuleVersionIdStringToken()
        {
            // A magic value indicates that the token value is to refer to a string constant for the spelling of the current module's MVID.
            this.GetCurrentWriter().WriteUInt32(Cci.MetadataWriter.ModuleVersionIdStringToken);
        }

        internal void EmitSourceDocumentIndexToken(Cci.DebugSourceDocument document)
        {
            this.GetCurrentWriter().WriteUInt32((module?.GetSourceDocumentIndexForIL(document) ?? 0xFFFF) | Cci.MetadataWriter.SourceDocumentIndex);
        }

        internal void EmitArrayBlockInitializer(ImmutableArray<byte> data, SyntaxNode syntaxNode, DiagnosticBag diagnostics)
        {
            // get helpers
            var initializeArray = module.GetInitArrayHelper();

            // map a field to the block (that makes it addressable via a token)
            var field = module.GetFieldForData(data, syntaxNode, diagnostics);

            // emit call to the helper
            EmitOpCode(ILOpCode.Dup);       //array
            EmitOpCode(ILOpCode.Ldtoken);
            EmitToken(field, syntaxNode, diagnostics);      //block
            EmitOpCode(ILOpCode.Call, -2);
            EmitToken(initializeArray, syntaxNode, diagnostics);
        }

        internal void EmitStackAllocBlockInitializer(ImmutableArray<byte> data, SyntaxNode syntaxNode, bool emitInitBlock, DiagnosticBag diagnostics)
        {
            if (emitInitBlock)
            {
                // All bytes are the same, no need for metadata blob

                EmitOpCode(ILOpCode.Dup);
                EmitIntConstant(data[0]);
                EmitIntConstant(data.Length);
                EmitOpCode(ILOpCode.Initblk, -3);
            }
            else
            {
                var field = module.GetFieldForData(data, syntaxNode, diagnostics);

                EmitOpCode(ILOpCode.Dup);
                EmitOpCode(ILOpCode.Ldsflda);
                EmitToken(field, syntaxNode, diagnostics);
                EmitIntConstant(data.Length);
                EmitOpCode(ILOpCode.Cpblk, -3);
            }
        }

        internal void EmitArrayBlockFieldRef(ImmutableArray<byte> data, ITypeSymbol elementType, SyntaxNode syntaxNode, DiagnosticBag diagnostics)
        {
            // map a field to the block (that makes it addressable)
            var field = module.GetFieldForData(data, syntaxNode, diagnostics);

            EmitOpCode(ILOpCode.Ldsflda);
            EmitToken(field, syntaxNode, diagnostics);
        }

        /// <summary>
        /// Mark current IL position with a label
        /// </summary>
        internal void MarkLabel(object label)
        {
            EndBlock();

            var block = this.GetCurrentBlock();

            //1.7.5 Backward branch constraints
            //It shall be possible, with a single forward-pass through the CIL instruction stream for any method, to infer the
            //exact state of the evaluation stack at every instruction (where by "state" we mean the number and type of each
            //item on the evaluation stack).
            //
            //In particular, if that single-pass analysis arrives at an instruction, call it location X, that immediately follows an
            //unconditional branch, and where X is not the target of an earlier branch instruction, then the state of the
            //evaluation stack at X, clearly, cannot be derived from existing information. In this case, the CLI demands that
            //the evaluation stack at X be empty.          

            LabelInfo labelInfo;
            if (_labelInfos.TryGetValue(label, out labelInfo))
            {
                Debug.Assert(labelInfo.bb == null, "duplicate use of a label");
                int labelStack = labelInfo.stack;

                var curStack = _emitState.CurStack;

                // we have already seen a branch to this label so we know its stack.
                // Now we will require that fall-through must agree with that stack value.
                // For the purpose of this assert we assume that all codepaths are reachable. 
                // This is a minor additional burden for languages to makes sure that stack is balanced 
                // even at labels that follow unconditional branches.
                // What we get is an invariant that satisfies 1.7.5 in reachable code 
                // even though we do not know yet what is reachable.
                Debug.Assert(curStack == labelStack, "forward branches and fall-through must agree on stack depth");

                _labelInfos[label] = labelInfo.WithNewTarget(block);
            }
            else
            {
                // this is a label for which we have not seen a branch yet.
                // it could mean two things - 
                // 1) it is a label of a backward branch or 
                // 2) it is a label for an unreachable forward branch and codegen did not bother to emit the branch.
                //
                // We cannot know here which case we have, so we cannot verify or force stack to be 0 on a backward branch.
                // We will just assume that languages do not do backward branches on nonempty stack
                // and let PEVerify catch that.
                //
                // With the above "assumption" current stack state is correct by definition
                // so label will assume current value and all other branches to this label 
                // will have to agree on that for consistency.

                var curStack = _emitState.CurStack;
                _labelInfos[label] = new LabelInfo(block, curStack, false);
            }

            _instructionCountAtLastLabel = _emitState.InstructionsEmitted;
        }

        internal void EmitBranch(ILOpCode code, object label, ILOpCode revOpCode = ILOpCode.Nop)
        {
            bool validOpCode = (code == ILOpCode.Nop) || code.IsBranch();

            Debug.Assert(validOpCode);
            Debug.Assert(revOpCode == ILOpCode.Nop || revOpCode.IsBranch());
            Debug.Assert(!code.HasVariableStackBehavior());

            _emitState.AdjustStack(code.NetStackBehavior());

            bool isConditional = code.IsConditionalBranch();

            LabelInfo labelInfo;
            if (!_labelInfos.TryGetValue(label, out labelInfo))
            {
                _labelInfos.Add(label, new LabelInfo(_emitState.CurStack, isConditional));
            }
            else
            {
                Debug.Assert(labelInfo.stack == _emitState.CurStack, "branches to same label with different stacks");
            }

            var block = this.GetCurrentBlock();

            // If this is a special block at the end of an exception handler,
            // the branch should be to the block itself.
            Debug.Assert((code != ILOpCode.Nop) || (block == _labelInfos[label].bb));

            block.SetBranch(label, code, revOpCode);

            if (code != ILOpCode.Nop)
            {
                _emitState.InstructionAdded();
            }

            this.EndBlock();
        }

        /// <summary>
        /// Primary method for emitting string switch jump table
        /// </summary>
        /// <param name="caseLabels">switch case labels</param>
        /// <param name="fallThroughLabel">fall through label for the jump table</param>
        /// <param name="key">Local holding the value to switch on.
        /// This value has already been loaded onto the execution stack.
        /// </param>
        /// <param name="keyHash">Local holding the hash value of the key for emitting
        /// hash table switch. Hash value has already been computed and loaded into keyHash.
        /// This parameter is null if emitting non hash table switch.
        /// </param>
        /// <param name="emitStringCondBranchDelegate">
        /// Delegate to emit string compare call and conditional branch based on the compare result.
        /// </param>
        /// <param name="computeStringHashcodeDelegate">
        /// Delegate to compute string hash consistent with value of keyHash.
        /// </param>
        internal void EmitStringSwitchJumpTable(
            KeyValuePair<ConstantValue, object>[] caseLabels,
            object fallThroughLabel,
            LocalOrParameter key,
            LocalDefinition? keyHash,
            SwitchStringJumpTableEmitter.EmitStringCompareAndBranch emitStringCondBranchDelegate,
            SwitchStringJumpTableEmitter.GetStringHashCode computeStringHashcodeDelegate)
        {
            Debug.Assert(caseLabels.Length > 0);

            var emitter = new SwitchStringJumpTableEmitter(
                this,
                key,
                caseLabels,
                fallThroughLabel,
                keyHash,
                emitStringCondBranchDelegate,
                computeStringHashcodeDelegate);

            emitter.EmitJumpTable();
        }

        /// <summary>
        /// Primary method for emitting integer switch jump table.
        /// </summary>
        /// <param name="caseLabels">switch case labels</param>
        /// <param name="fallThroughLabel">fall through label for the jump table.</param>
        /// <param name="key">Local or parameter holding the value to switch on.
        /// This value has already been loaded onto the execution stack.
        /// </param>
        /// <param name="keyTypeCode">Primitive type code of switch key.</param>
        internal void EmitIntegerSwitchJumpTable(
            KeyValuePair<ConstantValue, object>[] caseLabels,
            object fallThroughLabel,
            LocalOrParameter key,
            Cci.PrimitiveTypeCode keyTypeCode)
        {
            Debug.Assert(caseLabels.Length > 0);
            Debug.Assert(keyTypeCode != Cci.PrimitiveTypeCode.String);

            // CONSIDER: SwitchIntegralJumpTableEmitter will modify the caseLabels array by sorting it.
            // CONSIDER: Currently, only purpose of creating this caseLabels array is for Emitting the jump table.
            // CONSIDER: If this requirement changes, we may want to pass in ArrayBuilder<KeyValuePair<ConstantValue, object>> instead.

            var emitter = new SwitchIntegralJumpTableEmitter(this, caseLabels, fallThroughLabel, keyTypeCode, key);
            emitter.EmitJumpTable();
        }

        // Method to emit the virtual switch instruction
        internal void EmitSwitch(object[] labels)
        {
            _emitState.AdjustStack(-1);
            int curStack = _emitState.CurStack;

            foreach (object label in labels)
            {
                LabelInfo ld;
                if (!_labelInfos.TryGetValue(label, out ld))
                {
                    _labelInfos.Add(label, new LabelInfo(curStack, true));
                }
                else
                {
                    Debug.Assert(ld.stack == curStack, "branches to same label with different stacks");

                    if (!ld.targetOfConditionalBranches)
                    {
                        _labelInfos[label] = ld.SetTargetOfConditionalBranches();
                    }
                }
            }

            SwitchBlock switchBlock = this.CreateSwitchBlock();
            switchBlock.BranchLabels = labels;
            this.EndBlock();
        }

        internal void EmitRet(bool isVoid)
        {
            // Cannot return from within an exception handler.
            Debug.Assert(!this.InExceptionHandler);

            if (!isVoid)
            {
                _emitState.AdjustStack(-1);
            }

            var block = this.GetCurrentBlock();
            block.SetBranchCode(ILOpCode.Ret);

            _emitState.InstructionAdded();
            this.EndBlock();
        }

        internal void EmitThrow(bool isRethrow)
        {
            var block = this.GetCurrentBlock();
            if (isRethrow)
            {
                block.SetBranchCode(ILOpCode.Rethrow);
            }
            else
            {
                block.SetBranchCode(ILOpCode.Throw);
                _emitState.AdjustStack(-1);
            }

            _emitState.InstructionAdded();
            this.EndBlock();
        }

        private void EmitEndFinally()
        {
            var block = this.GetCurrentBlock();
            block.SetBranchCode(ILOpCode.Endfinally);
            this.EndBlock();
        }

        /// <summary>
        /// Finishes filter condition (and starts actual handler portion of the handler).
        /// Returns the last block of the condition.
        /// </summary>
        private BasicBlock FinishFilterCondition()
        {
            var block = this.GetCurrentBlock();
            block.SetBranchCode(ILOpCode.Endfilter);
            this.EndBlock();

            return block;
        }

        /// <summary>
        /// Generates code that creates an instance of multidimensional array
        /// </summary>
        internal void EmitArrayCreation(Microsoft.Cci.IArrayTypeReference arrayType, SyntaxNode syntaxNode, DiagnosticBag diagnostics)
        {
            Debug.Assert(!arrayType.IsSZArray, "should be used only with multidimensional arrays");

            var ctor = module.ArrayMethods.GetArrayConstructor(arrayType);

            // idx1, idx2 --> array
            this.EmitOpCode(ILOpCode.Newobj, 1 - (int)arrayType.Rank);
            this.EmitToken(ctor, syntaxNode, diagnostics);
        }

        /// <summary>
        /// Generates code that loads an element of a multidimensional array
        /// </summary>
        internal void EmitArrayElementLoad(Microsoft.Cci.IArrayTypeReference arrayType, SyntaxNode syntaxNode, DiagnosticBag diagnostics)
        {
            Debug.Assert(!arrayType.IsSZArray, "should be used only with multidimensional arrays");

            var load = module.ArrayMethods.GetArrayGet(arrayType);

            // this, idx1, idx2 --> value
            this.EmitOpCode(ILOpCode.Call, -(int)arrayType.Rank);
            this.EmitToken(load, syntaxNode, diagnostics);
        }

        /// <summary>
        /// Generates code that loads an address of an element of a multidimensional array.
        /// </summary>
        internal void EmitArrayElementAddress(Microsoft.Cci.IArrayTypeReference arrayType, SyntaxNode syntaxNode, DiagnosticBag diagnostics)
        {
            Debug.Assert(!arrayType.IsSZArray, "should be used only with multidimensional arrays");

            var address = module.ArrayMethods.GetArrayAddress(arrayType);

            // this, idx1, idx2 --> &value
            this.EmitOpCode(ILOpCode.Call, -(int)arrayType.Rank);
            this.EmitToken(address, syntaxNode, diagnostics);
        }

        /// <summary>
        /// Generates code that stores an element of a multidimensional array.
        /// </summary>
        internal void EmitArrayElementStore(Cci.IArrayTypeReference arrayType, SyntaxNode syntaxNode, DiagnosticBag diagnostics)
        {
            Debug.Assert(!arrayType.IsSZArray, "should be used only with multidimensional arrays");

            var store = module.ArrayMethods.GetArraySet(arrayType);

            // this, idx1, idx2, value --> void
            this.EmitOpCode(ILOpCode.Call, -(2 + (int)arrayType.Rank));
            this.EmitToken(store, syntaxNode, diagnostics);
        }

        internal void EmitLoad(LocalOrParameter localOrParameter)
        {
            if (localOrParameter.Local != null)
            {
                EmitLocalLoad(localOrParameter.Local);
            }
            else
            {
                EmitLoadArgumentOpcode(localOrParameter.ParameterIndex);
            }
        }

        // Generate a "load local" opcode with the given slot number.
        internal void EmitLocalLoad(LocalDefinition local)
        {
            var slot = local.SlotIndex;
            switch (slot)
            {
                case 0: EmitOpCode(ILOpCode.Ldloc_0); break;
                case 1: EmitOpCode(ILOpCode.Ldloc_1); break;
                case 2: EmitOpCode(ILOpCode.Ldloc_2); break;
                case 3: EmitOpCode(ILOpCode.Ldloc_3); break;
                default:
                    if (slot < 0xFF)
                    {
                        EmitOpCode(ILOpCode.Ldloc_s);
                        EmitInt8(unchecked((sbyte)slot));
                    }
                    else
                    {
                        EmitOpCode(ILOpCode.Ldloc);
                        EmitInt32(slot);
                    }
                    break;
            }
        }

        // Generate a "store local" opcode with the given slot number.
        internal void EmitLocalStore(LocalDefinition local)
        {
            var slot = local.SlotIndex;
            switch (slot)
            {
                case 0: EmitOpCode(ILOpCode.Stloc_0); break;
                case 1: EmitOpCode(ILOpCode.Stloc_1); break;
                case 2: EmitOpCode(ILOpCode.Stloc_2); break;
                case 3: EmitOpCode(ILOpCode.Stloc_3); break;
                default:
                    if (slot < 0xFF)
                    {
                        EmitOpCode(ILOpCode.Stloc_s);
                        EmitInt8(unchecked((sbyte)slot));
                    }
                    else
                    {
                        EmitOpCode(ILOpCode.Stloc);
                        EmitInt32(slot);
                    }
                    break;
            }
        }

        internal void EmitLocalAddress(LocalDefinition local)
        {
            if (local.IsReference)
            {
                EmitLocalLoad(local);
            }
            else
            {
                int slot = local.SlotIndex;

                if (slot < 0xFF)
                {
                    EmitOpCode(ILOpCode.Ldloca_s);
                    EmitInt8(unchecked((sbyte)slot));
                }
                else
                {
                    EmitOpCode(ILOpCode.Ldloca);
                    EmitInt32(slot);
                }
            }
        }

        // Generate a "load argument" opcode with the given argument number.
        internal void EmitLoadArgumentOpcode(int argNumber)
        {
            switch (argNumber)
            {
                case 0: EmitOpCode(ILOpCode.Ldarg_0); break;
                case 1: EmitOpCode(ILOpCode.Ldarg_1); break;
                case 2: EmitOpCode(ILOpCode.Ldarg_2); break;
                case 3: EmitOpCode(ILOpCode.Ldarg_3); break;
                default:
                    if (argNumber < 0xFF)
                    {
                        EmitOpCode(ILOpCode.Ldarg_s);
                        EmitInt8(unchecked((sbyte)argNumber));
                    }
                    else
                    {
                        EmitOpCode(ILOpCode.Ldarg);
                        EmitInt32(argNumber);
                    }
                    break;
            }
        }

        internal void EmitLoadArgumentAddrOpcode(int argNumber)
        {
            if (argNumber < 0xFF)
            {
                EmitOpCode(ILOpCode.Ldarga_s);
                EmitInt8(unchecked((sbyte)argNumber));
            }
            else
            {
                EmitOpCode(ILOpCode.Ldarga);
                EmitInt32(argNumber);
            }
        }

        // Generate a "store argument" opcode with the given argument number.
        internal void EmitStoreArgumentOpcode(int argNumber)
        {
            if (argNumber < 0xFF)
            {
                EmitOpCode(ILOpCode.Starg_s);
                EmitInt8(unchecked((sbyte)argNumber));
            }
            else
            {
                EmitOpCode(ILOpCode.Starg);
                EmitInt32(argNumber);
            }
        }

        internal void EmitConstantValue(ConstantValue value)
        {
            ConstantValueTypeDiscriminator discriminator = value.Discriminator;

            switch (discriminator)
            {
                case ConstantValueTypeDiscriminator.Null:
                    EmitNullConstant();
                    break;
                case ConstantValueTypeDiscriminator.SByte:
                    EmitSByteConstant(value.SByteValue);
                    break;
                case ConstantValueTypeDiscriminator.Byte:
                    EmitByteConstant(value.ByteValue);
                    break;
                case ConstantValueTypeDiscriminator.UInt16:
                    EmitUShortConstant(value.UInt16Value);
                    break;
                case ConstantValueTypeDiscriminator.Char:
                    EmitUShortConstant(value.CharValue);
                    break;
                case ConstantValueTypeDiscriminator.Int16:
                    EmitShortConstant(value.Int16Value);
                    break;
                case ConstantValueTypeDiscriminator.Int32:
                case ConstantValueTypeDiscriminator.UInt32:
                    EmitIntConstant(value.Int32Value);
                    break;
                case ConstantValueTypeDiscriminator.Int64:
                case ConstantValueTypeDiscriminator.UInt64:
                    EmitLongConstant(value.Int64Value);
                    break;
                case ConstantValueTypeDiscriminator.Single:
                    EmitSingleConstant(value.SingleValue);
                    break;
                case ConstantValueTypeDiscriminator.Double:
                    EmitDoubleConstant(value.DoubleValue);
                    break;
                case ConstantValueTypeDiscriminator.String:
                    EmitStringConstant(value.StringValue);
                    break;
                case ConstantValueTypeDiscriminator.Boolean:
                    EmitBoolConstant(value.BooleanValue);
                    break;
                default:
                    throw ExceptionUtilities.UnexpectedValue(discriminator);
            }
        }

        // Generate a "load integer constant" opcode for the given value.
        internal void EmitIntConstant(int value)
        {
            ILOpCode code = ILOpCode.Nop;
            switch (value)
            {
                case -1: code = ILOpCode.Ldc_i4_m1; break;
                case 0: code = ILOpCode.Ldc_i4_0; break;
                case 1: code = ILOpCode.Ldc_i4_1; break;
                case 2: code = ILOpCode.Ldc_i4_2; break;
                case 3: code = ILOpCode.Ldc_i4_3; break;
                case 4: code = ILOpCode.Ldc_i4_4; break;
                case 5: code = ILOpCode.Ldc_i4_5; break;
                case 6: code = ILOpCode.Ldc_i4_6; break;
                case 7: code = ILOpCode.Ldc_i4_7; break;
                case 8: code = ILOpCode.Ldc_i4_8; break;
            }

            if (code != ILOpCode.Nop)
            {
                EmitOpCode(code);
            }
            else
            {
                if (unchecked((sbyte)value == value))
                {
                    EmitOpCode(ILOpCode.Ldc_i4_s);
                    EmitInt8(unchecked((sbyte)value));
                }
                else
                {
                    EmitOpCode(ILOpCode.Ldc_i4);
                    EmitInt32(value);
                }
            }
        }

        internal void EmitBoolConstant(bool value)
        {
            EmitIntConstant(value ? 1 : 0);
        }

        internal void EmitByteConstant(byte value)
        {
            EmitIntConstant((int)value);
        }

        internal void EmitSByteConstant(sbyte value)
        {
            EmitIntConstant((int)value);
        }

        internal void EmitShortConstant(short value)
        {
            EmitIntConstant((int)value);
        }

        internal void EmitUShortConstant(ushort value)
        {
            EmitIntConstant((int)value);
        }

        internal void EmitLongConstant(long value)
        {
            if (value >= int.MinValue && value <= int.MaxValue)
            {
                EmitIntConstant((int)value);
                EmitOpCode(ILOpCode.Conv_i8);
            }
            else if (value >= uint.MinValue && value <= uint.MaxValue)
            {
                EmitIntConstant(unchecked((int)value));
                EmitOpCode(ILOpCode.Conv_u8);
            }
            else
            {
                EmitOpCode(ILOpCode.Ldc_i8);
                EmitInt64(value);
            }
        }

        internal void EmitSingleConstant(float value)
        {
            EmitOpCode(ILOpCode.Ldc_r4);
            EmitFloat(value);
        }

        internal void EmitDoubleConstant(double value)
        {
            EmitOpCode(ILOpCode.Ldc_r8);
            EmitDouble(value);
        }

        internal void EmitNullConstant()
        {
            EmitOpCode(ILOpCode.Ldnull);
        }

        internal void EmitStringConstant(string value)
        {
            if (value == null)
            {
                EmitNullConstant();
            }
            else
            {
                EmitOpCode(ILOpCode.Ldstr);
                EmitToken(value);
            }
        }

        private void EmitInt8(sbyte int8)
        {
            this.GetCurrentWriter().WriteSByte(int8);
        }

        private void EmitInt32(int int32)
        {
            this.GetCurrentWriter().WriteInt32(int32);
        }

        private void EmitInt64(long int64)
        {
            this.GetCurrentWriter().WriteInt64(int64);
        }

        private void EmitFloat(float floatValue)
        {
            int int32 = BitConverter.ToInt32(BitConverter.GetBytes(floatValue), 0);
            this.GetCurrentWriter().WriteInt32(int32);
        }

        private void EmitDouble(double doubleValue)
        {
            long int64 = BitConverter.DoubleToInt64Bits(doubleValue);
            this.GetCurrentWriter().WriteInt64(int64);
        }

        private static void WriteOpCode(BlobBuilder writer, ILOpCode code)
        {
            var size = code.Size();
            if (size == 1)
            {
                writer.WriteByte((byte)code);
            }
            else
            {
                // IL opcodes that occupy two bytes are written to
                // the byte stream with the high-order byte first,
                // in contrast to the little-endian format of the
                // numeric arguments and tokens.

                Debug.Assert(size == 2);
                writer.WriteByte((byte)((ushort)code >> 8));
                writer.WriteByte((byte)((ushort)code & 0xff));
            }
        }

        private BlobBuilder GetCurrentWriter()
        {
            return this.GetCurrentBlock().Writer;
        }
    }
}
