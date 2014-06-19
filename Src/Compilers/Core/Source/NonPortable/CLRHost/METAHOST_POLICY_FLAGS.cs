// Copyright (c) Microsoft Corporation.  All rights reserved.

namespace Microsoft.Runtime.Hosting.Interop
{
    using System;

    [System.Security.SecurityCritical]
    [Flags]
    internal enum MetaHostPolicyFlags
    {
        HighCompatibility  = 0x00,
        ApplyUpgradePolicy = 0x08,
        EmulateExeLaunch   = 0x10,
        ShowErrorDialog    = 0x20,
        UseProcessImagePath = 0x40,
        EnsureSkuSupported = 0x80
    }

    [System.Security.SecurityCritical]
    [Flags]
    internal enum MetaHostConfigFlags
    {
        LegacyV2ActivationPolicyUnset  = 0x00000000,
        LegacyV2ActivationPolicyTrue   = 0x00000001,
        LegacyV2ActivationPolicyFalse  = 0x00000002,
        LegacyV2ActivationPolicyMask   = 0x00000003,
    }
}

