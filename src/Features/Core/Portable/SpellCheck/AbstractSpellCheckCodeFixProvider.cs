// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.LanguageService;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.SpellCheck;

internal abstract class AbstractSpellCheckCodeFixProvider<TSimpleName> : CodeFixProvider
    where TSimpleName : SyntaxNode
{
    private const int MinTokenLength = 3;

    public override FixAllProvider GetFixAllProvider()
    {
        // Fix All is not supported by this code fix 
        // https://github.com/dotnet/roslyn/issues/34462
        return null;
    }

    protected abstract bool IsGeneric(SyntaxToken nameToken);
    protected abstract bool IsGeneric(TSimpleName nameNode);
    protected abstract bool IsGeneric(CompletionItem completionItem);
    protected abstract SyntaxToken CreateIdentifier(SyntaxToken nameToken, string newName);

    public override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        var document = context.Document;
        if (!document.CanApplyChange())
            return;

        var span = context.Span;
        var cancellationToken = context.CancellationToken;

        var syntaxRoot = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
        var node = syntaxRoot.FindNode(span);
        if (node != null && node.Span == span)
        {
            await CheckNodeAsync(context, document, node, cancellationToken).ConfigureAwait(false);
            return;
        }

        // didn't get a node that matches the span.  see if there's a token that matches.
        var token = syntaxRoot.FindToken(span.Start);
        if (token.RawKind != 0 && token.Span == span)
        {
            await CheckTokenAsync(context, document, token, cancellationToken).ConfigureAwait(false);
            return;
        }
    }

    private async Task CheckNodeAsync(CodeFixContext context, Document document, SyntaxNode node, CancellationToken cancellationToken)
    {
        SemanticModel semanticModel = null;
        foreach (var name in node.DescendantNodesAndSelf(DescendIntoChildren).OfType<TSimpleName>())
        {
            if (!ShouldSpellCheck(name))
            {
                continue;
            }

            // Only bother with identifiers that are at least 3 characters long.
            // We don't want to be too noisy as you're just starting to type something.
            var token = name.GetFirstToken();
            var nameText = token.ValueText;
            if (nameText?.Length >= MinTokenLength)
            {
                semanticModel ??= await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
                var symbolInfo = semanticModel.GetSymbolInfo(name, cancellationToken);
                if (symbolInfo.Symbol == null)
                {
                    await CreateSpellCheckCodeIssueAsync(context, token, IsGeneric(name), cancellationToken).ConfigureAwait(false);
                }
            }
        }
    }

    private async Task CheckTokenAsync(CodeFixContext context, Document document, SyntaxToken token, CancellationToken cancellationToken)
    {
        var syntaxFacts = document.GetLanguageService<ISyntaxFactsService>();
        if (!syntaxFacts.IsWord(token))
        {
            return;
        }

        var nameText = token.ValueText;
        if (nameText?.Length >= MinTokenLength)
        {
            await CreateSpellCheckCodeIssueAsync(context, token, IsGeneric(token), cancellationToken).ConfigureAwait(false);
        }
    }

    protected abstract bool ShouldSpellCheck(TSimpleName name);
    protected abstract bool DescendIntoChildren(SyntaxNode arg);

    private async Task CreateSpellCheckCodeIssueAsync(
        CodeFixContext context, SyntaxToken nameToken, bool isGeneric, CancellationToken cancellationToken)
    {
        var document = context.Document;
        var service = CompletionService.GetService(document);

        var memberDisplayOptions = await document.GetMemberDisplayOptionsAsync(cancellationToken).ConfigureAwait(false);

        // Disable snippets and unimported types from ever appearing in the completion items. 
        // -    It's very unlikely the user would ever misspell a snippet, then use spell-checking to fix it, 
        //      then try to invoke the snippet.
        // -    We believe spell-check should only compare what you have typed to what symbol would be offered here.
        var options = CompletionOptions.Default with
        {
            MemberDisplayOptions = memberDisplayOptions,
            SnippetsBehavior = SnippetsRule.NeverInclude,
            ShowItemsFromUnimportedNamespaces = false,
            TargetTypedCompletionFilter = false,
            ExpandedCompletionBehavior = ExpandedCompletionMode.NonExpandedItemsOnly
        };

        var passThroughOptions = document.Project.Solution.Options;

        var completionList = await service.GetCompletionsAsync(
            document, nameToken.SpanStart, options, passThroughOptions, cancellationToken: cancellationToken).ConfigureAwait(false);
        if (completionList.ItemsList.IsEmpty())
        {
            return;
        }

        var nameText = nameToken.ValueText;
        using var similarityChecker = new WordSimilarityChecker(nameText, substringsAreSimilar: true);

        await CheckItemsAsync(
            context, nameToken, isGeneric,
            completionList, similarityChecker).ConfigureAwait(false);
    }

    private async Task CheckItemsAsync(
        CodeFixContext context, SyntaxToken nameToken, bool isGeneric,
        CompletionList completionList, WordSimilarityChecker similarityChecker)
    {
        var document = context.Document;
        var cancellationToken = context.CancellationToken;

        var onlyConsiderGenerics = isGeneric;
        var results = new MultiDictionary<double, string>();

        foreach (var item in completionList.ItemsList)
        {
            if (onlyConsiderGenerics && !IsGeneric(item))
            {
                continue;
            }

            var candidateText = item.FilterText;
            if (!similarityChecker.AreSimilar(candidateText, out var matchCost))
            {
                continue;
            }

            var insertionText = await GetInsertionTextAsync(document, item, cancellationToken: cancellationToken).ConfigureAwait(false);
            results.Add(matchCost, insertionText);
        }

        var nameText = nameToken.ValueText;
        var codeActions = results.OrderBy(kvp => kvp.Key)
                                 .SelectMany(kvp => kvp.Value.Order())
                                 .Where(t => t != nameText)
                                 .Take(3)
                                 .Select(n => CreateCodeAction(nameToken, nameText, n, document))
                                 .ToImmutableArrayOrEmpty<CodeAction>();

        if (codeActions.Length > 1)
        {
            // Wrap the spell checking actions into a single top level suggestion
            // so as to not clutter the list.
            context.RegisterCodeFix(
                CodeAction.Create(
                    string.Format(FeaturesResources.Fix_typo_0, nameText),
                    codeActions,
                    isInlinable: true,
                    CodeActionPriority.Low),
                context.Diagnostics);
        }
        else
        {
            context.RegisterFixes(codeActions, context.Diagnostics);
        }
    }

    private static readonly char[] s_punctuation = ['(', '[', '<'];

    private static async Task<string> GetInsertionTextAsync(Document document, CompletionItem item, CancellationToken cancellationToken)
    {
        var service = CompletionService.GetService(document);
        var change = await service.GetChangeAsync(document, item, commitCharacter: null, cancellationToken).ConfigureAwait(false);
        var text = change.TextChange.NewText;
        var nonCharIndex = text.IndexOfAny(s_punctuation);
        return nonCharIndex > 0
            ? text[0..nonCharIndex]
            : text;
    }

    private CodeAction CreateCodeAction(SyntaxToken nameToken, string oldName, string newName, Document document)
    {
        return CodeAction.Create(
            string.Format(FeaturesResources.Change_0_to_1, oldName, newName),
            c => UpdateAsync(document, nameToken, newName, c),
            equivalenceKey: newName,
            CodeActionPriority.Low);
    }

    private async Task<Document> UpdateAsync(Document document, SyntaxToken nameToken, string newName, CancellationToken cancellationToken)
    {
        var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
        var newRoot = root.ReplaceToken(nameToken, CreateIdentifier(nameToken, newName));

        return document.WithSyntaxRoot(newRoot);
    }
}
