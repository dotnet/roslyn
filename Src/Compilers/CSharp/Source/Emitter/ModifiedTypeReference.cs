using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Roslyn.Compilers.Internal;

namespace Roslyn.Compilers.CSharp.Emit
{
    internal sealed class ModifiedTypeReference : Microsoft.Cci.IModifiedTypeReference
    {
        private readonly Microsoft.Cci.ITypeReference modifiedType;
        private readonly IEnumerable<Microsoft.Cci.ICustomModifier> customModifiers;

        public ModifiedTypeReference(Microsoft.Cci.ITypeReference modifiedType, IEnumerable<Microsoft.Cci.ICustomModifier> customModifiers)
        {
            Contract.ThrowIfNull(modifiedType);
            Contract.ThrowIfNull(customModifiers);

            this.modifiedType = modifiedType;
            this.customModifiers = customModifiers;
        }

        IEnumerable<Microsoft.Cci.ICustomModifier> Microsoft.Cci.IModifiedTypeReference.CustomModifiers
        {
            get 
            {
                return customModifiers;
            }
        }

        Microsoft.Cci.ITypeReference Microsoft.Cci.IModifiedTypeReference.UnmodifiedType
        {
            get 
            {
                return modifiedType; 
            }
        }

        bool Microsoft.Cci.ITypeReference.IsEnum
        {
            get { throw new NotImplementedException(); }
        }

        bool Microsoft.Cci.ITypeReference.IsValueType
        {
            get { throw new NotImplementedException(); }
        }

        Microsoft.Cci.ITypeDefinition Microsoft.Cci.ITypeReference.GetResolvedType(object m)
        {
            throw new NotImplementedException(); 
        }

        Microsoft.Cci.PrimitiveTypeCode Microsoft.Cci.ITypeReference.TypeCode(object m)
        {
            return Microsoft.Cci.PrimitiveTypeCode.NotPrimitive; 
        }

        uint Microsoft.Cci.ITypeReference.TypeDefRowId
        {
            get { throw new NotImplementedException(); }
        }

        IEnumerable<Microsoft.Cci.ICustomAttribute> Microsoft.Cci.IReference.Attributes
        {
            get
            {

                return SpecializedCollections.EmptyEnumerable<Microsoft.Cci.ICustomAttribute>();
            }
        }

        void Microsoft.Cci.IReference.Dispatch(Microsoft.Cci.IMetadataVisitor visitor)
        {
            visitor.Visit((Microsoft.Cci.IModifiedTypeReference)this);
        }

        IEnumerable<Microsoft.Cci.ILocation> Microsoft.Cci.IObjectWithLocations.Locations
        {
            get { throw new NotImplementedException(); }
        }

        Microsoft.Cci.IGenericMethodParameterReference Microsoft.Cci.ITypeReference.AsGenericMethodParameterReference
        {
            get
            {
                return null;
            }
        }

        Microsoft.Cci.IGenericTypeInstanceReference Microsoft.Cci.ITypeReference.AsGenericTypeInstanceReference
        {
            get
            {
                return null;
            }
        }

        Microsoft.Cci.IGenericTypeParameterReference Microsoft.Cci.ITypeReference.AsGenericTypeParameterReference
        {
            get
            {
                return null;
            }
        }

        Microsoft.Cci.INamespaceTypeDefinition Microsoft.Cci.ITypeReference.AsNamespaceTypeDefinition(object moduleBeingBuilt)
        {
            return null;
        }

        Microsoft.Cci.INamespaceTypeReference Microsoft.Cci.ITypeReference.AsNamespaceTypeReference
        {
            get
            {
                return null;
            }
        }

        Microsoft.Cci.INestedTypeDefinition Microsoft.Cci.ITypeReference.AsNestedTypeDefinition(object moduleBeingBuilt)
        {
            return null;
        }

        Microsoft.Cci.INestedTypeReference Microsoft.Cci.ITypeReference.AsNestedTypeReference
        {
            get
            {
                return null;
            }
        }

        Microsoft.Cci.ISpecializedNestedTypeReference Microsoft.Cci.ITypeReference.AsSpecializedNestedTypeReference
        {
            get
            {
                return null;
            }
        }

        Microsoft.Cci.ITypeDefinition Microsoft.Cci.ITypeReference.AsTypeDefinition(object m)
        {
            return null;
        }

        Microsoft.Cci.IDefinition Microsoft.Cci.IReference.AsDefinition(object m)
        {
            return null;
        }
    }
}
