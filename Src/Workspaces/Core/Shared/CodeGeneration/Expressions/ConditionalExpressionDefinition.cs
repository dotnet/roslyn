using System.Diagnostics.CodeAnalysis;
using Roslyn.Compilers.Common;

namespace Roslyn.Services.Shared.CodeGeneration
{
    [ExcludeFromCodeCoverage]
    internal class ConditionalExpressionDefinition : ExpressionDefinition
    {
        public CommonSyntaxNode Condition { get; private set; }
        public CommonSyntaxNode WhenTrue { get; private set; }
        public CommonSyntaxNode WhenFalse { get; private set; }

        public ConditionalExpressionDefinition(CommonSyntaxNode condition, CommonSyntaxNode whenTrue, CommonSyntaxNode whenFalse)
        {
            this.Condition = condition;
            this.WhenTrue = whenTrue;
            this.WhenFalse = whenFalse;
        }

        protected override CodeDefinition Clone()
        {
            return new ConditionalExpressionDefinition(this.Condition, this.WhenTrue, this.WhenFalse);
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