namespace Roslyn.Compilers.CSharp
{
    internal sealed partial class BoundReturnStatement
    {
        public BoundReturnStatement(SyntaxNode syntax, SyntaxTree syntaxTree, BoundExpression expression, bool wasCompilerGenerated, bool hasErrors = false)
            : this(syntax, syntaxTree, expression, hasErrors)
        {
            this.ExpressionOpt = expression;
            this.WasCompilerGenerated = wasCompilerGenerated;
        }
    }
}
