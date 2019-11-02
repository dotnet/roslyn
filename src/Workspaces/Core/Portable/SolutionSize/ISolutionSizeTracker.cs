// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Composition;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;

namespace Microsoft.CodeAnalysis.SolutionSize
{
    internal interface ISolutionSizeTracker : IWorkspaceService
    {
        long GetSolutionSize(Workspace workspace, SolutionId solutionId);
    }

    [ExportWorkspaceService(typeof(ISolutionSizeTracker)), Shared]
    internal class DefaultSolutionSizeTracker : ISolutionSizeTracker
    {
        public long GetSolutionSize(Workspace workspace, SolutionId solutionId) => -1;
    }
}
