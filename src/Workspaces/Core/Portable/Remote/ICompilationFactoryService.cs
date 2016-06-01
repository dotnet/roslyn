// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading;
using Microsoft.CodeAnalysis.Execution;

namespace Microsoft.CodeAnalysis.Remote
{
    internal interface ICompilationFactoryService
    {
        Compilation GetCompilationAsync(SolutionSnapshotId snapshot, ProjectId projectId, CancellationToken cancellationToken);
    }
}
