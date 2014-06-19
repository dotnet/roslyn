using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using Roslyn.Compilers.Internal;
using Roslyn.Compilers.Collections;

namespace Roslyn.Compilers.CSharp
{
    internal class ImplicitTypeConstructorSymbol : MethodSymbol
    {
        private readonly NamedTypeSymbol containingType;

        // TODO: builders are supposed to be transient, but this class appears to hold on to its builder.
        private readonly ReadOnlyArray<ImplicitTypeConstructorBuilder> builders;

        internal ImplicitTypeConstructorSymbol(NamedTypeSymbol containingType, ReadOnlyArray<ImplicitTypeConstructorBuilder> builders)
        {
            this.containingType = containingType;
            this.builders = builders;
        }

        internal ReadOnlyArray<ImplicitTypeConstructorBuilder> Builders
        {
            get
            {
                return builders;
            }
        }

        private Compilation Compilation
        {
            get { return (ContainingAssembly as SourceAssemblySymbol).Compilation; }
        }

        public override Symbol ContainingSymbol
        {
            get { return containingType; }
        }

        public override string Name
        {
            get { return MethodSymbol.InstanceConstructorName; }
        }

        public override bool IsVararg
        {
            get { return false; }
        }

        public override ReadOnlyArray<TypeParameterSymbol> TypeParameters
        {
            get { return ReadOnlyArray<TypeParameterSymbol>.Empty; }
        }

        public override ReadOnlyArray<ParameterSymbol> Parameters
        {
            get { return ReadOnlyArray<ParameterSymbol>.Empty; }
        }
        
        public override Accessibility DeclaredAccessibility
        {
            get { return Accessibility.Internal; }
        }

        // TODO ?
        public override ReadOnlyArray<Location> Locations
        {
            get { return ReadOnlyArray<Location>.Empty; }
        }

        public override TypeSymbol ReturnType
        {
            get { return Compilation.GetSpecialType(SpecialType.System_Void); }
        }

        public override IList<CustomModifier> ReturnTypeCustomModifiers
        {
            get { return CustomModifier.EmptyList; }
        }

        public override ReadOnlyArray<TypeSymbol> TypeArguments
        {
            get { return ReadOnlyArray<TypeSymbol>.Empty; }
        }

        public override IEnumerable<SymbolAttribute> GetAttributes()
        {
            return SpecializedCollections.EmptyEnumerable<SymbolAttribute>();
        }

        public override IEnumerable<SymbolAttribute> GetAttributes(NamedTypeSymbol attributeType)
        {
            return SpecializedCollections.EmptyEnumerable<SymbolAttribute>();
        }

        public override Symbol AssociatedPropertyOrEvent
        {
            get { return null; }
        }

        public override int Arity
        {
            get { return 0; }
        }

        public override bool ReturnsVoid
        {
            get { return true; }
        }

        public override MethodKind MethodKind
        {
            get { return MethodKind.Constructor; }
        }

        public override bool IsExtern
        {
            get { return false; }
        }

        public override bool IsSealed
        {
            get { return false; }
        }

        public override bool IsAbstract
        {
            get { return false; }
        }

        public override bool IsOverride
        {
            get { return false; }
        }

        public override bool IsVirtual
        {
            get { return false; }
        }

        public override bool IsStatic
        {
            get { return false; }
        }

        public override bool HidesBaseMethodsByName
        {
            get { return false; }
        }

        public override bool IsExtensionMethod
        {
            get { return false; }
        }

        internal override Microsoft.Cci.CallingConvention CallingConvention
        {
            get { return Microsoft.Cci.CallingConvention.HasThis; }
        }

        public override IEnumerable<MethodSymbol> ExplicitInterfaceImplementation
        {
            get { return SpecializedCollections.EmptyEnumerable<MethodSymbol>(); }
        }

        internal override bool IsFromSource
        {
            get { return true; }
        }
    }
}
