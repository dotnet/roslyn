// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections;
using System.Collections.Generic;

namespace Microsoft.CodeAnalysis.Shared.Utilities
{
    internal partial class BloomFilter
    {
        private readonly BitArray _bitArray;
        private readonly int _hashFunctionCount;
        private readonly bool _isCaseSensitive;

        /// <summary><![CDATA[
        /// 1) n  = Number of items in the filter
        /// 
        /// 2) p = Probability of false positives, (a double between 0 and 1).
        /// 
        /// 3) m = Number of bits in the filter
        /// 
        /// 4) k = Number of hash functions
        /// 
        /// m = ceil((n * log(p)) / log(1.0 / (pow(2.0, log(2.0)))))
        /// 
        /// k = round(log(2.0) * m / n)
        /// ]]></summary>
        public BloomFilter(int expectedCount, double falsePositiveProbability, bool isCaseSensitive)
        {
            int m = Math.Max(1, ComputeM(expectedCount, falsePositiveProbability));
            int k = Math.Max(1, ComputeK(expectedCount, falsePositiveProbability));

            // We must have size in even bytes, so that when we deserialize from bytes we get a bit array with the same count.
            // The count is used by the hash functions.
            int sizeInEvenBytes = (m + 7) & ~7;

            _bitArray = new BitArray(length: sizeInEvenBytes);
            _hashFunctionCount = k;
            _isCaseSensitive = isCaseSensitive;
        }

        public BloomFilter(double falsePositiveProbability, bool isCaseSensitive, ICollection<string> values)
            : this(values.Count, falsePositiveProbability, isCaseSensitive)
        {
            this.AddRange(values);
        }

        private BloomFilter(BitArray bitArray, int hashFunctionCount, bool isCaseSensitive)
        {
            if (bitArray == null)
            {
                throw new ArgumentNullException(nameof(bitArray));
            }

            _bitArray = bitArray;
            _hashFunctionCount = hashFunctionCount;
            _isCaseSensitive = isCaseSensitive;
        }

        // m = ceil((n * log(p)) / log(1.0 / (pow(2.0, log(2.0)))))
        private static int ComputeM(int expectedCount, double falsePositiveProbability)
        {
            var p = falsePositiveProbability;
            double n = expectedCount;

            var numerator = n * Math.Log(p);
            var denominator = Math.Log(1.0 / Math.Pow(2.0, Math.Log(2.0)));
            return unchecked((int)Math.Ceiling(numerator / denominator));
        }

        // k = round(log(2.0) * m / n)
        private static int ComputeK(int expectedCount, double falsePositiveProbability)
        {
            double n = expectedCount;
            double m = ComputeM(expectedCount, falsePositiveProbability);

            var temp = Math.Log(2.0) * m / n;
            return unchecked((int)Math.Round(temp));
        }

        /// <summary>
        /// Modification of the murmurhash2 algorithm.  Code is simpler because it operates over
        /// strings instead of byte arrays.  Because each string character is two bytes, it is known
        /// that the input will be an even number of bytes (though not necessarily a multiple of 4).
        /// 
        /// This is needed over the normal 'string.GetHashCode()' because we need to be able to generate
        /// 'k' different well distributed hashes for any given string s.  Also, we want to be able to
        /// generate these hashes without allocating any memory.  My ideal solution would be to use an
        /// MD5 hash.  However, there appears to be no way to do MD5 in .Net where you can:
        /// 
        /// a) feed it individual values instead of a byte[]
        /// 
        /// b) have the hash computed into a byte[] you provide instead of a newly allocated one
        /// 
        /// Generating 'k' pieces of garbage on each insert and lookup seems very wasteful.  So,
        /// instead, we use murmur hash since it provides well distributed values, allows for a
        /// seed, and allocates no memory.
        /// 
        /// Murmur hash is public domain.  Actual code is included below as reference.
        /// </summary>
        private int ComputeHash(string key, int seed)
        {
            unchecked
            {
                // 'm' and 'r' are mixing constants generated offline.
                // The values for m and r are chosen through experimentation and 
                // supported by evidence that they work well.

                const uint m = 0x5bd1e995;
                const int r = 24;

                // Initialize the hash to a 'random' value

                var numberOfCharsLeft = key.Length;
                var h = (uint)(seed ^ numberOfCharsLeft);

                // Mix 4 bytes at a time into the hash.  NOTE: 4 bytes is two chars, so we iterate
                // through the string two chars at a time.
                var index = 0;
                while (numberOfCharsLeft >= 2)
                {
                    var c1 = GetCharacter(key, index);
                    var c2 = GetCharacter(key, index + 1);

                    var k = c1 | (c2 << 16);

                    k *= m;
                    k ^= k >> r;
                    k *= m;

                    h *= m;
                    h ^= k;

                    index += 2;
                    numberOfCharsLeft -= 2;
                }

                // Handle the last char (or 2 bytes) if they exist.  This happens if the original string had
                // odd length.
                if (numberOfCharsLeft == 1)
                {
                    h ^= GetCharacter(key, index);
                    h *= m;
                }

                // Do a few final mixes of the hash to ensure the last few bytes are well-incorporated.

                h ^= h >> 13;
                h *= m;
                h ^= h >> 15;

                return (int)h;
            }
        }

