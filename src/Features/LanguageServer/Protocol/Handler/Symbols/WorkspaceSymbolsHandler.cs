// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.NavigateTo;
using Microsoft.VisualStudio.LanguageServer.Protocol;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.LanguageServer.Handler
{
    [Shared]
    [ExportLspMethod(Methods.WorkspaceSymbolName, mutatesSolutionState: false)]
    internal class WorkspaceSymbolsHandler : IRequestHandler<WorkspaceSymbolParams, SymbolInformation[]>
    {
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public WorkspaceSymbolsHandler()
        {
        }

        public TextDocumentIdentifier? GetTextDocumentIdentifier(WorkspaceSymbolParams request) => null;

        public async Task<SymbolInformation[]> HandleRequestAsync(WorkspaceSymbolParams request, RequestContext context, CancellationToken cancellationToken)
        {
            var solution = context.Solution;

            var searchTasks = Task.WhenAll(solution.Projects.Select(project => SearchProjectAsync(project, request, cancellationToken)));
            var result = await searchTasks.ConfigureAwait(false);
            return result.WhereNotNull().SelectMany(a => a).ToArray();

            // local functions
            static async Task<SymbolInformation[]?> SearchProjectAsync(Project project, WorkspaceSymbolParams request, CancellationToken cancellationToken)
            {
                using var progress = BufferedProgress.Create(request.PartialResultToken);

                var searchService = project.LanguageServices.GetService<INavigateToSearchService>();
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
                    await Task.WhenAll(items.Select(item => ReportSymbolInformation(progress, item, cancellationToken))).ConfigureAwait(false);
                }

                return progress.GetValues();

                static async Task ReportSymbolInformation(IProgress<SymbolInformation> progress, INavigateToSearchResult result, CancellationToken cancellationToken)
                {
                    var location = await ProtocolConversions.TextSpanToLocationAsync(result.NavigableItem.Document, result.NavigableItem.SourceSpan, cancellationToken).ConfigureAwait(false);
                    Contract.ThrowIfNull(location);
                    progress.Report(new SymbolInformation
                    {
                        Name = result.Name,
                        Kind = ProtocolConversions.NavigateToKindToSymbolKind(result.Kind),
                        Location = location,
                    });
                }
            }
        }
    }
}
