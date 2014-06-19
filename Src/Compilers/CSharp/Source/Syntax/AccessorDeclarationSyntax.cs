
namespace Roslyn.Compilers.CSharp
{
    public sealed partial class AccessorDeclarationSyntax
    {
        internal PropertyDeclarationSyntax PropertyDeclaration
        {
            get
            {
                return this.Parent.Parent as PropertyDeclarationSyntax;
            }
        }

        internal IndexerDeclarationSyntax IndexerDeclaration
        {
            get
            {
                return this.Parent.Parent as IndexerDeclarationSyntax;
            }
        }
    }
}