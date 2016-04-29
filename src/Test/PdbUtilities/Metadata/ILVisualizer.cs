// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using Roslyn.Utilities;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Text;
using System.Reflection.Emit;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;

namespace Roslyn.Test.MetadataUtilities
{
    public abstract class ILVisualizer
    {
        private static readonly OpCode[] s_oneByteOpCodes;
        private static readonly OpCode[] s_twoByteOpCodes;

        static ILVisualizer()
        {
            s_oneByteOpCodes = new OpCode[0x100];
            s_twoByteOpCodes = new OpCode[0x100];

            var typeOfOpCode = typeof(OpCode);

            foreach (FieldInfo fi in typeof(OpCodes).GetTypeInfo().DeclaredFields)
            {
                if (fi.FieldType != typeOfOpCode)
                {
                    continue;
                }

                OpCode opCode = (OpCode)fi.GetValue(null);
                var value = unchecked((ushort)opCode.Value);
                if (value < 0x100)
                {
                    s_oneByteOpCodes[value] = opCode;
                }
                else if ((value & 0xff00) == 0xfe00)
                {
                    s_twoByteOpCodes[value & 0xff] = opCode;
                }
            }
        }

        public enum HandlerKind
        {
            Try,
            Catch,
            Filter,
            Finally,
            Fault
        }

        public struct HandlerSpan : IComparable<HandlerSpan>
        {
            public readonly HandlerKind Kind;
            public readonly object ExceptionType;
            public readonly int StartOffset;
            public readonly int FilterHandlerStart;
            public readonly int EndOffset;

            public HandlerSpan(HandlerKind kind, object exceptionType, int startOffset, int endOffset, int filterHandlerStart = 0)
            {
                this.Kind = kind;
                this.ExceptionType = exceptionType;
                this.StartOffset = startOffset;
                this.EndOffset = endOffset;
                this.FilterHandlerStart = filterHandlerStart;
            }

            public int CompareTo(HandlerSpan other)
            {
                int result = this.StartOffset - other.StartOffset;
                if (result == 0)
                {
                    // Both blocks have same start. Order larger (outer) before smaller (inner).
                    result = other.EndOffset - this.EndOffset;
                }

                return result;
            }

            public string ToString(ILVisualizer visualizer)
            {
                switch (this.Kind)
                {
                    default:
                        return ".try";
                    case HandlerKind.Catch:
                        return "catch " + visualizer.VisualizeLocalType(this.ExceptionType);
                    case HandlerKind.Filter:
                        return "filter";
                    case HandlerKind.Finally:
                        return "finally";
                    case HandlerKind.Fault:
                        return "fault";
                }
            }

            public override string ToString()
            {
                throw new NotSupportedException("Use ToString(ILVisualizer)");
            }
        }

        public struct LocalInfo
        {
            public readonly string Name;
            public readonly bool IsPinned;
            public readonly bool IsByRef;
            public readonly object Type; // ITypeReference or ITypeSymbol

            public LocalInfo(string name, object type, bool isPinned, bool isByRef)
            {
                Name = name;
                Type = type;
                IsPinned = isPinned;
                IsByRef = isByRef;
            }
        }

        public const string IndentString = "  ";

        public abstract string VisualizeUserString(uint token);
        public abstract string VisualizeSymbol(uint token);
        public abstract string VisualizeLocalType(object type);

        private static ulong ReadUInt64(ImmutableArray<byte> buffer, ref int pos)
        {
            ulong result =
                buffer[pos] |
                (ulong)buffer[pos + 1] << 8 |
                (ulong)buffer[pos + 2] << 16 |
                (ulong)buffer[pos + 3] << 24 |
                (ulong)buffer[pos + 4] << 32 |
                (ulong)buffer[pos + 5] << 40 |
                (ulong)buffer[pos + 6] << 48 |
                (ulong)buffer[pos + 7] << 56;

            pos += sizeof(ulong);
            return result;
        }

        private static uint ReadUInt32(ImmutableArray<byte> buffer, ref int pos)
        {
            uint result = buffer[pos] | (uint)buffer[pos + 1] << 8 | (uint)buffer[pos + 2] << 16 | (uint)buffer[pos + 3] << 24;
            pos += sizeof(uint);
            return result;
        }

        private static int ReadInt32(ImmutableArray<byte> buffer, ref int pos)
        {
            return unchecked((int)ReadUInt32(buffer, ref pos));
        }

