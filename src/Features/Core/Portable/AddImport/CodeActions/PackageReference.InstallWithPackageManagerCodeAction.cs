// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CodeFixes.AddImport
{
    internal abstract partial class AbstractAddImportCodeFixProvider<TSimpleNameSyntax>
    {
        private partial class PackageReference : Reference
        {
            private class InstallWithPackageManagerCodeAction : CodeAction
            {
                private readonly PackageReference reference;

                public InstallWithPackageManagerCodeAction(PackageReference reference)
                {
                    this.reference = reference;
                }

                public override string Title => FeaturesResources.Install_with_package_manager;

                protected override Task<IEnumerable<CodeActionOperation>> ComputeOperationsAsync(CancellationToken cancellationToken)
                {
                    return Task.FromResult(SpecializedCollections.SingletonEnumerable<CodeActionOperation>(
                        new InstallWithPackageManagerCodeActionOperation(reference)));
                }

                private class InstallWithPackageManagerCodeActionOperation : CodeActionOperation
                {
                    private readonly PackageReference reference;

                    public InstallWithPackageManagerCodeActionOperation(PackageReference reference)
                    {
                        this.reference = reference;
                    }

                    public override string Title => FeaturesResources.Install_with_package_manager;

                    public override void Apply(Workspace workspace, CancellationToken cancellationToken)
                    {
                        reference._installerService.ShowManagePackagesDialog(reference._packageName);
                    }
                }
            }
        }
    }
}
