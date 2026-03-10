// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace Microsoft.CodeAnalysis.LanguageServer.HostWorkspace;

internal sealed class VSCodeSettings
{
    private const string DotnetDefaultSolutionSettingName = "dotnet.defaultSolution";

    public static VSCodeSettings Empty { get; } = new(defaultSolution: null, isDefaultSolutionLoadDisabled: false);

    public string? DefaultSolution { get; }
    public bool IsDefaultSolutionLoadDisabled { get; }

    private VSCodeSettings(string? defaultSolution, bool isDefaultSolutionLoadDisabled)
    {
        DefaultSolution = defaultSolution;
        IsDefaultSolutionLoadDisabled = isDefaultSolutionLoadDisabled;
    }

    public static VSCodeSettings Read(string settingsFilePath, ILogger logger)
    {
        if (!File.Exists(settingsFilePath))
        {
            return Empty;
        }

        try
        {
            return Parse(File.ReadAllText(settingsFilePath));
        }
        catch (Exception exception)
        {
            logger.LogWarning(exception, "Failed to parse VS Code settings file {SettingsFilePath}.", settingsFilePath);
            return Empty;
        }
    }

    public string? ResolveDefaultSolutionPath(string workspaceFolderPath)
    {
        if (IsDefaultSolutionLoadDisabled || string.IsNullOrEmpty(DefaultSolution))
        {
            return null;
        }

        return Path.IsPathRooted(DefaultSolution)
            ? DefaultSolution
            : Path.GetFullPath(Path.Combine(workspaceFolderPath, DefaultSolution));
    }

    internal static VSCodeSettings Parse(string json)
    {
        var options = new JsonDocumentOptions
        {
            AllowTrailingCommas = true,
            CommentHandling = JsonCommentHandling.Skip,
        };

        using var document = JsonDocument.Parse(json, options);
        if (document.RootElement.ValueKind != JsonValueKind.Object)
        {
            return Empty;
        }

        var defaultSolution = TryGetStringSetting(document.RootElement, DotnetDefaultSolutionSettingName);

        if (string.Equals(defaultSolution, "disable", StringComparison.Ordinal))
        {
            return new(defaultSolution: null, isDefaultSolutionLoadDisabled: true);
        }

        return string.IsNullOrEmpty(defaultSolution)
            ? Empty
            : new(defaultSolution, isDefaultSolutionLoadDisabled: false);
    }

    private static string? TryGetStringSetting(JsonElement root, string propertyName)
    {
        if (!root.TryGetProperty(propertyName, out var propertyValue) || propertyValue.ValueKind != JsonValueKind.String)
        {
            return null;
        }

        return propertyValue.GetString();
    }
}