        private static ushort ReadUInt16(ImmutableArray<byte> buffer, ref int pos)
        {
            ushort result = (ushort)(buffer[pos] | buffer[pos + 1] << 8);
            pos += sizeof(ushort);
            return result;
        }

        private static byte ReadByte(ImmutableArray<byte> buffer, ref int pos)
        {
            byte result = buffer[pos];
            pos += sizeof(byte);
            return result;
        }

        private static sbyte ReadSByte(ImmutableArray<byte> buffer, ref int pos)
        {
            sbyte result = unchecked((sbyte)buffer[pos]);
            pos += 1;
            return result;
        }

        private unsafe static float ReadSingle(ImmutableArray<byte> buffer, ref int pos)
        {
            uint value = ReadUInt32(buffer, ref pos);
            return *(float*)&value;
        }

        private unsafe static double ReadDouble(ImmutableArray<byte> buffer, ref int pos)
        {
            ulong value = ReadUInt64(buffer, ref pos);
            return *(double*)&value;
        }

        public void VisualizeHeader(StringBuilder sb, int codeSize, int maxStack, ImmutableArray<LocalInfo> locals)
        {
            if (codeSize >= 0 && maxStack >= 0)
            {
                if (codeSize == 0)
                {
                    sb.AppendLine("  // Unrealized IL");
                }
                else
                {
                    sb.AppendLine(string.Format("  // Code size {0,8} (0x{0:x})", codeSize));
                }

                sb.AppendLine(string.Format("  .maxstack  {0}", maxStack));
            }

            int i = 0;
            foreach (var local in locals)
            {
                sb.Append(i == 0 ? "  .locals init (" : new string(' ', "  .locals init (".Length));
                if (local.IsPinned)
                {
                    sb.Append("pinned ");
                }

                sb.Append(VisualizeLocalType(local.Type));
                if (local.IsByRef)
                {
                    sb.Append("&");
                }

                sb.Append(" ");
                sb.Append("V_" + i);

                sb.Append(i == locals.Length - 1 ? ")" : ",");

                var name = local.Name;
                if (name != null)
                {
                    sb.Append(" //");
                    sb.Append(name);
                }

                sb.AppendLine();

                i++;
            }
        }

        public string DumpMethod(
            int maxStack,
            ImmutableArray<byte> ilBytes,
            ImmutableArray<LocalInfo> locals,
            IReadOnlyList<HandlerSpan> exceptionHandlers,
            IReadOnlyDictionary<int, string> markers = null)
        {
            var builder = new StringBuilder();
            this.DumpMethod(builder, maxStack, ilBytes, locals, exceptionHandlers, markers);
            return builder.ToString();
        }

        public void DumpMethod(
            StringBuilder sb,
            int maxStack,
            ImmutableArray<byte> ilBytes,
            ImmutableArray<LocalInfo> locals,
            IReadOnlyList<HandlerSpan> exceptionHandlers,
            IReadOnlyDictionary<int, string> markers = null)
        {
            sb.AppendLine("{");

            VisualizeHeader(sb, ilBytes.Length, maxStack, locals);
            DumpILBlock(ilBytes, ilBytes.Length, sb, exceptionHandlers, 0, markers);

            sb.AppendLine("}");
        }

        /// <summary>
        /// Dumps all instructions in the stream into provided string builder.
        /// The blockOffset specifies the relative position of the block within method body (if known).
        /// </summary>
        public void DumpILBlock(
            ImmutableArray<byte> ilBytes,
            int length,
            StringBuilder sb,
            IReadOnlyList<HandlerSpan> spans = null,
            int blockOffset = 0,
            IReadOnlyDictionary<int, string> markers = null)
        {
            if (ilBytes == null)
            {
                return;
            }

            int spanIndex = 0;
            int curIndex = DumpILBlock(ilBytes, length, sb, spans, blockOffset, 0, spanIndex, IndentString, markers, out spanIndex);
            Debug.Assert(curIndex == length);
            Debug.Assert(spans == null || spanIndex == spans.Count);
        }

