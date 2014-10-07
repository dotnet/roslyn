using Roslyn.Compilers.Common;

namespace Roslyn.Services.Shared.CodeGeneration
{
    internal class ExpressionStatementDefinition : StatementDefinition
    {
        public CommonSyntaxNode Expression { get; private set; }

        public ExpressionStatementDefinition(CommonSyntaxNode expression)
        {
            this.Expression = expression;
        }

        protected override CodeDefinition Clone()
        {
            return new ExpressionStatementDefinition(this.Expression);
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