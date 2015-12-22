// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading.Tasks;

namespace Microsoft.VisualStudio.ProjectSystem.Designers.Input
{
    internal static class GetCommandStatusResult
    {
        public static Task<CommandStatusResult> Unhandled
        {
            get { return CommandStatusResult.Unhandled.AsTask(); }
        }

        public static Task<CommandStatusResult> Handled(string commandText, CommandStatus status)
        {
            return new CommandStatusResult(true, commandText, status | CommandStatus.Supported).AsTask();
        }
    }
}
