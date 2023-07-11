// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Runtime.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Shared.Utilities;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Packaging
{
    internal interface IPackageInstallerService : IWorkspaceService
    {
        bool IsEnabled(ProjectId projectId);

        bool IsInstalled(ProjectId projectId, string packageName);

        Task<bool> TryInstallPackageAsync(
            Workspace workspace, DocumentId documentId,
            string source, string packageName,
            string? version, bool includePrerelease,
            IProgressTracker progressTracker, CancellationToken cancellationToken);

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

    [DataContract]
    internal readonly struct PackageSource(string name, string source) : IEquatable<PackageSource>
    {
        [DataMember(Order = 0)]
        public readonly string Name = name;

        [DataMember(Order = 1)]
        public readonly string Source = source;

        public override bool Equals(object? obj)
            => obj is PackageSource source && Equals(source);

        public bool Equals(PackageSource other)
            => Name == other.Name && Source == other.Source;

        public override int GetHashCode()
            => Hash.Combine(Name, Source.GetHashCode());
    }
}
