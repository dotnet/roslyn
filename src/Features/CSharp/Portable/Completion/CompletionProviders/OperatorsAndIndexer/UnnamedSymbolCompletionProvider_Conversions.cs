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
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.Completion.Providers;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Host.Mef;
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
                symbols: ImmutableArray.Create(conversion),
                rules: CompletionItemRules.Default,
                contextPosition: position,
                properties: ConversionProperties.Add(MinimalTypeNamePropertyName, $"{targetTypeName}{optionalNullableQuestionmark}");

            context.AddItem(item);
        }

        private async Task<CompletionChange> GetConversionChangeAsync(
            Document document, CompletionItem item, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();

            //var position = SymbolCompletionItem.GetContextPosition(item);
            //Contract.ThrowIfFalse(item.Properties.TryGetValue(MinimalTypeNamePropertyName, out var typeName));

            //var root = await document.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            //var (tokenAtPosition, potentialDotTokenLeftOfCursor) = FindTokensAtPosition(root, position);
            //// syntax tree manipulations are too complicated if a mixture of conditionals is involved. Some text
            //// manipulation is easier here.

            ////                      ↓               | cursor position
            ////                   ↓                  | potentialDotTokenLeftOfCursor
            //// white?.Black.White.Black?.White      | current user input
            //// white?.Black.White.Black?.White      | rootExpression (text manipulation starts with this)
            ////       .Black.White                   | parentExpression (needed to calculate the position to insert the closing brace)
            ////                    Black             | identifier at cursor position (gets removed, because the user typed the name of a type)
            //// |----------------------|             | part to replace (TextChange.Span), if identifier is not present: ends at rootExpression.End (after White.)
            ////                   ↑                  | insert closing brace between White and dot (parentExpression.Span.End)
            //// ((Black)white?.Black.White).?.White  | The result. Because we removed the identifier, the remainder after the identifier may be syntactically wrong 
            ////                             ↑        | cursor after the manipulation is placed after the dot
            //var rootExpression = GetRootExpressionOfToken(potentialDotTokenLeftOfCursor);
            //var parentExpression = GetParentExpressionOfToken(potentialDotTokenLeftOfCursor);
            //var tokenToRemove = FindTokenToRemoveAtCursorPosition(tokenAtPosition);
            //if (rootExpression is null || parentExpression is null)
            //{
            //    // ProvideCompletionsAsync only adds CompletionItems, if GetParentExpressionOfToken returns an expression.
            //    // If GetParentExpressionOfToken returns an Expression, then GetRootExpressionOfToken should return an Expression too.
            //    throw ExceptionUtilities.Unreachable;
            //}

            //var spanToReplace = TextSpan.FromBounds(rootExpression.Span.Start, tokenToRemove.HasValue ? tokenToRemove.Value.Span.End : rootExpression.Span.End);
            //var cursorPositionOffset = spanToReplace.End - position;
            //var fromRootToParent = rootExpression.ToString();
            //if (tokenToRemove is SyntaxToken token)
            //{
            //    // Cut off the identifier
            //    var length = token.Span.Start - rootExpression.SpanStart;
            //    fromRootToParent = fromRootToParent.Substring(0, length);
            //    // place cursor right behind ).
            //    cursorPositionOffset = 0;
            //}

            //var fromRootToParentWithInsertedClosingBracket = fromRootToParent.Insert(parentExpression.Span.End - rootExpression.SpanStart, ")");
            //var conversion = $"(({typeName}){fromRootToParentWithInsertedClosingBracket}";
            //var newPosition = spanToReplace.Start + conversion.Length - cursorPositionOffset;

            //return CompletionChange.Create(new TextChange(spanToReplace, conversion), newPosition);
        }

        private Task<CompletionDescription> GetConversionDescriptionAsync(Document document, CompletionItem item, CancellationToken cancellationToken)
        {
            return SymbolCompletionItem.GetDescriptionAsync(item, document, cancellationToken);
        }
        //    var symbols = await SymbolCompletionItem.GetSymbolsAsync(item, document, cancellationToken).ConfigureAwait(false);
        //    return symbols.Length == 2 && symbols[0] is INamedTypeSymbol from && symbols[1] is INamedTypeSymbol to
        //        ? await GetBuiltInConversionDescriptionAsync(document, item, from, to, cancellationToken).ConfigureAwait(false)
        //        : await base.GetDescriptionWorkerAsync(document, item, cancellationToken).ConfigureAwait(false);
        //}

        //private static async Task<CompletionDescription> GetBuiltInConversionDescriptionAsync(Document document,
        //    CompletionItem item, INamedTypeSymbol fromType, INamedTypeSymbol toType, CancellationToken cancellationToken)
        //{
        //    var symbol = CodeGenerationSymbolFactory.CreateConversionSymbol(
        //        attributes: default,
        //        accessibility: Accessibility.Public,
        //        modifiers: DeclarationModifiers.Static,
        //        toType: toType,
        //        fromType: CodeGenerationSymbolFactory.CreateParameterSymbol(fromType, "value"));
        //    return await SymbolCompletionItem.GetDescriptionForSymbolsAsync(
        //        item, document, ImmutableArray.Create<ISymbol>(symbol), cancellationToken).ConfigureAwait(false);
        //}
    }
}
