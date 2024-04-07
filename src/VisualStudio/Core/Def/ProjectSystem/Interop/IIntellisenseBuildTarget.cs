// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Runtime.InteropServices;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem.Interop;

[ComImport]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
[Guid("F304ABA7-2448-4EE9-A706-D1838F200398")]
internal interface IIntellisenseBuildTarget
{
    // currently, reason is not being used.
    void SetIntellisenseBuildResult(bool succeeded, [MarshalAs(UnmanagedType.LPWStr)] string reason);
}
