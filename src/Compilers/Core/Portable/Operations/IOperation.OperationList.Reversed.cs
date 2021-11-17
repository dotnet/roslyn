// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using Microsoft.CodeAnalysis.Operations;
using Microsoft.CodeAnalysis.PooledObjects;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis
{
    public partial interface IOperation
    {
        public readonly partial struct OperationList
        {
            /// <summary>
            /// Implements a reverse-order struct-based collection of <see cref="Operation"/> nodes.
            /// This collection is ordered, but random access into the collection is not provided.
            /// This type is not hardened to <code>default(Reversed)</code>, and will null reference
            /// in these cases.
            /// </summary>
            [NonDefaultable]
            public readonly struct Reversed : IReadOnlyCollection<IOperation>
            {
                private readonly Operation _operation;

                internal Reversed(Operation operation)
                {
                    _operation = operation;
                }

                public int Count => _operation.ChildOperationsCount;

                public Enumerator GetEnumerator() => new Enumerator(_operation);

                public ImmutableArray<IOperation> ToImmutableArray()
                {
                    Enumerator enumerator = GetEnumerator();
                    switch (_operation)
                    {
                        case NoneOperation { Children: var children }:
                            return children;
                        case InvalidOperation { Children: var children }:
                            return children;
                        case { ChildOperationsCount: 0 }:
                            return ImmutableArray<IOperation>.Empty;
                        default:
                            var builder = ArrayBuilder<IOperation>.GetInstance(Count);
                            foreach (var child in this)
                            {
                                builder.Add(child);
                            }
                            return builder.ToImmutableAndFree();
                    }
                }

                IEnumerator<IOperation> IEnumerable<IOperation>.GetEnumerator() => this.GetEnumerator();
                IEnumerator IEnumerable.GetEnumerator() => this.GetEnumerator();

                /// <summary>
                /// Implements a reverse-order struct-based enumerator for <see cref="Operation"/> nodes. This type is not hardened
                /// to <code>default(Enumerator)</code>, and will null reference in these cases. Calling <see cref="Current"/> after
                /// <see cref="Enumerator.MoveNext"/> has returned false will throw an <see cref="InvalidOperationException"/>.
                /// </summary>
                [NonDefaultable]
                public struct Enumerator : IEnumerator<IOperation>
                {
                    private readonly Operation _operation;
                    private int _currentSlot;
                    private int _currentIndex;

                    internal Enumerator(Operation operation)
                    {
                        _operation = operation;
                        _currentSlot = int.MaxValue;
                        _currentIndex = int.MaxValue;
                    }

                    public IOperation Current
                    {
                        get
                        {
                            Debug.Assert(_operation != null && _currentSlot is >= 0 and not int.MaxValue && _currentIndex is >= 0 and not int.MaxValue);
                            return _operation.GetCurrent(_currentSlot, _currentIndex);
                        }
                    }

                    public bool MoveNext()
                    {
                        Debug.Assert((_currentSlot == int.MaxValue) == (_currentIndex == int.MaxValue));
                        (var result, _currentSlot, _currentIndex) = _operation.MoveNextReversed(_currentSlot, _currentIndex);
                        return result;
                    }

                    public void Reset()
                    {
                        _currentIndex = int.MaxValue;
                        _currentSlot = int.MaxValue;
                    }

                    object? IEnumerator.Current => Current;
                    void IDisposable.Dispose() { }
                }
            }
        }
    }
}
