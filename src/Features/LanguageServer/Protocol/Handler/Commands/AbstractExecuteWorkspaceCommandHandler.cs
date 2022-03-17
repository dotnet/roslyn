// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.LanguageServer.Handler.Commands
{
    internal abstract class AbstractExecuteWorkspaceCommandHandler : IRequestHandler<ExecuteCommandParams, object>
    {
        public abstract string Command { get; }

        public abstract bool MutatesSolutionState { get; }
        public abstract bool RequiresLSPSolution { get; }

        public abstract TextDocumentIdentifier? GetTextDocumentIdentifier(ExecuteCommandParams request);

        public abstract Task<object> HandleRequestAsync(ExecuteCommandParams request, RequestContext context, CancellationToken cancellationToken);

        public static string GetRequestNameForCommandName(string commandName) => $"{Methods.WorkspaceExecuteCommandName}/{commandName}";
    }
}
