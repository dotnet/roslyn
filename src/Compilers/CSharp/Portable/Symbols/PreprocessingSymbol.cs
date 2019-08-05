// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    /// <summary>
    /// Represents a preprocessing conditional compilation symbol.
    /// </summary>
    internal class PreprocessingSymbol : Symbol, IPreprocessingSymbol
    {
        private readonly string _name;

        internal PreprocessingSymbol(string name)
        {
            _name = name;
        }

        public override string Name
        {
            get
            {
                return _name;
            }
        }

        public override ImmutableArray<Location> Locations
        {
            get
            {
                return ImmutableArray<Location>.Empty;
            }
        }

        public override ImmutableArray<SyntaxReference> DeclaringSyntaxReferences
        {
            get
            {
                return GetDeclaringSyntaxReferenceHelper<CSharpSyntaxNode>(Locations);
            }
        }

        public override SymbolKind Kind
        {
            get
            {
                return SymbolKind.Preprocessing;
            }
        }

        public override Symbol ContainingSymbol
        {
            get
            {
                return null;
            }
        }

        public override Accessibility DeclaredAccessibility
        {
            get
            {
                return Accessibility.NotApplicable;
            }
        }

        public override bool IsStatic
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

        public override bool IsOverride
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

        public override bool IsSealed
        {
            get
            {
                return false;
            }
        }

        public override bool IsExtern
        {
            get
            {
                return false;
            }
        }

        internal override ObsoleteAttributeData ObsoleteAttributeData
        {
            get
            {
                return null;
            }
        }

        public override bool Equals(Symbol obj, TypeCompareKind compareKind)
        {
            if (ReferenceEquals(this, obj))
            {
                return true;
            }

            if (ReferenceEquals(obj, null))
            {
                return false;
            }

            PreprocessingSymbol other = obj as PreprocessingSymbol;

            return (object)other != null &&
                this.Name.Equals(other.Name);
        }

        public override int GetHashCode()
        {
            return this.Name.GetHashCode();
        }

        internal override TResult Accept<TArgument, TResult>(CSharpSymbolVisitor<TArgument, TResult> visitor, TArgument a)
        {
            throw new System.NotImplementedException();
        }

        public override void Accept(SymbolVisitor visitor)
        {
            throw new System.NotSupportedException();
        }

        public override TResult Accept<TResult>(SymbolVisitor<TResult> visitor)
        {
            throw new System.NotSupportedException();
        }

        public override void Accept(CSharpSymbolVisitor visitor)
        {
            throw new System.NotSupportedException();
        }

        public override TResult Accept<TResult>(CSharpSymbolVisitor<TResult> visitor)
        {
            throw new System.NotSupportedException();
        }
    }
}
