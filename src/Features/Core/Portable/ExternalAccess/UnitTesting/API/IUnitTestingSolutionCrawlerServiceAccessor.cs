// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using Microsoft.CodeAnalysis.Host;

namespace Microsoft.CodeAnalysis.ExternalAccess.UnitTesting.Api
{
    internal interface IUnitTestingSolutionCrawlerServiceAccessor : IWorkspaceService
    {
        void Reanalyze(Workspace workspace, IUnitTestingIncrementalAnalyzerImplementation analyzer, IEnumerable<ProjectId> projectIds = null, IEnumerable<DocumentId> documentIds = null, bool highPriority = false);
    }
}
