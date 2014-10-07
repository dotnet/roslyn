using Roslyn.Compilers.Common;

namespace Roslyn.Services.Shared.CodeGeneration
{
    internal class LocalDeclarationStatementDefinition : StatementDefinition
    {
        public bool IsConst { get; private set; }
        public CommonSyntaxNode VariableDeclaration { get; private set; }

        public LocalDeclarationStatementDefinition(bool isConst, CommonSyntaxNode variableDeclaration)
        {
            this.IsConst = isConst;
            this.VariableDeclaration = variableDeclaration;
        }

        protected override CodeDefinition Clone()
        {
            return new LocalDeclarationStatementDefinition(this.IsConst, this.VariableDeclaration);
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