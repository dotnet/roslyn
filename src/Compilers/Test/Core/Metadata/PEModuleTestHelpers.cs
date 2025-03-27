// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Reflection.Metadata;
using Microsoft.CodeAnalysis;

namespace Roslyn.Test.Utilities
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
