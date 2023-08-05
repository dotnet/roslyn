// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Windows.Shapes;
using Microsoft.VisualStudio;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.Utilities
{
    internal static class Exceptions
    {
        public static Exception ThrowEFail([CallerFilePath] string path = null, [CallerLineNumber] int line = 0)
        {
            Marshal.ThrowExceptionForHR(VSConstants.E_FAIL, new IntPtr(-1));
            return new InvalidOperationException($"This program location is thought to be unreachable. File='{path}' Line={line}");
        }

        public static Exception ThrowEInvalidArg([CallerFilePath] string path = null, [CallerLineNumber] int line = 0)
        {
            Marshal.ThrowExceptionForHR(VSConstants.E_INVALIDARG, new IntPtr(-1));
            return new InvalidOperationException($"This program location is thought to be unreachable. File='{path}' Line={line}");
        }

        public static Exception ThrowENotImpl([CallerFilePath] string path = null, [CallerLineNumber] int line = 0)
        {
            Marshal.ThrowExceptionForHR(VSConstants.E_NOTIMPL, new IntPtr(-1));
            return new InvalidOperationException($"This program location is thought to be unreachable. File='{path}' Line={line}");
        }

        public static Exception ThrowEUnexpected([CallerFilePath] string path = null, [CallerLineNumber] int line = 0)
        {
            Marshal.ThrowExceptionForHR(VSConstants.E_UNEXPECTED, new IntPtr(-1));
            return new InvalidOperationException($"This program location is thought to be unreachable. File='{path}' Line={line}");
        }
    }
}
