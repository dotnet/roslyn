// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editor.Implementation.InlineRename;
using Microsoft.CodeAnalysis.GoToDefinition;
using Microsoft.CodeAnalysis.Host.Mef;
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
    protected override async Task<ImmutableDictionary<string, ImmutableArray<string>>> GetRenameContextCoreAsync(IInlineRenameInfo inlineRenameInfo, IInlineRenameLocationSet inlineRenameLocationSet, CancellationToken cancellationToken)
    {
        var seen = PooledHashSet<TextSpan>.GetInstance();
        var definitions = ArrayBuilder<string>.GetInstance();
        var references = ArrayBuilder<string>.GetInstance();
        var docComments = ArrayBuilder<string>.GetInstance();

        foreach (var renameDefinition in inlineRenameInfo.DefinitionLocations)
        {
            var containingStatementOrDeclarationSpan =
                await renameDefinition.Document.TryGetSurroundingNodeSpanAsync<StatementSyntax>(renameDefinition.SourceSpan, cancellationToken).ConfigureAwait(false) ??
                await renameDefinition.Document.TryGetSurroundingNodeSpanAsync<MemberDeclarationSyntax>(renameDefinition.SourceSpan, cancellationToken).ConfigureAwait(false);

            var documentText = await renameDefinition.Document.GetTextAsync(cancellationToken).ConfigureAwait(false);
            if (documentText is null)
            {
                continue;
            }

            var symbolService = renameDefinition.Document.GetLanguageService<IGoToDefinitionSymbolService>();
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

            AddSpanOfInterest(documentText, renameDefinition.SourceSpan, containingStatementOrDeclarationSpan, definitions);
        }

        foreach (var renameLocation in inlineRenameLocationSet.Locations)
        {
            var containingStatementOrDeclarationSpan =
                await renameLocation.Document.TryGetSurroundingNodeSpanAsync<BaseMethodDeclarationSyntax>(renameLocation.TextSpan, cancellationToken).ConfigureAwait(false) ??
                await renameLocation.Document.TryGetSurroundingNodeSpanAsync<StatementSyntax>(renameLocation.TextSpan, cancellationToken).ConfigureAwait(false) ??
                await renameLocation.Document.TryGetSurroundingNodeSpanAsync<MemberDeclarationSyntax>(renameLocation.TextSpan, cancellationToken).ConfigureAwait(false);

            var documentText = await renameLocation.Document.GetTextAsync(cancellationToken).ConfigureAwait(false);
            if (documentText is null)
            {
                continue;
            }

            AddSpanOfInterest(documentText, renameLocation.TextSpan, containingStatementOrDeclarationSpan, references);
        }

        var context = ImmutableDictionary<string, ImmutableArray<string>>.Empty;
        if (!definitions.IsEmpty)
        {
            context = context.Add("definition", definitions.ToImmutableAndFree());
        }
        if (!references.IsEmpty)
        {
            context = context.Add("reference", references.ToImmutableAndFree());
        }
        if (!docComments.IsEmpty)
        {
            context = context.Add("documentation", docComments.ToImmutableAndFree());
        }
        return context;

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
            // select a span that encompasses 5 lines above and 5 lines below the error squiggle.
            if (surroundingSpanOfInterest is null || lineCount <= 0 || lineCount > 10)
            {
                startLine = Math.Max(0, documentText.Lines.GetLineFromPosition(fallbackSpan.Start).LineNumber - 5);
                endLine = Math.Min(documentText.Lines.Count - 1, documentText.Lines.GetLineFromPosition(fallbackSpan.End).LineNumber + 5);
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
}
