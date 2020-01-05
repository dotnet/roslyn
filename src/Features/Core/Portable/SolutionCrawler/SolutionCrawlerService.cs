// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Composition;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;

namespace Microsoft.CodeAnalysis.SolutionCrawler
{
    internal partial class SolutionCrawlerRegistrationService : ISolutionCrawlerRegistrationService
    {
        /// <summary>
        /// nested class of <see cref="SolutionCrawlerRegistrationService"/> since it is tightly coupled with it.
        /// 
        /// <see cref="ISolutionCrawlerService"/> is implemented by this class since WorkspaceService doesn't allow a class to implement
        /// more than one <see cref="IWorkspaceService"/>.
        /// </summary>
        [ExportWorkspaceService(typeof(ISolutionCrawlerService), ServiceLayer.Default), Shared]
        internal class SolutionCrawlerService : ISolutionCrawlerService
        {
            [ImportingConstructor]
            public SolutionCrawlerService()
            {
            }

            public void Reanalyze(Workspace workspace, IIncrementalAnalyzer analyzer, IEnumerable<ProjectId> projectIds = null, IEnumerable<DocumentId> documentIds = null, bool highPriority = false)
            {
                // if solution crawler doesn't exist for the given workspace. don't do anything
                if (workspace.Services.GetService<ISolutionCrawlerRegistrationService>() is SolutionCrawlerRegistrationService registration)
                {
                    registration.Reanalyze(workspace, analyzer, projectIds, documentIds, highPriority);
                }
            }

            public ISolutionCrawlerProgressReporter GetProgressReporter(Workspace workspace)
            {
                // if solution crawler doesn't exist for the given workspace, return null reporter
                if (workspace.Services.GetService<ISolutionCrawlerRegistrationService>() is SolutionCrawlerRegistrationService registration)
                {
                    // currently we have only 1 global reporter that are shared by all workspaces.
                    return registration._progressReporter;
                }

                return NullReporter.Instance;
            }
        }
    }
}
