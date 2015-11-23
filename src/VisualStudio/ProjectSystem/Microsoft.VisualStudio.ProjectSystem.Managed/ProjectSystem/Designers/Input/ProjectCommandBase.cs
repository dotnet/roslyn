// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Threading;

namespace Microsoft.VisualStudio.ProjectSystem.Designers.Input
{
    /// <summary>
    ///     Provides the base <see langword="abstract"/> class for all commands that operate on <see cref="IProjectTree"/> nodes.
    /// </summary>
    internal abstract class ProjectCommandBase : IAsyncCommandGroupHandler
    {
        private readonly Lazy<long[]> _commandIds;

        protected ProjectCommandBase()
        {
            _commandIds = new Lazy<long[]>(() => GetCommandIds(this));
        }
        
        public Task<CommandStatusResult> GetCommandStatusAsync(IImmutableSet<IProjectTree> nodes, long commandId, bool focused, string commandText, CommandStatus progressiveStatus)
        {
            Requires.NotNull(nodes, nameof(nodes));

            foreach (long otherCommandId in _commandIds.Value)
            {
                if (otherCommandId == commandId)
                    return GetCommandStatusAsync(nodes, focused, commandText, progressiveStatus);
            }

            return GetCommandStatusResult.Unhandled;
        }

        public Task<bool> TryHandleCommandAsync(IImmutableSet<IProjectTree> nodes, long commandId, bool focused, long commandExecuteOptions, IntPtr variantArgIn, IntPtr variantArgOut)
        {
            Requires.NotNull(nodes, nameof(nodes));

            foreach (long otherCommandId in _commandIds.Value)
            {
                if (otherCommandId == commandId)
                    return TryHandleCommandAsync(nodes, focused, commandExecuteOptions, variantArgIn, variantArgOut);
            }

            return TaskResult.False;
        }

        protected abstract Task<CommandStatusResult> GetCommandStatusAsync(IImmutableSet<IProjectTree> nodes, bool focused, string commandText, CommandStatus progressiveStatus);

        protected abstract Task<bool> TryHandleCommandAsync(IImmutableSet<IProjectTree> nodes, bool focused, long commandExecuteOptions, IntPtr variantArgIn, IntPtr variantArgOut);

        private static long[] GetCommandIds(ProjectCommandBase command)
        {
            ProjectCommandAttribute attribute = (ProjectCommandAttribute)Attribute.GetCustomAttribute(command.GetType(), typeof(ProjectCommandAttribute));
            if (attribute == null)
                return Array.Empty<long>();

            return attribute.CommandIds;
        }
    }
}
