// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;

namespace Microsoft.CodeAnalysis.Shared.Utilities;

internal partial class BloomFilter
{
    // From MurmurHash:
    // 'm' and 'r' are mixing constants generated off-line.
    // The values for m and r are chosen through experimentation and 
    // supported by evidence that they work well.
    private const uint Compute_Hash_m = 0x5bd1e995;
    private const int Compute_Hash_r = 24;

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
    private BloomFilter(int expectedCount, double falsePositiveProbability, bool isCaseSensitive)
    {
        var m = Math.Max(1, ComputeM(expectedCount, falsePositiveProbability));
        var k = Math.Max(1, ComputeK(expectedCount, falsePositiveProbability));

        // We must have size in even bytes, so that when we deserialize from bytes we get a bit array with the same count.
        // The count is used by the hash functions.
        var sizeInEvenBytes = (m + 7) & ~7;

        _bitArray = new BitArray(length: sizeInEvenBytes);
        _hashFunctionCount = k;
        _isCaseSensitive = isCaseSensitive;
    }

    public BloomFilter(double falsePositiveProbability, bool isCaseSensitive, HashSet<string> values)
        : this(values.Count, falsePositiveProbability, isCaseSensitive)
    {
        AddRange(values);
    }

    public BloomFilter(
        double falsePositiveProbability,
        HashSet<string> stringValues,
        HashSet<long> longValues)
        : this(stringValues.Count + longValues.Count, falsePositiveProbability, isCaseSensitive: false)
    {
        AddRange(stringValues);
        AddRange(longValues);
    }

