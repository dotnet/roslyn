// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using Roslyn.Test.Utilities;

namespace Microsoft.CodeAnalysis.Test.Utilities
{
    public static class MetadataReferenceExtensions
    {
        public static Guid GetModuleVersionId(this MetadataReference metadataReference)
            => GetManifestModuleMetadata(metadataReference).GetModuleVersionId();

        public static AssemblyIdentity GetAssemblyIdentity(this MetadataReference reference)
            => reference.GetManifestModuleMetadata().MetadataReader.ReadAssemblyIdentityOrThrow();

        public static ModuleMetadata GetManifestModuleMetadata(this MetadataReference reference)
            => reference is PortableExecutableReference peReference
                ? peReference.GetManifestModuleMetadata()
            : throw new InvalidOperationException();

        public static ModuleMetadata GetManifestModuleMetadata(this PortableExecutableReference peReference)
        {
            switch (peReference.GetMetadata())
            {
                case AssemblyMetadata assemblyMetadata:
                    {
                        if (assemblyMetadata.GetModules() is { Length: > 0 } modules)
                        {
                            return modules[0];
                        }
                    }
                    break;
                case ModuleMetadata moduleMetadata:
                    return moduleMetadata;
            }

            throw new InvalidOperationException();
        }
    }
}
