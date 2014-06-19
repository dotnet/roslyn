using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Roslyn.Compilers.Internal;

namespace Roslyn.Compilers.CSharp.Emit
{
    internal sealed class ArrayTypeReference : TypeReference, Microsoft.Cci.IArrayTypeReference
    {
        private readonly ArrayTypeSymbol underlyingArrayType;

        public ArrayTypeReference(Module moduleBeingBuilt, ArrayTypeSymbol underlyingArrayType)
            : base(moduleBeingBuilt)
        {
            Contract.ThrowIfNull(underlyingArrayType);

            this.underlyingArrayType = underlyingArrayType;
        }

        protected override TypeSymbol UnderlyingType
        {
            get 
            {
                return underlyingArrayType; 
            }
        }

        protected override Symbol UnderlyingSymbol
        {
            get
            {
                return underlyingArrayType;
            }
        }

        public override void Dispatch(Microsoft.Cci.IMetadataVisitor visitor)
        {
            visitor.Visit((Microsoft.Cci.IArrayTypeReference)this);
        }

        Microsoft.Cci.ITypeReference Microsoft.Cci.IArrayTypeReference.ElementType
        {
            get 
            {
                IList<Roslyn.Compilers.CSharp.CustomModifier> customModifiers = underlyingArrayType.CustomModifiers;

                if (customModifiers.Count == 0)
                {
                    return ModuleBeingBuilt.Translate(underlyingArrayType.ElementType);
                }
                else
                {
                    return new ModifiedTypeReference(ModuleBeingBuilt, underlyingArrayType.ElementType, customModifiers);
                }
            }
        }

        bool Microsoft.Cci.IArrayTypeReference.IsVector
        {
            get 
            { 
                return underlyingArrayType.Rank == 1; 
            }
        }

        IEnumerable<int> Microsoft.Cci.IArrayTypeReference.LowerBounds
        {
            get 
            { 
                return Enumerable.Empty<int>(); 
            }
        }

        uint Microsoft.Cci.IArrayTypeReference.Rank
        {
            get 
            {
                return (uint)underlyingArrayType.Rank; 
            }
        }

        IEnumerable<ulong> Microsoft.Cci.IArrayTypeReference.Sizes
        {
            get 
            {
                return Enumerable.Empty<ulong>();
            }
        }

        Microsoft.Cci.PrimitiveTypeCode Microsoft.Cci.ITypeReference.TypeCode(object m)
        {
            return Microsoft.Cci.PrimitiveTypeCode.NotPrimitive; 
        }

        public override uint TypeDefRowId
        {
            get { return 0; }
        }

        protected override Microsoft.Cci.IArrayTypeReference AsArrayTypeReference
        {
            get { return this; }
        }

        protected override Microsoft.Cci.IDefinition AsDefinition
        {
            get { return null; }
        }

        protected override Microsoft.Cci.IFunctionPointerTypeReference AsFunctionPointerTypeReference
        {
            get { return null; }
        }

        protected override Microsoft.Cci.IGenericMethodParameterReference AsGenericMethodParameterReference
        {
            get { return null; }
        }

        protected override Microsoft.Cci.IGenericTypeInstanceReference AsGenericTypeInstanceReference
        {
            get { return null; }
        }

        protected override Microsoft.Cci.IGenericTypeParameterReference AsGenericTypeParameterReference
        {
            get { return null; }
        }

        protected override Microsoft.Cci.INamespaceTypeDefinition AsNamespaceTypeDefinition
        {
            get { return null; }
        }

        protected override Microsoft.Cci.INamespaceTypeReference AsNamespaceTypeReference
        {
            get { return null; }
        }

        protected override Microsoft.Cci.INestedTypeDefinition AsNestedTypeDefinition
        {
            get { return null; }
        }

        protected override Microsoft.Cci.INestedTypeReference AsNestedTypeReference
        {
            get { return null; }
        }

        protected override Microsoft.Cci.ISpecializedNestedTypeReference AsSpecializedNestedTypeReference
        {
            get { return null; }
        }

        protected override Microsoft.Cci.ITypeDefinition AsTypeDefinition
        {
            get { return null; }
        }
    }
}
