﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.CodeAnalysis.SymbolSearch
{
    internal partial class SymbolSearchUpdateEngine
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
