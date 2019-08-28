// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.IO;

namespace Microsoft.CodeAnalysis.EditAndContinue
{
    internal readonly struct PdbDelta
    {
        // Tokens of updated methods. The debugger enumerates this list 
        // updated methods containing active statements.
        public readonly int[] UpdatedMethods;

        public readonly MemoryStream Stream;

        public PdbDelta(MemoryStream stream, int[] updatedMethods)
        {
            Stream = stream;
            UpdatedMethods = updatedMethods;
        }
    }
}
