// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
