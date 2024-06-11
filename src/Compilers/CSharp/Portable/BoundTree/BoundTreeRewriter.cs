// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.PooledObjects;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal abstract partial class BoundTreeRewriter : BoundTreeVisitor
    {
        [return: NotNullIfNotNull(nameof(type))]
        public virtual TypeSymbol? VisitType(TypeSymbol? type)
        {
            return type;
        }

        public ImmutableArray<T> VisitList<T>(ImmutableArray<T> list) where T : BoundNode
        {
            if (list.IsDefault)
            {
                return list;
            }

            return DoVisitList(list);
        }

        private ImmutableArray<T> DoVisitList<T>(ImmutableArray<T> list) where T : BoundNode
        {
            ArrayBuilder<T>? newList = null;
            for (int i = 0; i < list.Length; i++)
            {
                var item = list[i];
                System.Diagnostics.Debug.Assert(item != null);

                var visited = this.Visit(item);
                if (newList == null && item != visited)
                {
                    newList = ArrayBuilder<T>.GetInstance();
                    if (i > 0)
                    {
                        newList.AddRange(list, i);
                    }
                }

                if (newList != null && visited != null)
                {
                    newList.Add((T)visited);
                }
            }

            if (newList != null)
            {
                return newList.ToImmutableAndFree();
            }

            return list;
        }

        [return: NotNullIfNotNull(nameof(symbol))]
        protected virtual AliasSymbol? VisitAliasSymbol(AliasSymbol? symbol) => symbol;

        protected virtual DiscardSymbol VisitDiscardSymbol(DiscardSymbol symbol)
        {
            Debug.Assert(symbol is not null);
            return symbol;
        }

        protected virtual EventSymbol VisitEventSymbol(EventSymbol symbol)
        {
            Debug.Assert(symbol is not null);
            return symbol;
        }

        [return: NotNullIfNotNull(nameof(symbol))]
        protected virtual LabelSymbol? VisitLabelSymbol(LabelSymbol? symbol) => symbol;

        protected virtual LocalSymbol VisitLocalSymbol(LocalSymbol symbol)
        {
            Debug.Assert(symbol is not null);
            return symbol;
        }

        protected virtual NamespaceSymbol VisitNamespaceSymbol(NamespaceSymbol symbol)
        {
            Debug.Assert(symbol is not null);
            return symbol;
        }

        [return: NotNullIfNotNull(nameof(symbol))]
        protected virtual RangeVariableSymbol? VisitRangeVariableSymbol(RangeVariableSymbol? symbol) => symbol;

        [return: NotNullIfNotNull(nameof(symbol))]
        protected virtual FieldSymbol? VisitFieldSymbol(FieldSymbol? symbol) => symbol;

        protected virtual ParameterSymbol VisitParameterSymbol(ParameterSymbol symbol)
        {
            Debug.Assert(symbol is not null);
            return symbol;
        }

        [return: NotNullIfNotNull(nameof(symbol))]
        protected virtual PropertySymbol? VisitPropertySymbol(PropertySymbol? symbol) => symbol;

        [return: NotNullIfNotNull(nameof(symbol))]
        protected virtual MethodSymbol? VisitMethodSymbol(MethodSymbol? symbol) => symbol;

        [return: NotNullIfNotNull(nameof(symbol))]
        protected Symbol? VisitSymbol(Symbol? symbol)
        {
            if (symbol is null)
            {
                return null;
            }

            switch (symbol.Kind)
            {
                case SymbolKind.Alias:
                    return VisitAliasSymbol((AliasSymbol)symbol);
                case SymbolKind.Discard:
                    return VisitDiscardSymbol((DiscardSymbol)symbol);
                case SymbolKind.Event:
                    return VisitEventSymbol((EventSymbol)symbol);
                case SymbolKind.Label:
                    return VisitLabelSymbol((LabelSymbol)symbol);
                case SymbolKind.Local:
                    return VisitLocalSymbol((LocalSymbol)symbol);
                case SymbolKind.Namespace:
                    return VisitNamespaceSymbol((NamespaceSymbol)symbol);
                case SymbolKind.RangeVariable:
                    return VisitRangeVariableSymbol((RangeVariableSymbol)symbol);
                case SymbolKind.Field:
                    return VisitFieldSymbol((FieldSymbol)symbol);
                case SymbolKind.Parameter:
                    return VisitParameterSymbol((ParameterSymbol)symbol);
                case SymbolKind.Property:
                    return VisitPropertySymbol((PropertySymbol)symbol);
                case SymbolKind.Method:
                    return VisitMethodSymbol((MethodSymbol)symbol);

                default:
                    if (symbol is TypeSymbol type)
                    {
                        return VisitType(type);
                    }

                    throw ExceptionUtilities.UnexpectedValue(symbol.Kind);
            }
        }

        [return: NotNullIfNotNull(nameof(symbol))]
        protected FunctionTypeSymbol? VisitFunctionTypeSymbol(FunctionTypeSymbol? symbol)
        {
            return (FunctionTypeSymbol?)VisitType(symbol);
        }

        protected ImmutableArray<T> VisitSymbols<T>(ImmutableArray<T> symbols) where T : Symbol?
        {
            if (symbols.IsDefault)
            {
                return symbols;
            }

            ArrayBuilder<T>? builder = null;

            for (int i = 0; i < symbols.Length; i++)
            {
                T symbol = symbols[i];

                var newSymbol = (T?)VisitSymbol(symbol);
                if (newSymbol != (object?)symbol)
                {
                    Debug.Assert(newSymbol is not null);

                    if (builder is null)
                    {
                        builder = ArrayBuilder<T>.GetInstance(symbols.Length);
                        builder.AddRange(symbols, i);
                    }

                    builder.Add(newSymbol);
                }
                else if (builder is not null)
                {
                    builder.Add(symbol);
                }
            }

            return builder is null ? symbols : builder.ToImmutableAndFree();
        }

        protected virtual ImmutableArray<LocalSymbol> VisitLocals(ImmutableArray<LocalSymbol> locals) => locals;

        protected virtual ImmutableArray<MethodSymbol> VisitDeclaredLocalFunctions(ImmutableArray<MethodSymbol> localFunctions) => localFunctions;
    }

    internal abstract class BoundTreeRewriterWithStackGuard : BoundTreeRewriter
    {
        private int _recursionDepth;

        protected BoundTreeRewriterWithStackGuard()
        { }

        protected BoundTreeRewriterWithStackGuard(int recursionDepth)
        {
            _recursionDepth = recursionDepth;
        }

        protected int RecursionDepth => _recursionDepth;

        [return: NotNullIfNotNull(nameof(node))]
        public override BoundNode? Visit(BoundNode? node)
        {
            var expression = node as BoundExpression;
            if (expression != null)
            {
                return VisitExpressionWithStackGuard(ref _recursionDepth, expression);
            }

            return base.Visit(node);
        }

        protected BoundExpression VisitExpressionWithStackGuard(BoundExpression node)
        {
            return VisitExpressionWithStackGuard(ref _recursionDepth, node);
        }

        protected sealed override BoundExpression VisitExpressionWithoutStackGuard(BoundExpression node)
        {
            return (BoundExpression)base.Visit(node);
        }
    }

    internal abstract class BoundTreeRewriterWithStackGuardWithoutRecursionOnTheLeftOfBinaryOperator : BoundTreeRewriterWithStackGuard
    {
        protected BoundTreeRewriterWithStackGuardWithoutRecursionOnTheLeftOfBinaryOperator()
        { }

        protected BoundTreeRewriterWithStackGuardWithoutRecursionOnTheLeftOfBinaryOperator(int recursionDepth)
            : base(recursionDepth)
        { }

        public sealed override BoundNode? VisitBinaryOperator(BoundBinaryOperator node)
        {
            BoundExpression child = node.Left;

            if (child.Kind != BoundKind.BinaryOperator)
            {
                return base.VisitBinaryOperator(node);
            }

            var stack = ArrayBuilder<BoundBinaryOperator>.GetInstance();
            stack.Push(node);

            BoundBinaryOperator binary = (BoundBinaryOperator)child;

            while (true)
            {
                stack.Push(binary);
                child = binary.Left;

                if (child.Kind != BoundKind.BinaryOperator)
                {
                    break;
                }

                binary = (BoundBinaryOperator)child;
            }

            var left = (BoundExpression?)this.Visit(child);
            Debug.Assert(left is { });

            do
            {
                binary = stack.Pop();
                var right = (BoundExpression?)this.Visit(binary.Right);
                Debug.Assert(right is { });
                var type = this.VisitType(binary.Type);
                left = binary.Update(binary.OperatorKind, binary.Data, binary.ResultKind, left, right, type);
            }
            while (stack.Count > 0);

            Debug.Assert((object)binary == node);
            stack.Free();

            return left;
        }
    }
}
