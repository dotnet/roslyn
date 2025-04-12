// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Runtime.InteropServices;

namespace Microsoft.VisualStudio.LanguageServices.CSharp.ProjectSystemShim.Interop;

[ComImport]
[InterfaceTypeAttribute(ComInterfaceType.InterfaceIsIUnknown)]
[Guid("4D5D4C22-EE19-11d2-B556-00C04F68D4DB")]
internal interface ICSInputSet
{
    ICSCompiler GetCompiler();

    void AddSourceFile([MarshalAs(UnmanagedType.LPWStr)] string filename);
    void RemoveSourceFile([MarshalAs(UnmanagedType.LPWStr)] string filename);

    void RemoveAllSourceFiles();

    void AddResourceFile([MarshalAs(UnmanagedType.LPWStr)] string filename, [MarshalAs(UnmanagedType.LPWStr)] string ident, bool embed, bool vis);
    void RemoveResourceFile([MarshalAs(UnmanagedType.LPWStr)] string filename, [MarshalAs(UnmanagedType.LPWStr)] string ident, bool embed, bool vis);

    void SetWin32Resource([MarshalAs(UnmanagedType.LPWStr)] string filename);

    void SetOutputFileName([MarshalAs(UnmanagedType.LPWStr)] string filename);

    void SetOutputFileType(OutputFileType fileType);

    void SetImageBase(uint imageBase);

    void SetMainClass([MarshalAs(UnmanagedType.LPWStr)] string fullyQualifiedClassName);

    void SetWin32Icon([MarshalAs(UnmanagedType.LPWStr)] string iconFileName);

    void SetFileAlignment(uint align);

    void SetImageBase2(ulong imageBase);

    void SetPdbFileName([MarshalAs(UnmanagedType.LPWStr)] string filename);

    string GetWin32Resource();

    void SetWin32Manifest([MarshalAs(UnmanagedType.LPWStr)] string manifestFileName);
}
