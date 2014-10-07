using Roslyn.Compilers.Common;
namespace Roslyn.Services.Shared.CodeGeneration
{
    internal class LogicalNotExpressionDefinition : ExpressionDefinition
    {
        public CommonSyntaxNode Expression { get; private set; }

        public LogicalNotExpressionDefinition(CommonSyntaxNode expression)
        {
            this.Expression = expression;
        }

        protected override CodeDefinition Clone()
        {
            return new LogicalNotExpressionDefinition(this.Expression);
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