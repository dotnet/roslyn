// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Runtime.InteropServices;

namespace Microsoft.VisualStudio.LanguageServices.CSharp.CodeModel.Interop
{
    [ComImport]
    [InterfaceType(ComInterfaceType.InterfaceIsDual)]
    [Guid("72551468-315b-47c6-92e2-d20b3b92dc94")]
    internal interface ICSCodeTypeLocation
    {
        string ExternalLocation { get; }
    }
}
