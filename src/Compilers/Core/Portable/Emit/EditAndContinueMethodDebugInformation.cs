// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Reflection.Metadata;
using Microsoft.CodeAnalysis.CodeGen;

namespace Microsoft.CodeAnalysis.Emit
{
    /// <summary>
    /// Debugging information associated with the specified method that is emitted by the compiler to support Edit and Continue.
    /// </summary>
    public struct EditAndContinueMethodDebugInformation
    {
        internal readonly int MethodOrdinal;
        internal readonly ImmutableArray<LocalSlotDebugInfo> LocalSlots;
        internal readonly ImmutableArray<LambdaDebugInfo> Lambdas;
        internal readonly ImmutableArray<ClosureDebugInfo> Closures;

        internal EditAndContinueMethodDebugInformation(int methodOrdinal, ImmutableArray<LocalSlotDebugInfo> localSlots, ImmutableArray<ClosureDebugInfo> closures, ImmutableArray<LambdaDebugInfo> lambdas)
        {
            Debug.Assert(methodOrdinal >= -1);

            this.MethodOrdinal = methodOrdinal;
            this.LocalSlots = localSlots;
            this.Lambdas = lambdas;
            this.Closures = closures;
        }

        public static EditAndContinueMethodDebugInformation Create(ImmutableArray<byte> compressedSlotMap, ImmutableArray<byte> compressedLambdaMap)
        {
            int methodOrdinal;
            ImmutableArray<ClosureDebugInfo> closures;
            ImmutableArray<LambdaDebugInfo> lambdas;
            UncompressLambdaMap(compressedLambdaMap, out methodOrdinal, out closures, out lambdas);
            return new EditAndContinueMethodDebugInformation(methodOrdinal, UncompressSlotMap(compressedSlotMap), closures, lambdas);
        }

        #region Local Slots

        private const byte SyntaxOffsetBaseline = 0xff;

        private unsafe static ImmutableArray<LocalSlotDebugInfo> UncompressSlotMap(ImmutableArray<byte> compressedSlotMap)
        {
            if (compressedSlotMap.IsDefaultOrEmpty)
            {
                return default(ImmutableArray<LocalSlotDebugInfo>);
            }

            var mapBuilder = ArrayBuilder<LocalSlotDebugInfo>.GetInstance();
            int syntaxOffsetBaseline = -1;

            fixed (byte* compressedSlotMapPtr = &compressedSlotMap.ToArray()[0])
            {
                var blobReader = new BlobReader(compressedSlotMapPtr, compressedSlotMap.Length);
                while (blobReader.RemainingBytes > 0)
                {
                    byte b = blobReader.ReadByte();

                    if (b == SyntaxOffsetBaseline)
                    {
                        syntaxOffsetBaseline = -blobReader.ReadCompressedInteger();
                        continue;
                    }

                    if (b == 0)
                    {
                        // short-lived temp, no info
                        mapBuilder.Add(new LocalSlotDebugInfo(SynthesizedLocalKind.LoweringTemp, default(LocalDebugId)));
                        continue;
                    }

                    var kind = (SynthesizedLocalKind)((b & 0x3f) - 1);
                    bool hasOrdinal = (b & (1 << 7)) != 0;

                    int syntaxOffset;
                    if (!blobReader.TryReadCompressedInteger(out syntaxOffset))
                    {
                        // invalid data
                        return default(ImmutableArray<LocalSlotDebugInfo>);
                    }

                    syntaxOffset += syntaxOffsetBaseline;

                    int ordinal = 0;
                    if (hasOrdinal && !blobReader.TryReadCompressedInteger(out ordinal))
                    {
                        // invalid data
                        return default(ImmutableArray<LocalSlotDebugInfo>);
                    }

                    mapBuilder.Add(new LocalSlotDebugInfo(kind, new LocalDebugId(syntaxOffset, ordinal)));
                }
            }

            return mapBuilder.ToImmutableAndFree();
        }

