using Roslyn.Compilers.Common;

namespace Roslyn.Services.Shared.CodeGeneration
{
    internal class ValueNotEqualsExpressionDefinition : BinaryExpressionDefinition
    {
        public ValueNotEqualsExpressionDefinition(CommonSyntaxNode left, CommonSyntaxNode right)
            : base(left, right)
        {
        }

        protected override CodeDefinition Clone()
        {
            return new ValueNotEqualsExpressionDefinition(this.Left, this.Right);
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