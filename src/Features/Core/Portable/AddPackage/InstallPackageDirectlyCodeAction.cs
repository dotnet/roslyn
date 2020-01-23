// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.Packaging;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.AddPackage
{
    internal class InstallPackageDirectlyCodeAction : CodeAction
    {
        private readonly CodeActionOperation _installPackageOperation;

        public override string Title { get; }

        public InstallPackageDirectlyCodeAction(
            IPackageInstallerService installerService,
            Document document,
            string source,
            string packageName,
            string versionOpt,
            bool includePrerelease,
            bool isLocal)
        {
            Title = versionOpt == null
                ? FeaturesResources.Find_and_install_latest_version
                : isLocal
                    ? string.Format(FeaturesResources.Use_local_version_0, versionOpt)
                    : string.Format(FeaturesResources.Install_version_0, versionOpt);

            _installPackageOperation = new InstallPackageDirectlyCodeActionOperation(
                installerService, document, source, packageName,
                versionOpt, includePrerelease, isLocal);
        }

        protected override Task<IEnumerable<CodeActionOperation>> ComputeOperationsAsync(CancellationToken cancellationToken)
            => Task.FromResult(SpecializedCollections.SingletonEnumerable(_installPackageOperation));
    }
}
