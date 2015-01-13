// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.Utilities;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.FindSymbols
{
    internal partial class SyntaxTreeIdentifierInfo : AbstractPersistableState, IObjectWritable
    {
        private const string PersistenceName = "<SyntaxTreeInfoIdentifierPersistence>";
        private const string SerializationFormat = "1";

        /// <summary>
        /// in memory cache will hold onto any info related to opened documents in primary branch or all documents in forked branch
        /// 
        /// this is not snapshot based so multiple versions of snapshots can re-use same data as long as it is relevant.
        /// </summary>
        private static readonly ConditionalWeakTable<BranchId, ConditionalWeakTable<DocumentId, SyntaxTreeIdentifierInfo>> cache =
            new ConditionalWeakTable<BranchId, ConditionalWeakTable<DocumentId, SyntaxTreeIdentifierInfo>>();

        private readonly VersionStamp version;
        private readonly BloomFilter identifierFilter;
        private readonly BloomFilter escapedIdentifierFilter;

        public SyntaxTreeIdentifierInfo(
            VersionStamp version,
            BloomFilter identifierFilter,
            BloomFilter escapedIdentifierFilter) :
            base(version)
        {
            if (identifierFilter == null)
            {
                throw new ArgumentNullException("identifierFilter");
            }

            if (escapedIdentifierFilter == null)
            {
                throw new ArgumentNullException("escapedIdentifierFilter");
            }

            this.version = version;
            this.identifierFilter = identifierFilter;
            this.escapedIdentifierFilter = escapedIdentifierFilter;
        }

        /// <summary>
        /// Returns true when the identifier is probably (but not guaranteed) to be within the
        /// syntax tree.  Returns false when the identifier is guaranteed to not be within the
        /// syntax tree.
        /// </summary>
        public bool ProbablyContainsIdentifier(string identifier)
        {
            return identifierFilter.ProbablyContains(identifier);
        }

        /// <summary>
        /// Returns true when the identifier is probably (but not guaranteed) escaped within the
        /// text of the syntax tree.  Returns false when the identifier is guaranteed to not be
        /// escaped within the text of the syntax tree.  An identifier that is not escaped within
        /// the text can be found by searching the text directly.  An identifier that is escaped can
        /// only be found by parsing the text and syntactically interpreting any escaping
        /// mechanisms found in the language ("\uXXXX" or "@XXXX" in C# or "[XXXX]" in Visual
        /// Basic).
        /// </summary>
        public bool ProbablyContainsEscapedIdentifier(string identifier)
        {
            return escapedIdentifierFilter.ProbablyContains(identifier);
        }

        public void WriteTo(ObjectWriter writer)
        {
            this.identifierFilter.WriteTo(writer);
            this.escapedIdentifierFilter.WriteTo(writer);
        }

        private static SyntaxTreeIdentifierInfo ReadFrom(ObjectReader reader, VersionStamp version)
        {
            try
            {
                var identifierFilter = BloomFilter.ReadFrom(reader);
                var escapedIdentifierFilter = BloomFilter.ReadFrom(reader);

                return new SyntaxTreeIdentifierInfo(version, identifierFilter, escapedIdentifierFilter);
            }
            catch (Exception)
            {
            }

            return null;
        }

        public static Task<bool> PrecalculatedAsync(Document document, CancellationToken cancellationToken)
        {
            return PrecalculatedAsync(document, PersistenceName, SerializationFormat, cancellationToken);
        }

        public static async Task<SyntaxTreeIdentifierInfo> LoadAsync(Document document, CancellationToken cancellationToken)
        {
            var workspace = document.Project.Solution.Workspace;
            var infoTable = GetInfoTable(document.Project.Solution.BranchId, workspace);
            var version = await document.GetSyntaxVersionAsync(cancellationToken).ConfigureAwait(false);

            // first look to see if we already have the info in the live cache
            SyntaxTreeIdentifierInfo info;
            if (infoTable.TryGetValue(document.Id, out info) && info.Version == version)
            {
                return info;
            }

            // we know cached information is invalid, delete it.
            infoTable.Remove(document.Id);

            // now, check primary cache to see whether we have a hit
            var primaryInfoTable = GetInfoTable(workspace.PrimaryBranchId, workspace);
            if (primaryInfoTable.TryGetValue(document.Id, out info) && info.Version == version)
            {
                return info;
            }

            // check whether we can re-use persisted data
            info = await LoadAsync(document, PersistenceName, SerializationFormat, ReadFrom, cancellationToken).ConfigureAwait(false);
            if (info != null)
            {
                // check whether we need to cache it
                if (document.IsOpen())
                {
                    Contract.Requires(!await document.IsForkedDocumentWithSyntaxChangesAsync(cancellationToken).ConfigureAwait(false));
                    primaryInfoTable.Remove(document.Id);
                    primaryInfoTable.GetValue(document.Id, _ => info);
                }

                return info;
            }

            return null;
        }

        public async Task<bool> SaveAsync(Document document, CancellationToken cancellationToken)
        {
            var workspace = document.Project.Solution.Workspace;
            var infoTable = GetInfoTable(document.Project.Solution.BranchId, workspace);

            // if it is forked document
            if (await document.IsForkedDocumentWithSyntaxChangesAsync(cancellationToken).ConfigureAwait(false))
            {
                infoTable.Remove(document.Id);
                infoTable.GetValue(document.Id, _ => this);
                return false;
            }

            // okay, cache this info if it is from opened document or persistence failed.
            var persisted = await SaveAsync(document, PersistenceName, SerializationFormat, this, cancellationToken).ConfigureAwait(false);
            if (!persisted || document.IsOpen())
            {
                var primaryInfoTable = GetInfoTable(workspace.PrimaryBranchId, workspace);
                primaryInfoTable.Remove(document.Id);
                primaryInfoTable.GetValue(document.Id, _ => this);
            }

            return persisted;
        }

        private static ConditionalWeakTable<DocumentId, SyntaxTreeIdentifierInfo> GetInfoTable(BranchId branchId, Workspace workspace)
        {
            return cache.GetValue(branchId, id =>
            {
                if (id == workspace.PrimaryBranchId)
                {
                    workspace.DocumentClosed += OnDocumentClosed;
                }

                return new ConditionalWeakTable<DocumentId, SyntaxTreeIdentifierInfo>();
            });
        }

        private static void OnDocumentClosed(object sender, DocumentEventArgs e)
        {
            if (!e.Document.IsFromPrimaryBranch())
            {
                return;
            }

            ConditionalWeakTable<DocumentId, SyntaxTreeIdentifierInfo> infoTable;
            if (cache.TryGetValue(e.Document.Project.Solution.BranchId, out infoTable))
            {
                // remove closed document from primary branch from live cache.
                infoTable.Remove(e.Document.Id);
            }
        }
    }
}