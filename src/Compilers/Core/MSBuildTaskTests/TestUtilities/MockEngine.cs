// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using Microsoft.Build.Framework;
using Xunit.Abstractions;

namespace Microsoft.CodeAnalysis.BuildTasks.UnitTests
{
    internal sealed class MockEngine : IBuildEngine
    {
        private readonly ITestOutputHelper? _testOutputHelper;
        private StringBuilder _log = new StringBuilder();
        public MessageImportance MinimumMessageImportance = MessageImportance.Low;
        public List<BuildMessageEventArgs> BuildMessages = new List<BuildMessageEventArgs>();

        internal string Log
        {
            set { _log = new StringBuilder(value); }
            get { return _log.ToString(); }
        }

        public void LogErrorEvent(BuildErrorEventArgs eventArgs)
        {
            var msg = $"ERROR {eventArgs.Code}: {eventArgs.Message}";
            _testOutputHelper?.WriteLine(msg);
            _log.AppendLine(msg);
        }

        public void LogWarningEvent(BuildWarningEventArgs eventArgs)
        {
            var msg = $"WARNING {eventArgs.Code}: {eventArgs.Message}";
            _testOutputHelper?.WriteLine(msg);
            _log.AppendLine(msg);
        }

        public void LogCustomEvent(CustomBuildEventArgs eventArgs)
        {
            _testOutputHelper?.WriteLine(eventArgs.Message);
            _log.AppendLine(eventArgs.Message);
        }

        public void LogMessageEvent(BuildMessageEventArgs eventArgs)
        {
            _testOutputHelper?.WriteLine(eventArgs.Message);
            _log.AppendLine(eventArgs.Message);
            BuildMessages.Add(eventArgs);
        }

        public string ProjectFileOfTaskNode => "";
        public int ColumnNumberOfTaskNode => 0;
        public int LineNumberOfTaskNode => 0;
        public bool ContinueOnError => true;

        public MockEngine(ITestOutputHelper? testOutputHelper = null)
        {
            _testOutputHelper = testOutputHelper;
        }

        public bool BuildProjectFile(string projectFileName, string[] targetNames, IDictionary globalProperties, IDictionary targetOutputs)
            => throw new NotImplementedException();
    }
}
