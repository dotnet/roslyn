// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.PooledObjects;
using Roslyn.Utilities;
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

        /// <summary>
        /// Maps parameters and local functions from original enclosing context to corresponding rewritten symbols for rewritten context.
        /// </summary>
        private ImmutableDictionary<Symbol, Symbol> _symbolMap;

        private RewrittenMethodSymbol _rewrittenContainingMethod;

        public InstanceExtensionMethodBodyRewriter(MethodSymbol sourceMethod, SourceExtensionMetadataMethodSymbol metadataMethod)
        {
            Debug.Assert(sourceMethod is not null);
            Debug.Assert(metadataMethod is not null);
            Debug.Assert(sourceMethod.ContainingType.TryGetCorrespondingStaticMetadataExtensionMember(sourceMethod) == (object)metadataMethod);

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

        private (RewrittenMethodSymbol, ImmutableDictionary<Symbol, Symbol>) EnterMethod(MethodSymbol symbol, RewrittenLambdaOrLocalFunctionSymbol rewritten)
        {
            return EnterMethod(symbol, rewritten, rewritten.Parameters.AsSpan());
        }

        protected override MethodSymbol CurrentMethod => _rewrittenContainingMethod;

        protected override TypeMap TypeMap => _rewrittenContainingMethod.TypeMap;

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

        [return: NotNullIfNotNull("symbol")]
        protected override MethodSymbol? VisitMethodSymbol(MethodSymbol? symbol)
        {
            switch (symbol?.MethodKind)
            {
                case MethodKind.LambdaMethod:
                    throw ExceptionUtilities.Unreachable();

                case MethodKind.LocalFunction:
                    return (MethodSymbol)_symbolMap[symbol];

                default:
                    return base.VisitMethodSymbol(symbol);
            }
        }

        // PROTOTYPE(roles): Here we are pretty much duplicating what InstanceExtensionMethodReferenceRewriter would do.
        //                   We need to reevaluate whether we are getting enough advantage from this duplication.  
        public override BoundNode VisitCall(BoundCall node)
        {
            Debug.Assert(node != null);

            BoundExpression rewrittenCall;

            if (LocalRewriter.TryGetReceiver(node, out BoundCall? receiver1))
            {
                // Handle long call chain of both instance and extension method invocations.
                var calls = ArrayBuilder<BoundCall>.GetInstance();

                calls.Push(node);
                node = receiver1;

                while (LocalRewriter.TryGetReceiver(node, out BoundCall? receiver2))
                {
                    calls.Push(node);
                    node = receiver2;
                }

                // Rewrite the receiver
                BoundExpression? rewrittenReceiver = (BoundExpression?)this.Visit(node.ReceiverOpt);

                do
                {
                    rewrittenCall = visitArgumentsAndFinishRewrite(node, rewrittenReceiver);
                    rewrittenReceiver = rewrittenCall;
                }
                while (calls.TryPop(out node!));

                calls.Free();
            }
            else
            {
                // Rewrite the receiver
                BoundExpression? rewrittenReceiver = (BoundExpression?)this.Visit(node.ReceiverOpt);
                rewrittenCall = visitArgumentsAndFinishRewrite(node, rewrittenReceiver);
            }

            return rewrittenCall;

            BoundExpression visitArgumentsAndFinishRewrite(BoundCall node, BoundExpression? rewrittenReceiver)
            {
                return InstanceExtensionMethodReferenceRewriter.UpdateCall(
                    _rewrittenContainingMethod,
                    node,
                    this.VisitMethodSymbol(node.Method),
                    this.VisitSymbols<MethodSymbol>(node.OriginalMethodsOpt),
                    rewrittenReceiver,
                    this.VisitList(node.Arguments),
                    node.ArgumentRefKindsOpt,
                    node.InvokedAsExtensionMethod,
                    this.VisitType(node.Type));
            }
        }

        // PROTOTYPE(roles): Here we are pretty much duplicating what InstanceExtensionMethodReferenceRewriter would do.
        //                   We need to reevaluate whether we are getting enough advantage from this duplication.  
        public override BoundNode? VisitDelegateCreationExpression(BoundDelegateCreationExpression node)
        {
            return InstanceExtensionMethodReferenceRewriter.UpdateDelegateCreation(node, this.VisitMethodSymbol(node.MethodOpt), (BoundExpression)this.Visit(node.Argument), node.IsExtensionMethod, this.VisitType(node.Type));
        }

        // PROTOTYPE(roles): Handle deep recursion on long chain of binary operators
    }
}
