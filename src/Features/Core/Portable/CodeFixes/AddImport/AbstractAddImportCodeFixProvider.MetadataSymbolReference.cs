// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
