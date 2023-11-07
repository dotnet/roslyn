// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;
using System.IO;
using Roslyn.Utilities;

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
        /// until they are actually needed. The <see cref="Stream"/> provided here is owned by 
        /// this type and consumers should not dispose it.
        /// </summary>
        internal abstract class EmitStreamProvider
        {
            private Stream? _stream;

            protected EmitStreamProvider(Stream? stream = null)
            {
                _stream = stream;
            }

            /// <summary>
            /// This method will be called once during Emit at the time the Compilation needs 
            /// to create a stream for writing. It will not be called in the case of
            /// user errors in code. Shall not be called when <see cref="Stream"/> returns non-null.
            /// </summary>
            protected abstract Stream? CreateStream(DiagnosticBag diagnostics);

            /// <summary>
            /// Returns a <see cref="Stream"/>. If one cannot be gotten or created then a diagnostic will 
            /// be added to <paramref name="diagnostics"/>
            /// </summary>
            public Stream? GetOrCreateStream(DiagnosticBag diagnostics)
            {
                if (_stream is null)
                {
                    _stream = CreateStream(diagnostics);
                }

                return _stream;
            }
        }

        internal sealed class SimpleEmitStreamProvider : EmitStreamProvider
        {
            internal SimpleEmitStreamProvider(Stream stream)
                : base(stream)
            {
                RoslynDebug.Assert(stream != null);
            }

            protected override Stream CreateStream(DiagnosticBag diagnostics)
            {
                throw ExceptionUtilities.Unreachable();
            }
        }
    }
}
