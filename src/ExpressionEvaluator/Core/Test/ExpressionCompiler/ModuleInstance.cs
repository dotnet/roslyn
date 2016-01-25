// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Reflection.PortableExecutable;
using System.Runtime.InteropServices;

namespace Microsoft.CodeAnalysis.ExpressionEvaluator
{
    internal sealed class RuntimeInstance : IDisposable
    {
        internal RuntimeInstance(ImmutableArray<ModuleInstance> modules)
        {
            this.Modules = modules;
        }

        internal readonly ImmutableArray<ModuleInstance> Modules;

        void IDisposable.Dispose()
        {
            foreach (var module in this.Modules)
            {
                module.Dispose();
            }
        }
    }

    internal sealed class ModuleInstance : IDisposable
    {
        internal ModuleInstance(
            MetadataReference metadataReference,
            ModuleMetadata moduleMetadata,
            Guid moduleVersionId,
            byte[] fullImage,
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

        internal readonly MetadataReference MetadataReference;
        internal readonly ModuleMetadata ModuleMetadata;
        internal readonly Guid ModuleVersionId;
        internal readonly byte[] FullImage;
        internal readonly byte[] MetadataOnly;
        internal readonly GCHandle MetadataHandle;
        internal readonly object SymReader;
        private readonly bool _includeLocalSignatures;

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

        private bool _disposed;
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
