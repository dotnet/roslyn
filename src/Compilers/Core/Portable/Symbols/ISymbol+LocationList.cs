// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using Microsoft.CodeAnalysis.Symbols;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis;

public partial interface ISymbol
{
    [NonDefaultable]
    public readonly partial struct LocationList : IReadOnlyCollection<Location>
    {
        private readonly ISymbolInternal _symbol;

        internal LocationList(ISymbolInternal symbol)
        {
            _symbol = symbol;
        }

        public int Count => _symbol.LocationsCount;

        public Enumerator GetEnumerator() => new Enumerator(_symbol);

        public ImmutableArray<Location> ToImmutableArray()
        {
            return _symbol.Locations;
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

        public bool Any() => Count > 0;

        internal bool Any<TArg>(Func<Location, TArg, bool> predicate, TArg arg)
        {
            foreach (var location in this)
            {
                if (predicate(location, arg))
                    return true;
            }

            return false;
        }

        public Location First()
            => FirstOrDefault() ?? throw new InvalidOperationException();

        public Location? FirstOrDefault()
        {
            var enumerator = GetEnumerator();
            if (enumerator.MoveNext())
            {
                return enumerator.Current;
            }

            return null;
        }

        internal Reversed Reverse() => new Reversed(_symbol);

        public Location Last()
        {
            var enumerator = Reverse().GetEnumerator();
            if (enumerator.MoveNext())
            {
                return enumerator.Current;
            }

            throw new InvalidOperationException();
        }

        /// <summary>
        /// Implements a struct-based enumerator for <see cref="Location"/> nodes. This type is not hardened
        /// to <c>default(Enumerator)</c>, and will null reference in these cases. Calling <see cref="Current"/> after
        /// <see cref="Enumerator.MoveNext"/> has returned false will throw an <see cref="InvalidOperationException"/>.
        /// </summary>
        [NonDefaultable]
        public struct Enumerator
        {
            /// <summary>
            /// Implementation of the <see cref="Enumerator.MoveNext"/> and <see cref="Enumerator.Current"/>
            /// members are delegated to the virtual <see cref="ISymbolInternal.MoveNextLocation(int, int)"/> and
            /// <see cref="ISymbolInternal.GetCurrentLocation(int, int)"/> methods, respectively.
            /// </summary>
            private readonly ISymbolInternal _symbol;
            /// <summary>
            /// 
            /// </summary>
            private int _currentSlot;
            private int _currentIndex;

            internal Enumerator(ISymbolInternal symbol)
            {
                _symbol = symbol;
                _currentSlot = -1;
                _currentIndex = -1;
            }

            public Location Current
            {
                get
                {
                    Debug.Assert(_symbol != null && _currentSlot >= 0 && _currentIndex >= 0);
                    return _symbol.GetCurrentLocation(_currentSlot, _currentIndex);
                }
            }

            public bool MoveNext()
            {
                bool result;
                (result, _currentSlot, _currentIndex) = _symbol.MoveNextLocation(_currentSlot, _currentIndex);
                return result;
            }

            public void Reset()
            {
                _currentSlot = -1;
                _currentIndex = -1;
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