        private int DumpILBlock(
            ImmutableArray<byte> ilBytes,
            int length,
            StringBuilder sb,
            IReadOnlyList<HandlerSpan> spans,
            int blockOffset,
            int curIndex,
            int spanIndex,
            string indent,
            IReadOnlyDictionary<int, string> markers,
            out int nextSpanIndex)
        {
            int lastSpanIndex = spanIndex - 1;

            while (curIndex < length)
            {
                if (lastSpanIndex > 0 && StartsFilterHandler(spans, lastSpanIndex, curIndex + blockOffset))
                {
                    sb.Append(indent.Substring(0, indent.Length - IndentString.Length));
                    sb.Append("}  // end filter");
                    sb.AppendLine();

                    sb.Append(indent.Substring(0, indent.Length - IndentString.Length));
                    sb.Append("{  // handler");
                    sb.AppendLine();
                }

                if (StartsSpan(spans, spanIndex, curIndex + blockOffset))
                {
                    sb.Append(indent);
                    sb.Append(spans[spanIndex].ToString(this));
                    sb.AppendLine();
                    sb.Append(indent);
                    sb.Append("{");
                    sb.AppendLine();

                    curIndex = DumpILBlock(ilBytes, length, sb, spans, blockOffset, curIndex, spanIndex + 1, indent + IndentString, markers, out spanIndex);

                    sb.Append(indent);
                    sb.Append("}");
                    sb.AppendLine();
                }
                else
                {
                    int ilOffset = curIndex + blockOffset;
                    string marker;
                    if (markers != null && markers.TryGetValue(ilOffset, out marker))
                    {
                        sb.Append(indent.Substring(0, indent.Length - marker.Length));
                        sb.Append(marker);
                    }
                    else
                    {
                        sb.Append(indent);
                    }

                    sb.AppendFormat("IL_{0:x4}:", ilOffset);

                    OpCode opCode;
                    int expectedSize;

                    byte op1 = ilBytes[curIndex++];
                    if (op1 == 0xfe && curIndex < length)
                    {
                        byte op2 = ilBytes[curIndex++];
                        opCode = s_twoByteOpCodes[op2];
                        expectedSize = 2;
                    }
                    else
                    {
                        opCode = s_oneByteOpCodes[op1];
                        expectedSize = 1;
                    }

                    if (opCode.Size != expectedSize)
                    {
                        sb.AppendLine(string.Format("  <unknown 0x{0}{1:X2}>", expectedSize == 2 ? "fe" : "", op1));
                        continue;
                    }

                    sb.Append("  ");
                    sb.AppendFormat(opCode.OperandType == OperandType.InlineNone ? "{0}" : "{0,-10}", opCode);

                    switch (opCode.OperandType)
                    {
                        case OperandType.InlineField:
                        case OperandType.InlineMethod:
                        case OperandType.InlineTok:
                        case OperandType.InlineType:
                            {
                                uint pseudoToken = ReadUInt32(ilBytes, ref curIndex);
                                // Check for a raw token value, encoded with a 1 high-order bit.
                                if (opCode.OperandType == OperandType.InlineTok && (pseudoToken & 0x80000000) != 0 && pseudoToken != 0xffffffff)
                                {
                                    pseudoToken &= 0x7fffffff;
                                }
                                sb.Append(' ');
                                sb.Append(VisualizeSymbol(pseudoToken));
                                break;
                            }
                        case OperandType.InlineSig: // signature (calli), not emitted by C#/VB
                            sb.AppendFormat(" 0x{0:x}", ReadUInt32(ilBytes, ref curIndex));
                            break;

                        case OperandType.InlineString:
                            sb.Append(' ');
                            sb.Append(VisualizeUserString(ReadUInt32(ilBytes, ref curIndex)));
                            break;

                        case OperandType.InlineNone:
                            break;

                        case OperandType.ShortInlineI:
                            sb.AppendFormat(" {0}", ReadSByte(ilBytes, ref curIndex));
                            break;

                        case OperandType.ShortInlineVar:
                            sb.AppendFormat(" V_{0}", ReadByte(ilBytes, ref curIndex));
                            break;

                        case OperandType.InlineVar:
                            sb.AppendFormat(" V_{0}", ReadUInt16(ilBytes, ref curIndex));
                            break;

                        case OperandType.InlineI:
                            sb.AppendFormat(" 0x{0:x}", ReadUInt32(ilBytes, ref curIndex));
                            break;

                        case OperandType.InlineI8:
                            sb.AppendFormat(" 0x{0:x8}", ReadUInt64(ilBytes, ref curIndex));
                            break;

                        case OperandType.ShortInlineR:
                            {
                                var value = ReadSingle(ilBytes, ref curIndex);
                                if (value == 0 && 1 / value < 0)
                                {
                                    sb.Append(" -0.0");
                                }
                                else
                                {
                                    sb.AppendFormat(" {0}", value.ToString(CultureInfo.InvariantCulture));
                                }
                            }
                            break;

                        case OperandType.InlineR:
                            {
                                var value = ReadDouble(ilBytes, ref curIndex);
                                if (value == 0 && 1 / value < 0)
                                {
                                    sb.Append(" -0.0");
                                }
                                else
                                {
                                    sb.AppendFormat(" {0}", value.ToString(CultureInfo.InvariantCulture));
                                }
                            }
                            break;

                        case OperandType.ShortInlineBrTarget:
                            sb.AppendFormat(" IL_{0:x4}", ReadSByte(ilBytes, ref curIndex) + curIndex + blockOffset);
                            break;

                        case OperandType.InlineBrTarget:
                            sb.AppendFormat(" IL_{0:x4}", ReadInt32(ilBytes, ref curIndex) + curIndex + blockOffset);
                            break;

                        case OperandType.InlineSwitch:
                            int labelCount = ReadInt32(ilBytes, ref curIndex);
                            int instrEnd = curIndex + labelCount * 4;
                            sb.AppendLine("(");
                            for (int i = 0; i < labelCount; i++)
                            {
                                sb.AppendFormat("        IL_{0:x4}", ReadInt32(ilBytes, ref curIndex) + instrEnd + blockOffset);
                                sb.AppendLine((i == labelCount - 1) ? ")" : ",");
                            }
                            break;

                        default:
                            throw ExceptionUtilities.UnexpectedValue(opCode.OperandType);
                    }

                    sb.AppendLine();
                }

                if (EndsSpan(spans, lastSpanIndex, curIndex + blockOffset))
                {
                    break;
                }
            }

            nextSpanIndex = spanIndex;
            return curIndex;
        }

