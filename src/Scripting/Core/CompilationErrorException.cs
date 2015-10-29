// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
