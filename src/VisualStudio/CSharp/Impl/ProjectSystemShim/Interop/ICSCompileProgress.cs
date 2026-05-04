// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Runtime.InteropServices;

namespace Microsoft.VisualStudio.LanguageServices.CSharp.ProjectSystemShim.Interop;

[ComImport]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
[Guid("93615958-1FA4-4d6a-AA14-CB3B9A6B08AC")]
internal interface ICSCompileProgress
{
    // members not ported
}
