// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.Completion.Providers;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Completion.Providers
{
    [ExportCompletionProvider(nameof(ConversionCompletionProvider), LanguageNames.CSharp), Shared]
    [ExtensionOrder(After = nameof(SymbolCompletionProvider))]
    internal class ConversionCompletionProvider : OperatorIndexerCompletionProviderBase
    {
        private const string MinimalTypeNamePropertyName = "MinimalTypeName";

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public ConversionCompletionProvider()
        {
        }

        protected override int SortingGroupIndex => 2;

        protected override ImmutableArray<CompletionItem> GetCompletionItemsForTypeSymbol(ITypeSymbol container, SemanticModel semanticModel, int position)
        {
            var containerIsNullable = container.IsNullable();
            container = container.RemoveNullableIfPresent();
            var allMembers = container.GetMembers();
            var allExplicitConversions = from m in allMembers.OfType<IMethodSymbol>()
                                         where
                                             m.IsConversion() && // MethodKind.Conversion
                                             m.Name == WellKnownMemberNames.ExplicitConversionName && // op_Explicit
                                             m.Parameters.Length == 1 && // Malformed conversion operator may have more or less than one parameter
                                             container.Equals(m.Parameters[0].Type) // Convert from container type to other type
                                         let typeName = m.ReturnType.ToMinimalDisplayString(semanticModel, position)
                                         // Lifted conversion https://docs.microsoft.com/hu-hu/dotnet/csharp/language-reference/language-specification/conversions#lifted-conversion-operators
                                         let optionalNullableQuestionmark = containerIsNullable && m.ReturnType.IsStructType() ? "?" : ""
                                         select SymbolCompletionItem.CreateWithSymbolId(
                                             displayTextPrefix: "(",
                                             displayText: typeName, // The type to convert to
                                             displayTextSuffix: $"{optionalNullableQuestionmark})",
                                             filterText: typeName,
                                             sortText: SortText(typeName),
                                             symbols: ImmutableList.Create(m),
                                             rules: CompletionItemRules.Default,
                                             contextPosition: position,
                                             properties: CreatePropertiesBag((MinimalTypeNamePropertyName, $"{typeName}{optionalNullableQuestionmark}")));
            var builder = ImmutableArray.CreateBuilder<CompletionItem>();
            builder.AddRange(allExplicitConversions);
            builder.AddRange(GetBuiltInNumericConversions(semanticModel, container, containerIsNullable, position));
            return builder.ToImmutable();
        }

        private ImmutableArray<CompletionItem> GetBuiltInNumericConversions(SemanticModel semanticModel, ITypeSymbol container, bool containerIsNullable, int position)
        {
            if (container.SpecialType == SpecialType.System_Decimal)
            {
                // Decimal is defined in the spec with integrated conversions, but is the only type that reports it's conversions as normal method symbols
                return ImmutableArray<CompletionItem>.Empty;
            }
            var numericConversions = container.GetBuiltInNumericConversions();
            if (numericConversions is not null)
            {
                var optionalNullableQuestionmark = containerIsNullable ? "?" : "";
                var builtInNumericConversions = from specialType in numericConversions
                                                let typeSymbol = semanticModel.Compilation.GetSpecialType(specialType)
                                                let typeName = typeSymbol.ToMinimalDisplayString(semanticModel, position)
                                                select CommonCompletionItem.Create(
                                                    displayTextPrefix: "(",
                                                    displayText: typeName,
                                                    displayTextSuffix: $"{optionalNullableQuestionmark})",
                                                    filterText: typeName,
                                                    sortText: SortText(typeName),
                                                    glyph: typeSymbol.GetGlyph(),
                                                    rules: CompletionItemRules.Default,
                                                    properties: CreatePropertiesBag(
                                                        (MinimalTypeNamePropertyName, $"{typeName}{optionalNullableQuestionmark}"),
                                                        ("ContextPosition", position.ToString())));
                return builtInNumericConversions.ToImmutableArray();
            }

            return ImmutableArray<CompletionItem>.Empty;
        }

        internal override async Task<CompletionChange> GetChangeAsync(Document document, CompletionItem item, TextSpan completionListSpan, char? commitKey, bool disallowAddingImports, CancellationToken cancellationToken)
        {
            var position = SymbolCompletionItem.GetContextPosition(item);
            Contract.ThrowIfFalse(item.Properties.TryGetValue(MinimalTypeNamePropertyName, out var typeName));

            var root = await document.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var (tokenAtPosition, potentialDotTokenLeftOfCursor) = FindTokensAtPosition(position, root);
            // syntax tree manipulations are to complicated if a mixture of conditionals is involved. Some text manipulation is easier here.
            //                      ↓               | cursor position
            //                   ↓                  | normalizedToken (dot)
            // white?.Black.White.Black?.White      | current user input
            // white?.Black.White.Black?.White      | rootExpression (text manipulation starts with this)
            //       .Black.White                   | parentExpression (needed to calculate the position to insert the closing brace)
            //                    Black             | identifier at cursor position (gets removed, because the user typed the name of a type)
            // |----------------------|             | part to replace (TextChange.Span), if identifier is not present: ends at rootExpression.End (after White.)
            //                   ↑                  | insert closing brace between White and dot (parentExpression.Span.End)
            // ((Black)white?.Black.White).?.White  | The result. Because we removed the identifier, the remainder after the identifier may be syntactically wrong 
            //                             ↑        | cursor after the manipulation is placed after the dot
            var rootExpression = GetRootExpressionOfToken(potentialDotTokenLeftOfCursor);
            var parentExpression = GetParentExpressionOfToken(potentialDotTokenLeftOfCursor);
            var tokenToRemove = FindTokenToRemoveAtCursorPosition(tokenAtPosition);
            if (rootExpression is null || parentExpression is null)
            {
                // ProvideCompletionsAsync only adds CompletionItems, if GetParentExpressionOfToken returns an expression.
                // if GetParentExpressionOfToken returns an Expression, then should GetRootExpressionOfToken return an Expression too.
                throw ExceptionUtilities.Unreachable;
            }

            var spanToReplace = TextSpan.FromBounds(rootExpression.Span.Start, tokenToRemove.HasValue ? tokenToRemove.Value.Span.End : rootExpression.Span.End);
            var cursorPositionOffset = spanToReplace.End - position;
            var fromRootToParent = rootExpression.ToString();
            if (tokenToRemove is SyntaxToken token)
            {
                // Cut off the identifier
                var length = token.Span.Start - rootExpression.SpanStart;
                fromRootToParent = fromRootToParent.Substring(0, length);
                // place cursor right behind ).
                cursorPositionOffset = 0;
            }

            var fromRootToParentWithInsertedClosingBracket = fromRootToParent.Insert(parentExpression.Span.End - rootExpression.SpanStart, ")");
            var conversion = $"(({typeName}){fromRootToParentWithInsertedClosingBracket}";
            var newPosition = spanToReplace.Start + conversion.Length - cursorPositionOffset;
            return CompletionChange.Create(new TextChange(spanToReplace, conversion), newPosition);
        }
    }
}
