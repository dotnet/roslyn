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
        bool IsEnabled { get; }

        bool IsInstalled(Workspace workspace, ProjectId projectId, string packageName);

        bool TryInstallPackage(Workspace workspace, DocumentId documentId, string source, string packageName, string versionOpt, CancellationToken cancellationToken);

        IEnumerable<string> GetInstalledVersions(string packageName);

        IEnumerable<Project> GetProjectsWithInstalledPackage(Solution solution, string packageName, string version);

        void ShowManagePackagesDialog(string packageName);

        ImmutableArray<PackageSource> PackageSources { get; }
        event EventHandler PackageSourcesChanged;
    }

    internal struct PackageSource
    {
        public readonly string Name;
        public readonly string Source;

        public PackageSource(string name, string source)
        {
            this.Name = name;
            this.Source = source;
        }
    }
}
