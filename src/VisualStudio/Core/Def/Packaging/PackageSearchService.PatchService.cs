// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

namespace Microsoft.VisualStudio.LanguageServices.Packaging
{
    internal partial class PackageSearchService
    {
        private class PatchService : IPackageSearchPatchService
        {
            public byte[] ApplyPatch(byte[] databaseBytes, byte[] patchBytes)
            {
                return Patching.Delta.ApplyPatch(databaseBytes, patchBytes);
            }
        }
    }
}
