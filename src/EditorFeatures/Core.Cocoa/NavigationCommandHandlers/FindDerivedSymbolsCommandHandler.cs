// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Linq;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor.FindUsages;
using Microsoft.CodeAnalysis.Editor.Host;
using Microsoft.CodeAnalysis.ErrorReporting;
using Microsoft.CodeAnalysis.FindSymbols;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.VisualStudio.Commanding;
using Microsoft.VisualStudio.Text.Editor.Commanding.Commands.Navigation;
using Microsoft.VisualStudio.Utilities;
using Roslyn.Utilities;
using VSCommanding = Microsoft.VisualStudio.Commanding;
using Microsoft.CodeAnalysis.Host.Mef;

namespace Microsoft.CodeAnalysis.Editor.Implementation.NavigationCommandHandlers
{
    [Export(typeof(VSCommanding.ICommandHandler))]
    [ContentType(ContentTypeNames.RoslynContentType)]
    [Name(nameof(FindDerivedSymbolsCommandHandler))]
    internal sealed class FindDerivedSymbolsCommandHandler :
        AbstractNavigationCommandHandler<FindDerivedSymbolsCommandArgs>
    {
        private readonly IAsynchronousOperationListener _asyncListener;

        public override string DisplayName => nameof(FindDerivedSymbolsCommandHandler);

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public FindDerivedSymbolsCommandHandler(
            [ImportMany] IEnumerable<Lazy<IStreamingFindUsagesPresenter>> streamingPresenters,
            IAsynchronousOperationListenerProvider listenerProvider)
            : base(streamingPresenters)
        {
            Contract.ThrowIfNull(listenerProvider);

            _asyncListener = listenerProvider.GetListener(FeatureAttribute.FindReferences);
        }

        protected override bool TryExecuteCommand(int caretPosition, Document document, CommandExecutionContext context)
        {
            var streamingPresenter = base.GetStreamingPresenter();
            if (streamingPresenter != null)
            {
                // Fire and forget.  So no need for cancellation.
                _ = FindDerivedSymbolsAsync(document, caretPosition, streamingPresenter, CancellationToken.None);
                return true;
            }

            return false;
        }

        private static async Task<IEnumerable<ISymbol>> GatherSymbolsAsync(ISymbol symbol, Solution solution, CancellationToken cancellationToken)
        {
            // if the symbol is in an interface, or if it is an interface
            // we can use the FindInterfaceImplementationAsync call
            if (symbol.ContainingType is INamedTypeSymbol namedTypeSymbol && symbol.ContainingType.TypeKind == TypeKind.Interface)
            {
                return (await SymbolFinder.FindImplementationsAsync(namedTypeSymbol, solution, null, cancellationToken).ConfigureAwait(false)).OfType<ISymbol>();
            }
            else if (symbol is INamedTypeSymbol namedTypeSymbol2 && namedTypeSymbol2.TypeKind == TypeKind.Interface)
            {
                return (await SymbolFinder.FindImplementationsAsync(namedTypeSymbol2, solution, null, cancellationToken).ConfigureAwait(false)).OfType<ISymbol>();
            }
            // if it's not, but is instead a class, we can use FindDerivedClassesAsync
            else if (symbol is INamedTypeSymbol namedTypeSymbol3)
            {
                return (await SymbolFinder.FindDerivedClassesAsync(namedTypeSymbol3, solution, null, cancellationToken).ConfigureAwait(false)).OfType<ISymbol>();
            }
            // and lastly, if it's a method, we can use FindOverridesAsync
            else
            {
#pragma warning disable CA2007 // Consider calling ConfigureAwait on the awaited task
                return await SymbolFinder.FindOverridesAsync(symbol, solution, null, cancellationToken);
#pragma warning restore CA2007 // Consider calling ConfigureAwait on the awaited task
            }
        }

        private async Task FindDerivedSymbolsAsync(
            Document document, int caretPosition, IStreamingFindUsagesPresenter presenter, CancellationToken cancellationToken)
        {
            try
            {
                using var token = _asyncListener.BeginAsyncOperation(nameof(FindDerivedSymbolsAsync));

                var context = presenter.StartSearch(EditorFeaturesResources.Navigating, supportsReferences: true, cancellationToken);
                try
                {
                    using (Logger.LogBlock(
                        FunctionId.CommandHandler_FindAllReference,
                        KeyValueLogMessage.Create(LogType.UserAction, m => m["type"] = "streaming"),
                        context.CancellationToken))
                    {
#pragma warning disable CA2007 // Consider calling ConfigureAwait on the awaited task
                        var candidateSymbolProjectPair = await FindUsagesHelpers.GetRelevantSymbolAndProjectAtPositionAsync(document, caretPosition, context.CancellationToken);
#pragma warning restore CA2007 // Consider calling ConfigureAwait on the awaited task
                        if (candidateSymbolProjectPair?.symbol == null)
                            return;

#pragma warning disable CA2007 // Consider calling ConfigureAwait on the awaited task
                        var candidates = await GatherSymbolsAsync(candidateSymbolProjectPair.Value.symbol,
                            document.Project.Solution, context.CancellationToken);
#pragma warning restore CA2007 // Consider calling ConfigureAwait on the awaited task

                        foreach (var candidate in candidates)
                        {
                            var definitionItem = candidate.ToNonClassifiedDefinitionItem(document.Project.Solution, true);
#pragma warning disable CA2007 // Consider calling ConfigureAwait on the awaited task
                            await context.OnDefinitionFoundAsync(definitionItem);
#pragma warning restore CA2007 // Consider calling ConfigureAwait on the awaited task
                        }
                    }
                }
                finally
                {
                    await context.OnCompletedAsync().ConfigureAwait(false);
                }
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception e) when (FatalError.ReportAndCatch(e))
            {
            }
        }
    }
}
