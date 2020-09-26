// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis.Host;

namespace Microsoft.CodeAnalysis.ExternalAccess.UnitTesting.Api
{
    [Obsolete]
    internal interface IUnitTestingSolutionCrawlerServiceAccessor : IWorkspaceService
    {
        void Reanalyze(Workspace workspace, IEnumerable<ProjectId> projectIds = null, IEnumerable<DocumentId> documentIds = null, bool highPriority = false);

        void AddAnalyzerProvider(IUnitTestingIncrementalAnalyzerProviderImplementation provider, UnitTestingIncrementalAnalyzerProviderMetadataWrapper metadata);
    }
}
