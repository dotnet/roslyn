// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Threading.Tasks;
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
        public async Task<IList<TodoComment>> GetTodoCommentsAsync(DocumentId documentId, IList<TodoCommentDescriptor> tokens)
        {
            using (RoslynLogger.LogBlock(FunctionId.CodeAnalysisService_GetTodoCommentsAsync, documentId.ProjectId.DebugName, CancellationToken))
            {
                try
                {
                    var solution = await GetSolutionAsync().ConfigureAwait(false);
                    var document = solution.GetDocument(documentId);

                    var service = document.GetLanguageService<ITodoCommentService>();
                    if (service != null)
                    {
                        // todo comment service supported
                        return await service.GetTodoCommentsAsync(document, tokens, CancellationToken).ConfigureAwait(false);
                    }
                }
                catch (IOException)
                {
                    // stream to send over result has closed before we
                    // had chance to check cancellation
                }
                catch (OperationCanceledException)
                {
                    // rpc connection has closed.
                    // this can happen if client side cancelled the
                    // operation
                }

                return SpecializedCollections.EmptyList<TodoComment>();
            }
        }
    }
}