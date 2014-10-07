using Roslyn.Compilers.Common;

namespace Roslyn.Services.Shared.CodeGeneration
{
    internal class DefaultExpressionDefinition : ExpressionDefinition
    {
        public ITypeSymbol Type { get; private set; }

        public DefaultExpressionDefinition(ITypeSymbol type)
        {
            this.Type = type;
        }

        protected override CodeDefinition Clone()
        {
            return new DefaultExpressionDefinition(this.Type);
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
