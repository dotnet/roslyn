// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.Completion.Providers;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Microsoft.CodeAnalysis.CSharp.Completion.Providers
{
    [ExportCompletionProvider(nameof(OperatorIndexerCompletionProvider), LanguageNames.CSharp), Shared]
    [ExtensionOrder(After = nameof(SymbolCompletionProvider))]
    internal class OperatorIndexerCompletionProvider : LSPCompletionProvider
    {
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public OperatorIndexerCompletionProvider()
        {
        }

        private const string MinimalTypeNamePropertyName = "MinimalTypeName";
        private const string CompletionHandlerPropertyName = "CompletionHandler";
        private const string CompletionHandlerConversion = "Conversion";
        private const string CompletionHandlerOperator = "Operator";
        private const string CompletionHandlerIndexer = "Indexer";

        // CompletionItems for indexers/operators should be sorted below other suggestions like methods or properties of the type.
        // Identifier (of methods or properties) can start with "A Unicode character of classes Lu, Ll, Lt, Lm, Lo, or Nl". https://docs.microsoft.com/en-us/dotnet/csharp/language-reference/language-specification/lexical-structure#identifiers
        // Sorting is done via StringComparer.OrdinalIgnoreCase which compares the utf-16 bytes after converting to uppercase.
        // \ufffd http://www.fileformat.info/info/unicode/char/fffd/index.htm is the largest possible value for utf-16
        // and is also greater than surrogate pairs, if byte comparison is used. The "biggest" possible characters are 
        // \u3134a http://www.fileformat.info/info/unicode/char/3134a/index.htm surrogate pair "\ud884\udf4a" and
        // \uffdc http://www.fileformat.info/info/unicode/char/ffdc/index.htm (non-surrogate)
        private const string SortingPrefix = "\uFFFD";

        internal override ImmutableHashSet<char> TriggerCharacters => ImmutableHashSet.Create('.');

        internal override bool IsInsertionTrigger(SourceText text, int insertedCharacterPosition, OptionSet options)
            => text[insertedCharacterPosition] == '.';

        protected override Task<CompletionDescription> GetDescriptionWorkerAsync(Document document, CompletionItem item, CancellationToken cancellationToken)
            => SymbolCompletionItem.GetDescriptionAsync(item, document, cancellationToken);

        private static ImmutableDictionary<string, string> CreateCompletionHandlerProperty(string operation, params (string key, string value)[] additionalProperties)
        {
            var builder = ImmutableDictionary.CreateBuilder<string, string>();
            builder.Add(CompletionHandlerPropertyName, operation);
            foreach (var (key, value) in additionalProperties)
            {
                builder.Add(key, value);
            }

            return builder.ToImmutable();
        }

        public override async Task ProvideCompletionsAsync(CompletionContext context)
        {
            var cancellationToken = context.CancellationToken;
            var document = context.Document;
            var position = context.Position;
            var syntaxTree = await document.GetRequiredSyntaxTreeAsync(cancellationToken).ConfigureAwait(false);
            var token = syntaxTree.FindTokenOnLeftOfPosition(position, cancellationToken);
            token = token.GetPreviousTokenIfTouchingWord(position);
            if (!token.IsKind(SyntaxKind.DotToken))
            {
                return;
            }

            var expression = GetParentExpressionOfToken(token);
            if (expression is null)
            {
                return;
            }

            var semanticModel = await document.ReuseExistingSpeculativeModelAsync(expression.SpanStart, cancellationToken).ConfigureAwait(false);
            var container = semanticModel.GetTypeInfo(expression, cancellationToken).Type;
            if (container is null)
            {
                return;
            }

            var allMembers = container.GetMembers();
            var allExplicitConversions = from m in allMembers.OfType<IMethodSymbol>()
                                         where
                                             m.IsConversion() && // MethodKind.Conversion
                                             m.Name == WellKnownMemberNames.ExplicitConversionName && // op_Explicit
                                             m.Parameters.Length == 1 && // Malformed conversion operator may have more or less than one parameter
                                             container.Equals(m.Parameters[0].Type) // Convert from container type to other type
                                         let typeName = m.ReturnType.ToMinimalDisplayString(semanticModel, position)
                                         select SymbolCompletionItem.CreateWithSymbolId(
                                             displayText: $"({typeName})", // The type to convert to
                                             filterText: typeName,
                                             sortText: $"{SortingPrefix}{typeName}",
                                             symbols: ImmutableList.Create(m),
                                             rules: CompletionItemRules.Default,
                                             contextPosition: position,
                                             properties: CreateCompletionHandlerProperty(CompletionHandlerConversion, (MinimalTypeNamePropertyName, typeName)));
            context.AddItems(allExplicitConversions);

            var indexers = allMembers.OfType<IPropertySymbol>().Where(p => p.IsIndexer).ToImmutableList();
            if (!indexers.IsEmpty)
            {
                var indexerCompletion = SymbolCompletionItem.CreateWithSymbolId(
                    displayText: "this[]",
                    filterText: "this",
                    sortText: $"{SortingPrefix}this",
                    symbols: indexers,
                    rules: CompletionItemRules.Default,
                    contextPosition: position,
                    properties: CreateCompletionHandlerProperty(CompletionHandlerIndexer));
                context.AddItem(indexerCompletion);
            }

            var operators = from m in allMembers.OfType<IMethodSymbol>()
                            where m.IsUserDefinedOperator() && !IsExcludedOperator(m)
                            let sign = m.GetOperatorSignOfOperator()
                            select SymbolCompletionItem.CreateWithSymbolId(
                                displayText: sign,
                                filterText: "",
                                sortText: $"{SortingPrefix}{sign}",
                                symbols: ImmutableList.Create(m),
                                rules: CompletionItemRules.Default,
                                contextPosition: position,
                                properties: CreateCompletionHandlerProperty(CompletionHandlerOperator));
            context.AddItems(operators);
        }

        private static bool IsExcludedOperator(IMethodSymbol m)
        {
            switch (m.Name)
            {
                case WellKnownMemberNames.FalseOperatorName:
                case WellKnownMemberNames.TrueOperatorName:
                    return true;
                default:
                    return false;
            }
        }

        /// <summary>
        /// Returns the expression left to the passed dot <paramref name="token"/>.
        /// </summary>
        /// <example>
        /// Expression: a.b?.ccc.
        /// Token     :  ↑  ↑   ↑
        /// Returns   :  a  a.b .ccc
        /// </example>
        /// <param name="token">A dot token.</param>
        /// <returns>The expression left to the dot token or null.</returns>
        private static ExpressionSyntax? GetParentExpressionOfToken(SyntaxToken token)
        {
            var syntaxNode = token.Parent;
            return syntaxNode switch
            {
                MemberAccessExpressionSyntax memberAccess => memberAccess.Expression,
                MemberBindingExpressionSyntax memberBinding => memberBinding.GetParentConditionalAccessExpression()?.Expression,
                _ => null,
            };
        }

        /// <summary>
        /// Returns the expression left to the passed dot <paramref name="token"/>.
        /// </summary>
        /// <example>
        /// Given the expression a.b?.c.d. returns a.b?.c.d. for all dot tokens
        /// </example>
        /// <param name="token">A dot token.</param>
        /// <returns>The root expression associated with the dot or null.</returns>
        private static ExpressionSyntax? GetRootExpressionOfToken(SyntaxToken token)
        {
            var syntaxNode = token.Parent;
            return syntaxNode switch
            {
                MemberAccessExpressionSyntax memberAccess => memberAccess.Expression.GetRootConditionalAccessExpression() ?? (ExpressionSyntax)memberAccess,
                MemberBindingExpressionSyntax memberBinding => memberBinding.GetRootConditionalAccessExpression(),
                _ => null,
            };
        }

        private static SyntaxNodeOrToken? FindNodeOrTokenToRemoveAtCursorPosition(SyntaxToken tokenAtCursor)
        {
            return tokenAtCursor switch
            {
                { Parent: IdentifierNameSyntax identifierName } => identifierName,
                var token when token.IsKeyword() => token,
                _ => null,
            };
        }

        internal override async Task<CompletionChange> GetChangeAsync(Document document, CompletionItem item, TextSpan completionListSpan, char? commitKey, bool disallowAddingImports, CancellationToken cancellationToken)
        {
            if (item.Properties.TryGetValue(CompletionHandlerPropertyName, out var value))
            {
                var completionChange = value switch
                {
                    CompletionHandlerConversion => await HandleConversionChangeAsync(document, item, cancellationToken).ConfigureAwait(false),
                    CompletionHandlerIndexer => await HandleIndexerChangeAsync(document, item, cancellationToken).ConfigureAwait(false),
                    _ => throw ExceptionUtilities.UnexpectedValue(value),
                };
                if (completionChange is not null)
                {
                    return completionChange;
                }
            }

            return await base.GetChangeAsync(document, item, completionListSpan, commitKey, disallowAddingImports, cancellationToken).ConfigureAwait(false);
        }

        private static async Task<CompletionChange?> HandleConversionChangeAsync(Document document, CompletionItem item, CancellationToken cancellationToken)
        {
            var position = SymbolCompletionItem.GetContextPosition(item);
            Contract.ThrowIfFalse(item.Properties.TryGetValue(MinimalTypeNamePropertyName, out var typeName));

            var root = await document.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var tokenAtPosition = root.FindTokenOnLeftOfPosition(position, includeSkipped: true);
            var normalizedToken = tokenAtPosition.GetPreviousTokenIfTouchingWord(position);
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
            var rootExpression = GetRootExpressionOfToken(normalizedToken);
            var parentExpression = GetParentExpressionOfToken(normalizedToken);
            var nodeOrTokenToRemove = FindNodeOrTokenToRemoveAtCursorPosition(tokenAtPosition);
            if (rootExpression is null || parentExpression is null)
            {
                // ProvideCompletionsAsync only adds CompletionItems, if GetParentExpressionOfToken returns an expression.
                // if GetParentExpressionOfToken returns an Expression, then should GetRootExpressionOfToken return an Expression too.
                throw ExceptionUtilities.Unreachable;
            }

            var spanToReplace = TextSpan.FromBounds(rootExpression.Span.Start, nodeOrTokenToRemove.HasValue ? nodeOrTokenToRemove.Value.Span.End : rootExpression.Span.End);
            var cursorPositionOffset = spanToReplace.End - position;
            var fromRootToParent = rootExpression.ToString();
            if (nodeOrTokenToRemove is SyntaxNodeOrToken nodeOrToken)
            {
                // Cut off the identifier
                var length = nodeOrToken.Span.Start - rootExpression.SpanStart;
                fromRootToParent = fromRootToParent.Substring(0, length);
                // place cursor right behind ).
                cursorPositionOffset = 0;
            }
            var fromRootToParentWithInsertedClosingBracket = fromRootToParent.Insert(parentExpression.Span.End - rootExpression.SpanStart, ")");
            var conversion = $"(({typeName}){fromRootToParentWithInsertedClosingBracket}";
            var newPosition = spanToReplace.Start + conversion.Length - cursorPositionOffset;
            return CompletionChange.Create(new TextChange(spanToReplace, conversion), newPosition);
        }

        private static async Task<CompletionChange?> HandleIndexerChangeAsync(Document document, CompletionItem item, CancellationToken cancellationToken)
        {
            var root = await document.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var position = SymbolCompletionItem.GetContextPosition(item);
            var tokenAtPosition = root.FindTokenOnLeftOfPosition(position);
            var token = tokenAtPosition.GetPreviousTokenIfTouchingWord(position);
            if (token.IsKind(SyntaxKind.DotToken))
            {
                var newPosition = token.Span.End;
                var replaceSpan = TextSpan.FromBounds(token.SpanStart, tokenAtPosition.Span.End);
                return CompletionChange.Create(new TextChange(replaceSpan, "[]"), newPosition);
            }

            return null;
        }
    }

    internal static class OperatorSymbolExtensions
    {
        internal static string GetOperatorSignOfOperator(this IMethodSymbol m)
        {
            return m.Name switch
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

                _ => throw ExceptionUtilities.UnexpectedValue(m.Name),
            };
        }

        [Flags]
        internal enum OperatorPosition
        {
            None = 0,
            Perfix = 1,
            Infix = 2,
            Postfix = 4,
        }

        internal static OperatorPosition GetOperatorPosition(IMethodSymbol m)
        {
            switch (m.Name)
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
                    return OperatorPosition.Infix | OperatorPosition.Postfix;
                case WellKnownMemberNames.FalseOperatorName:
                case WellKnownMemberNames.TrueOperatorName:
                    return OperatorPosition.None;
                case WellKnownMemberNames.LogicalNotOperatorName:
                case WellKnownMemberNames.OnesComplementOperatorName:
                case WellKnownMemberNames.UnaryNegationOperatorName:
                case WellKnownMemberNames.UnaryPlusOperatorName:
                    return OperatorPosition.Perfix;
                default:
                    throw ExceptionUtilities.UnexpectedValue(m.Name);
            }
        }
    }
}
