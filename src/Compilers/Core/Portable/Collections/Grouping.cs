// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis
{
    /// <summary>
    /// A simple class to implement IGrouping.
    /// </summary>
    internal class Grouping<TKey, TElement> : IGrouping<TKey, TElement>
        where TKey : notnull
    {
        public TKey Key { get; }
        private readonly IEnumerable<TElement> _elements;

        public Grouping(TKey key, IEnumerable<TElement> elements)
        {
            this.Key = key;
            _elements = elements;
        }

        public Grouping(KeyValuePair<TKey, IEnumerable<TElement>> pair)
            : this(pair.Key, pair.Value)
        {
        }

        public IEnumerator<TElement> GetEnumerator()
        {
            return _elements.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
