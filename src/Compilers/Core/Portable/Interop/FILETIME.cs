// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Runtime.InteropServices;

namespace Microsoft.CodeAnalysis.Interop
{
#pragma warning disable RS0016 // Add public types and members to the declared API
    [StructLayout(LayoutKind.Sequential)]

    public struct FILETIME
    {
        public int dwLowDateTime;
        public int dwHighDateTime;
    }
#pragma warning restore RS0016 // Add public types and members to the declared API

}
