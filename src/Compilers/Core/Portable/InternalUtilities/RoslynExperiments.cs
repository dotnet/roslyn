// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.CodeAnalysis;

/// <summary>
/// IDs of currently active experimental APIs.
/// </summary>
internal static class RoslynExperiments
{
    /// <summary>
    /// Nullable disabled semantic model.
    /// </summary>
    internal const string RSEXPERIMENTAL001 = nameof(RSEXPERIMENTAL001);
    internal const string RSEXPERIMENTAL001_Url = "https://github.com/dotnet/roslyn/issues/70609";
}
