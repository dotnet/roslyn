using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.Packaging;
using Microsoft.CodeAnalysis.Shared.Utilities;

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

            private class PackageReferenceCodeAction : CodeAction
            {
                private readonly PackageReference _reference;
                private readonly string _title;
                private readonly ImmutableArray<CodeAction> _childCodeActions;

                public override string Title => _title;

                internal override bool HasCodeActions => true;

                internal override ImmutableArray<CodeAction> GetCodeActions() => _childCodeActions;


                public PackageReferenceCodeAction(
                    PackageReference reference,
                    Document document,
                    SyntaxNode node,
                    bool placeSystemNamespaceFirst) 
                {
                    _reference = reference;

                    _title = $"{reference.provider.GetDescription(reference.SearchResult.NameParts)} ({string.Format(FeaturesResources.from_0, reference._packageName)})";

                    var installedVersions = reference._installerService.GetInstalledVersions(reference._packageName);
                    var versionsAndSplits = installedVersions.Select(v => new { Version = v, Split = v.Split('.') }).ToList();

                    versionsAndSplits.Sort((v1, v2) =>
                    {
                        var diff = CompareSplit(v1.Split, v2.Split);
                        return diff != 0 ? diff : -v1.Version.CompareTo(v2.Version);
                    });

                    var codeActions = new List<CodeAction>();

                    // Add an action 
                    codeActions.AddRange(installedVersions.Select(v => CreateCodeAction(document, node, placeSystemNamespaceFirst, versionOpt: v)));
                    codeActions.Add(CreateCodeAction(document, node, placeSystemNamespaceFirst, versionOpt: null));

                    _childCodeActions = codeActions.ToImmutableArray();
                }

                private int CompareSplit(string[] split1, string[] split2)
                {
                    for (int i = 0, n = Math.Min(split1.Length, split2.Length); i < n; i++)
                    {
                        // Prefer things that look larger.  i.e. 7 should come before 6. 
                        // Use a logical string comparer so that 10 is understood to be
                        // greater than 3.
                        var diff = -LogicalStringComparer.Instance.Compare(split1[i], split2[i]);
                        if (diff != 0)
                        {
                            return diff;
                        }
                    }

                    // Choose the one with more parts.
                    return split2.Length - split1.Length;
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
                        title, c => GetOperationsAsync(versionOpt, document, node, placeSystemNamespaceFirst, c));
                }

                private async Task<IEnumerable<CodeActionOperation>> GetOperationsAsync(
                    String versionOpt, Document document, SyntaxNode node, bool placeSystemNamespaceFirst, CancellationToken cancellationToken)
                {
                    var newDocument = await _reference.provider.AddImportAsync(
                        node, _reference.SearchResult.NameParts, document, placeSystemNamespaceFirst, cancellationToken).ConfigureAwait(false);
                    var newSolution = newDocument.Project.Solution;

                    var operation1 = new ApplyChangesOperation(newSolution);
                    var operation2 = new InstallNugetPackageOperation(
                        _reference._installerService, document.Project, _reference._packageName, versionOpt);

                    var operations = ImmutableArray.Create<CodeActionOperation>(operation1, operation2);
                    return operations;
                }
            }

            private class InstallNugetPackageOperation : CodeActionOperation
            {
                private readonly Project _project;
                private readonly IPackageInstallerService _installerService;
                private readonly string _packageName;
                private readonly string _versionOpt;

                public InstallNugetPackageOperation(IPackageInstallerService installerService, Project project, string packageName, string versionOpt)
                {
                    _installerService = installerService;
                    _project = project;
                    _packageName = packageName;
                    _versionOpt = versionOpt;
                }

                public override string Title => _versionOpt == null
                    ? string.Format(FeaturesResources.Find_and_install_latest_version_of_0, _packageName)
                    : string.Format(FeaturesResources.Use_locally_installed_0_version_1, _packageName, _versionOpt);

                public override void Apply(Workspace workspace, CancellationToken cancellationToken)
                {
                    var currentProject = workspace.CurrentSolution.GetProject(_project.Id);
                    _installerService.TryInstallPackage(workspace, currentProject.Id, _packageName, _versionOpt);
                }
            }
        }
    }
}
