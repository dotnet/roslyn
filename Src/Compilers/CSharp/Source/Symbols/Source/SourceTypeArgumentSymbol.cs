using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace Roslyn.Compilers.CSharp.Symbols.Source
{
    /// Note: This will go away once we change method declarations to switch
    /// from TypeArgumentSyntax to TypeParameterSyntax
    internal class SourceTypeArgumentSymbol : TypeParameterSymbol
    {
        private readonly Symbol owner;
        private readonly int ordinal;
        private readonly IEnumerable<OldTypeArgumentBinderContext> binders;

        internal SourceTypeArgumentSymbol(Symbol owner, int ordinal, IEnumerable<OldTypeArgumentBinderContext> binders)
        {
            this.owner = owner;
            this.ordinal = ordinal;
            this.binders = binders;

            // TODOngafter 6: bind constraints
            // TODOngafter 5: check that all the partials use the same names for the type parameters
            // TODOngafter 7: check for consistent use of variance
            // TODOngafter 4: where does the property Locations get its value when this constructor is used?
        }

        internal SourceTypeArgumentSymbol(Symbol owner, int ordinal, string name)
        {
            this.name = name;
            this.owner = owner;
            this.ordinal = ordinal;
            this.binders = Enumerable.Empty<OldTypeArgumentBinderContext>();
        }

        public override Symbol ContainingSymbol
        {
            get
            {
                return owner;
            }
        }

        private IEnumerable<Location> locations;
        public override IEnumerable<Location> Locations
        {
            get
            {
                if (locations == null)
                {
                    Interlocked.CompareExchange(ref locations, binders.Select(b => b.Location()).ToArray(), null);
                }

                return locations;
            }
        }

        // public override IEnumerable<Symbol> GetMembers() { throw new NotImplementedException(); }
        // public override IEnumerable<Symbol> GetMembers(string name) { throw new NotImplementedException(); }
        // public override IEnumerable<NamedTypeSymbol> GetTypeMembers(string name, int arity) { throw new NotImplementedException(); }
        // public override IEnumerable<NamedTypeSymbol> GetTypeMembers(string name) { throw new NotImplementedException(); }
        // public override IEnumerable<NamedTypeSymbol> GetTypeMembers() { throw new NotImplementedException(); }
        // public override IEnumerable<NamedTypeSymbol> Interfaces { get { throw new NotImplementedException(); } }
        // public override NamedTypeSymbol BaseType { get { throw new NotImplementedException(); } }
        public override bool HasConstructorConstraint
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public override IEnumerable<TypeSymbol> ConstraintTypes
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public override string ToString()
        {
            return Name;
        }

        public override IEnumerable<SymbolAttribute> GetAttributes()
        {
            throw new NotImplementedException();
        }

        public override IEnumerable<SymbolAttribute> GetAttributes(NamedTypeSymbol attributeType)
        {
            throw new NotImplementedException();
        }

        // public override bool IsReferenceType { get { throw new NotImplementedException(); } }
        // public override bool IsValueType { get { throw new NotImplementedException(); } }
        public override int Ordinal
        {
            get
            {
                return ordinal;
            }
        }

        public override VarianceKind Variance
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        internal override bool HasValueTypeConstraint
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        internal override bool HasReferenceTypeConstraint
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        private string name;
        public override string Name
        {
            get
            {
                if (name == null)
                {
                    // TODOngafter 5: check that they all have the same name for the type parameter
                    // TODOngafter 5: type parameter may not have the same name as its owner
                    string result = binders.First().Declaration.SimpleName();
                    Interlocked.CompareExchange(ref name, result, null);
                }

                return name;
            }
        }
    }
}
