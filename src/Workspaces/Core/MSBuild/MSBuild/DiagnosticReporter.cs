// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.MSBuild
{
    internal sealed class DiagnosticReporter
    {
        internal ImmutableList<WorkspaceDiagnostic> Diagnostics;
        private readonly Workspace _workspace;

        public DiagnosticReporter(Workspace workspace)
        {
            _workspace = workspace;
            Diagnostics = [];
        }

        public void Report(DiagnosticReportingMode mode, string message, Func<string, Exception>? createException = null)
        {
            switch (mode)
            {
                case DiagnosticReportingMode.Throw:
                    if (createException is not null)
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
                    throw new ArgumentException(string.Format(WorkspaceMSBuildResources.Invalid_0_specified_1, nameof(DiagnosticReportingMode), nameof(mode)), nameof(mode));
            }
        }

        internal void AddDiagnostic(WorkspaceDiagnostic diagnostic)
        {
            ImmutableInterlocked.Update(ref Diagnostics, (list, d) => list.Add(d), diagnostic);
        }

        public void Report(WorkspaceDiagnostic diagnostic)
        {
            _workspace.OnWorkspaceFailed(diagnostic);
        }

        public void Report(IEnumerable<DiagnosticLogItem> log)
        {
            foreach (var logItem in log)
            {
                Report(DiagnosticReportingMode.Log, GetMSBuildFailedMessage(logItem.ProjectFilePath, logItem.ToString()));
            }
        }

        private static string GetMSBuildFailedMessage(string projectFilePath, string message)
            => RoslynString.IsNullOrWhiteSpace(message)
                ? string.Format(WorkspaceMSBuildResources.Msbuild_failed_when_processing_the_file_0, projectFilePath)
                : string.Format(WorkspaceMSBuildResources.Msbuild_failed_when_processing_the_file_0_with_message_1, projectFilePath, message);
    }
}
