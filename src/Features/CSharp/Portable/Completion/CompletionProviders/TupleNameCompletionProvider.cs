// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.Completion.Providers;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Extensions.ContextQuery;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.ErrorReporting;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.LanguageService;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Completion.Providers;

[ExportCompletionProvider(nameof(TupleNameCompletionProvider), LanguageNames.CSharp)]
[ExtensionOrder(After = nameof(XmlDocCommentCompletionProvider))]
[Shared]
internal sealed class TupleNameCompletionProvider : LSPCompletionProvider
{
    private const string ColonString = ":";

    [ImportingConstructor]
    [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    public TupleNameCompletionProvider()
    {
    }

    internal override string Language => LanguageNames.CSharp;

    public override async Task ProvideCompletionsAsync(CompletionContext completionContext)
    {
        try
        {
            var document = completionContext.Document;
            var cancellationToken = completionContext.CancellationToken;

            var context = await completionContext.GetSyntaxContextWithExistingSpeculativeModelAsync(document, cancellationToken).ConfigureAwait(false) as CSharpSyntaxContext;
            Contract.ThrowIfNull(context);

            var semanticModel = context.SemanticModel;

            var index = GetElementIndex(context);
            if (index == null)
            {
                return;
            }

            var typeInferrer = document.GetRequiredLanguageService<ITypeInferenceService>();
            var inferredTypes = typeInferrer.InferTypes(semanticModel, context.TargetToken.Parent!.SpanStart, cancellationToken)
                    .Where(t => t.IsTupleType)
                    .Cast<INamedTypeSymbol>()
                    .ToImmutableArray();

            AddItems(inferredTypes, index.Value, completionContext, context.TargetToken.Parent.SpanStart);
        }
        catch (Exception e) when (FatalError.ReportAndCatchUnlessCanceled(e, ErrorSeverity.General))
        {
            // nop
        }
    }

    private static int? GetElementIndex(CSharpSyntaxContext context)
    {
        var token = context.TargetToken;
        if (token.IsKind(SyntaxKind.OpenParenToken))
        {
            if (token.Parent is (kind: SyntaxKind.ParenthesizedExpression or SyntaxKind.TupleExpression or SyntaxKind.CastExpression))
            {
                return 0;
            }
        }

        if (token.IsKind(SyntaxKind.CommaToken) && token.Parent is TupleExpressionSyntax tupleExpr)
        {
            return (tupleExpr.Arguments.GetWithSeparators().IndexOf(context.TargetToken) + 1) / 2;
        }

        return null;
    }

    private static void AddItems(ImmutableArray<INamedTypeSymbol> inferredTypes, int index, CompletionContext context, int spanStart)
    {
        foreach (var type in inferredTypes)
        {
            if (index >= type.TupleElements.Length)
            {
                return;
            }

            // Note: the filter text does not include the ':'.  We want to ensure that if
            // the user types the name exactly (up to the colon) that it is selected as an
            // exact match.

            var field = type.TupleElements[index];

            context.AddItem(SymbolCompletionItem.CreateWithSymbolId(
              displayText: field.Name,
              displayTextSuffix: ColonString,
              symbols: [field],
              rules: CompletionItemRules.Default,
              contextPosition: spanStart,
              filterText: field.Name));
        }
    }

    protected override Task<TextChange?> GetTextChangeAsync(CompletionItem selectedItem, char? ch, CancellationToken cancellationToken)
    {
        return Task.FromResult<TextChange?>(new TextChange(
            selectedItem.Span,
            selectedItem.DisplayText));
    }

    public override ImmutableHashSet<char> TriggerCharacters => [];
}
