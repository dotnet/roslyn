﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.Utilities;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.FindSymbols
{
    internal interface IDeclaredSymbolInfoFactoryService : ILanguageService
    {
        // `rootNamespace` is required for VB projects that has non-global namespace as root namespace,
        // otherwise we would not be able to get correct data from syntax.
        void AddDeclaredSymbolInfos(Document document, SyntaxNode root, ArrayBuilder<DeclaredSymbolInfo> declaredSymbolInfos, Dictionary<string, ArrayBuilder<int>> extensionMethodInfo, CancellationToken cancellationToken);
    }

    internal sealed partial class SyntaxTreeIndex
    {
        // The probability of getting a false positive when calling ContainsIdentifier.
        private const double FalsePositiveProbability = 0.0001;

        public static readonly ObjectPool<HashSet<string>> StringLiteralHashSetPool = SharedPools.Default<HashSet<string>>();
        public static readonly ObjectPool<HashSet<long>> LongLiteralHashSetPool = SharedPools.Default<HashSet<long>>();

        /// <summary>
        /// String interning table so that we can share many more strings in our DeclaredSymbolInfo
        /// buckets.  Keyed off a Project instance so that we share all these strings as we create
        /// the or load the index items for this a specific Project.  This helps as we will generally 
        /// be creating or loading all the index items for the documents in a Project at the same time.
        /// Once this project is let go of (which happens with any solution change) then we'll dump
        /// this string table.  The table will have already served its purpose at that point and 
        /// doesn't need to be kept around further.
        /// </summary>
        private static readonly ConditionalWeakTable<Project, StringTable> s_projectStringTable = new();

        private static async Task<SyntaxTreeIndex> CreateIndexAsync(
            Document document, Checksum checksum, CancellationToken cancellationToken)
        {
            Contract.ThrowIfFalse(document.SupportsSyntaxTree);

            var root = await document.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            return CreateIndex(document, root, checksum, cancellationToken);
        }

        private static SyntaxTreeIndex CreateIndex(
            Document document, SyntaxNode root, Checksum checksum, CancellationToken cancellationToken)
        {
            var syntaxFacts = document.GetRequiredLanguageService<ISyntaxFactsService>();
            var infoFactory = document.GetRequiredLanguageService<IDeclaredSymbolInfoFactoryService>();
            var ignoreCase = !syntaxFacts.IsCaseSensitive;
            var isCaseSensitive = !ignoreCase;

            GetIdentifierSet(ignoreCase, out var identifiers, out var escapedIdentifiers);

            var stringLiterals = StringLiteralHashSetPool.Allocate();
            var longLiterals = LongLiteralHashSetPool.Allocate();

            using var _1 = ArrayBuilder<DeclaredSymbolInfo>.GetInstance(out var declaredSymbolInfos);
            using var _2 = PooledDictionary<string, ArrayBuilder<int>>.GetInstance(out var extensionMethodInfo);

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
                var containsDeconstruction = false;
                var containsAwait = false;
                var containsTupleExpressionOrTupleType = false;
                var containsImplicitObjectCreation = false;
                var containsGlobalAttributes = false;
                var containsConversion = false;

                var predefinedTypes = (int)PredefinedType.None;
                var predefinedOperators = (int)PredefinedOperator.None;

                if (syntaxFacts != null)
                {
                    foreach (var current in root.DescendantNodesAndTokensAndSelf(descendIntoTrivia: true))
                    {
                        if (current.IsNode)
                        {
                            var node = current.AsNode();

                            containsForEachStatement = containsForEachStatement || syntaxFacts.IsForEachStatement(node);
                            containsLockStatement = containsLockStatement || syntaxFacts.IsLockStatement(node);
                            containsUsingStatement = containsUsingStatement || syntaxFacts.IsUsingStatement(node);
                            containsQueryExpression = containsQueryExpression || syntaxFacts.IsQueryExpression(node);
                            containsElementAccess = containsElementAccess || syntaxFacts.IsElementAccessExpression(node);
                            containsIndexerMemberCref = containsIndexerMemberCref || syntaxFacts.IsIndexerMemberCRef(node);

                            containsDeconstruction = containsDeconstruction || syntaxFacts.IsDeconstructionAssignment(node)
                                || syntaxFacts.IsDeconstructionForEachStatement(node);

                            containsAwait = containsAwait || syntaxFacts.IsAwaitExpression(node);
                            containsTupleExpressionOrTupleType = containsTupleExpressionOrTupleType ||
                                syntaxFacts.IsTupleExpression(node) || syntaxFacts.IsTupleType(node);
                            containsImplicitObjectCreation = containsImplicitObjectCreation || syntaxFacts.IsImplicitObjectCreationExpression(node);
                            containsGlobalAttributes = containsGlobalAttributes || syntaxFacts.IsGlobalAttribute(node);
                            containsConversion = containsConversion || syntaxFacts.IsConversionExpression(node);
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
                                longLiterals.Add((char)token.Value!);
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

                    infoFactory.AddDeclaredSymbolInfos(
                        document, root, declaredSymbolInfos, extensionMethodInfo, cancellationToken);
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
                            containsIndexerMemberCref,
                            containsDeconstruction,
                            containsAwait,
                            containsTupleExpressionOrTupleType,
                            containsImplicitObjectCreation,
                            containsGlobalAttributes,
                            containsConversion),
                    new DeclarationInfo(declaredSymbolInfos.ToImmutable()),
                    new ExtensionMethodInfo(
                        extensionMethodInfo.ToImmutableDictionary(
                            static kvp => kvp.Key,
                            static kvp => kvp.Value.ToImmutable())));
            }
            finally
            {
                Free(ignoreCase, identifiers, escapedIdentifiers);
                StringLiteralHashSetPool.ClearAndFree(stringLiterals);
                LongLiteralHashSetPool.ClearAndFree(longLiterals);

                foreach (var (_, builder) in extensionMethodInfo)
                    builder.Free();
            }
        }

        public static StringTable GetStringTable(Project project)
            => s_projectStringTable.GetValue(project, static _ => StringTable.GetInstance());

        private static void GetIdentifierSet(bool ignoreCase, out HashSet<string> identifiers, out HashSet<string> escapedIdentifiers)
        {
            if (ignoreCase)
            {
                identifiers = SharedPools.StringIgnoreCaseHashSet.AllocateAndClear();
                escapedIdentifiers = SharedPools.StringIgnoreCaseHashSet.AllocateAndClear();

                Debug.Assert(identifiers.Comparer == StringComparer.OrdinalIgnoreCase);
                Debug.Assert(escapedIdentifiers.Comparer == StringComparer.OrdinalIgnoreCase);
                return;
            }

            identifiers = SharedPools.StringHashSet.AllocateAndClear();
            escapedIdentifiers = SharedPools.StringHashSet.AllocateAndClear();

            Debug.Assert(identifiers.Comparer == StringComparer.Ordinal);
            Debug.Assert(escapedIdentifiers.Comparer == StringComparer.Ordinal);
        }

        private static void Free(bool ignoreCase, HashSet<string> identifiers, HashSet<string> escapedIdentifiers)
        {
            if (ignoreCase)
            {
                Debug.Assert(identifiers.Comparer == StringComparer.OrdinalIgnoreCase);
                Debug.Assert(escapedIdentifiers.Comparer == StringComparer.OrdinalIgnoreCase);

                SharedPools.StringIgnoreCaseHashSet.ClearAndFree(identifiers);
                SharedPools.StringIgnoreCaseHashSet.ClearAndFree(escapedIdentifiers);
                return;
            }

            Debug.Assert(identifiers.Comparer == StringComparer.Ordinal);
            Debug.Assert(escapedIdentifiers.Comparer == StringComparer.Ordinal);

            SharedPools.StringHashSet.ClearAndFree(identifiers);
            SharedPools.StringHashSet.ClearAndFree(escapedIdentifiers);
        }
    }
}
