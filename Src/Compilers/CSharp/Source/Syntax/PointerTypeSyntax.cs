namespace Roslyn.Compilers.CSharp
{
    public sealed partial class PointerTypeSyntax : TypeSyntax
    {
        public override string PlainName
        {
            get { return ElementType.PlainName; }
        }
    }
}
