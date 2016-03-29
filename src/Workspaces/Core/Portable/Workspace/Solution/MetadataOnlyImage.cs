// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.IO;
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

        private static readonly ConditionalWeakTable<MetadataReference, Stream> s_lifetime = new ConditionalWeakTable<MetadataReference, Stream>();

        public MetadataReference CreateReference(ImmutableArray<string> aliases, bool embedInteropTypes, DocumentationProvider documentationProvider)
        {
            if (this.IsEmpty)
            {
                return null;
            }

            // first see whether we can use native memory directly.
            var stream = _storage.ReadStream();
            var supportNativeMemory = stream as ISupportDirectMemoryAccess;
            if (supportNativeMemory != null)
            {
                // this is unfortunate that if we give stream, compiler will just re-copy whole content to 
                // native memory again. this is a way to get around the issue by we getting native memory ourselves and then
                // give them pointer to the native memory. also we need to handle lifetime ourselves.
                var metadata = AssemblyMetadata.Create(ModuleMetadata.CreateFromImage(supportNativeMemory.GetPointer(), (int)stream.Length));

                var referenceWithNativeMemory = metadata.GetReference(
                    documentation: documentationProvider,
                    aliases: aliases,
                    embedInteropTypes: embedInteropTypes,
                    display: _assemblyName);

                // tie lifetime of stream to metadata reference we created. native memory's lifetime is tied to
                // stream internally and stream is shared between same temporary storage. so here, we should be 
                // sharing same native memory for all skeleton assemblies from same project snapshot.
                s_lifetime.GetValue(referenceWithNativeMemory, _ => stream);

                return referenceWithNativeMemory;
            }

            // Otherwise, we just let it use stream. Unfortunately, if we give stream, compiler will
            // internally copy it to native memory again. since compiler owns lifetime of stream,
            // it would be great if compiler can be little bit smarter on how it deals with stream.

            // We don't deterministically release the resulting metadata since we don't know 
            // when we should. So we leave it up to the GC to collect it and release all the associated resources.
            var metadataFromStream = AssemblyMetadata.CreateFromStream(stream);

            return metadataFromStream.GetReference(
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
