// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.Options;

namespace Microsoft.CodeAnalysis.Remote
{
    internal sealed class RemoteHostOptions
    {
        private const string FeatureName = "InternalFeatureOnOffOptions";

        // use 64bit OOP
        public static readonly Option2<bool> OOP64Bit = new(FeatureName, "OOP64Bit", defaultValue: true);

        public static readonly Option2<bool> OOPServerGCFeatureFlag = new(FeatureName, "OOPServerGCFeatureFlag", defaultValue: false);

        // use coreclr host for OOP
        public static readonly Option2<bool> OOPCoreClrFeatureFlag = new(FeatureName, "OOPCoreClrFeatureFlag", defaultValue: false);
    }
}
