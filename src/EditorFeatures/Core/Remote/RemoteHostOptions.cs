// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.Options;

namespace Microsoft.CodeAnalysis.Remote
{
    internal sealed class RemoteHostOptions
    {
        // use 64bit OOP
        public static readonly Option2<bool> OOP64Bit = new("dotnet_internal_feature_on_off_options_oop_64_bit", defaultValue: true);

        public static readonly Option2<bool> OOPServerGCFeatureFlag = new("dotnet_internal_feature_on_off_options_OOP_server_gc_feature_flag", defaultValue: false);

        // use coreclr host for OOP
        public static readonly Option2<bool> OOPCoreClrFeatureFlag = new("dotnet_internal_feature_on_off_options_oop_core_clr_feature_flag", defaultValue: false);
    }
}
