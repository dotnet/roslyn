// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

namespace Microsoft.Cci
{
    [Flags]
    internal enum TypeLibTypeFlags
    {
        FAppObject = 0x0001,
        FCanCreate = 0x0002,
        FLicensed = 0x0004,
        FPreDeclId = 0x0008,
        FHidden = 0x0010,
        FControl = 0x0020,
        FDual = 0x0040,
        FNonExtensible = 0x0080,
        FOleAutomation = 0x0100,
        FRestricted = 0x0200,
        FAggregatable = 0x0400,
        FReplaceable = 0x0800,
        FDispatchable = 0x1000,
        FReverseBind = 0x2000,
    }
}
