﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor.FindUsages;
using Microsoft.CodeAnalysis.Editor.Host;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.ErrorReporting;
using Microsoft.CodeAnalysis.FindSymbols;
using Microsoft.CodeAnalysis.Host.Mef;
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
    [Name(nameof(FindReferencesOfOverloadsCommandHandler))]
    internal sealed class FindReferencesOfOverloadsCommandHandler :
        AbstractNavigationCommandHandler<FindReferencesOfOverloadsCommandArgs>
    {
        private readonly IAsynchronousOperationListener _asyncListener;
        private readonly IThreadingContext _threadingContext;

        public override string DisplayName => nameof(FindReferencesOfOverloadsCommandHandler);

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public FindReferencesOfOverloadsCommandHandler(
            [ImportMany] IEnumerable<Lazy<IStreamingFindUsagesPresenter>> streamingPresenters,
            IAsynchronousOperationListenerProvider listenerProvider,
            IThreadingContext threadingContext)
            : base(streamingPresenters)
        {
            Contract.ThrowIfNull(streamingPresenters);
            Contract.ThrowIfNull(listenerProvider);
            Contract.ThrowIfNull(threadingContext);

            _asyncListener = listenerProvider.GetListener(FeatureAttribute.FindReferences);
            _threadingContext = threadingContext;
        }

        protected override bool TryExecuteCommand(int caretPosition, Document document, CommandExecutionContext context)
        {
            var streamingService = document.GetLanguageService<IFindUsagesService>();
            var streamingPresenter = GetStreamingPresenter();

            //See if we're running on a host that can provide streaming results.
            // We'll both need a FAR service that can stream results to us, and
            // a presenter that can accept streamed results.
            if (streamingService != null && streamingPresenter != null)
            {
                // Fire and forget.  So no need for cancellation.
                _ = StreamingFindReferencesAsync(document, caretPosition, streamingPresenter, CancellationToken.None);
                return true;
            }

            return false;
        }

        private static async Task<ISymbol[]> GatherSymbolsAsync(ISymbol symbol, Microsoft.CodeAnalysis.Solution solution, CancellationToken token)
        {
#pragma warning disable CA2007 // Consider calling ConfigureAwait on the awaited task
            var implementations = await SymbolFinder.FindImplementationsAsync(symbol, solution, null, token);
#pragma warning restore CA2007 // Consider calling ConfigureAwait on the awaited task
            var result = new ISymbol[implementations.Count() + 1];
            result[0] = symbol;
            var i = 1;
            foreach (var item in implementations)
            {
                result[i++] = item;
            }
            return result;
        }

        private async Task StreamingFindReferencesAsync(
            Document document, int caretPosition, IStreamingFindUsagesPresenter presenter, CancellationToken cancellationToken)
        {
            try
            {
                // first, let's see if we even have a comment, otherwise there's no use in starting a search
#pragma warning disable CA2007 // Consider calling ConfigureAwait on the awaited task
                var relevantSymbol = await FindUsagesHelpers.GetRelevantSymbolAndProjectAtPositionAsync(document, caretPosition, new CancellationToken());
#pragma warning restore CA2007 // Consider calling ConfigureAwait on the awaited task
                var symbol = relevantSymbol?.symbol;
                if (symbol == null)
                    return; // would be useful if we could notify the user why we didn't do anything
                            // maybe using something like an info bar?

                var findUsagesService = document.GetLanguageService<IFindUsagesService>();

                using var token = _asyncListener.BeginAsyncOperation(nameof(StreamingFindReferencesAsync));

                var context = presenter.StartSearch(EditorFeaturesResources.Find_References, supportsReferences: true, cancellationToken);

                using (Logger.LogBlock(
                    FunctionId.CommandHandler_FindAllReference,
                    KeyValueLogMessage.Create(LogType.UserAction, m => m["type"] = "streaming"),
                    context.CancellationToken))
                {
                    var symbolsToLookup = new List<ISymbol>();

                    foreach (var curSymbol in symbol.ContainingType.GetMembers()
                                                    .Where(m => m.Kind == symbol.Kind && m.Name == symbol.Name))
                    {
                        Compilation compilation;
                        if (!document.Project.TryGetCompilation(out compilation))
                        {
                            // TODO: should we do anything more here?
                            continue;
                        }

                        foreach (var sym in SymbolFinder.FindSimilarSymbols(curSymbol, compilation, context.CancellationToken))
                        {
                            // assumption here is, that FindSimilarSymbols returns symbols inside same project
#pragma warning disable CA2007 // Consider calling ConfigureAwait on the awaited task
                            var symbolsToAdd = await GatherSymbolsAsync(sym, document.Project.Solution, context.CancellationToken);
#pragma warning restore CA2007 // Consider calling ConfigureAwait on the awaited task
                            symbolsToLookup.AddRange(symbolsToAdd);
                        }
                    }

                    foreach (var candidate in symbolsToLookup)
                    {
#pragma warning disable CA2007 // Consider calling ConfigureAwait on the awaited task
                        await AbstractFindUsagesService.FindSymbolReferencesAsync(context, candidate, document.Project);
#pragma warning restore CA2007 // Consider calling ConfigureAwait on the awaited task
                    }

                    // Note: we don't need to put this in a finally.  The only time we might not hit
                    // this is if cancellation or another error gets thrown.  In the former case,
                    // that means that a new search has started.  We don't care about telling the
                    // context it has completed.  In the latter case something wrong has happened
                    // and we don't want to run any more code in this particular context.
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
