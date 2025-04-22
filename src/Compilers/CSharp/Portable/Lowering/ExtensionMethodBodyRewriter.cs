﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Roslyn.Utilities;
using ReferenceEqualityComparer = Roslyn.Utilities.ReferenceEqualityComparer;

namespace Microsoft.CodeAnalysis.CSharp
{
    /// <summary>
    /// Rewrites method body lowered in context of an extension method to a body
    /// that corresponds to its implementation form.
    /// </summary>
    internal sealed class ExtensionMethodBodyRewriter : BoundTreeToDifferentEnclosingContextRewriter
    {
        private readonly SourceExtensionImplementationMethodSymbol _implementationMethod;

        /// <summary>
        /// Maps parameters and local functions from original enclosing context to corresponding rewritten symbols for rewritten context.
        /// </summary>
        private ImmutableDictionary<Symbol, Symbol> _symbolMap;

        private RewrittenMethodSymbol _rewrittenContainingMethod;

        public ExtensionMethodBodyRewriter(MethodSymbol sourceMethod, SourceExtensionImplementationMethodSymbol implementationMethod)
        {
            Debug.Assert(sourceMethod is not null);
            Debug.Assert(implementationMethod is not null);
            Debug.Assert(sourceMethod == (object)implementationMethod.UnderlyingMethod);

            _implementationMethod = implementationMethod;
            _symbolMap = ImmutableDictionary<Symbol, Symbol>.Empty.WithComparers(ReferenceEqualityComparer.Instance, ReferenceEqualityComparer.Instance);

            bool haveExtraParameter = sourceMethod.ParameterCount != implementationMethod.ParameterCount;
            if (haveExtraParameter)
            {
                Debug.Assert(implementationMethod.ParameterCount - 1 == sourceMethod.ParameterCount);
                var receiverParameter = (WrappedParameterSymbol)implementationMethod.Parameters[0];
                _symbolMap = _symbolMap.Add(receiverParameter.UnderlyingParameter, receiverParameter);
            }
            EnterMethod(sourceMethod, implementationMethod, implementationMethod.Parameters.AsSpan()[(haveExtraParameter ? 1 : 0)..]);
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

        private (RewrittenMethodSymbol, ImmutableDictionary<Symbol, Symbol>) EnterMethod(MethodSymbol symbol, RewrittenLambdaOrLocalFunctionSymbol rewritten)
        {
            return EnterMethod(symbol, rewritten, rewritten.Parameters.AsSpan());
        }

        protected override MethodSymbol CurrentMethod => _rewrittenContainingMethod;

        protected override TypeMap TypeMap => _rewrittenContainingMethod.TypeMap;

        public override BoundNode? VisitThisReference(BoundThisReference node)
        {
            throw ExceptionUtilities.Unreachable();
        }

        public override ParameterSymbol VisitParameterSymbol(ParameterSymbol symbol)
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
            var savedState = EnterMethod(node.Symbol, (RewrittenLambdaOrLocalFunctionSymbol)symbol);

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

        [return: NotNullIfNotNull(nameof(symbol))]
        public override MethodSymbol? VisitMethodSymbol(MethodSymbol? symbol)
        {
            switch (symbol?.MethodKind)
            {
                case MethodKind.LambdaMethod:
                    throw ExceptionUtilities.Unreachable();

                case MethodKind.LocalFunction:
                    if (symbol.IsDefinition)
                    {
                        return (MethodSymbol)_symbolMap[symbol];
                    }

                    return ((MethodSymbol)_symbolMap[symbol.OriginalDefinition]).ConstructIfGeneric(TypeMap.SubstituteTypes(symbol.TypeArgumentsWithAnnotations));

                default:
                    return base.VisitMethodSymbol(symbol);
            }
        }

        [return: NotNullIfNotNull(nameof(symbol))]
        public override FieldSymbol? VisitFieldSymbol(FieldSymbol? symbol)
        {
            if (symbol is null)
            {
                return null;
            }

            return symbol.OriginalDefinition
                .AsMember((NamedTypeSymbol)TypeMap.SubstituteType(symbol.ContainingType).AsTypeSymbolOnly());
        }

        public override BoundNode? VisitCall(BoundCall node)
        {
            return ExtensionMethodReferenceRewriter.VisitCall(this, node);
        }

        public override BoundNode? VisitDelegateCreationExpression(BoundDelegateCreationExpression node)
        {
            return ExtensionMethodReferenceRewriter.VisitDelegateCreationExpression(this, node);
        }

        public override BoundNode VisitFunctionPointerLoad(BoundFunctionPointerLoad node)
        {
            return ExtensionMethodReferenceRewriter.VisitFunctionPointerLoad(this, node);
        }

        [return: NotNullIfNotNull(nameof(symbol))]
        public override PropertySymbol? VisitPropertySymbol(PropertySymbol? symbol)
        {
            Debug.Assert(symbol?.GetIsNewExtensionMember() != true);
            return base.VisitPropertySymbol(symbol);
        }
    }
}
