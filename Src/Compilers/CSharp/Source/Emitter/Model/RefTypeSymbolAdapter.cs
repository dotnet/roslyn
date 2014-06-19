using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Cci;
using Roslyn.Compilers.CSharp.Emit;

namespace Roslyn.Compilers.CSharp
{
    partial class RefTypeSymbol :
        IManagedPointerTypeReference
    {
        ITypeReference IManagedPointerTypeReference.GetTargetType(object context)
        {
            return ((Module)context).Translate(this.ReferencedType);
        }

        bool ITypeReference.IsEnum
        {
            get { return false; }
        }

        bool ITypeReference.IsValueType
        {
            get { return false; }
        }

        ITypeDefinition ITypeReference.GetResolvedType(object context)
        {
            return null;
        }

        PrimitiveTypeCode ITypeReference.TypeCode(object context)
        {
            return PrimitiveTypeCode.Reference;
        }

        uint ITypeReference.TypeDefRowId
        {
            get { return 0; }
        }

        IGenericMethodParameterReference ITypeReference.AsGenericMethodParameterReference
        {
            get { return null; }
        }

        IGenericTypeInstanceReference ITypeReference.AsGenericTypeInstanceReference
        {
            get { return null; }
        }

        IGenericTypeParameterReference ITypeReference.AsGenericTypeParameterReference
        {
            get { return null; }
        }

        INamespaceTypeDefinition ITypeReference.AsNamespaceTypeDefinition(object context)
        {
            return null;
        }

        INamespaceTypeReference ITypeReference.AsNamespaceTypeReference
        {
            get { return null; }
        }

        INestedTypeDefinition ITypeReference.AsNestedTypeDefinition(object context)
        {
            return null;
        }

        INestedTypeReference ITypeReference.AsNestedTypeReference
        {
            get { return null; }
        }

        ISpecializedNestedTypeReference ITypeReference.AsSpecializedNestedTypeReference
        {
            get { return null; }
        }

        ITypeDefinition ITypeReference.AsTypeDefinition(object context)
        {
            return null;
        }

        void IReference.Dispatch(IMetadataVisitor visitor)
        {
            visitor.Visit((Microsoft.Cci.IManagedPointerTypeReference)this);
        }

        IDefinition IReference.AsDefinition(object context)
        {
            return null;
        }
    }
}
