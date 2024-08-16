// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Xml;

namespace Roslyn.Test.PdbUtilities
{
    public class Token2SourceLineExporter
    {
        // NOTE: this type implementation is essentially an extraction from PdbReader 
        //       located under ndp\clr\src\ToolBox\CCI2\PdbReader folder

        private class PdbSource
        {
            internal readonly string name;
            internal Guid doctype;
            internal Guid language;
            internal Guid vendor;

            internal PdbSource(string name, Guid doctype, Guid language, Guid vendor)
            {
                this.name = name;
                this.doctype = doctype;
                this.language = language;
                this.vendor = vendor;
            }
        }

        private class PdbTokenLine
        {
            internal readonly uint token;
            internal readonly uint file_id;
            internal readonly uint line;
            internal readonly uint column;
            internal readonly uint endLine;
            internal readonly uint endColumn;
            internal PdbSource sourceFile;
            internal PdbTokenLine/*?*/ nextLine;

            internal PdbTokenLine(uint token, uint file_id, uint line, uint column, uint endLine, uint endColumn)
            {
                this.token = token;
                this.file_id = file_id;
                this.line = line;
                this.column = column;
                this.endLine = endLine;
                this.endColumn = endColumn;
            }
        }

        private class BitAccess
        {
            internal BitAccess(int capacity)
            {
                _buffer = new byte[capacity];
            }

            internal byte[] Buffer
            {
                get { return _buffer; }
            }
            private byte[] _buffer;

            internal void FillBuffer(Stream stream, int capacity)
            {
                MinCapacity(capacity);
                stream.Read(_buffer, 0, capacity);
                _offset = 0;
            }

            internal void Append(Stream stream, int count)
            {
                int newCapacity = _offset + count;
                if (_buffer.Length < newCapacity)
                {
                    byte[] newBuffer = new byte[newCapacity];
                    Array.Copy(_buffer, newBuffer, _buffer.Length);
                    _buffer = newBuffer;
                }
                stream.Read(_buffer, _offset, count);
                _offset += count;
            }

            internal int Position
            {
                get { return _offset; }
                set { _offset = value; }
            }
            private int _offset;

            internal void MinCapacity(int capacity)
            {
                if (_buffer.Length < capacity)
                {
                    _buffer = new byte[capacity];
                }
                _offset = 0;
            }

            internal void Align(int alignment)
            {
                while ((_offset % alignment) != 0)
                {
                    _offset++;
                }
            }

            internal void ReadInt16(out short value)
            {
                unchecked
                {
                    value = (short)((_buffer[_offset + 0] & 0xFF) |
                                          (_buffer[_offset + 1] << 8));
                }
                _offset += 2;
            }

            internal void ReadInt8(out sbyte value)
            {
                unchecked
                {
                    value = (sbyte)_buffer[_offset];
                }
                _offset += 1;
            }

            internal void ReadInt32(out int value)
            {
                unchecked
                {
                    value = (int)((_buffer[_offset + 0] & 0xFF) |
                                        (_buffer[_offset + 1] << 8) |
                                        (_buffer[_offset + 2] << 16) |
                                        (_buffer[_offset + 3] << 24));
                }
                _offset += 4;
            }

            internal void ReadInt64(out long value)
            {
                unchecked
                {
                    value = (long)(((ulong)_buffer[_offset + 0] & 0xFF) |
                                       ((ulong)_buffer[_offset + 1] << 8) |
                                       ((ulong)_buffer[_offset + 2] << 16) |
                                       ((ulong)_buffer[_offset + 3] << 24) |
                                       ((ulong)_buffer[_offset + 4] << 32) |
                                       ((ulong)_buffer[_offset + 5] << 40) |
                                       ((ulong)_buffer[_offset + 6] << 48) |
                                       ((ulong)_buffer[_offset + 7] << 56));
                }
                _offset += 8;
            }

            internal void ReadUInt16(out ushort value)
            {
                unchecked
                {
                    value = (ushort)((_buffer[_offset + 0] & 0xFF) |
                                           (_buffer[_offset + 1] << 8));
                }
                _offset += 2;
            }

            internal void ReadUInt8(out byte value)
            {
                unchecked
                {
                    value = (byte)((_buffer[_offset + 0] & 0xFF));
                }
                _offset += 1;
            }

            internal void ReadUInt32(out uint value)
            {
                unchecked
                {
                    value = (uint)((_buffer[_offset + 0] & 0xFF) |
                                         (_buffer[_offset + 1] << 8) |
                                         (_buffer[_offset + 2] << 16) |
                                         (_buffer[_offset + 3] << 24));
                }
                _offset += 4;
            }

            internal void ReadUInt64(out ulong value)
            {
                unchecked
                {
                    value = (ulong)(((ulong)_buffer[_offset + 0] & 0xFF) |
                                       ((ulong)_buffer[_offset + 1] << 8) |
                                       ((ulong)_buffer[_offset + 2] << 16) |
                                       ((ulong)_buffer[_offset + 3] << 24) |
                                       ((ulong)_buffer[_offset + 4] << 32) |
                                       ((ulong)_buffer[_offset + 5] << 40) |
                                       ((ulong)_buffer[_offset + 6] << 48) |
                                       ((ulong)_buffer[_offset + 7] << 56));
                }
                _offset += 8;
            }

            internal void ReadInt32(int[] values)
            {
                for (int i = 0; i < values.Length; i++)
                {
                    ReadInt32(out values[i]);
                }
            }

            internal void ReadUInt32(uint[] values)
            {
                for (int i = 0; i < values.Length; i++)
                {
                    ReadUInt32(out values[i]);
                }
            }

            internal void ReadBytes(byte[] bytes)
            {
                for (int i = 0; i < bytes.Length; i++)
                {
                    bytes[i] = _buffer[_offset++];
                }
            }

            internal float ReadFloat()
            {
                float result = BitConverter.ToSingle(_buffer, _offset);
                _offset += 4;
                return result;
            }

            internal double ReadDouble()
            {
                double result = BitConverter.ToDouble(_buffer, _offset);
                _offset += 8;
                return result;
            }

            internal decimal ReadDecimal()
            {
                int[] bits = new int[4];
                this.ReadInt32(bits);
                return new decimal(bits);
            }

