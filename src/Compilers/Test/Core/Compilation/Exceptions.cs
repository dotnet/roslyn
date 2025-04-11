// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using Microsoft.CodeAnalysis;
using static Roslyn.Test.Utilities.ExceptionHelper;

namespace Roslyn.Test.Utilities
{
    [Serializable]
    public class EmitException : Exception
    {
        [field: NonSerialized]
        public IEnumerable<Diagnostic> Diagnostics { get; }

        public EmitException(IEnumerable<Diagnostic> diagnostics, string directory)
            : base(GetMessageFromResult(diagnostics, directory))
        {
            this.Diagnostics = diagnostics;
        }

        protected EmitException(SerializationInfo serializationInfo, StreamingContext streamingContext)
        {
            throw new NotImplementedException();
        }
    }
}
