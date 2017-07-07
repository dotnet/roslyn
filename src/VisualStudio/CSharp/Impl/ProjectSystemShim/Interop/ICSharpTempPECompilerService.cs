// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Runtime.InteropServices;

namespace Microsoft.VisualStudio.LanguageServices.CSharp.ProjectSystemShim.Interop
{
    [ComImport]
    [Guid("DBA64C84-56DF-4E20-8AA6-02332A97F474")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    internal interface ICSharpTempPECompilerService
    {
        [PreserveSig]
        int CompileTempPE(
            [MarshalAs(UnmanagedType.LPWStr)] string pszOutputFileName,
            int sourceCount,
            [MarshalAs(UnmanagedType.LPArray, ArraySubType = UnmanagedType.LPWStr, SizeParamIndex = 1)] string[] fileNames,
            [MarshalAs(UnmanagedType.LPArray, ArraySubType = UnmanagedType.LPWStr, SizeParamIndex = 1)] string[] fileContents,
            int optionCount,
            [MarshalAs(UnmanagedType.LPArray, ArraySubType = UnmanagedType.LPWStr, SizeParamIndex = 4)] string[] optionNames,
            [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 4)] object[] optionValues);
    }
}
