// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.Options;

namespace Microsoft.CodeAnalysis.Remote;

internal sealed class RemoteHostOptionsStorage
{
    // use 64bit OOP
    public static readonly Option2<bool> OOP64Bit = new("dotnet_code_analysis_in_separate_process", defaultValue: true);

    // use coreclr host for OOP
    public static readonly Option2<bool> OOPCoreClr = new("dotnet_enable_core_clr_in_code_analysis_process", defaultValue: true);

    public static readonly Option2<bool> OOPServerGCFeatureFlag = new("dotnet_enable_server_garbage_collection_in_code_analysis_process", defaultValue: false);
}
