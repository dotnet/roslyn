// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Reflection.Metadata;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeGen;
using Roslyn.Test.MetadataUtilities;
using Roslyn.Utilities;
using Cci = Microsoft.Cci;
using ILOpCode = Microsoft.CodeAnalysis.CodeGen.ILOpCode;

namespace Roslyn.Test.Utilities
{
    internal sealed class ILBuilderVisualizer : ILVisualizer
    {
        private readonly ITokenDeferral _tokenDeferral;

        public ILBuilderVisualizer(ITokenDeferral tokenDeferral)
        {
            _tokenDeferral = tokenDeferral;
        }

        public override string VisualizeUserString(uint token)
        {
            return "\"" + _tokenDeferral.GetStringFromToken(token) + "\"";
        }

        public override string VisualizeSymbol(uint token)
        {
            Cci.IReference reference = _tokenDeferral.GetReferenceFromToken(token);
            ISymbol symbol = reference as ISymbol;
            return string.Format("\"{0}\"", symbol == null ? (object)reference : symbol.ToDisplayString(SymbolDisplayFormat.ILVisualizationFormat));
        }

        public override string VisualizeLocalType(object type)
        {
            ISymbol symbol = type as ISymbol;
            return symbol == null ? type.ToString() : symbol.ToDisplayString(SymbolDisplayFormat.ILVisualizationFormat);
        }

        /// <summary>
        /// Determine the list of spans ordered by handler
        /// block start, with outer handlers before inner.
        /// </summary>
        private static List<HandlerSpan> GetHandlerSpans(ImmutableArray<Cci.ExceptionHandlerRegion> regions)
        {
            if (regions.Length == 0)
            {
                return null;
            }

            var spans = new List<HandlerSpan>();

            // Add unique try blocks.
            foreach (Cci.ExceptionHandlerRegion region in regions)
            {
                var span = new HandlerSpan(HandlerKind.Try, null, region.TryStartOffset, region.TryEndOffset);

                int n = spans.Count;
                if (n == 0 || span.CompareTo(spans[n - 1]) != 0)
                {
                    spans.Add(span);
                }
            }

            // Add all handler blocks.
            foreach (Cci.ExceptionHandlerRegion region in regions)
            {
                HandlerSpan span;

                if (region.HandlerKind == ExceptionRegionKind.Filter)
                {
                    span = new HandlerSpan(HandlerKind.Filter, null, region.FilterDecisionStartOffset, region.HandlerEndOffset, region.HandlerStartOffset);
                }
                else
                {
                    HandlerKind kind;

                    switch (region.HandlerKind)
                    {
                        case ExceptionRegionKind.Catch:
                            kind = HandlerKind.Catch;
                            break;
                        case ExceptionRegionKind.Fault:
                            kind = HandlerKind.Fault;
                            break;
                        case ExceptionRegionKind.Filter:
                            kind = HandlerKind.Filter;
                            break;
                        default:
                            kind = HandlerKind.Finally;
                            break;
                    }

                    span = new HandlerSpan(kind, region.ExceptionType, region.HandlerStartOffset, region.HandlerEndOffset);
                }
                spans.Add(span);
            }

            spans.Sort();
            return spans;
        }

        /// <remarks>
        /// Invoked via Reflection from <see cref="ILBuilder.GetDebuggerDisplay()"/>
        /// </remarks>
        internal static string ILBuilderToString(
            ILBuilder builder,
            Func<Cci.ILocalDefinition, LocalInfo> mapLocal = null,
            IReadOnlyDictionary<int, string> markers = null)
        {
            var sb = new StringBuilder();

            var ilStream = builder.RealizedIL;
            if (mapLocal == null)
            {
                mapLocal = local => new LocalInfo(local.Name, local.Type, local.IsPinned, local.IsReference);
            }

            var locals = builder.LocalSlotManager.LocalsInOrder().SelectAsArray(mapLocal);
            var visualizer = new ILBuilderVisualizer(builder.module);

            if (!ilStream.IsDefault)
            {
                visualizer.DumpMethod(sb, builder.MaxStack, ilStream, locals, GetHandlerSpans(builder.RealizedExceptionHandlers), markers);
            }
            else
            {
                sb.AppendLine("{");

                visualizer.VisualizeHeader(sb, 0, builder.MaxStack, locals);
                // serialize blocks as-is
                var current = builder.leaderBlock;
                while (current != null)
                {
                    DumpBlockIL(current, sb);
                    current = current.NextBlock;
                }

                sb.AppendLine("}");
            }

            return sb.ToString();
        }

