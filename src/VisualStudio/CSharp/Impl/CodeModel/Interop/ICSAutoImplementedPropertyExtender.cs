// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Runtime.InteropServices;

namespace Microsoft.VisualStudio.LanguageServices.CSharp.CodeModel.Interop
{
    [ComImport]
    [InterfaceType(ComInterfaceType.InterfaceIsDual)]
    [Guid("b093257b-fe0c-4302-ad0f-38e276e57619")]
    internal interface ICSAutoImplementedPropertyExtender
    {
        bool IsAutoImplemented { get; }
    }
}
