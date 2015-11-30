// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
            private readonly ProjectId _projectId;

            public ProjectSymbolReference(INamespaceOrTypeSymbol symbol, ProjectId projectId)
                : base(symbol)
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

            public MetadataSymbolReference(INamespaceOrTypeSymbol symbol, PortableExecutableReference reference)
                : base(symbol)
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
