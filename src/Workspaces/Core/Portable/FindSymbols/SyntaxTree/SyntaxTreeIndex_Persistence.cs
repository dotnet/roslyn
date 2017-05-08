﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
        private const string SerializationFormat = "9";

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
            Document document, Checksum checksum, CancellationToken cancellationToken)
        {
            var solution = document.Project.Solution;
            var persistentStorageService = (IPersistentStorageService2)solution.Workspace.Services.GetService<IPersistentStorageService>();

            try
            {
                // attempt to load from persisted state
                using (var storage = persistentStorageService.GetStorage(solution, checkBranchId: false))
                using (var stream = await storage.ReadStreamAsync(document, PersistenceName, cancellationToken).ConfigureAwait(false))
                using (var reader = ObjectReader.TryGetReader(stream))
                {
                    if (reader != null)
                    {
                        if (FormatAndChecksumMatches(reader, SerializationFormat, checksum))
                        {
                            return ReadFrom(reader, checksum);
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

        private static bool FormatAndChecksumMatches(
            ObjectReader reader, string formatVersion, Checksum checksum)
        {
            return TryReadFormatAndChecksum(reader, formatVersion, out var persistChecksum) &&
                   persistChecksum == checksum;
        }

        public static async Task<Checksum> GetChecksumAsync(
            Document document, CancellationToken cancellationToken)
        {
            // Since we build the SyntaxTreeIndex from a SyntaxTree, we need our checksum to change
            // any time the SyntaxTree could have changed.  Right now, that can only happen if the
            // text of the document changes, or the ParseOptions change.  So we get the checksums
            // for both of those, and merge them together to make the final checksum.

            var documentChecksumState = await document.State.GetStateChecksumsAsync(cancellationToken).ConfigureAwait(false);
            var textChecksum = documentChecksumState.Text;

            var parseOptions = document.Project.ParseOptions;
            var serializer = new Serializer(document.Project.Solution.Workspace);
            var parseOptionsChecksum = ChecksumCache.GetOrCreate(
                parseOptions, _ => serializer.CreateChecksum(parseOptions, cancellationToken));

            return Checksum.Create(nameof(SyntaxTreeIndex), new[] { textChecksum, parseOptionsChecksum });
        }

        private async Task<bool> SaveAsync(
            Document document, CancellationToken cancellationToken)
        {
            var solution = document.Project.Solution;
            var persistentStorageService = (IPersistentStorageService2)solution.Workspace.Services.GetService<IPersistentStorageService>();

            try
            {
                using (var storage = persistentStorageService.GetStorage(solution, checkBranchId: false))
                using (var stream = SerializableBytes.CreateWritableStream())
                using (var writer = new ObjectWriter(stream, cancellationToken: cancellationToken))
                {
                    this.WriteFormatAndChecksum(writer, SerializationFormat);
                    this.WriteTo(writer);

                    stream.Position = 0;
                    return await storage.WriteStreamAsync(document, PersistenceName, stream, cancellationToken).ConfigureAwait(false);
                }
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
            var persistentStorageService = (IPersistentStorageService2)solution.Workspace.Services.GetService<IPersistentStorageService>();

            // check whether we already have info for this document
            try
            {
                using (var storage = persistentStorageService.GetStorage(solution, checkBranchId: false))
                using (var stream = await storage.ReadStreamAsync(document, PersistenceName, cancellationToken).ConfigureAwait(false))
                using (var reader = ObjectReader.TryGetReader(stream))
                {
                    if (reader != null)
                    {
                        return FormatAndChecksumMatches(reader, SerializationFormat, checksum);
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
    }
}