// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Symbols.PublicModel
{
    internal sealed class AliasSymbol : Symbol, IAliasSymbol
    {
        private readonly Symbols.AliasSymbol _underlying;

        public AliasSymbol(Symbols.AliasSymbol underlying)
        {
            RoslynDebug.Assert(underlying is object);
            _underlying = underlying;
        }

        internal override CSharp.Symbol UnderlyingSymbol => _underlying;

        INamespaceOrTypeSymbol IAliasSymbol.Target
        {
            get
            {
                return _underlying.Target.GetPublicSymbol();
            }
        }

        #region ISymbol Members

        protected override void Accept(SymbolVisitor visitor)
        {
            visitor.VisitAlias(this);
        }

        protected override TResult? Accept<TResult>(SymbolVisitor<TResult> visitor)
            where TResult : default
        {
            return visitor.VisitAlias(this);
        }

        protected override TResult Accept<TArgument, TResult>(SymbolVisitor<TArgument, TResult> visitor, TArgument argument)
        {
            return visitor.VisitAlias(this, argument);
        }

        #endregion
    }
}
