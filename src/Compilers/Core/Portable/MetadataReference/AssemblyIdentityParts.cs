// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

namespace Microsoft.CodeAnalysis
{
    [Flags]
    public enum AssemblyIdentityParts
    {
        Name = 1,
        Version = VersionMajor | VersionMinor | VersionBuild | VersionRevision,

        // version parts are assumed to be in order:
        VersionMajor = 1 << 1,
        VersionMinor = 1 << 2,
        VersionBuild = 1 << 3,
        VersionRevision = 1 << 4,

        Culture = 1 << 5,
        PublicKey = 1 << 6,
        PublicKeyToken = 1 << 7,
        PublicKeyOrToken = PublicKey | PublicKeyToken,
        Retargetability = 1 << 8,
        ContentType = 1 << 9,

        Unknown = 1 << 10
    }
}
