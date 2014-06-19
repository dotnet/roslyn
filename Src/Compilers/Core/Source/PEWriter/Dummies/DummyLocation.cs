using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Microsoft.Cci
{
    internal sealed class DummyLocation : ILocation
    {
        #region ILocation Members

        public IDocument Document
        {
            get { return Dummy.Document; }
        }

        #endregion

        public uint Offset
        {
            get { throw new NotImplementedException(); }
        }
    }
}