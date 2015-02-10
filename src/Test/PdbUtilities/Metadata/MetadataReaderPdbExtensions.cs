// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection.Metadata.Ecma335;

namespace System.Reflection.Metadata
{
    public enum ImportScopeKind
    {
        ImportNamespace = 1,
        ImportAssemblyNamespace = 2,
        ImportType = 3,
        ImportXmlNamespace = 4,
        ImportAssemblyReferenceAlias = 5,
        AliasAssemblyReference = 6,
        AliasNamespace = 7,
        AliasAssemblyNamespace = 8,
        AliasType = 9
    }

    public struct ImportDefinition
    {
        private readonly ImportScopeKind _kind;
        private readonly BlobHandle _alias;
        private readonly AssemblyReferenceHandle _assembly;
        private readonly Handle _typeOrNamespace;

        internal ImportDefinition(
            ImportScopeKind kind,
            BlobHandle alias = default(BlobHandle), 
            AssemblyReferenceHandle assembly = default(AssemblyReferenceHandle), 
            Handle typeOrNamespace = default(Handle))
        {
            Debug.Assert(
                typeOrNamespace.IsNil ||
                typeOrNamespace.Kind == HandleKind.Blob ||
                typeOrNamespace.Kind == HandleKind.TypeDefinition ||
                typeOrNamespace.Kind == HandleKind.TypeReference ||
                typeOrNamespace.Kind == HandleKind.TypeSpecification);

            _kind = kind;
            _alias = alias;
            _assembly = assembly;
            _typeOrNamespace = typeOrNamespace;
        }

        public ImportScopeKind Kind => _kind;
        public BlobHandle Alias => _alias;
        public AssemblyReferenceHandle TargetAssembly => _assembly;
        public BlobHandle TargetNamespace => (BlobHandle)_typeOrNamespace;
        public Handle TargetType => _typeOrNamespace;
    }

    internal static class MetadataReaderPdbExtensions
    {
        // TODO: struct enumerator (similar to SequencePointBlobReader)
        public static IEnumerable<ImportDefinition> GetImportDefinitions(this MetadataReader reader, BlobHandle handle)
        {
            var blobReader = reader.GetBlobReader(handle);

            while (blobReader.RemainingBytes > 0)
            {
                var kind = (ImportScopeKind)blobReader.ReadByte();

                switch (kind)
                {
                    case ImportScopeKind.ImportType:
                        yield return new ImportDefinition(
                            kind,
                            typeOrNamespace: blobReader.ReadTypeHandle());

                        break;

                    case ImportScopeKind.ImportNamespace:
                        yield return new ImportDefinition(
                            kind,
                            typeOrNamespace: MetadataTokens.BlobHandle(blobReader.ReadCompressedInteger()));

                        break;

                    case ImportScopeKind.ImportAssemblyNamespace:
                        yield return new ImportDefinition(
                            kind,
                            assembly: MetadataTokens.AssemblyReferenceHandle(blobReader.ReadCompressedInteger()),
                            typeOrNamespace: MetadataTokens.BlobHandle(blobReader.ReadCompressedInteger()));

                        break;

                    case ImportScopeKind.ImportAssemblyReferenceAlias:
                        yield return new ImportDefinition(
                            kind,
                            alias: MetadataTokens.BlobHandle(blobReader.ReadCompressedInteger()));

                        break;

                    case ImportScopeKind.AliasAssemblyReference:
                        yield return new ImportDefinition(
                            kind,
                            alias: MetadataTokens.BlobHandle(blobReader.ReadCompressedInteger()),
                            assembly: MetadataTokens.AssemblyReferenceHandle(blobReader.ReadCompressedInteger()));

                        break;

                    case ImportScopeKind.AliasType:
                        yield return new ImportDefinition(
                            kind,
                            alias: MetadataTokens.BlobHandle(blobReader.ReadCompressedInteger()),
                            typeOrNamespace: blobReader.ReadTypeHandle());

                        break;

                    case ImportScopeKind.ImportXmlNamespace:
                    case ImportScopeKind.AliasNamespace:
                        yield return new ImportDefinition(
                            kind,
                            alias: MetadataTokens.BlobHandle(blobReader.ReadCompressedInteger()),
                            typeOrNamespace: MetadataTokens.BlobHandle(blobReader.ReadCompressedInteger()));

                        break;

                    case ImportScopeKind.AliasAssemblyNamespace:
                        yield return new ImportDefinition(
                            kind,
                            alias: MetadataTokens.BlobHandle(blobReader.ReadCompressedInteger()),
                            assembly: MetadataTokens.AssemblyReferenceHandle(blobReader.ReadCompressedInteger()),
                            typeOrNamespace: MetadataTokens.BlobHandle(blobReader.ReadCompressedInteger()));

                        break;
                }
            }
        }

    }
}
