using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Microsoft.Cci
{
    internal sealed class DummyParameterTypeInformation : IParameterTypeInformation
    {
        #region IParameterTypeInformation Members

        public ISignature ContainingSignature
        {
            get { return Dummy.Method; }
        }

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

        #region IParameterListEntry Members

        public ushort Index
        {
            get { return 0; }
        }

        #endregion
    }
}