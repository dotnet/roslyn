// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace Microsoft.CodeAnalysis.LanguageServer.HostWorkspace;

internal sealed class VSCodeSettings(JsonElement rootElement)
{
    public static bool TryRead(string settingsFilePath, ILogger logger, [NotNullWhen(true)] out VSCodeSettings? settings)
    {
        if (File.Exists(settingsFilePath))
        {
            try
            {
                var json = File.ReadAllText(settingsFilePath);
                var options = new JsonDocumentOptions
                {
                    AllowTrailingCommas = true,
                    CommentHandling = JsonCommentHandling.Skip,
                };

                using var document = JsonDocument.Parse(json, options);
                if (document.RootElement.ValueKind != JsonValueKind.Object)
                {
                    settings = null;
                    return false;
                }

                settings = new VSCodeSettings(document.RootElement.Clone());
                return true;
            }
            catch (Exception exception)
            {
                logger.LogWarning(exception, "Failed to parse VS Code settings file {SettingsFilePath}.", settingsFilePath);
            }
        }

        settings = null;
        return false;
    }

    public string? TryGetStringSetting(string propertyName)
    {
        if (!rootElement.TryGetProperty(propertyName, out var propertyValue)
            || propertyValue.ValueKind != JsonValueKind.String)
        {
            return null;
        }

        return propertyValue.GetString();
    }

    public static class Names
    {
        public const string DefaultSolution = "dotnet.defaultSolution";
    }
}
