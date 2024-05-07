// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Microsoft.CodeAnalysis.Host;

namespace Microsoft.CodeAnalysis.ExternalAccess.UnitTesting.SolutionCrawler
{
    /// <summary>
    /// Provide a way to control solution crawler.
    /// </summary>
    internal interface IUnitTestingSolutionCrawlerService : IWorkspaceService
    {
        /// <summary>
        /// Ask solution crawler to re-analyze given <see cref="ProjectId"/>s or/and <see cref="DocumentId"/>s 
        /// in given <see cref="Workspace"/> with given <see cref="IUnitTestingIncrementalAnalyzer"/>.
        /// </summary>
        void Reanalyze(string? workspaceKind, SolutionServices services, IUnitTestingIncrementalAnalyzer analyzer, IEnumerable<ProjectId>? projectIds = null, IEnumerable<DocumentId>? documentIds = null);

        /// <summary>
        /// Get <see cref="IUnitTestingSolutionCrawlerProgressReporter"/> for the given <see cref="Workspace"/>
        /// </summary>
        IUnitTestingSolutionCrawlerProgressReporter GetProgressReporter(Workspace workspace);
    }
}
