// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Text;
using Microsoft.CodeAnalysis.Collections;
using Roslyn.Utilities;

namespace Microsoft.Cci
{
    /// <summary>
    /// Wraps a virtual string table index.
    /// An override to SerializeIndex does the resolving at the right time.
    /// </summary>
    internal struct StringIdx
    {
        public readonly uint VirtIdx;

        internal StringIdx(uint virtIdx)
        {
            this.VirtIdx = virtIdx;
        }
    }

    internal sealed class MetadataHeapsBuilder
    {
        private static readonly Encoding s_utf8Encoding = Encoding.UTF8;

        // #US heap
        private readonly Dictionary<string, uint> _userStringIndex = new Dictionary<string, uint>();
        private readonly BinaryWriter _userStringWriter = new BinaryWriter(new MemoryStream(1024), true);
        private readonly int _userStringIndexStartOffset;

        // #String heap
        private Dictionary<string, StringIdx> _stringIndex = new Dictionary<string, StringIdx>(128);
        private uint[] _stringIndexMap;
        private readonly BinaryWriter _stringWriter = new BinaryWriter(new MemoryStream(1024));
        private readonly int _stringIndexStartOffset;

        // #Blob heap
        private readonly Dictionary<ImmutableArray<byte>, uint> _blobIndex = new Dictionary<ImmutableArray<byte>, uint>(ByteSequenceComparer.Instance);
        private readonly BinaryWriter _blobWriter = new BinaryWriter(new MemoryStream(1024));
        private readonly int _blobIndexStartOffset;

        // #GUID heap
        private readonly Dictionary<Guid, uint> _guidIndex = new Dictionary<Guid, uint>();
        private readonly BinaryWriter _guidWriter = new BinaryWriter(new MemoryStream(16)); // full metadata has just a single guid

        private bool _streamsAreComplete;

        public MetadataHeapsBuilder(
            int userStringIndexStartOffset = 0,
            int stringIndexStartOffset = 0,
            int blobIndexStartOffset = 0,
            int guidIndexStartOffset = 0)
        {
            // Add zero-th entry to heaps. 
            // Full metadata represent empty blob/string at heap index 0.
            // Delta metadata requires these to avoid nil generation-relative handles, 
            // which are technically viable but confusing.
            _blobWriter.WriteByte(0);
            _stringWriter.WriteByte(0);
            _userStringWriter.WriteByte(0);

            // When EnC delta is applied #US, #String and #Blob heaps are appended.
            // Thus indices of strings and blobs added to this generation are offset
            // by the sum of respective heap sizes of all previous generations.
            _userStringIndexStartOffset = userStringIndexStartOffset;
            _stringIndexStartOffset = stringIndexStartOffset;
            _blobIndexStartOffset = blobIndexStartOffset;

            // Unlike other heaps, #Guid heap in EnC delta is zero-padded.
            _guidWriter.Pad(guidIndexStartOffset);
        }

        internal uint GetBlobIndex(MemoryStream stream)
        {
            // TODO: avoid making a copy if the blob exists in the index
            return GetBlobIndex(stream.ToImmutableArray());
        }

        internal uint GetBlobIndex(ImmutableArray<byte> blob)
        {
            uint result = 0;
            if (blob.Length == 0 || _blobIndex.TryGetValue(blob, out result))
            {
                return result;
            }

            Debug.Assert(!_streamsAreComplete);
            result = _blobWriter.BaseStream.Position + (uint)_blobIndexStartOffset;
            _blobIndex.Add(blob, result);
            _blobWriter.WriteCompressedUInt((uint)blob.Length);
            _blobWriter.WriteBytes(blob);
            return result;
        }

        public uint GetConstantBlobIndex(object value)
        {
            string str = value as string;
            if (str != null)
            {
                return this.GetBlobIndex(str);
            }

            MemoryStream sig = new MemoryStream();
            BinaryWriter writer = new BinaryWriter(sig, true);
            writer.WriteConstantValueBlob(value);
            return this.GetBlobIndex(sig);
        }

