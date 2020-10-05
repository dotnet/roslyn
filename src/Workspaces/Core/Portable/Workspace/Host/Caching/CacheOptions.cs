// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using Microsoft.CodeAnalysis.Options;

namespace Microsoft.CodeAnalysis.Host
{
    internal static class CacheOptions
    {
        internal static readonly Option2<int> RecoverableTreeLengthThreshold = new(nameof(CacheOptions), "RecoverableTreeLengthThreshold", defaultValue: 4096,
            storageLocations: new LocalUserProfileStorageLocation(@"Roslyn\Internal\Performance\Cache\RecoverableTreeLengthThreshold"));
    }
}
