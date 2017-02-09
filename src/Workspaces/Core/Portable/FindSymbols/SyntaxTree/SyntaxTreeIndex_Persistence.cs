// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Versions;
using Microsoft.CodeAnalysis.Shared.Utilities;

namespace Microsoft.CodeAnalysis.FindSymbols
{
    internal sealed partial class SyntaxTreeIndex : IObjectWritable
    {
        private const string PersistenceName = "<TreeInfoPersistence>";
        private const string SerializationFormat = "2";

        /// <summary>
        /// in memory cache will hold onto any info related to opened documents in primary branch or all documents in forked branch
        /// 
        /// this is not snapshot based so multiple versions of snapshots can re-use same data as long as it is relevant.
        /// </summary>
        private static readonly ConditionalWeakTable<BranchId, ConditionalWeakTable<DocumentId, SyntaxTreeIndex>> s_cache =
            new ConditionalWeakTable<BranchId, ConditionalWeakTable<DocumentId, SyntaxTreeIndex>>();

        public readonly VersionStamp Version;

        private void WriteVersion(ObjectWriter writer, string formatVersion)
        {
            writer.WriteString(formatVersion);
            this.Version.WriteTo(writer);
        }

        private static bool TryReadVersion(ObjectReader reader, string formatVersion, out VersionStamp version)
        {
            version = VersionStamp.Default;
            if (reader.ReadString() != formatVersion)
            {
                return false;
            }

            version = VersionStamp.ReadFrom(reader);
            return true;
        }

