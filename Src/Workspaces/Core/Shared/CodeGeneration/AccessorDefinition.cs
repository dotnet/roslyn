using System.Collections.Generic;
using Roslyn.Compilers.Common;

namespace Roslyn.Services.Shared.CodeGeneration
{
    internal class AccessorDefinition : CodeDefinition
    {
        public CommonAccessibility DeclaredAccessibility { get; private set; }
        public IList<CommonSyntaxNode> StatementsOpt { get; private set; }

        public AccessorDefinition(CommonAccessibility declaredAccessibility, IList<CommonSyntaxNode> statementsOpt)
        {
            this.DeclaredAccessibility = declaredAccessibility;
            this.StatementsOpt = statementsOpt;
        }

        protected override CodeDefinition Clone()
        {
            return new AccessorDefinition(this.DeclaredAccessibility, this.StatementsOpt);
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