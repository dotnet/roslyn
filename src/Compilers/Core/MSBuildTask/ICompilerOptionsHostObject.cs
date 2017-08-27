// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Runtime.InteropServices;

namespace Microsoft.CodeAnalysis.BuildTasks
{
    [ComImport]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    [Guid("117CC9AD-299A-4898-AAFD-8ADE0FE0A1EF")]
    public interface ICompilerOptionsHostObject
    {
        bool SetCompilerOptions([MarshalAs(UnmanagedType.BStr)] string compilerOptions);
    }
}
