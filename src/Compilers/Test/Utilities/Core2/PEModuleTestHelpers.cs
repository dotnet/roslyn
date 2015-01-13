// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.


using System.Reflection.Metadata;

namespace Microsoft.CodeAnalysis.Test.Utilities
{
    /// <summary>
    /// Helpers to test metadata.
    /// </summary>
    internal static class PEModuleTestHelpers
    {
        internal static MetadataReader GetMetadataReader(this PEModule module)
        {
            return module.MetadataReader;
        }

        internal static MetadataReader GetMetadataReader(this PEAssembly assembly)
        {
            return assembly.ManifestModule.MetadataReader;
        }
    }
}
