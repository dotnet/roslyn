// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.IO;
using System.Threading;
using Microsoft.CodeAnalysis.LanguageService;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Language.Intellisense;

namespace Microsoft.CodeAnalysis.Editor.Implementation.Peek
{
    internal static class PeekHelpers
    {
        internal static IDocumentPeekResult CreateDocumentPeekResult(string filePath, LinePositionSpan identifierLocation, LinePositionSpan entityOfInterestSpan, IPeekResultFactory peekResultFactory)
        {
            var fileName = Path.GetFileName(filePath);
            var label = string.Format("{0} - ({1}, {2})", fileName, identifierLocation.Start.Line + 1, identifierLocation.Start.Character + 1);

            var displayInfo = new PeekResultDisplayInfo(label: label, labelTooltip: filePath, title: fileName, titleTooltip: filePath);

            return CreateDocumentPeekResult(
                filePath,
                identifierLocation,
                entityOfInterestSpan,
                displayInfo,
                peekResultFactory,
                isReadOnly: false);
        }

        internal static IDocumentPeekResult CreateDocumentPeekResult(string filePath, LinePositionSpan identifierLocation, LinePositionSpan entityOfInterestSpan, PeekResultDisplayInfo displayInfo, IPeekResultFactory peekResultFactory, bool isReadOnly)
        {
            return peekResultFactory.Create(
                displayInfo,
                filePath: filePath,
                startLine: entityOfInterestSpan.Start.Line,
                startIndex: entityOfInterestSpan.Start.Character,
                endLine: entityOfInterestSpan.End.Line,
                endIndex: entityOfInterestSpan.End.Character,
                idLine: identifierLocation.Start.Line,
                idIndex: identifierLocation.Start.Character,
                isReadOnly: isReadOnly);
        }

        internal static LinePositionSpan GetEntityOfInterestSpan(ISymbol symbol, Workspace workspace, Location identifierLocation, CancellationToken cancellationToken)
        {
            // This is called on a background thread, but since we don't have proper asynchrony we must block
            var root = identifierLocation.SourceTree.GetRoot(cancellationToken);
            var node = root.FindToken(identifierLocation.SourceSpan.Start).Parent;

            var syntaxFactsService = workspace.Services.GetLanguageServices(root.Language).GetService<ISyntaxFactsService>();

            switch (symbol.Kind)
            {
                case SymbolKind.Event:
                case SymbolKind.Field:
                case SymbolKind.Method:
                case SymbolKind.Property:
                    node = node.FirstAncestorOrSelf<SyntaxNode, ISyntaxFactsService>((node, syntaxFactsService) => syntaxFactsService.IsMethodLevelMember(node), syntaxFactsService) ?? node;
                    break;

                case SymbolKind.NamedType:
                case SymbolKind.Namespace:
                    node = node.FirstAncestorOrSelf<SyntaxNode, ISyntaxFactsService>((node, syntaxFactsService) => syntaxFactsService.IsTopLevelNodeWithMembers(node), syntaxFactsService) ?? node;
                    break;
            }

            return identifierLocation.SourceTree.GetLocation(node.Span).GetMappedLineSpan().Span;
        }
    }
}
