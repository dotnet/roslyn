// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using Microsoft.AspNetCore.Razor.Utilities;
using Microsoft.CodeAnalysis.Razor.Utilities;
using Xunit;

namespace Microsoft.AspNetCore.Razor.Test.Common;

public static class TestPathUtilities
{
    public static string CreateRootedPath(params string[] parts)
    {
        var result = Path.Combine(parts);

        if (!Path.IsPathRooted(result))
        {
            result = PlatformInformation.IsWindows
                ? @"C:\" + result
                : "/" + result;
        }

        return result;
    }

    public static Uri GetUri(params string[] parts)
    {
        return new($"{Uri.UriSchemeFile}{Uri.SchemeDelimiter}{Path.Combine(parts)}");
    }

    public static void AssertEquivalent(string? expectedFilePath, string? actualFilePath)
    {
        Assert.True(FilePathNormalizer.AreFilePathsEquivalent(expectedFilePath, actualFilePath));
    }
}
