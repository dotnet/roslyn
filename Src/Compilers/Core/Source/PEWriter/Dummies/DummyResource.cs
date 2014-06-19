using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Microsoft.Cci
{
    internal sealed class DummyResource : IResource
    {
        #region IResource Members

        public IEnumerable<ICustomAttribute> Attributes
        {
            get { return IteratorHelper.GetEmptyEnumerable<ICustomAttribute>(); }
        }

        public IEnumerable<byte> Data
        {
            get { return IteratorHelper.GetEmptyEnumerable<byte>(); }
        }

        public IAssemblyReference DefiningAssembly
        {
            get { return Dummy.AssemblyReference; }
        }

        public bool IsInExternalFile
        {
            get { return false; }
        }

        public IFileReference ExternalFile
        {
            get { return Dummy.FileReference; }
        }

        public bool IsPublic
        {
            get { return false; }
        }

        public string Name
        {
            get { return Dummy.Name; }
        }

        public IResource Resource
        {
            get { return this; }
        }

        #endregion
    }
}