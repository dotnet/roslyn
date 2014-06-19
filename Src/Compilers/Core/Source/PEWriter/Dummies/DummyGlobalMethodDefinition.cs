using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Microsoft.Cci
{
    internal sealed class DummyGlobalMethodDefinition : IGlobalMethodDefinition
    {
        #region ISignature Members

        public bool ReturnValueIsByRef
        {
            get { return false; }
        }

        public IEnumerable<IParameterDefinition> Parameters
        {
            get { return IteratorHelper.GetEmptyEnumerable<IParameterDefinition>(); }
        }

        public ITypeReference Type
        {
            get { return Dummy.TypeReference; }
        }

        public IEnumerable<ILocation> Locations
        {
            get { return IteratorHelper.GetEmptyEnumerable<ILocation>(); }
        }

        public IEnumerable<ICustomAttribute> ReturnValueAttributes
        {
            get { return IteratorHelper.GetEmptyEnumerable<ICustomAttribute>(); }
        }

        public IEnumerable<ICustomModifier> ReturnValueCustomModifiers
        {
            get { return IteratorHelper.GetEmptyEnumerable<ICustomModifier>(); }
        }

        public bool ReturnValueIsModified
        {
            get { return false; }
        }

        #endregion

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

        #region IMethodDefinition Members

        public bool AcceptsExtraArguments
        {
            get { return false; }
        }

        public IMethodBody Body
        {
            get { return Dummy.MethodBody; }
        }

        public IEnumerable<IGenericMethodParameter> GenericParameters
        {
            get { return IteratorHelper.GetEmptyEnumerable<IGenericMethodParameter>(); }
        }

        // ^ [Pure]
        public ushort GenericParameterCount
        {
            get
            {
                // ^ assume false;
                return 0;
            }
        }

        public bool HasDeclarativeSecurity
        {
            get { return false; }
        }

        public bool HasExplicitThisParameter
        {
            get { return false; }
        }

        public bool IsAbstract
        {
            get { return false; }
        }

        public bool IsAccessCheckedOnOverride
        {
            get { return false; }
        }

        public bool IsCil
        {
            get { return false; }
        }

        public bool IsConstructor
        {
            get { return false; }
        }

        public bool IsStaticConstructor
        {
            get { return false; }
        }

        public bool IsExternal
        {
            get { return false; }
        }

        public bool IsForwardReference
        {
            get { return false; }
        }

        public bool IsGeneric
        {
            get { return false; }
        }

        public bool IsHiddenBySignature
        {
            get { return false; }
        }

        public bool IsNativeCode
        {
            get { return false; }
        }

        public bool IsNewSlot
        {
            get { return false; }
        }

        public bool IsNeverInlined
        {
            get { return false; }
        }

        public bool IsNeverOptimized
        {
            get { return false; }
        }

        public bool IsPlatformInvoke
        {
            get { return false; }
        }

        public bool IsRuntimeImplemented
        {
            get { return false; }
        }

        public bool IsRuntimeInternal
        {
            get { return false; }
        }

        public bool IsRuntimeSpecial
        {
            get { return false; }
        }

        public bool IsSealed
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

        public bool IsSynchronized
        {
            get { return false; }
        }

        public bool IsVirtual
        {
            get { return false; }
        }

        public bool IsUnmanaged
        {
            get { return false; }
        }

        public CallingConvention CallingConvention
        {
            get { return CallingConvention.Default; }
        }

        public bool PreserveSignature
        {
            get { return false; }
        }

        public IPlatformInvokeInformation PlatformInvokeData
        {
            get { return Dummy.PlatformInvokeInformation; }
        }

        public bool RequiresSecurityObject
        {
            get { return false; }
        }

        public bool ReturnValueIsMarshalledExplicitly
        {
            get { return false; }
        }

        public IMarshallingInformation ReturnValueMarshallingInformation
        {
            get { return Dummy.MarshallingInformation; }
        }

        public IEnumerable<ISecurityAttribute> SecurityAttributes
        {
            get { return IteratorHelper.GetEmptyEnumerable<ISecurityAttribute>(); }
        }

        #endregion

        #region IScopeMember<IScope<INamespaceMember>> Members

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

        #region ISignature Members

        IEnumerable<IParameterTypeInformation> ISignature.Parameters
        {
            get { return IteratorHelper.GetEmptyEnumerable<IParameterTypeInformation>(); }
        }

        #endregion

        #region IMethodReference Members

        public uint InternedKey
        {
            get { return 0; }
        }

        public ushort ParameterCount
        {
            get { return 0; }
        }

        public IMethodDefinition ResolvedMethod
        {
            get { return this; }
        }

        public IEnumerable<IParameterTypeInformation> ExtraParameters
        {
            get { return IteratorHelper.GetEmptyEnumerable<IParameterTypeInformation>(); }
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

        string IGlobalMethodDefinition.Name
        {
            get { throw new NotImplementedException(); }
        }

        IMethodBody IMethodDefinition.Body
        {
            get { throw new NotImplementedException(); }
        }

        IEnumerable<IGenericMethodParameter> IMethodDefinition.GenericParameters
        {
            get { throw new NotImplementedException(); }
        }

        bool IMethodDefinition.HasDeclarativeSecurity
        {
            get { throw new NotImplementedException(); }
        }

        bool IMethodDefinition.IsAbstract
        {
            get { throw new NotImplementedException(); }
        }

        bool IMethodDefinition.IsAccessCheckedOnOverride
        {
            get { throw new NotImplementedException(); }
        }

        bool IMethodDefinition.IsConstructor
        {
            get { throw new NotImplementedException(); }
        }

        bool IMethodDefinition.IsExternal
        {
            get { throw new NotImplementedException(); }
        }

        bool IMethodDefinition.IsForwardReference
        {
            get { throw new NotImplementedException(); }
        }

        bool IMethodDefinition.IsHiddenBySignature
        {
            get { throw new NotImplementedException(); }
        }

        bool IMethodDefinition.IsNativeCode
        {
            get { throw new NotImplementedException(); }
        }

        bool IMethodDefinition.IsNewSlot
        {
            get { throw new NotImplementedException(); }
        }

        bool IMethodDefinition.IsNeverInlined
        {
            get { throw new NotImplementedException(); }
        }

        bool IMethodDefinition.IsNeverOptimized
        {
            get { throw new NotImplementedException(); }
        }

        bool IMethodDefinition.IsPlatformInvoke
        {
            get { throw new NotImplementedException(); }
        }

        bool IMethodDefinition.IsRuntimeImplemented
        {
            get { throw new NotImplementedException(); }
        }

        bool IMethodDefinition.IsRuntimeInternal
        {
            get { throw new NotImplementedException(); }
        }

        bool IMethodDefinition.IsRuntimeSpecial
        {
            get { throw new NotImplementedException(); }
        }

        bool IMethodDefinition.IsSealed
        {
            get { throw new NotImplementedException(); }
        }

        bool IMethodDefinition.IsSpecialName
        {
            get { throw new NotImplementedException(); }
        }

        bool IMethodDefinition.IsStatic
        {
            get { throw new NotImplementedException(); }
        }

        bool IMethodDefinition.IsSynchronized
        {
            get { throw new NotImplementedException(); }
        }

        bool IMethodDefinition.IsVirtual
        {
            get { throw new NotImplementedException(); }
        }

        bool IMethodDefinition.IsUnmanaged
        {
            get { throw new NotImplementedException(); }
        }

        IEnumerable<IParameterDefinition> IMethodDefinition.Parameters
        {
            get { throw new NotImplementedException(); }
        }

        bool IMethodDefinition.PreserveSignature
        {
            get { throw new NotImplementedException(); }
        }

        IPlatformInvokeInformation IMethodDefinition.PlatformInvokeData
        {
            get { throw new NotImplementedException(); }
        }

        bool IMethodDefinition.RequiresSecurityObject
        {
            get { throw new NotImplementedException(); }
        }

        IEnumerable<ICustomAttribute> IMethodDefinition.ReturnValueAttributes
        {
            get { throw new NotImplementedException(); }
        }

        bool IMethodDefinition.ReturnValueIsMarshalledExplicitly
        {
            get { throw new NotImplementedException(); }
        }

        IMarshallingInformation IMethodDefinition.ReturnValueMarshallingInformation
        {
            get { throw new NotImplementedException(); }
        }

        IEnumerable<ISecurityAttribute> IMethodDefinition.SecurityAttributes
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

        bool IMethodReference.AcceptsExtraArguments
        {
            get { throw new NotImplementedException(); }
        }

        ushort IMethodReference.GenericParameterCount
        {
            get { throw new NotImplementedException(); }
        }

        bool IMethodReference.IsGeneric
        {
            get { throw new NotImplementedException(); }
        }

        ushort IMethodReference.ParameterCount
        {
            get { throw new NotImplementedException(); }
        }

        IMethodDefinition IMethodReference.ResolvedMethod
        {
            get { throw new NotImplementedException(); }
        }

        IEnumerable<IParameterTypeInformation> IMethodReference.ExtraParameters
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
    }
}