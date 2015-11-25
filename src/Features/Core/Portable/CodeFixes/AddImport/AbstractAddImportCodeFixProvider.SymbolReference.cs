using System;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.CodeFixes.AddImport
{
    internal abstract partial class AbstractAddImportCodeFixProvider
    {
        private struct SymbolReference : IComparable<SymbolReference>
        {
            public readonly INamespaceOrTypeSymbol Symbol;
            public readonly ProjectId ProjectId;

            public SymbolReference(INamespaceOrTypeSymbol symbol, ProjectId projectId)
            {
                Symbol = symbol;
                ProjectId = projectId;
            }

            public int CompareTo(SymbolReference other)
            {
                return INamespaceOrTypeSymbolExtensions.CompareNamespaceOrTypeSymbols(this.Symbol, other.Symbol);
            }

            internal Solution UpdateSolution(Document newDocument)
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
