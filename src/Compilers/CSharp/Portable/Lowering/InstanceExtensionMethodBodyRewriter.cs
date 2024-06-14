// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using ReferenceEqualityComparer = Roslyn.Utilities.ReferenceEqualityComparer;

namespace Microsoft.CodeAnalysis.CSharp
{
    /// <summary>
    /// Rewrites method body lowered in context of an instance extension method to a body
    /// that corresponds to its static/metadata form.
    /// </summary>
    internal sealed class InstanceExtensionMethodBodyRewriter : BoundTreeToDifferentEnclosingContextRewriter
    {
        private readonly SourceExtensionMetadataMethodSymbol _metadataMethod;
        private ImmutableDictionary<Symbol, Symbol> _symbolMap;
        private RewrittenMethodSymbol _rewrittenContainingMethod;

        public InstanceExtensionMethodBodyRewriter(MethodSymbol sourceMethod, SourceExtensionMetadataMethodSymbol metadataMethod)
        {
            _metadataMethod = metadataMethod;
            _symbolMap = ImmutableDictionary<Symbol, Symbol>.Empty.WithComparers(ReferenceEqualityComparer.Instance, ReferenceEqualityComparer.Instance);
            EnterMethod(sourceMethod, metadataMethod, metadataMethod.Parameters.AsSpan()[1..]);
            Debug.Assert(_rewrittenContainingMethod is not null);
        }

        private (RewrittenMethodSymbol, ImmutableDictionary<Symbol, Symbol>) EnterMethod(MethodSymbol symbol, RewrittenMethodSymbol rewritten, ReadOnlySpan<ParameterSymbol> rewrittenParameters)
        {
            ImmutableDictionary<Symbol, Symbol> saveSymbolMap = _symbolMap;
            RewrittenMethodSymbol savedContainer = _rewrittenContainingMethod;

            Debug.Assert(symbol.Parameters.Length == rewrittenParameters.Length);

            if (!rewrittenParameters.IsEmpty)
            {
                var builder = _symbolMap.ToBuilder();
                foreach (var parameter in symbol.Parameters)
                {
                    builder.Add(parameter, rewrittenParameters[parameter.Ordinal]);
                }
                _symbolMap = builder.ToImmutable();
            }

            _rewrittenContainingMethod = rewritten;

            return (savedContainer, saveSymbolMap);
        }

        private (RewrittenMethodSymbol, ImmutableDictionary<Symbol, Symbol>) EnterMethod(MethodSymbol symbol, RewrittenMethodSymbol rewritten)
        {
            return EnterMethod(symbol, rewritten, rewritten.Parameters.AsSpan());
        }

        protected override MethodSymbol CurrentMethod => _rewrittenContainingMethod;

        protected override TypeMap TypeMap => _rewrittenContainingMethod.TypeMap;

        // PROTOTYPE(roles): Should we adjust type of the node in the LocalRewriter, from extension type to extended type?
        //                   When we are leaving the type as is in the LocalRewriter, we are producing somewhat inconsistent
        //                   BoundCall nodes. The type of the first argument for the static/metadata call doesn't match corresponding
        //                   parameter's type until we perform a fix up here. 
        //                   If we were to adjust the type in the LocalRewriter, we would be be creating inconsistent BoundThisReference nodes.
        //                   The type of the node wouldn't match the containing type.
        public override BoundNode? VisitThisReference(BoundThisReference node)
        {
            return new BoundParameter(node.Syntax, _metadataMethod.Parameters[0]);
        }

        protected override ParameterSymbol VisitParameterSymbol(ParameterSymbol symbol)
        {
            return (ParameterSymbol)_symbolMap[symbol];
        }

        public override BoundNode? VisitLambda(BoundLambda node)
        {
            var rewritten = new RewrittenLambdaOrLocalFunctionSymbol(node.Symbol, _rewrittenContainingMethod);

            var savedState = EnterMethod(node.Symbol, rewritten);
            BoundBlock body = (BoundBlock)this.Visit(node.Body);
            (_rewrittenContainingMethod, _symbolMap) = savedState;

            TypeSymbol? type = this.VisitType(node.Type);
            return node.Update(node.UnboundLambda, rewritten, body, node.Diagnostics, node.Binder, type);
        }

        public override BoundNode? VisitLocalFunctionStatement(BoundLocalFunctionStatement node)
        {
            MethodSymbol symbol = this.VisitMethodSymbol(node.Symbol);
            var savedState = EnterMethod(node.Symbol, (RewrittenMethodSymbol)symbol);

            BoundBlock? blockBody = (BoundBlock?)this.Visit(node.BlockBody);
            BoundBlock? expressionBody = (BoundBlock?)this.Visit(node.ExpressionBody);

            (_rewrittenContainingMethod, _symbolMap) = savedState;

            return node.Update(symbol, blockBody, expressionBody);
        }

        public override BoundNode VisitBlock(BoundBlock node)
        {
            ImmutableDictionary<Symbol, Symbol> saveSymbolMap = _symbolMap;

            if (!node.LocalFunctions.IsEmpty)
            {
                var builder = _symbolMap.ToBuilder();

                foreach (var localFunction in node.LocalFunctions)
                {
                    builder.Add(localFunction, new RewrittenLambdaOrLocalFunctionSymbol(localFunction, _rewrittenContainingMethod));
                }

                _symbolMap = builder.ToImmutable();
            }

            var result = base.VisitBlock(node);

            _symbolMap = saveSymbolMap;
            return result;
        }

        protected override ImmutableArray<MethodSymbol> VisitDeclaredLocalFunctions(ImmutableArray<MethodSymbol> localFunctions)
        {
            return localFunctions.SelectAsArray(static (l, map) => (MethodSymbol)map[l], _symbolMap);
        }

        [return: NotNullIfNotNull("symbol")]
        protected override MethodSymbol? VisitMethodSymbol(MethodSymbol? symbol)
        {
            if (symbol?.MethodKind is MethodKind.LambdaMethod or MethodKind.LocalFunction)
            {
                return (MethodSymbol)_symbolMap[symbol];
            }

            return base.VisitMethodSymbol(symbol);
        }
    }
}
