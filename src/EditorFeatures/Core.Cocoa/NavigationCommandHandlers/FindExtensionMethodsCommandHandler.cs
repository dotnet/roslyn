// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor.Host;
using Microsoft.CodeAnalysis.ErrorReporting;
using Microsoft.CodeAnalysis.FindSymbols;
using Microsoft.CodeAnalysis.FindUsages;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.Options;
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
    [Name(nameof(FindExtensionMethodsCommandHandler))]
    internal sealed class FindExtensionMethodsCommandHandler :
        AbstractNavigationCommandHandler<FindExtensionMethodsCommandArgs>
    {
        private readonly IAsynchronousOperationListener _asyncListener;
        private readonly IGlobalOptionService _globalOptions;

        public override string DisplayName => nameof(FindExtensionMethodsCommandHandler);

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public FindExtensionMethodsCommandHandler(
            [ImportMany] IEnumerable<Lazy<IStreamingFindUsagesPresenter>> streamingPresenters,
            IAsynchronousOperationListenerProvider listenerProvider,
            IGlobalOptionService globalOptions)
            : base(streamingPresenters)
        {
            Contract.ThrowIfNull(listenerProvider);

            _asyncListener = listenerProvider.GetListener(FeatureAttribute.FindReferences);
            _globalOptions = globalOptions;
        }

        protected override bool TryExecuteCommand(int caretPosition, Document document, CommandExecutionContext context)
        {
            var streamingPresenter = base.GetStreamingPresenter();
            if (streamingPresenter != null)
            {
                _ = FindExtensionMethodsAsync(document, caretPosition, streamingPresenter);
                return true;
            }

            return false;
        }

        private async Task FindExtensionMethodsAsync(
            Document document, int caretPosition, IStreamingFindUsagesPresenter presenter)
        {
            var solution = document.Project.Solution;
            try
            {
                using var token = _asyncListener.BeginAsyncOperation(nameof(FindExtensionMethodsAsync));

                var (context, cancellationToken) = presenter.StartSearch(EditorFeaturesResources.Navigating, supportsReferences: true);

                try
                {
                    using (Logger.LogBlock(
                        FunctionId.CommandHandler_FindAllReference,
                        KeyValueLogMessage.Create(LogType.UserAction, m => m["type"] = "streaming"),
                        cancellationToken))
                    {
                        var candidateSymbolProjectPair = await FindUsagesHelpers.GetRelevantSymbolAndProjectAtPositionAsync(document, caretPosition, cancellationToken).ConfigureAwait(false);

                        var symbol = candidateSymbolProjectPair?.symbol as INamedTypeSymbol;

                        // if we didn't get the right symbol, just abort
                        if (symbol == null)
                            return;

                        if (!document.Project.TryGetCompilation(out var compilation))
                            return;

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

                                        var definitionItem = reducedMethod.ToNonClassifiedDefinitionItem(solution, includeHiddenLocations: true);

                                        await context.OnDefinitionFoundAsync(definitionItem, cancellationToken).ConfigureAwait(false);
                                    }
                                }
                            }
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
