// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

namespace Microsoft.CodeAnalysis.SymbolSearch
{
    internal partial class DefaultSymbolSearchUpdateEngine
    {
        private class PatchService : IPatchService
        {
            public byte[] ApplyPatch(byte[] databaseBytes, byte[] patchBytes)
            {
                return Patching.Delta.ApplyPatch(databaseBytes, patchBytes);
            }
        }
    }
}
