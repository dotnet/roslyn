// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.VisualStudio.LanguageServer.Protocol;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.LanguageServer.Handler
{
    [ExportCSharpVisualBasicStatelessLspService(typeof(GetTextDocumentWithContextHandler)), Shared]
    [Method(VSMethods.GetProjectContextsName)]
    internal class GetTextDocumentWithContextHandler : ILspServiceDocumentRequestHandler<VSGetProjectContextsParams, VSProjectContextList?>
    {
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public GetTextDocumentWithContextHandler()
        {
        }

        public bool MutatesSolutionState => false;
        public bool RequiresLSPSolution => true;

        public TextDocumentIdentifier GetTextDocumentIdentifier(VSGetProjectContextsParams request) => new TextDocumentIdentifier { Uri = request.TextDocument.Uri };

        public Task<VSProjectContextList?> HandleRequestAsync(VSGetProjectContextsParams request, RequestContext context, CancellationToken cancellationToken)
        {
            Contract.ThrowIfNull(context.Workspace);
            Contract.ThrowIfNull(context.Solution);

            var contextList = ProjectContextHelper.GetContextList(context.Workspace, context.Solution, request.TextDocument.Uri);
            if (contextList is null)
            {
                return SpecializedTasks.Null<VSProjectContextList>();
            }

            return Task.FromResult<VSProjectContextList?>(contextList);
        }
    }
}
