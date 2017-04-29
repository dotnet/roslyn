// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
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
        private const string SerializationFormat = "8";

        public readonly Checksum Checksum;

        private void WriteFormatAndChecksum(ObjectWriter writer, string formatVersion)
        {
            writer.WriteString(formatVersion);
            Checksum.WriteTo(writer);
        }

        private static bool TryReadFormatAndChecksum(
            ObjectReader reader, string formatVersion, out Checksum checksum)
        {
            checksum = null;
            if (reader.ReadString() != formatVersion)
            {
                return false;
            }

            checksum = Checksum.ReadFrom(reader);
            return true;
        }

        private static async Task<SyntaxTreeIndex> LoadAsync(
            Document document, string persistenceName, string formatVersion,
            Func<ObjectReader, Checksum, SyntaxTreeIndex> readFrom, CancellationToken cancellationToken)
        {
            var solution = document.Project.Solution;
            var persistentStorageService = (IPersistentStorageService2)solution.Workspace.Services.GetService<IPersistentStorageService>();

            var checksum = await GetChecksumAsync(document, cancellationToken).ConfigureAwait(false);

            try
            {
                // attempt to load from persisted state
                using (var storage = persistentStorageService.GetStorage(solution, checkBranchId: false))
                using (var stream = await storage.ReadStreamAsync(document, persistenceName, cancellationToken).ConfigureAwait(false))
                using (var reader = ObjectReader.TryGetReader(stream))
                {
                    if (reader != null)
                    {
                        if (DataPreambleMatches(reader, formatVersion, checksum))
                        {
                            return readFrom(reader, checksum);
                        }
                    }
                }
            }
            catch (Exception e) when (IOUtilities.IsNormalIOException(e))
            {
                // Storage APIs can throw arbitrary exceptions.
            }

            return null;
        }

        private static bool DataPreambleMatches(
            ObjectReader reader, string formatVersion, Checksum checksum)
        {
            return TryReadFormatAndChecksum(reader, formatVersion, out var persistChecksum) &&
                   persistChecksum == checksum;
        }

        public static async Task<Checksum> GetChecksumAsync(
            Document document, CancellationToken cancellationToken)
        {
            var text = await document.GetTextAsync(cancellationToken).ConfigureAwait(false);
            var textChecksum = Checksum.Create(WellKnownSynchronizationKinds.SourceText, text.GetChecksum());

            var parseOptions = document.Project.ParseOptions;

            var serializer = new Serializer(document.Project.Solution.Workspace);
            var parseOptionsChecksum = ChecksumCache.GetOrCreate(
                parseOptions, _ => serializer.CreateChecksum(parseOptions, cancellationToken));

            return Checksum.Create(nameof(SyntaxTreeIndex), new[] { textChecksum, parseOptionsChecksum });
        }

        private static async Task<bool> SaveAsync(
            Document document, string persistenceName, string formatVersion, SyntaxTreeIndex data, CancellationToken cancellationToken)
        {
            var solution = document.Project.Solution;
            var persistentStorageService = (IPersistentStorageService2)solution.Workspace.Services.GetService<IPersistentStorageService>();
            var checksum = await GetChecksumAsync(document, cancellationToken).ConfigureAwait(false);

            try
            {
                using (var storage = persistentStorageService.GetStorage(solution, checkBranchId: false))
                using (var stream = SerializableBytes.CreateWritableStream())
                using (var writer = new ObjectWriter(stream, cancellationToken: cancellationToken))
                {
                    data.WriteFormatAndChecksum(writer, formatVersion);
                    data.WriteTo(writer);

                    stream.Position = 0;
                    return await storage.WriteStreamAsync(document, persistenceName, stream, cancellationToken).ConfigureAwait(false);
                }
            }
            catch (Exception e) when (IOUtilities.IsNormalIOException(e))
            {
                // Storage APIs can throw arbitrary exceptions.
            }

            return false;
        }

        private static async Task<bool> PrecalculatedAsync(
            Document document, string persistenceName, string formatVersion, CancellationToken cancellationToken)
        {
            var solution = document.Project.Solution;
            var persistentStorageService = (IPersistentStorageService2)solution.Workspace.Services.GetService<IPersistentStorageService>();
            var checksum = await GetChecksumAsync(document, cancellationToken).ConfigureAwait(false);

            // check whether we already have info for this document
            try
            {
                using (var storage = persistentStorageService.GetStorage(solution, checkBranchId: false))
                using (var stream = await storage.ReadStreamAsync(document, persistenceName, cancellationToken).ConfigureAwait(false))
                using (var reader = ObjectReader.TryGetReader(stream))
                {
                    if (reader != null)
                    {
                        return DataPreambleMatches(reader, formatVersion, checksum);
                    }
                }
            }
            catch (Exception e) when (IOUtilities.IsNormalIOException(e))
            {
                // Storage APIs can throw arbitrary exceptions.
            }

            return false;
        }

        public void WriteTo(ObjectWriter writer)
        {
            _literalInfo.WriteTo(writer);
            _identifierInfo.WriteTo(writer);
            _contextInfo.WriteTo(writer);
            _declarationInfo.WriteTo(writer);
        }

        private static SyntaxTreeIndex ReadFrom(
            ObjectReader reader, Checksum checksum)
        {
            var literalInfo = LiteralInfo.TryReadFrom(reader);
            var identifierInfo = IdentifierInfo.TryReadFrom(reader);
            var contextInfo = ContextInfo.TryReadFrom(reader);
            var declarationInfo = DeclarationInfo.TryReadFrom(reader);

            if (literalInfo == null || identifierInfo == null || contextInfo == null || declarationInfo == null)
            {
                return null;
            }

            return new SyntaxTreeIndex(
                checksum, literalInfo.Value, identifierInfo.Value, contextInfo.Value, declarationInfo.Value);
        }

        private Task<bool> SaveAsync(Document document, CancellationToken cancellationToken)
            => SaveAsync(document, PersistenceName, SerializationFormat, this, cancellationToken);

        private static Task<SyntaxTreeIndex> LoadAsync(Document document, CancellationToken cancellationToken)
            => LoadAsync(document, PersistenceName, SerializationFormat, ReadFrom, cancellationToken);

        private static Task<bool> PrecalculatedAsync(Document document, CancellationToken cancellationToken)
            => PrecalculatedAsync(document, PersistenceName, SerializationFormat, cancellationToken);
    }
}