            internal void ReadBString(out string value)
            {
                this.ReadUInt16(out var len);
                value = Encoding.UTF8.GetString(_buffer, _offset, len);
                _offset += len;
            }

            internal void ReadCString(out string value)
            {
                int len = 0;
                while (_offset + len < _buffer.Length && _buffer[_offset + len] != 0)
                {
                    len++;
                }
                value = Encoding.UTF8.GetString(_buffer, _offset, len);
                _offset += len + 1;
            }

            internal void SkipCString(out string value)
            {
                int len = 0;
                while (_offset + len < _buffer.Length && _buffer[_offset + len] != 0)
                {
                    len++;
                }
                _offset += len + 1;
                value = null;
            }

            internal void ReadGuid(out Guid guid)
            {

                ReadUInt32(out var a);
                ReadUInt16(out var b);
                ReadUInt16(out var c);
                ReadUInt8(out var d);
                ReadUInt8(out var e);
                ReadUInt8(out var f);
                ReadUInt8(out var g);
                ReadUInt8(out var h);
                ReadUInt8(out var i);
                ReadUInt8(out var j);
                ReadUInt8(out var k);

                guid = unchecked(new Guid((int)a, (short)b, (short)c, d, e, f, g, h, i, j, k));
            }

            internal string ReadString()
            {
                int len = 0;
                while (_offset + len < _buffer.Length && _buffer[_offset + len] != 0)
                {
                    len += 2;
                }
                string result = Encoding.Unicode.GetString(_buffer, _offset, len);
                _offset += len + 2;
                return result;
            }
        }

        private readonly struct BitSet
        {
            internal BitSet(BitAccess bits)
            {
                bits.ReadInt32(out _size);    // 0..3 : Number of words
                _words = new uint[_size];
                bits.ReadUInt32(_words);
            }

            internal bool IsSet(int index)
            {
                int word = index / 32;
                if (word >= _size)
                    return false;
                return ((_words[word] & GetBit(index)) != 0);
            }

            private static uint GetBit(int index)
            {
                return ((uint)1 << (index % 32));
            }

            internal bool IsEmpty
            {
                get { return _size == 0; }
            }

            private readonly int _size;
            private readonly uint[] _words;
        }

        private class IntHashTable
        {
#pragma warning disable format // https://github.com/dotnet/roslyn/issues/70711 tracks removing this suppression.
            private static readonly int[] s_primes = [
                3, 7, 11, 17, 23, 29, 37, 47, 59, 71, 89, 107, 131, 163, 197, 239, 293, 353, 431, 521, 631, 761, 919,
                1103, 1327, 1597, 1931, 2333, 2801, 3371, 4049, 4861, 5839, 7013, 8419, 10103, 12143, 14591,
                17519, 21023, 25229, 30293, 36353, 43627, 52361, 62851, 75431, 90523, 108631, 130363, 156437,
                187751, 225307, 270371, 324449, 389357, 467237, 560689, 672827, 807403, 968897, 1162687, 1395263,
                1674319, 2009191, 2411033, 2893249, 3471899, 4166287, 4999559, 5999471, 7199369];
#pragma warning restore format

            private static int GetPrime(int minSize)
            {
                if (minSize < 0)
                {
                    throw new ArgumentException("Arg_HTCapacityOverflow");
                }
                for (int i = 0; i < s_primes.Length; i++)
                {
                    int size = s_primes[i];
                    if (size >= minSize)
                    {
                        return size;
                    }
                }
                throw new ArgumentException("Arg_HTCapacityOverflow");
            }

            // Deleted entries have their key set to buckets

            // The hash table data.
            // This cannot be serialized
            private struct @bucket
            {
                internal int key;
                internal int hash_coll;   // Store hash code; sign bit means there was a collision.
                internal Object val;
            }

            private bucket[] _buckets;

            // The total number of entries in the hash table.
            private int _count;

            // The total number of collision bits set in the hashtable
            private int _occupancy;

            private int _loadsize;
            private readonly int _loadFactorPerc;    // 100 = 1.0

            private int _version;

            // Constructs a new hashtable. The hashtable is created with an initial
            // capacity of zero and a load factor of 1.0.
            //| <include path='docs/doc[@for="IntHashTable.IntHashTable"]/*' />
            internal IntHashTable()
                : this(0, 100)
            {
            }

            internal IntHashTable(int capacity, int loadFactorPerc)
            {
                if (capacity < 0)
                    throw new ArgumentOutOfRangeException(nameof(capacity), "ArgumentOutOfRange_NeedNonNegNum");
                if (loadFactorPerc is not (>= 10 and <= 100))
                    throw new ArgumentOutOfRangeException(nameof(loadFactorPerc), "ArgumentOutOfRange_IntHashTableLoadFactor");

                // Based on perf work, .72 is the optimal load factor for this table.
                _loadFactorPerc = (loadFactorPerc * 72) / 100;

                int hashsize = GetPrime((int)(capacity / _loadFactorPerc));
                _buckets = new bucket[hashsize];

                _loadsize = (int)(_loadFactorPerc * hashsize) / 100;
                if (_loadsize >= hashsize)
                    _loadsize = hashsize - 1;
            }

            private static uint InitHash(int key, int hashsize, out uint seed, out uint incr)
            {
                // Hashcode must be positive.  Also, we must not use the sign bit, since
                // that is used for the collision bit.
                uint hashcode = (uint)key & 0x7FFFFFFF;
                seed = (uint)hashcode;
                // Restriction: incr MUST be between 1 and hashsize - 1, inclusive for
                // the modular arithmetic to work correctly.  This guarantees you'll
                // visit every bucket in the table exactly once within hashsize
                // iterations.  Violate this and it'll cause obscure bugs forever.
                // If you change this calculation for h2(key), update putEntry too!
                incr = (uint)(1 + (((seed >> 5) + 1) % ((uint)hashsize - 1)));
                return hashcode;
            }

            internal void Add(int key, Object value)
            {
                Insert(key, value, true);
            }

