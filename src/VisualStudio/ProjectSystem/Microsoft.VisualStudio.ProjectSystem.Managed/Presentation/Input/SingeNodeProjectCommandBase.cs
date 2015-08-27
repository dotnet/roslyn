// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.ProjectSystem.Designers;
using Microsoft.VisualStudio.ProjectSystem.Threading.Tasks;

namespace Microsoft.VisualStudio.ProjectSystem.Presentation.Input
{
    /// <summary>
    ///     Provides the base <see langword="abstract"/> class for all commands that handle a single project tree node.
    /// </summary>
    internal abstract class SingleNodeProjectCommandBase : ProjectCommandBase
    {
        protected SingleNodeProjectCommandBase()
        {
        }

        protected override sealed Task<CommandStatusResult> GetCommandStatusAsync(IImmutableSet<IProjectTree> nodes, bool focused, string commandText, CommandStatus progressiveStatus)
        {
            if (nodes.Count == 1)
            {
                return GetCommandStatusAsync(nodes.First(), focused, commandText, progressiveStatus);
            }

            return GetCommandStatusResult.Unhandled;
        }

        protected override sealed Task<bool> TryHandleCommandAsync(IImmutableSet<IProjectTree> nodes, bool focused, long commandExecuteOptions, IntPtr variantArgIn, IntPtr variantArgOut)
        {
            if (nodes.Count == 1)
            {
                return TryHandleCommandAsync(nodes.First(), focused, commandExecuteOptions, variantArgIn, variantArgOut);
            }

            return TaskResult.False;
        }

        protected abstract Task<CommandStatusResult> GetCommandStatusAsync(IProjectTree node, bool focused, string commandText, CommandStatus progressiveStatus);

        protected abstract Task<bool> TryHandleCommandAsync(IProjectTree node, bool focused, long commandExecuteOptions, IntPtr variantArgIn, IntPtr variantArgOut);
    }
}