        private static bool StartsSpan(IReadOnlyList<HandlerSpan> spans, int spanIndex, int curIndex)
        {
            return spans != null && spanIndex < spans.Count && spans[spanIndex].StartOffset == (uint)curIndex;
        }

        private static bool EndsSpan(IReadOnlyList<HandlerSpan> spans, int spanIndex, int curIndex)
        {
            return spans != null && spanIndex >= 0 && spans[spanIndex].EndOffset == (uint)curIndex;
        }

        private static bool StartsFilterHandler(IReadOnlyList<HandlerSpan> spans, int spanIndex, int curIndex)
        {
            return spans != null &&
                spanIndex < spans.Count &&
                spans[spanIndex].Kind == HandlerKind.Filter &&
                spans[spanIndex].FilterHandlerStart == (uint)curIndex;
        }

        public static IReadOnlyList<HandlerSpan> GetHandlerSpans(ImmutableArray<ExceptionRegion> entries)
        {
            if (entries.Length == 0)
            {
                return new HandlerSpan[0];
            }

            var result = new List<HandlerSpan>();
            foreach (ExceptionRegion entry in entries)
            {
                int tryStartOffset = entry.TryOffset;
                int tryEndOffset = entry.TryOffset + entry.TryLength;
                var span = new HandlerSpan(HandlerKind.Try, null, tryStartOffset, tryEndOffset);

                if (result.Count == 0 || span.CompareTo(result[result.Count - 1]) != 0)
                {
                    result.Add(span);
                }
            }

            foreach (ExceptionRegion entry in entries)
            {
                int handlerStartOffset = entry.HandlerOffset;
                int handlerEndOffset = entry.HandlerOffset + entry.HandlerLength;

                HandlerSpan span;
                switch (entry.Kind)
                {
                    case ExceptionRegionKind.Catch:
                        span = new HandlerSpan(HandlerKind.Catch, MetadataTokens.GetToken(entry.CatchType), handlerStartOffset, handlerEndOffset);
                        break;

                    case ExceptionRegionKind.Fault:
                        span = new HandlerSpan(HandlerKind.Fault, null, handlerStartOffset, handlerEndOffset);
                        break;

                    case ExceptionRegionKind.Filter:
                        span = new HandlerSpan(HandlerKind.Filter, null, handlerStartOffset, handlerEndOffset, entry.FilterOffset);
                        break;

                    case ExceptionRegionKind.Finally:
                        span = new HandlerSpan(HandlerKind.Finally, null, handlerStartOffset, handlerEndOffset);
                        break;

                    default:
                        throw new InvalidOperationException();
                }

                result.Add(span);
            }

            return result;
        }
    }
}
