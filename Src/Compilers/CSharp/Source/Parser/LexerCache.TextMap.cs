// #define COLLECT_STATS

using System;

namespace Roslyn.Compilers.CSharp.InternalSyntax
{
    partial class LexerCache
    {
        /// <summary>
        /// This is basically a hash-set of strings that is searchable by  
        /// strings, string sub ranges, character array ranges or string-builder.  
        /// </summary>
        private sealed class TextMap<TValue>
        {
            private class Entry
            {
                internal readonly int HashCode;
                internal readonly string Text;
                internal readonly TValue Value;
                internal Entry Next;

                internal Entry(string text, int hashCode, TValue value, Entry next)
                {
                    this.HashCode = hashCode;
                    this.Text = text;
                    this.Value = value;
                    this.Next = next;
                }
            }

            private Entry[] entries;
            private int count;
            private int mask;

            internal TextMap()
            {
                mask = 31;
                entries = new Entry[mask + 1];
            }

// TODO: remove this when done tweaking this cache.
#if COLLECT_STATS
            private static int hits = 0;
            private static int misses = 0;

            private static void Hit()
            {
                var h = System.Threading.Interlocked.Increment(ref hits);

                if (h % 10000 == 0)
                {
                    Console.WriteLine(h * 100 / (h + misses));
                }
            }

            private static void Miss()
            {
                System.Threading.Interlocked.Increment(ref misses);
            }
#endif

            public TValue Lookup(char[] key, int keyStart, int keyLength, Func<TValue> createValueFunction)
            {
                int hashCode = ComputeHashCode(key, keyStart, keyLength);
                for (Entry e = entries[hashCode & mask]; e != null; e = e.Next)
                {
                    if (e.HashCode == hashCode && TextEquals(e.Text, key, keyStart, keyLength))
                    {
#if COLLECT_STATS
                        Hit();
#endif
                        return e.Value;
                    }
                }

#if COLLECT_STATS
                Miss();
#endif

                var value = createValueFunction();
                this.AddEntry(new string(key, keyStart, keyLength), hashCode, value);
                return value;
            }

            // This table uses FNV1a as a string hash
            private int ComputeHashCode(char[] key, int start, int len)
            {
                int hashCode = unchecked((int)2166136261);  // FNV base
                int end = start + len;

                for (int i = start; i < end; i++)
                {
                    hashCode = (hashCode ^ key[i]) * 16777619;   // FNV prime
                }

                return hashCode;
            }

            private void AddEntry(string text, int hashCode, TValue value)
            {
                int index = hashCode & mask;
                Entry e = new Entry(text, hashCode, value, entries[index]);
                this.entries[index] = e;
                if (count++ == mask)
                {
                    this.Grow();
                }
            }

            private void Grow()
            {
                int newMask = mask * 2 + 1;
                Entry[] oldEntries = entries;
                Entry[] newEntries = new Entry[newMask + 1];

                // use oldEntries.Length to eliminate the rangecheck            
                for (int i = 0; i < oldEntries.Length; i++)
                {
                    Entry e = oldEntries[i];
                    while (e != null)
                    {
                        int newIndex = e.HashCode & newMask;
                        Entry tmp = e.Next;
                        e.Next = newEntries[newIndex];
                        newEntries[newIndex] = e;
                        e = tmp;
                    }
                }

                entries = newEntries;
                mask = newMask;
            }

            private static bool TextEquals(string array, char[] text, int start, int length)
            {
                if (array.Length != length)
                {
                    return false;
                }

                // use array.Length to eliminate the rangecheck
                for (int i = 0; i < array.Length; i++)
                {
                    if (array[i] != text[start + i])
                    {
                        return false;
                    }
                }

                return true;
            }
        }
    }
}