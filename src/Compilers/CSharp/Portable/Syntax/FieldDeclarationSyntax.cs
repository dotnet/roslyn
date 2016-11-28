
namespace Microsoft.CodeAnalysis.CSharp.Syntax
{
    public sealed partial class FieldDeclarationSyntax : BaseFieldDeclarationSyntax
    {
        public FieldDeclarationSyntax AddDeclarationVariables(params VariableDeclaratorSyntax[] items)
        {
            return this.WithDeclaration(this.Declaration.WithVariables(this.Declaration.Variables.AddRange(items)));
        }
    }
}