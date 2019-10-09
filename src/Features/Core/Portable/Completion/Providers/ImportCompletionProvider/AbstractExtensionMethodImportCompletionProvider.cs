// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

#nullable enable

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Completion.Log;
using Microsoft.CodeAnalysis.FindSymbols;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.Extensions.ContextQuery;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Completion.Providers
{
    internal abstract class AbstractExtensionMethodImportCompletionProvider : AbstractImportCompletionProvider
    {
        protected abstract bool TryGetReceiverTypeSymbol(SyntaxContext syntaxContext, [NotNullWhen(true)] out ITypeSymbol? receiverTypeSymbol);

        protected override bool ShouldProvideCompletion(Document document, SyntaxContext syntaxContext)
            => syntaxContext.IsRightOfNameSeparator && IsAddingImportsSupported(document);

        protected async override Task AddCompletionItemsAsync(
            CompletionContext completionContext,
            SyntaxContext syntaxContext,
            HashSet<string> namespaceInScope,
            bool isExpandedCompletion,
            CancellationToken cancellationToken)
        {
            using var telemetryCounter = new TelemetryCounter();

            if (TryGetReceiverTypeSymbol(syntaxContext, out var receiverTypeSymbol))
            {
                var project = completionContext.Document.Project;

                using var allTypeNamesDisposer = PooledHashSet<string>.GetInstance(out var allTypeNames);
                allTypeNames.Add(receiverTypeSymbol.MetadataName);
                allTypeNames.AddRange(receiverTypeSymbol.GetBaseTypes().Concat(receiverTypeSymbol.GetAllInterfacesIncludingThis()).Select(s => s.MetadataName));

                // interface doesn't inherit from object, but is implicitly convertable to object type.
                if (receiverTypeSymbol.IsInterfaceType())
                {
                    allTypeNames.Add("Object");
                }

                // We don't want to wait for creating indices from scratch unless user explicitly asked for unimported items (via expander).
                var matchedMethods = await GetPossibleExtensionMethodMatchesAsync(project, allTypeNames, loadOnly: !isExpandedCompletion, cancellationToken).ConfigureAwait(false);
                if (matchedMethods == null)
                {
                    return;
                }

                var assembly = (await project.GetCompilationAsync(cancellationToken).ConfigureAwait(false))!;
                using var itemsDisposer = ArrayBuilder<CompletionItem>.GetInstance(out var items);

                VisitNamespaceSymbol(assembly.GlobalNamespace, string.Empty, receiverTypeSymbol,
                    syntaxContext.SemanticModel, syntaxContext.Position, namespaceInScope, matchedMethods, items, telemetryCounter,
                    cancellationToken);

                completionContext.AddItems(items);
            }
        }

        /// <summary>
        /// Returns a multi-dictionary of mappings "FQN of containing type" => "extension method names"
        /// </summary>
        private static async Task<MultiDictionary<string, string>?> GetPossibleExtensionMethodMatchesAsync(
            Project currentProject,
            ISet<string> targetTypeNames,
            bool loadOnly,
            CancellationToken cancellationToken)
        {
            var solution = currentProject.Solution;
            var graph = currentProject.Solution.GetProjectDependencyGraph();
            var relevantProjectIds = graph.GetProjectsThatThisProjectTransitivelyDependsOn(currentProject.Id)
                                        .Concat(currentProject.Id);

            using var syntaxDisposer = ArrayBuilder<SyntaxTreeIndex>.GetInstance(out var syntaxTreeIndexBuilder);
            using var symbolDisposer = ArrayBuilder<SymbolTreeInfo>.GetInstance(out var symbolTreeInfoBuilder);
            var peReferences = PooledHashSet<PortableExecutableReference>.GetInstance();

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
                    foreach (var targetTypeName in targetTypeNames)
                    {
                        foreach (var methodInfo in info.GetMatchingExtensionMethodInfo(targetTypeName))
                        {
                            results.Add(methodInfo.FullyQualifiedContainerName, methodInfo.Name);
                        }
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
            HashSet<string> namespaceFilter,
            MultiDictionary<string, string> methodNameFilter,
            ArrayBuilder<CompletionItem> builder,
            TelemetryCounter counter,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            containingNamespace = CompletionHelper.ConcatNamespace(containingNamespace, namespaceSymbol.Name);

            foreach (var memberNamespace in namespaceSymbol.GetNamespaceMembers())
            {
                VisitNamespaceSymbol(memberNamespace, containingNamespace, receiverTypeSymbol, senamticModel, position, namespaceFilter, methodNameFilter, builder, counter, cancellationToken);
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
                    var methodSymbols = containgType.GetMembers().OfType<IMethodSymbol>();
                    foreach (var methodSymbol in methodSymbols)
                    {
                        if (TryGetMatchingExtensionMethod(methodSymbol, methodNames, senamticModel, position, receiverTypeSymbol, out var matchedMethodSymbol))
                        {
                            builder.Add(ImportCompletionItem.Create(matchedMethodSymbol, containingNamespace, "<>"));
                        }
                    }
                }
            }

            static bool TypeMightContainMatches(
                INamedTypeSymbol containingTypeSymbol,
                string containingNamespace,
                int position,
                SemanticModel senamticModel,
                MultiDictionary<string, string> methodNameFilter,
                out MultiDictionary<string, string>.ValueSet methodNames)
            {
                if (!containingTypeSymbol.MightContainExtensionMethods)
                {
                    methodNames = default;
                    return false;
                }

                var instance = PooledStringBuilder.GetInstance();
                instance.Builder.Append(containingNamespace);
                instance.Builder.Append(".");
                instance.Builder.Append(containingTypeSymbol.MetadataName);
                var fullyQualifiedTypeName = instance.ToStringAndFree();

                methodNames = methodNameFilter[fullyQualifiedTypeName];

                return methodNames.Count > 0 && senamticModel.IsAccessible(position, containingTypeSymbol);
            }
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
            if (methodSymbol.IsExtensionMethod && methodNames.Contains(methodSymbol.Name) && senamticModel.IsAccessible(position, methodSymbol))
            {
                reducedMethodSymbol = methodSymbol.ReduceExtensionMethod(receiverTypeSymbol);
            }

            return reducedMethodSymbol != null;
        }

        private class TelemetryCounter : IDisposable
        {
            public int TotalTypesChecked { get; set; }
            public int TotalExtensionMethodsChecked { get; set; }
            public int TotalExtensionMethodsProvided { get; set; }
            protected int Tick { get; }

            public TelemetryCounter()
            {
                Tick = Environment.TickCount;
            }

            public void Dispose()
            {
                var delta = Environment.TickCount - Tick;
                CompletionProvidersLogger.LogExtensionMethodCompletionTicksDataPoint(delta);
                CompletionProvidersLogger.LogExtensionMethodCompletionTypesCheckedDataPoint(TotalTypesChecked);
                CompletionProvidersLogger.LogExtensionMethodCompletionMethodsCheckedDataPoint(TotalExtensionMethodsChecked);
                CompletionProvidersLogger.LogExtensionMethodCompletionMethodsProvidedDataPoint(TotalExtensionMethodsProvided);
            }
        }
    }
}
