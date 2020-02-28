// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.ComponentModel.Composition;

namespace Microsoft.CodeAnalysis.LanguageServer.Handler.Commands
{
    /// <summary>
    /// Defines an attribute to export LSP handlers to handle a specific command from the 'workspace/executeCommand' method.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class), MetadataAttribute]
    internal class ExportExecuteWorkspaceCommandAttribute : ExportAttribute, IExecuteWorkspaceCommandHandlerMetadata
    {
        public string CommandName { get; }

        public ExportExecuteWorkspaceCommandAttribute(string commandName) : base(typeof(IExecuteWorkspaceCommandHandler))
        {
            if (string.IsNullOrEmpty(commandName))
            {
                throw new ArgumentException(nameof(commandName));
            }

            CommandName = commandName;
        }
    }
}
