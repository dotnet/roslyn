// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.Utilities;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.FindSymbols
{
    internal sealed partial class SyntaxTreeIndex
    {
        private readonly LiteralInfo _literalInfo;
        private readonly IdentifierInfo _identifierInfo;
        private readonly ContextInfo _contextInfo;
        private readonly DeclarationInfo _declarationInfo;
        private readonly ExtensionMethodInfo _extensionMethodInfo;

        private SyntaxTreeIndex(
            Checksum checksum,
            LiteralInfo literalInfo,
            IdentifierInfo identifierInfo,
            ContextInfo contextInfo,
            DeclarationInfo declarationInfo,
            ExtensionMethodInfo extensionMethodInfo)
        {
            this.Checksum = checksum;
            _literalInfo = literalInfo;
            _identifierInfo = identifierInfo;
            _contextInfo = contextInfo;
            _declarationInfo = declarationInfo;
            _extensionMethodInfo = extensionMethodInfo;
        }

        private static readonly ConditionalWeakTable<Document, SyntaxTreeIndex> s_documentToIndex = new ConditionalWeakTable<Document, SyntaxTreeIndex>();
        private static readonly ConditionalWeakTable<DocumentId, SyntaxTreeIndex> s_documentIdToIndex = new ConditionalWeakTable<DocumentId, SyntaxTreeIndex>();

        public static async Task PrecalculateAsync(Document document, CancellationToken cancellationToken)
        {
            using (Logger.LogBlock(FunctionId.SyntaxTreeIndex_Precalculate, cancellationToken))
            {
                Debug.Assert(document.IsFromPrimaryBranch());

                var checksum = await GetChecksumAsync(document, cancellationToken).ConfigureAwait(false);

                // Check if we've already created and persisted the index for this document.
                if (await PrecalculatedAsync(document, checksum, cancellationToken).ConfigureAwait(false))
                {
                    return;
                }

                using (Logger.LogBlock(FunctionId.SyntaxTreeIndex_Precalculate_Create, cancellationToken))
                {
                    // If not, create and save the index.
                    var data = await CreateIndexAsync(document, checksum, cancellationToken).ConfigureAwait(false);
                    await data.SaveAsync(document, cancellationToken).ConfigureAwait(false);
                }
            }
        }

        public static Task<SyntaxTreeIndex> GetIndexAsync(Document document, CancellationToken cancellationToken)
            => GetIndexAsync(document, loadOnly: false, cancellationToken);

        public static async Task<SyntaxTreeIndex> GetIndexAsync(
            Document document,
            bool loadOnly,
            CancellationToken cancellationToken)
        {
            // See if we already cached an index with this direct document index.  If so we can just
            // return it with no additional work.
            if (!s_documentToIndex.TryGetValue(document, out var index))
            {
                index = await GetIndexWorkerAsync(document, loadOnly, cancellationToken).ConfigureAwait(false);
                Contract.ThrowIfFalse(index != null || loadOnly == true, "Result can only be null if 'loadOnly: true' was passed.");

                if (index == null && loadOnly)
                {
                    return null;
                }

                // Populate our caches with this data.
                s_documentToIndex.GetValue(document, _ => index);
                s_documentIdToIndex.Remove(document.Id);
                s_documentIdToIndex.GetValue(document.Id, _ => index);
            }

            return index;
        }

        private static async Task<SyntaxTreeIndex> GetIndexWorkerAsync(
            Document document,
            bool loadOnly,
            CancellationToken cancellationToken)
        {
            var checksum = await GetChecksumAsync(document, cancellationToken).ConfigureAwait(false);

            // Check if we have an index for a previous version of this document.  If our
            // checksums match, we can just use that.
            if (s_documentIdToIndex.TryGetValue(document.Id, out var index) &&
                index.Checksum == checksum)
            {
                // The previous index we stored with this documentId is still valid.  Just
                // return that.
                return index;
            }

            // What we have in memory isn't valid.  Try to load from the persistence service.
            index = await LoadAsync(document, checksum, cancellationToken).ConfigureAwait(false);
            if (index != null || loadOnly)
            {
                return index;
            }

            // alright, we don't have cached information, re-calculate them here.
            index = await CreateIndexAsync(document, checksum, cancellationToken).ConfigureAwait(false);

            // okay, persist this info
            await index.SaveAsync(document, cancellationToken).ConfigureAwait(false);

            return index;
        }
    }

    internal class ProjectSyntaxTreeIndex
    {
        private const string PersistenceName = "<ProjectSyntaxTreeIndex>";

        private static readonly ConditionalWeakTable<Project, AsyncLazy<ProjectSyntaxTreeIndex>> _projectToIndex =
            new ConditionalWeakTable<Project, AsyncLazy<ProjectSyntaxTreeIndex>>();

        private readonly Dictionary<Document, SyntaxTreeIndex> _map;

        public ProjectSyntaxTreeIndex(Dictionary<Document, SyntaxTreeIndex> map)
        {
            _map = map;
        }

        internal static async Task<SyntaxTreeIndex> GetIndexAsync(Document document, CancellationToken cancellationToken)
        {
            var indexTask = _projectToIndex.GetValue(document.Project, p => new AsyncLazy<ProjectSyntaxTreeIndex>(c => LoadOrCreateIndexAsync(p, c), cacheResult: true));
            var index = await indexTask.GetValueAsync(cancellationToken).ConfigureAwait(false);
            return index._map[document];
        }

        private static int s_index;

        private static async Task<ProjectSyntaxTreeIndex> LoadOrCreateIndexAsync(
            Project project, CancellationToken cancellationToken)
        {
            Console.WriteLine(Interlocked.Increment(ref s_index) + " " + project.Name);
            var checksum = await SymbolTreeInfo.GetSourceSymbolsChecksumAsync(project, cancellationToken).ConfigureAwait(false);

            var index = await LoadIndexAsync(project, checksum, cancellationToken).ConfigureAwait(false);
            if (index == null)
            {
                index = await CreateIndexAsync(project, cancellationToken).ConfigureAwait(false);
                await SaveIndexAsync(project, index, checksum, cancellationToken).ConfigureAwait(false);
            }

            return index;
        }

        private static async Task<ProjectSyntaxTreeIndex> CreateIndexAsync(Project project, CancellationToken cancellationToken)
        {
            var map = new Dictionary<Document, SyntaxTreeIndex>();

            foreach (var document in project.Documents)
                map.Add(document, await SyntaxTreeIndex.GetIndexAsync(document, cancellationToken).ConfigureAwait(false));

            return new ProjectSyntaxTreeIndex(map);
        }

        private static async Task<ProjectSyntaxTreeIndex> LoadIndexAsync(
            Project project,
            Checksum checksum,
            CancellationToken cancellationToken)
        {
            var solution = project.Solution;
            var persistentStorageService = (IChecksummedPersistentStorageService)solution.Workspace.Services.GetService<IPersistentStorageService>();

            // attempt to load from persisted state
            using var storage = persistentStorageService.GetStorage(solution, checkBranchId: false);
            using var stream = await storage.ReadStreamAsync(project, PersistenceName, checksum, cancellationToken).ConfigureAwait(false);
            using var reader = ObjectReader.TryGetReader(stream, cancellationToken: cancellationToken);
            if (reader != null)
            {
                return ReadFrom(project, reader);
            }

            return null;
        }

        private static ProjectSyntaxTreeIndex ReadFrom(Project project, ObjectReader reader)
        {
            var map = new Dictionary<Document, SyntaxTreeIndex>();

            var count = reader.ReadInt32();
            for (var i = 0; i < count; i++)
            {
                var docId = DocumentId.ReadFrom(reader);
                var document = project.GetDocument(docId);
                Contract.ThrowIfNull(document);

                var checksum = Checksum.ReadFrom(reader);
                var index = SyntaxTreeIndex.ReadFrom(project, reader, checksum);
                map.Add(document, index);
            }

            return new ProjectSyntaxTreeIndex(map);
        }

        private static async Task<bool> SaveIndexAsync(
            Project project, ProjectSyntaxTreeIndex index, Checksum checksum, CancellationToken cancellationToken)
        {
            var solution = project.Solution;
            var persistentStorageService = (IChecksummedPersistentStorageService)solution.Workspace.Services.GetService<IPersistentStorageService>();

            using var storage = persistentStorageService.GetStorage(solution, checkBranchId: false);
            using var stream = SerializableBytes.CreateWritableStream();

            using (var writer = new ObjectWriter(stream, leaveOpen: true, cancellationToken))
            {
                index.WriteTo(writer);
            }

            stream.Position = 0;
            return await storage.WriteStreamAsync(project, PersistenceName, stream, checksum, cancellationToken).ConfigureAwait(false);
        }

        private void WriteTo(ObjectWriter writer)
        {
            writer.WriteInt32(_map.Count);
            foreach (var (doc, docIndex) in _map)
            {
                doc.Id.WriteTo(writer);
                docIndex.Checksum.WriteTo(writer);
                docIndex.WriteTo(writer);
            }
        }
    }
}
