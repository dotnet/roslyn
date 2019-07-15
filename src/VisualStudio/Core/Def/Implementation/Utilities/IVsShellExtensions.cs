// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
