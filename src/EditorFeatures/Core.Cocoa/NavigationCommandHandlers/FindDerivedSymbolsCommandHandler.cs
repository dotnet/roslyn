//
// FindExtensionMethodsCommandHandler.cs
//
// Copyright (c) 2019 Microsoft
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.

using System;
using System.Linq;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor.FindUsages;
using Microsoft.CodeAnalysis.Editor.Host;
using Microsoft.CodeAnalysis.ErrorReporting;
using Microsoft.CodeAnalysis.FindSymbols;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.Shared.Extensions;
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
    public class FindDerivedSymbolsCommandHandler :
        AbstractNavigationCommandHandler<FindDerivedSymbolsCommandArgs>
    {
        private readonly IAsynchronousOperationListener _asyncListener;

        public override string DisplayName => nameof(FindDerivedSymbolsCommandHandler);

        [ImportingConstructor]
        internal FindDerivedSymbolsCommandHandler(
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

        private async Task<IEnumerable<ISymbol>> GatherSymbolsAsync(ISymbol symbol, Solution solution, CancellationToken cancellationToken)
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
                return await SymbolFinder.FindOverridesAsync(symbol, solution, null, cancellationToken);
            }
        }

        private async Task FindDerivedSymbolsAsync(
                  Document document, int caretPosition,
                  IStreamingFindUsagesPresenter presenter)
        {
            try
            {
                using (var token = _asyncListener.BeginAsyncOperation(nameof(FindDerivedSymbolsAsync)))
                {
                    // Let the presented know we're starting a search.
                    var context = presenter.StartSearch(
                        EditorFeaturesResources.Navigating, supportsReferences: true);
                    try
                    {
                        using (Logger.LogBlock(
                            FunctionId.CommandHandler_FindAllReference,
                            KeyValueLogMessage.Create(LogType.UserAction, m => m["type"] = "streaming"),
                            context.CancellationToken))
                        {
                            var candidateSymbolProjectPair = await FindUsagesHelpers.GetRelevantSymbolAndProjectAtPositionAsync(document, caretPosition, context.CancellationToken);
                            if (candidateSymbolProjectPair?.symbol == null)
                                return;

                            var candidates = await GatherSymbolsAsync(candidateSymbolProjectPair.Value.symbol,
                                document.Project.Solution, context.CancellationToken);

                            foreach (var candidate in candidates)
                            {
                                var definitionItem = candidate.ToNonClassifiedDefinitionItem(document.Project.Solution, true);
                                await context.OnDefinitionFoundAsync(definitionItem);
                            }
                        }
                    }
                    finally
                    {
                        await context.OnCompletedAsync().ConfigureAwait(false);
                    }
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
