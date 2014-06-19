using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Microsoft.Cci
{
    internal sealed class DummyParameterDefinition : IParameterDefinition
    {
        #region IParameterDefinition Members

        public ISignature ContainingSignature
        {
            get { return Dummy.Method; }
        }

        public IMetadataConstant DefaultValue
        {
            get { return Dummy.Constant; }
        }

        public bool HasDefaultValue
        {
            get { return false; }
        }

        public bool IsIn
        {
            get { return false; }
        }

        public bool IsMarshalledExplicitly
        {
            get { return false; }
        }

        public bool IsOptional
        {
            get { return false; }
        }

        public bool IsOut
        {
            get { return false; }
        }

        public bool IsParameterArray
        {
            get { return false; }
        }

        public IMarshallingInformation MarshallingInformation
        {
            get { return Dummy.MarshallingInformation; }
        }

        public ITypeReference ParamArrayElementType
        {
            get { return Dummy.TypeReference; }
        }

        public IEnumerable<ILocation> Locations
        {
            get { return IteratorHelper.GetEmptyEnumerable<ILocation>(); }
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

        #region INamedEntity Members

        public string Name
        {
            get { return Dummy.Name; }
        }

        #endregion

        #region IParameterListEntry Members

        public ushort Index
        {
            get { return 0; }
        }

        #endregion

        #region IParameterTypeInformation Members

        public IEnumerable<ICustomModifier> CustomModifiers
        {
            get { return IteratorHelper.GetEmptyEnumerable<ICustomModifier>(); }
        }

        public bool IsByReference
        {
            get { return false; }
        }

        public bool IsModified
        {
            get { return false; }
        }

        public ITypeReference Type
        {
            get { return Dummy.TypeReference; }
        }

        #endregion

        #region IMetadataConstantContainer

        public IMetadataConstant Constant
        {
            get { return Dummy.Constant; }
        }

        #endregion
    }
}