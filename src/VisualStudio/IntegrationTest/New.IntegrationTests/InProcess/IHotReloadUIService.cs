// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Runtime.InteropServices;

namespace Roslyn.VisualStudio.NewIntegrationTests.InProcess;

[ComImport]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
[Guid("4F6E5436-F098-45FF-B85F-7AE940DFCA44")]
internal interface IHotReloadUIService
{
    int ShowHotReloadDialog(bool isManaged, EditAndContinueResult result, [In][MarshalAs(UnmanagedType.BStr)] string errorMessage);
}
