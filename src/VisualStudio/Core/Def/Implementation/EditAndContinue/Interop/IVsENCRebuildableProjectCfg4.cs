// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.EditAndContinue.Interop
{
    [ComImport]
    [Guid("64F7CF8B-8800-4DD7-B472-2090C90DDB64")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    internal interface IVsENCRebuildableProjectCfg4
    {
        [PreserveSig]
        int HasCustomMetadataEmitter([MarshalAs(UnmanagedType.VariantBool)] out bool value);
    }
}
