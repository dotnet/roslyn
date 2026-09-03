// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics.CodeAnalysis;

namespace Microsoft.CodeAnalysis;

/// <summary>
/// Memory safety rules version used by a module. See <see cref="IModuleSymbol.MemorySafetyRulesVersion"/> for more details.
/// </summary>
[Experimental(RoslynExperiments.PreviewLanguageFeatureApi, UrlFormat = "https://github.com/dotnet/roslyn/issues/82789")]
public enum MemorySafetyRulesVersion
{
    /// <summary>Legacy rules.</summary>
    [Experimental(RoslynExperiments.PreviewLanguageFeatureApi, UrlFormat = "https://github.com/dotnet/roslyn/issues/82789")]
    Version1 = 1,

    /// <summary>Updated rules introduced with the "unsafe evolution" language feature.</summary>
    [Experimental(RoslynExperiments.PreviewLanguageFeatureApi, UrlFormat = "https://github.com/dotnet/roslyn/issues/82789")]
    Version2 = 2,
}

internal static class MemorySafetyRulesVersionExtensions
{
    public static bool IsValid(this MemorySafetyRulesVersion version)
    {
        return version is MemorySafetyRulesVersion.Version1 or MemorySafetyRulesVersion.Version2;
    }
}
