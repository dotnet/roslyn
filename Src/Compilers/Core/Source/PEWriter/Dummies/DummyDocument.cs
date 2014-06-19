using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Microsoft.Cci
{
    internal sealed class DummyDocument : IDocument
    {
        #region IDocument Members

        public string Location
        {
            get { return string.Empty; }
        }

        public string Name
        {
            get { return Dummy.Name; }
        }

        #endregion
    }
}