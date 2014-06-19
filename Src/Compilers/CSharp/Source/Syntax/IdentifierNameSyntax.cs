namespace Roslyn.Compilers.CSharp
{
    public sealed partial class IdentifierNameSyntax : SimpleNameSyntax
    {
        public override string PlainName
        {
            get { return Identifier.ValueText; }
        }
    }
}
