// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;
using Word = System.UInt32;

namespace Microsoft.CodeAnalysis
{
    internal struct BitArray : IEquatable<BitArray>
    {
        // Cannot expose the following two field publicly because this structure is mutable
        // and might become not null/empty, unless we restrict access to it.
        private static Word[] emptyArray = SpecializedCollections.EmptyArray<Word>();
        private static readonly BitArray nullValue = new BitArray(0, null, 0);
        private static readonly BitArray emptyValue = new BitArray(0, emptyArray, 0);

        private const int Log2BitsPerWord = 5;
        internal const int BitsPerWord = 1 << Log2BitsPerWord;
        private const Word ZeroWord = 0;

        private Word bits0;
        private Word[] bits;
        private int capacity;

        private BitArray(Word bits0, Word[] bits, int capacity)
        {
            int requiredWords = WordsForCapacity(capacity);
            Debug.Assert(requiredWords == 0 || requiredWords <= bits.Length);
            this.bits0 = bits0;
            this.bits = bits;
            this.capacity = capacity;
            Check();
        }

        public bool Equals(BitArray other)
        {
            // Bit arrays only equal if their underlying sets are of the same size.
            return this.capacity == other.capacity
                && this.bits0 == other.bits0
                && this.bits.ValueEquals(other.bits);
        }

        public override bool Equals(object obj)
        {
            return obj is BitArray && Equals((BitArray)obj);
        }

        public override int GetHashCode()
        {
            int bitsHash = bits0.GetHashCode();

            if (bits != null)
            {
                for (int i = 0; i < bits.Length; i++)
                {
                    bitsHash = Hash.Combine(bits[i].GetHashCode(), bitsHash);
                }
            }

            return Hash.Combine(this.capacity, bitsHash);
        }

        private static int WordsForCapacity(int capacity)
        {
            if (capacity <= 0) return 0;
            int lastIndex = (capacity - 1) >> Log2BitsPerWord;
            return lastIndex;
        }

        public int Capacity
        {
            get
            {
                return capacity;
            }
        }

        [Conditional("DEBUG_BITARRAY")]
        private void Check()
        {
            Debug.Assert(this.capacity == 0 || WordsForCapacity(this.capacity) <= this.bits.Length);
        }

        public void EnsureCapacity(int newCapacity)
        {
            if (newCapacity > capacity)
            {
                int requiredWords = WordsForCapacity(newCapacity);
                if (requiredWords > bits.Length) Array.Resize(ref this.bits, requiredWords);
                this.capacity = newCapacity;
                Check();
            }
            Check();
        }

        internal IEnumerable<Word> Words()
        {
            if (bits0 != 0)
            {
                yield return bits0;
            }

            for (int i = 0; i < bits.Length; i++)
            {
                yield return bits[i];
            }
        }

