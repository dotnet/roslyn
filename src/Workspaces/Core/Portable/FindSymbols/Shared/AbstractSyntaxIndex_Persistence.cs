// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
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

        /// <summary>
        /// Cache of ParseOptions to a checksum for the <see cref="ParseOptions.PreprocessorSymbolNames"/> contained
        /// within.  Useful so we don't have to continually reenumerate and regenerate the checksum given how rarely
        /// these ever change.
        /// </summary>
        private static readonly ConditionalWeakTable<ParseOptions, Checksum> s_ppDirectivesToChecksum = new();

        public readonly Checksum? Checksum;

        public static int PrecalculatedCount;
        public static int ComputedCount;

        protected static async Task<TIndex?> LoadAsync(
            Document document,
            Checksum textChecksum,
            Checksum textAndDirectivesChecksum,
            IndexReader read,
            CancellationToken cancellationToken)
        {
            var storageService = document.Project.Solution.Workspace.Services.GetPersistentStorageService();
            var documentKey = DocumentKey.ToDocumentKey(document);
            var stringTable = SyntaxTreeIndex.GetStringTable(document.Project);

            // Try to read from the DB using either checksum.  If the writer determined there were no pp-directives,
            // then we may match it using textChecksum.  If there were pp directives, then we may match is using
            // textAndDirectivesChecksum.  if we match neither that means that either the data is not in the persistence
            // service, or it was written against genuinely different doc/pp-directive contents than before and we have
            // to recompute and store again.
            //
            // This does mean we have to potentially do two reads here.  However, that is cheap, and still nicer than
            // trying to produce the index again in the common case where we don't have to.
            return await LoadAsync(storageService, documentKey, textChecksum, stringTable, read, cancellationToken).ConfigureAwait(false) ??
                   await LoadAsync(storageService, documentKey, textAndDirectivesChecksum, stringTable, read, cancellationToken).ConfigureAwait(false);
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

        public static async ValueTask<(Checksum textOnlyChecksum, Checksum textAndDirectivesChecksum)> GetChecksumsAsync(
            Document document, CancellationToken cancellationToken)
        {
            // Since we build the SyntaxTreeIndex from a SyntaxTree, we need our checksum to change any time the
            // SyntaxTree could have changed.  Right now, that can only happen if the text of the document changes, or
            // the preprocessor directives change.  So we get the checksums for both of those, and merge them together
            // to make the final checksum.
            //
            // Note: this intentionally ignores *other* ParseOption changes.  This may look like it could cause us to
            // get innacurate results, but here's why it's ok.  The other ParseOption changes include:
            //
            //  1. LanguageVersion changes.  It's ok to ignore that as for practically all language versions we don't
            //     produce different trees.  And, while there are some lang versions that produce different trees (for
            //     example around how records are parsed), it's not realistic that the user would somehow have a
            //     document that did *not* include pp directives, which somehow had one of those constructs *and*
            //     somehow had the code produce different trees across different language versions.  e.g. no code out
            //     there is realistically depending on `record` parsing as a method in C# X and as an actual record in
            //     C# X+1.  If code is using constructs that are actually parsing differently downlevel, they will have
            //     pp directives to avoid even using that construct downlevel.
            //
            //  2. DocComment parsing mode changes. However, in the IDE we always at least parse doc comments (though we
            //     have options to control if we report errors in it or not).  Since we're always parsing, we're always
            //     at least getting the same syntax tree shape at the end of the day.
            //
            // We also want the checksum to change any time our serialization format changes.  If the format has
            // changed, all previous versions should be invalidated.
            var project = document.Project;

            var documentChecksumState = await document.State.GetStateChecksumsAsync(cancellationToken).ConfigureAwait(false);

            var directivesChecksum = s_ppDirectivesToChecksum.GetValue(
                project.ParseOptions!,
                static parseOptions => Checksum.Create(parseOptions.PreprocessorSymbolNames));

            var textChecksum = Checksum.Create(documentChecksumState.Text, s_serializationFormatChecksum);
            var textAndDirectivesChecksum = Checksum.Create(textChecksum, directivesChecksum);

            return (textChecksum, textAndDirectivesChecksum);
        }

        private async Task<bool> SaveAsync(
            Document document, CancellationToken cancellationToken)
        {
            var solution = document.Project.Solution;
            var persistentStorageService = solution.Workspace.Services.GetPersistentStorageService();

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

                var (textChecksum, textAndDirectivesChecksum) = await GetChecksumsAsync(document, cancellationToken).ConfigureAwait(false);

                // Check if we've already created and persisted the index for this document.
                if (await PrecalculatedAsync(document, textChecksum, textAndDirectivesChecksum, cancellationToken).ConfigureAwait(false))
                {
                    PrecalculatedCount++;
                    return;
                }

                using (Logger.LogBlock(FunctionId.SyntaxTreeIndex_Precalculate_Create, cancellationToken))
                {
                    // If not, create and save the index.
                    var data = await CreateIndexAsync(document, textChecksum, textAndDirectivesChecksum, create, cancellationToken).ConfigureAwait(false);
                    await data.SaveAsync(document, cancellationToken).ConfigureAwait(false);
                    ComputedCount++;
                }
            }
        }

        private static async Task<bool> PrecalculatedAsync(
            Document document, Checksum textChecksum, Checksum textAndDirectivesChecksum, CancellationToken cancellationToken)
        {
            var solution = document.Project.Solution;
            var persistentStorageService = solution.Workspace.Services.GetPersistentStorageService();

            // check whether we already have info for this document
            try
            {
                var storage = await persistentStorageService.GetStorageAsync(SolutionKey.ToSolutionKey(solution), cancellationToken).ConfigureAwait(false);
                await using var _ = storage.ConfigureAwait(false);

                // Check if we've already stored a checksum and it matches the checksum we expect.  If so, we're already
                // precalculated and don't have to recompute this index.  Otherwise if we don't have a checksum, or the
                // checksums don't match, go ahead and recompute it.
                //
                // Check with both checksums as we don't know at this reading point if the document has pp-directives in
                // it or not, and we don't want parse the document to find out.
                return await storage.ChecksumMatchesAsync(document, s_persistenceName, textChecksum, cancellationToken).ConfigureAwait(false) ||
                       await storage.ChecksumMatchesAsync(document, s_persistenceName, textAndDirectivesChecksum, cancellationToken).ConfigureAwait(false);
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
