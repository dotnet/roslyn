// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.CodeAnalysis.LanguageServer.Testing;

internal static class TestRunnerEnvironment
{
    public static Dictionary<string, string?> CreateEnvironmentVariables()
        => CreateEnvironmentVariables(
            Environment.GetEnvironmentVariables().Keys.Cast<string>(),
            Environment.GetEnvironmentVariable("DOTNET_ROOT_USER"));

    internal static Dictionary<string, string?> CreateEnvironmentVariables(
        IEnumerable<string> environmentVariableNames,
        string? dotnetRootUser)
    {
        var environmentVariables = new Dictionary<string, string?>(StringComparer.Ordinal);

        foreach (var name in environmentVariableNames)
        {
            if (name.StartsWith(DotnetCliHelper.DotnetRootEnvVar, StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(name, DotnetCliHelper.DotnetRootEnvVar, StringComparison.Ordinal))
            {
                environmentVariables[name] = null;
            }
        }

        environmentVariables[DotnetCliHelper.DotnetRootEnvVar] =
            string.IsNullOrEmpty(dotnetRootUser) || dotnetRootUser == "EMPTY" ? string.Empty : dotnetRootUser;

        return environmentVariables;
    }
}
