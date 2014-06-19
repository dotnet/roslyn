using System;
using System.Collections.Generic;

namespace Microsoft.Cci
{
    internal enum ErrorCode
    {
        PDBUsingNameTooLong
    }

    internal struct Diagnostic
    {
        public readonly ErrorCode ErrorCode;
        public readonly object[] Arguments;

        public Diagnostic(ErrorCode errorCode, params string[] arguments)
        {
            this.ErrorCode = errorCode;
            this.Arguments = arguments;
        }
    }
}
