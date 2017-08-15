// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host;

namespace Microsoft.CodeAnalysis.Versions
{
    /// <summary>
    /// A service that knows how to convert semantic version to project version
    /// </summary>
    internal interface ISemanticVersionTrackingService : IWorkspaceService
    {
        VersionStamp GetInitialProjectVersionFromSemanticVersion(Project project, VersionStamp semanticVersion);
        VersionStamp GetInitialDependentProjectVersionFromDependentSemanticVersion(Project project, VersionStamp dependentSemanticVersion);

        void LoadInitialSemanticVersions(Solution solution);
        void LoadInitialSemanticVersions(Project project);
        Task RecordSemanticVersionsAsync(Project project, CancellationToken cancellationToken);
    }
}
