using Roslyn.Compilers.Common;

namespace Roslyn.Services.Shared.CodeGeneration
{
    internal abstract class BinaryExpressionDefinition : ExpressionDefinition
    {
        public CommonSyntaxNode Left { get; private set; }
        public CommonSyntaxNode Right { get; private set; }

        protected BinaryExpressionDefinition(CommonSyntaxNode left, CommonSyntaxNode right)
        {
            this.Left = left;
            this.Right = right;
        }
    }
}