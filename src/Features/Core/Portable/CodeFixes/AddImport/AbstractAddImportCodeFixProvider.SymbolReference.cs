// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.CodeFixes.AddImport
{
    internal abstract partial class AbstractAddImportCodeFixProvider<TSimpleNameSyntax>
    {
        private abstract class SymbolReference : IComparable<SymbolReference>, IEquatable<SymbolReference>
        {
            public readonly SearchResult<INamespaceOrTypeSymbol> SearchResult;

            protected SymbolReference(SearchResult<INamespaceOrTypeSymbol> searchResult)
            {
                this.SearchResult = searchResult;
            }

            public int CompareTo(SymbolReference other)
            {
                // If references have different weights, order by the ones with lower weight (i.e.
                // they are better matches).
                if (this.SearchResult.Weight < other.SearchResult.Weight)
                {
                    return -1;
                }

                if (this.SearchResult.Weight > other.SearchResult.Weight)
                {
                    return 1;
                }

                // If the weight are the same, just order them based on their names.
                return INamespaceOrTypeSymbolExtensions.CompareNamespaceOrTypeSymbols(this.SearchResult.Symbol, other.SearchResult.Symbol);
            }

            public override bool Equals(object obj)
            {
                return Equals(obj as SymbolReference);
            }

            public bool Equals(SymbolReference other)
            {
                return object.Equals(this.SearchResult.Symbol, other?.SearchResult.Symbol);
            }

            public override int GetHashCode()
            {
                return this.SearchResult.Symbol.GetHashCode();
            }

            public abstract Solution UpdateSolution(Document newDocument);
        }

        private class ProjectSymbolReference : SymbolReference
        {
            private readonly ProjectId _projectId;

            public ProjectSymbolReference(SearchResult<INamespaceOrTypeSymbol> searchResult, ProjectId projectId)
                : base(searchResult)
            {
                _projectId = projectId;
            }

            public override Solution UpdateSolution(Document newDocument)
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

        private class MetadataSymbolReference : SymbolReference
        {
            private readonly PortableExecutableReference _reference;

            public MetadataSymbolReference(SearchResult<INamespaceOrTypeSymbol> searchResult, PortableExecutableReference reference)
                : base(searchResult)
            {
                _reference = reference;
            }

            public override Solution UpdateSolution(Document newDocument)
            {
                return newDocument.Project.AddMetadataReference(_reference).Solution;
            }
        }
    }
}
