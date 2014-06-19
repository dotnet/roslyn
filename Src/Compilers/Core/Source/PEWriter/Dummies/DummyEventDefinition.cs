using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Microsoft.Cci
{
    internal sealed class DummyEventDefinition : IEventDefinition
    {
        #region IEventDefinition Members

        public IEnumerable<IMethodReference> Accessors
        {
            get { return IteratorHelper.GetEmptyEnumerable<IMethodReference>(); }
        }

        public IMethodReference Adder
        {
            get { return Dummy.MethodReference; }
        }

        public IMethodReference/*?*/ Caller
        {
            get { return null; }
        }

        public IEnumerable<ILocation> Locations
        {
            get { return IteratorHelper.GetEmptyEnumerable<ILocation>(); }
        }

        public bool IsRuntimeSpecial
        {
            get { return false; }
        }

        public bool IsSpecialName
        {
            get { return false; }
        }

        public IMethodReference Remover
        {
            get { return Dummy.MethodReference; }
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

        #region ITypeMemberReference Members

        public ITypeReference ContainingType
        {
            get { return Dummy.TypeReference; }
        }

        public ITypeDefinitionMember ResolvedTypeDefinitionMember
        {
            get { return Dummy.Event; }
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

        IEnumerable<IMethodReference> IEventDefinition.Accessors
        {
            get { throw new NotImplementedException(); }
        }

        IMethodReference IEventDefinition.Adder
        {
            get { throw new NotImplementedException(); }
        }

        IMethodReference IEventDefinition.Caller
        {
            get { throw new NotImplementedException(); }
        }

        bool IEventDefinition.IsRuntimeSpecial
        {
            get { throw new NotImplementedException(); }
        }

        bool IEventDefinition.IsSpecialName
        {
            get { throw new NotImplementedException(); }
        }

        IMethodReference IEventDefinition.Remover
        {
            get { throw new NotImplementedException(); }
        }

        ITypeReference IEventDefinition.Type
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
    }
}