        public IEnumerable<int> TrueBits()
        {
            Check();
            if (bits0 != 0)
            {
                for (int bit = 0; bit < BitsPerWord; bit++)
                {
                    Word mask = ((Word)1) << bit;
                    if ((bits0 & mask) != 0)
                    {
                        if (bit >= capacity) yield break;
                        yield return bit;
                    }
                }
            }
            for (int i = 0; i < bits.Length; i++)
            {
                Word w = bits[i];
                if (w != 0)
                {
                    for (int b = 0; b < BitsPerWord; b++)
                    {
                        Word mask = ((Word)1) << b;
                        if ((w & mask) != 0)
                        {
                            int bit = ((i + 1) << Log2BitsPerWord) | b;
                            if (bit >= capacity) yield break;
                            yield return bit;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Create BitArray with at least the specified number of bits.
        /// </summary>
        public static BitArray Create(int capacity)
        {
            int requiredWords = WordsForCapacity(capacity);
            Word[] bits = (requiredWords == 0) ? emptyArray : new Word[requiredWords];
            return new BitArray(0, bits, capacity);
        }

        /// <summary>
        /// return a bit array with all bits set from index 0 through bitCount-1
        /// </summary>
        /// <param name="capacity"></param>
        /// <returns></returns>
        public static BitArray AllSet(int capacity)
        {
            int requiredWords = WordsForCapacity(capacity);
            Word[] bits = (requiredWords == 0) ? emptyArray : new Word[requiredWords];
            int lastWord = requiredWords - 1;
            Word bits0 = ~ZeroWord;
            for (int j = 0; j < lastWord; j++)
                bits[j] = ~ZeroWord;
            int numTrailingBits = capacity & ((BitsPerWord) - 1);
            if (numTrailingBits > 0)
            {
                Debug.Assert(numTrailingBits <= BitsPerWord);
                Word lastBits = ~((~ZeroWord) << numTrailingBits);
                if (lastWord < 0)
                {
                    bits0 = lastBits;
                }
                else
                {
                    bits[lastWord] = lastBits;
                }
            }
            else if (requiredWords > 0)
            {
                bits[lastWord] = ~ZeroWord;
            }

            return new BitArray(bits0, bits, capacity);
        }

        /// <summary>
        /// Maky a copy of a bit array.
        /// </summary>
        /// <returns></returns>
        public BitArray Clone()
        {
            return new BitArray(this.bits0, (this.bits == null) ? null : (this.bits.Length == 0) ? emptyArray : (Word[])this.bits.Clone(), capacity);
        }

        /// <summary>
        /// Is the given bit array null?
        /// </summary>
        public bool IsNull
        {
            get
            {
                return bits == null;
            }
        }

        public static BitArray Null
        {
            get
            {
                return nullValue;
            }
        }

        public static BitArray Empty
        {
            get
            {
                return emptyValue;
            }
        }

        /// <summary>
        /// Modify this bit vector by bitwise AND-ing each element with the other bit vector.
        /// For the purposes of the intersection, any bits beyond the current length will be treated as zeroes.
        /// Return true if any changes were made to the bits of this bit vector.
        /// </summary>
        public bool IntersectWith(BitArray other)
        {
            bool anyChanged = false;
            int otherLength = other.bits.Length;
            var thisBits = this.bits;
            int thisLength = thisBits.Length;

            if (otherLength > thisLength)
                otherLength = thisLength;

            // intersect the inline portion
            {
                var oldV = this.bits0;
                var newV = oldV & other.bits0;
                if (newV != oldV)
                {
                    this.bits0 = newV;
                    anyChanged = true;
                }
            }
            // intersect up to their common length.
            for (int i = 0; i < otherLength; i++)
            {
                var oldV = thisBits[i];
                var newV = oldV & other.bits[i];
                if (newV != oldV)
                {
                    thisBits[i] = newV;
                    anyChanged = true;
                }
            }

            // treat the other bit array as being extended with zeroes
            for (int i = otherLength; i < thisLength; i++)
            {
                if (thisBits[i] != 0)
                {
                    thisBits[i] = 0;
                    anyChanged = true;
                }
            }

            Check();
            return anyChanged;
        }

        /// <summary>
        /// Modify this bit vector by '|'ing each element with the other bit vector.
        /// </summary>
        /// <param name="other"></param>
        public void UnionWith(BitArray other)
        {
            int l = other.bits.Length;
            if (l > this.bits.Length)
                Array.Resize(ref bits, l + 1);
            this.bits0 |= other.bits0;
            for (int i = 0; i < l; i++)
                this.bits[i] |= other.bits[i];
            if (other.capacity > this.capacity)
                EnsureCapacity(other.capacity);
            Check();
        }

        public bool this[int index]
        {
            get
            {
                if (index >= capacity)
                    return false;
                int i = (index >> Log2BitsPerWord) - 1;
                int b = index & (BitsPerWord - 1);
                Word mask = ((Word)1) << b;
                var word = (i < 0) ? bits0 : bits[i];
                return (word & mask) != 0;
            }

            set
            {
                if (index >= capacity)
                    EnsureCapacity(index + 1);
                int i = (index >> Log2BitsPerWord) - 1;
                int b = index & (BitsPerWord - 1);
                Word mask = ((Word)1) << b;
                if (i < 0)
                {
                    if (value)
                        bits0 |= mask;
                    else
                        bits0 &= ~mask;
                }
                else
                {
                    if (value)
                        bits[i] |= mask;
                    else
                        bits[i] &= ~mask;
                }
            }
        }

        public void Clear()
        {
            bits0 = 0;
            if (bits != null) Array.Clear(bits, 0, bits.Length);
        }
    }
}