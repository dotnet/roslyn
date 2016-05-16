// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
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
    internal partial class SyntaxTreeInfo
    {
        /// <summary>
        /// snapshot based cache to guarantee same info is returned without re-calculating for same solution snapshot.
        /// since document will be re-created per new solution, this should go away as soon as there is any change on workspace.
        /// </summary>
        private static readonly ConditionalWeakTable<Document, SyntaxTreeIdentifierInfo> s_identifierSnapshotCache = new ConditionalWeakTable<Document, SyntaxTreeIdentifierInfo>();
        private static readonly ConditionalWeakTable<Document, SyntaxTreeContextInfo> s_contextSnapshotCache = new ConditionalWeakTable<Document, SyntaxTreeContextInfo>();
        private static readonly ConditionalWeakTable<Document, SyntaxTreeDeclarationInfo> s_declaredSymbolsSnapshotCache = new ConditionalWeakTable<Document, SyntaxTreeDeclarationInfo>();

        public static async Task PrecalculateAsync(Document document, CancellationToken cancellationToken)
        {
            Contract.Requires(document.IsFromPrimaryBranch());

            await PrecalculateBasicInfoAsync(document, cancellationToken).ConfigureAwait(false);

            // for now, don't put identifier locations in esent db
            //// await PrecalculateAdvancedInfoAsync(document, cancellationToken).ConfigureAwait(false);
        }

        private static async Task PrecalculateAdvancedInfoAsync(Document document, CancellationToken cancellationToken)
        {
            // we do not support precalculating opened file.
            if (document.IsOpen())
            {
                return;
            }

            // we already have information. move on
            if (await SyntaxTreeIdentifierInfo.IdentifierSetPrecalculatedAsync(document, cancellationToken).ConfigureAwait(false))
            {
                return;
            }

            await SyntaxTreeIdentifierInfo.SaveIdentifierSetAsync(document, cancellationToken).ConfigureAwait(false);
        }

        private static async Task PrecalculateBasicInfoAsync(Document document, CancellationToken cancellationToken)
        {
            // we already have information. move on
            if (await SyntaxTreeIdentifierInfo.PrecalculatedAsync(document, cancellationToken).ConfigureAwait(false) &&
                await SyntaxTreeContextInfo.PrecalculatedAsync(document, cancellationToken).ConfigureAwait(false) &&
                await SyntaxTreeDeclarationInfo.PrecalculatedAsync(document, cancellationToken).ConfigureAwait(false))
            {
                return;
            }

            var data = await CreateInfoAsync(document, cancellationToken).ConfigureAwait(false);

            await data.Item1.SaveAsync(document, cancellationToken).ConfigureAwait(false);
            await data.Item2.SaveAsync(document, cancellationToken).ConfigureAwait(false);
            await data.Item3.SaveAsync(document, cancellationToken).ConfigureAwait(false);
        }

        private static async Task<T> GetInfoAsync<T>(
            Document document,
            ConditionalWeakTable<Document, T> cache,
            Func<Document, CancellationToken, Task<T>> generator,
            Func<ValueTuple<SyntaxTreeIdentifierInfo, SyntaxTreeContextInfo, SyntaxTreeDeclarationInfo>, T> selector,
            CancellationToken cancellationToken)
            where T : class
        {
            T info;
            if (cache.TryGetValue(document, out info))
            {
                return info;
            }

            info = await generator(document, cancellationToken).ConfigureAwait(false);
            if (info != null)
            {
                return cache.GetValue(document, _ => info);
            }

            // alright, we don't have cached information, re-calculate them here.
            var data = await CreateInfoAsync(document, cancellationToken).ConfigureAwait(false);

            // okay, persist this info
            await data.Item1.SaveAsync(document, cancellationToken).ConfigureAwait(false);
            await data.Item2.SaveAsync(document, cancellationToken).ConfigureAwait(false);
            await data.Item3.SaveAsync(document, cancellationToken).ConfigureAwait(false);

            info = selector(data);
            return cache.GetValue(document, _ => info);
        }

        public static Task<SyntaxTreeContextInfo> GetContextInfoAsync(Document document, CancellationToken cancellationToken)
        {
            return GetInfoAsync(document, s_contextSnapshotCache, SyntaxTreeContextInfo.LoadAsync, tuple => tuple.Item2, cancellationToken);
        }

        public static Task<SyntaxTreeIdentifierInfo> GetIdentifierInfoAsync(Document document, CancellationToken cancellationToken)
        {
            return GetInfoAsync(document, s_identifierSnapshotCache, SyntaxTreeIdentifierInfo.LoadAsync, tuple => tuple.Item1, cancellationToken);
        }

        public static Task<SyntaxTreeDeclarationInfo> GetDeclarationInfoAsync(Document document, CancellationToken cancellationToken)
        {
            return GetInfoAsync(document, s_declaredSymbolsSnapshotCache, SyntaxTreeDeclarationInfo.LoadAsync, tuple => tuple.Item3, cancellationToken);
        }

        // The probability of getting a false positive when calling ContainsIdentifier.
        private const double FalsePositiveProbability = 0.0001;

        private static async Task<ValueTuple<SyntaxTreeIdentifierInfo, SyntaxTreeContextInfo, SyntaxTreeDeclarationInfo>> CreateInfoAsync(Document document, CancellationToken cancellationToken)
        {
            var syntaxFacts = document.GetLanguageService<ISyntaxFactsService>();
            var ignoreCase = syntaxFacts != null && !syntaxFacts.IsCaseSensitive;
            var isCaseSensitive = !ignoreCase;

            HashSet<string> identifiers;
            HashSet<string> escapedIdentifiers;
            GetIdentifierSet(ignoreCase, out identifiers, out escapedIdentifiers);

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

                var declaredSymbolInfos = new List<DeclaredSymbolInfo>();

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
                            DeclaredSymbolInfo declaredSymbolInfo;
                            if (syntaxFacts.TryGetDeclaredSymbolInfo(node, out declaredSymbolInfo))
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

                            PredefinedType predefinedType;
                            if (syntaxFacts.TryGetPredefinedType(token, out predefinedType))
                            {
                                predefinedTypes |= (int)predefinedType;
                            }

                            PredefinedOperator predefinedOperator;
                            if (syntaxFacts.TryGetPredefinedOperator(token, out predefinedOperator))
                            {
                                predefinedOperators |= (int)predefinedOperator;
                            }
                        }
                    }
                }

                var version = await document.GetSyntaxVersionAsync(cancellationToken).ConfigureAwait(false);

                return ValueTuple.Create(
                    new SyntaxTreeIdentifierInfo(
                        version,
                        new BloomFilter(FalsePositiveProbability, isCaseSensitive, identifiers),
                        new BloomFilter(FalsePositiveProbability, isCaseSensitive, escapedIdentifiers)),
                    new SyntaxTreeContextInfo(
                        version,
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
                    new SyntaxTreeDeclarationInfo(
                        version,
                        declaredSymbolInfos));
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
    }
}
