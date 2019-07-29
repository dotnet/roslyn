// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.LanguageServer.Handler.Commands
{
    internal interface IExecuteWorkspaceCommandHandler
    {
        /// <summary>
        /// Handles a specific command from a <see cref="Methods.WorkspaceExecuteCommandName"/> request.
        /// </summary>
        Task<object> HandleRequestAsync(Solution solution, ExecuteCommandParams request, ClientCapabilities clientCapabilities, CancellationToken cancellationToken, bool keepThreadContext = false);
    }
}
