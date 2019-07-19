using System;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.NavigateTo;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.LanguageServer.Protocol;
using StreamJsonRpc;
using LSP = Microsoft.VisualStudio.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.Remote
{
    internal partial class LanguageServer
    {
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

        [JsonRpcMethod(VSSymbolMethods.WorkspaceBeginSymbolName)]
        public async Task<VSBeginSymbolParams> WorkspaceBeginSymbolAsync(string query, int searchId, CancellationToken cancellationToken)
        {
            await SearchAsync(query, searchId, cancellationToken).ConfigureAwait(false);

            return new VSBeginSymbolParams();
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

                    VSSymbolInformation[] convertedResults = await Convert(result, cancellationToken).ConfigureAwait(false);

                    await InvokeAsync(
                        VSSymbolMethods.WorkspacePublishSymbolName,
                        new object[] { new VSPublishSymbolParams() { SearchId = searchId, Symbols = convertedResults } },
                        cancellationToken).ConfigureAwait(false);
                }
            }
        }

        private static async Task<VSSymbolInformation[]> Convert(ImmutableArray<INavigateToSearchResult> results, CancellationToken cancellationToken)
        {
            var symbols = new VSSymbolInformation[results.Length];

            for (int i = 0; i < results.Length; i++)
            {
                var symbolText = await results[i].NavigableItem.Document.GetTextAsync(cancellationToken).ConfigureAwait(false);
                symbols[i] = new VSSymbolInformation()
                {
                    Name = results[i].Name,
                    ContainerName = results[i].AdditionalInformation,
                    Kind = ToLSPSymbolKind(results[i].Kind),
                    Location = new LSP.Location()
                    {
                        Uri = new Uri(results[i].NavigableItem.Document.FilePath),
                        Range = TextSpanToRange(results[i].NavigableItem.SourceSpan, symbolText)
                    },
                    Icon = new VisualStudio.Text.Adornments.ImageElement(results[i].NavigableItem.Glyph.GetImageId())
                };
            }

            return symbols;
        }

        private static LSP.SymbolKind ToLSPSymbolKind(string kind)
        {
            switch (kind)
            {
                case NavigateToItemKind.Class:
                    return LSP.SymbolKind.Class;
                case NavigateToItemKind.Constant:
                    return LSP.SymbolKind.Constant;
                case NavigateToItemKind.Delegate:
                    return LSP.SymbolKind.Method;
                case NavigateToItemKind.Enum:
                    return LSP.SymbolKind.Enum;
                case NavigateToItemKind.EnumItem:
                    return LSP.SymbolKind.EnumMember;
                case NavigateToItemKind.Event:
                    return LSP.SymbolKind.Event;
                case NavigateToItemKind.Field:
                    return LSP.SymbolKind.Field;
                case NavigateToItemKind.File:
                    return LSP.SymbolKind.File;
                case NavigateToItemKind.Interface:
                    return LSP.SymbolKind.Interface;
                case NavigateToItemKind.Method:
                    return LSP.SymbolKind.Method;
                case NavigateToItemKind.Module:
                    return LSP.SymbolKind.Module;
                case NavigateToItemKind.Property:
                    return LSP.SymbolKind.Property;
                case NavigateToItemKind.Structure:
                    return LSP.SymbolKind.Struct;
                default:
                    return default;
            }
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