            internal Object this[int key]
            {
                get
                {
                    if (key < 0)
                    {
                        throw new ArgumentException("Argument_KeyLessThanZero");
                    }
                    // Take a snapshot of buckets, in case another thread does a resize
                    bucket[] lbuckets = _buckets;
                    uint hashcode = InitHash(key, lbuckets.Length, out var seed, out var incr);
                    int ntry = 0;

                    bucket b;
                    do
                    {
                        int bucketNumber = (int)(seed % (uint)lbuckets.Length);
                        b = lbuckets[bucketNumber];
                        if (b.val == null)
                        {
                            return null;
                        }
                        if (((b.hash_coll & 0x7FFFFFFF) == hashcode) && key == b.key)
                        {
                            return b.val;
                        }
                        seed += incr;
                    } while (b.hash_coll < 0 && ++ntry < lbuckets.Length);
                    return null;
                }
                //set {
                //  Insert(key, value, false);
                //}
            }

            private void expand()
            {
                rehash(GetPrime(1 + _buckets.Length * 2));
            }

            private void rehash()
            {
                rehash(_buckets.Length);
            }

            private void rehash(int newsize)
            {
                // reset occupancy
                _occupancy = 0;

                // Don't replace any internal state until we've finished adding to the
                // new bucket[].  This serves two purposes:
                //   1) Allow concurrent readers to see valid hashtable contents
                //      at all times
                //   2) Protect against an OutOfMemoryException while allocating this
                //      new bucket[].
                bucket[] newBuckets = new bucket[newsize];

                // rehash table into new buckets
                int nb;
                for (nb = 0; nb < _buckets.Length; nb++)
                {
                    bucket oldb = _buckets[nb];
                    if (oldb.val != null)
                    {
                        putEntry(newBuckets, oldb.key, oldb.val, oldb.hash_coll & 0x7FFFFFFF);
                    }
                }

                // New bucket[] is good to go - replace buckets and other internal state.
                _version++;
                _buckets = newBuckets;
                _loadsize = (int)(_loadFactorPerc * newsize) / 100;

                if (_loadsize >= newsize)
                {
                    _loadsize = newsize - 1;
                }

                return;
            }

            private void Insert(int key, Object nvalue, bool add)
            {
                if (key < 0)
                {
                    throw new ArgumentException("Argument_KeyLessThanZero");
                }
                if (nvalue == null)
                {
                    throw new ArgumentNullException(nameof(nvalue), "ArgumentNull_Value");
                }
                if (_count >= _loadsize)
                {
                    expand();
                }
                else if (_occupancy > _loadsize && _count > 100)
                {
                    rehash();
                }
                // Assume we only have one thread writing concurrently.  Modify
                // buckets to contain new data, as long as we insert in the right order.
                uint hashcode = InitHash(key, _buckets.Length, out var seed, out var incr);
                int ntry = 0;
                int emptySlotNumber = -1; // We use the empty slot number to cache the first empty slot. We chose to reuse slots
                // create by remove that have the collision bit set over using up new slots.

                do
                {
                    int bucketNumber = (int)(seed % (uint)_buckets.Length);

                    // Set emptySlot number to current bucket if it is the first available bucket that we have seen
                    // that once contained an entry and also has had a collision.
                    // We need to search this entire collision chain because we have to ensure that there are no
                    // duplicate entries in the table.

                    // Insert the key/value pair into this bucket if this bucket is empty and has never contained an entry
                    // OR
                    // This bucket once contained an entry but there has never been a collision
                    if (_buckets[bucketNumber].val == null)
                    {
                        // If we have found an available bucket that has never had a collision, but we've seen an available
                        // bucket in the past that has the collision bit set, use the previous bucket instead
                        if (emptySlotNumber != -1)
                        { // Reuse slot
                            bucketNumber = emptySlotNumber;
                        }

                        // We pretty much have to insert in this order.  Don't set hash
                        // code until the value & key are set appropriately.
                        _buckets[bucketNumber].val = nvalue;
                        _buckets[bucketNumber].key = key;
                        _buckets[bucketNumber].hash_coll |= (int)hashcode;
                        _count++;
                        _version++;
                        return;
                    }

                    // The current bucket is in use
                    // OR
                    // it is available, but has had the collision bit set and we have already found an available bucket
                    if (((_buckets[bucketNumber].hash_coll & 0x7FFFFFFF) == hashcode) &&
                                key == _buckets[bucketNumber].key)
                    {
                        if (add)
                        {
                            throw new ArgumentException("Argument_AddingDuplicate__" + _buckets[bucketNumber].key);
                        }
                        _buckets[bucketNumber].val = nvalue;
                        _version++;
                        return;
                    }

                    // The current bucket is full, and we have therefore collided.  We need to set the collision bit
                    // UNLESS
                    // we have remembered an available slot previously.
                    if (emptySlotNumber == -1)
                    {// We don't need to set the collision bit here since we already have an empty slot
                        if (_buckets[bucketNumber].hash_coll >= 0)
                        {
                            _buckets[bucketNumber].hash_coll |= unchecked((int)0x80000000);
                            _occupancy++;
                        }
                    }
                    seed += incr;
                } while (++ntry < _buckets.Length);

                // This code is here if and only if there were no buckets without a collision bit set in the entire table
                if (emptySlotNumber != -1)
                {
                    // We pretty much have to insert in this order.  Don't set hash
                    // code until the value & key are set appropriately.
                    _buckets[emptySlotNumber].val = nvalue;
                    _buckets[emptySlotNumber].key = key;
                    _buckets[emptySlotNumber].hash_coll |= (int)hashcode;
                    _count++;
                    _version++;
                    return;
                }

                // If you see this assert, make sure load factor & count are reasonable.
                // Then verify that our double hash function (h2, described at top of file)
                // meets the requirements described above. You should never see this assert.
                throw new InvalidOperationException("InvalidOperation_HashInsertFailed");
            }

