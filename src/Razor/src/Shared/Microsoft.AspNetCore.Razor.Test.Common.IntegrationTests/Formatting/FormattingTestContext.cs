// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Razor.Test.Common;

public sealed class FormattingTestContext
{
    public required bool ShouldFlipLineEndings { get; init; }

    public required bool CreatedByFormattingDiscoverer { get; init; }

    public static string FlipLineEndings(string input)
    {
        if (string.IsNullOrEmpty(input))
        {
            return input;
        }

        var hasCRLF = input.Contains("\r\n");
        var hasLF = !hasCRLF && input.Contains("\n");

        if (hasCRLF)
        {
            return input.Replace("\r\n", "\n");
        }
        else if (hasLF)
        {
            return input.Replace("\n", "\r\n");
        }

        return input;
    }
}
