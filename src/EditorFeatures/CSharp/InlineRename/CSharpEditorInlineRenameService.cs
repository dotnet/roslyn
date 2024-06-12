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
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Editor.CSharp.InlineRename;

[ExportLanguageService(typeof(IEditorInlineRenameService), LanguageNames.CSharp), Shared]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal sealed class CSharpEditorInlineRenameService(
    [ImportMany] IEnumerable<IRefactorNotifyService> refactorNotifyServices,
    IGlobalOptionService globalOptions) : AbstractEditorInlineRenameService(refactorNotifyServices, globalOptions)
{
    protected override async Task<ImmutableDictionary<string, string[]>> GetRenameContextCoreAsync(IInlineRenameInfo renameInfo, CancellationToken cancellationToken)
    {
        var seen = PooledHashSet<TextSpan>.GetInstance();
        var definitions = ArrayBuilder<string>.GetInstance();
        var references = ArrayBuilder<string>.GetInstance();

        foreach (var renameDefinition in renameInfo.DefinitionLocations)
        {
            var containingStatementOrDeclarationSpan =
                await renameDefinition.Document.TryGetSurroundingNodeSpanAsync<StatementSyntax>(renameDefinition.SourceSpan, cancellationToken).ConfigureAwait(false) ??
                await renameDefinition.Document.TryGetSurroundingNodeSpanAsync<MemberDeclarationSyntax>(renameDefinition.SourceSpan, cancellationToken).ConfigureAwait(false);

            var syntaxTree = await renameDefinition.Document.GetSyntaxTreeAsync(cancellationToken);
            var textSpan = await syntaxTree?.GetTextAsync(cancellationToken);
            var lineSpan = syntaxTree?.GetLineSpan(renameDefinition.SourceSpan, cancellationToken);

            if (lineSpan is null || textSpan is null)
            {
                continue;
            }

            AddSpanOfInterest(textSpan, lineSpan.Value, containingStatementOrDeclarationSpan, definitions);
        }

        var renameLocationOptions = new Rename.SymbolRenameOptions(RenameOverloads: true, RenameInStrings: true, RenameInComments: true);
        var renameLocations = await renameInfo.FindRenameLocationsAsync(renameLocationOptions, cancellationToken);
        foreach (var renameLocation in renameLocations.Locations)
        {
            var containingStatementOrDeclarationSpan =
                await renameLocation.Document.TryGetSurroundingNodeSpanAsync<BaseMethodDeclarationSyntax>(renameLocation.TextSpan, cancellationToken).ConfigureAwait(false) ??
                await renameLocation.Document.TryGetSurroundingNodeSpanAsync<StatementSyntax>(renameLocation.TextSpan, cancellationToken).ConfigureAwait(false) ??
                await renameLocation.Document.TryGetSurroundingNodeSpanAsync<MemberDeclarationSyntax>(renameLocation.TextSpan, cancellationToken).ConfigureAwait(false);

            var syntaxTree = await renameLocation.Document.GetSyntaxTreeAsync(cancellationToken);
            var textSpan = await syntaxTree?.GetTextAsync(cancellationToken);
            var lineSpan = syntaxTree?.GetLineSpan(renameLocation.TextSpan, cancellationToken);

            if (lineSpan is null || textSpan is null)
            {
                continue;
            }

            AddSpanOfInterest(textSpan, lineSpan.Value, containingStatementOrDeclarationSpan, references);
        }

        var context = ImmutableDictionary<string, string[]>.Empty
            .Add("definition", definitions.ToArrayAndFree())
            .Add("reference", references.ToArrayAndFree());
        return context;

        void AddSpanOfInterest(SourceText documentText, FileLinePositionSpan lineSpan, TextSpan? surroundingSpanOfInterest, ArrayBuilder<string> resultBuilder)
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
                startLine = Math.Max(0, lineSpan.StartLinePosition.Line - 5);
                endLine = Math.Min(documentText.Lines.Count - 1, lineSpan.EndLinePosition.Line + 5);
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
