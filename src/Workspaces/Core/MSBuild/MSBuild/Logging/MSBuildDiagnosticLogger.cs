// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Diagnostics;
using MSB = Microsoft.Build;

namespace Microsoft.CodeAnalysis.MSBuild.Logging
{
    internal class MSBuildDiagnosticLogger : MSB.Framework.ILogger
    {
        private string _projectFilePath;
        private DiagnosticLog _log;
        private MSB.Framework.IEventSource _eventSource;

        public string Parameters { get; set; }
        public MSB.Framework.LoggerVerbosity Verbosity { get; set; }

        public void SetProjectAndLog(string projectFilePath, DiagnosticLog log)
        {
            _projectFilePath = projectFilePath;
            _log = log;
        }

        private void OnErrorRaised(object sender, MSB.Framework.BuildErrorEventArgs e)
        {
            _log?.Add(new MSBuildDiagnosticLogItem(WorkspaceDiagnosticKind.Failure, _projectFilePath, e.Message, e.File, e.LineNumber, e.ColumnNumber));
        }

        private void OnWarningRaised(object sender, MSB.Framework.BuildWarningEventArgs e)
        {
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
