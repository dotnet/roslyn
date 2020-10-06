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
using Microsoft.CodeAnalysis.CodeGeneration;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.Completion.Providers;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.PooledObjects;
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
        private const string ContainerTypeNamePropertyName = "ContainerTypeName";

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public ConversionCompletionProvider()
        {
        }

        protected override int SortingGroupIndex => 2;

        protected override ImmutableArray<CompletionItem> GetCompletionItemsForTypeSymbol(ITypeSymbol container, bool isAccessedByConditionalAccess, ExpressionSyntax expression, SemanticModel semanticModel, int position, CancellationToken cancellationToken)
        {
            // Lifted nullable value types should be suggested if container is nullable or accessed via conditional access.
            // Container is nullable: int? id; id.$$ -> (byte?)
            var containerIsNullable = container.IsNullable();

            // Container is access via conditional access: System.Diagnostics.Process p; p?.Id.$$ -> (byte?)
            containerIsNullable = containerIsNullable || (isAccessedByConditionalAccess && container.IsValueType);

            container = container.RemoveNullableIfPresent();

            using var _ = ArrayBuilder<CompletionItem>.GetInstance(out var builder);

            AddUserDefinedConversionsOfType(builder, semanticModel, container, containerIsNullable, position);
            if (container is INamedTypeSymbol namedType)
            {
                AddBuiltInNumericConversions(builder, semanticModel, namedType, containerIsNullable, position);
                AddBuiltInEnumConversions(builder, semanticModel, namedType, expression, containerIsNullable, position, cancellationToken);
            }

            return builder.ToImmutable();
        }

        private void AddUserDefinedConversionsOfType(ArrayBuilder<CompletionItem> builder, SemanticModel semanticModel, ITypeSymbol container, bool containerIsNullable, int position)
        {
            var containerOrBaseType = (ITypeSymbol?)container;
            while (containerOrBaseType is not null)
            {
                var allMembers = containerOrBaseType.GetMembers();
                var allExplicitConversions = from m in allMembers.OfType<IMethodSymbol>()
                                             where
                                                 m.IsConversion() && // MethodKind.Conversion
                                                 m.Name == WellKnownMemberNames.ExplicitConversionName && // op_Explicit
                                                 m.Parameters.Length == 1 && // Malformed conversion operator may have more or less than one parameter
                                                 containerOrBaseType.Equals(m.Parameters[0].Type) // Convert from container type to other type
                                             let typeName = m.ReturnType.ToMinimalDisplayString(semanticModel, position)
                                             // Lifted conversion https://docs.microsoft.com/hu-hu/dotnet/csharp/language-reference/language-specification/conversions#lifted-conversion-operators
                                             select CreateSymbolCompletionItem(m, typeName, targetTypeIsNullable: containerIsNullable && m.ReturnType.IsStructType(), position);
                builder.AddRange(allExplicitConversions);
                containerOrBaseType = containerOrBaseType.BaseType;
            }
        }

        private void AddBuiltInNumericConversions(ArrayBuilder<CompletionItem> builder, SemanticModel semanticModel, INamedTypeSymbol container, bool containerIsNullable, int position)
        {
            if (container.SpecialType == SpecialType.System_Decimal)
            {
                // Decimal is defined in the spec with integrated conversions, but is the only type that reports it's conversions as normal method symbols
                return;
            }
            var numericConversions = container.GetBuiltInNumericConversions();
            if (numericConversions is not null)
            {
                AddCompletionItemsForSpecialTypes(builder, semanticModel, container, containerIsNullable, position, numericConversions);
            }
        }

        private void AddBuiltInEnumConversions(ArrayBuilder<CompletionItem> builder, SemanticModel semanticModel, INamedTypeSymbol container, ExpressionSyntax expression, bool containerIsNullable, int position, CancellationToken cancellationToken)
        {
            // https://docs.microsoft.com/en-us/dotnet/csharp/language-reference/language-specification/conversions#explicit-enumeration-conversions
            // Three kinds of conversions are defined in the spec.
            // Suggestion are made for one kind:
            // * From any enum_type to sbyte, byte, short, ushort, int, uint, long, ulong, char, float, double, or decimal.
            // No suggestion for the other two kinds of conversions:
            // * From sbyte, byte, short, ushort, int, uint, long, ulong, char, float, double, or decimal to any enum_type.
            // * From any enum_type to any other enum_type.

            // Suggest after enum members: SomeEnum.EnumValue.$$
            var suggestBuiltInEnumConversion = container.IsEnumMember();

            // Suggest for enum values, but not after the enum type (don't infer with the enum member listing)
            suggestBuiltInEnumConversion = suggestBuiltInEnumConversion ||
                (container.IsEnumType() &&  // someEnumVariable.$$
                    !(semanticModel.GetSymbolInfo(expression, cancellationToken).Symbol is ITypeSymbol typeSymbol && typeSymbol.IsEnumType())); // but not SomeEnum.$$

            if (suggestBuiltInEnumConversion)
            {
                var convertToSpecialTypes = new[]
                {
                    SpecialType.System_SByte,
                    SpecialType.System_Byte,
                    SpecialType.System_Int16,
                    SpecialType.System_UInt16,
                    SpecialType.System_Int32,
                    SpecialType.System_UInt32,
                    SpecialType.System_Int64,
                    SpecialType.System_UInt64,
                    SpecialType.System_Char,
                    SpecialType.System_Single,
                    SpecialType.System_Double,
                    SpecialType.System_Decimal,
                };

                AddCompletionItemsForSpecialTypes(builder, semanticModel, container, containerIsNullable, position, convertToSpecialTypes);
            }
        }

        private void AddCompletionItemsForSpecialTypes(ArrayBuilder<CompletionItem> builder, SemanticModel semanticModel, INamedTypeSymbol fromType, bool containerIsNullable, int position, SpecialType[] specialTypes)
        {
            var containerTypeName = fromType.ToMinimalDisplayString(semanticModel, position);
            var conversionCompletionItems = from specialType in specialTypes
                                            let targetTypeSymbol = semanticModel.Compilation.GetSpecialType(specialType)
                                            let targetTypeName = targetTypeSymbol.ToMinimalDisplayString(semanticModel, position)
                                            select CreateCommonCompletionItem(containerTypeName, targetTypeName, targetTypeIsNullable: containerIsNullable, position);
            builder.AddRange(conversionCompletionItems);
        }

        private CompletionItem CreateSymbolCompletionItem(IMethodSymbol methodSymbol, string targetTypeName, bool targetTypeIsNullable, int position)
        {
            var optionalNullableQuestionmark = GetOptionalNullableQuestionMark(targetTypeIsNullable);
            return SymbolCompletionItem.CreateWithSymbolId(
                           displayTextPrefix: "(",
                           displayText: targetTypeName,
                           displayTextSuffix: $"{optionalNullableQuestionmark})",
                           filterText: targetTypeName,
                           sortText: SortText(targetTypeName),
                           symbols: ImmutableList.Create(methodSymbol),
                           rules: CompletionItemRules.Default,
                           contextPosition: position,
                           properties: CreatePropertiesBag((MinimalTypeNamePropertyName, $"{targetTypeName}{optionalNullableQuestionmark}")));
        }

        private CompletionItem CreateCommonCompletionItem(string containerTypeName, string targetTypeName, bool targetTypeIsNullable, int position)
        {
            var optionalNullableQuestionmark = GetOptionalNullableQuestionMark(targetTypeIsNullable);
            return CommonCompletionItem.Create(
                           displayTextPrefix: "(",
                           displayText: targetTypeName,
                           displayTextSuffix: $"{optionalNullableQuestionmark})",
                           filterText: targetTypeName,
                           sortText: SortText(targetTypeName),
                           glyph: Glyph.Operator,
                           rules: CompletionItemRules.Default,
                           properties: CreatePropertiesBag(
                               (MinimalTypeNamePropertyName, $"{targetTypeName}{optionalNullableQuestionmark}"),
                               (ContainerTypeNamePropertyName, containerTypeName),
                               ("ContextPosition", position.ToString())));
        }

        private static string GetOptionalNullableQuestionMark(bool isNullable)
            => isNullable ? "?" : "";

        protected override async Task<CompletionDescription> GetDescriptionWorkerAsync(Document document, CompletionItem item, CancellationToken cancellationToken)
            => SymbolCompletionItem.HasSymbols(item)
                ? await base.GetDescriptionWorkerAsync(document, item, cancellationToken).ConfigureAwait(false)
                : GetBuildInConversionDescription(item);

        private static CompletionDescription GetBuildInConversionDescription(CompletionItem item)
        {
            Contract.ThrowIfFalse(item.Properties.TryGetValue(MinimalTypeNamePropertyName, out var targetTypeName));
            Contract.ThrowIfFalse(item.Properties.TryGetValue(ContainerTypeNamePropertyName, out var sourceTypeName));
            var taggedText = new[]
            {
                new TaggedText(TextTags.Keyword, sourceTypeName),
                new TaggedText(TextTags.Punctuation, "."),
                new TaggedText(TextTags.Keyword, "explicit"),
                new TaggedText(TextTags.Space, " "),
                new TaggedText(TextTags.Keyword, "operator"),
                new TaggedText(TextTags.Space, " "),
                new TaggedText(TextTags.Keyword, targetTypeName),
                new TaggedText(TextTags.Punctuation, "("),
                new TaggedText(TextTags.Keyword, sourceTypeName),
                new TaggedText(TextTags.Space, " "),
                new TaggedText(TextTags.Parameter, "value"),
                new TaggedText(TextTags.Punctuation, ")"),
                new TaggedText(TextTags.LineBreak, Environment.NewLine),
                new TaggedText(TextTags.Text, "Defines an explicit conversion of a"),
                new TaggedText(TextTags.Space, " "),
                new TaggedText(TextTags.Keyword, sourceTypeName),
                new TaggedText(TextTags.Space, " "),
                new TaggedText(TextTags.Text, "to a"),
                new TaggedText(TextTags.Space, " "),
                new TaggedText(TextTags.Keyword, targetTypeName),
                new TaggedText(TextTags.Punctuation, "."),
            };

            return CompletionDescription.Create(taggedText.ToImmutableArray());
        }

        internal override async Task<CompletionChange> GetChangeAsync(Document document, CompletionItem item, TextSpan completionListSpan, char? commitKey, bool disallowAddingImports, CancellationToken cancellationToken)
        {
            var position = SymbolCompletionItem.GetContextPosition(item);
            Contract.ThrowIfFalse(item.Properties.TryGetValue(MinimalTypeNamePropertyName, out var typeName));

            var root = await document.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var (tokenAtPosition, potentialDotTokenLeftOfCursor) = FindTokensAtPosition(position, root);
            // syntax tree manipulations are to complicated if a mixture of conditionals is involved. Some text manipulation is easier here.
            //                      ↓               | cursor position
            //                   ↓                  | potentialDotTokenLeftOfCursor
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
