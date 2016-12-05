using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.CodeAnalysis.Completion.Providers
{
    internal static partial class SymbolCompletionItem
    {
        internal static string GetSymbolName(CompletionItem item)
        {
            string name;
            if (item.Properties.TryGetValue("SymbolName", out name))
            {
                return name;
            }

            return null;
        }

        internal static SymbolKind? GetKind(CompletionItem item)
        {
            string kind;
            if (item.Properties.TryGetValue("SymbolKind", out kind))
            {
                return (SymbolKind)int.Parse(kind);
            }

            return null;
        }

        public static async Task<CompletionDescription> GetDescriptionAsync(CompletionItem item, ImmutableArray<ISymbol> symbols, Document document, CancellationToken cancellationToken)
        {
            var workspace = document.Project.Solution.Workspace;

            var position = SymbolCompletionItem.GetDescriptionPosition(item);
            if (position == -1)
            {
                position = item.Span.Start;
            }

            var supportedPlatforms = SymbolCompletionItem.GetSupportedPlatforms(item, workspace);

            // find appropriate document for descripton context
            var contextDocument = document;
            if (supportedPlatforms != null && supportedPlatforms.InvalidProjects.Contains(document.Id.ProjectId))
            {
                var contextId = document.GetLinkedDocumentIds().FirstOrDefault(id => !supportedPlatforms.InvalidProjects.Contains(id.ProjectId));
                if (contextId != null)
                {
                    contextDocument = document.Project.Solution.GetDocument(contextId);
                }
            }

            var semanticModel = await contextDocument.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
            if (symbols.Length != 0)
            {
                return await CommonCompletionUtilities.CreateDescriptionAsync(workspace, semanticModel, position, symbols, supportedPlatforms, cancellationToken).ConfigureAwait(false);
            }
            else
            {
                return CompletionDescription.Empty;
            }
        }
    }
}
