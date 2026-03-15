
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.LanguageService;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.Completion;

namespace Microsoft.CodeAnalysis.Completion.Providers;

internal abstract class AbstractAsyncSupportingCompletionProvider : LSPCompletionProvider
{
    protected const string Position = nameof(Position);
    protected const string LeftTokenPosition = nameof(LeftTokenPosition);
    protected const string AddModifiers = nameof(AddModifiers);

    protected abstract int GetAsyncKeywordInsertionPosition(SyntaxNode declaration);
    protected abstract Task<TextChange?> GetReturnTypeChangeAsync(Solution solution, SemanticModel semanticModel, SyntaxNode declaration, CancellationToken cancellationToken);
    protected abstract SyntaxNode? GetAsyncSupportingDeclaration(SyntaxToken leftToken, int position);

    public override async Task<CompletionChange> GetChangeAsync(Document document, CompletionItem item, char? commitKey = null, CancellationToken cancellationToken = default)
    {
        // IsComplexTextEdit is true when we want to add async to the container or do other complex edits.
        if (!item.IsComplexTextEdit)
            return await base.GetChangeAsync(document, item, commitKey, cancellationToken).ConfigureAwait(false);

        var (_, completionChanges) = await GetCompletionTextChangesAsync(document, item, commitKey, cancellationToken).ConfigureAwait(false);

        if (!item.TryGetProperty(AddModifiers, out _))
            return await CreateCompletionChangeAsync(completionChanges).ConfigureAwait(false);

        if (!item.TryGetProperty(Position, out var positionStr) || !int.TryParse(positionStr, out var position))
            position = item.Span.Start;

        if (!item.TryGetProperty(LeftTokenPosition, out var leftTokenPositionStr) || !int.TryParse(leftTokenPositionStr, out var leftTokenPosition))
            leftTokenPosition = item.Span.Start;

        var solution = document.Project.Solution;
        var semanticModel = await document.GetRequiredSemanticModelAsync(cancellationToken).ConfigureAwait(false);
        var syntaxTree = await document.GetRequiredSyntaxTreeAsync(cancellationToken).ConfigureAwait(false);
        var root = await syntaxTree.GetRootAsync(cancellationToken).ConfigureAwait(false);
        var leftToken = root.FindToken(leftTokenPosition);

        var declaration = GetAsyncSupportingDeclaration(leftToken, position);
        if (declaration == null)
            return await CreateCompletionChangeAsync(completionChanges).ConfigureAwait(false);

        var asyncKeywordInsertionPosition = GetAsyncKeywordInsertionPosition(declaration);
        var returnTypeChange = await GetReturnTypeChangeAsync(solution, semanticModel, declaration, cancellationToken).ConfigureAwait(false);

        var builder = ArrayBuilder<TextChange>.GetInstance();
        builder.AddRange(completionChanges);

        var prefixChanges = await GetPrefixTextChangesAsync(document, declaration, asyncKeywordInsertionPosition, cancellationToken).ConfigureAwait(false);
        builder.AddRange(prefixChanges);

        if (returnTypeChange != null)
            builder.Add(returnTypeChange.Value);

        var addImportsChanges = returnTypeChange == null
            ? []
            : await GetAddImportTextChangesAsync(document, leftTokenPosition, cancellationToken).ConfigureAwait(false);
        builder.AddRange(addImportsChanges);

        var allChanges = builder.ToImmutableAndFree().OrderBy(c => c.Span.Start).ToImmutableArray();
        return await CreateCompletionChangeAsync(allChanges).ConfigureAwait(false);

        async Task<CompletionChange> CreateCompletionChangeAsync(ImmutableArray<TextChange> changes)
        {
            var originalText = await document.GetValueTextAsync(cancellationToken).ConfigureAwait(false);
            var finalNewText = originalText.WithChanges(changes);
            return CompletionChange.Create(Utilities.Collapse(finalNewText, changes), changes);
        }
    }

    protected virtual Task<ImmutableArray<TextChange>> GetPrefixTextChangesAsync(Document document, SyntaxNode declaration, int insertionPosition, CancellationToken cancellationToken)
    {
        var syntaxFacts = document.GetRequiredLanguageService<ISyntaxFactsService>();
        var asyncKeyword = syntaxFacts.GetText(syntaxFacts.SyntaxKinds.AsyncKeyword);
        return Task.FromResult(ImmutableArray.Create(new TextChange(new TextSpan(insertionPosition, 0), asyncKeyword + " ")));
    }

    protected virtual string GetRequiredNamespace()
        => "System.Threading.Tasks";

    protected virtual Task<ImmutableArray<TextChange>> GetAddImportTextChangesAsync(Document document, int position, CancellationToken cancellationToken)
        => ImportCompletionProviderHelpers.GetAddImportTextChangesAsync(document, position, GetRequiredNamespace(), cancellationToken);

    protected virtual Task<(TextChange textChange, ImmutableArray<TextChange> allChanges)> GetCompletionTextChangesAsync(
        Document document, CompletionItem item, char? commitKey, CancellationToken cancellationToken)
    {
        return Task.FromResult((new TextChange(item.Span, item.DisplayText), ImmutableArray.Create(new TextChange(item.Span, item.DisplayText))));
    }
}
