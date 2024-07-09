// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editor.Implementation.InlineRename;
using Microsoft.CodeAnalysis.GoToDefinition;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Editor.CSharp.InlineRename;

[ExportLanguageService(typeof(IEditorInlineRenameService), LanguageNames.CSharp), Shared]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal sealed class CSharpEditorInlineRenameService(
    [ImportMany] IEnumerable<IRefactorNotifyService> refactorNotifyServices,
    IGlobalOptionService globalOptions) : AbstractEditorInlineRenameService(refactorNotifyServices, globalOptions)
{
    private const int NumberOfContextLines = 20;
    private const int MaxDefinitionCount = 10;
    private const int MaxReferenceCount = 50;

    public override bool IsRenameContextSupported => true;

    /// <summary>
    /// Uses semantic information of renamed symbol to produce a map containing contextual information for use in Copilot rename feature
    /// </summary>
    /// <returns>Map where key indicates the kind of semantic information, and value is an array of relevant code snippets.</returns>
    public override async Task<ImmutableDictionary<string, ImmutableArray<string>>> GetRenameContextAsync(
        IInlineRenameInfo inlineRenameInfo, IInlineRenameLocationSet inlineRenameLocationSet, CancellationToken cancellationToken)
    {
        using var _1 = PooledHashSet<TextSpan>.GetInstance(out var seen);
        using var _2 = ArrayBuilder<string>.GetInstance(out var definitions);
        using var _3 = ArrayBuilder<string>.GetInstance(out var references);
        using var _4 = ArrayBuilder<string>.GetInstance(out var docComments);

        foreach (var renameDefinition in inlineRenameInfo.DefinitionLocations.Take(MaxDefinitionCount))
        {
            // Find largest snippet of code that represents the definition
            var containingStatementOrDeclarationSpan =
                await TryGetSurroundingNodeSpanAsync<MemberDeclarationSyntax>(renameDefinition.Document, renameDefinition.SourceSpan, cancellationToken).ConfigureAwait(false) ??
                await TryGetSurroundingNodeSpanAsync<StatementSyntax>(renameDefinition.Document, renameDefinition.SourceSpan, cancellationToken).ConfigureAwait(false);

            // Find documentation comments of definitions
            var symbolService = renameDefinition.Document.GetRequiredLanguageService<IGoToDefinitionSymbolService>();
            if (symbolService is not null)
            {
                var textSpan = inlineRenameInfo.TriggerSpan;
                var (symbol, _, _) = await symbolService.GetSymbolProjectAndBoundSpanAsync(
                    renameDefinition.Document, textSpan.Start, cancellationToken)
                    .ConfigureAwait(true);
                var docComment = symbol?.GetDocumentationCommentXml(expandIncludes: true, cancellationToken: cancellationToken);
                if (!string.IsNullOrWhiteSpace(docComment))
                {
                    docComments.Add(docComment!);
                }
            }

            var documentText = await renameDefinition.Document.GetTextAsync(cancellationToken).ConfigureAwait(false);
            AddSpanOfInterest(documentText, renameDefinition.SourceSpan, containingStatementOrDeclarationSpan, definitions);
        }

        foreach (var renameLocation in inlineRenameLocationSet.Locations.Take(MaxReferenceCount))
        {
            // Find largest snippet of code that represents the reference
            var containingStatementOrDeclarationSpan =
                await TryGetSurroundingNodeSpanAsync<MemberDeclarationSyntax>(renameLocation.Document, renameLocation.TextSpan, cancellationToken).ConfigureAwait(false) ??
                await TryGetSurroundingNodeSpanAsync<BaseMethodDeclarationSyntax>(renameLocation.Document, renameLocation.TextSpan, cancellationToken).ConfigureAwait(false) ??
                await TryGetSurroundingNodeSpanAsync<StatementSyntax>(renameLocation.Document, renameLocation.TextSpan, cancellationToken).ConfigureAwait(false);

            var documentText = await renameLocation.Document.GetTextAsync(cancellationToken).ConfigureAwait(false);
            AddSpanOfInterest(documentText, renameLocation.TextSpan, containingStatementOrDeclarationSpan, references);
        }

        var contextBuilder = ImmutableDictionary.CreateBuilder<string, ImmutableArray<string>>();
        if (!definitions.IsEmpty)
        {
            contextBuilder.Add("definition", definitions.ToImmutable());
        }
        if (!references.IsEmpty)
        {
            contextBuilder.Add("reference", references.ToImmutable());
        }
        if (!docComments.IsEmpty)
        {
            contextBuilder.Add("documentation", docComments.ToImmutable());
        }

        return contextBuilder.ToImmutableDictionary();

        void AddSpanOfInterest(SourceText documentText, TextSpan fallbackSpan, TextSpan? surroundingSpanOfInterest, ArrayBuilder<string> resultBuilder)
        {
            int startPosition, endPosition, startLine = 0, endLine = 0, lineCount = 0;
            if (surroundingSpanOfInterest is not null)
            {
                startPosition = surroundingSpanOfInterest.Value.Start;
                endPosition = surroundingSpanOfInterest.Value.End;
                startLine = documentText.Lines.GetLineFromPosition(surroundingSpanOfInterest.Value.Start).LineNumber;
                endLine = documentText.Lines.GetLineFromPosition(surroundingSpanOfInterest.Value.End).LineNumber;
                lineCount = endLine - startLine + 1;
            }

            // If a well defined surrounding span was not computed or if the computed surrounding span was too large,
            // select a span that encompasses NumberOfContextLines lines above and NumberOfContextLines lines below the identifier.
            if (surroundingSpanOfInterest is null || lineCount <= 0 || lineCount > NumberOfContextLines * 2)
            {
                startLine = Math.Max(0, documentText.Lines.GetLineFromPosition(fallbackSpan.Start).LineNumber - NumberOfContextLines);
                endLine = Math.Min(documentText.Lines.Count - 1, documentText.Lines.GetLineFromPosition(fallbackSpan.End).LineNumber + NumberOfContextLines);
            }

            // If the start and end positions are not at the beginning and end of the start and end lines respectively,
            // expand to select the corresponding lines completely.
            startPosition = documentText.Lines[startLine].Start;
            endPosition = documentText.Lines[endLine].End;
            var length = endPosition - startPosition + 1;

            surroundingSpanOfInterest = new TextSpan(startPosition, length);

            if (seen.Add(surroundingSpanOfInterest.Value))
            {
                resultBuilder.Add(documentText.GetSubText(surroundingSpanOfInterest.Value).ToString());
            }
        }
    }

    /// <summary>
    /// Returns the <see cref="TextSpan"/> of the nearest encompassing <see cref="CSharpSyntaxNode"/> of type
    /// <typeparamref name="T"/> of which the supplied <paramref name="textSpan"/> is a part within the supplied
    /// <paramref name="document"/>.
    /// </summary>
    public static async Task<TextSpan?> TryGetSurroundingNodeSpanAsync<T>(
        Document document,
        TextSpan textSpan,
        CancellationToken cancellationToken)
            where T : CSharpSyntaxNode
    {
        if (document.Project.Language is not LanguageNames.CSharp)
        {
            return null;
        }

        var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
        if (root is null)
        {
            return null;
        }

        var containingNode = root.FindNode(textSpan);
        var targetNode = containingNode.FirstAncestorOrSelf<T>() ?? containingNode;

        return targetNode.Span;
    }
}
