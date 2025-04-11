// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;

#pragma warning disable CA1710 // Rename Microsoft.CodeAnalysis.ArrayBuilder<T> to end in 'Collection'.

namespace Analyzer.Utilities.PooledObjects
{
    internal partial class ArrayBuilder<T>
    {
        /// <summary>
        /// struct enumerator used in foreach.
        /// </summary>
        internal struct Enumerator : IEnumerator<T>
        {
            private readonly ArrayBuilder<T> _builder;
            private int _index;

            public Enumerator(ArrayBuilder<T> builder)
            {
                _builder = builder;
                _index = -1;
            }

            public readonly T Current => _builder[_index];

            public bool MoveNext()
            {
                _index++;
                return _index < _builder.Count;
            }

            public readonly void Dispose()
            {
            }

            readonly object? System.Collections.IEnumerator.Current => this.Current;

            public void Reset()
            {
                _index = -1;
            }
        }
    }
}
