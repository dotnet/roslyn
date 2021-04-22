// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Linq;
using System.Collections.Generic;
using System.ComponentModel.Composition;
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
using Microsoft.CodeAnalysis.Host.Mef;
using System.Threading;

namespace Microsoft.CodeAnalysis.Editor.Implementation.NavigationCommandHandlers
{
    [Export(typeof(VSCommanding.ICommandHandler))]
    [ContentType(ContentTypeNames.RoslynContentType)]
    [Name(nameof(FindExtensionMethodsCommandHandler))]
    internal sealed class FindExtensionMethodsCommandHandler :
        AbstractNavigationCommandHandler<FindExtensionMethodsCommandArgs>
    {
        private readonly IAsynchronousOperationListener _asyncListener;

        public override string DisplayName => nameof(FindExtensionMethodsCommandHandler);

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public FindExtensionMethodsCommandHandler(
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
                _ = FindExtensionMethodsAsync(document, caretPosition, streamingPresenter, CancellationToken.None);
                return true;
            }

            return false;
        }

        private async Task FindExtensionMethodsAsync(
            Document document, int caretPosition, IStreamingFindUsagesPresenter presenter, CancellationToken cancellationToken)
        {
            try
            {
                using var token = _asyncListener.BeginAsyncOperation(nameof(FindExtensionMethodsAsync));

                var (context, combinedCancellationToken) = presenter.StartSearch(EditorFeaturesResources.Navigating, supportsReferences: true, cancellationToken);
                cancellationToken = combinedCancellationToken;

                using (Logger.LogBlock(
                    FunctionId.CommandHandler_FindAllReference,
                    KeyValueLogMessage.Create(LogType.UserAction, m => m["type"] = "streaming"),
                    cancellationToken))
                {
#pragma warning disable CA2007 // Consider calling ConfigureAwait on the awaited task
                    var candidateSymbolProjectPair = await FindUsagesHelpers.GetRelevantSymbolAndProjectAtPositionAsync(document, caretPosition, cancellationToken);
#pragma warning restore CA2007 // Consider calling ConfigureAwait on the awaited task

                    var symbol = candidateSymbolProjectPair?.symbol as INamedTypeSymbol;

                    // if we didn't get the right symbol, just abort
                    if (symbol == null)
                    {
                        await context.OnCompletedAsync(cancellationToken).ConfigureAwait(false);
                        return;
                    }

                    Compilation compilation;
                    if (!document.Project.TryGetCompilation(out compilation))
                    {
                        await context.OnCompletedAsync(cancellationToken).ConfigureAwait(false);
                        return;
                    }

                    var solution = document.Project.Solution;

                    foreach (var type in compilation.Assembly.GlobalNamespace.GetAllTypes(cancellationToken))
                    {
                        if (!type.MightContainExtensionMethods)
                            continue;

                        foreach (var extMethod in type.GetMembers().OfType<IMethodSymbol>().Where(method => method.IsExtensionMethod))
                        {
                            if (cancellationToken.IsCancellationRequested)
                                break;

                            var reducedMethod = extMethod.ReduceExtensionMethod(symbol);
                            if (reducedMethod != null)
                            {
                                var loc = extMethod.Locations.First();

                                var sourceDefinition = await SymbolFinder.FindSourceDefinitionAsync(reducedMethod, solution, cancellationToken).ConfigureAwait(false);

                                // And if our definition actually is from source, then let's re-figure out what project it came from
                                if (sourceDefinition != null)
                                {
                                    var originatingProject = solution.GetProject(sourceDefinition.ContainingAssembly, cancellationToken);

                                    var definitionItem = reducedMethod.ToNonClassifiedDefinitionItem(solution, true);

#pragma warning disable CA2007 // Consider calling ConfigureAwait on the awaited task
                                    await context.OnDefinitionFoundAsync(definitionItem, cancellationToken);
#pragma warning restore CA2007 // Consider calling ConfigureAwait on the awaited task
                                }
                            }
                        }
                    }

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
