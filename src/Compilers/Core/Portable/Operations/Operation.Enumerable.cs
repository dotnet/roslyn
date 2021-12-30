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
    internal abstract partial class Operation : IOperation
    {
        /// <summary>
        /// Implements a struct-based enumerable for <see cref="Operation"/> nodes, using a slot-based system that tracks
        /// the current slot, and the current index in the slot if the current slot is an immutable array. This type is not hardened
        /// to <code>default(Enumerable)</code>, and will null reference in these cases.
        /// </summary>
        [NonDefaultable]
        internal readonly struct Enumerable : IEnumerable<IOperation>
        {
            private readonly Operation _operation;

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
                    case var _ when !GetEnumerator().MoveNext():
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

        /// <summary>
        /// Implements a struct-based enumerator for <see cref="Operation"/> nodes, using a slot-based system that tracks
        /// the current slot, and the current index in the slot if the current slot is an immutable array. This type is not hardened
        /// to <code>default(Enumerator)</code>, and will null reference in these cases. Implementation of the <see cref="Enumerator.MoveNext"/>
        /// and <see cref="Enumerator.Current"/> members are delegated to the virtual <see cref="Operation.MoveNext(int, int)"/> and
        /// <see cref="Operation.GetCurrent(int, int)"/> methods, respectively. Calling <see cref="Current"/> after
        /// <see cref="Enumerator.MoveNext"/> has returned false will throw an <see cref="InvalidOperationException"/>.
        /// </summary>
        [NonDefaultable]
        internal struct Enumerator : IEnumerator<IOperation>
        {
            private readonly Operation _operation;
            private int _currentSlot;
            private int _currentIndex;

            public Enumerator(Operation operation)
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
