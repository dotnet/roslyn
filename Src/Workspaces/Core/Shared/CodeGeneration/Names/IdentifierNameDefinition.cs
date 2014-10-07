namespace Roslyn.Services.Shared.CodeGeneration
{
    internal class IdentifierNameDefinition : SimpleNameDefinition
    {
        public IdentifierNameDefinition(string identifier)
            : base(identifier)
        {
        }

        protected override CodeDefinition Clone()
        {
            return new IdentifierNameDefinition(this.Identifier);
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