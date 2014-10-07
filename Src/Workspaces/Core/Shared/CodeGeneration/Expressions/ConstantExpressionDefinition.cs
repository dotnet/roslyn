namespace Roslyn.Services.Shared.CodeGeneration
{
    internal sealed class ConstantExpressionDefinition : LiteralExpressionDefinition
    {
        public object Value { get; private set; }

        public ConstantExpressionDefinition(object value)
        {
            this.Value = value;
        }

        protected override CodeDefinition Clone()
        {
            return new ConstantExpressionDefinition(this.Value);
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