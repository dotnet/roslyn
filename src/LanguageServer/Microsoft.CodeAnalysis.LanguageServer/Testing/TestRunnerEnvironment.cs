// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.CodeAnalysis.LanguageServer.Testing;

internal static class TestRunnerEnvironment
{
    private const string DotnetDiagnosticPortsEnvVar = "DOTNET_DiagnosticPorts";
    private const string DotnetDefaultDiagnosticPortSuspendEnvVar = "DOTNET_DefaultDiagnosticPortSuspend";

    public static Dictionary<string, string?> CreateEnvironmentVariables()
        => CreateEnvironmentVariables(
            Environment.GetEnvironmentVariables().Keys.Cast<string>(),
            Environment.GetEnvironmentVariable("DOTNET_ROOT_USER"),
            Environment.GetEnvironmentVariable(DotnetCliHelper.DotnetRootEnvVar));

    internal static Dictionary<string, string?> CreateEnvironmentVariables(
        IEnumerable<string> environmentVariableNames,
        string? dotnetRootUser,
        string? dotnetRoot)
    {
        var environmentVariables = new Dictionary<string, string?>(StringComparer.Ordinal);

        // Architecture-specific variants can take precedence over DOTNET_ROOT, so clear every inherited
        // variant before selecting one canonical runtime for the test process.
        foreach (var name in environmentVariableNames)
        {
            if (name.StartsWith(DotnetCliHelper.DotnetRootEnvVar, StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(name, DotnetCliHelper.DotnetRootEnvVar, StringComparison.Ordinal))
            {
                environmentVariables[name] = null;
            }
        }

        // DOTNET_ROOT_USER is set by the language server launcher to select or explicitly clear the user's
        // runtime. When it is absent, preserve a directly inherited DOTNET_ROOT such as the one Helix provides.
        environmentVariables[DotnetCliHelper.DotnetRootEnvVar] = dotnetRootUser switch
        {
            null => dotnetRoot ?? string.Empty,
            "" or "EMPTY" => string.Empty,
            _ => dotnetRootUser,
        };

        // Diagnostic ports target the language server process. Inheriting them can suspend a test process
        // before it connects to the test runner.
        environmentVariables[DotnetDiagnosticPortsEnvVar] = null;
        environmentVariables[DotnetDefaultDiagnosticPortSuspendEnvVar] = null;

        return environmentVariables;
    }
}
