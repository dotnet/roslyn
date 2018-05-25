// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
