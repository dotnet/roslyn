// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;

namespace Microsoft.AspNetCore.Razor.Microbenchmarks;

internal static class Helpers
{
    private static string? s_repoRootPath;
    private static string? s_testAppsPath;

    public static string GetRepoRootPath()
    {
        return s_repoRootPath ??= GetRepoRootPathCore();

        static string GetRepoRootPathCore()
        {
            var current = new DirectoryInfo(AppContext.BaseDirectory);
            while (current is not null && !File.Exists(Path.Combine(current.FullName, "Razor.slnx")))
            {
                current = current.Parent;
            }

            return current?.FullName ?? throw new InvalidOperationException("Could not find Razor.slnx");
        }
    }

    public static string GetTestAppsPath()
    {
        return s_testAppsPath ??= Path.Combine(GetRepoRootPath(), "src", "Razor", "benchmarks", "testapps");
    }
}
