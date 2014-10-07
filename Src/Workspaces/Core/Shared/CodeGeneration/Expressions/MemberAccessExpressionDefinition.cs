using Roslyn.Compilers.Common;

namespace Roslyn.Services.Shared.CodeGeneration
{
    internal class MemberAccessExpressionDefinition : ExpressionDefinition
    {
        public CommonSyntaxNode Expression { get; private set; }
        public CommonSyntaxNode SimpleName { get; private set; }

        public MemberAccessExpressionDefinition(CommonSyntaxNode expression, CommonSyntaxNode simpleName)
        {
            this.Expression = expression;
            this.SimpleName = simpleName;
        }

        protected override CodeDefinition Clone()
        {
            return new MemberAccessExpressionDefinition(this.Expression, this.SimpleName);
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