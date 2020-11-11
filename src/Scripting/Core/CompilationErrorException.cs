// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Immutable;

namespace Microsoft.CodeAnalysis.Scripting
{
    /// <summary>
    /// An exception thrown when the compilation stage of interactive execution produces compilation errors.
    /// </summary>
    public sealed class CompilationErrorException : Exception
    {
        /// <summary>
        /// The list of diagnostics produced by compilation.
        /// </summary>
        public ImmutableArray<Diagnostic> Diagnostics { get; }

        public CompilationErrorException(string message, ImmutableArray<Diagnostic> diagnostics)
            : base(message)
        {
            if (diagnostics.IsDefault)
            {
                throw new ArgumentNullException(nameof(diagnostics));
            }

            Diagnostics = diagnostics;
        }
    }
}
