// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Collections;
using Microsoft.CodeAnalysis.Symbols;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis;

public partial interface ISymbol
{
    internal readonly partial struct LocationList
    {
        /// <summary>
        /// Implements a reverse-order struct-based collection of <see cref="Location"/> nodes.
        /// This collection is ordered, but random access into the collection is not provided.
        /// </summary>
        [NonDefaultable]
        public readonly struct Reversed : IReadOnlyCollection<Location>
        {
            private readonly ISymbolInternal _symbol;

            internal Reversed(ISymbolInternal symbol)
            {
                _symbol = symbol;
            }

            public int Count => _symbol.LocationsCount;

            public Enumerator GetEnumerator() => new Enumerator(_symbol);

            public ImmutableArray<Location> ToImmutableArray()
            {
                switch (_symbol)
                {
                    case { LocationsCount: 0 }:
                        return ImmutableArray<Location>.Empty;

                    case { LocationsCount: < 5 }:
                        using (var storage = TemporaryArray<Location>.Empty)
                        {
                            foreach (var child in this)
                            {
                                storage.Add(child);
                            }

                            return storage.ToImmutableAndClear();
                        }

                    default:
                        var builder = ArrayBuilder<Location>.GetInstance(Count);
                        foreach (var child in this)
                        {
                            builder.Add(child);
                        }

                        return builder.ToImmutableAndFree();
                }
            }

            IEnumerator<Location> IEnumerable<Location>.GetEnumerator()
            {
                if (this.Count == 0)
                {
                    return SpecializedCollections.EmptyEnumerator<Location>();
                }

                return new EnumeratorImpl(new Enumerator(_symbol));
            }

            IEnumerator IEnumerable.GetEnumerator() => ((IEnumerable<Location>)this).GetEnumerator();

            /// <summary>
            /// Implements a reverse-order struct-based enumerator for <see cref="Location"/> nodes. This type is not hardened
            /// to <c>default(Enumerator)</c>, and will null reference in these cases. Calling <see cref="Current"/> after
            /// <see cref="Enumerator.MoveNext"/> has returned false will throw an <see cref="InvalidOperationException"/>.
            /// </summary>
            [NonDefaultable]
            public struct Enumerator
            {
                private readonly ISymbolInternal _symbol;
                private int _currentSlot;
                private int _currentIndex;

                internal Enumerator(ISymbolInternal symbol)
                {
                    _symbol = symbol;
                    _currentSlot = int.MaxValue;
                    _currentIndex = int.MaxValue;
                }

                public Location Current
                {
                    get
                    {
                        Debug.Assert(_symbol != null && _currentSlot is >= 0 and not int.MaxValue && _currentIndex is >= 0 and not int.MaxValue);
                        return _symbol.GetCurrentLocation(_currentSlot, _currentIndex);
                    }
                }

                public bool MoveNext()
                {
                    Debug.Assert((_currentSlot == int.MaxValue) == (_currentIndex == int.MaxValue));
                    (var result, _currentSlot, _currentIndex) = _symbol.MoveNextLocationReversed(_currentSlot, _currentIndex);
                    return result;
                }

                public void Reset()
                {
                    _currentIndex = int.MaxValue;
                    _currentSlot = int.MaxValue;
                }
            }

            private sealed class EnumeratorImpl : IEnumerator<Location>
            {
                private Enumerator _enumerator;

                public EnumeratorImpl(Enumerator enumerator)
                {
                    _enumerator = enumerator;
                }

                public Location Current => _enumerator.Current;
                object? IEnumerator.Current => _enumerator.Current;
                public void Dispose() { }
                public bool MoveNext() => _enumerator.MoveNext();
                public void Reset() => _enumerator.Reset();
            }
        }
    }
}