            private void putEntry(bucket[] newBuckets, int key, Object nvalue, int hashcode)
            {
                uint seed = (uint)hashcode;
                uint incr = (uint)(1 + (((seed >> 5) + 1) % ((uint)newBuckets.Length - 1)));

                do
                {
                    int bucketNumber = (int)(seed % (uint)newBuckets.Length);

                    if ((newBuckets[bucketNumber].val == null))
                    {
                        newBuckets[bucketNumber].val = nvalue;
                        newBuckets[bucketNumber].key = key;
                        newBuckets[bucketNumber].hash_coll |= hashcode;
                        return;
                    }

                    if (newBuckets[bucketNumber].hash_coll >= 0)
                    {
                        newBuckets[bucketNumber].hash_coll |= unchecked((int)0x80000000);
                        _occupancy++;
                    }
                    seed += incr;
                } while (true);
            }
        }

        private readonly struct DbiSecCon
        {
            internal DbiSecCon(BitAccess bits)
            {
                bits.ReadInt16(out section);
                bits.ReadInt16(out pad1);
                bits.ReadInt32(out offset);
                bits.ReadInt32(out size);
                bits.ReadUInt32(out flags);
                bits.ReadInt16(out module);
                bits.ReadInt16(out pad2);
                bits.ReadUInt32(out dataCrc);
                bits.ReadUInt32(out relocCrc);
            }

            internal readonly short section;                    // 0..1
            internal readonly short pad1;                       // 2..3
            internal readonly int offset;                     // 4..7
            internal readonly int size;                       // 8..11
            internal readonly uint flags;                      // 12..15
            internal readonly short module;                     // 16..17
            internal readonly short pad2;                       // 18..19
            internal readonly uint dataCrc;                    // 20..23
            internal readonly uint relocCrc;                   // 24..27
        }

        private class DbiModuleInfo
        {
            internal DbiModuleInfo(BitAccess bits, bool readStrings)
            {
                bits.ReadInt32(out opened);
                new DbiSecCon(bits);
                bits.ReadUInt16(out flags);
                bits.ReadInt16(out stream);
                bits.ReadInt32(out cbSyms);
                bits.ReadInt32(out cbOldLines);
                bits.ReadInt32(out cbLines);
                bits.ReadInt16(out files);
                bits.ReadInt16(out pad1);
                bits.ReadUInt32(out offsets);
                bits.ReadInt32(out niSource);
                bits.ReadInt32(out niCompiler);
                if (readStrings)
                {
                    bits.ReadCString(out moduleName);
                    bits.ReadCString(out objectName);
                }
                else
                {
                    bits.SkipCString(out moduleName);
                    bits.SkipCString(out objectName);
                }
                bits.Align(4);
            }

            internal readonly int opened;                 //  0..3
            internal readonly ushort flags;                  // 32..33
            internal readonly short stream;                 // 34..35
            internal readonly int cbSyms;                 // 36..39
            internal readonly int cbOldLines;             // 40..43
            internal readonly int cbLines;                // 44..57
            internal readonly short files;                  // 48..49
            internal readonly short pad1;                   // 50..51
            internal readonly uint offsets;
            internal readonly int niSource;
            internal readonly int niCompiler;
            internal readonly string moduleName;
            internal readonly string objectName;
        }

        private readonly struct DbiHeader
        {
            internal DbiHeader(BitAccess bits)
            {
                bits.ReadInt32(out sig);
                bits.ReadInt32(out ver);
                bits.ReadInt32(out age);
                bits.ReadInt16(out gssymStream);
                bits.ReadUInt16(out vers);
                bits.ReadInt16(out pssymStream);
                bits.ReadUInt16(out pdbver);
                bits.ReadInt16(out symrecStream);
                bits.ReadUInt16(out pdbver2);
                bits.ReadInt32(out gpmodiSize);
                bits.ReadInt32(out secconSize);
                bits.ReadInt32(out secmapSize);
                bits.ReadInt32(out filinfSize);
                bits.ReadInt32(out tsmapSize);
                bits.ReadInt32(out mfcIndex);
                bits.ReadInt32(out dbghdrSize);
                bits.ReadInt32(out ecinfoSize);
                bits.ReadUInt16(out flags);
                bits.ReadUInt16(out machine);
                bits.ReadInt32(out reserved);
            }

            internal readonly int sig;                        // 0..3
            internal readonly int ver;                        // 4..7
            internal readonly int age;                        // 8..11
            internal readonly short gssymStream;                // 12..13
            internal readonly ushort vers;                       // 14..15
            internal readonly short pssymStream;                // 16..17
            internal readonly ushort pdbver;                     // 18..19
            internal readonly short symrecStream;               // 20..21
            internal readonly ushort pdbver2;                    // 22..23
            internal readonly int gpmodiSize;                 // 24..27
            internal readonly int secconSize;                 // 28..31
            internal readonly int secmapSize;                 // 32..35
            internal readonly int filinfSize;                 // 36..39
            internal readonly int tsmapSize;                  // 40..43
            internal readonly int mfcIndex;                   // 44..47
            internal readonly int dbghdrSize;                 // 48..51
            internal readonly int ecinfoSize;                 // 52..55
            internal readonly ushort flags;                      // 56..57
            internal readonly ushort machine;                    // 58..59
            internal readonly int reserved;                   // 60..63
        }

        private readonly struct DbiDbgHdr
        {
            internal DbiDbgHdr(BitAccess bits)
            {
                bits.ReadUInt16(out snFPO);
                bits.ReadUInt16(out snException);
                bits.ReadUInt16(out snFixup);
                bits.ReadUInt16(out snOmapToSrc);
                bits.ReadUInt16(out snOmapFromSrc);
                bits.ReadUInt16(out snSectionHdr);
                bits.ReadUInt16(out snTokenRidMap);
                bits.ReadUInt16(out snXdata);
                bits.ReadUInt16(out snPdata);
                bits.ReadUInt16(out snNewFPO);
                bits.ReadUInt16(out snSectionHdrOrig);
            }

            internal readonly ushort snFPO;                 // 0..1
            internal readonly ushort snException;           // 2..3 (deprecated)
            internal readonly ushort snFixup;               // 4..5
            internal readonly ushort snOmapToSrc;           // 6..7
            internal readonly ushort snOmapFromSrc;         // 8..9
            internal readonly ushort snSectionHdr;          // 10..11
            internal readonly ushort snTokenRidMap;         // 12..13
            internal readonly ushort snXdata;               // 14..15
            internal readonly ushort snPdata;               // 16..17
            internal readonly ushort snNewFPO;              // 18..19
            internal readonly ushort snSectionHdrOrig;      // 20..21
        }

