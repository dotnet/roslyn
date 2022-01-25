// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CSharp
{
    // This is an unusual implementation of a mutable set. We sometimes have to take
    // the unions or intersections of sets using a custom equality comparator.
    // If the comparison says that two items are "equal" we nevertheless may 
    // express a preference for one or the other to be the one that "wins" and
    // ultimately ends up in the set. All equal items are equal, but some are more 
    // equal than others.
    //
    // In particular, we may have to make an arbitrary choice amongst several
    // possibilities, and it does not matter to the user which choice we make.
    // However, whichever choice we make should be *repeatable*. Compiling the
    // same program twice should produce the same errors, even if some of those
    // errors were chosen arbitrarily. 
    //
    // For example, when a lambda has been bound a dozen ways and it has failed
    // each time with "no method Blah on int", "... on string", "... on double",
    // we want to choose one of those errors to report. It does not matter which,
    // but to ensure that our test cases and user experience are stable, we need
    // a consistent way to choose one of them that does not rely on subtleties
    // of source-code ordering or implementation details of collection types.

    internal sealed class FirstAmongEqualsSet<T> : IEnumerable<T>
    {
        // We're going to be making one of these hash sets on every
        // intersection, so we might as well just make one and re-use it.
        private readonly HashSet<T> _hashSet;
        private readonly Dictionary<T, T> _dictionary;
        private readonly Func<T, T, int> _canonicalComparer;

        public FirstAmongEqualsSet(
            IEnumerable<T> items,
            IEqualityComparer<T> equalityComparer,
            Func<T, T, int> canonicalComparer)
        {
            _canonicalComparer = canonicalComparer;
            _dictionary = new Dictionary<T, T>(equalityComparer);
            _hashSet = new HashSet<T>(equalityComparer);
            UnionWith(items);
        }

        public void UnionWith(IEnumerable<T> items)
        {
            foreach (T item in items)
            {
                T current;
                if (!_dictionary.TryGetValue(item, out current) || IsMoreCanonical(item, current))
                {
                    _dictionary[item] = item;
                }
            }
        }

        // Is the new item better than the old one?
        private bool IsMoreCanonical(T newItem, T oldItem)
        {
            return _canonicalComparer(newItem, oldItem) > 0;
        }

        public void IntersectWith(IEnumerable<T> items)
        {
            Debug.Assert(_hashSet.Count == 0);

            // Make a copy of the input for quick indexing.
            // (As an optimization, we could check to see if the sequence already 
            // is a hash set; in practice it will not typically be.)
            _hashSet.UnionWith(items);

            // Remove from the dictionary all items that are not
            // in the input item set.

            // Make a copy of the keys so that we are not changing the 
            // dictionary as we enumerate it.
            foreach (var key in _dictionary.Keys.ToList())
            {
                if (!_hashSet.Contains(key))
                {
                    _dictionary.Remove(key);
                }
            }

            // Now update the dictionary so that it contains the more
            // canonical of the items that are in both sets.

            foreach (var item in _hashSet)
            {
                T current;
                if (_dictionary.TryGetValue(item, out current) && IsMoreCanonical(item, current))
                {
                    _dictionary[item] = item;
                }
            }
            _hashSet.Clear();
        }

        public IEnumerator<T> GetEnumerator()
        {
            return _dictionary.Values.GetEnumerator();
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }
    }
}
