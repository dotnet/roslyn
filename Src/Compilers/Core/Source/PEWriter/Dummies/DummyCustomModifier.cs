using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Microsoft.Cci
{
    internal sealed class DummyCustomModifier : ICustomModifier
    {
        #region ICustomModifier Members

        public bool IsOptional
        {
            get { return false; }
        }

        public ITypeReference Modifier
        {
            get { return Dummy.TypeReference; }
        }

        #endregion
    }
}