        private class PdbFileHeader
        {
            internal PdbFileHeader(Stream reader, BitAccess bits)
            {
                bits.MinCapacity(56);
                reader.Seek(0, SeekOrigin.Begin);
                bits.FillBuffer(reader, 52);

                this.magic = new byte[32];
                bits.ReadBytes(this.magic);                 //   0..31
                bits.ReadInt32(out this.pageSize);          //  32..35
                bits.ReadInt32(out this.freePageMap);       //  36..39
                bits.ReadInt32(out this.pagesUsed);         //  40..43
                bits.ReadInt32(out this.directorySize);     //  44..47
                bits.ReadInt32(out this.zero);              //  48..51

                int directoryPages = ((((directorySize + pageSize - 1) / pageSize) * 4) + pageSize - 1) / pageSize;
                this.directoryRoot = new int[directoryPages];
                bits.FillBuffer(reader, directoryPages * 4);
                bits.ReadInt32(this.directoryRoot);
            }

            internal readonly byte[] magic;
            internal readonly int pageSize;
            internal readonly int freePageMap;
            internal readonly int pagesUsed;
            internal readonly int directorySize;
            internal readonly int zero;
            internal readonly int[] directoryRoot;
        }

        private class PdbReader
        {
            internal PdbReader(Stream reader, int pageSize)
            {
                this.pageSize = pageSize;
                this.reader = reader;
            }

            internal void Seek(int page, int offset)
            {
                reader.Seek(page * pageSize + offset, SeekOrigin.Begin);
            }

            internal void Read(byte[] bytes, int offset, int count)
            {
                reader.Read(bytes, offset, count);
            }

            internal int PagesFromSize(int size)
            {
                return (size + pageSize - 1) / (pageSize);
            }

            internal readonly int pageSize;
            internal readonly Stream reader;
        }

        private class DataStream
        {
            internal DataStream()
            {
            }

            internal DataStream(int contentSize, BitAccess bits, int count)
            {
                this.contentSize = contentSize;
                if (count > 0)
                {
                    this.pages = new int[count];
                    bits.ReadInt32(this.pages);
                }
            }

            internal void Read(PdbReader reader, BitAccess bits)
            {
                bits.MinCapacity(contentSize);
                Read(reader, 0, bits.Buffer, 0, contentSize);
            }

            internal void Read(PdbReader reader, int position,
                             byte[] bytes, int offset, int data)
            {
                if (position + data > contentSize)
                {
                    throw new Exception(
                        string.Format(
                            "DataStream can't read off end of stream. (pos={0},siz={1})",
                            position, data));
                }
                if (position == contentSize)
                {
                    return;
                }

                int left = data;
                int page = position / reader.pageSize;
                int rema = position % reader.pageSize;

                // First get remained of first page.
                if (rema != 0)
                {
                    int todo = reader.pageSize - rema;
                    if (todo > left)
                    {
                        todo = left;
                    }

                    reader.Seek(pages[page], rema);
                    reader.Read(bytes, offset, todo);

                    offset += todo;
                    left -= todo;
                    page++;
                }

                // Now get the remaining pages.
                while (left > 0)
                {
                    int todo = reader.pageSize;
                    if (todo > left)
                    {
                        todo = left;
                    }

                    reader.Seek(pages[page], 0);
                    reader.Read(bytes, offset, todo);

                    offset += todo;
                    left -= todo;
                    page++;
                }
            }

            internal int Length
            {
                get { return contentSize; }
            }

            internal readonly int contentSize;
            internal readonly int[] pages;
        }

        private class MsfDirectory
        {
            internal MsfDirectory(PdbReader reader, PdbFileHeader head, BitAccess bits)
            {
                int pages = reader.PagesFromSize(head.directorySize);

                // 0..n in page of directory pages.
                bits.MinCapacity(head.directorySize);
                int directoryRootPages = head.directoryRoot.Length;
                int pagesPerPage = head.pageSize / 4;
                int pagesToGo = pages;
                for (int i = 0; i < directoryRootPages; i++)
                {
                    int pagesInThisPage = pagesToGo <= pagesPerPage ? pagesToGo : pagesPerPage;
                    reader.Seek(head.directoryRoot[i], 0);
                    bits.Append(reader.reader, pagesInThisPage * 4);
                    pagesToGo -= pagesInThisPage;
                }
                bits.Position = 0;

                DataStream stream = new DataStream(head.directorySize, bits, pages);
                bits.MinCapacity(head.directorySize);
                stream.Read(reader, bits);

                // 0..3 in directory pages
                bits.ReadInt32(out var count);

                // 4..n
                int[] sizes = new int[count];
                bits.ReadInt32(sizes);

                // n..m
                streams = new DataStream[count];
                for (int i = 0; i < count; i++)
                {
                    if (sizes[i] <= 0)
                    {
                        streams[i] = new DataStream();
                    }
                    else
                    {
                        streams[i] = new DataStream(sizes[i], bits,
                                                    reader.PagesFromSize(sizes[i]));
                    }
                }
            }

            internal readonly DataStream[] streams;
        }

        private struct CV_FileCheckSum
        {
            internal uint name;           // Index of name in name table.
            internal byte len;            // Hash length
            internal byte type;           // Hash type
        }

