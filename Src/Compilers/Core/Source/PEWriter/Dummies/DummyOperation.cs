using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Microsoft.Cci
{
    internal sealed class DummyOperation : IOperation
    {
        #region IOperation Members

        public Roslyn.Compilers.Internal.ILOpCode OperationCode
        {
            get { return Roslyn.Compilers.Internal.ILOpCode.Nop; }
        }

        public uint Offset
        {
            get { return 0; }
        }

        public ILocation Location
        {
            get { return Dummy.Location; }
        }

        public object/*?*/ Value
        {
            get { return null; }
        }

        #endregion
    }
}