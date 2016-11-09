// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Runtime.InteropServices;
using Microsoft.VisualStudio.Setup.Configuration;

namespace Roslyn.Test.Utilities
{
    internal static class Interop
    {
        public const int REGDB_E_CLASSNOTREG = unchecked((int)0x80040154);

        [DllImport("Microsoft.VisualStudio.Setup.Configuration.Native.dll")]
        public static extern void GetSetupConfiguration([MarshalAs(UnmanagedType.Interface)] out ISetupConfiguration setupConfiguration);
    }
}
