// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Symbols.PublicModel
{
    internal sealed class DiscardSymbol : Symbol, IDiscardSymbol
    {
        private readonly Symbols.DiscardSymbol _underlying;
        private ITypeSymbol? _lazyType;

        public DiscardSymbol(Symbols.DiscardSymbol underlying)
        {
            RoslynDebug.Assert(underlying != null);
            _underlying = underlying;
        }

        internal override CSharp.Symbol UnderlyingSymbol => _underlying;

        ITypeSymbol IDiscardSymbol.Type
        {
            get
            {
                if (_lazyType is null)
                {
                    Interlocked.CompareExchange(ref _lazyType, _underlying.TypeWithAnnotations.GetPublicSymbol(), null);
                }

                return _lazyType;
            }
        }

        CodeAnalysis.NullableAnnotation IDiscardSymbol.NullableAnnotation => _underlying.TypeWithAnnotations.ToPublicAnnotation();

        protected override void Accept(SymbolVisitor visitor) => visitor.VisitDiscard(this);
        protected override TResult? Accept<TResult>(SymbolVisitor<TResult> visitor) where TResult : default => visitor.VisitDiscard(this);
        protected override TResult Accept<TArgument, TResult>(SymbolVisitor<TArgument, TResult> visitor, TArgument argument) => visitor.VisitDiscard(this, argument);
    }
}