        private enum SYM
        {
            S_END = 0x0006,  // Block, procedure, "with" or thunk end
            S_OEM = 0x0404,  // OEM defined symbol
            S_REGISTER_ST = 0x1001,  // Register variable
            S_CONSTANT_ST = 0x1002,  // constant symbol
            S_UDT_ST = 0x1003,  // User defined type
            S_COBOLUDT_ST = 0x1004,  // special UDT for cobol that does not symbol pack
            S_MANYREG_ST = 0x1005,  // multiple register variable
            S_BPREL32_ST = 0x1006,  // BP-relative
            S_LDATA32_ST = 0x1007,  // Module-local symbol
            S_GDATA32_ST = 0x1008,  // Global data symbol
            S_PUB32_ST = 0x1009,  // a internal symbol (CV internal reserved)
            S_LPROC32_ST = 0x100a,  // Local procedure start
            S_GPROC32_ST = 0x100b,  // Global procedure start
            S_VFTABLE32 = 0x100c,  // address of virtual function table
            S_REGREL32_ST = 0x100d,  // register relative address
            S_LTHREAD32_ST = 0x100e,  // local thread storage
            S_GTHREAD32_ST = 0x100f,  // global thread storage
            S_LPROCMIPS_ST = 0x1010,  // Local procedure start
            S_GPROCMIPS_ST = 0x1011,  // Global procedure start
            S_FRAMEPROC = 0x1012,  // extra frame and proc information
            S_COMPILE2_ST = 0x1013,  // extended compile flags and info
            S_MANYREG2_ST = 0x1014,  // multiple register variable
            S_LPROCIA64_ST = 0x1015,  // Local procedure start (IA64)
            S_GPROCIA64_ST = 0x1016,  // Global procedure start (IA64)
            S_LOCALSLOT_ST = 0x1017,  // local IL sym with field for local slot index
            S_PARAMSLOT_ST = 0x1018,  // local IL sym with field for parameter slot index
            S_ANNOTATION = 0x1019,  // Annotation string literals
            S_GMANPROC_ST = 0x101a,  // Global proc
            S_LMANPROC_ST = 0x101b,  // Local proc
            S_RESERVED1 = 0x101c,  // reserved
            S_RESERVED2 = 0x101d,  // reserved
            S_RESERVED3 = 0x101e,  // reserved
            S_RESERVED4 = 0x101f,  // reserved
            S_LMANDATA_ST = 0x1020,
            S_GMANDATA_ST = 0x1021,
            S_MANFRAMEREL_ST = 0x1022,
            S_MANREGISTER_ST = 0x1023,
            S_MANSLOT_ST = 0x1024,
            S_MANMANYREG_ST = 0x1025,
            S_MANREGREL_ST = 0x1026,
            S_MANMANYREG2_ST = 0x1027,
            S_MANTYPREF = 0x1028,  // Index for type referenced by name from metadata
            S_UNAMESPACE_ST = 0x1029,  // Using namespace
            S_ST_MAX = 0x1100,  // starting point for SZ name symbols
            S_OBJNAME = 0x1101,  // path to object file name
            S_THUNK32 = 0x1102,  // Thunk Start
            S_BLOCK32 = 0x1103,  // block start
            S_WITH32 = 0x1104,  // with start
            S_LABEL32 = 0x1105,  // code label
            S_REGISTER = 0x1106,  // Register variable
            S_CONSTANT = 0x1107,  // constant symbol
            S_UDT = 0x1108,  // User defined type
            S_COBOLUDT = 0x1109,  // special UDT for cobol that does not symbol pack
            S_MANYREG = 0x110a,  // multiple register variable
            S_BPREL32 = 0x110b,  // BP-relative
            S_LDATA32 = 0x110c,  // Module-local symbol
            S_GDATA32 = 0x110d,  // Global data symbol
            S_PUB32 = 0x110e,  // a internal symbol (CV internal reserved)
            S_LPROC32 = 0x110f,  // Local procedure start
            S_GPROC32 = 0x1110,  // Global procedure start
            S_REGREL32 = 0x1111,  // register relative address
            S_LTHREAD32 = 0x1112,  // local thread storage
            S_GTHREAD32 = 0x1113,  // global thread storage
            S_LPROCMIPS = 0x1114,  // Local procedure start
            S_GPROCMIPS = 0x1115,  // Global procedure start
            S_COMPILE2 = 0x1116,  // extended compile flags and info
            S_MANYREG2 = 0x1117,  // multiple register variable
            S_LPROCIA64 = 0x1118,  // Local procedure start (IA64)
            S_GPROCIA64 = 0x1119,  // Global procedure start (IA64)
            S_LOCALSLOT = 0x111a,  // local IL sym with field for local slot index
            S_SLOT = S_LOCALSLOT,  // alias for LOCALSLOT
            S_PARAMSLOT = 0x111b,  // local IL sym with field for parameter slot index
            S_LMANDATA = 0x111c,
            S_GMANDATA = 0x111d,
            S_MANFRAMEREL = 0x111e,
            S_MANREGISTER = 0x111f,
            S_MANSLOT = 0x1120,
            S_MANMANYREG = 0x1121,
            S_MANREGREL = 0x1122,
            S_MANMANYREG2 = 0x1123,
            S_UNAMESPACE = 0x1124,  // Using namespace
            S_PROCREF = 0x1125,  // Reference to a procedure
            S_DATAREF = 0x1126,  // Reference to data
            S_LPROCREF = 0x1127,  // Local Reference to a procedure
            S_ANNOTATIONREF = 0x1128,  // Reference to an S_ANNOTATION symbol
            S_TOKENREF = 0x1129,  // Reference to one of the many MANPROCSYM's
            S_GMANPROC = 0x112a,  // Global proc
            S_LMANPROC = 0x112b,  // Local proc
            S_TRAMPOLINE = 0x112c,  // trampoline thunks
            S_MANCONSTANT = 0x112d,  // constants with metadata type info
            S_ATTR_FRAMEREL = 0x112e,  // relative to virtual frame ptr
            S_ATTR_REGISTER = 0x112f,  // stored in a register
            S_ATTR_REGREL = 0x1130,  // relative to register (alternate frame ptr)
            S_ATTR_MANYREG = 0x1131,  // stored in >1 register
            S_SEPCODE = 0x1132,
            S_LOCAL = 0x1133,  // defines a local symbol in optimized code
            S_DEFRANGE = 0x1134,  // defines a single range of addresses in which symbol can be evaluated
            S_DEFRANGE2 = 0x1135,  // defines ranges of addresses in which symbol can be evaluated
            S_SECTION = 0x1136,  // A COFF section in a PE executable
            S_COFFGROUP = 0x1137,  // A COFF group
            S_EXPORT = 0x1138,  // A export
            S_CALLSITEINFO = 0x1139,  // Indirect call site information
            S_FRAMECOOKIE = 0x113a,  // Security cookie information
            S_DISCARDED = 0x113b,  // Discarded by LINK /OPT:REF (experimental, see richards)
            S_RECTYPE_MAX,              // one greater than last
            S_RECTYPE_LAST = S_RECTYPE_MAX - 1,
        };

