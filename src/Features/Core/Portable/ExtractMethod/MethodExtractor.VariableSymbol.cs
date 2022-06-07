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

namespace Microsoft.CodeAnalysis.ExtractMethod
{
    internal abstract partial class MethodExtractor
    {
        /// <summary>
        /// temporary symbol until we have a symbol that can hold onto both local and parameter symbol
        /// </summary>
        protected abstract class VariableSymbol
        {
            protected VariableSymbol(Compilation compilation, ITypeSymbol type)
            {
                OriginalTypeHadAnonymousTypeOrDelegate = type.ContainsAnonymousType();
                OriginalType = OriginalTypeHadAnonymousTypeOrDelegate ? type.RemoveAnonymousTypes(compilation) : type;
            }

            public abstract int DisplayOrder { get; }
            public abstract string Name { get; }
            public abstract bool CanBeCapturedByLocalFunction { get; }

            public abstract bool GetUseSaferDeclarationBehavior(CancellationToken cancellationToken);
            public abstract SyntaxAnnotation IdentifierTokenAnnotation { get; }
            public abstract SyntaxToken GetOriginalIdentifierToken(CancellationToken cancellationToken);

            public abstract void AddIdentifierTokenAnnotationPair(
                List<Tuple<SyntaxToken, SyntaxAnnotation>> annotations, CancellationToken cancellationToken);

            protected abstract int CompareTo(VariableSymbol right);

            /// <summary>
            /// return true if original type had anonymous type or delegate somewhere in the type
            /// </summary>
            public bool OriginalTypeHadAnonymousTypeOrDelegate { get; }

            /// <summary>
            /// get the original type with anonymous type removed
            /// </summary>
            public ITypeSymbol OriginalType { get; }

            public static int Compare(
                VariableSymbol left,
                VariableSymbol right,
                INamedTypeSymbol cancellationTokenType)
            {
                // CancellationTokens always go at the end of method signature.
                var leftIsCancellationToken = left.OriginalType.Equals(cancellationTokenType);
                var rightIsCancellationToken = right.OriginalType.Equals(cancellationTokenType);

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
        }

        protected abstract class NotMovableVariableSymbol : VariableSymbol
        {
            public NotMovableVariableSymbol(Compilation compilation, ITypeSymbol type)
                : base(compilation, type)
            {
            }

            public override bool GetUseSaferDeclarationBehavior(CancellationToken cancellationToken)
            {
                // decl never get moved
                return false;
            }

            public override SyntaxToken GetOriginalIdentifierToken(CancellationToken cancellationToken)
                => throw ExceptionUtilities.Unreachable;

            public override SyntaxAnnotation IdentifierTokenAnnotation => throw ExceptionUtilities.Unreachable;

            public override void AddIdentifierTokenAnnotationPair(
                List<Tuple<SyntaxToken, SyntaxAnnotation>> annotations, CancellationToken cancellationToken)
            {
                // do nothing for parameter
            }
        }

        protected class ParameterVariableSymbol : NotMovableVariableSymbol, IComparable<ParameterVariableSymbol>
        {
            private readonly IParameterSymbol _parameterSymbol;

            public ParameterVariableSymbol(Compilation compilation, IParameterSymbol parameterSymbol, ITypeSymbol type)
                : base(compilation, type)
            {
                Contract.ThrowIfNull(parameterSymbol);
                _parameterSymbol = parameterSymbol;
            }

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

                var compare = CompareMethodParameters((IMethodSymbol)_parameterSymbol.ContainingSymbol, (IMethodSymbol)other._parameterSymbol.ContainingSymbol);
                if (compare != 0)
                {
                    return compare;
                }

                Contract.ThrowIfFalse(_parameterSymbol.Ordinal != other._parameterSymbol.Ordinal);
                return (_parameterSymbol.Ordinal > other._parameterSymbol.Ordinal) ? 1 : -1;
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

            public override string Name
            {
                get
                {
                    return _parameterSymbol.ToDisplayString(
                        new SymbolDisplayFormat(
                            parameterOptions: SymbolDisplayParameterOptions.IncludeName,
                            miscellaneousOptions: SymbolDisplayMiscellaneousOptions.EscapeKeywordIdentifiers));
                }
            }

            public override bool CanBeCapturedByLocalFunction => true;
        }

        protected class LocalVariableSymbol<T> : VariableSymbol, IComparable<LocalVariableSymbol<T>> where T : SyntaxNode
        {
            private readonly SyntaxAnnotation _annotation = new();
            private readonly ILocalSymbol _localSymbol;
            private readonly HashSet<int> _nonNoisySet;

            public LocalVariableSymbol(Compilation compilation, ILocalSymbol localSymbol, ITypeSymbol type, HashSet<int> nonNoisySet)
                : base(compilation, type)
            {
                Contract.ThrowIfNull(localSymbol);
                Contract.ThrowIfNull(nonNoisySet);

                _localSymbol = localSymbol;
                _nonNoisySet = nonNoisySet;
            }

