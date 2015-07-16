// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Reflection.Emit;

namespace Microsoft.Cci
{
    internal static class InstructionOperandTypes
    {
        internal static OperandType ReadOperandType(ImmutableArray<byte> il, ref int position)
        {
            byte operation = il[position++];
            if (operation == 0xfe)
            {
                return (OperandType)TwoByte[il[position++]];
            }
            else
            {
                return (OperandType)OneByte[operation];
            }
        }

        // internal for testing
        internal static readonly byte[] OneByte = new byte[]
        {
            (byte)OperandType.InlineNone,           // nop
            (byte)OperandType.InlineNone,           // break
            (byte)OperandType.InlineNone,           // ldarg.0
            (byte)OperandType.InlineNone,           // ldarg.1
            (byte)OperandType.InlineNone,           // ldarg.2
            (byte)OperandType.InlineNone,           // ldarg.3
            (byte)OperandType.InlineNone,           // ldloc.0
            (byte)OperandType.InlineNone,           // ldloc.1
            (byte)OperandType.InlineNone,           // ldloc.2
            (byte)OperandType.InlineNone,           // ldloc.3
            (byte)OperandType.InlineNone,           // stloc.0
            (byte)OperandType.InlineNone,           // stloc.1
            (byte)OperandType.InlineNone,           // stloc.2
            (byte)OperandType.InlineNone,           // stloc.3
            (byte)OperandType.ShortInlineVar,       // ldarg.s
            (byte)OperandType.ShortInlineVar,       // ldarga.s
            (byte)OperandType.ShortInlineVar,       // starg.s
            (byte)OperandType.ShortInlineVar,       // ldloc.s
            (byte)OperandType.ShortInlineVar,       // ldloca.s
            (byte)OperandType.ShortInlineVar,       // stloc.s
            (byte)OperandType.InlineNone,           // ldnull
            (byte)OperandType.InlineNone,           // ldc.i4.m1
            (byte)OperandType.InlineNone,           // ldc.i4.0
            (byte)OperandType.InlineNone,           // ldc.i4.1
            (byte)OperandType.InlineNone,           // ldc.i4.2
            (byte)OperandType.InlineNone,           // ldc.i4.3
            (byte)OperandType.InlineNone,           // ldc.i4.4
            (byte)OperandType.InlineNone,           // ldc.i4.5
            (byte)OperandType.InlineNone,           // ldc.i4.6
            (byte)OperandType.InlineNone,           // ldc.i4.7
            (byte)OperandType.InlineNone,           // ldc.i4.8
            (byte)OperandType.ShortInlineI,         // ldc.i4.s
            (byte)OperandType.InlineI,              // ldc.i4
            (byte)OperandType.InlineI8,             // ldc.i8
            (byte)OperandType.ShortInlineR,         // ldc.r4
            (byte)OperandType.InlineR,              // ldc.r8
            0,
            (byte)OperandType.InlineNone,           // dup
            (byte)OperandType.InlineNone,           // pop
            (byte)OperandType.InlineMethod,         // jmp
            (byte)OperandType.InlineMethod,         // call
            (byte)OperandType.InlineSig,            // calli
            (byte)OperandType.InlineNone,           // ret
            (byte)OperandType.ShortInlineBrTarget,  // br.s
            (byte)OperandType.ShortInlineBrTarget,  // brfalse.s
            (byte)OperandType.ShortInlineBrTarget,  // brtrue.s
            (byte)OperandType.ShortInlineBrTarget,  // beq.s
            (byte)OperandType.ShortInlineBrTarget,  // bge.s
            (byte)OperandType.ShortInlineBrTarget,  // bgt.s
            (byte)OperandType.ShortInlineBrTarget,  // ble.s
            (byte)OperandType.ShortInlineBrTarget,  // blt.s
            (byte)OperandType.ShortInlineBrTarget,  // bne.un.s
            (byte)OperandType.ShortInlineBrTarget,  // bge.un.s
            (byte)OperandType.ShortInlineBrTarget,  // bgt.un.s
            (byte)OperandType.ShortInlineBrTarget,  // ble.un.s
            (byte)OperandType.ShortInlineBrTarget,  // blt.un.s
            0,
            0,
            0,
            0,
            0,
            0,
            0,
            0,
            0,
            0,
            0,
            0,
            0,
            (byte)OperandType.InlineSwitch,         // switch
            (byte)OperandType.InlineNone,           // ldind.i1
            (byte)OperandType.InlineNone,           // ldind.u1
            (byte)OperandType.InlineNone,           // ldind.i2
            (byte)OperandType.InlineNone,           // ldind.u2
            (byte)OperandType.InlineNone,           // ldind.i4
            (byte)OperandType.InlineNone,           // ldind.u4
            (byte)OperandType.InlineNone,           // ldind.i8
            (byte)OperandType.InlineNone,           // ldind.i
            (byte)OperandType.InlineNone,           // ldind.r4
            (byte)OperandType.InlineNone,           // ldind.r8
            (byte)OperandType.InlineNone,           // ldind.ref
            (byte)OperandType.InlineNone,           // stind.ref
            (byte)OperandType.InlineNone,           // stind.i1
            (byte)OperandType.InlineNone,           // stind.i2
            (byte)OperandType.InlineNone,           // stind.i4
            (byte)OperandType.InlineNone,           // stind.i8
            (byte)OperandType.InlineNone,           // stind.r4
            (byte)OperandType.InlineNone,           // stind.r8
            (byte)OperandType.InlineNone,           // add
            (byte)OperandType.InlineNone,           // sub
            (byte)OperandType.InlineNone,           // mul
            (byte)OperandType.InlineNone,           // div
            (byte)OperandType.InlineNone,           // div.un
            (byte)OperandType.InlineNone,           // rem
            (byte)OperandType.InlineNone,           // rem.un
            (byte)OperandType.InlineNone,           // and
            (byte)OperandType.InlineNone,           // or
            (byte)OperandType.InlineNone,           // xor
            (byte)OperandType.InlineNone,           // shl
            (byte)OperandType.InlineNone,           // shr
            (byte)OperandType.InlineNone,           // shr.un
            (byte)OperandType.InlineNone,           // neg
            (byte)OperandType.InlineNone,           // not
            (byte)OperandType.InlineNone,           // conv.i1
            (byte)OperandType.InlineNone,           // conv.i2
            (byte)OperandType.InlineNone,           // conv.i4
            (byte)OperandType.InlineNone,           // conv.i8
            (byte)OperandType.InlineNone,           // conv.r4
            (byte)OperandType.InlineNone,           // conv.r8
            (byte)OperandType.InlineNone,           // conv.u4
            (byte)OperandType.InlineNone,           // conv.u8
            (byte)OperandType.InlineMethod,         // callvirt
            (byte)OperandType.InlineType,           // cpobj
            (byte)OperandType.InlineType,           // ldobj
            (byte)OperandType.InlineString,         // ldstr
            (byte)OperandType.InlineMethod,         // newobj
            (byte)OperandType.InlineType,           // castclass
            (byte)OperandType.InlineType,           // isinst
            (byte)OperandType.InlineNone,           // conv.r.un
            0,
            0,
            (byte)OperandType.InlineType,           // unbox
            (byte)OperandType.InlineNone,           // throw
            (byte)OperandType.InlineField,          // ldfld
            (byte)OperandType.InlineField,          // ldflda
            (byte)OperandType.InlineField,          // stfld
            (byte)OperandType.InlineField,          // ldsfld
            (byte)OperandType.InlineField,          // ldsflda
            (byte)OperandType.InlineField,          // stsfld
            (byte)OperandType.InlineType,           // stobj
            (byte)OperandType.InlineNone,           // conv.ovf.i1.un
            (byte)OperandType.InlineNone,           // conv.ovf.i2.un
            (byte)OperandType.InlineNone,           // conv.ovf.i4.un
            (byte)OperandType.InlineNone,           // conv.ovf.i8.un
            (byte)OperandType.InlineNone,           // conv.ovf.u1.un
            (byte)OperandType.InlineNone,           // conv.ovf.u2.un
            (byte)OperandType.InlineNone,           // conv.ovf.u4.un
            (byte)OperandType.InlineNone,           // conv.ovf.u8.un
            (byte)OperandType.InlineNone,           // conv.ovf.i.un
            (byte)OperandType.InlineNone,           // conv.ovf.u.un
            (byte)OperandType.InlineType,           // box
            (byte)OperandType.InlineType,           // newarr
            (byte)OperandType.InlineNone,           // ldlen
            (byte)OperandType.InlineType,           // ldelema
            (byte)OperandType.InlineNone,           // ldelem.i1
            (byte)OperandType.InlineNone,           // ldelem.u1
            (byte)OperandType.InlineNone,           // ldelem.i2
            (byte)OperandType.InlineNone,           // ldelem.u2
            (byte)OperandType.InlineNone,           // ldelem.i4
            (byte)OperandType.InlineNone,           // ldelem.u4
            (byte)OperandType.InlineNone,           // ldelem.i8
            (byte)OperandType.InlineNone,           // ldelem.i
            (byte)OperandType.InlineNone,           // ldelem.r4
            (byte)OperandType.InlineNone,           // ldelem.r8
            (byte)OperandType.InlineNone,           // ldelem.ref
            (byte)OperandType.InlineNone,           // stelem.i
            (byte)OperandType.InlineNone,           // stelem.i1
            (byte)OperandType.InlineNone,           // stelem.i2
            (byte)OperandType.InlineNone,           // stelem.i4
            (byte)OperandType.InlineNone,           // stelem.i8
            (byte)OperandType.InlineNone,           // stelem.r4
            (byte)OperandType.InlineNone,           // stelem.r8
            (byte)OperandType.InlineNone,           // stelem.ref
            (byte)OperandType.InlineType,           // ldelem
            (byte)OperandType.InlineType,           // stelem
            (byte)OperandType.InlineType,           // unbox.any
            0,
            0,
            0,
            0,
            0,
            0,
            0,
            0,
            0,
            0,
            0,
            0,
            0,
            (byte)OperandType.InlineNone,           // conv.ovf.i1
            (byte)OperandType.InlineNone,           // conv.ovf.u1
            (byte)OperandType.InlineNone,           // conv.ovf.i2
            (byte)OperandType.InlineNone,           // conv.ovf.u2
            (byte)OperandType.InlineNone,           // conv.ovf.i4
            (byte)OperandType.InlineNone,           // conv.ovf.u4
            (byte)OperandType.InlineNone,           // conv.ovf.i8
            (byte)OperandType.InlineNone,           // conv.ovf.u8
            0,
            0,
            0,
            0,
            0,
            0,
            0,
            (byte)OperandType.InlineType,           // refanyval
            (byte)OperandType.InlineNone,           // ckfinite
            0,
            0,
            (byte)OperandType.InlineType,           // mkrefany
            0,
            0,
            0,
            0,
            0,
            0,
            0,
            0,
            0,
            (byte)OperandType.InlineTok,            // ldtoken
            (byte)OperandType.InlineNone,           // conv.u2
            (byte)OperandType.InlineNone,           // conv.u1
            (byte)OperandType.InlineNone,           // conv.i
            (byte)OperandType.InlineNone,           // conv.ovf.i
            (byte)OperandType.InlineNone,           // conv.ovf.u
            (byte)OperandType.InlineNone,           // add.ovf
            (byte)OperandType.InlineNone,           // add.ovf.un
            (byte)OperandType.InlineNone,           // mul.ovf
            (byte)OperandType.InlineNone,           // mul.ovf.un
            (byte)OperandType.InlineNone,           // sub.ovf
            (byte)OperandType.InlineNone,           // sub.ovf.un
            (byte)OperandType.InlineNone,           // endfinally
            0,
            (byte)OperandType.ShortInlineBrTarget,  // leave.s
            (byte)OperandType.InlineNone,           // stind.i
            (byte)OperandType.InlineNone,           // conv.u            (0xe0)
            0,
            0,
            0,
            0,
            0,
            0,
            0,
            0,
            0,
            0,
            0,
            0,
            0,
            0,
            0,
            0,
            0,
            0,
            0,
            0,
            0,
            0,
            0,
            0,
            0,
            0,
            0,
            0,
            0,
            0,
        };

