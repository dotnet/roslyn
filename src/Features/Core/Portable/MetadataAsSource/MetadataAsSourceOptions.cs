// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.CodeCleanup;
using Microsoft.CodeAnalysis.CodeGeneration;
using Microsoft.CodeAnalysis.Host;

namespace Microsoft.CodeAnalysis.MetadataAsSource;

/// <summary>
/// Options for metadata as source navigation
/// </summary>
/// <param name="GenerationOptions">Options to use to prettify the generated document.</param>
/// <param name="NavigateToDecompiledSources"><see langword="false"/> to disallow decompiling code, which may
/// result in signagures only being returned if there is no other non-decompilation option available</param>
/// <param name="AlwaysUseDefaultSymbolServers">Whether navigation should try to use the default Microsoft and
/// Nuget symbol servers regardless of debugger settings</param>
internal readonly record struct MetadataAsSourceOptions(
    CleanCodeGenerationOptions GenerationOptions,
    bool NavigateToDecompiledSources = true,
    bool AlwaysUseDefaultSymbolServers = true)
{
    public static MetadataAsSourceOptions GetDefault(HostLanguageServices languageServices)
        => new(GenerationOptions: CleanCodeGenerationOptions.GetDefault(languageServices));
}
