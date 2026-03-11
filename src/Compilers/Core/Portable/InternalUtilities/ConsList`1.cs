// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace Roslyn.Utilities
{
    /// <summary>
    /// a simple Lisp-like immutable list.  Good to use when lists are always accessed from the head.
    /// </summary>
    internal class ConsList<T> : IEnumerable<T>
    {
        public static readonly ConsList<T> Empty = new();

        private readonly T? _head;
        private readonly ConsList<T>? _tail;

        internal struct Enumerator : IEnumerator<T>
        {
            private T? _current;
            private ConsList<T> _tail;

            internal Enumerator(ConsList<T> list)
            {
                _current = default;
                _tail = list;
            }

            public T Current
            {
                get
                {
                    Debug.Assert(_tail != null);

                    // This never returns null after a proper call to `MoveNext` returned true.
                    return _current!;
                }
            }

            public bool MoveNext()
            {
                var currentTail = _tail;
                var newTail = currentTail._tail;

                if (newTail != null)
                {
                    _current = currentTail._head;
                    _tail = newTail;
                    return true;
                }

                _current = default;
                return false;
            }

            public void Dispose()
            {
            }

            object? IEnumerator.Current
            {
                get
                {
                    return this.Current;
                }
            }

            public void Reset()
            {
                throw new NotSupportedException();
            }
        }

        private ConsList()
        {
            _head = default;
            _tail = null;
        }

        public ConsList(T head, ConsList<T> tail)
        {
            Debug.Assert(tail != null);

            _head = head;
            _tail = tail;
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        public T Head
        {
            get
            {
                Debug.Assert(this != Empty);
                return _head!;
            }
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        public ConsList<T> Tail
        {
            get
            {
                Debug.Assert(this != Empty);
                RoslynDebug.Assert(_tail is object);
                return _tail;
            }
        }

        public bool Any()
        {
            return this != Empty;
        }

        public ConsList<T> Push(T value)
        {
            return new ConsList<T>(value, this);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        IEnumerator<T> IEnumerable<T>.GetEnumerator()
        {
            return GetEnumerator();
        }

        public Enumerator GetEnumerator()
        {
            return new Enumerator(this);
        }

        public override string ToString()
        {
            StringBuilder result = new StringBuilder("ConsList[");
            bool any = false;
            for (ConsList<T> list = this; list._tail != null; list = list._tail)
            {
                if (any)
                {
                    result.Append(", ");
                }

                result.Append(list.Head);
                any = true;
            }

            result.Append(']');
            return result.ToString();
        }
    }
}
