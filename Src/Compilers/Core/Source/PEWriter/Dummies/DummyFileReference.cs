using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Microsoft.Cci
{
    internal sealed class DummyFileReference : IFileReference
    {
        #region IFileReference Members

        public IAssembly ContainingAssembly
        {
            get { return Dummy.Assembly; }
        }

        public bool HasMetadata
        {
            get { return false; }
        }

        public string FileName
        {
            get { return Dummy.Name; }
        }

        public IEnumerable<byte> HashValue
        {
            get { return IteratorHelper.GetEmptyEnumerable<byte>(); }
        }

        #endregion
    }
}