            public override int DisplayOrder => 1;

            protected override int CompareTo(VariableSymbol right)
                => CompareTo((LocalVariableSymbol<T>)right);

            public int CompareTo(LocalVariableSymbol<T> other)
            {
                Contract.ThrowIfNull(other);

                if (this == other)
                {
                    return 0;
                }

                Contract.ThrowIfFalse(_localSymbol.Locations.Length == 1);
                Contract.ThrowIfFalse(other._localSymbol.Locations.Length == 1);
                Contract.ThrowIfFalse(_localSymbol.Locations[0].IsInSource);
                Contract.ThrowIfFalse(other._localSymbol.Locations[0].IsInSource);
                Contract.ThrowIfFalse(_localSymbol.Locations[0].SourceTree == other._localSymbol.Locations[0].SourceTree);
                Contract.ThrowIfFalse(_localSymbol.Locations[0].SourceSpan.Start != other._localSymbol.Locations[0].SourceSpan.Start);

                return _localSymbol.Locations[0].SourceSpan.Start - other._localSymbol.Locations[0].SourceSpan.Start;
            }

            public override string Name
            {
                get
                {
                    return _localSymbol.ToDisplayString(
                        new SymbolDisplayFormat(
                            miscellaneousOptions: SymbolDisplayMiscellaneousOptions.EscapeKeywordIdentifiers));
                }
            }

            public override SyntaxToken GetOriginalIdentifierToken(CancellationToken cancellationToken)
            {
                Contract.ThrowIfFalse(_localSymbol.Locations.Length == 1);
                Contract.ThrowIfFalse(_localSymbol.Locations[0].IsInSource);
                Contract.ThrowIfNull(_localSymbol.Locations[0].SourceTree);

                var tree = _localSymbol.Locations[0].SourceTree;
                var span = _localSymbol.Locations[0].SourceSpan;

                var token = tree.GetRoot(cancellationToken).FindToken(span.Start);
                Contract.ThrowIfFalse(token.Span.Equals(span));

                return token;
            }

            public override SyntaxAnnotation IdentifierTokenAnnotation => _annotation;

            public override bool CanBeCapturedByLocalFunction => true;

            public override void AddIdentifierTokenAnnotationPair(
                List<Tuple<SyntaxToken, SyntaxAnnotation>> annotations, CancellationToken cancellationToken)
            {
                annotations.Add(Tuple.Create(GetOriginalIdentifierToken(cancellationToken), _annotation));
            }

            public override bool GetUseSaferDeclarationBehavior(CancellationToken cancellationToken)
            {
                var identifier = GetOriginalIdentifierToken(cancellationToken);

                // check whether there is a noisy trivia around the token.
                if (ContainsNoisyTrivia(identifier.LeadingTrivia))
                {
                    return true;
                }

                if (ContainsNoisyTrivia(identifier.TrailingTrivia))
                {
                    return true;
                }

                var declStatement = identifier.Parent.FirstAncestorOrSelf<T>();
                if (declStatement == null)
                {
                    return true;
                }

                foreach (var token in declStatement.DescendantTokens())
                {
                    if (ContainsNoisyTrivia(token.LeadingTrivia))
                    {
                        return true;
                    }

                    if (ContainsNoisyTrivia(token.TrailingTrivia))
                    {
                        return true;
                    }
                }

                return false;
            }

            private bool ContainsNoisyTrivia(SyntaxTriviaList list)
                => list.Any(t => !_nonNoisySet.Contains(t.RawKind));
        }

        protected class QueryVariableSymbol : NotMovableVariableSymbol, IComparable<QueryVariableSymbol>
        {
            private readonly IRangeVariableSymbol _symbol;

            public QueryVariableSymbol(Compilation compilation, IRangeVariableSymbol symbol, ITypeSymbol type)
                : base(compilation, type)
            {
                Contract.ThrowIfNull(symbol);
                _symbol = symbol;
            }

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

                var locationLeft = _symbol.Locations.First();
                var locationRight = other._symbol.Locations.First();

                Contract.ThrowIfFalse(locationLeft.IsInSource);
                Contract.ThrowIfFalse(locationRight.IsInSource);
                Contract.ThrowIfFalse(locationLeft.SourceTree == locationRight.SourceTree);
                Contract.ThrowIfFalse(locationLeft.SourceSpan.Start != locationRight.SourceSpan.Start);

                return locationLeft.SourceSpan.Start - locationRight.SourceSpan.Start;
            }

            public override string Name
            {
                get
                {
                    return _symbol.ToDisplayString(
                        new SymbolDisplayFormat(
                            miscellaneousOptions: SymbolDisplayMiscellaneousOptions.EscapeKeywordIdentifiers));
                }
            }

            public override bool CanBeCapturedByLocalFunction => false;
        }
    }
}
