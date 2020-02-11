// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Execution;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.TodoComments;
using Roslyn.Utilities;
using RoslynLogger = Microsoft.CodeAnalysis.Internal.Log.Logger;

namespace Microsoft.CodeAnalysis.Remote
{
    // root level service for all Roslyn services
    internal partial class CodeAnalysisService : IRemoteTodoCommentService
    {
        /// <summary>
        /// This is top level entry point for TodoComments service from client (VS).
        /// 
        /// This will be called by ServiceHub/JsonRpc framework
        /// </summary>
        public Task<IList<TodoComment>> GetTodoCommentsAsync(PinnedSolutionInfo solutionInfo, DocumentId documentId, IList<TodoCommentDescriptor> tokens, CancellationToken cancellationToken)
        {
            return RunServiceAsync(async () =>
            {
                using (RoslynLogger.LogBlock(FunctionId.CodeAnalysisService_GetTodoCommentsAsync, documentId.DebugName, cancellationToken))
                {
                    var solution = await GetSolutionAsync(solutionInfo, cancellationToken).ConfigureAwait(false);
                    var document = solution.GetDocument(documentId);

                    var service = document.GetLanguageService<ITodoCommentService>();
                    if (service != null)
                    {
                        // todo comment service supported
                        return await service.GetTodoCommentsAsync(document, tokens, cancellationToken).ConfigureAwait(false);
                    }

                    return SpecializedCollections.EmptyList<TodoComment>();
                }
            }, cancellationToken);
        }
    }
}
