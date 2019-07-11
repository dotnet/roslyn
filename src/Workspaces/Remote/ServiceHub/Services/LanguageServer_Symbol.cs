using System;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.NavigateTo;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.LanguageServer.Protocol;
using StreamJsonRpc;
using LSP = Microsoft.VisualStudio.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.Remote
{
    internal partial class LanguageServer
    {
        private int searchIds;

        private static IImmutableSet<string> KindsProvided { get; } = ImmutableHashSet.Create(
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
        public int WorkspaceBeginSymbol(string query, CancellationToken cancellationToken)
        {
            int searchId = searchIds++;
            // Fire and forget
            SearchAsync(query, searchId, cancellationToken).ConfigureAwait(false);
            return searchId;
        }

        private async Task SearchAsync(string query, int searchId, CancellationToken cancellationToken)
        {
            using (UserOperationBooster.Boost())
            {
                foreach (var project in SolutionService.PrimaryWorkspace.CurrentSolution.Projects)
                {
                    if (cancellationToken.IsCancellationRequested)
                    {
                        return;
                    }

                    var result = await AbstractNavigateToSearchService.SearchProjectInCurrentProcessAsync(
                        project, ImmutableArray<Document>.Empty, query, KindsProvided, cancellationToken).ConfigureAwait(false);

                    if (cancellationToken.IsCancellationRequested)
                    {
                        return;
                    }

                    SymbolInformation[] convertedResults = await Convert(result, cancellationToken).ConfigureAwait(false);

                    await InvokeAsync(
                        MSLSPMethods.WorkspacePublishSymbolName,
                        new object[] { new MSLSPPublishSymbolParams() { SearchId = searchId, Symbols = convertedResults } },
                        cancellationToken).ConfigureAwait(false);
                }
            }

            await InvokeAsync(MSLSPMethods.WorkspaceCompleteSymbolName, new object[] { searchId }, cancellationToken).ConfigureAwait(false);
        }

        private static async Task<SymbolInformation[]> Convert(ImmutableArray<INavigateToSearchResult> results, CancellationToken cancellationToken)
        {
            var symbols = new SymbolInformation[results.Length];

            for (int i = 0; i < results.Length; i++)
            {
                var symbolText = await results[i].NavigableItem.Document.GetTextAsync(cancellationToken).ConfigureAwait(false);
                symbols[i] = new SymbolInformation()
                {
                    Name = results[i].Name,
                    ContainerName = results[i].AdditionalInformation,
                    Kind = LSP.SymbolKind.Method,
                    Location = new LSP.Location()
                    {
                        Uri = new Uri(results[i].NavigableItem.Document.FilePath),
                        Range = TextSpanToRange(results[i].NavigableItem.SourceSpan, symbolText)
                    }
                };
            }

            return symbols;
        }

        private static Range TextSpanToRange(TextSpan textSpan, SourceText text)
        {
            var linePosSpan = text.Lines.GetLinePositionSpan(textSpan);
            return LinePositionToRange(linePosSpan);
        }

        private static Range LinePositionToRange(LinePositionSpan linePositionSpan)
        {
            return new Range { Start = LinePositionToPosition(linePositionSpan.Start), End = LinePositionToPosition(linePositionSpan.End) };
        }

        private static Position LinePositionToPosition(LinePosition linePosition)
        {
            return new Position { Line = linePosition.Line, Character = linePosition.Character };
        }
    }
}
