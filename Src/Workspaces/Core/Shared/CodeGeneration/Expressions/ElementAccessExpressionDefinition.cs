using System.Collections.Generic;
using Roslyn.Compilers.Common;
using Roslyn.Utilities;

namespace Roslyn.Services.Shared.CodeGeneration
{
    internal class ElementAccessExpressionDefinition : ExpressionDefinition
    {
        public CommonSyntaxNode Expression { get; private set; }
        public IList<CommonSyntaxNode> Arguments { get; private set; }

        public ElementAccessExpressionDefinition(CommonSyntaxNode expression, IList<CommonSyntaxNode> arguments)
        {
            this.Expression = expression;
            this.Arguments = arguments ?? SpecializedCollections.EmptyList<CommonSyntaxNode>();
        }

        protected override CodeDefinition Clone()
        {
            return new InvocationExpressionDefinition(this.Expression, this.Arguments);
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