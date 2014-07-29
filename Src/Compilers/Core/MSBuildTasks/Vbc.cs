// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Threading;
using Microsoft.Build.Framework;

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
        /// Construct the command line from the task properties
        /// </summary> 
        /// <returns></returns> 
        protected override string GenerateCommandLineCommands()
        {
            return TaskUtilities.AppendSessionGuidUnlessDesignTime(base.GenerateCommandLineCommands(), this.VsSessionGuid, this);
        }

        protected override string ToolName
        {
            get
            {
                return "vbc2.exe";
            }
        }
    }
}
