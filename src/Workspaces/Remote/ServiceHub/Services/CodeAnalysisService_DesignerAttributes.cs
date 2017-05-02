// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.DesignerAttributes;
using Microsoft.CodeAnalysis.Internal.Log;
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
        public async Task<DesignerAttributeResult[]> ScanDesignerAttributesAsync(ProjectId projectId)
        {
            using (RoslynLogger.LogBlock(FunctionId.CodeAnalysisService_GetDesignerAttributesAsync, projectId.DebugName, CancellationToken))
            {
                var solution = await GetSolutionAsync().ConfigureAwait(false);
                var project = solution.GetProject(projectId);
                var service = project.LanguageServices.GetService<IDesignerAttributeService>();

                var results = new DesignerAttributeResult[project.DocumentIds.Count];
                var index = 0;
                foreach (var document in project.Documents)
                {
                    var result = await service.ScanDesignerAttributesAsync(document, CancellationToken).ConfigureAwait(false);
                    results[index] = result;
                    index++;
                }

                return results;
            }
        }
    }
}