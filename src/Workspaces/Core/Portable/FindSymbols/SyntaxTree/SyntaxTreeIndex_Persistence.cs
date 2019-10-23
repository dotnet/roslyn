// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Serialization;
using Microsoft.CodeAnalysis.Shared.Utilities;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.FindSymbols
{
    internal sealed partial class SyntaxTreeIndex : IObjectWritable
    {
        private const string PersistenceName = "<SyntaxTreeIndex>";
        private static readonly Checksum SerializationFormatChecksum = Checksum.Create("17");

        public readonly Checksum Checksum;

        private static async Task<SyntaxTreeIndex> LoadAsync(
            Document document, Checksum checksum, CancellationToken cancellationToken)
        {
            var solution = document.Project.Solution;
            var persistentStorageService = (IChecksummedPersistentStorageService)solution.Workspace.Services.GetService<IPersistentStorageService>();

            try
            {
                // attempt to load from persisted state
                using var storage = persistentStorageService.GetStorage(solution, checkBranchId: false);
                using var stream = await storage.ReadStreamAsync(document, PersistenceName, checksum, cancellationToken).ConfigureAwait(false);
                using var reader = ObjectReader.TryGetReader(stream);
                if (reader != null)
                {
                    return ReadFrom(GetStringTable(document.Project), reader, checksum);
                }
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
            var projectChecksumState = await document.Project.State.GetStateChecksumsAsync(cancellationToken).ConfigureAwait(false);
            var parseOptionsChecksum = projectChecksumState.ParseOptions;

            var documentChecksumState = await document.State.GetStateChecksumsAsync(cancellationToken).ConfigureAwait(false);
            var textChecksum = documentChecksumState.Text;

            return Checksum.Create(
                WellKnownSynchronizationKind.SyntaxTreeIndex,
                new[] { textChecksum, parseOptionsChecksum, SerializationFormatChecksum });
        }

        private async Task<bool> SaveAsync(
            Document document, CancellationToken cancellationToken)
        {
            var solution = document.Project.Solution;
            var persistentStorageService = (IChecksummedPersistentStorageService)solution.Workspace.Services.GetService<IPersistentStorageService>();

            try
            {
                using var storage = persistentStorageService.GetStorage(solution, checkBranchId: false);
                using var stream = SerializableBytes.CreateWritableStream();
                using var writer = new ObjectWriter(stream, cancellationToken: cancellationToken);

                this.WriteTo(writer);

                stream.Position = 0;
                return await storage.WriteStreamAsync(document, PersistenceName, stream, this.Checksum, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception e) when (IOUtilities.IsNormalIOException(e))
            {
                // Storage APIs can throw arbitrary exceptions.
            }

            return false;
        }

        private static async Task<bool> PrecalculatedAsync(
            Document document, Checksum checksum, CancellationToken cancellationToken)
        {
            var solution = document.Project.Solution;
            var persistentStorageService = (IChecksummedPersistentStorageService)solution.Workspace.Services.GetService<IPersistentStorageService>();

            // check whether we already have info for this document
            try
            {
                using var storage = persistentStorageService.GetStorage(solution, checkBranchId: false);
                // Check if we've already stored a checksum and it matches the checksum we 
                // expect.  If so, we're already precalculated and don't have to recompute
                // this index.  Otherwise if we don't have a checksum, or the checksums don't
                // match, go ahead and recompute it.
                var persistedChecksum = await storage.ReadChecksumAsync(document, PersistenceName, cancellationToken).ConfigureAwait(false);
                return persistedChecksum == checksum;
            }
            catch (Exception e) when (IOUtilities.IsNormalIOException(e))
            {
                // Storage APIs can throw arbitrary exceptions.
            }

            return false;
        }

        bool IObjectWritable.ShouldReuseInSerialization => true;

        public void WriteTo(ObjectWriter writer)
        {
            _literalInfo.WriteTo(writer);
            _identifierInfo.WriteTo(writer);
            _contextInfo.WriteTo(writer);
            _declarationInfo.WriteTo(writer);
            _extensionMethodInfo.WriteTo(writer);
        }

        private static SyntaxTreeIndex ReadFrom(
            StringTable stringTable, ObjectReader reader, Checksum checksum)
        {
            var literalInfo = LiteralInfo.TryReadFrom(reader);
            var identifierInfo = IdentifierInfo.TryReadFrom(reader);
            var contextInfo = ContextInfo.TryReadFrom(reader);
            var declarationInfo = DeclarationInfo.TryReadFrom(stringTable, reader);
            var extensionMethodInfo = ExtensionMethodInfo.TryReadFrom(reader);

            if (literalInfo == null || identifierInfo == null || contextInfo == null || declarationInfo == null || extensionMethodInfo == null)
            {
                return null;
            }

            return new SyntaxTreeIndex(
                checksum, literalInfo.Value, identifierInfo.Value, contextInfo.Value, declarationInfo.Value, extensionMethodInfo.Value);
        }
    }
}
