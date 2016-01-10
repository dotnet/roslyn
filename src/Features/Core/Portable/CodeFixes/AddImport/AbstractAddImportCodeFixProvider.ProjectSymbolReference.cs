using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.CodeAnalysis.CodeFixes.AddImport
{
    internal abstract partial class AbstractAddImportCodeFixProvider<TSimpleNameSyntax>
    {
        private class ProjectSymbolReference : SymbolReference
        {
            private readonly ProjectId _projectId;

            public ProjectSymbolReference(AbstractAddImportCodeFixProvider<TSimpleNameSyntax> provider, SymbolResult<INamespaceOrTypeSymbol> symbolResult, ProjectId projectId)
                : base(provider, symbolResult)
            {
                _projectId = projectId;
            }

            protected override Solution UpdateSolution(Document newDocument)
            {
                if (_projectId == newDocument.Project.Id)
                {
                    // This reference was found while searching in the project for our document.  No
                    // need to make any solution changes.
                    return newDocument.Project.Solution;
                }

                // If this reference came from searching another project, then add a project reference
                // as well.
                var newProject = newDocument.Project;
                newProject = newProject.AddProjectReference(new ProjectReference(_projectId));

                return newProject.Solution;
            }
        }
    }
}
