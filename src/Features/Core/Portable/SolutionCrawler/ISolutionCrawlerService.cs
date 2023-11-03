// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Microsoft.CodeAnalysis.Host;

namespace Microsoft.CodeAnalysis.SolutionCrawler
{
    /// <summary>
    /// Provide a way to control solution crawler.
    /// </summary>
    internal interface ISolutionCrawlerService : IWorkspaceService
    {
        /// <summary>
        /// Ask solution crawler to re-analyze given <see cref="ProjectId"/>s or/and <see cref="DocumentId"/>s 
        /// in given <see cref="Workspace"/> with given <see cref="IIncrementalAnalyzer"/>.
        /// If both <paramref name="projectIds"/> and <paramref name="documentIds"/> are null, the entire
        /// <see cref="Workspace.CurrentSolution"/> for the given <paramref name="workspace"/> is re-analyzed.
        /// </summary>
        void Reanalyze(Workspace workspace, IIncrementalAnalyzer analyzer, IEnumerable<ProjectId>? projectIds, IEnumerable<DocumentId>? documentIds, bool highPriority);

        /// <summary>
        /// Get <see cref="ISolutionCrawlerProgressReporter"/> for the given <see cref="Workspace"/>
        /// </summary>
        ISolutionCrawlerProgressReporter GetProgressReporter(Workspace workspace);
    }
}
