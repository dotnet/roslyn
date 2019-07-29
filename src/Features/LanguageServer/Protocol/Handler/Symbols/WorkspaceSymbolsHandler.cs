// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.NavigateTo;
using Microsoft.VisualStudio.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.LanguageServer.Handler
{
    [Shared]
    [ExportLspMethod(Methods.WorkspaceSymbolName)]
    internal class WorkspaceSymbolsHandler : IRequestHandler<WorkspaceSymbolParams, SymbolInformation[]>
    {
        public async Task<SymbolInformation[]> HandleRequestAsync(Solution solution, WorkspaceSymbolParams request,
            ClientCapabilities clientCapabilities, CancellationToken cancellationToken, bool keepThreadContext = false)
        {
            var searchTasks = Task.WhenAll(solution.Projects.Select(project => SearchProjectAsync(project, request, keepThreadContext, cancellationToken)));
            return (await searchTasks.ConfigureAwait(keepThreadContext)).SelectMany(s => s).ToArray();

            // local functions
            static async Task<ImmutableArray<SymbolInformation>> SearchProjectAsync(Project project, WorkspaceSymbolParams request, bool keepThreadContext, CancellationToken cancellationToken)
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
                        cancellationToken).ConfigureAwait(keepThreadContext);
                    var projectSymbolsTasks = Task.WhenAll(items.Select(item => CreateSymbolInformation(item, keepThreadContext, cancellationToken)));
                    return (await projectSymbolsTasks.ConfigureAwait(keepThreadContext)).ToImmutableArray();
                }

                return ImmutableArray.Create<SymbolInformation>();

                static async Task<SymbolInformation> CreateSymbolInformation(INavigateToSearchResult result, bool keepThreadContext, CancellationToken cancellationToken)
                {
                    return new SymbolInformation
                    {
                        Name = result.Name,
                        Kind = ProtocolConversions.NavigateToKindToSymbolKind(result.Kind),
                        Location = await ProtocolConversions.TextSpanToLocationAsync(result.NavigableItem.Document, result.NavigableItem.SourceSpan, cancellationToken).ConfigureAwait(keepThreadContext),
                    };
                }
            }
        }
    }
}
