// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;

namespace Microsoft.CodeAnalysis.Options;

internal static class LegacyRazorOptions
{
    public static readonly Option2<bool> ForceRuntimeCodeGeneration = new("razor_force_runtime_code_generation", defaultValue: false);

    internal static ImmutableArray<Lazy<IDynamicFileInfoProvider, FileExtensionsMetadata>> FilterDynamicFileInfoProviders(
        IGlobalOptionService globalOptions,
        IEnumerable<Lazy<IDynamicFileInfoProvider, FileExtensionsMetadata>> dynamicFileInfoProviders)
    {
        if (globalOptions.GetOption(ForceRuntimeCodeGeneration))
        {
            dynamicFileInfoProviders = dynamicFileInfoProviders.Where(p => !p.Metadata.Extensions.Contains("razor"));
        }

        return [.. dynamicFileInfoProviders];
    }
}
