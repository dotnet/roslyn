using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Microsoft.Cci
{
    internal sealed class DummyPropertyDefinition : IPropertyDefinition
    {
        #region IPropertyDefinition Members

        public IEnumerable<IMethodReference> Accessors
        {
            get { return IteratorHelper.GetEmptyEnumerable<IMethodReference>(); }
        }

        public IMetadataConstant DefaultValue
        {
            get { return Dummy.Constant; }
        }

        public IMethodReference/*?*/ Getter
        {
            get { return null; }
        }

        public bool HasDefaultValue
        {
            get { return false; }
        }

        public bool IsRuntimeSpecial
        {
            get { return false; }
        }

        public bool IsSpecialName
        {
            get { return false; }
        }

        public IEnumerable<ILocation> Locations
        {
            get { return IteratorHelper.GetEmptyEnumerable<ILocation>(); }
        }

        public IMethodReference/*?*/ Setter
        {
            get { return null; }
        }

        #endregion

        #region ISignature Members

        public IEnumerable<IParameterDefinition> Parameters
        {
            get { return IteratorHelper.GetEmptyEnumerable<IParameterDefinition>(); }
        }

        public IEnumerable<ICustomAttribute> ReturnValueAttributes
        {
            get { return IteratorHelper.GetEmptyEnumerable<ICustomAttribute>(); }
        }

        public IEnumerable<ICustomModifier> ReturnValueCustomModifiers
        {
            get { return IteratorHelper.GetEmptyEnumerable<ICustomModifier>(); }
        }

        public bool ReturnValueIsByRef
        {
            get { return false; }
        }

        public bool ReturnValueIsModified
        {
            get { return false; }
        }

        public ITypeReference Type
        {
            get { return Dummy.TypeReference; }
        }

        public CallingConvention CallingConvention
        {
            get { return CallingConvention.C; }
        }

        #endregion

        #region ITypeDefinitionMember Members

        public ITypeDefinition ContainingTypeDefinition
        {
            get { return Dummy.Type; }
        }

        public TypeMemberVisibility Visibility
        {
            get { return TypeMemberVisibility.Other; }
        }

        #endregion

        #region ITypeMemberReference Members

        public ITypeReference ContainingType
        {
            get { return Dummy.TypeReference; }
        }

        public ITypeDefinitionMember ResolvedTypeDefinitionMember
        {
            get { return Dummy.Property; }
        }

        #endregion

        #region IContainerMember<ITypeDefinition> Members

        public ITypeDefinition Container
        {
            get { return Dummy.Type; }
        }

        #endregion

        #region INamedEntity Members

        public string Name
        {
            get { return Dummy.Name; }
        }

        #endregion

        #region IDefinition Members

        public IEnumerable<ICustomAttribute> Attributes
        {
            get { return IteratorHelper.GetEmptyEnumerable<ICustomAttribute>(); }
        }

        #endregion

        #region IDoubleDispatcher Members

        public void Dispatch(IMetadataVisitor visitor)
        {
        }

        #endregion

        #region IScopeMember<IScope<ITypeDefinitionMember>> Members

        #endregion

        #region ISignature Members

        IEnumerable<IParameterTypeInformation> ISignature.Parameters
        {
            get { return IteratorHelper.GetEmptyEnumerable<IParameterTypeInformation>(); }
        }

        #endregion

        #region IMetadataConstantContainer

        public IMetadataConstant Constant
        {
            get { return Dummy.Constant; }
        }

        #endregion

        IEnumerable<IMethodReference> IPropertyDefinition.Accessors
        {
            get { throw new NotImplementedException(); }
        }

        IMetadataConstant IPropertyDefinition.DefaultValue
        {
            get { throw new NotImplementedException(); }
        }

        IMethodReference IPropertyDefinition.Getter
        {
            get { throw new NotImplementedException(); }
        }

        bool IPropertyDefinition.HasDefaultValue
        {
            get { throw new NotImplementedException(); }
        }

        bool IPropertyDefinition.IsRuntimeSpecial
        {
            get { throw new NotImplementedException(); }
        }

        bool IPropertyDefinition.IsSpecialName
        {
            get { throw new NotImplementedException(); }
        }

        IEnumerable<IParameterDefinition> IPropertyDefinition.Parameters
        {
            get { throw new NotImplementedException(); }
        }

        IEnumerable<ICustomAttribute> IPropertyDefinition.ReturnValueAttributes
        {
            get { throw new NotImplementedException(); }
        }

        IMethodReference IPropertyDefinition.Setter
        {
            get { throw new NotImplementedException(); }
        }

        CallingConvention ISignature.CallingConvention
        {
            get { throw new NotImplementedException(); }
        }

        IEnumerable<ICustomModifier> ISignature.ReturnValueCustomModifiers
        {
            get { throw new NotImplementedException(); }
        }

        bool ISignature.ReturnValueIsByRef
        {
            get { throw new NotImplementedException(); }
        }

        bool ISignature.ReturnValueIsModified
        {
            get { throw new NotImplementedException(); }
        }

        ITypeReference ISignature.Type
        {
            get { throw new NotImplementedException(); }
        }

        ITypeDefinition ITypeDefinitionMember.ContainingTypeDefinition
        {
            get { throw new NotImplementedException(); }
        }

        TypeMemberVisibility ITypeDefinitionMember.Visibility
        {
            get { throw new NotImplementedException(); }
        }

        ITypeReference ITypeMemberReference.ContainingType
        {
            get { throw new NotImplementedException(); }
        }

        IEnumerable<ICustomAttribute> IReference.Attributes
        {
            get { throw new NotImplementedException(); }
        }

        void IReference.Dispatch(IMetadataVisitor visitor)
        {
            throw new NotImplementedException();
        }

        IEnumerable<ILocation> IObjectWithLocations.Locations
        {
            get { throw new NotImplementedException(); }
        }

        string INamedEntity.Name
        {
            get { throw new NotImplementedException(); }
        }

        IMetadataConstant IMetadataConstantContainer.Constant
        {
            get { throw new NotImplementedException(); }
        }
    }
}