// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Runtime.InteropServices;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.CodeModel.Interop
{
    [ComVisible(true)]
    [Guid("3078feac-b063-3f25-9796-bb0cbbc88980")]
    public enum PropertyTypeEnum
    {
        ReadOnly = 0,
        WriteOnly,
        ReadWrite
    }
}
