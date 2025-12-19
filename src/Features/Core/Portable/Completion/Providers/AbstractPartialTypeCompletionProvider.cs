// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.ErrorReporting;
using Microsoft.CodeAnalysis.LanguageService;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.Extensions.ContextQuery;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Completion.Providers;

internal abstract partial class AbstractPartialTypeCompletionProvider<TSyntaxContext> : LSPCompletionProvider
    where TSyntaxContext : SyntaxContext
{
    protected AbstractPartialTypeCompletionProvider()
    {
    }

    public sealed override async Task ProvideCompletionsAsync(CompletionContext completionContext)
    {
        try
        {
            var document = completionContext.Document;
            var position = completionContext.Position;
            var cancellationToken = completionContext.CancellationToken;

            var tree = await document.GetRequiredSyntaxTreeAsync(cancellationToken).ConfigureAwait(false);
            var node = GetPartialTypeSyntaxNode(tree, position, cancellationToken);

            if (node != null)
            {
                var semanticModel = await document.ReuseExistingSpeculativeModelAsync(node, cancellationToken).ConfigureAwait(false);
                if (semanticModel.GetDeclaredSymbol(node, cancellationToken) is INamedTypeSymbol declaredSymbol)
                {
                    var syntaxContextService = document.GetRequiredLanguageService<ISyntaxContextService>();
                    var syntaxContext = (TSyntaxContext)syntaxContextService.CreateContext(document, semanticModel, position, cancellationToken);
                    var symbols = LookupCandidateSymbols(syntaxContext, declaredSymbol, cancellationToken);
                    var items = symbols?.Select(s => CreateCompletionItem(s, syntaxContext));

                    if (items != null)
                    {
                        completionContext.AddItems(items);
                    }
                }
            }
        }
        catch (Exception e) when (FatalError.ReportAndCatchUnlessCanceled(e, ErrorSeverity.General))
        {
            // nop
        }
    }

    private CompletionItem CreateCompletionItem(INamedTypeSymbol symbol, TSyntaxContext context)
    {
        var (displayText, suffix, insertionText) = GetDisplayAndSuffixAndInsertionText(symbol, context);

        return SymbolCompletionItem.CreateWithSymbolId(
            displayText: displayText,
            displayTextSuffix: suffix,
            insertionText: insertionText,
            symbols: [symbol],
            contextPosition: context.Position,
            properties: GetProperties(symbol, context),
            rules: CompletionItemRules.Default);
    }

    protected abstract ImmutableArray<KeyValuePair<string, string>> GetProperties(INamedTypeSymbol symbol, TSyntaxContext context);

    protected abstract SyntaxNode? GetPartialTypeSyntaxNode(SyntaxTree tree, int position, CancellationToken cancellationToken);

    protected abstract (string displayText, string suffix, string insertionText) GetDisplayAndSuffixAndInsertionText(INamedTypeSymbol symbol, TSyntaxContext context);

    protected virtual IEnumerable<INamedTypeSymbol>? LookupCandidateSymbols(TSyntaxContext context, INamedTypeSymbol declaredSymbol, CancellationToken cancellationToken)
    {
        if (declaredSymbol == null)
        {
            throw new ArgumentNullException(nameof(declaredSymbol));
        }

        var semanticModel = context.SemanticModel;

        if (declaredSymbol.ContainingSymbol is not INamespaceOrTypeSymbol containingSymbol)
            return [];

        return semanticModel.LookupNamespacesAndTypes(context.Position, containingSymbol)
                            .OfType<INamedTypeSymbol>()
                            .Where(symbol => declaredSymbol.TypeKind == symbol.TypeKind &&
                                             NotNewDeclaredMember(symbol, context) &&
                                             InSameProject(symbol, semanticModel.Compilation));
    }

    private static bool InSameProject(INamedTypeSymbol symbol, Compilation compilation)
        => symbol.DeclaringSyntaxReferences.Any(static (r, compilation) => compilation.SyntaxTrees.Contains(r.SyntaxTree), compilation);

    private static bool NotNewDeclaredMember(INamedTypeSymbol symbol, TSyntaxContext context)
    {
        return symbol.DeclaringSyntaxReferences
                     .Select(reference => reference.GetSyntax())
                     .Any(node => !(node.SyntaxTree == context.SyntaxTree && node.Span.IntersectsWith(context.Position)));
    }

    internal override Task<CompletionDescription> GetDescriptionWorkerAsync(Document document, CompletionItem item, CompletionOptions options, SymbolDescriptionOptions displayOptions, CancellationToken cancellationToken)
        => SymbolCompletionItem.GetDescriptionAsync(item, document, displayOptions, cancellationToken);

    public override async Task<TextChange?> GetTextChangeAsync(Document document, CompletionItem selectedItem, char? ch, CancellationToken cancellationToken)
    {
        var insertionText = SymbolCompletionItem.GetInsertionText(selectedItem);
        return new TextChange(selectedItem.Span, insertionText);
    }
}
