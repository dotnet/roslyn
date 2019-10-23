// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

#nullable enable

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Completion.Log;
using Microsoft.CodeAnalysis.FindSymbols;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Remote;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Completion.Providers
{
    internal sealed class SerializableImportCompletionItem
    {
        public readonly string SymbolKeyData;
        public readonly int Arity;
        public readonly string Name;
        public readonly Glyph Glyph;
        public readonly string ContainingNamespace;

        public SerializableImportCompletionItem(string symbolKeyData, string name, int arity, Glyph glyph, string containingNamespace)
        {
            SymbolKeyData = symbolKeyData;
            Arity = arity;
            Name = name;
            Glyph = glyph;
            ContainingNamespace = containingNamespace;
        }
    }

    internal static partial class ExtensionMethodImportCompletionService
    {
        private static readonly char[] s_dotSeparator = new char[] { '.' };

        private static readonly object s_gate = new object();
        private static Task s_indexingTask = Task.CompletedTask;

        public static async Task<ImmutableArray<SerializableImportCompletionItem>> GetUnimportExtensionMethodsAsync(
            Document document,
            int position,
            ITypeSymbol receiverTypeSymbol,
            ISet<string> namespaceInScope,
            bool isExpandedCompletion,
            CancellationToken cancellationToken)
        {
            var ticks = Environment.TickCount;
            var project = document.Project;

            // This service is only defined for C# and VB, but we'll be a bit paranoid.
            var client = RemoteSupportedLanguages.IsSupported(project.Language)
                ? await project.Solution.Workspace.TryGetRemoteHostClientAsync(cancellationToken).ConfigureAwait(false)
                : null;

            var (serializableItems, counter) = client == null
                ? await GetUnimportExtensionMethodsInCurrentProcessAsync(document, position, receiverTypeSymbol, namespaceInScope, isExpandedCompletion, cancellationToken).ConfigureAwait(false)
                : await GetUnimportExtensionMethodsInRemoteProcessAsync(client, document, position, receiverTypeSymbol, namespaceInScope, isExpandedCompletion, cancellationToken).ConfigureAwait(false);

            counter.TotalTicks = Environment.TickCount - ticks;
            counter.TotalExtensionMethodsProvided = serializableItems.Length;

            // TODO: remove this
            Internal.Log.Logger.Log(Internal.Log.FunctionId.Completion_ExtensionMethodImportCompletionProvider_GetCompletionItemsAsync, Internal.Log.KeyValueLogMessage.Create(m =>
            {
                m["ExtMethodData"] = counter.ToString();
            }));

            return serializableItems;
        }

        public static async Task<(ImmutableArray<SerializableImportCompletionItem>, StatisticCounter)> GetUnimportExtensionMethodsInRemoteProcessAsync(
            RemoteHostClient client,
            Document document,
            int position,
            ITypeSymbol receiverTypeSymbol,
            ISet<string> namespaceInScope,
            bool isExpandedCompletion,
            CancellationToken cancellationToken)
        {
            var project = document.Project;
            var (serializableItems, counter) = await client.TryRunCodeAnalysisRemoteAsync<(IList<SerializableImportCompletionItem>, StatisticCounter)>(
                project.Solution,
                nameof(IRemoteExtensionMethodImportCompletionService.GetUnimportedExtensionMethodsAsync),
                new object[] { document.Id, position, SymbolKey.CreateString(receiverTypeSymbol), namespaceInScope.ToArray(), isExpandedCompletion },
                cancellationToken).ConfigureAwait(false);

            return (serializableItems.ToImmutableArray(), counter);
        }

        public static async Task<(ImmutableArray<SerializableImportCompletionItem>, StatisticCounter)> GetUnimportExtensionMethodsInCurrentProcessAsync(
            Document document,
            int position,
            ITypeSymbol receiverTypeSymbol,
            ISet<string> namespaceInScope,
            bool isExpandedCompletion,
            CancellationToken cancellationToken)
        {
            var counter = new StatisticCounter();
            var ticks = Environment.TickCount;

            var project = document.Project;
            var assembly = (await project.GetCompilationAsync(cancellationToken).ConfigureAwait(false))!;

            // Get the metadata name of all the base types and interfaces this type derived from.
            using var _ = PooledHashSet<string>.GetInstance(out var allTypeNamesBuilder);
            allTypeNamesBuilder.Add(receiverTypeSymbol.MetadataName);
            allTypeNamesBuilder.AddRange(receiverTypeSymbol.GetBaseTypes().Select(t => t.MetadataName));
            allTypeNamesBuilder.AddRange(receiverTypeSymbol.GetAllInterfacesIncludingThis().Select(t => t.MetadataName));

            // interface doesn't inherit from object, but is implicitly convertable to object type.
            if (receiverTypeSymbol.IsInterfaceType())
            {
                allTypeNamesBuilder.Add(nameof(Object));
            }

            var allTypeNames = allTypeNamesBuilder.ToImmutableArray();
            var matchedMethods = await GetPossibleExtensionMethodMatchesAsync(
                project, allTypeNames, isPrecalculation: false, cancellationToken).ConfigureAwait(false);

            counter.GetFilterTicks = Environment.TickCount - ticks;
            counter.NoFilter = matchedMethods == null;

            // Don't show unimported extension methods if the index isn't ready.
            // Queue a background task to calculate index if previous task is completed.
            // TODO: hide expander button
            if (matchedMethods == null)
            {
                lock (s_gate)
                {
                    if (s_indexingTask.IsCompleted)
                    {
                        s_indexingTask = Task.Run(() => GetPossibleExtensionMethodMatchesAsync(project, allTypeNames, isPrecalculation: true, CancellationToken.None));
                    }
                }

                return (ImmutableArray<SerializableImportCompletionItem>.Empty, counter);
            }

            ticks = Environment.TickCount;

            var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
            var items = GetExtensionMethodItems(assembly.GlobalNamespace, receiverTypeSymbol,
                semanticModel!, position, namespaceInScope, matchedMethods, counter, cancellationToken);

            counter.GetSymbolTicks = Environment.TickCount - ticks;

            return (items, counter);
        }

        private static async Task<MultiDictionary<string, string>?> GetPossibleExtensionMethodMatchesAsync(
            Project currentProject,
            ImmutableArray<string> targetTypeNames,
            bool isPrecalculation,
            CancellationToken cancellationToken)
        {
            var solution = currentProject.Solution;
            var cacheService = GetCacheService(solution.Workspace);
            var graph = currentProject.Solution.GetProjectDependencyGraph();
            var relevantProjectIds = graph.GetProjectsThatThisProjectTransitivelyDependsOn((global::Microsoft.CodeAnalysis.ProjectId)currentProject.Id)
                                        .Concat<global::Microsoft.CodeAnalysis.ProjectId>((global::Microsoft.CodeAnalysis.ProjectId)currentProject.Id);

            using var syntaxDisposer = ArrayBuilder<CacheEntry>.GetInstance(out var syntaxBuilder);
            using var symbolDisposer = ArrayBuilder<SymbolTreeInfo>.GetInstance(out var symbolBuilder);
            var peReferences = PooledHashSet<global::Microsoft.CodeAnalysis.PortableExecutableReference>.GetInstance();

            try
            {
                foreach (var projectId in relevantProjectIds.Concat(currentProject.Id))
                {
                    var project = solution.GetProject(projectId);
                    if (project == null || !project.SupportsCompilation)
                    {
                        continue;
                    }

                    // Transitively get all the PE references
                    peReferences.AddRange(project.MetadataReferences.OfType<PortableExecutableReference>());

                    // Don't trigger index creation except for documents in current project.
                    var loadOnly = !isPrecalculation && projectId != currentProject.Id;
                    var cacheEntry = await GetCacheEntryAsync(project, loadOnly, cacheService, cancellationToken).ConfigureAwait(false);

                    // Don't provide anything if we don't have all the required SyntaxTreeIndex created.
                    if (cacheEntry == null)
                    {
                        return null;
                    }

                    syntaxBuilder.Add(cacheEntry.Value);
                }

                foreach (var peReference in peReferences)
                {
                    var info = await SymbolTreeInfo.GetInfoForMetadataReferenceAsync(
                        solution, peReference, loadOnly: !isPrecalculation, cancellationToken).ConfigureAwait(false);

                    // Don't provide anything if we don't have all the required SymbolTreeInfo created.
                    if (info == null)
                    {
                        return null;
                    }

                    if (info.ContainsExtensionMethod)
                    {
                        symbolBuilder.Add(info);
                    }
                }

                // We are just trying to populate the cache in background, no need to return any results.
                if (isPrecalculation)
                {
                    return null;
                }

                var results = new MultiDictionary<string, string>();

                // Find matching extension methods from source.
                foreach (var info in syntaxBuilder)
                {
                    // Add simple extension methods with matching target type name
                    foreach (var targetTypeName in targetTypeNames)
                    {
                        var methodInfos = info.SimpleExtensionMethodInfo[targetTypeName];
                        if (methodInfos.Count == 0)
                        {
                            continue;
                        }

                        foreach (var methodInfo in methodInfos)
                        {
                            results.Add(methodInfo.FullyQualifiedContainerName, methodInfo.Name);
                        }
                    }

                    // Add all complex extension methods, we will need to completely rely on symbols to match them.
                    foreach (var methodInfo in info.ComplexExtensionMethodInfo)
                    {
                        results.Add(methodInfo.FullyQualifiedContainerName, methodInfo.Name);
                    }
                }

                // Find matching extension methods from metadata
                foreach (var info in symbolBuilder)
                {
                    var methodInfos = info.GetMatchingExtensionMethodInfo(targetTypeNames);
                    foreach (var methodInfo in methodInfos)
                    {
                        results.Add(methodInfo.FullyQualifiedContainerName, methodInfo.Name);
                    }
                }

                return results;
            }
            finally
            {
                peReferences.Free();
            }
        }

        private static ImmutableArray<SerializableImportCompletionItem> GetExtensionMethodItems(
            INamespaceSymbol rootNamespaceSymbol,
            ITypeSymbol receiverTypeSymbol,
            SemanticModel semanticModel,
            int position,
            ISet<string> namespaceFilter,
            MultiDictionary<string, string> methodNameFilter,
            StatisticCounter counter,
            CancellationToken cancellationToken)
        {
            var compilation = semanticModel.Compilation;
            using var _ = ArrayBuilder<SerializableImportCompletionItem>.GetInstance(out var builder);

            using var conflictTypeRootNode = new Node(name: string.Empty);

            foreach (var (fullyQualifiedContainerName, methodNames) in methodNameFilter)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var indexOfLastDot = fullyQualifiedContainerName.LastIndexOf('.');
                var qualifiedNamespaceName = indexOfLastDot > 0 ? fullyQualifiedContainerName.Substring(0, indexOfLastDot) : string.Empty;

                if (namespaceFilter.Contains(qualifiedNamespaceName))
                {
                    continue;
                }

                // Container of extension method (static class in C# and Module in VB) can't be generic or nested.
                // Note that we might incorrectly ignore valid types, because, for example, calling `GetTypeByMetadataName` 
                // would return null if we have multiple definitions of a type even though only one is accessible from here
                // (e.g. an internal type declared inside a shared document).
                var containerSymbol = compilation.GetTypeByMetadataName(fullyQualifiedContainerName);

                if (containerSymbol != null)
                {
                    ProcessContainingType(receiverTypeSymbol, semanticModel, position, counter, builder, methodNames, qualifiedNamespaceName, containerSymbol);
                }
                else
                {
                    conflictTypeRootNode.Add(fullyQualifiedContainerName, (qualifiedNamespaceName, methodNames));
                }
            }
            var ticks = Environment.TickCount;

            GetItemsFromConflictingTypes(rootNamespaceSymbol, conflictTypeRootNode, builder, receiverTypeSymbol, semanticModel, position, counter);

            counter.GetSymbolExtraTicks = Environment.TickCount - ticks;

            return builder.ToImmutable();
        }

        private static void ProcessContainingType(
            ITypeSymbol receiverTypeSymbol, SemanticModel semanticModel, int position, StatisticCounter counter,
            ArrayBuilder<SerializableImportCompletionItem> builder,
            MultiDictionary<string, string>.ValueSet methodNames, string qualifiedNamespaceName,
            INamedTypeSymbol containerSymbol)
        {
            counter.TotalTypesChecked++;

            if (containerSymbol != null &&
                containerSymbol.MightContainExtensionMethods &&
                IsSymbolAccessible(containerSymbol, position, semanticModel))
            {
                foreach (var methodName in methodNames)
                {
                    var methodSymbols = containerSymbol.GetMembers(methodName).OfType<IMethodSymbol>();
                    ProcessMethods(qualifiedNamespaceName, receiverTypeSymbol, semanticModel, position, builder, counter, methodNames, methodSymbols);
                }
            }
        }

        private static void GetItemsFromConflictingTypes(INamespaceSymbol rootNamespaceSymbol, Node conflictTypeNodes, ArrayBuilder<SerializableImportCompletionItem> builder,
            ITypeSymbol receiverTypeSymbol, SemanticModel semanticModel, int position, StatisticCounter counter)
        {
            Debug.Assert(!conflictTypeNodes.NamespaceAndMethodNames.HasValue);

            foreach (var child in conflictTypeNodes.Children.Values)
            {
                if (child.NamespaceAndMethodNames == null)
                {
                    var childNamespace = rootNamespaceSymbol.GetMembers(child.Name).OfType<INamespaceSymbol>().FirstOrDefault();
                    GetItemsFromConflictingTypes(childNamespace, child, builder, receiverTypeSymbol, semanticModel, position, counter);
                }
                else
                {
                    var types = rootNamespaceSymbol.GetMembers(child.Name).OfType<INamedTypeSymbol>();
                    foreach (var type in types)
                    {
                        var (namespaceName, methodNames) = child.NamespaceAndMethodNames.Value;
                        ProcessContainingType(receiverTypeSymbol, semanticModel, position, counter, builder, methodNames, namespaceName, type);
                    }
                }
            }
        }

        private static void ProcessMethods(
            string containingNamespace,
            ITypeSymbol receiverTypeSymbol,
            SemanticModel semanticModel,
            int position,
            ArrayBuilder<SerializableImportCompletionItem> builder,
            StatisticCounter counter,
            MultiDictionary<string, string>.ValueSet methodNames,
            IEnumerable<IMethodSymbol> methodSymbols)
        {
            foreach (var methodSymbol in methodSymbols)
            {
                counter.TotalExtensionMethodsChecked++;
                IMethodSymbol? reducedMethodSymbol = null;

                if (methodSymbol.IsExtensionMethod &&
                    (methodNames.Count == 0 || methodNames.Contains(methodSymbol.Name)) &&
                    IsSymbolAccessible(methodSymbol, position, semanticModel))
                {
                    reducedMethodSymbol = methodSymbol.ReduceExtensionMethod(receiverTypeSymbol);
                }

                if (reducedMethodSymbol != null)
                {
                    var symbolKeyData = SymbolKey.CreateString(reducedMethodSymbol);
                    builder.Add(new SerializableImportCompletionItem(
                        symbolKeyData,
                        reducedMethodSymbol.Name,
                        reducedMethodSymbol.Arity,
                        reducedMethodSymbol.GetGlyph(),
                        containingNamespace));
                }
            }
        }

        // We only call this when the containing symbol is accessible, 
        // so being declared as public means this symbol is also accessible.
        private static bool IsSymbolAccessible(ISymbol symbol, int position, SemanticModel semanticModel)
            => symbol.DeclaredAccessibility == Accessibility.Public || semanticModel.IsAccessible(position, symbol);

        private class Node : IDisposable
        {
            public string Name { get; }

            public PooledDictionary<string, Node> Children { get; }

            public (string namespaceName, MultiDictionary<string, string>.ValueSet methodNames)? NamespaceAndMethodNames { get; private set; }

            public Node(string name)
            {
                Name = name;
                Children = PooledDictionary<string, Node>.GetInstance();
            }


            public void Add(string fullyQualifiedContainerName, (string namespaceName, MultiDictionary<string, string>.ValueSet methodNames) namespaceAndMethodNames)
            {
                var parts = fullyQualifiedContainerName.Split(s_dotSeparator);

                var current = this;
                foreach (var part in parts)
                {
                    if (!current.Children.TryGetValue(part, out var child))
                    {
                        child = new Node(part);
                        current.Children.Add(part, child);
                    }

                    current = child;
                }

                // Type and Namespace can't have identical name
                Debug.Assert(current.Children.Count == 0);
                current.NamespaceAndMethodNames = namespaceAndMethodNames;
            }

            public void Dispose()
            {
                foreach (var childNode in Children.Values)
                {
                    childNode.Dispose();
                }

                Children.Free();
            }
        }

        // TODO remove
        private static string GetMethodSignature(IMethodSymbol methodSymbol)
        {
            var typeParameters = methodSymbol.TypeParameters.Length > 0
                ? $"<{methodSymbol.TypeParameters.Select(tp => tp.Name).Aggregate(ConcatString)}>"
                : "";
            var parameterTypes = methodSymbol.Parameters.Length > 0
                ? methodSymbol.Parameters.Select(p => p.Type.ToSignatureDisplayString()).Aggregate(ConcatString)
                : "";

            return $"{methodSymbol.Name}{typeParameters}({parameterTypes})";

            static string ConcatString(string s1, string s2)
            {
                return $"{s1}, {s2}";
            }
        }

        // TODO remove
        private static ImmutableArray<SerializableImportCompletionItem> GetExtensionMethodItemsWithOutFilter(
            INamespaceSymbol namespaceSymbol,
            string containingNamespace,
            ITypeSymbol receiverTypeSymbol,
            SemanticModel semanticModel,
            int position,
            ISet<string> namespaceFilter,
            StatisticCounter counter,
            CancellationToken cancellationToken)
        {
            using var _ = ArrayBuilder<SerializableImportCompletionItem>.GetInstance(out var filterItems);
            VisitNamespaceSymbol(namespaceSymbol, containingNamespace, receiverTypeSymbol,
                semanticModel!, position, namespaceFilter, filterItems, counter,
                cancellationToken);

            return filterItems.ToImmutable();

            static void VisitNamespaceSymbol(
                INamespaceSymbol namespaceSymbol,
                string containingNamespace,
                ITypeSymbol receiverTypeSymbol,
                SemanticModel senamticModel,
                int position,
                ISet<string> namespaceFilter,
                ArrayBuilder<SerializableImportCompletionItem> builder,
                StatisticCounter counter,
                CancellationToken cancellationToken)
            {
                cancellationToken.ThrowIfCancellationRequested();
                containingNamespace = CompletionHelper.ConcatNamespace(containingNamespace, namespaceSymbol.Name);

                foreach (var memberNamespace in namespaceSymbol.GetNamespaceMembers())
                {
                    VisitNamespaceSymbol(
                        memberNamespace, containingNamespace, receiverTypeSymbol, senamticModel, position, namespaceFilter,
                        builder, counter, cancellationToken);
                }

                // All types in this namespace are already in-scope.
                if (namespaceFilter.Contains(containingNamespace))
                {
                    return;
                }

                foreach (var containgType in namespaceSymbol.GetTypeMembers())
                {
                    counter.TotalTypesChecked++;
                    if (containgType.MightContainExtensionMethods && IsSymbolAccessible(containgType, position, senamticModel))
                    {
                        var methodSymbols = containgType.GetMembers().OfType<IMethodSymbol>();
                        ProcessMethods(containingNamespace, receiverTypeSymbol, senamticModel, position, builder, counter, methodNames: default, methodSymbols);
                    }
                }
            }
        }
    }

    internal sealed class StatisticCounter
    {
        public bool NoFilter;
        public int TotalTicks;
        public int TotalExtensionMethodsProvided;
        public int GetFilterTicks;
        public int GetSymbolTicks;
        public int GetSymbolExtraTicks;
        public int TotalTypesChecked;
        public int TotalExtensionMethodsChecked;

        // TODO: remove
        public override string ToString()
        {
            return
$@"
NoFilter : {NoFilter}

TotalTicks: {TotalTicks}
GetFilterTicks : {GetFilterTicks}
GetSymbolTicks : {GetSymbolTicks}
GetSymbolExtraTicks : {GetSymbolExtraTicks}

TotalTypesChecked : {TotalTypesChecked}
TotalExtensionMethodsChecked : {TotalExtensionMethodsChecked}
TotalExtensionMethodsProvided : {TotalExtensionMethodsProvided}
";
        }

        public void Report()
        {
            CompletionProvidersLogger.LogExtensionMethodCompletionTicksDataPoint(TotalTicks);
            CompletionProvidersLogger.LogExtensionMethodCompletionMethodsProvidedDataPoint(TotalExtensionMethodsProvided);

            if (NoFilter)
            {
                CompletionProvidersLogger.LogExtensionMethodCompletionGetSymbolNoFilterTicksDataPoint(GetSymbolTicks);
                CompletionProvidersLogger.LogExtensionMethodCompletionTypesCheckedNoFilterDataPoint(TotalTypesChecked);
                CompletionProvidersLogger.LogExtensionMethodCompletionMethodsCheckedNoFilterDataPoint(TotalExtensionMethodsChecked);
            }
            else
            {
                CompletionProvidersLogger.LogExtensionMethodCompletionGetFilterTicksDataPoint(GetFilterTicks);
                CompletionProvidersLogger.LogExtensionMethodCompletionGetSymbolWithFilterTicksDataPoint(GetSymbolTicks);
                CompletionProvidersLogger.LogExtensionMethodCompletionTypesCheckedWithFilterDataPoint(TotalTypesChecked);
                CompletionProvidersLogger.LogExtensionMethodCompletionMethodsCheckedWithFilterDataPoint(TotalExtensionMethodsChecked);
            }
        }
    }
}
