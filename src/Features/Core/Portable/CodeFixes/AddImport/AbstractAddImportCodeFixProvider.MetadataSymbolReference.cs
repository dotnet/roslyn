// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.IO;
using Microsoft.CodeAnalysis.CodeActions;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CodeFixes.AddImport
{
    internal abstract partial class AbstractAddImportCodeFixProvider<TSimpleNameSyntax>
    {
        private class MetadataSymbolReference : SymbolReference
        {
            private readonly PortableExecutableReference _reference;

            public MetadataSymbolReference(AbstractAddImportCodeFixProvider<TSimpleNameSyntax> provider, SymbolResult<INamespaceOrTypeSymbol> symbolResult, PortableExecutableReference reference)
                : base(provider, symbolResult)
            {
                _reference = reference;
            }

            protected override string TryGetDescription(
                Project project, SyntaxNode node, SemanticModel semanticModel)
            {
                // If 'TryGetDescription' returns 'null' then that means that we don't actually want to add a reference
                // in this case.  As such, just continue to return the 'null' outwards.
                var description = base.TryGetDescription(project, node, semanticModel);
                if (description == null)
                {
                    return null;
                }

                return string.Format(FeaturesResources.Add_reference_to_0, 
                    Path.GetFileName(_reference.FilePath));
            }

            protected override Solution UpdateSolution(Document newDocument)
            {
                return newDocument.Project.AddMetadataReference(_reference).Solution;
            }

            protected override CodeActionPriority GetPriority(Document document)
            {
                // Adding metadata references should be considered lower pri than anything else.
                return CodeActionPriority.Low;
            }

            protected override Glyph? GetGlyph(Document document) => Glyph.AddReference;

            public override bool Equals(object obj)
            {
                var reference = obj as MetadataSymbolReference;
                return base.Equals(reference) &&
                    StringComparer.OrdinalIgnoreCase.Equals(_reference.FilePath, reference._reference.FilePath);
            }

            public override int GetHashCode()
            {
                return Hash.Combine(base.GetHashCode(), StringComparer.OrdinalIgnoreCase.GetHashCode(_reference.FilePath));
            }

            protected override bool CheckForExistingImport(Project project) => false;
        }
    }
}
