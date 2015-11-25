using System;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.CodeFixes.AddImport
{
    internal abstract partial class AbstractAddImportCodeFixProvider
    {
        private abstract class SymbolReference : IComparable<SymbolReference>, IEquatable<SymbolReference>
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

            public override bool Equals(object obj)
            {
                return Equals(obj as SymbolReference);
            }

            public bool Equals(SymbolReference other)
            {
                return object.Equals(this.Symbol, other?.Symbol);
            }

            public override int GetHashCode()
            {
                return this.Symbol.GetHashCode();
            }

            public abstract Solution UpdateSolution(Document newDocument);
        }

        private class ProjectSymbolReference : SymbolReference
        {
            private readonly ProjectId projectId;

            public ProjectSymbolReference(INamespaceOrTypeSymbol symbol, ProjectId projectId)
                : base(symbol)
            {
                this.projectId = projectId;
            }

            public override Solution UpdateSolution(Document newDocument)
            {
                if (this.projectId == newDocument.Project.Id)
                {
                    // This reference was found while searching in the project for our document.  No
                    // need to make any solution changes.
                    return newDocument.Project.Solution;
                }

                // If this reference came from searching another project, then add a project reference
                // as well.
                var newProject = newDocument.Project;
                newProject = newProject.AddProjectReference(new ProjectReference(this.projectId));

                return newProject.Solution;
            }
        }

        private class MetadataSymbolReference : SymbolReference
        {
            private readonly PortableExecutableReference reference;

            public MetadataSymbolReference(INamespaceOrTypeSymbol symbol, PortableExecutableReference reference)
                : base(symbol)
            {
                this.reference = reference;
            }

            public override Solution UpdateSolution(Document newDocument)
            {
                return newDocument.Project.AddMetadataReference(this.reference).Solution;
            }
        }
    }
}
