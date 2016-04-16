// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Runtime.InteropServices;
using Microsoft.VisualStudio;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.Utilities
{
    internal static class Exceptions
    {
        public static Exception ThrowEFail()
        {
            Marshal.ThrowExceptionForHR(VSConstants.E_FAIL, new IntPtr(-1));

            // never reached...
            return null;
        }

        public static Exception ThrowEInvalidArg()
        {
            Marshal.ThrowExceptionForHR(VSConstants.E_INVALIDARG, new IntPtr(-1));

            // never reached...
            return null;
        }

        public static Exception ThrowENotImpl()
        {
            Marshal.ThrowExceptionForHR(VSConstants.E_NOTIMPL, new IntPtr(-1));

            // never reached...
            return null;
        }

        public static Exception ThrowEUnexpected()
        {
            Marshal.ThrowExceptionForHR(VSConstants.E_UNEXPECTED, new IntPtr(-1));

            // never reached...
            return null;
        }
    }
}
