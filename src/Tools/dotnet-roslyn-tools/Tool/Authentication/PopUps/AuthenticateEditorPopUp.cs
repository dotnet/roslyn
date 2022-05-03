// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Logging;
using System.Collections.ObjectModel;

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
            Contents = new ReadOnlyCollection<Line>(new List<Line>
            {
                new Line("Create new GitHub personal access tokens at https://github.com/settings/tokens (repo_public scopes needed)", isComment: true),
                new Line($"{githubTokenElement}={GetCurrentSettingForDisplay(Settings.GitHubToken, string.Empty, isSecret: true)}"),
                new Line("Create new DevDiv Azure Dev Ops tokens at https://dev.azure.com/devdiv/_usersSettings/tokens (build_execute,code_full,release_execute,packaging scopes are needed)", isComment: true),
                new Line($"{devdivAzureDevOpsTokenElement}={GetCurrentSettingForDisplay(Settings.DevDivAzureDevOpsToken, string.Empty, isSecret: true)}"),
                new Line("Create new DncEng Azure Dev Ops tokens at https://dev.azure.com/devdiv/_usersSettings/tokens (build_execute,code_full,release_execute,packaging scopes are needed)", isComment: true),
                new Line($"{dncengAzureDevOpsTokenElement}={GetCurrentSettingForDisplay(Settings.DncEngAzureDevOpsToken, string.Empty, isSecret: true)}"),
                new Line(""),
                new Line("Set elements above before saving.", isComment: true),
            });
        }

        public LocalSettings Settings { get; set; }

        public override int ProcessContents(IList<Line> contents)
        {
            foreach (Line line in contents)
            {
                string[] keyValue = line.Text.Split("=");

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
