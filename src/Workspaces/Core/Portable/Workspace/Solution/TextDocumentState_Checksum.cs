// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.Serialization;
using Microsoft.CodeAnalysis.Shared.Utilities;
using Microsoft.CodeAnalysis.Versions;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis
{
    internal partial class TextDocumentState
    {
        private const string TextChecksum = nameof(TextChecksum);
        private const string SerializationFormat = "1";

        public bool TryGetStateChecksums(out DocumentStateChecksums stateChecksums)
        {
            return _lazyChecksums.TryGetValue(out stateChecksums);
        }

        public Task<DocumentStateChecksums> GetStateChecksumsAsync(CancellationToken cancellationToken)
        {
            return _lazyChecksums.GetValueAsync(cancellationToken);
        }

        public async Task<Checksum> GetChecksumAsync(CancellationToken cancellationToken)
        {
            var collection = await _lazyChecksums.GetValueAsync(cancellationToken).ConfigureAwait(false);
            return collection.Checksum;
        }

        private async Task<DocumentStateChecksums> ComputeChecksumsAsync(CancellationToken cancellationToken)
        {
            using (Logger.LogBlock(FunctionId.DocumentState_ComputeChecksumsAsync, FilePath, cancellationToken))
            {
                var textAndVersionTask = GetTextAndVersionAsync(cancellationToken);

                var serializer = new Serializer(solutionServices.Workspace);

                var infoChecksum = serializer.CreateChecksum(Info.Attributes, cancellationToken);
                var textChecksum = await GetTextChecksumAsync(serializer, await textAndVersionTask.ConfigureAwait(false), cancellationToken).ConfigureAwait(false);

                return new DocumentStateChecksums(infoChecksum, textChecksum);
            }
        }

        private async Task<Checksum> GetTextChecksumAsync(Serializer serializer, TextAndVersion textAndVersion, CancellationToken cancellationToken)
        {
            // calculating checksum for source text is one of most expansive checksum calculation we need to do.
            // this should let us get around it if possible
            var solution = solutionServices.Workspace.CurrentSolution;

            var document = solution.GetDocument(Id);
            if (document == null)
            {
                // can't use persistent service since it is based on solution objects
                return serializer.CreateChecksum(textAndVersion.Text, cancellationToken);
            }

            var storage = solution.Workspace.Services.GetService<IPersistentStorageService>()?.GetStorage(solution);
            if (storage == null)
            {
                // persistent service not available
                return serializer.CreateChecksum(textAndVersion.Text, cancellationToken);
            }

            try
            {
                using (var stream = await storage.ReadStreamAsync(document, TextChecksum, cancellationToken).ConfigureAwait(false))
                using (var reader = ObjectReader.TryGetReader(stream))
                {
                    if (TryReadVersion(reader, out var persistedVersion) &&
                        document.CanReusePersistedTextVersion(textAndVersion.Version, persistedVersion))
                    {
                        return Checksum.ReadFrom(reader);
                    }
                }

                // either checksum doesn't exist or can't reuse. re-calculate the checksum
                var checksum = serializer.CreateChecksum(textAndVersion.Text, cancellationToken);

                // save newly calculated checksum
                using (var stream = SerializableBytes.CreateWritableStream())
                using (var writer = new ObjectWriter(stream, cancellationToken))
                {
                    WriteVersionsTo(writer, textAndVersion.Version);
                    checksum.WriteTo(writer);

                    stream.Position = 0;
                    await storage.WriteStreamAsync(document, TextChecksum, stream, cancellationToken).ConfigureAwait(false);
                }

                return checksum;
            }
            catch (Exception e) when (IOUtilities.IsNormalIOException(e))
            {
                // Storage APIs can throw arbitrary exceptions.
            }

            // go simple route if persistent thing didn't work out
            return serializer.CreateChecksum(textAndVersion.Text, cancellationToken);
        }

        private void WriteVersionsTo(ObjectWriter writer, VersionStamp version)
        {
            writer.WriteString(SerializationFormat);
            version.WriteTo(writer); ;
        }

        private static bool TryReadVersion(ObjectReader reader, out VersionStamp persistedVersion)
        {
            persistedVersion = VersionStamp.Default;
            if (reader?.ReadString() != SerializationFormat)
            {
                return false;
            }

            persistedVersion = VersionStamp.ReadFrom(reader);
            return true;
        }
    }
}