        internal void SerializeLocalSlots(Cci.BinaryWriter writer)
        {
            int syntaxOffsetBaseline = -1;
            foreach (LocalSlotDebugInfo localSlot in this.LocalSlots)
            {
                if (localSlot.Id.SyntaxOffset < syntaxOffsetBaseline)
                {
                    syntaxOffsetBaseline = localSlot.Id.SyntaxOffset;
                }
            }

            if (syntaxOffsetBaseline != -1)
            {
                writer.WriteByte(SyntaxOffsetBaseline);
                writer.WriteCompressedUInt((uint)(-syntaxOffsetBaseline));
            }

            foreach (LocalSlotDebugInfo localSlot in this.LocalSlots)
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
                writer.WriteCompressedUInt((uint)(localSlot.Id.SyntaxOffset - syntaxOffsetBaseline));

                if (hasOrdinal)
                {
                    writer.WriteCompressedUInt((uint)localSlot.Id.Ordinal);
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
            methodOrdinal = MethodDebugId.UndefinedOrdinal;
            closures = default(ImmutableArray<ClosureDebugInfo>);
            lambdas = default(ImmutableArray<LambdaDebugInfo>);

            if (compressedLambdaMap.IsDefaultOrEmpty)
            {
                return;
            }

            var closuresBuilder = ArrayBuilder<ClosureDebugInfo>.GetInstance();
            var lambdasBuilder = ArrayBuilder<LambdaDebugInfo>.GetInstance();

            int syntaxOffsetBaseline = -1;
            int closureCount;

            fixed (byte* blobPtr = &compressedLambdaMap.ToArray()[0])
            {
                var blobReader = new BlobReader(blobPtr, compressedLambdaMap.Length);

                if (!blobReader.TryReadCompressedInteger(out methodOrdinal))
                {
                    // invalid data
                    return;
                }

                // [-1, inf)
                methodOrdinal--;

                if (!blobReader.TryReadCompressedInteger(out syntaxOffsetBaseline))
                {
                    // invalid data
                    return;
                }

                syntaxOffsetBaseline = -syntaxOffsetBaseline;

                if (!blobReader.TryReadCompressedInteger(out closureCount))
                {
                    // invalid data
                    return;
                }

                for (int i = 0; i < closureCount; i++)
                {
                    int syntaxOffset;
                    if (!blobReader.TryReadCompressedInteger(out syntaxOffset))
                    {
                        // invalid data
                        return;
                    }

                    closuresBuilder.Add(new ClosureDebugInfo(syntaxOffset + syntaxOffsetBaseline));
                }

                while (blobReader.RemainingBytes > 0)
                {
                    int syntaxOffset;
                    if (!blobReader.TryReadCompressedInteger(out syntaxOffset))
                    {
                        // invalid data
                        return;
                    }

                    int closureOrdinal;
                    if (!blobReader.TryReadCompressedInteger(out closureOrdinal))
                    {
                        // invalid data
                        return;
                    }

                    closureOrdinal--;
                    if (closureOrdinal < -1 || closureOrdinal >= closureCount)
                    {
                        // invalid data
                        return;
                    }

                    lambdasBuilder.Add(new LambdaDebugInfo(syntaxOffset + syntaxOffsetBaseline, closureOrdinal));
                }
            }

            closures = closuresBuilder.ToImmutableAndFree();
            lambdas = lambdasBuilder.ToImmutableAndFree();
        }

        internal void SerializeLambdaMap(Cci.BinaryWriter writer)
        {
            Debug.Assert(this.MethodOrdinal >= -1);
            writer.WriteCompressedUInt((uint)(this.MethodOrdinal + 1));

            int syntaxOffsetBaseline = -1;
            foreach (ClosureDebugInfo info in this.Closures)
            {
                if (info.SyntaxOffset < syntaxOffsetBaseline)
                {
                    syntaxOffsetBaseline = info.SyntaxOffset;
                }
            }

            foreach (LambdaDebugInfo info in this.Lambdas)
            {
                if (info.SyntaxOffset < syntaxOffsetBaseline)
                {
                    syntaxOffsetBaseline = info.SyntaxOffset;
                }
            }

            writer.WriteCompressedUInt((uint)(-syntaxOffsetBaseline));
            writer.WriteCompressedUInt((uint)this.Closures.Length);

            foreach (ClosureDebugInfo info in this.Closures)
            {
                writer.WriteCompressedUInt((uint)(info.SyntaxOffset - syntaxOffsetBaseline));
            }

            foreach (LambdaDebugInfo info in this.Lambdas)
            {
                Debug.Assert(info.ClosureOrdinal >= -1);

                writer.WriteCompressedUInt((uint)(info.SyntaxOffset - syntaxOffsetBaseline));
                writer.WriteCompressedUInt((uint)(info.ClosureOrdinal + 1));
            }
        }

        #endregion
    }
}
