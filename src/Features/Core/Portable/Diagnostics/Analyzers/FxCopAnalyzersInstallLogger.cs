// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.Internal.Log;

namespace Microsoft.CodeAnalysis.Diagnostics.Analyzers
{
    internal static class FxCopAnalyzersInstallLogger
    {
        private const string Name = "FxCopAnalyzersInstall";

        public static void Log(string action)
        {
            Logger.Log(FunctionId.FxCopAnalyzersInstall, KeyValueLogMessage.Create(LogType.UserAction, m =>
            {
                m[nameof(Name)] = Name;
                m[nameof(action)] = action;
            }));
        }

        public static void LogVsixInstallationStatus(Workspace workspace, FxCopAnalyzersInstallStatus installStatus)
        {
            var installed = workspace.Options.GetOption(FxCopAnalyzersInstallOptions.VsixInstalled);
            if (!installed && installStatus == FxCopAnalyzersInstallStatus.Installed)
            {
                // first time after vsix installed
                workspace.Options = workspace.Options.WithChangedOption(FxCopAnalyzersInstallOptions.VsixInstalled, true);
                Log("VsixInstalled");
            }

            if (installed && installStatus == FxCopAnalyzersInstallStatus.NotInstalled)
            {
                // first time after vsix is uninstalled
                workspace.Options = workspace.Options.WithChangedOption(FxCopAnalyzersInstallOptions.VsixInstalled, false);
                Log("VsixUninstalled");
            }
        }

        public static void LogCandidacyRequirementsTracking(long lastTriggeredTimeBinary)
        {
            if (lastTriggeredTimeBinary == FxCopAnalyzersInstallOptions.LastDateTimeUsedSuggestionAction.DefaultValue)
            {
                Log("StartCandidacyRequirementsTracking");
            }
        }
    }
}
