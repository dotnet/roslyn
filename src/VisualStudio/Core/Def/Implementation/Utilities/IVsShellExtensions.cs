// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Microsoft.VisualStudio.Shell.Interop;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.Utilities
{
    internal static class IVsShellExtensions
    {
        public static bool TryGetPropertyValue(this IVsShell shell, __VSSPROPID id, out IntPtr value)
        {
            var hresult = shell.GetProperty((int)id, out var objValue);
            if (ErrorHandler.Succeeded(hresult) && objValue != null)
            {
                value = (IntPtr.Size == 4) ? (IntPtr)(int)objValue : (IntPtr)(long)objValue;
                return true;
            }

            value = default;
            return false;
        }
    }
}
