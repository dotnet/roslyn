// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

namespace Microsoft.CodeAnalysis.LanguageServer.Handler.Commands
{
    internal interface IExecuteWorkspaceCommandHandlerMetadata
    {
        /// <summary>
        /// Defines the 'workspace/executeCommand' command name that should be handled.
        /// </summary>
        string CommandName { get; }
    }
}
