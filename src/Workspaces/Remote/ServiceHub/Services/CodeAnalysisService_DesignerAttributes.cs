// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.DesignerAttributes;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.Shared.Extensions;
using RoslynLogger = Microsoft.CodeAnalysis.Internal.Log.Logger;

namespace Microsoft.CodeAnalysis.Remote
{
    // root level service for all Roslyn services
    internal partial class CodeAnalysisService : IRemoteDesignerAttributeService
    {
        /// <summary>
        /// This is top level entry point for DesignerAttribute service from client (VS).
        /// 
        /// This will be called by ServiceHub/JsonRpc framework
        /// </summary>
        public async Task<DesignerAttributeResult> ScanDesignerAttributesAsync(DocumentId documentId)
        {
            using (RoslynLogger.LogBlock(FunctionId.CodeAnalysisService_GetTodoCommentsAsync, documentId.ProjectId.DebugName, CancellationToken))
            {
                try
                {
                    var solution = await GetSolutionAsync().ConfigureAwait(false);
                    var document = solution.GetDocument(documentId);

                    var service = document.GetLanguageService<IDesignerAttributeService>();
                    if (service != null)
                    {
                        // todo comment service supported
                        return await service.ScanDesignerAttributesAsync(document, CancellationToken).ConfigureAwait(false);
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

                return new DesignerAttributeResult(designerAttributeArgument: null, containsErrors: true, notApplicable: true);
            }
        }
    }
}