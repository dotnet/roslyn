// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
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
                this.OriginalTypeHadAnonymousTypeOrDelegate = type.ContainsAnonymousType();
                this.OriginalType = this.OriginalTypeHadAnonymousTypeOrDelegate ? type.RemoveAnonymousTypes(compilation) : type;
            }

            public abstract int DisplayOrder { get; }
            public abstract string Name { get; }

            public abstract bool GetUseSaferDeclarationBehavior(CancellationToken cancellationToken);
            public abstract SyntaxAnnotation IdentifierTokenAnnotation { get; }
            public abstract SyntaxToken GetOriginalIdentifierToken(CancellationToken cancellationToken);

            public abstract void AddIdentifierTokenAnnotationPair(
                List<Tuple<SyntaxToken, SyntaxAnnotation>> annotations, CancellationToken cancellationToken);

            protected abstract int CompareTo(VariableSymbol right);

            /// <summary>
            /// return true if original type had anonymous type or delegate somewhere in the type
            /// </summary>
            public bool OriginalTypeHadAnonymousTypeOrDelegate { get; private set; }

            /// <summary>
            /// get the original type with anonymous type removed
            /// </summary>
            public ITypeSymbol OriginalType { get; private set; }

            public static int Compare(VariableSymbol left, VariableSymbol right)
            {
                if (left.DisplayOrder == right.DisplayOrder)
                {
                    return left.CompareTo(right);
                }

                return left.DisplayOrder - right.DisplayOrder;
            }
        }

        protected abstract class NotMovableVariableSymbol : VariableSymbol
        {
            public NotMovableVariableSymbol(Compilation compilation, ITypeSymbol type) :
                base(compilation, type)
            {
            }

            public override bool GetUseSaferDeclarationBehavior(CancellationToken cancellationToken)
            {
                // decl never get moved
                return false;
            }

            [ExcludeFromCodeCoverage]
            public override SyntaxToken GetOriginalIdentifierToken(CancellationToken cancellationToken)
            {
                throw ExceptionUtilities.Unreachable;
            }

            [ExcludeFromCodeCoverage]
            public override SyntaxAnnotation IdentifierTokenAnnotation
            {
                get { throw ExceptionUtilities.Unreachable; }
            }

            public override void AddIdentifierTokenAnnotationPair(
                List<Tuple<SyntaxToken, SyntaxAnnotation>> annotations, CancellationToken cancellationToken)
            {
                // do nothing for parameter
            }
        }

        protected class ParameterVariableSymbol : NotMovableVariableSymbol, IComparable<ParameterVariableSymbol>
        {
            private readonly IParameterSymbol parameterSymbol;

            public ParameterVariableSymbol(Compilation compilation, IParameterSymbol parameterSymbol, ITypeSymbol type) :
                base(compilation, type)
            {
                Contract.ThrowIfNull(parameterSymbol);
                this.parameterSymbol = parameterSymbol;
            }

            public override int DisplayOrder
            {
                get { return 0; }
            }

            protected override int CompareTo(VariableSymbol right)
            {
                return this.CompareTo((ParameterVariableSymbol)right);
            }

            public int CompareTo(ParameterVariableSymbol other)
            {
                Contract.ThrowIfNull(other);

                if (this == other)
                {
                    return 0;
                }

                var compare = CompareTo((IMethodSymbol)this.parameterSymbol.ContainingSymbol, (IMethodSymbol)other.parameterSymbol.ContainingSymbol);
                if (compare != 0)
                {
                    return compare;
                }

                Contract.ThrowIfFalse(this.parameterSymbol.Ordinal != other.parameterSymbol.Ordinal);
                return (this.parameterSymbol.Ordinal > other.parameterSymbol.Ordinal) ? 1 : -1;
            }

            private int CompareTo(IMethodSymbol left, IMethodSymbol right)
            {
                if (left == null && right == null)
                {
                    return 0;
                }

                if (left.Equals(right))
                {
                    return 0;
                }

                if (left.MethodKind == MethodKind.AnonymousFunction &&
                    right.MethodKind != MethodKind.AnonymousFunction)
                {
                    return 1;
                }

                if (left.MethodKind != MethodKind.AnonymousFunction &&
                    right.MethodKind == MethodKind.AnonymousFunction)
                {
                    return -1;
                }

                if (left.MethodKind == MethodKind.AnonymousFunction &&
                    right.MethodKind == MethodKind.AnonymousFunction)
                {
                    Contract.ThrowIfFalse(left.Locations.Length == 1);
                    Contract.ThrowIfFalse(right.Locations.Length == 1);

                    return left.Locations[0].SourceSpan.Start - right.Locations[0].SourceSpan.Start;
                }

                return Contract.FailWithReturn<int>("Shouldn't reach here");
            }

            public override string Name
            {
                get
                {
                    return this.parameterSymbol.ToDisplayString(
                        new SymbolDisplayFormat(
                            parameterOptions: SymbolDisplayParameterOptions.IncludeName,
                            miscellaneousOptions: SymbolDisplayMiscellaneousOptions.EscapeKeywordIdentifiers));
                }
            }
        }

        protected class LocalVariableSymbol<T> : VariableSymbol, IComparable<LocalVariableSymbol<T>> where T : SyntaxNode
        {
            private readonly SyntaxAnnotation annotation;
            private readonly ILocalSymbol localSymbol;
            private readonly HashSet<int> nonNoisySet;

            public LocalVariableSymbol(Compilation compilation, ILocalSymbol localSymbol, ITypeSymbol type, HashSet<int> nonNoisySet) :
                base(compilation, type)
            {
                Contract.ThrowIfNull(localSymbol);
                Contract.ThrowIfNull(nonNoisySet);

                this.annotation = new SyntaxAnnotation();
                this.localSymbol = localSymbol;
                this.nonNoisySet = nonNoisySet;
            }

            public override int DisplayOrder
            {
                get { return 1; }
            }

            protected override int CompareTo(VariableSymbol right)
            {
                return this.CompareTo((LocalVariableSymbol<T>)right);
            }

            public int CompareTo(LocalVariableSymbol<T> other)
            {
                Contract.ThrowIfNull(other);

                if (this == other)
                {
                    return 0;
                }

                Contract.ThrowIfFalse(this.localSymbol.Locations.Length == 1);
                Contract.ThrowIfFalse(other.localSymbol.Locations.Length == 1);
                Contract.ThrowIfFalse(this.localSymbol.Locations[0].IsInSource);
                Contract.ThrowIfFalse(other.localSymbol.Locations[0].IsInSource);
                Contract.ThrowIfFalse(this.localSymbol.Locations[0].SourceTree == other.localSymbol.Locations[0].SourceTree);
                Contract.ThrowIfFalse(this.localSymbol.Locations[0].SourceSpan.Start != other.localSymbol.Locations[0].SourceSpan.Start);

                return this.localSymbol.Locations[0].SourceSpan.Start - other.localSymbol.Locations[0].SourceSpan.Start;
            }

            public override string Name
            {
                get
                {
                    return this.localSymbol.ToDisplayString(
                        new SymbolDisplayFormat(
                            miscellaneousOptions: SymbolDisplayMiscellaneousOptions.EscapeKeywordIdentifiers));
                }
            }

            public override SyntaxToken GetOriginalIdentifierToken(CancellationToken cancellationToken)
            {
                Contract.ThrowIfFalse(this.localSymbol.Locations.Length == 1);
                Contract.ThrowIfFalse(this.localSymbol.Locations[0].IsInSource);
                Contract.ThrowIfNull(this.localSymbol.Locations[0].SourceTree);

                var tree = this.localSymbol.Locations[0].SourceTree;
                var span = this.localSymbol.Locations[0].SourceSpan;

                var token = tree.GetRoot(cancellationToken).FindToken(span.Start);
                Contract.ThrowIfFalse(token.Span.Equals(span));

                return token;
            }

            public override SyntaxAnnotation IdentifierTokenAnnotation
            {
                get { return this.annotation; }
            }

            public override void AddIdentifierTokenAnnotationPair(
                List<Tuple<SyntaxToken, SyntaxAnnotation>> annotations, CancellationToken cancellationToken)
            {
                annotations.Add(Tuple.Create(this.GetOriginalIdentifierToken(cancellationToken), this.annotation));
            }

            public override bool GetUseSaferDeclarationBehavior(CancellationToken cancellationToken)
            {
                var identifier = this.GetOriginalIdentifierToken(cancellationToken);

                // check whether there is a noisy trivia around the token.
                if (ContainsNoisyTrivia(identifier.LeadingTrivia))
                {
                    return true;
                }

                if (ContainsNoisyTrivia(identifier.TrailingTrivia))
                {
                    return true;
                }

                var declStatement = identifier.Parent.FirstAncestorOrSelf<T>((n) => true);
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
            {
                return list.Any(t => !this.nonNoisySet.Contains(t.RawKind));
            }
        }

        protected class QueryVariableSymbol : NotMovableVariableSymbol, IComparable<QueryVariableSymbol>
        {
            private readonly IRangeVariableSymbol symbol;

            public QueryVariableSymbol(Compilation compilation, IRangeVariableSymbol symbol, ITypeSymbol type) :
                base(compilation, type)
            {
                Contract.ThrowIfNull(symbol);
                this.symbol = symbol;
            }

            public override int DisplayOrder
            {
                get { return 2; }
            }

            protected override int CompareTo(VariableSymbol right)
            {
                return this.CompareTo((QueryVariableSymbol)right);
            }

            public int CompareTo(QueryVariableSymbol other)
            {
                Contract.ThrowIfNull(other);

                if (this == other)
                {
                    return 0;
                }

                var locationLeft = this.symbol.Locations.First();
                var locationRight = other.symbol.Locations.First();

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
                    return this.symbol.ToDisplayString(
                        new SymbolDisplayFormat(
                            miscellaneousOptions: SymbolDisplayMiscellaneousOptions.EscapeKeywordIdentifiers));
                }
            }
        }
    }
}
