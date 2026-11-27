// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections;
using System.Collections.Generic;

namespace Microsoft.AspNetCore.Razor.Language;

public sealed partial class TagHelperObjectBuilderCollection<TObject, TBuilder>
    where TObject : TagHelperObject<TObject>
    where TBuilder : TagHelperObjectBuilder<TObject>
{
    public struct Enumerator : IEnumerator<TBuilder>
    {
        private readonly TagHelperObjectBuilderCollection<TObject, TBuilder> _collection;
        private TBuilder _current;
        private int _index;

        internal Enumerator(TagHelperObjectBuilderCollection<TObject, TBuilder> collection)
        {
            _collection = collection;
            _index = 0;
            _current = default!;
        }

        public readonly TBuilder Current => _current;

        readonly object IEnumerator.Current => _current;

        public bool MoveNext()
        {
            var collection = _collection;
            if (_index < collection.Count)
            {
                _current = _collection[_index];
                _index++;
                return true;
            }

            return false;
        }

        public void Reset()
        {
            _index = 0;
            _current = default!;
        }

        readonly void IDisposable.Dispose()
        {
        }
    }
}
