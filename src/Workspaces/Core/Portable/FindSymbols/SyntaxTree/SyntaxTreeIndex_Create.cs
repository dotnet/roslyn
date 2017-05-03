﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.ErrorReporting;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.Utilities;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.FindSymbols
{
    internal sealed partial class SyntaxTreeIndex
    {
        // The probability of getting a false positive when calling ContainsIdentifier.
        private const double FalsePositiveProbability = 0.0001;

        public static readonly ObjectPool<HashSet<string>> StringLiteralHashSetPool =
            new ObjectPool<HashSet<string>>(() => new HashSet<string>(), 20);

        public static readonly ObjectPool<HashSet<long>> LongLiteralHashSetPool =
            new ObjectPool<HashSet<long>>(() => new HashSet<long>(), 20);

        private static async Task<SyntaxTreeIndex> CreateIndexAsync(
            Document document, Checksum checksum, CancellationToken cancellationToken)
        {
            var syntaxFacts = document.GetLanguageService<ISyntaxFactsService>();
            var ignoreCase = syntaxFacts != null && !syntaxFacts.IsCaseSensitive;
            var isCaseSensitive = !ignoreCase;

            GetIdentifierSet(ignoreCase, out var identifiers, out var escapedIdentifiers);

            var stringLiterals = StringLiteralHashSetPool.Allocate();
            var longLiterals = LongLiteralHashSetPool.Allocate();

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

                            if (syntaxFacts.IsIdentifier(token) ||
                                syntaxFacts.IsGlobalNamespaceKeyword(token))
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

                            if (syntaxFacts.IsStringLiteral(token))
                            {
                                stringLiterals.Add(token.ValueText);
                            }

                            if (syntaxFacts.IsCharacterLiteral(token))
                            {
                                longLiterals.Add((char)token.Value);
                            }

                            if (syntaxFacts.IsNumericLiteral(token))
                            {
                                var value = token.Value;
                                switch (value)
                                {
                                    case decimal d:
                                        // not supported for now.
                                        break;
                                    case double d:
                                        longLiterals.Add(BitConverter.DoubleToInt64Bits(d));
                                        break;
                                    case float f:
                                        longLiterals.Add(BitConverter.DoubleToInt64Bits(f));
                                        break;
                                    default:
                                        longLiterals.Add(IntegerUtilities.ToInt64(token.Value));
                                        break;
                                }
                            }
                        }
                    }
                }

                return new SyntaxTreeIndex(
                    checksum,
                    new LiteralInfo(
                        new BloomFilter(FalsePositiveProbability, stringLiterals, longLiterals)),
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
                StringLiteralHashSetPool.ClearAndFree(stringLiterals);
                LongLiteralHashSetPool.ClearAndFree(longLiterals);
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