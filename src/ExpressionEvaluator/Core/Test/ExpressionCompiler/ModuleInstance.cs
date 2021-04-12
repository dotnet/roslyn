// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Immutable;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Reflection.PortableExecutable;
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

        public static unsafe ModuleInstance Create(
            PEMemoryBlock metadata,
            Guid moduleVersionId,
            ISymUnmanagedReader symReader = null)
        {
            return Create((IntPtr)metadata.Pointer, metadata.Length, moduleVersionId, symReader);
        }

        public static ModuleInstance Create(
            IntPtr metadataAddress,
            int metadataLength,
            Guid moduleVersionId,
            ISymUnmanagedReader symReader = null)
        {
            return new ModuleInstance(
                metadata: null,
                moduleVersionId: moduleVersionId,
                metadataLength: metadataLength,
                metadataAddress: metadataAddress,
                symReader: symReader,
                includeLocalSignatures: false);
        }

        public static ModuleInstance Create(PortableExecutableReference reference)
        {
            // make a copy of the metadata, so that we don't dispose the metadata of a reference that are shared across tests:
            return Create(reference.GetMetadata(), symReader: null, includeLocalSignatures: false);
        }

        public static ModuleInstance Create(ImmutableArray<byte> assemblyImage, ISymUnmanagedReader symReader, bool includeLocalSignatures = true)
        {
            // create a new instance of metadata, the resulting object takes an ownership:
            return Create(AssemblyMetadata.CreateFromImage(assemblyImage), symReader, includeLocalSignatures);
        }

        private static unsafe ModuleInstance Create(
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
