// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections;
using System.Text;
using Microsoft.Build.Framework;

namespace Microsoft.CodeAnalysis.CompilerServer.UnitTests
{
    internal class MockEngine : IBuildEngine
    {
        public int ColumnNumberOfTaskNode
        {
            get
            {
                throw new NotImplementedException();
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
                throw new NotImplementedException();
            }
        }

        public string ProjectFileOfTaskNode
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public bool BuildProjectFile(string projectFileName, string[] targetNames, IDictionary globalProperties, IDictionary targetOutputs)
        {
            throw new NotImplementedException();
        }

        private readonly StringBuilder _messages = new StringBuilder();
        private readonly StringBuilder _errors = new StringBuilder();
        private readonly StringBuilder _warnings = new StringBuilder();

        public void LogCustomEvent(CustomBuildEventArgs e)
        {
            throw new NotImplementedException();
        }

        public void LogErrorEvent(BuildErrorEventArgs e) => _errors.AppendLine(e.Message);

        public void LogMessageEvent(BuildMessageEventArgs e) => _messages.AppendLine(e.Message);

        public void LogWarningEvent(BuildWarningEventArgs e) => _warnings.AppendLine(e.Message);

        public string Messages => _messages.ToString();
        public string Errors => _errors.ToString();
        public string Warnings => _warnings.ToString();
    }
}
