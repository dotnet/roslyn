// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

#nullable enable

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
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
            ImmutableHashSet<string> namespaceInScope,
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
            ImmutableHashSet<string> namespaceInScope,
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
            ImmutableHashSet<string> namespaceInScope,
            bool isExpandedCompletion,
            CancellationToken cancellationToken)
        {
            var coutner = new StatisticCounter();
            var ticks = Environment.TickCount;

            var project = document.Project;
            var assembly = (await project.GetCompilationAsync(cancellationToken).ConfigureAwait(false))!;

            using var allTypeNamesDisposer = PooledHashSet<string>.GetInstance(out var allTypeNames);
            allTypeNames.Add(receiverTypeSymbol.MetadataName);
            allTypeNames.AddRange(receiverTypeSymbol.GetBaseTypes().Concat<global::Microsoft.CodeAnalysis.INamedTypeSymbol>((global::System.Collections.Generic.IEnumerable<global::Microsoft.CodeAnalysis.INamedTypeSymbol>)receiverTypeSymbol.GetAllInterfacesIncludingThis()).Select(s => s.MetadataName));

            // interface doesn't inherit from object, but is implicitly convertable to object type.
            if (receiverTypeSymbol.IsInterfaceType())
            {
                allTypeNames.Add("Object");
            }

            // We don't want to wait for creating indices from scratch unless user explicitly asked for unimported items (via expander).
            var matchedMethods = await GetPossibleExtensionMethodMatchesAsync(
                project, allTypeNames.ToImmutableArray(), loadOnly: !isExpandedCompletion,
                coutner, cancellationToken).ConfigureAwait(false);

            // If the index isn't ready, we will simply iterate through all extension methods symbols.
            if (matchedMethods == null)
            {
                coutner.NoFilter = true;
            }

            var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);

            coutner.GetFilterTicks = Environment.TickCount - ticks;

            ticks = Environment.TickCount;
            using var filterItemsDisposer = ArrayBuilder<SerializableImportCompletionItem>.GetInstance(out var filterItems);

            VisitNamespaceSymbol(assembly.GlobalNamespace, string.Empty, receiverTypeSymbol,
                semanticModel!, position, namespaceInScope, methodNameFilter: matchedMethods, filterItems, coutner,
                cancellationToken);

            coutner.GetSymbolTicks = Environment.TickCount - ticks;

            return (filterItems.ToImmutable(), coutner);
        }

        private static async Task<MultiDictionary<string, string>?> GetPossibleExtensionMethodMatchesAsync(
            Project currentProject,
            ImmutableArray<string> targetTypeNames,
            bool loadOnly,
            StatisticCounter counter,
            CancellationToken cancellationToken)
        {
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
                    // Alway create indices for documents in current project if they don't exist.
                    loadOnly &= projectId != currentProject.Id;

                    var project = solution.GetProject(projectId);
                    if (project == null || !project.SupportsCompilation)
                    {
                        continue;
                    }

                    // Transitively get all the PE references
                    peReferences.AddRange(project.MetadataReferences.OfType<PortableExecutableReference>());

                    foreach (var document in project.Documents)
                    {
                        // Don't look for extension methods in generated code.
                        if (document.State.Attributes.IsGenerated)
                        {
                            continue;
                        }

                        var info = await document.GetSyntaxTreeIndexAsync(loadOnly, cancellationToken).ConfigureAwait(false);

                        // Don't provide anyting if we don't have all the required SyntaxTreeIndex created.
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
                        solution, peReference, loadOnly, cancellationToken).ConfigureAwait(false);

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

        private static void VisitNamespaceSymbol(
            INamespaceSymbol namespaceSymbol,
            string containingNamespace,
            ITypeSymbol receiverTypeSymbol,
            SemanticModel senamticModel,
            int position,
            ImmutableHashSet<string> namespaceFilter,
            MultiDictionary<string, string>? methodNameFilter,
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
                    methodNameFilter, builder, counter, cancellationToken);
            }

            if (namespaceFilter.Contains(containingNamespace))
            {
                // All types in this namespace are already in-scope.
                return;
            }

            foreach (var containgType in namespaceSymbol.GetTypeMembers())
            {
                if (TypeMightContainMatches(containgType, containingNamespace, position, senamticModel, methodNameFilter, out var methodNames))
                {
                    counter.IncreaseTotalTypesChecked();

                    if (methodNames.Count == 0)
                    {
                        var methodSymbols = containgType.GetMembers().OfType<IMethodSymbol>();
                        ProcessMethods(containingNamespace, receiverTypeSymbol, senamticModel, position, builder, counter, methodNames, methodSymbols);
                    }
                    else
                    {
                        foreach (var methodName in methodNames)
                        {
                            var methodSymbols = containgType.GetMembers(methodName).OfType<IMethodSymbol>();
                            ProcessMethods(containingNamespace, receiverTypeSymbol, senamticModel, position, builder, counter, methodNames, methodSymbols);
                        }
                    }
                }
            }
        }

        private static void ProcessMethods(
            string containingNamespace,
            ITypeSymbol receiverTypeSymbol,
            SemanticModel senamticModel,
            int position,
            ArrayBuilder<SerializableImportCompletionItem> builder,
            StatisticCounter counter,
            MultiDictionary<string, string>.ValueSet methodNames,
            IEnumerable<IMethodSymbol> methodSymbols)
        {
            foreach (var methodSymbol in methodSymbols)
            {
                counter.IncreaseTotalExtensionMethodsChecked();

                if (TryGetMatchingExtensionMethod(methodSymbol, methodNames, senamticModel, position, receiverTypeSymbol, out var matchedMethodSymbol))
                {
                    var symbolKeyData = SymbolKey.CreateString(matchedMethodSymbol);
                    builder.Add(new SerializableImportCompletionItem(
                        symbolKeyData,
                        matchedMethodSymbol.Name,
                        matchedMethodSymbol.Arity,
                        matchedMethodSymbol.GetGlyph(),
                        containingNamespace));
                }
            }
        }

        private static bool TypeMightContainMatches(
            INamedTypeSymbol containingTypeSymbol,
            string containingNamespace,
            int position,
            SemanticModel senamticModel,
            MultiDictionary<string, string>? methodNameFilter,
            out MultiDictionary<string, string>.ValueSet methodNames)
        {
            if (!containingTypeSymbol.MightContainExtensionMethods)
            {
                methodNames = default;
                return false;
            }

            if (methodNameFilter != null)
            {
                var instance = PooledStringBuilder.GetInstance();
                instance.Builder.Append(containingNamespace);
                instance.Builder.Append(".");
                instance.Builder.Append(containingTypeSymbol.MetadataName);
                var fullyQualifiedTypeName = instance.ToStringAndFree();

                methodNames = methodNameFilter[fullyQualifiedTypeName];
                if (methodNames.Count == 0)
                {
                    return false;
                }
            }
            else
            {
                methodNames = default;
            }

            return containingTypeSymbol.DeclaredAccessibility == Accessibility.Public ||
                senamticModel.IsAccessible(position, containingTypeSymbol);
        }

        private static bool TryGetMatchingExtensionMethod(
            IMethodSymbol methodSymbol,
            MultiDictionary<string, string>.ValueSet methodNames,
            SemanticModel senamticModel,
            int position,
            ITypeSymbol receiverTypeSymbol,
            [NotNullWhen(true)] out IMethodSymbol? reducedMethodSymbol)
        {
            reducedMethodSymbol = null;

            if (methodSymbol.IsExtensionMethod &&
                (methodNames.Count == 0 || methodNames.Contains(methodSymbol.Name)) &&
                (methodSymbol.DeclaredAccessibility == Accessibility.Public || senamticModel.IsAccessible(position, methodSymbol)))
            {
                reducedMethodSymbol = methodSymbol.ReduceExtensionMethod(receiverTypeSymbol);
            }

            return reducedMethodSymbol != null;
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
    }

    internal sealed class StatisticCounter
    {
        public int TotalTicks;
        public int TotalTypesChecked;
        public int TotalExtensionMethodsChecked;
        public int TotalExtensionMethodsProvided;
        public int GetFilterTicks;
        public int GetSymbolTicks;
        public bool NoFilter;

        public void IncreaseTotalTypesChecked() => TotalTypesChecked++;
        public void IncreaseTotalExtensionMethodsChecked() => TotalExtensionMethodsChecked++;

        // TODO: remove
        public override string ToString()
        {
            return
$@"
TotalTicks: {TotalTicks}
GetFilterTicks : {GetFilterTicks}
GetSymbolTicks : {GetSymbolTicks}
NoFilter : {NoFilter}

TotalTypesChecked : {TotalTypesChecked}
TotalExtensionMethodsChecked : {TotalExtensionMethodsChecked}
TotalExtensionMethodsProvided : {TotalExtensionMethodsProvided}
";
        }

        public void Report()
        {
            CompletionProvidersLogger.LogExtensionMethodCompletionTicksDataPoint(TotalTicks);
            CompletionProvidersLogger.LogExtensionMethodCompletionGetFilterTicksDataPoint(GetFilterTicks);
            CompletionProvidersLogger.LogExtensionMethodCompletionGetSymbolTicksDataPoint(GetSymbolTicks);

            CompletionProvidersLogger.LogExtensionMethodCompletionTypesCheckedDataPoint(TotalTypesChecked);
            CompletionProvidersLogger.LogExtensionMethodCompletionMethodsCheckedDataPoint(TotalExtensionMethodsChecked);
            CompletionProvidersLogger.LogExtensionMethodCompletionMethodsProvidedDataPoint(TotalExtensionMethodsProvided);

            if (NoFilter)
            {
                CompletionProvidersLogger.LogExtensionMethodCompletionNoFilter();
            }
        }
    }
}
