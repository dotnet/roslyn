// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Runtime.InteropServices;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.CodeModel.Interop;

[ComVisible(true)]
[Guid("3078feac-b063-3f25-9796-bb0cbbc88980")]
public enum PropertyTypeEnum
{
    ReadOnly = 0,
    WriteOnly,
    ReadWrite
}
