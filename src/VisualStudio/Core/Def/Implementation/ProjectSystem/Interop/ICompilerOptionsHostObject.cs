// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Runtime.InteropServices;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem.Interop
{
    /// <summary>
    /// An internal redefinition of ICompilerOptionsHostObject from Microsoft.CodeAnalysis.BuildTasks. We cannot take
    /// a binary dependency on the build task because no component in Visual Studio may do so -- we cannot rely that any
    /// specific version of the build task is present since the customer may have a NuGet package installed that contains
    /// different versions. Since this a COM interface, it's easiest to redefine.
    /// </summary>
    [ComImport]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    [Guid("117CC9AD-299A-4898-AAFD-8ADE0FE0A1EF")]
    internal interface ICompilerOptionsHostObject
    {
        [PreserveSig]
        int SetCompilerOptions([MarshalAs(UnmanagedType.BStr)] string compilerOptions, [MarshalAs(UnmanagedType.VariantBool)] out bool supported);
    }
}
