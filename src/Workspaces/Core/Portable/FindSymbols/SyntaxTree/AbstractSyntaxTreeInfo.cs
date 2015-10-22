// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.FindSymbols
{
    internal abstract class AbstractSyntaxTreeInfo : AbstractPersistableState, IObjectWritable
    {
        protected AbstractSyntaxTreeInfo(VersionStamp version)
            : base(version)
        {
        }

        public abstract void WriteTo(ObjectWriter writer);

        public abstract Task<bool> SaveAsync(Document document, CancellationToken cancellationToken);

        protected async Task<bool> SaveAsync(
            Document document,
            ConditionalWeakTable<BranchId, ConditionalWeakTable<DocumentId, AbstractSyntaxTreeInfo>> cache,
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

        protected static async Task<AbstractSyntaxTreeInfo> LoadAsync(
            Document document,
            Func<ObjectReader, VersionStamp, AbstractSyntaxTreeInfo> reader,
            ConditionalWeakTable<BranchId, ConditionalWeakTable<DocumentId, AbstractSyntaxTreeInfo>> cache,
            string persistenceName,
            string serializationFormat,
            CancellationToken cancellationToken)
        {
            var infoTable = cache.GetValue(document.Project.Solution.BranchId, _ => new ConditionalWeakTable<DocumentId, AbstractSyntaxTreeInfo>());
            var version = await document.GetSyntaxVersionAsync(cancellationToken).ConfigureAwait(false);

            // first look to see if we already have the info in the cache
            AbstractSyntaxTreeInfo info;
            if (infoTable.TryGetValue(document.Id, out info) && info.Version == version)
            {
                return info;
            }

            // cache is invalid. remove it
            infoTable.Remove(document.Id);

            // check primary cache to see whether we have valid info there
            var primaryInfoTable = cache.GetValue(document.Project.Solution.Workspace.PrimaryBranchId, _ => new ConditionalWeakTable<DocumentId, AbstractSyntaxTreeInfo>());
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

        private static ConditionalWeakTable<DocumentId, AbstractSyntaxTreeInfo> GetInfoTable(
            BranchId branchId,
            Workspace workspace,
            ConditionalWeakTable<BranchId, ConditionalWeakTable<DocumentId, AbstractSyntaxTreeInfo>> cache)
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

                        ConditionalWeakTable<DocumentId, AbstractSyntaxTreeInfo> infoTable;
                        if (cache.TryGetValue(e.Document.Project.Solution.BranchId, out infoTable))
                        {
                            // remove closed document from primary branch from live cache.
                            infoTable.Remove(e.Document.Id);
                        }
                    };
                }

                return new ConditionalWeakTable<DocumentId, AbstractSyntaxTreeInfo>();
            });
        }
    }
}
