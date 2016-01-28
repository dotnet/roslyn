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
        internal readonly MetadataReference MetadataReference;
        internal readonly ModuleMetadata ModuleMetadata;
        internal readonly Guid ModuleVersionId;
        internal readonly ImmutableArray<byte> FullImage;
        internal readonly byte[] MetadataOnly;
        internal readonly GCHandle MetadataHandle;
        internal readonly object SymReader;
        private readonly bool _includeLocalSignatures;
        private bool _disposed;

        private ModuleInstance(
            MetadataReference metadataReference,
            ModuleMetadata moduleMetadata,
            Guid moduleVersionId,
            ImmutableArray<byte> fullImage,
            byte[] metadataOnly,
            object symReader,
            bool includeLocalSignatures)
        {
            Debug.Assert((fullImage == null) || (fullImage.Length > metadataOnly.Length));

            this.MetadataReference = metadataReference;
            this.ModuleMetadata = moduleMetadata;
            this.ModuleVersionId = moduleVersionId;
            this.FullImage = fullImage;
            this.MetadataOnly = metadataOnly;
            this.MetadataHandle = GCHandle.Alloc(metadataOnly, GCHandleType.Pinned);
            this.SymReader = symReader; // should be non-null if and only if there are symbols
            _includeLocalSignatures = includeLocalSignatures;
        }

        public static ModuleInstance Create(byte[] metadataOnly)
        {
            return new ModuleInstance(
                metadataReference: null,
                moduleMetadata: null,
                moduleVersionId: default(Guid),
                fullImage: default(ImmutableArray<byte>),
                metadataOnly: metadataOnly,
                symReader: null,
                includeLocalSignatures: false);
        }

        public static ModuleInstance Create(
            MetadataReference reference,
            ImmutableArray<byte> peImage,
            object symReader,
            bool includeLocalSignatures)
        {
            var moduleMetadata = reference.GetModuleMetadata();
            var moduleId = moduleMetadata.Module.GetModuleVersionIdOrThrow();
            // The Expression Compiler expects metadata only, no headers or IL.
            var metadataBytes = moduleMetadata.Module.PEReaderOpt.GetMetadata().GetContent().ToArray();
            return new ModuleInstance(
                reference,
                moduleMetadata,
                moduleId,
                peImage,
                metadataBytes,
                symReader,
                includeLocalSignatures && (peImage != null));
        }

        public static ModuleInstance Create(ImmutableArray<byte> peImage, ISymUnmanagedReader symReader)
        {
            var peReference = AssemblyMetadata.CreateFromImage(peImage).GetReference();
            return Create(peReference, peImage, symReader, includeLocalSignatures: true);
        }

        internal IntPtr MetadataAddress
        {
            get { return this.MetadataHandle.AddrOfPinnedObject(); }
        }

        internal int MetadataLength
        {
            get { return this.MetadataOnly.Length; }
        }

        internal MetadataBlock MetadataBlock
        {
            get { return new MetadataBlock(this.ModuleVersionId, Guid.Empty, this.MetadataAddress, this.MetadataLength); }
        }

        internal unsafe MetadataReader MetadataReader
        {
            get { return new MetadataReader((byte*)MetadataHandle.AddrOfPinnedObject(), MetadataLength); }
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                this.MetadataHandle.Free();
                _disposed = true;
            }
        }

        internal int GetLocalSignatureToken(MethodDefinitionHandle methodHandle)
        {
            if (!_includeLocalSignatures)
            {
                return 0;
            }

            using (var metadata = ModuleMetadata.CreateFromImage(this.FullImage))
            {
                var reader = metadata.MetadataReader;
                var methodIL = metadata.Module.GetMethodBodyOrThrow(methodHandle);
                var localSignatureHandle = methodIL.LocalSignature;
                return reader.GetToken(localSignatureHandle);
            }
        }
    }
}
