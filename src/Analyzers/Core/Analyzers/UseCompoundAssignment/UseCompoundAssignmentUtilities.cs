// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.LanguageService;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.UseCompoundAssignment
{
    internal static class UseCompoundAssignmentUtilities
    {
        internal const string Increment = nameof(Increment);
        internal const string Decrement = nameof(Decrement);

        public static void GenerateMaps<TSyntaxKind>(
            ImmutableArray<(TSyntaxKind exprKind, TSyntaxKind assignmentKind, TSyntaxKind tokenKind)> kinds,
            out ImmutableDictionary<TSyntaxKind, TSyntaxKind> binaryToAssignmentMap,
            out ImmutableDictionary<TSyntaxKind, TSyntaxKind> assignmentToTokenMap) where TSyntaxKind : struct
        {
            var binaryToAssignmentBuilder = ImmutableDictionary.CreateBuilder<TSyntaxKind, TSyntaxKind>();
            var assignmentToTokenBuilder = ImmutableDictionary.CreateBuilder<TSyntaxKind, TSyntaxKind>();

            foreach (var (exprKind, assignmentKind, tokenKind) in kinds)
            {
                binaryToAssignmentBuilder[exprKind] = assignmentKind;
                assignmentToTokenBuilder[assignmentKind] = tokenKind;
            }

            binaryToAssignmentMap = binaryToAssignmentBuilder.ToImmutable();
            assignmentToTokenMap = assignmentToTokenBuilder.ToImmutable();

            Debug.Assert(binaryToAssignmentMap.Count == assignmentToTokenMap.Count);
            Debug.Assert(binaryToAssignmentMap.Values.All(assignmentToTokenMap.ContainsKey));
        }

        public static bool IsSideEffectFree(
            ISyntaxFacts syntaxFacts, SyntaxNode expr, SemanticModel semanticModel, CancellationToken cancellationToken)
        {
            return IsSideEffectFreeRecurse(syntaxFacts, expr, semanticModel, isTopLevel: true, cancellationToken);
        }

        private static bool IsSideEffectFreeRecurse(
            ISyntaxFacts syntaxFacts, SyntaxNode expr, SemanticModel semanticModel,
            bool isTopLevel, CancellationToken cancellationToken)
        {
            if (expr == null)
            {
                return false;
            }

            // it basically has to be of the form "a.b.c", where all components are locals,
            // parameters or fields.  Basically, nothing that can cause arbitrary user code
            // execution when being evaluated by the compiler.

            if (syntaxFacts.IsThisExpression(expr) ||
                syntaxFacts.IsBaseExpression(expr))
            {
                // Referencing this/base like  this.a.b.c causes no side effects itself.
                return true;
            }

            if (syntaxFacts.IsIdentifierName(expr))
            {
                return IsSideEffectFreeSymbol(expr, semanticModel, isTopLevel, cancellationToken);
            }

            if (syntaxFacts.IsParenthesizedExpression(expr))
            {
                syntaxFacts.GetPartsOfParenthesizedExpression(expr,
                    out _, out var expression, out _);

                return IsSideEffectFreeRecurse(syntaxFacts, expression, semanticModel, isTopLevel, cancellationToken);
            }

            if (syntaxFacts.IsSimpleMemberAccessExpression(expr))
            {
                syntaxFacts.GetPartsOfMemberAccessExpression(expr,
                    out var subExpr, out _);
                return IsSideEffectFreeRecurse(syntaxFacts, subExpr, semanticModel, isTopLevel: false, cancellationToken) &&
                       IsSideEffectFreeSymbol(expr, semanticModel, isTopLevel, cancellationToken);
            }

            if (syntaxFacts.IsConditionalAccessExpression(expr))
            {
                syntaxFacts.GetPartsOfConditionalAccessExpression(expr,
                    out var expression, out var whenNotNull);
                return IsSideEffectFreeRecurse(syntaxFacts, expression, semanticModel, isTopLevel: false, cancellationToken) &&
                       IsSideEffectFreeRecurse(syntaxFacts, whenNotNull, semanticModel, isTopLevel: false, cancellationToken);
            }

            // Something we don't explicitly handle.  Assume this may have side effects.
            return false;
        }

        private static bool IsSideEffectFreeSymbol(
            SyntaxNode expr, SemanticModel semanticModel, bool isTopLevel, CancellationToken cancellationToken)
        {
            var symbolInfo = semanticModel.GetSymbolInfo(expr, cancellationToken);
            if (symbolInfo.CandidateSymbols.Length > 0 ||
                symbolInfo.Symbol == null)
            {
                // couldn't bind successfully, assume that this might have side-effects.
                return false;
            }

            var symbol = symbolInfo.Symbol;
            switch (symbol.Kind)
            {
                case SymbolKind.Namespace:
                case SymbolKind.NamedType:
                case SymbolKind.Field:
                case SymbolKind.Parameter:
                case SymbolKind.Local:
                    return true;
            }

            if (symbol.Kind == SymbolKind.Property && isTopLevel)
            {
                // If we have `this.Prop = this.Prop * 2`, then that's just a single read/write of
                // the prop and we can safely make that `this.Prop *= 2` (since it will still be a
                // single read/write).  However, if we had `this.prop.x = this.prop.x * 2`, then
                // that's multiple reads of `this.prop`, and it's not safe to convert that to
                // `this.prop.x *= 2` in the case where calling 'prop' may have side effects.
                //
                // Note, this doesn't apply if the property is a ref-property.  In that case, we'd
                // go from a read and a write to to just a read (and a write to it's returned ref
                // value).
                var property = (IPropertySymbol)symbol;
                if (property.RefKind == RefKind.None)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
