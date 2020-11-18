// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections;
using System;
using System.Diagnostics;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.Operations;
using Microsoft.CodeAnalysis.PooledObjects;

namespace Microsoft.CodeAnalysis
{
    internal abstract partial class Operation : IOperation
    {
        internal readonly struct Enumerable : IEnumerable<IOperation>
        {
            private readonly Operation? _operation;

            internal Enumerable(Operation operation)
            {
                _operation = operation;
            }

            public Enumerator GetEnumerator() => new Enumerator(_operation);

            public ImmutableArray<IOperation> ToImmutableArray()
            {
                switch (_operation)
                {
                    case NoneOperation { Children: var children }:
                        return children;
                    case InvalidOperation { Children: var children }:
                        return children;
                    case null:
                    case { } when !GetEnumerator().MoveNext():
                        return ImmutableArray<IOperation>.Empty;
                    default:
                        var builder = ArrayBuilder<IOperation>.GetInstance();
                        foreach (var child in this)
                        {
                            builder.Add(child);
                        }
                        return builder.ToImmutableAndFree();
                }
            }

            IEnumerator<IOperation> IEnumerable<IOperation>.GetEnumerator() => this.GetEnumerator();
            IEnumerator IEnumerable.GetEnumerator() => this.GetEnumerator();
        }

        internal struct Enumerator : IEnumerator<IOperation>
        {
            private readonly Operation? _operation;
            private int _currentSlot;
            private int _currentIndex;

            public Enumerator(Operation? operation)
            {
                _operation = operation;
                _currentSlot = -1;
                _currentIndex = -1;
            }

            public IOperation Current
            {
                get
                {
                    Debug.Assert(_operation != null && _currentSlot >= 0 && _currentIndex >= 0);
                    return _operation.GetCurrent(_currentSlot, _currentIndex);
                }
            }

            public bool MoveNext()
            {
                switch (_operation?.MoveNext(_currentSlot, _currentIndex))
                {
                    case null or (false, _, _):
                        _currentSlot = int.MinValue;
                        _currentIndex = int.MinValue;
                        return false;

                    case (true, var nextSlot, var nextIndex):
                        _currentSlot = nextSlot;
                        _currentIndex = nextIndex;
                        return true;
                }

            }

            void IEnumerator.Reset()
            {
                _currentSlot = -1;
                _currentIndex = -1;
            }

            object? IEnumerator.Current => this.Current;
            void IDisposable.Dispose() { }
        }
    }
}
