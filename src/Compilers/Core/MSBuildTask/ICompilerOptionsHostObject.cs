﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

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
