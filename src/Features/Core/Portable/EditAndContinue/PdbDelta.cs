// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.IO;

namespace Microsoft.CodeAnalysis.EditAndContinue
{
    internal readonly struct PdbDelta
    {
        // Tokens of updated methods. The debugger enumerates this list 
        // updated methods containing active statements.
        public readonly ImmutableArray<int> UpdatedMethods;

        public readonly ImmutableArray<byte> Stream;

        public PdbDelta(ImmutableArray<byte> stream, ImmutableArray<int> updatedMethods)
        {
            Stream = stream;
            UpdatedMethods = updatedMethods;
        }
    }
}
