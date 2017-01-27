﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Runtime.CompilerServices;
using System.Threading;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Internal.Log;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis
{
    internal class MetadataOnlyImage
    {
        public static readonly MetadataOnlyImage Empty = new MetadataOnlyImage(storage: null, assemblyName: string.Empty);
        private static readonly EmitOptions s_emitOptions = new EmitOptions(metadataOnly: true);

        private readonly ITemporaryStreamStorage _storage;
        private readonly string _assemblyName;

        private MetadataOnlyImage(ITemporaryStreamStorage storage, string assemblyName)
        {
            _storage = storage;
            _assemblyName = assemblyName;
        }

        public bool IsEmpty
        {
            get { return _storage == null; }
        }

        public static MetadataOnlyImage Create(Workspace workspace, ITemporaryStorageService service, Compilation compilation, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                workspace.LogTestMessage($"Beginning to create a skeleton assembly for {compilation.AssemblyName}...");

                using (Logger.LogBlock(FunctionId.Workspace_SkeletonAssembly_EmitMetadataOnlyImage, cancellationToken))
                {
                    // TODO: make it to use SerializableBytes.WritableStream rather than MemoryStream so that
                    //       we don't allocate anything for skeleton assembly.
                    using (var stream = SerializableBytes.CreateWritableStream())
                    {
                        // note: cloning compilation so we don't retain all the generated symbols after its emitted.
                        // * REVIEW * is cloning clone p2p reference compilation as well?
                        var emitResult = compilation.Clone().Emit(stream, options: s_emitOptions, cancellationToken: cancellationToken);

                        if (emitResult.Success)
                        {
                            workspace.LogTestMessage($"Successfully emitted a skeleton assembly for {compilation.AssemblyName}");
                            var storage = service.CreateTemporaryStreamStorage(cancellationToken);

                            stream.Position = 0;
                            storage.WriteStream(stream, cancellationToken);

                            return new MetadataOnlyImage(storage, compilation.AssemblyName);
                        }
                        else
                        {
                            workspace.LogTestMessage($"Failed to create a skeleton assembly for {compilation.AssemblyName}:");

                            foreach (var diagnostic in emitResult.Diagnostics)
                            {
                                workspace.LogTestMessage("  " + diagnostic.GetMessage());
                            }
                        }
                    }
                }
            }
            finally
            {
                workspace.LogTestMessage($"Done trying to create a skeleton assembly for {compilation.AssemblyName}");
            }

            return Empty;
        }

        /// <summary>
        /// A map to ensure that the streams from the temporary storage service that back the metadata we create stay alive as long
        /// as the metadata is alive.
        /// </summary>
        private static readonly ConditionalWeakTable<AssemblyMetadata, ISupportDirectMemoryAccess> s_lifetime 
            = new ConditionalWeakTable<AssemblyMetadata, ISupportDirectMemoryAccess>();

        public MetadataReference CreateReference(ImmutableArray<string> aliases, bool embedInteropTypes, DocumentationProvider documentationProvider)
        {
            if (this.IsEmpty)
            {
                return null;
            }

            // first see whether we can use native memory directly.
            var stream = _storage.ReadStream();
            var supportNativeMemory = stream as ISupportDirectMemoryAccess;
            AssemblyMetadata metadata;

            if (supportNativeMemory != null)
            {
                // this is unfortunate that if we give stream, compiler will just re-copy whole content to 
                // native memory again. this is a way to get around the issue by we getting native memory ourselves and then
                // give them pointer to the native memory. also we need to handle lifetime ourselves.
                metadata = AssemblyMetadata.Create(ModuleMetadata.CreateFromImage(supportNativeMemory.GetPointer(), (int)stream.Length));

                // Tie lifetime of stream to metadata we created. It is important to tie this to the Metadata and not the
                // metadata reference, as PE symbols hold onto just the Metadata. We can use Add here since we created
                // a brand new object in AssemblyMetadata.Create above.
                s_lifetime.Add(metadata, supportNativeMemory);
            }
            else
            { 
                // Otherwise, we just let it use stream. Unfortunately, if we give stream, compiler will
                // internally copy it to native memory again. since compiler owns lifetime of stream,
                // it would be great if compiler can be little bit smarter on how it deals with stream.

                // We don't deterministically release the resulting metadata since we don't know 
                // when we should. So we leave it up to the GC to collect it and release all the associated resources.
                metadata = AssemblyMetadata.CreateFromStream(stream);
            }

            return metadata.GetReference(
                documentation: documentationProvider,
                aliases: aliases,
                embedInteropTypes: embedInteropTypes,
                display: _assemblyName);
        }

        public void Cleanup()
        {
            if (_storage != null)
            {
                _storage.Dispose();
            }
        }
    }
}
