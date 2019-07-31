// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using Roslyn.Utilities;
using System.IO;

namespace Microsoft.CodeAnalysis
{
    internal static class AssemblyIdentityUtils
    {
        public static AssemblyIdentity TryGetAssemblyIdentity(string filePath)
        {
            try
            {
                using var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
                using var peReader = new PEReader(stream);

                var metadataReader = peReader.GetMetadataReader();

                AssemblyDefinition assemblyDefinition = metadataReader.GetAssemblyDefinition();

                string name = metadataReader.GetString(assemblyDefinition.Name);
                Version version = assemblyDefinition.Version;

                StringHandle cultureHandle = assemblyDefinition.Culture;
                string cultureName = (!cultureHandle.IsNil) ? metadataReader.GetString(cultureHandle) : null;
                AssemblyFlags flags = assemblyDefinition.Flags;

                bool hasPublicKey = (flags & AssemblyFlags.PublicKey) != 0;
                BlobHandle publicKeyHandle = assemblyDefinition.PublicKey;
                ImmutableArray<byte> publicKeyOrToken = !publicKeyHandle.IsNil
                    ? metadataReader.GetBlobBytes(publicKeyHandle).AsImmutableOrNull()
                    : default;
                return new AssemblyIdentity(name, version, cultureName, publicKeyOrToken, hasPublicKey);
            }
            catch { }

            return null;
        }
    }
}