        private static async Task<SyntaxTreeIndex> LoadAsync(
            Document document, string persistenceName, string formatVersion,
            Func<ObjectReader, VersionStamp, SyntaxTreeIndex> readFrom, CancellationToken cancellationToken)
        {
            var persistentStorageService = document.Project.Solution.Workspace.Services.GetService<IPersistentStorageService>();
            var syntaxVersion = await document.GetSyntaxVersionAsync(cancellationToken).ConfigureAwait(false);

            try
            {
                // attempt to load from persisted state
                using (var storage = persistentStorageService.GetStorage(document.Project.Solution))
                using (var stream = await storage.ReadStreamAsync(document, persistenceName, cancellationToken).ConfigureAwait(false))
                using (var reader = StreamObjectReader.TryGetReader(stream))
                {
                    if (reader != null)
                    {
                        if (TryReadVersion(reader, formatVersion, out var persistVersion) &&
                            document.CanReusePersistedSyntaxTreeVersion(syntaxVersion, persistVersion))
                        {
                            return readFrom(reader, syntaxVersion);
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

        private static async Task<bool> SaveAsync(
            Document document, string persistenceName, string formatVersion, SyntaxTreeIndex data, CancellationToken cancellationToken)
        {
            Contract.Requires(!await document.IsForkedDocumentWithSyntaxChangesAsync(cancellationToken).ConfigureAwait(false));

            var persistentStorageService = document.Project.Solution.Workspace.Services.GetService<IPersistentStorageService>();

            try
            {
                using (var storage = persistentStorageService.GetStorage(document.Project.Solution))
                using (var stream = SerializableBytes.CreateWritableStream())
                using (var writer = new StreamObjectWriter(stream, cancellationToken: cancellationToken))
                {
                    data.WriteVersion(writer, formatVersion);
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

        private static async Task<bool> PrecalculatedAsync(Document document, string persistenceName, string formatVersion, CancellationToken cancellationToken)
        {
            Contract.Requires(document.IsFromPrimaryBranch());

            var persistentStorageService = document.Project.Solution.Workspace.Services.GetService<IPersistentStorageService>();
            var syntaxVersion = await document.GetSyntaxVersionAsync(cancellationToken).ConfigureAwait(false);

            // check whether we already have info for this document
            try
            {
                using (var storage = persistentStorageService.GetStorage(document.Project.Solution))
                using (var stream = await storage.ReadStreamAsync(document, persistenceName, cancellationToken).ConfigureAwait(false))
                using (var reader = StreamObjectReader.TryGetReader(stream))
                {
                    if (reader != null)
                    {
                        return TryReadVersion(reader, formatVersion, out var persistVersion) &&
                               document.CanReusePersistedSyntaxTreeVersion(syntaxVersion, persistVersion);
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
            _identifierInfo.WriteTo(writer);
            _contextInfo.WriteTo(writer);
            _declarationInfo.WriteTo(writer);
        }

        private static SyntaxTreeIndex ReadFrom(ObjectReader reader, VersionStamp version)
        {
            var identifierInfo = IdentifierInfo.TryReadFrom(reader);
            var contextInfo = ContextInfo.TryReadFrom(reader);
            var declarationInfo = DeclarationInfo.TryReadFrom(reader);

            if (identifierInfo == null || contextInfo == null || declarationInfo == null)
            {
                return null;
            }

            return new SyntaxTreeIndex(
                version, identifierInfo.Value, contextInfo.Value, declarationInfo.Value);
        }

        private Task<bool> SaveAsync(Document document, CancellationToken cancellationToken)
            => SaveAsync(document, s_cache, PersistenceName, SerializationFormat, cancellationToken);

        private async Task<bool> SaveAsync(
            Document document,
            ConditionalWeakTable<BranchId, ConditionalWeakTable<DocumentId, SyntaxTreeIndex>> cache,
            string persistenceName,
            string serializationFormat,
            CancellationToken cancellationToken)
        {
            var workspace = document.Project.Solution.Workspace;
            var infoTable = GetInfoTable(document.Project.Solution.BranchId, workspace, cache);

            // if it is forked document
            if (await document.IsForkedDocumentWithSyntaxChangesAsync(cancellationToken).ConfigureAwait(false))
            {
                infoTable.Remove(document.Id);
                infoTable.GetValue(document.Id, _ => this);
                return false;
            }

            // okay, cache this info if it is from opened document or persistence failed.
            var persisted = await SaveAsync(document, persistenceName, serializationFormat, this, cancellationToken).ConfigureAwait(false);
            if (!persisted || document.IsOpen())
            {
                var primaryInfoTable = GetInfoTable(workspace.PrimaryBranchId, workspace, cache);
                primaryInfoTable.Remove(document.Id);
                primaryInfoTable.GetValue(document.Id, _ => this);
            }

            return persisted;
        }

        private static Task<SyntaxTreeIndex> LoadAsync(Document document, CancellationToken cancellationToken)
            => LoadAsync(document, ReadFrom, s_cache, PersistenceName, SerializationFormat, cancellationToken);

        private static async Task<SyntaxTreeIndex> LoadAsync(
            Document document,
            Func<ObjectReader, VersionStamp, SyntaxTreeIndex> reader,
            ConditionalWeakTable<BranchId, ConditionalWeakTable<DocumentId, SyntaxTreeIndex>> cache,
            string persistenceName,
            string serializationFormat,
            CancellationToken cancellationToken)
        {
            var infoTable = cache.GetValue(
                document.Project.Solution.BranchId, 
                _ => new ConditionalWeakTable<DocumentId, SyntaxTreeIndex>());
            var version = await document.GetSyntaxVersionAsync(cancellationToken).ConfigureAwait(false);
            // first look to see if we already have the info in the cache
            if (infoTable.TryGetValue(document.Id, out var info) && info.Version == version)
            {
                return info;
            }

            // cache is invalid. remove it
            infoTable.Remove(document.Id);

            // check primary cache to see whether we have valid info there
            var primaryInfoTable = cache.GetValue(
                document.Project.Solution.Workspace.PrimaryBranchId,
                _ => new ConditionalWeakTable<DocumentId, SyntaxTreeIndex>());
            if (primaryInfoTable.TryGetValue(document.Id, out info) && info.Version == version)
            {
                return info;
            }

            // check whether we can get it from persistence service
            info = await LoadAsync(document, persistenceName, serializationFormat, reader, cancellationToken).ConfigureAwait(false);
            if (info != null)
            {
                // save it in the cache. persisted info is always from primary branch. no reason to save it to the branched document cache.
                primaryInfoTable.Remove(document.Id);
                primaryInfoTable.GetValue(document.Id, _ => info);
                return info;
            }

            // well, we don't have this information.
            return null;
        }

        private static Task<bool> PrecalculatedAsync(Document document, CancellationToken cancellationToken)
            => PrecalculatedAsync(document, PersistenceName, SerializationFormat, cancellationToken);

        private static ConditionalWeakTable<DocumentId, SyntaxTreeIndex> GetInfoTable(
            BranchId branchId,
            Workspace workspace,
            ConditionalWeakTable<BranchId, ConditionalWeakTable<DocumentId, SyntaxTreeIndex>> cache)
        {
            return cache.GetValue(branchId, id =>
            {
                if (id == workspace.PrimaryBranchId)
                {
                    workspace.DocumentClosed += (sender, e) =>
                    {
                        if (!e.Document.IsFromPrimaryBranch())
                        {
                            return;
                        }

                        if (cache.TryGetValue(e.Document.Project.Solution.BranchId, out var infoTable))
                        {
                            // remove closed document from primary branch from live cache.
                            infoTable.Remove(e.Document.Id);
                        }
                    };
                }

                return new ConditionalWeakTable<DocumentId, SyntaxTreeIndex>();
            });
        }
    }
}