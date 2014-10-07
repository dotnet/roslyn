using Roslyn.Compilers.Common;
namespace Roslyn.Services.Shared.CodeGeneration
{
    internal class QualifiedNameDefinition : NameDefinition
    {
        public CommonSyntaxNode Left { get; private set; }
        public CommonSyntaxNode Right { get; private set; }

        public QualifiedNameDefinition(CommonSyntaxNode left, CommonSyntaxNode right)
        {
            this.Left = left;
            this.Right = right;
        }

        protected override CodeDefinition Clone()
        {
            return new QualifiedNameDefinition(this.Left, this.Right);
        }

        public override void Accept(ICodeDefinitionVisitor visitor)
        {
            visitor.Visit(this);
        }

        public override T Accept<T>(ICodeDefinitionVisitor<T> visitor)
        {
            return visitor.Visit(this);
        }

        public override TResult Accept<TArgument, TResult>(ICodeDefinitionVisitor<TArgument, TResult> visitor, TArgument argument)
        {
            return visitor.Visit(this, argument);
        }
    }
}