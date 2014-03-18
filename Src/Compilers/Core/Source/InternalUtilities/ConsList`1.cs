// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
        public static readonly ConsList<T> Empty = new ConsList<T>();

        private readonly T head;
        private readonly ConsList<T> tail;

        internal struct Enumerator : IEnumerator<T>
        {
            private T current;
            private ConsList<T> tail;

            internal Enumerator(ConsList<T> list)
            {
                current = default(T);
                tail = list;
            }

            public T Current
            {
                get
                {
                    Debug.Assert(tail != null);
                    return current;
                }
            }

            public bool MoveNext()
            {
                var currentTail = this.tail;
                var newTail = currentTail.tail;

                if (newTail != null)
                {
                    this.current = currentTail.head;
                    this.tail = newTail;
                    return true;
                }

                this.current = default(T);
                return false;
            }

            public void Dispose()
            {
            }

            object IEnumerator.Current
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
            this.head = default(T);
            this.tail = null;
        }

        public ConsList(T head, ConsList<T> tail)
        {
            Debug.Assert(tail != null);

            this.head = head;
            this.tail = tail;
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        public T Head
        {
            get
            {
                Debug.Assert(this != Empty);
                return head;
            }
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        public ConsList<T> Tail
        {
            get
            {
                Debug.Assert(this != Empty);
                return tail;
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
            for (ConsList<T> list = this; list.tail != null; list = list.tail)
            {
                if (any)
                {
                    result.Append(", ");
                }

                result.Append(list.head);
                any = true;
            }

            result.Append("]");
            return result.ToString();
        }
    }
}