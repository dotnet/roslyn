// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
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
        private static readonly ConditionalWeakTable<Document, SyntaxTreeIdentifierInfo> identifierSnapshotCache = new ConditionalWeakTable<Document, SyntaxTreeIdentifierInfo>();
        private static readonly ConditionalWeakTable<Document, SyntaxTreeContextInfo> contextSnapshotCache = new ConditionalWeakTable<Document, SyntaxTreeContextInfo>();

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
                await SyntaxTreeContextInfo.PrecalculatedAsync(document, cancellationToken).ConfigureAwait(false))
            {
                return;
            }

            var data = await CreateInfoAsync(document, cancellationToken).ConfigureAwait(false);

            await data.Item1.SaveAsync(document, cancellationToken).ConfigureAwait(false);
            await data.Item2.SaveAsync(document, cancellationToken).ConfigureAwait(false);
        }

        public static async Task<SyntaxTreeContextInfo> GetContextInfoAsync(Document document, CancellationToken cancellationToken)
        {
            SyntaxTreeContextInfo info;
            if (contextSnapshotCache.TryGetValue(document, out info))
            {
                return info;
            }

            info = await SyntaxTreeContextInfo.LoadAsync(document, cancellationToken).ConfigureAwait(false);
            if (info != null)
            {
                return contextSnapshotCache.GetValue(document, _ => info);
            }

            // alright, we don't have cached information, re-calcuate them here.
            var data = await CreateInfoAsync(document, cancellationToken).ConfigureAwait(false);

            // okay, persist this info.
            await data.Item1.SaveAsync(document, cancellationToken).ConfigureAwait(false);
            await data.Item2.SaveAsync(document, cancellationToken).ConfigureAwait(false);

            info = data.Item2;
            return contextSnapshotCache.GetValue(document, _ => info);
        }

        public static async Task<SyntaxTreeIdentifierInfo> GetIdentifierInfoAsync(Document document, CancellationToken cancellationToken)
        {
            SyntaxTreeIdentifierInfo info;
            if (identifierSnapshotCache.TryGetValue(document, out info))
            {
                return info;
            }

            info = await SyntaxTreeIdentifierInfo.LoadAsync(document, cancellationToken).ConfigureAwait(false);
            if (info != null)
            {
                return identifierSnapshotCache.GetValue(document, _ => info);
            }

            // alright, we don't have cached information, re-calcuate them here.
            var data = await CreateInfoAsync(document, cancellationToken).ConfigureAwait(false);

            // okay, persist this info.
            await data.Item1.SaveAsync(document, cancellationToken).ConfigureAwait(false);
            await data.Item2.SaveAsync(document, cancellationToken).ConfigureAwait(false);

            info = data.Item1;
            return identifierSnapshotCache.GetValue(document, _ => info);
        }

        // The probability of getting a false positive when calling ContainsIdentifier.
        private const double FalsePositiveProbability = 0.0001;

        private static async Task<ValueTuple<SyntaxTreeIdentifierInfo, SyntaxTreeContextInfo>> CreateInfoAsync(Document document, CancellationToken cancellationToken)
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
                        containsIndexerMemberCref));
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