        public uint GetBlobIndex(string str)
        {
            byte[] byteArray = new byte[str.Length * 2];
            int i = 0;
            foreach (char ch in str)
            {
                byteArray[i++] = (byte)(ch & 0xFF);
                byteArray[i++] = (byte)(ch >> 8);
            }

            return this.GetBlobIndex(ImmutableArray.Create(byteArray));
        }

        public uint GetGuidIndex(Guid guid)
        {
            if (guid == Guid.Empty)
            {
                return 0;
            }

            uint result;
            if (_guidIndex.TryGetValue(guid, out result))
            {
                return result;
            }

            return AllocateGuid(guid);
        }

        public uint AllocateGuid(Guid guid)
        {
            Debug.Assert(!_streamsAreComplete);

            // The only GUIDs that are serialized are MVID, EncId, and EncBaseId in the
            // Module table. Each of those GUID offsets are relative to the local heap,
            // even for deltas, so there's no need for a GetGuidStreamPosition() method
            // to offset the positions by the size of the original heap in delta metadata.
            // Unlike #Blob, #String and #US streams delta #GUID stream is padded to the 
            // size of the previous generation #GUID stream before new GUIDs are added.

            // Metadata Spec: 
            // The Guid heap is an array of GUIDs, each 16 bytes wide. 
            // Its first element is numbered 1, its second 2, and so on.
            uint result = (_guidWriter.BaseStream.Length >> 4) + 1;

            _guidIndex.Add(guid, result);
            _guidWriter.WriteBytes(guid.ToByteArray());

            return result;
        }

        public unsafe byte[] GetExistingBlob(int signatureOffset)
        {
            fixed (byte* ptr = _blobWriter.BaseStream.Buffer)
            {
                var reader = new BlobReader(ptr + signatureOffset, (int)_blobWriter.BaseStream.Length + (int)_blobIndexStartOffset - signatureOffset);
                int size;
                bool isValid = reader.TryReadCompressedInteger(out size);
                Debug.Assert(isValid);
                return reader.ReadBytes(size);
            }
        }

        public StringIdx GetStringIndex(string str)
        {
            StringIdx index;
            if (str.Length == 0)
            {
                index = new StringIdx(0);
            }
            else if (!_stringIndex.TryGetValue(str, out index))
            {
                Debug.Assert(!_streamsAreComplete);
                index = new StringIdx((uint)_stringIndex.Count + 1); // idx 0 is reserved for empty string
                _stringIndex.Add(str, index);
            }

            return index;
        }

        public uint ResolveStringIndex(StringIdx index)
        {
            return _stringIndexMap[index.VirtIdx];
        }

        public uint GetUserStringToken(string str)
        {
            uint index;
            if (!_userStringIndex.TryGetValue(str, out index))
            {
                Debug.Assert(!_streamsAreComplete);
                index = _userStringWriter.BaseStream.Position + (uint)_userStringIndexStartOffset;
                _userStringIndex.Add(str, index);
                _userStringWriter.WriteCompressedUInt((uint)str.Length * 2 + 1);
                _userStringWriter.WriteStringUtf16LE(str);

                // Write out a trailing byte indicating if the string is really quite simple
                byte stringKind = 0;
                foreach (char ch in str)
                {
                    if (ch >= 0x7F)
                    {
                        stringKind = 1;
                    }
                    else
                    {
                        switch ((int)ch)
                        {
                            case 0x1:
                            case 0x2:
                            case 0x3:
                            case 0x4:
                            case 0x5:
                            case 0x6:
                            case 0x7:
                            case 0x8:
                            case 0xE:
                            case 0xF:
                            case 0x10:
                            case 0x11:
                            case 0x12:
                            case 0x13:
                            case 0x14:
                            case 0x15:
                            case 0x16:
                            case 0x17:
                            case 0x18:
                            case 0x19:
                            case 0x1A:
                            case 0x1B:
                            case 0x1C:
                            case 0x1D:
                            case 0x1E:
                            case 0x1F:
                            case 0x27:
                            case 0x2D:
                                stringKind = 1;
                                break;
                            default:
                                continue;
                        }
                    }

                    break;
                }

                _userStringWriter.WriteByte(stringKind);
            }

            return 0x70000000 | index;
        }

