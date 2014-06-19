using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Roslyn.Compilers.CSharp
{
    static class DeclarationUtils
    {
        internal static NamespaceOrTypeSymbol BuildSymbol(
            this MergedNamespaceOrTypeDeclaration declaration,
            NamespaceOrTypeSymbol parent,
            DiagnosticBag diagnostics)
        {
            if (declaration is MergedNamespaceDeclaration)
            {
                return BuildSymbol((MergedNamespaceDeclaration)declaration, parent, diagnostics);
            }
            else if (declaration is MergedTypeDeclaration)
            {
                return BuildSymbol((MergedTypeDeclaration)declaration, parent, diagnostics);
            }
            else
            {
                throw new InvalidOperationException();
            }
        }

        internal static NamedTypeSymbol BuildSymbol(
            this MergedTypeDeclaration declaration,
            NamespaceOrTypeSymbol parent,
            DiagnosticBag diagnostics)
        {
            return new SourceNamedTypeSymbol(parent, declaration, diagnostics);
        }

        internal static NamespaceSymbol BuildSymbol(
            this MergedNamespaceDeclaration declaration,
            NamespaceOrTypeSymbol parent,
            DiagnosticBag diagnostics)
        {
            return new SourceNamespaceSymbol(parent, declaration, diagnostics);
        }
    }
}
