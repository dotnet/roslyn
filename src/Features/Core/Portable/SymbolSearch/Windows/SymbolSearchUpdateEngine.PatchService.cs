// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.CodeAnalysis.SymbolSearch;

internal sealed partial class SymbolSearchUpdateEngine
{
    private sealed class PatchService : IPatchService
    {
        public byte[] ApplyPatch(byte[] databaseBytes, byte[] patchBytes)
            => NativePatching.ApplyPatch(databaseBytes, patchBytes);
    }
}
