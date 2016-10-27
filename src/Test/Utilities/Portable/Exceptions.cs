// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using static Roslyn.Test.Utilities.ExceptionHelper; 

namespace Microsoft.CodeAnalysis.Test.Utilities
{
    public sealed class EmitException : Exception
    {
        public IEnumerable<Diagnostic> Diagnostics { get; }

        internal EmitException(IEnumerable<Diagnostic> diagnostics, string directory)
            : base(GetMessageFromResult(diagnostics, directory))
        {
            this.Diagnostics = diagnostics;
        }
    }

    public sealed class PeVerifyException : Exception
    {
        internal PeVerifyException(string output, string exePath) : base(GetMessageFromResult(output, exePath)) { }
    }

    public sealed class ExecutionException : Exception
    {
        internal ExecutionException(string expectedOutput, string actualOutput, string exePath) : base(GetMessageFromResult(expectedOutput, actualOutput, exePath)) { }

        internal ExecutionException(Exception innerException, string exePath) : base(GetMessageFromException(innerException, exePath), innerException) { }
    }
}
