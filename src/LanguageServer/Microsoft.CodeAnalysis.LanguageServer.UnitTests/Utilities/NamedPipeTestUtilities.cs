// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.CodeAnalysis.LanguageServer.UnitTests;

internal static class NamedPipeTestUtilities
{
    private const int UniquePipeSuffixLength = 12;

    internal static string CreateShortPipeName(string prefix = "")
    {
        ArgumentNullException.ThrowIfNull(prefix);

        // Keep the name comfortably below Unix domain socket path limits when Helix temp paths are prefixed.
        return prefix + Guid.NewGuid().ToString("N")[..UniquePipeSuffixLength];
    }
}
