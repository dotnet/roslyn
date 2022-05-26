// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections;
using System.Reflection;
using System.Text;
using Microsoft.Build.Framework;

namespace Microsoft.CodeAnalysis.BuildTasks.UnitTests
{
    /// <summary>
    /// An engine to output event messages as MSBuild does to test Vbc.ParseVBErrorOrWarning.
    /// </summary>
    internal sealed class ErrorLoggingEngine : IBuildEngine
    {
        private StringBuilder _log = new StringBuilder();
        public MessageImportance MinimumMessageImportance = MessageImportance.Low;
        private readonly MethodInfo _formatErrorMethod;
        private readonly MethodInfo _formatWarningMethod;

        public ErrorLoggingEngine()
        {
            // Use the formatting from Microsoft.Build.Shared.EventArgsFormatting.
            var assembly = Assembly.LoadFrom("Microsoft.Build.dll");
            var formattingClass = assembly.GetType("Microsoft.Build.Shared.EventArgsFormatting") ?? throw new Exception("Could not find EventArgsFormatting type");
            _formatErrorMethod = formattingClass.GetMethod("FormatEventMessage", BindingFlags.Static | BindingFlags.NonPublic, null, CallingConventions.Any,
                new Type[] { typeof(BuildErrorEventArgs) }, null) ?? throw new Exception("Could not find FormatEventMessage(BuildErrorEventArgs).");
            _formatWarningMethod = formattingClass.GetMethod("FormatEventMessage", BindingFlags.Static | BindingFlags.NonPublic, null, CallingConventions.Any,
                new Type[] { typeof(BuildWarningEventArgs) }, null) ?? throw new Exception("Could not find FormatEventMessage(BuildWarningEventArgs).");
        }

        internal string Log
        {
            set { _log = new StringBuilder(value); }
            get { return _log.ToString(); }
        }

        public void LogErrorEvent(BuildErrorEventArgs eventArgs)
        {
            _log.Append(_formatErrorMethod.Invoke(null, new object[] { eventArgs }));
            _log.AppendLine();
        }

        public void LogWarningEvent(BuildWarningEventArgs eventArgs)
        {
            _log.Append(_formatWarningMethod.Invoke(null, new object[] { eventArgs }));
            _log.AppendLine();
        }

        public void LogCustomEvent(CustomBuildEventArgs eventArgs)
        {
        }

        public void LogMessageEvent(BuildMessageEventArgs eventArgs)
        {
        }

        public string ProjectFileOfTaskNode => "";
        public int ColumnNumberOfTaskNode => 0;
        public int LineNumberOfTaskNode => 0;
        public bool ContinueOnError => true;

        public bool BuildProjectFile(string projectFileName, string[] targetNames, IDictionary globalProperties, IDictionary targetOutputs)
            => throw new NotImplementedException();

    }
}
