// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Language.StandardClassification;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Classification;

namespace Microsoft.VisualStudio.InteractiveWindow.Commands
{
    /// <summary>
    /// Represents a command which can be run from a REPL window.
    /// 
    /// This interface is a MEF contract and can be implemented and exported to add commands to the REPL window.
    /// </summary>
    public interface IInteractiveWindowCommand
    {
        /// <summary>
        /// Asynchronously executes the command with specified arguments and calls back the given completion when finished.
        /// </summary>
        /// <param name="window">The interactive window.</param>
        /// <param name="arguments">Command arguments.</param>
        /// <returns>The task that completes the execution.</returns>
        Task<ExecutionResult> Execute(IInteractiveWindow window, string arguments);

        /// <summary>
        /// Gets a brief (ideally single-line) description of the REPL command which is displayed when the user asks for help.
        /// </summary>
        string Description
        {
            get;
        }

        /// <summary>
        /// A single line parameters listing, or null if the command doesn't take any parameters. For example, "[on|off]".
        /// </summary>
        string CommandLine
        {
            get;
        }

        /// <summary>
        /// Gets detailed description of the command usage.
        /// </summary>
        /// <remarks>
        /// Returns a sequence of lines.
        /// </remarks>
        IEnumerable<string> DetailedDescription
        {
            get;
        }

        /// <summary>
        /// Parameter name and description for parameters of the command.
        /// </summary>
        IEnumerable<KeyValuePair<string, string>> ParametersDescription
        {
            get;
        }

        /// <summary>
        /// The name of the command. May not contain any whitespace characters.
        /// </summary>
        IEnumerable<string> Names
        {
            get;
        }

        /// <summary>
        /// Provides classification for command arguments.
        /// </summary>
        IEnumerable<ClassificationSpan> ClassifyArguments(ITextSnapshot snapshot, Span argumentsSpan, Span spanToClassify);
    }
}
