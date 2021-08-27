// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Microsoft.CodeAnalysis.Symbols;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis
{
    internal abstract class AbstractLookupSymbolsInfo<TSymbol>
        where TSymbol : class, ISymbolInternal
    {
        public struct ArityEnumerator : IEnumerator<int>
        {
            private int _current;
            private readonly int _low32bits;
            private int[]? _arities;

            private const int resetValue = -1;
            private const int reachedEndValue = int.MaxValue;

            internal ArityEnumerator(int bitVector, HashSet<int>? arities)
            {
                _current = resetValue;
                _low32bits = bitVector;
                if (arities == null)
                {
                    _arities = null;
                }
                else
                {
                    _arities = arities.ToArray();
                    Array.Sort(_arities);
                }
            }

            public int Current => _current;

            public void Dispose() => _arities = null;

            object? System.Collections.IEnumerator.Current => _current;

            public bool MoveNext()
            {
                if (_current == reachedEndValue)
                {
                    // Already reached the end
                    return false;
                }

                // Find the next set bit
                int arity;

                // Find the next set bit
                for (arity = ++_current; arity < 32; arity++)
                {
                    if (((_low32bits >> arity) & 1) != 0)
                    {
                        _current = arity;
                        return true;
                    }
                }

                if (_arities != null)
                {
                    // Binary search for the current value
                    int index = _arities.BinarySearch(arity);
                    if (index < 0)
                    {
                        index = ~index;
                    }

                    if (index < _arities.Length)
                    {
                        _current = _arities[index];
                        return true;
                    }
                }

                _current = reachedEndValue;
                return false;
            }

            public void Reset() => _current = resetValue;
        }

        // TODO: Is the cost of boxing every instance of UniqueSymbolOrArities that we
        // hand out justified by the benefit of not being able to call our internal
        // APIs incorrectly (without an explicit cast)?
        public interface IArityEnumerable
        {
            ArityEnumerator GetEnumerator();
            int Count { get; }
        }

        // PERF: This is a very frequent allocation, so the aim is to keep
        // it as small as possible.
        private struct UniqueSymbolOrArities : IArityEnumerable
        {
            // For most situations, the set of arities is small and
            // the values are in the low single digits. However, the
            // theoretical max for arity is 32,767 (Int16.MaxValue).
            // The arities field is, therefore, a bitvector of the
            // arity values from zero to 31.
            // If an arity greater than 31 is encountered, then
            // uniqueSymbolOrArities becomes a HashSet for bits
            // 32 and above.

            // This (object) field may be a TSymbol, null or a HashSet<int>
            // If it's a TSymbol:
            //   Then arityBitVectorOrUniqueArity is interpreted as a unique
            //   arity (which may be any value)
            // If it's null:
            //   Then arityBitVectorOrUniqueArity is interpreted as a bitvector
            //   of arities.
            // Otherwise it's a HashSet<int>:
            //   Then arityBitVectorOrUniqueArity is interpreted as a bitvector
            //   of arities for arities from zero to 31 and the HashSet contains
            //   arities of 32 or more.
            private object? _uniqueSymbolOrArities;
            private int _arityBitVectorOrUniqueArity;

            public UniqueSymbolOrArities(int arity, TSymbol uniqueSymbol)
            {
                _uniqueSymbolOrArities = uniqueSymbol;
                _arityBitVectorOrUniqueArity = arity;
                //if there's no unique symbol, how can there be an arity?
                Debug.Assert((uniqueSymbol != null) || (arity == 0));
            }

            public void AddSymbol(TSymbol symbol, int arity)
            {
                if (symbol != null && symbol == _uniqueSymbolOrArities)
                {
                    Debug.Assert(arity == _arityBitVectorOrUniqueArity);
                    return;
                }

                if (this.HasUniqueSymbol)
                {
                    // The symbol is no longer unique. So clear the
                    // UniqueSymbol field and record the unique arity
                    // before adding the new arity value.
                    Debug.Assert(_uniqueSymbolOrArities is TSymbol);
                    _uniqueSymbolOrArities = null;

                    int uniqueArity = _arityBitVectorOrUniqueArity;
                    _arityBitVectorOrUniqueArity = 0;
                    AddArity(uniqueArity);
                }

                AddArity(arity);
            }

            private bool HasUniqueSymbol => _uniqueSymbolOrArities != null && !(_uniqueSymbolOrArities is HashSet<int>);

            private void AddArity(int arity)
            {
                Debug.Assert(!this.HasUniqueSymbol);

                // arities between 0 and 31 will fit in the bit vector
                if (arity < 32)
                {
                    unchecked
                    {
                        int bit = 1 << arity;
                        _arityBitVectorOrUniqueArity |= bit;
                    }
                    return;
                }

                // Otherwise, use a HashSet
                var hashSet = _uniqueSymbolOrArities as HashSet<int>;
                if (hashSet == null)
                {
                    hashSet = new HashSet<int>();
                    _uniqueSymbolOrArities = hashSet;
                }

                hashSet.Add(arity);
            }

            public void GetUniqueSymbolOrArities(out IArityEnumerable? arities, out TSymbol? uniqueSymbol)
            {
                if (this.HasUniqueSymbol)
                {
                    arities = null;
#nullable disable // Can '_uniqueSymbolOrArities' be null? https://github.com/dotnet/roslyn/issues/39166
                    uniqueSymbol = (TSymbol)_uniqueSymbolOrArities;
#nullable enable
                }
                else
                {
                    arities = (_uniqueSymbolOrArities == null && _arityBitVectorOrUniqueArity == 0) ? null : (IArityEnumerable)this;
                    uniqueSymbol = null;
                }
            }

            public ArityEnumerator GetEnumerator()
            {
                Debug.Assert(!this.HasUniqueSymbol);
                return new ArityEnumerator(_arityBitVectorOrUniqueArity, (HashSet<int>?)_uniqueSymbolOrArities);
            }

            public int Count
            {
                get
                {
                    Debug.Assert(!this.HasUniqueSymbol);
                    int count = BitArithmeticUtilities.CountBits(_arityBitVectorOrUniqueArity);
                    var set = (HashSet<int>?)_uniqueSymbolOrArities;
                    if (set != null)
                    {
                        count += set.Count;
                    }

                    return count;
                }
            }

#if DEBUG
            internal TSymbol? UniqueSymbol => _uniqueSymbolOrArities as TSymbol;
#endif
        }

        private readonly IEqualityComparer<string> _comparer;
        private readonly Dictionary<string, UniqueSymbolOrArities> _nameMap;
        internal string? FilterName { get; set; }

        protected AbstractLookupSymbolsInfo(IEqualityComparer<string> comparer)
        {
            _comparer = comparer;
            _nameMap = new Dictionary<string, UniqueSymbolOrArities>(comparer);
        }

        public bool CanBeAdded(string name) => FilterName == null || _comparer.Equals(name, FilterName);

        public void AddSymbol(TSymbol symbol, string name, int arity)
        {
            UniqueSymbolOrArities pair;
            if (!_nameMap.TryGetValue(name, out pair))
            {
                // First time seeing a symbol with this name.  Create a mapping for it from the name
                // to the one arity we've seen, and also store around the symbol as it's currently
                // unique.
                pair = new UniqueSymbolOrArities(arity, symbol);
                _nameMap.Add(name, pair);
            }
            else
            {
                pair.AddSymbol(symbol, arity);

                // Since 'pair' is a struct, the dictionary must be updated with the new value
                _nameMap[name] = pair;
            }

#if DEBUG
            // After adding this symbol, the name must map to it (if it's unique), or it must map to
            // nothing (if it's not unique).  If it maps to another symbol then we've done something
            // horribly wrong.
            Debug.Assert(pair.UniqueSymbol == null || pair.UniqueSymbol == symbol);
#endif
        }

        public ICollection<String> Names => _nameMap.Keys;

        public int Count => _nameMap.Count;

        /// <summary>
        /// If <paramref name="uniqueSymbol"/> is set, then <paramref name="arities"/> will be null.
        /// The only arity in that case will be encoded in the symbol. 
        /// </summary>
        /// <param name="name"></param>
        /// <param name="arities"></param>
        /// <param name="uniqueSymbol"></param>
        /// <returns></returns>
        public bool TryGetAritiesAndUniqueSymbol(
            string name,
            out IArityEnumerable? arities,
            out TSymbol? uniqueSymbol)
        {
            Debug.Assert(CanBeAdded(name));

            UniqueSymbolOrArities pair;
            if (!_nameMap.TryGetValue(name, out pair))
            {
                arities = null;
                uniqueSymbol = null;
                return false;
            }

            // If a unique symbol is set (not null), then its arity should be determined
            // by inspecting the symbol.
            pair.GetUniqueSymbolOrArities(out arities, out uniqueSymbol);
            return true;
        }

        public void Clear()
        {
            _nameMap.Clear();
            FilterName = null;
        }
    }
}
