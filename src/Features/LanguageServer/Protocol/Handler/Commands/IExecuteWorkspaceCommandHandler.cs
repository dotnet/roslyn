// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
        Task<object> HandleRequestAsync(ExecuteCommandParams request, RequestContext context, CancellationToken cancellationToken);
    }
}
