// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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

            public T Current
            {
                get
                {
                    return _builder[_index];
                }
            }

            public bool MoveNext()
            {
                _index++;
                return _index < _builder.Count;
            }

            public void Dispose()
            {
            }

            object? System.Collections.IEnumerator.Current
            {
                get
                {
                    return this.Current;
                }
            }

            public void Reset()
            {
                _index = -1;
            }
        }
    }
}