        private enum DEBUG_S_SUBSECTION
        {
            SYMBOLS = 0xF1,
            LINES = 0xF2,
            STRINGTABLE = 0xF3,
            FILECHKSMS = 0xF4,
            FRAMEDATA = 0xF5,
        }

        private struct OemSymbol
        {
            internal Guid idOem;      // an oem ID (GUID)
            internal uint typind;     // (type index) Type index
            //internal byte[] rgl;        // user data, force 4-byte alignment
        };

        private Token2SourceLineExporter()
        {
        }

        private static readonly XmlWriterSettings s_xmlWriterSettings = new XmlWriterSettings
        {
            Encoding = Encoding.UTF8,
            Indent = true,
            IndentChars = "  ",
            NewLineChars = "\r\n",
        };

        public static string TokenToSourceMap2Xml(Stream read, bool maskToken = false)
        {
            var builder = new StringBuilder();

            using (var writer = XmlWriter.Create(builder, s_xmlWriterSettings))
            {
                writer.WriteStartElement("token-map");

                List<PdbTokenLine> list = new List<PdbTokenLine>(LoadTokenToSourceMapping(read).Values);
                list.Sort(
                    (x, y) =>
                    {
                        int result = x.line.CompareTo(y.line);
                        if (result != 0)
                            return result;
                        result = x.column.CompareTo(y.column);
                        if (result != 0)
                            return result;
                        result = x.endLine.CompareTo(y.endLine);
                        if (result != 0)
                            return result;
                        result = x.endColumn.CompareTo(y.endColumn);
                        if (result != 0)
                            return result;
                        return x.token.CompareTo(y.token);
                    });

                foreach (var rec in list)
                {
                    writer.WriteStartElement("token-location");

                    writer.WriteAttributeString("token", Token2String(rec.token, maskToken));
                    writer.WriteAttributeString("file", rec.sourceFile.name);
                    writer.WriteAttributeString("start-line", rec.line.ToString());
                    writer.WriteAttributeString("start-column", rec.column.ToString());
                    writer.WriteAttributeString("end-line", rec.endLine.ToString());
                    writer.WriteAttributeString("end-column", rec.endColumn.ToString());

                    writer.WriteEndElement(); // "token-location";
                }

                writer.WriteEndElement(); // "token-map";
            }

            return builder.ToString();
        }

        private static string Token2String(uint token, bool maskToken)
        {
            string result = token.ToString("X8");
            if (maskToken)
                result = result[..2] + "xxxxxx";
            return "0x" + result;
        }

        private static Dictionary<uint, PdbTokenLine> LoadTokenToSourceMapping(Stream read)
        {
            var tokenToSourceMapping = new Dictionary<uint, PdbTokenLine>();
            BitAccess bits = new BitAccess(512 * 1024);
            PdbFileHeader head = new PdbFileHeader(read, bits);
            PdbReader reader = new PdbReader(read, head.pageSize);
            MsfDirectory dir = new MsfDirectory(reader, head, bits);

            dir.streams[1].Read(reader, bits);
            Dictionary<string, int> nameIndex = LoadNameIndex(bits);
            if (!nameIndex.TryGetValue("/NAMES", out var nameStream))
            {
                throw new Exception("No `name' stream");
            }

            dir.streams[nameStream].Read(reader, bits);
            IntHashTable names = LoadNameStream(bits);

            dir.streams[3].Read(reader, bits);
            LoadDbiStream(bits, out var modules, out var header, true);

            if (modules != null)
            {
                for (int m = 0; m < modules.Length; m++)
                {
                    var module = modules[m];
                    if (module.stream > 0)
                    {
                        dir.streams[module.stream].Read(reader, bits);
                        if (module.moduleName == "TokenSourceLineInfo")
                        {
                            LoadTokenToSourceInfo(bits, module, names, tokenToSourceMapping);
                        }
                    }
                }
            }

            return tokenToSourceMapping;
        }

        private static Dictionary<string, int> LoadNameIndex(BitAccess bits)
        {
            Dictionary<string, int> result = [];
            bits.ReadInt32(out var ver);    //  0..3  Version
            bits.ReadInt32(out var sig);    //  4..7  Signature
            bits.ReadInt32(out var age);    //  8..11 Age
            bits.ReadGuid(out var guid);       // 12..27 GUID

            // Read string buffer.
            bits.ReadInt32(out var buf);    // 28..31 Bytes of Strings

            int beg = bits.Position;
            int nxt = bits.Position + buf;

            bits.Position = nxt;
            // n+4..7 maximum ni.

            bits.ReadInt32(out var cnt);
            bits.ReadInt32(out var max);

            BitSet present = new BitSet(bits);
            BitSet deleted = new BitSet(bits);
            if (!deleted.IsEmpty)
            {
                throw new Exception("Unsupported PDB deleted bitset is not empty.");
            }

            int j = 0;
            for (int i = 0; i < max; i++)
            {
                if (present.IsSet(i))
                {
                    bits.ReadInt32(out var ns);
                    bits.ReadInt32(out var ni);

                    int saved = bits.Position;
                    bits.Position = beg + ns;
                    bits.ReadCString(out var name);
                    bits.Position = saved;

                    result.Add(name.ToUpperInvariant(), ni);
                    j++;
                }
            }
            if (j != cnt)
            {
                throw new Exception(string.Format("Count mismatch. ({0} != {1})", j, cnt));
            }
            return result;
        }

        private static readonly Guid s_msilMetaData =
            new Guid(unchecked((int)0xc6ea3fc9), 0x59b3, 0x49d6, 0xbc, 0x25, 0x09, 0x02, 0xbb, 0xab, 0xb4, 0x60);

