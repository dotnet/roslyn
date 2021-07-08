// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using Roslyn.Utilities;
using MSB = Microsoft.Build;

namespace Microsoft.CodeAnalysis.MSBuild.Logging
{
    internal class MSBuildDiagnosticLogger : MSB.Framework.ILogger
    {
        private string? _projectFilePath;
        private DiagnosticLog? _log;
        private MSB.Framework.IEventSource? _eventSource;

        public string? Parameters { get; set; }
        public MSB.Framework.LoggerVerbosity Verbosity { get; set; }

        public void SetProjectAndLog(string projectFilePath, DiagnosticLog log)
        {
            _projectFilePath = projectFilePath;
            _log = log;
        }

        private void OnErrorRaised(object sender, MSB.Framework.BuildErrorEventArgs e)
        {
            RoslynDebug.AssertNotNull(_projectFilePath);
            _log?.Add(new MSBuildDiagnosticLogItem(WorkspaceDiagnosticKind.Failure, _projectFilePath, e.Message, e.File, e.LineNumber, e.ColumnNumber));
        }

        private void OnWarningRaised(object sender, MSB.Framework.BuildWarningEventArgs e)
        {
            RoslynDebug.AssertNotNull(_projectFilePath);
            _log?.Add(new MSBuildDiagnosticLogItem(WorkspaceDiagnosticKind.Warning, _projectFilePath, e.Message, e.File, e.LineNumber, e.ColumnNumber));
        }

        public void Initialize(MSB.Framework.IEventSource eventSource)
        {
            Debug.Assert(_eventSource == null);

            _eventSource = eventSource;
            _eventSource.ErrorRaised += OnErrorRaised;
            _eventSource.WarningRaised += OnWarningRaised;
        }

        public void Shutdown()
        {
            if (_eventSource != null)
            {
                _eventSource.ErrorRaised -= OnErrorRaised;
                _eventSource.WarningRaised -= OnWarningRaised;

                _eventSource = null;

                _projectFilePath = null;
                _log = null;
            }
        }
    }
}
