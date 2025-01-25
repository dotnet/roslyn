// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.ObjectModel;
using Microsoft.Extensions.Logging;

namespace Microsoft.RoslynTools.Authentication.PopUps
{
    internal class AuthenticateEditorPopUp : EditorPopUp
    {
        private readonly ILogger _logger;

        private const string githubTokenElement = "github_token";
        private const string devdivAzureDevOpsTokenElement = "devdiv_azdo_token";
        private const string dncengAzureDevOpsTokenElement = "dnceng_azdo_token";

        public AuthenticateEditorPopUp(string path, ILogger logger)
            : base(path)
        {
            _logger = logger;
            try
            {
                // Load current settings
                Settings = LocalSettings.LoadSettingsFile();
            }
            catch (Exception e)
            {
                // Failed to load the settings file.  Quite possible it just doesn't exist.
                // In this case, just initialize the settings to empty
                _logger.LogTrace("Couldn't load or locate the settings file ({Message}).  Initializing empty settings file", e.Message);
                Settings = new LocalSettings();
            }

            // Initialize line contents.
            Contents = new ReadOnlyCollection<Line>(
            [
                new("Create new GitHub personal access tokens at https://github.com/settings/tokens (repo_public scopes needed)", isComment: true),
                new($"{githubTokenElement}={GetCurrentSettingForDisplay(Settings.GitHubToken, string.Empty, isSecret: true)}"),
                new(string.Empty),
                new("[OPTIONAL]", isComment: true),
                new("Set Azure DevOps tokens (or leave empty to use local credentials)", isComment: true),
                new(string.Empty),
                new("Use the PatGeneratorTool https://dev.azure.com/dnceng/public/_artifacts/feed/dotnet-eng/NuGet/Microsoft.DncEng.PatGeneratorTool", isComment: true),
                new("with the `dotnet pat-generator --scopes build_execute code_full release_execute packaging --organizations devdiv --expires-in 7` command", isComment: true),
                new($"{devdivAzureDevOpsTokenElement}={GetCurrentSettingForDisplay(Settings.DevDivAzureDevOpsToken, string.Empty, true)}"),
                new(string.Empty),
                new("Use the PatGeneratorTool https://dev.azure.com/dnceng/public/_artifacts/feed/dotnet-eng/NuGet/Microsoft.DncEng.PatGeneratorTool", isComment: true),
                new("with the `dotnet pat-generator --scopes build_execute code_full release_execute packaging --organizations dnceng --expires-in 7` command", isComment: true),
                new($"{dncengAzureDevOpsTokenElement}={GetCurrentSettingForDisplay(Settings.DncEngAzureDevOpsToken, string.Empty, true)}"),
                new(string.Empty),
                new("Set elements above before saving.", isComment: true),
            ]);
        }

        public LocalSettings Settings { get; set; }

        public override int ProcessContents(IList<Line> contents)
        {
            foreach (var line in contents)
            {
                var keyValue = line.Text.Split("=");

                switch (keyValue[0])
                {
                    case githubTokenElement:
                        Settings.GitHubToken = ParseSetting(keyValue[1], Settings.GitHubToken, isSecret: true);
                        break;
                    case devdivAzureDevOpsTokenElement:
                        Settings.DevDivAzureDevOpsToken = ParseSetting(keyValue[1], Settings.DevDivAzureDevOpsToken, isSecret: true);
                        break;
                    case dncengAzureDevOpsTokenElement:
                        Settings.DncEngAzureDevOpsToken = ParseSetting(keyValue[1], Settings.DncEngAzureDevOpsToken, isSecret: true);
                        break;
                    default:
                        _logger.LogWarning("'{SettingName}' is an unknown field in the authentication scope", keyValue[0]);
                        break;
                }
            }

            return Settings.SaveSettingsFile(_logger);
        }
    }
}
