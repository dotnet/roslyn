// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
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
        protected abstract class VariableSymbol
        {
            /// <summary>
            /// Get the type of <see cref="GetSymbol"/> to use when generating code. May contain anonymous types in it.
            /// Note: this is not necessarily the type symbol that can be directly accessed off of <see
            /// cref="GetSymbol"/> itself.  For example, it may have had nullability information changes applied to it.
            /// </summary>
            public ITypeSymbol SymbolType { get; }

            private readonly bool _isCancellationToken;

            protected VariableSymbol(ITypeSymbol symbolType)
            {
                SymbolType = symbolType;
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

            public abstract int DisplayOrder { get; }
            public abstract bool CanBeCapturedByLocalFunction { get; }

            public abstract SyntaxAnnotation IdentifierTokenAnnotation { get; }
            public abstract SyntaxToken GetOriginalIdentifierToken(CancellationToken cancellationToken);

            public abstract void AddIdentifierTokenAnnotationPair(
                MultiDictionary<SyntaxToken, SyntaxAnnotation> annotations, CancellationToken cancellationToken);

            protected abstract int CompareTo(VariableSymbol right);

            public static int Compare(VariableSymbol left, VariableSymbol right)
            {
                // CancellationTokens always go at the end of method signature.
                var leftIsCancellationToken = left._isCancellationToken;
                var rightIsCancellationToken = right._isCancellationToken;

                if (leftIsCancellationToken && !rightIsCancellationToken)
                {
                    return 1;
                }
                else if (!leftIsCancellationToken && rightIsCancellationToken)
                {
                    return -1;
                }

                if (left.DisplayOrder == right.DisplayOrder)
                {
                    return left.CompareTo(right);
                }

                return left.DisplayOrder - right.DisplayOrder;
            }

            public string Name => this.GetSymbol().ToDisplayString(
                new SymbolDisplayFormat(
                    parameterOptions: SymbolDisplayParameterOptions.IncludeName,
                    miscellaneousOptions: SymbolDisplayMiscellaneousOptions.EscapeKeywordIdentifiers));
        }

        protected abstract class VariableSymbol<TSymbol>(TSymbol symbol, ITypeSymbol symbolType)
            : VariableSymbol(symbolType)
            where TSymbol : ISymbol
        {
            protected TSymbol Symbol { get; } = symbol;

            protected override ISymbol GetSymbol() => this.Symbol;
        }

        protected abstract class NotMovableVariableSymbol<TSymbol>(
            TSymbol symbol, ITypeSymbol symbolType) : VariableSymbol<TSymbol>(symbol, symbolType)
            where TSymbol : ISymbol
        {
            public override SyntaxToken GetOriginalIdentifierToken(CancellationToken cancellationToken)
                => default;

            public override SyntaxAnnotation IdentifierTokenAnnotation => throw ExceptionUtilities.Unreachable();

            public override void AddIdentifierTokenAnnotationPair(
                MultiDictionary<SyntaxToken, SyntaxAnnotation> annotations, CancellationToken cancellationToken)
            {
                // do nothing for parameter
            }
        }

        protected sealed class ParameterVariableSymbol(IParameterSymbol symbol, ITypeSymbol symbolType)
            : NotMovableVariableSymbol<IParameterSymbol>(symbol, symbolType), IComparable<ParameterVariableSymbol>
        {
            public override int DisplayOrder => 0;

            protected override int CompareTo(VariableSymbol right)
                => CompareTo((ParameterVariableSymbol)right);

            public int CompareTo(ParameterVariableSymbol other)
            {
                Contract.ThrowIfNull(other);

                if (this == other)
                {
                    return 0;
                }

                var compare = CompareMethodParameters((IMethodSymbol)this.Symbol.ContainingSymbol, (IMethodSymbol)other.Symbol.ContainingSymbol);
                if (compare != 0)
                {
                    return compare;
                }

                Contract.ThrowIfFalse(Symbol.Ordinal != other.Symbol.Ordinal);
                return (Symbol.Ordinal > other.Symbol.Ordinal) ? 1 : -1;
            }

            private static int CompareMethodParameters(IMethodSymbol left, IMethodSymbol right)
            {
                if (left == null && right == null)
                {
                    // not method parameters
                    return 0;
                }

                if (left.Equals(right))
                {
                    // parameter of same method
                    return 0;
                }

                // these methods can be either regular one, anonymous function, local function and etc
                // but all must belong to same outer regular method.
                // so, it should have location pointing to same tree
                var leftLocations = left.Locations;
                var rightLocations = right.Locations;

                var commonTree = leftLocations.Select(l => l.SourceTree).Intersect(rightLocations.Select(l => l.SourceTree)).WhereNotNull().First();

                var leftLocation = leftLocations.First(l => l.SourceTree == commonTree);
                var rightLocation = rightLocations.First(l => l.SourceTree == commonTree);

                return leftLocation.SourceSpan.Start - rightLocation.SourceSpan.Start;
            }

            public override bool CanBeCapturedByLocalFunction => true;
        }

        protected sealed class LocalVariableSymbol(ILocalSymbol localSymbol, ITypeSymbol symbolType)
            : VariableSymbol<ILocalSymbol>(localSymbol, symbolType), IComparable<LocalVariableSymbol>
        {
            private readonly SyntaxAnnotation _annotation = new();

            public override int DisplayOrder => 1;

            protected override int CompareTo(VariableSymbol right)
                => CompareTo((LocalVariableSymbol)right);

            public int CompareTo(LocalVariableSymbol other)
            {
                Contract.ThrowIfNull(other);

                if (this == other)
                {
                    return 0;
                }

                Contract.ThrowIfFalse(Symbol.Locations.Length == 1);
                Contract.ThrowIfFalse(other.Symbol.Locations.Length == 1);
                Contract.ThrowIfFalse(Symbol.Locations[0].IsInSource);
                Contract.ThrowIfFalse(other.Symbol.Locations[0].IsInSource);
                Contract.ThrowIfFalse(Symbol.Locations[0].SourceTree == other.Symbol.Locations[0].SourceTree);
                Contract.ThrowIfFalse(Symbol.Locations[0].SourceSpan.Start != other.Symbol.Locations[0].SourceSpan.Start);

                return Symbol.Locations[0].SourceSpan.Start - other.Symbol.Locations[0].SourceSpan.Start;
            }

            public override SyntaxToken GetOriginalIdentifierToken(CancellationToken cancellationToken)
            {
                Contract.ThrowIfFalse(Symbol.Locations.Length == 1);
                Contract.ThrowIfFalse(Symbol.Locations[0].IsInSource);
                Contract.ThrowIfNull(Symbol.Locations[0].SourceTree);

                var tree = Symbol.Locations[0].SourceTree;
                var span = Symbol.Locations[0].SourceSpan;

                var token = tree.GetRoot(cancellationToken).FindToken(span.Start);
                Contract.ThrowIfFalse(token.Span.Equals(span));

                return token;
            }

            public override SyntaxAnnotation IdentifierTokenAnnotation => _annotation;

            public override bool CanBeCapturedByLocalFunction => true;

            public override void AddIdentifierTokenAnnotationPair(
                MultiDictionary<SyntaxToken, SyntaxAnnotation> annotations, CancellationToken cancellationToken)
            {
                annotations.Add(GetOriginalIdentifierToken(cancellationToken), _annotation);
            }
        }

        protected sealed class QueryVariableSymbol(IRangeVariableSymbol symbol, ITypeSymbol symbolType)
            : NotMovableVariableSymbol<IRangeVariableSymbol>(symbol, symbolType), IComparable<QueryVariableSymbol>
        {
            public override int DisplayOrder => 2;

            protected override int CompareTo(VariableSymbol right)
                => CompareTo((QueryVariableSymbol)right);

            public int CompareTo(QueryVariableSymbol other)
            {
                Contract.ThrowIfNull(other);

                if (this == other)
                {
                    return 0;
                }

                var locationLeft = this.Symbol.Locations.First();
                var locationRight = other.Symbol.Locations.First();

                Contract.ThrowIfFalse(locationLeft.IsInSource);
                Contract.ThrowIfFalse(locationRight.IsInSource);
                Contract.ThrowIfFalse(locationLeft.SourceTree == locationRight.SourceTree);
                Contract.ThrowIfFalse(locationLeft.SourceSpan.Start != locationRight.SourceSpan.Start);

                return locationLeft.SourceSpan.Start - locationRight.SourceSpan.Start;
            }

            public override bool CanBeCapturedByLocalFunction => false;
        }
    }
}
