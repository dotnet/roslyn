// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.ErrorReporting;
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
        bool TryGetDeclaredSymbolInfo(StringTable stringTable, SyntaxNode node, string rootNamespace, out DeclaredSymbolInfo declaredSymbolInfo);

        bool TryGetTargetTypeName(SyntaxNode node, out string instanceTypeName);

        bool TryGetAliasesFromUsingDirective(SyntaxNode node, out ImmutableArray<(string aliasName, string name)> aliases);

        string GetRootNamespace(CompilationOptions compilationOptions);
    }

    internal sealed partial class SyntaxTreeIndex
    {
        // The probability of getting a false positive when calling ContainsIdentifier.
        private const double FalsePositiveProbability = 0.0001;

        public static readonly ObjectPool<HashSet<string>> StringLiteralHashSetPool =
            new ObjectPool<HashSet<string>>(() => new HashSet<string>(), 20);

        public static readonly ObjectPool<HashSet<long>> LongLiteralHashSetPool =
            new ObjectPool<HashSet<long>>(() => new HashSet<long>(), 20);

        /// <summary>
        /// String interning table so that we can share many more strings in our DeclaredSymbolInfo
        /// buckets.  Keyed off a Project instance so that we share all these strings as we create
        /// the or load the index items for this a specific Project.  This helps as we will generally 
        /// be creating or loading all the index items for the documents in a Project at the same time.
        /// Once this project is let go of (which happens with any solution change) then we'll dump
        /// this string table.  The table will have already served its purpose at that point and 
        /// doesn't need to be kept around further.
        /// </summary>
        private static readonly ConditionalWeakTable<Project, StringTable> s_projectStringTable =
            new ConditionalWeakTable<Project, StringTable>();

        private static async Task<SyntaxTreeIndex> CreateIndexAsync(
            Document document, Checksum checksum, CancellationToken cancellationToken)
        {
            var project = document.Project;
            var stringTable = GetStringTable(project);

            var syntaxFacts = document.GetLanguageService<ISyntaxFactsService>();
            var infoFactory = document.GetLanguageService<IDeclaredSymbolInfoFactoryService>();
            var ignoreCase = syntaxFacts != null && !syntaxFacts.IsCaseSensitive;
            var isCaseSensitive = !ignoreCase;

            GetIdentifierSet(ignoreCase, out var identifiers, out var escapedIdentifiers);

            var stringLiterals = StringLiteralHashSetPool.Allocate();
            var longLiterals = LongLiteralHashSetPool.Allocate();

            var simpleExtensionMethodInfoBuilder = PooledDictionary<string, ArrayBuilder<int>>.GetInstance();
            var complexExtensionMethodInfoBuilder = ArrayBuilder<int>.GetInstance();

            var usingAliases = PooledDictionary<string, string>.GetInstance();

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

                var predefinedTypes = (int)PredefinedType.None;
                var predefinedOperators = (int)PredefinedOperator.None;

                var declaredSymbolInfos = ArrayBuilder<DeclaredSymbolInfo>.GetInstance();

                if (syntaxFacts != null)
                {
                    var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
                    var rootNamespace = infoFactory.GetRootNamespace(project.CompilationOptions);

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

                            containsDeconstruction = containsDeconstruction || syntaxFacts.IsDeconstructionAssignment(node)
                                || syntaxFacts.IsDeconstructionForEachStatement(node);

                            containsAwait = containsAwait || syntaxFacts.IsAwaitExpression(node);
                            containsTupleExpressionOrTupleType = containsTupleExpressionOrTupleType ||
                                syntaxFacts.IsTupleExpression(node) || syntaxFacts.IsTupleType(node);

                            if (syntaxFacts.IsUsingAliasDirective(node) && infoFactory.TryGetAliasesFromUsingDirective(node, out var aliases))
                            {
                                foreach (var (aliasName, name) in aliases)
                                {
                                    // In C#, it's valid to declare two alias with identical name,
                                    // as long as they are in different containers.
                                    //
                                    // e.g.
                                    //      using X = System.String;
                                    //      namespace N
                                    //      {
                                    //          using X = System.Int32;
                                    //      }
                                    //
                                    // If we detect this, we will simply treat extension methods whose
                                    // target type is this alais as complex method.
                                    if (usingAliases.ContainsKey(aliasName))
                                    {
                                        usingAliases[aliasName] = null;
                                    }
                                    else
                                    {
                                        usingAliases[aliasName] = name;
                                    }
                                }
                            }

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
                            if (infoFactory.TryGetDeclaredSymbolInfo(stringTable, node, rootNamespace, out var declaredSymbolInfo))
                            {
                                if (root.FullSpan.Contains(declaredSymbolInfo.Span))
                                {
                                    var declaredSymbolInfoIndex = declaredSymbolInfos.Count;
                                    declaredSymbolInfos.Add(declaredSymbolInfo);

                                    AddExtensionMethodInfo(
                                        infoFactory,
                                        node,
                                        usingAliases,
                                        declaredSymbolInfoIndex,
                                        declaredSymbolInfo,
                                        simpleExtensionMethodInfoBuilder,
                                        complexExtensionMethodInfoBuilder);
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
                            containsIndexerMemberCref,
                            containsDeconstruction,
                            containsAwait,
                            containsTupleExpressionOrTupleType),
                    new DeclarationInfo(
                            declaredSymbolInfos.ToImmutableAndFree()),
                    new ExtensionMethodInfo(
                        simpleExtensionMethodInfoBuilder.ToImmutableDictionary(s_getKey, s_getValuesAsImmutableArray),
                        complexExtensionMethodInfoBuilder.ToImmutable()));
            }
            finally
            {
                Free(ignoreCase, identifiers, escapedIdentifiers);
                StringLiteralHashSetPool.ClearAndFree(stringLiterals);
                LongLiteralHashSetPool.ClearAndFree(longLiterals);
                simpleExtensionMethodInfoBuilder.Free();
                complexExtensionMethodInfoBuilder.Free();
                usingAliases.Free();
            }
        }

        private static readonly Func<KeyValuePair<string, ArrayBuilder<int>>, string> s_getKey = kvp => kvp.Key;
        private static readonly Func<KeyValuePair<string, ArrayBuilder<int>>, ImmutableArray<int>> s_getValuesAsImmutableArray = kvp => kvp.Value.ToImmutableAndFree();

        private static void AddExtensionMethodInfo(
            IDeclaredSymbolInfoFactoryService infoFactory,
            SyntaxNode node,
            PooledDictionary<string, string> aliases,
            int declaredSymbolInfoIndex,
            DeclaredSymbolInfo declaredSymbolInfo,
            PooledDictionary<string, ArrayBuilder<int>> simpleInfoBuilder,
            ArrayBuilder<int> complexInfoBuilder)
        {
            if (declaredSymbolInfo.Kind != DeclaredSymbolInfoKind.ExtensionMethod)
            {
                return;
            }

            if (!infoFactory.TryGetTargetTypeName(node, out var targetTypeName))
            {
                return;
            }

            // complex method
            if (targetTypeName == null)
            {
                complexInfoBuilder.Add(declaredSymbolInfoIndex);
                return;
            }

            // Target type is an alias
            if (aliases.TryGetValue(targetTypeName, out var originalName))
            {
                // it is an alias of multiple with identical name,
                // simply treat it as a complex method.
                if (originalName == null)
                {
                    complexInfoBuilder.Add(declaredSymbolInfoIndex);
                    return;
                }

                // replace the alias with its original name.
                targetTypeName = originalName;
            }

            // So we've got a simple method.
            if (!simpleInfoBuilder.TryGetValue(targetTypeName, out var arrayBuilder))
            {
                arrayBuilder = ArrayBuilder<int>.GetInstance();
                simpleInfoBuilder[targetTypeName] = arrayBuilder;
            }

            arrayBuilder.Add(declaredSymbolInfoIndex);
        }

        private static StringTable GetStringTable(Project project)
            => s_projectStringTable.GetValue(project, _ => StringTable.GetInstance());

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
