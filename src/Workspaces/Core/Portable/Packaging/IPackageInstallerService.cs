// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
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

        ImmutableArray<Project> GetProjectsWithInstalledPackage(Solution solution, string packageName, string version);
        bool CanShowManagePackagesDialog();
        void ShowManagePackagesDialog(string packageName);

        /// <summary>
        /// Attempts to get the package sources applicable to the workspace.  Note: this call is made on a best effort
        /// basis.  If the results are not available (for example, they have not been computed, and doing so would
        /// require switching to the UI thread), then an empty array can be returned.
        /// </summary>
        /// <returns>
        /// <para>A collection of package sources.</para>
        /// </returns>
        ImmutableArray<PackageSource> TryGetPackageSources();

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
