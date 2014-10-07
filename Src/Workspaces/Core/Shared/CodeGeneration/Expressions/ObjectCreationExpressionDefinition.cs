using System.Collections.Generic;
using Roslyn.Compilers.Common;

namespace Roslyn.Services.Shared.CodeGeneration
{
    internal class ObjectCreationExpressionDefinition : ExpressionDefinition
    {
        public ITypeSymbol Type { get; private set; }
        public IList<CommonSyntaxNode> Arguments { get; private set; }

        public ObjectCreationExpressionDefinition(ITypeSymbol type, IList<CommonSyntaxNode> arguments)
        {
            this.Type = type;
            this.Arguments = arguments;
        }

        protected override CodeDefinition Clone()
        {
            return new ObjectCreationExpressionDefinition(this.Type, this.Arguments);
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