// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Reflection.Metadata;
using Microsoft.CodeAnalysis.CodeGen;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Emit
{
    /// <summary>
    /// Debugging information associated with the specified method that is emitted by the compiler to support Edit and Continue.
    /// </summary>
    public struct EditAndContinueMethodDebugInformation
    {
        internal readonly ImmutableArray<ValueTuple<SynthesizedLocalKind, LocalDebugId>> LocalSlots;

        internal EditAndContinueMethodDebugInformation(ImmutableArray<ValueTuple<SynthesizedLocalKind, LocalDebugId>> localSlots)
        {
            this.LocalSlots = localSlots;
        }

        public static EditAndContinueMethodDebugInformation Create(ImmutableArray<byte> compressedSlotMap)
        {
            return new EditAndContinueMethodDebugInformation(UncompressSlotMap(compressedSlotMap));
        }

        private static bool HasSubordinal(SynthesizedLocalKind kind)
        {
            switch (kind)
            {
                case SynthesizedLocalKind.AwaitByRefSpill:
                    return true;

                default:
                    return false;
            }
        }

        private const byte AlignmentValue = 0xff;

        private unsafe static ImmutableArray<ValueTuple<SynthesizedLocalKind, LocalDebugId>> UncompressSlotMap(ImmutableArray<byte> compressedSlotMap)
        {
            if (compressedSlotMap.IsDefaultOrEmpty)
            {
                return default(ImmutableArray<ValueTuple<SynthesizedLocalKind, LocalDebugId>>);
            }

            var mapBuilder = ArrayBuilder<ValueTuple<SynthesizedLocalKind, LocalDebugId>>.GetInstance();
            
            fixed (byte* compressedSlotMapPtr = &compressedSlotMap.ToArray()[0])
            {
                var blobReader = new BlobReader((IntPtr)compressedSlotMapPtr, compressedSlotMap.Length);

                while (blobReader.RemainingBytes > 0)
                {
                    byte b = blobReader.ReadByte();

                    if (b == AlignmentValue)
                    {
                        break;
                    }

                    if (b == 0)
                    {
                        // short-lived temp, no info
                        mapBuilder.Add(ValueTuple.Create(SynthesizedLocalKind.LoweringTemp, default(LocalDebugId)));
                        continue;
                    }

                    int ordinalCount = 0;

                    if ((b & (1 << 7)) != 0)
                    {
                        // highest bit set - we have an ordinal
                        ordinalCount++;
                    }

                    var kind = (SynthesizedLocalKind)((b & 0x3f) - 1);
                    if (HasSubordinal(kind))
                    {
                        ordinalCount++;
                    }

                    // TODO: Right now all integers are >= -1, but we should not assume that and read Ecma335 compressed int instead.
                    uint syntaxOffsetUnsigned;
                    if (!blobReader.TryReadCompressedUInt32(out syntaxOffsetUnsigned)) 
                    {
                        return default(ImmutableArray<ValueTuple<SynthesizedLocalKind, LocalDebugId>>);
                    }

                    int syntaxOffset = (int)syntaxOffsetUnsigned - 1;

                    uint ordinal = 0;
                    if (ordinalCount >= 1 && (!blobReader.TryReadCompressedUInt32(out ordinal) || ordinal > int.MaxValue))
                    {
                        return default(ImmutableArray<ValueTuple<SynthesizedLocalKind, LocalDebugId>>);
                    }

                    uint subordinal = 0;
                    if (ordinalCount >= 2 && (!blobReader.TryReadCompressedUInt32(out subordinal) || subordinal > int.MaxValue))
                    {
                        return default(ImmutableArray<ValueTuple<SynthesizedLocalKind, LocalDebugId>>);
                    }

                    mapBuilder.Add(ValueTuple.Create(kind, new LocalDebugId(syntaxOffset, (int)ordinal, (int)subordinal)));
                }
            }

            return mapBuilder.ToImmutableAndFree();
        }

        internal void SerializeCustomDebugInformation(ArrayBuilder<Cci.MemoryStream> customDebugInfo)
        {
            if (this.LocalSlots.IsDefaultOrEmpty)
            {
                return;
            }

            Cci.MemoryStream customMetadata = new Cci.MemoryStream();
            Cci.BinaryWriter cmw = new Cci.BinaryWriter(customMetadata, true);
            cmw.WriteByte(4); // version
            cmw.WriteByte(6); // kind: EditAndContinueLocalSlotMap
            cmw.Align(4);

            // length (will be patched)
            uint lengthPosition = cmw.BaseStream.Position;
            cmw.WriteUint(0);

            foreach (ValueTuple<SynthesizedLocalKind, LocalDebugId> localSlot in this.LocalSlots)
            {
                var kind = localSlot.Item1;

                byte b = (byte)(kind.IsLongLived() ? kind + 1 : 0);
                Debug.Assert((b & (1 << 7)) == 0);

                cmw.WriteByte(b);

                if (b == 0)
                {
                    continue;
                }

                if (localSlot.Item2.Ordinal != 0)
                {
                    b |= 1 << 7;
                }

                Debug.Assert(HasSubordinal(kind) == (localSlot.Item2.Subordinal != 0));
                
                // TODO: Right now all integers are >= -1, but we should not assume that and write Ecma335 compressed int instead.
                cmw.WriteCompressedUInt(unchecked((uint)(localSlot.Item2.SyntaxOffset + 1)));

                if (localSlot.Item2.Ordinal != 0)
                {
                    cmw.WriteCompressedUInt((uint)localSlot.Item2.Ordinal);
                }

                if (localSlot.Item2.Subordinal != 0)
                {
                    cmw.WriteCompressedUInt((uint)localSlot.Item2.Subordinal);
                }
            }

            uint length = customMetadata.Position;

            // align with values that the reader skips
            while (length % 4 != 0)
            {
                cmw.WriteByte(AlignmentValue);
                length++;
            }

            cmw.BaseStream.Position = lengthPosition;
            cmw.WriteUint(length);
            cmw.BaseStream.Position = length;

            customDebugInfo.Add(customMetadata);
        }
    }
}
