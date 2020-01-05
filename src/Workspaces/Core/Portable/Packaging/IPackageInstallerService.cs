// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using Microsoft.CodeAnalysis.Host;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Packaging
{
    internal interface IPackageInstallerService : IWorkspaceService
    {
        bool IsEnabled(ProjectId projectId);

        bool IsInstalled(Workspace workspace, ProjectId projectId, string packageName);

        bool TryInstallPackage(Workspace workspace, DocumentId documentId,
            string source, string packageName,
            string versionOpt, bool includePrerelease,
            CancellationToken cancellationToken);

        ImmutableArray<string> GetInstalledVersions(string packageName);

        IEnumerable<Project> GetProjectsWithInstalledPackage(Solution solution, string packageName, string version);
        bool CanShowManagePackagesDialog();
        void ShowManagePackagesDialog(string packageName);

        ImmutableArray<PackageSource> GetPackageSources();

        event EventHandler PackageSourcesChanged;
    }

    internal struct PackageSource : IEquatable<PackageSource>
    {
        public readonly string Name;
        public readonly string Source;

        public PackageSource(string name, string source)
        {
            Name = name;
            Source = source;
        }

        public override bool Equals(object obj)
            => Equals((PackageSource)obj);

        public bool Equals(PackageSource other)
            => Name == other.Name && Source == other.Source;

        public override int GetHashCode()
            => Hash.Combine(Name, Source.GetHashCode());
    }
}