        public void Complete()
        {
            Debug.Assert(!_streamsAreComplete);
            _streamsAreComplete = true;
            SerializeStringHeap();
        }

        public ImmutableArray<int> GetHeapSizes()
        {
            var heapSizes = new int[MetadataTokens.HeapCount];

            heapSizes[(int)HeapIndex.UserString] = (int)_userStringWriter.BaseStream.Length;
            heapSizes[(int)HeapIndex.String] = (int)_stringWriter.BaseStream.Length;
            heapSizes[(int)HeapIndex.Blob] = (int)_blobWriter.BaseStream.Length;
            heapSizes[(int)HeapIndex.Guid] = (int)_guidWriter.BaseStream.Length;

            return ImmutableArray.CreateRange(heapSizes);
        }

        /// <summary>
        /// Fills in stringIndexMap with data from stringIndex and write to stringWriter.
        /// Releases stringIndex as the stringTable is sealed after this point.
        /// </summary>
        private void SerializeStringHeap()
        {
            // Sort by suffix and remove stringIndex
            var sorted = new List<KeyValuePair<string, StringIdx>>(_stringIndex);
            sorted.Sort(new SuffixSort());
            _stringIndex = null;

            // Create VirtIdx to Idx map and add entry for empty string
            _stringIndexMap = new uint[sorted.Count+1];
            _stringIndexMap[0] = 0;

            // Find strings that can be folded
            string prev = String.Empty;
            foreach (KeyValuePair<string, StringIdx> cur in sorted)
            {
                uint position = _stringWriter.BaseStream.Position + (uint)_stringIndexStartOffset;

                // It is important to use ordinal comparison otherwise we'll use the current culture!
                if (prev.EndsWith(cur.Key, StringComparison.Ordinal))
                {
                    // Map over the tail of prev string. Watch for null-terminator of prev string.
                    _stringIndexMap[cur.Value.VirtIdx] = position - (uint)(s_utf8Encoding.GetByteCount(cur.Key) + 1);
                }
                else
                {
                    _stringIndexMap[cur.Value.VirtIdx] = position;
                    _stringWriter.WriteString(cur.Key, s_utf8Encoding);
                    _stringWriter.WriteByte(0);
                }

                prev = cur.Key;
            }
        }

        /// <summary>
        /// Sorts strings such that a string is followed immediately by all strings
        /// that are a suffix of it.  
        /// </summary>
        private class SuffixSort : IComparer<KeyValuePair<string, StringIdx>>
        {
            public int Compare(KeyValuePair<string, StringIdx> xPair, KeyValuePair<string, StringIdx> yPair)
            {
                string x = xPair.Key;
                string y = yPair.Key;

                for (int i = x.Length - 1, j = y.Length - 1; i >= 0 & j >= 0; i--, j--)
                {
                    if (x[i] < y[j])
                    {
                        return -1;
                    }

                    if (x[i] > y[j])
                    {
                        return +1;
                    }
                }

                return y.Length.CompareTo(x.Length);
            }
        }

        public void WriteTo(MemoryStream stream, out uint guidHeapStartOffset)
        {
            WriteAligned(_stringWriter.BaseStream, stream);
            WriteAligned(_userStringWriter.BaseStream, stream);

            guidHeapStartOffset = stream.Position;

            WriteAligned(_guidWriter.BaseStream, stream);
            WriteAligned(_blobWriter.BaseStream, stream);
        }

        private static void WriteAligned(MemoryStream source, MemoryStream target)
        {
            int length = (int)source.Length;
            source.WriteTo(target);
            target.Write(0, BitArithmeticUtilities.Align(length, 4) - length);
        }
    }
}
