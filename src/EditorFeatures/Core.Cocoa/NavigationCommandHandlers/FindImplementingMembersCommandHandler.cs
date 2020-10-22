//
// FindBaseSymbolsCommandHandler.cs
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
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Editor.FindUsages;
using Microsoft.CodeAnalysis.Editor.Host;
using Microsoft.CodeAnalysis.ErrorReporting;
using Microsoft.CodeAnalysis.FindUsages;
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
    [Name(nameof(FindImplementingMembersCommandHandler))]
    public class FindImplementingMembersCommandHandler :
        AbstractNavigationCommandHandler<FindImplementingMembersCommandArgs>
    {
        private readonly IAsynchronousOperationListener _asyncListener;

        public override string DisplayName => nameof(FindImplementingMembersCommandHandler);

        [ImportingConstructor]
        internal FindImplementingMembersCommandHandler(
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
                _ = FindImplementingMembersAsync(document, caretPosition, streamingPresenter);
                return true;
            }

            return false;
        }

        private async Task FindImplementingMembersAsync(
            Document document, int caretPosition,
            IStreamingFindUsagesPresenter presenter)
        {
            try
            {
                using (var token = _asyncListener.BeginAsyncOperation(nameof(FindImplementingMembersAsync)))
                {
                    // Let the presented know we're starting a search.
                    var context = presenter.StartSearch(
                        EditorFeaturesResources.Navigating, supportsReferences: true);

                    using (Logger.LogBlock(
                        FunctionId.CommandHandler_FindAllReference,
                        KeyValueLogMessage.Create(LogType.UserAction, m => m["type"] = "streaming"),
                        context.CancellationToken))
                    {
                        try
                        {
                            var relevantSymbol = await FindUsagesHelpers.GetRelevantSymbolAndProjectAtPositionAsync(document, caretPosition, context.CancellationToken);

                            var interfaceSymbol = relevantSymbol?.symbol as INamedTypeSymbol;

                            if (interfaceSymbol == null || interfaceSymbol.TypeKind != TypeKind.Interface)
                            {
                                //looks like it's not a relevant symbol
                                return;
                            }

                            // we now need to find the class that implements this particular interface, at the
                            // caret position, or somewhere around it
                            SyntaxNode nodeRoot;
                            if (!document.TryGetSyntaxRoot(out nodeRoot))
                                return;

                            var syntaxTree = await document.GetSyntaxTreeAsync();
                            var documentToken = nodeRoot.FindToken(caretPosition);

                            if (documentToken == null || !documentToken.Span.IntersectsWith(caretPosition))
                                return; // looks like it's not relevant

                            // the parents should bring us to the class definition
                            var parentTypeNode = documentToken.Parent?.Parent?.Parent?.Parent;
                            var compilation = await document.Project.GetCompilationAsync();

                            // let's finally get our implementing type
                            var namedTypeSymbol = compilation.GetSemanticModel(syntaxTree).GetDeclaredSymbol(parentTypeNode) as INamedTypeSymbol;
                            // unless something went wrong, and we got an empty symbol,
                            if (namedTypeSymbol == null) return;

                            // we can search for implementations of the interface, within this type
                            await InspectInterfaceAsync(context, interfaceSymbol, namedTypeSymbol, document.Project);

                            // now, we iterate on interfaces of our interfaces
                            foreach (var iFace in interfaceSymbol.AllInterfaces)
                            {
                                await InspectInterfaceAsync(context, iFace, namedTypeSymbol, document.Project);
                            }
                        }
                        finally
                        {
                            await context.OnCompletedAsync().ConfigureAwait(false);
                        }
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

        private async Task InspectInterfaceAsync(IFindUsagesContext context, INamedTypeSymbol interfaceSymbol, INamedTypeSymbol namedTypeSymbol, Project project)
        {
            foreach (var interfaceMember in interfaceSymbol.GetMembers())
            {
                if (context.CancellationToken.IsCancellationRequested)
                    return;

                var impl = namedTypeSymbol.FindImplementationForInterfaceMember(interfaceMember);
                if (impl == null)
                    continue;

                var definitionItem = impl.ToNonClassifiedDefinitionItem(project.Solution, true);
                await context.OnDefinitionFoundAsync(definitionItem);
            }
        }
    }
}
