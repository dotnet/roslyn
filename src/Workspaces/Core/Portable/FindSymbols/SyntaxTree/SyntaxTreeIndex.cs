// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.ErrorReporting;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.Utilities;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.FindSymbols
{
    internal sealed partial class SyntaxTreeIndex : AbstractPersistableState, IObjectWritable
    {
        // The probability of getting a false positive when calling ContainsIdentifier.
        private const double FalsePositiveProbability = 0.0001;

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

        public static async Task<SyntaxTreeIndex> CreateInfoAsync(Document document, CancellationToken cancellationToken)
        {
            var syntaxFacts = document.GetLanguageService<ISyntaxFactsService>();
            var ignoreCase = syntaxFacts != null && !syntaxFacts.IsCaseSensitive;
            var isCaseSensitive = !ignoreCase;

            GetIdentifierSet(ignoreCase, out var identifiers, out var escapedIdentifiers);

            try
            {
                var containsForEachStatement = false;
                var containsLockStatement = false;
                var containsUsingStatement = false;
                var containsQueryExpression = false;
                var containsThisConstructorInitializer = false;
                var containsBaseConstructorInitializer = false;
                var containsElementAccess = false;
                var containsIndexerMemberCref = false;

                var predefinedTypes = (int)PredefinedType.None;
                var predefinedOperators = (int)PredefinedOperator.None;

                var declaredSymbolInfos = ArrayBuilder<DeclaredSymbolInfo>.GetInstance();

                if (syntaxFacts != null)
                {
                    var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);

                    foreach (var current in root.DescendantNodesAndTokensAndSelf(descendIntoTrivia: true))
                    {
                        if (current.IsNode)
                        {
                            var node = (SyntaxNode)current;

                            containsForEachStatement = containsForEachStatement || syntaxFacts.IsForEachStatement(node);
                            containsLockStatement = containsLockStatement || syntaxFacts.IsLockStatement(node);
                            containsUsingStatement = containsUsingStatement || syntaxFacts.IsUsingStatement(node);
                            containsQueryExpression = containsQueryExpression || syntaxFacts.IsQueryExpression(node);
                            containsElementAccess = containsElementAccess || syntaxFacts.IsElementAccessExpression(node);
                            containsIndexerMemberCref = containsIndexerMemberCref || syntaxFacts.IsIndexerMemberCRef(node);
                            // We've received a number of error reports where DeclaredSymbolInfo.GetSymbolAsync() will
                            // crash because the document's syntax root doesn't contain the span of the node returned
                            // by TryGetDeclaredSymbolInfo().  There are two possibilities for this crash:
                            //   1) syntaxFacts.TryGetDeclaredSymbolInfo() is returning a bad span, or
                            //   2) Document.GetSyntaxRootAsync() (called from DeclaredSymbolInfo.GetSymbolAsync) is
                            //      returning a bad syntax root that doesn't represent the original parsed document.
                            // By adding the `root.FullSpan.Contains()` check below, if we get similar crash reports in
                            // the future then we know the problem lies in (2).  If, however, the problem is really in
                            // TryGetDeclaredSymbolInfo, then this will at least prevent us from returning bad spans
                            // and will prevent the crash from occurring.
                            if (syntaxFacts.TryGetDeclaredSymbolInfo(node, out var declaredSymbolInfo))
                            {
                                if (root.FullSpan.Contains(declaredSymbolInfo.Span))
                                {
                                    declaredSymbolInfos.Add(declaredSymbolInfo);
                                }
                                else
                                {
                                    var message =
$@"Invalid span in {nameof(declaredSymbolInfo)}.
{nameof(declaredSymbolInfo.Span)} = {declaredSymbolInfo.Span}
{nameof(root.FullSpan)} = {root.FullSpan}";
                                    FatalError.ReportWithoutCrash(new InvalidOperationException(message));
                                }
                            }
                        }
                        else
                        {
                            var token = (SyntaxToken)current;

                            containsThisConstructorInitializer = containsThisConstructorInitializer || syntaxFacts.IsThisConstructorInitializer(token);
                            containsBaseConstructorInitializer = containsBaseConstructorInitializer || syntaxFacts.IsBaseConstructorInitializer(token);

                            if (syntaxFacts.IsIdentifier(token) || syntaxFacts.IsGlobalNamespaceKeyword(token))
                            {
                                var valueText = token.ValueText;

                                identifiers.Add(valueText);
                                if (valueText.Length != token.Width())
                                {
                                    escapedIdentifiers.Add(valueText);
                                }
                            }

                            if (syntaxFacts.TryGetPredefinedType(token, out var predefinedType))
                            {
                                predefinedTypes |= (int)predefinedType;
                            }

                            if (syntaxFacts.TryGetPredefinedOperator(token, out var predefinedOperator))
                            {
                                predefinedOperators |= (int)predefinedOperator;
                            }
                        }
                    }
                }

                var version = await document.GetSyntaxVersionAsync(cancellationToken).ConfigureAwait(false);

                return new SyntaxTreeIndex(
                    version,
                    new IdentifierInfo(
                        new BloomFilter(FalsePositiveProbability, isCaseSensitive, identifiers),
                        new BloomFilter(FalsePositiveProbability, isCaseSensitive, escapedIdentifiers)),
                    new ContextInfo(
                            predefinedTypes,
                            predefinedOperators,
                            containsForEachStatement,
                            containsLockStatement,
                            containsUsingStatement,
                            containsQueryExpression,
                            containsThisConstructorInitializer,
                            containsBaseConstructorInitializer,
                            containsElementAccess,
                            containsIndexerMemberCref),
                    new DeclarationInfo(
                            declaredSymbolInfos.ToImmutableAndFree()));
            }
            finally
            {
                Free(ignoreCase, identifiers, escapedIdentifiers);
            }
        }

        private static void GetIdentifierSet(bool ignoreCase, out HashSet<string> identifiers, out HashSet<string> escapedIdentifiers)
        {
            if (ignoreCase)
            {
                identifiers = SharedPools.StringIgnoreCaseHashSet.AllocateAndClear();
                escapedIdentifiers = SharedPools.StringIgnoreCaseHashSet.AllocateAndClear();

                Contract.Requires(identifiers.Comparer == StringComparer.OrdinalIgnoreCase);
                Contract.Requires(escapedIdentifiers.Comparer == StringComparer.OrdinalIgnoreCase);
                return;
            }

            identifiers = SharedPools.StringHashSet.AllocateAndClear();
            escapedIdentifiers = SharedPools.StringHashSet.AllocateAndClear();

            Contract.Requires(identifiers.Comparer == StringComparer.Ordinal);
            Contract.Requires(escapedIdentifiers.Comparer == StringComparer.Ordinal);
        }

        private static void Free(bool ignoreCase, HashSet<string> identifiers, HashSet<string> escapedIdentifiers)
        {
            if (ignoreCase)
            {
                Contract.Requires(identifiers.Comparer == StringComparer.OrdinalIgnoreCase);
                Contract.Requires(escapedIdentifiers.Comparer == StringComparer.OrdinalIgnoreCase);

                SharedPools.StringIgnoreCaseHashSet.ClearAndFree(identifiers);
                SharedPools.StringIgnoreCaseHashSet.ClearAndFree(escapedIdentifiers);
                return;
            }

            Contract.Requires(identifiers.Comparer == StringComparer.Ordinal);
            Contract.Requires(escapedIdentifiers.Comparer == StringComparer.Ordinal);

            SharedPools.StringHashSet.ClearAndFree(identifiers);
            SharedPools.StringHashSet.ClearAndFree(escapedIdentifiers);
        }

        public ImmutableArray<DeclaredSymbolInfo> DeclaredSymbolInfos => _declarationInfo.DeclaredSymbolInfos;

        public bool ProbablyContainsIdentifier(string identifier) => _identifierInfo.ProbablyContainsIdentifier(identifier);
        public bool ProbablyContainsEscapedIdentifier(string identifier) => _identifierInfo.ProbablyContainsEscapedIdentifier(identifier);

        public bool ContainsPredefinedType(PredefinedType type) => _contextInfo.ContainsPredefinedType(type);
        public bool ContainsPredefinedOperator(PredefinedOperator op) => _contextInfo.ContainsPredefinedOperator(op);

        public bool ContainsForEachStatement => _contextInfo.ContainsForEachStatement;
        public bool ContainsLockStatement => _contextInfo.ContainsLockStatement;
        public bool ContainsUsingStatement => _contextInfo.ContainsUsingStatement;
        public bool ContainsQueryExpression => _contextInfo.ContainsQueryExpression;
        public bool ContainsThisConstructorInitializer => _contextInfo.ContainsThisConstructorInitializer;
        public bool ContainsBaseConstructorInitializer => _contextInfo.ContainsBaseConstructorInitializer;
        public bool ContainsElementAccessExpression => _contextInfo.ContainsElementAccessExpression;
        public bool ContainsIndexerMemberCref => _contextInfo.ContainsIndexerMemberCref;
    }
}