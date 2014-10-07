using Roslyn.Compilers.Common;

namespace Roslyn.Services.Shared.CodeGeneration
{
    internal abstract class IsOrAsExpressionDefinition : ExpressionDefinition
    {
        public CommonSyntaxNode Expression { get; private set; }
        public ITypeSymbol Type { get; private set; }

        public IsOrAsExpressionDefinition(CommonSyntaxNode expression, ITypeSymbol type)
        {
            this.Expression = expression;
            this.Type = type;
        }
    }
}