// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Roslyn.Utilities;
using Microsoft.CodeAnalysis.Options;
using System.Collections;

namespace Microsoft.CodeAnalysis.Editor.UnitTests.CodeActions
{
    public abstract partial class AbstractCodeActionOrUserDiagnosticTest
    {
        internal sealed class OptionsDictionary : IOptionsCollection
        {
            private readonly Dictionary<OptionKey2, object> _map;
            public OptionsDictionary(params (OptionKey2 key, object value)[] options)
            {
                _map = new Dictionary<OptionKey2, object>();
                foreach (var option in options)
                {
                    Add(option.key, option.value);
                }
            }

            public object this[OptionKey2 key] { get => _map[key]; set => _map[key] = value; }

            public ICollection<OptionKey2> Keys => _map.Keys;

            public ICollection<object> Values => _map.Values;

            public int Count => _map.Count;

            public bool IsReadOnly => false;

            public void Add(OptionKey2 key, object value)
                => _map.Add(key, value);

            public void Add(KeyValuePair<OptionKey2, object> item)
                => _map.Add(item.Key, item.Value);

            public void Clear()
                => _map.Clear();

            public bool Contains(KeyValuePair<OptionKey2, object> item)
                => _map.Contains(item);

            public bool ContainsKey(OptionKey2 key)
                => _map.ContainsKey(key);

            public void CopyTo(KeyValuePair<OptionKey2, object>[] array, int arrayIndex)
                => throw new NotImplementedException();

            public IEnumerator<KeyValuePair<OptionKey2, object>> GetEnumerator()
                => _map.GetEnumerator();

            public bool Remove(OptionKey2 key)
                => _map.Remove(key);

            public bool Remove(KeyValuePair<OptionKey2, object> item)
                => _map.Remove(item.Key);

            public bool TryGetValue(OptionKey2 key, out object value)
                => _map.TryGetValue(key, out value);

            IEnumerator IEnumerable.GetEnumerator()
                => _map.GetEnumerator();
        }
    }
}
