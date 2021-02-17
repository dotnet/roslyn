// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor.FindUsages;
using Microsoft.CodeAnalysis.Editor.Host;
using Microsoft.CodeAnalysis.ErrorReporting;
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
    [Name(nameof(FindBaseSymbolsCommandHandler))]
    internal sealed class FindBaseSymbolsCommandHandler :
        AbstractNavigationCommandHandler<FindBaseSymbolsCommandArgs>
    {
        private readonly IAsynchronousOperationListener _asyncListener;

        public override string DisplayName => nameof(FindBaseSymbolsCommandHandler);

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public FindBaseSymbolsCommandHandler(
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
                _ = StreamingFindBaseSymbolsAsync(document, caretPosition, streamingPresenter, CancellationToken.None);
                return true;
            }

            return false;
        }

        private async Task StreamingFindBaseSymbolsAsync(
            Document document, int caretPosition,
            IStreamingFindUsagesPresenter presenter,
            CancellationToken cancellationToken)
        {
            try
            {
                using var token = _asyncListener.BeginAsyncOperation(nameof(StreamingFindBaseSymbolsAsync));

                var context = presenter.StartSearch(EditorFeaturesResources.Navigating, supportsReferences: true, cancellationToken);

                using (Logger.LogBlock(
                    FunctionId.CommandHandler_FindAllReference,
                    KeyValueLogMessage.Create(LogType.UserAction, m => m["type"] = "streaming"),
                    context.CancellationToken))
                {
                    try
                    {
#pragma warning disable CA2007 // Consider calling ConfigureAwait on the awaited task
                        var relevantSymbol = await FindUsagesHelpers.GetRelevantSymbolAndProjectAtPositionAsync(document, caretPosition, context.CancellationToken);
#pragma warning restore CA2007 // Consider calling ConfigureAwait on the awaited task

                        var overriddenSymbol = relevantSymbol?.symbol.GetOverriddenMember();

                        while (overriddenSymbol != null)
                        {
                            if (context.CancellationToken.IsCancellationRequested)
                            {
                                return;
                            }

                            var definitionItem = overriddenSymbol.ToNonClassifiedDefinitionItem(document.Project.Solution, true);
#pragma warning disable CA2007 // Consider calling ConfigureAwait on the awaited task
                            await context.OnDefinitionFoundAsync(definitionItem);
#pragma warning restore CA2007 // Consider calling ConfigureAwait on the awaited task

                            // try getting the next one
                            overriddenSymbol = overriddenSymbol.GetOverriddenMember();
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
