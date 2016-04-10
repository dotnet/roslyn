// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

#if !SRM
using System;
#endif

#if SRM
namespace System.Reflection.Metadata
#else
namespace Microsoft.CodeAnalysis.CodeGen
#endif
{
#if SRM
    public
#endif
    static partial class ILOpCodeExtensions
    {
        /// <summary>
        /// Returns true of the specified op-code is a branch to a label.
        /// </summary>
        public static bool IsBranch(this ILOpCode opCode)
        {
            switch (opCode)
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

        /// <summary>
        /// Calculate the size of the specified branch instruction operand.
        /// </summary>
        /// <param name="opCode">Branch op-code.</param>
        /// <returns>1 if <paramref name="opCode"/> is a short branch or 4 if it is a long branch.</returns>
        /// <exception cref="ArgumentException">Specified <paramref name="opCode"/> is not a branch op-code.</exception>
        public static int GetBranchOperandSize(this ILOpCode opCode)
        {
            switch (opCode)
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

            throw new ArgumentException("Expected branch opcode.", nameof(opCode));
        }

        /// <summary>
        /// Get a short form of the specified branch op-code.
        /// </summary>
        /// <param name="opCode">Branch op-code.</param>
        /// <returns>Short form of the branch op-code.</returns>
        /// <exception cref="ArgumentException">Specified <paramref name="opCode"/> is not a branch op-code.</exception>
        public static ILOpCode GetShortBranch(this ILOpCode opCode)
        {
            switch (opCode)
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
                    return opCode;

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

            throw new ArgumentException("Expected branch opcode.", nameof(opCode));
        }

        /// <summary>
        /// Get a long form of the specified branch op-code.
        /// </summary>
        /// <param name="opCode">Branch op-code.</param>
        /// <returns>Long form of the branch op-code.</returns>
        /// <exception cref="ArgumentException">Specified <paramref name="opCode"/> is not a branch op-code.</exception>
        public static ILOpCode GetLongBranch(this ILOpCode opCode)
        {
            switch (opCode)
            {
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
                    return opCode;

                case ILOpCode.Br_s:
                    return ILOpCode.Br;

                case ILOpCode.Brfalse_s:
                    return ILOpCode.Brfalse;

                case ILOpCode.Brtrue_s:
                    return ILOpCode.Brtrue;

                case ILOpCode.Beq_s:
                    return ILOpCode.Beq;

                case ILOpCode.Bge_s:
                    return ILOpCode.Bge;

                case ILOpCode.Bgt_s:
                    return ILOpCode.Bgt;

                case ILOpCode.Ble_s:
                    return ILOpCode.Ble;

                case ILOpCode.Blt_s:
                    return ILOpCode.Blt;

                case ILOpCode.Bne_un_s:
                    return ILOpCode.Bne_un;

                case ILOpCode.Bge_un_s:
                    return ILOpCode.Bge_un;

                case ILOpCode.Bgt_un_s:
                    return ILOpCode.Bgt_un;

                case ILOpCode.Ble_un_s:
                    return ILOpCode.Ble_un;

                case ILOpCode.Blt_un_s:
                    return ILOpCode.Blt_un;

                case ILOpCode.Leave_s:
                    return ILOpCode.Leave;
            }

            throw new ArgumentException("Expected branch opcode.", nameof(opCode));
        }
    }
}
