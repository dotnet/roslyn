using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Roslyn.Compilers.Internal;

namespace Roslyn.Compilers.CSharp.Emit
{
    internal abstract class TypeReference : Reference, Microsoft.Cci.ITypeReference
    {
        public TypeReference(Module moduleBeingBuilt)
            : base(moduleBeingBuilt)
        {
        }

        protected abstract TypeSymbol UnderlyingType { get; }

        bool Microsoft.Cci.ITypeReference.IsEnum
        {
            get
            {
                return UnderlyingType.IsEnumType();
            }
        }

        bool Microsoft.Cci.ITypeReference.IsValueType
        {
            get 
            { 
                return UnderlyingType.IsValueType; 
            }
        }

        Microsoft.Cci.ITypeDefinition Microsoft.Cci.ITypeReference.GetResolvedType(object m)
        {
            return this as Microsoft.Cci.ITypeDefinition; 
        }

        Microsoft.Cci.PrimitiveTypeCode Microsoft.Cci.ITypeReference.TypeCode(object m)
        {
            NamedTypeSymbol namedType = UnderlyingType as NamedTypeSymbol;

            if (namedType != null)
            {
                return ModuleBeingBuilt.SourceModule.GetTypeCodeOfType(namedType);
            }

            return Microsoft.Cci.PrimitiveTypeCode.NotPrimitive;
        }

        public abstract uint TypeDefRowId { get; }


        Microsoft.Cci.IArrayTypeReference Microsoft.Cci.ITypeReference.AsArrayTypeReference
        {
            get
            {
                return AsArrayTypeReference;
            }
        }

        protected abstract Microsoft.Cci.IArrayTypeReference AsArrayTypeReference { get; }

        Microsoft.Cci.IFunctionPointerTypeReference Microsoft.Cci.ITypeReference.AsFunctionPointerTypeReference 
        {
            get
            {
                return AsFunctionPointerTypeReference;
            }
        }

        protected abstract Microsoft.Cci.IFunctionPointerTypeReference AsFunctionPointerTypeReference { get; }

        Microsoft.Cci.IGenericMethodParameterReference Microsoft.Cci.ITypeReference.AsGenericMethodParameterReference
        {
            get
            {
                return AsGenericMethodParameterReference;
            }
        }

        protected abstract Microsoft.Cci.IGenericMethodParameterReference AsGenericMethodParameterReference { get; }

        Microsoft.Cci.IGenericTypeInstanceReference Microsoft.Cci.ITypeReference.AsGenericTypeInstanceReference
        {
            get
            {
                return AsGenericTypeInstanceReference;
            }
        }

        protected abstract Microsoft.Cci.IGenericTypeInstanceReference AsGenericTypeInstanceReference { get; }

        Microsoft.Cci.IGenericTypeParameterReference Microsoft.Cci.ITypeReference.AsGenericTypeParameterReference
        {
            get
            {
                return AsGenericTypeParameterReference;
            }
        }

        protected abstract Microsoft.Cci.IGenericTypeParameterReference AsGenericTypeParameterReference { get; }

        Microsoft.Cci.INamespaceTypeDefinition Microsoft.Cci.ITypeReference.AsNamespaceTypeDefinition(object moduleBeingBuilt)
        {
            return AsNamespaceTypeDefinition;
        }

        protected abstract Microsoft.Cci.INamespaceTypeDefinition AsNamespaceTypeDefinition { get; }

        Microsoft.Cci.INamespaceTypeReference Microsoft.Cci.ITypeReference.AsNamespaceTypeReference
        {
            get
            {
                return AsNamespaceTypeReference;
            }
        }

        protected abstract Microsoft.Cci.INamespaceTypeReference AsNamespaceTypeReference { get; }

        Microsoft.Cci.INestedTypeDefinition Microsoft.Cci.ITypeReference.AsNestedTypeDefinition(object moduleBeingBuilt)
        {
            return AsNestedTypeDefinition;
        }

        protected abstract Microsoft.Cci.INestedTypeDefinition AsNestedTypeDefinition { get; }

        Microsoft.Cci.INestedTypeReference Microsoft.Cci.ITypeReference.AsNestedTypeReference
        {
            get
            {
                return AsNestedTypeReference;
            }
        }

        protected abstract Microsoft.Cci.INestedTypeReference AsNestedTypeReference { get; }

        Microsoft.Cci.ISpecializedNestedTypeReference Microsoft.Cci.ITypeReference.AsSpecializedNestedTypeReference
        {
            get
            {
                return AsSpecializedNestedTypeReference;
            }
        }

        protected abstract Microsoft.Cci.ISpecializedNestedTypeReference AsSpecializedNestedTypeReference { get; }


        Microsoft.Cci.ITypeDefinition Microsoft.Cci.ITypeReference.AsTypeDefinition(object m)
        {
            return AsTypeDefinition;
        }

        protected abstract Microsoft.Cci.ITypeDefinition AsTypeDefinition { get; }


    }
}
