namespace Roslyn.Compilers.CSharp
{
    public sealed partial class ArrayTypeSyntax : TypeSyntax
    {
        public override string PlainName
        {
            get { return ElementType.PlainName; }
        }
    }
}
