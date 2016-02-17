// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.Build.Execution;
using Microsoft.Build.Logging;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem.Interop;
using Roslyn.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem
{
    internal partial class AbstractProject : IIntellisenseBuildTarget
    {
        private static readonly object s_diagnosticKey = new object();
        private static readonly FieldInfo s_fieldInfo = typeof(BuildManager).GetField("_buildParameters", BindingFlags.GetField | BindingFlags.Instance | BindingFlags.NonPublic);

        // REVIEW: can temp path changed while process running?
        private static readonly string GeneralLogFile = $"{Path.GetTempPath()}\\<guid>.designtime.log";

        // set default to true so that we maintain old behavior when project system doesn't
        // implement IIntellisenseBuildTarget
        private bool _intellisenseBuildSucceeded = true;
        private string _intellisenseBuildFailureReason = null;

        public void SetIntellisenseBuildResult(bool succeeded, string reason)
        {
            // set intellisense related info
            _intellisenseBuildSucceeded = succeeded;
            _intellisenseBuildFailureReason = string.IsNullOrWhiteSpace(reason) ? null : reason.Trim();

            UpdateHostDiagnostics(succeeded, reason);

            if (_pushingChangesToWorkspaceHosts)
            {
                // set workspace reference info
                ProjectTracker.NotifyWorkspaceHosts(host => (host as IVisualStudioWorkspaceHost2)?.OnHasAllInformation(Id, succeeded));
            }
        }

        private void UpdateHostDiagnostics(bool succeeded, string reason)
        {
            if (!succeeded)
            {
                // report intellisense build failure to error list
                this.HostDiagnosticUpdateSource?.UpdateDiagnosticsForProject(
                    _id, s_diagnosticKey, SpecializedCollections.SingletonEnumerable(CreateIntellisenseBuildFailureDiagnostic(reason)));
            }
            else
            {
                // clear intellisense build failure diagnostic from error list.
                this.HostDiagnosticUpdateSource?.ClearDiagnosticsForProject(_id, s_diagnosticKey);
            }
        }

        private DiagnosticData CreateIntellisenseBuildFailureDiagnostic(string reason)
        {
            // log intellisense build failure
            Logger.Log(FunctionId.IntellisenseBuild_Failed, KeyValueLogMessage.Create(m => m["Reason"] = reason ?? string.Empty));

            return new DiagnosticData(
                IDEDiagnosticIds.IntellisenseBuildFailedDiagnosticId,
                FeaturesResources.ErrorCategory,
                ServicesVSResources.IntellisenseBuildFailedMessage,
                ServicesVSResources.ResourceManager.GetString(nameof(ServicesVSResources.IntellisenseBuildFailedMessage), CodeAnalysis.Diagnostics.Extensions.s_USCultureInfo),
                DiagnosticSeverity.Warning,
                isEnabledByDefault: true,
                warningLevel: 0,
                workspace: Workspace,
                projectId: Id,
                title: ServicesVSResources.IntellisenseBuildFailedTitle,
                description: GetDescription(reason),
                helpLink: "http://go.microsoft.com/fwlink/p/?LinkID=734719");
        }

        private string GetDescription(string reason)
        {
            var logFilePath = GetLogFile();
            var logFileDescription = string.Format(
                    logFilePath == null ? ServicesVSResources.IntellisenseBuildFailedDescription : ServicesVSResources.IntellisenseBuildFailedDescriptionWithLog,
                    logFilePath ?? GeneralLogFile);

            if (string.IsNullOrWhiteSpace(reason))
            {
                return logFileDescription;
            }

            return string.Join(Environment.NewLine, logFileDescription, string.Empty, ServicesVSResources.IntellisenseBuildFailedDescriptionExtra, reason);
        }

        private string GetLogFile()
        {
            var manager = BuildManager.DefaultBuildManager;
            if (manager == null)
            {
                return null;
            }

            var buildParameters = s_fieldInfo.GetValue(manager) as BuildParameters;
            if (buildParameters == null)
            {
                return null;
            }

            var fileLogger = buildParameters.Loggers?.OfType<FileLogger>()?.FirstOrDefault();
            if (fileLogger == null || fileLogger.Parameters == null)
            {
                return null;
            }

            const string logFile = "logfile=";
            const string designTime = ".designtime.log";

            var startIndex = fileLogger.Parameters.IndexOf(logFile, StringComparison.OrdinalIgnoreCase);
            var endIndex = fileLogger.Parameters.IndexOf(designTime, StringComparison.OrdinalIgnoreCase);
            if (startIndex < 0 || endIndex < 0 || endIndex <= startIndex + logFile.Length)
            {
                return null;
            }

            var logFilename = fileLogger.Parameters.Substring(startIndex + logFile.Length, endIndex - startIndex - logFile.Length) + designTime;
            if (!File.Exists(logFilename))
            {
                return null;
            }

            return logFilename;
        }
    }
}
