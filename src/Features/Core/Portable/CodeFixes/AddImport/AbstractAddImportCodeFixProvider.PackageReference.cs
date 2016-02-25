// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.Packaging;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CodeFixes.AddImport
{
    internal abstract partial class AbstractAddImportCodeFixProvider<TSimpleNameSyntax>
    {
        private class PackageReference : Reference
        {
            private readonly IPackageInstallerService _installerService;
            private readonly string _packageName;

            public PackageReference(
                AbstractAddImportCodeFixProvider<TSimpleNameSyntax> provider,
                IPackageInstallerService installerService,
                SearchResult searchResult,
                string packageName)
                : base(provider, searchResult)
            {
                _installerService = installerService;
                _packageName = packageName;
            }

            public override Task<CodeAction> CreateCodeActionAsync(
                Document document, SyntaxNode node, bool placeSystemNamespaceFirst, CancellationToken cancellationToken)
            {
                return Task.FromResult<CodeAction>(new PackageReferenceCodeAction(this, document, node, placeSystemNamespaceFirst));
            }

            public override bool Equals(object obj)
            {
                var reference = obj as PackageReference;
                return base.Equals(obj) &&
                    _packageName == reference._packageName;
            }

            public override int GetHashCode()
            {
                return Hash.Combine(_packageName, base.GetHashCode());
            }

            private class PackageReferenceCodeAction : CodeAction
            {
                private readonly PackageReference _reference;
                private readonly string _title;
                private readonly ImmutableArray<CodeAction> _childCodeActions;

                public override string Title => _title;

                internal override bool HasCodeActions => true;

                internal override ImmutableArray<CodeAction> GetCodeActions() => _childCodeActions;

                internal override int? Glyph => (int)CodeAnalysis.Glyph.NuGet;

                public PackageReferenceCodeAction(
                    PackageReference reference,
                    Document document,
                    SyntaxNode node,
                    bool placeSystemNamespaceFirst)
                {
                    _reference = reference;

                    _title = $"{reference.provider.GetDescription(reference.SearchResult.NameParts)} ({string.Format(FeaturesResources.from_0, reference._packageName)})";

                    // Determine what versions of this package are already installed in some project
                    // in this solution.  We'll offer to add those specific versions to this project,
                    // followed by an option to "Find and install latest version."
                    var installedVersions = reference._installerService.GetInstalledVersions(reference._packageName);
                    var codeActions = new List<CodeAction>();

                    // First add the actions to install a specific version.
                    codeActions.AddRange(installedVersions.Select(v => CreateCodeAction(document, node, placeSystemNamespaceFirst, versionOpt: v)));

                    // Now add the action to install the latest version.
                    codeActions.Add(CreateCodeAction(document, node, placeSystemNamespaceFirst, versionOpt: null));

                    // And finally the action to show the package manager dialog.
                    codeActions.Add(new InstallWithPackageManagerCodeAction(reference));

                    _childCodeActions = codeActions.ToImmutableArray();
                }

                private CodeAction CreateCodeAction(
                    Document document,
                    SyntaxNode node,
                    bool placeSystemNamespaceFirst,
                    string versionOpt)
                {
                    var title = versionOpt == null
                        ? FeaturesResources.Find_and_install_latest_version
                        : string.Format(FeaturesResources.Use_local_version_0, versionOpt);
                    return new OperationBasedCodeAction(
                        title, glyph: null, 
                        getOperations: c => GetOperationsAsync(versionOpt, document, node, placeSystemNamespaceFirst, c),
                        isApplicable: null);
                }

                private async Task<IEnumerable<CodeActionOperation>> GetOperationsAsync(
                    string versionOpt, Document document, SyntaxNode node, bool placeSystemNamespaceFirst, CancellationToken cancellationToken)
                {
                    _reference.ReplaceNameNode(ref node, ref document, cancellationToken);

                    var newDocument = await _reference.provider.AddImportAsync(
                        node, _reference.SearchResult.NameParts, document, placeSystemNamespaceFirst, cancellationToken).ConfigureAwait(false);
                    var newSolution = newDocument.Project.Solution;

                    var operation1 = new ApplyChangesOperation(newSolution);

                    var text = await document.GetTextAsync(cancellationToken).ConfigureAwait(false);
                    var operation2 = new InstallNugetPackageOperation(
                        _reference._installerService, document, _reference._packageName, versionOpt);

                    var operations = ImmutableArray.Create<CodeActionOperation>(operation1, operation2);
                    return operations;
                }
            }

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

            private class InstallNugetPackageOperation : CodeActionOperation
            {
                private readonly Document _document;
                private readonly IPackageInstallerService _installerService;
                private readonly string _packageName;
                private readonly string _versionOpt;
                private readonly List<string> _projectsWithMatchingVersion;

                public InstallNugetPackageOperation(
                    IPackageInstallerService installerService, Document document, string packageName, string versionOpt)
                {
                    _installerService = installerService;
                    _document = document;
                    _packageName = packageName;
                    _versionOpt = versionOpt;
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
                    : string.Format(FeaturesResources.Use_locally_installed_0_version_1_This_version_used_in_2,
                        _packageName, _versionOpt, string.Join(", ", _projectsWithMatchingVersion));

                internal override bool ApplyDuringTests => true;

                public override void Apply(Workspace workspace, CancellationToken cancellationToken)
                {
                    _installerService.TryInstallPackage(workspace, _document.Id, _packageName, _versionOpt, cancellationToken);
                }
            }
        }
    }
}
