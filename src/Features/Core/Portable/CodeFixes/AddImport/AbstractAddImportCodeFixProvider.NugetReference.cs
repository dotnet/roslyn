using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.Nuget;

namespace Microsoft.CodeAnalysis.CodeFixes.AddImport
{
    internal abstract partial class AbstractAddImportCodeFixProvider<TSimpleNameSyntax>
    {
        private class NugetReference : Reference
        {
            private readonly INugetPackageInstallerService _installerService;
            private readonly string _packageName;

            public NugetReference(
                AbstractAddImportCodeFixProvider<TSimpleNameSyntax> provider,
                INugetPackageInstallerService installerService,
                SearchResult searchResult,
                string packageName)
                : base(provider, searchResult)
            {
                _installerService = installerService;
                _packageName = packageName;
            }

            public override string GetDescription(SemanticModel semanticModel, SyntaxNode node)
            {
                return $"using { string.Join(".", this.SearchResult.NameParts) } (from {_packageName})";
            }

            public override async Task<ImmutableArray<CodeActionOperation>> GetOperationsAsync(Document document, SyntaxNode node, bool placeSystemNamespaceFirst, CancellationToken cancellationToken)
            {
                var newDocument = await provider.AddImportAsync(node, SearchResult.NameParts, document, placeSystemNamespaceFirst, cancellationToken).ConfigureAwait(false);
                var newSolution = newDocument.Project.Solution;

                var operation1 = new ApplyChangesOperation(newSolution);
                var operation2 = new InstallNugetPackageOperation(_installerService, document.Project, _packageName);

                var operations = ImmutableArray.Create<CodeActionOperation>(operation1, operation2);
                return operations;
            }

            private class InstallNugetPackageOperation : CodeActionOperation
            {
                private readonly Project _project;
                private readonly INugetPackageInstallerService _installerService;
                private readonly string _packageName;

                public InstallNugetPackageOperation(INugetPackageInstallerService installerService, Project project, string packageName)
                {
                    _installerService = installerService;
                    _project = project;
                    _packageName = packageName;
                }

                public override string Title => $"Install Nuget package '{_packageName}'";

                public override void Apply(Workspace workspace, CancellationToken cancellationToken)
                {
                    var currentProject = workspace.CurrentSolution.GetProject(_project.Id);
                    _installerService.InstallPackage(currentProject, _packageName);
                }
            }
        }
    }
}
