// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;

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
