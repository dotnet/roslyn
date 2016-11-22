﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.IO;
using System.Threading;
using Microsoft.CodeAnalysis.CodeActions;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CodeFixes.AddImport
{
    internal abstract partial class AbstractAddImportCodeFixProvider<TSimpleNameSyntax>
    {
        private partial class MetadataSymbolReference : SymbolReference
        {
            private readonly PortableExecutableReference _reference;

            public MetadataSymbolReference(
                AbstractAddImportCodeFixProvider<TSimpleNameSyntax> provider,
                SymbolResult<INamespaceOrTypeSymbol> symbolResult, PortableExecutableReference reference)
                : base(provider, symbolResult)
            {
                _reference = reference;
            }

            protected override string TryGetDescription(
                Document document, SyntaxNode node, 
                SemanticModel semanticModel, CancellationToken cancellationToken)
            {
                // If 'TryGetDescription' returns 'null' then that means that we don't actually want to add a reference
                // in this case.  As such, just continue to return the 'null' outwards.
                var description = base.TryGetDescription(document, node, semanticModel, cancellationToken);
                if (description == null)
                {
                    return null;
                }

                return string.Format(FeaturesResources.Add_reference_to_0, 
                    Path.GetFileName(_reference.FilePath));
            }

            protected override Solution GetUpdatedSolution(Document newDocument)
                => newDocument.Project.AddMetadataReference(_reference).Solution;

            // Adding metadata references should be considered lower pri than anything else.
            protected override CodeActionPriority GetPriority(Document document)
                => CodeActionPriority.Low;

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
