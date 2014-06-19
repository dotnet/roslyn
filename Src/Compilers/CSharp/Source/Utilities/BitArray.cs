using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;

namespace Roslyn.Compilers.CSharp
{
    struct BitArray
    {
        int[] bits;
        public static BitArray Null = new BitArray(null);
        public static BitArray Empty = new BitArray(new int[0]);
        private BitArray(int[] bits)
        {
            this.bits = bits;
        }
        public static BitArray AllSet(int next)
        {
            int i = next >> 5;
            int b = next & 31;
            int mask = 1 << b;

            int[] bits = new int[i + 1];
            for (int j = 0; j < i; j++)
                bits[j] = -1;
            bits[i] = mask - 1;

            return new BitArray(bits);
        }
        public BitArray Clone()
        {
            return new BitArray((this.bits == null) ? null : (int[])this.bits.Clone());
        }
        public bool IsNull
        {
            get
            {
                return bits == null;
            }
        }
        public bool IntersectWith(BitArray other)
        {
            bool anyChanged = false;
            int l = other.bits.Length;
            if (l > this.bits.Length)
                Array.Resize(ref bits, l + 1);
            for (int i = 0; i < l; i++)
            {
                var thisBits = this.bits;
                var oldV = thisBits[i];
                var newV = oldV & other.bits[i];
                if (newV != oldV) {
                    thisBits[i] = newV;
                    anyChanged = true;
                }
            }
            return anyChanged;
        }
        public void UnionWith(BitArray other)
        {
            int l = other.bits.Length;
            if (l > this.bits.Length)
                Array.Resize(ref bits, l + 1);
            for (int i = 0; i < l; i++)
                this.bits[i] |= other.bits[i];
        }
        public bool this[int index]
        {
            get
            {
                int i = index >> 5;
                if (i >= bits.Length)
                    return false;
                int b = index & 31;
                int mask = 1 << b;
                return (bits[i] & mask) != 0;
            }
            set
            {
                int i = index >> 5;
                if (i >= bits.Length)
                    Array.Resize(ref bits, i + 1);
                int b = index & 31;
                int mask = 1 << b;
                if (value)
                    bits[i] |= mask;
                else
                    bits[i] &= ~mask;
            }
        }
    }
}