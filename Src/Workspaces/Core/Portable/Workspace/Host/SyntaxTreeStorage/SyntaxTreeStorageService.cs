// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Composition;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.LanguageServices;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Host
{
    [ExportWorkspaceService(typeof(ISyntaxTreeStorageService), ServiceLayer.Default), Shared]
    internal class SyntaxTreeStorageService : ISyntaxTreeStorageService
    {
        private static readonly ConditionalWeakTable<SyntaxTree, ITemporaryStorage> map =
            new ConditionalWeakTable<SyntaxTree, ITemporaryStorage>();

        private readonly SimpleTaskQueue queue = new SimpleTaskQueue(TaskScheduler.Default);

        public void EnqueueStore(SyntaxTree tree, SyntaxNode root, ITemporaryStorageService service, CancellationToken cancellationToken)
        {
            // this can be used more like in fire and forget fashion
            EnqueueStoreAsync(tree, root, service, cancellationToken);
        }

        public Task EnqueueStoreAsync(SyntaxTree tree, SyntaxNode root, ITemporaryStorageService service, CancellationToken cancellationToken)
        {
            // returned task can be used to monitor when actual store happened.
            return queue.ScheduleTask(() => Store(tree, root, service, cancellationToken), cancellationToken);
        }

        public async Task StoreAsync(SyntaxTree tree, SyntaxNode root, ITemporaryStorageService service, CancellationToken cancellationToken)
        {
            ITemporaryStorage storage;
            if (map.TryGetValue(tree, out storage))
            {
                // we already have it serialized to temporary storage
                return;
            }

            // tree will be always held alive in memory, but nodes come and go. serialize nodes to storage
            using (var stream = SerializableBytes.CreateWritableStream())
            {
                root.SerializeTo(stream, cancellationToken);
                stream.Position = 0;

                storage = service.CreateTemporaryStorage(cancellationToken);
                await storage.WriteStreamAsync(stream, cancellationToken).ConfigureAwait(false);
            }

            SaveTreeToMap(tree, storage);
        }

        public void Store(SyntaxTree tree, SyntaxNode root, ITemporaryStorageService service, CancellationToken cancellationToken)
        {
            ITemporaryStorage storage;
            if (map.TryGetValue(tree, out storage))
            {
                // we already have it serialized to temporary storage
                return;
            }

            // tree will be always held alive in memory, but nodes come and go. serialize nodes to storage
            using (var stream = SerializableBytes.CreateWritableStream())
            {
                root.SerializeTo(stream, cancellationToken);
                stream.Position = 0;

                storage = service.CreateTemporaryStorage(cancellationToken);
                storage.WriteStream(stream, cancellationToken);
            }

            SaveTreeToMap(tree, storage);
        }

        private static void SaveTreeToMap(SyntaxTree tree, ITemporaryStorage storage)
        {
            var saved = map.GetValue(tree, _ => storage);

            // somebody has beaten us, let storage go.
            if (saved != storage)
            {
                storage.Dispose();
            }
        }

        public bool CanRetrieve(SyntaxTree tree)
        {
            ITemporaryStorage unused;
            return map.TryGetValue(tree, out unused);
        }

        public SyntaxNode Retrieve(SyntaxTree tree, ISyntaxTreeFactoryService service, CancellationToken cancellationToken)
        {
            ITemporaryStorage storage;
            if (!map.TryGetValue(tree, out storage))
            {
                return null;
            }

            using (var stream = storage.ReadStream(cancellationToken))
            {
                return service.DeserializeNodeFrom(stream, cancellationToken);
            }
        }

        public async Task<SyntaxNode> RetrieveAsync(SyntaxTree tree, ISyntaxTreeFactoryService service, CancellationToken cancellationToken)
        {
            ITemporaryStorage storage;
            if (!map.TryGetValue(tree, out storage))
            {
                return null;
            }

            using (var stream = await storage.ReadStreamAsync(cancellationToken).ConfigureAwait(false))
            {
                return service.DeserializeNodeFrom(stream, cancellationToken);
            }
        }
    }
}
