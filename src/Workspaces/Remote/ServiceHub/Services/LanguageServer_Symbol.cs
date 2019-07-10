using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.NavigateTo;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.LanguageServer.Protocol;
using StreamJsonRpc;

namespace Microsoft.CodeAnalysis.Remote
{
    internal partial class LanguageServer
    {
        private int searchIds;

        public IImmutableSet<string> KindsProvided { get; } = ImmutableHashSet.Create(
           NavigateToItemKind.Class,
           NavigateToItemKind.Constant,
           NavigateToItemKind.Delegate,
           NavigateToItemKind.Enum,
           NavigateToItemKind.EnumItem,
           NavigateToItemKind.Event,
           NavigateToItemKind.Field,
           NavigateToItemKind.Interface,
           NavigateToItemKind.Method,
           NavigateToItemKind.Module,
           NavigateToItemKind.Property,
           NavigateToItemKind.Structure);

        [JsonRpcMethod(MSLSPMethods.WorkspaceBeginSymbolName)]
        public int WorkspaceBeginSymbol(string query)
        {
            int searchId = searchIds++;
            Task.Run(async () =>
            {
                foreach (var project in SolutionService.PrimaryWorkspace.CurrentSolution.Projects)
                {
                    using (UserOperationBooster.Boost())
                    {
                        //var solution = await GetSolutionAsync(cancellationToken).ConfigureAwait(false);

                        //var project = solution.GetProject(projectId);
                        //var priorityDocuments = priorityDocumentIds.Select(d => solution.GetDocument(d))
                        //                                           .ToImmutableArray();

                        var result = await AbstractNavigateToSearchService.SearchProjectInCurrentProcessAsync(
                        project, ImmutableArray<Document>.Empty, query, KindsProvided, CancellationToken.None).ConfigureAwait(false);

                        var convertedResults = await Convert(result).ConfigureAwait(false);

                        await InvokeAsync(MSLSPMethods.WorkspacePublishSymbolName, new object[] { new MSLSPPublishSymbolParams() { SearchId = searchId, Symbols = convertedResults } }, CancellationToken.None).ConfigureAwait(false);
                    }
                }

                await InvokeAsync(MSLSPMethods.WorkspaceCompleteSymbolName, new object[] { searchId }, CancellationToken.None).ConfigureAwait(false);
            });

            return searchId;
        }

        private async Task<SymbolInformation[]> Convert(
            ImmutableArray<INavigateToSearchResult> results)
        {
            var symbols = new SymbolInformation[results.Length];

            for (int i = 0; i < results.Length; i++)
            {
                var symbolText = await results[i].NavigableItem.Document.GetTextAsync(CancellationToken.None).ConfigureAwait(false);
                symbols[i] = new SymbolInformation()
                {
                    Name = results[i].Name,
                    ContainerName = results[i].AdditionalInformation,
                    Kind = VisualStudio.LanguageServer.Protocol.SymbolKind.Method,
                    Location = new VisualStudio.LanguageServer.Protocol.Location()
                    {
                        Uri = new Uri(results[i].NavigableItem.Document.FilePath),
                        Range = TextSpanToRange(results[i].NavigableItem.SourceSpan, symbolText)
                    }
                };
            }

            return symbols;
        }

        public static Range TextSpanToRange(TextSpan textSpan, SourceText text)
        {
            var linePosSpan = text.Lines.GetLinePositionSpan(textSpan);
            return LinePositionToRange(linePosSpan);
        }

        public static Range LinePositionToRange(LinePositionSpan linePositionSpan)
        {
            return new Range { Start = LinePositionToPosition(linePositionSpan.Start), End = LinePositionToPosition(linePositionSpan.End) };
        }

        public static Position LinePositionToPosition(LinePosition linePosition)
        {
            return new Position { Line = linePosition.Line, Character = linePosition.Character };
        }
    }
}
