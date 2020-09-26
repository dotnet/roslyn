// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.Options;

namespace Microsoft.CodeAnalysis.Experimentation
{
    internal static class KeybindingResetOptions
    {
        private const string LocalRegistryPath = @"Roslyn\Internal\KeybindingsStatus\";

        public static readonly Option<ReSharperStatus> ReSharperStatus = new(nameof(KeybindingResetOptions),
            nameof(ReSharperStatus), defaultValue: Experimentation.ReSharperStatus.NotInstalledOrDisabled,
            storageLocations: new LocalUserProfileStorageLocation(LocalRegistryPath + nameof(ReSharperStatus)));

        public static readonly Option<bool> NeedsReset = new(nameof(KeybindingResetOptions),
            nameof(NeedsReset), defaultValue: false,
            storageLocations: new LocalUserProfileStorageLocation(LocalRegistryPath + nameof(NeedsReset)));

        public static readonly Option<bool> NeverShowAgain = new(nameof(KeybindingResetOptions),
            nameof(NeverShowAgain), defaultValue: false,
            storageLocations: new LocalUserProfileStorageLocation(LocalRegistryPath + nameof(NeverShowAgain)));
    }
}
