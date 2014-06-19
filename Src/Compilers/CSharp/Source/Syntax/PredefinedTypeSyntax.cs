namespace Roslyn.Compilers.CSharp
{
    public sealed partial class PredefinedTypeSyntax : TypeSyntax
    {
        public override string PlainName
        {
            get
            {
                return Keyword.ValueText;
            }
        }
    }
}