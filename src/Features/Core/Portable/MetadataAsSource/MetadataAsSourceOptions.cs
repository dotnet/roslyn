// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.Serialization;
using Microsoft.CodeAnalysis.CodeGeneration;
using Microsoft.CodeAnalysis.Host;

namespace Microsoft.CodeAnalysis.MetadataAsSource;

/// <summary>
/// Options for metadata as source navigation
/// </summary>
/// <param name="GenerationOptions">Options to use to prettify the generated document.</param>
[DataContract]
internal readonly record struct MetadataAsSourceOptions(
    [property: DataMember] CleanCodeGenerationOptions GenerationOptions)
{
    /// <summary>
    /// <see langword="false"/> to disallow decompiling code, which may
    /// result in signagures only being returned if there is no other non-decompilation option available
    /// </summary>
    [DataMember]
    public bool NavigateToDecompiledSources { get; init; } = true;

    /// <summary>
    /// Whether navigation should try to use the default Microsoft and
    /// Nuget symbol servers regardless of debugger settings
    /// </summary>
    [DataMember]
    public bool AlwaysUseDefaultSymbolServers { get; init; } = true;

    /// <summary>
    /// <see langword="false"/> to disallow downloading PDBs and trying to find source from
    /// Source Link or embedded source.
    /// </summary>
    [DataMember]
    public bool NavigateToSourceLinkAndEmbeddedSources { get; init; } = true;

    public static MetadataAsSourceOptions GetDefault(LanguageServices languageServices)
        => new(GenerationOptions: CleanCodeGenerationOptions.GetDefault(languageServices));
}