        internal static string LocalSignatureToString(
            ILBuilder builder,
            Func<Cci.ILocalDefinition, LocalInfo> mapLocal = null)
        {
            var sb = new StringBuilder();

            if (mapLocal == null)
            {
                mapLocal = local => new LocalInfo(local.Name, local.Type, local.IsPinned, local.IsReference);
            }

            var locals = builder.LocalSlotManager.LocalsInOrder().SelectAsArray(mapLocal);
            var visualizer = new ILBuilderVisualizer(builder.module);

            visualizer.VisualizeHeader(sb, -1, -1, locals);
            return sb.ToString();
        }

        private static string BasicBlockToString(ILBuilder.BasicBlock block)
        {
            StringBuilder sb = new StringBuilder();
            DumpBlockIL(block, sb);
            return sb.ToString();
        }

        private static void DumpBlockIL(ILBuilder.BasicBlock block, StringBuilder sb)
        {
            var switchBlock = block as ILBuilder.SwitchBlock;
            if (switchBlock != null)
            {
                DumpSwitchBlockIL(switchBlock, sb);
            }
            else
            {
                DumpBasicBlockIL(block, sb);
            }
        }

        private static void DumpBasicBlockIL(ILBuilder.BasicBlock block, StringBuilder sb)
        {
            var instrCnt = block.RegularInstructionsLength;
            if (instrCnt != 0)
            {
                var il = block.RegularInstructions.ToImmutableArray();
                new ILBuilderVisualizer(block.builder.module).DumpILBlock(il, instrCnt, sb, SpecializedCollections.EmptyArray<ILVisualizer.HandlerSpan>(), block.Start);
            }

            if (block.BranchCode != ILOpCode.Nop)
            {
                sb.Append(string.Format("  IL_{0:x4}:", block.RegularInstructionsLength + block.Start));
                sb.Append(string.Format("  {0,-10}", GetInstructionName(block.BranchCode)));

                if (block.BranchCode.IsBranchToLabel())
                {
                    var branchBlock = block.BranchBlock;
                    if (branchBlock == null)
                    {
                        // this happens if label is not yet marked.
                        sb.Append(" <unmarked label>");
                    }
                    else
                    {
                        sb.Append(string.Format(" IL_{0:x4}", branchBlock.Start));
                    }
                }

                sb.AppendLine();
            }
        }

        private static void DumpSwitchBlockIL(ILBuilder.SwitchBlock block, StringBuilder sb)
        {
            var il = block.RegularInstructions.ToImmutableArray();
            new ILBuilderVisualizer(block.builder.module).DumpILBlock(il, il.Length, sb, SpecializedCollections.EmptyArray<HandlerSpan>(), block.Start);

            // switch (N, t1, t2... tN)
            //  IL ==> ILOpCode.Switch < unsigned int32 > < int32 >... < int32 >

            sb.Append(string.Format("  IL_{0:x4}:", block.RegularInstructionsLength + block.Start));
            sb.Append(string.Format("  {0,-10}", GetInstructionName(block.BranchCode)));
            sb.Append(string.Format("  IL_{0:x4}:", block.BranchesCount));

            var blockBuilder = ArrayBuilder<ILBuilder.BasicBlock>.GetInstance();
            block.GetBranchBlocks(blockBuilder);

            foreach (var branchBlock in blockBuilder)
            {
                if (branchBlock == null)
                {
                    // this happens if label is not yet marked.
                    sb.Append(" <unmarked label>");
                }
                else
                {
                    sb.Append(string.Format(" IL_{0:x4}", branchBlock.Start));
                }
            }

            blockBuilder.Free();

            sb.AppendLine();
        }

