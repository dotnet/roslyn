// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Runtime.InteropServices;
using Microsoft.VisualStudio;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.Utilities;

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
