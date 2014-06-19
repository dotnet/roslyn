using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Microsoft.Cci
{
    internal sealed class DummyName : IName
    {
        #region IName Members

        public int UniqueKey
        {
            get { return 1; }
        }

        public int UniqueKeyIgnoringCase
        {
            get { return 1; }
        }

        public string Value
        {
            get { return string.Empty; }
        }

        #endregion
    }
}