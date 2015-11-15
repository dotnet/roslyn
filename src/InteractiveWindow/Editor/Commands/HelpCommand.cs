// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Threading.Tasks;

namespace Microsoft.VisualStudio.InteractiveWindow.Commands
{
    [Export(typeof(IInteractiveWindowCommand))]
    internal sealed class HelpReplCommand : InteractiveWindowCommand
    {
        internal const string CommandName = "help";

        public override string Description
        {
            get { return InteractiveWindowResources.HelpCommandDescription; }
        }

        public override IEnumerable<string> Names
        {
            get { yield return CommandName; }
        }

        public override string CommandLine
        {
            get { return InteractiveWindowResources.CommandNamePlaceholder; }
        }

        public override Task<ExecutionResult> Execute(IInteractiveWindow window, string arguments)
        {
            string commandName;
            IInteractiveWindowCommand command;
            if (!ParseArguments(window, arguments, out commandName, out command))
            {
                window.ErrorOutputWriter.WriteLine(string.Format(InteractiveWindowResources.UnknownCommand, commandName));
                ReportInvalidArguments(window);
                return ExecutionResult.Failed;
            }

            var commands = (IInteractiveWindowCommands)window.Properties[typeof(IInteractiveWindowCommands)];
            if (command != null)
            {
                commands.DisplayCommandHelp(command);
            }
            else
            {
                commands.DisplayHelp();
            }

            return ExecutionResult.Succeeded;
        }

        private static readonly char[] s_whitespaceChars = new[] { '\r', '\n', ' ', '\t' };

        private bool ParseArguments(IInteractiveWindow window, string arguments, out string commandName, out IInteractiveWindowCommand command)
        {
            string name = arguments.Split(s_whitespaceChars)[0];

            if (name.Length == 0)
            {
                command = null;
                commandName = null;
                return true;
            }

            var commands = window.GetInteractiveCommands();
            string prefix = commands.CommandPrefix;

            // display help on a particular command:
            command = commands[name];

            if (command == null && name.StartsWith(prefix, StringComparison.Ordinal))
            {
                name = name.Substring(prefix.Length);
                command = commands[name];
            }

            commandName = name;
            return command != null;
        }
    }
}
