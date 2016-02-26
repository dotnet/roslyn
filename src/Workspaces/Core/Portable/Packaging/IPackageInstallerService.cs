// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Packaging
{
    internal interface IPackageInstallerService : IWorkspaceService
    {
        bool IsInstalled(Workspace workspace, ProjectId projectId, string packageName);

        bool TryInstallPackage(Workspace workspace, DocumentId documentId, string packageName, string versionOpt, CancellationToken cancellationToken);

        IEnumerable<string> GetInstalledVersions(string packageName);

        IEnumerable<Project> GetProjectsWithInstalledPackage(Solution solution, string packageName, string version);

        void ShowManagePackagesDialog(string packageName);

        ImmutableArray<string> PackageSources { get; }
        bool IsEnabled { get; }
    }
}
