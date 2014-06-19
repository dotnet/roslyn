using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Microsoft.Cci
{
    internal sealed class DummyFunctionPointerType : IFunctionPointer
    {
        #region IFunctionPointer Members

        public CallingConvention CallingConvention
        {
            get { return CallingConvention.Default; }
        }

        public IEnumerable<IParameterTypeInformation> Parameters
        {
            get { return IteratorHelper.GetEmptyEnumerable<IParameterTypeInformation>(); }
        }

        public IEnumerable<IParameterTypeInformation> ExtraArgumentTypes
        {
            get { return IteratorHelper.GetEmptyEnumerable<IParameterTypeInformation>(); }
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

        #endregion

        #region ITypeDefinition Members

        public ushort Alignment
        {
            get { return 0; }
        }

        public IEnumerable<ITypeReference> BaseClasses
        {
            get { return IteratorHelper.GetEmptyEnumerable<ITypeReference>(); }
        }

        public IEnumerable<IEventDefinition> Events
        {
            get { return IteratorHelper.GetEmptyEnumerable<IEventDefinition>(); }
        }

        public IEnumerable<IFieldDefinition> Fields
        {
            get { return IteratorHelper.GetEmptyEnumerable<IFieldDefinition>(); }
        }

        public IEnumerable<IMethodDefinition> Methods
        {
            get { return IteratorHelper.GetEmptyEnumerable<IMethodDefinition>(); }
        }

        public IEnumerable<INestedTypeDefinition> NestedTypes
        {
            get { return IteratorHelper.GetEmptyEnumerable<INestedTypeDefinition>(); }
        }

        public IEnumerable<IPropertyDefinition> Properties
        {
            get { return IteratorHelper.GetEmptyEnumerable<IPropertyDefinition>(); }
        }

        public IEnumerable<IMethodImplementation> ExplicitImplementationOverrides
        {
            get { return IteratorHelper.GetEmptyEnumerable<IMethodImplementation>(); }
        }

        public IEnumerable<IGenericTypeParameter> GenericParameters
        {
            get { return IteratorHelper.GetEmptyEnumerable<IGenericTypeParameter>(); }
        }

        public ushort GenericParameterCount
        {
            get
            {
                // ^ assume false;
                return 0;
            }
        }

        public IGenericTypeInstanceReference InstanceType
        {
            get { return Dummy.GenericTypeInstance; }
        }

        public IEnumerable<ITypeReference> Interfaces
        {
            get { return IteratorHelper.GetEmptyEnumerable<ITypeReference>(); }
        }

        public bool IsAbstract
        {
            get { return false; }
        }

        public bool IsClass
        {
            get { return false; }
        }

        public bool IsDelegate
        {
            get { return false; }
        }

        public bool IsEnum
        {
            get { return false; }
        }

        public bool IsGeneric
        {
            get { return false; }
        }

        public bool IsInterface
        {
            get { return false; }
        }

        public bool IsReferenceType
        {
            get { return false; }
        }

        public bool IsSealed
        {
            get { return true; }
        }

        public bool IsStatic
        {
            get { return true; }
        }

        public bool IsValueType
        {
            get { return false; }
        }

        public bool IsStruct
        {
            get { return false; }
        }

        public IEnumerable<ITypeDefinitionMember> Members
        {
            get { return IteratorHelper.GetEmptyEnumerable<ITypeDefinitionMember>(); }
        }

        public IEnumerable<ITypeDefinitionMember> PrivateHelperMembers
        {
            get { return this.Members; }
        }

        public uint SizeOf
        {
            get { return 0; }
        }

        public IEnumerable<ISecurityAttribute> SecurityAttributes
        {
            get { return IteratorHelper.GetEmptyEnumerable<ISecurityAttribute>(); }
        }

        public ITypeReference UnderlyingType
        {
            get { return Dummy.TypeReference; }
        }

        public PrimitiveTypeCode TypeCode
        {
            get { return PrimitiveTypeCode.Invalid; }
        }

        public IEnumerable<ILocation> Locations
        {
            get { return IteratorHelper.GetEmptyEnumerable<ILocation>(); }
        }

        public LayoutKind Layout
        {
            get { return LayoutKind.Auto; }
        }

        public bool IsSpecialName
        {
            get { return false; }
        }

        public bool IsComObject
        {
            get { return false; }
        }

        public bool IsSerializable
        {
            get { return false; }
        }

        public bool IsBeforeFieldInit
        {
            get { return false; }
        }

        public StringFormatKind StringFormat
        {
            get { return StringFormatKind.Ansi; }
        }

        public bool IsRuntimeSpecial
        {
            get { return false; }
        }

        public bool HasDeclarativeSecurity
        {
            get { return false; }
        }

        #endregion

        #region IDefinition Members

        public IEnumerable<ICustomAttribute> Attributes
        {
            get { return IteratorHelper.GetEmptyEnumerable<ICustomAttribute>(); }
        }

        public void Dispatch(IMetadataVisitor visitor)
        {
        }

        #endregion

        #region IScope<ITypeDefinitionMember> Members

        public bool Contains(ITypeDefinitionMember member)
        {
            return false;
        }

        public IEnumerable<ITypeDefinitionMember> GetMatchingMembersNamed(IName name, bool ignoreCase, Function<ITypeDefinitionMember, bool> predicate)
        {
            return IteratorHelper.GetEmptyEnumerable<ITypeDefinitionMember>();
        }

        public IEnumerable<ITypeDefinitionMember> GetMatchingMembers(Function<ITypeDefinitionMember, bool> predicate)
        {
            return IteratorHelper.GetEmptyEnumerable<ITypeDefinitionMember>();
        }

        public IEnumerable<ITypeDefinitionMember> GetMembersNamed(IName name, bool ignoreCase)
        {
            return IteratorHelper.GetEmptyEnumerable<ITypeDefinitionMember>();
        }

        #endregion

        #region IFunctionPointerTypeReference Members

        #endregion

        #region ITypeReference Members

        public bool IsAlias
        {
            get { return false; }
        }

        public IAliasForType AliasForType
        {
            get { return Dummy.AliasForType; }
        }

        ITypeDefinition ITypeReference.ResolvedType
        {
            get { return this; }
        }

        public uint TypeDefRowId
        {
            get { return 0; }
        }

        #endregion

        IEnumerable<IParameterTypeInformation> IFunctionPointerTypeReference.ExtraArgumentTypes
        {
            get { throw new NotImplementedException(); }
        }

        bool ITypeReference.IsEnum
        {
            get { throw new NotImplementedException(); }
        }

        bool ITypeReference.IsValueType
        {
            get { throw new NotImplementedException(); }
        }

        PrimitiveTypeCode ITypeReference.TypeCode
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

        CallingConvention ISignature.CallingConvention
        {
            get { throw new NotImplementedException(); }
        }

        IEnumerable<IParameterTypeInformation> ISignature.Parameters
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

        ushort ITypeDefinition.Alignment
        {
            get { throw new NotImplementedException(); }
        }

        IEnumerable<ITypeReference> ITypeDefinition.BaseClasses
        {
            get { throw new NotImplementedException(); }
        }

        IEnumerable<IEventDefinition> ITypeDefinition.Events
        {
            get { throw new NotImplementedException(); }
        }

        IEnumerable<IMethodImplementation> ITypeDefinition.ExplicitImplementationOverrides
        {
            get { throw new NotImplementedException(); }
        }

        IEnumerable<IFieldDefinition> ITypeDefinition.Fields
        {
            get { throw new NotImplementedException(); }
        }

        IEnumerable<IGenericTypeParameter> ITypeDefinition.GenericParameters
        {
            get { throw new NotImplementedException(); }
        }

        ushort ITypeDefinition.GenericParameterCount
        {
            get { throw new NotImplementedException(); }
        }

        bool ITypeDefinition.HasDeclarativeSecurity
        {
            get { throw new NotImplementedException(); }
        }

        IEnumerable<ITypeReference> ITypeDefinition.Interfaces
        {
            get { throw new NotImplementedException(); }
        }

        bool ITypeDefinition.IsAbstract
        {
            get { throw new NotImplementedException(); }
        }

        bool ITypeDefinition.IsBeforeFieldInit
        {
            get { throw new NotImplementedException(); }
        }

        bool ITypeDefinition.IsComObject
        {
            get { throw new NotImplementedException(); }
        }

        bool ITypeDefinition.IsGeneric
        {
            get { throw new NotImplementedException(); }
        }

        bool ITypeDefinition.IsInterface
        {
            get { throw new NotImplementedException(); }
        }

        bool ITypeDefinition.IsRuntimeSpecial
        {
            get { throw new NotImplementedException(); }
        }

        bool ITypeDefinition.IsSerializable
        {
            get { throw new NotImplementedException(); }
        }

        bool ITypeDefinition.IsSpecialName
        {
            get { throw new NotImplementedException(); }
        }

        bool ITypeDefinition.IsSealed
        {
            get { throw new NotImplementedException(); }
        }

        LayoutKind ITypeDefinition.Layout
        {
            get { throw new NotImplementedException(); }
        }

        IEnumerable<IMethodDefinition> ITypeDefinition.Methods
        {
            get { throw new NotImplementedException(); }
        }

        IEnumerable<ITypeDefinitionMember> ITypeDefinition.PrivateHelperMembers
        {
            get { throw new NotImplementedException(); }
        }

        IEnumerable<IPropertyDefinition> ITypeDefinition.Properties
        {
            get { throw new NotImplementedException(); }
        }

        IEnumerable<ISecurityAttribute> ITypeDefinition.SecurityAttributes
        {
            get { throw new NotImplementedException(); }
        }

        uint ITypeDefinition.SizeOf
        {
            get { throw new NotImplementedException(); }
        }

        StringFormatKind ITypeDefinition.StringFormat
        {
            get { throw new NotImplementedException(); }
        }

        public bool IsNamespaceTypeReference
        {
            get { throw new NotImplementedException(); }
        }

        public bool IsGenericTypeInstance
        {
            get { throw new NotImplementedException(); }
        }
    }
}