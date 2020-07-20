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

        IEnumerable<Project> GetProjectsWithInstalledPackage(Solution solution, string packageName, string version);
        bool CanShowManagePackagesDialog();
        void ShowManagePackagesDialog(string packageName);

        /// <summary>
        /// Gets the package sources applicable to the workspace.
        /// </summary>
        /// <param name="allowSwitchToMainThread"><see langword="true"/> to allow the implementation to switch to the
        /// main thread (if necessary) to compute the result; otherwise <see langword="false"/> to return without an
        /// answer if such a switch would be required.</param>
        /// <param name="cancellationToken">The cancellation token that the asynchronous operation will observe.</param>
        /// <returns>
        /// <para>A collection of package sources.</para>
        /// <para>-or-</para>
        /// <para><see langword="null"/> if <paramref name="allowSwitchToMainThread"/> is <see langword="false"/> and
        /// the package sources could not be computed without switching to the main thread.</para>
        /// <para>-or-</para>
        /// <para><see langword="null"/> if the package sources were invalidated by the project system before the
        /// computation completed.</para>
        /// </returns>
        ValueTask<ImmutableArray<PackageSource>?> TryGetPackageSourcesAsync(bool allowSwitchToMainThread, CancellationToken cancellationToken);

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
