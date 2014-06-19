using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Roslyn.Compilers.Internal;

namespace Roslyn.Compilers.CSharp.Emit
{
    internal sealed class PointerTypeReference : TypeReference, Microsoft.Cci.IPointerTypeReference
    {
        private readonly PointerTypeSymbol underlyingPointerType;

        public PointerTypeReference(Module moduleBeingBuilt, PointerTypeSymbol underlyingPointerType)
            : base(moduleBeingBuilt)
        {
            Contract.ThrowIfNull(underlyingPointerType);

            this.underlyingPointerType = underlyingPointerType;
        }

        protected override TypeSymbol UnderlyingType
        {
            get
            {
                return underlyingPointerType;
            }
        }

        protected override Symbol UnderlyingSymbol
        {
            get
            {
                return underlyingPointerType;
            }
        }

        public override void Dispatch(Microsoft.Cci.IMetadataVisitor visitor)
        {
            visitor.Visit((Microsoft.Cci.IPointerTypeReference)this);
        }

        public override uint TypeDefRowId
        {
            get { return 0; }
        }

        public Microsoft.Cci.ITypeReference TargetType
        {
            get
            {
                IList<Roslyn.Compilers.CSharp.CustomModifier> customModifiers = underlyingPointerType.CustomModifiers;

                if (customModifiers.Count == 0)
                {
                    return ModuleBeingBuilt.Translate(underlyingPointerType.PointedAtType);
                }
                else
                {
                    return new ModifiedTypeReference(ModuleBeingBuilt, underlyingPointerType.PointedAtType, customModifiers);
                }
            }
        }

        Microsoft.Cci.PrimitiveTypeCode Microsoft.Cci.ITypeReference.TypeCode(object m)
        {
            return Microsoft.Cci.PrimitiveTypeCode.Pointer;
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
