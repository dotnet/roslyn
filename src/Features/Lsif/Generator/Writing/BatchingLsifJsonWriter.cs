// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Microsoft.CodeAnalysis.LanguageServerIndexFormat.Generator.Graph;

namespace Microsoft.CodeAnalysis.LanguageServerIndexFormat.Generator.Writing
{
    /// <summary>
    /// An <see cref="ILsifJsonWriter"/> that lets us batch up a bunch of elements and
    /// write them at once. This allows for less contention on shared locks since we can have
    /// multiple parallel tasks producing items, and then only at the end contribute items into
    /// the final file.
    /// </summary>
    internal sealed class BatchingLsifJsonWriter : ILsifJsonWriter
    {
        /// <summary>
        /// A lock that must be held when adding elements to <see cref="_elements"/>.
        /// </summary>
        private readonly object _elementsGate = new object();
        private List<Element> _elements = new List<Element>();

        /// <summary>
        /// A lock held when writing to ensure that we maintain ordering of elements across multiple threads.
        /// </summary>
        /// <remarks>
        /// The LSIF file format requires that any vertices referenced by an edge must be written before
        /// the edges that use that vertex. This creates some complexity because we want to be able to batch
        /// writes to the JSON as much as possible to avoid a lot of locking overhead. There's some shared state
        /// that we must track for global symbols, like IDs of various reference lists, and for us to add
        /// reference to those we need to ensure that shared state is flushed before we can write per-document state.
        /// 
        /// This lock is acquired during the entirety of a call to <see cref="FlushToUnderlyingAndEmpty"/>. That
        /// method wants to hold <see cref="_elementsGate"/> for a short as possible, to allow other threads
        /// producing new (unrelated) work to not be blocked behind us writing to the output. But if multiple threads
        /// are trying to flush the same thing, only one thread is going to acquire the list of items to flush.
        /// However, all threads need to wait until that flushing is complete before they can continue to write
        /// to the shared output. By having two locks, this lets us release <see cref="_elementsGate"/> as soon
        /// as we have the list of items, but we can still ensure that all callers to <see cref="FlushToUnderlyingAndEmpty"/>
        /// have waited for what they needed done.
        /// </remarks>
        private readonly object _writingGate = new object();

        private readonly ILsifJsonWriter _underlyingWriter;

        public BatchingLsifJsonWriter(ILsifJsonWriter underlyingWriter)
        {
            _underlyingWriter = underlyingWriter;
        }

        public void Write(Element element)
        {
            lock (_elementsGate)
            {
                _elements.Add(element);
            }
        }

        public void WriteAll(List<Element> elements)
        {
            lock (_elementsGate)
            {
                _elements.AddRange(elements);
            }
        }

        public void FlushToUnderlyingAndEmpty()
        {
            lock (_writingGate)
            {
                List<Element> localElements;

                lock (_elementsGate)
                {
                    localElements = _elements;
                    _elements = new List<Element>();
                }

                _underlyingWriter.WriteAll(localElements);
            }
        }
    }
}
