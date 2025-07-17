// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Runtime.InteropServices;

namespace Microsoft.VisualStudio.LanguageServices.CSharp.CodeModel.Interop;

[ComImport]
[InterfaceType(ComInterfaceType.InterfaceIsDual)]
[Guid("b093257b-fe0c-4302-ad0f-38e276e57619")]
internal interface ICSAutoImplementedPropertyExtender
{
    bool IsAutoImplemented { get; }
}
