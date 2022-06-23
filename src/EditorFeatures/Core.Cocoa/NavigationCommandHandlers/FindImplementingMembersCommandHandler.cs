// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor.Host;
using Microsoft.CodeAnalysis.ErrorReporting;
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
    [Name(nameof(FindImplementingMembersCommandHandler))]
    internal sealed class FindImplementingMembersCommandHandler :
        AbstractNavigationCommandHandler<FindImplementingMembersCommandArgs>
    {
        private readonly IAsynchronousOperationListener _asyncListener;
        private readonly IGlobalOptionService _globalOptions;

        public override string DisplayName => nameof(FindImplementingMembersCommandHandler);

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public FindImplementingMembersCommandHandler(
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
                _ = FindImplementingMembersAsync(document, caretPosition, streamingPresenter);
                return true;
            }

            return false;
        }

        private async Task FindImplementingMembersAsync(
            Document document, int caretPosition, IStreamingFindUsagesPresenter presenter)
        {
            try
            {
                using var token = _asyncListener.BeginAsyncOperation(nameof(FindImplementingMembersAsync));

                // Let the presented know we're starting a search.  We pass in no cancellation token here as this
                // operation itself is fire-and-forget and the user won't cancel the operation through us (though
                // the window itself can cancel the operation if it is taken over for another find operation.
                var (context, cancellationToken) = presenter.StartSearch(EditorFeaturesResources.Navigating, supportsReferences: true);

                using (Logger.LogBlock(
                    FunctionId.CommandHandler_FindAllReference,
                    KeyValueLogMessage.Create(LogType.UserAction, m => m["type"] = "streaming"),
                    cancellationToken))
                {
                    try
                    {
                        var relevantSymbol = await FindUsagesHelpers.GetRelevantSymbolAndProjectAtPositionAsync(document, caretPosition, cancellationToken).ConfigureAwait(false);

                        var interfaceSymbol = relevantSymbol?.symbol as INamedTypeSymbol;

                        if (interfaceSymbol == null || interfaceSymbol.TypeKind != TypeKind.Interface)
                        {
                            //looks like it's not a relevant symbol
                            return;
                        }

                        // we now need to find the class that implements this particular interface, at the
                        // caret position, or somewhere around it
                        if (!document.TryGetSyntaxRoot(out var nodeRoot))
                            return;

                        var syntaxTree = await document.GetSyntaxTreeAsync(cancellationToken).ConfigureAwait(false);
                        var documentToken = nodeRoot.FindToken(caretPosition);

                        if (!documentToken.Span.IntersectsWith(caretPosition))
                            return; // looks like it's not relevant

                        // the parents should bring us to the class definition
                        var parentTypeNode = documentToken.Parent?.Parent?.Parent?.Parent;
                        var compilation = await document.Project.GetCompilationAsync(cancellationToken).ConfigureAwait(false);

                        // let's finally get our implementing type
                        var namedTypeSymbol = compilation.GetSemanticModel(syntaxTree).GetDeclaredSymbol(parentTypeNode, cancellationToken: cancellationToken) as INamedTypeSymbol;
                        // unless something went wrong, and we got an empty symbol,
                        if (namedTypeSymbol == null)
                            return;

                        // we can search for implementations of the interface, within this type
                        await InspectInterfaceAsync(context, interfaceSymbol, namedTypeSymbol, document.Project, cancellationToken).ConfigureAwait(false);

                        // now, we iterate on interfaces of our interfaces
                        foreach (var iFace in interfaceSymbol.AllInterfaces)
                        {
                            await InspectInterfaceAsync(context, iFace, namedTypeSymbol, document.Project, cancellationToken).ConfigureAwait(false);
                        }
                    }
                    finally
                    {
                        await context.OnCompletedAsync(cancellationToken).ConfigureAwait(false);
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

        private static async Task InspectInterfaceAsync(
            IFindUsagesContext context, INamedTypeSymbol interfaceSymbol, INamedTypeSymbol namedTypeSymbol, Project project, CancellationToken cancellationToken)
        {
            foreach (var interfaceMember in interfaceSymbol.GetMembers())
            {
                if (cancellationToken.IsCancellationRequested)
                    return;

                var impl = namedTypeSymbol.FindImplementationForInterfaceMember(interfaceMember);
                if (impl == null)
                    continue;

                var definitionItem = impl.ToNonClassifiedDefinitionItem(project.Solution, includeHiddenLocations: true);
                await context.OnDefinitionFoundAsync(definitionItem, cancellationToken).ConfigureAwait(false);
            }
        }
    }
}
