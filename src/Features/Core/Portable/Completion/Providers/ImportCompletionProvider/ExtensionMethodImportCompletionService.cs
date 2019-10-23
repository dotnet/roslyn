// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

#nullable enable

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
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

    internal static class ExtensionMethodImportCompletionService
    {
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
            using var _ = PooledHashSet<string>.GetInstance(out var allTypeNames);
            allTypeNames.Add(receiverTypeSymbol.MetadataName);
            allTypeNames.AddRange(receiverTypeSymbol.GetBaseTypes().Select(t => t.MetadataName));
            allTypeNames.AddRange(receiverTypeSymbol.GetAllInterfacesIncludingThis().Select(t => t.MetadataName));

            // interface doesn't inherit from object, but is implicitly convertable to object type.
            if (receiverTypeSymbol.IsInterfaceType())
            {
                allTypeNames.Add(nameof(Object));
            }

            var matchedMethods = await GetPossibleExtensionMethodMatchesAsync(
                project, allTypeNames.ToImmutableArray(), cancellationToken).ConfigureAwait(false);

            counter.GetFilterTicks = Environment.TickCount - ticks;
            counter.NoFilter = matchedMethods == null;

            // Don't show unimported extension methods if the index isn't ready.
            // User can still use expander to get them w/o filter if needed.
            if (matchedMethods == null && !isExpandedCompletion)
            {
                return (ImmutableArray<SerializableImportCompletionItem>.Empty, counter);
            }

            ticks = Environment.TickCount;

            var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
            var items = GetExtensionMethodItems(assembly.GlobalNamespace, string.Empty, receiverTypeSymbol,
                semanticModel!, position, namespaceInScope, matchedMethods, counter, cancellationToken);

            counter.GetSymbolTicks = Environment.TickCount - ticks;

            return (items, counter);
        }

        private static async Task<MultiDictionary<string, string>?> GetPossibleExtensionMethodMatchesAsync(
            Project currentProject,
            ImmutableArray<string> targetTypeNames,
            CancellationToken cancellationToken)
        {
            bool.TryParse(Environment.GetEnvironmentVariable("FORCE_LOAD"), out var forceLoad);

            var solution = currentProject.Solution;
            var graph = currentProject.Solution.GetProjectDependencyGraph();
            var relevantProjectIds = graph.GetProjectsThatThisProjectTransitivelyDependsOn((global::Microsoft.CodeAnalysis.ProjectId)currentProject.Id)
                                        .Concat<global::Microsoft.CodeAnalysis.ProjectId>((global::Microsoft.CodeAnalysis.ProjectId)currentProject.Id);

            using var syntaxDisposer = ArrayBuilder<SyntaxTreeIndex>.GetInstance(out var syntaxTreeIndexBuilder);
            using var symbolDisposer = ArrayBuilder<SymbolTreeInfo>.GetInstance(out var symbolTreeInfoBuilder);
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
                    var loadOnly = !forceLoad && projectId != currentProject.Id;

                    foreach (var document in project.Documents)
                    {
                        // Don't look for extension methods in generated code.
                        if (document.State.Attributes.IsGenerated)
                        {
                            continue;
                        }

                        var info = await document.GetSyntaxTreeIndexAsync(loadOnly, cancellationToken).ConfigureAwait(false);

                        // Don't provide anything if we don't have all the required SyntaxTreeIndex created.
                        if (info == null)
                        {
                            return null;
                        }

                        if (info.ContainsExtensionMethod)
                        {
                            syntaxTreeIndexBuilder.Add(info);
                        }
                    }
                }

                foreach (var peReference in peReferences)
                {
                    var info = await SymbolTreeInfo.GetInfoForMetadataReferenceAsync(
                        solution, peReference, loadOnly: false, cancellationToken).ConfigureAwait(false);

                    // Don't provide anyting if we don't have all the required SymbolTreeInfo created.
                    if (info == null)
                    {
                        return null;
                    }

                    if (info.ContainsExtensionMethod)
                    {
                        symbolTreeInfoBuilder.Add(info);
                    }
                }

                var results = new MultiDictionary<string, string>();

                // Find matching extension methods from source.
                foreach (var info in syntaxTreeIndexBuilder)
                {
                    // Add simple extension methods with matching target type name
                    foreach (var targetTypeName in targetTypeNames)
                    {
                        if (!info.SimpleExtensionMethodInfo.TryGetValue(targetTypeName, out var methodInfoIndices))
                        {
                            continue;
                        }

                        foreach (var index in methodInfoIndices)
                        {
                            if (info.TryGetDeclaredSymbolInfo(index, out var methodInfo))
                            {
                                results.Add(methodInfo.FullyQualifiedContainerName, methodInfo.Name);
                            }
                        }
                    }

                    // Add all complex extension methods, we will need to completely rely on symbols to match them.
                    foreach (var index in info.ComplexExtensionMethodInfo)
                    {
                        if (info.TryGetDeclaredSymbolInfo(index, out var methodInfo))
                        {
                            results.Add(methodInfo.FullyQualifiedContainerName, methodInfo.Name);
                        }
                    }
                }

                // Find matching extension methods from metadata
                foreach (var info in symbolTreeInfoBuilder)
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
            string containingNamespace,
            ITypeSymbol receiverTypeSymbol,
            SemanticModel semanticModel,
            int position,
            ISet<string> namespaceFilter,
            MultiDictionary<string, string>? methodNameFilter,
            StatisticCounter counter,
            CancellationToken cancellationToken)
        {
            if (methodNameFilter != null)
            {
                return GetExtensionMethodItemsWithFilter(rootNamespaceSymbol, receiverTypeSymbol, semanticModel, position, namespaceFilter, methodNameFilter, counter, cancellationToken);
            }

            return GetExtensionMethodItemsWithOutFilter(rootNamespaceSymbol, containingNamespace, receiverTypeSymbol, semanticModel, position, namespaceFilter, counter, cancellationToken);
        }

        private static readonly char[] s_dotSeparator = new char[] { '.' };

        private static ImmutableArray<SerializableImportCompletionItem> GetExtensionMethodItemsWithFilter(
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
                    foreach (var conflictTypeSymbol in GetConflictingSymbols(rootNamespaceSymbol, fullyQualifiedContainerName.Split(s_dotSeparator).ToImmutableArray()))
                    {
                        ProcessContainingType(receiverTypeSymbol, semanticModel, position, counter, builder, methodNames, qualifiedNamespaceName, conflictTypeSymbol);
                    }
                }
            }

            return builder.ToImmutable();

            static void ProcessContainingType(
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

            static ImmutableArray<INamedTypeSymbol> GetConflictingSymbols(INamespaceSymbol rootNamespaceSymbol, ImmutableArray<string> fullyQualifiedContainerNameParts)
            {
                var namespaceNameParts = fullyQualifiedContainerNameParts.RemoveAt(fullyQualifiedContainerNameParts.Length - 1);
                var typeName = fullyQualifiedContainerNameParts[fullyQualifiedContainerNameParts.Length - 1];
                var current = rootNamespaceSymbol;

                // First find the namespace symbol 
                foreach (var name in namespaceNameParts)
                {
                    if (current == null)
                    {
                        break;
                    }

                    current = current.GetMembers(name).OfType<INamespaceSymbol>().FirstOrDefault();
                }

                if (current != null)
                {
                    return current.GetMembers(typeName).OfType<INamedTypeSymbol>().ToImmutableArray();
                }

                return ImmutableArray<INamedTypeSymbol>.Empty;
            }
        }

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
    }

    internal sealed class StatisticCounter
    {
        public bool NoFilter;
        public int TotalTicks;
        public int TotalExtensionMethodsProvided;
        public int GetFilterTicks;
        public int GetSymbolTicks;
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
