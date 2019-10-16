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
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.Extensions.ContextQuery;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Completion.Providers
{
    internal abstract class AbstractExtensionMethodImportCompletionProvider : AbstractImportCompletionProvider
    {
        protected abstract string GenericSuffix { get; }

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
            if (TryGetReceiverTypeSymbol(syntaxContext, out var receiverTypeSymbol))
            {
                using var telemetryCounter = new TelemetryCounter();
                var ticks = Environment.TickCount;

                var project = completionContext.Document.Project;
                var assembly = (await project.GetCompilationAsync(cancellationToken).ConfigureAwait(false))!;

                using var allTypeNamesDisposer = PooledHashSet<string>.GetInstance(out var allTypeNames);
                allTypeNames.Add(receiverTypeSymbol.MetadataName);
                allTypeNames.AddRange(receiverTypeSymbol.GetBaseTypes().Concat(receiverTypeSymbol.GetAllInterfacesIncludingThis()).Select(s => s.MetadataName));

                // interface doesn't inherit from object, but is implicitly convertable to object type.
                if (receiverTypeSymbol.IsInterfaceType())
                {
                    allTypeNames.Add("Object");
                }

                // We don't want to wait for creating indices from scratch unless user explicitly asked for unimported items (via expander).
                var (matchedMethods, totalComplexTypes) = await ExtensionMethodFilteringService.GetPossibleExtensionMethodMatchesAsync(
                    project, allTypeNames.ToImmutableArray(), loadOnly: !isExpandedCompletion,
                    cancellationToken).ConfigureAwait(false);

                // If the index isn't ready, we will simply iterate through all extension methods symbols.
                if (matchedMethods == null)
                {
                    telemetryCounter.NoFilter = true;
                }

                telemetryCounter.TotalComplexTypes = totalComplexTypes;
                telemetryCounter.GetFilterTicks = Environment.TickCount - ticks;

                ticks = Environment.TickCount;
                using var filterItemsDisposer = ArrayBuilder<CompletionItem>.GetInstance(out var filterItems);

                VisitNamespaceSymbol(assembly.GlobalNamespace, string.Empty, receiverTypeSymbol,
                    syntaxContext.SemanticModel, syntaxContext.Position, namespaceInScope, methodNameFilter: matchedMethods, filterItems, telemetryCounter,
                    cancellationToken);

                telemetryCounter.GetSymbolTicks = Environment.TickCount - ticks;
                telemetryCounter.TotalExtensionMethodsProvided = filterItems.Count;

                completionContext.AddItems(filterItems);
            }
        }

        private void VisitNamespaceSymbol(
            INamespaceSymbol namespaceSymbol,
            string containingNamespace,
            ITypeSymbol receiverTypeSymbol,
            SemanticModel senamticModel,
            int position,
            HashSet<string> namespaceFilter,
            MultiDictionary<string, string>? methodNameFilter,
            ArrayBuilder<CompletionItem> builder,
            TelemetryCounter telemetryCounter,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            containingNamespace = CompletionHelper.ConcatNamespace(containingNamespace, namespaceSymbol.Name);

            foreach (var memberNamespace in namespaceSymbol.GetNamespaceMembers())
            {
                VisitNamespaceSymbol(
                    memberNamespace, containingNamespace, receiverTypeSymbol, senamticModel, position, namespaceFilter,
                    methodNameFilter, builder, telemetryCounter, cancellationToken);
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
                    telemetryCounter.IncreaseTotalTypesChecked();

                    var methodSymbols = containgType.GetMembers().OfType<IMethodSymbol>();
                    foreach (var methodSymbol in methodSymbols)
                    {
                        telemetryCounter.IncreaseTotalExtensionMethodsChecked();

                        if (TryGetMatchingExtensionMethod(methodSymbol, methodNames, senamticModel, position, receiverTypeSymbol, out var matchedMethodSymbol))
                        {
                            builder.Add(ImportCompletionItem.Create(matchedMethodSymbol, containingNamespace, GenericSuffix));
                        }
                    }
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

        private class TelemetryCounter : IDisposable
        {
            public int TotalTypesChecked { get; private set; }
            public int TotalExtensionMethodsChecked { get; private set; }
            public int TotalExtensionMethodsProvided { get; set; }
            public int GetFilterTicks { get; set; }
            public int GetSymbolTicks { get; set; }
            public bool NoFilter { get; set; }
            public int TotalComplexTypes { get; set; }

            public void IncreaseTotalTypesChecked() => TotalTypesChecked++;
            public void IncreaseTotalExtensionMethodsChecked() => TotalExtensionMethodsChecked++;

            public void Dispose()
            {
                // TODO: remove this
                Internal.Log.Logger.Log(Internal.Log.FunctionId.Completion_ExtensionMethodImportCompletionProvider_GetCompletionItemsAsync, Internal.Log.KeyValueLogMessage.Create(m =>
                {
                    m["ExtMethodData"] = this.ToString();
                }));

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

            public override string ToString()
            {
                return
$@"
GetFilterTicks : {GetFilterTicks}
GetSymbolTicks : {GetSymbolTicks}
NoFilter : {NoFilter}

TotalTypesChecked : {TotalTypesChecked}
TotalExtensionMethodsChecked : {TotalExtensionMethodsChecked}
TotalExtensionMethodsProvided : {TotalExtensionMethodsProvided}
TotalComplexTypes : {TotalComplexTypes}
";
            }
        }
    }
}