        private static string GetInstructionName(ILOpCode opcode)
        {
            switch (opcode)
            {
                case ILOpCode.Nop: return "nop";
                case ILOpCode.Break: return "break";
                case ILOpCode.Ldarg_0: return "ldarg.0";
                case ILOpCode.Ldarg_1: return "ldarg.1";
                case ILOpCode.Ldarg_2: return "ldarg.2";
                case ILOpCode.Ldarg_3: return "ldarg.3";
                case ILOpCode.Ldloc_0: return "ldloc.0";
                case ILOpCode.Ldloc_1: return "ldloc.1";
                case ILOpCode.Ldloc_2: return "ldloc.2";
                case ILOpCode.Ldloc_3: return "ldloc.3";
                case ILOpCode.Stloc_0: return "stloc.0";
                case ILOpCode.Stloc_1: return "stloc.1";
                case ILOpCode.Stloc_2: return "stloc.2";
                case ILOpCode.Stloc_3: return "stloc.3";
                case ILOpCode.Ldarg_s: return "ldarg.s";
                case ILOpCode.Ldarga_s: return "ldarga.s";
                case ILOpCode.Starg_s: return "starg.s";
                case ILOpCode.Ldloc_s: return "ldloc.s";
                case ILOpCode.Ldloca_s: return "ldloca.s";
                case ILOpCode.Stloc_s: return "stloc.s";
                case ILOpCode.Ldnull: return "ldnull";
                case ILOpCode.Ldc_i4_m1: return "ldc.i4.m1";
                case ILOpCode.Ldc_i4_0: return "ldc.i4.0";
                case ILOpCode.Ldc_i4_1: return "ldc.i4.1";
                case ILOpCode.Ldc_i4_2: return "ldc.i4.2";
                case ILOpCode.Ldc_i4_3: return "ldc.i4.3";
                case ILOpCode.Ldc_i4_4: return "ldc.i4.4";
                case ILOpCode.Ldc_i4_5: return "ldc.i4.5";
                case ILOpCode.Ldc_i4_6: return "ldc.i4.6";
                case ILOpCode.Ldc_i4_7: return "ldc.i4.7";
                case ILOpCode.Ldc_i4_8: return "ldc.i4.8";
                case ILOpCode.Ldc_i4_s: return "ldc.i4.s";
                case ILOpCode.Ldc_i4: return "ldc.i4";
                case ILOpCode.Ldc_i8: return "ldc.i8";
                case ILOpCode.Ldc_r4: return "ldc.r4";
                case ILOpCode.Ldc_r8: return "ldc.r8";
                case ILOpCode.Dup: return "dup";
                case ILOpCode.Pop: return "pop";
                case ILOpCode.Jmp: return "jmp";
                case ILOpCode.Call: return "call";
                case ILOpCode.Calli: return "calli";
                case ILOpCode.Ret: return "ret";
                case ILOpCode.Br_s: return "br.s";
                case ILOpCode.Brfalse_s: return "brfalse.s";
                case ILOpCode.Brtrue_s: return "brtrue.s";
                case ILOpCode.Beq_s: return "beq.s";
                case ILOpCode.Bge_s: return "bge.s";
                case ILOpCode.Bgt_s: return "bgt.s";
                case ILOpCode.Ble_s: return "ble.s";
                case ILOpCode.Blt_s: return "blt.s";
                case ILOpCode.Bne_un_s: return "bne.un.s";
                case ILOpCode.Bge_un_s: return "bge.un.s";
                case ILOpCode.Bgt_un_s: return "bgt.un.s";
                case ILOpCode.Ble_un_s: return "ble.un.s";
                case ILOpCode.Blt_un_s: return "blt.un.s";
                case ILOpCode.Br: return "br";
                case ILOpCode.Brfalse: return "brfalse";
                case ILOpCode.Brtrue: return "brtrue";
                case ILOpCode.Beq: return "beq";
                case ILOpCode.Bge: return "bge";
                case ILOpCode.Bgt: return "bgt";
                case ILOpCode.Ble: return "ble";
                case ILOpCode.Blt: return "blt";
                case ILOpCode.Bne_un: return "bne.un";
                case ILOpCode.Bge_un: return "bge.un";
                case ILOpCode.Bgt_un: return "bgt.un";
                case ILOpCode.Ble_un: return "ble.un";
                case ILOpCode.Blt_un: return "blt.un";
                case ILOpCode.Switch: return "switch";
                case ILOpCode.Ldind_i1: return "ldind.i1";
                case ILOpCode.Ldind_u1: return "ldind.u1";
                case ILOpCode.Ldind_i2: return "ldind.i2";
                case ILOpCode.Ldind_u2: return "ldind.u2";
                case ILOpCode.Ldind_i4: return "ldind.i4";
                case ILOpCode.Ldind_u4: return "ldind.u4";
                case ILOpCode.Ldind_i8: return "ldind.i8";
                case ILOpCode.Ldind_i: return "ldind.i";
                case ILOpCode.Ldind_r4: return "ldind.r4";
                case ILOpCode.Ldind_r8: return "ldind.r8";
                case ILOpCode.Ldind_ref: return "ldind.ref";
                case ILOpCode.Stind_ref: return "stind.ref";
                case ILOpCode.Stind_i1: return "stind.i1";
                case ILOpCode.Stind_i2: return "stind.i2";
                case ILOpCode.Stind_i4: return "stind.i4";
                case ILOpCode.Stind_i8: return "stind.i8";
                case ILOpCode.Stind_r4: return "stind.r4";
                case ILOpCode.Stind_r8: return "stind.r8";
                case ILOpCode.Add: return "add";
                case ILOpCode.Sub: return "sub";
                case ILOpCode.Mul: return "mul";
                case ILOpCode.Div: return "div";
                case ILOpCode.Div_un: return "div.un";
                case ILOpCode.Rem: return "rem";
                case ILOpCode.Rem_un: return "rem.un";
                case ILOpCode.And: return "and";
                case ILOpCode.Or: return "or";
                case ILOpCode.Xor: return "xor";
                case ILOpCode.Shl: return "shl";
                case ILOpCode.Shr: return "shr";
                case ILOpCode.Shr_un: return "shr.un";
                case ILOpCode.Neg: return "neg";
                case ILOpCode.Not: return "not";
                case ILOpCode.Conv_i1: return "conv.i1";
                case ILOpCode.Conv_i2: return "conv.i2";
                case ILOpCode.Conv_i4: return "conv.i4";
                case ILOpCode.Conv_i8: return "conv.i8";
                case ILOpCode.Conv_r4: return "conv.r4";
                case ILOpCode.Conv_r8: return "conv.r8";
                case ILOpCode.Conv_u4: return "conv.u4";
                case ILOpCode.Conv_u8: return "conv.u8";
                case ILOpCode.Callvirt: return "callvirt";
                case ILOpCode.Cpobj: return "cpobj";
                case ILOpCode.Ldobj: return "ldobj";
                case ILOpCode.Ldstr: return "ldstr";
                case ILOpCode.Newobj: return "newobj";
                case ILOpCode.Castclass: return "castclass";
                case ILOpCode.Isinst: return "isinst";
                case ILOpCode.Conv_r_un: return "conv.r.un";
                case ILOpCode.Unbox: return "unbox";
                case ILOpCode.Throw: return "throw";
                case ILOpCode.Ldfld: return "ldfld";
                case ILOpCode.Ldflda: return "ldflda";
                case ILOpCode.Stfld: return "stfld";
                case ILOpCode.Ldsfld: return "ldsfld";
                case ILOpCode.Ldsflda: return "ldsflda";
                case ILOpCode.Stsfld: return "stsfld";
                case ILOpCode.Stobj: return "stobj";
                case ILOpCode.Conv_ovf_i1_un: return "conv.ovf.i1.un";
                case ILOpCode.Conv_ovf_i2_un: return "conv.ovf.i2.un";
                case ILOpCode.Conv_ovf_i4_un: return "conv.ovf.i4.un";
                case ILOpCode.Conv_ovf_i8_un: return "conv.ovf.i8.un";
                case ILOpCode.Conv_ovf_u1_un: return "conv.ovf.u1.un";
                case ILOpCode.Conv_ovf_u2_un: return "conv.ovf.u2.un";
                case ILOpCode.Conv_ovf_u4_un: return "conv.ovf.u4.un";
                case ILOpCode.Conv_ovf_u8_un: return "conv.ovf.u8.un";
                case ILOpCode.Conv_ovf_i_un: return "conv.ovf.i.un";
                case ILOpCode.Conv_ovf_u_un: return "conv.ovf.u.un";
                case ILOpCode.Box: return "box";
                case ILOpCode.Newarr: return "newarr";
                case ILOpCode.Ldlen: return "ldlen";
                case ILOpCode.Ldelema: return "ldelema";
                case ILOpCode.Ldelem_i1: return "ldelem.i1";
                case ILOpCode.Ldelem_u1: return "ldelem.u1";
                case ILOpCode.Ldelem_i2: return "ldelem.i2";
                case ILOpCode.Ldelem_u2: return "ldelem.u2";
                case ILOpCode.Ldelem_i4: return "ldelem.i4";
                case ILOpCode.Ldelem_u4: return "ldelem.u4";
                case ILOpCode.Ldelem_i8: return "ldelem.i8";
                case ILOpCode.Ldelem_i: return "ldelem.i";
                case ILOpCode.Ldelem_r4: return "ldelem.r4";
                case ILOpCode.Ldelem_r8: return "ldelem.r8";
                case ILOpCode.Ldelem_ref: return "ldelem.ref";
                case ILOpCode.Stelem_i: return "stelem.i";
                case ILOpCode.Stelem_i1: return "stelem.i1";
                case ILOpCode.Stelem_i2: return "stelem.i2";
                case ILOpCode.Stelem_i4: return "stelem.i4";
                case ILOpCode.Stelem_i8: return "stelem.i8";
                case ILOpCode.Stelem_r4: return "stelem.r4";
                case ILOpCode.Stelem_r8: return "stelem.r8";
                case ILOpCode.Stelem_ref: return "stelem.ref";
                case ILOpCode.Ldelem: return "ldelem";
                case ILOpCode.Stelem: return "stelem";
                case ILOpCode.Unbox_any: return "unbox.any";
                case ILOpCode.Conv_ovf_i1: return "conv.ovf.i1";
                case ILOpCode.Conv_ovf_u1: return "conv.ovf.u1";
                case ILOpCode.Conv_ovf_i2: return "conv.ovf.i2";
                case ILOpCode.Conv_ovf_u2: return "conv.ovf.u2";
                case ILOpCode.Conv_ovf_i4: return "conv.ovf.i4";
                case ILOpCode.Conv_ovf_u4: return "conv.ovf.u4";
                case ILOpCode.Conv_ovf_i8: return "conv.ovf.i8";
                case ILOpCode.Conv_ovf_u8: return "conv.ovf.u8";
                case ILOpCode.Refanyval: return "refanyval";
                case ILOpCode.Ckfinite: return "ckfinite";
                case ILOpCode.Mkrefany: return "mkrefany";
                case ILOpCode.Ldtoken: return "ldtoken";
                case ILOpCode.Conv_u2: return "conv.u2";
                case ILOpCode.Conv_u1: return "conv.u1";
                case ILOpCode.Conv_i: return "conv.i";
                case ILOpCode.Conv_ovf_i: return "conv.ovf.i";
                case ILOpCode.Conv_ovf_u: return "conv.ovf.u";
                case ILOpCode.Add_ovf: return "add.ovf";
                case ILOpCode.Add_ovf_un: return "add.ovf.un";
                case ILOpCode.Mul_ovf: return "mul.ovf";
                case ILOpCode.Mul_ovf_un: return "mul.ovf.un";
                case ILOpCode.Sub_ovf: return "sub.ovf";
                case ILOpCode.Sub_ovf_un: return "sub.ovf.un";
                case ILOpCode.Endfinally: return "endfinally";
                case ILOpCode.Leave: return "leave";
                case ILOpCode.Leave_s: return "leave.s";
                case ILOpCode.Stind_i: return "stind.i";
                case ILOpCode.Conv_u: return "conv.u";
                case ILOpCode.Arglist: return "arglist";
                case ILOpCode.Ceq: return "ceq";
                case ILOpCode.Cgt: return "cgt";
                case ILOpCode.Cgt_un: return "cgt.un";
                case ILOpCode.Clt: return "clt";
                case ILOpCode.Clt_un: return "clt.un";
                case ILOpCode.Ldftn: return "ldftn";
                case ILOpCode.Ldvirtftn: return "ldvirtftn";
                case ILOpCode.Ldarg: return "ldarg";
                case ILOpCode.Ldarga: return "ldarga";
                case ILOpCode.Starg: return "starg";
                case ILOpCode.Ldloc: return "ldloc";
                case ILOpCode.Ldloca: return "ldloca";
                case ILOpCode.Stloc: return "stloc";
                case ILOpCode.Localloc: return "localloc";
                case ILOpCode.Endfilter: return "endfilter";
                case ILOpCode.Unaligned: return "unaligned.";
                case ILOpCode.Volatile: return "volatile.";
                case ILOpCode.Tail: return "tail.";
                case ILOpCode.Initobj: return "initobj";
                case ILOpCode.Constrained: return "constrained.";
                case ILOpCode.Cpblk: return "cpblk";
                case ILOpCode.Initblk: return "initblk";
                case ILOpCode.Rethrow: return "rethrow";
                case ILOpCode.Sizeof: return "sizeof";
                case ILOpCode.Refanytype: return "refanytype";
                case ILOpCode.Readonly: return "readonly.";
            }

            throw ExceptionUtilities.UnexpectedValue(opcode);
        }
    }
}
