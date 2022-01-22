// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Runtime.InteropServices;

namespace Microsoft.VisualStudio.LanguageServices.CSharp.ProjectSystemShim.Interop
{
    [ComImport]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    [Guid("4D5D4C21-EE19-11d2-B556-00C04F68D4DB")]
    internal interface ICSCompilerConfig
    {
        /// <summary>
        /// Return the number of options available.
        /// </summary>
        int GetOptionCount();

        /// <summary>
        /// Return info about the given option.
        /// </summary>
        void GetOptionInfoAt(int index,
                             out CompilerOptions optionID,
                             [MarshalAs(UnmanagedType.LPWStr)] out string switchName,
                             [MarshalAs(UnmanagedType.LPWStr)] out string switchDescription,
                             out uint flags);

        void GetOptionInfoAtEx(int index,
                               out CompilerOptions optionID,
                               [MarshalAs(UnmanagedType.LPWStr)] out string shortSwitchName,
                               [MarshalAs(UnmanagedType.LPWStr)] out string longSwitchName,
                               [MarshalAs(UnmanagedType.LPWStr)] out string descriptiveSwitchName,
                               out string switchDescription,
                               out uint flags);

        void ResetAllOptions();

        [PreserveSig]
        [return: MarshalAs(UnmanagedType.Error)]
        int SetOption(CompilerOptions optionID, HACK_VariantStructure value);
        void GetOption(CompilerOptions optionID, IntPtr variant);

        /// <summary>
        /// Commit changes to the options, validating first. If the configuration as it is currently is invalid, S_FALSE
        /// is returned, and error is populated with an error describing the problem.
        /// </summary>
        [PreserveSig]
        int CommitChanges(ref ICSError error);

        ICSCompiler GetCompiler();

        IntPtr GetWarnNumbers(out int count);

        [return: MarshalAs(UnmanagedType.LPWStr)]
        string GetWarnInfo(int warnIndex);
    }
}
