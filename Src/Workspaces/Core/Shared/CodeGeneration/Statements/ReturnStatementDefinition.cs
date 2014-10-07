using Roslyn.Compilers.Common;

namespace Roslyn.Services.Shared.CodeGeneration
{
    internal class ReturnStatementDefinition : StatementDefinition
    {
        public CommonSyntaxNode ExpressionOpt { get; private set; }

        public ReturnStatementDefinition(CommonSyntaxNode expressionOpt)
        {
            this.ExpressionOpt = expressionOpt;
        }

        protected override CodeDefinition Clone()
        {
            return new ReturnStatementDefinition(this.ExpressionOpt);
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