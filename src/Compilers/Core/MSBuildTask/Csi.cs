// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Text;

using Microsoft.Build.Framework;
using Microsoft.CodeAnalysis.CSharp;

namespace Microsoft.CodeAnalysis.BuildTasks
{
    public class Csi : InteractiveCompiler
    {
        #region Properties - Please keep these alphabetized.

        // These are the parameters specific to Csi.
        // The ones shared between Csi and Vbi are defined in InteractiveCompiler.cs, which is the base class.

        #endregion

        #region Tool Members
        /// <summary>
        /// Return the name of the tool to execute.
        /// </summary>
        protected override string ToolName => "csi.exe";
        #endregion

        #region Interactive Compiler Members
        protected override void AddResponseFileCommands(CommandLineBuilderExtension commandLine)
        {
            commandLine.AppendSwitchIfNotNull("/lib:", AdditionalLibPaths, ",");
            commandLine.AppendSwitchIfNotNull("/loadpaths:", AdditionalLoadPaths, ",");
            commandLine.AppendSwitchIfNotNull("/imports:", Imports, ";");

            Csc.AddReferencesToCommandLine(commandLine, References, isInteractive: true);

            base.AddResponseFileCommands(commandLine);
        }
        #endregion
    }
}
