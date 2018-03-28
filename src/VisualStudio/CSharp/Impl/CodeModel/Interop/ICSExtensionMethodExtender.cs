// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Runtime.InteropServices;

namespace Microsoft.VisualStudio.LanguageServices.CSharp.CodeModel.Interop
{
    [ComImport]
    [InterfaceType(ComInterfaceType.InterfaceIsDual)]
    [Guid("f82170cc-efe8-4f5e-8209-bc2c27b3f54d")]
    internal interface ICSExtensionMethodExtender
    {
        bool IsExtension { get; }
    }
}
