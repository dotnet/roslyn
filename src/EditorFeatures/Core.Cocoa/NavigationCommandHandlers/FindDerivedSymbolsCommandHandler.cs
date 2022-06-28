// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor.Host;
using Microsoft.CodeAnalysis.ErrorReporting;
using Microsoft.CodeAnalysis.FindSymbols;
using Microsoft.CodeAnalysis.FindUsages;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.VisualStudio.Commanding;
using Microsoft.VisualStudio.Text.Editor.Commanding.Commands.Navigation;
using Microsoft.VisualStudio.Utilities;
using Roslyn.Utilities;
using VSCommanding = Microsoft.VisualStudio.Commanding;

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
                _ = FindDerivedSymbolsAsync(document, caretPosition, streamingPresenter);
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
                return await SymbolFinder.FindImplementationsAsync(namedTypeSymbol, solution, null, cancellationToken).ConfigureAwait(false);
            }
            else if (symbol is INamedTypeSymbol namedTypeSymbol2 && namedTypeSymbol2.TypeKind == TypeKind.Interface)
            {
                return await SymbolFinder.FindImplementationsAsync(namedTypeSymbol2, solution, null, cancellationToken).ConfigureAwait(false);
            }
            // if it's not, but is instead a class, we can use FindDerivedClassesAsync
            else if (symbol is INamedTypeSymbol namedTypeSymbol3)
            {
                return await SymbolFinder.FindDerivedClassesAsync(namedTypeSymbol3, solution, null, cancellationToken).ConfigureAwait(false);
            }
            // and lastly, if it's a method, we can use FindOverridesAsync
            else
            {
                return await SymbolFinder.FindOverridesAsync(symbol, solution, null, cancellationToken).ConfigureAwait(false);
            }
        }

        private async Task FindDerivedSymbolsAsync(
            Document document, int caretPosition, IStreamingFindUsagesPresenter presenter)
        {
            try
            {
                using var token = _asyncListener.BeginAsyncOperation(nameof(FindDerivedSymbolsAsync));

                var (context, cancellationToken) = presenter.StartSearch(EditorFeaturesResources.Navigating, supportsReferences: true);
                try
                {
                    using (Logger.LogBlock(
                        FunctionId.CommandHandler_FindAllReference,
                        KeyValueLogMessage.Create(LogType.UserAction, m => m["type"] = "streaming"),
                        cancellationToken))
                    {
                        var candidateSymbolProjectPair = await FindUsagesHelpers.GetRelevantSymbolAndProjectAtPositionAsync(document, caretPosition, cancellationToken).ConfigureAwait(false);
                        if (candidateSymbolProjectPair?.symbol == null)
                            return;

                        var candidates = await GatherSymbolsAsync(candidateSymbolProjectPair.Value.symbol,
                            document.Project.Solution, cancellationToken).ConfigureAwait(false);

                        foreach (var candidate in candidates)
                        {
                            var definitionItem = candidate.ToNonClassifiedDefinitionItem(document.Project.Solution, includeHiddenLocations: true);
                            await context.OnDefinitionFoundAsync(definitionItem, cancellationToken).ConfigureAwait(false);
                        }
                    }
                }
                finally
                {
                    await context.OnCompletedAsync(cancellationToken).ConfigureAwait(false);
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
