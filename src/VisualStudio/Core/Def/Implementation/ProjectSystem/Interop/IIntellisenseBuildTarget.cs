// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Runtime.InteropServices;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem.Interop
{
    [ComImport]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    [Guid("F304ABA7-2448-4EE9-A706-D1838F200398")]
    internal interface IIntellisenseBuildTarget
    {
        // currently, reason is not being used.
        void SetIntellisenseBuildResult(bool succeeded, [MarshalAs(UnmanagedType.LPWStr)] string reason);
    }
}
