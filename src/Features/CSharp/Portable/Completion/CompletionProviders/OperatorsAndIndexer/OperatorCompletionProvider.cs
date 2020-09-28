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

        protected override IEnumerable<CompletionItem> GetCompletionItemsForTypeSymbol(ITypeSymbol container, SemanticModel semanticModel, int position)
        {
            if (!IsExcludedFromOperators(semanticModel, container))
            {
                var allMembers = container.GetMembers();
                var operators = from m in allMembers.OfType<IMethodSymbol>()
                                where m.IsUserDefinedOperator() && !IsExcludedOperator(m)
                                let sign = m.GetOperatorSignOfOperator()
                                select SymbolCompletionItem.CreateWithSymbolId(
                                    displayText: sign,
                                    filterText: "",
                                    sortText: SortText($"{sign}{m.Name}"),
                                    symbols: ImmutableList.Create(m),
                                    rules: CompletionItemRules.Default,
                                    contextPosition: position);
                return operators;
            }

            return SpecializedCollections.EmptyEnumerable<CompletionItem>();
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

        private static bool IsExcludedFromOperators(SemanticModel semanticModel, ITypeSymbol container)
        {
            if (container.IsSpecialType())
            {
                return true;
            }
            var unbound = container is INamedTypeSymbol named && named.IsGenericType
                ? named.ConstructedFrom
                : container;
            return ExcludedTypes().Any(t => EqualityComparer<ITypeSymbol>.Default.Equals(unbound, t));

            IEnumerable<ITypeSymbol?> ExcludedTypes()
            {
                // System
                yield return GetTypeByMetadataName(typeof(DateTime));
                yield return GetTypeByMetadataName(typeof(TimeSpan));
                yield return GetTypeByMetadataName(typeof(DateTimeOffset));
                yield return GetTypeByMetadataName(typeof(decimal));
                yield return GetTypeByMetadataName(typeof(IntPtr));
                yield return GetTypeByMetadataName(typeof(UIntPtr));
                yield return GetTypeByMetadataName(typeof(Guid));
                yield return GetTypeByMetadataName(typeof(Span<>));
                // System.Numeric
                yield return GetTypeByMetadataName(typeof(BigInteger));
                yield return GetTypeByMetadataName(typeof(Complex));
                yield return GetTypeByMetadataName(typeof(Matrix3x2));
                yield return GetTypeByMetadataName(typeof(Matrix4x4));
                yield return GetTypeByMetadataName(typeof(Plane));
                yield return GetTypeByMetadataName(typeof(Quaternion));
                yield return GetTypeByMetadataName(typeof(Vector<>));
                yield return GetTypeByMetadataName(typeof(Vector2));
                yield return GetTypeByMetadataName(typeof(Vector3));
                yield return GetTypeByMetadataName(typeof(Vector4));
                // System.Data.SqlTypes
                yield return GetTypeByMetadataName(typeof(SqlBinary));
                yield return GetTypeByMetadataName(typeof(SqlBoolean));
                yield return GetTypeByMetadataName(typeof(SqlByte));
                yield return GetTypeByMetadataName(typeof(SqlDateTime));
                yield return GetTypeByMetadataName(typeof(SqlDecimal));
                yield return GetTypeByMetadataName(typeof(SqlDouble));
                yield return GetTypeByMetadataName(typeof(SqlGuid));
                yield return GetTypeByMetadataName(typeof(SqlInt16));
                yield return GetTypeByMetadataName(typeof(SqlInt32));
                yield return GetTypeByMetadataName(typeof(SqlInt64));
                yield return GetTypeByMetadataName(typeof(SqlMoney));
                yield return GetTypeByMetadataName(typeof(SqlSingle));
                yield return GetTypeByMetadataName(typeof(SqlString));

                INamedTypeSymbol? GetTypeByMetadataName(Type type)
                {
                    var typeName = type.FullName;
                    if (typeName is not null)
                    {
                        return semanticModel.Compilation.GetTypeByMetadataName(typeName);
                    }

                    return null;
                }
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
                    var change = await ReplaceDotAndTokenAfterWithTextAsync(document, item, $" {operatorSign} ", 0, cancellationToken).ConfigureAwait(false);
                    if (change is not null)
                    {
                        return change;
                    }
                }
                if (operatorPosition.HasFlag(OperatorPosition.Postfix))
                {
                    var change = await ReplaceDotAndTokenAfterWithTextAsync(document, item, $"{operatorSign} ", 0, cancellationToken).ConfigureAwait(false);
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
