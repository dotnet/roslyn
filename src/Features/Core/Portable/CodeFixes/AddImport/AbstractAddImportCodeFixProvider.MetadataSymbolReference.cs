using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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

            protected override Glyph? GetGlyph(Document document) => Glyph.Reference;
        }
    }
}
