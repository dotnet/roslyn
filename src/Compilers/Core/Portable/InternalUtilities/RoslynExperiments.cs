// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.CodeAnalysis;

/// <summary>
/// Defines diagnostic info for Roslyn experimental APIs.
/// </summary>
internal static class RoslynExperiments
{
    internal const string NullableDisabledSemanticModel = "RSEXPERIMENTAL001";
    internal const string NullableDisabledSemanticModel_Url = "https://github.com/dotnet/roslyn/issues/70609";

    internal const string Interceptors = "RSEXPERIMENTAL002";
    internal const string Interceptors_Url = "https://github.com/dotnet/csharplang/issues/7009";

    internal const string SyntaxTokenParser = "RSEXPERIMENTAL003";
    internal const string SyntaxTokenParser_Url = "https://github.com/dotnet/roslyn/issues/73002";
}
