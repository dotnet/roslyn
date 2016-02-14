// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.Collections;
using System.Collections.Generic;

namespace Microsoft.Cci
{
    internal partial class BlobBuilder
    {
        // internal for testing
        internal struct Chunks : IEnumerable<BlobBuilder>, IEnumerator<BlobBuilder>, IEnumerator
        {
            private readonly BlobBuilder _head;
            private BlobBuilder _next;
            private BlobBuilder _currentOpt;

            internal Chunks(BlobBuilder builder)
            {
                Debug.Assert(builder.IsHead);

                _head = builder;
                _next = builder.FirstChunk;
                _currentOpt = null;
            }

            object IEnumerator.Current => Current;
            public BlobBuilder Current => _currentOpt;

            public bool MoveNext()
            {
                if (_currentOpt == _head)
                {
                    return false;
                }

                if (_currentOpt == _head._nextOrPrevious)
                {
                    _currentOpt = _head;
                    return true;
                }

                _currentOpt = _next;
                _next = _next._nextOrPrevious;
                return true;
            }

            public void Reset()
            {
                _currentOpt = null;
                _next = _head.FirstChunk;
            }

            void IDisposable.Dispose() { }

            // IEnumerable:
            public Chunks GetEnumerator() => this;
            IEnumerator<BlobBuilder> IEnumerable<BlobBuilder>.GetEnumerator() => GetEnumerator();
            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }

        public struct Blobs : IEnumerable<Blob>, IEnumerator<Blob>, IEnumerator
        {
            private Chunks _chunks;

            internal Blobs(BlobBuilder builder)
            {
                _chunks = new Chunks(builder);
            }

            object IEnumerator.Current => Current;

            public Blob Current
            {
                get
                {
                    var current = _chunks.Current;
                    if (current != null)
                    {
                        return new Blob(current._buffer, 0, current.Length);
                    }
                    else
                    {
                        return default(Blob);
                    }
                }
            }

            public bool MoveNext() => _chunks.MoveNext();
            public void Reset() => _chunks.Reset();

            void IDisposable.Dispose() { }

            // IEnumerable:
            public Blobs GetEnumerator() => this;
            IEnumerator<Blob> IEnumerable<Blob>.GetEnumerator() => GetEnumerator();
            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }
    }
}
