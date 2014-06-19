using Microsoft.CodeAnalysis.CSharp.Semantics;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CSharp.Syntax.InternalSyntax
{
    partial class LexerCache
    {
        /// <summary>
        /// This is basically a hash-set of strings that is searchable by  
        /// strings, string sub ranges, character array ranges or string-builder.  
        /// </summary>
        private sealed class IdentityDictionary<TValue>
        {
            private class Entry
            {
                internal readonly object Key;
                internal readonly TValue Value;
                internal Entry Next;

                internal Entry(object key, TValue value, Entry next)
                {
                    this.Key = key;
                    this.Value = value;
                    this.Next = next;
                }

                internal Entry DeepClone()
                {
                    return new Entry(Key, Value, Next == null ? null : Next.DeepClone());
                }
            }

            private Entry[] entries;
            private int count;
            private int mask;

            internal IdentityDictionary()
            {
                mask = 31;
                entries = new Entry[mask + 1];
            }

            public IdentityDictionary<TValue> Clone()
            {
                var entriesClone = new Entry[entries.Length];
                for (int i = 0; i < entries.Length; i++)
                {
                    var e = entries[i];
                    if (e != null)
                    {
                        entriesClone[i] = e.DeepClone();
                    }
                }

                return new IdentityDictionary<TValue>(entriesClone, count, mask);
            }

            private IdentityDictionary(Entry[] entries, int count, int mask)
            {
                this.entries = entries;
                this.count = count;
                this.mask = mask;
            }

            public void Add(object key, TValue value)
            {
                int hashCode = key.GetHashCode();
                this.AddEntry(hashCode, key, value);
            }

            private void AddEntry(int hashCode, object key, TValue value)
            {
                int index = hashCode & mask;
                var e = new Entry(key, value, entries[index]);

                this.entries[index] = e;
                if (count++ == mask)
                {
                    this.Grow();
                }
            }

            public bool TryGetValue(object key, out TValue value)
            {
                int hashCode = key.GetHashCode();
                for (Entry e = entries[hashCode & mask]; e != null; e = e.Next)
                {
                    if (key == e.Key)
                    {
                        value = e.Value;
                        return true;
                    }
                }

                value = default(TValue);
                return false;
            }

            private void Grow()
            {
                int newMask = mask * 2 + 1;
                var oldEntries = entries;
                var newEntries = new Entry[newMask + 1];

                // use oldEntries.Length to eliminate the rangecheck            
                for (int i = 0; i < oldEntries.Length; i++)
                {
                    var e = oldEntries[i];
                    while (e != null)
                    {
                        int newIndex = e.Key.GetHashCode() & newMask;
                        var tmp = e.Next;
                        e.Next = newEntries[newIndex];
                        newEntries[newIndex] = e;
                        e = tmp;
                    }
                }

                entries = newEntries;
                mask = newMask;
            }
        }
    }
}