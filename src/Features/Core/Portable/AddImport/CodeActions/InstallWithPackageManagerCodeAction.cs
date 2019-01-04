// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.Packaging;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.AddImport
{
    internal abstract partial class AbstractAddImportFeatureService<TSimpleNameSyntax>
    {
        private class InstallWithPackageManagerCodeAction : CodeAction
        {
            private readonly IPackageInstallerService _installerService;
            private readonly string _packageName;

            public InstallWithPackageManagerCodeAction(
                IPackageInstallerService installerService,
                string packageName)
            {
                _installerService = installerService;
                _packageName = packageName;
            }

            public override string Title => FeaturesResources.Install_with_package_manager;

            protected override Task<IEnumerable<CodeActionOperation>> ComputeOperationsAsync(CancellationToken cancellationToken)
            {
                return Task.FromResult(SpecializedCollections.SingletonEnumerable<CodeActionOperation>(
                    new InstallWithPackageManagerCodeActionOperation(_installerService, _packageName)));
            }

            private class InstallWithPackageManagerCodeActionOperation : CodeActionOperation
            {
                private readonly IPackageInstallerService _installerService;
                private readonly string _packageName;

                public InstallWithPackageManagerCodeActionOperation(
                    IPackageInstallerService installerService,
                    string packageName)
                {
                    _installerService = installerService;
                    _packageName = packageName;
                }

                public override string Title => FeaturesResources.Install_with_package_manager;

                public override void Apply(Workspace workspace, CancellationToken cancellationToken)
                    => _installerService.ShowManagePackagesDialog(_packageName);
            }
        }
    }
}
