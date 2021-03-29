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
            => metadataReference is PortableExecutableReference peReference
                ? peReference.GetModuleVersionId()
                : throw new InvalidOperationException();

        public static Guid GetModuleVersionId(this PortableExecutableReference peReference)
        {
            if (peReference.GetMetadata() is AssemblyMetadata assemblyMetadata &&
                assemblyMetadata.GetModules() is { Length: 1 } modules)
            {
                return modules[0].GetModuleVersionId();
            }

            throw new InvalidOperationException();
        }
    }
}
