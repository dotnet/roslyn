using Roslyn.Compilers.Common;

namespace Roslyn.Services.Shared.CodeGeneration
{
    internal class VariableDeclaratorDefinition : CodeDefinition
    {
        public string Name { get; private set; }
        public CommonSyntaxNode ExpressionOpt { get; private set; }

        public VariableDeclaratorDefinition(string name, CommonSyntaxNode expressionOpt)
        {
            this.Name = name;
            this.ExpressionOpt = expressionOpt;
        }

        protected override CodeDefinition Clone()
        {
            return new VariableDeclaratorDefinition(this.Name, this.ExpressionOpt);
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