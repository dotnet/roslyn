// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Completion.Log;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions.ContextQuery;

namespace Microsoft.CodeAnalysis.Completion.Providers
{
    internal abstract class AbstractExtensionMethodImportCompletionProvider : AbstractImportCompletionProvider
    {
        protected override bool ShouldProvideCompletion(Document document, SyntaxContext syntaxContext)
            => syntaxContext.IsRightOfNameSeparator && IsAddingImportsSupported(document);

        protected abstract bool TryGetReceiverTypeSymbol(SyntaxContext syntaxContext, out ITypeSymbol receiverTypeSymbol);

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
                var assembly = await completionContext.Document.Project.GetCompilationAsync(cancellationToken).ConfigureAwait(false);
                using var disposer = ArrayBuilder<CompletionItem>.GetInstance(out var builder);

                VisitNamespaceSymbol(assembly.GlobalNamespace, string.Empty, receiverTypeSymbol, syntaxContext.SemanticModel, syntaxContext.Position, namespaceInScope, builder, telemetryCounter, cancellationToken);
                completionContext.AddItems(builder);

                telemetryCounter.TotalExtensionMethodsProvided = builder.Count;
            }
        }

        private static void VisitNamespaceSymbol(INamespaceSymbol namespaceSymbol,
            string containingNamespace,
            ITypeSymbol receiverTypeSymbol,
            SemanticModel senamticModel,
            int position,
            HashSet<string> filter,
            ArrayBuilder<CompletionItem> builder,
            TelemetryCounter counter,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            containingNamespace = ConcatNamespace(containingNamespace, namespaceSymbol.Name);

            foreach (var memberNamespace in namespaceSymbol.GetNamespaceMembers())
            {
                VisitNamespaceSymbol(memberNamespace, containingNamespace, receiverTypeSymbol, senamticModel, position, filter, builder, counter, cancellationToken);
            }

            if (filter.Contains(containingNamespace))
            {
                return;
            }

            var extensionMethods = namespaceSymbol.GetTypeMembers()
                .Where(t => t.MightContainExtensionMethods && senamticModel.IsAccessible(position, t))
                .SelectMany(t =>
                {
                    counter.TotalTypesChecked++;
                    return t.GetMembers();
                })
                .OfType<IMethodSymbol>()
                .Where(m => m.IsExtensionMethod && senamticModel.IsAccessible(position, m))
                .Select(m =>
                {
                    counter.TotalExtensionMethodsChecked++;
                    return m.ReduceExtensionMethod(receiverTypeSymbol);
                })
                .Where(m => m != null)
                .Select(m => ImportCompletionItem.Create(m, containingNamespace, "<>"));

            builder.AddRange(extensionMethods);
        }

        private static string ConcatNamespace(string containingNamespace, string name)
        {
            Debug.Assert(name != null);
            if (string.IsNullOrEmpty(containingNamespace))
            {
                return name;
            }

            return containingNamespace + "." + name;
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
