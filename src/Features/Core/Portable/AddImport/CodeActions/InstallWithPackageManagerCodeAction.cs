// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.Packaging;

namespace Microsoft.CodeAnalysis.AddImport;

internal abstract partial class AbstractAddImportFeatureService<TSimpleNameSyntax>
{
    private sealed class InstallWithPackageManagerCodeAction(
        IPackageInstallerService installerService,
        string packageName) : CodeAction
    {
        private readonly IPackageInstallerService _installerService = installerService;
        private readonly string _packageName = packageName;

        public override string Title => FeaturesResources.Install_with_package_manager;

        protected override Task<ImmutableArray<CodeActionOperation>> ComputeOperationsAsync(
            IProgress<CodeAnalysisProgress> progress, CancellationToken cancellationToken)
        {
            return Task.FromResult(ImmutableArray.Create<CodeActionOperation>(
                new InstallWithPackageManagerCodeActionOperation(_installerService, _packageName)));
        }

        private sealed class InstallWithPackageManagerCodeActionOperation(
            IPackageInstallerService installerService,
            string packageName) : CodeActionOperation
        {
            private readonly IPackageInstallerService _installerService = installerService;
            private readonly string _packageName = packageName;

            public override string Title => FeaturesResources.Install_with_package_manager;

            public override void Apply(Workspace workspace, CancellationToken cancellationToken)
                => _installerService.ShowManagePackagesDialog(_packageName);
        }
    }
}