        private uint GetCharacter(string key, int index)
        {
            var c = key[index];
            return _isCaseSensitive ? c : char.ToLowerInvariant(c);
        }

#if false
        //-----------------------------------------------------------------------------
        // MurmurHash2, by Austin Appleby
        //
        // Note - This code makes a few assumptions about how your machine behaves -
        // 1. We can read a 4-byte value from any address without crashing
        // 2. sizeof(int) == 4
        //
        // And it has a few limitations -
        // 1. It will not work incrementally.
        // 2. It will not produce the same results on little-endian and big-endian
        //    machines.
        unsigned int MurmurHash2(const void* key, int len, unsigned int seed)
        {
            // 'm' and 'r' are mixing constants generated offline.
            // The values for m and r are chosen through experimentation and 
            // supported by evidence that they work well.
            
            const unsigned int m = 0x5bd1e995;
            const int r = 24;

            // Initialize the hash to a 'random' value
            unsigned int h = seed ^ len;

            // Mix 4 bytes at a time into the hash
            const unsigned char* data = (const unsigned char*)key;

            while(len >= 4)
            {
                unsigned int k = *(unsigned int*)data;

                k *= m; 
                k ^= k >> r; 
                k *= m; 

                h *= m; 
                h ^= k;

                data += 4;
                len -= 4;
            }
    
            // Handle the last few bytes of the input array
            switch(len)
            {
                case 3: h ^= data[2] << 16;
                case 2: h ^= data[1] << 8;
                case 1: h ^= data[0];
                        h *= m;
            };

            // Do a few final mixes of the hash to ensure the last few
            // bytes are well-incorporated.

            h ^= h >> 13;
            h *= m;
            h ^= h >> 15;

            return h;
        } 
#endif

        public void AddRange(IEnumerable<string> values)
        {
            foreach (var v in values)
            {
                this.Add(v);
            }
        }

        public void Add(string value)
        {
            for (var i = 0; i < _hashFunctionCount; i++)
            {
                var hash = ComputeHash(value, i);
                hash = hash % _bitArray.Length;
                _bitArray[Math.Abs(hash)] = true;
            }
        }

        public bool ProbablyContains(string value)
        {
            for (var i = 0; i < _hashFunctionCount; i++)
            {
                var hash = ComputeHash(value, i);
                hash = hash % _bitArray.Length;
                if (!_bitArray[Math.Abs(hash)])
                {
                    return false;
                }
            }

            return true;
        }

        public bool IsEquivalent(BloomFilter filter)
        {
            return IsEquivalent(_bitArray, filter._bitArray)
                && _hashFunctionCount == filter._hashFunctionCount
                && _isCaseSensitive == filter._isCaseSensitive;
        }

        private bool IsEquivalent(BitArray array1, BitArray array2)
        {
            if (array1.Length != array2.Length)
            {
                return false;
            }

            for (int i = 0; i < array1.Length; i++)
            {
                if (array1[i] != array2[i])
                {
                    return false;
                }
            }

            return true;
        }
    }
}
