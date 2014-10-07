using Roslyn.Compilers.Common;

namespace Roslyn.Services.Shared.CodeGeneration
{
    internal class IsExpressionDefinition : IsOrAsExpressionDefinition
    {
        public IsExpressionDefinition(CommonSyntaxNode expression, ITypeSymbol type)
            : base(expression, type)
        {
        }

        protected override CodeDefinition Clone()
        {
            return new IsExpressionDefinition(this.Expression, this.Type);
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