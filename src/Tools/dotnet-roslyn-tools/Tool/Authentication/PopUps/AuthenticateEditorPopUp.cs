// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.ObjectModel;
using Microsoft.Extensions.Logging;

namespace Microsoft.RoslynTools.Authentication.PopUps;

internal class AuthenticateEditorPopUp : EditorPopUp
{
    private readonly ILogger _logger;

    private const string GithubTokenElement = "github_token";
    private const string DevdivAzureDevOpsTokenElement = "devdiv_azdo_token";
    private const string DncengAzureDevOpsTokenElement = "dnceng_azdo_token";

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
            new("GitHub authentication", isComment: true),
            new("=====================", isComment: true),
            new("- (Recommended) Install GitHub CLI (https://cli.github.com/manual/), run `gh auth login`, and leave this empty", isComment: true),
            new("- (Alternative) Create a fine-grained personal access token at https://github.com/settings/personal-access-tokens", isComment: true),
            new("  - Choose `dotnet` as the resource owner", isComment: true),
            new("  - Enable SSO for organizations where repository access is needed (typically dotnet and microsoft)", isComment: true),
            new("- (Not recommended) Leave empty without GitHub CLI authentication; GitHub rate limits will be very low", isComment: true),
            new($"{GithubTokenElement}={GetCurrentSettingForDisplay(Settings.GitHubToken, string.Empty, isSecret: true)}"),
            new(string.Empty),
            new("Azure DevOps authentication", isComment: true),
            new("===========================", isComment: true),
            new("- (Recommended) Leave empty and darc will sign you in via a browser or device code auth flow", isComment: true),
            new("- (Alternative) Create a PAT with the `Build.Execute` and `Code.Write` scopes", isComment: true),
            new("- (Alternative) Use the PatGeneratorTool https://dev.azure.com/dnceng/public/_artifacts/feed/dotnet-eng/NuGet/Microsoft.DncEng.PatGeneratorTool", isComment: true),
            new("  - Run `dotnet pat-generator --scopes build_execute code_manage release_execute packaging --organizations <dnceng, devdiv> --expires-in 7`", isComment: true),
            new("  - Token lasts 7 days", isComment: true),
            new($"{DevdivAzureDevOpsTokenElement}={GetCurrentSettingForDisplay(Settings.DevDivAzureDevOpsToken, string.Empty, true)}"),
            new($"{DncengAzureDevOpsTokenElement}={GetCurrentSettingForDisplay(Settings.DncEngAzureDevOpsToken, string.Empty, true)}"),
            new(string.Empty),
            new("Set elements above before saving.", isComment: true),
        ]);
    }

    public LocalSettings Settings { get; set; }

    public override int ProcessContents(IList<Line> contents)
    {
        foreach (var line in contents)
        {
            var keyValue = line.Text.Split('=');

            switch (keyValue[0])
            {
                case GithubTokenElement:
                    Settings.GitHubToken = ParseSetting(keyValue[1], Settings.GitHubToken, isSecret: true);
                    break;
                case DevdivAzureDevOpsTokenElement:
                    Settings.DevDivAzureDevOpsToken = ParseSetting(keyValue[1], Settings.DevDivAzureDevOpsToken, isSecret: true);
                    break;
                case DncengAzureDevOpsTokenElement:
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
