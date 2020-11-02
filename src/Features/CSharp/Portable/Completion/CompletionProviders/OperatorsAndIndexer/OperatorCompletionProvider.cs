// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Data.SqlTypes;
using System.Linq;
using System.Numerics;
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
            {
                return ImmutableArray<CompletionItem>.Empty;
            }
            // User-defined operator declaration constraints:
            // https://docs.microsoft.com/en-us/dotnet/csharp/language-reference/language-specification/classes#operators
            // * "An operator declaration must include both a public and a static modifier." -> No need to test for accessibility of members
            // * "Like other members, operators declared in a base class are inherited by derived classes." -> Search in container.GetBaseTypesAndThis()
            // Operator lifting and candidate selection:
            // https://docs.microsoft.com/en-us/dotnet/csharp/language-reference/language-specification/expressions#candidate-user-defined-operators
            var containerIsNullable = container.IsNullable();
            container = container.RemoveNullableIfPresent();
            var operators = from t in container.GetBaseTypesAndThis()
                            from m in t.GetMembers().OfType<IMethodSymbol>()
                            where m.IsUserDefinedOperator() && !IsExcludedOperator(m) && (containerIsNullable ? m.IsLiftable() : true)
                            select SymbolCompletionItem.CreateWithSymbolId(
                                displayText: m.GetOperatorSignOfOperator(),
                                filterText: "",
                                sortText: SortText($"{m.GetOperatorSortIndex():000}"),
                                symbols: ImmutableList.Create(m),
                                rules: CompletionItemRules.Default,
                                contextPosition: position);
            return operators.ToImmutableArray();
        }

        private static bool IsExcludedSymbol(ITypeSymbol container)
        {
            if (container.IsSpecialType() || // System.IntPtr is not considered a special type but nint is. We unify both:
                container.SpecialType is SpecialType.System_IntPtr or SpecialType.System_UIntPtr)
            {
                // Built-in types have built-in operators. These are not listed as `IMethodSymbols` with the following exceptions:
                // * string: == != are listed but + not 
                // * float/double: The 6 comparison operators are listed but not the arithmetical operators
                // * decimal: complete (all 15 operators)
                // * IntPtr/UIntPtr: complete (+ - == !=)
                return true;
            }

            return false;
        }

        private static bool IsExcludedOperator(IMethodSymbol m)
            => m.Name switch
            {
                WellKnownMemberNames.TrueOperatorName or
                WellKnownMemberNames.FalseOperatorName => true,
                _ => false,
            };

        internal override async Task<CompletionChange> GetChangeAsync(Document document, CompletionItem item, TextSpan completionListSpan, char? commitKey, bool disallowAddingImports, CancellationToken cancellationToken)
        {
            var symbols = await SymbolCompletionItem.GetSymbolsAsync(item, document, cancellationToken).ConfigureAwait(false);
            var symbol = symbols.Length == 1
                ? symbols[0] as IMethodSymbol
                : null;
            if (symbol is not null)
            {
                Contract.ThrowIfFalse(symbol.IsUserDefinedOperator());
                var operatorPosition = symbol.GetOperatorPosition();
                var operatorSign = symbol.GetOperatorSignOfOperator();
                if (operatorPosition.HasFlag(OperatorPosition.Infix))
                {
                    var change = await ReplaceDotAndTokenAfterWithTextAsync(document, item, text: $" {operatorSign} ", removeConditionalAccess: true, positionOffset: 0, cancellationToken).ConfigureAwait(false);
                    if (change is not null)
                    {
                        return change;
                    }
                }
                if (operatorPosition.HasFlag(OperatorPosition.Postfix))
                {
                    var change = await ReplaceDotAndTokenAfterWithTextAsync(document, item, text: $"{operatorSign} ", removeConditionalAccess: true, positionOffset: 0, cancellationToken).ConfigureAwait(false);
                    if (change is not null)
                    {
                        return change;
                    }
                }
                if (operatorPosition.HasFlag(OperatorPosition.Prefix))
                {
                    var position = SymbolCompletionItem.GetContextPosition(item);
                    var root = await document.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
                    var (_, potentialDotTokenLeftOfCursor) = FindTokensAtPosition(position, root);
                    var rootExpression = GetRootExpressionOfToken(potentialDotTokenLeftOfCursor);
                    if (rootExpression is not null)
                    {
                        var spanToReplace = TextSpan.FromBounds(rootExpression.Span.Start, rootExpression.Span.End);
                        var cursorPositionOffset = spanToReplace.End - position;
                        var fromRootToParent = rootExpression.ToString();
                        var prefixed = $"{operatorSign}{fromRootToParent}";
                        var newPosition = spanToReplace.Start + prefixed.Length - cursorPositionOffset;
                        return CompletionChange.Create(new TextChange(spanToReplace, prefixed), newPosition);
                    }
                }
            }

            return await base.GetChangeAsync(document, item, completionListSpan, commitKey, disallowAddingImports, cancellationToken).ConfigureAwait(false);
        }
    }
}
