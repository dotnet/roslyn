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
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis
{
    public partial interface IOperation
    {
        /// <summary>
        /// Implements a struct-based collection of <see cref="Operation"/> nodes. This collection is ordered, but
        /// random access into the collection is not provided. This type is not hardened to 
        /// <code>default(ChildCollection)</code>, and will null reference in these cases.
        /// </summary>
        [NonDefaultable]
        public readonly partial struct OperationList : IReadOnlyCollection<IOperation>
        {
            private readonly Operation _operation;

            internal OperationList(Operation operation)
            {
                _operation = operation;
            }

            public int Count => _operation.ChildOperationsCount;

            public Enumerator GetEnumerator() => new Enumerator(_operation);

            public ImmutableArray<IOperation> ToImmutableArray()
            {
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

            public bool Any() => Count > 0;

            public IOperation First()
            {
                var enumerator = GetEnumerator();
                if (enumerator.MoveNext())
                {
                    return enumerator.Current;
                }

                throw new InvalidOperationException();
            }

            public Reversed Reverse() => new Reversed(_operation);

            public IOperation Last()
            {
                var enumerator = Reverse().GetEnumerator();
                if (enumerator.MoveNext())
                {
                    return enumerator.Current;
                }

                throw new InvalidOperationException();
            }

            /// <summary>
            /// Implements a struct-based enumerator for <see cref="Operation"/> nodes. This type is not hardened
            /// to <code>default(Enumerator)</code>, and will null reference in these cases. Calling <see cref="Current"/> after
            /// <see cref="Enumerator.MoveNext"/> has returned false will throw an <see cref="InvalidOperationException"/>.
            /// </summary>
            [NonDefaultable]
            public struct Enumerator : IEnumerator<IOperation>
            {
                /// <summary>
                /// Implementation of the <see cref="Enumerator.MoveNext"/> and <see cref="Enumerator.Current"/>
                /// members are delegated to the virtual <see cref="Operation.MoveNext(int, int)"/> and
                /// <see cref="Operation.GetCurrent(int, int)"/> methods, respectively.
                /// </summary>
                private readonly Operation _operation;
                /// <summary>
                /// 
                /// </summary>
                private int _currentSlot;
                private int _currentIndex;

                internal Enumerator(Operation operation)
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
                    bool result;
                    (result, _currentSlot, _currentIndex) = _operation.MoveNext(_currentSlot, _currentIndex);
                    return result;
                }

                public void Reset()
                {
                    _currentSlot = -1;
                    _currentIndex = -1;
                }

                object? IEnumerator.Current => this.Current;
                void IDisposable.Dispose() { }
            }
        }
    }
}
