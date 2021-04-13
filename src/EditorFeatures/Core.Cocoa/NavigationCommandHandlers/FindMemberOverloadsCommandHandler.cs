// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Linq;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor.FindUsages;
using Microsoft.CodeAnalysis.Editor.Host;
using Microsoft.CodeAnalysis.ErrorReporting;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.VisualStudio.Commanding;
using Microsoft.VisualStudio.Text.Editor.Commanding.Commands.Navigation;
using Microsoft.VisualStudio.Utilities;
using Roslyn.Utilities;
using VSCommanding = Microsoft.VisualStudio.Commanding;
using Microsoft.CodeAnalysis.Host.Mef;
using System.Threading;

namespace Microsoft.CodeAnalysis.Editor.Implementation.NavigationCommandHandlers
{
    [Export(typeof(VSCommanding.ICommandHandler))]
    [ContentType(ContentTypeNames.RoslynContentType)]
    [Name(nameof(FindMemberOverloadsCommandHandler))]
    internal sealed class FindMemberOverloadsCommandHandler :
        AbstractNavigationCommandHandler<FindMemberOverloadsCommandArgs>
    {
        private readonly IAsynchronousOperationListener _asyncListener;

        public override string DisplayName => nameof(FindMemberOverloadsCommandHandler);

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public FindMemberOverloadsCommandHandler(
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
                _ = FindMemberOverloadsAsync(document, caretPosition, streamingPresenter, CancellationToken.None);
                return true;
            }

            return false;
        }

        private async Task FindMemberOverloadsAsync(
            Document document, int caretPosition, IStreamingFindUsagesPresenter presenter, CancellationToken cancellationToken)
        {
            try
            {
                using var token = _asyncListener.BeginAsyncOperation(nameof(FindMemberOverloadsAsync));

                var context = presenter.StartSearch(
                    EditorFeaturesResources.Navigating, supportsReferences: true, cancellationToken);

                using (Logger.LogBlock(
                    FunctionId.CommandHandler_FindAllReference,
                    KeyValueLogMessage.Create(LogType.UserAction, m => m["type"] = "streaming"),
                    context.CancellationToken))
                {
                    try
                    {
#pragma warning disable CA2007 // Consider calling ConfigureAwait on the awaited task
                        var candidateSymbolProjectPair = await FindUsagesHelpers.GetRelevantSymbolAndProjectAtPositionAsync(document, caretPosition, context.CancellationToken);
#pragma warning restore CA2007 // Consider calling ConfigureAwait on the awaited task

                        // we need to get the containing type (i.e. class)
                        var symbol = candidateSymbolProjectPair?.symbol;

                        // if we didn't get any symbol, that's it
                        if (symbol == null || symbol.ContainingType == null)
                            return;

                        foreach (var curSymbol in symbol.ContainingType.GetMembers()
                                                        .Where(m => m.Kind == symbol.Kind && m.Name == symbol.Name))
                        {
                            var definitionItem = curSymbol.ToNonClassifiedDefinitionItem(document.Project.Solution, true);
#pragma warning disable CA2007 // Consider calling ConfigureAwait on the awaited task
                            await context.OnDefinitionFoundAsync(definitionItem);
#pragma warning restore CA2007 // Consider calling ConfigureAwait on the awaited task
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
