namespace Roslyn.Services.Shared.CodeGeneration
{
    internal class RawExpressionDefinition : ExpressionDefinition
    {
        public string Text { get; private set; }

        public RawExpressionDefinition(string text)
        {
            this.Text = text;
        }

        protected override CodeDefinition Clone()
        {
            return new RawExpressionDefinition(this.Text);
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