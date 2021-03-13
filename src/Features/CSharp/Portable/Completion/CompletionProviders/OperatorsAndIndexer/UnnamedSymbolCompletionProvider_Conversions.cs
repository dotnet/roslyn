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
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Completion.Providers
{
    internal partial class UnnamedSymbolCompletionProvider
    {
        private readonly int ConversionSortingGroupIndex = 1;
        private readonly ImmutableDictionary<string, string> ConversionProperties =
            ImmutableDictionary<string, string>.Empty.Add(KindName, ConversionKindName);

        private void AddConversion(CompletionContext context, SemanticModel semanticModel, int position, IMethodSymbol conversion)
        {
            var symbols = GetConversionSymbols(conversion);

            var targetTypeName = conversion.ReturnType.ToMinimalDisplayString(semanticModel, position);
            var targetTypeIsNullable = false;
            var optionalNullableQuestionmark = targetTypeIsNullable ? "?" : "";
            var item = SymbolCompletionItem.CreateWithSymbolId(
                displayTextPrefix: "(",
                displayText: targetTypeName,
                displayTextSuffix: $"{optionalNullableQuestionmark})",
                filterText: targetTypeName,
                sortText: SortText(ConversionSortingGroupIndex, targetTypeName),
                glyph: Glyph.Operator,
                symbols: symbols,
                rules: CompletionItemRules.Default,
                contextPosition: position,
                properties: ConversionProperties
                    .Add(MinimalTypeNamePropertyName, $"{targetTypeName}{optionalNullableQuestionmark}")
                    .Add(DocumentationCommentXmlName, conversion.GetDocumentationCommentXml(cancellationToken: context.CancellationToken) ?? ""));

            context.AddItem(item);
        }

        private static ImmutableArray<ISymbol> GetConversionSymbols(IMethodSymbol conversion)
        {
            // If it's a non-synthesized method, then we can just encode it as is.
            if (conversion is not CodeGenerationSymbol)
                return ImmutableArray.Create<ISymbol>(conversion);

            // Otherwise, keep track of the to/from types and we'll rehydrate this when needed.
            return ImmutableArray.Create<ISymbol>(conversion.ContainingType, conversion.Parameters.First().Type, conversion.ReturnType);
        }

        private static async Task<CompletionChange> GetConversionChangeAsync(
            Document document, CompletionItem item, CancellationToken cancellationToken)
        {
            var position = SymbolCompletionItem.GetContextPosition(item);
            var text = await document.GetTextAsync(cancellationToken).ConfigureAwait(false);
            var root = await document.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var (token, dotToken, _) = FindTokensAtPosition(root, position);

            var questionToken = dotToken.GetPreviousToken().Kind() == SyntaxKind.QuestionToken
                ? dotToken.GetPreviousToken()
                : (SyntaxToken?)null;

            var expression = (ExpressionSyntax)dotToken.GetRequiredParent();
            expression = expression.GetRootConditionalAccessExpression() ?? expression;

            var replacement = questionToken != null
                ? $"(({item.DisplayText}){text.ToString(TextSpan.FromBounds(expression.SpanStart, questionToken.Value.FullSpan.Start))}){questionToken.Value}"
                : $"(({item.DisplayText}){text.ToString(TextSpan.FromBounds(expression.SpanStart, dotToken.SpanStart))})";
            var fullTextChange = new TextChange(
                TextSpan.FromBounds(expression.SpanStart, token.Span.End),
                replacement);

            var newPosition = expression.SpanStart + replacement.Length;
            return CompletionChange.Create(fullTextChange, newPosition);
        }

        private static async Task<CompletionDescription?> GetConversionDescriptionAsync(Document document, CompletionItem item, CancellationToken cancellationToken)
        {
            var symbols = await SymbolCompletionItem.GetSymbolsAsync(item, document, cancellationToken).ConfigureAwait(false);

            ISymbol symbol;
            if (symbols.Length == 1)
            {
                // We successfully found the original conversion method.
                symbol = symbols[0];
            }
            else if (symbols.Length == 3 &&
                symbols[0] is INamedTypeSymbol containingType &&
                symbols[1] is ITypeSymbol fromType &&
                symbols[2] is ITypeSymbol toType)
            {
                // Otherwise, this was synthesized.  So rehydrate the synthesized symbol.
                symbol = CodeGenerationSymbolFactory.CreateConversionSymbol(
                    attributes: default,
                    accessibility: Accessibility.Public,
                    modifiers: DeclarationModifiers.Static,
                    toType: toType,
                    fromType: CodeGenerationSymbolFactory.CreateParameterSymbol(fromType, "value"),
                    containingType: containingType,
                    documentationCommentXml: item.Properties[DocumentationCommentXmlName]);
            }
            else
            {
                return null;
            }

            return await SymbolCompletionItem.GetDescriptionForSymbolsAsync(
                item, document, ImmutableArray.Create(symbol), cancellationToken).ConfigureAwait(false);
        }
    }
}
