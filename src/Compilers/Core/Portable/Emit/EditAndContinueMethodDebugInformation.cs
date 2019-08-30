// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection.Metadata;
using Microsoft.CodeAnalysis.CodeGen;
using Microsoft.CodeAnalysis.PooledObjects;

namespace Microsoft.CodeAnalysis.Emit
{
    /// <summary>
    /// Debugging information associated with the specified method that is emitted by the compiler to support Edit and Continue.
    /// </summary>
    public readonly struct EditAndContinueMethodDebugInformation
    {
        internal readonly int MethodOrdinal;
        internal readonly ImmutableArray<LocalSlotDebugInfo> LocalSlots;
        internal readonly ImmutableArray<LambdaDebugInfo> Lambdas;
        internal readonly ImmutableArray<ClosureDebugInfo> Closures;

        internal EditAndContinueMethodDebugInformation(int methodOrdinal, ImmutableArray<LocalSlotDebugInfo> localSlots, ImmutableArray<ClosureDebugInfo> closures, ImmutableArray<LambdaDebugInfo> lambdas)
        {
            Debug.Assert(methodOrdinal >= -1);

            MethodOrdinal = methodOrdinal;
            LocalSlots = localSlots;
            Lambdas = lambdas;
            Closures = closures;
        }

        /// <summary>
        /// Deserializes Edit and Continue method debug information from specified blobs.
        /// </summary>
        /// <param name="compressedSlotMap">Local variable slot map.</param>
        /// <param name="compressedLambdaMap">Lambda and closure map.</param>
        /// <exception cref="InvalidDataException">Invalid data.</exception>
        public static EditAndContinueMethodDebugInformation Create(ImmutableArray<byte> compressedSlotMap, ImmutableArray<byte> compressedLambdaMap)
        {
            UncompressLambdaMap(compressedLambdaMap, out var methodOrdinal, out var closures, out var lambdas);
            return new EditAndContinueMethodDebugInformation(methodOrdinal, UncompressSlotMap(compressedSlotMap), closures, lambdas);
        }

        private static InvalidDataException CreateInvalidDataException(ImmutableArray<byte> data, int offset)
        {
            const int maxReportedLength = 1024;

            int start = Math.Max(0, offset - maxReportedLength / 2);
            int end = Math.Min(data.Length, offset + maxReportedLength / 2);

            byte[] left = new byte[offset - start];
            data.CopyTo(start, left, 0, left.Length);

            byte[] right = new byte[end - offset];
            data.CopyTo(offset, right, 0, right.Length);

            throw new InvalidDataException(string.Format(CodeAnalysisResources.InvalidDataAtOffset,
                offset, (start != 0) ? "..." : "", BitConverter.ToString(left), BitConverter.ToString(right), (end != data.Length) ? "..." : ""));
        }

        #region Local Slots

        private const byte SyntaxOffsetBaseline = 0xff;

        /// <exception cref="InvalidDataException">Invalid data.</exception>
        private unsafe static ImmutableArray<LocalSlotDebugInfo> UncompressSlotMap(ImmutableArray<byte> compressedSlotMap)
        {
            if (compressedSlotMap.IsDefaultOrEmpty)
            {
                return default;
            }

            var mapBuilder = ArrayBuilder<LocalSlotDebugInfo>.GetInstance();
            int syntaxOffsetBaseline = -1;

            fixed (byte* compressedSlotMapPtr = &compressedSlotMap.ToArray()[0])
            {
                var blobReader = new BlobReader(compressedSlotMapPtr, compressedSlotMap.Length);
                while (blobReader.RemainingBytes > 0)
                {
                    try
                    {
                        // Note: integer operations below can't overflow since compressed integers are in range [0, 0x20000000)

                        byte b = blobReader.ReadByte();

                        if (b == SyntaxOffsetBaseline)
                        {
                            syntaxOffsetBaseline = -blobReader.ReadCompressedInteger();
                            continue;
                        }

                        if (b == 0)
                        {
                            // short-lived temp, no info
                            mapBuilder.Add(new LocalSlotDebugInfo(SynthesizedLocalKind.LoweringTemp, default));
                            continue;
                        }

                        var kind = (SynthesizedLocalKind)((b & 0x3f) - 1);
                        bool hasOrdinal = (b & (1 << 7)) != 0;

                        int syntaxOffset = blobReader.ReadCompressedInteger() + syntaxOffsetBaseline;

                        int ordinal = hasOrdinal ? blobReader.ReadCompressedInteger() : 0;

                        mapBuilder.Add(new LocalSlotDebugInfo(kind, new LocalDebugId(syntaxOffset, ordinal)));
                    }
                    catch (BadImageFormatException)
                    {
                        throw CreateInvalidDataException(compressedSlotMap, blobReader.Offset);
                    }
                }
            }

            return mapBuilder.ToImmutableAndFree();
        }

        internal void SerializeLocalSlots(BlobBuilder writer)
        {
            int syntaxOffsetBaseline = -1;
            foreach (LocalSlotDebugInfo localSlot in LocalSlots)
            {
                if (localSlot.Id.SyntaxOffset < syntaxOffsetBaseline)
                {
                    syntaxOffsetBaseline = localSlot.Id.SyntaxOffset;
                }
            }

            if (syntaxOffsetBaseline != -1)
            {
                writer.WriteByte(SyntaxOffsetBaseline);
                writer.WriteCompressedInteger(-syntaxOffsetBaseline);
            }

            foreach (LocalSlotDebugInfo localSlot in LocalSlots)
            {
                SynthesizedLocalKind kind = localSlot.SynthesizedKind;
                Debug.Assert(kind <= SynthesizedLocalKind.MaxValidValueForLocalVariableSerializedToDebugInformation);

                if (!kind.IsLongLived())
                {
                    writer.WriteByte(0);
                    continue;
                }

                byte b = (byte)(kind + 1);
                Debug.Assert((b & (1 << 7)) == 0);

                bool hasOrdinal = localSlot.Id.Ordinal > 0;

                if (hasOrdinal)
                {
                    b |= 1 << 7;
                }

                writer.WriteByte(b);
                writer.WriteCompressedInteger(localSlot.Id.SyntaxOffset - syntaxOffsetBaseline);

                if (hasOrdinal)
                {
                    writer.WriteCompressedInteger(localSlot.Id.Ordinal);
                }
            }
        }

        #endregion

        #region Lambdas

        private unsafe static void UncompressLambdaMap(
            ImmutableArray<byte> compressedLambdaMap,
            out int methodOrdinal,
            out ImmutableArray<ClosureDebugInfo> closures,
            out ImmutableArray<LambdaDebugInfo> lambdas)
        {
            methodOrdinal = DebugId.UndefinedOrdinal;
            closures = default;
            lambdas = default;

            if (compressedLambdaMap.IsDefaultOrEmpty)
            {
                return;
            }

            var closuresBuilder = ArrayBuilder<ClosureDebugInfo>.GetInstance();
            var lambdasBuilder = ArrayBuilder<LambdaDebugInfo>.GetInstance();

            fixed (byte* blobPtr = &compressedLambdaMap.ToArray()[0])
            {
                var blobReader = new BlobReader(blobPtr, compressedLambdaMap.Length);
                try
                {
                    // Note: integer operations below can't overflow since compressed integers are in range [0, 0x20000000)

                    // [-1, inf)
                    methodOrdinal = blobReader.ReadCompressedInteger() - 1;

                    int syntaxOffsetBaseline = -blobReader.ReadCompressedInteger();

                    int closureCount = blobReader.ReadCompressedInteger();

                    for (int i = 0; i < closureCount; i++)
                    {
                        int syntaxOffset = blobReader.ReadCompressedInteger();

                        var closureId = new DebugId(closuresBuilder.Count, generation: 0);
                        closuresBuilder.Add(new ClosureDebugInfo(syntaxOffset + syntaxOffsetBaseline, closureId));
                    }

                    while (blobReader.RemainingBytes > 0)
                    {
                        int syntaxOffset = blobReader.ReadCompressedInteger();
                        int closureOrdinal = blobReader.ReadCompressedInteger() + LambdaDebugInfo.MinClosureOrdinal;

                        if (closureOrdinal >= closureCount)
                        {
                            throw CreateInvalidDataException(compressedLambdaMap, blobReader.Offset);
                        }

                        var lambdaId = new DebugId(lambdasBuilder.Count, generation: 0);
                        lambdasBuilder.Add(new LambdaDebugInfo(syntaxOffset + syntaxOffsetBaseline, lambdaId, closureOrdinal));
                    }
                }
                catch (BadImageFormatException)
                {
                    throw CreateInvalidDataException(compressedLambdaMap, blobReader.Offset);
                }
            }

            closures = closuresBuilder.ToImmutableAndFree();
            lambdas = lambdasBuilder.ToImmutableAndFree();
        }

        internal void SerializeLambdaMap(BlobBuilder writer)
        {
            Debug.Assert(MethodOrdinal >= -1);
            writer.WriteCompressedInteger(MethodOrdinal + 1);

            int syntaxOffsetBaseline = -1;
            foreach (ClosureDebugInfo info in Closures)
            {
                if (info.SyntaxOffset < syntaxOffsetBaseline)
                {
                    syntaxOffsetBaseline = info.SyntaxOffset;
                }
            }

            foreach (LambdaDebugInfo info in Lambdas)
            {
                if (info.SyntaxOffset < syntaxOffsetBaseline)
                {
                    syntaxOffsetBaseline = info.SyntaxOffset;
                }
            }

            writer.WriteCompressedInteger(-syntaxOffsetBaseline);
            writer.WriteCompressedInteger(Closures.Length);

            foreach (ClosureDebugInfo info in Closures)
            {
                writer.WriteCompressedInteger(info.SyntaxOffset - syntaxOffsetBaseline);
            }

            foreach (LambdaDebugInfo info in Lambdas)
            {
                Debug.Assert(info.ClosureOrdinal >= LambdaDebugInfo.MinClosureOrdinal);
                Debug.Assert(info.LambdaId.Generation == 0);

                writer.WriteCompressedInteger(info.SyntaxOffset - syntaxOffsetBaseline);
                writer.WriteCompressedInteger(info.ClosureOrdinal - LambdaDebugInfo.MinClosureOrdinal);
            }
        }

        #endregion
    }
}
