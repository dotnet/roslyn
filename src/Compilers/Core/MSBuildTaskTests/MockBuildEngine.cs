// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using Microsoft.Build.Framework;

namespace Microsoft.CodeAnalysis.BuildTasks.UnitTests
{
    internal class MockBuildEngine : IBuildEngine
    {
        public int ColumnNumberOfTaskNode
        {
            get
            {
                return 1;
            }
        }

        public bool ContinueOnError
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public int LineNumberOfTaskNode
        {
            get
            {
                return 1;
            }
        }

        public string ProjectFileOfTaskNode
        {
            get
            {
                return "Project.csproj";
            }
        }

        public bool BuildProjectFile(string projectFileName, string[] targetNames, IDictionary globalProperties, IDictionary targetOutputs)
        {
            throw new NotImplementedException();
        }

        private readonly List<string> _messages = new List<string>();
        private readonly List<string> _errors = new List<string>();
        private readonly List<string> _warnings = new List<string>();

        public void LogCustomEvent(CustomBuildEventArgs e)
        {
            throw new NotImplementedException();
        }

        public void LogErrorEvent(BuildErrorEventArgs e) => _errors.Add(e.Message);

        public void LogMessageEvent(BuildMessageEventArgs e) => _messages.Add(e.Message);

        public void LogWarningEvent(BuildWarningEventArgs e) => _warnings.Add(e.Message);

        public IList<string> Messages => _messages;
        public IList<string> Errors => _errors;
        public IList<string> Warnings => _warnings;
    }
}
