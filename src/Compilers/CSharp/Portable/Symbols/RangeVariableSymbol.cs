// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Diagnostics;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    /// <summary>
    /// A RangeVariableSymbol represents an identifier introduced in a query expression as the
    /// identifier of a "from" clause, an "into" query continuation, a "let" clause, or a "join" clause.
    /// </summary>
    internal class RangeVariableSymbol : Symbol, IRangeVariableSymbol
    {
        private readonly string _name;
        private readonly ImmutableArray<Location> _locations;
        private readonly Symbol _containingSymbol;

        internal RangeVariableSymbol(string Name, Symbol containingSymbol, Location location, bool isTransparent = false)
        {
            _name = Name;
            _containingSymbol = containingSymbol;
            _locations = ImmutableArray.Create<Location>(location);
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
        {
            get
            {
                return _locations;
            }
        }

        public override ImmutableArray<SyntaxReference> DeclaringSyntaxReferences
        {
            get
            {
                SyntaxToken token = (SyntaxToken)_locations[0].SourceTree.GetRoot().FindToken(_locations[0].SourceSpan.Start);
                Debug.Assert(token.Kind() == SyntaxKind.IdentifierToken);
                CSharpSyntaxNode node = (CSharpSyntaxNode)token.Parent;
                Debug.Assert(node is QueryClauseSyntax || node is QueryContinuationSyntax || node is JoinIntoClauseSyntax);
                return ImmutableArray.Create<SyntaxReference>(node.GetReference());
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
        internal sealed override ObsoleteAttributeData ObsoleteAttributeData
        {
            get { return null; }
        }

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

        public override void Accept(SymbolVisitor visitor)
        {
            visitor.VisitRangeVariable(this);
        }

        public override TResult Accept<TResult>(SymbolVisitor<TResult> visitor)
        {
            return visitor.VisitRangeVariable(this);
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

            var symbol = obj as RangeVariableSymbol;
            return (object)symbol != null
                && symbol._locations[0].Equals(_locations[0])
                && _containingSymbol.Equals(symbol.ContainingSymbol, compareKind);
        }

        public override int GetHashCode()
        {
            return Hash.Combine(_locations[0].GetHashCode(), _containingSymbol.GetHashCode());
        }
    }
}
