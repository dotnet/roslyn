// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Classification;

namespace Microsoft.VisualStudio.InteractiveWindow.Commands
{
    /// <summary>
    /// Provides handling of meta-commands in the interactive window.  Instances can be
    /// created using the <see cref="IInteractiveWindowCommandsFactory"/> service.
    /// </summary>
    public interface IInteractiveWindowCommands
    {
        /// <summary>
        /// Checks to see if the current input is in command mode (it is prefixed with the
        /// command prefix).
        /// </summary>
        bool InCommand
        {
            get;
        }

        /// <summary>
        /// Gets the prefix which is used for interactive window commands.
        /// </summary>
        string CommandPrefix { get; }

        /// <summary>
        /// Attempts to execute the command and returns the execution result.
        /// 
        /// Returns null if the current command is unrecognized.
        /// </summary>
        /// <returns></returns>
        Task<ExecutionResult> TryExecuteCommand();

        /// <summary>
        /// Gets the registered list of commands that this IInteractiveWindowCommands was created with.
        /// </summary>
        /// <returns></returns>
        IEnumerable<IInteractiveWindowCommand> GetCommands();

        /// <summary>
        /// Gets an individual command by name.
        /// </summary>
        IInteractiveWindowCommand this[string name]
        {
            get;
        }

        /// <summary>
        /// Displays help into the interactive window for the specified command.
        /// </summary>
        void DisplayCommandHelp(IInteractiveWindowCommand command);

        /// <summary>
        /// Displays usage information in the interactive window for the specified command.
        /// </summary>
        void DisplayCommandUsage(IInteractiveWindowCommand command, TextWriter writer, bool displayDetails);

        /// <summary>
        /// Displays help for all of the available commands.
        /// </summary>
        void DisplayHelp();

        /// <summary>
        /// Classifies the specified command snapshot.
        /// </summary>
        IEnumerable<ClassificationSpan> Classify(SnapshotSpan span);
    }
}
