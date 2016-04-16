// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.FindSymbols
{
    internal class SyntaxTreeContextInfo : AbstractPersistableState, IObjectWritable
    {
        private const string PersistenceName = "<SyntaxTreeInfoContextPersistence>";
        private const string SerializationFormat = "1";

        /// <summary>
        /// hold context info in memory. since context info is quite small (less than 30 bytes per a document),
        /// holding this in memory should be fine.
        /// </summary>
        private static readonly ConditionalWeakTable<BranchId, ConditionalWeakTable<DocumentId, SyntaxTreeContextInfo>> s_cache =
            new ConditionalWeakTable<BranchId, ConditionalWeakTable<DocumentId, SyntaxTreeContextInfo>>();

        private readonly int _predefinedTypes;
        private readonly int _predefinedOperators;
        private readonly ContainingNodes _containingNodes;

        internal SyntaxTreeContextInfo(
            VersionStamp version,
            int predefinedTypes,
            int predefinedOperators,
            bool containsForEachStatement,
            bool containsLockStatement,
            bool containsUsingStatement,
            bool containsQueryExpression,
            bool containsThisConstructorInitializer,
            bool containsBaseConstructorInitializer,
            bool containsElementAccessExpression,
            bool containsIndexerMemberCref) :
            this(version, predefinedTypes, predefinedOperators,
                 ConvertToContainingNodeFlag(
                     containsForEachStatement,
                     containsLockStatement,
                     containsUsingStatement,
                     containsQueryExpression,
                     containsThisConstructorInitializer,
                     containsBaseConstructorInitializer,
                     containsElementAccessExpression,
                     containsIndexerMemberCref))
        {
        }

        private SyntaxTreeContextInfo(VersionStamp version, int predefinedTypes, int predefinedOperators, ContainingNodes containingNodes) :
            base(version)
        {
            _predefinedTypes = predefinedTypes;
            _predefinedOperators = predefinedOperators;
            _containingNodes = containingNodes;
        }

        private static ContainingNodes ConvertToContainingNodeFlag(
            bool containsForEachStatement,
            bool containsLockStatement,
            bool containsUsingStatement,
            bool containsQueryExpression,
            bool containsThisConstructorInitializer,
            bool containsBaseConstructorInitializer,
            bool containsElementAccessExpression,
            bool containsIndexerMemberCref)
        {
            var containingNodes = ContainingNodes.None;

            containingNodes = containsForEachStatement ? (containingNodes | ContainingNodes.ContainsForEachStatement) : containingNodes;
            containingNodes = containsLockStatement ? (containingNodes | ContainingNodes.ContainsLockStatement) : containingNodes;
            containingNodes = containsUsingStatement ? (containingNodes | ContainingNodes.ContainsUsingStatement) : containingNodes;
            containingNodes = containsQueryExpression ? (containingNodes | ContainingNodes.ContainsQueryExpression) : containingNodes;
            containingNodes = containsThisConstructorInitializer ? (containingNodes | ContainingNodes.ContainsThisConstructorInitializer) : containingNodes;
            containingNodes = containsBaseConstructorInitializer ? (containingNodes | ContainingNodes.ContainsBaseConstructorInitializer) : containingNodes;
            containingNodes = containsElementAccessExpression ? (containingNodes | ContainingNodes.ContainsElementAccessExpression) : containingNodes;
            containingNodes = containsIndexerMemberCref ? (containingNodes | ContainingNodes.ContainsIndexerMemberCref) : containingNodes;

            return containingNodes;
        }

        public bool ContainsPredefinedType(PredefinedType type)
        {
            return (_predefinedTypes & (int)type) == (int)type;
        }

        public bool ContainsPredefinedOperator(PredefinedOperator op)
        {
            return (_predefinedOperators & (int)op) == (int)op;
        }

        public bool ContainsForEachStatement
        {
            get
            {
                return (_containingNodes & ContainingNodes.ContainsForEachStatement) == ContainingNodes.ContainsForEachStatement;
            }
        }

        public bool ContainsLockStatement
        {
            get
            {
                return (_containingNodes & ContainingNodes.ContainsLockStatement) == ContainingNodes.ContainsLockStatement;
            }
        }

        public bool ContainsUsingStatement
        {
            get
            {
                return (_containingNodes & ContainingNodes.ContainsUsingStatement) == ContainingNodes.ContainsUsingStatement;
            }
        }

        public bool ContainsQueryExpression
        {
            get
            {
                return (_containingNodes & ContainingNodes.ContainsQueryExpression) == ContainingNodes.ContainsQueryExpression;
            }
        }

        public bool ContainsThisConstructorInitializer
        {
            get
            {
                return (_containingNodes & ContainingNodes.ContainsThisConstructorInitializer) == ContainingNodes.ContainsThisConstructorInitializer;
            }
        }

        public bool ContainsBaseConstructorInitializer
        {
            get
            {
                return (_containingNodes & ContainingNodes.ContainsBaseConstructorInitializer) == ContainingNodes.ContainsBaseConstructorInitializer;
            }
        }

        public bool ContainsElementAccessExpression
        {
            get
            {
                return (_containingNodes & ContainingNodes.ContainsElementAccessExpression) == ContainingNodes.ContainsElementAccessExpression;
            }
        }

        public bool ContainsIndexerMemberCref
        {
            get
            {
                return (_containingNodes & ContainingNodes.ContainsIndexerMemberCref) == ContainingNodes.ContainsIndexerMemberCref;
            }
        }

        public void WriteTo(ObjectWriter writer)
        {
            // TODO: convert these set to use bit array rather than enum hashset
            writer.WriteInt32(_predefinedTypes);
            writer.WriteInt32(_predefinedOperators);
            writer.WriteInt32((int)_containingNodes);
        }

        private static SyntaxTreeContextInfo ReadFrom(ObjectReader reader, VersionStamp version)
        {
            try
            {
                var predefinedTypes = reader.ReadInt32();
                var predefinedOperators = reader.ReadInt32();
                var containingNodes = (ContainingNodes)reader.ReadInt32();

                return new SyntaxTreeContextInfo(version, predefinedTypes, predefinedOperators, containingNodes);
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

        public static async Task<SyntaxTreeContextInfo> LoadAsync(Document document, CancellationToken cancellationToken)
        {
            var infoTable = s_cache.GetValue(document.Project.Solution.BranchId, _ => new ConditionalWeakTable<DocumentId, SyntaxTreeContextInfo>());
            var version = await document.GetSyntaxVersionAsync(cancellationToken).ConfigureAwait(false);

            // first look to see if we already have the info in the cache
            SyntaxTreeContextInfo info;
            if (infoTable.TryGetValue(document.Id, out info) && info.Version == version)
            {
                return info;
            }

            // cache is invalid. remove it
            infoTable.Remove(document.Id);

            // check primary cache to see whether we have valid info there
            var primaryInfoTable = s_cache.GetValue(document.Project.Solution.Workspace.PrimaryBranchId, _ => new ConditionalWeakTable<DocumentId, SyntaxTreeContextInfo>());
            if (primaryInfoTable.TryGetValue(document.Id, out info) && info.Version == version)
            {
                return info;
            }

            // check whether we can get it from persistence service
            info = await LoadAsync(document, PersistenceName, SerializationFormat, ReadFrom, cancellationToken).ConfigureAwait(false);
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

        public async Task<bool> SaveAsync(Document document, CancellationToken cancellationToken)
        {
            var infoTable = s_cache.GetValue(document.Project.Solution.BranchId, _ => new ConditionalWeakTable<DocumentId, SyntaxTreeContextInfo>());

            // if it is forked document, no reason to persist
            if (await document.IsForkedDocumentWithSyntaxChangesAsync(cancellationToken).ConfigureAwait(false))
            {
                // cache new information to forked document cache
                infoTable.Remove(document.Id);
                infoTable.GetValue(document.Id, _ => this);
                return false;
            }

            // if it is not forked document, save it and cache to primary branch cache
            var primaryInfoTable = s_cache.GetValue(document.Project.Solution.Workspace.PrimaryBranchId, _ => new ConditionalWeakTable<DocumentId, SyntaxTreeContextInfo>());
            primaryInfoTable.Remove(document.Id);
            primaryInfoTable.GetValue(document.Id, _ => this);

            return await SaveAsync(document, PersistenceName, SerializationFormat, this, cancellationToken).ConfigureAwait(false);
        }

        [Flags]
        private enum ContainingNodes
        {
            None = 0,
            ContainsForEachStatement = 1,
            ContainsLockStatement = 1 << 1,
            ContainsUsingStatement = 1 << 2,
            ContainsQueryExpression = 1 << 3,
            ContainsThisConstructorInitializer = 1 << 4,
            ContainsBaseConstructorInitializer = 1 << 5,
            ContainsElementAccessExpression = 1 << 6,
            ContainsIndexerMemberCref = 1 << 7,
        }
    }
}
