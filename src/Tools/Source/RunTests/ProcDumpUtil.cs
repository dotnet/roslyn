// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Security.Principal;
using Microsoft.Win32;

namespace RunTests
{
    internal static class DumpUtil
    {
#pragma warning disable CA1416 // Validate platform compatibility
        internal static void EnableRegistryDumpCollection(string dumpDirectory)
        {
            Debug.Assert(IsAdministrator());

            using var registryKey = Registry.LocalMachine.CreateSubKey(@"SOFTWARE\Microsoft\Windows\Windows Error Reporting\LocalDumps", writable: true);
            registryKey.SetValue("DumpType", 2, RegistryValueKind.DWord);
            registryKey.SetValue("DumpCount", 2, RegistryValueKind.DWord);
            registryKey.SetValue("DumpFolder", dumpDirectory, RegistryValueKind.String);
        }

        internal static void DisableRegistryDumpCollection()
        {
            Debug.Assert(IsAdministrator());

            using var registryKey = Registry.LocalMachine.CreateSubKey(@"SOFTWARE\Microsoft\Windows\Windows Error Reporting\LocalDumps", writable: true);
            registryKey.DeleteValue("DumpType", throwOnMissingValue: false);
            registryKey.DeleteValue("DumpCount", throwOnMissingValue: false);
            registryKey.DeleteValue("DumpFolder", throwOnMissingValue: false);
        }

        internal static bool IsAdministrator()
        {
            using var identity = WindowsIdentity.GetCurrent();
            WindowsPrincipal principal = new WindowsPrincipal(identity);
            return principal.IsInRole(WindowsBuiltInRole.Administrator);
        }
#pragma warning restore CA1416 // Validate platform compatibility
    }
}
