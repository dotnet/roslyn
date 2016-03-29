// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Reflection.PortableExecutable;
using System.Runtime.InteropServices;
using Microsoft.DiaSymReader;

namespace Microsoft.CodeAnalysis.ExpressionEvaluator.UnitTests
{
    internal sealed class ModuleInstance : IDisposable
    {
        // Metadata are owned and disposed by the containing object:
        private readonly Metadata _metadataOpt;

        internal readonly int MetadataLength;
        internal readonly IntPtr MetadataAddress;

        internal readonly Guid ModuleVersionId;
        internal readonly object SymReader;
        private readonly bool _includeLocalSignatures;

        private ModuleInstance(
            Metadata metadata,
            Guid moduleVersionId,
            int metadataLength,
            IntPtr metadataAddress,
            object symReader,
            bool includeLocalSignatures)
        {
            _metadataOpt = metadata;
            ModuleVersionId = moduleVersionId;
            MetadataLength = metadataLength;
            MetadataAddress = metadataAddress;
            SymReader = symReader; // should be non-null if and only if there are symbols
            _includeLocalSignatures = includeLocalSignatures;
        }

        public static ModuleInstance Create(IntPtr metadataAddress, int metadataLength, Guid moduleVersionId)
        {
            return new ModuleInstance(
                metadata: null,
                moduleVersionId: moduleVersionId,
                metadataLength: metadataLength,
                metadataAddress: metadataAddress,
                symReader: null,
                includeLocalSignatures: false);
        }

        public unsafe static ModuleInstance Create(PortableExecutableReference reference)
        {
            // make a copy of the metadata, so that we don't dispose the metadata of a reference that are shared accross tests:
            return Create(reference.GetMetadata().Copy(), symReader: null, includeLocalSignatures: false);
        }

        public unsafe static ModuleInstance Create(ImmutableArray<byte> assemblyImage, ISymUnmanagedReader symReader, bool includeLocalSignatures = true)
        {
            // create a new instance of metadata, the resulting object takes an ownership:
            return Create(AssemblyMetadata.CreateFromImage(assemblyImage), symReader, includeLocalSignatures);
        }

        private unsafe static ModuleInstance Create(
            Metadata metadata,
            object symReader,
            bool includeLocalSignatures)
        {
            var assemblyMetadata = metadata as AssemblyMetadata;
            var moduleMetadata = (assemblyMetadata == null) ? (ModuleMetadata)metadata : assemblyMetadata.GetModules()[0];

            var moduleId = moduleMetadata.Module.GetModuleVersionIdOrThrow();
            var metadataBlock = moduleMetadata.Module.PEReaderOpt.GetMetadata();

            return new ModuleInstance(
                metadata,
                moduleId,
                metadataBlock.Length,
                (IntPtr)metadataBlock.Pointer,
                symReader,
                includeLocalSignatures);
        }

        public void Dispose() => _metadataOpt?.Dispose();

        public MetadataReference GetReference() => (_metadataOpt as AssemblyMetadata)?.GetReference() ?? ((ModuleMetadata)_metadataOpt).GetReference();

        internal MetadataBlock MetadataBlock => new MetadataBlock(ModuleVersionId, Guid.Empty, MetadataAddress, MetadataLength);

        internal unsafe MetadataReader GetMetadataReader() => new MetadataReader((byte*)MetadataAddress, MetadataLength);

        internal int GetLocalSignatureToken(MethodDefinitionHandle methodHandle)
        {
            if (!_includeLocalSignatures)
            {
                return 0;
            }

            var moduleMetadata = (_metadataOpt as AssemblyMetadata)?.GetModules()[0] ?? (ModuleMetadata)_metadataOpt;
            var methodIL = moduleMetadata.Module.GetMethodBodyOrThrow(methodHandle);
            var localSignatureHandle = methodIL.LocalSignature;
            return moduleMetadata.MetadataReader.GetToken(localSignatureHandle);
        }
    }
}
