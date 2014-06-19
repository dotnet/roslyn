using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Microsoft.Cci
{
    internal sealed class DummyMethodReference : IMethodReference
    {
        #region IMethodReference Members

        public bool AcceptsExtraArguments
        {
            get { return false; }
        }

        public ushort GenericParameterCount
        {
            get
            {
                // ^ assume false;
                return 0;
            }
        }

        public uint InternedKey
        {
            get { return 0; }
        }

        public bool IsGeneric
        {
            get { return false; }
        }

        public ushort ParameterCount
        {
            get { return 0; }
        }

        public IMethodDefinition ResolvedMethod
        {
            get
            {
                // ^ assume false;
                return Dummy.Method;
            }
        }

        public IEnumerable<IParameterTypeInformation> ExtraParameters
        {
            get { return IteratorHelper.GetEmptyEnumerable<IParameterTypeInformation>(); }
        }

        #endregion

        #region ISignature Members

        public CallingConvention CallingConvention
        {
            get { return CallingConvention.C; }
        }

        public IEnumerable<IParameterTypeInformation> Parameters
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

        #region ITypeMemberReference Members

        public ITypeReference ContainingType
        {
            get { return Dummy.TypeReference; }
        }

        public ITypeDefinitionMember ResolvedTypeDefinitionMember
        {
            get { return Dummy.Method; }
        }

        #endregion

        #region IReference Members

        public IEnumerable<ICustomAttribute> Attributes
        {
            get { return IteratorHelper.GetEmptyEnumerable<ICustomAttribute>(); }
        }

        public IEnumerable<ILocation> Locations
        {
            get { return IteratorHelper.GetEmptyEnumerable<ILocation>(); }
        }

        public void Dispatch(IMetadataVisitor visitor)
        {
        }

        #endregion

        #region INamedEntity Members

        public string Name
        {
            get { return Dummy.Name; }
        }

        #endregion
    }
}