// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.LanguageServer.Handler
{
    internal abstract class AbstractWorkspaceCommandHandler<ResponseType> : IWorkspaceCommandHandler, IRequestHandler<ExecuteCommandParams, ResponseType>
    {
        public string MethodName => $"{Methods.WorkspaceExecuteCommandName}/{CommandName}";

        public abstract string CommandName { get; }
        public abstract bool MutatesSolutionState { get; }
        public abstract bool SkipBuildingLSPSolution { get; }

        public abstract TextDocumentIdentifier? GetTextDocumentIdentifier(ExecuteCommandParams request);
        public abstract Task<ResponseType> HandleRequestAsync(ExecuteCommandParams request, RequestContext context, CancellationToken cancellationToken);
    }
}
