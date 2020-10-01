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

        protected override ImmutableArray<CompletionItem> GetCompletionItemsForTypeSymbol(ITypeSymbol container, bool isAccessedByConditionalAccess, SemanticModel semanticModel, int position)
        {
            var containerIsNullable = container.IsNullable();
            container = container.RemoveNullableIfPresent();
            var allMembers = container.GetMembers();
            var operators = from m in allMembers.OfType<IMethodSymbol>()
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
