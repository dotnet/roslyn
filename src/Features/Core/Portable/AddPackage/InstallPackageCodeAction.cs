// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.Packaging;
using Microsoft.CodeAnalysis.Shared.Utilities;

namespace Microsoft.CodeAnalysis.AddPackage
{
    internal class InstallPackageCodeAction : CodeAction
    {
        private readonly CodeActionOperation _installPackageOperation;

        public override string Title => _installPackageOperation.Title;

        public InstallPackageCodeAction(
            IPackageInstallerService installerService,
            Document document,
            string source,
            string packageName,
            string versionOpt,
            bool isLocal)
        {
            _installPackageOperation = new InstallPackageCodeActionOperation(
                installerService, document, source, packageName, versionOpt, isLocal);
        }

        internal override Task<ImmutableArray<CodeActionOperation>> ComputeOperationsAsync(
            IProgressTracker progressTracker, CancellationToken cancellationToken)
        {
            return Task.FromResult(ImmutableArray.Create(_installPackageOperation));
        }
    }
}