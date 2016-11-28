
namespace Microsoft.CodeAnalysis.CSharp.Syntax
{
    public sealed partial class FixedStatementSyntax : StatementSyntax
    {
        public FixedStatementSyntax AddDeclarationVariables(params VariableDeclaratorSyntax[] items)
        {
            return this.WithDeclaration(this.Declaration.WithVariables(this.Declaration.Variables.AddRange(items)));
        }
    }
}