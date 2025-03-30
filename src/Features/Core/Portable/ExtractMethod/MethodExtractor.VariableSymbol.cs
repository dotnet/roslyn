// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.ExtractMethod;

internal abstract partial class AbstractExtractMethodService<
    TStatementSyntax,
    TExecutableStatementSyntax,
    TExpressionSyntax>
{
    internal abstract partial class MethodExtractor
    {
        protected abstract class VariableSymbol : IComparable<VariableSymbol>
        {
            /// <summary>
            /// Get the type of <see cref="GetSymbol"/> to use when generating code. May contain anonymous types in it.
            /// Note: this is not necessarily the type symbol that can be directly accessed off of <see
            /// cref="GetSymbol"/> itself.  For example, it may have had nullability information changes applied to it.
            /// </summary>
            public ITypeSymbol SymbolType { get; }

            private readonly int _displayOrder;
            private readonly bool _isCancellationToken;

            protected VariableSymbol(ITypeSymbol symbolType, int displayOrder)
            {
                SymbolType = symbolType;
                _displayOrder = displayOrder;
                _isCancellationToken = IsCancellationToken(SymbolType);

                static bool IsCancellationToken(ITypeSymbol originalType)
                {
                    return originalType is
                    {
                        Name: nameof(CancellationToken),
                        ContainingNamespace.Name: nameof(System.Threading),
                        ContainingNamespace.ContainingNamespace.Name: nameof(System),
                        ContainingNamespace.ContainingNamespace.ContainingNamespace.IsGlobalNamespace: true,
                    };
                }
            }

            /// <summary>
            /// The underlying symbol this points at.
            /// </summary>
            protected abstract ISymbol GetSymbol();

            public abstract bool CanBeCapturedByLocalFunction { get; }

            public abstract SyntaxAnnotation IdentifierTokenAnnotation { get; }
            public abstract SyntaxToken GetOriginalIdentifierToken(CancellationToken cancellationToken);

            protected abstract int CompareToWorker(VariableSymbol other);

            public abstract void AddIdentifierTokenAnnotationPair(
                MultiDictionary<SyntaxToken, SyntaxAnnotation> annotations, CancellationToken cancellationToken);

            public int CompareTo(VariableSymbol? other)
            {
                Contract.ThrowIfNull(other);
                if (this == other)
                    return 0;

                // CancellationTokens always go at the end of method signature.
                return (this._isCancellationToken, other._isCancellationToken) switch
                {
                    (true, false) => 1,
                    (false, true) => -1,
                    // Then order by the general class of the variable (parameter, local, range-var).
                    _ when (this._displayOrder != other._displayOrder) => this._displayOrder - other._displayOrder,
                    // Finally, compare within the general class of the variable.
                    _ => this.CompareToWorker(other),
                };
            }

            public string Name => this.GetSymbol().ToDisplayString(
                new SymbolDisplayFormat(
                    parameterOptions: SymbolDisplayParameterOptions.IncludeName,
                    miscellaneousOptions: SymbolDisplayMiscellaneousOptions.EscapeKeywordIdentifiers));
        }

        protected abstract class VariableSymbol<TVariableSymbol, TSymbol>(
            TSymbol symbol, ITypeSymbol symbolType, int displayOrder)
            : VariableSymbol(symbolType, displayOrder)
            where TVariableSymbol : VariableSymbol<TVariableSymbol, TSymbol>
            where TSymbol : ISymbol
        {
            protected TSymbol Symbol { get; } = symbol;

            protected override ISymbol GetSymbol() => this.Symbol;

            protected sealed override int CompareToWorker(VariableSymbol right)
                => this.CompareTo((TVariableSymbol)right);

            protected abstract int CompareTo(TVariableSymbol right);

            protected static int DefaultCompareTo(ISymbol left, ISymbol right)
            {
                var locationLeft = left.Locations.First();
                var locationRight = right.Locations.First();

                Contract.ThrowIfFalse(locationLeft.IsInSource);
                Contract.ThrowIfFalse(locationRight.IsInSource);
                Contract.ThrowIfFalse(locationLeft.SourceTree == locationRight.SourceTree);

                return locationLeft.SourceSpan.Start - locationRight.SourceSpan.Start;
            }
        }

        protected abstract class NotMovableVariableSymbol<TVariableSymbol, TSymbol>(
            TSymbol symbol, ITypeSymbol symbolType, int displayOrder)
            : VariableSymbol<TVariableSymbol, TSymbol>(symbol, symbolType, displayOrder)
            where TVariableSymbol : VariableSymbol<TVariableSymbol, TSymbol>
            where TSymbol : ISymbol
        {
            public sealed override SyntaxToken GetOriginalIdentifierToken(CancellationToken cancellationToken)
                => default;

            public sealed override SyntaxAnnotation IdentifierTokenAnnotation
                => throw ExceptionUtilities.Unreachable();

            public sealed override void AddIdentifierTokenAnnotationPair(
                MultiDictionary<SyntaxToken, SyntaxAnnotation> annotations, CancellationToken cancellationToken)
            {
                // do nothing for parameter
            }
        }

        protected sealed class ParameterVariableSymbol(IParameterSymbol symbol, ITypeSymbol symbolType)
            : NotMovableVariableSymbol<ParameterVariableSymbol, IParameterSymbol>(
                symbol, symbolType, displayOrder: 0)
        {
            public override bool CanBeCapturedByLocalFunction => true;

            protected override int CompareTo(ParameterVariableSymbol other)
            {
                // these methods can be either regular one, anonymous function, local function and etc but all must
                // belong to same outer regular method. so, it should have location pointing to same tree
                var compare = DefaultCompareTo((IMethodSymbol)this.Symbol.ContainingSymbol, (IMethodSymbol)other.Symbol.ContainingSymbol);
                if (compare != 0)
                    return compare;

                Contract.ThrowIfFalse(Symbol.Ordinal != other.Symbol.Ordinal);
                return Symbol.Ordinal - other.Symbol.Ordinal;
            }
        }

        protected sealed class LocalVariableSymbol(ILocalSymbol localSymbol, ITypeSymbol symbolType)
            : VariableSymbol<LocalVariableSymbol, ILocalSymbol>(
                localSymbol, symbolType, displayOrder: 1)
        {
            private readonly SyntaxAnnotation _annotation = new();

            public override bool CanBeCapturedByLocalFunction => true;

            protected override int CompareTo(LocalVariableSymbol other)
                => DefaultCompareTo(this.Symbol, other.Symbol);

            public override SyntaxToken GetOriginalIdentifierToken(CancellationToken cancellationToken)
                => Symbol.Locations.First().FindToken(cancellationToken);

            public override SyntaxAnnotation IdentifierTokenAnnotation => _annotation;

            public override void AddIdentifierTokenAnnotationPair(
                MultiDictionary<SyntaxToken, SyntaxAnnotation> annotations, CancellationToken cancellationToken)
            {
                annotations.Add(GetOriginalIdentifierToken(cancellationToken), _annotation);
            }
        }

        protected sealed class QueryVariableSymbol(IRangeVariableSymbol symbol, ITypeSymbol symbolType)
            : NotMovableVariableSymbol<QueryVariableSymbol, IRangeVariableSymbol>(
                symbol, symbolType, displayOrder: 2)
        {
            public override bool CanBeCapturedByLocalFunction => false;

            protected override int CompareTo(QueryVariableSymbol other)
                => DefaultCompareTo(this.Symbol, other.Symbol);
        }
    }
}
