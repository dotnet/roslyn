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
            Marshal.ThrowExceptionForHR(VSConstants.E_FAIL);

            // never reached...
            return null;
        }

        public static Exception ThrowEInvalidArg()
        {
            Marshal.ThrowExceptionForHR(VSConstants.E_INVALIDARG);

            // never reached...
            return null;
        }

        public static Exception ThrowENotImpl()
        {
            Marshal.ThrowExceptionForHR(VSConstants.E_NOTIMPL);

            // never reached...
            return null;
        }

        public static Exception ThrowEUnexpected()
        {
            Marshal.ThrowExceptionForHR(VSConstants.E_UNEXPECTED);

            // never reached...
            return null;
        }
    }
}
