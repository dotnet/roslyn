// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.NavigateTo;
using Microsoft.VisualStudio.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.LanguageServer.Handler
{
    [Shared]
    [ExportLspMethod(Methods.WorkspaceSymbolName)]
    internal class WorkspaceSymbolsHandler : AbstractRequestHandler<WorkspaceSymbolParams, SymbolInformation[]>
    {
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public WorkspaceSymbolsHandler(ILspSolutionProvider solutionProvider) : base(solutionProvider)
        {
        }

        public override async Task<SymbolInformation[]> HandleRequestAsync(WorkspaceSymbolParams request, RequestContext context, CancellationToken cancellationToken)
        {
            var solution = SolutionProvider.GetCurrentSolutionForMainWorkspace();
            var searchTasks = Task.WhenAll(solution.Projects.Select(project => SearchProjectAsync(project, request, cancellationToken)));
            return (await searchTasks.ConfigureAwait(false)).SelectMany(s => s).ToArray();

            // local functions
            static async Task<ImmutableArray<SymbolInformation>> SearchProjectAsync(Project project, WorkspaceSymbolParams request, CancellationToken cancellationToken)
            {
                var searchService = project.LanguageServices.GetService<INavigateToSearchService_RemoveInterfaceAboveAndRenameThisAfterInternalsVisibleToUsersUpdate>();
                if (searchService != null)
                {
                    // TODO - Update Kinds Provided to return all necessary symbols.
                    // https://github.com/dotnet/roslyn/projects/45#card-20033822
                    var items = await searchService.SearchProjectAsync(
                        project,
                        ImmutableArray<Document>.Empty,
                        request.Query,
                        searchService.KindsProvided,
                        cancellationToken).ConfigureAwait(false);
                    var projectSymbolsTasks = Task.WhenAll(items.Select(item => CreateSymbolInformation(item, cancellationToken)));
                    return (await projectSymbolsTasks.ConfigureAwait(false)).ToImmutableArray();
                }

                return ImmutableArray.Create<SymbolInformation>();

                static async Task<SymbolInformation> CreateSymbolInformation(INavigateToSearchResult result, CancellationToken cancellationToken)
                {
                    return new SymbolInformation
                    {
                        Name = result.Name,
                        Kind = ProtocolConversions.NavigateToKindToSymbolKind(result.Kind),
                        Location = await ProtocolConversions.TextSpanToLocationAsync(result.NavigableItem.Document, result.NavigableItem.SourceSpan, cancellationToken).ConfigureAwait(false),
                    };
                }
            }
        }
    }
}
