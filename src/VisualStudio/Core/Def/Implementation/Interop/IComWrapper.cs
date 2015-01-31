// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Runtime.InteropServices;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.Interop
{
    [ComImport]
    [Guid("CBD71F2C-6BC5-4932-B851-B93EB3151386")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    internal interface IComWrapper
    {
        IntPtr GCHandlePtr { get; }
    }
}
