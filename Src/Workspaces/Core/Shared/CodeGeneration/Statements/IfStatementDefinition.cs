using System.Collections.Generic;
using Roslyn.Compilers.Common;

namespace Roslyn.Services.Shared.CodeGeneration
{
    internal class IfStatementDefinition : StatementDefinition
    {
        public CommonSyntaxNode Condition { get; private set; }
        public IList<CommonSyntaxNode> TrueStatements { get; private set; }
        public IList<CommonSyntaxNode> FalseStatementsOpt { get; private set; }

        public IfStatementDefinition(CommonSyntaxNode condition, IList<CommonSyntaxNode> trueStatements, IList<CommonSyntaxNode> falseStatementsOpt)
        {
            this.Condition = condition;
            this.TrueStatements = trueStatements;
            this.FalseStatementsOpt = falseStatementsOpt;
        }

        protected override CodeDefinition Clone()
        {
            return new IfStatementDefinition(this.Condition, this.TrueStatements, this.FalseStatementsOpt);
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