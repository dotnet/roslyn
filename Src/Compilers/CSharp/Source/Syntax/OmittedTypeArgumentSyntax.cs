namespace Roslyn.Compilers.CSharp
{
    public sealed partial class OmittedTypeArgumentSyntax : TypeSyntax
    {
        public override string PlainName
        {
            get { return string.Empty; }
        }
    }
}
