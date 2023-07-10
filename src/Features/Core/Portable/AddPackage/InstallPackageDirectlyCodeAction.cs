// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.Packaging;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.AddPackage
{
    internal class InstallPackageDirectlyCodeAction(
        IPackageInstallerService installerService,
        Document document,
        string source,
        string packageName,
        string versionOpt,
        bool includePrerelease,
        bool isLocal) : CodeAction
    {
        private readonly CodeActionOperation _installPackageOperation = new InstallPackageDirectlyCodeActionOperation(
                installerService, document, source, packageName,
                versionOpt, includePrerelease, isLocal);

        public override string Title { get; } = versionOpt == null
                ? FeaturesResources.Find_and_install_latest_version
                : isLocal
                    ? string.Format(FeaturesResources.Use_local_version_0, versionOpt)
                    : string.Format(FeaturesResources.Install_version_0, versionOpt);

        protected override Task<IEnumerable<CodeActionOperation>> ComputeOperationsAsync(CancellationToken cancellationToken)
            => Task.FromResult(SpecializedCollections.SingletonEnumerable(_installPackageOperation));
    }
}
