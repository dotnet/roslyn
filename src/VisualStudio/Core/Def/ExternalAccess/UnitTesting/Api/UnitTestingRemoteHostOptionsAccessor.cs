// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Linq;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Remote;

namespace Microsoft.CodeAnalysis.ExternalAccess.UnitTesting.Api
{
    internal static class UnitTestingRemoteHostOptionsAccessor
    {
        public static Option<bool> OOP64Bit => new Option<bool>(
            RemoteHostOptions.OOP64Bit.Feature, RemoteHostOptions.OOP64Bit.Name, defaultValue: RemoteHostOptions.OOP64Bit.DefaultValue,
            storageLocations: RemoteHostOptions.OOP64Bit.StorageLocations.ToArray());
    }
}
