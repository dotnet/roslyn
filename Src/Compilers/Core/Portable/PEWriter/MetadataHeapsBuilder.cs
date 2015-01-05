
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
        private static readonly Encoding Utf8Encoding = Encoding.UTF8;

        // #US heap
        private readonly Dictionary<string, uint> userStringIndex = new Dictionary<string, uint>();
        private readonly BinaryWriter userStringWriter = new BinaryWriter(new MemoryStream(1024), true);
        private readonly int userStringIndexStartOffset;

        // #String heap
        private Dictionary<string, uint> stringIndex = new Dictionary<string, uint>(128);
        private Dictionary<uint, uint> stringIndexMap;
        private readonly BinaryWriter stringWriter = new BinaryWriter(new MemoryStream(1024));
        private readonly int stringIndexStartOffset;

        // #Blob heap
        private readonly Dictionary<ImmutableArray<byte>, uint> blobIndex = new Dictionary<ImmutableArray<byte>, uint>(ByteSequenceComparer.Instance);
        private readonly BinaryWriter blobWriter = new BinaryWriter(new MemoryStream(1024));
        private readonly int blobIndexStartOffset;

        // #GUID heap
        private readonly Dictionary<Guid, uint> guidIndex = new Dictionary<Guid, uint>();
        private readonly BinaryWriter guidWriter = new BinaryWriter(new MemoryStream(16)); // full metadata has just a single guid

        private bool streamsAreComplete;

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
            this.blobWriter.WriteByte(0);
            this.stringWriter.WriteByte(0);
            this.userStringWriter.WriteByte(0);

            // When EnC delta is applied #US, #String and #Blob heaps are appended.
            // Thus indices of strings and blobs added to this generation are offset
            // by the sum of respective heap sizes of all previous generations.
            this.userStringIndexStartOffset = userStringIndexStartOffset;
            this.stringIndexStartOffset = stringIndexStartOffset;
            this.blobIndexStartOffset = blobIndexStartOffset;

            // Unlike other heaps, #Guid heap in EnC delta is zero-padded.
            this.guidWriter.Pad(guidIndexStartOffset);
        }

        internal uint GetBlobIndex(MemoryStream stream)
        {
            // TODO: avoid making a copy if the blob exists in the index
            return GetBlobIndex(stream.ToImmutableArray());
        }

        internal uint GetBlobIndex(ImmutableArray<byte> blob)
        {
            uint result = 0;
            if (blob.Length == 0 || this.blobIndex.TryGetValue(blob, out result))
            {
                return result;
            }

            Debug.Assert(!this.streamsAreComplete);
            result = this.blobWriter.BaseStream.Position + (uint)blobIndexStartOffset;
            this.blobIndex.Add(blob, result);
            this.blobWriter.WriteCompressedUInt((uint)blob.Length);
            this.blobWriter.WriteBytes(blob);
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
            if (this.guidIndex.TryGetValue(guid, out result))
            {
                return result;
            }

            return AllocateGuid(guid);
        }

        public uint AllocateGuid(Guid guid)
        {
            Debug.Assert(!this.streamsAreComplete);

            // The only GUIDs that are serialized are MVID, EncId, and EncBaseId in the
            // Module table. Each of those GUID offsets are relative to the local heap,
            // even for deltas, so there's no need for a GetGuidStreamPosition() method
            // to offset the positions by the size of the original heap in delta metadata.
            // Unlike #Blob, #String and #US streams delta #GUID stream is padded to the 
            // size of the previous generation #GUID stream before new GUIDs are added.

            // Metadata Spec: 
            // The Guid heap is an array of GUIDs, each 16 bytes wide. 
            // Its first element is numbered 1, its second 2, and so on.
            uint result = (this.guidWriter.BaseStream.Length >> 4) + 1;

            this.guidIndex.Add(guid, result);
            this.guidWriter.WriteBytes(guid.ToByteArray());

            return result;
        }

        public unsafe byte[] GetExistingBlob(int signatureOffset)
        {
            fixed (byte* ptr = this.blobWriter.BaseStream.Buffer)
            {
                var reader = new BlobReader(ptr + signatureOffset, (int)this.blobWriter.BaseStream.Length + (int)this.blobIndexStartOffset - signatureOffset);
                int size;
                bool isValid = reader.TryReadCompressedInteger(out size);
                Debug.Assert(isValid);
                return reader.ReadBytes(size);
            }
        }

        public StringIdx GetStringIndex(string str)
        {
            uint index = 0;
            if (str.Length > 0 && !this.stringIndex.TryGetValue(str, out index))
            {
                Debug.Assert(!this.streamsAreComplete);
                index = (uint)this.stringIndex.Count + 1; // idx 0 is reserved for empty string
                this.stringIndex.Add(str, index);
            }

            return new StringIdx(index);
        }

        public uint ResolveStringIndex(StringIdx index)
        {
            return this.stringIndexMap[index.VirtIdx];
        }

        public uint GetUserStringToken(string str)
        {
            uint index;
            if (!this.userStringIndex.TryGetValue(str, out index))
            {
                Debug.Assert(!this.streamsAreComplete);
                index = this.userStringWriter.BaseStream.Position + (uint)this.userStringIndexStartOffset;
                this.userStringIndex.Add(str, index);
                this.userStringWriter.WriteCompressedUInt((uint)str.Length * 2 + 1);
                this.userStringWriter.WriteChars(str.ToCharArray());

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

                this.userStringWriter.WriteByte(stringKind);
            }

            return 0x70000000 | index;
        }

        public void Complete()
        {
            Debug.Assert(!streamsAreComplete);
            streamsAreComplete = true;
            SerializeStringHeap();
        }

        public ImmutableArray<int> GetHeapSizes()
        {
            var heapSizes = new int[MetadataTokens.HeapCount];

            heapSizes[(int)HeapIndex.UserString] = (int)this.userStringWriter.BaseStream.Length;
            heapSizes[(int)HeapIndex.String] = (int)this.stringWriter.BaseStream.Length;
            heapSizes[(int)HeapIndex.Blob] = (int)this.blobWriter.BaseStream.Length;
            heapSizes[(int)HeapIndex.Guid] = (int)this.guidWriter.BaseStream.Length;

            return ImmutableArray.CreateRange(heapSizes);
        }

        /// <summary>
        /// Fills in stringIndexMap with data from stringIndex and write to stringWriter.
        /// Releases stringIndex as the stringTable is sealed after this point.
        /// </summary>
        private void SerializeStringHeap()
        {
            // Sort by suffix and remove stringIndex
            var sorted = new List<KeyValuePair<string, uint>>(this.stringIndex);
            sorted.Sort(new SuffixSort());
            this.stringIndex = null;

            // Create VirtIdx to Idx map and add entry for empty string
            this.stringIndexMap = new Dictionary<uint, uint>(sorted.Count);
            this.stringIndexMap.Add(0, 0);

            // Find strings that can be folded
            string prev = String.Empty;
            foreach (KeyValuePair<string, uint> cur in sorted)
            {
                uint position = this.stringWriter.BaseStream.Position + (uint)this.stringIndexStartOffset;

                // It is important to use ordinal comparison otherwise we'll use the current culture!
                if (prev.EndsWith(cur.Key, StringComparison.Ordinal))
                {
                    // Map over the tail of prev string. Watch for null-terminator of prev string.
                    this.stringIndexMap.Add(cur.Value, position - (uint)(Utf8Encoding.GetByteCount(cur.Key) + 1));
                }
                else
                {
                    this.stringIndexMap.Add(cur.Value, position);

                    // TODO (tomat): consider reusing the buffer instead of allocating a new one for each string
                    this.stringWriter.WriteBytes(Utf8Encoding.GetBytes(cur.Key));

                    this.stringWriter.WriteByte(0);
                }

                prev = cur.Key;
            }
        }

        /// <summary>
        /// Sorts strings such that a string is followed immediately by all strings
        /// that are a suffix of it.  
        /// </summary>
        private class SuffixSort : IComparer<KeyValuePair<string, uint>>
        {
            public int Compare(KeyValuePair<string, uint> xPair, KeyValuePair<string, uint> yPair)
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
            WriteAligned(this.stringWriter.BaseStream, stream);
            WriteAligned(this.userStringWriter.BaseStream, stream);

            guidHeapStartOffset = stream.Position;

            WriteAligned(this.guidWriter.BaseStream, stream);
            WriteAligned(this.blobWriter.BaseStream, stream);
        }

        private void WriteAligned(MemoryStream source, MemoryStream target)
        {
            int length = (int)source.Length;
            source.WriteTo(target);
            target.Write(0, BitArithmeticUtilities.Align(length, 4) - length);
        }
    }
}
