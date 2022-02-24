// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.Utilities;
using Microsoft.CodeAnalysis.Storage;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.FindSymbols
{
    internal partial class AbstractSyntaxIndex<TIndex> : IObjectWritable
    {
        private static readonly string s_persistenceName = typeof(TIndex).Name;
        private static readonly Checksum s_serializationFormatChecksum = Checksum.Create("29");

        public readonly Checksum? Checksum;

        protected static Task<TIndex?> LoadAsync(
            Document document,
            Checksum checksum,
            IndexReader read,
            CancellationToken cancellationToken)
        {
            var solution = document.Project.Solution;
            var database = solution.Options.GetPersistentStorageDatabase();

            var storageService = solution.Workspace.Services.GetPersistentStorageService(database);
            return LoadAsync(storageService, DocumentKey.ToDocumentKey(document), checksum, SyntaxTreeIndex.GetStringTable(document.Project), read, cancellationToken);
        }

        protected static async Task<TIndex?> LoadAsync(
            IChecksummedPersistentStorageService storageService,
            DocumentKey documentKey,
            Checksum? checksum,
            StringTable stringTable,
            IndexReader read,
            CancellationToken cancellationToken)
        {
            try
            {
                var storage = await storageService.GetStorageAsync(documentKey.Project.Solution, cancellationToken).ConfigureAwait(false);
                await using var _ = storage.ConfigureAwait(false);

                // attempt to load from persisted state
                using var stream = await storage.ReadStreamAsync(documentKey, s_persistenceName, checksum, cancellationToken).ConfigureAwait(false);
                using var reader = ObjectReader.TryGetReader(stream, cancellationToken: cancellationToken);
                if (reader != null)
                    return read(stringTable, reader, checksum);
            }
            catch (Exception e) when (IOUtilities.IsNormalIOException(e))
            {
                // Storage APIs can throw arbitrary exceptions.
            }

            return null;
        }

        public static async Task<Checksum> GetChecksumAsync(
            Document document, CancellationToken cancellationToken)
        {
            // Since we build the SyntaxTreeIndex from a SyntaxTree, we need our checksum to change
            // any time the SyntaxTree could have changed.  Right now, that can only happen if the
            // text of the document changes, or the ParseOptions change.  So we get the checksums
            // for both of those, and merge them together to make the final checksum.
            //
            // We also want the checksum to change any time our serialization format changes.  If
            // the format has changed, all previous versions should be invalidated.
            var project = document.Project;
            var parseOptionsChecksum = project.State.GetParseOptionsChecksum();

            var documentChecksumState = await document.State.GetStateChecksumsAsync(cancellationToken).ConfigureAwait(false);
            var textChecksum = documentChecksumState.Text;

            return Checksum.Create(textChecksum, parseOptionsChecksum, s_serializationFormatChecksum);
        }

        private async Task<bool> SaveAsync(
            Document document, CancellationToken cancellationToken)
        {
            var solution = document.Project.Solution;
            var persistentStorageService = solution.Workspace.Services.GetPersistentStorageService(solution.Options);

            try
            {
                var storage = await persistentStorageService.GetStorageAsync(SolutionKey.ToSolutionKey(solution), cancellationToken).ConfigureAwait(false);
                await using var _ = storage.ConfigureAwait(false);
                using var stream = SerializableBytes.CreateWritableStream();

                using (var writer = new ObjectWriter(stream, leaveOpen: true, cancellationToken))
                {
                    WriteTo(writer);
                }

                stream.Position = 0;
                return await storage.WriteStreamAsync(document, s_persistenceName, stream, this.Checksum, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception e) when (IOUtilities.IsNormalIOException(e))
            {
                // Storage APIs can throw arbitrary exceptions.
            }

            return false;
        }

        protected static async Task PrecalculateAsync(Document document, IndexCreator create, CancellationToken cancellationToken)
        {
            if (!document.SupportsSyntaxTree)
                return;

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
                    var data = await CreateIndexAsync(document, checksum, create, cancellationToken).ConfigureAwait(false);
                    await data.SaveAsync(document, cancellationToken).ConfigureAwait(false);
                }
            }
        }

        private static async Task<bool> PrecalculatedAsync(
            Document document, Checksum checksum, CancellationToken cancellationToken)
        {
            var solution = document.Project.Solution;
            var persistentStorageService = solution.Workspace.Services.GetPersistentStorageService(solution.Options);

            // check whether we already have info for this document
            try
            {
                var storage = await persistentStorageService.GetStorageAsync(SolutionKey.ToSolutionKey(solution), cancellationToken).ConfigureAwait(false);
                await using var _ = storage.ConfigureAwait(false);
                // Check if we've already stored a checksum and it matches the checksum we 
                // expect.  If so, we're already precalculated and don't have to recompute
                // this index.  Otherwise if we don't have a checksum, or the checksums don't
                // match, go ahead and recompute it.
                return await storage.ChecksumMatchesAsync(document, s_persistenceName, checksum, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception e) when (IOUtilities.IsNormalIOException(e))
            {
                // Storage APIs can throw arbitrary exceptions.
            }

            return false;
        }

        bool IObjectWritable.ShouldReuseInSerialization => true;

        public abstract void WriteTo(ObjectWriter writer);
    }
}
