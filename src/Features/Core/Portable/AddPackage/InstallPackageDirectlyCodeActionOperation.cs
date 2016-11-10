﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.Packaging;
using Microsoft.CodeAnalysis.Shared.Utilities;

namespace Microsoft.CodeAnalysis.AddPackage
{
    /// <summary>
    /// Operation responsible purely for installing a nuget package with a specific 
    /// version, or a the latest version of a nuget package.  Is not responsible
    /// for adding an import to user code.
    /// </summary>
    internal class InstallPackageDirectlyCodeActionOperation : CodeActionOperation
    {
        private readonly Document _document;
        private readonly IPackageInstallerService _installerService;
        private readonly string _source;
        private readonly string _packageName;
        private readonly string _versionOpt;
        private readonly bool _isLocal;
        private readonly List<string> _projectsWithMatchingVersion;

        public InstallPackageDirectlyCodeActionOperation(
            IPackageInstallerService installerService,
            Document document,
            string source,
            string packageName,
            string versionOpt,
            bool isLocal)
        {
            _installerService = installerService;
            _document = document;
            _source = source;
            _packageName = packageName;
            _versionOpt = versionOpt;
            _isLocal = isLocal;
            if (versionOpt != null)
            {
                const int projectsToShow = 5;
                var otherProjects = installerService.GetProjectsWithInstalledPackage(
                    _document.Project.Solution, packageName, versionOpt).ToList();
                _projectsWithMatchingVersion = otherProjects.Take(projectsToShow).Select(p => p.Name).ToList();
                if (otherProjects.Count > projectsToShow)
                {
                    _projectsWithMatchingVersion.Add("...");
                }
            }
        }

        public override string Title => _versionOpt == null
            ? string.Format(FeaturesResources.Find_and_install_latest_version_of_0, _packageName)
            : _isLocal
                ? string.Format(FeaturesResources.Use_locally_installed_0_version_1_This_version_used_in_colon_2, _packageName, _versionOpt, string.Join(", ", _projectsWithMatchingVersion))
                : string.Format(FeaturesResources.Install_0_1, _packageName, _versionOpt);

        internal override bool ApplyDuringTests => true;

        internal override bool TryApply(Workspace workspace, IProgressTracker progressTracker, CancellationToken cancellationToken)
        {
            return _installerService.TryInstallPackage(
                workspace, _document.Id, _source, _packageName, _versionOpt, cancellationToken);
        }
    }
}