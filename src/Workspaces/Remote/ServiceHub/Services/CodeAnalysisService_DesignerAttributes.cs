// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.DesignerAttributes;
using Microsoft.CodeAnalysis.Internal.Log;
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
        public async Task<IList<DesignerAttributeDocumentData>> ScanDesignerAttributesAsync(ProjectId projectId, CancellationToken cancellationToken)
        {
            using (RoslynLogger.LogBlock(FunctionId.CodeAnalysisService_GetDesignerAttributesAsync, projectId.DebugName, cancellationToken))
            {
                var solution = await GetSolutionAsync(cancellationToken).ConfigureAwait(false);
                var project = solution.GetProject(projectId);

                var data = await AbstractDesignerAttributeService.TryAnalyzeProjectInCurrentProcessAsync(
                    project, cancellationToken).ConfigureAwait(false);

                if (data.Count == 0)
                {
                    return SpecializedCollections.EmptyList<DesignerAttributeDocumentData>();
                }

                return data.Values.ToList();
            }
        }
    }
}
