// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Composition;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Options;

namespace Microsoft.CodeAnalysis.SolutionCrawler
{
    internal partial class SolutionCrawlerRegistrationService : ISolutionCrawlerRegistrationService
    {
        internal static readonly Option2<bool> EnableSolutionCrawler = new("dotnet_enable_solution_crawler", defaultValue: true);

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
            [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
            public SolutionCrawlerService()
            {
            }

            public void Reanalyze(Workspace workspace, IIncrementalAnalyzer analyzer, IEnumerable<ProjectId>? projectIds, IEnumerable<DocumentId>? documentIds, bool highPriority)
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
