// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;

namespace Microsoft.CodeAnalysis.Debugging
{
    internal struct CustomDebugInfoEncoder
    {
        public BlobBuilder Builder { get; }

        private readonly Blob _recordCountFixup;
        private int _recordCount;

        public CustomDebugInfoEncoder(BlobBuilder builder)
        {
            Debug.Assert(builder.Count == 0);

            Builder = builder;
            _recordCount = 0;

            // header:
            builder.WriteByte(CustomDebugInfoConstants.Version);

            // reserve byte for record count:
            _recordCountFixup = builder.ReserveBytes(1);

            // alignment:
            builder.WriteInt16(0);
        }

        public readonly int RecordCount => _recordCount;

        /// <exception cref="InvalidOperationException">More than <see cref="byte.MaxValue"/> records added.</exception>
        public readonly byte[] ToArray()
        {
            if (_recordCount == 0)
            {
                return null;
            }

            Debug.Assert(_recordCount <= byte.MaxValue);
            new BlobWriter(_recordCountFixup).WriteByte((byte)_recordCount);
            return Builder.ToArray();
        }

        public void AddStateMachineTypeName(string typeName)
        {
            Debug.Assert(typeName != null);

            AddRecord(
                CustomDebugInfoKind.StateMachineTypeName,
                typeName,
                (name, builder) =>
                {
                    builder.WriteUTF16(name);
                    builder.WriteInt16(0);
                });
        }

        public void AddForwardMethodInfo(MethodDefinitionHandle methodHandle)
        {
            AddRecord(
                CustomDebugInfoKind.ForwardMethodInfo,
                methodHandle,
                (mh, builder) => builder.WriteInt32(MetadataTokens.GetToken(mh)));
        }

        public void AddForwardModuleInfo(MethodDefinitionHandle methodHandle)
        {
            AddRecord(
                CustomDebugInfoKind.ForwardModuleInfo,
                methodHandle,
                (mh, builder) => builder.WriteInt32(MetadataTokens.GetToken(mh)));
        }

        public void AddUsingGroups(IReadOnlyCollection<int> groupSizes)
        {
            Debug.Assert(groupSizes.Count <= ushort.MaxValue);

            // This originally wrote (uint)12, (ushort)1, (ushort)0 in the
            // case where usingCounts was empty, but I'm not sure why.
            if (groupSizes.Count == 0)
            {
                return;
            }

            AddRecord(
                CustomDebugInfoKind.UsingGroups,
                groupSizes,
                (uc, builder) =>
                {
                    builder.WriteUInt16((ushort)uc.Count);
                    foreach (var usingCount in uc)
                    {
                        Debug.Assert(usingCount <= ushort.MaxValue);
                        builder.WriteUInt16((ushort)usingCount);
                    }
                });
        }

        public void AddStateMachineHoistedLocalScopes(ImmutableArray<StateMachineHoistedLocalScope> scopes)
        {
            if (scopes.IsDefaultOrEmpty)
            {
                return;
            }

            AddRecord(
                CustomDebugInfoKind.StateMachineHoistedLocalScopes,
                scopes,
                (s, builder) =>
                {
                    builder.WriteInt32(s.Length);
                    foreach (var scope in s)
                    {
                        if (scope.IsDefault)
                        {
                            builder.WriteInt32(0);
                            builder.WriteInt32(0);
                        }
                        else
                        {
                            // Dev12 C# emits end-inclusive range
                            builder.WriteInt32(scope.StartOffset);
                            builder.WriteInt32(scope.EndOffset - 1);
                        }
                    }
                });
        }

        internal const int DynamicAttributeSize = 64;
        internal const int IdentifierSize = 64;

        public void AddDynamicLocals(IReadOnlyCollection<(string LocalName, byte[] Flags, int Count, int SlotIndex)> dynamicLocals)
        {
            Debug.Assert(dynamicLocals != null);

            AddRecord(
                CustomDebugInfoKind.DynamicLocals,
                dynamicLocals,
                (infos, builder) =>
                {
                    builder.WriteInt32(infos.Count);

                    foreach (var info in infos)
                    {
                        Debug.Assert(info.Flags.Length <= DynamicAttributeSize);
                        Debug.Assert(info.LocalName.Length <= IdentifierSize);

                        builder.WriteBytes(info.Flags);
                        builder.WriteBytes(0, sizeof(byte) * (DynamicAttributeSize - info.Flags.Length));
                        builder.WriteInt32(info.Count);
                        builder.WriteInt32(info.SlotIndex);
                        builder.WriteUTF16(info.LocalName);
                        builder.WriteBytes(0, sizeof(char) * (IdentifierSize - info.LocalName.Length));
                    }
                });
        }

        public void AddTupleElementNames(IReadOnlyCollection<(string LocalName, int SlotIndex, int ScopeStart, int ScopeEnd, ImmutableArray<string> Names)> tupleLocals)
        {
            Debug.Assert(tupleLocals != null);

            AddRecord(
                CustomDebugInfoKind.TupleElementNames,
                tupleLocals,
                (infos, builder) =>
                {
                    Debug.Assert(infos.Count > 0);

                    builder.WriteInt32(infos.Count);
                    foreach (var info in infos)
                    {
                        // Constants have slot index -1 and scope specified,
                        // variables have a slot index specified and no scope.
                        Debug.Assert((info.SlotIndex == -1) ^ (info.ScopeStart == 0 && info.ScopeEnd == 0));

                        builder.WriteInt32(info.Names.Length);
                        foreach (var name in info.Names)
                        {
                            if (name != null)
                            {
                                builder.WriteUTF8(name);
                            }

                            builder.WriteByte(0);
                        }

                        builder.WriteInt32(info.SlotIndex);
                        builder.WriteInt32(info.ScopeStart);
                        builder.WriteInt32(info.ScopeEnd);
                        if (info.LocalName != null)
                        {
                            builder.WriteUTF8(info.LocalName);
                        }

                        builder.WriteByte(0);
                    }
                });
        }

        public void AddRecord<T>(
            CustomDebugInfoKind kind,
            T debugInfo,
            Action<T, BlobBuilder> recordSerializer)
        {
            var startOffset = Builder.Count;
            Builder.WriteByte(CustomDebugInfoConstants.Version);
            Builder.WriteByte((byte)kind);
            Builder.WriteByte(0);

            // alignment size and length (will be patched)
            var alignmentSizeAndLengthWriter = new BlobWriter(Builder.ReserveBytes(sizeof(byte) + sizeof(uint)));

            recordSerializer(debugInfo, Builder);

            var length = Builder.Count - startOffset;
            var alignedLength = 4 * ((length + 3) / 4);
            var alignmentSize = (byte)(alignedLength - length);
            Builder.WriteBytes(0, alignmentSize);

            // Fill in alignment size and length. 
            // For backward compat, alignment size should only be emitted for records introduced since Roslyn. 
            alignmentSizeAndLengthWriter.WriteByte((kind > CustomDebugInfoKind.DynamicLocals) ? alignmentSize : (byte)0);
            alignmentSizeAndLengthWriter.WriteUInt32((uint)alignedLength);

            _recordCount++;
        }
    }
}
