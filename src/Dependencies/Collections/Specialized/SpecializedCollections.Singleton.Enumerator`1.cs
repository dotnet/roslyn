// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System.Collections;
using System.Collections.Generic;

namespace Microsoft.CodeAnalysis.Collections
{
    internal static partial class SpecializedCollections
    {
        private static partial class Singleton
        {
            internal class Enumerator<T> : IEnumerator<T>
            {
                private readonly T _loneValue;
                private bool _moveNextCalled;

                public Enumerator(T value)
                {
                    _loneValue = value;
                    _moveNextCalled = false;
                }

                public T Current => _loneValue;

                object? IEnumerator.Current => _loneValue;

                public void Dispose()
                {
                }

                public bool MoveNext()
                {
                    if (!_moveNextCalled)
                    {
                        _moveNextCalled = true;
                        return true;
                    }

                    return false;
                }

                public void Reset()
                {
                    _moveNextCalled = false;
                }
            }
        }
    }
}
