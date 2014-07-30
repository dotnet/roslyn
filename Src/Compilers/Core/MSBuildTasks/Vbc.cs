// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.Threading;
using Microsoft.Build.Framework;
using Microsoft.CodeAnalysis.CompilerServer;

namespace Microsoft.CodeAnalysis.BuildTasks
{
    /// <summary>
    /// The replacement "Vbc" build task. It must be named "Vbc".
    /// 
    /// Eventually this should migrate into the MSBuild build tasks.
    /// </summary>
    /// <remarks>
    /// Debugging MSBuild Tasks is somewhat difficult. There are two general approaches I have found
    /// useful. 
    /// 
    /// 1. There is logging support built in to the task and the compiler server it talks to.
    ///    Set the environment variable: RoslynCommandLineLogFile to the name of a file to log
    ///    to. You can then look at the log to figure out what might be going on.
    /// 2. Use "Debugger.Launch" below. This line will launch the debugger, which
    ///    then allows you to debug the task.
    /// </remarks>
    public class Vbc : Microsoft.Build.Tasks.Vbc
    {
        private int exitCode = 0;

        private readonly CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();

        // The preview buildtask needs to work with VS 2013 which does not have this property and VS 2014 which does
        // In the preview we just hide the property if it already exists with a pragma
#pragma warning disable 0108
        public string VsSessionGuid { get; set; }
#pragma warning restore 0108

        protected override bool UseAlternateCommandLineToolToExecute()
        {
            // Roslyn MSBuild task does not support using host object for compilation
            return true;
        }

        /// <summary>
        /// Cancel the in-process build task.
        /// </summary>
        public override void Cancel()
        {
            base.Cancel();
            cancellationTokenSource.Cancel();
        }

        /// <summary> 
        /// Construct the command line from the task properties
        /// </summary> 
        /// <returns></returns> 
        protected override string GenerateCommandLineCommands()
        {
            return TaskUtilities.AppendSessionGuidUnlessDesignTime(base.GenerateCommandLineCommands(), this.VsSessionGuid, this);
        }

        /// <summary>
        /// The return code of the compilation. Strangely, this isn't overridable from ToolTask, so we need 
        /// to create our own.
        /// </summary>
        [Output]
        public new int ExitCode
        {
            get
            {
                return exitCode;
            }
        }

        protected override int ExecuteTool(string pathToTool, string responseFileCommands, string commandLineCommands)
        {
            Log.LogMessage(MessageImportance.Normal, "Using shared compiler process to build.");

            BuildRequest req = TaskUtilities.CreateRequest(
                BuildProtocolConstants.RequestId_VisualBasicCompile,
                this.GetWorkingDirectory(),
                this.EnvironmentVariables,
                this.GenerateCommandLineCommands(),
                this.GenerateResponseFileCommands());

            if (req == null)
            {
                this.exitCode = -1;
                LogMessages("Fatal Error: more than " + ushort.MaxValue + " command line arguments.",
                    this.StandardErrorImportanceToUse);
            }
            else
            {
                bool canceled;
                string output;
                string errorOutput;
                this.exitCode = TaskUtilities.ExecuteTool(req, cancellationTokenSource.Token, out canceled, out output, out errorOutput);
                if (!canceled && exitCode < 0)
                {
                    Debug.Assert(output == null);
                    Debug.Assert(errorOutput == null);

                    // For some reason, this error message is not logged through logMessages.
                    Log.LogError("Fatal Error: Please see the event log for more details.");
                }
                else
                {
                    if (output != null) LogMessages(output, this.StandardOutputImportanceToUse);

                    if (errorOutput != null)
                    {
                        if (LogStandardErrorAsError)
                        {
                            LogErrorOutput(errorOutput);
                        }
                        else
                        {
                            LogMessages(errorOutput, this.StandardErrorImportanceToUse);
                        }
                    }
                }
            }

            return this.exitCode;

        }

        private void LogErrorOutput(string output)
        {
            string[] lines = output.Split(new char[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (string line in lines)
            {
                string trimmedMessage = line.Trim();
                if (trimmedMessage != "")
                {
                    Log.LogError(trimmedMessage);
                }
            }
        }

        /// <summary>
        /// Log each of the messages in the given output with the given importants.
        /// We assume each line is a message to log.
        /// </summary>
        private void LogMessages(string output, MessageImportance messageImportance)
        {
            string[] lines = output.Split(new string[] { "\r\n" }, StringSplitOptions.None);
            foreach (string line in lines)
            {
                //Code below will parse the set of four lines that comprise a VB
                //error message into a single object. The four-line format contains
                //a second line that is blank. This must be passed to the code below
                //to satisfy the parser. The parser needs to work with output from
                //old compilers as well. 
                LogEventsFromTextOutput(line, messageImportance);
            }
        }
    }
}
