namespace Roslyn.Compilers.CSharp
{
    public sealed partial class NullableTypeSyntax : TypeSyntax
    {
        public override string PlainName
        {
            get { return ElementType.PlainName; }
        }
    }
}
