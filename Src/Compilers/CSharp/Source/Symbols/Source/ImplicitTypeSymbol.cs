namespace Roslyn.Compilers.CSharp
{
    internal sealed class ImplicitTypeSymbol : SourceNamedTypeSymbol
    {
        internal ImplicitTypeSymbol(NamespaceOrTypeSymbol containingSymbol, MergedTypeDeclaration declaration, DiagnosticBag diagnostics)
            : base(containingSymbol, declaration, diagnostics)
        {
        }

        public override bool IsImplicitClass
        {
            get
            {
                return true;
            }
        }
    }
}
