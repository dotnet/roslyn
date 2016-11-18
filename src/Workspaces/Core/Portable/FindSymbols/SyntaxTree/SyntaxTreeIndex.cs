// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.FindSymbols
{
    internal sealed partial class SyntaxTreeIndex : AbstractPersistableState, IObjectWritable
    {
        private const string PersistenceName = "<TreeInfoPersistence>";
        private const string SerializationFormat = "1";

        /// <summary>
        /// in memory cache will hold onto any info related to opened documents in primary branch or all documents in forked branch
        /// 
        /// this is not snapshot based so multiple versions of snapshots can re-use same data as long as it is relevant.
        /// </summary>
        private static readonly ConditionalWeakTable<BranchId, ConditionalWeakTable<DocumentId, SyntaxTreeIndex>> s_cache =
            new ConditionalWeakTable<BranchId, ConditionalWeakTable<DocumentId, SyntaxTreeIndex>>();

        private readonly VersionStamp _version;

        private readonly IdentifierInfo _identifierInfo;
        private readonly ContextInfo _contextInfo;
        private readonly DeclarationInfo _declarationInfo;

        private SyntaxTreeIndex(
            VersionStamp version,
            IdentifierInfo identifierInfo,
            ContextInfo contextInfo,
            DeclarationInfo declarationInfo)
            : base(version)
        {
            _identifierInfo = identifierInfo;
            _contextInfo = contextInfo;
            _declarationInfo = declarationInfo;
        }

        public void WriteTo(ObjectWriter writer)
        {
            _identifierInfo.WriteTo(writer);
            _contextInfo.WriteTo(writer);
            _declarationInfo.WriteTo(writer);
        }

        private static SyntaxTreeIndex ReadFrom(ObjectReader reader, VersionStamp version)
        {
            var identifierInfo = IdentifierInfo.ReadFrom(reader);
            var contextInfo = ContextInfo.ReadFrom(reader);
            var declarationInfo = DeclarationInfo.ReadFrom(reader);

            if (identifierInfo == null || contextInfo == null || declarationInfo == null)
            {
                return null;
            }

            return new SyntaxTreeIndex(
                version, identifierInfo.Value, contextInfo.Value, declarationInfo.Value);
        }

        public Task<bool> SaveAsync(Document document, CancellationToken cancellationToken)
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

        public static Task<SyntaxTreeIndex> LoadAsync(Document document, CancellationToken cancellationToken)
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
            SyntaxTreeIndex info;
            if (infoTable.TryGetValue(document.Id, out info) && info.Version == version)
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

        public static Task<bool> PrecalculatedAsync(Document document, CancellationToken cancellationToken)
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

                        ConditionalWeakTable<DocumentId, SyntaxTreeIndex> infoTable;
                        if (cache.TryGetValue(e.Document.Project.Solution.BranchId, out infoTable))
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