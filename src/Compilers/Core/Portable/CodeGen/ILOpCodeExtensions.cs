// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Diagnostics;
using System.Reflection.Metadata;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CodeGen
{
    internal static class ILOpCodeExtensions
    {
        public static int Size(this ILOpCode opcode)
        {
            int code = (int)opcode;
            if (code <= 0xff)
            {
                Debug.Assert(code < 0xf0);
                return 1;
            }
            else
            {
                Debug.Assert((code & 0xff00) == 0xfe00);
                return 2;
            }
        }

        public static int BranchOperandSize(this ILOpCode opcode)
        {
            switch (opcode)
            {
                case ILOpCode.Br_s:
                case ILOpCode.Brfalse_s:
                case ILOpCode.Brtrue_s:
                case ILOpCode.Beq_s:
                case ILOpCode.Bge_s:
                case ILOpCode.Bgt_s:
                case ILOpCode.Ble_s:
                case ILOpCode.Blt_s:
                case ILOpCode.Bne_un_s:
                case ILOpCode.Bge_un_s:
                case ILOpCode.Bgt_un_s:
                case ILOpCode.Ble_un_s:
                case ILOpCode.Blt_un_s:
                case ILOpCode.Leave_s:
                    return 1;

                case ILOpCode.Br:
                case ILOpCode.Brfalse:
                case ILOpCode.Brtrue:
                case ILOpCode.Beq:
                case ILOpCode.Bge:
                case ILOpCode.Bgt:
                case ILOpCode.Ble:
                case ILOpCode.Blt:
                case ILOpCode.Bne_un:
                case ILOpCode.Bge_un:
                case ILOpCode.Bgt_un:
                case ILOpCode.Ble_un:
                case ILOpCode.Blt_un:
                case ILOpCode.Leave:
                    return 4;
            }

            throw ExceptionUtilities.UnexpectedValue(opcode);
        }

        public static ILOpCode GetShortOpcode(this ILOpCode opcode)
        {
            switch (opcode)
            {
                case ILOpCode.Br:
                    return ILOpCode.Br_s;

                case ILOpCode.Brfalse:
                    return ILOpCode.Brfalse_s;

                case ILOpCode.Brtrue:
                    return ILOpCode.Brtrue_s;

                case ILOpCode.Beq:
                    return ILOpCode.Beq_s;

                case ILOpCode.Bge:
                    return ILOpCode.Bge_s;

                case ILOpCode.Bgt:
                    return ILOpCode.Bgt_s;

                case ILOpCode.Ble:
                    return ILOpCode.Ble_s;

                case ILOpCode.Blt:
                    return ILOpCode.Blt_s;

                case ILOpCode.Bne_un:
                    return ILOpCode.Bne_un_s;

                case ILOpCode.Bge_un:
                    return ILOpCode.Bge_un_s;

                case ILOpCode.Bgt_un:
                    return ILOpCode.Bgt_un_s;

                case ILOpCode.Ble_un:
                    return ILOpCode.Ble_un_s;

                case ILOpCode.Blt_un:
                    return ILOpCode.Blt_un_s;

                case ILOpCode.Leave:
                    return ILOpCode.Leave_s;
            }

            throw ExceptionUtilities.UnexpectedValue(opcode);
        }

        public static ILOpCode GetLeaveOpcode(this ILOpCode opcode)
        {
            switch (opcode)
            {
                case ILOpCode.Br:
                    return ILOpCode.Leave;

                case ILOpCode.Br_s:
                    return ILOpCode.Leave_s;
            }

            throw ExceptionUtilities.UnexpectedValue(opcode);
        }

        public static bool HasVariableStackBehavior(this ILOpCode opcode)
        {
            switch (opcode)
            {
                case ILOpCode.Call:
                case ILOpCode.Calli:
                case ILOpCode.Callvirt:
                case ILOpCode.Newobj:
                case ILOpCode.Ret:
                    return true;
            }
            return false;
        }

        /// <summary>
        /// These opcodes represent control transfer.
        /// They should not appear inside basic blocks.
        /// </summary>
        public static bool IsControlTransfer(this ILOpCode opcode)
        {
            if (opcode.IsBranchToLabel())
            {
                return true;
            }

            switch (opcode)
            {
                case ILOpCode.Ret:
                case ILOpCode.Throw:
                case ILOpCode.Rethrow:
                case ILOpCode.Endfilter:
                case ILOpCode.Endfinally:
                case ILOpCode.Switch:
                case ILOpCode.Jmp:
                    return true;
            }
            return false;
        }

        /// <summary>
        /// Opcodes that represents a branch to a label.
        /// </summary>
        public static bool IsBranchToLabel(this ILOpCode opcode)
        {
            switch (opcode)
            {
                case ILOpCode.Br:
                case ILOpCode.Br_s:
                case ILOpCode.Brtrue:
                case ILOpCode.Brtrue_s:
                case ILOpCode.Brfalse:
                case ILOpCode.Brfalse_s:
                case ILOpCode.Beq:
                case ILOpCode.Beq_s:
                case ILOpCode.Bne_un:
                case ILOpCode.Bne_un_s:
                case ILOpCode.Bge:
                case ILOpCode.Bge_s:
                case ILOpCode.Bge_un:
                case ILOpCode.Bge_un_s:
                case ILOpCode.Bgt:
                case ILOpCode.Bgt_s:
                case ILOpCode.Bgt_un:
                case ILOpCode.Bgt_un_s:
                case ILOpCode.Ble:
                case ILOpCode.Ble_s:
                case ILOpCode.Ble_un:
                case ILOpCode.Ble_un_s:
                case ILOpCode.Blt:
                case ILOpCode.Blt_s:
                case ILOpCode.Blt_un:
                case ILOpCode.Blt_un_s:
                case ILOpCode.Leave:
                case ILOpCode.Leave_s:
                    return true;
            }
            return false;
        }

        public static bool IsConditionalBranch(this ILOpCode opcode)
        {
            switch (opcode)
            {
                case ILOpCode.Brtrue:
                case ILOpCode.Brtrue_s:
                case ILOpCode.Brfalse:
                case ILOpCode.Brfalse_s:
                case ILOpCode.Beq:
                case ILOpCode.Beq_s:
                case ILOpCode.Bne_un:
                case ILOpCode.Bne_un_s:
                case ILOpCode.Bge:
                case ILOpCode.Bge_s:
                case ILOpCode.Bge_un:
                case ILOpCode.Bge_un_s:
                case ILOpCode.Bgt:
                case ILOpCode.Bgt_s:
                case ILOpCode.Bgt_un:
                case ILOpCode.Bgt_un_s:
                case ILOpCode.Ble:
                case ILOpCode.Ble_s:
                case ILOpCode.Ble_un:
                case ILOpCode.Ble_un_s:
                case ILOpCode.Blt:
                case ILOpCode.Blt_s:
                case ILOpCode.Blt_un:
                case ILOpCode.Blt_un_s:
                    return true;
                    // these are not conditional

                    //case ILOpCode.Br:
                    //case ILOpCode.Br_s:
                    //case ILOpCode.Leave:
                    //case ILOpCode.Leave_s:

                    // this is treated specially. It will not use regular single label
                    //case ILOpCode.Switch
            }

            return false;
        }

        public static bool IsRelationalBranch(this ILOpCode opcode)
        {
            switch (opcode)
            {
                case ILOpCode.Beq:
                case ILOpCode.Beq_s:
                case ILOpCode.Bne_un:
                case ILOpCode.Bne_un_s:
                case ILOpCode.Bge:
                case ILOpCode.Bge_s:
                case ILOpCode.Bge_un:
                case ILOpCode.Bge_un_s:
                case ILOpCode.Bgt:
                case ILOpCode.Bgt_s:
                case ILOpCode.Bgt_un:
                case ILOpCode.Bgt_un_s:
                case ILOpCode.Ble:
                case ILOpCode.Ble_s:
                case ILOpCode.Ble_un:
                case ILOpCode.Ble_un_s:
                case ILOpCode.Blt:
                case ILOpCode.Blt_s:
                case ILOpCode.Blt_un:
                case ILOpCode.Blt_un_s:
                    return true;
            }

            return false;
        }

        /// <summary>
        /// Opcodes that represents a branch to a label.
        /// </summary>
        public static bool CanFallThrough(this ILOpCode opcode)
        {
            //12.4.2.8.1  Most instructions can allow control to fall through after their execution—only unconditional branches,
            //ret, jmp, leave(.s), endfinally, endfault, endfilter, throw, and rethrow do not.
            switch (opcode)
            {
                case ILOpCode.Br:
                case ILOpCode.Br_s:
                case ILOpCode.Ret:
                case ILOpCode.Jmp:
                case ILOpCode.Throw:
                //NOTE: from the codegen view endfilter is a logical  "brfalse <continueHandlerSearch>" 
                //      endfilter must be used once at the end of the filter and must be lexically followed by the handler
                //      to which the control returns if filter result was 1.
                //case ILOpCode.Endfilter:
                case ILOpCode.Endfinally:
                case ILOpCode.Leave:
                case ILOpCode.Leave_s:
                case ILOpCode.Rethrow:
                    return false;
            }

            return true;
        }

        public static int NetStackBehavior(this ILOpCode opcode)
        {
            Debug.Assert(!opcode.HasVariableStackBehavior());
            return opcode.StackPushCount() - opcode.StackPopCount();
        }

        public static int StackPopCount(this ILOpCode opcode)
        {
            switch (opcode)
            {
                case ILOpCode.Nop:
                case ILOpCode.Break:
                case ILOpCode.Ldarg_0:
                case ILOpCode.Ldarg_1:
                case ILOpCode.Ldarg_2:
                case ILOpCode.Ldarg_3:
                case ILOpCode.Ldloc_0:
                case ILOpCode.Ldloc_1:
                case ILOpCode.Ldloc_2:
                case ILOpCode.Ldloc_3:
                    return 0;
                case ILOpCode.Stloc_0:
                case ILOpCode.Stloc_1:
                case ILOpCode.Stloc_2:
                case ILOpCode.Stloc_3:
                    return 1;
                case ILOpCode.Ldarg_s:
                case ILOpCode.Ldarga_s:
                    return 0;
                case ILOpCode.Starg_s:
                    return 1;
                case ILOpCode.Ldloc_s:
                case ILOpCode.Ldloca_s:
                    return 0;
                case ILOpCode.Stloc_s:
                    return 1;
                case ILOpCode.Ldnull:
                case ILOpCode.Ldc_i4_m1:
                case ILOpCode.Ldc_i4_0:
                case ILOpCode.Ldc_i4_1:
                case ILOpCode.Ldc_i4_2:
                case ILOpCode.Ldc_i4_3:
                case ILOpCode.Ldc_i4_4:
                case ILOpCode.Ldc_i4_5:
                case ILOpCode.Ldc_i4_6:
                case ILOpCode.Ldc_i4_7:
                case ILOpCode.Ldc_i4_8:
                case ILOpCode.Ldc_i4_s:
                case ILOpCode.Ldc_i4:
                case ILOpCode.Ldc_i8:
                case ILOpCode.Ldc_r4:
                case ILOpCode.Ldc_r8:
                    return 0;
                case ILOpCode.Dup:
                case ILOpCode.Pop:
                    return 1;
                case ILOpCode.Jmp:
                    return 0;
                case ILOpCode.Call:
                case ILOpCode.Calli:
                case ILOpCode.Ret:
                    return -1; // Variable
                case ILOpCode.Br_s:
                    return 0;
                case ILOpCode.Brfalse_s:
                case ILOpCode.Brtrue_s:
                    return 1;
                case ILOpCode.Beq_s:
                case ILOpCode.Bge_s:
                case ILOpCode.Bgt_s:
                case ILOpCode.Ble_s:
                case ILOpCode.Blt_s:
                case ILOpCode.Bne_un_s:
                case ILOpCode.Bge_un_s:
                case ILOpCode.Bgt_un_s:
                case ILOpCode.Ble_un_s:
                case ILOpCode.Blt_un_s:
                    return 2;
                case ILOpCode.Br:
                    return 0;
                case ILOpCode.Brfalse:
                case ILOpCode.Brtrue:
                    return 1;
                case ILOpCode.Beq:
                case ILOpCode.Bge:
                case ILOpCode.Bgt:
                case ILOpCode.Ble:
                case ILOpCode.Blt:
                case ILOpCode.Bne_un:
                case ILOpCode.Bge_un:
                case ILOpCode.Bgt_un:
                case ILOpCode.Ble_un:
                case ILOpCode.Blt_un:
                    return 2;
                case ILOpCode.Switch:
                case ILOpCode.Ldind_i1:
                case ILOpCode.Ldind_u1:
                case ILOpCode.Ldind_i2:
                case ILOpCode.Ldind_u2:
                case ILOpCode.Ldind_i4:
                case ILOpCode.Ldind_u4:
                case ILOpCode.Ldind_i8:
                case ILOpCode.Ldind_i:
                case ILOpCode.Ldind_r4:
                case ILOpCode.Ldind_r8:
                case ILOpCode.Ldind_ref:
                    return 1;
                case ILOpCode.Stind_ref:
                case ILOpCode.Stind_i1:
                case ILOpCode.Stind_i2:
                case ILOpCode.Stind_i4:
                case ILOpCode.Stind_i8:
                case ILOpCode.Stind_r4:
                case ILOpCode.Stind_r8:
                case ILOpCode.Add:
                case ILOpCode.Sub:
                case ILOpCode.Mul:
                case ILOpCode.Div:
                case ILOpCode.Div_un:
                case ILOpCode.Rem:
                case ILOpCode.Rem_un:
                case ILOpCode.And:
                case ILOpCode.Or:
                case ILOpCode.Xor:
                case ILOpCode.Shl:
                case ILOpCode.Shr:
                case ILOpCode.Shr_un:
                    return 2;
                case ILOpCode.Neg:
                case ILOpCode.Not:
                case ILOpCode.Conv_i1:
                case ILOpCode.Conv_i2:
                case ILOpCode.Conv_i4:
                case ILOpCode.Conv_i8:
                case ILOpCode.Conv_r4:
                case ILOpCode.Conv_r8:
                case ILOpCode.Conv_u4:
                case ILOpCode.Conv_u8:
                    return 1;
                case ILOpCode.Callvirt:
                    return -1; // Variable
                case ILOpCode.Cpobj:
                    return 2;
                case ILOpCode.Ldobj:
                    return 1;
                case ILOpCode.Ldstr:
                    return 0;
                case ILOpCode.Newobj:
                    return -1; // Variable
                case ILOpCode.Castclass:
                case ILOpCode.Isinst:
                case ILOpCode.Conv_r_un:
                case ILOpCode.Unbox:
                case ILOpCode.Throw:
                case ILOpCode.Ldfld:
                case ILOpCode.Ldflda:
                    return 1;
                case ILOpCode.Stfld:
                    return 2;
                case ILOpCode.Ldsfld:
                case ILOpCode.Ldsflda:
                    return 0;
                case ILOpCode.Stsfld:
                    return 1;
                case ILOpCode.Stobj:
                    return 2;
                case ILOpCode.Conv_ovf_i1_un:
                case ILOpCode.Conv_ovf_i2_un:
                case ILOpCode.Conv_ovf_i4_un:
                case ILOpCode.Conv_ovf_i8_un:
                case ILOpCode.Conv_ovf_u1_un:
                case ILOpCode.Conv_ovf_u2_un:
                case ILOpCode.Conv_ovf_u4_un:
                case ILOpCode.Conv_ovf_u8_un:
                case ILOpCode.Conv_ovf_i_un:
                case ILOpCode.Conv_ovf_u_un:
                case ILOpCode.Box:
                case ILOpCode.Newarr:
                case ILOpCode.Ldlen:
                    return 1;
                case ILOpCode.Ldelema:
                case ILOpCode.Ldelem_i1:
                case ILOpCode.Ldelem_u1:
                case ILOpCode.Ldelem_i2:
                case ILOpCode.Ldelem_u2:
                case ILOpCode.Ldelem_i4:
                case ILOpCode.Ldelem_u4:
                case ILOpCode.Ldelem_i8:
                case ILOpCode.Ldelem_i:
                case ILOpCode.Ldelem_r4:
                case ILOpCode.Ldelem_r8:
                case ILOpCode.Ldelem_ref:
                    return 2;
                case ILOpCode.Stelem_i:
                case ILOpCode.Stelem_i1:
                case ILOpCode.Stelem_i2:
                case ILOpCode.Stelem_i4:
                case ILOpCode.Stelem_i8:
                case ILOpCode.Stelem_r4:
                case ILOpCode.Stelem_r8:
                case ILOpCode.Stelem_ref:
                    return 3;
                case ILOpCode.Ldelem:
                    return 2;
                case ILOpCode.Stelem:
                    return 3;
                case ILOpCode.Unbox_any:
                case ILOpCode.Conv_ovf_i1:
                case ILOpCode.Conv_ovf_u1:
                case ILOpCode.Conv_ovf_i2:
                case ILOpCode.Conv_ovf_u2:
                case ILOpCode.Conv_ovf_i4:
                case ILOpCode.Conv_ovf_u4:
                case ILOpCode.Conv_ovf_i8:
                case ILOpCode.Conv_ovf_u8:
                case ILOpCode.Refanyval:
                case ILOpCode.Ckfinite:
                case ILOpCode.Mkrefany:
                    return 1;
                case ILOpCode.Ldtoken:
                    return 0;
                case ILOpCode.Conv_u2:
                case ILOpCode.Conv_u1:
                case ILOpCode.Conv_i:
                case ILOpCode.Conv_ovf_i:
                case ILOpCode.Conv_ovf_u:
                    return 1;
                case ILOpCode.Add_ovf:
                case ILOpCode.Add_ovf_un:
                case ILOpCode.Mul_ovf:
                case ILOpCode.Mul_ovf_un:
                case ILOpCode.Sub_ovf:
                case ILOpCode.Sub_ovf_un:
                    return 2;
                case ILOpCode.Endfinally:
                case ILOpCode.Leave:
                case ILOpCode.Leave_s:
                    return 0;
                case ILOpCode.Stind_i:
                    return 2;
                case ILOpCode.Conv_u:
                    return 1;
                case ILOpCode.Arglist:
                    return 0;
                case ILOpCode.Ceq:
                case ILOpCode.Cgt:
                case ILOpCode.Cgt_un:
                case ILOpCode.Clt:
                case ILOpCode.Clt_un:
                    return 2;
                case ILOpCode.Ldftn:
                    return 0;
                case ILOpCode.Ldvirtftn:
                    return 1;
                case ILOpCode.Ldarg:
                case ILOpCode.Ldarga:
                    return 0;
                case ILOpCode.Starg:
                    return 1;
                case ILOpCode.Ldloc:
                case ILOpCode.Ldloca:
                    return 0;
                case ILOpCode.Stloc:
                case ILOpCode.Localloc:
                case ILOpCode.Endfilter:
                    return 1;
                case ILOpCode.Unaligned:
                case ILOpCode.Volatile:
                case ILOpCode.Tail:
                    return 0;
                case ILOpCode.Initobj:
                    return 1;
                case ILOpCode.Constrained:
                    return 0;
                case ILOpCode.Cpblk:
                case ILOpCode.Initblk:
                    return 3;
                case ILOpCode.Rethrow:
                case ILOpCode.Sizeof:
                    return 0;
                case ILOpCode.Refanytype:
                    return 1;
                case ILOpCode.Readonly:
                    return 0;
            }

            throw ExceptionUtilities.UnexpectedValue(opcode);
        }

        public static int StackPushCount(this ILOpCode opcode)
        {
            switch (opcode)
            {
                case ILOpCode.Nop:
                case ILOpCode.Break:
                    return 0;
                case ILOpCode.Ldarg_0:
                case ILOpCode.Ldarg_1:
                case ILOpCode.Ldarg_2:
                case ILOpCode.Ldarg_3:
                case ILOpCode.Ldloc_0:
                case ILOpCode.Ldloc_1:
                case ILOpCode.Ldloc_2:
                case ILOpCode.Ldloc_3:
                    return 1;
                case ILOpCode.Stloc_0:
                case ILOpCode.Stloc_1:
                case ILOpCode.Stloc_2:
                case ILOpCode.Stloc_3:
                    return 0;
                case ILOpCode.Ldarg_s:
                case ILOpCode.Ldarga_s:
                    return 1;
                case ILOpCode.Starg_s:
                    return 0;
                case ILOpCode.Ldloc_s:
                case ILOpCode.Ldloca_s:
                    return 1;
                case ILOpCode.Stloc_s:
                    return 0;
                case ILOpCode.Ldnull:
                case ILOpCode.Ldc_i4_m1:
                case ILOpCode.Ldc_i4_0:
                case ILOpCode.Ldc_i4_1:
                case ILOpCode.Ldc_i4_2:
                case ILOpCode.Ldc_i4_3:
                case ILOpCode.Ldc_i4_4:
                case ILOpCode.Ldc_i4_5:
                case ILOpCode.Ldc_i4_6:
                case ILOpCode.Ldc_i4_7:
                case ILOpCode.Ldc_i4_8:
                case ILOpCode.Ldc_i4_s:
                case ILOpCode.Ldc_i4:
                case ILOpCode.Ldc_i8:
                case ILOpCode.Ldc_r4:
                case ILOpCode.Ldc_r8:
                    return 1;
                case ILOpCode.Dup:
                    return 2;
                case ILOpCode.Pop:
                case ILOpCode.Jmp:
                    return 0;
                case ILOpCode.Call:
                case ILOpCode.Calli:
                    return -1; // Variable
                case ILOpCode.Ret:
                case ILOpCode.Br_s:
                case ILOpCode.Brfalse_s:
                case ILOpCode.Brtrue_s:
                case ILOpCode.Beq_s:
                case ILOpCode.Bge_s:
                case ILOpCode.Bgt_s:
                case ILOpCode.Ble_s:
                case ILOpCode.Blt_s:
                case ILOpCode.Bne_un_s:
                case ILOpCode.Bge_un_s:
                case ILOpCode.Bgt_un_s:
                case ILOpCode.Ble_un_s:
                case ILOpCode.Blt_un_s:
                case ILOpCode.Br:
                case ILOpCode.Brfalse:
                case ILOpCode.Brtrue:
                case ILOpCode.Beq:
                case ILOpCode.Bge:
                case ILOpCode.Bgt:
                case ILOpCode.Ble:
                case ILOpCode.Blt:
                case ILOpCode.Bne_un:
                case ILOpCode.Bge_un:
                case ILOpCode.Bgt_un:
                case ILOpCode.Ble_un:
                case ILOpCode.Blt_un:
                case ILOpCode.Switch:
                    return 0;
                case ILOpCode.Ldind_i1:
                case ILOpCode.Ldind_u1:
                case ILOpCode.Ldind_i2:
                case ILOpCode.Ldind_u2:
                case ILOpCode.Ldind_i4:
                case ILOpCode.Ldind_u4:
                case ILOpCode.Ldind_i8:
                case ILOpCode.Ldind_i:
                case ILOpCode.Ldind_r4:
                case ILOpCode.Ldind_r8:
                case ILOpCode.Ldind_ref:
                    return 1;
                case ILOpCode.Stind_ref:
                case ILOpCode.Stind_i1:
                case ILOpCode.Stind_i2:
                case ILOpCode.Stind_i4:
                case ILOpCode.Stind_i8:
                case ILOpCode.Stind_r4:
                case ILOpCode.Stind_r8:
                    return 0;
                case ILOpCode.Add:
                case ILOpCode.Sub:
                case ILOpCode.Mul:
                case ILOpCode.Div:
                case ILOpCode.Div_un:
                case ILOpCode.Rem:
                case ILOpCode.Rem_un:
                case ILOpCode.And:
                case ILOpCode.Or:
                case ILOpCode.Xor:
                case ILOpCode.Shl:
                case ILOpCode.Shr:
                case ILOpCode.Shr_un:
                case ILOpCode.Neg:
                case ILOpCode.Not:
                case ILOpCode.Conv_i1:
                case ILOpCode.Conv_i2:
                case ILOpCode.Conv_i4:
                case ILOpCode.Conv_i8:
                case ILOpCode.Conv_r4:
                case ILOpCode.Conv_r8:
                case ILOpCode.Conv_u4:
                case ILOpCode.Conv_u8:
                    return 1;
                case ILOpCode.Callvirt:
                    return -1; // Variable
                case ILOpCode.Cpobj:
                    return 0;
                case ILOpCode.Ldobj:
                case ILOpCode.Ldstr:
                case ILOpCode.Newobj:
                case ILOpCode.Castclass:
                case ILOpCode.Isinst:
                case ILOpCode.Conv_r_un:
                case ILOpCode.Unbox:
                    return 1;
                case ILOpCode.Throw:
                    return 0;
                case ILOpCode.Ldfld:
                case ILOpCode.Ldflda:
                    return 1;
                case ILOpCode.Stfld:
                    return 0;
                case ILOpCode.Ldsfld:
                case ILOpCode.Ldsflda:
                    return 1;
                case ILOpCode.Stsfld:
                case ILOpCode.Stobj:
                    return 0;
                case ILOpCode.Conv_ovf_i1_un:
                case ILOpCode.Conv_ovf_i2_un:
                case ILOpCode.Conv_ovf_i4_un:
                case ILOpCode.Conv_ovf_i8_un:
                case ILOpCode.Conv_ovf_u1_un:
                case ILOpCode.Conv_ovf_u2_un:
                case ILOpCode.Conv_ovf_u4_un:
                case ILOpCode.Conv_ovf_u8_un:
                case ILOpCode.Conv_ovf_i_un:
                case ILOpCode.Conv_ovf_u_un:
                case ILOpCode.Box:
                case ILOpCode.Newarr:
                case ILOpCode.Ldlen:
                case ILOpCode.Ldelema:
                case ILOpCode.Ldelem_i1:
                case ILOpCode.Ldelem_u1:
                case ILOpCode.Ldelem_i2:
                case ILOpCode.Ldelem_u2:
                case ILOpCode.Ldelem_i4:
                case ILOpCode.Ldelem_u4:
                case ILOpCode.Ldelem_i8:
                case ILOpCode.Ldelem_i:
                case ILOpCode.Ldelem_r4:
                case ILOpCode.Ldelem_r8:
                case ILOpCode.Ldelem_ref:
                    return 1;
                case ILOpCode.Stelem_i:
                case ILOpCode.Stelem_i1:
                case ILOpCode.Stelem_i2:
                case ILOpCode.Stelem_i4:
                case ILOpCode.Stelem_i8:
                case ILOpCode.Stelem_r4:
                case ILOpCode.Stelem_r8:
                case ILOpCode.Stelem_ref:
                    return 0;
                case ILOpCode.Ldelem:
                    return 1;
                case ILOpCode.Stelem:
                    return 0;
                case ILOpCode.Unbox_any:
                case ILOpCode.Conv_ovf_i1:
                case ILOpCode.Conv_ovf_u1:
                case ILOpCode.Conv_ovf_i2:
                case ILOpCode.Conv_ovf_u2:
                case ILOpCode.Conv_ovf_i4:
                case ILOpCode.Conv_ovf_u4:
                case ILOpCode.Conv_ovf_i8:
                case ILOpCode.Conv_ovf_u8:
                case ILOpCode.Refanyval:
                case ILOpCode.Ckfinite:
                case ILOpCode.Mkrefany:
                case ILOpCode.Ldtoken:
                case ILOpCode.Conv_u2:
                case ILOpCode.Conv_u1:
                case ILOpCode.Conv_i:
                case ILOpCode.Conv_ovf_i:
                case ILOpCode.Conv_ovf_u:
                case ILOpCode.Add_ovf:
                case ILOpCode.Add_ovf_un:
                case ILOpCode.Mul_ovf:
                case ILOpCode.Mul_ovf_un:
                case ILOpCode.Sub_ovf:
                case ILOpCode.Sub_ovf_un:
                    return 1;
                case ILOpCode.Endfinally:
                case ILOpCode.Leave:
                case ILOpCode.Leave_s:
                case ILOpCode.Stind_i:
                    return 0;
                case ILOpCode.Conv_u:
                case ILOpCode.Arglist:
                case ILOpCode.Ceq:
                case ILOpCode.Cgt:
                case ILOpCode.Cgt_un:
                case ILOpCode.Clt:
                case ILOpCode.Clt_un:
                case ILOpCode.Ldftn:
                case ILOpCode.Ldvirtftn:
                case ILOpCode.Ldarg:
                case ILOpCode.Ldarga:
                    return 1;
                case ILOpCode.Starg:
                    return 0;
                case ILOpCode.Ldloc:
                case ILOpCode.Ldloca:
                    return 1;
                case ILOpCode.Stloc:
                    return 0;
                case ILOpCode.Localloc:
                    return 1;
                case ILOpCode.Endfilter:
                case ILOpCode.Unaligned:
                case ILOpCode.Volatile:
                case ILOpCode.Tail:
                case ILOpCode.Initobj:
                case ILOpCode.Constrained:
                case ILOpCode.Cpblk:
                case ILOpCode.Initblk:
                case ILOpCode.Rethrow:
                    return 0;
                case ILOpCode.Sizeof:
                case ILOpCode.Refanytype:
                    return 1;
                case ILOpCode.Readonly:
                    return 0;
            }

            throw ExceptionUtilities.UnexpectedValue(opcode);
        }
    }
}
