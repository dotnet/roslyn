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

namespace Microsoft.CodeAnalysis.Editor.Implementation.NavigationCommandHandlers
{
    [Export(typeof(VSCommanding.ICommandHandler))]
    [ContentType(ContentTypeNames.RoslynContentType)]
    [Name(nameof(FindExtensionMethodsCommandHandler))]
    public class FindExtensionMethodsCommandHandler :
        AbstractNavigationCommandHandler<FindExtensionMethodsCommandArgs>
    {
        private readonly IAsynchronousOperationListener _asyncListener;

        public override string DisplayName => nameof(FindExtensionMethodsCommandHandler);

        [ImportingConstructor]
        internal FindExtensionMethodsCommandHandler(
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
                _ = FindExtensionMethodsAsync(document, caretPosition, streamingPresenter);
                return true;
            }

            return false;
        }

        private async Task FindExtensionMethodsAsync(
                  Document document, int caretPosition,
                  IStreamingFindUsagesPresenter presenter)
        {
            try
            {
                using (var token = _asyncListener.BeginAsyncOperation(nameof(FindExtensionMethodsAsync)))
                {
                    // Let the presented know we're starting a search.
                    var context = presenter.StartSearch(
                        EditorFeaturesResources.Navigating, supportsReferences: true);

                    using (Logger.LogBlock(
                        FunctionId.CommandHandler_FindAllReference,
                        KeyValueLogMessage.Create(LogType.UserAction, m => m["type"] = "streaming"),
                        context.CancellationToken))
                    {
                        var candidateSymbolProjectPair = await FindUsagesHelpers.GetRelevantSymbolAndProjectAtPositionAsync(document, caretPosition, context.CancellationToken);

                        var symbol = candidateSymbolProjectPair?.symbol as INamedTypeSymbol;

                        // if we didn't get the right symbol, just abort
                        if (symbol == null)
                        {
                            await context.OnCompletedAsync().ConfigureAwait(false);
                            return;
                        }

                        Compilation compilation;
                        if (!document.Project.TryGetCompilation(out compilation))
                        {
                            await context.OnCompletedAsync().ConfigureAwait(false);
                            return;
                        }

                        var solution = document.Project.Solution;

                        foreach (var type in compilation.Assembly.GlobalNamespace.GetAllTypes(context.CancellationToken))
                        {
                            if (!type.MightContainExtensionMethods)
                                continue;

                            foreach (var extMethod in type.GetMembers().OfType<IMethodSymbol>().Where(method => method.IsExtensionMethod))
                            {
                                if (context.CancellationToken.IsCancellationRequested)
                                    break;

                                var reducedMethod = extMethod.ReduceExtensionMethod(symbol);
                                if (reducedMethod != null)
                                {
                                    var loc = extMethod.Locations.First();

                                    var sourceDefinition = await SymbolFinder.FindSourceDefinitionAsync(reducedMethod, solution, context.CancellationToken).ConfigureAwait(false);

                                    // And if our definition actually is from source, then let's re-figure out what project it came from
                                    if (sourceDefinition != null)
                                    {
                                        var originatingProject = solution.GetProject(sourceDefinition.ContainingAssembly, context.CancellationToken);

                                        var definitionItem = reducedMethod.ToNonClassifiedDefinitionItem(solution, true);

                                        await context.OnDefinitionFoundAsync(definitionItem);
                                    }
                                }
                            }
                        }

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
