// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Diagnostics;
using System.IO;

namespace Microsoft.CodeAnalysis
{
    /// <summary>
    /// The compilation object is an immutable representation of a single invocation of the
    /// compiler. Although immutable, a compilation is also on-demand, and will realize and cache
    /// data as necessary. A compilation can produce a new compilation from existing compilation
    /// with the application of small deltas. In many cases, it is more efficient than creating a
    /// new compilation from scratch, as the new compilation can reuse information from the old
    /// compilation.
    /// </summary>
    public abstract partial class Compilation
    {
        /// <summary>
        /// Abstraction that allows the caller to delay the creation of the <see cref="Stream"/> values 
        /// until they are actually needed.
        /// </summary>
        internal abstract class EmitStreamProvider
        {
            public abstract bool HasPdbStream
            {
                get;
            }

            public abstract Stream GetPeStream(DiagnosticBag diagnostics);

            public abstract Stream GetPdbStream(DiagnosticBag diagnostics);
        }

        private sealed class SimpleEmitStreamProvider : EmitStreamProvider
        {
            private readonly Stream _peStream;
            private readonly Stream _pdbStream;

            internal SimpleEmitStreamProvider(Stream peStream, Stream pdbStream = null)
            {
                Debug.Assert(peStream.CanWrite);
                Debug.Assert(pdbStream == null || pdbStream.CanWrite);
                _peStream = peStream;
                _pdbStream = pdbStream;
            }

            public override bool HasPdbStream
            {
                get { return _pdbStream != null; }
            }

            public override Stream GetPeStream(DiagnosticBag diagnostics)
            {
                return _peStream;
            }

            public override Stream GetPdbStream(DiagnosticBag diagnostics)
            {
                return _pdbStream;
            }
        }
    }
}
