// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Runtime.InteropServices;

namespace Microsoft.VisualStudio.LanguageServices.CSharp.CodeModel.Interop
{
    [ComImport]
    [InterfaceType(ComInterfaceType.InterfaceIsDual)]
    [Guid("dfcfc5f4-9404-457c-af3b-c116c7bb5c6d")]
    internal interface ICSPartialMethodExtender
    {
        bool IsPartial { get; }
        bool IsDeclaration { get; }
        bool HasOtherPart { get; }
    }
}
