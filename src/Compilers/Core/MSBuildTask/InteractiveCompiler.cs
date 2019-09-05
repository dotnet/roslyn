// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.IO;
using System.Linq;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.BuildTasks
{
    /// <summary>
    /// This class defines all of the common stuff that is shared between the Vbc and Csc tasks.
    /// This class is not instantiatable as a Task just by itself.
    /// </summary>
    public abstract class InteractiveCompiler : ManagedToolTask
    {
        internal readonly PropertyDictionary _store = new PropertyDictionary();

        public InteractiveCompiler()
        {
            TaskResources = ErrorString.ResourceManager;
        }

        #region Properties - Please keep these alphabetized.
        public string[] AdditionalLibPaths
        {
            set
            {
                _store[nameof(AdditionalLibPaths)] = value;
            }

            get
            {
                return (string[])_store[nameof(AdditionalLibPaths)];
            }
        }

        public string[] AdditionalLoadPaths
        {
            set
            {
                _store[nameof(AdditionalLoadPaths)] = value;
            }

            get
            {
                return (string[])_store[nameof(AdditionalLoadPaths)];
            }
        }

        [Output]
        public ITaskItem[] CommandLineArgs
        {
            set
            {
                _store[nameof(CommandLineArgs)] = value;
            }

            get
            {
                return (ITaskItem[])_store[nameof(CommandLineArgs)];
            }
        }

        public string Features
        {
            set
            {
                _store[nameof(Features)] = value;
            }

            get
            {
                return (string)_store[nameof(Features)];
            }
        }

        public ITaskItem[] Imports
        {
            set
            {
                _store[nameof(Imports)] = value;
            }

            get
            {
                return (ITaskItem[])_store[nameof(Imports)];
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

        public ITaskItem[] References
        {
            set
            {
                _store[nameof(References)] = value;
            }

            get
            {
                return (ITaskItem[])_store[nameof(References)];
            }
        }

        public ITaskItem[] ResponseFiles
        {
            set
            {
                _store[nameof(ResponseFiles)] = value;
            }

            get
            {
                return (ITaskItem[])_store[nameof(ResponseFiles)];
            }
        }

        public string[] ScriptArguments
        {
            set
            {
                _store[nameof(ScriptArguments)] = value;
            }

            get
            {
                return (string[])_store[nameof(ScriptArguments)];
            }
        }

        public ITaskItem[] ScriptResponseFiles
        {
            set
            {
                _store[nameof(ScriptResponseFiles)] = value;
            }

            get
            {
                return (ITaskItem[])_store[nameof(ScriptResponseFiles)];
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

        public ITaskItem Source
        {
            set
            {
                _store[nameof(Source)] = value;
            }

            get
            {
                return (ITaskItem)_store[nameof(Source)];
            }
        }
        #endregion

        #region Tool Members

        // See ManagedCompiler.cs on the logic of this property
        private bool HasToolBeenOverridden => !(string.IsNullOrEmpty(ToolPath) && ToolExe == ToolName);

        protected sealed override bool IsManagedTool => !HasToolBeenOverridden;

        protected sealed override string PathToManagedTool => Utilities.GenerateFullPathToTool(ToolName);

        protected sealed override string PathToNativeTool => Path.Combine(ToolPath ?? "", ToolExe);

        protected override int ExecuteTool(string pathToTool, string responseFileCommands, string commandLineCommands)
        {
            if (ProvideCommandLineArgs)
            {
                CommandLineArgs = GetArguments(commandLineCommands, responseFileCommands).Select(arg => new TaskItem(arg)).ToArray();
            }

            return (SkipInteractiveExecution) ? 0 : base.ExecuteTool(pathToTool, responseFileCommands, commandLineCommands);
        }

        public string GenerateCommandLineContents() => GenerateCommandLineCommands();

        protected sealed override string ToolArguments
        {
            get
            {
                var builder = new CommandLineBuilderExtension();
                AddCommandLineCommands(builder);
                return builder.ToString();
            }
        }

        public string GenerateResponseFileContents() => GenerateResponseFileCommands();

        protected sealed override string GenerateResponseFileCommands()
        {
            var commandLineBuilder = new CommandLineBuilderExtension();
            AddResponseFileCommands(commandLineBuilder);
            return commandLineBuilder.ToString();
        }

        #endregion

        /// <summary>
        /// Fills the provided CommandLineBuilderExtension with those switches and other information that can't go into a response file and
        /// must go directly onto the command line.
        /// </summary>
        protected virtual void AddCommandLineCommands(CommandLineBuilderExtension commandLine)
        {
        }

        /// <summary>
        /// Fills the provided CommandLineBuilderExtension with those switches and other information that can go into a response file.
        /// </summary>
        protected virtual void AddResponseFileCommands(CommandLineBuilderExtension commandLine)
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

            if (ResponseFiles != null)
            {
                foreach (var scriptResponse in ScriptResponseFiles)
                {
                    commandLine.AppendSwitchIfNotNull("@", scriptResponse.ItemSpec);
                }
            }
        }

        /// <summary>
        /// Get the command line arguments to pass to the compiler.
        /// </summary>
        private string[] GetArguments(string commandLineCommands, string responseFileCommands)
        {
            var commandLineArguments = CommandLineUtilities.SplitCommandLineIntoArguments(commandLineCommands, removeHashComments: true);
            var responseFileArguments = CommandLineUtilities.SplitCommandLineIntoArguments(responseFileCommands, removeHashComments: true);
            return commandLineArguments.Concat(responseFileArguments).ToArray();
        }
    }
}
