// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.CodeAnalysis.Options;

internal static class LegacyRazorOptions
{
    public static readonly Option2<bool> ForceRuntimeCodeGeneration = new("razor_force_runtime_code_generation", defaultValue: false);
}
