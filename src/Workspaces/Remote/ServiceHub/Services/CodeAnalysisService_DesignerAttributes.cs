// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.DesignerAttributes;
using Microsoft.CodeAnalysis.Execution;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;
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
        public Task<DesignerAttributeResult> ScanDesignerAttributesAsync(DocumentId documentId, CancellationToken cancellationToken)
        {
            return RunServiceAsync(async () =>
            {
                using (RoslynLogger.LogBlock(FunctionId.CodeAnalysisService_GetDesignerAttributesAsync, documentId.DebugName, cancellationToken))
                {
                    var solution = await GetSolutionAsync(cancellationToken).ConfigureAwait(false);
                    var document = solution.GetDocument(documentId);

                    var service = document.GetLanguageService<IDesignerAttributeService>();
                    if (service != null)
                    {
                        // todo comment service supported
                        return await service.ScanDesignerAttributesAsync(document, cancellationToken).ConfigureAwait(false);
                    }

                    return new DesignerAttributeResult(designerAttributeArgument: null, containsErrors: true, applicable: false);
                }
            }, cancellationToken);
        }
    }
}
