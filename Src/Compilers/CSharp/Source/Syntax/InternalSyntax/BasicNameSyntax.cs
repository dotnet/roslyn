namespace Roslyn.Compilers.CSharp.InternalSyntax
{
    internal partial class IdentifierNameSyntax
    {
        public override string GetText()
        {
            return this.Identifier.Text;
        }
    }
}