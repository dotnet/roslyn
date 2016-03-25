// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using static Roslyn.Test.Utilities.ExceptionHelper; 

namespace Microsoft.CodeAnalysis.Test.Utilities
{
    public class EmitException : Exception
    {
        public IEnumerable<Diagnostic> Diagnostics { get; }

        public EmitException(IEnumerable<Diagnostic> diagnostics, string directory)
            : base(GetMessageFromResult(diagnostics, directory))
        {
            this.Diagnostics = diagnostics;
        }
    }

    public class PeVerifyException : Exception
    {
        public PeVerifyException(string output, string exePath) : base(GetMessageFromResult(output, exePath)) { }
    }

    public class ExecutionException : Exception
    {
        public ExecutionException(string expectedOutput, string actualOutput, string exePath) : base(GetMessageFromResult(expectedOutput, actualOutput, exePath)) { }

        public ExecutionException(Exception innerException, string exePath) : base(GetMessageFromException(innerException, exePath), innerException) { }
    }
}
