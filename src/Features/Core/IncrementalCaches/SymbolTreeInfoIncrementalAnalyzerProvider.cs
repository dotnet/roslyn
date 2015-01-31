// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.FindSymbols;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.SolutionCrawler;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.IncrementalCaches
{
    [ExportIncrementalAnalyzerProvider(WorkspaceKind.Host), Shared]
    internal class SymbolTreeInfoIncrementalAnalyzerProvider : IIncrementalAnalyzerProvider
    {
        public IIncrementalAnalyzer CreateIncrementalAnalyzer(Workspace workspace)
        {
            return new IncrementalAnalyzer();
        }

        private class IncrementalAnalyzer : IncrementalAnalyzerBase
        {
            private SolutionId solutionId;
            private Dictionary<ProjectId, int> symbolCountByProjectMap = new Dictionary<ProjectId, int>();

            public override async Task AnalyzeProjectAsync(Project project, bool semanticsChanged, CancellationToken cancellationToken)
            {
                if (symbolCountByProjectMap == null || !project.SupportsCompilation || !semanticsChanged)
                {
                    return;
                }

                // we do this just to report total symbol numbers
                var compilation = await project.GetCompilationAsync(cancellationToken).ConfigureAwait(false);

                var info = SymbolTreeInfo.Create(VersionStamp.Default, compilation.Assembly, cancellationToken);
                if (info != null)
                {
                    RecordCount(project.Id, info.Count);
                }
            }

            public override Task NewSolutionSnapshotAsync(Solution solution, CancellationToken cancellationToken)
            {
                // check whether we are good to report total symbol numbers
                if (symbolCountByProjectMap == null || symbolCountByProjectMap.Count < solution.ProjectIds.Count || string.IsNullOrEmpty(solution.FilePath))
                {
                    return SpecializedTasks.EmptyTask;
                }

                if (solutionId != null && solutionId != solution.Id)
                {
                    ReportCount();
                    return SpecializedTasks.EmptyTask;
                }

                solutionId = solution.Id;
                foreach (var projectId in solution.ProjectIds)
                {
                    if (!symbolCountByProjectMap.ContainsKey(projectId))
                    {
                        return SpecializedTasks.EmptyTask;
                    }
                }

                ReportCount();
                return SpecializedTasks.EmptyTask;
            }

            public override void RemoveProject(ProjectId projectId)
            {
                if (symbolCountByProjectMap != null)
                {
                    symbolCountByProjectMap.Remove(projectId);
                }
            }

            private void ReportCount()
            {
                var sourceSymbolCount = symbolCountByProjectMap.Sum(kv => kv.Value).ToString();
                Logger.Log(FunctionId.Run_Environment, KeyValueLogMessage.Create(m => m["SourceSymbolCount"] = sourceSymbolCount));

                // we only report it once
                symbolCountByProjectMap = null;
                solutionId = null;
            }

            private void RecordCount(ProjectId id, int count)
            {
                if (symbolCountByProjectMap == null)
                {
                    return;
                }

                symbolCountByProjectMap[id] = count;
            }
        }
    }
}
