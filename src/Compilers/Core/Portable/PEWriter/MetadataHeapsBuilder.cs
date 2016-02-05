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
    /// Represents a value on #String heap that has not been serialized yet.
    /// </summary>
    internal struct StringIdx : IEquatable<StringIdx>
    {
        // index in _stringIndexToHeapPositionMap
        public readonly int MapIndex;

        internal StringIdx(int mapIndex)
        {
            MapIndex = mapIndex;
        }

        public bool Equals(StringIdx other)
        {
            return MapIndex == other.MapIndex;
        }

        public override bool Equals(object obj)
        {
            return obj is StringIdx && Equals((StringIdx)obj);
        }

        public override int GetHashCode()
        {
            return MapIndex.GetHashCode();
        }

        public static bool operator ==(StringIdx left, StringIdx right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(StringIdx left, StringIdx right)
        {
            return !left.Equals(right);
        }
    }

    /// <summary>
    /// Represents a value on #Blob heap that has not been serialized yet.
    /// </summary>
    internal struct BlobIdx : IEquatable<BlobIdx>
    {
        // The position of the blob on heap relative to the start of the heap.
        // In EnC deltas this value is not the same as the value stored in blob token.
        public readonly int HeapPosition;

        internal BlobIdx(int heapPosition)
        {
            HeapPosition = heapPosition;
        }

        public bool Equals(BlobIdx other)
        {
            return HeapPosition == other.HeapPosition;
        }

        public override bool Equals(object obj)
        {
            return obj is BlobIdx && Equals((BlobIdx)obj);
        }

        public override int GetHashCode()
        {
            return HeapPosition.GetHashCode();
        }

        public static bool operator ==(BlobIdx left, BlobIdx right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(BlobIdx left, BlobIdx right)
        {
            return !left.Equals(right);
        }
    }

    internal sealed class MetadataHeapsBuilder
    {
        // #US heap
        private readonly Dictionary<string, int> _userStrings = new Dictionary<string, int>();
        private readonly BlobBuilder _userStringWriter = new BlobBuilder(1024);
        private readonly int _userStringHeapStartOffset;

        // #String heap
        private Dictionary<string, StringIdx> _strings = new Dictionary<string, StringIdx>(128);
        private int[] _stringIndexToResolvedOffsetMap;
        private BlobBuilder _stringWriter;
        private readonly int _stringHeapStartOffset;

        // #Blob heap
        private readonly Dictionary<ImmutableArray<byte>, BlobIdx> _blobs = new Dictionary<ImmutableArray<byte>, BlobIdx>(ByteSequenceComparer.Instance);
        private readonly int _blobHeapStartOffset;
        private int _blobHeapSize;

        // #GUID heap
        private readonly Dictionary<Guid, int> _guids = new Dictionary<Guid, int>();
        private readonly BlobBuilder _guidWriter = new BlobBuilder(16); // full metadata has just a single guid

        private bool _streamsAreComplete;

        public MetadataHeapsBuilder(
            int userStringHeapStartOffset = 0,
            int stringHeapStartOffset = 0,
            int blobHeapStartOffset = 0,
            int guidHeapStartOffset = 0)
        {
            // Add zero-th entry to all heaps, even in EnC delta.
            // We don't want generation-relative handles to ever be IsNil.
            // In both full and delta metadata all nil heap handles should have zero value.
            // There should be no blob handle that references the 0 byte added at the 
            // beginning of the delta blob.
            _userStringWriter.WriteByte(0);

            _blobs.Add(ImmutableArray<byte>.Empty, new BlobIdx(0));
            _blobHeapSize = 1;

            // When EnC delta is applied #US, #String and #Blob heaps are appended.
            // Thus indices of strings and blobs added to this generation are offset
            // by the sum of respective heap sizes of all previous generations.
            _userStringHeapStartOffset = userStringHeapStartOffset;
            _stringHeapStartOffset = stringHeapStartOffset;
            _blobHeapStartOffset = blobHeapStartOffset;

            // Unlike other heaps, #Guid heap in EnC delta is zero-padded.
            _guidWriter.WriteBytes(0, guidHeapStartOffset);
        }

        internal BlobIdx GetBlobIndex(BlobBuilder builder)
        {
            // TODO: avoid making a copy if the blob exists in the index
            return GetBlobIndex(builder.ToImmutableArray());
        }

        internal BlobIdx GetBlobIndex(ImmutableArray<byte> blob)
        {
            BlobIdx index;
            if (!_blobs.TryGetValue(blob, out index))
            {
                Debug.Assert(!_streamsAreComplete);

                index = new BlobIdx(_blobHeapSize);
                _blobs.Add(blob, index);

                _blobHeapSize += BlobWriterImpl.GetCompressedIntegerSize(blob.Length) + blob.Length;
            }
            
            return index;
        }

        public BlobIdx GetConstantBlobIndex(object value)
        {
            string str = value as string;
            if (str != null)
            {
                return this.GetBlobIndex(str);
            }

            var writer = new BlobBuilder();
            writer.WriteConstant(value);
            return this.GetBlobIndex(writer);
        }

        public BlobIdx GetBlobIndex(string str)
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

        public BlobIdx GetBlobIndexUtf8(string str)
        {
            return GetBlobIndex(ImmutableArray.Create(Encoding.UTF8.GetBytes(str)));
        }

        public int GetGuidIndex(Guid guid)
        {
            if (guid == Guid.Empty)
            {
                return 0;
            }

            int result;
            if (_guids.TryGetValue(guid, out result))
            {
                return result;
            }

            return AllocateGuid(guid);
        }

        public int AllocateGuid(Guid guid)
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
            int result = (_guidWriter.Count >> 4) + 1;

            _guids.Add(guid, result);
            _guidWriter.WriteBytes(guid.ToByteArray());

            return result;
        }

        public StringIdx GetStringIndex(string str)
        {
            StringIdx index;
            if (str.Length == 0)
            {
                index = new StringIdx(0);
            }
            else if (!_strings.TryGetValue(str, out index))
            {
                Debug.Assert(!_streamsAreComplete);
                index = new StringIdx(_strings.Count + 1); // idx 0 is reserved for empty string
                _strings.Add(str, index);
            }

            return index;
        }

        public int ResolveStringIndex(StringIdx index)
        {
            return _stringIndexToResolvedOffsetMap[index.MapIndex];
        }

        public int ResolveBlobIndex(BlobIdx index)
        {
            return (index.HeapPosition == 0) ? 0 : _blobHeapStartOffset + index.HeapPosition;
        }

        public bool TryGetUserStringToken(string str, out int token)
        {
            int index;
            if (!_userStrings.TryGetValue(str, out index))
            {
                Debug.Assert(!_streamsAreComplete);

                index = _userStringWriter.Position + _userStringHeapStartOffset;

                // User strings are referenced by metadata tokens (8 bits of which are used for the token type) leaving only 24 bits for the offset. 
                if ((index & 0xFF000000) != 0)
                {
                    token = 0;
                    return false;
                }

                _userStrings.Add(str, index);
                _userStringWriter.WriteCompressedInteger((uint)str.Length * 2 + 1);

                _userStringWriter.WriteUTF16(str);

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

            token = 0x70000000 | index;
            return true;
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

            heapSizes[(int)HeapIndex.UserString] = _userStringWriter.Count;
            heapSizes[(int)HeapIndex.String] = _stringWriter.Count;
            heapSizes[(int)HeapIndex.Blob] = _blobHeapSize;
            heapSizes[(int)HeapIndex.Guid] = _guidWriter.Count;

            return ImmutableArray.CreateRange(heapSizes);
        }

        /// <summary>
        /// Fills in stringIndexMap with data from stringIndex and write to stringWriter.
        /// Releases stringIndex as the stringTable is sealed after this point.
        /// </summary>
        private void SerializeStringHeap()
        {
            // Sort by suffix and remove stringIndex
            var sorted = new List<KeyValuePair<string, StringIdx>>(_strings);
            sorted.Sort(new SuffixSort());
            _strings = null;

            _stringWriter = new BlobBuilder(1024);

            // Create VirtIdx to Idx map and add entry for empty string
            _stringIndexToResolvedOffsetMap = new int[sorted.Count + 1];

            _stringIndexToResolvedOffsetMap[0] = 0;
            _stringWriter.WriteByte(0);

            // Find strings that can be folded
            string prev = string.Empty;
            foreach (KeyValuePair<string, StringIdx> entry in sorted)
            {
                int position = _stringHeapStartOffset + _stringWriter.Position;
                
                // It is important to use ordinal comparison otherwise we'll use the current culture!
                if (prev.EndsWith(entry.Key, StringComparison.Ordinal) && !BlobUtilities.IsLowSurrogateChar(entry.Key[0]))
                {
                    // Map over the tail of prev string. Watch for null-terminator of prev string.
                    _stringIndexToResolvedOffsetMap[entry.Value.MapIndex] = position - (BlobUtilities.GetUTF8ByteCount(entry.Key) + 1);
                }
                else
                {
                    _stringIndexToResolvedOffsetMap[entry.Value.MapIndex] = position;
                    _stringWriter.WriteUTF8(entry.Key, allowUnpairedSurrogates: false);
                    _stringWriter.WriteByte(0);
                }

                prev = entry.Key;
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

        public void WriteTo(BlobBuilder writer, out int guidHeapStartOffset)
        {
            WriteAligned(_stringWriter, writer);
            WriteAligned(_userStringWriter, writer);

            guidHeapStartOffset = writer.Position;

            WriteAligned(_guidWriter, writer);
            WriteAlignedBlobHeap(writer);
        }

        private void WriteAlignedBlobHeap(BlobBuilder builder)
        {
            int alignment = BitArithmeticUtilities.Align(_blobHeapSize, 4) - _blobHeapSize;

            var writer = builder.ReserveBytes(_blobHeapSize + alignment);

            // Perf consideration: With large heap the following loop may cause a lot of cache misses 
            // since the order of entries in _blobs dictionary depends on the hash of the array values, 
            // which is not correlated to the heap index. If we observe such issue we should order 
            // the entries by heap position before running this loop.
            foreach (var entry in _blobs)
            {
                int heapOffset = entry.Value.HeapPosition;
                var blob = entry.Key;

                writer.Offset = heapOffset;
                writer.WriteCompressedInteger((uint)blob.Length);
                writer.WriteBytes(blob);
            }

            writer.Offset = _blobHeapSize;
            writer.WriteBytes(0, alignment);
        }

        private static void WriteAligned(BlobBuilder source, BlobBuilder target)
        {
            int length = source.Count;
            target.LinkSuffix(source);
            target.WriteBytes(0, BitArithmeticUtilities.Align(length, 4) - length);
        }
    }
}
