using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.CodeFixes.AddImport
{
    internal abstract partial class AbstractAddImportCodeFixProvider<TSimpleNameSyntax>
    {
        private class ProjectSymbolReference : SymbolReference
        {
            private readonly Project _project;

            public ProjectSymbolReference(
                AbstractAddImportCodeFixProvider<TSimpleNameSyntax> provider,
                SymbolResult<INamespaceOrTypeSymbol> symbolResult,
                Project project)
                : base(provider, symbolResult)
            {
                _project = project;
            }

            protected override Glyph? GetGlyph(Document document)
            {
                return document.Project.Id == _project.Id
                    ? default(Glyph?)
                    : _project.GetGlyph();
            }

            protected override Solution UpdateSolution(Document newDocument)
            {
                if (_project.Id == newDocument.Project.Id)
                {
                    // This reference was found while searching in the project for our document.  No
                    // need to make any solution changes.
                    return newDocument.Project.Solution;
                }

                // If this reference came from searching another project, then add a project reference
                // as well.
                var newProject = newDocument.Project;
                newProject = newProject.AddProjectReference(new ProjectReference(_project.Id));

                return newProject.Solution;
            }
        }
    }
}
