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
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Completion.Providers
{
    [ExportCompletionProvider(nameof(OperatorCompletionProvider), LanguageNames.CSharp), Shared]
    [ExtensionOrder(After = nameof(IndexerCompletionProvider))]
    internal class OperatorCompletionProvider : OperatorIndexerCompletionProviderBase
    {
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public OperatorCompletionProvider()
        {
        }

        protected override int SortingGroupIndex => 3;

        protected override ImmutableArray<CompletionItem> GetCompletionItemsForTypeSymbol(SemanticModel semanticModel,
            ITypeSymbol container,
            int position,
            bool isAccessedByConditionalAccess,
            CancellationToken cancellationToken)
        {
            if (IsExcludedSymbol(container))
                return ImmutableArray<CompletionItem>.Empty;

            using var _ = ArrayBuilder<CompletionItem>.GetInstance(out var result);

            var containerIsNullable = container.IsNullable();
            container = container.RemoveNullableIfPresent();

            foreach (var type in container.GetBaseTypesAndThis())
            {
                foreach (var method in type.GetMembers().OfType<IMethodSymbol>())
                {
                    if (!method.IsUserDefinedOperator())
                        continue;

                    if (method.Name is WellKnownMemberNames.TrueOperatorName or WellKnownMemberNames.FalseOperatorName)
                        continue;

                    if (containerIsNullable && !IsLiftable(method))
                        continue;

                    result.Add(SymbolCompletionItem.CreateWithSymbolId(
                        displayText: GetDisplayText(method),
                        displayTextSuffix: null,
                        inlineDescription: GetInlineDescription(method),
                        filterText: "",
                        sortText: SortText($"{GetOperatorSortIndex(method):000}"),
                        symbols: ImmutableList.Create(method),
                        rules: CompletionItemRules.Default,
                        contextPosition: position));
                }
            }

            return result.ToImmutable();
        }

        private static bool IsExcludedSymbol(ITypeSymbol container)
        {
            return container.IsSpecialType() ||
                container.SpecialType == SpecialType.System_IntPtr ||
                container.SpecialType == SpecialType.System_UIntPtr;
        }

        internal override async Task<CompletionChange> GetChangeAsync(
            Document document,
            CompletionItem item,
            TextSpan completionListSpan,
            char? commitKey,
            bool disallowAddingImports,
            CancellationToken cancellationToken)
        {
            var symbols = await SymbolCompletionItem.GetSymbolsAsync(item, document, cancellationToken).ConfigureAwait(false);
            if (symbols.FirstOrDefault() is IMethodSymbol symbol && symbol.IsUserDefinedOperator())
            {
                var operatorPosition = GetOperatorPosition(symbol);
                var operatorSign = GetDisplayText(symbol);

                if (operatorPosition.HasFlag(OperatorPosition.Infix))
                    return await ReplaceDotAndTokenAfterWithTextAsync(document, item, text: $" {operatorSign} ", removeConditionalAccess: true, positionOffset: 0, cancellationToken).ConfigureAwait(false);

                if (operatorPosition.HasFlag(OperatorPosition.Postfix))
                    return await ReplaceDotAndTokenAfterWithTextAsync(document, item, text: $"{operatorSign} ", removeConditionalAccess: true, positionOffset: 0, cancellationToken).ConfigureAwait(false);

                if (operatorPosition.HasFlag(OperatorPosition.Prefix))
                {
                    var position = SymbolCompletionItem.GetContextPosition(item);
                    var root = await document.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
                    var (_, potentialDotTokenLeftOfCursor) = FindTokensAtPosition(position, root);

                    var rootExpression = GetRootExpressionOfToken(potentialDotTokenLeftOfCursor);
                    // base.ProvideCompletionsAsync checks GetParentExpressionOfToken is not null.
                    // If GetRootExpressionOfToken returns something, so does GetParentExpressionOfToken.
                    Contract.ThrowIfNull(rootExpression);

                    var spanToReplace = TextSpan.FromBounds(rootExpression.Span.Start, rootExpression.Span.End);
                    var cursorPositionOffset = spanToReplace.End - position;
                    var fromRootToParent = rootExpression.ToString();
                    var prefixed = $"{operatorSign}{fromRootToParent}";
                    var newPosition = spanToReplace.Start + prefixed.Length - cursorPositionOffset;
                    return CompletionChange.Create(new TextChange(spanToReplace, prefixed), newPosition);
                }
            }

            return await base.GetChangeAsync(document, item, commitKey, cancellationToken).ConfigureAwait(false);
        }

        private static string GetDisplayText(IMethodSymbol method)
        {
            return method.Name switch
            {
                // binary
                WellKnownMemberNames.AdditionOperatorName => "+",
                WellKnownMemberNames.BitwiseAndOperatorName => "&",
                WellKnownMemberNames.BitwiseOrOperatorName => "|",
                WellKnownMemberNames.DivisionOperatorName => "/",
                WellKnownMemberNames.EqualityOperatorName => "==",
                WellKnownMemberNames.ExclusiveOrOperatorName => "^",
                WellKnownMemberNames.GreaterThanOperatorName => ">",
                WellKnownMemberNames.GreaterThanOrEqualOperatorName => ">=",
                WellKnownMemberNames.InequalityOperatorName => "!=",
                WellKnownMemberNames.LeftShiftOperatorName => "<<",
                WellKnownMemberNames.LessThanOperatorName => "<",
                WellKnownMemberNames.LessThanOrEqualOperatorName => "<=",
                WellKnownMemberNames.ModulusOperatorName => "%",
                WellKnownMemberNames.MultiplyOperatorName => "*",
                WellKnownMemberNames.RightShiftOperatorName => ">>",
                WellKnownMemberNames.SubtractionOperatorName => "-",

                // Unary
                WellKnownMemberNames.DecrementOperatorName => "--",
                WellKnownMemberNames.FalseOperatorName => "false",
                WellKnownMemberNames.IncrementOperatorName => "++",
                WellKnownMemberNames.LogicalNotOperatorName => "!",
                WellKnownMemberNames.OnesComplementOperatorName => "~",
                WellKnownMemberNames.TrueOperatorName => "true",
                WellKnownMemberNames.UnaryNegationOperatorName => "-",
                WellKnownMemberNames.UnaryPlusOperatorName => "+",

                var name => throw ExceptionUtilities.UnexpectedValue(name),
            };
        }

        private static int GetOperatorSortIndex(IMethodSymbol method)
        {
            return method.Name switch
            {
                // comparison and negation
                WellKnownMemberNames.EqualityOperatorName => 0,             // ==
                WellKnownMemberNames.InequalityOperatorName => 1,           // !=
                WellKnownMemberNames.GreaterThanOperatorName => 2,          // >
                WellKnownMemberNames.GreaterThanOrEqualOperatorName => 3,   // >=
                WellKnownMemberNames.LessThanOperatorName => 4,             // <
                WellKnownMemberNames.LessThanOrEqualOperatorName => 5,      // <=
                WellKnownMemberNames.LogicalNotOperatorName => 6,           // !
                // mathematical
                WellKnownMemberNames.AdditionOperatorName => 7,             // +
                WellKnownMemberNames.SubtractionOperatorName => 8,          // -
                WellKnownMemberNames.MultiplyOperatorName => 9,             // *
                WellKnownMemberNames.DivisionOperatorName => 10,            // /
                WellKnownMemberNames.ModulusOperatorName => 11,             // %
                WellKnownMemberNames.IncrementOperatorName => 12,           // ++
                WellKnownMemberNames.DecrementOperatorName => 13,           // --
                WellKnownMemberNames.UnaryPlusOperatorName => 14,           // +
                WellKnownMemberNames.UnaryNegationOperatorName => 15,       // -
                // bit operations
                WellKnownMemberNames.BitwiseAndOperatorName => 16,          // &
                WellKnownMemberNames.BitwiseOrOperatorName => 17,           // |
                WellKnownMemberNames.ExclusiveOrOperatorName => 18,         // ^
                WellKnownMemberNames.LeftShiftOperatorName => 19,           // <<
                WellKnownMemberNames.RightShiftOperatorName => 20,          // >>
                WellKnownMemberNames.OnesComplementOperatorName => 21,      // ~
                // true false
                WellKnownMemberNames.FalseOperatorName => 22,               // false
                WellKnownMemberNames.TrueOperatorName => 23,                // true

                var name => throw ExceptionUtilities.UnexpectedValue(name),
            };
        }

        private static string GetInlineDescription(IMethodSymbol method)
        {
            return method.Name switch
            {
                // binary
                WellKnownMemberNames.AdditionOperatorName => "a + b",
                WellKnownMemberNames.BitwiseAndOperatorName => "a & b",
                WellKnownMemberNames.BitwiseOrOperatorName => "a | b",
                WellKnownMemberNames.DivisionOperatorName => "a / b",
                WellKnownMemberNames.EqualityOperatorName => "a == b",
                WellKnownMemberNames.ExclusiveOrOperatorName => "a ^ b",
                WellKnownMemberNames.GreaterThanOperatorName => "a > b",
                WellKnownMemberNames.GreaterThanOrEqualOperatorName => "a >= b",
                WellKnownMemberNames.InequalityOperatorName => "a != b",
                WellKnownMemberNames.LeftShiftOperatorName => "a << b",
                WellKnownMemberNames.LessThanOperatorName => "a < b",
                WellKnownMemberNames.LessThanOrEqualOperatorName => "a <= b",
                WellKnownMemberNames.ModulusOperatorName => "a % b",
                WellKnownMemberNames.MultiplyOperatorName => "a * b",
                WellKnownMemberNames.RightShiftOperatorName => "a >> b",
                WellKnownMemberNames.SubtractionOperatorName => "a - b",

                // Unary
                WellKnownMemberNames.DecrementOperatorName => "a--",
                WellKnownMemberNames.FalseOperatorName => "false",
                WellKnownMemberNames.IncrementOperatorName => "a++",
                WellKnownMemberNames.LogicalNotOperatorName => "!a",
                WellKnownMemberNames.OnesComplementOperatorName => "~a",
                WellKnownMemberNames.TrueOperatorName => "true",
                WellKnownMemberNames.UnaryNegationOperatorName => "-a",
                WellKnownMemberNames.UnaryPlusOperatorName => "+a",

                var name => throw ExceptionUtilities.UnexpectedValue(name),
            };
        }

        private static OperatorPosition GetOperatorPosition(IMethodSymbol method)
        {
            switch (method.Name)
            {
                // binary
                case WellKnownMemberNames.AdditionOperatorName:
                case WellKnownMemberNames.BitwiseAndOperatorName:
                case WellKnownMemberNames.BitwiseOrOperatorName:
                case WellKnownMemberNames.DivisionOperatorName:
                case WellKnownMemberNames.EqualityOperatorName:
                case WellKnownMemberNames.ExclusiveOrOperatorName:
                case WellKnownMemberNames.GreaterThanOperatorName:
                case WellKnownMemberNames.GreaterThanOrEqualOperatorName:
                case WellKnownMemberNames.InequalityOperatorName:
                case WellKnownMemberNames.LeftShiftOperatorName:
                case WellKnownMemberNames.LessThanOperatorName:
                case WellKnownMemberNames.LessThanOrEqualOperatorName:
                case WellKnownMemberNames.ModulusOperatorName:
                case WellKnownMemberNames.MultiplyOperatorName:
                case WellKnownMemberNames.RightShiftOperatorName:
                case WellKnownMemberNames.SubtractionOperatorName:
                    return OperatorPosition.Infix;
                // Unary
                case WellKnownMemberNames.DecrementOperatorName:
                case WellKnownMemberNames.IncrementOperatorName:
                    return OperatorPosition.Prefix | OperatorPosition.Postfix;
                case WellKnownMemberNames.FalseOperatorName:
                case WellKnownMemberNames.TrueOperatorName:
                    return OperatorPosition.None;
                case WellKnownMemberNames.LogicalNotOperatorName:
                case WellKnownMemberNames.OnesComplementOperatorName:
                case WellKnownMemberNames.UnaryNegationOperatorName:
                case WellKnownMemberNames.UnaryPlusOperatorName:
                    return OperatorPosition.Prefix;
                default:
                    throw ExceptionUtilities.UnexpectedValue(method.Name);
            }
        }

        private static bool IsLiftable(IMethodSymbol symbol)
        {
            // https://docs.microsoft.com/en-us/dotnet/csharp/language-reference/language-specification/expressions#lifted-operators

            // Common for all:
            if (symbol.IsUserDefinedOperator() && symbol.Parameters.All(p => p.Type.IsValueType))
            {
                switch (symbol.Name)
                {
                    // Unary
                    case WellKnownMemberNames.UnaryPlusOperatorName:
                    case WellKnownMemberNames.IncrementOperatorName:
                    case WellKnownMemberNames.UnaryNegationOperatorName:
                    case WellKnownMemberNames.DecrementOperatorName:
                    case WellKnownMemberNames.LogicalNotOperatorName:
                    case WellKnownMemberNames.OnesComplementOperatorName:
                        return symbol.Parameters.Length == 1 && symbol.ReturnType.IsValueType;
                    // Binary 
                    case WellKnownMemberNames.AdditionOperatorName:
                    case WellKnownMemberNames.SubtractionOperatorName:
                    case WellKnownMemberNames.MultiplyOperatorName:
                    case WellKnownMemberNames.DivisionOperatorName:
                    case WellKnownMemberNames.ModulusOperatorName:
                    case WellKnownMemberNames.BitwiseAndOperatorName:
                    case WellKnownMemberNames.BitwiseOrOperatorName:
                    case WellKnownMemberNames.ExclusiveOrOperatorName:
                    case WellKnownMemberNames.LeftShiftOperatorName:
                    case WellKnownMemberNames.RightShiftOperatorName:
                        return symbol.Parameters.Length == 2 && symbol.ReturnType.IsValueType;
                    // Equality + Relational 
                    case WellKnownMemberNames.EqualityOperatorName:
                    case WellKnownMemberNames.InequalityOperatorName:

                    case WellKnownMemberNames.LessThanOperatorName:
                    case WellKnownMemberNames.GreaterThanOperatorName:
                    case WellKnownMemberNames.LessThanOrEqualOperatorName:
                    case WellKnownMemberNames.GreaterThanOrEqualOperatorName:
                        return symbol.Parameters.Length == 2 && symbol.ReturnType.SpecialType == SpecialType.System_Boolean;
                }
            }

            return false;
        }
    }
}