        private static void LoadTokenToSourceInfo(
            BitAccess bits, DbiModuleInfo module, IntHashTable names, Dictionary<uint, PdbTokenLine> tokenToSourceMapping)
        {
            bits.Position = 0;
            bits.ReadInt32(out var sig);
            if (sig != 4)
            {
                throw new Exception(string.Format("Invalid signature. (sig={0})", sig));
            }

            bits.Position = 4;

            while (bits.Position < module.cbSyms)
            {

                bits.ReadUInt16(out var siz);
                int star = bits.Position;
                int stop = bits.Position + siz;
                bits.Position = star;
                bits.ReadUInt16(out var rec);

                switch ((SYM)rec)
                {
                    case SYM.S_OEM:
                        OemSymbol oem;

                        bits.ReadGuid(out oem.idOem);
                        bits.ReadUInt32(out oem.typind);
                        // internal byte[]   rgl;        // user data, force 4-byte alignment

                        if (oem.idOem == s_msilMetaData)
                        {
                            string name = bits.ReadString();
                            if (name == "TSLI")
                            {
                                bits.ReadUInt32(out var token);
                                bits.ReadUInt32(out var file_id);
                                bits.ReadUInt32(out var line);
                                bits.ReadUInt32(out var column);
                                bits.ReadUInt32(out var endLine);
                                bits.ReadUInt32(out var endColumn);
                                if (!tokenToSourceMapping.TryGetValue(token, out var tokenLine))
                                    tokenToSourceMapping.Add(token, new PdbTokenLine(token, file_id, line, column, endLine, endColumn));
                                else
                                {
                                    while (tokenLine.nextLine != null)
                                        tokenLine = tokenLine.nextLine;
                                    tokenLine.nextLine = new PdbTokenLine(token, file_id, line, column, endLine, endColumn);
                                }
                            }
                            bits.Position = stop;
                            break;
                        }
                        else
                        {
                            throw new Exception(string.Format("OEM section: guid={0} ti={1}", oem.idOem, oem.typind));
                        }

                    case SYM.S_END:
                        bits.Position = stop;
                        break;

                    default:
                        bits.Position = stop;
                        break;
                }
            }

            bits.Position = module.cbSyms + module.cbOldLines;
            int limit = module.cbSyms + module.cbOldLines + module.cbLines;
            IntHashTable sourceFiles = ReadSourceFileInfo(bits, (uint)limit, names);
            foreach (var tokenLine in tokenToSourceMapping.Values)
            {
                tokenLine.sourceFile = (PdbSource)sourceFiles[(int)tokenLine.file_id];
            }
        }

        private static readonly Guid s_symDocumentTypeGuid = new Guid("{5a869d0b-6611-11d3-bd2a-0000f80849bd}");

        private static IntHashTable ReadSourceFileInfo(BitAccess bits, uint limit, IntHashTable names)
        {
            IntHashTable checks = new IntHashTable();

            int begin = bits.Position;
            while (bits.Position < limit)
            {
                bits.ReadInt32(out var sig);
                bits.ReadInt32(out var siz);
                int place = bits.Position;
                int endSym = bits.Position + siz;

                switch ((DEBUG_S_SUBSECTION)sig)
                {
                    case DEBUG_S_SUBSECTION.FILECHKSMS:
                        while (bits.Position < endSym)
                        {
                            CV_FileCheckSum chk;

                            int ni = bits.Position - place;
                            bits.ReadUInt32(out chk.name);
                            bits.ReadUInt8(out chk.len);
                            bits.ReadUInt8(out chk.type);
                            PdbSource src = new PdbSource(/*(uint)ni,*/ (string)names[(int)chk.name], s_symDocumentTypeGuid, Guid.Empty, Guid.Empty);
                            checks.Add(ni, src);
                            bits.Position += chk.len;
                            bits.Align(4);
                        }
                        bits.Position = endSym;
                        break;

                    default:
                        bits.Position = endSym;
                        break;
                }
            }
            return checks;
        }

        private static IntHashTable LoadNameStream(BitAccess bits)
        {
            IntHashTable ht = new IntHashTable();
            bits.ReadUInt32(out var sig);   //  0..3  Signature
            bits.ReadInt32(out var ver);    //  4..7  Version

            // Read (or skip) string buffer.
            bits.ReadInt32(out var buf);    //  8..11 Bytes of Strings

            if (sig != 0xeffeeffe || ver != 1)
            {
                throw new Exception(string.Format("Unsupported Name Stream version. (sig={0:x8}, ver={1})", sig, ver));
            }
            int beg = bits.Position;
            int nxt = bits.Position + buf;
            bits.Position = nxt;

            // Read hash table.
            bits.ReadInt32(out var siz);    // n+0..3 Number of hash buckets.
            nxt = bits.Position;

            for (int i = 0; i < siz; i++)
            {

                bits.ReadInt32(out var ni);

                if (ni != 0)
                {
                    int saved = bits.Position;
                    bits.Position = beg + ni;
                    bits.ReadCString(out var name);
                    bits.Position = saved;

                    ht.Add(ni, name);
                }
            }
            bits.Position = nxt;

            return ht;
        }

        private static void LoadDbiStream(BitAccess bits, out DbiModuleInfo[] modules, out DbiDbgHdr header, bool readStrings)
        {
            DbiHeader dh = new DbiHeader(bits);
            header = new DbiDbgHdr();

            // Read gpmod section.
            var modList = new List<DbiModuleInfo>();
            int end = bits.Position + dh.gpmodiSize;
            while (bits.Position < end)
            {
                DbiModuleInfo mod = new DbiModuleInfo(bits, readStrings);
                modList.Add(mod);
            }
            if (bits.Position != end)
            {
                throw new Exception(string.Format("Error reading DBI stream, pos={0} != {1}", bits.Position, end));
            }

            if (modList.Count > 0)
            {
                modules = modList.ToArray();
            }
            else
            {
                modules = null;
            }

            // Skip the Section Contribution substream.
            bits.Position += dh.secconSize;

            // Skip the Section Map substream.
            bits.Position += dh.secmapSize;

            // Skip the File Info substream.
            bits.Position += dh.filinfSize;

            // Skip the TSM substream.
            bits.Position += dh.tsmapSize;

            // Skip the EC substream.
            bits.Position += dh.ecinfoSize;

            // Read the optional header.
            end = bits.Position + dh.dbghdrSize;
            if (dh.dbghdrSize > 0)
            {
                header = new DbiDbgHdr(bits);
            }
            bits.Position = end;
        }
    }
}
