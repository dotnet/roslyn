// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Symbols;

namespace Microsoft.CodeAnalysis.CSharp
{
    /// <summary>
    /// Rewrites method body lowered in context of an extension method to a body
    /// that corresponds to its implementation form.
    /// </summary>
    internal sealed class ExtensionMethodBodyRewriter : BoundTreeToDifferentEnclosingContextRewriter
    {
        /// <summary>
        /// Maps parameters and local functions from original enclosing context to corresponding rewritten symbols for rewritten context.
        /// </summary>
        private ImmutableDictionary<Symbol, Symbol> _symbolMap;

        private RewrittenMethodSymbol _rewrittenContainingMethod;

        /// <summary>
        /// To allow regular capture analysis we do not want to reuse locals with an incorrect containing symbol
        /// </summary>
        protected override bool EnforceAccurateContainerForLocals => true;

        public ExtensionMethodBodyRewriter(MethodSymbol sourceMethod, SourceExtensionImplementationMethodSymbol implementationMethod)
        {
            Debug.Assert(sourceMethod is not null);
            Debug.Assert(implementationMethod is not null);
            Debug.Assert(sourceMethod == (object)implementationMethod.UnderlyingMethod);

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

            // BoundMethodDefIndex in instrumentation will refer to the lambda method symbol, so we need to map it.
            _symbolMap = _symbolMap.Add(node.Symbol, rewritten);

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
                    return (MethodSymbol)_symbolMap[symbol];

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
            Debug.Assert(symbol?.IsExtensionBlockMember() != true);
            return base.VisitPropertySymbol(symbol);
        }

        public override BoundNode VisitUnaryOperator(BoundUnaryOperator node)
        {
            return ExtensionMethodReferenceRewriter.VisitUnaryOperator(this, node);
        }

        protected override BoundBinaryOperator.UncommonData? VisitBinaryOperatorData(BoundBinaryOperator node)
        {
            return ExtensionMethodReferenceRewriter.VisitBinaryOperatorData(this, node);
        }

        public override BoundNode? VisitMethodDefIndex(BoundMethodDefIndex node)
        {
            return ExtensionMethodReferenceRewriter.VisitMethodDefIndex(this, node);
        }
    }
}
