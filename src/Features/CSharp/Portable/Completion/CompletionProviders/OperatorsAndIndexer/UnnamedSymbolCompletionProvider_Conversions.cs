// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeGeneration;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.Completion.Providers;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Completion.Providers
{
    internal partial class UnnamedSymbolCompletionProvider
    {
        // Place conversions before operators.
        private const int ConversionSortingGroupIndex = 1;

        /// <summary>
        /// Tag to let us know we need to rehydrate the conversion from the parameter and return type.
        /// </summary>
        private const string RehydrateName = "Rehydrate";
        private static readonly ImmutableDictionary<string, string> s_conversionProperties =
            ImmutableDictionary<string, string>.Empty.Add(KindName, ConversionKindName);

        // We set conversion items' match priority to lower than default so completion selects other symbols over it when user starts typing.
        // e.g. method symbol `Should` should be selected over `(short)` when "sh" is typed.
        private static readonly CompletionItemRules s_conversionRules = CompletionItemRules.Default.WithMatchPriority(MatchPriority.Default - 1);

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
                properties: properties));
        }

        private static (ImmutableArray<ISymbol> symbols, ImmutableDictionary<string, string> properties) GetConversionSymbolsAndProperties(
            CompletionContext context, IMethodSymbol conversion)
        {
            // If it's a non-synthesized method, then we can just encode it as is.
            if (conversion is not CodeGenerationSymbol)
                return (ImmutableArray.Create<ISymbol>(conversion), s_conversionProperties);

            // Otherwise, encode the constituent parts so we can recover it in GetConversionDescriptionAsync;
            var properties = s_conversionProperties
                .Add(RehydrateName, RehydrateName)
                .Add(DocumentationCommentXmlName, conversion.GetDocumentationCommentXml(cancellationToken: context.CancellationToken) ?? "");
            var symbols = ImmutableArray.Create<ISymbol>(conversion.ContainingType, conversion.Parameters.First().Type, conversion.ReturnType);
            return (symbols, properties);
        }

        private static async Task<CompletionChange> GetConversionChangeAsync(
            Document document, CompletionItem item, CancellationToken cancellationToken)
        {
            var position = SymbolCompletionItem.GetContextPosition(item);
            var text = await document.GetTextAsync(cancellationToken).ConfigureAwait(false);
            var root = await document.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var (dotToken, _) = GetDotAndExpressionStart(root, position, cancellationToken);

            var questionToken = dotToken.GetPreviousToken().Kind() == SyntaxKind.QuestionToken
                ? dotToken.GetPreviousToken()
                : (SyntaxToken?)null;

            var expression = (ExpressionSyntax)dotToken.GetRequiredParent();
            expression = expression.GetRootConditionalAccessExpression() ?? expression;

            var replacement = questionToken != null
                ? $"(({item.DisplayText}){text.ToString(TextSpan.FromBounds(expression.SpanStart, questionToken.Value.FullSpan.Start))}){questionToken.Value}"
                : $"(({item.DisplayText}){text.ToString(TextSpan.FromBounds(expression.SpanStart, dotToken.SpanStart))})";

            // If we're at `x.$$.y` then we only want to replace up through the first dot.
            var tokenOnLeft = root.FindTokenOnLeftOfPosition(position, includeSkipped: true);
            var fullTextChange = new TextChange(
                TextSpan.FromBounds(
                    expression.SpanStart,
                    tokenOnLeft.Kind() == SyntaxKind.DotDotToken ? tokenOnLeft.SpanStart + 1 : tokenOnLeft.Span.End),
                replacement);

            var newPosition = expression.SpanStart + replacement.Length;
            return CompletionChange.Create(fullTextChange, newPosition);
        }

        private static async Task<CompletionDescription?> GetConversionDescriptionAsync(Document document, CompletionItem item, CancellationToken cancellationToken)
        {
            var conversion = await TryRehydrateAsync(document, item, cancellationToken).ConfigureAwait(false);
            if (conversion == null)
                return null;

            return await SymbolCompletionItem.GetDescriptionForSymbolsAsync(
                item, document, ImmutableArray.Create(conversion), cancellationToken).ConfigureAwait(false);
        }

        private static async Task<ISymbol?> TryRehydrateAsync(Document document, CompletionItem item, CancellationToken cancellationToken)
        {
            // If we're need to rehydrate the conversion, pull out the necessary parts.
            if (item.Properties.ContainsKey(RehydrateName))
            {
                var symbols = await SymbolCompletionItem.GetSymbolsAsync(item, document, cancellationToken).ConfigureAwait(false);
                if (symbols.Length == 3 &&
                    symbols[0] is INamedTypeSymbol containingType &&
                    symbols[1] is ITypeSymbol fromType &&
                    symbols[2] is ITypeSymbol toType)
                {
                    return CodeGenerationSymbolFactory.CreateConversionSymbol(
                        toType: toType,
                        fromType: CodeGenerationSymbolFactory.CreateParameterSymbol(fromType, "value"),
                        containingType: containingType,
                        documentationCommentXml: item.Properties[DocumentationCommentXmlName]);
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
}
