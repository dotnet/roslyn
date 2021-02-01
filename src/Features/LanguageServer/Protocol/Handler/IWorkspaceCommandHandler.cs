// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.VisualStudio.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.LanguageServer.Handler
{
    internal interface IWorkspaceCommandHandler : IRequestHandler
    {
        /// <summary>
        /// The name of the command to execute for a <see cref="Methods.WorkspaceExecuteCommandName"/> request.
        /// </summary>
        string CommandName { get; }
    }
}
