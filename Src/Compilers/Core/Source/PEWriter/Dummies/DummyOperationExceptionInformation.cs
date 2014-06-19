using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Microsoft.Cci
{
    internal sealed class DummyOperationExceptionInformation : IOperationExceptionInformation
    {
        #region IOperationExceptionInformation Members

        public HandlerKind HandlerKind
        {
            get { return HandlerKind.Illegal; }
        }

        public ITypeReference ExceptionType
        {
            get { return Dummy.TypeReference; }
        }

        public uint TryStartOffset
        {
            get { return 0; }
        }

        public uint TryEndOffset
        {
            get { return 0; }
        }

        public uint FilterDecisionStartOffset
        {
            get { return 0; }
        }

        public uint HandlerStartOffset
        {
            get { return 0; }
        }

        public uint HandlerEndOffset
        {
            get { return 0; }
        }

        #endregion
    }
}