        // internal for testing
        internal static readonly byte[] TwoByte = new byte[]
        {
            (byte)OperandType.InlineNone,           // arglist           (0xfe 0x00)
            (byte)OperandType.InlineNone,           // ceq
            (byte)OperandType.InlineNone,           // cgt
            (byte)OperandType.InlineNone,           // cgt.un
            (byte)OperandType.InlineNone,           // clt
            (byte)OperandType.InlineNone,           // clt.un
            (byte)OperandType.InlineMethod,         // ldftn
            (byte)OperandType.InlineMethod,         // ldvirtftn
            0,
            (byte)OperandType.InlineVar,            // ldarg
            (byte)OperandType.InlineVar,            // ldarga
            (byte)OperandType.InlineVar,            // starg
            (byte)OperandType.InlineVar,            // ldloc
            (byte)OperandType.InlineVar,            // ldloca
            (byte)OperandType.InlineVar,            // stloc
            (byte)OperandType.InlineNone,           // localloc
            0,
            (byte)OperandType.InlineNone,           // endfilter
            (byte)OperandType.ShortInlineI,         // unaligned.
            (byte)OperandType.InlineNone,           // volatile.
            (byte)OperandType.InlineNone,           // tail.
            (byte)OperandType.InlineType,           // initobj
            (byte)OperandType.InlineType,           // constrained.
            (byte)OperandType.InlineNone,           // cpblk
            (byte)OperandType.InlineNone,           // initblk
            0,
            (byte)OperandType.InlineNone,           // rethrow
            0,
            (byte)OperandType.InlineType,           // sizeof
            (byte)OperandType.InlineNone,           // refanytype
            (byte)OperandType.InlineNone,           // readonly.         (0xfe 0x1e)
        };
    }
}
