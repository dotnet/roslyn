using System;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.CodeFixes.AddImport
{
    internal abstract partial class AbstractAddImportCodeFixProvider
    {
        private abstract class SymbolReference : IComparable<SymbolReference>
        {
            public readonly INamespaceOrTypeSymbol Symbol;

            protected SymbolReference(INamespaceOrTypeSymbol symbol)
            {
                this.Symbol = symbol;
            }

            public int CompareTo(SymbolReference other)
            {
                return INamespaceOrTypeSymbolExtensions.CompareNamespaceOrTypeSymbols(this.Symbol, other.Symbol);
            }

            public abstract Solution UpdateSolution(Document newDocument);
        }

        private class ProjectSymbolReference : SymbolReference
        {
            public readonly ProjectId ProjectId;

            public ProjectSymbolReference(INamespaceOrTypeSymbol symbol, ProjectId projectId)
                : base(symbol)
            {
                ProjectId = projectId;
            }

            public override Solution UpdateSolution(Document newDocument)
            {
                if (this.ProjectId == newDocument.Project.Id)
                {
                    // This reference was found while searching in the project for our document.  No
                    // need to make any solution changes.
                    return newDocument.Project.Solution;
                }

                // If this reference came from searching another project, then add a project reference
                // as well.
                var newProject = newDocument.Project;
                newProject = newProject.AddProjectReference(new ProjectReference(this.ProjectId));

                return newProject.Solution;
            }
        }
    }
}
