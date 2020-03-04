﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Runtime.InteropServices;
using Microsoft.VisualStudio.Setup.Configuration;

namespace Microsoft.CodeAnalysis.MSBuild.UnitTests
{
    internal static class Interop
    {
        private const int REGDG_E_CLASSNOTREG = unchecked((int)0x80040154);

        [DllImport("Microsoft.VisualStudio.Setup.Configuration.Native.dll", ExactSpelling = true, PreserveSig = true)]
        private static extern int GetSetupConfiguration(
            [MarshalAs(UnmanagedType.Interface), Out] out ISetupConfiguration configuration,
            IntPtr reserved);

        public static ISetupConfiguration2 GetSetupConfiguration()
        {
            try
            {
                return new SetupConfiguration();
            }
            catch (COMException ex) when (ex.ErrorCode == REGDG_E_CLASSNOTREG)
            {
                // We could not CoCreate the SetupConfiguration object. If that fails, try p/invoking.
                var hresult = GetSetupConfiguration(out var configuration, IntPtr.Zero);

                if (hresult < 0)
                {
                    throw new COMException($"Failed to get {nameof(ISetupConfiguration)}", hresult);
                }

                return configuration as ISetupConfiguration2;
            }
        }
    }
}
