using Roslyn.Compilers.Common;

namespace Roslyn.Services.Shared.CodeGeneration
{
    internal class CastExpressionDefinition : ExpressionDefinition
    {
        public ITypeSymbol Type { get; private set; }
        public CommonSyntaxNode Expression { get; private set; }

        public CastExpressionDefinition(ITypeSymbol type, CommonSyntaxNode expression)
        {
            this.Type = type;
            this.Expression = expression;
        }

        protected override CodeDefinition Clone()
        {
            return new CastExpressionDefinition(this.Type, this.Expression);
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