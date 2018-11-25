// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Microsoft.CodeAnalysis.MSBuild.Logging;

namespace Microsoft.CodeAnalysis.MSBuild
{
    internal class DiagnosticReporter
    {
        private readonly Workspace _workspace;

        public DiagnosticReporter(Workspace workspace)
        {
            _workspace = workspace;
        }

        public void Report(DiagnosticReportingMode mode, string message, Func<string, Exception> createException = null)
        {
            switch (mode)
            {
                case DiagnosticReportingMode.Throw:
                    if (createException != null)
                    {
                        throw createException(message);
                    }

                    throw new InvalidOperationException(message);

                case DiagnosticReportingMode.Log:
                    Report(new WorkspaceDiagnostic(WorkspaceDiagnosticKind.Failure, message));
                    break;

                case DiagnosticReportingMode.Ignore:
                    break;

                default:
                    throw new ArgumentException($"Invalid {nameof(DiagnosticReportingMode)} specified: {mode}", nameof(mode));
            }
        }

        public void Report(WorkspaceDiagnostic diagnostic)
        {
            _workspace.OnWorkspaceFailed(diagnostic);
        }

        public void Report(DiagnosticLog log)
        {
            foreach (var logItem in log)
            {
                Report(DiagnosticReportingMode.Log, GetMSBuildFailedMessage(logItem.ProjectFilePath, logItem.ToString()));
            }
        }

        private static string GetMSBuildFailedMessage(string projectFilePath, string message)
            => string.IsNullOrWhiteSpace(message)
                ? string.Format(WorkspaceMSBuildResources.Msbuild_failed_when_processing_the_file_0, projectFilePath)
                : string.Format(WorkspaceMSBuildResources.Msbuild_failed_when_processing_the_file_0_with_message_1, projectFilePath, message);
    }
}
