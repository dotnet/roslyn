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

                var tick = Environment.TickCount;
                using var allTypeNamesDisposer = PooledHashSet<string>.GetInstance(out var allTypeNames);
                allTypeNames.Add(receiverTypeSymbol.MetadataName);
                allTypeNames.AddRange(receiverTypeSymbol.GetBaseTypes().Concat(receiverTypeSymbol.GetAllInterfacesIncludingThis()).Select(s => s.MetadataName));

                // interface doesn't inherit from object, but is implicitly convertable to object type.
                if (receiverTypeSymbol.IsInterfaceType())
                {
                    allTypeNames.Add("Object");
                }

                var matchedMethods = await GetPossibleExtensionMethodMatchesAsync(project, allTypeNames, cancellationToken).ConfigureAwait(false);
                telemetryCounter.TotalTypesChecked = Environment.TickCount - tick;

                var assembly = (await project.GetCompilationAsync(cancellationToken).ConfigureAwait(false))!;
                using var itemsDisposer = ArrayBuilder<CompletionItem>.GetInstance(out var items);

                tick = Environment.TickCount;
                VisitNamespaceSymbol(assembly.GlobalNamespace, string.Empty, receiverTypeSymbol,
                    syntaxContext.SemanticModel, syntaxContext.Position, namespaceInScope, matchedMethods, items, telemetryCounter,
                    cancellationToken);
                telemetryCounter.TotalExtensionMethodsChecked = Environment.TickCount - tick;

                completionContext.AddItems(items);

                telemetryCounter.TotalExtensionMethodsProvided = items.Count;
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

        /// <summary>
        /// Returns a multi-dictionary of mappings "FQN of containing type" => "extension method names"
        /// </summary>
        private static async Task<MultiDictionary<string, string>> GetPossibleExtensionMethodMatchesAsync(
            Project currentProject,
            ISet<string> targetTypeNames,
            CancellationToken cancellationToken)
        {
            var solution = currentProject.Solution;
            var graph = currentProject.Solution.GetProjectDependencyGraph();
            var dependencies = graph.GetProjectsThatThisProjectTransitivelyDependsOn(currentProject.Id);

#nullable restore
            var relevantProjects = dependencies.Select(solution.GetProject)
                                                 .Where(p => p.SupportsCompilation)
                                                 .Concat(currentProject);

            var results = new MultiDictionary<string, string>();

            // Find matching extension methods from source.
            foreach (var project in relevantProjects)
            {
                foreach (var document in project.Documents)
                {
#nullable enable
                    var info = await document.GetSyntaxTreeIndexAsync(cancellationToken).ConfigureAwait(false);
                    if (!info.ContainsExtensionMethod)
                    {
                        continue;
                    }

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
            }

            // Find matching extension methods from metadata
            foreach (var peReference in currentProject.MetadataReferences.OfType<PortableExecutableReference>())
            {
                var info = await SymbolTreeInfo.GetInfoForMetadataReferenceAsync(
                    solution, peReference, loadOnly: false, cancellationToken).ConfigureAwait(false);

                if (!info.ContainsExtensionMethod)
                {
                    continue;
                }

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
