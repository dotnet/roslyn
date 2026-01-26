// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Diagnostics;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    /// <summary>
    /// A RangeVariableSymbol represents an identifier introduced in a query expression as the
    /// identifier of a "from" clause, an "into" query continuation, a "let" clause, or a "join" clause.
    /// </summary>
    internal sealed class RangeVariableSymbol : Symbol
    {
        private readonly string _name;
        private readonly Location? _location;
        private readonly Symbol _containingSymbol;

        internal RangeVariableSymbol(string Name, Symbol containingSymbol, Location? location, bool isTransparent = false)
        {
            _name = Name;
            _containingSymbol = containingSymbol;
            _location = location;
            this.IsTransparent = isTransparent;
        }

        internal bool IsTransparent { get; }

        public override string Name
        {
            get
            {
                return _name;
            }
        }

        public override SymbolKind Kind
        {
            get
            {
                return SymbolKind.RangeVariable;
            }
        }

        public override ImmutableArray<Location> Locations
            => _location is null ? ImmutableArray<Location>.Empty : ImmutableArray.Create(_location);

        public override Location? TryGetFirstLocation()
            => _location;

        public override ImmutableArray<SyntaxReference> DeclaringSyntaxReferences
        {
            get
            {
                if (_location is null)
                    return ImmutableArray<SyntaxReference>.Empty;

                Debug.Assert(_location.SourceTree != null);
                SyntaxToken token = _location.SourceTree.GetRoot().FindToken(_location.SourceSpan.Start);
                Debug.Assert(token.Kind() == SyntaxKind.IdentifierToken);
                var node = token.Parent;
                Debug.Assert(node is QueryClauseSyntax || node is QueryContinuationSyntax || node is JoinIntoClauseSyntax);
                return ImmutableArray.Create(node.GetReference());
            }
        }

        public override bool IsExtern
        {
            get
            {
                return false;
            }
        }

        public override bool IsSealed
        {
            get
            {
                return false;
            }
        }

        public override bool IsAbstract
        {
            get
            {
                return false;
            }
        }

        public override bool IsOverride
        {
            get
            {
                return false;
            }
        }

        public override bool IsVirtual
        {
            get
            {
                return false;
            }
        }

        public override bool IsStatic
        {
            get
            {
                return false;
            }
        }

        /// <summary>
        /// Returns data decoded from Obsolete attribute or null if there is no Obsolete attribute.
        /// This property returns ObsoleteAttributeData.Uninitialized if attribute arguments haven't been decoded yet.
        /// </summary>
        internal sealed override ObsoleteAttributeData? ObsoleteAttributeData
        {
            get { return null; }
        }

        internal sealed override CallerUnsafeMode CallerUnsafeMode => CallerUnsafeMode.None;

        public override Accessibility DeclaredAccessibility
        {
            get
            {
                return Accessibility.NotApplicable;
            }
        }

        public override Symbol ContainingSymbol
        {
            get
            {
                return _containingSymbol;
            }
        }

        internal override TResult Accept<TArg, TResult>(CSharpSymbolVisitor<TArg, TResult> visitor, TArg a)
        {
            return visitor.VisitRangeVariable(this, a);
        }

        public override void Accept(CSharpSymbolVisitor visitor)
        {
            visitor.VisitRangeVariable(this);
        }

        public override TResult Accept<TResult>(CSharpSymbolVisitor<TResult> visitor)
        {
            return visitor.VisitRangeVariable(this);
        }

        public override bool Equals(Symbol obj, TypeCompareKind compareKind)
        {
            if (obj == (object)this)
            {
                return true;
            }

            // If we have no location, we have no way to compare two distinct range variables for equality.  So if we
            // don't have the exact same instance, we have to presume these are not the same.
            if (_location is null)
            {
                return false;
            }

            return obj is RangeVariableSymbol symbol
                && _location.Equals(symbol._location)
                && _containingSymbol.Equals(symbol.ContainingSymbol, compareKind);
        }

        public override int GetHashCode()
        {
            return Hash.Combine(_location?.GetHashCode() ?? 0, _containingSymbol.GetHashCode());
        }

        protected override ISymbol CreateISymbol()
        {
            return new PublicModel.RangeVariableSymbol(this);
        }
    }
}
