// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;

namespace Microsoft.AspNetCore.Razor.Microbenchmarks;

internal static class Helpers
{
    private static string? s_projectRootPath;
    private static string? s_testAppsPath;

    private static string GetProjectRootPath()
    {
        return s_projectRootPath ??= GetProjectRootPathCore();

        static string GetProjectRootPathCore()
        {
            var current = new DirectoryInfo(AppContext.BaseDirectory);
            while (current is not null && !File.Exists(Path.Combine(current.FullName, "Microsoft.AspNetCore.Razor.Microbenchmarks.csproj")))
            {
                current = current.Parent;
            }

            return current?.FullName ?? throw new InvalidOperationException("Could not find Microsoft.AspNetCore.Razor.Microbenchmarks.csproj");
        }
    }

    public static string GetTestAppsPath()
    {
        return s_testAppsPath ??= Path.Combine(GetProjectRootPath(), "testapps");
    }
}
