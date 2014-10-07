using System.Collections.Generic;
using Roslyn.Compilers.Common;

namespace Roslyn.Services.Shared.CodeGeneration
{
    internal class UsingStatementDefinition : StatementDefinition
    {
        public CommonSyntaxNode VariableDeclarationOrExpression { get; private set; }
        public IList<CommonSyntaxNode> Statements { get; private set; }

        public UsingStatementDefinition(CommonSyntaxNode variableDeclarationOrExpression, IList<CommonSyntaxNode> statements)
        {
            this.VariableDeclarationOrExpression = variableDeclarationOrExpression;
            this.Statements = statements;
        }

        protected override CodeDefinition Clone()
        {
            return new UsingStatementDefinition(this.VariableDeclarationOrExpression, this.Statements);
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