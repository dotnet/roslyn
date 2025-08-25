// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.IO;
using System.Linq;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.BuildTasks
{
    /// <summary>
    /// This class defines all of the common stuff that is shared between the Vbi and Csi tasks.
    /// This class is not instantiatable as a Task just by itself.
    /// </summary>
    public abstract class InteractiveCompiler : ManagedToolTask
    {
        public InteractiveCompiler()
            : base(ErrorString.ResourceManager)
        {
        }

        #region Properties - Please keep these alphabetized.
        public string[]? AdditionalLibPaths
        {
            set
            {
                _store[nameof(AdditionalLibPaths)] = value;
            }

            get
            {
                return (string[]?)_store[nameof(AdditionalLibPaths)];
            }
        }

        public string[]? AdditionalLoadPaths
        {
            set
            {
                _store[nameof(AdditionalLoadPaths)] = value;
            }

            get
            {
                return (string[]?)_store[nameof(AdditionalLoadPaths)];
            }
        }

        [Output]
        public ITaskItem[]? CommandLineArgs
        {
            set
            {
                _store[nameof(CommandLineArgs)] = value;
            }

            get
            {
                return (ITaskItem[]?)_store[nameof(CommandLineArgs)];
            }
        }

        public string? Features
        {
            set
            {
                _store[nameof(Features)] = value;
            }

            get
            {
                return (string?)_store[nameof(Features)];
            }
        }

        public ITaskItem[]? Imports
        {
            set
            {
                _store[nameof(Imports)] = value;
            }

            get
            {
                return (ITaskItem[]?)_store[nameof(Imports)];
            }
        }

        public bool ProvideCommandLineArgs
        {
            set
            {
                _store[nameof(ProvideCommandLineArgs)] = value;
            }

            get
            {
                return _store.GetOrDefault(nameof(ProvideCommandLineArgs), false);
            }
        }

        public ITaskItem[]? References
        {
            set
            {
                _store[nameof(References)] = value;
            }

            get
            {
                return (ITaskItem[]?)_store[nameof(References)];
            }
        }

        public ITaskItem[]? ResponseFiles
        {
            set
            {
                _store[nameof(ResponseFiles)] = value;
            }

            get
            {
                return (ITaskItem[]?)_store[nameof(ResponseFiles)];
            }
        }

        public string[]? ScriptArguments
        {
            set
            {
                _store[nameof(ScriptArguments)] = value;
            }

            get
            {
                return (string[]?)_store[nameof(ScriptArguments)];
            }
        }

        public ITaskItem[]? ScriptResponseFiles
        {
            set
            {
                _store[nameof(ScriptResponseFiles)] = value;
            }

            get
            {
                return (ITaskItem[]?)_store[nameof(ScriptResponseFiles)];
            }
        }

        public bool SkipInteractiveExecution
        {
            set
            {
                _store[nameof(SkipInteractiveExecution)] = value;
            }

            get
            {
                return _store.GetOrDefault(nameof(SkipInteractiveExecution), false);
            }
        }

        public ITaskItem? Source
        {
            set
            {
                _store[nameof(Source)] = value;
            }

            get
            {
                return (ITaskItem?)_store[nameof(Source)];
            }
        }
        #endregion

        #region Tool Members

        protected override int ExecuteTool(string pathToTool, string responseFileCommands, string commandLineCommands)
        {
            if (ProvideCommandLineArgs)
            {
                CommandLineArgs = GenerateCommandLineArgsTaskItems(responseFileCommands);
            }

            return (SkipInteractiveExecution) ? 0 : base.ExecuteTool(pathToTool, responseFileCommands, commandLineCommands);
        }

        #endregion

        /// <summary>
        /// Fills the provided CommandLineBuilderExtension with those switches and other information that can go into a response file.
        /// </summary>
        protected override void AddResponseFileCommands(CommandLineBuilderExtension commandLine)
        {
            commandLine.AppendSwitch("/i-");

            ManagedCompiler.AddFeatures(commandLine, Features);

            if (ResponseFiles != null)
            {
                foreach (var response in ResponseFiles)
                {
                    commandLine.AppendSwitchIfNotNull("@", response.ItemSpec);
                }
            }

            commandLine.AppendFileNameIfNotNull(Source);

            if (ScriptArguments != null)
            {
                foreach (var scriptArgument in ScriptArguments)
                {
                    commandLine.AppendArgumentIfNotNull(scriptArgument);
                }
            }

            if (ScriptResponseFiles != null)
            {
                foreach (var scriptResponse in ScriptResponseFiles)
                {
                    commandLine.AppendSwitchIfNotNull("@", scriptResponse.ItemSpec);
                }
            }
        }
    }
}
