// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.Options;
using Microsoft.VisualStudio.LanguageServices.Remote;

namespace Microsoft.CodeAnalysis.ExternalAccess.UnitTesting.Api
{
    internal static class UnitTestingRemoteHostOptionsAccessor
    {
        private const string LocalRegistryPath = @"Roslyn\Internal\RemoteServices\";

        public static Option<bool> OOP64Bit => new Option<bool>(
            nameof(RemoteHostOptions), nameof(OOP64Bit), defaultValue: false,
            storageLocations: new LocalUserProfileStorageLocation(LocalRegistryPath + nameof(OOP64Bit)));
    }
}
