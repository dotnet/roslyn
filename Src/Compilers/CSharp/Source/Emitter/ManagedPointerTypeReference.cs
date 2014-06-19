using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Roslyn.Compilers.Internal;

namespace Roslyn.Compilers.CSharp.Emit
{
    internal sealed class ManagedPointerTypeReference : TypeReference, Microsoft.Cci.IManagedPointerTypeReference
    {
        private readonly RefTypeSymbol underlyingRefType;

        public ManagedPointerTypeReference(Module moduleBeingBuilt, RefTypeSymbol underlyingRefType)
            : base(moduleBeingBuilt)
        {
            Contract.ThrowIfNull(underlyingRefType);

            this.underlyingRefType = underlyingRefType;
        }

        protected override TypeSymbol UnderlyingType
        {
            get 
            {
                return underlyingRefType; 
            }
        }

        protected override Symbol UnderlyingSymbol
        {
            get
            {
                return underlyingRefType;
            }
        }

        public override void Dispatch(Microsoft.Cci.IMetadataVisitor visitor)
        {
            visitor.Visit((Microsoft.Cci.IManagedPointerTypeReference)this);
        }

        public override uint TypeDefRowId
        {
            get { return 0; }
        }

        public Microsoft.Cci.ITypeReference TargetType
        {
            get 
            { 
                return ModuleBeingBuilt.Translate(underlyingRefType.ReferencedType); 
            }
        }

        Microsoft.Cci.PrimitiveTypeCode Microsoft.Cci.ITypeReference.TypeCode(object m)
        {
            return Microsoft.Cci.PrimitiveTypeCode.Reference;
        }

        protected override Microsoft.Cci.IArrayTypeReference AsArrayTypeReference
        {
            get { return null; }
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
