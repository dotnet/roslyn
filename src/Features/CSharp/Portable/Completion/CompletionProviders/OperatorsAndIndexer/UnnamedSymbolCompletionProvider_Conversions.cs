// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeGeneration;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.Completion.Providers;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.LanguageService;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.LanguageService;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Completion.Providers;

internal partial class UnnamedSymbolCompletionProvider
{
    // Place conversions before operators.
    private const int ConversionSortingGroupIndex = 1;

    /// <summary>
    /// Tag to let us know we need to rehydrate the conversion from the parameter and return type.
    /// </summary>
    private const string RehydrateName = "Rehydrate";
    private static readonly ImmutableArray<KeyValuePair<string, string>> s_conversionProperties =
        [KeyValuePairUtil.Create(KindName, ConversionKindName)];

    // We set conversion items' match priority to "Deprioritize" so completion selects other symbols over it when user starts typing.
    // e.g. method symbol `Should` should be selected over `(short)` when "sh" is typed.
    private static readonly CompletionItemRules s_conversionRules = CompletionItemRules.Default.WithMatchPriority(MatchPriority.Deprioritize);

    private static void AddConversion(CompletionContext context, SemanticModel semanticModel, int position, IMethodSymbol conversion)
    {
        var (symbols, properties) = GetConversionSymbolsAndProperties(context, conversion);

        var targetTypeName = conversion.ReturnType.ToMinimalDisplayString(semanticModel, position);
        context.AddItem(SymbolCompletionItem.CreateWithSymbolId(
            displayTextPrefix: "(",
            displayText: targetTypeName,
            displayTextSuffix: ")",
            filterText: targetTypeName,
            sortText: SortText(ConversionSortingGroupIndex, targetTypeName),
            glyph: Glyph.Operator,
            symbols: symbols,
            rules: s_conversionRules,
            contextPosition: position,
            properties: properties,
            isComplexTextEdit: true));
    }

    private static (ImmutableArray<ISymbol> symbols, ImmutableArray<KeyValuePair<string, string>> properties) GetConversionSymbolsAndProperties(
        CompletionContext context, IMethodSymbol conversion)
    {
        // If it's a non-synthesized method, then we can just encode it as is.
        if (conversion is not CodeGenerationSymbol)
            return (ImmutableArray.Create<ISymbol>(conversion), s_conversionProperties);

        // Otherwise, encode the constituent parts so we can recover it in GetConversionDescriptionAsync;
        using var _ = ArrayBuilder<KeyValuePair<string, string>>.GetInstance(out var builder);

        builder.AddRange(s_conversionProperties);
        builder.Add(KeyValuePairUtil.Create(RehydrateName, RehydrateName));
        builder.Add(KeyValuePairUtil.Create(DocumentationCommentXmlName, conversion.GetDocumentationCommentXml(cancellationToken: context.CancellationToken) ?? ""));
        var symbols = ImmutableArray.Create<ISymbol>(conversion.ContainingType, conversion.Parameters.First().Type, conversion.ReturnType);
        return (symbols, builder.ToImmutable());
    }

    private static async Task<CompletionChange> GetConversionChangeAsync(
        Document document, CompletionItem item, CancellationToken cancellationToken)
    {
        var position = SymbolCompletionItem.GetContextPosition(item);
        var text = await document.GetValueTextAsync(cancellationToken).ConfigureAwait(false);
        var root = await document.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
        var (dotToken, _) = GetDotAndExpressionStart(root, position, cancellationToken);

        var questionToken = dotToken.GetPreviousToken().Kind() == SyntaxKind.QuestionToken
            ? dotToken.GetPreviousToken()
            : (SyntaxToken?)null;

        var expression = (ExpressionSyntax)dotToken.GetRequiredParent();
        expression = expression.GetRootConditionalAccessExpression() ?? expression;

        using var _ = ArrayBuilder<TextChange>.GetInstance(out var builder);

        // First, add the cast prior to the expression.
        var castText = $"(({item.DisplayText})";
        builder.Add(new TextChange(new TextSpan(expression.SpanStart, 0), castText));

        // The expression went up to either a `.`, `..`, `?.` or `?..`
        //
        // In the case of `expr.` produce `((T)expr)$$`
        //
        // In the case of `expr..` produce ((T)expr)$$.
        //
        // In the case of `expr?.` produce `((T)expr)?$$`
        if (questionToken == null)
        {
            // Always eat the first dot in `.` or `..` and replace that with the paren.
            builder.Add(new TextChange(new TextSpan(dotToken.SpanStart, 1), ")"));
        }
        else
        {
            // Place a paren before the question.
            builder.Add(new TextChange(new TextSpan(questionToken.Value.SpanStart, 0), ")"));
            // then remove the first dot that comes after.
            builder.Add(new TextChange(new TextSpan(dotToken.SpanStart, 1), ""));
        }

        // If the user partially wrote out the conversion type, delete what they've written.
        var tokenOnLeft = root.FindTokenOnLeftOfPosition(position, includeSkipped: true);
        if (CSharpSyntaxFacts.Instance.IsWord(tokenOnLeft))
            builder.Add(new TextChange(tokenOnLeft.Span, ""));

        var newText = text.WithChanges(builder);
        var allChanges = builder.ToImmutable();

        // Collapse all text changes down to a single change (for clients that only care about that), but also keep
        // all the individual changes around for clients that prefer the fine-grained information.
        return CompletionChange.Create(
            CodeAnalysis.Completion.Utilities.Collapse(newText, allChanges),
            allChanges);
    }

    private static async Task<CompletionDescription?> GetConversionDescriptionAsync(Document document, CompletionItem item, SymbolDescriptionOptions displayOptions, CancellationToken cancellationToken)
    {
        var conversion = await TryRehydrateAsync(document, item, cancellationToken).ConfigureAwait(false);
        if (conversion == null)
            return null;

        return await SymbolCompletionItem.GetDescriptionForSymbolsAsync(
            item, document, [conversion], displayOptions, cancellationToken).ConfigureAwait(false);
    }

    private static async Task<ISymbol?> TryRehydrateAsync(Document document, CompletionItem item, CancellationToken cancellationToken)
    {
        // If we're need to rehydrate the conversion, pull out the necessary parts.
        if (item.TryGetProperty(RehydrateName, out var _))
        {
            var symbols = await SymbolCompletionItem.GetSymbolsAsync(item, document, cancellationToken).ConfigureAwait(false);
            if (symbols is [INamedTypeSymbol containingType, ITypeSymbol fromType, ITypeSymbol toType])
            {
                return CodeGenerationSymbolFactory.CreateConversionSymbol(
                    toType: toType,
                    fromType: CodeGenerationSymbolFactory.CreateParameterSymbol(fromType, "value"),
                    containingType: containingType,
                    documentationCommentXml: item.GetProperty(DocumentationCommentXmlName));
            }

            return null;
        }
        else
        {
            // Otherwise, just go retrieve the conversion directly.
            var symbols = await SymbolCompletionItem.GetSymbolsAsync(item, document, cancellationToken).ConfigureAwait(false);
            return symbols.Length == 1 ? symbols.Single() : null;
        }
    }
}
