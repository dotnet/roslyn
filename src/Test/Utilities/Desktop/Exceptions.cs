// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using static Roslyn.Test.Utilities.ExceptionHelper; 

namespace Microsoft.CodeAnalysis.Test.Utilities
{
    [Serializable]
    public class EmitException : Exception
    {
        public IEnumerable<Diagnostic> Diagnostics { get; }

        protected EmitException(SerializationInfo info, StreamingContext context) 
            : base(info, context) { }

        public EmitException(IEnumerable<Diagnostic> diagnostics, string directory)
            : base(GetMessageFromResult(diagnostics, directory))
        {
            this.Diagnostics = diagnostics;
        }
    }

    [Serializable]
    public class PeVerifyException : Exception
    {
        protected PeVerifyException(SerializationInfo info, StreamingContext context) 
            : base(info, context) { }

        public PeVerifyException(string output, string exePath) 
            : base(GetMessageFromResult(output, exePath)) { }

    }

    [Serializable]
    public class ExecutionException : Exception
    {
        public ExecutionException(string expectedOutput, string actualOutput, string exePath) 
            : base(GetMessageFromResult(expectedOutput, actualOutput, exePath)) { }

        public ExecutionException(Exception innerException, string exePath) 
            : base(GetMessageFromException(innerException, exePath), innerException) { }

    }
}
