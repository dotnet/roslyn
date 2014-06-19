using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Microsoft.Cci
{
    internal sealed class DummySectionBlock : ISectionBlock
    {
        #region ISectionBlock Members

        public PESectionKind PESectionKind
        {
            get { return PESectionKind.Illegal; }
        }

        public uint Offset
        {
            get { return 0; }
        }

        public uint Size
        {
            get { return 0; }
        }

        public IEnumerable<byte> Data
        {
            get { return IteratorHelper.GetEmptyEnumerable<byte>(); }
        }

        #endregion
    }
}