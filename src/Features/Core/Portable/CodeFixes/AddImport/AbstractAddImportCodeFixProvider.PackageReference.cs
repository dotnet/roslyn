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
            private readonly string _source;
            private readonly string _packageName;
            private readonly string _versionOpt;

            public PackageReference(
                AbstractAddImportCodeFixProvider<TSimpleNameSyntax> provider,
                IPackageInstallerService installerService,
                SearchResult searchResult,
                string source,
                string packageName,
                string versionOpt)
                : base(provider, searchResult)
            {
                _installerService = installerService;
                _source = source;
                _packageName = packageName;
                _versionOpt = versionOpt;
            }

            public override Task<CodeAction> CreateCodeActionAsync(
                Document document, SyntaxNode node, bool placeSystemNamespaceFirst, CancellationToken cancellationToken)
            {
                return Task.FromResult<CodeAction>(new PackageReferenceCodeAction(
                    this, document, node, placeSystemNamespaceFirst));
            }

            public override bool Equals(object obj)
            {
                var reference = obj as PackageReference;
                return base.Equals(obj) &&
                    _packageName == reference._packageName &&
                    _versionOpt == reference._versionOpt;
            }

            public override int GetHashCode()
            {
                return Hash.Combine(_versionOpt,
                    Hash.Combine(_packageName, base.GetHashCode()));
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

                // Adding a nuget reference is lower priority than other fixes..
                internal override CodeActionPriority Priority => CodeActionPriority.Low;

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
                    codeActions.AddRange(installedVersions.Select(
                        v => CreateCodeAction(document, node, placeSystemNamespaceFirst, versionOpt: v, isLocal: true)));

                    // Now add the action to install the specific version.
                    var preferredVersion = _reference._versionOpt;
                    if (preferredVersion == null || !installedVersions.Contains(preferredVersion))
                    {
                        codeActions.Add(CreateCodeAction(document, node, placeSystemNamespaceFirst,
                            versionOpt: _reference._versionOpt, isLocal: false));
                    }

                    // And finally the action to show the package manager dialog.
                    codeActions.Add(new InstallWithPackageManagerCodeAction(reference));

                    _childCodeActions = codeActions.ToImmutableArray();
                }

                private CodeAction CreateCodeAction(
                    Document document,
                    SyntaxNode node,
                    bool placeSystemNamespaceFirst,
                    string versionOpt,
                    bool isLocal)
                {
                    var title = versionOpt == null
                        ? FeaturesResources.Find_and_install_latest_version
                        : isLocal
                            ? string.Format(FeaturesResources.Use_local_version_0, versionOpt)
                            : string.Format(FeaturesResources.Install_version_0, versionOpt);

                    // Nuget hits should always come after other results.
                    return new OperationBasedCodeAction(
                        title, glyph: null, priority: CodeActionPriority.Low,
                        getOperations: c => GetOperationsAsync(versionOpt, isLocal, document, node, placeSystemNamespaceFirst, c),
                        isApplicable: null);
                }

                private async Task<IEnumerable<CodeActionOperation>> GetOperationsAsync(
                    string versionOpt, 
                    bool isLocal,
                    Document document, 
                    SyntaxNode node, 
                    bool placeSystemNamespaceFirst, 
                    CancellationToken cancellationToken)
                {
                    _reference.ReplaceNameNode(ref node, ref document, cancellationToken);

                    var newDocument = await _reference.provider.AddImportAsync(
                        node, _reference.SearchResult.NameParts, document, placeSystemNamespaceFirst, cancellationToken).ConfigureAwait(false);
                    var newSolution = newDocument.Project.Solution;

                    var operation1 = new ApplyChangesOperation(newSolution);

                    var text = await document.GetTextAsync(cancellationToken).ConfigureAwait(false);
                    var operation2 = new InstallNugetPackageOperation(
                        _reference._installerService, document, _reference._source, _reference._packageName, versionOpt, isLocal);

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
                private readonly string _source;
                private readonly string _packageName;
                private readonly string _versionOpt;
                private readonly bool _isLocal;
                private readonly List<string> _projectsWithMatchingVersion;

                public InstallNugetPackageOperation(
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
                        ? string.Format(FeaturesResources.Use_locally_installed_0_version_1_This_version_used_in_2, _packageName, _versionOpt, string.Join(", ", _projectsWithMatchingVersion))
                        : string.Format(FeaturesResources.Install_0_1, _packageName, _versionOpt);

                internal override bool ApplyDuringTests => true;

                public override void Apply(Workspace workspace, CancellationToken cancellationToken)
                {
                    _installerService.TryInstallPackage(
                        workspace, _document.Id, _source, _packageName, _versionOpt, cancellationToken);
                }
            }
        }
    }
}
