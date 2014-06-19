using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Roslyn.Compilers.Internal;
using Roslyn.Compilers.Collections;

namespace Roslyn.Compilers.CSharp
{
    /// <summary>
    /// An RefTypeSymbol represents an ref or out type as the type
    /// of a parameter.
    /// </summary>
    public sealed partial class RefTypeSymbol : TypeSymbol
    {
        private TypeSymbol referencedType;
        private RefKind refKind;

        /// <summary>
        /// Create a new RefTypeSymbol.
        /// </summary>
        /// <param name="referencedType">The referenced type.</param>
        /// <param name="refKind">The kind of reference ("ref" or "out").</param>
        internal RefTypeSymbol(TypeSymbol referencedType, RefKind refKind)
        {
            if (referencedType == null)
            {
                throw new ArgumentNullException("referencedType");
            }

            this.referencedType = referencedType;
            this.refKind = refKind;
        }

        /// <summary>
        /// Gets the kind of ref (ref versus out)
        /// </summary>
        public RefKind RefKind
        {
            get { return refKind; }
        }

        /// <summary>
        /// Gets the underlying type that is being passed "ref" or "out".
        /// </summary>
        public TypeSymbol ReferencedType
        {
            get { return referencedType; }
        }

        public override NamedTypeSymbol BaseType
        {
            get
            {
                // The base type of a ref type is the same as the referenced type.
                return referencedType.BaseType;
            }
        }

        public override ReadOnlyArray<NamedTypeSymbol> Interfaces
        {
            get
            {
                // The interfaces of a ref type are the same as the referenced type.
                return referencedType.Interfaces;
            }
        }

        public override bool IsReferenceType
        {
            get { return referencedType.IsReferenceType; }
        }

        public override bool IsValueType
        {
            get { return referencedType.IsValueType; }
        }

        public override IEnumerable<Symbol> GetMembers()
        {
            return referencedType.GetMembers();
        }

        public override ReadOnlyArray<Symbol> GetMembers(string name)
        {
            return referencedType.GetMembers(name);
        }

        public override IEnumerable<NamedTypeSymbol> GetTypeMembers()
        {
            return referencedType.GetTypeMembers();
        }

        public override ReadOnlyArray<NamedTypeSymbol> GetTypeMembers(string name)
        {
            return referencedType.GetTypeMembers(name);
        }

        public override IEnumerable<NamedTypeSymbol> GetTypeMembers(string name, int arity)
        {
            return referencedType.GetTypeMembers(name, arity);
        }

        public override string GetFullName()
        {
            string refPrefix = string.Empty;

            switch (refKind)
            {
                case RefKind.Out:
                    refPrefix = "out";
                    break;
                case RefKind.Ref:
                    refPrefix = "ref";
                    break;
                default:
                    throw new NotSupportedException();
            }

            return refPrefix + " " + referencedType.GetFullName();
        }

        public override SymbolKind Kind
        {
            get
            {
                return SymbolKind.RefType;
            }
        }

        public override TypeKind TypeKind
        {
            // TODO: Should there be separate TypeKind.RefType and TypeKind.OutType instead?
            get
            {
                return TypeKind.RefType;
            }
        }

        public override Symbol ContainingSymbol
        {
            get { return null; }
        }

        public override ReadOnlyArray<Location> Locations
        {
            get { return ReadOnlyArray<Location>.Empty; }
        }

        public override IEnumerable<SymbolAttribute> GetAttributes()
        {
            // Arrays don't have attributes.
            return SpecializedCollections.EmptyEnumerable<SymbolAttribute>();
        }

        public override IEnumerable<SymbolAttribute> GetAttributes(NamedTypeSymbol attributeType)
        {
            // Arrays don't have attributes.
            return SpecializedCollections.EmptyEnumerable<SymbolAttribute>();
        }

        protected internal override TResult Accept<TArg, TResult>(SymbolVisitor<TArg, TResult> visitor, TArg a)
        {
            return visitor.VisitRefType(this, a);
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
    }

    /// <summary>
    /// Denotes the kind of reference parameter.
    /// </summary>
    public enum RefKind
    {
        /// <summary>
        /// Indicates a "ref" parameter.
        /// </summary>
        Ref,

        /// <summary>
        /// Indicates an "out" parameter.
        /// </summary>
        Out
    }
}