    private BloomFilter(BitArray bitArray, int hashFunctionCount, bool isCaseSensitive)
    {
        _bitArray = bitArray ?? throw new ArgumentNullException(nameof(bitArray));
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
    /// MD5 hash.  However, there appears to be no way to do MD5 in .NET where you can:
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
    private static int ComputeHash(string key, int seed, bool isCaseSensitive)
    {
        unchecked
        {
            // Initialize the hash to a 'random' value

            var numberOfCharsLeft = key.Length;
            var h = (uint)(seed ^ numberOfCharsLeft);

            // Mix 4 bytes at a time into the hash.  NOTE: 4 bytes is two chars, so we iterate
            // through the string two chars at a time.
            var index = 0;
            while (numberOfCharsLeft >= 2)
            {
                var c1 = GetCharacter(key, index, isCaseSensitive);
                var c2 = GetCharacter(key, index + 1, isCaseSensitive);

                h = CombineTwoCharacters(h, c1, c2);

                index += 2;
                numberOfCharsLeft -= 2;
            }

            // Handle the last char (or 2 bytes) if they exist.  This happens if the original string had
            // odd length.
            if (numberOfCharsLeft == 1)
            {
                var c = GetCharacter(key, index, isCaseSensitive);
                h = CombineLastCharacter(h, c);
            }

            // Do a few final mixes of the hash to ensure the last few bytes are well-incorporated.

            h = FinalMix(h);

            return (int)h;
        }
    }

    private static int ComputeHash(long key, int seed)
    {
        // This is a duplicate of ComputeHash(string key, int seed).  However, because
        // we only have 64bits to encode we just unroll that function here.  See
        // Other function for documentation on what's going on here.
        unchecked
        {
            // Initialize the hash to a 'random' value

            var numberOfCharsLeft = 4;
            var h = (uint)(seed ^ numberOfCharsLeft);

            // Mix 4 bytes at a time into the hash.  NOTE: 4 bytes is two chars, so we iterate
            // through the long two chars at a time.
            var index = 0;
            while (numberOfCharsLeft >= 2)
            {
                var c1 = GetCharacter(key, index);
                var c2 = GetCharacter(key, index + 1);

                h = CombineTwoCharacters(h, c1, c2);

                index += 2;
                numberOfCharsLeft -= 2;
            }

            Debug.Assert(numberOfCharsLeft == 0);

            // Do a few final mixes of the hash to ensure the last few bytes are well-incorporated.
            h = FinalMix(h);

            return (int)h;
        }
    }

    private static uint CombineLastCharacter(uint h, uint c)
    {
        unchecked
        {
            h ^= c;
            h *= Compute_Hash_m;
            return h;
        }
    }

    private static uint FinalMix(uint h)
    {
        unchecked
        {
            h ^= h >> 13;
            h *= Compute_Hash_m;
            h ^= h >> 15;
            return h;
        }
    }

    private static uint CombineTwoCharacters(uint h, uint c1, uint c2)
    {
        unchecked
        {
            var k = c1 | (c2 << 16);

            k *= Compute_Hash_m;
            k ^= k >> Compute_Hash_r;
            k *= Compute_Hash_m;

            h *= Compute_Hash_m;
            h ^= k;

            return h;
        }
    }

    private static char GetCharacter(string key, int index, bool isCaseSensitive)
    {
        var c = key[index];
        return isCaseSensitive ? c : char.ToLowerInvariant(c);
    }

    private static char GetCharacter(long key, int index)
    {
        Debug.Assert(index <= 3);
        return (char)(key >> (16 * index));
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

    private void AddRange(HashSet<string> values)
    {
        foreach (var v in values)
        {
            Add(v);
        }
    }

    private void AddRange(HashSet<long> values)
    {
        foreach (var v in values)
        {
            Add(v);
        }
    }

    public void Add(string value)
    {
        for (var i = 0; i < _hashFunctionCount; i++)
        {
            var hash = ComputeHash(value, i, _isCaseSensitive);
            _bitArray[GetBitArrayIndexFromHash(hash)] = true;
        }
    }

    private int GetBitArrayIndexFromHash(int hash)
    {
        hash %= _bitArray.Length;
        return Math.Abs(hash);
    }

    public void Add(long value)
    {
        for (var i = 0; i < _hashFunctionCount; i++)
        {
            var hash = ComputeHash(value, i);
            _bitArray[GetBitArrayIndexFromHash(hash)] = true;
        }
    }

    public bool ProbablyContains(string value)
    {
        // Request an array of immutable hashes for this input. Note that it's possible
        // that the returned array might return a cached entry calculated by a different
        // bloom filter and thus might have more entries than we need, but it's ok as
        // it's guaranteed that the first _hashFunctionCount of those values are the values
        // we would have computed had we not used the cache.
        var hashes = BloomFilterHash.GetOrCreateHashArray(value, _isCaseSensitive, _hashFunctionCount);

        for (var i = 0; i < _hashFunctionCount; i++)
        {
            var hash = hashes[i];
            if (!_bitArray[GetBitArrayIndexFromHash(hash)])
            {
                return false;
            }
        }

        return true;
    }

    public bool ProbablyContains(long value)
    {
        for (var i = 0; i < _hashFunctionCount; i++)
        {
            var hash = ComputeHash(value, i);
            if (!_bitArray[GetBitArrayIndexFromHash(hash)])
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

    private static bool IsEquivalent(BitArray array1, BitArray array2)
    {
        if (array1.Length != array2.Length)
        {
            return false;
        }

        for (var i = 0; i < array1.Length; i++)
        {
            if (array1[i] != array2[i])
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// Provides mechanism to efficiently obtain bloom filter hash for a value. Backed by a single element cache.
    /// </summary>
    internal sealed class BloomFilterHash
    {
        private static BloomFilterHash? s_cachedHash;

        private readonly string _value;
        private readonly bool _isCaseSensitive;
        private readonly ImmutableArray<int> _hashes;

        private BloomFilterHash(string value, bool isCaseSensitive, int hashFunctionCount)
        {
            _value = value;
            _isCaseSensitive = isCaseSensitive;

            var hashBuilder = new FixedSizeArrayBuilder<int>(hashFunctionCount);

            for (var i = 0; i < hashFunctionCount; i++)
                hashBuilder.Add(BloomFilter.ComputeHash(value, i, _isCaseSensitive));

            _hashes = hashBuilder.MoveToImmutable();
        }

        /// <summary>
        /// Although calculating this hash isn't terribly expensive, it does involve multiple
        /// (usually around 13) hashings of the string (the actual count is <see cref="BloomFilter._hashFunctionCount"/>).
        /// The typical usage pattern of bloom filters is that some operation (eg: find references)
        /// requires asking a multitude of bloom filters whether a particular value is likely contained.
        /// The vast majority of those bloom filters will end up hashing that string to the same values, so
        /// we put those values into a simple cache and see if it can be used before calculating.
        /// Local testing has put the hit rate of this at around 99%.
        ///
        /// Note that it's possible for this method to return an array from the cache longer than hashFunctionCount,
        /// but if so, it's guaranteed that the values returned in the first hashFunctionCount entries are
        /// the same as if the cache hadn't been used.
        /// </summary>
        public static ImmutableArray<int> GetOrCreateHashArray(string value, bool isCaseSensitive, int hashFunctionCount)
        {
            var cachedHash = s_cachedHash;

            // Not an equivalency check on the hashFunctionCount as a longer array is ok. This is because the
            // values in the array are determined by value and isCaseSensitive and hashFunctionCount is simply
            // used to determine the length of the returned array. As long as the cached entry matches the value
            // and isCaseSensitive and is at least as long as we need, then we can use it.
            if (cachedHash == null
                || cachedHash._isCaseSensitive != isCaseSensitive
                || cachedHash._hashes.Length < hashFunctionCount
                || cachedHash._value != value)
            {
                cachedHash = new BloomFilterHash(value, isCaseSensitive, hashFunctionCount);
                s_cachedHash = cachedHash;
            }

            return cachedHash._hashes;
        }

        // Used only by tests
        internal static bool TryGetCachedEntry(out bool isCaseSensitive, out string value)
        {
            var cachedHash = s_cachedHash;

            if (cachedHash == null)
            {
                isCaseSensitive = false;
                value = string.Empty;

                return false;
            }

            isCaseSensitive = cachedHash._isCaseSensitive;
            value = cachedHash._value;

            return true;
        }

        internal static void ResetCachedEntry()
        {
            s_cachedHash = null;
        }
    }
}
