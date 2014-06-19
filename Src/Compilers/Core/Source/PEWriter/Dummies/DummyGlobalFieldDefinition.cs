using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Microsoft.Cci
{
    internal sealed class DummyGlobalFieldDefinition : IGlobalFieldDefinition
    {
        #region INamedEntity Members

        public string Name
        {
            get { return Dummy.Name; }
        }

        #endregion

        #region INamespaceMember Members

        public void Dispatch(IMetadataVisitor visitor)
        {
        }

        #endregion

        #region IContainerMember<INamespaceDefinition> Members

        #endregion

        #region IDefinition Members

        public IEnumerable<ICustomAttribute> Attributes
        {
            get { return IteratorHelper.GetEmptyEnumerable<ICustomAttribute>(); }
        }

        #endregion

        #region IScopeMember<IScope<INamespaceMember>> Members

        #endregion

        #region IFieldDefinition Members

        public uint BitLength
        {
            get { return 0; }
        }

        public bool IsBitField
        {
            get { return false; }
        }

        public IEnumerable<ILocation> Locations
        {
            get { return IteratorHelper.GetEmptyEnumerable<ILocation>(); }
        }

        public bool IsCompileTimeConstant
        {
            get { return false; }
        }

        public bool IsMapped
        {
            get { return false; }
        }

        public bool IsMarshalledExplicitly
        {
            get { return false; }
        }

        public bool IsNotSerialized
        {
            get { return true; }
        }

        public bool IsReadOnly
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

        public bool IsStatic
        {
            get { return false; }
        }

        public ISectionBlock FieldMapping
        {
            get { return Dummy.SectionBlock; }
        }

        public uint Offset
        {
            get { return 0; }
        }

        public int SequenceNumber
        {
            get { return 0; }
        }

        public IMetadataConstant CompileTimeValue
        {
            get { return Dummy.Constant; }
        }

        public IMarshallingInformation MarshallingInformation
        {
            get
            {
                // ^ assume false;
                IMarshallingInformation/*?*/ dummyValue = null;

                // ^ assume dummyValue != null;
                return dummyValue;
            }
        }

        public ITypeReference Type
        {
            get { return Dummy.TypeReference; }
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

        #region IContainerMember<ITypeDefinition> Members

        #endregion

        #region IScopeMember<IScope<ITypeDefinitionMember>> Members

        #endregion

        #region IFieldReference Members

        public IFieldDefinition ResolvedField
        {
            get { return this; }
        }

        #endregion

        #region ITypeMemberReference Members

        public ITypeReference ContainingType
        {
            get { return Dummy.TypeReference; }
        }

        public ITypeDefinitionMember ResolvedTypeDefinitionMember
        {
            get { return this; }
        }

        #endregion

        #region IMetadataConstantContainer

        public IMetadataConstant Constant
        {
            get { return Dummy.Constant; }
        }

        #endregion

        IMetadataConstant IFieldDefinition.CompileTimeValue
        {
            get { throw new NotImplementedException(); }
        }

        ISectionBlock IFieldDefinition.FieldMapping
        {
            get { throw new NotImplementedException(); }
        }

        bool IFieldDefinition.IsCompileTimeConstant
        {
            get { throw new NotImplementedException(); }
        }

        bool IFieldDefinition.IsMapped
        {
            get { throw new NotImplementedException(); }
        }

        bool IFieldDefinition.IsMarshalledExplicitly
        {
            get { throw new NotImplementedException(); }
        }

        bool IFieldDefinition.IsNotSerialized
        {
            get { throw new NotImplementedException(); }
        }

        bool IFieldDefinition.IsReadOnly
        {
            get { throw new NotImplementedException(); }
        }

        bool IFieldDefinition.IsRuntimeSpecial
        {
            get { throw new NotImplementedException(); }
        }

        bool IFieldDefinition.IsSpecialName
        {
            get { throw new NotImplementedException(); }
        }

        bool IFieldDefinition.IsStatic
        {
            get { throw new NotImplementedException(); }
        }

        IMarshallingInformation IFieldDefinition.MarshallingInformation
        {
            get { throw new NotImplementedException(); }
        }

        uint IFieldDefinition.Offset
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

        ITypeReference IFieldReference.Type
        {
            get { throw new NotImplementedException(); }
        }

        IFieldDefinition IFieldReference.ResolvedField
        {
            get { throw new NotImplementedException(); }
        }

        IMetadataConstant IMetadataConstantContainer.Constant
        {
            get { throw new NotImplementedException(); }
        }

        public bool IsSpecialized
        {
            get { throw new NotImplementedException(); }
        }
    }
}