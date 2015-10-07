// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Globalization;
using System.Reflection;
using System.Reflection.Metadata;

namespace Microsoft.CodeAnalysis
{
    internal static class MetadataReaderExtensions
    {
        internal static bool GetWinMdVersion(this MetadataReader reader, out int majorVersion, out int minorVersion)
        {
            if (reader.MetadataKind == MetadataKind.WindowsMetadata)
            {
                // Name should be of the form "WindowsRuntime {major}.{minor}".
                const string prefix = "WindowsRuntime ";
                string version = reader.MetadataVersion;
                if (version.StartsWith(prefix, StringComparison.Ordinal))
                {
                    var parts = version.Substring(prefix.Length).Split('.');
                    if ((parts.Length == 2) &&
                        int.TryParse(parts[0], NumberStyles.None, CultureInfo.InvariantCulture, out majorVersion) &&
                        int.TryParse(parts[1], NumberStyles.None, CultureInfo.InvariantCulture, out minorVersion))
                    {
                        return true;
                    }
                }
            }

            majorVersion = 0;
            minorVersion = 0;
            return false;
        }

        /// <exception cref="BadImageFormatException">An exception from metadata reader.</exception>
        internal static AssemblyIdentity ReadAssemblyIdentityOrThrow(this MetadataReader reader)
        {
            if (!reader.IsAssembly)
            {
                return null;
            }

            var assemblyDef = reader.GetAssemblyDefinition();

            return reader.CreateAssemblyIdentityOrThrow(
                assemblyDef.Version,
                assemblyDef.Flags,
                assemblyDef.PublicKey,
                assemblyDef.Name,
                assemblyDef.Culture,
                isReference: false);
        }

        /// <exception cref="BadImageFormatException">An exception from metadata reader.</exception>
        internal static ImmutableArray<AssemblyIdentity> GetReferencedAssembliesOrThrow(this MetadataReader reader)
        {
            var result = ArrayBuilder<AssemblyIdentity>.GetInstance(reader.AssemblyReferences.Count);
            try
            {
                foreach (var assemblyRef in reader.AssemblyReferences)
                {
                    AssemblyReference reference = reader.GetAssemblyReference(assemblyRef);
                    result.Add(reader.CreateAssemblyIdentityOrThrow(
                        reference.Version,
                        reference.Flags,
                        reference.PublicKeyOrToken,
                        reference.Name,
                        reference.Culture,
                        isReference: true));
                }

                return result.ToImmutable();
            }
            finally
            {
                result.Free();
            }
        }

        /// <exception cref="BadImageFormatException">An exception from metadata reader.</exception>
        internal static Guid GetModuleVersionIdOrThrow(this MetadataReader reader)
        {
            return reader.GetGuid(reader.GetModuleDefinition().Mvid);
        }

        /// <exception cref="BadImageFormatException">An exception from metadata reader.</exception>
        private static AssemblyIdentity CreateAssemblyIdentityOrThrow(
            this MetadataReader reader,
            Version version,
            AssemblyFlags flags,
            BlobHandle publicKey,
            StringHandle name,
            StringHandle culture,
            bool isReference)
        {
            string nameStr = reader.GetString(name);
            if (!MetadataHelpers.IsValidMetadataIdentifier(nameStr))
            {
                throw new BadImageFormatException(string.Format(CodeAnalysisResources.InvalidAssemblyName, nameStr));
            }

            string cultureName = culture.IsNil ? null : reader.GetString(culture);
            if (cultureName != null && !MetadataHelpers.IsValidMetadataIdentifier(cultureName))
            {
                throw new BadImageFormatException(string.Format(CodeAnalysisResources.InvalidCultureName, cultureName));
            }

            ImmutableArray<byte> publicKeyOrToken = reader.GetBlobContent(publicKey);
            bool hasPublicKey;

            if (isReference)
            {
                hasPublicKey = (flags & AssemblyFlags.PublicKey) != 0;
                if (hasPublicKey)
                {
                    if (!MetadataHelpers.IsValidPublicKey(publicKeyOrToken))
                    {
                        throw new BadImageFormatException(CodeAnalysisResources.InvalidPublicKey);
                    }
                }
                else
                {
                    if (!publicKeyOrToken.IsEmpty &&
                        publicKeyOrToken.Length != AssemblyIdentity.PublicKeyTokenSize)
                    {
                        throw new BadImageFormatException(CodeAnalysisResources.InvalidPublicKeyToken);
                    }
                }
            }
            else
            {
                // Assembly definitions never contain a public key token, they only can have a full key or nothing,
                // so the flag AssemblyFlags.PublicKey does not make sense for them and is ignored.
                // See Ecma-335, Partition II Metadata, 22.2 "Assembly : 0x20".
                // This also corresponds to the behavior of the native C# compiler and sn.exe tool.
                hasPublicKey = !publicKeyOrToken.IsEmpty;
                if (hasPublicKey && !MetadataHelpers.IsValidPublicKey(publicKeyOrToken))
                {
                    throw new BadImageFormatException(CodeAnalysisResources.InvalidPublicKey);
                }
            }

            if (publicKeyOrToken.IsEmpty)
            {
                publicKeyOrToken = default(ImmutableArray<byte>);
            }

            return new AssemblyIdentity(
                name: nameStr,
                version: version,
                cultureName: cultureName,
                publicKeyOrToken: publicKeyOrToken,
                hasPublicKey: hasPublicKey,
                isRetargetable: (flags & AssemblyFlags.Retargetable) != 0,
                contentType: (AssemblyContentType)((int)(flags & AssemblyFlags.ContentTypeMask) >> 9),
                noThrow: true);
        }

        internal static bool DeclaresTheObjectClass(this MetadataReader reader)
        {
            return reader.DeclaresType(IsTheObjectClass);
        }

        private static bool IsTheObjectClass(this MetadataReader reader, TypeDefinition typeDef)
        {
            return typeDef.BaseType.IsNil &&
                reader.IsPublicNonInterfaceType(typeDef, "System", "Object");
        }

        internal static bool DeclaresType(this MetadataReader reader, Func<MetadataReader, TypeDefinition, bool> predicate)
        {
            foreach (TypeDefinitionHandle handle in reader.TypeDefinitions)
            {
                try
                {
                    var typeDef = reader.GetTypeDefinition(handle);
                    if (predicate(reader, typeDef))
                    {
                        return true;
                    }
                }
                catch (BadImageFormatException)
                {
                }
            }

            return false;
        }

        /// <exception cref="BadImageFormatException">An exception from metadata reader.</exception>
        internal static bool IsPublicNonInterfaceType(this MetadataReader reader, TypeDefinition typeDef, string namespaceName, string typeName)
        {
            return (typeDef.Attributes & (TypeAttributes.Public | TypeAttributes.Interface)) == TypeAttributes.Public &&
                reader.StringComparer.Equals(typeDef.Name, typeName) &&
                reader.StringComparer.Equals(typeDef.Namespace, namespaceName);
        }
    }
}
